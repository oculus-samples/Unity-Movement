// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
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
        public bool ShouldDrawSource => GetToggleValue(_drawSourceCheckbox, _drawSourceValue);

        /// <summary>
        /// Gets whether the target skeleton should be drawn.
        /// </summary>
        public bool ShouldDrawTarget => GetToggleValue(_drawTargetCheckbox, _drawTargetValue);

        /// <summary>
        /// Gets whether the preview skeleton should be drawn.
        /// </summary>
        public bool ShouldDrawPreview => GetToggleValue(_drawPreviewCheckbox, _drawPreviewValue);

        /// <summary>
        /// Gets whether mappings should be automatically updated.
        /// </summary>
        public bool ShouldAutoUpdateMappings => GetToggleValue(_autoUpdateMappingCheckbox, _autoUpdateMappingValue);

        /// <summary>
        /// Gets whether twist joints should be mapped in config generation.
        /// </summary>
        public bool ShouldMapTwistJoints => GetToggleValue(_mapTwistJointsCheckbox, _mapTwistJointsValue);

        /// <summary>
        /// Gets the list of joint indices that are blocked from childAlignedTwist behavior.
        /// Using a block list approach - only joints marked as disabled are blocked.
        /// </summary>
        public List<int> ChildAlignedTwistBlockList
        {
            get
            {
                if (_childAlignedTwistToggles == null || _config?.TargetSkeletonData?.Joints == null)
                {
                    return null; // No block list means all eligible joints are allowed
                }

                var blockList = new List<int>();
                for (var i = 0; i < _childAlignedTwistToggles.Count && i < _config.TargetSkeletonData.Joints.Length; i++)
                {
                    // Block joints that are explicitly disabled (toggle value is false)
                    if (_childAlignedTwistToggles[i] != null && !_childAlignedTwistToggles[i].value)
                    {
                        blockList.Add(i);
                    }
                }

                return blockList.Count > 0 ? blockList : null;
            }
        }

        /// <summary>
        /// Gets the list of detected twist joint indices that should be included in mapping generation.
        /// Only includes joints that are enabled via checkboxes.
        /// </summary>
        public List<int> EnabledDetectedTwistJoints
        {
            get
            {
                var enabledJoints = new List<int>();
                if (_detectedTwistToggles == null || _window?.ConfigHandle == 0)
                {
                    return enabledJoints;
                }

                // Get twist joints from the native API to map toggle indices to joint indices
                if (MSDKUtility.GetTwistJoints(_window.ConfigHandle, MSDKUtility.SkeletonType.TargetSkeleton, out var twistJoints))
                {
                    for (int i = 0; i < _detectedTwistToggles.Count && i < twistJoints.Length; i++)
                    {
                        if (_detectedTwistToggles[i] != null && _detectedTwistToggles[i].value)
                        {
                            enabledJoints.Add(twistJoints[i].TwistJointIndex);
                        }
                    }
                }

                return enabledJoints;
            }
        }

        private readonly MSDKUtilityEditorWindow _window;
        private MSDKUtilityEditorConfig _config => _window?.Config;

        private Label _mappingJointLabel;
        private Label _mappingJointWeightsLabel;
        private VisualElement _statusBadge;
        private Toggle _drawSourceCheckbox;
        private Toggle _drawTargetCheckbox;
        private Toggle _drawPreviewCheckbox;
        private Toggle _autoUpdateMappingCheckbox;
        private Toggle _mapTwistJointsCheckbox;
        private DropdownField _sourceJointDropdown;
        private int _lastSelectedTargetJointIndex = -1;
        private VisualElement _jointInfoSection;
        private VisualElement _childAlignedTwistSection;
        private List<Toggle> _childAlignedTwistToggles;
        private List<Toggle> _detectedTwistToggles;
        private MSDKUtilityEditorConfig.EditorStep _lastStep = MSDKUtilityEditorConfig.EditorStep.Configuration;

        // Tab management
        private enum OverlayTab
        {
            Visualization,
            JointMapping
        }

        private OverlayTab _activeTab = OverlayTab.JointMapping;
        private VisualElement _tabContentContainer;
        private List<Button> _tabButtons = new List<Button>();

        // Persistent toggle values (used as fallbacks when UI elements are null)
        private static bool _drawSourceValue = true;
        private static bool _drawTargetValue = true;
        private static bool _drawPreviewValue = false;
        private static bool _autoUpdateMappingValue = true;
        private static bool _mapTwistJointsValue = true;

        // Persistent twist toggle values - keyed by joint index
        private static Dictionary<int, bool> _childAlignedTwistValues = new Dictionary<int, bool>();
        private static Dictionary<int, bool> _detectedTwistValues = new Dictionary<int, bool>();

        /// <summary>
        /// Helper method to get toggle value with fallback.
        /// </summary>
        private static bool GetToggleValue(Toggle toggle, bool fallbackValue) => toggle?.value ?? fallbackValue;

        /// <summary>
        /// Helper method to create styled container with rounded corners.
        /// </summary>
        private static VisualElement CreateRoundedContainer()
        {
            return new VisualElement
            {
                style =
                {
                    backgroundColor = DarkCardBackgroundColor,
                    borderTopLeftRadius = TinyBorderRadius,
                    borderTopRightRadius = TinyBorderRadius,
                    borderBottomLeftRadius = TinyBorderRadius,
                    borderBottomRightRadius = TinyBorderRadius,
                    paddingTop = SmallPadding,
                    paddingBottom = SmallPadding,
                    paddingLeft = Margins.HeaderBottom,
                    paddingRight = Margins.HeaderBottom
                }
            };
        }

        /// <summary>
        /// Helper method to find source joint index by name.
        /// </summary>
        private int FindSourceJointIndex(string sourceJointName)
        {
            if (string.IsNullOrEmpty(sourceJointName) || _config?.SourceSkeletonData?.Joints == null)
            {
                return -1;
            }

            for (var i = 0; i < _config.SourceSkeletonData.Joints.Length; i++)
            {
                if (_config.SourceSkeletonData.Joints[i] == sourceJointName)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Helper method to update previewer.
        /// </summary>
        private void UpdatePreviewer()
        {
            _window.Previewer.UpdateTargetDraw(_config);
            _window.Previewer.ReloadCharacter(_config);
        }

        /// <summary>
        /// Creates an icon button with Unity editor icons and hover tooltip.
        /// </summary>
        private Button CreateIconButton(string iconName, string buttonText, string tooltip, System.Action clickAction, bool isResetButton = false)
        {
            var button = new Button(clickAction)
            {
                tooltip = tooltip,
                style =
                {
                    flexGrow = 1,
                    height = 30,
                    fontSize = 12,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    backgroundColor = isResetButton ? new Color(0.6f, 0.3f, 0.3f, 0.8f) : new Color(0.3f, 0.3f, 0.3f, 0.8f),
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4,
                    color = new Color(0.9f, 0.9f, 0.9f, 1f),
                    alignItems = Align.Center,
                    justifyContent = Justify.Center,
                    marginLeft = 2,
                    marginRight = 2,
                    flexDirection = FlexDirection.Column,
                    paddingTop = 2,
                    paddingBottom = 2
                }
            };

            // Try to add Unity editor icon
            var iconContent = EditorGUIUtility.IconContent(iconName);
            if (iconContent?.image != null)
            {
                var iconElement = new Image
                {
                    image = iconContent.image,
                    style =
                    {
                        width = 12,
                        height = 12,
                        marginBottom = 2,
                        flexShrink = 0
                    }
                };
                button.Add(iconElement);
            }

            // Add text label
            var label = new Label(buttonText)
            {
                style =
                {
                    fontSize = 10,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = new Color(0.9f, 0.9f, 0.9f, 1f),
                    unityTextAlign = TextAnchor.MiddleCenter
                }
            };
            button.Add(label);

            // Add hover effects
            var originalBackgroundColor = isResetButton ? new Color(0.6f, 0.3f, 0.3f, 0.8f) : new Color(0.3f, 0.3f, 0.3f, 0.8f);
            var hoverBackgroundColor = isResetButton ? new Color(0.7f, 0.4f, 0.4f, 0.9f) : new Color(0.4f, 0.4f, 0.4f, 0.9f);

            button.RegisterCallback<MouseEnterEvent>(evt =>
            {
                button.style.backgroundColor = hoverBackgroundColor;
                label.style.color = new Color(1f, 1f, 1f, 1f);
            });

            button.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                button.style.backgroundColor = originalBackgroundColor;
                label.style.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            });

            return button;
        }

        /// <summary>
        /// Creates a functional status badge that indicates mapping status.
        /// </summary>
        private VisualElement CreateStatusBadge()
        {
            var statusBadge = new VisualElement
            {
                style =
                {
                    width = 8,
                    height = 8,
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4,
                    alignSelf = Align.Center
                }
            };

            // Update status badge color based on mapping state - will be updated in Update() method
            UpdateStatusBadge(statusBadge);

            return statusBadge;
        }

        /// <summary>
        /// Updates the status badge color based on the current joint mapping state.
        /// </summary>
        private void UpdateStatusBadge(VisualElement statusBadge)
        {
            if (_window == null || _window.SelectedIndex == -1)
            {
                statusBadge.style.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.8f); // Gray - no selection
                return;
            }

            // Check if the selected joint has any mappings
            bool hasMappings = HasJointMappings(_window.SelectedIndex);
            bool hasGoodMappings = HasGoodJointMappings(_window.SelectedIndex);
            if (hasGoodMappings)
            {
                statusBadge.style.backgroundColor = new Color(0.2f, 0.8f, 0.2f, 0.8f); // Green - good mappings
            }
            else if (hasMappings)
            {
                statusBadge.style.backgroundColor = new Color(1f, 0.6f, 0f, 0.8f); // Orange - has mappings but weak
            }
            else
            {
                statusBadge.style.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 0.8f); // Red - no mappings
            }
        }

        /// <summary>
        /// Checks if a joint has any mappings.
        /// </summary>
        private bool HasJointMappings(int jointIndex)
        {
            if (_config?.JointMappings == null || jointIndex < 0)
            {
                return false;
            }

            for (int i = 0; i < _config.JointMappings.Length; i++)
            {
                if (_config.JointMappings[i].JointIndex == jointIndex)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a joint has good quality mappings (rotation weight > 0.5 or position weight > 0.7).
        /// </summary>
        private bool HasGoodJointMappings(int jointIndex)
        {
            if (_config?.JointMappings == null || _config?.JointMappingEntries == null || jointIndex < 0)
            {
                return false;
            }

            var entryStartIndex = 0;
            for (int i = 0; i < _config.JointMappings.Length; i++)
            {
                var jointMapping = _config.JointMappings[i];

                if (jointMapping.JointIndex == jointIndex)
                {
                    // Check all entries for this mapping
                    for (int k = 0; k < jointMapping.EntriesCount; k++)
                    {
                        var entryIndex = entryStartIndex + k;
                        if (entryIndex < _config.JointMappingEntries.Length)
                        {
                            var entry = _config.JointMappingEntries[entryIndex];

                            // Consider mapping good if it has decent rotation or position weights
                            if (entry.RotationWeight > 0.5f || entry.PositionWeight > 0.7f)
                            {
                                return true;
                            }
                        }
                    }
                }

                entryStartIndex += jointMapping.EntriesCount;
            }

            return false;
        }

        /// <summary>
        /// Checks if a joint is eligible for child aligned twist behavior.
        /// A joint is eligible if it is either the actual joint or descendant of a start joint,
        /// and must be ancestor of the end joint.
        /// </summary>
        private bool IsJointEligibleForChildAlignedTwist(Transform joint)
        {
            if (joint == null || _config?.KnownSkeletonJoints == null)
            {
                return false;
            }

            return IsTwistEligibleInChain(joint,
                       _config.KnownSkeletonJoints[(int)MSDKUtility.KnownJointType.LeftUpperArm],
                       _config.KnownSkeletonJoints[(int)MSDKUtility.KnownJointType.LeftWrist]) ||
                   IsTwistEligibleInChain(joint,
                       _config.KnownSkeletonJoints[(int)MSDKUtility.KnownJointType.RightUpperArm],
                       _config.KnownSkeletonJoints[(int)MSDKUtility.KnownJointType.RightWrist]) ||
                   IsTwistEligibleInChain(joint,
                       _config.KnownSkeletonJoints[(int)MSDKUtility.KnownJointType.LeftUpperLeg],
                       _config.KnownSkeletonJoints[(int)MSDKUtility.KnownJointType.LeftAnkle]) ||
                   IsTwistEligibleInChain(joint,
                       _config.KnownSkeletonJoints[(int)MSDKUtility.KnownJointType.RightUpperLeg],
                       _config.KnownSkeletonJoints[(int)MSDKUtility.KnownJointType.RightAnkle]);
        }

        /// <summary>
        /// Checks if a joint is eligible for twist behavior within a specific joint chain.
        /// A joint is eligible if it is either the start joint itself or a descendant of the start joint,
        /// and must be an ancestor of the end joint.
        /// </summary>
        private bool IsTwistEligibleInChain(Transform joint, Transform startJoint, Transform endJoint)
        {
            if (joint == null || startJoint == null || endJoint == null)
            {
                return false;
            }

            // Check if joint is the start joint itself OR descendant of start joint
            bool isInStartChain = joint == startJoint || MSDKUtilityHelper.IsDescendantOf(joint, startJoint);

            // Check if joint is ancestor of end joint
            bool isAncestorOfEnd = MSDKUtilityHelper.IsAncestorOf(joint, endJoint);

            return isInStartChain && isAncestorOfEnd;
        }

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
                    maxWidth = OverlayMaxWidth,
                    flexGrow = 1 // Allow the overlay to expand vertically as needed
                }
            };

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
                    justifyContent = Justify.SpaceBetween,
                    flexShrink = 0 // Prevent header from shrinking
                }
            };

            headerContainer.Add(new Label
            {
                text = "Retargeting Overlay",
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = SubHeaderFontSize
                }
            });

            root.Add(headerContainer);

            // Create tabbed interface
            var tabContainer = CreateTabbedInterface();
            root.Add(tabContainer);

            // Update section visibility based on current step
            UpdateSectionVisibility();

            return root;
        }

        /// <summary>
        /// Creates the tabbed interface container.
        /// </summary>
        private VisualElement CreateTabbedInterface()
        {
            var tabContainer = new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    backgroundColor = DarkCardBackgroundColor,
                    borderTopLeftRadius = SmallBorderRadius,
                    borderTopRightRadius = SmallBorderRadius,
                    borderBottomLeftRadius = SmallBorderRadius,
                    borderBottomRightRadius = SmallBorderRadius,
                    paddingTop = SmallPadding,
                    paddingLeft = Margins.HeaderBottom,
                    paddingRight = Margins.HeaderBottom,
                    paddingBottom = SmallPadding
                }
            };

            // Create tab navigation buttons
            var tabNavigation = CreateTabNavigationBar();
            tabContainer.Add(tabNavigation);

            // Create tab content container
            _tabContentContainer = new ScrollView(ScrollViewMode.Vertical)
            {
                style =
                {
                    flexGrow = 1,
                    marginTop = SmallPadding,
                    width = Length.Percent(100),
                    overflow = Overflow.Hidden
                },
                horizontalScrollerVisibility = ScrollerVisibility.Hidden
            };

            tabContainer.Add(_tabContentContainer);

            // Initialize with default tab content
            SwitchToTab(_activeTab);

            return tabContainer;
        }

        /// <summary>
        /// Creates the tab navigation bar with tab buttons.
        /// </summary>
        private VisualElement CreateTabNavigationBar()
        {
            var tabNavBar = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexShrink = 0,
                    marginBottom = SmallPadding
                }
            };

            _tabButtons.Clear();

            // Visualization tab
            var visualizationTab = new Button(() => SwitchToTab(OverlayTab.Visualization))
            {
                text = "Visualization",
                style =
                {
                    flexGrow = 1,
                    height = 24,
                    fontSize = StandardFontSize - 1,
                    marginRight = 2,
                    backgroundColor = _activeTab == OverlayTab.Visualization
                        ? PrimaryColorLight // Use existing color scheme
                        : NeutralButtonColor,
                    color = _activeTab == OverlayTab.Visualization
                        ? TextColor // White text for active tab
                        : new Color(0.7f, 0.7f, 0.7f, 1f), // Dimmed text for inactive tab
                    unityFontStyleAndWeight = _activeTab == OverlayTab.Visualization
                        ? FontStyle.Bold // Bold text for active tab
                        : FontStyle.Normal,
                    borderTopWidth = _activeTab == OverlayTab.Visualization ? 2 : 0,
                    borderTopColor = PrimaryColor // Use existing primary color for border
                }
            };
            _tabButtons.Add(visualizationTab);
            tabNavBar.Add(visualizationTab);

            // Joint Mapping tab
            var mappingTab = new Button(() => SwitchToTab(OverlayTab.JointMapping))
            {
                text = "Joint Mapping",
                style =
                {
                    flexGrow = 1,
                    height = 24,
                    fontSize = StandardFontSize - 1,
                    marginLeft = 2,
                    backgroundColor = _activeTab == OverlayTab.JointMapping
                        ? PrimaryColorLight // Use existing color scheme
                        : NeutralButtonColor,
                    color = _activeTab == OverlayTab.JointMapping
                        ? TextColor // White text for active tab
                        : new Color(0.7f, 0.7f, 0.7f, 1f), // Dimmed text for inactive tab
                    unityFontStyleAndWeight = _activeTab == OverlayTab.JointMapping
                        ? FontStyle.Bold // Bold text for active tab
                        : FontStyle.Normal,
                    borderTopWidth = _activeTab == OverlayTab.JointMapping ? 2 : 0,
                    borderTopColor = PrimaryColor // Use existing primary color for border
                }
            };
            _tabButtons.Add(mappingTab);
            tabNavBar.Add(mappingTab);

            return tabNavBar;
        }

        /// <summary>
        /// Switches to the specified tab and updates the UI accordingly.
        /// </summary>
        private void SwitchToTab(OverlayTab tab)
        {
            _activeTab = tab;
            UpdateTabButtonStyles();
            UpdateTabContent();
        }

        /// <summary>
        /// Updates the tab button styles to reflect the active tab.
        /// </summary>
        private void UpdateTabButtonStyles()
        {
            for (int i = 0; i < _tabButtons.Count; i++)
            {
                var button = _tabButtons[i];
                var isActive = (OverlayTab)i == _activeTab;

                // Update background color
                button.style.backgroundColor = isActive
                    ? PrimaryColorLight // Use existing color scheme
                    : NeutralButtonColor;

                // Update text color
                button.style.color = isActive
                    ? TextColor // White text for active tab
                    : new Color(0.7f, 0.7f, 0.7f, 1f); // Dimmed text for inactive tab

                // Update font weight
                button.style.unityFontStyleAndWeight = isActive
                    ? FontStyle.Bold // Bold text for active tab
                    : FontStyle.Normal;

                // Update border
                button.style.borderTopWidth = isActive ? 2 : 0;
                button.style.borderTopColor = PrimaryColor; // Use existing primary color for border
            }
        }

        /// <summary>
        /// Updates the tab content based on the active tab.
        /// </summary>
        private void UpdateTabContent()
        {
            if (_tabContentContainer == null) return;

            _tabContentContainer.Clear();

            switch (_activeTab)
            {
                case OverlayTab.Visualization:
                    var visualizationSection = CreateVisualizationSection();
                    var applyScaleSection = CreateApplyScaleSection();
                    _tabContentContainer.Add(visualizationSection);
                    _tabContentContainer.Add(applyScaleSection);
                    break;

                case OverlayTab.JointMapping:
                    // Create Auto Mapping section first
                    var autoMappingSection = CreateAutoMappingSection();
                    _tabContentContainer.Add(autoMappingSection);

                    // Only show Joint Info and Manipulation section during MinTPose and MaxTPose steps
                    if (_window?.Step is MSDKUtilityEditorConfig.EditorStep.MinTPose
                        or MSDKUtilityEditorConfig.EditorStep.MaxTPose)
                    {
                        _jointInfoSection = CreateJointInfoSection();
                        _tabContentContainer.Add(_jointInfoSection);
                    }

                    // Add twist settings if configuration step and twist mappings is enabled
                    if (_window?.Step == MSDKUtilityEditorConfig.EditorStep.Configuration && ShouldMapTwistJoints)
                    {
                        _childAlignedTwistSection = CreateTwistSection();
                        _tabContentContainer.Add(_childAlignedTwistSection);
                    }

                    break;
            }
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
            _mapTwistJointsCheckbox = new Toggle { value = _mapTwistJointsValue };

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

            // Add toggle rows to container
            toggleContainer.Add(sourceToggleRow);
            toggleContainer.Add(targetToggleRow);
            toggleContainer.Add(previewToggleRow);

            visualizationSection.Add(toggleContainer);
            return visualizationSection;
        }

        private VisualElement CreateApplyScaleSection()
        {
            var applyScaleSection = new VisualElement
            {
                style = { marginBottom = SmallBorderRadius }
            };

            // Create apply scale button
            var applyScaleButton = new Button(ApplyManualScale)
            {
                text = "Preview Scale",
                style =
                {
                    height = ButtonHeight,
                    backgroundColor = new Color(0.2f, 0.6f, 1f, 0.8f), // Blue tint to make it stand out
                    borderTopLeftRadius = SmallBorderRadius,
                    borderTopRightRadius = SmallBorderRadius,
                    borderBottomLeftRadius = SmallBorderRadius,
                    borderBottomRightRadius = SmallBorderRadius,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };

            applyScaleSection.Add(applyScaleButton);
            return applyScaleSection;
        }

        private VisualElement CreateAutoMappingSection()
        {
            var autoMappingSection = new VisualElement
            {
                style = { marginBottom = SmallBorderRadius }
            };

            var autoMappingHeader = CreateSectionHeaderLabel("Auto Mapping");
            autoMappingHeader.style.fontSize = SubHeaderFontSize;
            autoMappingHeader.style.marginBottom = SmallPadding;
            autoMappingSection.Add(autoMappingHeader);

            // Create a container for square checkboxes
            var toggleContainer = new VisualElement
            {
                style = { width = Length.Percent(100) }
            };

            // Create hidden toggles for property getters (if not already created)
            _autoUpdateMappingCheckbox ??= new Toggle { value = _autoUpdateMappingValue };
            _mapTwistJointsCheckbox ??= new Toggle { value = _mapTwistJointsValue };

            // Create Map Twists toggle (first option)
            var mapTwistToggleRow = CreateCustomToggleRow("Map Twists", _mapTwistJointsValue, value =>
            {
                _mapTwistJointsValue = value;
                _mapTwistJointsCheckbox.value = value;
                // Trigger overlay redraw when state changes
                UpdateTabContent();
            });

            // Create Auto Update Mappings toggle (second option)
            var autoUpdateToggleRow = CreateCustomToggleRow("Auto Update Mappings", _autoUpdateMappingValue, value =>
            {
                _autoUpdateMappingValue = value;
                _autoUpdateMappingCheckbox.value = value;
            });

            // Add both toggles vertically to prevent overflow
            toggleContainer.Add(mapTwistToggleRow);
            toggleContainer.Add(autoUpdateToggleRow);

            autoMappingSection.Add(toggleContainer);
            return autoMappingSection;
        }

        private VisualElement CreateJointInfoSection()
        {
            var jointInfoSection = new VisualElement();

            var jointInfoHeader = CreateSectionHeaderLabel("Joint Info");
            jointInfoHeader.style.fontSize = SubHeaderFontSize;
            jointInfoHeader.style.marginBottom = TinyPadding;
            jointInfoSection.Add(jointInfoHeader);

            // Create compact joint info container with improved styling
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
                    paddingBottom = SmallPadding,
                    paddingLeft = Margins.HeaderBottom,
                    paddingRight = Margins.HeaderBottom,
                    marginBottom = SmallPadding,
                    minHeight = 60 // Reduced from JointInfoMinHeight for more compact display
                }
            };

            // Create header row with joint name and compact info
            var headerRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.SpaceBetween,
                    alignItems = Align.Center,
                    marginBottom = 2,
                    width = Length.Percent(100)
                }
            };

            _mappingJointLabel = new Label(string.Empty)
            {
                style =
                {
                    fontSize = SubHeaderFontSize,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = new Color(0.9f, 0.9f, 0.9f, 1f), // Slightly brighter text
                    flexShrink = 0
                }
            };

            // Create a functional status indicator/badge
            _statusBadge = CreateStatusBadge();

            headerRow.Add(_mappingJointLabel);
            headerRow.Add(_statusBadge);

            _mappingJointWeightsLabel = new Label(string.Empty)
            {
                enableRichText = true,
                style =
                {
                    whiteSpace = WhiteSpace.Normal,
                    fontSize = StandardFontSize - 1, // Slightly smaller for more compact display
                    color = new Color(0.8f, 0.8f, 0.8f, 1f), // Dimmed for secondary information
                    marginTop = 2
                }
            };

            jointInfoContainer.Add(headerRow);
            jointInfoContainer.Add(_mappingJointWeightsLabel);
            jointInfoSection.Add(jointInfoContainer);

            // Add joint manipulation section
            var jointManipulationSection = CreateJointManipulationSection();
            jointInfoSection.Add(jointManipulationSection);

            return jointInfoSection;
        }

        private VisualElement CreateJointManipulationSection()
        {
            var manipulationSection = new VisualElement
            {
                style = { marginTop = SmallPadding }
            };

            var manipulationHeader = CreateSectionHeaderLabel("Joint Manipulation");
            manipulationHeader.style.fontSize = SubHeaderFontSize;
            manipulationHeader.style.marginBottom = TinyPadding;
            manipulationSection.Add(manipulationHeader);

            // Create manipulation container
            var manipulationContainer = CreateRoundedContainer();

            // Create dropdown for source joints
            var dropdownLabel = new Label("Source Joint:")
            {
                style =
                {
                    fontSize = StandardFontSize,
                    marginBottom = TinyPadding
                }
            };
            manipulationContainer.Add(dropdownLabel);

            // Create dropdown container with navigation buttons
            var dropdownContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Stretch,
                    marginBottom = SmallPadding,
                    width = Length.Percent(100)
                }
            };

            // Previous button
            var prevButton = new Button(() => NavigateDropdown(-1))
            {
                text = "<",
                style =
                {
                    width = 20,
                    height = 20,
                    marginRight = 3,
                    fontSize = 10,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    flexShrink = 0
                }
            };

            _sourceJointDropdown = new DropdownField
            {
                style =
                {
                    flexGrow = 1,
                    marginLeft = 3,
                    marginRight = 3,
                    minWidth = 120,
                    maxWidth = Length.None(),
                    whiteSpace = WhiteSpace.NoWrap,
                    overflow = Overflow.Hidden
                }
            };

            // Next button
            var nextButton = new Button(() => NavigateDropdown(1))
            {
                text = ">",
                style =
                {
                    width = 20,
                    height = 20,
                    marginLeft = 3,
                    fontSize = 10,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    flexShrink = 0
                }
            };

            // Initialize with empty choices - will be populated when data is available
            _sourceJointDropdown.choices = new List<string>();

            // Update dropdown choices when data becomes available
            UpdateSourceJointDropdownChoices();

            dropdownContainer.Add(prevButton);
            dropdownContainer.Add(_sourceJointDropdown);
            dropdownContainer.Add(nextButton);
            manipulationContainer.Add(dropdownContainer);

            // Create single row of icon buttons
            var buttonContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.FlexStart,
                    marginTop = TinyPadding
                }
            };

            // IK button - Inverse Kinematics to align with source position
            var ikButton = CreateIconButton("ConfigurableJoint Icon", "IK", "IK to Position", () => IKToPosition(_sourceJointDropdown.value));

            // Snap button - Direct snap to source position
            var snapButton = CreateIconButton("MoveTool", "Snap", "Snap to Position", () => SnapToPosition(_sourceJointDropdown.value));

            // Restore Position button - Reset to original position
            var restorePositionButton = CreateIconButton("RotateTool", "Position", "Restore Original Position", RestoreOriginalPosition, true);

            // Restore Rotation button - Reset to original rotation
            var restoreRotationButton = CreateIconButton("RotateTool", "Rotation", "Restore Original Rotation", RestoreOriginalRotation, true);

            buttonContainer.Add(ikButton);
            buttonContainer.Add(snapButton);
            buttonContainer.Add(restorePositionButton);
            buttonContainer.Add(restoreRotationButton);
            manipulationContainer.Add(buttonContainer);

            manipulationSection.Add(manipulationContainer);
            return manipulationSection;
        }

        private VisualElement CreateTwistSection()
        {
            var childAlignedTwistSection = new VisualElement
            {
                style = { marginTop = SmallPadding }
            };

            var childAlignedTwistHeader = CreateSectionHeaderLabel("Twist Settings");
            childAlignedTwistHeader.style.fontSize = SubHeaderFontSize;
            childAlignedTwistHeader.style.marginBottom = TinyPadding;
            childAlignedTwistSection.Add(childAlignedTwistHeader);

            // Create child aligned twist container with better height management
            var childAlignedTwistContainer = new VisualElement
            {
                style =
                {
                    backgroundColor = DarkCardBackgroundColor,
                    borderTopLeftRadius = TinyBorderRadius,
                    borderTopRightRadius = TinyBorderRadius,
                    borderBottomLeftRadius = TinyBorderRadius,
                    borderBottomRightRadius = TinyBorderRadius,
                    paddingTop = SmallPadding,
                    paddingBottom = SmallPadding,
                    paddingLeft = Margins.HeaderBottom,
                    paddingRight = Margins.HeaderBottom,
                    flexGrow = 1,
                    minHeight = 120 // Minimum height to show at least some content
                }
            };

            // Create a tabbed view for Child-Aligned Twists and Detected Twists
            var tabContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    flexGrow = 1
                }
            };

            // Create tab buttons with more compact styling
            var tabButtonContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    marginBottom = 4, // Reduced margin
                    flexShrink = 0
                }
            };

            var childAlignedTab = new Button(SwitchToChildAlignedTwistTab)
            {
                text = "Child-Aligned Twists",
                style =
                {
                    flexGrow = 1,
                    marginRight = 1,
                    height = 20, // Reduced height
                    fontSize = StandardFontSize - 2, // Smaller font
                    backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f)
                }
            };

            var detectedTwistTab = new Button(SwitchToDetectedTwistTab)
            {
                text = "Detected Twists",
                style =
                {
                    flexGrow = 1,
                    marginLeft = 1,
                    height = 20, // Reduced height
                    fontSize = StandardFontSize - 2, // Smaller font
                    backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f)
                }
            };

            tabButtonContainer.Add(childAlignedTab);
            tabButtonContainer.Add(detectedTwistTab);
            tabContainer.Add(tabButtonContainer);

            // Child-Aligned Twist content
            var childAlignedContent = new VisualElement
            {
                name = "ChildAlignedContent",
                style =
                {
                    display = DisplayStyle.Flex,
                    flexGrow = 1
                }
            };

            var childAlignedDescriptionLabel = new Label("Select joints for child-aligned behavior:")
            {
                style =
                {
                    fontSize = StandardFontSize - 2,
                    color = new Color(0.8f, 0.8f, 0.8f, 1f),
                    marginBottom = 2, // Reduced margin
                    whiteSpace = WhiteSpace.Normal
                }
            };
            childAlignedContent.Add(childAlignedDescriptionLabel);

            var childAlignedScrollView = new ScrollView(ScrollViewMode.Vertical)
            {
                name = "ChildAlignedScrollView",
                style =
                {
                    flexGrow = 1,
                    width = Length.Percent(100),
                    overflow = Overflow.Hidden
                    // Remove maxHeight to allow scrolling within available space
                },
                horizontalScrollerVisibility = ScrollerVisibility.Hidden
            };

            // Initialize toggle list
            _childAlignedTwistToggles = new List<Toggle>();

            // Create container for two-column layout
            var twoColumnContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    width = Length.Percent(100)
                }
            };

            var leftColumn = new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    marginRight = SmallPadding / 2
                }
            };

            var rightColumn = new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    marginLeft = SmallPadding / 2
                }
            };

            twoColumnContainer.Add(leftColumn);
            twoColumnContainer.Add(rightColumn);

            // Create toggles only for eligible joints and organize in two columns
            if (_config?.TargetSkeletonData?.Joints != null)
            {
                var eligibleJoints = new List<(int index, string name)>();

                // First pass: collect eligible joints
                for (int i = 0; i < _config.TargetSkeletonData.Joints.Length; i++)
                {
                    var jointName = _config.TargetSkeletonData.Joints[i];
                    var joint = _config.SkeletonJoints[i];

                    if (IsJointEligibleForChildAlignedTwist(joint))
                    {
                        eligibleJoints.Add((i, jointName));
                    }
                }

                // Second pass: create toggles and distribute them to columns
                for (int i = 0; i < _config.TargetSkeletonData.Joints.Length; i++)
                {
                    // Only show child aligned twist option for eligible joints
                    if (!IsJointEligibleForChildAlignedTwist(_config.SkeletonJoints[i]))
                    {
                        // Add null toggle to maintain index consistency
                        _childAlignedTwistToggles.Add(null);
                        continue;
                    }

                    var jointName = _config.TargetSkeletonData.Joints[i];
                    var jointIndex = i;

                    // Use persistent value if available, otherwise fall back to existing mappings
                    bool initialState = _childAlignedTwistValues.ContainsKey(jointIndex)
                        ? _childAlignedTwistValues[jointIndex]
                        : HasChildAlignedTwistMapping(jointIndex);

                    var toggle = new Toggle
                    {
                        text = jointName,
                        value = initialState,
                        style =
                        {
                            fontSize = StandardFontSize - 2,
                            marginBottom = 1 // Reduced margin for more compact display
                        }
                    };

                    // Store initial state in persistent dictionary
                    _childAlignedTwistValues[jointIndex] = initialState;

                    // Track changes to maintain persistence
                    var capturedJointIndex = jointIndex; // Capture for closure
                    toggle.RegisterValueChangedCallback(evt =>
                    {
                        _childAlignedTwistValues[capturedJointIndex] = evt.newValue;
                    });

                    _childAlignedTwistToggles.Add(toggle);

                    // Distribute toggles to columns based on their order among eligible joints
                    var eligibleIndex = eligibleJoints.FindIndex(j => j.index == jointIndex);
                    if (eligibleIndex % 2 == 0)
                    {
                        leftColumn.Add(toggle);
                    }
                    else
                    {
                        rightColumn.Add(toggle);
                    }
                }
            }

            childAlignedScrollView.Add(twoColumnContainer);
            childAlignedContent.Add(childAlignedScrollView);

            // Detected Twist Joints content
            var detectedTwistContent = new VisualElement
            {
                name = "DetectedTwistContent",
                style =
                {
                    display = DisplayStyle.None,
                    flexGrow = 1
                }
            };

            var detectedTwistDescriptionLabel = new Label("Toggle individual twist joints:")
            {
                style =
                {
                    fontSize = StandardFontSize - 2,
                    color = new Color(0.8f, 0.8f, 0.8f, 1f),
                    marginBottom = 2, // Reduced margin
                    whiteSpace = WhiteSpace.Normal
                }
            };
            detectedTwistContent.Add(detectedTwistDescriptionLabel);

            var detectedTwistScrollView = new ScrollView(ScrollViewMode.Vertical)
            {
                name = "DetectedTwistScrollView",
                style =
                {
                    flexGrow = 1,
                    width = Length.Percent(100),
                    overflow = Overflow.Hidden
                    // Remove maxHeight to allow scrolling within available space
                },
                horizontalScrollerVisibility = ScrollerVisibility.Hidden
            };
            // Initialize detected twist toggle list
            _detectedTwistToggles = new List<Toggle>();

            // Create toggles for detected twist joints
            CreateDetectedTwistToggles(detectedTwistScrollView);

            detectedTwistContent.Add(detectedTwistScrollView);

            tabContainer.Add(childAlignedContent);
            tabContainer.Add(detectedTwistContent);
            childAlignedTwistContainer.Add(tabContainer);
            childAlignedTwistSection.Add(childAlignedTwistContainer);
            return childAlignedTwistSection;
        }

        private bool HasChildAlignedTwistMapping(int jointIndex)
        {
            // Check if this joint has a child-aligned twist mapping in the current config
            if (_config?.JointMappings == null)
            {
                return false;
            }

            // Look for existing mappings with ChildAlignedTwist behavior for this joint
            for (int i = 0; i < _config.JointMappings.Length; i++)
            {
                var mapping = _config.JointMappings[i];
                if (mapping.JointIndex == jointIndex &&
                    mapping.Behavior == MSDKUtility.JointMappingBehaviorType.ChildAlignedTwist)
                {
                    return true;
                }
            }

            return false;
        }

        private bool CheckForExistingChildAlignedTwistMapping(int jointIndex)
        {
            // Fallback to checking existing mappings if no persistent state exists
            if (_config?.JointMappings == null)
            {
                return false;
            }

            // Look for existing mappings with ChildAlignedTwist behavior for this joint
            for (int i = 0; i < _config.JointMappings.Length; i++)
            {
                var mapping = _config.JointMappings[i];
                if (mapping.JointIndex == jointIndex &&
                    mapping.Behavior == MSDKUtility.JointMappingBehaviorType.ChildAlignedTwist)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a detected twist joint has mappings in the current config.
        /// Returns true if the joint is present in the config (checkbox on), false if not present (checkbox off).
        /// </summary>
        private bool HasDetectedTwistMapping(int jointIndex)
        {
            // Check if this joint has any twist-related mapping in the current config
            if (_config?.JointMappings == null)
            {
                return false;
            }

            // Look for existing mappings with twist behaviors (TwistJoint or ChildAlignedTwist) for this joint
            foreach (var mapping in _config.JointMappings)
            {
                if (mapping.JointIndex == jointIndex &&
                    (mapping.Behavior == MSDKUtility.JointMappingBehaviorType.Twist ||
                     mapping.Behavior == MSDKUtility.JointMappingBehaviorType.ChildAlignedTwist))
                {
                    return true;
                }
            }

            return false;
        }

        private void CreateDetectedTwistToggles(ScrollView scrollView)
        {
            _detectedTwistToggles.Clear();

            if (_window?.ConfigHandle == 0 || _config?.TargetSkeletonData?.Joints == null)
            {
                return;
            }

            // Clear existing content first
            scrollView.Clear();

            // Create container for two-column layout
            var twoColumnContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    width = Length.Percent(100)
                }
            };

            var leftColumn = new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    marginRight = SmallPadding / 2
                }
            };

            var rightColumn = new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    marginLeft = SmallPadding / 2
                }
            };

            twoColumnContainer.Add(leftColumn);
            twoColumnContainer.Add(rightColumn);

            // Get twist joints from the native API
            if (MSDKUtility.GetTwistJoints(_window.ConfigHandle, MSDKUtility.SkeletonType.TargetSkeleton,
                    out var twistJoints))
            {
                int toggleIndex = 0;
                foreach (var twistJoint in twistJoints)
                {
                    var jointIndex = twistJoint.TwistJointIndex;
                    if (jointIndex < 0 || jointIndex >= _config.TargetSkeletonData.Joints.Length)
                    {
                        continue;
                    }

                    var jointName = _config.TargetSkeletonData.Joints[jointIndex];

                    // Use persistent value if available, otherwise fall back to existing mappings
                    bool initialState = _detectedTwistValues.ContainsKey(jointIndex)
                        ? _detectedTwistValues[jointIndex]
                        : HasDetectedTwistMapping(jointIndex);

                    var toggle = new Toggle
                    {
                        text = $"{jointName}",
                        value = initialState,
                        style =
                        {
                            fontSize = StandardFontSize - 2, // Match child-aligned toggle styling
                            marginBottom = 1 // Reduced margin for more compact display
                        }
                    };

                    // Store initial state in persistent dictionary
                    _detectedTwistValues[jointIndex] = initialState;

                    // Track changes to maintain persistence
                    var capturedJointIndex = jointIndex; // Capture for closure
                    toggle.RegisterValueChangedCallback(evt =>
                    {
                        _detectedTwistValues[capturedJointIndex] = evt.newValue;
                    });

                    _detectedTwistToggles.Add(toggle);

                    // Distribute toggles to columns
                    if (toggleIndex % 2 == 0)
                    {
                        leftColumn.Add(toggle);
                    }
                    else
                    {
                        rightColumn.Add(toggle);
                    }

                    toggleIndex++;
                }
            }

            scrollView.Add(twoColumnContainer);
        }

        private void SwitchToChildAlignedTwistTab()
        {
            var container = _childAlignedTwistSection?[1];
            if (container == null) return;

            var tabContainer = container[0]; // First child is the tab container
            if (tabContainer == null) return;

            var childAlignedContent = tabContainer.Q<VisualElement>("ChildAlignedContent");
            var detectedTwistContent = tabContainer.Q<VisualElement>("DetectedTwistContent");

            if (childAlignedContent != null && detectedTwistContent != null)
            {
                childAlignedContent.style.display = DisplayStyle.Flex;
                detectedTwistContent.style.display = DisplayStyle.None;

                // Update button styles
                var tabButtonContainer = tabContainer[0];
                if (tabButtonContainer is not { childCount: >= 2 })
                {
                    return;
                }

                var detectedTwistTab = tabButtonContainer[1] as Button;

                if (tabButtonContainer[0] is Button childAlignedTab)
                {
                    childAlignedTab.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f);
                }

                if (detectedTwistTab != null)
                {
                    detectedTwistTab.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
                }
            }
        }

        private void SwitchToDetectedTwistTab()
        {
            var container = _childAlignedTwistSection?[1];

            var tabContainer = container?[0]; // First child is the tab container
            if (tabContainer == null)
            {
                return;
            }

            var childAlignedContent = tabContainer.Q<VisualElement>("ChildAlignedContent");
            var detectedTwistContent = tabContainer.Q<VisualElement>("DetectedTwistContent");

            if (childAlignedContent == null || detectedTwistContent == null)
            {
                return;
            }

            childAlignedContent.style.display = DisplayStyle.None;
            detectedTwistContent.style.display = DisplayStyle.Flex;

            // Update button styles
            var tabButtonContainer = tabContainer[0];
            if (tabButtonContainer is { childCount: >= 2 })
            {
                var detectedTwistTab = tabButtonContainer[1] as Button;

                if (tabButtonContainer[0] is Button childAlignedTab)
                {
                    childAlignedTab.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
                }

                if (detectedTwistTab != null)
                {
                    detectedTwistTab.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f);
                }
            }

            // Refresh detected twist toggles when switching to this tab
            var detectedScrollView = detectedTwistContent.Q<ScrollView>("DetectedTwistScrollView");
            if (detectedScrollView != null)
            {
                CreateDetectedTwistToggles(detectedScrollView);
            }
        }

        private void SetAllChildAlignedTwistToggles(bool value)
        {
            if (_childAlignedTwistToggles != null)
            {
                foreach (var toggle in _childAlignedTwistToggles)
                {
                    toggle.value = value;
                }
            }
        }

        private void SnapToPosition(string sourceJointName)
        {
            if (_window.SelectedIndex == -1 || _config.SkeletonJoints == null)
            {
                return;
            }

            var sourceJointIndex = FindSourceJointIndex(sourceJointName);
            if (sourceJointIndex == -1)
            {
                return;
            }

            var sourceJoint = _config.SourceSkeletonData.TPoseArray[sourceJointIndex];
            var targetJoint = _config.SkeletonJoints[_window.SelectedIndex];

            if (targetJoint == null)
            {
                return;
            }

            Undo.RecordObject(targetJoint, "Snap to Position");
            targetJoint.position = sourceJoint.Position;
            UpdatePreviewer();
        }

        private void IKToPosition(string sourceJointName)
        {
            if (_window.SelectedIndex == -1 || _config.SkeletonJoints == null)
            {
                return;
            }

            var sourceJointIndex = FindSourceJointIndex(sourceJointName);
            if (sourceJointIndex == -1)
            {
                return;
            }

            var sourceJoint = _config.SourceSkeletonData.TPoseArray[sourceJointIndex];
            var targetJoint = _config.SkeletonJoints[_window.SelectedIndex];

            if (targetJoint == null || targetJoint.parent == null)
            {
                return;
            }

            Undo.RecordObject(targetJoint.parent, "IK to Position");
            Undo.RecordObject(targetJoint, "IK to Position");

            var targetParent = targetJoint.parent;
            var targetPosition = sourceJoint.Position;

            var desiredDistance = Vector3.Distance(targetParent.position, targetPosition);
            var currentDistance = Vector3.Distance(targetParent.position, targetJoint.position);

            if (Mathf.Abs(desiredDistance - currentDistance) > 0.001f)
            {
                var direction = (targetPosition - targetParent.position).normalized;
                targetJoint.position = targetParent.position + direction * desiredDistance;
                currentDistance = desiredDistance;
            }

            if (currentDistance > 0.001f)
            {
                var currentDirection = (targetJoint.position - targetParent.position).normalized;
                var desiredDirection = (targetPosition - targetParent.position).normalized;
                var rotation = Quaternion.FromToRotation(currentDirection, desiredDirection);
                targetParent.rotation = rotation * targetParent.rotation;
            }

            UpdatePreviewer();
        }

        private void RestoreOriginalPosition()
        {
            if (_window.SelectedIndex == -1 || _config.SkeletonJoints == null)
            {
                return;
            }

            MSDKUtility.GetSkeletonTPose(_window.ConfigHandle, MSDKUtility.SkeletonType.TargetSkeleton,
                MSDKUtility.SkeletonTPoseType.UnscaledTPose, MSDKUtility.JointRelativeSpaceType.LocalSpace,
                out var tPose);

            var targetJoint = _config.SkeletonJoints[_window.SelectedIndex];
            if (targetJoint == null)
            {
                return;
            }

            Undo.RecordObject(targetJoint, "Restore Original Position");
            targetJoint.localPosition = tPose[_window.SelectedIndex].Position;
            UpdatePreviewer();
        }

        private void RestoreOriginalRotation()
        {
            if (_window.SelectedIndex == -1 || _config.SkeletonJoints == null)
            {
                return;
            }

            MSDKUtility.GetSkeletonTPose(_window.ConfigHandle, MSDKUtility.SkeletonType.TargetSkeleton,
                MSDKUtility.SkeletonTPoseType.UnscaledTPose, MSDKUtility.JointRelativeSpaceType.LocalSpace,
                out var tPose);

            var targetJoint = _config.SkeletonJoints[_window.SelectedIndex];
            if (targetJoint == null)
            {
                return;
            }

            Undo.RecordObject(targetJoint, "Restore Original Rotation");
            targetJoint.localRotation = tPose[_window.SelectedIndex].Orientation;
            UpdatePreviewer();
        }

        private void ApplyManualScale()
        {
            // Apply manual scaling using the same utility method
            JointAlignmentUtility.LoadScale(_config);
            _window.Previewer.ReloadCharacter(_config);
        }

        private void NavigateDropdown(int direction)
        {
            if (_sourceJointDropdown?.choices == null || _sourceJointDropdown.choices.Count == 0)
            {
                return;
            }

            // Find current index based on the dropdown's current value
            var currentIndex = _sourceJointDropdown.choices.IndexOf(_sourceJointDropdown.value);
            if (currentIndex == -1)
            {
                currentIndex = 0; // Default to first item if current value not found
            }

            // Calculate new index with wrapping
            var newIndex = currentIndex + direction;

            if (newIndex < 0)
            {
                newIndex = _sourceJointDropdown.choices.Count - 1;
            }
            else if (newIndex >= _sourceJointDropdown.choices.Count)
            {
                newIndex = 0;
            }

            // Update the dropdown value
            _sourceJointDropdown.value = _sourceJointDropdown.choices[newIndex];
        }

        /// <summary>
        /// Finds the source joint with the highest rotational weight for the currently selected target joint.
        /// If all rotation weights are zero, finds the source joint with the highest position weight.
        /// </summary>
        /// <returns>The index of the source joint with highest weight, or -1 if no source mapping found.</returns>
        private int GetSourceJointWithHighestRotationWeight()
        {
            if (_window?.Config == null || _window.SelectedIndex == -1 ||
                _config.JointMappings == null || _config.JointMappingEntries == null ||
                _config?.SourceSkeletonData?.Joints == null)
            {
                return -1;
            }

            var selectedTargetJointIndex = _window.SelectedIndex;
            var highestRotationWeight = -1f;
            var highestPositionWeight = -1f;
            var bestRotationSourceJointIndex = -1;
            var bestPositionSourceJointIndex = -1;
            var hasSourceMappings = false;

            // Find all mappings for the selected target joint
            var entryStartIndex = 0;
            for (var i = 0; i < _config.JointMappings.Length; i++)
            {
                var jointMapping = _config.JointMappings[i];

                // Check if this mapping is for the selected target joint
                if (jointMapping.JointIndex == selectedTargetJointIndex)
                {
                    // Check if this mapping uses source skeleton (not target-only mappings)
                    if (jointMapping.Type == MSDKUtility.SkeletonType.SourceSkeleton)
                    {
                        hasSourceMappings = true;

                        // Check all entries for this mapping
                        for (var k = 0; k < jointMapping.EntriesCount; k++)
                        {
                            var entryIndex = entryStartIndex + k;
                            if (entryIndex < _config.JointMappingEntries.Length)
                            {
                                var entry = _config.JointMappingEntries[entryIndex];

                                // Track highest rotation weight
                                if (entry.RotationWeight > highestRotationWeight)
                                {
                                    highestRotationWeight = entry.RotationWeight;
                                    bestRotationSourceJointIndex = entry.JointIndex;
                                }

                                // Track highest position weight
                                if (entry.PositionWeight > highestPositionWeight)
                                {
                                    highestPositionWeight = entry.PositionWeight;
                                    bestPositionSourceJointIndex = entry.JointIndex;
                                }
                            }
                        }
                    }
                }

                // Move to the next mapping's entries
                entryStartIndex += jointMapping.EntriesCount;
            }

            // Return -1 if no source mappings found (only target mappings)
            if (!hasSourceMappings)
            {
                return -1;
            }

            // If we have rotation weights > 0, use the highest rotation weight
            if (highestRotationWeight > 0f)
            {
                return bestRotationSourceJointIndex;
            }

            // If all rotation weights are 0, use the highest position weight
            return bestPositionSourceJointIndex;
        }


        /// <summary>
        /// Reloads the overlay.
        /// This method ensures the overlay is visible and forces a complete refresh
        /// </summary>
        public void Reload()
        {
            // Ensure the overlay is visible
            collapsed = false;
            displayed = true;

            // Force a complete refresh of the UI content
            if (_tabContentContainer != null)
            {
                UpdateTabContent();
            }

            // Reset the status badge to gray to force an update
            if (_statusBadge != null)
            {
                _statusBadge.style.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);
            }

            // Clear cached values to force refresh
            _lastSelectedTargetJointIndex = -1;

            // Force repaint of the scene view to ensure the overlay is redrawn
            UnityEditor.SceneView.RepaintAll();
        }

        /// <summary>
        /// Updates the source joint dropdown choices when data becomes available.
        /// </summary>
        private void UpdateSourceJointDropdownChoices()
        {
            if (_sourceJointDropdown == null)
            {
                return;
            }

            // Check if we have source skeleton data available
            if (_config?.SourceSkeletonData?.Joints != null)
            {
                // Only update if choices are empty or different
                var currentChoices = _sourceJointDropdown.choices;
                if (currentChoices == null || currentChoices.Count == 0 ||
                    currentChoices.Count != _config.SourceSkeletonData.Joints.Length)
                {
                    _sourceJointDropdown.choices = new List<string>(_config.SourceSkeletonData.Joints);

                    // Set initial value if we have choices
                    if (_sourceJointDropdown.choices.Count > 0)
                    {
                        // Find the source joint with the highest rotational weight for the selected target joint
                        var initialIndex = GetSourceJointWithHighestRotationWeight();

                        // Fallback to selected target joint index if no mapping found, otherwise start from 0
                        if (initialIndex == -1)
                        {
                            initialIndex = _window != null && _window.SelectedIndex >= 0 &&
                                           _window.SelectedIndex < _sourceJointDropdown.choices.Count
                                ? _window.SelectedIndex
                                : 0;
                        }

                        _sourceJointDropdown.value = _sourceJointDropdown.choices[initialIndex];
                    }
                }
            }
            else
            {
                // Clear choices if no data is available
                if (_sourceJointDropdown.choices?.Count > 0)
                {
                    _sourceJointDropdown.choices = new List<string>();
                    _sourceJointDropdown.value = string.Empty;
                }
            }
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

            // Check if the step has changed and update section visibility accordingly
            if (_window != null && _window.Step != _lastStep)
            {
                _lastStep = _window.Step;
                UpdateSectionVisibility();

                // Force refresh of joint mapping content when step changes
                RefreshJointMappingContent();
            }

            // Update source joint dropdown when data becomes available
            UpdateSourceJointDropdownChoices();

            // Update child aligned twist section every frame to handle when skeleton joints are loaded
            UpdateChildAlignedTwistSection();

            // Update status badge if it exists
            if (_statusBadge != null)
            {
                UpdateStatusBadge(_statusBadge);
            }

            if (_window != null && _window.SelectedIndex == -1)
            {
                _mappingJointLabel.text = "No Joint Selected";
                _mappingJointWeightsLabel.text = "Select a joint to view mapping information";
                _lastSelectedTargetJointIndex = -1;
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

                // Check if the target joint selection has changed
                if (_window.SelectedIndex != _lastSelectedTargetJointIndex)
                {
                    _lastSelectedTargetJointIndex = _window.SelectedIndex;
                    UpdateSourceJointDropdownForNewTargetSelection();
                }
            }
        }

        /// <summary>
        /// Updates the visibility of sections based on the current editor step.
        /// </summary>
        private void UpdateSectionVisibility()
        {
            if (_window == null)
            {
                return;
            }

            // Determine appropriate tab based on current step
            var targetTab = _activeTab;

            // During MinTPose and MaxTPose steps, switch to JointMapping tab
            if (_window.Step is >= MSDKUtilityEditorConfig.EditorStep.MinTPose
                and < MSDKUtilityEditorConfig.EditorStep.Review)
            {
                targetTab = OverlayTab.JointMapping;
            }
            else
            {
                // For other steps, default to Visualization tab
                targetTab = OverlayTab.Visualization;
            }

            // Switch to the appropriate tab if needed
            if (_activeTab != targetTab)
            {
                SwitchToTab(targetTab);
            }

            // Update tab button availability based on step
            UpdateTabButtonAvailability();
        }

        /// <summary>
        /// Updates tab button availability based on the current editor step.
        /// </summary>
        private void UpdateTabButtonAvailability()
        {
            if (_tabButtons.Count < 2 || _window == null)
            {
                return;
            }

            var visualizationButton = _tabButtons[(int)OverlayTab.Visualization];
            var mappingButton = _tabButtons[(int)OverlayTab.JointMapping];

            // Visualization tab is always available
            visualizationButton.SetEnabled(true);

            // Mapping tab is available during MinTPose and MaxTPose steps or always available
            bool
                mappingAvailable =
                    true; // Mapping is now always available since it contains twist settings conditionally
            mappingButton.SetEnabled(mappingAvailable);

            // Update button opacity to show availability
            visualizationButton.style.opacity = 1f;
            mappingButton.style.opacity = 1f;
        }

        /// <summary>
        /// Updates the source joint dropdown when a new target joint is selected.
        /// </summary>
        private void UpdateSourceJointDropdownForNewTargetSelection()
        {
            if (_sourceJointDropdown == null)
            {
                return;
            }

            // Ensure dropdown is populated first
            UpdateSourceJointDropdownChoices();

            // Now check if we have choices to work with
            if (_sourceJointDropdown.choices == null || _sourceJointDropdown.choices.Count == 0)
            {
                return;
            }

            // Find the source joint with the highest rotational weight for the newly selected target joint
            var bestSourceJointIndex = GetSourceJointWithHighestRotationWeight();

            // Update the dropdown to show the source joint with highest rotational weight
            if (bestSourceJointIndex >= 0 && bestSourceJointIndex < _sourceJointDropdown.choices.Count)
            {
                _sourceJointDropdown.value = _sourceJointDropdown.choices[bestSourceJointIndex];
            }
        }

        /// <summary>
        /// Refreshes the joint mapping content when switching between configuration editor steps.
        /// This ensures all joint mapping data and UI elements are properly updated.
        /// </summary>
        private void RefreshJointMappingContent()
        {
            // Only refresh if we're currently on the Joint Mapping tab
            if (_activeTab != OverlayTab.JointMapping)
            {
                return;
            }

            // Force refresh of the tab content to reflect new step
            UpdateTabContent();

            // Clear cached selection to force refresh of joint info
            _lastSelectedTargetJointIndex = -1;

            // Reset source joint dropdown to force refresh
            if (_sourceJointDropdown != null)
            {
                _sourceJointDropdown.choices = new List<string>();
                _sourceJointDropdown.value = string.Empty;
            }

            // Clear twist toggles to force recreation
            _childAlignedTwistToggles?.Clear();
            _detectedTwistToggles?.Clear();
        }

        /// <summary>
        /// Updates the child aligned twist section to handle skeleton joints loading.
        /// This method is called every frame to recreate the toggles when skeleton data becomes available.
        /// </summary>
        private void UpdateChildAlignedTwistSection()
        {
            if (_childAlignedTwistSection == null || _config?.TargetSkeletonData?.Joints == null ||
                _config.SkeletonJoints == null)
            {
                return;
            }

            // Check if we need to recreate the toggles (when skeleton joints are loaded but toggles are not properly populated)
            bool needsUpdate = _childAlignedTwistToggles == null ||
                               _childAlignedTwistToggles.Count != _config.TargetSkeletonData.Joints.Length ||
                               (_childAlignedTwistToggles.Count > 0 && _childAlignedTwistToggles.All(t => t == null));

            if (!needsUpdate)
            {
                return;
            }

            // Navigate the UI structure to find the ScrollView
            // Structure is: _childAlignedTwistSection > childAlignedTwistContainer > scrollView
            if (_childAlignedTwistSection.childCount == 0)
            {
                return;
            }

            // Skip the header (first child) and get the container (second child)
            if (_childAlignedTwistSection.childCount < 2)
            {
                return;
            }

            var container = _childAlignedTwistSection[1]; // Second child is the container
            var scrollView = container.Q<ScrollView>();
            if (scrollView == null)
            {
                return;
            }

            // Clear existing toggles from scroll view
            scrollView.Clear();

            // Reinitialize toggle list
            _childAlignedTwistToggles = new List<Toggle>();

            // Recreate toggles for eligible joints
            for (int i = 0; i < _config.TargetSkeletonData.Joints.Length; i++)
            {
                var jointName = _config.TargetSkeletonData.Joints[i];
                var jointIndex = i;
                var joint = _config.SkeletonJoints[jointIndex];

                // Only show child aligned twist option for eligible joints
                if (!IsJointEligibleForChildAlignedTwist(joint))
                {
                    // Add null toggle to maintain index consistency
                    _childAlignedTwistToggles.Add(null);
                    continue;
                }

                // Use persistent value if available, otherwise fall back to existing mappings
                bool initialState = _childAlignedTwistValues.ContainsKey(jointIndex)
                    ? _childAlignedTwistValues[jointIndex]
                    : HasChildAlignedTwistMapping(jointIndex);

                var toggle = new Toggle
                {
                    text = jointName,
                    value = initialState,
                    style =
                    {
                        fontSize = StandardFontSize - 1,
                        marginBottom = 2
                    }
                };

                // Store initial state in persistent dictionary
                _childAlignedTwistValues[jointIndex] = initialState;

                // Track changes to maintain persistence
                var capturedJointIndex = jointIndex; // Capture for closure
                toggle.RegisterValueChangedCallback(evt =>
                {
                    _childAlignedTwistValues[capturedJointIndex] = evt.newValue;
                });

                _childAlignedTwistToggles.Add(toggle);
                scrollView.Add(toggle);
            }
        }
    }
}
