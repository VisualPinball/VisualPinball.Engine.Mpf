// Visual Pinball Engine
// Copyright (C) 2025 freezy and VPE Team
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
using System.Text.RegularExpressions;
using Mpf.Vpe;
using VisualPinball.Engine.Common;
using VisualPinball.Engine.Game.Engines;
using VisualPinball.Unity;

namespace VisualPinball.Engine.Mpf.Unity
{
    public static class MpfExtensions
    {
        public static IEnumerable<SerializedGamelogicEngineSwitch> GetSwitches(
            this MachineDescription md
        )
        {
            return md.Switches.Select(sw =>
            {
                var gleSw = new SerializedGamelogicEngineSwitch(sw.Name)
                {
                    NormallyClosed = sw.SwitchType.ToLower() == "nc",
                };

                if (
                    Regex
                        .Match(
                            sw.Name,
                            "l(eft)?_?flipper|flipper_?l(eft)?",
                            RegexOptions.IgnoreCase
                        )
                        .Success
                )
                {
                    gleSw.Description = "Left Flipper Button";
                    gleSw.InputActionHint = InputConstants.ActionLeftFlipper;
                }
                if (
                    Regex
                        .Match(
                            sw.Name,
                            "r(ight)?_?flipper|flipper_?r(ight)?",
                            RegexOptions.IgnoreCase
                        )
                        .Success
                )
                {
                    gleSw.Description = "Right Flipper Button";
                    gleSw.InputActionHint = InputConstants.ActionRightFlipper;
                }
                else if (Regex.Match(sw.Name, "plunger", RegexOptions.IgnoreCase).Success)
                {
                    gleSw.Description = "Plunger Button";
                    gleSw.InputActionHint = InputConstants.ActionPlunger;
                }
                else if (Regex.Match(sw.Name, "start", RegexOptions.IgnoreCase).Success)
                {
                    gleSw.Description = "Start Button";
                    gleSw.InputActionHint = InputConstants.ActionStartGame;
                }
                else if (Regex.Match(sw.Name, "trough_?jam", RegexOptions.IgnoreCase).Success)
                {
                    gleSw.Description = "Trough: Jam Switch";
                    gleSw.DeviceHint = "^Trough\\s*\\d?";
                    gleSw.DeviceItemHint = "jam";
                }
                else
                {
                    var troughSwitchMatch = Regex.Match(
                        sw.Name,
                        "trough_?(\\d+)",
                        RegexOptions.IgnoreCase
                    );
                    if (troughSwitchMatch.Success)
                    {
                        var num = troughSwitchMatch.Groups[1].Value;
                        gleSw.Description = $"Trough {num}";
                        gleSw.DeviceHint = "^Trough\\s*\\d?";
                        gleSw.DeviceItemHint = num;
                    }
                }

                return gleSw;
            });
        }

        public static IEnumerable<SerializedGamelogicEngineCoil> GetCoils(
            this MachineDescription md
        )
        {
            var coils = md
                .Coils.Select(coil =>
                {
                    var gleCoil = new SerializedGamelogicEngineCoil(coil.Name);

                    if (
                        Regex
                            .Match(
                                coil.Name,
                                "(l(eft)?_?flipper|flipper_?l(eft)?_?(main)?)$",
                                RegexOptions.IgnoreCase
                            )
                            .Success
                    )
                    {
                        gleCoil.Description = "Left Flipper";
                        gleCoil.DeviceHint = "^(LeftFlipper|LFlipper|FlipperLeft|FlipperL)$";
                        gleCoil.DeviceItemHint = FlipperComponent.MainCoilItem;
                    }
                    else if (
                        Regex
                            .Match(
                                coil.Name,
                                "(l(eft)?_?flipper|flipper_?l(eft)?)_?hold$",
                                RegexOptions.IgnoreCase
                            )
                            .Success
                    )
                    {
                        gleCoil.Description = "Left Flipper (Hold)";
                        gleCoil.DeviceHint = "^(LeftFlipper|LFlipper|FlipperLeft|FlipperL)$";
                        gleCoil.DeviceItemHint = FlipperComponent.HoldCoilItem;
                    }
                    else if (
                        Regex
                            .Match(
                                coil.Name,
                                "(r(ight)?_?flipper|flipper_?r(ight)?_?(main)?)$",
                                RegexOptions.IgnoreCase
                            )
                            .Success
                    )
                    {
                        gleCoil.Description = "Right Flipper";
                        gleCoil.DeviceHint = "^(RightFlipper|RFlipper|FlipperRight|FlipperR)$";
                        gleCoil.DeviceItemHint = FlipperComponent.HoldCoilItem;
                    }
                    else if (
                        Regex
                            .Match(
                                coil.Name,
                                "(r(ight)?_?flipper|flipper_?r(ight)?)_?hold$",
                                RegexOptions.IgnoreCase
                            )
                            .Success
                    )
                    {
                        gleCoil.Description = "Right Flipper (Hold)";
                        gleCoil.DeviceHint = "^(RightFlipper|RFlipper|FlipperRight|FlipperR)$";
                        gleCoil.DeviceItemHint = FlipperComponent.MainCoilItem;
                    }
                    else if (
                        Regex.Match(coil.Name, "trough_?eject", RegexOptions.IgnoreCase).Success
                    )
                    {
                        gleCoil.Description = "Trough Eject";
                        gleCoil.DeviceHint = "^Trough\\s*\\d?";
                        gleCoil.DeviceItemHint = "eject";
                    }

                    return gleCoil;
                })
                .ToArray();

            return coils;
        }

        public static IEnumerable<SerializedGamelogicEngineLamp> GetLights(
            this MachineDescription md
        )
        {
            // todo color
            return md.Lights.Select(light => new SerializedGamelogicEngineLamp(light.Name));
        }

        public static Dictionary<string, string> GetSwitchNumbersByNameDict(
            this MachineDescription md
        )
        {
            Dictionary<string, string> ret = new();

            foreach (SwitchDescription sw in md.Switches)
            {
                ret[sw.Name] = sw.HardwareNumber;
            }

            return ret;
        }

        public static Dictionary<string, string> GetCoilNumbersByNameDict(
            this MachineDescription md
        )
        {
            Dictionary<string, string> ret = new();

            foreach (CoilDescription coil in md.Coils)
            {
                ret[coil.Name] = coil.HardwareNumber;
            }

            return ret;
        }

        public static Dictionary<string, string> GetLampNumbersByNameDict(
            this MachineDescription md
        )
        {
            Dictionary<string, string> ret = new();

            foreach (LightDescription light in md.Lights)
            {
                ret[light.Name] = light.HardwareChannelColor;
            }

            return ret;
        }

        public static IEnumerable<DisplayConfig> GetDmds(this MachineDescription md)
        {
            return md.Dmds.Select(dmd => new DisplayConfig(dmd.Name, dmd.Width, dmd.Height));
        }

        public static bool Equals(this GamelogicEngineSwitch gleSwitch, SwitchDescription mpfSwitch)
        {
            return gleSwitch.Id == mpfSwitch.Name
                && gleSwitch.NormallyClosed == (mpfSwitch.SwitchType.ToLower() == "nc");
        }

        public static bool Equals(this GamelogicEngineCoil gleCoil, CoilDescription mpfCoil)
        {
            return gleCoil.Id == mpfCoil.Name;
        }

        public static bool Equals(this GamelogicEngineLamp gleLamp, LightDescription mpfLight)
        {
            return gleLamp.Id == mpfLight.Name;
        }

        public static bool Equals(this DisplayConfig cfg, DmdDescription dmdDesc)
        {
            return cfg.Id == dmdDesc.Name
                && cfg.Width == dmdDesc.Width
                && cfg.Height == dmdDesc.Height;
        }
    }
}
