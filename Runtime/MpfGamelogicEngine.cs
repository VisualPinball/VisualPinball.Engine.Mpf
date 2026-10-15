// Visual Pinball Engine
// Copyright (C) 2025 freezy and VPE Team
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mpf.Vpe;
using NLog;
using NUnit.Framework.Constraints;
using UnityEngine;
using VisualPinball.Engine.Game.Engines;
using VisualPinball.Engine.Mpf.Unity.MediaController;
using VisualPinball.Plugins;
using VisualPinball.Unity;
using Logger = NLog.Logger;

namespace VisualPinball.Engine.Mpf.Unity
{
    /// <summary>
    /// Allows the Mission Pinball Framework to drive VPE by sending switch changes to MPF
    /// and applying changes to coils, lights and hardware rules requested by MPF.
    /// </summary>
    [AddComponentMenu("Pinball/Gamelogic Engine/MPF Game Logic")]
    public class MpfGamelogicEngine
        : MonoBehaviour,
            IGamelogicEngine,
            IGamelogicInputThreading,
            IGamelogicNativeOutputs,
            IDiagnosticsSource,
            IGamelogicStartupStatus
    {
        /// <inheritdoc />
        public string StartupFailure => _mpfWrangler?.StartupError;

        [SerializeField]
        private MpfWranglerOptions _wranglerOptions;

        [SerializeField]
        private SerializedGamelogicEngineSwitch[] _requestedSwitches =
            Array.Empty<SerializedGamelogicEngineSwitch>();

        [SerializeField]
        private SerializedGamelogicEngineLamp[] _requestedLamps =
            Array.Empty<SerializedGamelogicEngineLamp>();

        [SerializeField]
        private SerializedGamelogicEngineCoil[] _requestedCoils =
            Array.Empty<SerializedGamelogicEngineCoil>();

        [SerializeField]
        private DisplayConfig[] _mpfDotMatrixDisplays;

        // MPF uses names and numbers/ids (for hardware mapping) to identify switches, coils, and
        // lamps. VPE only uses names, which is why the arrays above do not store the numbers.
        // These dictionaries store the numbers to make communication with MPF possible.
        [SerializeField]
        private MpfNameNumberDictionary _mpfSwitchNumbers = new();

        [SerializeField]
        private MpfNameNumberDictionary _mpfCoilNumbers = new();

        [SerializeField]
        private MpfNameNumberDictionary _mpfLampNumbers = new();

        private MpfWrangler _mpfWrangler;
        private MpfWrangler MpfWrangler
        {
            get
            {
                if (_mpfWrangler == null)
                    MpfWrangler = new MpfWrangler(_wranglerOptions);
                return _mpfWrangler;
            }
            set
            {
                if (value != _mpfWrangler)
                {
                    if (_mpfWrangler != null)
                    {
                        _mpfWrangler.MpfStateChanged -= OnMpfStateChanged;
                        if (_mpfWrangler.BcpInterface != null)
                            _mpfWrangler.BcpInterface.ConnectionStateChanged -= OnBcpStateChanged;
                        _mpfWrangler.Dispose();
                    }
                    var prevMpfState = MpfState;
                    var prevBcpState = BcpState;
                    _mpfWrangler = value;
                    if (_mpfWrangler != null)
                    {
                        _mpfWrangler.MpfStateChanged += OnMpfStateChanged;
                        if (_mpfWrangler.BcpInterface != null)
                            _mpfWrangler.BcpInterface.ConnectionStateChanged += OnBcpStateChanged;
                    }
                    if (prevMpfState != MpfState)
                        MpfStateChanged?.Invoke(
                            this,
                            new StateChangedEventArgs<MpfState>(MpfState, prevMpfState)
                        );
                    if (prevBcpState != BcpState)
                        BcpStateChanged?.Invoke(
                            this,
                            new StateChangedEventArgs<BcpConnectionState>(BcpState, prevBcpState)
                        );
                }
            }
        }
        private Player _player;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        // per engine, so one table's mapping problems don't hide the next one's
        private readonly LogOncePerKey<string> _unmappedSwitchLog = new();
        private readonly LogOncePerKey<string> _unknownLampNumberLog = new();
        private readonly LogOncePerKey<string> _unknownCoilNumberLog = new();
        private readonly LogOncePerKey<string> _unknownHardwareRuleLog = new();
        private readonly LogOncePerKey<string> _segmentDisplayLog = new();
        private static readonly LogThrottle SwitchNotSentThrottle = new(3, TimeSpan.FromSeconds(30));
        private static readonly LogThrottle SwitchSendFailedThrottle = new(3, TimeSpan.FromSeconds(30));

        // for diagnostics: whether the machine description stored in VPE matches MPF's
        private string _machineDescriptionStatus = "not checked";

        private readonly MpfOutputModel _nativeOutputModel = new(MonotonicMs);
        private NativeOutputSink _nativeOutputSink;
        private INativeOutputListener _nativeOutputs;

        public string Name => "Mission Pinball Framework";
        public GamelogicInputDispatchMode SwitchDispatchMode => GamelogicInputDispatchMode.MainThread;
        public GamelogicEngineSwitch[] RequestedSwitches => _requestedSwitches;
        public GamelogicEngineLamp[] RequestedLamps => _requestedLamps;
        public GamelogicEngineCoil[] RequestedCoils => _requestedCoils;
        public GamelogicEngineWire[] AvailableWires => Array.Empty<GamelogicEngineWire>();

        public MpfState MpfState
        {
            get
            {
                if (_mpfWrangler != null)
                    return _mpfWrangler.MpfState;
                else
                    return MpfState.NotConnected;
            }
        }

        public event EventHandler<StateChangedEventArgs<MpfState>> MpfStateChanged;

        public BcpConnectionState BcpState
        {
            get
            {
                if (_mpfWrangler != null && _mpfWrangler.BcpInterface != null)
                    return _mpfWrangler.BcpInterface.ConnectionState;
                else
                    return BcpConnectionState.NotConnected;
            }
        }

        public event EventHandler<StateChangedEventArgs<BcpConnectionState>> BcpStateChanged;

        public MpfMediaController MediaControllerSetting => _wranglerOptions.MediaController;

#pragma warning disable CS0067
        public event EventHandler<RequestedDisplays> OnDisplaysRequested;
        public event EventHandler<string> OnDisplayClear;
        public event EventHandler<DisplayFrameData> OnDisplayUpdateFrame;
        public event EventHandler<LampEventArgs> OnLampChanged;
        public event EventHandler<LampsEventArgs> OnLampsChanged;
        public event EventHandler<CoilEventArgs> OnCoilChanged;
        public event EventHandler<EventArgs> OnStarted;
        public event EventHandler<SwitchEventArgs2> OnSwitchChanged;
#pragma warning restore CS0067

#if UNITY_EDITOR
        private static SemaphoreSlim _editorGetMachineDescriptionSemaphore;

        public async Task QueryParseAndStoreMpfMachineDescription(CancellationToken ct)
        {
            if (Application.isPlaying)
                throw new Exception("This method should only be called in edit mode.");

            _editorGetMachineDescriptionSemaphore ??= new SemaphoreSlim(1, 1);
            // Wait until prior calls to this method have finished
            await _editorGetMachineDescriptionSemaphore.WaitAsync(ct);
            var defaultWranglerOptions = _wranglerOptions;
            try
            {
                // Use a new wrangler that has the right options for getting the machine
                // description. The _mpfWrangler field is temporarily overwritten to make the
                // MPF state of this wrangler show up in the inspector.
                _wranglerOptions = MpfWranglerOptions.Create(
                    mediaController: MpfMediaController.None,
                    outputType: MpfOutputType.LogInUnityConsole,
                    machineFolder: _wranglerOptions.MachineFolder,
                    cacheConfigFiles: false,
                    forceReloadConfig: true
                );
                MpfWrangler = new MpfWrangler(_wranglerOptions);
                // Not runtime, so there is no machine state.
                // Also doesn't matter for getting machine desc.
                var initialState = new MachineState();

                await MpfWrangler.StartMpf(initialState, ct);
                try
                {
                    var machineDescription = await MpfWrangler.GetMachineDescription(
                        timeout: 3f,
                        ct
                    );

                    ct.ThrowIfCancellationRequested();

                    _requestedSwitches = machineDescription.GetSwitches().ToArray();
                    _requestedCoils = machineDescription.GetCoils().ToArray();
                    _requestedLamps = machineDescription.GetLights().ToArray();
                    _mpfSwitchNumbers.Init(machineDescription.GetSwitchNumbersByNameDict());
                    _mpfCoilNumbers.Init(machineDescription.GetCoilNumbersByNameDict());
                    _mpfLampNumbers.Init(machineDescription.GetLampNumbersByNameDict());
                    _mpfDotMatrixDisplays = machineDescription.GetDmds().ToArray();
                }
                finally
                {
                    await MpfWrangler.StopMpf();
                }
            }
            finally
            {
                MpfWrangler.Dispose();
                MpfWrangler = null;
                _wranglerOptions = defaultWranglerOptions;
                _editorGetMachineDescriptionSemaphore.Release();
                if (_editorGetMachineDescriptionSemaphore.CurrentCount == 1)
                {
                    _editorGetMachineDescriptionSemaphore.Dispose();
                    _editorGetMachineDescriptionSemaphore = null;
                }
            }
        }
#endif

        public async Task OnInit(
            Player player,
            TableApi tableApi,
            BallManager ballManager,
            CancellationToken ct
        )
        {
            ct.ThrowIfCancellationRequested();

            _player = player;

            MpfWrangler.MpfFadeLightRequestReceived += ExecuteMpfFadeLightRequest;
            MpfWrangler.MpfPulseCoilRequestReceived += ExecuteMpfPulseCoilRequest;
            MpfWrangler.MpfEnableCoilRequestReceived += ExecuteMpfEnableCoilRequest;
            MpfWrangler.MpfDisableCoilRequestReceived += ExecuteMpfCommandDisableCoilRequest;
            MpfWrangler.MpfConfigureHardwareRuleRequestReceived +=
                ExecuteMpfConfigureHardwareRuleRequest;
            MpfWrangler.MpfRemoveHardwareRuleRequestReceived += ExecuteMpfRemoveHardwareRuleRequest;
            MpfWrangler.MpfSetDmdFrameRequestReceived += ExecuteMpfSetDmdFrameRequest;
            MpfWrangler.MpfSetSegmentDisplayFrameRequestReceived +=
                ExecuteMpfSetSegmentDisplayFrameRequest;

            MachineState initialState = CompileMachineState(player);
            try
            {
                await MpfWrangler.StartMpf(initialState, ct);
            }
            catch (Exception e) when (MpfWrangler.IsEnvironmentFailure(e))
            {
                // MPF is missing, didn't respond or exited. The wrangler logged what failed and
                // how to find out why. That's for the user to fix, not a VPE error, so the table
                // runs without gamelogic.
                return;
            }

            OnDisplaysRequested?.Invoke(this, new RequestedDisplays(_mpfDotMatrixDisplays));
            OnStarted?.Invoke(this, EventArgs.Empty);

            MachineDescription md;
            try
            {
                md = await MpfWrangler.GetMachineDescription(timeout: 3f, ct);
            }
            catch (Exception e) when (MpfWrangler.IsEnvironmentFailure(e))
            {
                _machineDescriptionStatus = "could not be read from MPF";
                Logger.Warn(e, "Could not get MPF's machine description to compare it with VPE's.");
                return;
            }

            var differences = GetMachineDescriptionDifferences(md);
            if (differences.Count > 0)
            {
                _machineDescriptionStatus = $"differs ({differences.Count} difference(s))";
                Logger.Warn(
                    "MPF's machine description differs from the one stored in VPE: {Differences:l}. "
                        + "Click 'Get Machine Description' in the inspector of the MPF gamelogic "
                        + "engine to update it.",
                    string.Join("; ", differences)
                );
            }
            else
            {
                _machineDescriptionStatus = "matches";
                Logger.Info("MPF's machine description matches VPE's machine description.");
            }
        }

        private async void OnDestroy()
        {
            try
            {
                await MpfWrangler.StopMpf();
            }
            catch (Exception e) when (e is OperationCanceledException || MpfWrangler.IsEnvironmentFailure(e))
            {
                // MPF was gone already, or didn't answer the quit request
                Logger.Debug(e, "MPF didn't shut down cleanly.");
            }
            catch (Exception e)
            {
                Logger.Error(e, "Stopping MPF failed.");
            }
            MpfWrangler.MpfFadeLightRequestReceived -= ExecuteMpfFadeLightRequest;
            MpfWrangler.MpfPulseCoilRequestReceived -= ExecuteMpfPulseCoilRequest;
            MpfWrangler.MpfEnableCoilRequestReceived -= ExecuteMpfEnableCoilRequest;
            MpfWrangler.MpfDisableCoilRequestReceived -= ExecuteMpfCommandDisableCoilRequest;
            MpfWrangler.MpfConfigureHardwareRuleRequestReceived -=
                ExecuteMpfConfigureHardwareRuleRequest;
            MpfWrangler.MpfRemoveHardwareRuleRequestReceived -= ExecuteMpfRemoveHardwareRuleRequest;
            MpfWrangler.MpfSetDmdFrameRequestReceived -= ExecuteMpfSetDmdFrameRequest;
            MpfWrangler.MpfSetSegmentDisplayFrameRequestReceived -=
                ExecuteMpfSetSegmentDisplayFrameRequest;
            MpfWrangler.Dispose();
        }

        public static bool TryGetBcpInterface(
            Component requestingComponent,
            out BcpInterface bcpInterface
        )
        {
            var gle = requestingComponent.GetComponentInParent<MpfGamelogicEngine>();
            var componentType = requestingComponent.GetType().Name;
            var gameObjectName = requestingComponent.gameObject.name;

            if (!Application.isPlaying)
            {
                Logger.Warn(
                    "{Component:l} on {GameObject} requests a BCP interface (used to talk to MPF), "
                        + "but the game is not running.",
                    componentType,
                    gameObjectName
                );
                bcpInterface = null;
                return false;
            }

            if (gle == null)
            {
                Logger.Warn(
                    "{Component:l} on {GameObject} requests a BCP interface (used to talk to MPF), "
                        + "but no MPF gamelogic engine was found. Make sure the component is part of "
                        + "the table hierarchy and that the table's root object has an "
                        + "'MpfGamelogicEngine' component, or remove the component.",
                    componentType,
                    gameObjectName
                );
                bcpInterface = null;
                return false;
            }

            if (gle._wranglerOptions.MediaController != MpfMediaController.Included)
            {
                Logger.Warn(
                    "{Component:l} on {GameObject} requests a BCP interface (used to talk to MPF), "
                        + "but the MPF gamelogic engine doesn't use the included media controller. "
                        + "Set 'Media Controller' to 'Included' in its inspector.",
                    componentType,
                    gameObjectName
                );
                bcpInterface = null;
                return false;
            }

            if (gle.MpfWrangler == null)
            {
                // the property creates the wrangler, so this can't happen
                Logger.Error(
                    "{Component:l} on {GameObject} requests a BCP interface, but the MPF gamelogic "
                        + "engine is not initialized.",
                    componentType,
                    gameObjectName
                );
                bcpInterface = null;
                return false;
            }

            bcpInterface = gle.MpfWrangler.BcpInterface;
            return true;
        }

        #region Diagnostics

        public string DiagnosticsSection => "MPF";

        public void Describe(DiagnosticsWriter writer)
        {
            var options = _wranglerOptions;
            var wrangler = _mpfWrangler;
            if (options == null)
            {
                writer.Add("options", "none");
                return;
            }

            writer.Add("mpf-version", wrangler?.MpfVersion ?? (
                options.StartupBehavior == MpfStartupBehavior.DelayConnection
                    ? "unknown (not queried with startup behavior DelayConnection)"
                    : "unknown (not connected)"
            ));
            writer.Add("state", MpfState.ToString());
            if (!string.IsNullOrEmpty(wrangler?.StartupError))
                writer.Add("startup-error", wrangler.StartupError);
            if (wrangler?.ExitCode is int exitCode)
                writer.Add("exit-code", exitCode);

            writer.Add("executable-source", options.ExecutableSource.ToString());
            if (!string.IsNullOrEmpty(wrangler?.ExecutablePath))
            {
                writer.Add("executable", wrangler.ExecutablePath);
                if (options.ExecutableSource == MpfExecutableSource.Included)
                    writer.Add("executable-found", System.IO.File.Exists(wrangler.ExecutablePath));
                writer.Add("arguments", wrangler.Arguments ?? "");
            }

            var machineFolder = options.MachineFolder;
            writer.Add("machine-folder", machineFolder ?? "");
            if (!string.IsNullOrEmpty(machineFolder))
                writer.Add("machine-folder-found", System.IO.Directory.Exists(machineFolder));
            writer.Add("machine-description", _machineDescriptionStatus);

            writer.Add("media-controller", options.MediaController.ToString());
            if (options.MediaController == MpfMediaController.Included)
            {
                writer.Add("bcp-state", BcpState.ToString());
                writer.AddSetting("bcp-port", options.BcpInterfaceOptions?.Port);
            }

            var effectiveOutput = wrangler?.EffectiveOutputType;
            writer.Add(
                "output",
                effectiveOutput == null || effectiveOutput == options.OutputType
                    ? options.OutputType.ToString()
                    : $"{effectiveOutput} (configured {options.OutputType}, release builds hide MPF)"
            );
            if (wrangler != null && wrangler.OutputCaptured)
                writer.Add("mpf-output", "captured in the log (logger Mpf.Process)");
            var logFolder = wrangler?.MpfLogFolder
                ?? (string.IsNullOrEmpty(machineFolder) ? null : System.IO.Path.Combine(machineFolder, "logs"));
            if (logFolder != null)
            {
                writer.Add("mpf-log-folder", logFolder);
                writer.Add("mpf-log-folder-found", System.IO.Directory.Exists(logFolder));
            }

            writer.AddSetting("startup-behavior", options.StartupBehavior);
            writer.AddSetting("connect-timeout", options.ConnectTimeout);
            writer.AddSetting("connect-delay", options.ConnectDelay);
            writer.AddSetting("verbose-logging", options.VerboseLogging);
            writer.AddSetting("cache-config-files", options.CacheConfigFiles);
            writer.AddSetting("force-reload-config", options.ForceReloadConfig);
            writer.AddSetting("force-load-all-assets", options.ForceLoadAllAssetsOnStart);
            writer.Add("switches", _requestedSwitches?.Length ?? 0);
            writer.Add("coils", _requestedCoils?.Length ?? 0);
            writer.Add("lamps", _requestedLamps?.Length ?? 0);
        }

        #endregion

        public void DisplayChanged(DisplayFrameData displayFrameData) { }

        public bool GetCoil(string id)
        {
            return _player.CoilStatuses.ContainsKey(id) && _player.CoilStatuses[id];
        }

        public LampState GetLamp(string id)
        {
            return _player.LampStatuses.ContainsKey(id)
                ? _player.LampStatuses[id]
                : LampState.Default;
        }

        public bool GetSwitch(string id)
        {
            return _player.SwitchStatuses.ContainsKey(id)
                && _player.SwitchStatuses[id].IsSwitchEnabled;
        }

        public void SetCoil(string id, bool isEnabled)
        {
            OnCoilChanged?.Invoke(this, new CoilEventArgs(id, isEnabled));
        }

        public void SetLamp(string id, float value, bool isCoil, LampSource source)
        {
            OnLampChanged?.Invoke(this, new LampEventArgs(id, value, isCoil, source));
        }

        public async void Switch(string id, bool isClosed)
        {
            // async void: nothing observes this method's task, so whatever goes wrong is logged here
            try
            {
                OnSwitchChanged?.Invoke(this, new SwitchEventArgs2(id, isClosed));

                // VPE runs MPF's hardware rules as wires, so their coils only show up here.
                if (_mpfSwitchNumbers.TryGetNumberByName(id, out var ruleSwitchNumber))
                    _nativeOutputModel.EnqueueSwitch(ruleSwitchNumber, isClosed);

                if (_mpfSwitchNumbers.ContainsName(id))
                {
                    var state = MpfWrangler.MpfState;
                    if (state == MpfState.Connected)
                    {
                        var number = _mpfSwitchNumbers.GetNumberByName(id);
                        var change = new SwitchChanges
                        {
                            SwitchNumber = number,
                            SwitchState = isClosed,
                        };
                        await MpfWrangler.SendSwitchChange(change);
                    }
                    else if (SwitchNotSentThrottle.ShouldLog(out var suppressed))
                    {
                        if (suppressed == 0)
                            Logger.Warn(
                                "Switch {SwitchId} was not sent to MPF, because MPF is {MpfState}.",
                                id,
                                state
                            );
                        else
                            Logger.Warn(
                                "Switch {SwitchId} was not sent to MPF, because MPF is {MpfState} "
                                    + "({Suppressed} similar suppressed).",
                                id,
                                state,
                                suppressed
                            );
                    }
                }
                else if (_unmappedSwitchLog.ShouldLog(id))
                {
                    Logger.Warn(
                        "Switch {SwitchId} has no MPF number, so its changes are not sent to MPF. Click "
                            + "'Get Machine Description' in the inspector of the MPF gamelogic engine "
                            + "to update it. Further changes of this switch are not logged.",
                        id
                    );
                }
            }
            catch (OperationCanceledException)
            {
                // MPF is stopping
            }
            catch (Exception e) when (MpfWrangler.IsEnvironmentFailure(e))
            {
                if (SwitchSendFailedThrottle.ShouldLog(out var suppressed))
                    Logger.Warn(
                        e,
                        "Could not send switch {SwitchId} to MPF ({Suppressed} similar suppressed before).",
                        id,
                        suppressed
                    );
            }
            catch (Exception e)
            {
                if (SwitchSendFailedThrottle.ShouldLog(out var suppressed))
                    Logger.Error(
                        e,
                        "Sending switch {SwitchId} to MPF failed ({Suppressed} similar suppressed before).",
                        id,
                        suppressed
                    );
            }
        }

        private void Update()
        {
            DispatchNativeOutputs();
        }

        /// <summary>
        /// Reports every light and coil MPF drives by its machine config number, including
        /// outputs the table does not map. See <see cref="MpfOutputModel"/> for the numbering.
        /// </summary>
        /// <remarks>
        /// Called on the main thread, like <see cref="Update"/>. The model only queues MPF
        /// commands while attached and checks that under its queue lock, so a command that races
        /// with the detach cannot reach the next listener.
        /// </remarks>
        public void SetNativeOutputListener(INativeOutputListener listener)
        {
            if (listener != null)
            {
                _nativeOutputModel.Attach();
                _nativeOutputs = listener;
            }
            else
            {
                _nativeOutputs = null;
                // Release the last listener too, so a detached plugin is not kept alive by the engine.
                if (_nativeOutputSink != null)
                    _nativeOutputSink.Listener = null;
                _nativeOutputModel.Detach();
            }
        }

        private void DispatchNativeOutputs()
        {
            var listener = _nativeOutputs;
            if (listener == null)
                return;
            _nativeOutputSink ??= new NativeOutputSink();
            _nativeOutputSink.Listener = listener;
            _nativeOutputModel.Poll(_nativeOutputSink);
            listener.NativeOutputPollEnd();
        }

        private static double MonotonicMs() =>
            System.Diagnostics.Stopwatch.GetTimestamp()
            * 1000d
            / System.Diagnostics.Stopwatch.Frequency;

        /// <summary>
        /// MPF lights are dimmable channels (brightness 0..1, faded by <see cref="MpfOutputModel"/>);
        /// MPF coils are switched on and off by the VPE platform.
        /// </summary>
        private sealed class NativeOutputSink : IMpfOutputSink
        {
            public INativeOutputListener Listener;

            public void OutputChanged(MpfOutputKind kind, int number, double value)
            {
                switch (kind)
                {
                    case MpfOutputKind.Coil:
                        Listener.NativeOutputChanged(
                            ControllerEventKind.Coil,
                            number,
                            value,
                            ControllerValueSemantics.Binary,
                            1d
                        );
                        break;
                    case MpfOutputKind.GeneralIllumination:
                        Listener.NativeOutputChanged(
                            ControllerEventKind.GeneralIllumination,
                            number,
                            value,
                            ControllerValueSemantics.Physical,
                            1d
                        );
                        break;
                    default:
                        Listener.NativeOutputChanged(
                            ControllerEventKind.Lamp,
                            number,
                            value,
                            ControllerValueSemantics.Physical,
                            1d
                        );
                        break;
                }
            }
        }

        private void OnMpfStateChanged(object sender, StateChangedEventArgs<MpfState> args)
        {
            MpfStateChanged?.Invoke(this, args);
        }

        private void OnBcpStateChanged(
            object sender,
            StateChangedEventArgs<BcpConnectionState> args
        )
        {
            BcpStateChanged?.Invoke(this, args);
        }

        /// <summary>
        /// Lists how the machine description stored in VPE differs from MPF's, e.g.
        /// "coils not in MPF: c_flipper". Empty if they match.
        /// </summary>
        private List<string> GetMachineDescriptionDifferences(MachineDescription md)
        {
            var differences = new List<string>();
            AddDifference(
                differences,
                "switches not in MPF or with a different type",
                _requestedSwitches
                    .Where(gleSw => !md.Switches.Any(mpfSw => MpfExtensions.Equals(gleSw, mpfSw)))
                    .Select(sw => sw.Id)
            );
            AddDifference(
                differences,
                "coils not in MPF",
                _requestedCoils
                    .Where(gleCoil => !md.Coils.Any(mpfCoil => MpfExtensions.Equals(gleCoil, mpfCoil)))
                    .Select(coil => coil.Id)
            );
            AddDifference(
                differences,
                "lights not in MPF",
                _requestedLamps
                    .Where(gleLamp => !md.Lights.Any(mpfLight => MpfExtensions.Equals(gleLamp, mpfLight)))
                    .Select(lamp => lamp.Id)
            );
            AddDifference(
                differences,
                "DMDs not in MPF or with a different size",
                (_mpfDotMatrixDisplays ?? Array.Empty<DisplayConfig>())
                    .Where(cfg => !md.Dmds.Any(mpfDmd => MpfExtensions.Equals(cfg, mpfDmd)))
                    .Select(cfg => cfg.Id)
            );
            AddDifference(
                differences,
                "switch numbers",
                _mpfSwitchNumbers.GetDifferences(md.GetSwitchNumbersByNameDict())
            );
            AddDifference(
                differences,
                "coil numbers",
                _mpfCoilNumbers.GetDifferences(md.GetCoilNumbersByNameDict())
            );
            AddDifference(
                differences,
                "light numbers",
                _mpfLampNumbers.GetDifferences(md.GetLampNumbersByNameDict())
            );
            return differences;
        }

        private static void AddDifference(
            List<string> differences,
            string what,
            IEnumerable<string> names
        )
        {
            const int maxNames = 10;
            var list = names.ToList();
            if (list.Count == 0)
                return;
            var text = string.Join(", ", list.Take(maxNames));
            if (list.Count > maxNames)
                text += $" and {list.Count - maxNames} more";
            differences.Add($"{what}: {text}");
        }

        private MachineState CompileMachineState(Player player)
        {
            var initialState = new MachineState();
            foreach (var switchName in player.SwitchStatuses.Keys)
            {
                if (_mpfSwitchNumbers.TryGetNumberByName(switchName, out var number))
                {
                    var isClosed = player.SwitchStatuses[switchName].IsSwitchClosed;
                    initialState.InitialSwitchStates.Add(number, isClosed);
                }
            }
            return initialState;
        }

        private void ExecuteMpfFadeLightRequest(object sender, FadeLightRequest request)
        {
            foreach (var fade in request.Fades)
                _nativeOutputModel.EnqueueFade(
                    fade.LightNumber,
                    fade.TargetBrightness,
                    request.CommonFadeMs
                );

            var args = new List<LampEventArgs>();
            foreach (var fade in request.Fades)
            {
                if (_mpfLampNumbers.TryGetNameByNumber(fade.LightNumber, out var lampName))
                    args.Add(new LampEventArgs(lampName, fade.TargetBrightness));
                else if (_unknownLampNumberLog.ShouldLog(fade.LightNumber))
                    Logger.Warn(
                        "MPF drives light number {LightNumber}, which VPE has no lamp for. Update the "
                            + "machine description in the MPF gamelogic engine inspector. Further "
                            + "changes of this light are not logged.",
                        fade.LightNumber
                    );
            }
            OnLampsChanged?.Invoke(this, new LampsEventArgs(args.ToArray()));
        }

        private void ExecuteMpfPulseCoilRequest(object sender, PulseCoilRequest request)
        {
            _nativeOutputModel.EnqueuePulse(request.CoilNumber, request.PulseMs);

            if (_mpfCoilNumbers.TryGetNameByNumber(request.CoilNumber, out var coilName))
            {
                SetCoil(coilName, true);
                _player.ScheduleAction(request.PulseMs, () => SetCoil(coilName, false));
            }
            else
                LogUnknownCoilNumber(request.CoilNumber);
        }

        private void ExecuteMpfEnableCoilRequest(object sender, EnableCoilRequest request)
        {
            _nativeOutputModel.EnqueueEnable(request.CoilNumber);

            if (_mpfCoilNumbers.TryGetNameByNumber(request.CoilNumber, out var coilName))
                SetCoil(coilName, true);
            else
                LogUnknownCoilNumber(request.CoilNumber);
        }

        private void ExecuteMpfCommandDisableCoilRequest(object sender, DisableCoilRequest request)
        {
            _nativeOutputModel.EnqueueDisable(request.CoilNumber);

            if (_mpfCoilNumbers.TryGetNameByNumber(request.CoilNumber, out var coilName))
                SetCoil(coilName, false);
            else
                LogUnknownCoilNumber(request.CoilNumber);
        }

        private void ExecuteMpfConfigureHardwareRuleRequest(
            object sender,
            ConfigureHardwareRuleRequest request
        )
        {
            var switchNumber = request.SwitchNumber;
            var coilNumber = request.CoilNumber;
            _nativeOutputModel.ConfigureRule(
                switchNumber,
                coilNumber,
                request.PulseMs,
                request.HoldPower
            );
            if (
                _mpfSwitchNumbers.TryGetNameByNumber(switchNumber, out var switchName)
                && _mpfCoilNumbers.TryGetNameByNumber(coilNumber, out var coilName)
            )
                _player.AddHardwareRule(switchName, coilName);
            else
                LogUnknownHardwareRule(switchNumber, coilNumber);
        }

        private void ExecuteMpfRemoveHardwareRuleRequest(
            object sender,
            RemoveHardwareRuleRequest request
        )
        {
            var switchNumber = request.SwitchNumber;
            var coilNumber = request.CoilNumber;
            _nativeOutputModel.RemoveRule(switchNumber, coilNumber);
            if (
                _mpfSwitchNumbers.TryGetNameByNumber(switchNumber, out var switchName)
                && _mpfCoilNumbers.TryGetNameByNumber(coilNumber, out var coilName)
            )
                _player.RemoveHardwareRule(switchName, coilName);
            else
                LogUnknownHardwareRule(switchNumber, coilNumber);
        }

        private void LogUnknownCoilNumber(string coilNumber)
        {
            if (_unknownCoilNumberLog.ShouldLog(coilNumber))
                Logger.Warn(
                    "MPF drives coil number {CoilNumber}, which VPE has no coil for. Update the "
                        + "machine description in the MPF gamelogic engine inspector. Further "
                        + "changes of this coil are not logged.",
                    coilNumber
                );
        }

        private void LogUnknownHardwareRule(string switchNumber, string coilNumber)
        {
            if (!_unknownHardwareRuleLog.ShouldLog(switchNumber + "/" + coilNumber))
                return;
            Logger.Warn(
                "MPF configured a hardware rule for switch number {SwitchNumber} and coil number "
                    + "{CoilNumber}, but VPE has no {Missing:l} for it, so the rule is ignored. Update "
                    + "the machine description in the MPF gamelogic engine inspector.",
                switchNumber,
                coilNumber,
                _mpfSwitchNumbers.ContainsNumber(switchNumber) ? "coil" : "switch"
            );
        }

        private void ExecuteMpfSetDmdFrameRequest(object sender, SetDmdFrameRequest request)
        {
            var frameData = new DisplayFrameData(
                request.Name,
                DisplayFrameFormat.Dmd24,
                request.Frame.ToByteArray()
            );
            OnDisplayUpdateFrame?.Invoke(this, frameData);
        }

        private void ExecuteMpfSetSegmentDisplayFrameRequest(
            object sender,
            SetSegmentDisplayFrameRequest request
        )
        {
            if (_segmentDisplayLog.ShouldLog(request.Name ?? ""))
                Logger.Warn(
                    "MPF sent a frame for segment display {Display}, but VPE's MPF integration doesn't "
                        + "support segment displays yet. Further frames are ignored silently.",
                    request.Name
                );
        }
    }
}
