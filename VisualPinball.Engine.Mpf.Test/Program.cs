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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VisualPinball.Engine.Mpf;

namespace MpfTest
{
	public static class Program
	{
		public static async Task Main(string[] args)
		{
			var machineFolder = Path.GetFullPath(@"../../../../VisualPinball.Engine.Mpf/machine");

			// Console.WriteLine("Starting...");
			// var client = new MpfClient();
			// client.Connect();
			// client.StartGame(new Dictionary<string, bool> {
			// 	{ "sw11", true }
			// });
			// Console.WriteLine("Description = " + client.GetMachineDescription());
			// Console.WriteLine("Done!");


			var s = Stopwatch.StartNew();
			var mpfApi = new MpfApi(machineFolder);

			mpfApi.Launch();

			mpfApi.StartGame(new Dictionary<string, bool> {
				{"sw_11", false},
			});
			mpfApi.Client.OnConfigureHardwareRule += (_, request) => {
				Console.WriteLine($"[MPF] configure hw/rule: sw{request.SwitchNumber} -> c{request.CoilNumber} @{request.HoldPower} | pulse: {request.PulseMs}ms @{request.PulsePower}");
			};
			mpfApi.Client.OnRemoveHardwareRule += (_, request) => {
				Console.WriteLine($"[MPF] remove hw/rule: sw{request.SwitchNumber} -> c{request.CoilNumber}");
			};
			mpfApi.Client.OnEnableCoil += (_, request) => {
				Console.WriteLine($"[MPF] enable coil c{request.CoilNumber} @{request.HoldPower} | pulse: {request.PulseMs}ms @{request.PulsePower}");
			};
			mpfApi.Client.OnDisableCoil += (_, request) => {
				Console.WriteLine($"[MPF] disable coil c{request.CoilNumber}");
			};
			mpfApi.Client.OnPulseCoil += (_, request) => {
				Console.WriteLine($"[MPF] pulse coil c{request.CoilNumber} {request.PulseMs}ms @{request.PulsePower}");
			};
			mpfApi.Client.OnFadeLight += (_, request) => {
				Console.WriteLine($"[MPF] light fades ({request.CommonFadeMs}ms):");
				foreach (var fade in request.Fades.ToList()) {
					Console.WriteLine($"  l{fade.LightNumber} @{fade.TargetBrightness}");
				}
			};

			var descr = mpfApi.GetMachineDescription();
			Console.WriteLine($"Description: {descr} in {s.ElapsedMilliseconds}ms");

			ConsoleKeyInfo key;
			do {
				key = Console.ReadKey();
				switch (key.Key) {
					case ConsoleKey.A:
						await mpfApi.Switch("0", true);
						break;
					case ConsoleKey.S:
						await mpfApi.Switch("1", false);
						break;
				}
			} while (key.Key != ConsoleKey.Escape);

			mpfApi.Dispose();
		}
	}
}
