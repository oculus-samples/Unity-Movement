// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using UnityEditor;
using UnityEngine;

namespace Oculus.Movement.Utils
{
    /// <summary>
    /// Use this class in custom <see cref="Editor"/>s to simplify insertion of Inspector
    /// warnings or feature buttons.
    /// </summary>
    public class InspectorGuiHelper
    {
        /// <summary>
        /// What kind of warning icon to show
        /// </summary>
        public enum OptionalIcon { None, Warning }

        /// <summary>
        /// Delegate to test if this additional Inspector UI should be added
        /// </summary>
        private Func<bool> IsRequired;

        /// <summary>
        /// Optional text of this additional Inspector UI
        /// </summary>
        private string Message;

        /// <summary>
        /// Text for the button in this additional Inspector UI
        /// </summary>
        private string Button;

        /// <summary>
        /// Delegate to trigger when the optional Inspector button is pressed
        /// </summary>
        private Action ActivateHelp;

        /// <summary>
        /// What kind of warning icon to show
        /// </summary>
        private OptionalIcon Icon;

        /// <summary>
        /// Call <see cref="DrawInInspector"/> to conditionally add an inspector GUI
        /// </summary>
        /// <param name="isRequired">Delegate to test if this additional Inspector UI should be
        /// added</param>
        /// <param name="activateHelp">Delegate to trigger when the optional Inspector button
        /// is pressed</param>
        /// <param name="message">Optional text of this additional Inspector UI</param>
        /// <param name="button">Text for the button in this additional Inspector UI</param>
        /// <param name="icon">What kind of warning icon to show</param>
        public InspectorGuiHelper(Func<bool> isRequired, Action activateHelp, string message,
        string button, OptionalIcon icon)
        {
            IsRequired = isRequired;
            Message = message;
            Button = button;
            ActivateHelp = activateHelp;
            Icon = icon;
        }

        /// <summary>
        /// Call this in the <see cref="Editor.OnInspectorGUI"/> method of a custom Editor.
        /// </summary>
        public void DrawInInspector()
        {
            if (IsRequired() && RenderWarningWithButton(Message, Button, Icon))
            {
                ActivateHelp();
            }
        }

        private static bool RenderWarningWithButton(string labelString, string buttonString,
        OptionalIcon Icon)
        {
            bool isButtonClicked = false;
            GUIContent icon = Icon switch
            {
                OptionalIcon.Warning => EditorGUIUtility.IconContent("console.warnicon@2x"),
                _ => null
            };
            var alignByCenter = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter
            };
            using (var z = new EditorGUILayout.VerticalScope("HelpBox"))
            {
                float horizontalSpace = 0;
                bool hasLabel = !string.IsNullOrEmpty(labelString);
                if (icon != null)
                {
                    EditorGUI.LabelField(z.rect, icon, EditorStyles.helpBox);
                    GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
                    horizontalSpace = EditorGUIUtility.singleLineHeight + 5 +
                        (hasLabel ? EditorGUIUtility.standardVerticalSpacing * 5 : 0);
                }
                if (hasLabel)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(horizontalSpace);
                    GUILayout.Label(labelString, alignByCenter);
                    GUILayout.EndHorizontal();
                }
                if (!string.IsNullOrEmpty(buttonString))
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(horizontalSpace);
                    isButtonClicked = GUILayout.Button(buttonString);
                    GUILayout.EndHorizontal();
                }
            }
            return isButtonClicked;
        }
    }
}
