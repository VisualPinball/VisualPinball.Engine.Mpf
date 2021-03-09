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

using System.IO;
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
				if (!Directory.Exists(_mpfEngine.MachineFolder)) {
					EditorUtility.DisplayDialog("Mission Pinball Framework", "Gotta choose a valid machine folder first!", "Okay");
				} else if (!Directory.Exists(Path.Combine(_mpfEngine.MachineFolder, "config"))) {
					EditorUtility.DisplayDialog("Mission Pinball Framework", $"{_mpfEngine.MachineFolder} doesn't seem a valid machine folder. We expect a \"config\" subfolder in there!", "Okay");
				} else {
					_mpfEngine.GetMachineDescription();
				}
			}

			EditorGUILayout.LabelField("Switches", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
			foreach (var switchDescription in _mpfEngine.MachineDescription.Switches) {
				EditorGUILayout.LabelField(switchDescription.HardwareNumber, switchDescription.Name);
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
