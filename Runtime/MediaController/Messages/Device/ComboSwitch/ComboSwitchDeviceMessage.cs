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

namespace VisualPinball.Engine.Mpf.Unity.MediaController.Messages.Device.ComboSwitch
{
    public class ComboSwitchDeviceMessage
        : SpecificDeviceMessageBase,
            IEquatable<ComboSwitchDeviceMessage>
    {
        public readonly ComboSwitchStatus Status;

        public ComboSwitchDeviceMessage(string deviceName, ComboSwitchStatus status)
            : base(deviceName)
        {
            Status = status;
        }

        public static ComboSwitchDeviceMessage FromStateJson(StateJson state, string deviceName)
        {
            ComboSwitchStatus status = StringEnum.GetValueFromString<ComboSwitchStatus>(
                state.state
            );
            return new(deviceName, status);
        }

        public bool Equals(ComboSwitchDeviceMessage other)
        {
            return base.Equals(other) && Status == other.Status;
        }

        public class StateJson
        {
            public string state;
        }
    }

    public enum ComboSwitchStatus
    {
        [StringValue("inactive")]
        Inactive,

        [StringValue("both")]
        Both,

        [StringValue("one")]
        One,
    }
}
