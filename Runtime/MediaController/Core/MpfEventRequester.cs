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

using System.Collections.Generic;
using UnityEngine;

namespace VisualPinball.Engine.Mpf.Unity.MediaController
{
    public class MpfEventRequester<TEvent>
    {
        public delegate ISentMessage CreateMessage(TEvent @event);

        private readonly BcpInterface _bcpInterface;
        private readonly CreateMessage _createStartListeningMessage;
        private readonly CreateMessage _createStopListeningMessage;
        private readonly Dictionary<TEvent, HashSet<object>> _listeners = new();
        private bool _canSendMonitoringRequests = false;
        private bool CanSendMonitoringRequests
        {
            get => _canSendMonitoringRequests;
            set
            {
                if (!_canSendMonitoringRequests && value)
                {
                    foreach (var kvp in _listeners)
                    {
                        if (kvp.Value.Count > 0)
                            SendStartMonitoringRequest(kvp.Key);
                    }
                }

                _canSendMonitoringRequests = value;
            }
        }

        public MpfEventRequester(
            BcpInterface bcpInterface,
            CreateMessage createStartListeningMessage,
            CreateMessage createStopListeningMessage
        )
        {
            _bcpInterface = bcpInterface;
            _bcpInterface.ConnectionStateChanged += (sender, args) =>
                CanSendMonitoringRequests = false;
            _bcpInterface.ResetCompleted += (sender, args) => CanSendMonitoringRequests = true;
            _createStartListeningMessage = createStartListeningMessage;
            _createStopListeningMessage = createStopListeningMessage;
        }

        public void AddListener(object listener, TEvent @event)
        {
            if (_listeners.TryAdd(@event, new HashSet<object> { listener }))
            {
                if (CanSendMonitoringRequests)
                    SendStartMonitoringRequest(@event);
            }
            else if (!_listeners[@event].Add(listener))
                Debug.LogError(
                    $"[EventPool] Cannot add listener '{listener}' to event '{@event}' because it "
                        + "was already added."
                );
        }

        public void RemoveListener(object listener, TEvent @event)
        {
            if (
                _listeners.TryGetValue(@event, out var listenersForThisEvent)
                && listenersForThisEvent.Remove(listener)
            )
            {
                if (listenersForThisEvent.Count == 0)
                {
                    _listeners.Remove(@event);
                    SendStopMonitoringRequest(@event);
                }
            }
            else
                Debug.LogError(
                    $"[EventPool] Cannot remove listener '{listener}' from event '{@event}' "
                        + "because it is not a listener."
                );
        }

        private void SendStartMonitoringRequest(TEvent @event)
        {
            var startListeningMsg = _createStartListeningMessage(@event);
            _bcpInterface.EnqueueMessage(startListeningMsg);
        }

        private void SendStopMonitoringRequest(TEvent @event)
        {
            var stopListeningMsg = _createStopListeningMessage(@event);
            _bcpInterface.EnqueueMessage(stopListeningMsg);
        }
    }
}
