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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Grpc.Core;
using NLog.Filters;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using VisualPinball.Unity;

namespace VisualPinball.Engine.Mpf.Unity.Editor
{
    [CustomEditor(typeof(MpfGamelogicEngine))]
    public class MpfGamelogicEngineInspector : UnityEditor.Editor
    {
        [SerializeField]
        private VisualTreeAsset _inspectorXml;

        private CancellationTokenSource _getMachineDescCts;

        public override VisualElement CreateInspectorGUI()
        {
            var root = _inspectorXml.Instantiate();
            var mpfEngine = (MpfGamelogicEngine)serializedObject.targetObject;
            var tableComponent = mpfEngine.GetComponentInParent<TableComponent>();

            var machineFolderField = root.Q<TextField>("machine-folder");
            var machineFolderInput = machineFolderField.Q(name: "unity-text-input");
            machineFolderInput.RegisterCallback<ClickEvent>(
                (evt) =>
                {
                    if (!Directory.Exists(Application.streamingAssetsPath))
                    {
                        Directory.CreateDirectory(Application.streamingAssetsPath);
                    }
                    var path = EditorUtility.OpenFolderPanel(
                        "Mission Pinball Framework: Choose machine folder",
                        Application.streamingAssetsPath,
                        ""
                    );
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        path = path.Replace("\\", "/");
                        if (path.Contains("StreamingAssets/"))
                            path = "./StreamingAssets/" + path.Split("StreamingAssets/")[1];

                        var machineFolderProp = serializedObject.FindProperty(
                            $"_mpfStarter.{nameof(MpfStarter._machineFolder)}"
                        );
                        machineFolderProp.stringValue = path;
                        serializedObject.ApplyModifiedProperties();
                    }
                }
            );

            var getDescBtn = root.Q<Button>("get-machine-description");
            if (Application.isPlaying)
                getDescBtn.SetEnabled(false);

            var getDescBtnDefaultText = getDescBtn.text;

            getDescBtn.clicked += async () =>
            {
                Undo.RecordObject(mpfEngine, "Get machine description");
                PrefabUtility.RecordPrefabInstancePropertyModifications(mpfEngine);
                if (_getMachineDescCts == null)
                {
                    _getMachineDescCts = new CancellationTokenSource();
                    getDescBtn.text = "Cancel";

                    try
                    {
                        await mpfEngine.QueryParseAndStoreMpfMachineDescription(
                            _getMachineDescCts.Token
                        );
                    }
                    catch (Exception ex)
                        when (ex is OperationCanceledException
                            || (
                                ex is RpcException exception
                                && exception.StatusCode == StatusCode.Cancelled
                            )
                        ) { }

                    getDescBtn.text = getDescBtnDefaultText;
                    _getMachineDescCts?.Dispose();
                    _getMachineDescCts = null;
                }
                else
                {
                    _getMachineDescCts?.Cancel();
                    _getMachineDescCts?.Dispose();
                    _getMachineDescCts = null;
                    getDescBtn.text = getDescBtnDefaultText;
                }
            };

            var repopulateHardwareBtn = root.Q<Button>("populate-hardware");
            if (Application.isPlaying)
                repopulateHardwareBtn.SetEnabled(false);

            repopulateHardwareBtn.clicked += () =>
            {
                if (
                    EditorUtility.DisplayDialog(
                        "Mission Pinball Framework",
                        "This will clear all linked switches, coils and lamps and re-populate them. Are you sure you want to do that?",
                        "Yes",
                        "No"
                    )
                )
                {
                    Undo.RecordObject(tableComponent, "Populate hardware");
                    PrefabUtility.RecordPrefabInstancePropertyModifications(tableComponent);
                    tableComponent.RepopulateHardware(mpfEngine);
                    TableSelector.Instance.TableUpdated();
                    SceneView.RepaintAll();
                }
            };

            var switchesProp = serializedObject.FindProperty(
                nameof(MpfGamelogicEngine._requestedSwitches)
            );
            var switchFoldout = root.Q<Foldout>("switches");
            UpdateSwitchList(mpfEngine, switchFoldout);
            switchFoldout.TrackPropertyValue(
                switchesProp,
                (prop) => UpdateSwitchList(mpfEngine, switchFoldout)
            );

            var coilsProp = serializedObject.FindProperty(
                nameof(MpfGamelogicEngine._requestedCoils)
            );
            var coilFoldout = root.Q<Foldout>("coils");
            UpdateCoilList(mpfEngine, coilFoldout);
            coilFoldout.TrackPropertyValue(
                coilsProp,
                (prop) => UpdateCoilList(mpfEngine, coilFoldout)
            );

            var lampsProp = serializedObject.FindProperty(
                nameof(MpfGamelogicEngine._requestedLamps)
            );
            var lampsFoldout = root.Q<Foldout>("lamps");
            UpdateLampList(mpfEngine, lampsFoldout);
            lampsFoldout.TrackPropertyValue(
                lampsProp,
                (prop) =>
                {
                    IEnumerable<string> ids = mpfEngine.RequestedLamps.Select(lamp => lamp.Id);
                    UpdateGameItemList(lampsFoldout, ids);
                }
            );

            return root;
        }

        private void OnDisable()
        {
            _getMachineDescCts?.Cancel();
            _getMachineDescCts?.Dispose();
            _getMachineDescCts = null;
        }

        private void UpdateSwitchList(MpfGamelogicEngine mpfEngine, VisualElement parent)
        {
            IEnumerable<string> ids = mpfEngine.RequestedSwitches.Select(sw => sw.Id);
            UpdateGameItemList(parent, ids);
        }

        private void UpdateCoilList(MpfGamelogicEngine mpfEngine, VisualElement parent)
        {
            IEnumerable<string> ids = mpfEngine.RequestedCoils.Select(coil => coil.Id);
            UpdateGameItemList(parent, ids);
        }

        private void UpdateLampList(MpfGamelogicEngine mpfEngine, VisualElement parent)
        {
            IEnumerable<string> ids = mpfEngine.RequestedLamps.Select(lamp => lamp.Id);
            UpdateGameItemList(parent, ids);
        }

        private void UpdateGameItemList(VisualElement parent, IEnumerable<string> itemIds)
        {
            parent.Clear();
            foreach (var id in itemIds)
            {
                var label = new Label(id);
                parent.Add(label);
            }
        }
    }
}
