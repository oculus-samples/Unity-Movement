// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System;
using System.Collections.Generic;
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

        public MSDKUtilityEditorMetadata EditorMetadataObject
        {
            get => _utilityConfig?.EditorMetadataObject;
            set => _utilityConfig.EditorMetadataObject = value;
        }

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
        public string SelectedJointName =>
            _target.JointNames != null ? _target.JointNames[SelectedIndex] : string.Empty;

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

                var allMappingsText = new StringBuilder();
                var entryIndex = 0;
                var currentBehavior = JointMappingBehaviorType.Invalid;

                for (var i = 0; i < _target.JointMappings.Length; i++)
                {
                    var jointMapping = _target.JointMappings[i];
                    if (jointMapping.JointIndex != SelectedIndex)
                    {
                        continue;
                    }

                    // Add behavior header if it's different from the current one, and reset the entry index
                    if (currentBehavior != jointMapping.Behavior)
                    {
                        currentBehavior = jointMapping.Behavior;
                        if (allMappingsText.Length != 0)
                        {
                            allMappingsText.Append("\n");
                        }

                        allMappingsText.AppendFormat("<b>Behavior: <color=#0080FF>{0}</color></b>\n", currentBehavior);
                        entryIndex = 0;
                    }

                    // Find the starting index for this mapping's entries
                    var startIndex = 0;
                    for (var j = 0; j < i; j++)
                    {
                        startIndex += _target.JointMappings[j].EntriesCount;
                    }

                    // Collect all entries for this mapping
                    var entries = new List<(JointMappingEntry entry, string jointName, int originalIndex)>();
                    for (var k = 0; k < _target.JointMappings[i].EntriesCount; k++)
                    {
                        var entry = _target.JointMappingEntries[startIndex + k];
                        var jointName = _target.JointMappings[i].Type == SkeletonType.SourceSkeleton
                            ? _source.JointNames[entry.JointIndex]
                            : _target.JointNames[entry.JointIndex];

                        entries.Add((entry, jointName, k));
                    }

                    // Sort entries by RotationWeight (primary) and PositionWeight (secondary) in descending order
                    entries.Sort((a, b) =>
                    {
                        var rotationComparison = b.entry.RotationWeight.CompareTo(a.entry.RotationWeight);
                        return rotationComparison != 0
                            ? rotationComparison
                            : b.entry.PositionWeight.CompareTo(a.entry.PositionWeight);
                    });

                    // Add sorted entries to the output
                    foreach (var (entry, jointName, _) in entries)
                    {
                        allMappingsText.AppendFormat(
                            " <b>{0}. {1}</b> \n- Rotation: {2:F3} | Position: {3:F3} \n",
                            ++entryIndex,
                            jointName,
                            entry.RotationWeight,
                            entry.PositionWeight);
                    }
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
        private bool _debugging;
        private bool _currentTransformEditorFoldout = true;
        private bool _configBoneTransformsFoldout;
        private bool _validatedConfigFinish;
        private bool _displayedConfig;
        private bool _initialized;

        // Styling settings.
        private const int _buttonSizeHeight = 28;
        private const int _singleLineSpace = 8;
        private const int _doubleLineSpace = 16;
        private const int _headerSize = 18;
        private const int _buttonPadding = 6;

        // UI Colors
        private static readonly Color _primaryColor = new Color(0.0f, 0.47f, 0.95f, 1.0f);
        private static readonly Color _backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.05f);
        private static readonly Color _cardBackgroundColor = new Color(1f, 1f, 1f, 0.03f);
        private static readonly Color _borderColor = new Color(0f, 0f, 0f, 0.2f);
        private static readonly Color _headerBackgroundColor = new Color(0.0f, 0.47f, 0.95f, 0.1f);
        private static readonly Color _buttonHoverColor = new Color(0.0f, 0.47f, 0.95f, 0.2f);

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
            if (_previewer.Retargeter == null)
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
            if (EditorMetadataObject == null)
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

            _previewer.UpdateTargetDraw(this);
            _utilityConfig.SetTPose = false;
            return modifications;
        }

        private void OnUndoRedoPerformed()
        {
            _previewer.UpdateTargetDraw(this);
        }

        /**********************************************************
         *
         *               UI Element Sections
         *
         **********************************************************/

        private void CreateRootGUI()
        {
            // Get and clear the root element
            var root = rootVisualElement;
            root.Clear();

            // Apply global styles to the root element
            root.style.backgroundColor = _backgroundColor;

            // Create main scroll view with improved styling
            var scrollView = new ScrollView
            {
                horizontalScrollerVisibility = ScrollerVisibility.Hidden,
                style =
                {
                    paddingTop = 8,
                    paddingBottom = 12,
                    paddingLeft = 10,
                    paddingRight = 10
                }
            };

            // Create transform editor container
            var transformContainer = new IMGUIContainer(DrawTransformEditor)
            {
                style =
                {
                    marginTop = 8,
                    paddingTop = 8,
                    paddingBottom = 8,
                    paddingLeft = 8,
                    paddingRight = 8,
                    borderTopWidth = 1,
                    borderTopColor = new Color(0, 0, 0, 0.2f)
                }
            };

            // Add debug section if debugging is enabled
            if (_debugging)
            {
                var debugSection = DebugConfigSection();
                debugSection.style.marginBottom = 16;
                scrollView.Add(debugSection);
            }

            // Create main content container with card-like styling
            var mainContentCard = new VisualElement
            {
                style =
                {
                    backgroundColor = _cardBackgroundColor,
                    borderBottomWidth = 1,
                    borderTopWidth = 1,
                    borderLeftWidth = 1,
                    borderRightWidth = 1,
                    borderBottomColor = _borderColor,
                    borderTopColor = _borderColor,
                    borderLeftColor = _borderColor,
                    borderRightColor = _borderColor,
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4,
                    marginBottom = 8,
                    paddingBottom = 8
                }
            };

            // Add retargeting setup section to the main content card
            var retargetingSection = RetargetingSetupSection();
            if (retargetingSection == null)
            {
                return;
            }
            retargetingSection.style.paddingTop = 0;
            mainContentCard.Add(retargetingSection);

            // Add the main content card to the scroll view
            scrollView.Add(mainContentCard);

            // Reduced spacing between sections
            scrollView.style.marginTop = 0;

            // Add editor steps section with improved styling
            var editorStepsSection = RenderEditorSteps();
            if (editorStepsSection != null)
            {
                editorStepsSection.style.backgroundColor = new Color(1f, 1f, 1f, 0.02f);
                editorStepsSection.style.borderTopLeftRadius =
                    editorStepsSection.style.borderTopRightRadius =
                        editorStepsSection.style.borderBottomLeftRadius =
                            editorStepsSection.style.borderBottomRightRadius = 4;
                editorStepsSection.style.paddingLeft = 12;
                editorStepsSection.style.paddingRight = 12;
                editorStepsSection.style.paddingTop = 8;
                editorStepsSection.style.paddingBottom = 8;
                editorStepsSection.style.marginBottom = 8;
                scrollView.Add(editorStepsSection);
            }

            // Add transform container
            scrollView.Add(transformContainer);

            // Add the scroll view to the root
            root.Add(scrollView);

            // Register UI callbacks for mouse events
            var allElements = rootVisualElement.Query<VisualElement>().ToList();
            foreach (var element in allElements)
            {
                element.RegisterCallback<MouseDownEvent>(evt => { _utilityConfig.CurrentlyEditing = true; });
                element.RegisterCallback<MouseUpEvent>(evt => { _utilityConfig.CurrentlyEditing = false; });
            }

            // Add hover effects to buttons
            var allButtons = rootVisualElement.Query<Button>().ToList();

            // Store all button original colors
            var buttonOriginalColors = new Dictionary<Button, Color>();

            foreach (var button in allButtons)
            {
                // Get the current background color
                var originalColor = button.text switch
                {
                    // Special handling for different button types
                    "Next" or "Done" => new Color(0.1f, 0.6f, 0.3f, 0.7f),
                    "Previous" => new Color(0.2f, 0.2f, 0.2f, 0.8f),
                    "Pose character to T-Pose" => new Color(_primaryColor.r, _primaryColor.g, _primaryColor.b, 0.1f),
                    "Validate and save config" => new Color(0.1f, 0.6f, 0.3f, 0.7f),
                    _ => new Color(0, 0, 0, 0)
                };

                // Store the original color
                buttonOriginalColors[button] = originalColor;

                // Set initial background color
                button.style.backgroundColor = originalColor;

                // Add hover effect - only apply if button is enabled
                button.RegisterCallback<MouseEnterEvent>(evt =>
                {
                    if (button.enabledSelf)
                    {
                        button.style.backgroundColor = _buttonHoverColor;
                    }
                });

                // Restore original color on mouse exit
                button.RegisterCallback<MouseLeaveEvent>(evt =>
                {
                    if (buttonOriginalColors.TryGetValue(button, out Color color))
                    {
                        button.style.backgroundColor = color;
                    }
                });
            }
        }

        private VisualElement RetargetingSetupSection()
        {
            if (_previewer == null)
            {
                return null;
            }

            // Create serialized objects for binding
            var serializedPreviewerObject = new SerializedObject(_previewer);

            // Create root container
            var root = CreateStandardContainer();

            // Create a styled header with background
            var headerContainer = new VisualElement
            {
                style =
                {
                    backgroundColor = _headerBackgroundColor,
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4,
                    paddingTop = 8,
                    paddingBottom = 8,
                    paddingLeft = 10,
                    paddingRight = 10,
                    marginBottom = 6,
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.SpaceBetween
                }
            };

            headerContainer.Add(new Label
            { text = "Retargeting Setup", style = { unityFontStyleAndWeight = FontStyle.Bold } });
            headerContainer.Add(new Label { text = _currentProgressText });
            root.Add(headerContainer);

            // Add title in a styled container
            var titleContainer = new VisualElement
            {
                style =
                {
                    marginBottom = 6,
                    paddingLeft = 4
                }
            };
            titleContainer.Add(CreateHeaderLabel(_currentTitle, _headerSize));
            root.Add(titleContainer);

            // Add scene view character field in a styled container
            var characterFieldContainer = new VisualElement
            {
                style =
                {
                    marginBottom = 8,
                    paddingLeft = 4,
                    paddingRight = 4
                }
            };

            characterFieldContainer.Add(CreatePropertyField(
                serializedPreviewerObject,
                "_sceneViewCharacter",
                "Scene View Character:",
                _buttonSizeHeight));

            root.Add(characterFieldContainer);

            // Add config container for buttons in a styled box
            var configOuterContainer = new VisualElement
            {
                style =
                {
                    paddingLeft = 4,
                    paddingRight = 4,
                }
            };

            var configContainer = new VisualElement
            {
                style =
                {
                    height = 4,
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.SpaceBetween,
                    alignItems = Align.Center,
                    minWidth = 200
                }
            };

            // Add config asset field
            var configAssetField = new ObjectField("Config Asset:")
            {
                style =
                {
                    marginTop = 2,
                    flexGrow = 1,
                    flexShrink = 1,
                    minWidth = 100,
                    height = 18,
                    alignContent = Align.Center
                },
                value = EditorMetadataObject.ConfigJson
            };
            configAssetField.RegisterValueChangedCallback(e =>
            {
                EditorMetadataObject.ConfigJson = configAssetField.value as TextAsset;
                EditorUtility.SetDirty(EditorMetadataObject);
                AssetDatabase.SaveAssets();
            });

            // Create action buttons
            var saveButton = CreateIconActionButton("Save", () => SaveUpdateConfig(true));
            var refreshButton = CreateIconActionButton("Refresh", RefreshConfig);
            var createButton = CreateIconActionButton("CreateAddNew", CreateNewConfig);

            // Create button container with fixed width to prevent clipping
            var buttonContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexShrink = 0,
                    flexGrow = 0,
                    marginLeft = 8,
                    width = 90,
                    justifyContent = Justify.FlexEnd
                }
            };

            buttonContainer.Add(saveButton);
            buttonContainer.Add(refreshButton);
            buttonContainer.Add(createButton);

            // Add elements to container
            configContainer.Add(configAssetField);
            configContainer.Add(buttonContainer);

            configOuterContainer.Add(configContainer);
            root.Add(configOuterContainer);

            return root;
        }

        private VisualElement PreviewSection()
        {
            var root = CreateStandardContainer();

            // Create a styled header with background
            var headerContainer = new VisualElement
            {
                style =
                {
                    backgroundColor = _headerBackgroundColor,
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4,
                    paddingTop = 8,
                    paddingBottom = 8,
                    paddingLeft = 10,
                    paddingRight = 10,
                    marginBottom = 2
                }
            };

            headerContainer.Add(CreateHeaderLabel("Preview Sequences", _headerSize));
            root.Add(headerContainer);

            // Main container with card-like styling
            var previewContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    justifyContent = Justify.Center,
                    backgroundColor = _cardBackgroundColor,
                    borderBottomColor = _borderColor,
                    borderBottomWidth = 1,
                    borderLeftColor = _borderColor,
                    borderLeftWidth = 1,
                    borderRightColor = _borderColor,
                    borderRightWidth = 1,
                    borderTopColor = _borderColor,
                    borderTopWidth = 1,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4,
                    paddingTop = 8,
                    paddingBottom = 8,
                    paddingLeft = 10,
                    paddingRight = 10,
                    marginBottom = 8
                }
            };

            // Add scale slider for review step
            if (Step == EditorStep.Review)
            {
                var scaleLabel = new Label("Size Adjustment")
                {
                    style =
                    {
                        fontSize = 14,
                        unityFontStyleAndWeight = FontStyle.Bold,
                        marginBottom = 6
                    }
                };
                previewContainer.Add(scaleLabel);

                // Create outer container with fixed width to prevent overflow
                var scaleOuterContainer = new VisualElement
                {
                    style =
                    {
                        width = Length.Percent(100),
                        marginBottom = 12,
                        marginLeft = 8,
                        marginRight = 8,
                        paddingLeft = 8,
                        paddingRight = 8,
                        overflow = Overflow.Hidden // Prevent content from overflowing
                    }
                };

                var scaleContainer = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        alignItems = Align.Center,
                        width = Length.Percent(100),
                        maxWidth = Length.Percent(100)
                    }
                };

                // Fixed-width label
                var sizeLabel = new Label("Size: ")
                {
                    style =
                    {
                        width = 40,
                        minWidth = 40,
                        flexShrink = 0,
                        unityTextAlign = TextAnchor.MiddleLeft
                    }
                };
                scaleContainer.Add(sizeLabel);

                // Slider with constraints to prevent overflow
                var scaleSlider = new Slider(0f, 100f)
                {
                    style = {
                        flexGrow = 1,
                        flexShrink = 1,
                        minWidth = 80,
                        maxWidth = Length.Percent(80) // Limit slider width to leave room for the value field
                    },
                    value = _utilityConfig.ScaleSize
                };

                // Right-aligned value field with fixed width
                var scaleValue = new IntegerField
                {
                    value = Mathf.RoundToInt(_utilityConfig.ScaleSize),
                    style = {
                        width = 60,
                        minWidth = 60,
                        maxWidth = 60,
                        marginLeft = 8,
                        flexShrink = 0,
                        unityTextAlign = TextAnchor.MiddleRight // Right-align text
                    }
                };

                // Connect slider and field
                scaleSlider.RegisterValueChangedCallback(e =>
                {
                    _utilityConfig.ScaleSize = e.newValue;
                    scaleValue.value = Mathf.RoundToInt(e.newValue);
                });

                scaleValue.RegisterValueChangedCallback(e =>
                {
                    scaleSlider.value = Mathf.Clamp(e.newValue, 0f, 100f);
                });

                scaleContainer.Add(scaleSlider);
                scaleContainer.Add(scaleValue);
                scaleOuterContainer.Add(scaleContainer);
                previewContainer.Add(scaleOuterContainer);
            }

            // Create section for sequences
            var sequencesLabel = new Label("Available Sequences")
            {
                style =
                {
                    fontSize = 14,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginTop = Step == EditorStep.Review ? 0 : 6,
                    marginBottom = 6
                }
            };
            previewContainer.Add(sequencesLabel);

            // Create responsive grid for sequence buttons
            var sequenceGrid = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexWrap = Wrap.Wrap,
                    justifyContent = Justify.Center,
                    marginBottom = 8
                }
            };

            // Add sequence buttons
            sequenceGrid.Add(CreateSequenceButton("Walking", Resources.Load<Texture2D>("Editor/Walking")));
            sequenceGrid.Add(CreateSequenceButton("Squatting", Resources.Load<Texture2D>("Editor/Squatting")));

            previewContainer.Add(sequenceGrid);

            // Add help text
            var helpBox = new HelpBox("Select a sequence to preview character retargeting.",
                HelpBoxMessageType.Info)
            {
                style =
                {
                    marginTop = 0
                }
            };
            previewContainer.Add(helpBox);

            // Add the preview container to the root
            root.Add(previewContainer);

            // Initialize playback UI if needed
            if (_fileReader is { HasOpenedFileForPlayback: true })
            {
                _playbackUI = new MSDKUtilityEditorPlaybackUI(this);
                _playbackUI.Init();
                _playbackUI.StartPlayback();
                root.Add(_playbackUI);
            }

            return root;
        }

        private Button CreateSequenceButton(string sequenceName, Texture2D image)
        {
            var button = new Button(() =>
            {
                CreateGUI();
                OpenPlaybackFile(sequenceName);
                CreateGUI();
            })
            {
                style =
                {
                    width = 120,
                    height = 90,
                    marginRight = 8,
                    marginBottom = 8,
                    paddingTop = 6,
                    paddingBottom = 6,
                    flexDirection = FlexDirection.Column,
                    alignItems = Align.Center,
                    justifyContent = Justify.Center,
                    borderTopLeftRadius = 3,
                    borderTopRightRadius = 3,
                    borderBottomLeftRadius = 3,
                    borderBottomRightRadius = 3
                }
            };

            // Add image
            var imageElement = new Image
            {
                image = image,
                style =
                {
                    width = 60,
                    height = 60,
                    marginBottom = 4
                }
            };
            button.Add(imageElement);

            // Add label
            var label = new Label(sequenceName)
            {
                style =
                {
                    fontSize = 12,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };
            button.Add(label);

            return button;
        }

        private VisualElement AlignmentSection()
        {
            var root = CreateStandardContainer();

            // Create a styled header with background
            var headerContainer = new VisualElement
            {
                style =
                {
                    backgroundColor = _headerBackgroundColor,
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4,
                    paddingTop = 8,
                    paddingBottom = 8,
                    paddingLeft = 10,
                    paddingRight = 10,
                    marginBottom = 4
                }
            };

            headerContainer.Add(CreateHeaderLabel("Alignment", _headerSize));
            root.Add(headerContainer);

            // Main container with card-like styling
            var alignmentContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    justifyContent = Justify.Center,
                    backgroundColor = _cardBackgroundColor,
                    borderBottomColor = _borderColor,
                    borderBottomWidth = 1,
                    borderLeftColor = _borderColor,
                    borderLeftWidth = 1,
                    borderRightColor = _borderColor,
                    borderRightWidth = 1,
                    borderTopColor = _borderColor,
                    borderTopWidth = 1,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4,
                    paddingTop = 8,
                    paddingBottom = 8,
                    paddingLeft = 10,
                    paddingRight = 10,
                    marginBottom = 8,
                }
            };

            // Create section for primary alignment actions
            var primaryActionsLabel = new Label("Primary Alignment Actions")
            {
                style =
                {
                    fontSize = 14,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom = 6
                }
            };
            alignmentContainer.Add(primaryActionsLabel);

            // Create responsive button grid for primary actions
            var buttonGrid = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexWrap = Wrap.Wrap,
                    marginBottom = 12
                }
            };

            // Determine T-pose options based on current step
            var otherTPose = Step == EditorStep.MaxTPose ? "min" : "max";
            var tPoseOption = Step == EditorStep.MaxTPose ? SkeletonTPoseType.MinTPose : SkeletonTPoseType.MaxTPose;

            // Common button style setup
            void SetupButtonStyle(Button button)
            {
                button.style.flexGrow = 1;
                button.style.minWidth = 120;
                button.style.marginRight = 4;
                button.style.marginBottom = 4;
                button.style.height = 28;
            }

            // Create styled buttons with icons for primary actions
            var alignButton =
                CreateStyledMappingButton(PerformAutoAlignment, "Align with Skeleton", "Grid.MoveTool@2x");
            alignButton.tooltip = "Automatically align the target skeleton with the source skeleton";
            SetupButtonStyle(alignButton);

            var scaleButton = CreateStyledMappingButton(() =>
                {
                    var minHeight = 1.028f;
                    var maxHeight = 2.078f;
                    GetSkeletonTPose(_configHandle, SkeletonType.TargetSkeleton, tPoseOption,
                        JointRelativeSpaceType.RootOriginRelativeSpace, out var tPose);
                    ScalePoseToHeight(_configHandle, SkeletonType.TargetSkeleton,
                        JointRelativeSpaceType.RootOriginRelativeSpace,
                        tPoseOption == SkeletonTPoseType.MaxTPose ? minHeight : maxHeight, ref tPose);
                    _target.ReferencePose = tPose.ToArray();
                    _previewer.ReloadCharacter(_target);
                    SaveUpdateConfig(false);
                }, $"Scale from {otherTPose} T-Pose", "ScaleTool");
            scaleButton.tooltip = $"Scale the character using the {otherTPose} T-Pose as reference";
            SetupButtonStyle(scaleButton);

            var resetButton = CreateStyledMappingButton(() =>
            {
                GetSkeletonTPose(_configHandle, SkeletonType.TargetSkeleton, SkeletonTPoseType.UnscaledTPose,
                    JointRelativeSpaceType.RootOriginRelativeSpace, out var tPose);
                _target.ReferencePose = tPose.ToArray();
                _utilityConfig.SetTPose = true;
                _utilityConfig.RootScale = Vector3.one;
                _previewer.ReloadCharacter(_target);
            }, "Reset T-Pose", "Refresh");
            resetButton.tooltip = "Reset the character to its default T-Pose";
            SetupButtonStyle(resetButton);

            var updateButton = CreateStyledMappingButton(() =>
            {
                SaveUpdateConfig(false);
                _previewer.ReloadCharacter(_target);
            }, "Update Mapping", "Update-Available");
            updateButton.tooltip = "Update the joint mapping based on current pose";
            SetupButtonStyle(updateButton);
            updateButton.style.marginRight = 0; // Last button doesn't need right margin

            // Add buttons to grid
            buttonGrid.Add(alignButton);
            buttonGrid.Add(scaleButton);
            buttonGrid.Add(resetButton);
            buttonGrid.Add(updateButton);
            alignmentContainer.Add(buttonGrid);

            // Create section for fine-tuning
            var fineTuningLabel = new Label("Fine-Tuning")
            {
                style =
                {
                    fontSize = 14,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginTop = 4,
                    marginBottom = 4
                }
            };
            alignmentContainer.Add(fineTuningLabel);

            // Create responsive grid for fine-tuning buttons
            var fineTuningGrid = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexWrap = Wrap.Wrap,
                    marginBottom = 8
                }
            };

            // Create styled buttons for fine-tuning
            var wristsButton = CreateStyledMappingButton(PerformMatchWrists, "Align Wrists", "AvatarPivot@2x");
            wristsButton.tooltip = "Align the wrist joints between source and target skeletons";
            SetupButtonStyle(wristsButton);

            var fingersButton = CreateStyledMappingButton(PerformMatchFingers, "Align Fingers", "AvatarPivot@2x");
            fingersButton.tooltip = "Align the finger joints between source and target skeletons";
            SetupButtonStyle(fingersButton);
            fingersButton.style.marginRight = 0; // Last button doesn't need right margin

            // Add buttons to fine-tuning grid
            fineTuningGrid.Add(wristsButton);
            fineTuningGrid.Add(fingersButton);
            alignmentContainer.Add(fineTuningGrid);

            // Add help text
            var helpBox =
                new HelpBox("Use 'Align with Skeleton' first, then fine-tune with the matching tools if needed.",
                    HelpBoxMessageType.Info)
                {
                    style =
                    {
                        marginTop = 0
                    }
                };
            alignmentContainer.Add(helpBox);

            // Add the alignment container to the root
            root.Add(alignmentContainer);

            return root;
        }

        private Button CreateStyledMappingButton(Action callback, string text, string iconName)
        {
            var button = new Button(callback)
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    paddingTop = 4,
                    paddingBottom = 4,
                    paddingLeft = 8,
                    paddingRight = 8,
                    flexShrink = 1
                }
            };

            // Create a container for the icon and label to manage layout
            var contentContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    flexGrow = 1,
                    flexShrink = 1
                }
            };

            // Try to load the icon
            var icon = EditorGUIUtility.IconContent(iconName).image;
            if (icon != null)
            {
                var iconElement = new Image
                {
                    image = icon,
                    style =
                    {
                        width = 16,
                        height = 16,
                        marginRight = 4,
                        flexShrink = 0 // Prevent icon from shrinking
                    }
                };
                contentContainer.Add(iconElement);
            }

            var label = new Label(text)
            {
                style =
                {
                    unityTextAlign = TextAnchor.MiddleLeft,
                    fontSize = 11, // Slightly smaller font
                    flexShrink = 1, // Allow text to shrink if needed
                    flexWrap = Wrap.Wrap, // Allow text to wrap
                    whiteSpace = WhiteSpace.Normal // Allow text to wrap
                }
            };
            contentContainer.Add(label);
            button.Add(contentContainer);

            return button;
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
                        CreateConfig(this, false, data);
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
                var knownJointField = new ObjectField(i.ToString())
                {
                    style = { justifyContent = Justify.SpaceBetween },
                    value = knownJoints.GetArrayElementAtIndex(knownJointIndex).objectReferenceValue
                };

                knownJointField.RegisterValueChangedCallback(e =>
                {
                    if (e.previousValue != e.newValue)
                    {
                        knownJoints.GetArrayElementAtIndex(knownJointIndex).objectReferenceValue = e.newValue;
                        serializedTargetObject.ApplyModifiedProperties();
                        SaveUpdateConfig(false);
                    }
                });

                knownJointField.Bind(serializedTargetObject);
                knownJointsContainer.Add(knownJointField);
            }

            // Add elements to root
            root.Add(foldout);
            root.Add(CreateHeaderLabel("Known Joints", 12));
            root.Add(knownJointsContainer);

            // Add T-Pose button with improved styling
            var tPoseButton = new Button(() =>
            {
                JointAlignmentUtility.SetToTPose(_source, _target);
                JointAlignmentUtility.UpdateTPoseData(_target);
                _previewer.ReloadCharacter(_target);
            })
            {
                text = "Pose character to T-Pose",
                style =
                {
                    paddingTop = _buttonPadding,
                    paddingBottom = _buttonPadding,
                    backgroundColor = new Color(_primaryColor.r, _primaryColor.g, _primaryColor.b, 0.1f),
                    borderTopLeftRadius = 3,
                    borderTopRightRadius = 3,
                    borderBottomLeftRadius = 3,
                    borderBottomRightRadius = 3,
                    marginTop = 8,
                    marginBottom = 8
                }
            };
            root.Add(tPoseButton);

            // Add navigation buttons
            root.Add(PreviousNextSteps(true, false, false, EditorMetadataObject.ConfigJson != null));

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
            var validateButton = new Button(() =>
            {
                _validatedConfigFinish = true;
                SaveConfig(this, false);
                CreateGUI();
            })
            {
                text = "Validate and save config",
                style =
                {
                    paddingTop = _buttonPadding,
                    paddingBottom = _buttonPadding,
                    backgroundColor = new Color(0.1f, 0.6f, 0.3f, 0.7f), // Match Next/Done button color
                    color = Color.white,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    borderTopLeftRadius = 3,
                    borderTopRightRadius = 3,
                    borderBottomLeftRadius = 3,
                    borderBottomRightRadius = 3,
                    marginBottom = 8
                }
            };
            root.Add(validateButton);
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

            root.Add(CreateNavigationButton(lastStep ? "Done" : "Next", enableNext, true, () =>
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
                    SaveUpdateConfig(true);
                }

                ResetConfig(this, false);
                LoadConfig(this);
                _overlay.Reload();
            }

            if (lastStep)
            {
                ExitEditor();
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
            _fileReader.ClosePlaybackFile();
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
                    height = _buttonSizeHeight,
                    borderTopLeftRadius = 3,
                    borderTopRightRadius = 3,
                    borderBottomLeftRadius = 3,
                    borderBottomRightRadius = 3,
                    marginLeft = 4,
                    marginRight = 4
                }
            };

            // Apply special styling for "Next" or "Done" buttons
            if (text == "Next" || text == "Done")
            {
                button.style.backgroundColor = new Color(0.1f, 0.6f, 0.3f, 0.7f); // Distinct green color
                button.style.color = Color.white;
                button.style.unityFontStyleAndWeight = FontStyle.Bold;
            }

            button.SetEnabled(enabled);
            button.visible = visible;
            return button;
        }

        private Button CreateIconActionButton(string iconName, Action clickAction)
        {
            var button = new Button(clickAction)
            {
                style =
                {
                    width = 26,
                    height = 24,
                    marginLeft = 4,
                    paddingLeft = 4,
                    paddingRight = 4,
                    paddingTop = 2,
                    paddingBottom = 2,
                    flexShrink = 0,
                    flexGrow = 0,
                    backgroundColor = new Color(0, 0, 0, 0)
                }
            };

            button.Add(new Image
            {
                image = EditorGUIUtility.IconContent(iconName).image,
                style =
                {
                    width = 16,
                    height = 16,
                    flexShrink = 0,
                    flexGrow = 0
                }
            });

            return button;
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

        public void ModifyConfig(bool updateMappings, bool performAlignment)
        {
            UpdateConfig(this, false, updateMappings, performAlignment);
            EditorUtility.SetDirty(EditorMetadataObject);
            AssetDatabase.SaveAssets();
            CreateGUI();
        }

        public void SaveUpdateConfig(bool saveConfig)
        {
            UpdateConfig(this, saveConfig, false, false);
            EditorUtility.SetDirty(EditorMetadataObject);
            AssetDatabase.SaveAssets();
            CreateGUI();
        }

        private void RefreshConfig()
        {
            ResetConfig(this, true, EditorMetadataObject.ConfigJson.text);
            EditorUtility.SetDirty(EditorMetadataObject);
            AssetDatabase.SaveAssets();
            CreateGUI();
        }

        private void CreateNewConfig()
        {
            CreateConfig(this, true);
            EditorUtility.SetDirty(EditorMetadataObject);
            AssetDatabase.SaveAssets();
            CreateGUI();
        }

        private void PerformMatchWrists()
        {
            JointAlignmentUtility.PerformWristMatching(_source, _target);
            JointAlignmentUtility.PerformArmScaling(_source, _target);
            _previewer.ReloadCharacter(_target);
        }

        private void PerformMatchFingers()
        {
            JointAlignmentUtility.PerformFingerMatching(this, _leftStartBoneIds, _leftEndBoneIds,
                KnownJointType.LeftWrist);
            JointAlignmentUtility.PerformFingerMatching(this, _rightStartBoneIds, _rightEndBoneIds,
                KnownJointType.RightWrist);
            _previewer.ReloadCharacter(_target);
        }

        private void PerformAutoAlignment()
        {
            JointAlignmentUtility.AutoAlignment(this);
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

            if (_previewer.Retargeter != null || _fileReader.IsPlaying)
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
            if (EditorMetadataObject == null)
            {
                MSDKUtilityEditorMetadata.LoadConfigAsset(UtilityConfig.MetadataAssetPath,
                    ref _utilityConfig.EditorMetadataObject, ref _displayedConfig);
            }

            return EditorMetadataObject != null;
        }

        private void ValidateConfiguration()
        {
            if (EditorMetadataObject.ConfigJson == null || _target == null || _source == null)
            {
                return;
            }

            if (_target.ConfigHandle == INVALID_HANDLE)
            {
                LoadConfig(this);
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
            if (this == null)
            {
                return null;
            }

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
