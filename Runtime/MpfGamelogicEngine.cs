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
using VisualPinball.Unity;
using Logger = NLog.Logger;

namespace VisualPinball.Engine.Mpf.Unity
{
    /// <summary>
    /// Allows the Mission Pinball Framework to drive VPE by sending switch changes to MPF
    /// and applying changes to coils, lights and hardware rules requested by MPF.
    /// </summary>
    public class MpfGamelogicEngine : MonoBehaviour, IGamelogicEngine
    {
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

        public string Name => "Mission Pinball Framework";
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
            await MpfWrangler.StartMpf(initialState, ct);

            OnDisplaysRequested?.Invoke(this, new RequestedDisplays(_mpfDotMatrixDisplays));
            OnStarted?.Invoke(this, EventArgs.Empty);

            var md = await MpfWrangler.GetMachineDescription(timeout: 3f, ct);
            if (!DoesMachineDescriptionMatch(md))
                Logger.Warn("Mismatch between MPF's and VPE's machine description detected.");
            else
                Logger.Info("MPF's machine description matches VPE's machine description.");
        }

        private async void OnDestroy()
        {
            await MpfWrangler.StopMpf();
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

            var errorMessage =
                $"Component '{requestingComponent.GetType()}' on game object "
                + $"'{requestingComponent.gameObject.name}' is requesting a BCP interface, but "
                + "{0} The BCP interface is used to communicate with the Mission Pinball Framework";

            if (!Application.isPlaying)
            {
                Logger.Error(string.Format(errorMessage, "the game is not running."));
                bcpInterface = null;
                return false;
            }

            if (gle == null)
            {
                Logger.Error(
                    string.Format(
                        errorMessage,
                        "no MPF game logic engine was found. Make sure the requesting component is "
                            + "attached to an object that is part of the table hierarchy and "
                            + "attach an 'MpfGamelogicEngine' component to the root object of the "
                            + "table or remove the component that requested the BCP interface."
                    )
                );
                bcpInterface = null;
                return false;
            }

            if (gle._wranglerOptions.MediaController != MpfMediaController.Included)
            {
                Logger.Error(
                    string.Format(
                        errorMessage,
                        "the game logic engine is not configured to use the integrated MPF media "
                            + "controller. Set 'Media Controller' to 'Included' in the MPF game "
                            + "logic engine inspector."
                    )
                );
                bcpInterface = null;
                return false;
            }

            if (gle.MpfWrangler == null)
            {
                Logger.Error(
                    string.Format(errorMessage, "the game logic engine is not initialized.")
                );
                bcpInterface = null;
                return false;
            }

            bcpInterface = gle.MpfWrangler.BcpInterface;
            return true;
        }

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
            OnSwitchChanged?.Invoke(this, new SwitchEventArgs2(id, isClosed));

            if (_mpfSwitchNumbers.ContainsName(id))
            {
                if (MpfWrangler.MpfState == MpfState.Connected)
                {
                    var number = _mpfSwitchNumbers.GetNumberByName(id);
                    var change = new SwitchChanges
                    {
                        SwitchNumber = number,
                        SwitchState = isClosed,
                    };
                    await MpfWrangler.SendSwitchChange(change);
                }
                else
                {
                    Logger.Warn(
                        $"Switch change '{id}' will not be sent to MPF because MPF is not ready"
                    );
                }
            }
            else
            {
                Logger.Error(
                    $"Switch '{id}' is defined in the MPF game logic engine but not"
                        + $" associated with an MPF number. State change cannot be forwarded to MPF."
                );
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

        private bool DoesMachineDescriptionMatch(MachineDescription md)
        {
            return _requestedSwitches.All(
                    (gleSw) => md.Switches.Any((mpfSw) => MpfExtensions.Equals(gleSw, mpfSw))
                )
                && _requestedCoils.All(
                    (gleCoil) => md.Coils.Any((mpfCoil) => MpfExtensions.Equals(gleCoil, mpfCoil))
                )
                && _requestedLamps.All(
                    (gleLamp) =>
                        md.Lights.Any((mpfLight) => MpfExtensions.Equals(gleLamp, mpfLight))
                )
                && _mpfDotMatrixDisplays.All(
                    (displayCfg) =>
                        md.Dmds.Any((mpfDmd) => MpfExtensions.Equals(displayCfg, mpfDmd))
                )
                && _mpfSwitchNumbers.Equals(md.GetSwitchNumbersByNameDict())
                && _mpfCoilNumbers.Equals(md.GetCoilNumbersByNameDict())
                && _mpfLampNumbers.Equals(md.GetLampNumbersByNameDict());
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
            var args = new List<LampEventArgs>();
            foreach (var fade in request.Fades)
            {
                if (_mpfLampNumbers.TryGetNameByNumber(fade.LightNumber, out var lampName))
                    args.Add(new LampEventArgs(lampName, fade.TargetBrightness));
                else
                    Logger.Error(
                        $"MPF sent a lamp number '{fade.LightNumber}' that is"
                            + $" not associated with a lamp id."
                    );
            }
            OnLampsChanged?.Invoke(this, new LampsEventArgs(args.ToArray()));
        }

        private void ExecuteMpfPulseCoilRequest(object sender, PulseCoilRequest request)
        {
            if (_mpfCoilNumbers.TryGetNameByNumber(request.CoilNumber, out var coilName))
            {
                SetCoil(coilName, true);
                _player.ScheduleAction(request.PulseMs, () => SetCoil(coilName, false));
            }
            else
                Logger.Error(
                    $"MPF sent a coil number '{request.CoilNumber}'"
                        + $" that is not associated with a coil id."
                );
        }

        private void ExecuteMpfEnableCoilRequest(object sender, EnableCoilRequest request)
        {
            if (_mpfCoilNumbers.TryGetNameByNumber(request.CoilNumber, out var coilName))
                SetCoil(coilName, true);
            else
                Logger.Error(
                    $"MPF sent a coil number '{request.CoilNumber}'"
                        + $" that is not associated with a coil id."
                );
        }

        private void ExecuteMpfCommandDisableCoilRequest(object sender, DisableCoilRequest request)
        {
            if (_mpfCoilNumbers.TryGetNameByNumber(request.CoilNumber, out var coilName))
                SetCoil(coilName, false);
            else
                Logger.Error(
                    $"MPF sent a coil number '{request.CoilNumber}'"
                        + $" that is not associated with a coil id."
                );
        }

        private void ExecuteMpfConfigureHardwareRuleRequest(
            object sender,
            ConfigureHardwareRuleRequest request
        )
        {
            var switchNumber = request.SwitchNumber;
            var coilNumber = request.CoilNumber;
            if (
                _mpfSwitchNumbers.TryGetNameByNumber(switchNumber, out var switchName)
                && _mpfCoilNumbers.TryGetNameByNumber(coilNumber, out var coilName)
            )
                _player.AddHardwareRule(switchName, coilName);
            else
                Logger.Error(
                    $"MPF wants to add a hardware rule for switch number "
                        + $"'{switchNumber} and coil number '{coilNumber}.' At least one "
                        + $"of them is not associated with an id."
                );
        }

        private void ExecuteMpfRemoveHardwareRuleRequest(
            object sender,
            RemoveHardwareRuleRequest request
        )
        {
            var switchNumber = request.SwitchNumber;
            var coilNumber = request.CoilNumber;
            if (
                _mpfSwitchNumbers.TryGetNameByNumber(switchNumber, out var switchName)
                && _mpfCoilNumbers.TryGetNameByNumber(coilNumber, out var coilName)
            )
                _player.RemoveHardwareRule(switchName, coilName);
            else
                Logger.Error(
                    $"MPF wants to remove a hardware rule for switch number "
                        + $"'{switchNumber} and coil number '{coilNumber}.' At least one "
                        + $"of them is not associated with an id."
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
            Logger.Error(
                "MPF sent a segment display frame, but segment displays are not yet supported by "
                    + "VPEs MPF integration."
            );
        }
    }
}
