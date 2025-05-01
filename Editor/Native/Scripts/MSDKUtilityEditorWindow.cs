// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.IO;
using System.Text;
using Meta.XR.Movement.Recording;
using Meta.XR.Movement.Retargeting;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static Meta.XR.Movement.Editor.MSDKUtilityEditorConfig;
using static Meta.XR.Movement.MSDKUtility;
using static Meta.XR.Movement.Retargeting.SkeletonData;

namespace Meta.XR.Movement.Editor
{
    /// <summary>
    /// Main utility editor window used for modifying character configs.
    /// </summary>
    [Serializable]
    public class MSDKUtilityEditorWindow : EditorWindow, ISupportsOverlays
    {
        // Public properties.
        /// <summary>
        /// Current editor step.
        /// </summary>
        public EditorStep Step => _utilityConfig.Step;
        /// <summary>
        /// Preview section.
        /// </summary>
        public MSDKUtilityEditorPreviewer Previewer => _previewer;
        /// <summary>
        /// Reads and deserializes file for playing back sequences.
        /// </summary>
        public SequenceFileReader FileReader => _fileReader;
        /// <summary>
        /// Config instance.
        /// </summary>
        public MSDKUtilityEditorConfig UtilityConfig => _utilityConfig;
        /// <summary>
        /// Visual overlay.
        /// </summary>
        public MSDKUtilityEditorOverlay Overlay => _overlay;
        /// <summary>
        /// Current preview pose.
        /// </summary>
        public string CurrentPreviewPose => _currentPreviewPose;
        /// <summary>
        /// Returns the selected joint.
        /// </summary>
        public string SelectedJointName => _target.JointNames[SelectedIndex];

        /// <summary>
        /// Returns the preview stage.
        /// </summary>
        public MSDKUtilityEditorStage PreviewStage
        {
            get => _previewStage;
            set => _previewStage = value;
        }

        /// <summary>
        /// Returns the target editor config.
        /// </summary>
        public MSDKUtilityEditorConfig TargetInfo
        {
            get => _target;
            set => _target = value;
        }

        /// <summary>
        /// Returns the source editor config.
        /// </summary>
        public MSDKUtilityEditorConfig SourceInfo
        {
            get => _source;
            set => _source = value;
        }

        /// <summary>
        /// Selected joint index.
        /// </summary>
        public int SelectedIndex
        {
            get
            {
                if (_target == null || _target.SkeletonJoints == null || _target.SkeletonJoints.Length == 0)
                {
                    return -1;
                }

                for (var i = 0; i < _target.SkeletonJoints.Length; i++)
                {
                    if (Selection.activeTransform == _target.SkeletonJoints[i])
                    {
                        return i;
                    }
                }

                return -1;
            }
        }

        /// <summary>
        /// Returns the selected joint mappings text.
        /// </summary>
        public string SelectedJointMappingsText
        {
            get
            {
                if (_target == null || SelectedIndex == -1 || _target.JointMappings.Length == 0)
                {
                    return null;
                }

                var selectedMappingIndex = 0;
                for (var i = 0; i < _target.JointMappings.Length; i++)
                {
                    if (_target.JointMappings[i].JointIndex == SelectedIndex)
                    {
                        selectedMappingIndex = i;
                    }
                }

                var allMappingsText = new StringBuilder();
                var mappingCount = _target.JointMappings[selectedMappingIndex].EntriesCount;
                var startIndex = 0;
                for (var i = 0; i < selectedMappingIndex; i++)
                {
                    startIndex += _target.JointMappings[i].EntriesCount;
                }

                for (var i = 0; i < mappingCount; i++)
                {
                    var entry = _target.JointMappingEntries[startIndex + i];
                    allMappingsText.AppendFormat(
                        "{0}. {1} \n- Rotation: {2} | Position: {3} \n",
                        i + 1,
                        _source.JointNames[entry.JointIndex],
                        entry.RotationWeight,
                        entry.PositionWeight);
                }

                return allMappingsText.ToString();
            }
        }

        // Serialized fields.
        [SerializeField]
        private UnityEditor.Editor _sourceEditor;

        [SerializeField]
        private UnityEditor.Editor _targetEditor;

        [SerializeField]
        private UnityEditor.Editor _currentTransformEditor;

        [SerializeField]
        private MSDKUtilityEditorPreviewer _previewer;

        [SerializeField]
        private MSDKUtilityEditorConfig _source;

        [SerializeField]
        private MSDKUtilityEditorConfig _target;

        [SerializeField]
        private MSDKUtilityEditorConfig _utilityConfig;

        [SerializeField]
        private MSDKUtilityEditorStage _previewStage;

        // Private fields.
        private string _currentProgressText => $"{(int)Step + 1} / {(int)EditorStep.End}";

        private ulong _configHandle
        {
            get => _target?.ConfigHandle ?? INVALID_HANDLE;
            set => _target.ConfigHandle = value;
        }

        private GameObject _sceneViewCharacter
        {
            get => _previewer?.SceneViewCharacter;
            set => _previewer.SceneViewCharacter = value;
        }

        private MSDKUtilityEditorMetadata _editorMetadataObject
        {
            get => _utilityConfig?.EditorMetadataObject;
            set => _utilityConfig.EditorMetadataObject = value;
        }

        private string _metadataAssetPath
        {
            get => _utilityConfig?.MetadataAssetPath;
            set => _utilityConfig.MetadataAssetPath = value;
        }

        private string _currentTitle
        {
            get
            {
                return Step switch
                {
                    EditorStep.Configuration => "Setup Configuration",
                    EditorStep.MinTPose => "Align Min T-Pose",
                    EditorStep.MaxTPose => "Align Max T-Pose",
                    EditorStep.Review => "Review & Export",
                    _ => ""
                };
            }
        }

        // Editor components.
        private MSDKUtilityEditorOverlay _overlay;
        private MSDKUtilityEditorPlaybackUI _playbackUI;
        private SequenceFileReader _fileReader;
        private string _currentPreviewPose;

        // Editor settings.
        private bool _debugging = false;
        private bool _currentTransformEditorFoldout = true;
        private bool _configBoneTransformsFoldout;
        private bool _validatedConfigFinish;
        private bool _displayedConfig;
        private bool _initialized;

        // Styling settings.
        private const int _buttonSizeHeight = 24;
        private const int _singleLineSpace = 8;
        private const int _doubleLineSpace = 16;
        private const int _headerSize = 18;
        private const int _buttonPadding = 4;

        private readonly FullBodyTrackingBoneId[] _leftStartBoneIds =
        {
            FullBodyTrackingBoneId.LeftHandThumbMetacarpal,
            FullBodyTrackingBoneId.LeftHandIndexProximal,
            FullBodyTrackingBoneId.LeftHandMiddleProximal,
            FullBodyTrackingBoneId.LeftHandRingProximal,
            FullBodyTrackingBoneId.LeftHandLittleProximal,
        };

        private readonly FullBodyTrackingBoneId[] _leftEndBoneIds =
        {
            FullBodyTrackingBoneId.LeftHandThumbDistal,
            FullBodyTrackingBoneId.LeftHandIndexDistal,
            FullBodyTrackingBoneId.LeftHandMiddleDistal,
            FullBodyTrackingBoneId.LeftHandRingDistal,
            FullBodyTrackingBoneId.LeftHandLittleDistal,
        };

        private readonly FullBodyTrackingBoneId[] _rightStartBoneIds =
        {
            FullBodyTrackingBoneId.RightHandThumbMetacarpal,
            FullBodyTrackingBoneId.RightHandIndexProximal,
            FullBodyTrackingBoneId.RightHandMiddleProximal,
            FullBodyTrackingBoneId.RightHandRingProximal,
            FullBodyTrackingBoneId.RightHandLittleProximal,
        };

        private readonly FullBodyTrackingBoneId[] _rightEndBoneIds =
        {
            FullBodyTrackingBoneId.RightHandThumbDistal,
            FullBodyTrackingBoneId.RightHandIndexDistal,
            FullBodyTrackingBoneId.RightHandMiddleDistal,
            FullBodyTrackingBoneId.RightHandRingDistal,
            FullBodyTrackingBoneId.RightHandLittleDistal,
        };

        /**********************************************************
         *
         *               Unity Functions
         *
         **********************************************************/

        /// <summary>
        /// Creates a docked inspector for the retargeting editor.
        /// </summary>
        /// <returns></returns>
        public static MSDKUtilityEditorWindow CreateDockedInspector()
        {
            // The last argument docks the custom inspector next to the inspector tab.
            var nativeUtilityEditorToolbar =
                GetWindow<MSDKUtilityEditorWindow>("Retargeting Editor",
                    false, Type.GetType("UnityEditor.InspectorWindow,UnityEditor.dll"));
            return nativeUtilityEditorToolbar;
        }

        /// <summary>
        /// Runs initialization.
        /// </summary>
        public void Init()
        {
            RemoveExistingOverlays();
            _overlay = new MSDKUtilityEditorOverlay(this);
            SceneView.AddOverlayToActiveView(_overlay);
            _source = CreateInstance<MSDKUtilityEditorConfig>();
            _target = CreateInstance<MSDKUtilityEditorConfig>();
            _previewer.InitializeSkeletonDraws();
            _initialized = true;
            CreateGUI();
        }

        /// <summary>
        /// Opens file for playback.
        /// </summary>
        /// <param name="poseName">Playback file name.</param>
        public void OpenPlaybackFile(string poseName)
        {
            _fileReader.Init(_source.ConfigHandle);
            if (_fileReader.IsPlaying)
            {
                _fileReader.ClosePlaybackFile();
            }

            _currentPreviewPose = poseName;
            // Obtain the path to the pose file. It lives in the package folder,
            // and that folder might live outside of the project.
            var fullPath = Path.GetFullPath(
                Path.Combine("Packages/com.meta.xr.sdk.movement/Editor/Native/Poses", poseName)
            );
            fullPath = Path.ChangeExtension(fullPath, ".sbn");
            _fileReader.PlayBackRecording(fullPath);

            // If there is no previewCharacter, we need to create one.
            if (_previewer.PreviewRetargeter == null)
            {
                _fileReader.PlayNextFrame();
                _previewer.InstantiatePreviewCharacterRetargeter();
            }
        }

        private void CreateGUI()
        {
            if (!_initialized || !EnsureMetadataLoaded())
            {
                return;
            }

            ValidateConfiguration();
            _previewer.AssociateSceneCharacter(_target);
            CreateRootGUI();
        }

        private void Update()
        {
            if (_fileReader is { IsPlaying: true })
            {
                _fileReader.PlayNextFrame();
            }

            _playbackUI?.Update();
        }

        private void OnDestroy()
        {
            foreach (var handle in _utilityConfig.Handles)
            {
                DestroyHandle(handle);
            }

            if (_currentTransformEditor != null)
            {
                DestroyImmediate(_currentTransformEditor);
            }

            SceneView.RemoveOverlayFromActiveView(_overlay);
            DestroyImmediate(_utilityConfig);
            DestroyImmediate(_previewer);
            DestroyImmediate(_source);
            DestroyImmediate(_target);
        }

        private void OnEnable()
        {
            if (_utilityConfig == null)
            {
                _utilityConfig = CreateInstance<MSDKUtilityEditorConfig>();
            }

            if (_previewer == null)
            {
                _previewer = CreateInstance<MSDKUtilityEditorPreviewer>();
                _previewer.Window = this;
            }

            _fileReader ??= new SequenceFileReader();
            SceneView.duringSceneGui += OnSceneGUI;
            Undo.postprocessModifications += OnPostProcessModifications;
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        private void OnDisable()
        {
            _previewer.DestroyPreviewCharacterRetargeter();
            _fileReader.ClosePlaybackFile();
            _fileReader = null;
            SceneView.duringSceneGui -= OnSceneGUI;
            Undo.postprocessModifications -= OnPostProcessModifications;
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        private void OnSelectionChange()
        {
            if (_editorMetadataObject == null)
            {
                return;
            }

            CreateGUI();
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (_sceneViewCharacter == null || !_utilityConfig.DrawLines)
            {
                return;
            }

            if (_previewer.TargetSkeletonDrawTPose == null || _previewer.SourceSkeletonDrawTPose == null ||
                _target == null || _source == null)
            {
                return;
            }

            UpdateSkeletonDraws();
            HandleSceneMouseEvents();

            SceneView.RepaintAll();
            _overlay?.Update();
        }

        private UndoPropertyModification[] OnPostProcessModifications(UndoPropertyModification[] modifications)
        {
            bool validModification = false;
            foreach (var modification in modifications)
            {
                if (modification.currentValue.target is Transform)
                {
                    validModification = true;
                }
            }

            if (!validModification)
            {
                return modifications;
            }

            _previewer.UpdateTargetDraw(_previewer.PreviewRetargeter, _utilityConfig, _editorMetadataObject, _source, _target);
            _utilityConfig.SetTPose = false;
            return modifications;
        }

        private void OnUndoRedoPerformed()
        {
            _previewer.UpdateTargetDraw(_previewer.PreviewRetargeter, _utilityConfig, _editorMetadataObject, _source, _target);
        }

        /**********************************************************
         *
         *               UI Element Sections
         *
         **********************************************************/

        private void CreateRootGUI()
        {
            var root = rootVisualElement;
            root.Clear();

            var scrollView = new ScrollView
            {
                horizontalScrollerVisibility = ScrollerVisibility.Hidden,
                style =
                {
                    paddingTop = _singleLineSpace,
                    paddingBottom = _singleLineSpace,
                    paddingLeft = _singleLineSpace,
                    paddingRight = _singleLineSpace
                }
            };
            var transformContainer = new IMGUIContainer(DrawTransformEditor);

            // Debugging.
            if (_debugging)
            {
                scrollView.Add(DebugConfigSection());
                scrollView.Add(DrawLine(Color.black, 2));
            }

            // Add UI elements
            scrollView.Add(RetargetingSetupSection());
            scrollView.Add(DrawLine(Color.black, 1));
            scrollView.Add(RenderEditorSteps());
            scrollView.Add(transformContainer);
            root.Add(scrollView);

            // Register UI callbacks
            var allElements = rootVisualElement.Query<VisualElement>().ToList();
            foreach (var element in allElements)
            {
                element.RegisterCallback<MouseDownEvent>(evt => { _utilityConfig.CurrentlyEditing = true; });
                element.RegisterCallback<MouseUpEvent>(evt => { _utilityConfig.CurrentlyEditing = false; });
            }
        }

        private VisualElement RetargetingSetupSection()
        {
            // Create serialized objects for binding
            var serializedPreviewerObject = new SerializedObject(_previewer);

            // Create root container
            var root = CreateStandardContainer();

            // Add header box with title and progress
            root.Add(CreateHeaderBox(" Retargeting Setup", _currentProgressText));

            // Add title
            root.Add(CreateSpacerLabel());
            root.Add(CreateHeaderLabel(_currentTitle, _headerSize));

            // Add scene view character field
            root.Add(CreatePropertyField(
                serializedPreviewerObject,
                "_sceneViewCharacter",
                "Scene View Character:",
                _buttonSizeHeight));


            // Add config container for buttons
            var configContainer = new VisualElement
            {
                style =
                {
                    height = _buttonSizeHeight,
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.SpaceBetween
                }
            };

            // Add config asset field
            var configAssetField = new ObjectField("Config Asset:")
            {
                style =
                {
                    marginTop = 2,
                    flexGrow = 3,
                    height = 20,
                    alignContent = Align.Center
                },
                value = _editorMetadataObject.ConfigJson
            };
            configAssetField.RegisterValueChangedCallback(e =>
            {
                _editorMetadataObject.ConfigJson = configAssetField.value as TextAsset;
                EditorUtility.SetDirty(_editorMetadataObject);
                AssetDatabase.SaveAssets();
            });

            // Create action buttons
            var saveButton = CreateIconActionButton("Save", () => SaveUpdateConfig(true));
            var refreshButton = CreateIconActionButton("Refresh", RefreshConfig);
            var createButton = CreateIconActionButton("CreateAddNew", CreateNewConfig);

            // Add elements to container
            configContainer.Add(configAssetField);
            configContainer.Add(saveButton);
            configContainer.Add(refreshButton);
            configContainer.Add(createButton);
            root.Add(configContainer);
            return root;
        }

        private VisualElement PreviewSection()
        {
            var root = CreateStandardContainer();

            // Add header
            root.Add(CreateHeaderLabel("Preview Sequences", _headerSize));
            root.Add(new VisualElement { style = { height = _singleLineSpace } });

            // Add scale slider for review step
            if (Step == EditorStep.Review)
            {
                var scaleContainer = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                scaleContainer.Add(new Label("Size: "));

                var scaleSlider = new Slider(0f, 100f)
                {
                    style = { flexGrow = 1, marginLeft = 75 },
                    value = _utilityConfig.ScaleSize
                };

                var scaleValue = new FloatField
                {
                    value = _utilityConfig.ScaleSize,
                    style = { width = 50 }
                };

                // Connect slider and field
                scaleSlider.RegisterValueChangedCallback(e =>
                {
                    _utilityConfig.ScaleSize = e.newValue;
                    scaleValue.value = e.newValue;
                });

                scaleValue.RegisterValueChangedCallback(e =>
                {
                    scaleSlider.value = Mathf.Clamp(e.newValue, 0f, 100f);
                });

                scaleContainer.Add(scaleSlider);
                scaleContainer.Add(scaleValue);
                root.Add(scaleContainer);
            }

            // Add sequence buttons
            var sequenceContainer = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, paddingBottom = _singleLineSpace }
            };

            sequenceContainer.Add(SequenceEntryButton("Walking", Resources.Load<Texture2D>("Editor/Walking")));
            sequenceContainer.Add(SequenceEntryButton("Squatting", Resources.Load<Texture2D>("Editor/Squatting")));
            root.Add(sequenceContainer);

            // Initialize playback UI if needed
            if (_fileReader.HasOpenedFileForPlayback)
            {
                _playbackUI = new MSDKUtilityEditorPlaybackUI(this);
                _playbackUI.Init();
                _playbackUI.StartPlayback();
                root.Add(_playbackUI);
            }

            root.Add(DrawLine(Color.black, 1));
            return root;
        }

        private VisualElement AlignmentSection()
        {
            var root = CreateStandardContainer();
            root.Add(CreateHeaderLabel("Alignment", _headerSize));

            var alignmentContainer = new VisualElement
            {
                style = { flexDirection = FlexDirection.Column, justifyContent = Justify.Center }
            };

            // Create top row with main alignment buttons
            var top = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, justifyContent = Justify.Center }
            };

            // Determine T-pose options based on current step
            var otherTPose = Step == EditorStep.MaxTPose ? "min" : "max";
            var tPoseOption = Step == EditorStep.MaxTPose ? SkeletonTPoseType.MinTPose : SkeletonTPoseType.MaxTPose;

            // Add alignment buttons to top row
            top.Add(MappingButton(PerformAutoMapping, "Align with Skeleton"));
            top.Add(MappingButton(() =>
                {
                    GetSkeletonTPose(_configHandle, SkeletonType.TargetSkeleton, tPoseOption,
                        JointRelativeSpaceType.RootOriginRelativeSpace, out var tPose);
                    _target.ReferencePose = tPose.ToArray();
                    _previewer.ReloadCharacter(_target);
                    _previewer.ScaleCharacter(_source, _target, true);
                    JointMappingUtility.UpdateJointMapping(this, _previewer.PreviewRetargeter, _previewer, _utilityConfig,
                        _editorMetadataObject, _source, _target);
                }, $"Scale from {otherTPose} T-Pose"));

            top.Add(MappingButton(() =>
            {
                GetSkeletonTPose(_configHandle, SkeletonType.TargetSkeleton, SkeletonTPoseType.UnscaledTPose,
                    JointRelativeSpaceType.RootOriginRelativeSpace, out var tPose);
                _target.ReferencePose = tPose.ToArray();
                _utilityConfig.SetTPose = true;
                _utilityConfig.RootScale = Vector3.one;
                _previewer.ReloadCharacter(_target);
            }, "Reset T-Pose"));


            top.Add(MappingButton(() =>
            {
                JointMappingUtility.UpdateJointMapping(this, _previewer.PreviewRetargeter, _previewer, _utilityConfig,
                    _editorMetadataObject,
                    _source, _target);
                _previewer.ReloadCharacter(_target);
            }, "Update Mapping"));

            // Create bottom row with matching buttons
            var bottom = new VisualElement
            {
                style = { justifyContent = Justify.Center, flexDirection = FlexDirection.Row }
            };

            var bottomLeft = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    alignSelf = Align.FlexStart,
                    alignContent = Align.FlexEnd,
                    paddingLeft = _buttonSizeHeight
                }
            };

            // Add matching buttons
            bottomLeft.Add(MappingButton(PerformMatchWrists, "Match Wrists", true));
            bottomLeft.Add(MappingButton(PerformMatchFingers, "Match Fingers", true));
            bottomLeft.Add(MappingButton(PerformMatchLegs, "Match Legs", true));

            // Add placeholder containers for layout
            bottom.Add(bottomLeft);
            bottom.Add(new VisualElement { style = { width = 150 } }); // bottomMiddle
            bottom.Add(new VisualElement { style = { width = 150 } }); // bottomRight

            // Assemble the UI
            alignmentContainer.Add(top);
            alignmentContainer.Add(bottom);
            root.Add(alignmentContainer);

            return root;
        }

        /**********************************************************
         *
         *               Editor Steps
         *
         **********************************************************/

        private VisualElement RenderEditorSteps()
        {
            switch (Step)
            {
                case EditorStep.Configuration:
                    return FirstEditorStep();
                case EditorStep.MinTPose:
                    return SecondEditorStep();
                case EditorStep.MaxTPose:
                    return ThirdEditorStep();
                case EditorStep.Review:
                    return FourthEditorStep();
                case EditorStep.End:
                default:
                    ExitEditor();
                    return null;
            }
        }

        private VisualElement FirstEditorStep()
        {
            var root = EditorStepContainer();
            var serializedTargetObject = new SerializedObject(_target);

            // Create and configure bone transforms foldout
            var foldout = new Foldout
            {
                text = "Config Bone Transforms",
                value = _configBoneTransformsFoldout,
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    paddingTop = _singleLineSpace,
                    paddingBottom = _singleLineSpace
                }
            };
            foldout.RegisterValueChangedCallback(val => _configBoneTransformsFoldout = val.newValue);

            // Add joint entries to foldout
            for (var i = 0; i < _target.SkeletonInfo.JointCount; i++)
            {
                var entryContainer = CreateRightJointEntryContainer();
                var leftSideContainer = CreateLeftJointEntryContainer();
                var jointName = _target.JointNames[i];

                GetChildJointIndexes(_configHandle, SkeletonType.TargetSkeleton, i, out var childJointIndexes);
                var hasChildIndex = childJointIndexes.Length > 0;

                // Add remove button or spacer
                if (i > 0 && !hasChildIndex)
                {
                    var removeJointButton = new Button(() =>
                    {
                        var data = MSDKUtilityEditor.CreateRetargetingData(_sceneViewCharacter, _target);
                        data.RemoveJoint(jointName);
                        CreateConfig(this, _previewer.PreviewRetargeter, _previewer, _utilityConfig,
                            _utilityConfig.EditorMetadataObject, _metadataAssetPath, false, data);
                        CreateGUI();
                    })
                    {
                        text = "-",
                        style =
                        {
                            width = _buttonSizeHeight,
                            height = _buttonSizeHeight
                        }
                    };
                    leftSideContainer.Add(removeJointButton);
                }
                else
                {
                    leftSideContainer.Add(CreateSpacerLabel());
                }

                // Add joint name label
                leftSideContainer.Add(CreateJointNameLabel(jointName));

                // Add property field
                var boneTransformField = CreateBoneTransformField(serializedTargetObject, i);

                // Assemble container
                entryContainer.Add(leftSideContainer);
                entryContainer.Add(boneTransformField);
                foldout.Add(entryContainer);
            }

            // Create known joints container
            var knownJointsContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    paddingTop = _singleLineSpace,
                    paddingBottom = _doubleLineSpace
                }
            };
            for (var i = KnownJointType.Root; i < KnownJointType.KnownJointCount; i++)
            {
                var knownJoints = serializedTargetObject.FindProperty("KnownSkeletonJoints");
                var knownJointIndex = (int)i;
                var knownJointField = new PropertyField(
                    knownJoints.GetArrayElementAtIndex(knownJointIndex),
                    i.ToString())
                {
                    style = { justifyContent = Justify.SpaceBetween }
                };

                knownJointField.RegisterValueChangeCallback(evt =>
                {
                    UpdateConfig(this, _previewer.PreviewRetargeter, _previewer, _utilityConfig,
                        _editorMetadataObject, false, false);
                });

                knownJointField.Bind(serializedTargetObject);
                knownJointsContainer.Add(knownJointField);
            }

            // Add elements to root
            root.Add(foldout);
            root.Add(CreateHeaderLabel("Known Joints", 12));
            root.Add(knownJointsContainer);

            // Add T-Pose button
            root.Add(new Button(() =>
            {
                JointAlignmentUtility.SetToTPose(_source, _target);
                JointAlignmentUtility.UpdateTPoseData(_target);
                _previewer.ReloadCharacter(_target);
            })
            {
                text = "Pose character to T-Pose",
                style = { paddingTop = _buttonPadding, paddingBottom = _buttonPadding }
            });

            // Add navigation buttons
            root.Add(PreviousNextSteps(true, false, false, _editorMetadataObject.ConfigJson != null));

            return root;
        }

        private VisualElement SecondEditorStep()
        {
            var root = EditorStepContainer();
            root.Add(PreviewSection());
            root.Add(AlignmentSection());
            root.Add(PreviousNextSteps(false, false, true, true));
            return root;
        }

        private VisualElement ThirdEditorStep()
        {
            var root = EditorStepContainer();
            root.Add(PreviewSection());
            root.Add(AlignmentSection());
            root.Add(PreviousNextSteps(false, false, true, true));
            return root;
        }

        private VisualElement FourthEditorStep()
        {
            var root = EditorStepContainer();
            root.Add(PreviewSection());
            root.Add(new Button(() =>
            {
                _validatedConfigFinish = true;
                SaveConfig(this, _previewer.PreviewRetargeter, _previewer, _utilityConfig,
                    _editorMetadataObject,
                    false);
                CreateGUI();
            })
            {
                text = "Validate and save config",
                style = { paddingTop = _buttonPadding, paddingBottom = _buttonPadding }
            });
            if (_validatedConfigFinish)
            {
                root.Add(new HelpBox("No issues detected in mapping!", HelpBoxMessageType.Info));
            }

            root.Add(PreviousNextSteps(false, true, true, _validatedConfigFinish));
            return root;
        }

        private VisualElement PreviousNextSteps(bool firstStep, bool lastStep, bool enablePrevious, bool enableNext)
        {
            var root = CreateStandardContainer();
            root.style.flexDirection = FlexDirection.Row;
            root.style.justifyContent = Justify.SpaceBetween;

            if (!_target.IsValid())
            {
                root.Add(new HelpBox("Configuration is invalid! Please fix any missing fields! " +
                                     "The configuration can also be refreshed into a valid configuration by " +
                                     "using the buttons past the config asset field.", HelpBoxMessageType.Error));
            }

            root.Add(CreateNavigationButton("Previous", enablePrevious, enablePrevious, () =>
            {
                SavePrompt(lastStep, false);
                CreateGUI();
            }));

            root.Add(CreateNavigationButton("Next", enableNext, true, () =>
            {
                SavePrompt(lastStep, true);
                CreateGUI();
            }));

            return root;
        }

        private void SavePrompt(bool lastStep, bool next)
        {
            int choice = 0;
            if (!lastStep)
            {
                choice = EditorUtility.DisplayDialogComplex("Save Configuration Changes",
                    "Save updates to the configuration?",
                    "Yes", "No", "Cancel");
            }

            if (choice is 0 or 1)
            {
                if (next)
                {
                    _utilityConfig.Step++;
                }
                else
                {
                    _utilityConfig.Step--;
                }

                _validatedConfigFinish = false;
                _previewer.DestroyPreviewCharacterRetargeter();
                if (choice == 0)
                {
                    UpdateConfig(this, _previewer.PreviewRetargeter, _previewer, _utilityConfig,
                        _editorMetadataObject,
                        true, false);
                }

                ResetConfig(this, _previewer.PreviewRetargeter, _previewer, _utilityConfig,
                    _editorMetadataObject.ConfigJson.text, false);
                LoadConfig(this, _previewer.PreviewRetargeter, _previewer, _utilityConfig,
                    _editorMetadataObject.ConfigJson.text);
                _overlay.Reload();
            }
        }

        private void DrawTransformEditor()
        {
            if (Selection.activeTransform != null)
            {
                if (_currentTransformEditor != null && _currentTransformEditor.target != Selection.activeTransform)
                {
                    DestroyImmediate(_currentTransformEditor);
                }

                if (_currentTransformEditor == null)
                {
                    _currentTransformEditor = UnityEditor.Editor.CreateEditor(Selection.activeTransform);
                }

                EditorGUILayout.Space();
                _currentTransformEditorFoldout = EditorGUILayout.InspectorTitlebar(_currentTransformEditorFoldout,
                    Selection.activeTransform, true);

                if (_currentTransformEditorFoldout && _currentTransformEditor != null)
                {
                    _currentTransformEditor.OnInspectorGUI();
                }
            }
            else if (_currentTransformEditor != null)
            {
                DestroyImmediate(_currentTransformEditor);
                _currentTransformEditor = null;
            }
        }

        private void ExitEditor()
        {
            MSDKUtilityEditorStage.DestroyCurrentScene(_previewStage.scene);
            StageUtility.GoToMainStage();
            Close();
        }

        /**********************************************************
         *
         *               UI Element Helpers
         *
         **********************************************************/

        private Button CreateNavigationButton(string text, bool enabled, bool visible, Action onClick)
        {
            var button = new Button(onClick)
            {
                text = text,
                style =
                {
                    alignSelf = Align.Stretch,
                    flexGrow = 1,
                    minWidth = 100,
                    height = _buttonSizeHeight
                }
            };
            button.SetEnabled(enabled);
            button.visible = visible;
            return button;
        }

        private Button CreateIconActionButton(string iconName, Action clickAction)
        {
            var button = new Button(clickAction)
            {
                style = { flexGrow = 1 }
            };

            button.Add(new Image
            {
                image = EditorGUIUtility.IconContent(iconName).image
            });

            return button;
        }

        private Button MappingButton(Action callback, string text, bool isMatchingStyle = false)
        {
            var button = new Button(callback)
            {
                text = text,
                style =
                {
                    paddingTop = _buttonPadding,
                    paddingLeft = _buttonPadding,
                    paddingRight = _buttonPadding,
                    paddingBottom = _buttonPadding
                }
            };
            if (isMatchingStyle)
            {
                button.style.alignSelf = Align.Stretch;
            }
            else
            {
                button.style.width = 150;
            }

            return button;
        }

        private VisualElement SequenceEntryButton(string sequenceName, Texture2D image)
        {
            var root = new VisualElement
            {
                style = { flexDirection = FlexDirection.Column }
            };

            // Add label
            root.Add(new Label
            {
                text = sequenceName,
                style =
                {
                    fontSize = 12,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    alignSelf = Align.Center,
                    paddingTop = _buttonPadding,
                    paddingBottom = _buttonPadding
                }
            });

            // Add button with image
            var button = new Button(() =>
            {
                CreateGUI();
                OpenPlaybackFile(sequenceName);
                CreateGUI();
            });

            button.Add(new Image { image = image });
            button.style.height = button.style.width = 60;
            root.Add(button);

            return root;
        }

        private VisualElement CreateRightJointEntryContainer()
        {
            return new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.SpaceBetween,
                    alignItems = Align.FlexEnd
                }
            };
        }

        private VisualElement CreateLeftJointEntryContainer()
        {
            return new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    marginRight = 1,
                    marginBottom = -2,
                    alignItems = Align.Center
                }
            };
        }

        private VisualElement EditorStepContainer()
        {
            return new VisualElement
            {
                style =
                {
                    paddingTop = _singleLineSpace,
                    paddingBottom = _singleLineSpace
                }
            };
        }

        private VisualElement CreateStandardContainer()
        {
            return new VisualElement
            {
                style =
                {
                    paddingTop = _singleLineSpace,
                    paddingBottom = _singleLineSpace
                }
            };
        }

        private VisualElement DrawLine(Color color, float height)
        {
            return new Box
            {
                style =
                {
                    backgroundColor = color,
                    height = height
                }
            };
        }

        private Box CreateHeaderBox(string leftText, string rightText)
        {
            var box = new Box
            {
                style =
                {
                    height = 18,
                    justifyContent = Justify.SpaceBetween,
                    flexDirection = FlexDirection.Row
                }
            };

            box.Add(new Label { text = leftText });
            box.Add(new Label { text = rightText });

            return box;
        }

        private Label CreateHeaderLabel(string text, int fontSize)
        {
            return new Label
            {
                text = text,
                style =
                {
                    fontSize = fontSize,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };
        }

        private Label CreateJointNameLabel(string jointName)
        {
            return new Label
            {
                text = jointName,
                style =
                {
                    unityTextAlign = TextAnchor.MiddleLeft,
                    height = _buttonSizeHeight,
                }
            };
        }

        private Label CreateSpacerLabel()
        {
            return new Label
            {
                text = " ",
                style =
                {
                    width = _buttonSizeHeight,
                    height = _buttonSizeHeight
                }
            };
        }

        private PropertyField CreateBoneTransformField(SerializedObject serializedObject, int index)
        {
            var field = new PropertyField(
                serializedObject.FindProperty("SkeletonJoints").GetArrayElementAtIndex(index),
                string.Empty)
            {
                style = { flexGrow = 1 }
            };
            field.Bind(serializedObject);
            return field;
        }

        private PropertyField CreatePropertyField(SerializedObject serializedObject, string propertyPath, string label,
            float height = 0)
        {
            var field = new PropertyField(
                serializedObject.FindProperty(propertyPath),
                label);

            if (height > 0)
            {
                field.style.height = height;
            }

            field.Bind(serializedObject);
            return field;
        }

        private Foldout CreateConfigFoldout(string configTitle, UnityEditor.Editor editor,
            SerializedObject serializedObject)
        {
            var foldout = new Foldout
            {
                text = configTitle,
                value = false
            };

            foldout.Add(new IMGUIContainer(editor.OnInspectorGUI));
            foldout.Bind(serializedObject);

            return foldout;
        }


        /**********************************************************
         *
         *               Utility functions
         *
         **********************************************************/

        private void SaveUpdateConfig(bool saveConfig)
        {
            UpdateConfig(this, _previewer.PreviewRetargeter, _previewer, _utilityConfig,
                _editorMetadataObject, saveConfig, false);
            EditorUtility.SetDirty(_editorMetadataObject);
            AssetDatabase.SaveAssets();
            CreateGUI();
        }

        private void RefreshConfig()
        {
            ResetConfig(this, _previewer.PreviewRetargeter, _previewer, _utilityConfig,
                _editorMetadataObject.ConfigJson.text);
            EditorUtility.SetDirty(_editorMetadataObject);
            AssetDatabase.SaveAssets();
            CreateGUI();
        }

        private void CreateNewConfig()
        {
            CreateConfig(this, _previewer.PreviewRetargeter, _previewer, _utilityConfig,
                _utilityConfig.EditorMetadataObject, _metadataAssetPath);
            EditorUtility.SetDirty(_editorMetadataObject);
            AssetDatabase.SaveAssets();
            CreateGUI();
        }

        private void PerformMatchWrists()
        {
            JointMappingUtility.PerformWristMatching(_source, _target);
            JointAlignmentUtility.PerformArmScaling(_source, _target);
            _previewer.ReloadCharacter(_target);
        }

        private void PerformMatchFingers()
        {
            JointMappingUtility.PerformFingerMatching(this, _previewer.PreviewRetargeter, _previewer, _utilityConfig,
                _editorMetadataObject, _source, _target, _leftStartBoneIds, _leftEndBoneIds, KnownJointType.LeftWrist);
            JointMappingUtility.PerformFingerMatching(this, _previewer.PreviewRetargeter, _previewer, _utilityConfig,
                _editorMetadataObject, _source, _target, _rightStartBoneIds, _rightEndBoneIds, KnownJointType.RightWrist);
            _previewer.ReloadCharacter(_target);
        }

        private void PerformMatchLegs()
        {
            JointMappingUtility.PerformLegMatching(_source, _target);
            _previewer.ReloadCharacter(_target);
        }

        public void PerformAutoMapping()
        {
            JointMappingUtility.AutoMapping(this, _previewer.PreviewRetargeter, _previewer, _utilityConfig, _source,
                ref _target);
            JointMappingUtility.UpdateJointMapping(this, _previewer.PreviewRetargeter, _previewer, _utilityConfig,
                _editorMetadataObject,
                _source, _target);
            _previewer.ReloadCharacter(_target);
        }

        private void InitializeSkeletonDraws()
        {
        }

        private void RemoveExistingOverlays()
        {
            foreach (var sceneView in SceneView.sceneViews)
            {
                if (sceneView is MSDKUtilityEditorOverlay targetOverlay)
                {
                    SceneView.RemoveOverlayFromActiveView(targetOverlay);
                }
            }
        }

        private void UpdateSkeletonDraws()
        {
            if (_overlay == null)
            {
                return;
            }

            if (_overlay.ShouldDrawSource)
            {
                if (Step is > EditorStep.Configuration and < EditorStep.Review)
                {
                    _previewer.SourceSkeletonDrawTPose.Draw();
                }
            }

            if (_overlay.ShouldDrawTarget)
            {
                _previewer.TargetSkeletonDrawTPose.Draw();
            }

            if (_previewer.PreviewRetargeter != null || _fileReader.IsPlaying)
            {
                _previewer.DrawPreviewCharacter();
            }

            if (!_fileReader.HasOpenedFileForPlayback)
            {
                _previewer.DestroyPreviewCharacterRetargeter();
                Repaint();
            }
        }

        private void HandleSceneMouseEvents()
        {
            if (Event.current.button != 0)
            {
                return;
            }

            if (Event.current.type == EventType.MouseDown)
            {
                _utilityConfig.CurrentlyEditing = true;
            }
            else if (_utilityConfig.CurrentlyEditing && Event.current.type == EventType.MouseUp)
            {
                _utilityConfig.CurrentlyEditing = false;
                if (_utilityConfig.SetTPose)
                {
                    _utilityConfig.SetTPose = false;
                }
                else
                {
                    JointAlignmentUtility.LoadScale(_target, _utilityConfig);
                    _previewer.ReloadCharacter(_target);
                }
            }
        }

        private bool EnsureMetadataLoaded()
        {
            if (_editorMetadataObject == null)
            {
                MSDKUtilityEditorMetadata.LoadConfigAsset(_metadataAssetPath,
                    ref _utilityConfig.EditorMetadataObject, ref _displayedConfig);
            }

            return _editorMetadataObject != null;
        }

        private void ValidateConfiguration()
        {
            if (_editorMetadataObject.ConfigJson == null || _target == null || _source == null)
            {
                return;
            }

            if (_target.ConfigHandle == INVALID_HANDLE)
            {
                LoadConfig(this, _previewer.PreviewRetargeter, _previewer, _utilityConfig);
            }

            ValidateMSDKUtilityEditorInfo(_previewer, _previewer.TargetSkeletonDrawTPose, _target);

            if (Step >= EditorStep.MinTPose)
            {
                ValidateMSDKUtilityEditorInfo(_previewer, _previewer.SourceSkeletonDrawTPose, _source);
            }
        }

        // Debugging.
        private VisualElement DebugConfigSection()
        {
            // Create serialized object and root container
            var serializedObject = new SerializedObject(this);
            var root = CreateStandardContainer();

            // Create main debug foldout
            var foldout = new Foldout { text = "DEBUG", value = false };

            // Setup editors for source and target properties
            var sourceProperty = serializedObject.FindProperty("_source");
            var targetProperty = serializedObject.FindProperty("_target");
            UnityEditor.Editor.CreateCachedEditor(sourceProperty.objectReferenceValue, null, ref _sourceEditor);
            UnityEditor.Editor.CreateCachedEditor(targetProperty.objectReferenceValue, null, ref _targetEditor);

            // Create nested container with source and target foldouts
            var nestedContainer = new VisualElement { style = { marginLeft = 15 } };

            // Add config foldouts
            nestedContainer.Add(CreateConfigFoldout("Source Config Info", _sourceEditor, serializedObject));
            nestedContainer.Add(CreateConfigFoldout("Target Config Info", _targetEditor, serializedObject));

            // Assemble UI hierarchy
            foldout.Add(nestedContainer);
            root.Add(foldout);

            return root;
        }
    }
}
