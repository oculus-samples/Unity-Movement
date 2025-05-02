// Copyright (c) Meta Platforms, Inc. and affiliates.

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
        public bool ShouldDrawSource => _drawSourceCheckbox?.value ?? false;

        /// <summary>
        /// Gets whether the target skeleton should be drawn.
        /// </summary>
        public bool ShouldDrawTarget => _drawTargetCheckbox?.value ?? false;

        /// <summary>
        /// Gets whether the preview skeleton should be drawn.
        /// </summary>
        public bool ShouldDrawPreview => _drawPreviewCheckbox?.value ?? false;

        /// <summary>
        /// Gets whether mappings should be automatically updated.
        /// </summary>
        public bool ShouldAutoUpdateMappings => _autoUpdateMappingCheckbox?.value ?? false;

        private readonly MSDKUtilityEditorWindow _window;

        private Label _mappingJointLabel;
        private Label _mappingJointWeightsLabel;
        private Toggle _drawSourceCheckbox;
        private Toggle _drawTargetCheckbox;
        private Toggle _drawPreviewCheckbox;
        private Toggle _autoUpdateMappingCheckbox;

        /// <summary>
        /// Creates the panel content for the overlay.
        /// </summary>
        /// <returns>The visual element containing the panel content.</returns>
        public override VisualElement CreatePanelContent()
        {
            var root = new VisualElement();
            if (_window == null)
            {
                displayed = false;
                return root;
            }

            // Top row with checkboxes and update mapping button
            var topRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    justifyContent = Justify.SpaceBetween
                }
            };
            var middleRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.SpaceBetween
                }
            };

            _drawSourceCheckbox = new Toggle("Draw Source Skeleton");
            _drawTargetCheckbox = new Toggle("Draw Target Skeleton");
            _drawPreviewCheckbox = new Toggle("Draw Preview Skeleton");
            _autoUpdateMappingCheckbox = new Toggle("Auto Update Mappings");
            _drawSourceCheckbox.value = true;
            _drawTargetCheckbox.value = true;
            _drawPreviewCheckbox.value = false;
            _autoUpdateMappingCheckbox.value = true;

            topRow.Add(_drawSourceCheckbox);
            topRow.Add(_drawTargetCheckbox);
            topRow.Add(_drawPreviewCheckbox);
            middleRow.Add(_autoUpdateMappingCheckbox);

            // Rest of the content below
            var leftColumn = new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    flexDirection = FlexDirection.Column
                }
            };
            var spacerColumn = new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    flexDirection = FlexDirection.Column
                }
            };
            var rightColumn = new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    flexDirection = FlexDirection.Column
                }
            };
            var columnsContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row
                }
            };

            _mappingJointLabel = new Label(string.Empty)
            {
                style =
                {
                    fontSize = 18,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };
            _mappingJointWeightsLabel = new Label(string.Empty);

            //TODO: Restore old preview poses (Standing, Squatting, Walking, Stretching, Reaching)
            leftColumn.Add(_mappingJointLabel);
            leftColumn.Add(_mappingJointWeightsLabel);

            // Add columns to the container and container to root.
            root.Add(topRow);

            if (_window.Step is < MSDKUtilityEditorConfig.EditorStep.MinTPose
                or >= MSDKUtilityEditorConfig.EditorStep.Review)
            {
                return root;
            }


            root.Add(middleRow);
            columnsContainer.Add(leftColumn);
            root.Add(columnsContainer);
            return root;
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
                _mappingJointLabel.text = string.Empty;
                _mappingJointWeightsLabel.text = string.Empty;
            }
            else
            {
                _mappingJointLabel.text = _window.SelectedJointName;
                var mappingsInfoText = _window.SelectedJointMappingsText;
                if (mappingsInfoText == null)
                {
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
