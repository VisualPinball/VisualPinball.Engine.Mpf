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

using System.Collections.Generic;
using System.Linq;
using Mpf.Vpe;
using VisualPinball.Engine.Game.Engines;

namespace VisualPinball.Engine.Mpf.Unity
{
	public static class MpfExtensions
	{
		public static IEnumerable<GamelogicEngineSwitch> GetSwitches(this MachineDescription md)
		{
			return md.Switches.Select(sw => new GamelogicEngineSwitch(sw.Name, int.Parse(sw.HardwareNumber)) {
				NormallyClosed = sw.SwitchType.ToLower() == "nc"
			});
		}

		public static IEnumerable<GamelogicEngineCoil> GetCoils(this MachineDescription md)
		{
			return md.Coils.Select(coil => new GamelogicEngineCoil(coil.Name, int.Parse(coil.HardwareNumber)));
		}

		public static IEnumerable<GamelogicEngineLamp> GetLights(this MachineDescription md)
		{
			// todo color
			return md.Lights.Select(light => new GamelogicEngineLamp(light.Name, int.Parse(light.HardwareChannelColor)));
		}
	}
}
