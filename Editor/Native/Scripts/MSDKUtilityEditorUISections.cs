// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

using static Meta.XR.Movement.Editor.MSDKUtilityEditorUIConstants;
using static Meta.XR.Movement.Editor.MSDKUtilityEditorUIFactory;
using static Meta.XR.Movement.MSDKUtility;
using static Meta.XR.Movement.Retargeting.SkeletonData;

namespace Meta.XR.Movement.Editor
{
    /// <summary>
    /// Specialized UI section builders for the MSDK Utility Editor.
    /// Separates UI creation logic into focused, reusable components.
    /// </summary>
    public static class MSDKUtilityEditorUISections
    {
        #region Retargeting Setup Section

        /// <summary>
        /// Creates the retargeting setup section with character and config controls.
        /// </summary>
        public static VisualElement CreateRetargetingSetupSection(MSDKUtilityEditorWindow window)
        {
            if (window.Previewer == null)
            {
                return null;
            }

            var serializedPreviewerObject = new SerializedObject(window.Previewer);
            var root = CreateStandardContainer();

            // Create header
            var headerContainer = CreateHeaderContainer("Retargeting Setup", window.CurrentProgressText);
            root.Add(headerContainer);

            // Add title
            var titleContainer = new VisualElement
            {
                style = { marginBottom = Margins.HeaderBottom, paddingLeft = SmallPadding }
            };
            titleContainer.Add(CreateHeaderLabel(window.CurrentTitle, HeaderFontSize));
            root.Add(titleContainer);

            // Add scene view character field
            var characterFieldContainer = new VisualElement
            {
                style = { marginBottom = CardPadding, paddingLeft = SmallPadding, paddingRight = SmallPadding }
            };
            characterFieldContainer.Add(CreatePropertyField(
                serializedPreviewerObject,
                "_sceneViewCharacter",
                "Scene View Character:",
                ButtonHeight));
            root.Add(characterFieldContainer);

            // Add config controls
            root.Add(CreateConfigControlsSection(window));

            return root;
        }

        private static VisualElement CreateConfigControlsSection(MSDKUtilityEditorWindow window)
        {
            var configOuterContainer = new VisualElement
            {
                style = { paddingLeft = SmallPadding, paddingRight = SmallPadding }
            };

            var configContainer = new VisualElement
            {
                style =
                {
                    height = SmallPadding,
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
                    marginTop = TinyPadding,
                    flexGrow = 1,
                    flexShrink = 1,
                    minWidth = NavigationButtonMinWidth,
                    height = HeaderFontSize,
                    alignContent = Align.Center
                },
                value = window.EditorMetadataObject.ConfigJson
            };

            configAssetField.RegisterValueChangedCallback(e =>
            {
                int choice = EditorUtility.DisplayDialogComplex("Change Config",
                    "Change configuration file?",
                    "Yes", "No", "Cancel");
                if (choice == 0)
                {
                    window.EditorMetadataObject.ConfigJson = configAssetField.value as TextAsset;
                    EditorUtility.SetDirty(window.EditorMetadataObject);
                    AssetDatabase.SaveAssets();
                }
                else
                {
                    configAssetField.SetValueWithoutNotify(window.EditorMetadataObject.ConfigJson);
                }
            });

            // Create action buttons
            var buttonContainer = CreateConfigActionButtons(window);

            configContainer.Add(configAssetField);
            configContainer.Add(buttonContainer);
            configOuterContainer.Add(configContainer);

            return configOuterContainer;
        }

        private static VisualElement CreateConfigActionButtons(MSDKUtilityEditorWindow window)
        {
            var buttonContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexShrink = 0,
                    flexGrow = 0,
                    marginLeft = CardPadding,
                    width = 90,
                    justifyContent = Justify.FlexEnd
                }
            };

            var saveButton = CreateIconActionButton("Save", () => window.SaveUpdateConfig(true));
            var refreshButton = CreateIconActionButton("Refresh", window.RefreshConfig);
            var createButton = CreateIconActionButton("CreateAddNew", window.CreateNewConfig);

            buttonContainer.Add(saveButton);
            buttonContainer.Add(refreshButton);
            buttonContainer.Add(createButton);

            return buttonContainer;
        }

        #endregion

        #region Preview Section

        /// <summary>
        /// Creates the preview section with sequence controls and scale adjustment.
        /// </summary>
        public static VisualElement CreatePreviewSection(MSDKUtilityEditorWindow window)
        {
            var root = CreateStandardContainer();
            var headerContainer = CreateHeaderContainer("Preview Sequences");
            root.Add(headerContainer);

            var previewContainer = CreateCardContainer();
            previewContainer.style.borderTopLeftRadius = 0;
            previewContainer.style.borderTopRightRadius = 0;

            // Add scale slider for review step
            if (window.Step == MSDKUtilityEditorConfig.EditorStep.Review)
            {
                previewContainer.Add(CreateScaleAdjustmentSection(window));
            }

            // Add sequences section
            previewContainer.Add(CreateSequencesSection(window));

            // Add help text
            var helpBox = new HelpBox("Select a sequence to preview character retargeting.",
                HelpBoxMessageType.Info)
            {
                style = { marginTop = 0 }
            };
            previewContainer.Add(helpBox);

            root.Add(previewContainer);

            // Add playback UI if needed
            if (window.FileReader is { HasOpenedFileForPlayback: true })
            {
                var playbackUI = new MSDKUtilityEditorPlaybackUI(window);
                playbackUI.Init();
                playbackUI.StartPlayback();
                root.Add(playbackUI);

                // Store reference in the window for updates
                window.SetPlaybackUI(playbackUI);
            }

            return root;
        }

        private static VisualElement CreateScaleAdjustmentSection(MSDKUtilityEditorWindow window)
        {
            var scaleSection = new VisualElement();

            var scaleLabel = CreateSectionHeaderLabel("Size Adjustment");
            scaleSection.Add(scaleLabel);

            var scaleOuterContainer = new VisualElement
            {
                style =
                {
                    width = Length.Percent(100),
                    marginBottom = Margins.SectionBottom,
                    marginLeft = CardPadding,
                    marginRight = CardPadding,
                    paddingLeft = CardPadding,
                    paddingRight = CardPadding,
                    overflow = Overflow.Hidden
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

            // Slider with constraints
            var scaleSlider = new Slider(0f, 100f)
            {
                style =
                {
                    flexGrow = 1,
                    flexShrink = 1,
                    minWidth = MinSliderWidth,
                    maxWidth = Length.Percent(MaxSliderWidthPercent)
                },
                value = window.UtilityConfig.ScaleSize
            };

            // Value field
            var scaleValue = new IntegerField
            {
                value = Mathf.RoundToInt(window.UtilityConfig.ScaleSize),
                style =
                {
                    width = ScaleFieldWidth,
                    minWidth = ScaleFieldWidth,
                    maxWidth = ScaleFieldWidth,
                    marginLeft = CardPadding,
                    flexShrink = 0,
                    unityTextAlign = TextAnchor.MiddleRight
                }
            };

            // Connect slider and field
            scaleSlider.RegisterValueChangedCallback(e =>
            {
                window.UtilityConfig.ScaleSize = e.newValue;
                scaleValue.value = Mathf.RoundToInt(e.newValue);
            });

            scaleValue.RegisterValueChangedCallback(e =>
            {
                scaleSlider.value = Mathf.Clamp(e.newValue, 0f, 100f);
            });

            scaleContainer.Add(scaleSlider);
            scaleContainer.Add(scaleValue);
            scaleOuterContainer.Add(scaleContainer);
            scaleSection.Add(scaleOuterContainer);

            return scaleSection;
        }

        private static VisualElement CreateSequencesSection(MSDKUtilityEditorWindow window)
        {
            var sequencesSection = new VisualElement();

            var sequencesLabel = CreateSectionHeaderLabel("Available Sequences");
            sequencesSection.Add(sequencesLabel);

            var sequenceGrid = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexWrap = Wrap.Wrap,
                    justifyContent = Justify.Center,
                    marginBottom = CardPadding
                }
            };

            // Dynamically load all sequence icons and create buttons
            var sequenceNames = GetAvailableSequenceNames();
            foreach (var sequenceName in sequenceNames)
            {
                sequenceGrid.Add(CreateSequenceButton(sequenceName,
                    LoadSequenceIcon(sequenceName),
                    () => window.OpenSequence(sequenceName)));
            }

            sequencesSection.Add(sequenceGrid);
            return sequencesSection;
        }

        #endregion

        #region Alignment Section

        /// <summary>
        /// Creates the alignment section with primary and fine-tuning controls.
        /// </summary>
        public static VisualElement CreateAlignmentSection(MSDKUtilityEditorWindow window)
        {
            var root = CreateStandardContainer();
            var headerContainer = CreateHeaderContainer("Alignment");
            root.Add(headerContainer);

            var alignmentContainer = CreateCardContainer();
            alignmentContainer.style.borderTopLeftRadius = 0;
            alignmentContainer.style.borderTopRightRadius = 0;

            // Primary alignment actions
            alignmentContainer.Add(CreatePrimaryAlignmentSection(window));

            // Fine-tuning section
            alignmentContainer.Add(CreateFineTuningSection(window));

            // Help text
            var helpBox = new HelpBox("Use 'Align with Skeleton' first, then fine-tune with the matching tools if needed.",
                HelpBoxMessageType.Info)
            {
                style = { marginTop = 0 }
            };
            alignmentContainer.Add(helpBox);

            root.Add(alignmentContainer);
            return root;
        }

        private static VisualElement CreatePrimaryAlignmentSection(MSDKUtilityEditorWindow window)
        {
            var primarySection = new VisualElement();

            var primaryActionsLabel = CreateSectionHeaderLabel("Primary Alignment Actions");
            primarySection.Add(primaryActionsLabel);

            var buttonGrid = CreateButtonGrid();

            // Determine T-pose options based on current step
            var otherTPose = window.Step == MSDKUtilityEditorConfig.EditorStep.MaxTPose ? "min" : "max";
            var tPoseOption = window.Step == MSDKUtilityEditorConfig.EditorStep.MaxTPose
                ? SkeletonTPoseType.MinTPose
                : SkeletonTPoseType.MaxTPose;

            // Create primary action buttons
            var alignButton = CreateStyledMappingButton(
                window.PerformAutoAlignment,
                "Align with Skeleton",
                "Grid.MoveTool@2x");
            alignButton.tooltip = "Automatically align the target skeleton with the source skeleton";
            ApplyMappingButtonStyle(alignButton);

            var scaleButton = CreateStyledMappingButton(() =>
            {
                window.PerformScaleFromTPose(tPoseOption, otherTPose);
            }, $"Scale from {otherTPose} T-Pose", "ScaleTool");
            scaleButton.tooltip = $"Scale the character using the {otherTPose} T-Pose as reference";
            ApplyMappingButtonStyle(scaleButton);

            var resetButton = CreateStyledMappingButton(
                window.ResetToTPose,
                "Reset T-Pose",
                "Refresh");
            resetButton.tooltip = "Reset the character to its default T-Pose";
            ApplyMappingButtonStyle(resetButton);

            var updateButton = CreateStyledMappingButton(
                window.UpdateMapping,
                "Update Mapping",
                "Update-Available");
            updateButton.tooltip = "Update the joint mapping based on current pose";
            ApplyMappingButtonStyle(updateButton);
            updateButton.style.marginRight = 0;

            buttonGrid.Add(alignButton);
            buttonGrid.Add(scaleButton);
            buttonGrid.Add(resetButton);
            buttonGrid.Add(updateButton);
            primarySection.Add(buttonGrid);

            return primarySection;
        }

        private static VisualElement CreateFineTuningSection(MSDKUtilityEditorWindow window)
        {
            var fineTuningSection = new VisualElement();

            var fineTuningLabel = CreateSectionHeaderLabel("Fine-Tuning");
            fineTuningLabel.style.marginTop = SmallPadding;
            fineTuningSection.Add(fineTuningLabel);

            var fineTuningGrid = CreateButtonGrid();

            var originalButton = CreateStyledMappingButton(
                window.LoadOriginalHands,
                "Load Wrists & Fingers",
                "AvatarPivot@2x");
            originalButton.tooltip = "Load the original target skeleton hands & fingers";
            ApplyMappingButtonStyle(originalButton);
            originalButton.style.marginRight = 0;

            var wristsButton = CreateStyledMappingButton(
                window.PerformMatchWrists,
                "Align Wrists",
                "AvatarPivot@2x");
            wristsButton.tooltip = "Align the wrist joints between source and target skeletons";
            ApplyMappingButtonStyle(wristsButton);

            var fingersButton = CreateStyledMappingButton(
                window.PerformMatchFingers,
                "Align Fingers",
                "AvatarPivot@2x");
            fingersButton.tooltip = "Align the finger joints between source and target skeletons";
            ApplyMappingButtonStyle(fingersButton);
            fingersButton.style.marginRight = 0;

            fineTuningGrid.Add(originalButton);
            fineTuningGrid.Add(wristsButton);
            fineTuningGrid.Add(fingersButton);
            fineTuningSection.Add(fineTuningGrid);

            return fineTuningSection;
        }

        #endregion

        #region Editor Steps

        /// <summary>
        /// Creates the appropriate editor step section based on the current step.
        /// </summary>
        public static VisualElement CreateEditorStepSection(MSDKUtilityEditorWindow window)
        {
            return window.Step switch
            {
                MSDKUtilityEditorConfig.EditorStep.Configuration => CreateConfigurationStep(window),
                MSDKUtilityEditorConfig.EditorStep.MinTPose => CreateTPoseStep(window),
                MSDKUtilityEditorConfig.EditorStep.MaxTPose => CreateTPoseStep(window),
                MSDKUtilityEditorConfig.EditorStep.Review => CreateReviewStep(window),
                _ => null
            };
        }

        private static VisualElement CreateConfigurationStep(MSDKUtilityEditorWindow window)
        {
            var root = CreateStandardContainer();
            var serializedTargetObject = new SerializedObject(window.TargetInfo);

            // Create bone transforms foldout
            root.Add(CreateBoneTransformsFoldout(window, serializedTargetObject));

            // Create known joints section
            root.Add(CreateKnownJointsSection(serializedTargetObject));

            // Add action buttons
            root.Add(CreateConfigurationActionButtons(window));

            // Add navigation
            root.Add(CreateNavigationSection(window, true, false, false,
                window.EditorMetadataObject.ConfigJson != null));

            return root;
        }

        private static VisualElement CreateTPoseStep(MSDKUtilityEditorWindow window)
        {
            var root = CreateStandardContainer();
            root.Add(CreatePreviewSection(window));
            root.Add(CreateAlignmentSection(window));
            root.Add(CreateNavigationSection(window, false, false, true, true));
            return root;
        }

        private static VisualElement CreateReviewStep(MSDKUtilityEditorWindow window)
        {
            var root = CreateStandardContainer();
            root.Add(CreatePreviewSection(window));

            var validateButton = new Button(() => window.ValidateAndSaveConfig())
            {
                text = "Validate and save config",
                style =
                {
                    paddingTop = ButtonPadding,
                    paddingBottom = ButtonPadding,
                    backgroundColor = SuccessButtonColor,
                    color = TextColor,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    borderTopLeftRadius = SmallBorderRadius,
                    borderTopRightRadius = SmallBorderRadius,
                    borderBottomLeftRadius = SmallBorderRadius,
                    borderBottomRightRadius = SmallBorderRadius,
                    marginBottom = CardPadding
                }
            };
            root.Add(validateButton);

            if (window.ValidatedConfigFinish)
            {
                root.Add(new HelpBox("No issues detected in mapping!", HelpBoxMessageType.Info));
            }

            root.Add(CreateNavigationSection(window, false, true, true, window.ValidatedConfigFinish));
            return root;
        }

        #endregion

        #region Helper Methods

        private static VisualElement CreateBoneTransformsFoldout(MSDKUtilityEditorWindow window,
            SerializedObject serializedTargetObject)
        {
            var foldout = new Foldout
            {
                text = "Config Bone Transforms",
                value = window.ConfigBoneTransformsFoldout,
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    paddingTop = SingleLineSpace,
                    paddingBottom = SingleLineSpace
                }
            };
            foldout.RegisterValueChangedCallback(val => window.ConfigBoneTransformsFoldout = val.newValue);

            // Add joint entries
            for (var i = 0; i < window.TargetInfo.SkeletonInfo.JointCount; i++)
            {
                foldout.Add(CreateJointEntry(window, serializedTargetObject, i));
            }

            return foldout;
        }

        private static VisualElement CreateJointEntry(MSDKUtilityEditorWindow window,
            SerializedObject serializedTargetObject, int index)
        {
            var entryContainer = CreateJointEntryContainer();
            var leftSideContainer = CreateLeftJointEntryContainer();
            var jointName = window.TargetInfo.JointNames[index];

            GetChildJointIndexes(window.ConfigHandle, SkeletonType.TargetSkeleton, index, out var childJointIndexes);
            var hasChildIndex = childJointIndexes.Length > 0;

            // Add remove button or spacer
            if (index > 0 && !hasChildIndex)
            {
                var removeJointButton = new Button(() => window.RemoveJoint(jointName))
                {
                    text = "-",
                    style = { width = ButtonHeight, height = ButtonHeight }
                };
                leftSideContainer.Add(removeJointButton);
            }
            else
            {
                leftSideContainer.Add(CreateSpacerLabel());
            }

            leftSideContainer.Add(CreateJointNameLabel(jointName));
            entryContainer.Add(leftSideContainer);
            entryContainer.Add(CreateBoneTransformField(serializedTargetObject, index));

            return entryContainer;
        }

        private static VisualElement CreateKnownJointsSection(SerializedObject serializedTargetObject)
        {
            var knownJointsContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    paddingTop = SingleLineSpace,
                    paddingBottom = DoubleLineSpace
                }
            };

            var headerLabel = CreateHeaderLabel("Known Joints", StandardFontSize);
            knownJointsContainer.Add(headerLabel);

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
                        // Note: SaveUpdateConfig call would need to be passed as callback
                    }
                });

                knownJointField.Bind(serializedTargetObject);
                knownJointsContainer.Add(knownJointField);
            }

            return knownJointsContainer;
        }

        private static VisualElement CreateConfigurationActionButtons(MSDKUtilityEditorWindow window)
        {
            var buttonContainer = new VisualElement();

            var originalPoseButton = new Button(window.LoadOriginalSourcePose)
            {
                text = "Load original T-Pose",
                style =
                {
                    paddingTop = ButtonPadding,
                    paddingBottom = ButtonPadding,
                    backgroundColor = PrimaryColorVeryLight,
                    borderTopLeftRadius = SmallBorderRadius,
                    borderTopRightRadius = SmallBorderRadius,
                    borderBottomLeftRadius = SmallBorderRadius,
                    borderBottomRightRadius = SmallBorderRadius,
                    marginTop = CardPadding,
                    marginBottom = SmallPadding
                }
            };

            var originalHandsButton = new Button(window.LoadOriginalHands)
            {
                text = "Load original hands",
                style =
                {
                    paddingTop = ButtonPadding,
                    paddingBottom = ButtonPadding,
                    backgroundColor = PrimaryColorVeryLight,
                    borderTopLeftRadius = SmallBorderRadius,
                    borderTopRightRadius = SmallBorderRadius,
                    borderBottomLeftRadius = SmallBorderRadius,
                    borderBottomRightRadius = SmallBorderRadius,
                    marginTop = SmallPadding,
                    marginBottom = SmallPadding
                }
            };

            var tPoseButton = new Button(window.SetToTPose)
            {
                text = "Pose character to T-Pose",
                style =
                {
                    paddingTop = ButtonPadding,
                    paddingBottom = ButtonPadding,
                    backgroundColor = PrimaryColorVeryLight,
                    borderTopLeftRadius = SmallBorderRadius,
                    borderTopRightRadius = SmallBorderRadius,
                    borderBottomLeftRadius = SmallBorderRadius,
                    borderBottomRightRadius = SmallBorderRadius,
                    marginTop = SmallPadding,
                    marginBottom = SmallPadding
                }
            };

            buttonContainer.Add(originalPoseButton);
            buttonContainer.Add(originalHandsButton);
            buttonContainer.Add(tPoseButton);

            return buttonContainer;
        }

        private static VisualElement CreateNavigationSection(MSDKUtilityEditorWindow window,
            bool firstStep, bool lastStep, bool enablePrevious, bool enableNext)
        {
            var root = CreateStandardContainer();
            root.style.flexDirection = FlexDirection.Row;
            root.style.justifyContent = Justify.SpaceBetween;

            if (!window.TargetInfo.IsValid())
            {
                root.Add(new HelpBox("Configuration is invalid! Please fix any missing fields! " +
                                     "The configuration can also be refreshed into a valid configuration by " +
                                     "using the buttons past the config asset field.", HelpBoxMessageType.Error));
            }

            root.Add(CreateNavigationButton("Previous", enablePrevious, enablePrevious, () =>
            {
                window.NavigatePrevious(lastStep);
            }));

            root.Add(CreateNavigationButton(lastStep ? "Done" : "Next", enableNext, true, () =>
            {
                window.NavigateNext(lastStep);
            }));

            return root;
        }

        private static Texture2D LoadSequenceIcon(string sequenceName)
        {
            var iconPath = $"Packages/com.meta.xr.sdk.movement/Editor/Native/Poses/Icons/{sequenceName}.png";
            return AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
        }

        private static string[] GetAvailableSequenceNames()
        {
            var iconsFolderPath = "Packages/com.meta.xr.sdk.movement/Editor/Native/Poses/Icons";
            var sequenceNames = new System.Collections.Generic.List<string>();

            // Find all PNG files in the icons folder
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { iconsFolderPath });

            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (assetPath.EndsWith(".png"))
                {
                    var fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                    sequenceNames.Add(fileName);
                }
            }

            // Sort alphabetically for consistent ordering
            sequenceNames.Sort();
            return sequenceNames.ToArray();
        }
        #endregion

        #region Debug Section

        /// <summary>
        /// Creates a debug section for development purposes.
        /// </summary>
        public static VisualElement CreateDebugSection(MSDKUtilityEditorWindow window,
            UnityEditor.Editor sourceEditor, UnityEditor.Editor targetEditor)
        {
            if (window == null)
            {
                return null;
            }

            // Create serialized object and root container
            var serializedObject = new SerializedObject(window);
            var root = CreateStandardContainer();

            // Create main debug foldout
            var foldout = new Foldout { text = "DEBUG", value = false };

            // Setup editors for source and target properties
            var sourceProperty = serializedObject.FindProperty("_source");
            var targetProperty = serializedObject.FindProperty("_target");
            UnityEditor.Editor.CreateCachedEditor(sourceProperty.objectReferenceValue, null, ref sourceEditor);
            UnityEditor.Editor.CreateCachedEditor(targetProperty.objectReferenceValue, null, ref targetEditor);

            // Create nested container with source and target foldouts
            var nestedContainer = new VisualElement { style = { marginLeft = 15 } };

            // Add config foldouts
            nestedContainer.Add(CreateConfigFoldout("Source Config Info", sourceEditor, serializedObject));
            nestedContainer.Add(CreateConfigFoldout("Target Config Info", targetEditor, serializedObject));

            // Assemble UI hierarchy
            foldout.Add(nestedContainer);
            root.Add(foldout);

            return root;
        }

        private static Foldout CreateConfigFoldout(string configTitle, UnityEditor.Editor editor,
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

        #endregion
    }
}
