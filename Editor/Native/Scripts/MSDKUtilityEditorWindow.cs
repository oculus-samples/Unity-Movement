// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Meta.XR.Movement.Recording;
using Unity.Collections;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Overlays;
using UnityEditor.SceneManagement;
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
        public EditorStep Step => _config.Step;

        /// <summary>
        /// Preview section.
        /// </summary>
        public MSDKUtilityEditorPreviewer Previewer => _previewer;

        public MSDKUtilityEditorMetadata EditorMetadataObject
        {
            get => _config.EditorMetadataObject;
            set => _config.EditorMetadataObject = value;
        }

        /// <summary>
        /// Reads and deserializes file for playing back sequences.
        /// </summary>
        public SequenceFileReader FileReader => _fileReader;

        /// <summary>
        /// Config instance.
        /// </summary>
        public MSDKUtilityEditorConfig Config => _config;

        /// <summary>
        /// Visual overlay.
        /// </summary>
        public MSDKUtilityEditorOverlay Overlay => _overlay;

        /// <summary>
        /// Current preview pose.
        /// </summary>
        public string CurrentPreviewPose => _currentPreviewPose;

        /// <summary>
        /// Gets whether playback should be restarted (used for UI state management).
        /// </summary>
        public bool ShouldRestartPlayback => _shouldRestartPlayback;

        /// <summary>
        /// Returns the selected joint.
        /// </summary>
        public string SelectedJointName =>
            _config.TargetSkeletonData?.Joints != null
                ? _config.TargetSkeletonData.Joints[SelectedIndex]
                : string.Empty;

        /// <summary>
        /// Returns the preview stage.
        /// </summary>
        public MSDKUtilityEditorStage PreviewStage
        {
            get => _previewStage;
            set => _previewStage = value;
        }

        /// <summary>
        /// Selected joint index.
        /// </summary>
        public int SelectedIndex
        {
            get
            {
                if (_config.SkeletonJoints == null || _config.SkeletonJoints.Length == 0)
                {
                    return -1;
                }

                for (var i = 0; i < _config.SkeletonJoints.Length; i++)
                {
                    if (Selection.activeTransform == _config.SkeletonJoints[i])
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
                if (SelectedIndex == -1 || _config.JointMappings.Length == 0)
                {
                    return null;
                }

                var allMappingsText = new StringBuilder();
                var entryIndex = 0;
                var currentBehavior = JointMappingBehaviorType.Invalid;

                for (var i = 0; i < _config.JointMappings.Length; i++)
                {
                    var jointMapping = _config.JointMappings[i];
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
                        startIndex += _config.JointMappings[j].EntriesCount;
                    }

                    // Collect all entries for this mapping
                    var entries = new List<(JointMappingEntry entry, string jointName, int originalIndex)>();
                    for (var k = 0; k < _config.JointMappings[i].EntriesCount; k++)
                    {
                        var entry = _config.JointMappingEntries[startIndex + k];
                        var jointName = _config.JointMappings[i].Type == SkeletonType.SourceSkeleton
                            ? _config.SourceSkeletonData?.Joints[entry.JointIndex]
                            : _config.TargetSkeletonData?.Joints[entry.JointIndex];

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

        /// <summary>
        /// Gets or sets the config bone transforms foldout state.
        /// </summary>
        public bool ConfigBoneTransformsFoldout
        {
            get => _configBoneTransformsFoldout;
            set => _configBoneTransformsFoldout = value;
        }

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
        public ulong ConfigHandle => _config?.ConfigHandle ?? INVALID_HANDLE;

        /// <summary>
        /// Gets or sets the custom data source path.
        /// </summary>
        public string CustomDataSourcePath { get; set; }

        // Serialized fields.
        [SerializeField]
        private UnityEditor.Editor _configEditor;

        [SerializeField]
        private UnityEditor.Editor _currentTransformEditor;

        [SerializeField]
        private MSDKUtilityEditorPreviewer _previewer;

        [SerializeField]
        private MSDKUtilityEditorConfig _config;

        [SerializeField]
        private MSDKUtilityEditorStage _previewStage;

        // Private fields.
        private GameObject _sceneViewCharacter
        {
            get => _previewer.SceneViewCharacter;
            set => _previewer.SceneViewCharacter = value;
        }

        // Editor components.
        private MSDKUtilityEditorOverlay _overlay;
        private MSDKUtilityEditorPlaybackUI _playbackUI;
        private SequenceFileReader _fileReader;
        private string _currentPreviewPose;

        // Playback restart functionality
        private bool _shouldRestartPlayback;
        private string _restartPlaybackPose;
        private int _restartPlaybackSnapshotIndex;

        // Editor settings.
        private bool _debugging;
        private bool _currentTransformEditorFoldout = true;
        private bool _configBoneTransformsFoldout;
        private bool _validatedConfigFinish;
        private bool _initialized;
        private Vector2 _scrollPosition;


        /**********************************************************
         *
         *               Unity Functions
         *
         **********************************************************/

        /// <summary>
        /// Handles script reloading by reinitializing the entire window using the config scriptable object.
        /// </summary>
        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            var existingWindows = Resources.FindObjectsOfTypeAll<MSDKUtilityEditorWindow>();

            foreach (var window in existingWindows)
            {
                if (window != null)
                {
                    try
                    {
                        // Reinitialize the entire window using the config scriptable object
                        window.EnsureBasicObjectsInitialized();

                        if (window.EnsureMetadataLoaded())
                        {
                            LoadConfig(window);
                            window.Init();
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning(
                            $"Failed to reinitialize MSDKUtilityEditorWindow after script reload: {e.Message}");
                        // If reinitalization fails, close the window to prevent broken UI state
                        window.Close();
                    }
                }
            }
        }

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
            EnsureOverlayInitialized();
            _previewer.InitializeSkeletonDraws();
            _initialized = true;
            OnUndoRedoPerformed();
        }

        /// <summary>
        /// Ensures the overlay is properly initialized and added to the scene view.
        /// If the overlay is null, it will be created. This method avoids unnecessary recreation
        /// to preserve UI state and settings.
        /// </summary>
        private void EnsureOverlayInitialized()
        {
            try
            {
                // Only create overlay if it doesn't exist
                if (_overlay == null)
                {
                    _overlay = new MSDKUtilityEditorOverlay(this)
                    {
                        displayed = true,
                        collapsed = false
                    };
                    SceneView.AddOverlayToActiveView(_overlay);
                }
                // If overlay exists but is not displayed, just show it without recreating
                else if (!_overlay.displayed)
                {
                    _overlay.displayed = true;
                    _overlay.collapsed = false;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to initialize overlay: {e.Message}. Proceeding with default settings.");
                // Set overlay to null so that UpdateSkeletonDraws will use default settings
                _overlay = null;
            }
        }

        private void Update()
        {
            // Handle playback restart if needed
            if (_shouldRestartPlayback && !string.IsNullOrEmpty(_restartPlaybackPose))
            {
                RestartPlaybackFromStoredState();
            }

            // Handle sequence playback with improved timing
            if (_fileReader is { IsPlaying: true })
            {
                _fileReader.PlayNextFrame();
            }

            _playbackUI?.Update();
        }

        private void OnDestroy()
        {
            foreach (var handle in _config.Handles)
            {
                DestroyHandle(handle);
            }

            if (_currentTransformEditor != null)
            {
                DestroyImmediate(_currentTransformEditor);
            }

            if (_overlay != null)
            {
                SceneView.RemoveOverlayFromActiveView(_overlay);
            }

            DestroyImmediate(_config);
            DestroyImmediate(_previewer);
        }

        private void OnEnable()
        {
            EnsureBasicObjectsInitialized();
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
            if (EditorMetadataObject != null)
            {
                CreateGUI();
            }
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (_sceneViewCharacter == null || !_config.DrawLines)
            {
                return;
            }

            if (_previewer.TargetSkeletonDrawTPose == null ||
                _previewer.SourceSkeletonDrawTPose == null ||
                _config == null)
            {
                return;
            }

            // Ensure overlay is initialized if it's null
            if (_initialized && _overlay is not { displayed: true })
            {
                EnsureOverlayInitialized();
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

            _previewer.UpdateTargetDraw(_config);
            _config.SetTPose = false;

            return modifications;
        }

        private void OnUndoRedoPerformed()
        {
            // Store playback state before stopping to enable restart
            StorePlaybackStateForRestart();

            try
            {
                // Clear the current playback UI reference to ensure it gets recreated
                ClearPlaybackUI();

                // Force a complete reload of the character to prevent AABB errors
                if (_previewer != null && _config.SkeletonJoints != null)
                {
                    _previewer.UpdateTargetDraw(_config);

                    // If we have a valid target config, reload the character completely
                    if (ConfigHandle != INVALID_HANDLE)
                    {
                        _previewer.ReloadCharacter(_config);
                    }

                    // Ensure skeleton draws are properly initialized after undo
                    _previewer.EnsureSkeletonDrawsInitialized();
                }

                // Recreate the GUI to ensure playback UI is properly restored
                CreateGUI();

                // Force a repaint to update the scene view
                SceneView.RepaintAll();
                Repaint();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Error handling undo/redo operation: {e.Message}");

                // If there's an error, try to reinitialize the previewer
                try
                {
                    _previewer.DestroyPreviewCharacterRetargeter();
                    _previewer.InitializeSkeletonDraws();

                    // Reset to T-pose as a fallback to ensure valid state
                    if (ConfigHandle != INVALID_HANDLE)
                    {
                        ResetToTPose();
                    }

                    // Still try to recreate the GUI even after an error
                    CreateGUI();
                }
                catch (Exception reinitException)
                {
                    Debug.LogError($"Failed to reinitialize previewer after undo error: {reinitException.Message}");
                }
            }
        }

        /// <summary>
        /// Ensures that basic objects (_config, _previewer, _fileReader) are properly initialized.
        /// </summary>
        private void EnsureBasicObjectsInitialized()
        {
            if (_config == null)
            {
                _config = CreateInstance<MSDKUtilityEditorConfig>();
            }

            if (_previewer == null)
            {
                _previewer = CreateInstance<MSDKUtilityEditorPreviewer>();
                _previewer.Window = this;
            }

            _fileReader ??= new SequenceFileReader();
        }

        /**********************************************************
         *
         *               UI Element Sections
         *
         **********************************************************/

        private void CreateGUI()
        {
            if (!_initialized || !EnsureMetadataLoaded())
            {
                return;
            }

            if (ConfigHandle == INVALID_HANDLE)
            {
                LoadConfig(this);
            }

            _previewer.AssociateSceneCharacter(_config);
            _previewer.UpdateSourceDraw(_config);
            CreateRootGUI();
        }

        /// <summary>
        /// Opens file for playback.
        /// </summary>
        /// <param name="poseName">Playback file name.</param>
        public void OpenPlaybackFile(string poseName)
        {
            _fileReader.Init(ConfigHandle);
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
                element.RegisterCallback<MouseDownEvent>(evt => { _config.CurrentlyEditing = true; });
                element.RegisterCallback<MouseUpEvent>(evt => { _config.CurrentlyEditing = false; });
            }

            // Add hover effects to buttons using the factory method
            var allButtons = rootVisualElement.Query<Button>().ToList();
            ApplyButtonHoverEffects(allButtons);
        }

        private VisualElement RenderEditorSteps()
        {
            return CreateEditorStepSection(this);
        }

        private VisualElement RetargetingSetupSection()
        {
            return CreateRetargetingSetupSection(this);
        }

        /**********************************************************
         *
         *               Editor Steps
         *
         **********************************************************/

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

            var knownRoot = _config.KnownSkeletonJoints[(int)KnownJointType.Root];

            // Always read source information from the config file using the MSDKUtility API
            // instead of searching through a data path for an asset
            var sourceData = CreateFromConfig(EditorMetadataObject.ConfigJson.text, SkeletonType.SourceSkeleton);

            if (sourceData == null)
            {
                Debug.LogError("Failed to extract source skeleton data from config JSON. Cannot remove joint.");
                return;
            }

            var targetData = CreateFromTransform(knownRoot == null ? _sceneViewCharacter.transform : knownRoot);
            targetData.FilterJoints(_config.TargetSkeletonData.Joints);
            targetData.RemoveJoint(jointName);
            CreateConfig(this, false, sourceData, targetData);
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

            // Cancel button pressed - do nothing
            if (choice == 2)
            {
                return;
            }

            // Proceed with step transition only if Yes or No was selected
            if (choice is 0 or 1)
            {
                _validatedConfigFinish = false;
                _previewer.DestroyPreviewCharacterRetargeter();

                // Only save and update configuration if user pressed "Yes"
                if (choice == 0)
                {
                    SaveUpdateConfig(true);
                }

                // Go into the next/previous step
                if (next)
                {
                    _config.Step++;
                }
                else
                {
                    _config.Step--;
                }

                // Load config without changes if user pressed "No"
                ResetConfig(this, false, EditorMetadataObject.ConfigJson.text);
                LoadConfig(this);

                // Update character from the loaded configuration
                _previewer.AssociateSceneCharacter(_config);
                _previewer.UpdateSourceDraw(_config);
                _previewer.UpdateTargetDraw(_config);
                _previewer.ReloadCharacter(_config);

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
            _config.CurrentPose = tPose.ToArray();
            _previewer.ReloadCharacter(_config);
            SaveUpdateConfig(false);
        }

        /// <summary>
        /// Resets the character to T-pose.
        /// </summary>
        public void ResetToTPose()
        {
            GetSkeletonTPose(ConfigHandle, SkeletonType.TargetSkeleton, SkeletonTPoseType.UnscaledTPose,
                JointRelativeSpaceType.RootOriginRelativeSpace, out var tPose);
            _config.CurrentPose = tPose.ToArray();
            _config.SetTPose = true;
            _config.RootScale = Vector3.one;
            _previewer.ReloadCharacter(_config);
        }

        /// <summary>
        /// Updates the mapping and reloads the character.
        /// </summary>
        public void UpdateMapping()
        {
            bool? twistJointOverride = null;

            // Show prompt for twist joint mapping in min/max t-pose steps
            if (Step == MSDKUtilityEditorConfig.EditorStep.MinTPose ||
                Step == MSDKUtilityEditorConfig.EditorStep.MaxTPose)
            {
                int choice = EditorUtility.DisplayDialogComplex(
                    "Update Mapping",
                    "Do you want to map twist joints?",
                    "Yes", "No", "Cancel");

                if (choice == 2) // Cancel
                {
                    return;
                }

                twistJointOverride = choice == 0; // Yes = true, No = false
            }

            SaveUpdateConfig(false, twistJointOverride);
            _previewer.ReloadCharacter(_config);
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
            PerformActionWithPlaybackRestart(() =>
            {
                JointAlignmentUtility.AutoAlignment(this);
                // Apply scaling when align skeleton button is pressed
                JointAlignmentUtility.LoadScale(_config);
                _previewer.ReloadCharacter(_config);
            });
        }

        /// <summary>
        /// Performs wrist matching alignment.
        /// </summary>
        public void PerformMatchWrists()
        {
            PerformActionWithPlaybackRestart(() =>
            {
                JointAlignmentUtility.PerformWristMatching(_config);
                JointAlignmentUtility.PerformArmScaling(_config);
                _previewer.ReloadCharacter(_config);
            });
        }

        /// <summary>
        /// Performs finger matching alignment using native implementation.
        /// </summary>
        public void PerformMatchFingers()
        {
            PerformActionWithPlaybackRestart(() =>
            {
                // Use native finger alignment implementation
                AlignInputToSource(_config.ConfigName,
                    AlignmentFlags.HandAndFingerRotations |
                    AlignmentFlags.MatchHandAndFingerPoseWithDeformation,
                    new NativeArray<NativeTransform>(_config.CurrentPose, Allocator.Temp),
                    ConfigHandle,
                    SkeletonType.SourceSkeleton,
                    ConfigHandle,
                    out var newConfigHandle);

                // Update the config with the new handle
                _config.AddHandle(newConfigHandle);

                // Get the updated pose from the aligned skeleton
                GetSkeletonTPose(newConfigHandle, SkeletonType.TargetSkeleton, _config.SkeletonTPoseType,
                    JointRelativeSpaceType.RootOriginRelativeSpace, out var alignedPose);

                // Only update hand + finger joints (descendants of hand transforms) instead of copying entire pose
                var handTransforms = new HashSet<Transform>
                {
                    _config.KnownSkeletonJoints[(int)KnownJointType.LeftWrist],
                    _config.KnownSkeletonJoints[(int)KnownJointType.RightWrist]
                };

                // Helper function to check if a transform is a finger (descendant of hand but not the hand itself)
                bool IsHandJoint(Transform transform)
                {
                    var current = transform.parent;
                    while (current != null)
                    {
                        if (handTransforms.Contains(current))
                        {
                            return true; // This is a descendant of a hand (finger)
                        }

                        current = current.parent;
                    }

                    return false;
                }
                for (var i = 0; i < _config.SkeletonJoints.Length && i < alignedPose.Length; i++)
                {
                    if (_config.SkeletonJoints[i] != null && IsHandJoint(_config.SkeletonJoints[i]))
                    {
                        _config.CurrentPose[i] = alignedPose[i];
                    }
                }

                _previewer.ReloadCharacter(_config);
            });
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
            _previewer.UpdateTargetDraw(_config);
            _previewer.ReloadCharacter(_config);
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
                _config.KnownSkeletonJoints[(int)KnownJointType.LeftWrist],
                _config.KnownSkeletonJoints[(int)KnownJointType.RightWrist]
            };

            // Helper function to check if a transform is a descendant of any hand transform
            bool IsDescendantOfHand(Transform transform)
            {
                var current = transform;
                while (current != null)
                {
                    if (handTransforms.Contains(current))
                    {
                        return true;
                    }

                    current = current.parent;
                }

                return false;
            }

            foreach (var sourceTransform in sourceTransforms)
            {
                if (!sceneTransformMap.TryGetValue(sourceTransform.name, out var sceneTransform))
                {
                    continue;
                }

                // Copy local position, rotation and scale for all descendants of hand transforms (including hands themselves)
                if (IsDescendantOfHand(sceneTransform))
                {
                    sceneTransform.localPosition = sourceTransform.localPosition;
                    sceneTransform.localRotation = sourceTransform.localRotation;
                    sceneTransform.localScale = sourceTransform.localScale;
                }
            }

            // Update the target draw and reload the character
            _previewer.UpdateTargetDraw(_config);
            _previewer.ReloadCharacter(_config);
        }

        /// <summary>
        /// Sets the character to T-pose.
        /// </summary>
        public void SetToTPose()
        {
            // Align target to source, but only use the aligned unscaled T-Pose.
            AlignTargetToSource(_config.ConfigName,
                AlignmentFlags.All,
                ConfigHandle,
                SkeletonType.SourceSkeleton,
                ConfigHandle,
                out var newConfigHandle);
            GetSkeletonTPose(newConfigHandle, SkeletonType.TargetSkeleton, SkeletonTPoseType.UnscaledTPose,
                JointRelativeSpaceType.RootOriginRelativeSpace, out var tPose);
            tPose.CopyTo(_config.CurrentPose);
            _config.AddHandle(newConfigHandle);
            _previewer.ReloadCharacter(_config);
        }

        /**********************************************************
         *
         *               Utility functions
         *
         **********************************************************/

        public void ModifyConfig(bool updateMappings, bool performAlignment)
        {
            // When updating mappings through auto update, we need to respect the twist joint settings
            bool? twistJointOverride = null;
            if (updateMappings)
            {
                // Use the overlay's twist joint mapping setting, but only if auto-update is enabled
                twistJointOverride = Overlay?.ShouldMapTwistJoints;
            }

            UpdateConfig(this, false, updateMappings, performAlignment, twistJointOverride);
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

        public void SaveUpdateConfig(bool saveConfig, bool? twistJointOverride)
        {
            // When we have a twist joint override, we want to update mappings
            bool updateMappings = twistJointOverride.HasValue;
            UpdateConfig(this, saveConfig, updateMappings, false, twistJointOverride);
            EditorUtility.SetDirty(EditorMetadataObject);
            AssetDatabase.SaveAssets();
            CreateGUI();
        }

        private void RemoveExistingOverlays()
        {
            // Find and remove all existing MSDKUtilityEditorOverlay instances from all scene views
            var existingOverlays = new List<MSDKUtilityEditorOverlay>();

            foreach (SceneView sceneView in SceneView.sceneViews)
            {
                if (sceneView != null && sceneView.overlayCanvas != null)
                {
                    try
                    {
                        // Use reflection to access the internal 'overlays' field
                        var overlayCanvasType = sceneView.overlayCanvas.GetType();
                        var overlaysField = overlayCanvasType.GetField("overlays",
                            BindingFlags.NonPublic | BindingFlags.Instance);

                        if (overlaysField != null)
                        {
                            var overlays =
                                overlaysField.GetValue(sceneView.overlayCanvas) as System.Collections.IEnumerable;
                            if (overlays != null)
                            {
                                foreach (var overlay in overlays)
                                {
                                    if (overlay is MSDKUtilityEditorOverlay msdkOverlay)
                                    {
                                        existingOverlays.Add(msdkOverlay);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"Failed to access overlays via reflection: {e.Message}");
                    }
                }
            }

            // Remove all found overlays
            foreach (var overlay in existingOverlays)
            {
                try
                {
                    SceneView.RemoveOverlayFromActiveView(overlay);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to remove existing overlay: {e.Message}");
                }
            }
        }

        private void UpdateSkeletonDraws()
        {
            // If overlay is null, proceed with default settings (show both source and target)
            bool shouldDrawSource = _overlay?.ShouldDrawSource ?? true;
            bool shouldDrawTarget = _overlay?.ShouldDrawTarget ?? true;

            if (shouldDrawSource)
            {
                if (Step is > EditorStep.Configuration and < EditorStep.Review)
                {
                    _previewer.EnsureSkeletonDrawsInitialized();
                    _previewer.SourceSkeletonDrawTPose.Draw();
                }
            }

            if (shouldDrawTarget)
            {
                _previewer.EnsureSkeletonDrawsInitialized();
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
                _config.CurrentlyEditing = true;
            }
            else if (_config.CurrentlyEditing && Event.current.type == EventType.MouseUp)
            {
                _config.CurrentlyEditing = false;
                if (_config.SetTPose)
                {
                    _config.SetTPose = false;
                }
                // Removed automatic scaling - now only done manually via button or specific scenarios
            }
        }

        private bool EnsureMetadataLoaded()
        {
            if (EditorMetadataObject != null)
            {
                return EditorMetadataObject != null;
            }

            var originalAsset =
                AssetDatabase.LoadAssetAtPath(_config.MetadataAssetPath, typeof(GameObject)) as GameObject;
            _config.EditorMetadataObject =
                MSDKUtilityEditor.GetOrCreateMetadata(originalAsset, originalAsset, CustomDataSourcePath);

            return EditorMetadataObject != null;
        }

        // Debugging.
        private VisualElement DebugConfigSection()
        {
            return CreateDebugSection(this, _configEditor);
        }

        /**********************************************************
         *
         *               Playback Restart Functionality
         *
         **********************************************************/

        /// <summary>
        /// Stores the current playback state before stopping playback to enable restart.
        /// </summary>
        private void StorePlaybackStateForRestart()
        {
            // Only store state if playback is currently active
            if (_fileReader is { IsPlaying: true } && !string.IsNullOrEmpty(_currentPreviewPose))
            {
                _restartPlaybackPose = _currentPreviewPose;
                _restartPlaybackSnapshotIndex = _fileReader.SnapshotIndex;
                _shouldRestartPlayback = true;

                // Stop playback after storing state
                _fileReader.ClosePlaybackFile();
            }
        }

        /// <summary>
        /// Restarts playback from the stored state after undo/redo or alignment operations.
        /// </summary>
        private void RestartPlaybackFromStoredState()
        {
            try
            {
                // Clear the restart flag first to prevent infinite loops
                _shouldRestartPlayback = false;

                // Ensure we have valid restart data
                if (string.IsNullOrEmpty(_restartPlaybackPose))
                {
                    return;
                }

                // Restart the playback
                OpenPlaybackFile(_restartPlaybackPose);

                // Seek to the stored position if we have a valid snapshot index
                if (_restartPlaybackSnapshotIndex > 0 && _fileReader.HasOpenedFileForPlayback)
                {
                    _fileReader.Seek(_restartPlaybackSnapshotIndex);
                }

                // Ensure the playback UI is properly initialized after restart
                if (_playbackUI != null)
                {
                    // Force the playback UI to reinitialize its slider with the correct range
                    _playbackUI.StartPlayback();
                }

                // Clear the stored state
                _restartPlaybackPose = null;
                _restartPlaybackSnapshotIndex = 0;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to restart playback: {e.Message}");
                // Clear restart state on failure
                _shouldRestartPlayback = false;
                _restartPlaybackPose = null;
                _restartPlaybackSnapshotIndex = 0;
            }
        }

        /// <summary>
        /// Stores playback state and performs the specified action, then schedules playback restart.
        /// This is a helper method for alignment operations that should preserve playback.
        /// </summary>
        /// <param name="action">The action to perform that might affect the character.</param>
        private void PerformActionWithPlaybackRestart(Action action)
        {
            // Store current playback state
            StorePlaybackStateForRestart();

            try
            {
                // Perform the action
                action?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error performing action with playback restart: {e.Message}");
                // Clear restart state if action failed
                _shouldRestartPlayback = false;
                _restartPlaybackPose = null;
            }
        }
    }
}
