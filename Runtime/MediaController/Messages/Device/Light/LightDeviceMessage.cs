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

using System;
using UnityEngine;

namespace VisualPinball.Engine.Mpf.Unity.MediaController.Messages.Device.Light
{
    public class LightDeviceMessage : SpecificDeviceMessageBase, IEquatable<LightDeviceMessage>
    {
        public const string Type = "light";
        public readonly Color LightColor;

        public LightDeviceMessage(string deviceName, Color lightColor)
            : base(deviceName)
        {
            LightColor = lightColor;
        }

        public static LightDeviceMessage FromStateJson(StateJson state, string deviceName)
        {
            float r,
                g,
                b;
            try
            {
                r = state.color[0] / 255f;
                g = state.color[1] / 255f;
                b = state.color[2] / 255f;
            }
            catch (IndexOutOfRangeException e)
            {
                throw new InvalidDeviceStateException(Type, typeof(StateJson), e);
            }

            return new LightDeviceMessage(deviceName, new Color(r, g, b));
        }

        public bool Equals(LightDeviceMessage other)
        {
            return base.Equals(other) && LightColor == other.LightColor;
        }

        public class StateJson
        {
            public int[] color;
        }
    }
}
