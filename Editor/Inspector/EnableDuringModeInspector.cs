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
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using VisualPinball.Engine.Mpf.Unity.MediaController.ObjectToggle;

namespace VisualPinball.Engine.Mpf.Unity.Editor
{
    [CustomEditor(typeof(EnableDuringMode)), CanEditMultipleObjects]
    public class EnableDuringModeInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            root.Add(new MissingGleHelpBoxes(this));
            root.Add(new DisabledParentObjectHelpBox(this));
            InspectorElement.FillDefaultInspector(root, serializedObject, this);
            return root;
        }
    }
}