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
using System.IO;
using System.Threading.Tasks;
using Mpf.Vpe;

namespace VisualPinball.Engine.Mpf
{
	public class MpfApi : IDisposable
	{
		public readonly MpfClient Client = new MpfClient();
		private readonly MpfSpawner _spawner;

		public MpfApi(string machineFolder)
		{
			_spawner = new MpfSpawner(Path.GetFullPath(machineFolder));
		}

		public static MachineDescription GetMachineDescription(string machineFolder)
		{
			var client = new MpfClient();
			var spawner = new MpfSpawner(machineFolder);
			spawner.Spawn(new MpfConsoleOptions { OutputType = OutputType.Log, VerboseLogging = true });
			client.Connect("localhost:50051");
			client.StartGame(new Dictionary<string, bool>(), false);
			var description = client.GetMachineDescription();
			client.Shutdown();
			return description;
		}

		/// <summary>
		/// Launches MPF in the background and connects to it via gRPC.
		/// </summary>
		/// <param name="options">MPF options</param>
		/// <param name="port">gRPC port to use for MPC/VPE communication</param>
		/// <returns></returns>
		public void Launch(MpfConsoleOptions options, int port = 50051)
		{
			_spawner.Spawn(options);
			Client.Connect($"localhost:{port}");
		}

		/// <summary>
		/// Starts MPF, i.e. it will start polling for switches and sending events.
		/// </summary>
		/// <param name="initialSwitches">Initial switch states of the machine</param>
		public void StartGame(Dictionary<string, bool> initialSwitches = null)
		{
			Client.StartGame(initialSwitches ?? new Dictionary<string, bool>());
		}

		/// <summary>
		/// Returns the machine description.
		/// </summary>
		public MachineDescription GetMachineDescription()
		{
			return Client.GetMachineDescription();
		}

		public async Task Switch(string swName, bool swValue)
		{
			await Client.Switch(swName, swValue);
		}

		public void Dispose()
		{
			Client?.Shutdown();
		}
	}
}
