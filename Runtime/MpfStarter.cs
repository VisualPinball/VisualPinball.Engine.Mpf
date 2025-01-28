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
using System.Diagnostics;
using System.IO;
using System.Text;
using NLog;
using UnityEngine;
using Logger = NLog.Logger;

namespace VisualPinball.Engine.Mpf.Unity
{
    // GodotOrLegacyMc: MPF versions pre v0.80 use a discontinued kivvy-based media
    // controller, newer versions use Godot.
    public enum MpfMediaController
    {
        None,
        GodotOrLegacyMc,
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
    };

    [Serializable]
    public class MpfStarter
    {
        [SerializeField]
        private MpfExecutableSource _executableSource = MpfExecutableSource.Included;

        [SerializeField]
        private MpfMediaController _mediaController = MpfMediaController.None;

        [SerializeField]
        private MpfOutputType _outputType = MpfOutputType.TableInTerminal;

        [SerializeField]
        private string _machineFolder = "./StreamingAssets/MpfMachineFolder";

        [SerializeField]
        private bool _verboseLogging = false;

        [SerializeField]
        private bool _cacheConfigFiles = true;

        [SerializeField]
        private bool _forceReloadConfig = false;

        [SerializeField]
        private bool _forceLoadAllAssetsOnStart = false;

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

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
        }

        // This is a factory method instead of a constructor because otherwise Unity will not
        // respect the default field values defined above when a new instance of the
        // MpfGamelogicEngine is created in the inspector
        public static MpfStarter Create(
            MpfExecutableSource executableSource = MpfExecutableSource.Included,
            MpfMediaController mediaController = MpfMediaController.None,
            MpfOutputType outputType = MpfOutputType.TableInTerminal,
            string machineFolder = "./StreamingAssets/MpfMachineFolder",
            bool verboseLogging = false,
            bool cacheConfigFiles = true,
            bool forceReloadConfig = false,
            bool forceLoadAllAssetsOnStart = false
        )
        {
            return new MpfStarter
            {
                _executableSource = executableSource,
                _mediaController = mediaController,
                _outputType = outputType,
                _machineFolder = machineFolder,
                _verboseLogging = verboseLogging,
                _cacheConfigFiles = cacheConfigFiles,
                _forceReloadConfig = forceReloadConfig,
                _forceLoadAllAssetsOnStart = forceLoadAllAssetsOnStart,
            };
        }

        private MpfOutputType OutputTypeOverride =>
            UnityEngine.Debug.isDebugBuild ? _outputType : MpfOutputType.None;

        public Process StartMpf()
        {
            var process = new Process();
            process.StartInfo.FileName = GetExecutablePath();
            process.StartInfo.Arguments = GetCmdArgs(MachineFolder);
            // Make sure the MPF window does not pop up in release builds
            var outputTypeOverride = UnityEngine.Debug.isDebugBuild
                ? OutputTypeOverride
                : MpfOutputType.None;
            var openWindow =
                outputTypeOverride == MpfOutputType.LogInTerminal
                || outputTypeOverride == MpfOutputType.TableInTerminal;
            process.StartInfo.UseShellExecute = openWindow;
            process.StartInfo.CreateNoWindow = !openWindow;

            if (!process.StartInfo.CreateNoWindow)
            {
                // On Linux and macOS, start the process through the terminal so it has a window.
#if UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
                process.StartInfo.Aguments = $"-e {process.StartInfo.FileName} {args}";
                process.StartInfo.FileName = "x-terminal-emulator";
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
                // There is no way to pass arguments trough the macOS terminal,
                // so create a temporary shell script that contains the arguments.
                // So the logic goes: This process -> terminal -> shell script -> MPF
                // Very convoluted but there is no other way as far as I know.
                string tmpScriptPath = Path.Combine(Application.temporaryCachePath, "mpf.sh");
                File.WriteAllText(tmpScriptPath, $"#!/bin/bash\n{process.StartInfo.FileName} {process.StartInfo.Arguments}");
                Process.Start("chmod", $"u+x {tmpScriptPath}");
                process.StartInfo.Arguments = $"-a Terminal {tmpScriptPath}";
                process.StartInfo.FileName = "open";
#endif
            }

            if (!openWindow)
            {
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardOutput = true;

                if (outputTypeOverride == MpfOutputType.LogInUnityConsole)
                {
                    process.OutputDataReceived += new DataReceivedEventHandler(
                        (sender, e) => Logger.Info($"MPF: {e.Data}")
                    );

                    process.ErrorDataReceived += new DataReceivedEventHandler(
                        (sender, e) =>
                        {
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

            if (outputTypeOverride == MpfOutputType.LogInUnityConsole)
            {
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }

            return process;
        }

        private string GetExecutablePath()
        {
            switch (_executableSource)
            {
                case MpfExecutableSource.Included:
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                    var dir = Constants.MpfBinaryDirWindows;
                    var name = Constants.MpfBinaryNameWindows;
#elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
                    var dir Constants.MpfBinaryDirLinux;
                    var name = Constants.MpfBinaryNameLinux
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
                    var dir = Constants.MpfBinaryDirMacOS;
                    var name = Constants.MpfBinaryNameMacOS
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
                default:
                    throw new NotImplementedException(
                        $"Cannot get path for unknown MPF executable source '{_executableSource}'"
                    );
            }
        }

        private string GetCmdArgs(string machineFolder)
        {
            var sb = new StringBuilder(machineFolder);

            switch (_mediaController)
            {
                case MpfMediaController.None:
                    sb.Append(" -b");
                    break;
                case MpfMediaController.GodotOrLegacyMc:
                    sb.Insert(0, "both ");
                    break;
                case MpfMediaController.Other:
                    // Default behavior of MPF
                    break;
            }

            if (OutputTypeOverride != MpfOutputType.TableInTerminal)
                sb.Append(" -t");

            if (_verboseLogging)
                sb.Append(" -v -V");

            if (!_cacheConfigFiles)
                sb.Append(" -A");

            if (_forceReloadConfig)
                sb.Append(" -a");

            if (_forceLoadAllAssetsOnStart)
                sb.Append(" -f");

            return sb.ToString();
        }
    }
}
