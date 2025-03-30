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
using VisualPinball.Engine.Mpf.Unity.MediaController.Messages.Trigger;
using VisualPinball.Unity;
using Logger = NLog.Logger;

namespace VisualPinball.Engine.Mpf.Unity.MediaController.Sound
{
    [AddComponentMenu("Pinball/Sound/MPF Event Sound")]
    public class EventSound : EventSoundComponent<MpfEventListener, EventArgs>
    {
        [SerializeField] private string _eventName;

        private static Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        protected override void Subscribe(MpfEventListener eventSource)
        {
            eventSource.Triggered += OnEvent;
        }

        protected override void Unsubscribe(MpfEventListener eventSource)
        {
            eventSource.Triggered -= OnEvent;
        }

        protected override bool TryFindEventSource(out MpfEventListener eventSource)
        {
            eventSource = null;
            if (string.IsNullOrWhiteSpace(_eventName))
            {
                Logger.Warn("No event name is specified. The component 'MPF Event Sound' on game object "
                            + $"'{gameObject.name}' will not do anything.");
                return false;
            }

            if (!MpfGamelogicEngine.TryGetBcpInterface(this, out var bcpInterface))
                return false;
            eventSource = new MpfEventListener(bcpInterface, _eventName);
            return true;
        }
    }
}