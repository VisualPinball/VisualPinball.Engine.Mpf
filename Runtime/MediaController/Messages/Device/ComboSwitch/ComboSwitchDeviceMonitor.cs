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
    public class ComboSwitchDeviceMonitor
        : DeviceMonitor<ComboSwitchDeviceMessage, ComboSwitchDeviceMessage.StateJson>
    {
        protected override string Type => "combo_switch";
        protected override ParseStateDelegate ParseState => ComboSwitchDeviceMessage.FromStateJson;
        public event EventHandler<DeviceAttributeChangeEventArgs<ComboSwitchStatus>> StatusChanged;

        public ComboSwitchDeviceMonitor(BcpInterface bcpInterface, string deviceName)
            : base(bcpInterface, deviceName) { }

        protected override void HandleAttributeChange(DeviceAttributeChange change)
        {
            if (change.AttributeName == nameof(ComboSwitchDeviceMessage.StateJson.state))
                StatusChanged?.Invoke(
                    this,
                    change.GetEventArgsForPrimitiveTypes<ComboSwitchStatus>()
                );
            else
                throw new UnknownDeviceAttributeException(change.AttributeName, Type);
        }
    }
}
