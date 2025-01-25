// Visual Pinball Engine
// Copyright (C) 2021 freezy and VPE Team
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Net.Http;
using Grpc.Core;
using Grpc.Net.Client;
using Mpf.Vpe;
using NLog;
using UnityEngine;
using VisualPinball.Engine.Game.Engines;
using VisualPinball.Unity;
using Logger = NLog.Logger;

namespace VisualPinball.Engine.Mpf.Unity
{
    public class MpfGamelogicEngine : MonoBehaviour, IGamelogicEngine
    {
        [SerializeField]
        public SerializedGamelogicEngineSwitch[] _requestedSwitches =
            Array.Empty<SerializedGamelogicEngineSwitch>();

        [SerializeField]
        public SerializedGamelogicEngineLamp[] _requestedLamps =
            Array.Empty<SerializedGamelogicEngineLamp>();

        [SerializeField]
        public SerializedGamelogicEngineCoil[] _requestedCoils =
            Array.Empty<SerializedGamelogicEngineCoil>();

        [SerializeField]
        private MpfStarter _mpfStarter;

        [SerializeField]
        private float _connectTimeout = 20f;

        // MPF uses names and numbers/ids (for hardware mapping) to identify switches, coils, and
        // lamps. VPE only uses names, which is why the arrays above do not store the numbers.
        // These dictionaries store the numbers to make communication with MPF possible.
        [SerializeField]
        private MpfNameNumberDictionary _mpfSwitchNumbers = new();

        [SerializeField]
        private MpfNameNumberDictionary _mpfCoilNumbers = new();

        [SerializeField]
        private MpfNameNumberDictionary _mpfLampNumbers = new();

        [SerializeField]
        private DisplayConfig[] _mpfDotMatrixDisplays;

        private Player _player;
        private Process _mpfProcess;
        private GrpcChannel _grpcChannel;
        private AsyncServerStreamingCall<Commands> _mpfCommandStreamCall;
        private AsyncClientStreamingCall<SwitchChanges, EmptyResponse> _mpfSwitchStreamCall;
        private CancellationTokenSource _mpfCommunicationCts;
        private Task _receiveMpfCommandsTask;

        private const string _grpcAddress = "http://localhost:50051";

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public string Name => "Mission Pinball Framework";
        public GamelogicEngineSwitch[] RequestedSwitches => _requestedSwitches;
        public GamelogicEngineLamp[] RequestedLamps => _requestedLamps;
        public GamelogicEngineCoil[] RequestedCoils => _requestedCoils;
        public GamelogicEngineWire[] AvailableWires => Array.Empty<GamelogicEngineWire>();

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
        public void QueryParseAndStoreMpfMachineDescription()
        {
            // TODO: Ditch IMGUI for UiToolkit, then do this whole thing asynchronously
            var args = new MpfStarter()
            {
                _mediaController = MpfStarter.MediaController.None,
                _outputType = MpfStarter.OutputType.LogInTerminal,
            };
            using var mpfProcess = args.StartMpf();
            Thread.Sleep(15000);
            using var handler = new YetAnotherHttpHandler() { Http2Only = true };
            var options = new GrpcChannelOptions() { HttpHandler = handler };
            using var grpcChannel = GrpcChannel.ForAddress(_grpcAddress, options);
            var client = new MpfHardwareService.MpfHardwareServiceClient(grpcChannel);
            client.Start(new MachineState());
            var machineDescription = client.GetMachineDescription(
                new EmptyRequest(),
                deadline: DateTime.UtcNow.AddSeconds(3)
            );
            client.Quit(new QuitRequest(), deadline: DateTime.UtcNow.AddSeconds(3));

            _requestedSwitches = machineDescription.GetSwitches().ToArray();
            _requestedCoils = machineDescription.GetCoils().ToArray();
            _requestedLamps = machineDescription.GetLights().ToArray();
            _mpfSwitchNumbers.Init(machineDescription.GetSwitchNumbersByNameDict());
            _mpfCoilNumbers.Init(machineDescription.GetCoilNumbersByNameDict());
            _mpfLampNumbers.Init(machineDescription.GetLampNumbersByNameDict());
            _mpfDotMatrixDisplays = machineDescription.GetDmds().ToArray();
        }
#endif

        public async Task OnInit(
            Player player,
            TableApi tableApi,
            BallManager ballManager,
            CancellationToken ct
        )
        {
            _player = player;
            _mpfProcess = _mpfStarter.StartMpf();
            var handler = new YetAnotherHttpHandler() { Http2Only = true };
            var options = new GrpcChannelOptions()
            {
                HttpHandler = handler,
                DisposeHttpClient = true,
            };
            _grpcChannel = GrpcChannel.ForAddress(_grpcAddress, options);
            _mpfCommunicationCts = new CancellationTokenSource();
            using var waitCts = CancellationTokenSource.CreateLinkedTokenSource(
                ct,
                _mpfCommunicationCts.Token
            );
            await WaitUntilMpfReady(waitCts.Token);
            var client = new MpfHardwareService.MpfHardwareServiceClient(_grpcChannel);
            var s = CompileMachineState(player);
            _mpfCommandStreamCall = client.Start(s, cancellationToken: _mpfCommunicationCts.Token);
            _mpfSwitchStreamCall = client.SendSwitchChanges(
                cancellationToken: _mpfCommunicationCts.Token
            );
            _receiveMpfCommandsTask = ReceiveMpfCommands();
            OnDisplaysRequested?.Invoke(this, new RequestedDisplays(_mpfDotMatrixDisplays));
            OnStarted?.Invoke(this, EventArgs.Empty);
        }

        // This method repeatedly tries to connect to MPF. Ideally, you would use gRPC's
        // wait-for-ready feature instead, but it is not supported in .netstandard 2.1, which is
        // mandated by Unity. Links:
        // https://grpc.io/docs/guides/wait-for-ready/
        // https://github.com/grpc/grpc-dotnet/issues/1190
        // https://github.com/grpc/grpc-dotnet/blob/c9d26719e8b2a8f03424cacbb168540e35a94b0b/src/Grpc.Net.Client/Grpc.Net.Client.csproj#L21C1-L23C19
        // Alternatively, you could use the channel status, but that is also not supported:
        // https://github.com/grpc/grpc-dotnet/issues/1275
        // Previously, the problem was 'solved' by simply waiting 1.5 seconds to give MPF
        // time to start up, but depending on the computer and whether or not prepackaged
        // (pyinstaller) binaries are used, this is not always enough.
        private async Task WaitUntilMpfReady(CancellationToken ct)
        {
            Logger.Info("Attempting to connect to MPF...");
            var startTime = DateTime.Now;
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(
                _mpfCommunicationCts.Token
            );
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(_connectTimeout), timeoutCts.Token);
            var pingTask = PingUntilResponse();

            if (await Task.WhenAny(pingTask, timeoutTask) == pingTask)
            {
                var pingResponse = await pingTask;
                timeoutCts.Cancel();
                try
                {
                    await timeoutTask;
                }
                catch (OperationCanceledException) { }
                var timeToConnect = (DateTime.Now - startTime).TotalSeconds;
                Logger.Info(
                    $"Successfully connected to MPF in {timeToConnect:F2} seconds. "
                        + $"MPF version: {pingResponse.MpfVersion}"
                );
                return;
            }

            await timeoutTask;
            _mpfCommunicationCts.Cancel();
            try
            {
                await pingTask;
            }
            catch (OperationCanceledException) { }
            throw new TimeoutException(
                $"Timed out trying to connect to MPF after {_connectTimeout} seconds."
            );
        }

        private async Task<PingResponse> PingUntilResponse()
        {
            while (true)
            {
                try
                {
                    var client = new MpfHardwareService.MpfHardwareServiceClient(_grpcChannel);
                    var response = await client.PingAsync(
                        new EmptyRequest(),
                        deadline: DateTime.UtcNow.AddSeconds(1),
                        cancellationToken: _mpfCommunicationCts.Token
                    );
                    return response;
                }
                catch (Exception ex) when (ex is IOException || ex is RpcException) { }
                _mpfCommunicationCts.Token.ThrowIfCancellationRequested();
                Logger.Info("No response from MPF. Retrying...");
            }
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

        private async void OnDestroy()
        {
            _mpfCommunicationCts?.Cancel();
            await _receiveMpfCommandsTask;
            _mpfCommunicationCts?.Dispose();
            _mpfCommunicationCts = null;
            _receiveMpfCommandsTask = null;
            var client = new MpfHardwareService.MpfHardwareServiceClient(_grpcChannel);
            await client.QuitAsync(new QuitRequest(), deadline: DateTime.UtcNow.AddSeconds(3));
            _mpfCommandStreamCall?.Dispose();
            _mpfCommandStreamCall = null;
            _mpfSwitchStreamCall?.Dispose();
            _mpfSwitchStreamCall = null;
            _grpcChannel?.Dispose();
            _grpcChannel = null;
            if (_mpfProcess != null && !_mpfProcess.HasExited)
                _mpfProcess?.Kill();
            _mpfProcess?.Dispose();
            _mpfProcess = null;
        }

        private async Task ReceiveMpfCommands()
        {
            try
            {
                while (
                    await _mpfCommandStreamCall.ResponseStream.MoveNext(_mpfCommunicationCts.Token)
                )
                {
                    var command = _mpfCommandStreamCall.ResponseStream.Current;
                    ExecuteMpfCommand(command);
                }
            }
            catch (RpcException ex)
            {
                if (!_mpfCommunicationCts.IsCancellationRequested)
                    Logger.Error($"Unable to receive commands from MPF. RPC Status: {ex.Status}");
            }
        }

        private void ExecuteMpfCommand(Commands command)
        {
            switch (command.CommandCase)
            {
                case Commands.CommandOneofCase.None:
                    break;
                case Commands.CommandOneofCase.FadeLight:
                    var args = new List<LampEventArgs>();
                    foreach (var fade in command.FadeLight.Fades)
                    {
                        if (_mpfLampNumbers.TryGetNameByNumber(fade.LightNumber, out var lampName))
                            args.Add(new LampEventArgs(lampName, fade.TargetBrightness));
                        else
                            Logger.Error(
                                $"MPF sent a lamp number '{fade.LightNumber}' that is"
                                    + $" not associated with a lamp id."
                            );

                        OnLampsChanged?.Invoke(this, new LampsEventArgs(args.ToArray()));
                    }
                    break;
                case Commands.CommandOneofCase.PulseCoil:
                    if (
                        _mpfCoilNumbers.TryGetNameByNumber(
                            command.PulseCoil.CoilNumber,
                            out var coilName
                        )
                    )
                    {
                        SetCoil(coilName, true);
                        _player.ScheduleAction(
                            command.PulseCoil.PulseMs,
                            () => SetCoil(coilName, false)
                        );
                    }
                    else
                        Logger.Error(
                            $"MPF sent a coil number '{command.PulseCoil.CoilNumber}'"
                                + $" that is not associated with a coil id."
                        );
                    break;
                case Commands.CommandOneofCase.EnableCoil:
                    if (
                        _mpfCoilNumbers.TryGetNameByNumber(
                            command.EnableCoil.CoilNumber,
                            out coilName
                        )
                    )
                        SetCoil(coilName, true);
                    else
                        Logger.Error(
                            $"MPF sent a coil number '{command.EnableCoil.CoilNumber}'"
                                + $" that is not associated with a coil id."
                        );
                    break;
                case Commands.CommandOneofCase.DisableCoil:
                    if (
                        _mpfCoilNumbers.TryGetNameByNumber(
                            command.DisableCoil.CoilNumber,
                            out coilName
                        )
                    )
                        SetCoil(coilName, false);
                    else
                        Logger.Error(
                            $"MPF sent a coil number '{command.DisableCoil.CoilNumber}'"
                                + $" that is not associated with a coil id."
                        );
                    break;
                case Commands.CommandOneofCase.ConfigureHardwareRule:
                    var switchNumber = command.ConfigureHardwareRule.SwitchNumber;
                    var coilNumber = command.ConfigureHardwareRule.CoilNumber;
                    if (
                        _mpfSwitchNumbers.TryGetNameByNumber(switchNumber, out var switchName)
                        && _mpfCoilNumbers.TryGetNameByNumber(coilNumber, out coilName)
                    )
                        _player.AddHardwareRule(switchName, coilName);
                    else
                        Logger.Error(
                            $"MPF wants to add a hardware rule for switch number "
                                + $"'{switchNumber} and coil number '{coilNumber}.' At least one "
                                + $"of them is not associated with an id."
                        );
                    break;
                case Commands.CommandOneofCase.RemoveHardwareRule:
                    switchNumber = command.RemoveHardwareRule.SwitchNumber;
                    coilNumber = command.RemoveHardwareRule.CoilNumber;
                    if (
                        _mpfSwitchNumbers.TryGetNameByNumber(switchNumber, out switchName)
                        && _mpfCoilNumbers.TryGetNameByNumber(coilNumber, out coilName)
                    )
                        _player.RemoveHardwareRule(switchName, coilName);
                    else
                        Logger.Error(
                            $"MPF wants to remove a hardware rule for switch number "
                                + $"'{switchNumber} and coil number '{coilNumber}.' At least one "
                                + $"of them is not associated with an id."
                        );
                    break;
                case Commands.CommandOneofCase.DmdFrameRequest:
                    var frameData = new DisplayFrameData(
                        command.DmdFrameRequest.Name,
                        DisplayFrameFormat.Dmd24,
                        command.DmdFrameRequest.Frame.ToByteArray()
                    );
                    OnDisplayUpdateFrame?.Invoke(this, frameData);
                    break;
                case Commands.CommandOneofCase.SegmentDisplayFrameRequest:
                    Logger.Error("Segment displays are not yet supported by VPEs MPF integration");
                    break;
                default:
                    Logger.Error($"MPF sent an unknown commnand '{command}'");
                    break;
            }
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
                var number = _mpfSwitchNumbers.GetNumberByName(id);
                var change = new SwitchChanges { SwitchNumber = number, SwitchState = isClosed };
                await _mpfSwitchStreamCall.RequestStream.WriteAsync(
                    change,
                    _mpfCommunicationCts.Token
                );
            }
            else
            {
                Logger.Error(
                    $"Switch '{id}' is defined in the MPF game logic engine but not"
                        + $" associated with an MPF number. State change cannot be forwarded to MPF."
                );
            }
        }
    }
}
