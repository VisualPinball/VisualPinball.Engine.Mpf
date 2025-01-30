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

using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace VisualPinball.Engine.Mpf.Unity.Editor
{
    public class PostInstallSetup
    {
        [InitializeOnLoadMethod]
        private static void OnLoad()
        {
            Events.registeredPackages += RegisteredPackagesEventHandler;
        }

        private static void RegisteredPackagesEventHandler(PackageRegistrationEventArgs args)
        {
            var packageInfo = args.added.FirstOrDefault(package =>
                package.name == Constants.PackageName
            );
            if (packageInfo != default)
                Setup(packageInfo.resolvedPath);
        }

        private static void Setup(string path)
        {
            // Copy sample machine folder to streaming assets
            var sourcePath = Path.Combine(path, "SampleMachineFolder");
            var destPath = Path.Combine(Application.streamingAssetsPath, "MpfMachineFolder");
            CopyUtil.CopyDirectory(sourcePath, destPath, recursive: true, overwrite: false);
        }
    }
}
