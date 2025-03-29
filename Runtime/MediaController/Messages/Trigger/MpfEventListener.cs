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

namespace VisualPinball.Engine.Mpf.Unity.MediaController.Messages.Trigger
{
    public class MpfEventListener : IDisposable
    {
        [SerializeField]
        private string _eventName;

        private BcpInterface _bcpInterface;
        private BcpMessageHandler<TriggerMessage> _messageHandler;

        public event EventHandler Triggered;

        public MpfEventListener(BcpInterface bcpInterface, string eventName)
        {
            _bcpInterface = bcpInterface;
            _eventName = eventName;
            _bcpInterface.MpfEvents.AddListener(this, _eventName);
            _messageHandler =
                (BcpMessageHandler<TriggerMessage>)
                    _bcpInterface.MessageHandlers.Handlers[TriggerMessage.Command];
            _messageHandler.Received += TriggerMessageHandler_Received;
        }

        public void Dispose()
        {
            _bcpInterface?.MpfEvents.RemoveListener(this, _eventName);
            if (_messageHandler != null)
                _messageHandler.Received -= TriggerMessageHandler_Received;
        }

        private void TriggerMessageHandler_Received(object sender, TriggerMessage msg)
        {
            if (msg.TriggerName == _eventName)
                Triggered?.Invoke(this, EventArgs.Empty);
        }
    }
}
