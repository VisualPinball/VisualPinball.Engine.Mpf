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
using UnityEngine;
using UnityEngine.UIElements;

namespace VisualPinball.Engine.Mpf.Unity.Editor
{
    public class DisabledParentObjectHelpBox : VisualElement
    {
        private readonly HelpBox _box;
        private readonly UnityEditor.Editor _editor;

        public DisabledParentObjectHelpBox(UnityEditor.Editor editor)
        {
            _editor = editor;
            _box = new HelpBox(
                "The game object this component is attached to or one of its parents is disabled. This component" +
                " cannot initialize until its parent object becomes active in the hierarchy. It is possible that" +
                " events from MPF will be missed.",
                HelpBoxMessageType.Warning);
            Add(_box);

            RegisterCallback<AttachToPanelEvent>(evt =>
            {
                UpdateHelpBoxVisibility();
                EditorApplication.hierarchyChanged += OnHierarchyChanged;
            });

            RegisterCallback<DetachFromPanelEvent>(evt =>
            {
                EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            });
        }

        private void OnHierarchyChanged() => UpdateHelpBoxVisibility();

        private void UpdateHelpBoxVisibility()
        {
            _box.style.display = _editor.targets.ToList().Any(IsParentObjectDisabled)
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        private static bool IsParentObjectDisabled(UnityEngine.Object target) =>
            !((Component)target).gameObject.activeInHierarchy;
    }
}