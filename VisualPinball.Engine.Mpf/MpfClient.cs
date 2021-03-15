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
using System.Threading;
using System.Threading.Tasks;
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
		private Thread _commandsThread;
		private AsyncServerStreamingCall<Commands> _commandStream;
		private AsyncClientStreamingCall<SwitchChanges, EmptyResponse> _switchStream;

		public void Connect(string serverIpPort = "127.0.0.1:50051")
		{
			Logger.Info($"Connecting to {serverIpPort}...");
			_channel = new Channel(serverIpPort, ChannelCredentials.Insecure);
			_client = new MpfHardwareService.MpfHardwareServiceClient(_channel);
		}

		public void StartGame(Dictionary<string, bool> initialSwitches, bool handleStream = true)
		{
			var ms = new MachineState();
			foreach (var sw in initialSwitches.Keys) {
				ms.InitialSwitchStates.Add(sw, initialSwitches[sw]);
			}

			Logger.Info("Starting player with machine state: " + ms);
			_commandStream = _client.Start(ms);

			if (handleStream) {
				_commandsThread = new Thread(ReceiveCommands) { IsBackground = true };
				_commandsThread.Start();
			}

			_switchStream = _client.SendSwitchChanges();
		}

		public async Task Switch(string swName, bool swValue)
		{
			await _switchStream.RequestStream.WriteAsync(new SwitchChanges
				{SwitchNumber = swName, SwitchState = swValue});
		}

		private async void ReceiveCommands()
		{
			Logger.Info("Client started, retrieving commands...");
			while (await _commandStream.ResponseStream.MoveNext()) {
				var commands = _commandStream.ResponseStream.Current;
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
			}
		}

		public MachineDescription GetMachineDescription()
		{
			Logger.Info($"Getting machine description...");
			return _client.GetMachineDescription(new EmptyRequest());
		}

		public void Shutdown()
		{
			Logger.Info("Shutting down...");
			_client.Quit(new QuitRequest());
			_commandStream?.Dispose();
			_channel.ShutdownAsync().Wait();
			Logger.Info("All down.");
		}
	}
}
