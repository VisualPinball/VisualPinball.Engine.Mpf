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

namespace VisualPinball.Engine.Mpf.Unity.MediaController.Messages.Device.Switch
{
    public class SwitchDeviceMessage : SpecificDeviceMessageBase, IEquatable<SwitchDeviceMessage>
    {
        public readonly bool IsActive;
        public readonly int RecycleJitterCount;

        public SwitchDeviceMessage(string deviceName, bool isActive, int recycleJitterCount)
            : base(deviceName)
        {
            IsActive = isActive;
            RecycleJitterCount = recycleJitterCount;
        }

        public static SwitchDeviceMessage FromStateJson(StateJson state, string deviceName)
        {
            return new(deviceName, state.state != 0, state.recycle_jitter_count);
        }

        public bool Equals(SwitchDeviceMessage other)
        {
            return base.Equals(other)
                && IsActive == other.IsActive
                && RecycleJitterCount == other.RecycleJitterCount;
        }

        public class StateJson
        {
            public int state;
            public int recycle_jitter_count;
        }
    }
}
