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
    public class LightDeviceMonitor
        : DeviceMonitor<LightDeviceMessage, LightDeviceMessage.StateJson>
    {
        protected override string Type => LightDeviceMessage.Type;
        protected override ParseStateDelegate ParseState => LightDeviceMessage.FromStateJson;
        public event EventHandler<DeviceAttributeChangeEventArgs<Color>> ColorChanged;

        public LightDeviceMonitor(BcpInterface bcpInterface, string deviceName)
            : base(bcpInterface, deviceName) { }

        protected override void HandleAttributeChange(DeviceAttributeChange change)
        {
            if (change.AttributeName == nameof(LightDeviceMessage.StateJson.color))
                ColorChanged?.Invoke(this, change.GetEventArgsForColor());
            else
                throw new UnknownDeviceAttributeException(change.AttributeName, Type);
        }
    }
}
