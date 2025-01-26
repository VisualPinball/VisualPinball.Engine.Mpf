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

        void IPostprocessBuildWithReport.OnPostprocessBuild(BuildReport report)
        {
            if (
                report.summary.result == BuildResult.Failed
                || report.summary.result == BuildResult.Cancelled
            )
                return;

            Logger.Info("Adding MPF binaries to build...");

            // Get the directory of the MPF package from the Unity package manager
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(
                typeof(BuildPostprocessing).Assembly
            );
            var platform = report.summary.platform;
            var binaryDirName = platform switch
            {
                BuildTarget.StandaloneLinux64 => Constants.MpfBinaryDirLinux,
                BuildTarget.StandaloneOSX => Constants.MpfBinaryDirMacOS,
                BuildTarget.StandaloneWindows => Constants.MpfBinaryDirWindows,
                BuildTarget.StandaloneWindows64 => Constants.MpfBinaryDirWindows,
                _ => throw new PlatformNotSupportedException(
                    "Visual Pinball Engine does not ship with an MPF executable for the build "
                        + $"platform '{platform}.' The build will not work unless MPF is installed "
                        + $"on the end-user's device"
                ),
            };
            var sourcePath = Path.Combine(
                packageInfo.resolvedPath,
                Constants.MpfBinariesDirName,
                binaryDirName
            );

            var dataDir = Directory
                .GetDirectories(Directory.GetParent(report.summary.outputPath).ToString(), "*_Data")
                .FirstOrDefault();

            var destPath = Path.Combine(
                dataDir,
                "StreamingAssets",
                Constants.MpfBinariesDirName,
                binaryDirName
            );

            Directory.CreateDirectory(destPath);
            CopyUtil.CopyDirectory(sourcePath, destPath, recursive: true);

            Logger.Info("Successfully added MPF binaries to build.");
        }
    }
}
