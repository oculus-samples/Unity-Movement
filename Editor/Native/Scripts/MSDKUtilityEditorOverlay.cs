// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;
using static Meta.XR.Movement.Editor.MSDKUtilityEditorUIConstants;
using static Meta.XR.Movement.Editor.MSDKUtilityEditorUIFactory;

namespace Meta.XR.Movement.Editor
{
    /// <summary>
    /// Overlay for the Movement SDK utility editor in the scene view.
    /// </summary>
    [Overlay(typeof(SceneView), "NativeUtilityEditorOverlay",
        "Retargeting Tool", defaultDisplay = true, defaultDockPosition = DockPosition.Bottom), Serializable]
    public class MSDKUtilityEditorOverlay : Overlay
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MSDKUtilityEditorOverlay"/> class with the specified editor window.
        /// </summary>
        /// <param name="win">The editor window.</param>
        public MSDKUtilityEditorOverlay(MSDKUtilityEditorWindow win) => _window = win;

        /// <summary>
        /// Initializes a new instance of the <see cref="MSDKUtilityEditorOverlay"/> class.
        /// </summary>
        public MSDKUtilityEditorOverlay()
        {
        }

        /// <summary>
        /// Gets whether the source skeleton should be drawn.
        /// </summary>
        public bool ShouldDrawSource => _drawSourceCheckbox?.value ?? _drawSourceValue;

        /// <summary>
        /// Gets whether the target skeleton should be drawn.
        /// </summary>
        public bool ShouldDrawTarget => _drawTargetCheckbox?.value ?? _drawTargetValue;

        /// <summary>
        /// Gets whether the preview skeleton should be drawn.
        /// </summary>
        public bool ShouldDrawPreview => _drawPreviewCheckbox?.value ?? _drawPreviewValue;

        /// <summary>
        /// Gets whether mappings should be automatically updated.
        /// </summary>
        public bool ShouldAutoUpdateMappings => _autoUpdateMappingCheckbox?.value ?? _autoUpdateMappingValue;

        private readonly MSDKUtilityEditorWindow _window;

        private Label _mappingJointLabel;
        private Label _mappingJointWeightsLabel;
        private Toggle _drawSourceCheckbox;
        private Toggle _drawTargetCheckbox;
        private Toggle _drawPreviewCheckbox;
        private Toggle _autoUpdateMappingCheckbox;

        // Persistent toggle values
        private static bool _drawSourceValue = true;
        private static bool _drawTargetValue = true;
        private static bool _drawPreviewValue = false;
        private static bool _autoUpdateMappingValue = true;

        /// <summary>
        /// Creates the panel content for the overlay.
        /// </summary>
        /// <returns>The visual element containing the panel content.</returns>
        public override VisualElement CreatePanelContent()
        {
            var root = new VisualElement
            {
                style =
                {
                    paddingTop = TinyPadding,
                    paddingBottom = TinyPadding,
                    paddingLeft = SmallPadding,
                    paddingRight = SmallPadding,
                    minWidth = OverlayMinWidth,
                    maxWidth = OverlayMaxWidth
                }
            };

            if (_window == null)
            {
                displayed = false;
                return root;
            }

            // Create compact header with title
            var headerContainer = new VisualElement
            {
                style =
                {
                    backgroundColor = HeaderBackgroundColor,
                    borderTopLeftRadius = SmallBorderRadius,
                    borderTopRightRadius = SmallBorderRadius,
                    paddingTop = SmallBorderRadius,
                    paddingBottom = SmallBorderRadius,
                    paddingLeft = Margins.HeaderBottom,
                    paddingRight = Margins.HeaderBottom,
                    marginBottom = TinyPadding,
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.SpaceBetween
                }
            };

            headerContainer.Add(new Label
            {
                text = "Retargeting Overlay",
                style = {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = SubHeaderFontSize
                }
            });

            root.Add(headerContainer);

            // Create main content card
            var mainCard = CreateCardContainer();
            mainCard.style.borderTopLeftRadius = SmallBorderRadius;
            mainCard.style.borderTopRightRadius = SmallBorderRadius;
            mainCard.style.paddingTop = SmallPadding;
            mainCard.style.paddingLeft = Margins.HeaderBottom;
            mainCard.style.paddingRight = Margins.HeaderBottom;
            mainCard.style.marginBottom = TinyPadding;

            // Create visualization options section
            var visualizationSection = CreateVisualizationSection();
            mainCard.Add(visualizationSection);

            // Create joint information section
            var jointInfoSection = CreateJointInfoSection();
            mainCard.Add(jointInfoSection);

            // Add the main card to the root
            root.Add(mainCard);

            // Only show certain sections based on the current step
            if (_window.Step is < MSDKUtilityEditorConfig.EditorStep.MinTPose
                or >= MSDKUtilityEditorConfig.EditorStep.Review)
            {
                jointInfoSection.style.display = DisplayStyle.None;
            }

            return root;
        }

        private VisualElement CreateVisualizationSection()
        {
            var visualizationSection = new VisualElement
            {
                style = { marginBottom = SmallBorderRadius }
            };

            var visualizationHeader = CreateSectionHeaderLabel("Visualization Options");
            visualizationHeader.style.fontSize = SubHeaderFontSize;
            visualizationHeader.style.marginBottom = SmallPadding;
            visualizationSection.Add(visualizationHeader);

            // Create a container for toggles
            var toggleContainer = new VisualElement
            {
                style = { width = Length.Percent(100) }
            };

            // Create hidden toggles for property getters
            _drawSourceCheckbox = new Toggle { value = _drawSourceValue };
            _drawTargetCheckbox = new Toggle { value = _drawTargetValue };
            _drawPreviewCheckbox = new Toggle { value = _drawPreviewValue };
            _autoUpdateMappingCheckbox = new Toggle { value = _autoUpdateMappingValue };

            // Create toggle rows using the factory method
            var sourceToggleRow = CreateCustomToggleRow("Draw Source Skeleton", _drawSourceValue, value =>
            {
                _drawSourceValue = value;
                _drawSourceCheckbox.value = value;
            });
            var targetToggleRow = CreateCustomToggleRow("Draw Target Skeleton", _drawTargetValue, value =>
            {
                _drawTargetValue = value;
                _drawTargetCheckbox.value = value;
            });
            var previewToggleRow = CreateCustomToggleRow("Draw Preview Skeleton", _drawPreviewValue, value =>
            {
                _drawPreviewValue = value;
                _drawPreviewCheckbox.value = value;
            });
            var updateToggleRow = CreateCustomToggleRow("Auto Update Mappings", _autoUpdateMappingValue, value =>
            {
                _autoUpdateMappingValue = value;
                _autoUpdateMappingCheckbox.value = value;
            });

            // Add toggle rows to container
            toggleContainer.Add(sourceToggleRow);
            toggleContainer.Add(targetToggleRow);
            toggleContainer.Add(previewToggleRow);
            toggleContainer.Add(updateToggleRow);

            visualizationSection.Add(toggleContainer);
            return visualizationSection;
        }

        private VisualElement CreateJointInfoSection()
        {
            var jointInfoSection = new VisualElement();

            var jointInfoHeader = CreateSectionHeaderLabel("Joint Info");
            jointInfoHeader.style.fontSize = SubHeaderFontSize;
            jointInfoHeader.style.marginBottom = TinyPadding;
            jointInfoSection.Add(jointInfoHeader);

            // Create joint info container
            var jointInfoContainer = new VisualElement
            {
                style =
                {
                    backgroundColor = DarkCardBackgroundColor,
                    borderTopLeftRadius = TinyBorderRadius,
                    borderTopRightRadius = TinyBorderRadius,
                    borderBottomLeftRadius = TinyBorderRadius,
                    borderBottomRightRadius = TinyBorderRadius,
                    paddingTop = SmallPadding,
                    paddingBottom = 0,
                    paddingLeft = Margins.HeaderBottom,
                    paddingRight = Margins.HeaderBottom,
                    minHeight = JointInfoMinHeight
                }
            };

            _mappingJointLabel = new Label(string.Empty)
            {
                style =
                {
                    fontSize = SubHeaderFontSize,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom = 1
                }
            };

            _mappingJointWeightsLabel = new Label(string.Empty)
            {
                enableRichText = true,
                style =
                {
                    whiteSpace = WhiteSpace.Normal,
                    fontSize = StandardFontSize
                }
            };

            jointInfoContainer.Add(_mappingJointLabel);
            jointInfoContainer.Add(_mappingJointWeightsLabel);
            jointInfoSection.Add(jointInfoContainer);

            return jointInfoSection;
        }


        /// <summary>
        /// Reloads the overlay.
        /// </summary>
        public void Reload()
        {
            // Toggle to reload overlay.
            collapsed = false;
            displayed = false;
            displayed = true;
        }

        /// <summary>
        /// Updates the overlay with the current selection information.
        /// </summary>
        public void Update()
        {
            if (_mappingJointLabel == null)
            {
                return;
            }

            if (_window != null && _window.SelectedIndex == -1)
            {
                _mappingJointLabel.text = "No Joint Selected";
                _mappingJointWeightsLabel.text = "Select a joint to view mapping information";
            }
            else
            {
                _mappingJointLabel.text = _window.SelectedJointName;
                var mappingsInfoText = _window.SelectedJointMappingsText;
                if (mappingsInfoText == null)
                {
                    _mappingJointWeightsLabel.text = "No mapping information available";
                    return;
                }

                _mappingJointWeightsLabel.text = mappingsInfoText;
            }
        }

        private bool FindPoseAsset(string poseName, out string pathToPoseAsset)
        {
            var guids = AssetDatabase.FindAssets(poseName, new[] { "Packages" });
            if (guids == null || guids.Length == 0)
            {
                pathToPoseAsset = string.Empty;
                return false;
            }

            pathToPoseAsset = AssetDatabase.GUIDToAssetPath(guids[0]);
            return true;
        }
    }
}
