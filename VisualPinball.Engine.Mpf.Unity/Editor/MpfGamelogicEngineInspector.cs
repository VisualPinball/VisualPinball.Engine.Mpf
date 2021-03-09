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

// ReSharper disable AssignmentInConditionalExpression

using System.IO;
using UnityEditor;
using UnityEngine;
using VisualPinball.Unity.Editor;

namespace VisualPinball.Engine.Mpf.Unity.Editor
{
	[CustomEditor(typeof(MpfGamelogicEngine))]
	public class MpfGamelogicEngineInspector : UnityEditor.Editor
	{
		private MpfGamelogicEngine _mpfEngine;
		private bool _foldoutSwitches;
		private bool _foldoutCoils;
		private bool _foldoutLamps;

		private void OnEnable()
		{
			_mpfEngine = target as MpfGamelogicEngine;
		}

		public override void OnInspectorGUI()
		{
			var pos = EditorGUILayout.GetControlRect(true, 18f);
			pos = EditorGUI.PrefixLabel(pos, new GUIContent("Machine Folder"));

			if (GUI.Button(pos, _mpfEngine.machineFolder, EditorStyles.objectField)) {
				var path = EditorUtility.OpenFolderPanel("Mission Pinball Framework: Choose machine folder", _mpfEngine.machineFolder, "");
				if (!string.IsNullOrWhiteSpace(path)) {
					_mpfEngine.machineFolder = path;
				}
			}

			if (GUILayout.Button("Synchronize")) {
				if (!Directory.Exists(_mpfEngine.machineFolder)) {
					EditorUtility.DisplayDialog("Mission Pinball Framework", "Gotta choose a valid machine folder first!", "Okay");
				} else if (!Directory.Exists(Path.Combine(_mpfEngine.machineFolder, "config"))) {
					EditorUtility.DisplayDialog("Mission Pinball Framework", $"{_mpfEngine.machineFolder} doesn't seem a valid machine folder. We expect a \"config\" subfolder in there!", "Okay");
				} else {
					_mpfEngine.GetMachineDescription();
				}
			}

			var naStyle = new GUIStyle(GUI.skin.label) {
				fontStyle = FontStyle.Italic
			};

			// list switches, coils and lamps
			if (_mpfEngine.AvailableCoils.Length + _mpfEngine.AvailableSwitches.Length + _mpfEngine.AvailableLamps.Length > 0) {
				if (_foldoutSwitches = EditorGUILayout.BeginFoldoutHeaderGroup(_foldoutSwitches, "Switches")) {
					foreach (var sw in _mpfEngine.AvailableSwitches) {
						EditorGUILayout.LabelField(new GUIContent($"  [{sw.InternalId}] {sw.Id} ", Icons.Switch(sw.NormallyClosed, IconSize.Small)));
					}
					if (_mpfEngine.AvailableSwitches.Length == 0) {
						EditorGUILayout.LabelField("No switches in this machine.", naStyle);
					}
				}
				EditorGUILayout.EndFoldoutHeaderGroup();

				if (_foldoutCoils = EditorGUILayout.BeginFoldoutHeaderGroup(_foldoutCoils, "Coils")) {
					foreach (var sw in _mpfEngine.AvailableCoils) {
						EditorGUILayout.LabelField(new GUIContent($"  [{sw.InternalId}] {sw.Id} ", Icons.Coil(IconSize.Small)));
					}
					if (_mpfEngine.AvailableCoils.Length == 0) {
						EditorGUILayout.LabelField("No coils in this machine.", naStyle);
					}
				}
				EditorGUILayout.EndFoldoutHeaderGroup();

				if (_foldoutLamps = EditorGUILayout.BeginFoldoutHeaderGroup(_foldoutLamps, "Lamps")) {
					foreach (var sw in _mpfEngine.AvailableLamps) {
						EditorGUILayout.LabelField(new GUIContent($"  [{sw.InternalId}] {sw.Id} ", Icons.Light(IconSize.Small)));
					}
					if (_mpfEngine.AvailableLamps.Length == 0) {
						EditorGUILayout.LabelField("No lamps in this machine.", naStyle);
					}
				}
				EditorGUILayout.EndFoldoutHeaderGroup();
			}
		}
	}
}
