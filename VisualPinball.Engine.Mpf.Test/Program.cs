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
using System.Threading.Tasks;
using VisualPinball.Engine.Mpf;

namespace MpfTest
{
	public static class Program
	{
		public static void Main(string[] args)
		{
			// Console.WriteLine("Starting...");
			// var client = new MpfClient();
			// client.Connect();
			// client.StartGame(new Dictionary<string, bool> {
			// 	{ "sw11", true }
			// });
			// Console.WriteLine("Description = " + client.GetMachineDescription());
			// Console.WriteLine("Done!");


			var s = Stopwatch.StartNew();
			var mpfApi = new MpfApi(@"../../../../VisualPinball.Engine.Mpf/machine");

			mpfApi.Launch();

			mpfApi.StartGame(new Dictionary<string, bool> {
				{"sw_11", false},
			});

			var descr = mpfApi.GetMachineDescription();
			Console.WriteLine($"Description: {descr} in {s.ElapsedMilliseconds}ms");
		}
	}
}
