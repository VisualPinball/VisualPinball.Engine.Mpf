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

namespace VisualPinball.Engine.Mpf.Unity.MediaController.Messages.Device.Flipper
{
    public class FlipperDeviceMonitor
        : DeviceMonitor<FlipperDeviceMessage, FlipperDeviceMessage.StateJson>
    {
        protected override string Type => "flipper";
        protected override ParseStateDelegate ParseState => FlipperDeviceMessage.FromStateJson;
        public event EventHandler<DeviceAttributeChangeEventArgs<bool>> EnabledChanged;

        public FlipperDeviceMonitor(BcpInterface bcpInterface, string deviceName)
            : base(bcpInterface, deviceName) { }

        protected override void HandleAttributeChange(DeviceAttributeChange change)
        {
            if (change.AttributeName == nameof(FlipperDeviceMessage.StateJson.enabled))
                EnabledChanged?.Invoke(this, change.GetEventArgsForPrimitiveTypes<bool>());
            else
                throw new UnknownDeviceAttributeException(change.AttributeName, Type);
        }
    }
}
