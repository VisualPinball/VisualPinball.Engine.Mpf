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
using System.Linq;
using System.Text.RegularExpressions;
using Mpf.Vpe;
using VisualPinball.Engine.Common;
using VisualPinball.Engine.Game.Engines;
using VisualPinball.Unity;
using NLog;
using Logger = NLog.Logger;

namespace VisualPinball.Engine.Mpf.Unity
{
	public static class MpfExtensions
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public static IEnumerable<GamelogicEngineSwitch> GetSwitches(this MachineDescription md)
		{
			return md.Switches.Select(sw => {
				var gleSw = new GamelogicEngineSwitch(sw.Name, int.Parse(sw.HardwareNumber)) {
					NormallyClosed = sw.SwitchType.ToLower() == "nc"
				};

				if (Regex.Match(sw.Name, "l(eft)?_?flipper|flipper_?l(eft)?", RegexOptions.IgnoreCase).Success) {
					gleSw.Description = "Left Flipper Button";
					gleSw.InputActionHint = InputConstants.ActionLeftFlipper;

				} if (Regex.Match(sw.Name, "r(ight)?_?flipper|flipper_?r(ight)?", RegexOptions.IgnoreCase).Success) {
					gleSw.Description = "Right Flipper Button";
					gleSw.InputActionHint = InputConstants.ActionRightFlipper;

				} else if (Regex.Match(sw.Name, "plunger", RegexOptions.IgnoreCase).Success) {
					gleSw.Description = "Plunger Button";
					gleSw.InputActionHint = InputConstants.ActionPlunger;

				} else if (Regex.Match(sw.Name, "start", RegexOptions.IgnoreCase).Success) {
					gleSw.Description = "Start Button";
					gleSw.InputActionHint = InputConstants.ActionStartGame;

				} else if (Regex.Match(sw.Name, "trough_?jam", RegexOptions.IgnoreCase).Success) {
					gleSw.Description = "Trough: Jam Switch";
					gleSw.DeviceHint = "^Trough\\s*\\d?";
					gleSw.DeviceItemHint = "jam";

				} else {
					var troughSwitchMatch = Regex.Match(sw.Name, "trough_?(\\d+)", RegexOptions.IgnoreCase);
					if (troughSwitchMatch.Success) {
						var num = troughSwitchMatch.Groups[1].Value;
						gleSw.Description = $"Trough {num}";
						gleSw.DeviceHint = "^Trough\\s*\\d?";
						gleSw.DeviceItemHint = num;
					}
				}

				return gleSw;
			});
		}

		public static IEnumerable<GamelogicEngineCoil> GetCoils(this MachineDescription md)
		{
			var coils = md.Coils.Select(coil => {
				var gleCoil = new GamelogicEngineCoil(coil.Name, int.Parse(coil.HardwareNumber));

				if (Regex.Match(coil.Name, "(l(eft)?_?flipper|flipper_?l(eft)?_?(main)?)$", RegexOptions.IgnoreCase).Success) {
					gleCoil.Description = "Left Flipper";
					gleCoil.DeviceHint = "^(LeftFlipper|LFlipper|FlipperLeft|FlipperL)$";
					gleCoil.DeviceItemHint = FlipperComponent.MainCoilItem;

				} else if (Regex.Match(coil.Name, "(l(eft)?_?flipper|flipper_?l(eft)?)_?hold$", RegexOptions.IgnoreCase).Success) {
					gleCoil.Description = "Left Flipper (Hold)";
					gleCoil.DeviceHint = "^(LeftFlipper|LFlipper|FlipperLeft|FlipperL)$";
					gleCoil.DeviceItemHint = FlipperComponent.HoldCoilItem;

				} else if (Regex.Match(coil.Name, "(r(ight)?_?flipper|flipper_?r(ight)?_?(main)?)$", RegexOptions.IgnoreCase).Success) {
					gleCoil.Description = "Right Flipper";
					gleCoil.DeviceHint = "^(RightFlipper|RFlipper|FlipperRight|FlipperR)$";
					gleCoil.DeviceItemHint = FlipperComponent.HoldCoilItem;

				} else if (Regex.Match(coil.Name, "(r(ight)?_?flipper|flipper_?r(ight)?)_?hold$", RegexOptions.IgnoreCase).Success) {
					gleCoil.Description = "Right Flipper (Hold)";
					gleCoil.DeviceHint = "^(RightFlipper|RFlipper|FlipperRight|FlipperR)$";
					gleCoil.DeviceItemHint = FlipperComponent.MainCoilItem;

				} else if (Regex.Match(coil.Name, "trough_?eject", RegexOptions.IgnoreCase).Success) {
					gleCoil.Description = "Trough Eject";
					gleCoil.DeviceHint = "^Trough\\s*\\d?";
					gleCoil.DeviceItemHint = "eject";
				}

				return gleCoil;
			}).ToArray();

			return coils;
		}

		public static IEnumerable<GamelogicEngineLamp> GetLights(this MachineDescription md)
		{
			// todo color
			return md.Lights.Select(light => {
				Logger.Info(light);
				return new GamelogicEngineLamp(light.Name, int.Parse(light.HardwareChannelNumber));
			});
		}
	}
}
