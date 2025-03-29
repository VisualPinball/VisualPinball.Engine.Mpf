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

namespace VisualPinball.Engine.Mpf.Unity.MediaController.Sound
{
    [AddComponentMenu("Visual Pinball/Sound/Mpf Event Sound")]
    public class MpfEventSound : EventSoundComponent<MpfEventListener, EventArgs>
    {
        [SerializeField]
        private string _eventName;

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
            if (MpfGamelogicEngine.TryGetBcpInterface(this, out var bcpInterface))
            {
                eventSource = new MpfEventListener(bcpInterface, _eventName);
                return true;
            }

            eventSource = null;
            return false;
        }
    }
}
