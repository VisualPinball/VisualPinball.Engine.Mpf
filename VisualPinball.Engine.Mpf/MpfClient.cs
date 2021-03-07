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
using Grpc.Core.Utils;
using Mpf.Vpe;
using NLog;

namespace VisualPinball.Engine.Mpf
{
	/// <summary>
	/// A wrapper with a nicer API than the proto-generated one.
	/// </summary>
	public class MpfClient
	{
		public event EventHandler<FadeLightRequest> OnFadeLight;
		public event EventHandler<PulseCoilRequest> OnPulseCoil;
		public event EventHandler<EnableCoilRequest> OnEnableCoil;
		public event EventHandler<DisableCoilRequest> OnDisableCoil;
		public event EventHandler<ConfigureHardwareRuleRequest> OnConfigureHardwareRule;
		public event EventHandler<RemoveHardwareRuleRequest> OnRemoveHardwareRule;

		private Channel _channel;
		private MpfHardwareService.MpfHardwareServiceClient _client;

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();


		public void Connect(string serverIpPort = "127.0.0.1:50051")
		{
			Logger.Info($"Connecting to {serverIpPort}...");
			_channel = new Channel(serverIpPort, ChannelCredentials.Insecure);
			_client = new MpfHardwareService.MpfHardwareServiceClient(_channel);
		}

		public void StartGame(Dictionary<string, bool> initialSwitches)
		{
			var ms = new MachineState();
			foreach (var sw in initialSwitches.Keys) {
				ms.InitialSwitchStates.Add(sw, initialSwitches[sw]);
			}

			Logger.Info("Starting client...");
			using (var call = _client.Start(ms)) {

				Logger.Info("Client started, retrieving commands...");
				var count = 0;
				call.ResponseStream.ForEachAsync(commands => {

					Logger.Info($"New command: {commands.CommandCase}");
					count++;
					switch (commands.CommandCase) {
						case Commands.CommandOneofCase.None:
							break;
						case Commands.CommandOneofCase.FadeLight:
							OnFadeLight?.Invoke(this, commands.FadeLight);
							break;
						case Commands.CommandOneofCase.PulseCoil:
							OnPulseCoil?.Invoke(this, commands.PulseCoil);
							break;
						case Commands.CommandOneofCase.EnableCoil:
							OnEnableCoil?.Invoke(this, commands.EnableCoil);
							break;
						case Commands.CommandOneofCase.DisableCoil:
							OnDisableCoil?.Invoke(this, commands.DisableCoil);
							break;
						case Commands.CommandOneofCase.ConfigureHardwareRule:
							OnConfigureHardwareRule?.Invoke(this, commands.ConfigureHardwareRule);
							break;
						case Commands.CommandOneofCase.RemoveHardwareRule:
							OnRemoveHardwareRule?.Invoke(this, commands.RemoveHardwareRule);
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
					return Task.CompletedTask;
				}).Wait();

				Logger.Info($"{count} commands dispatched.");
			}
		}

		public MachineDescription GetMachineDescription()
		{
			Logger.Info($"Getting machine description...");
			return _client.GetMachineDescription(new EmptyRequest());
		}

		public void Shutdown() {
			_channel.ShutdownAsync().Wait();
		}
	}
}
