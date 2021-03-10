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
using System.Runtime.InteropServices;
using System.Threading;

namespace VisualPinball.Engine.Mpf
{
	internal class MpfSpawner
	{
		private Thread _thread;
		private readonly string _pwd;
		private readonly string _machineFolder;

		private readonly SemaphoreSlim _ready = new SemaphoreSlim(0, 1);

		public MpfSpawner(string machineFolder)
		{
			_pwd = Path.GetDirectoryName(machineFolder);
			_machineFolder = Path.GetFileName(machineFolder);
		}

		public void Spawn(MpfConsoleOptions options)
		{
			var mpfExe = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "mpf.exe" : "mpf";
			var mpfExePath = GetFullPath(mpfExe);
			if (mpfExePath == null) {
				throw new InvalidOperationException($"Could not find {mpfExe}!");
			}

			_thread = new Thread(() => {
				Thread.CurrentThread.IsBackground = true;
				RunMpf(mpfExePath, options);
			});

			_thread.Start();
			_ready.Wait();
		}

		private void RunMpf(string mpfExePath, MpfConsoleOptions options)
		{
			var args = $"\"{_machineFolder}\"";
			if (!options.UseMediaController) {
				args += " -b";
			}
			if (options.ShowLogInsteadOfConsole) {
				args += " -t";
			}
			if (options.VerboseLogging) {
				args += " -v -V";
			}
			var info = new ProcessStartInfo {
				FileName = mpfExePath,
				WorkingDirectory = _pwd,
				Arguments = args,
				UseShellExecute = true,
				RedirectStandardOutput = false,

			};

			using (var process = Process.Start(info)) {
				_ready.Release();
				process.WaitForExit();
			}
		}

		/// <summary>
		/// Goes through the OS's PATHs to find the provided executable.
		/// </summary>
		/// <param name="fileName">Executable filename</param>
		/// <returns>Full path or null of not found.</returns>
		private static string GetFullPath(string fileName)
		{
			// in current working directory?
			if (File.Exists(fileName)) {
				return Path.GetFullPath(fileName);
			}

			// go through all PATHs
			var values = Environment.GetEnvironmentVariable("PATH");
			foreach (var path in values.Split(Path.PathSeparator)) {
				var fullPath = Path.Combine(path, fileName);
				if (File.Exists(fullPath)) {
					return fullPath;
				}
			}
			return null;
		}
	}

	/// <summary>
	/// A few things we can configure when launching MPF
	/// <seealso cref="https://docs.missionpinball.org/en/latest/running/commands/game.html">Documentation</seealso>
	///
	/// </summary>
	public class MpfConsoleOptions
	{
		public bool UseMediaController;
		public bool ShowLogInsteadOfConsole;
		public bool VerboseLogging = true;
	}
}
