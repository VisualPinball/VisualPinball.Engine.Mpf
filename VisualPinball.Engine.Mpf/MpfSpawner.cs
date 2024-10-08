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
using NLog;

namespace VisualPinball.Engine.Mpf
{
	internal class MpfSpawner
	{
		private Thread _thread;
		private readonly string _pwd;
		private readonly string _machineFolder;

		private readonly SemaphoreSlim _ready = new SemaphoreSlim(0, 1);
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

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

			switch (options.MediaController) {
				case MediaController.None:
					args += " -b";
					break;
				case MediaController.MpfMc:
					args = "both " + args;
					break;
				case MediaController.Other:
					// Default behavior of MPF
					break;
			}

			switch (options.OutputType) {
				case OutputType.TextUi:
					// Default behavior of MPF
					break;
				case OutputType.Log:
					args += " -t";
					break;
			}

			if (options.VerboseLogging) {
				args += " -v -V";
			}

			if (!options.cacheConfigFiles) {
				args += " -A";
			}

			if (options.forceReloadConfig) {
				args += " -a";
			}

			if (options.forceLoadAllAssetsOnStart) {
				args += " -f";
			}

			var info = new ProcessStartInfo {
				FileName = mpfExePath,
				WorkingDirectory = _pwd,
				Arguments = args,
				UseShellExecute = !options.CatchStdOut,
				RedirectStandardOutput = options.CatchStdOut,
			};

			Logger.Info($"[MPF] Spawning: > {mpfExePath} {args}");

			using (var process = Process.Start(info)) {
				Thread.Sleep(1500);

				_ready.Release();
				if (!options.CatchStdOut) {
					process.WaitForExit();

				} else {
					using (var reader = process.StandardOutput) {
						var result = reader.ReadToEnd();
						Console.Write(result);
						process.WaitForExit();
					}
				}
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
			var values = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine);
			values += Path.PathSeparator + Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);
			foreach (var path in values.Split(Path.PathSeparator)) {
				var fullPath = Path.Combine(path, fileName);
				if (File.Exists(fullPath)) {
					return fullPath;
				}
			}
			return null;
		}
	}

	public enum MediaController { None, MpfMc, Other };
	public enum OutputType { TextUi, Log };

	/// <summary>
	/// A few things we can configure when launching MPF
	/// <see also cref="https://docs.missionpinball.org/en/latest/running/commands/game.html">Documentation</seealso>	
	/// </summary>
	[Serializable]
	public class MpfConsoleOptions
	{
		public MediaController MediaController = MediaController.MpfMc;
		public OutputType OutputType = OutputType.TextUi;
		public bool VerboseLogging = false;
		public bool CatchStdOut = false;
		public bool cacheConfigFiles = true;
		public bool forceReloadConfig = false;
		public bool forceLoadAllAssetsOnStart = false;
	}
}
