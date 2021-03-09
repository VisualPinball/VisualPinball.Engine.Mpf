// Visual Pinball Engine
// Copyright (C) 2021 freezy and VPE Team
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

namespace VisualPinball.Engine.Mpf.Unity.Editor
{
	[CustomEditor(typeof(MpfGamelogicEngine))]
	public class MpfGamelogicEngineInspector : UnityEditor.Editor
	{
		private MpfGamelogicEngine _mpfEngine;

		private void OnEnable()
		{
			_mpfEngine = target as MpfGamelogicEngine;
		}

		public override void OnInspectorGUI()
		{
			var pos = EditorGUILayout.GetControlRect(true, 18f);
			pos = EditorGUI.PrefixLabel(pos, new GUIContent("Machine Folder"));

			if (GUI.Button(pos, _mpfEngine.MachineFolder, EditorStyles.objectField)) {
				var path = EditorUtility.OpenFolderPanel("Mission Pinball Framework: Choose machine folder", _mpfEngine.MachineFolder, "");
				if (!string.IsNullOrWhiteSpace(path)) {
					_mpfEngine.MachineFolder = path;
				}
			}

			if (GUILayout.Button("Synchronize")) {
				_mpfEngine.GetMachineDescription();
			}


			// if (GUILayout.Button("StartGame")) {
			// 	_mpfEngine.Client.Play();
			// }
			// if (GUILayout.Button("GetMachineDescription")) {
			// 	Debug.Log(_mpfEngine.Client.GetMachineDescription());
			// }
		}
	}
}
