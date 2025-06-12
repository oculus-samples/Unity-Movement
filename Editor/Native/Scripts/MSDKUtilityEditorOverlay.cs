// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

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

        // UI styling constants
        private static readonly Color _primaryColor = new Color(0.0f, 0.47f, 0.95f, 1.0f);
        private static readonly Color _cardBackgroundColor = new Color(1f, 1f, 1f, 0.03f);
        private static readonly Color _borderColor = new Color(0f, 0f, 0f, 0.2f);
        private static readonly Color _headerBackgroundColor = new Color(0.0f, 0.47f, 0.95f, 0.1f);
        private static readonly Color _toggleActiveColor = new Color(0.0f, 0.47f, 0.95f, 1.0f);

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
                    paddingTop = 2,
                    paddingBottom = 2,
                    paddingLeft = 4,
                    paddingRight = 4,
                    minWidth = 240,
                    maxWidth = 240
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
                    backgroundColor = _headerBackgroundColor,
                    borderTopLeftRadius = 3,
                    borderTopRightRadius = 3,
                    paddingTop = 3,
                    paddingBottom = 3,
                    paddingLeft = 6,
                    paddingRight = 6,
                    marginBottom = 2,
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.SpaceBetween
                }
            };

            headerContainer.Add(new Label
            {
                text = "Retargeting Overlay",
                style = {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 13
                }
            });

            root.Add(headerContainer);

            // Create more compact main content card with more height
            var mainCard = new VisualElement
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
                    borderTopLeftRadius = 3,
                    borderTopRightRadius = 3,
                    borderBottomLeftRadius = 3,
                    borderBottomRightRadius = 3,
                    paddingTop = 4,
                    paddingBottom = 4,
                    paddingLeft = 6,
                    paddingRight = 6,
                    marginBottom = 2
                }
            };

            // Create more compact visualization options section
            var visualizationSection = new VisualElement
            {
                style =
                {
                    marginBottom = 3
                }
            };

            var visualizationHeader = new Label("Visualization Options")
            {
                style =
                {
                    fontSize = 13,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom = 4,
                    color = _primaryColor
                }
            };
            visualizationSection.Add(visualizationHeader);

            // Create a container for toggles
            var toggleContainer = new VisualElement
            {
                style =
                {
                    width = Length.Percent(100)
                }
            };

            // Create hidden toggles for property getters
            _drawSourceCheckbox = new Toggle { value = _drawSourceValue };
            _drawTargetCheckbox = new Toggle { value = _drawTargetValue };
            _drawPreviewCheckbox = new Toggle { value = _drawPreviewValue };
            _autoUpdateMappingCheckbox = new Toggle { value = _autoUpdateMappingValue };

            // Create toggle rows directly with labels and update both static values and hidden toggles
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

            // Add container to visualization section
            visualizationSection.Add(toggleContainer);

            mainCard.Add(visualizationSection);

            // Create more compact joint information section
            var jointInfoSection = new VisualElement();

            var jointInfoHeader = new Label("Joint Info")
            {
                style =
                {
                    fontSize = 13,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom = 2,
                    color = _primaryColor
                }
            };
            jointInfoSection.Add(jointInfoHeader);

            // Create more compact joint info container with more height
            var jointInfoContainer = new VisualElement
            {
                style =
                {
                    backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.2f),
                    borderTopLeftRadius = 2,
                    borderTopRightRadius = 2,
                    borderBottomLeftRadius = 2,
                    borderBottomRightRadius = 2,
                    paddingTop = 4,
                    paddingBottom = 0,
                    paddingLeft = 6,
                    paddingRight = 6,
                    minHeight = 60 // Ensure minimum height
                }
            };

            _mappingJointLabel = new Label(string.Empty)
            {
                style =
                {
                    fontSize = 13,
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
                    fontSize = 12
                }
            };

            jointInfoContainer.Add(_mappingJointLabel);
            jointInfoContainer.Add(_mappingJointWeightsLabel);
            jointInfoSection.Add(jointInfoContainer);

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


        private VisualElement CreateCustomToggleRow(string labelText, bool isChecked, Action<bool> onValueChanged)
        {
            // Create a row container that will be clickable
            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    marginBottom = 6,
                    width = Length.Percent(100)
                }
            };

            // Create a container for the checkbox with fixed width
            var checkboxContainer = new VisualElement
            {
                style =
                {
                    width = 24,
                    alignItems = Align.Center,
                    justifyContent = Justify.Center
                }
            };

            // Create custom checkbox visual
            var checkbox = new VisualElement
            {
                style =
                {
                    width = 16,
                    height = 16,
                    backgroundColor = isChecked ? _toggleActiveColor : new Color(0.2f, 0.2f, 0.2f, 0.5f),
                    borderTopLeftRadius = 2,
                    borderTopRightRadius = 2,
                    borderBottomLeftRadius = 2,
                    borderBottomRightRadius = 2
                }
            };

            // Add checkmark if checked
            if (isChecked)
            {
                var checkmark = new VisualElement
                {
                    style =
                    {
                        width = 10,
                        height = 10,
                        backgroundColor = new Color(1f, 1f, 1f, 0.8f),
                        alignSelf = Align.Center,
                        position = Position.Absolute,
                        left = 3,
                        top = 3
                    }
                };
                checkbox.Add(checkmark);
            }

            checkboxContainer.Add(checkbox);

            // Create text label with explicit color and styling
            var textLabel = new Label
            {
                text = labelText,
                style =
                {
                    fontSize = 12,
                    color = Color.white, // Use pure white for maximum visibility
                    unityTextAlign = TextAnchor.MiddleLeft,
                    flexGrow = 1,
                    marginLeft = 4,
                    paddingTop = 2
                }
            };

            // Add elements to row
            row.Add(checkboxContainer);
            row.Add(textLabel);

            // Make the entire row clickable
            row.AddManipulator(new Clickable(() =>
            {
                // Toggle the state
                isChecked = !isChecked;

                // Update checkbox visual
                checkbox.style.backgroundColor = isChecked ? _toggleActiveColor : new Color(0.2f, 0.2f, 0.2f, 0.5f);

                // Update checkmark
                checkbox.Clear();
                if (isChecked)
                {
                    var checkmark = new VisualElement
                    {
                        style =
                        {
                            width = 10,
                            height = 10,
                            backgroundColor = new Color(1f, 1f, 1f, 0.8f),
                            alignSelf = Align.Center,
                            position = Position.Absolute,
                            left = 3,
                            top = 3
                        }
                    };
                    checkbox.Add(checkmark);
                }

                // Call the callback to update the persistent value
                onValueChanged?.Invoke(isChecked);
            }));

            // Add hover effect
            row.RegisterCallback<MouseEnterEvent>(evt =>
            {
                row.style.backgroundColor = new Color(1f, 1f, 1f, 0.05f);
            });

            row.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                row.style.backgroundColor = new Color(0f, 0f, 0f, 0f);
            });

            return row;
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
