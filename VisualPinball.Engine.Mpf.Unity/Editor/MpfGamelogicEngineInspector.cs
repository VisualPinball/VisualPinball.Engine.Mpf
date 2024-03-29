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
using VisualPinball.Unity;
using VisualPinball.Unity.Editor;

namespace VisualPinball.Engine.Mpf.Unity.Editor
{
	[CustomEditor(typeof(MpfGamelogicEngine))]
	public class MpfGamelogicEngineInspector : UnityEditor.Editor
	{
		private MpfGamelogicEngine _mpfEngine;
		private TableComponent _tableComponent;

		private bool _foldoutSwitches;
		private bool _foldoutCoils;
		private bool _foldoutLamps;

		private bool HasData => _mpfEngine.RequestedSwitches.Length + _mpfEngine.RequestedCoils.Length + _mpfEngine.RequestedLamps.Length > 0;

		private void OnEnable()
		{
			_mpfEngine = target as MpfGamelogicEngine;
			if (_mpfEngine != null) {
				_tableComponent = _mpfEngine.gameObject.GetComponentInParent<TableComponent>();
			}
		}

		public override void OnInspectorGUI()
		{
			if (!_tableComponent) {
				EditorGUILayout.HelpBox($"Cannot find table. The gamelogic engine must be applied to a table object or one of its children.", MessageType.Error);
				return;
			}

			var pos = EditorGUILayout.GetControlRect(true, 18f);
			pos = EditorGUI.PrefixLabel(pos, new GUIContent("Machine Folder"));

			if (GUI.Button(pos, _mpfEngine.machineFolder, EditorStyles.objectField)) {
				var path = EditorUtility.OpenFolderPanel("Mission Pinball Framework: Choose machine folder", _mpfEngine.machineFolder, "");
				if (!string.IsNullOrWhiteSpace(path)) {
					_mpfEngine.machineFolder = path;
				}
			}

			if (GUILayout.Button("Get Machine Description")) {
				if (!Directory.Exists(_mpfEngine.machineFolder)) {
					EditorUtility.DisplayDialog("Mission Pinball Framework", "Gotta choose a valid machine folder first!", "Okay");
				} else if (!Directory.Exists(Path.Combine(_mpfEngine.machineFolder, "config"))) {
					EditorUtility.DisplayDialog("Mission Pinball Framework", $"{_mpfEngine.machineFolder} doesn't seem a valid machine folder. We expect a \"config\" subfolder in there!", "Okay");
				} else {
					_mpfEngine.GetMachineDescription();
				}
			}

			EditorGUI.BeginDisabledGroup(!HasData);
			if (GUILayout.Button("Populate Hardware")) {
				if (EditorUtility.DisplayDialog("Mission Pinball Framework", "This will clear all linked switches, coils and lamps and re-populate them. You sure you want to do that?", "Yes", "No")) {
					_tableComponent.RepopulateHardware(_mpfEngine);
					TableSelector.Instance.TableUpdated();
					SceneView.RepaintAll();
				}
			}
			EditorGUI.EndDisabledGroup();


			var naStyle = new GUIStyle(GUI.skin.label) {
				fontStyle = FontStyle.Italic
			};

			// list switches, coils and lamps
			if (_mpfEngine.RequestedCoils.Length + _mpfEngine.RequestedSwitches.Length + _mpfEngine.RequestedLamps.Length > 0) {
				if (_foldoutSwitches = EditorGUILayout.BeginFoldoutHeaderGroup(_foldoutSwitches, "Switches")) {
					foreach (var sw in _mpfEngine.RequestedSwitches) {
						EditorGUILayout.LabelField(new GUIContent($"  {sw.Id} ", Icons.Switch(sw.NormallyClosed, IconSize.Small)));
					}
					if (_mpfEngine.RequestedSwitches.Length == 0) {
						EditorGUILayout.LabelField("No switches in this machine.", naStyle);
					}
				}
				EditorGUILayout.EndFoldoutHeaderGroup();

				if (_foldoutCoils = EditorGUILayout.BeginFoldoutHeaderGroup(_foldoutCoils, "Coils")) {
					foreach (var sw in _mpfEngine.RequestedCoils) {
						EditorGUILayout.LabelField(new GUIContent($"  {sw.Id} ", Icons.Coil(IconSize.Small)));
					}
					if (_mpfEngine.RequestedCoils.Length == 0) {
						EditorGUILayout.LabelField("No coils in this machine.", naStyle);
					}
				}
				EditorGUILayout.EndFoldoutHeaderGroup();

				if (_foldoutLamps = EditorGUILayout.BeginFoldoutHeaderGroup(_foldoutLamps, "Lamps")) {
					foreach (var sw in _mpfEngine.RequestedLamps) {
						EditorGUILayout.LabelField(new GUIContent($"  {sw.Id} ", Icons.Light(IconSize.Small)));
					}
					if (_mpfEngine.RequestedLamps.Length == 0) {
						EditorGUILayout.LabelField("No lamps in this machine.", naStyle);
					}
				}
				EditorGUILayout.EndFoldoutHeaderGroup();
			}
		}
	}
}
