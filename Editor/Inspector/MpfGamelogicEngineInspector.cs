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

// ReSharper disable AssignmentInConditionalExpression

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Grpc.Core;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using VisualPinball.Engine.Mpf.Unity.MediaController;
using VisualPinball.Unity;

namespace VisualPinball.Engine.Mpf.Unity.Editor
{
    [CustomEditor(typeof(MpfGamelogicEngine))]
    public class MpfGamelogicEngineInspector : UnityEditor.Editor
    {
        [SerializeField]
        private VisualTreeAsset _inspectorXml;

        private CancellationTokenSource _getMachineDescCts;
        private MpfGamelogicEngine _mpfEngine;
        private PropertyField _connectTimeoutField;
        private PropertyField _connectDelayField;
        private VisualElement _commandLineOptionsContainer;
        private VisualElement _startupBehaviorOptionsContainer;
        private TextField _mpfStateField;
        private TextField _mediaControllerStateField;
        private VisualElement _bcpOptionsContainer;

        public override VisualElement CreateInspectorGUI()
        {
            var root = _inspectorXml.Instantiate();
            _mpfEngine = (MpfGamelogicEngine)serializedObject.targetObject;
            var tableComponent = _mpfEngine.GetComponentInParent<TableComponent>();

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
                        path = MpfWranglerOptions.RealPathToSerializedPath(path);
                        var machineFolderProp = serializedObject.FindProperty(
                            $"_wranglerOptions._machineFolder"
                        );
                        machineFolderProp.stringValue = path;
                        serializedObject.ApplyModifiedProperties();
                    }
                }
            );

            var getDescBtn = root.Q<Button>("get-machine-description");
            var optionsBox = root.Q<VisualElement>("options");
            if (Application.isPlaying)
                getDescBtn.SetEnabled(false);

            var getDescBtnDefaultText = getDescBtn.text;

            getDescBtn.clicked += async () =>
            {
                if (_getMachineDescCts == null)
                {
                    Undo.RecordObject(_mpfEngine, "Get machine description");
                    PrefabUtility.RecordPrefabInstancePropertyModifications(_mpfEngine);
                    getDescBtn.text = "Cancel";
                    optionsBox.SetEnabled(false);
                    _getMachineDescCts = new CancellationTokenSource();

                    try
                    {
                        await _mpfEngine.QueryParseAndStoreMpfMachineDescription(
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
                    finally
                    {
                        optionsBox.SetEnabled(true);
                        getDescBtn.text = getDescBtnDefaultText;
                        getDescBtn.SetEnabled(true);
                        _getMachineDescCts?.Dispose();
                        _getMachineDescCts = null;
                    }
                }
                else
                {
                    getDescBtn.SetEnabled(false);
                    _getMachineDescCts?.Cancel();
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
                        "This will clear all linked switches, coils and lamps and re-populate "
                            + "them. Are you sure you want to do that?",
                        "Yes",
                        "No"
                    )
                )
                {
                    Undo.RecordObject(tableComponent, "Populate hardware");
                    PrefabUtility.RecordPrefabInstancePropertyModifications(tableComponent);
                    tableComponent.RepopulateHardware(_mpfEngine);
                    TableSelector.Instance.TableUpdated();
                    SceneView.RepaintAll();
                }
            };

            var switchesProp = serializedObject.FindProperty("_requestedSwitches");
            var switchFoldout = root.Q<Foldout>("switches");
            UpdateSwitchList(_mpfEngine, switchFoldout);
            switchFoldout.TrackPropertyValue(
                switchesProp,
                (prop) => UpdateSwitchList(_mpfEngine, switchFoldout)
            );

            var coilsProp = serializedObject.FindProperty("_requestedCoils");
            var coilFoldout = root.Q<Foldout>("coils");
            UpdateCoilList(_mpfEngine, coilFoldout);
            coilFoldout.TrackPropertyValue(
                coilsProp,
                (prop) => UpdateCoilList(_mpfEngine, coilFoldout)
            );

            var lampsProp = serializedObject.FindProperty("_requestedLamps");
            var lampsFoldout = root.Q<Foldout>("lamps");
            UpdateLampList(_mpfEngine, lampsFoldout);
            lampsFoldout.TrackPropertyValue(
                lampsProp,
                (prop) =>
                {
                    IEnumerable<string> ids = _mpfEngine.RequestedLamps.Select(lamp => lamp.Id);
                    UpdateGameItemList(lampsFoldout, ids);
                }
            );

            var startupBehaviorField = root.Q<PropertyField>("startup-behavior");
            var startupBehaviorProp = serializedObject.FindProperty(
                "_wranglerOptions._startupBehavior"
            );
            _connectTimeoutField = root.Q<PropertyField>("connect-timeout");
            _connectDelayField = root.Q<PropertyField>("connect-delay");
            OnStartupBehaviorChanged(startupBehaviorProp);
            startupBehaviorField.TrackPropertyValue(startupBehaviorProp, OnStartupBehaviorChanged);

            // Grey out command line options if VPE does not launch MPF itself
            _commandLineOptionsContainer = root.Q<VisualElement>("command-line-options");
            _startupBehaviorOptionsContainer = root.Q<VisualElement>("startup-behavior-options");
            var executableSourceProp = serializedObject.FindProperty(
                "_wranglerOptions._executableSource"
            );
            OnExecutableSourceChanged(executableSourceProp);
            _commandLineOptionsContainer.TrackPropertyValue(
                executableSourceProp,
                OnExecutableSourceChanged
            );

            MachineFolderValidationBoxes(machineFolderField);

            _mpfStateField = root.Q<TextField>("mpf-state");
            UpdateMpfStateField(_mpfEngine.MpfState);
            _mpfEngine.MpfStateChanged += OnMpfStateChanged;

            _mediaControllerStateField = root.Q<TextField>("media-controller-state");
            UpdateBcpStateField(_mpfEngine.BcpState);
            _mpfEngine.BcpStateChanged += OnBcpStateChanged;

            _bcpOptionsContainer = root.Q<VisualElement>("bcp-options");
            var mediaControllerProp = serializedObject.FindProperty(
                "_wranglerOptions._mediaController"
            );
            UpdateMediaControllerUiVisibility(mediaControllerProp);
            _bcpOptionsContainer.TrackPropertyValue(
                mediaControllerProp,
                UpdateMediaControllerUiVisibility
            );

            return root;
        }

        private void OnDisable()
        {
            _mpfEngine.MpfStateChanged -= OnMpfStateChanged;
            _mpfEngine.BcpStateChanged -= OnBcpStateChanged;
            _getMachineDescCts?.Cancel();
            _getMachineDescCts?.Dispose();
            _getMachineDescCts = null;
        }

        private void OnStartupBehaviorChanged(SerializedProperty startupBehaviorProp)
        {
            switch ((MpfStartupBehavior)startupBehaviorProp.intValue)
            {
                case MpfStartupBehavior.PingUntilReady:
                    _connectTimeoutField.style.display = DisplayStyle.Flex;
                    _connectDelayField.style.display = DisplayStyle.None;
                    break;
                case MpfStartupBehavior.DelayConnection:
                    _connectTimeoutField.style.display = DisplayStyle.None;
                    _connectDelayField.style.display = DisplayStyle.Flex;
                    break;
            }
        }

        private void OnExecutableSourceChanged(SerializedProperty executableSourceProp)
        {
            var source = (MpfExecutableSource)executableSourceProp.intValue;
            bool willLaunchMpf = source != MpfExecutableSource.AssumeRunning;
            _commandLineOptionsContainer.SetEnabled(willLaunchMpf);
            _startupBehaviorOptionsContainer.SetEnabled(willLaunchMpf);
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

        private void OnMpfStateChanged(object sender, StateChangedEventArgs<MpfState> args)
        {
            UpdateMpfStateField(args.CurrentState);
        }

        private void UpdateMpfStateField(MpfState state)
        {
            _mpfStateField.value = state.ToString();
        }

        private void OnBcpStateChanged(
            object sender,
            StateChangedEventArgs<BcpConnectionState> args
        )
        {
            UpdateBcpStateField(args.CurrentState);
        }

        private void UpdateBcpStateField(BcpConnectionState state)
        {
            _mediaControllerStateField.value = state.ToString();
        }

        private void UpdateMediaControllerUiVisibility(SerializedProperty mediaControllerProp)
        {
            var mediaController = (MpfMediaController)mediaControllerProp.enumValueIndex;
            var usingIncludedMediaController = mediaController == MpfMediaController.Included;
            _mediaControllerStateField.SetEnabled(usingIncludedMediaController);
            _bcpOptionsContainer.SetEnabled(usingIncludedMediaController);
        }

        private void MachineFolderValidationBoxes(VisualElement machineFolderField)
        {
            var machineFolderProp = serializedObject.FindProperty(
                "_wranglerOptions._machineFolder"
            );
            var notAMachineFolderErrorBox = new HelpBox(
                "The machine folder is not valid. It must contain a folder called 'config' "
                    + "with at least one .yaml file inside.",
                HelpBoxMessageType.Error
            );
            var streamingAssetsWarnBox = new HelpBox(
                "The machine folder is not located in the 'StreamingAssets' folder. It will not be"
                    + " included in builds.",
                HelpBoxMessageType.Warning
            );
            streamingAssetsWarnBox.TrackPropertyValue(machineFolderProp, UpdateVisibility);
            var container = machineFolderField.parent;
            var index = container.IndexOf(machineFolderField);
            container.Insert(index, notAMachineFolderErrorBox);
            container.Insert(index + 1, streamingAssetsWarnBox);
            UpdateVisibility(machineFolderProp);

            void UpdateVisibility(SerializedProperty _)
            {
                var options = (MpfWranglerOptions)
                    serializedObject.FindProperty("_wranglerOptions").boxedValue;
                var machineFolder = options.MachineFolder;
                var isMachineFolderInStreamingAssets = machineFolder.StartsWith(
                    Application.streamingAssetsPath
                );
                streamingAssetsWarnBox.style.display = isMachineFolderInStreamingAssets
                    ? DisplayStyle.None
                    : DisplayStyle.Flex;

                var configDir = Path.Combine(machineFolder, "config");
                var isValidMachineFolder =
                    Directory.Exists(configDir) && Directory.GetFiles(configDir, "*.yaml").Any();
                notAMachineFolderErrorBox.style.display = isValidMachineFolder
                    ? DisplayStyle.None
                    : DisplayStyle.Flex;
            }
        }
    }
}
