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

namespace VisualPinball.Engine.Mpf.Unity.MediaController.ObjectToggle
{
    /// <summary>
    /// Enable the parent game object while the specified mode is active in MPF.
    /// </summary>
    [AddComponentMenu("Pinball/MPF Media Controller/Enable During MPF Mode")]
    [DisallowMultipleComponent]
    public class EnableDuringMode : MonoBehaviour
    {
        [SerializeField] private string _mode;

        private ModeMonitor _modeMonitor;

        private void Awake()
        {
            if (!MpfGamelogicEngine.TryGetBcpInterface(this, out var bcpInterface)) return;

            _modeMonitor = new ModeMonitor(bcpInterface, _mode);
            _modeMonitor.IsModeActiveChanged += OnModeActiveChanged;
        }

        private void Start()
        {
            // This is done in Start to give other components like this one attached to children of this game object a
            // chance to run their Awake functions.
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_modeMonitor != null)
            {
                _modeMonitor.IsModeActiveChanged -= OnModeActiveChanged;
                _modeMonitor.Dispose();
            }
        }

        private void OnModeActiveChanged(object sender, bool isActive)
        {
            gameObject.SetActive(isActive);
        }
    }
}