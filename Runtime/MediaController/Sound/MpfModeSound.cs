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

using UnityEngine;
using VisualPinball.Engine.Mpf.Unity.MediaController.Messages.Mode;
using VisualPinball.Unity;

namespace VisualPinball.Engine.Mpf.Unity.MediaController.Sound
{
    [AddComponentMenu("Visual Pinball/Sound/Mpf Mode Sound")]
    public class MpfModeSound : BinaryEventSoundComponent<ModeMonitor, bool>
    {
        [SerializeField]
        private string _modeName;

        protected override bool InterpretAsBinary(bool eventArgs) => eventArgs; // Big brain time

        protected override void Subscribe(ModeMonitor eventSource)
        {
            eventSource.IsModeActiveChanged += OnEvent;
        }

        protected override void Unsubscribe(ModeMonitor eventSource)
        {
            eventSource.IsModeActiveChanged -= OnEvent;
        }

        protected override bool TryFindEventSource(out ModeMonitor eventSource)
        {
            if (MpfGamelogicEngine.TryGetBcpInterface(this, out var bcpInterface))
            {
                eventSource = new ModeMonitor(bcpInterface, _modeName);
                return true;
            }

            eventSource = null;
            return false;
        }
    }
}
