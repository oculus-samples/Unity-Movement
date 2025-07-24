// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static Meta.XR.Movement.Editor.MSDKUtilityEditorUIConstants;

namespace Meta.XR.Movement.Editor
{
    /// <summary>
    /// Factory class for creating consistent UI elements across the MSDK Utility Editor.
    /// Provides reusable components with standardized styling and behavior.
    /// </summary>
    public static class MSDKUtilityEditorUIFactory
    {
        #region Container Creation

        /// <summary>
        /// Creates a standard container with consistent padding.
        /// </summary>
        public static VisualElement CreateStandardContainer()
        {
            return new VisualElement
            {
                style =
                {
                    paddingTop = SingleLineSpace,
                    paddingBottom = SingleLineSpace
                }
            };
        }

        /// <summary>
        /// Creates a card-style container with background and border.
        /// </summary>
        public static VisualElement CreateCardContainer()
        {
            return new VisualElement
            {
                style =
                {
                    backgroundColor = CardBackgroundColor,
                    borderBottomWidth = 1,
                    borderTopWidth = 1,
                    borderLeftWidth = 1,
                    borderRightWidth = 1,
                    borderBottomColor = BorderColor,
                    borderTopColor = BorderColor,
                    borderLeftColor = BorderColor,
                    borderRightColor = BorderColor,
                    borderTopLeftRadius = BorderRadius,
                    borderTopRightRadius = BorderRadius,
                    borderBottomLeftRadius = BorderRadius,
                    borderBottomRightRadius = BorderRadius,
                    marginBottom = Margins.CardBottom,
                    paddingBottom = CardPadding
                }
            };
        }

        /// <summary>
        /// Creates a header container with background styling.
        /// </summary>
        public static VisualElement CreateHeaderContainer(string title, string subtitle = null)
        {
            var headerContainer = new VisualElement
            {
                style =
                {
                    backgroundColor = HeaderBackgroundColor,
                    borderTopLeftRadius = BorderRadius,
                    borderTopRightRadius = BorderRadius,
                    paddingTop = CardPadding,
                    paddingBottom = CardPadding,
                    paddingLeft = CardPadding + TinyPadding,
                    paddingRight = CardPadding + TinyPadding,
                    marginBottom = Margins.HeaderBottom,
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.SpaceBetween
                }
            };

            headerContainer.Add(CreateHeaderLabel(title, HeaderFontSize));

            if (!string.IsNullOrEmpty(subtitle))
            {
                headerContainer.Add(new Label { text = subtitle });
            }

            return headerContainer;
        }

        #endregion

        #region Label Creation

        /// <summary>
        /// Creates a header label with consistent styling.
        /// </summary>
        public static Label CreateHeaderLabel(string text, int fontSize = HeaderFontSize)
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

        /// <summary>
        /// Creates a section header label with primary color.
        /// </summary>
        public static Label CreateSectionHeaderLabel(string text)
        {
            return new Label(text)
            {
                style =
                {
                    fontSize = SubHeaderFontSize,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom = Margins.SubHeaderBottom,
                    color = TextColor
                }
            };
        }

        /// <summary>
        /// Creates a joint name label with consistent styling.
        /// </summary>
        public static Label CreateJointNameLabel(string jointName)
        {
            return new Label
            {
                text = jointName,
                style =
                {
                    unityTextAlign = TextAnchor.MiddleLeft,
                    height = ButtonHeight,
                }
            };
        }

        /// <summary>
        /// Creates a spacer label for layout purposes.
        /// </summary>
        public static Label CreateSpacerLabel()
        {
            return new Label
            {
                text = " ",
                style =
                {
                    width = ButtonHeight,
                    height = ButtonHeight
                }
            };
        }

        #endregion

        #region Button Creation

        /// <summary>
        /// Creates a navigation button with consistent styling.
        /// </summary>
        public static Button CreateNavigationButton(string text, bool enabled, bool visible, Action onClick)
        {
            var button = new Button(onClick)
            {
                text = text,
                style =
                {
                    alignSelf = Align.Stretch,
                    flexGrow = 1,
                    minWidth = NavigationButtonMinWidth,
                    height = ButtonHeight,
                    borderTopLeftRadius = SmallBorderRadius,
                    borderTopRightRadius = SmallBorderRadius,
                    borderBottomLeftRadius = SmallBorderRadius,
                    borderBottomRightRadius = SmallBorderRadius,
                    marginLeft = SmallPadding,
                    marginRight = SmallPadding
                }
            };

            // Apply special styling for "Next" or "Done" buttons
            if (text == "Next" || text == "Done")
            {
                button.style.backgroundColor = SuccessButtonColor;
                button.style.color = TextColor;
                button.style.unityFontStyleAndWeight = FontStyle.Bold;
            }

            button.SetEnabled(enabled);
            button.visible = visible;
            return button;
        }

        /// <summary>
        /// Creates an icon action button with consistent styling.
        /// </summary>
        public static Button CreateIconActionButton(string iconName, Action clickAction)
        {
            var button = new Button(clickAction)
            {
                style =
                {
                    width = ActionButtonWidth,
                    height = ActionButtonHeight,
                    marginLeft = SmallPadding,
                    paddingLeft = SmallPadding,
                    paddingRight = SmallPadding,
                    paddingTop = TinyPadding,
                    paddingBottom = TinyPadding,
                    flexShrink = 0,
                    flexGrow = 0,
                    backgroundColor = TransparentColor
                }
            };

            button.Add(new Image
            {
                image = EditorGUIUtility.IconContent(iconName).image,
                style =
                {
                    width = IconSize,
                    height = IconSize,
                    flexShrink = 0,
                    flexGrow = 0
                }
            });

            return button;
        }

        /// <summary>
        /// Creates a styled mapping button with icon and text.
        /// </summary>
        public static Button CreateStyledMappingButton(Action callback, string text, string iconName)
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

        /// <summary>
        /// Creates a sequence button with image and label.
        /// </summary>
        public static Button CreateSequenceButton(string sequenceName, Texture2D image, Action onClick)
        {
            var button = new Button(onClick)
            {
                style =
                {
                    width = SequenceButtonWidth,
                    height = SequenceButtonHeight,
                    marginRight = CardPadding,
                    marginBottom = CardPadding,
                    paddingTop = Margins.HeaderBottom,
                    paddingBottom = Margins.HeaderBottom,
                    flexDirection = FlexDirection.Column,
                    alignItems = Align.Center,
                    justifyContent = Justify.Center,
                    borderTopLeftRadius = SmallBorderRadius,
                    borderTopRightRadius = SmallBorderRadius,
                    borderBottomLeftRadius = SmallBorderRadius,
                    borderBottomRightRadius = SmallBorderRadius
                }
            };

            // Add image
            var imageElement = new Image
            {
                image = image,
                style =
                {
                    width = LargeIconSize,
                    height = LargeIconSize,
                    marginBottom = SmallPadding
                }
            };
            button.Add(imageElement);

            // Add label
            var label = new Label(sequenceName)
            {
                style =
                {
                    fontSize = StandardFontSize,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };
            button.Add(label);

            return button;
        }

        #endregion

        #region Form Elements

        /// <summary>
        /// Creates a property field with consistent styling.
        /// </summary>
        public static PropertyField CreatePropertyField(SerializedObject serializedObject, string propertyPath,
            string label, float height = 0)
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

        /// <summary>
        /// Creates a bone transform field for joint configuration.
        /// </summary>
        public static PropertyField CreateBoneTransformField(SerializedObject serializedObject, int index)
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

        #endregion

        #region Layout Helpers

        /// <summary>
        /// Creates a responsive button grid container.
        /// </summary>
        public static VisualElement CreateButtonGrid()
        {
            return new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexWrap = Wrap.Wrap,
                    marginBottom = Margins.SectionBottom
                }
            };
        }

        /// <summary>
        /// Creates a joint entry container for configuration.
        /// </summary>
        public static VisualElement CreateJointEntryContainer()
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

        /// <summary>
        /// Creates a left-side container for joint entries.
        /// </summary>
        public static VisualElement CreateLeftJointEntryContainer()
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

        #endregion

        #region Hover Effects

        /// <summary>
        /// Applies hover effects to a collection of buttons.
        /// </summary>
        public static void ApplyButtonHoverEffects(IEnumerable<Button> buttons)
        {
            var buttonOriginalColors = new Dictionary<Button, Color>();

            foreach (var button in buttons)
            {
                // Get the current background color
                var originalColor = button.text switch
                {
                    "Next" or "Done" => SuccessButtonColor,
                    "Previous" => NeutralButtonColor,
                    "Pose character to T-Pose" => PrimaryColorVeryLight,
                    "Load original T-Pose" => PrimaryColorVeryLight,
                    "Load original hands" => PrimaryColorVeryLight,
                    "Validate and save config" => SuccessButtonColor,
                    _ => TransparentColor
                };

                // Store the original color
                buttonOriginalColors[button] = originalColor;

                // Set initial background color
                button.style.backgroundColor = originalColor;

                // Add hover effect - only apply if button is enabled
                button.RegisterCallback<MouseEnterEvent>(_ =>
                {
                    if (button.enabledSelf)
                    {
                        button.style.backgroundColor = ButtonHoverColor;
                    }
                });

                // Restore original color on mouse exit
                button.RegisterCallback<MouseLeaveEvent>(_ =>
                {
                    if (buttonOriginalColors.TryGetValue(button, out var color))
                    {
                        button.style.backgroundColor = color;
                    }
                });
            }
        }

        /// <summary>
        /// Applies standard button styling for mapping buttons.
        /// </summary>
        public static void ApplyMappingButtonStyle(Button button)
        {
            button.style.flexGrow = 1;
            button.style.minWidth = MinButtonWidth;
            button.style.marginRight = Margins.ButtonRight;
            button.style.marginBottom = Margins.ButtonBottom;
            button.style.height = ButtonHeight;
        }

        #endregion

        #region Toggle Components

        /// <summary>
        /// Creates a custom toggle row for overlay controls.
        /// </summary>
        public static VisualElement CreateCustomToggleRow(string labelText, bool isChecked, Action<bool> onValueChanged)
        {
            // Create a row container that will be clickable
            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    marginBottom = Margins.HeaderBottom,
                    width = Length.Percent(100)
                }
            };

            // Create a container for the checkbox with fixed width
            var checkboxContainer = new VisualElement
            {
                style =
                {
                    width = ActionButtonHeight,
                    alignItems = Align.Center,
                    justifyContent = Justify.Center
                }
            };

            // Create custom checkbox visual
            var checkbox = new VisualElement
            {
                style =
                {
                    width = CheckboxSize,
                    height = CheckboxSize,
                    backgroundColor = isChecked ? ToggleActiveColor : ToggleInactiveColor,
                    borderTopLeftRadius = TinyBorderRadius,
                    borderTopRightRadius = TinyBorderRadius,
                    borderBottomLeftRadius = TinyBorderRadius,
                    borderBottomRightRadius = TinyBorderRadius
                }
            };

            // Add checkmark if checked
            if (isChecked)
            {
                var checkmark = new VisualElement
                {
                    style =
                    {
                        width = CheckmarkSize,
                        height = CheckmarkSize,
                        backgroundColor = CheckmarkColor,
                        alignSelf = Align.Center,
                        position = Position.Absolute,
                        left = SmallBorderRadius,
                        top = SmallBorderRadius
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
                    fontSize = StandardFontSize,
                    color = TextColor,
                    unityTextAlign = TextAnchor.MiddleLeft,
                    flexGrow = 1,
                    marginLeft = SmallPadding,
                    paddingTop = TinyPadding
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
                checkbox.style.backgroundColor = isChecked ? ToggleActiveColor : ToggleInactiveColor;

                // Update checkmark
                checkbox.Clear();
                if (isChecked)
                {
                    var checkmark = new VisualElement
                    {
                        style =
                        {
                            width = CheckmarkSize,
                            height = CheckmarkSize,
                            backgroundColor = CheckmarkColor,
                            alignSelf = Align.Center,
                            position = Position.Absolute,
                            left = SmallBorderRadius,
                            top = SmallBorderRadius
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
                row.style.backgroundColor = HoverHighlightColor;
            });

            row.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                row.style.backgroundColor = TransparentColor;
            });

            return row;
        }

        #endregion
    }
}
