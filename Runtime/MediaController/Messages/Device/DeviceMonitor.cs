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
using Newtonsoft.Json;
using UnityEngine;

namespace VisualPinball.Engine.Mpf.Unity.MediaController.Messages.Device
{
    public abstract class DeviceMonitor<TMessage, TDeviceState>
        : MonitorBase<TMessage, DeviceMessage>
        where TMessage : SpecificDeviceMessageBase, IEquatable<TMessage>
    {
        private string _deviceName;

        protected DeviceMonitor(BcpInterface bcpInterface, string deviceName)
            : base(bcpInterface)
        {
            _deviceName = deviceName;
        }

        protected override string BcpCommand => DeviceMessage.Command;
        protected abstract string Type { get; }
        protected delegate TMessage ParseStateDelegate(
            TDeviceState deserializedState,
            string deviceName
        );
        protected abstract ParseStateDelegate ParseState { get; }

        protected override bool MatchesMonitoringCriteria(DeviceMessage msg)
        {
            return base.MatchesMonitoringCriteria(msg)
                && msg.Type == Type
                && msg.Name == _deviceName;
        }

        protected override void MessageHandler_Received(object sender, DeviceMessage msg)
        {
            base.MessageHandler_Received(sender, msg);
            if (MatchesMonitoringCriteria(msg) && msg.Change != null)
                HandleAttributeChange(msg.Change);
        }

        protected override TMessage GetValueFromMessage(DeviceMessage msg)
        {
            TDeviceState deserializedState;
            try
            {
                deserializedState = msg.State.ToObject<TDeviceState>();
            }
            catch (JsonException e)
            {
                throw new InvalidDeviceStateException(msg.Type, typeof(TDeviceState), e);
            }

            TMessage specificDeviceMessage = ParseState(deserializedState, msg.Name);
            return specificDeviceMessage;
        }

        protected abstract void HandleAttributeChange(DeviceAttributeChange change);
    }
}
