using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace VisualPinball.Engine.Mpf.Unity.Editor
{
    public class MissingGleHelpBoxes : VisualElement
    {
        private readonly HelpBox _missingGleHelpBox;
        private readonly HelpBox _misconfiguredGleHelpBox;
        private readonly UnityEditor.Editor _editor;

        public MissingGleHelpBoxes(UnityEditor.Editor editor)
        {
            _editor = editor;
            _missingGleHelpBox = new HelpBox(
                "This component must be on a game object that is underneath an "
                + "'MPF Game Logic' component in the scene hierarchy.",
                HelpBoxMessageType.Error
            );
            Add(_missingGleHelpBox);

            _misconfiguredGleHelpBox = new HelpBox(
                "The MPF game logic engine is not configured to use the included media "
                + $"controller. Set 'Media Controller' to '{MpfMediaController.Included}' "
                + "in the game logic engine inspector.",
                HelpBoxMessageType.Error
            );
            Add(_misconfiguredGleHelpBox);

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
            if (_editor.targets.ToList().Any(IsGleMissing))
                _missingGleHelpBox.style.display = DisplayStyle.Flex;
            else
                _missingGleHelpBox.style.display = DisplayStyle.None;
            if (_editor.targets.ToList().Any(IsGleMisconfigured))
                _misconfiguredGleHelpBox.style.display = DisplayStyle.Flex;
            else
                _misconfiguredGleHelpBox.style.display = DisplayStyle.None;
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