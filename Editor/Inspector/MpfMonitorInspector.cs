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

using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using VisualPinball.Engine.Mpf.Unity.MediaController.Text;

namespace VisualPinball.Engine.Mpf.Unity.Editor
{
    [
        CustomEditor(typeof(MonitoredVariableTextBase), editorForChildClasses: true),
        CanEditMultipleObjects
    ]
    public class MpfMonitorInspector : UnityEditor.Editor
    {
        private HelpBox _missingGleHelpBox;
        private HelpBox _misconfiguredGleHelpBox;

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            _missingGleHelpBox = new HelpBox(
                "This component must be on a game object that is underneath an "
                    + $"'{nameof(MpfGamelogicEngine)}' component in the scene hierarchy.",
                HelpBoxMessageType.Error
            );
            root.Add(_missingGleHelpBox);

            _misconfiguredGleHelpBox = new HelpBox(
                "The MPF game logic engine is not configured to use the included media "
                    + $"controller. Set 'Media Controller' to '{MpfMediaController.Included}' "
                    + "in the game logic engine inspector.",
                HelpBoxMessageType.Error
            );
            root.Add(_misconfiguredGleHelpBox);

            UpdateErrorBoxVisibility();

            EditorApplication.hierarchyChanged += OnHierarchyChanged;

            InspectorElement.FillDefaultInspector(root, serializedObject, this);
            return root;
        }

        private void OnHierarchyChanged() => UpdateErrorBoxVisibility();

        private void UpdateErrorBoxVisibility()
        {
            if (targets.ToList().Any(IsGleMissing))
                _missingGleHelpBox.style.display = DisplayStyle.Flex;
            else
                _missingGleHelpBox.style.display = DisplayStyle.None;
            if (targets.ToList().Any(IsGleMisconfigured))
                _misconfiguredGleHelpBox.style.display = DisplayStyle.Flex;
            else
                _misconfiguredGleHelpBox.style.display = DisplayStyle.None;
        }

        private void OnDisable()
        {
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
        }

        private MpfGamelogicEngine GetParentGle(UnityEngine.Object target)
        {
            return ((Component)target).GetComponentInParent<MpfGamelogicEngine>();
        }

        private bool IsGleMissing(UnityEngine.Object target) => GetParentGle(target) == null;

        private bool IsGleMisconfigured(UnityEngine.Object target)
        {
            return !IsGleMissing(target)
                && GetParentGle(target).MediaControllerSetting != MpfMediaController.Included;
        }
    }
}
