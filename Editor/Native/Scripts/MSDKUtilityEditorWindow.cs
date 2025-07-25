// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Meta.XR.Movement.Recording;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

using static Meta.XR.Movement.Editor.MSDKUtilityEditorConfig;
using static Meta.XR.Movement.Editor.MSDKUtilityEditorUIConstants;
using static Meta.XR.Movement.Editor.MSDKUtilityEditorUIFactory;
using static Meta.XR.Movement.Editor.MSDKUtilityEditorUISections;
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
        private GameObject _sceneViewCharacter
        {
            get => _previewer?.SceneViewCharacter;
            set => _previewer.SceneViewCharacter = value;
        }

        // Editor components.
        private MSDKUtilityEditorOverlay _overlay;
        private MSDKUtilityEditorPlaybackUI _playbackUI;
        private SequenceFileReader _fileReader;
        private string _currentPreviewPose;

        // Editor settings.
        private bool _debugging;
        private bool _currentTransformEditorFoldout = true;
        private bool _validatedConfigFinish;
        private bool _displayedConfig;
        private bool _initialized;
        private Vector2 _scrollPosition;

        /// <summary>
        /// Gets or sets the config bone transforms foldout state.
        /// </summary>
        public bool ConfigBoneTransformsFoldout
        {
            get => _configBoneTransformsFoldout;
            set => _configBoneTransformsFoldout = value;
        }
        private bool _configBoneTransformsFoldout;

        /// <summary>
        /// Gets the current progress text for the header.
        /// </summary>
        public string CurrentProgressText => $"{(int)Step + 1} / {(int)EditorStep.End}";

        /// <summary>
        /// Gets the current title based on the editor step.
        /// </summary>
        public string CurrentTitle
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

        /// <summary>
        /// Gets whether the configuration has been validated and finished.
        /// </summary>
        public bool ValidatedConfigFinish => _validatedConfigFinish;

        /// <summary>
        /// Gets the config handle for internal operations.
        /// </summary>
        public ulong ConfigHandle => _target?.ConfigHandle ?? INVALID_HANDLE;


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
                Path.Combine("Packages/com.meta.xr.sdk.movement/Editor/Native/Poses/Sequences", poseName)
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

        /// <summary>
        /// Sets the playback UI reference for updates.
        /// </summary>
        /// <param name="playbackUI">The playback UI instance.</param>
        public void SetPlaybackUI(MSDKUtilityEditorPlaybackUI playbackUI)
        {
            _playbackUI = playbackUI;
        }

        /// <summary>
        /// Clears the playback UI reference.
        /// </summary>
        public void ClearPlaybackUI()
        {
            _playbackUI = null;
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

            // Delay overlay and GUI initialization to avoid Unity's internal initialization issues
            if (_initialized)
            {
                EditorApplication.delayCall += () =>
                {
                    if (this != null) // Check if window still exists
                    {
                        ReinitializeAfterDomainReload();
                    }
                };
            }
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

        private void ReinitializeAfterDomainReload()
        {
            try
            {
                // Find existing overlay instead of creating a new one
                FindAndReinitializeOverlay();

                // Refresh GUI after domain reload
                CreateGUI();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to reinitialize MSDKUtilityEditorWindow after domain reload: {e.Message}");
            }
        }

        private void FindAndReinitializeOverlay()
        {
            // After domain reload, Unity's overlay system may have recreated overlays automatically
            // We need to find and reconnect to any existing overlay or create a new one

            // First, try to find if there's already an overlay instance that Unity created
            // We'll use a different approach - check if we can create a new overlay safely
            try
            {
                // Remove any existing overlays first to avoid duplicates
                RemoveExistingOverlays();

                // Create a new overlay - Unity's overlay system will handle the lifecycle
                _overlay = new MSDKUtilityEditorOverlay(this);
                SceneView.AddOverlayToActiveView(_overlay);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to reinitialize overlay: {e.Message}");
                // Set overlay to null so we don't try to use a broken reference
                _overlay = null;
            }
        }

        /**********************************************************
         *
         *               UI Element Sections
         *
         **********************************************************/

        private void CreateRootGUI()
        {
            // Get the root element
            var root = rootVisualElement;

            // Store current scroll position if there's a ScrollView
            var existingScrollView = root.Q<ScrollView>();
            if (existingScrollView != null)
            {
                _scrollPosition = existingScrollView.scrollOffset;
            }

            // Clear the root element
            root.Clear();

            // Apply global styles to the root element
            root.style.backgroundColor = BackgroundColor;

            // Create main scroll view with improved styling
            var scrollView = new ScrollView
            {
                horizontalScrollerVisibility = ScrollerVisibility.Hidden,
                style =
                {
                    paddingTop = CardPadding,
                    paddingBottom = Margins.SectionBottom,
                    paddingLeft = CardPadding + TinyPadding,
                    paddingRight = CardPadding + TinyPadding
                }
            };

            // Store reference to the scroll view to manage scroll position
            scrollView.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                // Restore scroll position after layout is complete
                if (_scrollPosition != Vector2.zero)
                {
                    scrollView.scrollOffset = _scrollPosition;
                }
            });

            // Create transform editor container
            var transformContainer = new IMGUIContainer(DrawTransformEditor)
            {
                style =
                {
                    marginTop = CardPadding,
                    paddingTop = CardPadding,
                    paddingBottom = CardPadding,
                    paddingLeft = CardPadding,
                    paddingRight = CardPadding,
                    borderTopWidth = 1,
                    borderTopColor = BorderColor
                }
            };

            // Add debug section if debugging is enabled
            if (_debugging)
            {
                var debugSection = DebugConfigSection();
                if (debugSection == null)
                {
                    return;
                }

                debugSection.style.marginBottom = DoubleLineSpace;
                scrollView.Add(debugSection);
            }

            // Create main content container with card-like styling
            var mainContentCard = CreateCardContainer();

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
                            editorStepsSection.style.borderBottomRightRadius = BorderRadius;
                editorStepsSection.style.paddingLeft = Margins.SectionBottom;
                editorStepsSection.style.paddingRight = Margins.SectionBottom;
                editorStepsSection.style.paddingTop = CardPadding;
                editorStepsSection.style.paddingBottom = CardPadding;
                editorStepsSection.style.marginBottom = CardPadding;
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

            // Add hover effects to buttons using the factory method
            var allButtons = rootVisualElement.Query<Button>().ToList();
            ApplyButtonHoverEffects(allButtons);
        }

        private VisualElement RetargetingSetupSection()
        {
            return CreateRetargetingSetupSection(this);
        }

        private VisualElement PreviewSection()
        {
            return CreatePreviewSection(this);
        }

        /// <summary>
        /// Opens a sequence for playback and refreshes the GUI.
        /// </summary>
        /// <param name="sequenceName">Name of the sequence to open.</param>
        public void OpenSequence(string sequenceName)
        {
            CreateGUI();
            OpenPlaybackFile(sequenceName);
            CreateGUI();
        }

        private VisualElement AlignmentSection()
        {
            return CreateAlignmentSection(this);
        }

        private Button CreateStyledMappingButton(Action callback, string text, string iconName)
        {
            var button = new Button(callback)
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    paddingTop = SmallPadding,
                    paddingBottom = SmallPadding,
                    paddingLeft = CardPadding,
                    paddingRight = CardPadding,
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
                        width = IconSize,
                        height = IconSize,
                        marginRight = SmallPadding,
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
                    fontSize = SmallFontSize,
                    flexShrink = 1,
                    flexWrap = Wrap.Wrap,
                    whiteSpace = WhiteSpace.Normal
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
            return CreateEditorStepSection(this);
        }

        /// <summary>
        /// Removes a joint from the configuration.
        /// </summary>
        /// <param name="jointName">Name of the joint to remove.</param>
        public void RemoveJoint(string jointName)
        {
            // Store current scroll position before removing the joint
            var scrollView = rootVisualElement.Q<ScrollView>();
            if (scrollView != null)
            {
                _scrollPosition = scrollView.scrollOffset;
            }

            var knownRoot = _target.KnownSkeletonJoints[(int)KnownJointType.Root];
            var data = MSDKUtilityEditor.CreateRetargetingData(knownRoot == null
                    ? _sceneViewCharacter.transform
                    : knownRoot,
                _target);
            data.RemoveJoint(jointName);
            CreateConfig(this, false, data);
            CreateGUI();
        }

        /// <summary>
        /// Validates and saves the configuration.
        /// </summary>
        public void ValidateAndSaveConfig()
        {
            _validatedConfigFinish = true;
            SaveConfig(this, false);
            CreateGUI();
        }

        /// <summary>
        /// Navigates to the previous step.
        /// </summary>
        /// <param name="lastStep">Whether this is the last step.</param>
        public void NavigatePrevious(bool lastStep)
        {
            SavePrompt(lastStep, false);
            CreateGUI();
        }

        /// <summary>
        /// Navigates to the next step.
        /// </summary>
        /// <param name="lastStep">Whether this is the last step.</param>
        public void NavigateNext(bool lastStep)
        {
            SavePrompt(lastStep, true);
            CreateGUI();
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
                _overlay?.Reload();
            }

            // Only exit the editor if we're on the last step AND moving forward (next)
            if (lastStep && next)
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

        /// <summary>
        /// Performs T-pose scaling from the specified T-pose type.
        /// </summary>
        /// <param name="tPoseOption">The T-pose type to scale from.</param>
        /// <param name="otherTPose">The name of the other T-pose for UI display.</param>
        public void PerformScaleFromTPose(SkeletonTPoseType tPoseOption, string otherTPose)
        {
            var minHeight = 1.028f;
            var maxHeight = 2.078f;
            GetSkeletonTPose(ConfigHandle, SkeletonType.TargetSkeleton, tPoseOption,
                JointRelativeSpaceType.RootOriginRelativeSpace, out var tPose);
            ScalePoseToHeight(ConfigHandle, SkeletonType.TargetSkeleton,
                JointRelativeSpaceType.RootOriginRelativeSpace,
                tPoseOption == SkeletonTPoseType.MaxTPose ? minHeight : maxHeight, ref tPose);
            _target.ReferencePose = tPose.ToArray();
            _previewer.ReloadCharacter(_target);
            SaveUpdateConfig(false);
        }

        /// <summary>
        /// Resets the character to T-pose.
        /// </summary>
        public void ResetToTPose()
        {
            GetSkeletonTPose(ConfigHandle, SkeletonType.TargetSkeleton, SkeletonTPoseType.UnscaledTPose,
                JointRelativeSpaceType.RootOriginRelativeSpace, out var tPose);
            _target.ReferencePose = tPose.ToArray();
            _utilityConfig.SetTPose = true;
            _utilityConfig.RootScale = Vector3.one;
            _previewer.ReloadCharacter(_target);
        }

        /// <summary>
        /// Updates the mapping and reloads the character.
        /// </summary>
        public void UpdateMapping()
        {
            SaveUpdateConfig(false);
            _previewer.ReloadCharacter(_target);
        }

        /// <summary>
        /// Refreshes the configuration from the current config JSON.
        /// </summary>
        public void RefreshConfig()
        {
            ResetConfig(this, true, EditorMetadataObject.ConfigJson.text);
            EditorUtility.SetDirty(EditorMetadataObject);
            AssetDatabase.SaveAssets();
            CreateGUI();
        }

        /// <summary>
        /// Creates a new configuration.
        /// </summary>
        public void CreateNewConfig()
        {
            CreateConfig(this, true);
            EditorUtility.SetDirty(EditorMetadataObject);
            AssetDatabase.SaveAssets();
            CreateGUI();
        }

        /// <summary>
        /// Performs automatic alignment between source and target skeletons.
        /// </summary>
        public void PerformAutoAlignment()
        {
            JointAlignmentUtility.AutoAlignment(this);
            _previewer.ReloadCharacter(_target);
        }

        /// <summary>
        /// Performs wrist matching alignment.
        /// </summary>
        public void PerformMatchWrists()
        {
            JointAlignmentUtility.PerformWristMatching(_source, _target);
            JointAlignmentUtility.PerformArmScaling(_source, _target);
            _previewer.ReloadCharacter(_target);
        }

        /// <summary>
        /// Performs finger matching alignment.
        /// </summary>
        public void PerformMatchFingers()
        {
            JointAlignmentUtility.PerformFingerMatching(this, _leftStartBoneIds, _leftEndBoneIds,
                KnownJointType.LeftWrist);
            JointAlignmentUtility.PerformFingerMatching(this, _rightStartBoneIds, _rightEndBoneIds,
                KnownJointType.RightWrist);
            _previewer.ReloadCharacter(_target);
        }

        /// <summary>
        /// Loads the original source pose from the prefab asset.
        /// </summary>
        public void LoadOriginalSourcePose()
        {
            // Get the prefab asset that the scene character originates from
            var sourceAsset = EditorMetadataObject.Model;
            if (sourceAsset == null)
            {
                EditorUtility.DisplayDialog("Error", "This character doesn't have a source prefab asset.", "OK");
                return;
            }

            // Get all transforms from both the source asset and the scene character
            var sourceTransforms = sourceAsset.GetComponentsInChildren<Transform>();
            var sceneTransforms = _sceneViewCharacter.GetComponentsInChildren<Transform>();

            // Create a dictionary to quickly look up scene transforms by name
            var sceneTransformMap = new Dictionary<string, Transform>();
            foreach (var transform in sceneTransforms)
            {
                sceneTransformMap[transform.name] = transform;
            }

            // Copy transform data from source to scene character
            foreach (var t in sceneTransforms)
            {
                Undo.RecordObject(t, "Load Original Source Pose");
            }

            foreach (var sourceTransform in sourceTransforms)
            {
                if (!sceneTransformMap.TryGetValue(sourceTransform.name, out var sceneTransform))
                {
                    continue;
                }

                // Copy local position, rotation and scale
                sceneTransform.localPosition = sourceTransform.localPosition;
                sceneTransform.localRotation = sourceTransform.localRotation;
                sceneTransform.localScale = sourceTransform.localScale;
            }

            // Update the target draw and reload the character
            _previewer.UpdateTargetDraw(this);
            _previewer.ReloadCharacter(_target);
        }

        /// <summary>
        /// Loads the original hands from the prefab asset.
        /// </summary>
        public void LoadOriginalHands()
        {
            // Get the prefab asset that the scene character originates from
            var sourceAsset = EditorMetadataObject.Model;
            if (sourceAsset == null)
            {
                EditorUtility.DisplayDialog("Error", "This character doesn't have a source prefab asset.", "OK");
                return;
            }

            // Get all transforms from both the source asset and the scene character
            var sourceTransforms = sourceAsset.GetComponentsInChildren<Transform>();
            var sceneTransforms = _sceneViewCharacter.GetComponentsInChildren<Transform>();

            // Create a dictionary to quickly look up scene transforms by name
            var sceneTransformMap = new Dictionary<string, Transform>();
            foreach (var transform in sceneTransforms)
            {
                sceneTransformMap[transform.name] = transform;
            }

            // Copy transform data from source to scene character
            foreach (var t in sceneTransforms)
            {
                Undo.RecordObject(t, "Load Original Hands");
            }

            var handTransforms = new HashSet<Transform>
            {
                _target.KnownSkeletonJoints[(int)KnownJointType.LeftWrist],
                _target.KnownSkeletonJoints[(int)KnownJointType.RightWrist]
            };
            foreach (var sourceTransform in sourceTransforms)
            {
                if (!sceneTransformMap.TryGetValue(sourceTransform.name, out var sceneTransform))
                {
                    continue;
                }

                // Copy local position, rotation and scale
                var parent = sceneTransform.parent;
                if (handTransforms.Contains(parent))
                {
                    sceneTransform.localPosition = sourceTransform.localPosition;
                    sceneTransform.localRotation = sourceTransform.localRotation;
                    sceneTransform.localScale = sourceTransform.localScale;
                    handTransforms.Add(parent);
                }
            }

            // Update the target draw and reload the character
            _previewer.UpdateTargetDraw(this);
            _previewer.ReloadCharacter(_target);
        }

        /// <summary>
        /// Sets the character to T-pose.
        /// </summary>
        public void SetToTPose()
        {
            // Align target to source, which will update the T-Pose.
            AlignTargetToSource(_source.ConfigName,
                AlignmentFlags.All,
                ConfigHandle,
                SkeletonType.SourceSkeleton,
                ConfigHandle,
                out var newConfigHandle);
            GetSkeletonTPose(newConfigHandle, SkeletonType.TargetSkeleton, SkeletonTPoseType.UnscaledTPose,
                JointRelativeSpaceType.RootOriginRelativeSpace, out var tPose);
            tPose.CopyTo(_target.ReferencePose);
            _utilityConfig.Handles.Add(newConfigHandle);
            _previewer.ReloadCharacter(_target);
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

            Validate(_previewer, _previewer.TargetSkeletonDrawTPose, _target);

            if (Step >= EditorStep.MinTPose)
            {
                Validate(_previewer, _previewer.SourceSkeletonDrawTPose, _source);
            }
        }

        // Debugging.
        private VisualElement DebugConfigSection()
        {
            return CreateDebugSection(this, _sourceEditor, _targetEditor);
        }
    }
}
