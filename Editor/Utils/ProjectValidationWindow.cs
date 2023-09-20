// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEditor;
using UnityEngine;

namespace Oculus.Movement.Utils
{
    /// <summary>
    /// Editor window that displays information about configuring the project.
    /// </summary>
    public class ProjectValidationWindow : EditorWindow
    {
        private static ProjectValidationWindow _projectValidationWindow;

        /// <summary>
        /// Shows the project validation window.
        /// </summary>
        public static void ShowProjectValidationWindow()
        {
            if (!HasOpenInstances<ProjectValidationWindow>())
            {
                _projectValidationWindow = GetWindow<ProjectValidationWindow>();
                _projectValidationWindow.titleContent = new GUIContent("Movement Validation");
                _projectValidationWindow.Focus();
            }
        }

        private void OnEnable()
        {
            EditorWindow editorWindow = this;

            Vector2 windowSize = new Vector2(600, 300);
            editorWindow.minSize = windowSize;
            editorWindow.maxSize = windowSize;
        }

        private void OnGUI()
        {
            bool layersValid = ProjectValidation.TestLayers();
            GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
            labelStyle.richText = true;
            labelStyle.wordWrap = true;

            GUILayout.BeginVertical();
            {
                GUI.enabled = !layersValid;
                GUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    GUILayout.Label("Layers", EditorStyles.boldLabel);
                    GUILayout.Label(
                        "For the sample scenes, the following layers must be present in the project: <b>layer index " +
                        "10, layer index 11, and HiddenMesh</b>. \n\nTo help with this, you may import the Layers " +
                        "preset by first navigating to layers (<b>Edit -> Project Settings -> Tags and Layers</b>), " +
                        "then selecting the tiny settings icon located at the top right corner and choosing " +
                        "the <b>Layers</b> preset located in the <b>Samples/Settings</b> folder.",
                        labelStyle);
                    GUILayout.Space(5);
                }
                GUILayout.EndVertical();
                GUI.enabled = true;
            }
            GUILayout.EndVertical();
            GUILayout.Space(5);
        }
    }
}
