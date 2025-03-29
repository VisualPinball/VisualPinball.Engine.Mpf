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

namespace VisualPinball.Engine.Mpf.Unity.MediaController.Messages.Device.Playfield
{
    public class PlayfieldDeviceMonitor
        : DeviceMonitor<PlayfieldDeviceMessage, PlayfieldDeviceMessage.StateJson>
    {
        protected override string Type => "playfield";
        protected override ParseStateDelegate ParseState => PlayfieldDeviceMessage.FromStateJson;

        public event EventHandler<DeviceAttributeChangeEventArgs<int>> AvailableBallsChanged;
        public event EventHandler<DeviceAttributeChangeEventArgs<int>> BallsRequestedChanged;
        public event EventHandler<DeviceAttributeChangeEventArgs<int>> BallsChanged;

        public PlayfieldDeviceMonitor(BcpInterface bcpInterface, string deviceName)
            : base(bcpInterface, deviceName) { }

        protected override void HandleAttributeChange(DeviceAttributeChange change)
        {
            switch (change.AttributeName)
            {
                case nameof(PlayfieldDeviceMessage.StateJson.available_balls):
                    AvailableBallsChanged?.Invoke(
                        this,
                        change.GetEventArgsForPrimitiveTypes<int>()
                    );
                    break;
                case nameof(PlayfieldDeviceMessage.StateJson.balls_requested):
                    BallsRequestedChanged?.Invoke(
                        this,
                        change.GetEventArgsForPrimitiveTypes<int>()
                    );
                    break;
                case nameof(PlayfieldDeviceMessage.StateJson.balls):
                    BallsChanged?.Invoke(this, change.GetEventArgsForPrimitiveTypes<int>());
                    break;
                default:
                    throw new UnknownDeviceAttributeException(change.AttributeName, Type);
            }
        }
    }
}
