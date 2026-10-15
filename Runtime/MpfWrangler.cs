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

        // MPF's own output (stdout/stderr), when it's not shown in a terminal window
        private static readonly Logger ProcessLogger = LogManager.GetLogger("Mpf.Process");
        private static readonly LogThrottle ProcessWarnThrottle = new(200, TimeSpan.FromSeconds(1));
        private static readonly LogThrottle EmptyCommandThrottle = new(3, TimeSpan.FromSeconds(30));
        private static readonly LogThrottle CommandHandlerFailedThrottle = new(3, TimeSpan.FromSeconds(30));
        private static readonly LogOncePerKey<int> UnknownCommandLog = new();

        private string _startupStep;
        private string _machineFolder;
        private volatile bool _processExitExpected;
        private bool _launchedViaTerminal;
        private bool _echoOutputToConsole;
        private bool _inStdErrTraceback;

        /// <summary>
        /// MPF's version, as reported when VPE connected to it. Null until then, and with
        /// <see cref="MpfStartupBehavior.DelayConnection"/>, which doesn't ask.
        /// </summary>
        public string MpfVersion { get; private set; }

        /// <summary>
        /// The MPF executable that was started last, or null if none was.
        /// </summary>
        public string ExecutablePath { get; private set; }

        /// <summary>
        /// The command line arguments MPF was started with last.
        /// </summary>
        public string Arguments { get; private set; }

        /// <summary>
        /// Why the last start failed, or null.
        /// </summary>
        public string StartupError { get; private set; }

        /// <summary>
        /// MPF's exit code, once it exited.
        /// </summary>
        public int? ExitCode { get; private set; }

        /// <summary>
        /// Whether MPF's output is captured into the log (logger <c>Mpf.Process</c>). When MPF
        /// runs in a terminal window, it isn't, and <see cref="MpfLogFolder"/> has its log.
        /// </summary>
        public bool OutputCaptured { get; private set; }

        /// <summary>
        /// Where MPF writes its own log files.
        /// </summary>
        public string MpfLogFolder
        {
            get
            {
                // resolving the machine folder needs the main thread, so it's cached when MPF starts
                var machineFolder = _machineFolder ?? _options.MachineFolder;
                return string.IsNullOrEmpty(machineFolder) ? null : Path.Combine(machineFolder, "logs");
            }
        }

        /// <summary>
        /// The output type actually used. Release builds never show MPF's terminal.
        /// </summary>
        public MpfOutputType EffectiveOutputType => OutputTypeOverride;

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
                StartupError = null;
                _machineFolder = _options.MachineFolder;

                if (_options.ExecutableSource != MpfExecutableSource.AssumeRunning)
                {
                    _startupStep = "starting the MPF process";
                    KillAllMpfProcesses();
                    _mpfProcess = StartMpfProcess();
                }
                else
                {
                    Logger.Info(
                        "Connecting to an MPF instance that is expected to be running already, at {Address:l}",
                        _grpcAddress
                    );
                }

                _startupStep = "connecting to MPF";
                if (_options.MediaController == MpfMediaController.Included)
                    await Task.WhenAll(BcpInterface.StartServer(), ConnectToMpf(initialState, ct));
                else
                    await ConnectToMpf(initialState, ct);
            }
            catch (Exception ex)
            {
                LogStartupFailure(ex);
                try
                {
                    if (_options.MediaController == MpfMediaController.Included)
                        await BcpInterface.StopServer();
                }
                catch (Exception stopEx)
                {
                    // the start failure is what counts, it's rethrown below
                    Logger.Debug(stopEx, "Stopping the BCP server after the failed MPF start failed too.");
                }
                finally
                {
                    _processExitExpected = true;
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
                }
                throw;
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

            _processExitExpected = true;
            Logger.Info("Stopping MPF");
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
                                    {
                                        Logger.Info("MPF didn't quit within a second, killing it.");
                                        _mpfProcess.Kill();
                                    }
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
            var executable = GetExecutablePath();
            var machineFolder = _machineFolder;
            ExecutablePath = executable;
            Arguments = GetCmdArgs(machineFolder);
            ExitCode = null;

            if (_options.ExecutableSource == MpfExecutableSource.Included && !File.Exists(executable))
                throw new FileNotFoundException(
                    $"The MPF executable included with VPE is missing at {executable}.",
                    executable
                );
            if (!Directory.Exists(machineFolder))
                Logger.Warn(
                    "MPF machine folder {MachineFolder:l} does not exist, so MPF won't find its config.",
                    machineFolder
                );

            var process = new Process();
            process.StartInfo.FileName = executable;
            process.StartInfo.Arguments = Arguments;
            // Make sure the MPF window does not pop up in release builds
            var outputType = OutputTypeOverride;
            var createWindow =
                outputType is MpfOutputType.LogInTerminal or MpfOutputType.TableInTerminal;
            process.StartInfo.UseShellExecute = createWindow;
            process.StartInfo.CreateNoWindow = !createWindow;
            _launchedViaTerminal = false;

            if (createWindow)
            {
                // On Linux and macOS, start the process through the terminal so it has a window.
#if UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
                process.StartInfo.Arguments =
                    $"-e {process.StartInfo.FileName} {process.StartInfo.Arguments}";
                process.StartInfo.FileName = "x-terminal-emulator";
                _launchedViaTerminal = true;
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
                _launchedViaTerminal = true;
#endif
            }
            else
            {
                // Always read what MPF writes: a redirected pipe that nobody reads fills up and
                // blocks MPF. It goes to the log, so a user's report includes it.
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardOutput = true;
                _echoOutputToConsole = outputType == MpfOutputType.LogInUnityConsole;
                _inStdErrTraceback = false;
                process.OutputDataReceived += (sender, e) => LogProcessOutput(e.Data, false);
                process.ErrorDataReceived += (sender, e) => LogProcessOutput(e.Data, true);
            }

            process.EnableRaisingEvents = true;
            process.Exited += (sender, args) => OnProcessExited(process);
            _processExitExpected = false;
            OutputCaptured = !createWindow;

            process.Start();

            if (OutputCaptured)
            {
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }

            Logger.Info(
                "Started MPF (pid {Pid}): {Executable:l} {Arguments:l}, machine folder {MachineFolder:l}, "
                    + "output {OutputType}, media controller {MediaController}, MPF's own log in {MpfLogFolder:l}",
                process.Id,
                process.StartInfo.FileName,
                process.StartInfo.Arguments,
                machineFolder,
                outputType,
                _options.MediaController,
                MpfLogFolder
            );

            return process;
        }

        /// <summary>
        /// Logs a line MPF wrote. Called on a thread pool thread, one line at a time per stream.
        /// </summary>
        /// <remarks>
        /// MPF writes all of its log to stderr (https://github.com/missionpinball/mpf/issues/1866),
        /// so the level is guessed from the text. Errors in there come from the machine config or
        /// the game code, not from VPE, so they're warnings here.
        /// </remarks>
        private void LogProcessOutput(string line, bool fromStdErr)
        {
            if (line == null)
                return;

            var warn = fromStdErr && IsProblemLine(line, ref _inStdErrTraceback);
            if (warn)
            {
                if (ProcessWarnThrottle.ShouldLog(out var suppressed))
                {
                    if (suppressed == 0)
                        ProcessLogger.Warn("{Line:l}", line);
                    else
                        ProcessLogger.Warn(
                            "{Line:l} ({Suppressed} lines suppressed before)",
                            line,
                            suppressed
                        );
                }
            }
            else if (_echoOutputToConsole)
                // the user chose to see MPF's output in the Unity console
                ProcessLogger.Info("{Line:l}", line);
            else if (ProcessLogger.IsDebugEnabled)
                ProcessLogger.Debug("{Line:l}", line);
        }

        /// <summary>
        /// Whether a line of Python output reports a problem. Lines of a traceback count too.
        /// </summary>
        private static bool IsProblemLine(string line, ref bool inTraceback)
        {
            if (line.StartsWith("Traceback (most recent call last)", StringComparison.Ordinal))
            {
                inTraceback = true;
                return true;
            }
            if (inTraceback)
            {
                // indented lines are the stack, the first one that isn't is the exception
                if (line.Length > 0 && char.IsWhiteSpace(line[0]))
                    return true;
                inTraceback = false;
                return true;
            }
            return line.Contains("ERROR")
                || line.Contains("CRITICAL")
                || line.Contains("Exception")
                || line.Contains("WARNING");
        }

        private void OnProcessExited(Process process)
        {
            int? exitCode = null;
            try
            {
                exitCode = process.ExitCode;
            }
            catch (Exception e)
            {
                // already disposed after stopping
                Logger.Debug(e, "Cannot read MPF's exit code.");
            }
            ExitCode = exitCode;
            var code = exitCode?.ToString() ?? "unknown";

            if (_launchedViaTerminal)
                // that's the terminal launcher, which returns right away
                Logger.Debug("MPF's terminal launcher exited with code {ExitCode:l}", code);
            else if (_processExitExpected)
                Logger.Info("MPF exited with code {ExitCode:l}", code);
            else
                Logger.Warn(
                    "MPF exited unexpectedly with code {ExitCode:l}. {Hint:l}",
                    code,
                    OutputCaptured
                        ? "Its output is in the log above (logger Mpf.Process)."
                        : $"Its own log is in {MpfLogFolder}."
                );
        }

        /// <summary>
        /// Whether a start failed because of something outside of VPE: MPF missing, not
        /// responding, or exiting.
        /// </summary>
        internal static bool IsEnvironmentFailure(Exception e) =>
            e is System.ComponentModel.Win32Exception or TimeoutException or RpcException or IOException;

        private void LogStartupFailure(Exception ex)
        {
            var step = _startupStep ?? "starting MPF";
            if (ex is OperationCanceledException)
            {
                Logger.Debug("MPF startup was cancelled while {Step:l}.", step);
                return;
            }

            StartupError = $"{step}: {ex.Message}";
            var executable =
                _options.ExecutableSource == MpfExecutableSource.AssumeRunning
                    ? "(assumed to be running)"
                    : ExecutablePath;
            if (IsEnvironmentFailure(ex))
                Logger.Warn(
                    ex,
                    "Could not start MPF, failed while {Step:l}. Executable source {ExecutableSource}, "
                        + "executable {Executable:l}, machine folder {MachineFolder:l}. {Hint:l}",
                    step,
                    _options.ExecutableSource,
                    executable,
                    _machineFolder,
                    OutputCaptured
                        ? "MPF's output is in the log above (logger Mpf.Process)."
                        : $"MPF's own log is in {MpfLogFolder}."
                );
            else
                // not something the user can fix, the caller logs the exception
                Logger.Info(
                    "MPF startup failed while {Step:l} (executable {Executable:l}, machine folder {MachineFolder:l})",
                    step,
                    executable,
                    _machineFolder
                );
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

                _startupStep = "opening the command streams to MPF";
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
            catch (Exception)
            {
                _mpfCommunicationCts?.Cancel();
                try
                {
                    if (_receiveMpfCommandsTask != null)
                        await _receiveMpfCommandsTask;
                }
                catch (Exception receiveEx)
                {
                    // the connection failure is what counts, it's rethrown below
                    if (receiveEx is not OperationCanceledException)
                        Logger.Debug(receiveEx, "Receiving MPF commands failed while giving up connecting.");
                }
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
                }
                throw;
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
                    _startupStep = "waiting for MPF to respond";
                    Logger.Info(
                        "Waiting for MPF to respond at {Address:l}, for up to {Timeout} s",
                        _grpcAddress,
                        _options.ConnectTimeout
                    );
                    var stopwatch = Stopwatch.StartNew();
                    var pingResponse = await PingUntilResponseOrTimeout(channel, ct);
                    MpfVersion = pingResponse.MpfVersion;
                    Logger.Info(
                        "Connected to MPF {MpfVersion:l} in {ConnectMs} ms",
                        MpfVersion,
                        stopwatch.ElapsedMilliseconds
                    );
                    break;
                case MpfStartupBehavior.DelayConnection:
                    _startupStep = "waiting before connecting to MPF";
                    await Task.Delay(TimeSpan.FromSeconds(_options.ConnectDelay), ct);
                    Logger.Info(
                        "Connecting to MPF after a fixed delay of {Delay} s (its version isn't queried in this mode)",
                        _options.ConnectDelay
                    );
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
                    Logger.Debug("No response from MPF yet, retrying.");
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
                    try
                    {
                        ExecuteMpfCommand(command);
                    }
                    catch (Exception e)
                    {
                        // a handler failing must not stop the commands that follow
                        if (CommandHandlerFailedThrottle.ShouldLog(out var suppressed))
                            Logger.Error(
                                e,
                                "Handling MPF command {Command} failed ({Suppressed} similar suppressed before)",
                                command.CommandCase,
                                suppressed
                            );
                    }
                }
            }
            catch (RpcException ex)
            {
                if (!_mpfCommunicationCts.IsCancellationRequested)
                {
                    MpfState = MpfState.NotConnected;
                    Logger.Warn(
                        ex,
                        "Lost the connection to MPF, it no longer drives the table. {Hint:l}",
                        OutputCaptured
                            ? "MPF's output is in the log above (logger Mpf.Process)."
                            : $"MPF's own log is in {MpfLogFolder}."
                    );
                }
            }
        }

        private void ExecuteMpfCommand(Commands command)
        {
            switch (command.CommandCase)
            {
                case Commands.CommandOneofCase.None:
                    if (EmptyCommandThrottle.ShouldLog())
                        Logger.Warn("Received an empty command from MPF.");
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
                    // a newer MPF than this package knows
                    if (UnknownCommandLog.ShouldLog((int)command.CommandCase))
                        Logger.Warn(
                            "MPF sent command {CommandCase}, which VPE doesn't know. MPF {MpfVersion:l} "
                                + "may be newer than VPE's MPF package.",
                            (int)command.CommandCase,
                            MpfVersion ?? "(unknown version)"
                        );
                    break;
            }
        }

        private void KillAllMpfProcesses()
        {
            var mpfProcesses = Process.GetProcessesByName("mpf");
            try
            {
                var killed = 0;
                foreach (var process in mpfProcesses)
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                        killed++;
                    }
                }
                if (killed > 0)
                    Logger.Info("Killed {Count} MPF process(es) that were still running.", killed);
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
