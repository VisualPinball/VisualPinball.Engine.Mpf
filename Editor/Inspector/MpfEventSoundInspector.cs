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

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using VisualPinball.Engine.Mpf.Unity.MediaController.Sound;
using VisualPinball.Unity.Editor;

namespace VisualPinball.Engine.Mpf.Unity.Editor
{
    [CustomEditor(typeof(MpfEventSound)), CanEditMultipleObjects]
    public class MpfEventSoundInspector : SoundComponentInspector
    {
        [SerializeField]
        private VisualTreeAsset mpfEventSoundInspectorXml;

        public override VisualElement CreateInspectorGUI()
        {
            var root = base.CreateInspectorGUI();
            var inspectorUi = mpfEventSoundInspectorXml.Instantiate();
            root.Add(inspectorUi);
            return root;
        }
    }
}
