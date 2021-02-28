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
		private readonly MpfClient _client = new MpfClient();
		private readonly MpfSpawner _spawner;

		public MpfApi(string machineFolder)
		{
			_spawner = new MpfSpawner(Path.GetFullPath(machineFolder));
		}

		/*/// <summary>
		/// Launches MPF in the background and connects to it via gRPC.
		/// </summary>
		/// <param name="port">gRPC port to use for MPC/VPE communication</param>
		/// <returns></returns>
		public async Task Launch(int port = 50051)
		{
			await _spawner.Spawn();
			await _client.Connect($"localhost:{port}");
		}

		/// <summary>
		/// Starts MPF, i.e. it will start polling for switches and sending events.
		/// </summary>
		/// <param name="initialSwitches">Initial switch states of the machine</param>
		public void Start(Dictionary<string, bool> initialSwitches = null)
		{
			_client.Start(initialSwitches ?? new Dictionary<string, bool>());
		}

		/// <summary>
		/// Returns the machine description.
		/// </summary>
		public async Task<MachineDescription> GetMachineDescription()
		{
			return await _client.GetMachineDescription();
		}
		*/
		public void Dispose()
		{
			//_client?.Dispose();
		}
	}
}
