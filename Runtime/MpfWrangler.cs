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
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Net.Http;
using Grpc.Core;
using Grpc.Net.Client;
using Mpf.Vpe;
using NLog;
using UnityEngine;
using VisualPinball.Engine.Mpf.Unity.MediaController;
using VisualPinball.Unity;
using Logger = NLog.Logger;

namespace VisualPinball.Engine.Mpf.Unity
{
    // GodotOrLegacyMc: MPF versions pre v0.80 use a discontinued kivvy-based media
    // controller, newer versions use Godot.
    public enum MpfMediaController
    {
        None,
        GodotOrLegacyMc,
        Included,
        Other,
    };

    public enum MpfOutputType
    {
        None,
        TableInTerminal,
        LogInTerminal,
        LogInUnityConsole,
    };

    public enum MpfExecutableSource
    {
        Included,
        ManuallyInstalled,
        AssumeRunning,
    };

    public enum MpfStartupBehavior
    {
        PingUntilReady,
        DelayConnection,
    }

    public enum MpfState
    {
        NotConnected,
        Starting,
        Connected,
        Stopping,
    }

    public class StateChangedEventArgs<T> : EventArgs
    {
        public readonly T CurrentState;
        public readonly T PrevState;

        public StateChangedEventArgs(T newState, T prevState)
        {
            CurrentState = newState;
            PrevState = prevState;
        }
    }

    [Serializable]
    public class MpfWranglerOptions
    {
        [SerializeField]
        private MpfExecutableSource _executableSource = MpfExecutableSource.Included;
        public MpfExecutableSource ExecutableSource => _executableSource;

        [SerializeField]
        private MpfStartupBehavior _startupBehavior = MpfStartupBehavior.PingUntilReady;
        public MpfStartupBehavior StartupBehavior => _startupBehavior;

        [SerializeField]
        private MpfMediaController _mediaController = MpfMediaController.Included;
        public MpfMediaController MediaController => _mediaController;

        [SerializeField]
        private BcpInterfaceOptions _bcpInterfaceOptions = new();
        public BcpInterfaceOptions BcpInterfaceOptions => _bcpInterfaceOptions;

        [SerializeField]
        private MpfOutputType _outputType = MpfOutputType.TableInTerminal;
        public MpfOutputType OutputType => _outputType;

        [SerializeField]
        private string _machineFolder = "./StreamingAssets/MpfMachineFolder";

        public string MachineFolder
        {
            get
            {
                if (_machineFolder != null && _machineFolder.Contains("StreamingAssets"))
                {
                    var m = _machineFolder.Replace("\\", "/");
                    m = m.Split("StreamingAssets")[1];
                    m = m.TrimStart('/');
                    return Path.Combine(Application.streamingAssetsPath, m).Replace("\\", "/");
                }

                return _machineFolder;
            }
            set => _machineFolder = RealPathToSerializedPath(value);
        }

        public static string RealPathToSerializedPath(string path)
        {
            path = path.Replace("\\", "/");
            if (path.Contains("StreamingAssets/"))
                path = "./StreamingAssets/" + path.Split("StreamingAssets/")[1];
            return path;
        }

        [SerializeField]
        private bool _verboseLogging = false;
        public bool VerboseLogging => _verboseLogging;

        [SerializeField]
        private bool _cacheConfigFiles = true;
        public bool CacheConfigFiles => _cacheConfigFiles;

        [SerializeField]
        private bool _forceReloadConfig = false;
        public bool ForceReloadConfig => _forceReloadConfig;

        [SerializeField]
        private bool _forceLoadAllAssetsOnStart = false;
        public bool ForceLoadAllAssetsOnStart => _forceLoadAllAssetsOnStart;

        [Range(3, 30), SerializeField]
        private float _connectTimeout = 10f;
        public float ConnectTimeout => _connectTimeout;

        [Range(0, 15), SerializeField]
        private float _connectDelay = 3f;
        public float ConnectDelay => _connectDelay;

        public static MpfWranglerOptions Create(
            MpfExecutableSource executableSource = MpfExecutableSource.Included,
            MpfStartupBehavior startupBehavior = MpfStartupBehavior.PingUntilReady,
            MpfMediaController mediaController = MpfMediaController.None,
            BcpInterfaceOptions bcpInterfaceOptions = null,
            MpfOutputType outputType = MpfOutputType.TableInTerminal,
            string machineFolder = "./StreamingAssets/MpfMachineFolder",
            bool verboseLogging = false,
            bool cacheConfigFiles = true,
            bool forceReloadConfig = false,
            bool forceLoadAllAssetsOnStart = false,
            float connectTimeout = 10f,
            float connectDelay = 3f
        )
        {
            return new MpfWranglerOptions
            {
                _executableSource = executableSource,
                _startupBehavior = startupBehavior,
                _mediaController = mediaController,
                _bcpInterfaceOptions = bcpInterfaceOptions ?? new BcpInterfaceOptions(),
                _outputType = outputType,
                _machineFolder = machineFolder,
                _verboseLogging = verboseLogging,
                _cacheConfigFiles = cacheConfigFiles,
                _forceReloadConfig = forceReloadConfig,
                _forceLoadAllAssetsOnStart = forceLoadAllAssetsOnStart,
                _connectTimeout = connectTimeout,
                _connectDelay = connectDelay,
            };
        }
    }

    /// <summary>
    /// Responsible for managing the MPF process and the gRPC connection to it. Also starts and
    /// stops the BCP server if the included media controller is used.
    /// </summary>
    public class MpfWrangler : IDisposable
    {
        public event EventHandler<StateChangedEventArgs<MpfState>> MpfStateChanged;
        public event EventHandler<FadeLightRequest> MpfFadeLightRequestReceived;
        public event EventHandler<PulseCoilRequest> MpfPulseCoilRequestReceived;
        public event EventHandler<EnableCoilRequest> MpfEnableCoilRequestReceived;
        public event EventHandler<DisableCoilRequest> MpfDisableCoilRequestReceived;
        public event EventHandler<ConfigureHardwareRuleRequest> MpfConfigureHardwareRuleRequestReceived;
        public event EventHandler<RemoveHardwareRuleRequest> MpfRemoveHardwareRuleRequestReceived;
        public event EventHandler<SetDmdFrameRequest> MpfSetDmdFrameRequestReceived;
        public event EventHandler<SetSegmentDisplayFrameRequest> MpfSetSegmentDisplayFrameRequestReceived;

        private readonly MpfWranglerOptions _options;
        private Process _mpfProcess;
        private GrpcChannel _grpcChannel;
        private AsyncServerStreamingCall<Commands> _mpfCommandStreamCall;
        private AsyncClientStreamingCall<SwitchChanges, EmptyResponse> _mpfSwitchStreamCall;
        private CancellationTokenSource _mpfCommunicationCts;
        private Task _receiveMpfCommandsTask;
        private MpfState _mpfState;
        private LazyInit<SemaphoreSlim> _startStopSemaphore = new(() => new SemaphoreSlim(1, 1));

        private const string _grpcAddress = "http://localhost:50051";

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public MpfState MpfState
        {
            get => _mpfState;
            private set
            {
                if (value != _mpfState)
                {
                    var prevState = _mpfState;
                    _mpfState = value;
                    MpfStateChanged?.Invoke(
                        this,
                        new StateChangedEventArgs<MpfState>(_mpfState, prevState)
                    );
                }
            }
        }

        public readonly BcpInterface BcpInterface;

        public MpfWrangler(MpfWranglerOptions options)
        {
            _options = options;
            if (_options.MediaController == MpfMediaController.Included)
                BcpInterface = new BcpInterface(_options.BcpInterfaceOptions);
        }

        private MpfOutputType OutputTypeOverride =>
            UnityEngine.Debug.isDebugBuild ? _options.OutputType : MpfOutputType.None;

        public async Task StartMpf(MachineState initialState, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            await _startStopSemaphore.Ref.WaitAsync(ct);

            if (MpfState == MpfState.Connected)
            {
                _startStopSemaphore.Ref.Release();
                return;
            }

            try
            {
                MpfState = MpfState.Starting;

                if (_options.ExecutableSource != MpfExecutableSource.AssumeRunning)
                {
                    KillAllMpfProcesses();
                    _mpfProcess = StartMpfProcess();
                }

                if (_options.MediaController == MpfMediaController.Included)
                    await Task.WhenAll(BcpInterface.StartServer(), ConnectToMpf(initialState, ct));
                else
                    await ConnectToMpf(initialState, ct);
            }
            catch (Exception ex)
            {
                try
                {
                    if (_options.MediaController == MpfMediaController.Included)
                        await BcpInterface.StopServer();
                }
                finally
                {
                    if (_mpfProcess != null && !_mpfProcess.HasExited)
                        _mpfProcess.Kill();

                    KillAllMpfProcesses();

                    _mpfProcess?.Dispose();
                    _mpfProcess = null;

                    try
                    {
                        MpfState = MpfState.NotConnected;
                    }
                    finally
                    {
                        _startStopSemaphore.Ref.Release();
                    }

                    throw ex;
                }
            }

            try
            {
                MpfState = MpfState.Connected;
            }
            finally
            {
                _startStopSemaphore.Ref.Release();
            }
        }

        public async Task<MachineDescription> GetMachineDescription(
            float timeout,
            CancellationToken ct
        )
        {
            ct.ThrowIfCancellationRequested();
            if (MpfState != MpfState.Connected)
                throw new InvalidOperationException(
                    "MPF must be connected to get machine description."
                );

            var client = new MpfHardwareService.MpfHardwareServiceClient(_grpcChannel);
            return await client.GetMachineDescriptionAsync(
                new EmptyRequest(),
                deadline: DateTime.UtcNow.AddSeconds(timeout),
                cancellationToken: ct
            );
        }

        public async Task SendSwitchChange(SwitchChanges changes)
        {
            if (MpfState != MpfState.Connected)
                throw new InvalidOperationException(
                    "Cannot send switch change to MPF, because it is not connected."
                );

            await _mpfSwitchStreamCall.RequestStream.WriteAsync(
                changes,
                _mpfCommunicationCts.Token
            );
        }

        public async Task StopMpf()
        {
            await _startStopSemaphore.Ref.WaitAsync();

            if (MpfState == MpfState.NotConnected)
            {
                _startStopSemaphore.Ref.Release();
                return;
            }

            try
            {
                MpfState = MpfState.Stopping;
            }
            finally
            {
                try
                {
                    _mpfCommunicationCts?.Cancel();
                    if (_receiveMpfCommandsTask != null)
                    {
                        try
                        {
                            await _receiveMpfCommandsTask;
                        }
                        catch (OperationCanceledException) { }
                    }
                }
                finally
                {
                    try
                    {
                        var client = new MpfHardwareService.MpfHardwareServiceClient(_grpcChannel);
                        _ = await client.QuitAsync(
                            new QuitRequest(),
                            deadline: DateTime.UtcNow.AddSeconds(1)
                        );
                    }
                    finally
                    {
                        try
                        {
                            if (_options.MediaController == MpfMediaController.Included)
                                await BcpInterface.StopServer();
                        }
                        finally
                        {
                            try
                            {
                                if (_mpfProcess != null && !_mpfProcess.HasExited)
                                {
                                    // MPF should shut down on its own after receiving the Quit
                                    // message. If it is still running after one second, just kill
                                    // it.
                                    var processExited = new TaskCompletionSource<bool>();
                                    _mpfProcess.Exited += new EventHandler(
                                        (sender, args) => processExited.TrySetResult(true)
                                    );

                                    if (
                                        await Task.WhenAny(
                                            processExited.Task,
                                            Task.Delay(TimeSpan.FromSeconds(1))
                                        ) != processExited.Task
                                        && !_mpfProcess.HasExited
                                    )
                                        _mpfProcess.Kill();
                                }
                            }
                            finally
                            {
                                _receiveMpfCommandsTask = null;
                                _mpfCommunicationCts?.Dispose();
                                _mpfCommunicationCts = null;
                                _mpfCommandStreamCall?.Dispose();
                                _mpfCommandStreamCall = null;
                                _mpfSwitchStreamCall?.Dispose();
                                _mpfSwitchStreamCall = null;
                                _grpcChannel?.Dispose();
                                _grpcChannel = null;
                                _mpfProcess?.Dispose();
                                _mpfProcess = null;

                                try
                                {
                                    MpfState = MpfState.NotConnected;
                                }
                                finally
                                {
                                    _startStopSemaphore.Ref.Release();
                                }
                            }
                        }
                    }
                }
            }
        }

        private Process StartMpfProcess()
        {
            var process = new Process();
            process.StartInfo.FileName = GetExecutablePath();
            process.StartInfo.Arguments = GetCmdArgs(_options.MachineFolder);
            // Make sure the MPF window does not pop up in release builds
            var createWindow =
                OutputTypeOverride is MpfOutputType.LogInTerminal or MpfOutputType.TableInTerminal;
            process.StartInfo.UseShellExecute = createWindow;
            process.StartInfo.CreateNoWindow = !createWindow;

            if (createWindow)
            {
                // On Linux and macOS, start the process through the terminal so it has a window.
#if UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
                process.StartInfo.Arguments =
                    $"-e {process.StartInfo.FileName} {process.StartInfo.Arguments}";
                process.StartInfo.FileName = "x-terminal-emulator";
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
                // There is no way to pass arguments trough the macOS terminal,
                // so create a temporary shell script that contains the arguments.
                // The call chain is: This process -> terminal -> shell script -> MPF
                // Very convoluted but there is no better way as far as Stackoverflow knows:
                // https://stackoverflow.com/questions/29510815/how-to-pass-command-line-arguments-to-a-program-run-with-the-open-command
                string tmpScriptPath = Path.Combine(Application.temporaryCachePath, "mpf.sh");
                File.WriteAllText(
                    tmpScriptPath,
                    $"#!/bin/bash\n{process.StartInfo.FileName} {process.StartInfo.Arguments}"
                );
                Process.Start("chmod", $"u+x {tmpScriptPath}");
                process.StartInfo.Arguments = $"-a Terminal {tmpScriptPath}";
                process.StartInfo.FileName = "open";
#endif
            }
            else
            {
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardOutput = true;

                if (OutputTypeOverride == MpfOutputType.LogInUnityConsole)
                {
                    process.OutputDataReceived += new DataReceivedEventHandler(
                        (sender, e) => Logger.Info($"MPF: {e.Data}")
                    );

                    process.ErrorDataReceived += new DataReceivedEventHandler(
                        (sender, e) =>
                        {
                            if (e.Data == null)
                                return;
                            // For some reason, all (?) output from MPF is routed to this error handler,
                            // so filter manually. This is obviously flawed and will sometimes fail
                            // to recognize errors.
                            // https://github.com/missionpinball/mpf/issues/1866
                            if (e.Data.Contains("ERROR") || e.Data.Contains("Exception"))
                                Logger.Error($"MPF: {e.Data}");
                            else if (e.Data.Contains("WARNING"))
                                Logger.Warn($"MPF: {e.Data}");
                            else
                                Logger.Info($"MPF: {e.Data}");
                        }
                    );
                }
            }

            process.Start();

            if (OutputTypeOverride == MpfOutputType.LogInUnityConsole)
            {
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }

            return process;
        }

        private async Task ConnectToMpf(MachineState initialState, CancellationToken ct)
        {
            var handler = new YetAnotherHttpHandler() { Http2Only = true };
            var options = new GrpcChannelOptions()
            {
                HttpHandler = handler,
                DisposeHttpClient = true,
            };
            _grpcChannel = GrpcChannel.ForAddress(_grpcAddress, options);
            _mpfCommunicationCts = new CancellationTokenSource();

            try
            {
                if (_options.ExecutableSource != MpfExecutableSource.AssumeRunning)
                    await WaitUntilMpfReady(_grpcChannel, ct);

                var client = new MpfHardwareService.MpfHardwareServiceClient(_grpcChannel);
                _mpfCommandStreamCall = client.Start(
                    initialState,
                    cancellationToken: _mpfCommunicationCts.Token
                );
                _mpfSwitchStreamCall = client.SendSwitchChanges(
                    cancellationToken: _mpfCommunicationCts.Token
                );
                _receiveMpfCommandsTask = ReceiveMpfCommands();
            }
            catch (Exception ex)
            {
                _mpfCommunicationCts?.Cancel();
                try
                {
                    if (_receiveMpfCommandsTask != null)
                        await _receiveMpfCommandsTask;
                }
                catch (OperationCanceledException) { }
                finally
                {
                    _receiveMpfCommandsTask = null;
                    _mpfCommunicationCts?.Dispose();
                    _mpfCommunicationCts = null;
                    _grpcChannel?.Dispose();
                    _grpcChannel = null;
                    _mpfSwitchStreamCall?.Dispose();
                    _mpfSwitchStreamCall = null;
                    _mpfCommandStreamCall?.Dispose();
                    _mpfCommandStreamCall = null;
                    throw ex;
                }
            }
        }

        private string GetExecutablePath()
        {
            switch (_options.ExecutableSource)
            {
                case MpfExecutableSource.Included:
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                    var dir = Constants.MpfBinaryDirWindows;
                    var name = Constants.MpfBinaryNameWindows;
#elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
                    var dir = Constants.MpfBinaryDirLinux;
                    var name = Constants.MpfBinaryNameLinux;
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
                    var dir = Constants.MpfBinaryDirMacOS;
                    var name = Constants.MpfBinaryNameMacOS;
#else
                    goto case ExecutableSource.ManuallyInstalled;
#endif

#if UNITY_EDITOR
                    var root = Constants.GetPackageDir();
#else
                    var root = Application.streamingAssetsPath;
#endif
                    return Path.Combine(root, Constants.MpfBinariesDirName, dir, name);

                case MpfExecutableSource.ManuallyInstalled:
                    return "mpf";
                case MpfExecutableSource.AssumeRunning:
                    throw new InvalidOperationException(
                        $"Executable source is set to '{MpfExecutableSource.AssumeRunning},' so "
                            + "there is no need to start any executable."
                    );
                default:
                    throw new NotImplementedException(
                        $"Cannot get path for unknown MPF executable source '{_options.ExecutableSource}'"
                    );
            }
        }

        private string GetCmdArgs(string machineFolder)
        {
            var sb = new StringBuilder(machineFolder);

            switch (_options.MediaController)
            {
                case MpfMediaController.None:
                    sb.Append(" -b");
                    break;
                case MpfMediaController.GodotOrLegacyMc:
                    sb.Insert(0, "both ");
                    break;
                default:
                    break;
            }

            if (OutputTypeOverride != MpfOutputType.TableInTerminal)
                sb.Append(" -t");

            if (_options.VerboseLogging)
                sb.Append(" -v -V");

            if (!_options.CacheConfigFiles)
                sb.Append(" -A");

            if (_options.ForceReloadConfig)
                sb.Append(" -a");

            if (_options.ForceLoadAllAssetsOnStart)
                sb.Append(" -f");

            return sb.ToString();
        }

        private async Task WaitUntilMpfReady(GrpcChannel channel, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            // The 'Ping' RPC was added to the VPE-MPF interface definition in January 2025. As of
            // January 28th 2025, it is only available in a fork
            // (https://github.com/arthurkehrwald/mpf/tree/0.80.x) of MPF and the binaries included
            // with VPE. For compatibility with official versions of MPF, connection can simply be
            // delayed by a few seconds, but this can fail if MPF is slow to start up and wastes
            // time if MPF starts up more quickly than expected.
            switch (_options.StartupBehavior)
            {
                case MpfStartupBehavior.PingUntilReady:
                    Logger.Info("Attempting to connect to MPF...");
                    var startTime = DateTime.Now;
                    var pingResponse = await PingUntilResponseOrTimeout(channel, ct);
                    var timeToConnect = (DateTime.Now - startTime).TotalSeconds;
                    Logger.Info(
                        $"Successfully connected to MPF in {timeToConnect:F2} seconds. "
                            + $"MPF version: {pingResponse.MpfVersion}"
                    );
                    break;
                case MpfStartupBehavior.DelayConnection:
                    await Task.Delay(TimeSpan.FromSeconds(_options.ConnectDelay), ct);
                    break;
            }
        }

        // This method repeatedly pings MPF until it responds or time runs out. Ideally,
        // you would use gRPC's wait-for-ready feature instead, but that is not supported in
        // .netstandard 2.1, which is mandated by Unity. Links:
        // https://grpc.io/docs/guides/wait-for-ready/
        // https://github.com/grpc/grpc-dotnet/issues/1190
        // https://github.com/grpc/grpc-dotnet/blob/c9d26719e8b2a8f03424cacbb168540e35a94b0b/src/Grpc.Net.Client/Grpc.Net.Client.csproj#L21C1-L23C19
        // Alternatively, you could use the channel status, but that is also not supported:
        // https://github.com/grpc/grpc-dotnet/issues/1275
        private async Task<PingResponse> PingUntilResponseOrTimeout(
            GrpcChannel channel,
            CancellationToken ct
        )
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(_options.ConnectTimeout), cts.Token);
            var pingTask = PingUntilResponse(channel, cts.Token);

            if (await Task.WhenAny(pingTask, timeoutTask) == pingTask)
            {
                var pingResponse = await pingTask;
                cts.Cancel();
                try
                {
                    await timeoutTask;
                }
                catch (OperationCanceledException) { }
                return pingResponse;
            }

            await timeoutTask;
            cts.Cancel();
            try
            {
                await pingTask;
            }
            catch (OperationCanceledException) { }

            ct.ThrowIfCancellationRequested();

            try
            {
                // Try one last time in case the application lost focus and wasn't actually pinging
                var client = new MpfHardwareService.MpfHardwareServiceClient(channel);
                return await client.PingAsync(
                    new EmptyRequest(),
                    deadline: DateTime.UtcNow.AddSeconds(1),
                    cancellationToken: ct
                );
            }
            catch (Exception ex) when (ex is IOException or RpcException)
            {
                throw new TimeoutException(
                    $"Timed out trying to connect to MPF after {_options.ConnectTimeout} seconds."
                );
            }
        }

        private async Task<PingResponse> PingUntilResponse(
            GrpcChannel channel,
            CancellationToken ct
        )
        {
            ct.ThrowIfCancellationRequested();
            var client = new MpfHardwareService.MpfHardwareServiceClient(channel);
            while (true)
            {
                try
                {
                    return await client.PingAsync(
                        new EmptyRequest(),
                        deadline: DateTime.UtcNow.AddSeconds(1),
                        cancellationToken: ct
                    );
                }
                catch (Exception ex) when (ex is IOException or RpcException)
                {
                    ct.ThrowIfCancellationRequested();
                    Logger.Info("No response from MPF. Retrying...");
                    await Task.Delay(TimeSpan.FromSeconds(0.2));
                }
            }
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
                {
                    MpfState = MpfState.NotConnected;
                    Logger.Error($"Unable to receive commands from MPF: {ex}");
                }
            }
        }

        private void ExecuteMpfCommand(Commands command)
        {
            switch (command.CommandCase)
            {
                case Commands.CommandOneofCase.None:
                    Logger.Warn("Received empty command from MPF.");
                    break;
                case Commands.CommandOneofCase.FadeLight:
                    MpfFadeLightRequestReceived?.Invoke(this, command.FadeLight);
                    break;
                case Commands.CommandOneofCase.PulseCoil:
                    MpfPulseCoilRequestReceived?.Invoke(this, command.PulseCoil);
                    break;
                case Commands.CommandOneofCase.EnableCoil:
                    MpfEnableCoilRequestReceived?.Invoke(this, command.EnableCoil);
                    break;
                case Commands.CommandOneofCase.DisableCoil:
                    MpfDisableCoilRequestReceived?.Invoke(this, command.DisableCoil);
                    break;
                case Commands.CommandOneofCase.ConfigureHardwareRule:
                    MpfConfigureHardwareRuleRequestReceived?.Invoke(
                        this,
                        command.ConfigureHardwareRule
                    );
                    break;
                case Commands.CommandOneofCase.RemoveHardwareRule:
                    MpfRemoveHardwareRuleRequestReceived?.Invoke(this, command.RemoveHardwareRule);
                    break;
                case Commands.CommandOneofCase.DmdFrameRequest:
                    MpfSetDmdFrameRequestReceived?.Invoke(this, command.DmdFrameRequest);
                    break;
                case Commands.CommandOneofCase.SegmentDisplayFrameRequest:
                    MpfSetSegmentDisplayFrameRequestReceived?.Invoke(
                        this,
                        command.SegmentDisplayFrameRequest
                    );
                    break;
                default:
                    Logger.Error($"MPF sent an unknown commnand '{command}'");
                    break;
            }
        }

        private void KillAllMpfProcesses()
        {
            var mpfProcesses = Process.GetProcessesByName("mpf");
            try
            {
                foreach (var process in mpfProcesses)
                {
                    if (!process.HasExited)
                        process.Kill();
                }
            }
            finally
            {
                foreach (var process in mpfProcesses)
                    process.Dispose();
            }
        }

        public void Dispose()
        {
            _mpfCommunicationCts?.Dispose();
            BcpInterface?.Dispose();
            _grpcChannel?.Dispose();
            _mpfProcess?.Dispose();
            _mpfCommandStreamCall?.Dispose();
            _mpfSwitchStreamCall?.Dispose();
            _startStopSemaphore.Ref.Dispose();
        }
    }
}
