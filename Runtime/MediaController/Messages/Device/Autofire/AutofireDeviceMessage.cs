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

namespace VisualPinball.Engine.Mpf.Unity.MediaController.Messages.Device.Autofire
{
    public class AutofireDeviceMessage
        : SpecificDeviceMessageBase,
            IEquatable<AutofireDeviceMessage>
    {
        public readonly bool Enabled;

        public AutofireDeviceMessage(string deviceName, bool enabled)
            : base(deviceName)
        {
            Enabled = enabled;
        }

        public static AutofireDeviceMessage FromStateJson(StateJson state, string deviceName)
        {
            return new(deviceName, state.enabled);
        }

        public bool Equals(AutofireDeviceMessage other)
        {
            return base.Equals(other) && Enabled == other.Enabled;
        }

        public class StateJson
        {
            public bool enabled;
        }
    }
}
