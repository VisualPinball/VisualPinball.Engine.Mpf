// Visual Pinball Engine
// Copyright (C) 2026 freezy and VPE Team
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections.Generic;

namespace VisualPinball.Engine.Mpf.Unity
{
    /// <summary>The kind of an MPF output, as reported to native output listeners.</summary>
    public enum MpfOutputKind
    {
        Lamp,
        GeneralIllumination,
        Coil,
    }

    /// <summary>Receives the output changes of one <see cref="MpfOutputModel.Poll"/>.</summary>
    public interface IMpfOutputSink
    {
        /// <param name="kind">The output kind.</param>
        /// <param name="number">
        /// The output's number from the machine config, or -1 when it is not a plain integer.
        /// </param>
        /// <param name="value">
        /// Lamps and GI: brightness 0..1, already faded. Coils: 0 or 1.
        /// </param>
        void OutputChanged(MpfOutputKind kind, int number, double value);
    }

    /// <summary>
    /// Tracks the state of every light and coil MPF drives over the VPE platform, independent of
    /// the table's mapping, and reports their changes by number once per poll.
    /// </summary>
    /// <remarks>
    /// <para>
    /// MPF's <c>visual_pinball_engine</c> platform passes the <c>number:</c> of the machine config
    /// through as an opaque string (see <c>mpf/platforms/visual_pinball_engine</c>). Coils arrive
    /// as that string. Lights arrive as <c>&lt;subtype&gt;-&lt;number&gt;</c>, where the subtype
    /// is <c>light</c> unless the config sets one, and each channel of a multi-channel light is
    /// its own number. This model reports:
    /// <list type="bullet">
    /// <item>coils by their config number;</item>
    /// <item>lights of subtype <c>light</c> as lamps and of subtype <c>gi</c> as GI strings, by
    /// their config number;</item>
    /// <item>number -1 for anything else: numbers that are not plain non-negative integers (such as
    /// FAST <c>0-3</c> or P-ROC <c>A0-B0-1</c>) and lights of other subtypes, whose numbers
    /// would overlap with those of subtype <c>light</c>.</item>
    /// </list>
    /// </para>
    /// <para>
    /// MPF delegates fades to the platform (the VPE light implements <c>set_fade</c> and sends the
    /// target brightness with a fade time), so this model interpolates them linearly, as MPF's
    /// software fades do, and reports the faded brightness. Coils are reported on or off: on
    /// from an enable or pulse until a disable or the pulse's end. Pulse and hold power are not
    /// modelled.
    /// </para>
    /// <para>
    /// MPF fires flipper, slingshot and pop bumper coils through hardware rules, which VPE runs as
    /// wires without telling the engine. The model keeps MPF's rules and applies them to the
    /// switch changes the engine receives: a rule turns its coil on when the switch closes and
    /// off when it opens, or after the pulse time when the rule has no hold power. Removing a rule
    /// releases what it drives. A coil stays on while anything drives it: MPF's own enable or
    /// pulse, or any rule; MPF's disable turns it off regardless.
    /// </para>
    /// <para>
    /// Every command is stamped with the clock when it is queued, and <see cref="Poll"/> applies
    /// commands and pulse ends in that order, so a late poll does not stretch fades or pulses.
    /// Commands are only queued while attached (<see cref="Attach"/>, <see cref="Detach"/>); the
    /// check happens under the queue lock, so nothing queued before a detach survives it.
    /// </para>
    /// <para>
    /// The queue and rule methods may be called from any thread; <see cref="Poll"/> and
    /// <see cref="Detach"/> belong to one thread (Unity's main thread). Once every output has been
    /// seen and the queues have grown, polling does not allocate.
    /// </para>
    /// </remarks>
    public sealed class MpfOutputModel
    {
        /// <summary>The subtype MPF's VPE platform gives lights that have none in the config.</summary>
        public const string LampSubtype = "light";

        /// <summary>The light subtype reported as a GI string.</summary>
        public const string GiSubtype = "gi";

        private enum CommandType
        {
            Fade,
            Pulse,
            Enable,
            Disable,
            RuleOn,
            RuleOff,
        }

        private struct Command
        {
            public CommandType Type;
            public string Number;
            public string Switch;
            public float Brightness;
            public uint Milliseconds;
            public bool Hold;
            public double TimeMs;
        }

        private struct Rule
        {
            public string Coil;
            public uint PulseMs;
            public bool Hold;
        }

        /// <summary>How MPF's own commands drive a coil.</summary>
        private enum Drive
        {
            None,
            Held,
            Pulse,
        }

        /// <summary>A hardware rule currently driving a coil, keyed by its switch.</summary>
        private struct RuleDrive
        {
            public string Switch;
            public bool Hold;
            public double EndMs;
        }

        private sealed class Output
        {
            public MpfOutputKind Kind;
            public int Number;

            // lights
            public double Start;
            public double Target;
            public double FadeStartMs;
            public double FadeEndMs;
            public bool Fading;
            public bool InFadeList;
            public bool Dirty;
            public double Reported;

            // coils
            public bool On;
            public Drive Explicit;
            public double ExplicitEndMs;
            public List<RuleDrive> Rules;
            public double PulseEndMs; // earliest end of any pulse driving the coil
            public bool Pulsing; // in _pulsing
        }

        private readonly Func<double> _clockMs;

        // guarded by _queueLock
        private readonly object _queueLock = new();
        private List<Command> _incoming = new();
        private bool _attached;
        private readonly Dictionary<string, List<Rule>> _rules = new(StringComparer.Ordinal);

        // owned by the polling thread
        private List<Command> _processing = new();
        private readonly Dictionary<string, Output> _lights = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Output> _coils = new(StringComparer.Ordinal);
        private readonly List<Output> _fading = new();
        private readonly List<Output> _dirty = new();
        private readonly List<Output> _pulsing = new();

        /// <param name="clockMs">A monotonic clock in milliseconds, safe to call from any thread.</param>
        public MpfOutputModel(Func<double> clockMs)
        {
            _clockMs = clockMs ?? throw new ArgumentNullException(nameof(clockMs));
        }

        /// <summary>Starts accepting commands.</summary>
        public void Attach()
        {
            lock (_queueLock)
            {
                _attached = true;
            }
        }

        /// <summary>
        /// Stops accepting commands, drops the queued ones and forgets all output states. Hardware
        /// rules are kept, since MPF configured them independently of any listener.
        /// </summary>
        public void Detach()
        {
            lock (_queueLock)
            {
                _attached = false;
                _incoming.Clear();
            }
            _processing.Clear();
            _lights.Clear();
            _coils.Clear();
            _fading.Clear();
            _dirty.Clear();
            _pulsing.Clear();
        }

        /// <summary>Queues MPF's <c>FadeLightRequest</c> for one channel.</summary>
        /// <param name="lightNumber">The channel's number as MPF sends it, such as <c>light-12</c>.</param>
        /// <param name="targetBrightness">The brightness to reach, 0..1.</param>
        /// <param name="fadeMs">The fade duration; 0 sets the brightness at once.</param>
        public void EnqueueFade(string lightNumber, float targetBrightness, uint fadeMs) =>
            Enqueue(CommandType.Fade, lightNumber, targetBrightness, fadeMs);

        /// <summary>Queues MPF's <c>PulseCoilRequest</c>.</summary>
        public void EnqueuePulse(string coilNumber, uint pulseMs) =>
            Enqueue(CommandType.Pulse, coilNumber, 0, pulseMs);

        /// <summary>Queues MPF's <c>EnableCoilRequest</c>.</summary>
        public void EnqueueEnable(string coilNumber) =>
            Enqueue(CommandType.Enable, coilNumber, 0, 0);

        /// <summary>Queues MPF's <c>DisableCoilRequest</c>.</summary>
        public void EnqueueDisable(string coilNumber) =>
            Enqueue(CommandType.Disable, coilNumber, 0, 0);

        /// <summary>
        /// Records MPF's <c>ConfigureHardwareRuleRequest</c>, replacing an earlier rule between the
        /// same switch and coil. Accepted while detached.
        /// </summary>
        /// <param name="switchNumber">MPF's switch number.</param>
        /// <param name="coilNumber">MPF's coil number.</param>
        /// <param name="pulseMs">How long the coil fires when the rule has no hold power.</param>
        /// <param name="holdPower">
        /// MPF's hold power; above zero, the coil stays on while the switch is closed.
        /// </param>
        public void ConfigureRule(
            string switchNumber,
            string coilNumber,
            uint pulseMs,
            float holdPower
        )
        {
            if (string.IsNullOrEmpty(switchNumber) || string.IsNullOrEmpty(coilNumber))
                return;
            var rule = new Rule
            {
                Coil = coilNumber,
                PulseMs = pulseMs,
                Hold = holdPower > 0,
            };
            lock (_queueLock)
            {
                if (!_rules.TryGetValue(switchNumber, out var rules))
                {
                    rules = new List<Rule>();
                    _rules[switchNumber] = rules;
                }
                for (var i = 0; i < rules.Count; i++)
                {
                    if (rules[i].Coil == coilNumber)
                    {
                        rules[i] = rule;
                        return;
                    }
                }
                rules.Add(rule);
            }
        }

        /// <summary>
        /// Removes the rule between a switch and a coil (MPF's <c>RemoveHardwareRuleRequest</c>)
        /// and queues the release of what it drives. The coil stays on if something else drives it.
        /// </summary>
        public void RemoveRule(string switchNumber, string coilNumber)
        {
            if (string.IsNullOrEmpty(switchNumber) || string.IsNullOrEmpty(coilNumber))
                return;
            lock (_queueLock)
            {
                if (!_rules.TryGetValue(switchNumber, out var rules))
                    return;
                var removed = false;
                for (var i = rules.Count - 1; i >= 0; i--)
                {
                    if (rules[i].Coil == coilNumber)
                    {
                        rules.RemoveAt(i);
                        removed = true;
                    }
                }
                // release whatever the rule drives now (MPF clears rules on tilt without disabling)
                if (removed && _attached)
                {
                    _incoming.Add(
                        new Command
                        {
                            Type = CommandType.RuleOff,
                            Number = coilNumber,
                            Switch = switchNumber,
                            TimeMs = _clockMs(),
                        }
                    );
                }
            }
        }

        /// <summary>
        /// Applies the hardware rules of a switch that changed, queuing what their coils do.
        /// </summary>
        /// <param name="switchNumber">MPF's switch number.</param>
        /// <param name="isClosed">Whether the switch is now closed.</param>
        public void EnqueueSwitch(string switchNumber, bool isClosed)
        {
            if (string.IsNullOrEmpty(switchNumber))
                return;
            lock (_queueLock)
            {
                if (
                    !_attached
                    || !_rules.TryGetValue(switchNumber, out var rules)
                    || rules.Count == 0
                )
                    return;
                var now = _clockMs();
                foreach (var rule in rules)
                {
                    _incoming.Add(
                        new Command
                        {
                            Type = isClosed ? CommandType.RuleOn : CommandType.RuleOff,
                            Number = rule.Coil,
                            Switch = switchNumber,
                            Milliseconds = rule.PulseMs,
                            Hold = rule.Hold,
                            TimeMs = now,
                        }
                    );
                }
            }
        }

        /// <summary>
        /// Applies the queued commands in the order and at the time they arrived, ends pulses in
        /// between, and reports every output whose value changed since the last poll.
        /// </summary>
        /// <remarks>
        /// Coil transitions are reported in order, so a coil enabled and disabled within one poll
        /// reports both. Lights report their brightness at the time of the poll.
        /// </remarks>
        /// <param name="sink">Receives the changes.</param>
        public void Poll(IMpfOutputSink sink)
        {
            double nowMs;
            lock (_queueLock)
            {
                (_incoming, _processing) = (_processing, _incoming);
                // read under the lock, so no queued command is stamped later than the poll
                nowMs = _clockMs();
            }

            foreach (var command in _processing)
            {
                var timeMs = System.Math.Min(command.TimeMs, nowMs);
                EndPulses(timeMs, sink);
                if (string.IsNullOrEmpty(command.Number))
                    continue;
                switch (command.Type)
                {
                    case CommandType.Fade:
                        ApplyFade(command, timeMs);
                        break;
                    case CommandType.Pulse:
                    {
                        // a pulse on an enabled coil disables it afterwards
                        var coil = GetCoil(command.Number);
                        coil.Explicit = Drive.Pulse;
                        coil.ExplicitEndMs = timeMs + command.Milliseconds;
                        UpdateCoil(coil, sink);
                        break;
                    }
                    case CommandType.Enable:
                    {
                        var coil = GetCoil(command.Number);
                        coil.Explicit = Drive.Held;
                        UpdateCoil(coil, sink);
                        break;
                    }
                    case CommandType.Disable:
                    {
                        var coil = GetCoil(command.Number);
                        coil.Explicit = Drive.None;
                        coil.Rules?.Clear();
                        UpdateCoil(coil, sink);
                        break;
                    }
                    case CommandType.RuleOn:
                    {
                        var coil = GetCoil(command.Number);
                        coil.Rules ??= new List<RuleDrive>();
                        var drive = new RuleDrive
                        {
                            Switch = command.Switch,
                            Hold = command.Hold,
                            EndMs = command.Hold
                                ? double.PositiveInfinity
                                : timeMs + command.Milliseconds,
                        };
                        var index = FindRule(coil, command.Switch);
                        if (index >= 0)
                            coil.Rules[index] = drive;
                        else
                            coil.Rules.Add(drive);
                        UpdateCoil(coil, sink);
                        break;
                    }
                    case CommandType.RuleOff:
                    {
                        // the switch opened or the rule was removed
                        var coil = GetCoil(command.Number);
                        var index = FindRule(coil, command.Switch);
                        if (index >= 0)
                        {
                            coil.Rules.RemoveAt(index);
                            UpdateCoil(coil, sink);
                        }
                        break;
                    }
                }
            }
            _processing.Clear();
            EndPulses(nowMs, sink);

            foreach (var light in _dirty)
            {
                light.Dirty = false;
                Report(light, nowMs, sink);
            }
            _dirty.Clear();

            for (var i = _fading.Count - 1; i >= 0; i--)
            {
                var light = _fading[i];
                Report(light, nowMs, sink);
                if (!light.Fading || nowMs >= light.FadeEndMs)
                {
                    light.Fading = false;
                    light.InFadeList = false;
                    RemoveAt(_fading, i);
                }
            }
        }

        /// <summary>
        /// Parses a coil number from the machine config: a plain non-negative decimal integer, or -1.
        /// </summary>
        public static int CoilNumber(string number) => ParseNumber(number, 0);

        /// <summary>
        /// Classifies a light channel number as MPF's VPE platform sends it
        /// (<c>&lt;subtype&gt;-&lt;number&gt;</c>).
        /// </summary>
        /// <param name="lightNumber">The channel number, such as <c>light-12</c> or <c>gi-1</c>.</param>
        /// <param name="kind">GI for subtype <c>gi</c>, lamp otherwise.</param>
        /// <returns>
        /// The config number for subtypes <c>light</c> and <c>gi</c> when it is a plain
        /// non-negative decimal integer, or -1.
        /// </returns>
        public static int LightNumber(string lightNumber, out MpfOutputKind kind)
        {
            kind = MpfOutputKind.Lamp;
            var dash = lightNumber?.IndexOf('-') ?? -1;
            if (dash <= 0)
                return -1;
            if (IsSubtype(lightNumber, dash, GiSubtype))
            {
                kind = MpfOutputKind.GeneralIllumination;
                return ParseNumber(lightNumber, dash + 1);
            }
            return IsSubtype(lightNumber, dash, LampSubtype)
                ? ParseNumber(lightNumber, dash + 1)
                : -1;
        }

        private void Enqueue(CommandType type, string number, float brightness, uint ms)
        {
            lock (_queueLock)
            {
                if (!_attached)
                    return;
                _incoming.Add(
                    new Command
                    {
                        Type = type,
                        Number = number,
                        Brightness = brightness,
                        Milliseconds = ms,
                        TimeMs = _clockMs(),
                    }
                );
            }
        }

        private void ApplyFade(Command command, double timeMs)
        {
            if (!_lights.TryGetValue(command.Number, out var light))
            {
                var number = LightNumber(command.Number, out var kind);
                light = new Output { Kind = kind, Number = number };
                _lights[command.Number] = light;
            }

            var target = System.Math.Clamp((double)command.Brightness, 0d, 1d);
            if (command.Milliseconds == 0)
            {
                light.Target = target;
                light.Fading = false; // leaves _fading on its next pass
            }
            else
            {
                light.Start = Evaluate(light, timeMs);
                light.Target = target;
                light.FadeStartMs = timeMs;
                light.FadeEndMs = timeMs + command.Milliseconds;
                light.Fading = true;
                if (!light.InFadeList)
                {
                    light.InFadeList = true;
                    _fading.Add(light);
                }
            }
            if (!light.Dirty)
            {
                light.Dirty = true;
                _dirty.Add(light);
            }
        }

        private Output GetCoil(string number)
        {
            if (!_coils.TryGetValue(number, out var coil))
            {
                coil = new Output { Kind = MpfOutputKind.Coil, Number = CoilNumber(number) };
                _coils[number] = coil;
            }
            return coil;
        }

        private static int FindRule(Output coil, string switchNumber)
        {
            if (coil.Rules == null)
                return -1;
            for (var i = 0; i < coil.Rules.Count; i++)
            {
                if (string.Equals(coil.Rules[i].Switch, switchNumber, StringComparison.Ordinal))
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Recomputes whether anything drives the coil and when its next pulse ends, and reports
        /// the change.
        /// </summary>
        private void UpdateCoil(Output coil, IMpfOutputSink sink)
        {
            var on = coil.Explicit != Drive.None;
            var endMs = coil.Explicit == Drive.Pulse ? coil.ExplicitEndMs : double.PositiveInfinity;
            if (coil.Rules != null)
            {
                foreach (var rule in coil.Rules)
                {
                    on = true;
                    if (!rule.Hold && rule.EndMs < endMs)
                        endMs = rule.EndMs;
                }
            }
            coil.PulseEndMs = endMs;
            var pulsing = !double.IsPositiveInfinity(endMs);
            if (pulsing && !coil.Pulsing)
            {
                coil.Pulsing = true;
                _pulsing.Add(coil);
            }
            else if (!pulsing && coil.Pulsing)
            {
                coil.Pulsing = false;
                var index = _pulsing.IndexOf(coil);
                if (index >= 0)
                    RemoveAt(_pulsing, index);
            }
            SetCoil(coil, on, sink);
        }

        /// <summary>
        /// Ends the pulses that are over by <paramref name="timeMs"/>, earliest first.
        /// </summary>
        private void EndPulses(double timeMs, IMpfOutputSink sink)
        {
            while (true)
            {
                var earliest = -1;
                for (var i = 0; i < _pulsing.Count; i++)
                {
                    var end = _pulsing[i].PulseEndMs;
                    if (end <= timeMs && (earliest < 0 || end < _pulsing[earliest].PulseEndMs))
                        earliest = i;
                }
                if (earliest < 0)
                    return;
                var coil = _pulsing[earliest];
                var endMs = coil.PulseEndMs;
                if (coil.Explicit == Drive.Pulse && coil.ExplicitEndMs <= endMs)
                    coil.Explicit = Drive.None;
                if (coil.Rules != null)
                {
                    for (var i = coil.Rules.Count - 1; i >= 0; i--)
                    {
                        if (!coil.Rules[i].Hold && coil.Rules[i].EndMs <= endMs)
                            coil.Rules.RemoveAt(i);
                    }
                }
                UpdateCoil(coil, sink);
            }
        }

        private static void SetCoil(Output coil, bool on, IMpfOutputSink sink)
        {
            if (coil.On == on)
                return;
            coil.On = on;
            sink.OutputChanged(MpfOutputKind.Coil, coil.Number, on ? 1d : 0d);
        }

        private static void Report(Output light, double nowMs, IMpfOutputSink sink)
        {
            var value = Evaluate(light, nowMs);
            if (value == light.Reported)
                return;
            light.Reported = value;
            sink.OutputChanged(light.Kind, light.Number, value);
        }

        private static double Evaluate(Output light, double nowMs)
        {
            if (!light.Fading || nowMs >= light.FadeEndMs)
                return light.Target;
            if (nowMs <= light.FadeStartMs)
                return light.Start;
            var t = (nowMs - light.FadeStartMs) / (light.FadeEndMs - light.FadeStartMs);
            return light.Start + (light.Target - light.Start) * t;
        }

        private static bool IsSubtype(string lightNumber, int dash, string subtype) =>
            dash == subtype.Length
            && string.Compare(
                lightNumber,
                0,
                subtype,
                0,
                dash,
                StringComparison.OrdinalIgnoreCase
            ) == 0;

        private static int ParseNumber(string text, int start)
        {
            if (text == null || start >= text.Length)
                return -1;
            long value = 0;
            for (var i = start; i < text.Length; i++)
            {
                var c = text[i];
                if (c < '0' || c > '9')
                    return -1;
                value = value * 10 + (c - '0');
                if (value > int.MaxValue)
                    return -1;
            }
            return (int)value;
        }

        private static void RemoveAt(List<Output> list, int index)
        {
            var last = list.Count - 1;
            list[index] = list[last];
            list.RemoveAt(last);
        }
    }
}
