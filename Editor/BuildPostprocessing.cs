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
using System.IO;
using System.Linq;
using NLog;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using Logger = NLog.Logger;

namespace VisualPinball.Engine.Mpf.Unity.Editor
{
    public class BuildPostprocessing : IPostprocessBuildWithReport
    {
        int IOrderedCallback.callbackOrder => 0;

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private const string _unsupportedPlaformMessage =
            "Visual Pinball Engine does not ship with an MPF executable for the build "
            + "platform '{}}.' The build will not work unless MPF is installed "
            + "on the end-user's device";

        void IPostprocessBuildWithReport.OnPostprocessBuild(BuildReport report)
        {
            if (
                report.summary.result == BuildResult.Failed
                || report.summary.result == BuildResult.Cancelled
            )
                return;

            var streamingAssetsPath = FindStreamingAssets(
                report.summary.platform,
                report.summary.outputPath
            );
            CleanMachineFolder(streamingAssetsPath);
            AddMpfBinaries(report.summary.platform, streamingAssetsPath);
        }

        private static string FindStreamingAssets(BuildTarget platform, string buildExePath)
        {
            string dataDir;

            if (
                platform
                is BuildTarget.StandaloneWindows
                    or BuildTarget.StandaloneWindows64
                    or BuildTarget.StandaloneLinux64
            )
            {
                dataDir = Directory
                    .GetDirectories(Directory.GetParent(buildExePath).ToString(), "*_Data")
                    .FirstOrDefault();
            }
            else if (platform is BuildTarget.StandaloneOSX)
            {
                dataDir = Path.Combine(buildExePath, "Contents", "Resources", "Data");
            }
            else
            {
                throw new PlatformNotSupportedException(
                    string.Format(_unsupportedPlaformMessage, platform)
                );
            }

            return Path.Combine(dataDir, "StreamingAssets");
        }

        private static void AddMpfBinaries(BuildTarget platform, string streamingAssetsPath)
        {
            Logger.Info("Adding MPF binaries to build...");
            // Get the directory of the MPF package from the Unity package manager
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(
                typeof(BuildPostprocessing).Assembly
            );

            var binaryDirName = platform switch
            {
                BuildTarget.StandaloneLinux64 => Constants.MpfBinaryDirLinux,
                BuildTarget.StandaloneOSX => Constants.MpfBinaryDirMacOS,
                BuildTarget.StandaloneWindows => Constants.MpfBinaryDirWindows,
                BuildTarget.StandaloneWindows64 => Constants.MpfBinaryDirWindows,
                _ => throw new PlatformNotSupportedException(
                    string.Format(_unsupportedPlaformMessage, platform)
                ),
            };
            var sourcePath = Path.Combine(
                packageInfo.resolvedPath,
                Constants.MpfBinariesDirName,
                binaryDirName
            );

            var destPath = Path.Combine(
                streamingAssetsPath,
                Constants.MpfBinariesDirName,
                binaryDirName
            );

            Directory.CreateDirectory(destPath);
            CopyUtil.CopyDirectory(sourcePath, destPath, recursive: true, overwrite: true);

            Logger.Info("Successfully added MPF binaries to build.");
        }

        private static void CleanMachineFolder(string streamingAssetsPath)
        {
            Logger.Info("Removing log files and audits from machine folder...");

            var machineFolders = Directory
                .GetDirectories(streamingAssetsPath)
                .Where((dir) => File.Exists(Path.Combine(dir, "config", "config.yaml")));

            foreach (var mf in machineFolders)
            {
                // Delete log files from previous runs
                var logDir = Path.Combine(mf, "logs");
                var logFiles = Directory.GetFiles(logDir, "*.log", SearchOption.TopDirectoryOnly);
                foreach (var logFile in logFiles)
                    File.Delete(logFile);

                // Delete audits file (contains statistics about previous runs)
                var auditsFile = Path.Combine(mf, "data", "audits.yaml");
                if (File.Exists(auditsFile))
                    File.Delete(auditsFile);
            }

            Logger.Info("Successfully removed log files and audits from machine folder.");
        }
    }
}
