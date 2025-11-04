// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using UnityEngine;

namespace Meta.XR.Movement.Editor
{
    /// <summary>
    /// Centralized UI constants and styling definitions for the MSDK Utility Editor.
    /// Eliminates magic numbers and provides consistent styling across components.
    /// </summary>
    public static class MSDKUtilityEditorUIConstants
    {
        // Layout dimensions
        public const int ButtonHeight = 28;
        public const int SingleLineSpace = 8;
        public const int DoubleLineSpace = 16;
        public const int HeaderFontSize = 18;
        public const int SubHeaderFontSize = 14;
        public const int StandardFontSize = 12;
        public const int SmallFontSize = 11;
        public const int ButtonPadding = 6;
        public const int CardPadding = 8;
        public const int SmallPadding = 4;
        public const int TinyPadding = 2;
        public const int BorderRadius = 4;
        public const int SmallBorderRadius = 3;
        public const int TinyBorderRadius = 2;
        public const int IconSize = 16;
        public const int LargeIconSize = 60;
        public const int OverlayMinWidth = 300;
        public const int OverlayMaxWidth = 300;
        public const int SequenceButtonWidth = 120;
        public const int SequenceButtonHeight = 90;
        public const int ActionButtonWidth = 26;
        public const int ActionButtonHeight = 24;
        public const int NavigationButtonMinWidth = 100;
        public const int ScaleFieldWidth = 60;
        public const int CheckboxSize = 16;
        public const int CheckmarkSize = 10;
        public const int JointInfoMinHeight = 60;

        // Colors - Primary Theme
        public static readonly Color PrimaryColor = new(0.0f, 0.47f, 0.95f, 1.0f);
        public static readonly Color PrimaryColorLight = new(0.0f, 0.47f, 0.95f, 0.2f);
        public static readonly Color PrimaryColorVeryLight = new(0.0f, 0.47f, 0.95f, 0.1f);

        // Colors - Background
        public static readonly Color BackgroundColor = new(0.15f, 0.15f, 0.15f, 0.05f);
        public static readonly Color CardBackgroundColor = new(1f, 1f, 1f, 0.03f);
        public static readonly Color DarkCardBackgroundColor = new(0.1f, 0.1f, 0.1f, 0.2f);
        public static readonly Color HeaderBackgroundColor = new(0.0f, 0.47f, 0.95f, 0.1f);

        // Colors - Interactive Elements
        public static readonly Color ButtonHoverColor = new(0.0f, 0.47f, 0.95f, 0.2f);
        public static readonly Color SuccessButtonColor = new(0.1f, 0.6f, 0.3f, 0.7f);
        public static readonly Color NeutralButtonColor = new(0.2f, 0.2f, 0.2f, 0.8f);
        public static readonly Color ToggleActiveColor = new(0.0f, 0.47f, 0.95f, 1.0f);
        public static readonly Color ToggleInactiveColor = new(0.2f, 0.2f, 0.2f, 0.5f);
        public static readonly Color HoverHighlightColor = new(1f, 1f, 1f, 0.05f);

        // Colors - Borders and Separators
        public static readonly Color BorderColor = new(0f, 0f, 0f, 0.2f);
        public static readonly Color SeparatorColor = new(0, 0, 0, 0.2f);

        // Colors - Text
        public static readonly Color TextColor = Color.white;
        public static readonly Color CheckmarkColor = new(1f, 1f, 1f, 0.8f);

        // Colors - Transparent
        public static readonly Color TransparentColor = new(0, 0, 0, 0);

        // Animation and interaction values
        public const float HoverTransitionDuration = 0.1f;
        public const float ClickFeedbackDuration = 0.05f;

        // Layout constraints
        public const float MinSliderWidth = 80;
        public const float MaxSliderWidthPercent = 80f;
        public const int MinButtonWidth = 120;
        public const int MaxContentWidth = 400;

        // Spacing patterns
        public static class Spacing
        {
            public const int None = 0;
            public const int Tiny = 2;
            public const int Small = 4;
            public const int Medium = 8;
            public const int Large = 12;
            public const int ExtraLarge = 16;
        }

        // Common margin/padding configurations
        public static class Margins
        {
            public const int CardBottom = 8;
            public const int SectionBottom = 12;
            public const int ButtonRight = 4;
            public const int ButtonBottom = 4;
            public const int HeaderBottom = 6;
            public const int SubHeaderBottom = 4;
        }
    }
}
