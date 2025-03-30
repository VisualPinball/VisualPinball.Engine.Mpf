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
using Logger = NLog.Logger;

namespace VisualPinball.Engine.Mpf.Unity.MediaController.Sound
{
    [AddComponentMenu("Pinball/Sound/MPF Mode Sound")]
    public class ModeSound : BinaryEventSoundComponent<ModeMonitor, bool>
    {
        [SerializeField] private string _modeName;

        private static Logger Logger = NLog.LogManager.GetCurrentClassLogger();

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
            eventSource = null;
            if (string.IsNullOrWhiteSpace(_modeName))
            {
                Logger.Warn("No mode name is specified. The component 'MPF Mode Sound' on game object "
                            + $"'{gameObject.name}' will not do anything.");
                return false;
            }

            if (!MpfGamelogicEngine.TryGetBcpInterface(this, out var bcpInterface))
                return false;
            eventSource = new ModeMonitor(bcpInterface, _modeName);
            return true;
        }
    }
}