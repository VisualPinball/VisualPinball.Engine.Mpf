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
    public class SwitchDeviceMonitor
        : DeviceMonitor<SwitchDeviceMessage, SwitchDeviceMessage.StateJson>
    {
        protected override string Type => "switch";
        protected override ParseStateDelegate ParseState => SwitchDeviceMessage.FromStateJson;
        public event EventHandler<DeviceAttributeChangeEventArgs<bool>> IsActiveChanged;
        public event EventHandler<DeviceAttributeChangeEventArgs<int>> RecycleJitterCountChanged;

        public SwitchDeviceMonitor(BcpInterface bcpInterface, string deviceName)
            : base(bcpInterface, deviceName) { }

        protected override void HandleAttributeChange(DeviceAttributeChange change)
        {
            switch (change.AttributeName)
            {
                case nameof(SwitchDeviceMessage.StateJson.state):
                    change = new(
                        change.AttributeName,
                        ConvertIsActiveString(change.OldValue),
                        ConvertIsActiveString(change.NewValue)
                    );
                    IsActiveChanged?.Invoke(this, change.GetEventArgsForPrimitiveTypes<bool>());
                    break;
                case nameof(SwitchDeviceMessage.StateJson.recycle_jitter_count):
                    RecycleJitterCountChanged?.Invoke(
                        this,
                        change.GetEventArgsForPrimitiveTypes<int>()
                    );
                    break;
                default:
                    throw new UnknownDeviceAttributeException(change.AttributeName, Type);
            }
        }

        private string ConvertIsActiveString(string isActive)
        {
            return isActive == "0" ? "false" : "true";
        }
    }
}
