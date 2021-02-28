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

using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Protobuf.Collections;
using Mpf.Vpe;

namespace VisualPinball.Engine.Mpf
{
	public class MpfClient
	{
		private Channel _channel;
		private MpfHardwareService.MpfHardwareServiceClient _client;
		private readonly string _server = "127.0.0.1:50051";

		public void Connect()
		{
			_channel = new Channel(_server, ChannelCredentials.Insecure);
			_client = new MpfHardwareService.MpfHardwareServiceClient(_channel);
		}

		public void Play()
		{
			var ms = new MachineState();
			ms.InitialSwitchStates.Add("sw11", true);
			_client.Start(ms);
		}

		public MachineDescription GetMachineDescription()
		{
			return _client.GetMachineDescription(new EmptyRequest());
		}

		private void OnDisable() {
			_channel.ShutdownAsync().Wait();
		}
	}
}
