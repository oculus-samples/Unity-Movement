// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEditor;
using UnityEngine;

namespace Oculus.Movement.Utils
{
    /// <summary>
    /// Validates various project settings for the samples to work correctly.
    /// </summary>
    [InitializeOnLoad]
    public class ProjectValidation
    {
        private static readonly string[] _expectedLayers = { "Character", "MirroredCharacter", "HiddenMesh" };

        static ProjectValidation()
        {
            if (!ShouldShowWindow())
            {
                return;
            }

            ProjectValidationWindow.ShowProjectValidationWindow();
        }

        /// <summary>
        /// If all expected layers are in the project, returns true.
        /// </summary>
        /// <returns>True if all expected layers are in the project.</returns>
        public static bool TestLayers()
        {
            bool allLayersArePresent = true;
            foreach (var expectedLayer in _expectedLayers)
            {
                if (LayerMask.NameToLayer(expectedLayer) == -1)
                {
                    allLayersArePresent = false;
                    break;
                }
            }
            return allLayersArePresent;
        }

        private static bool ShouldShowWindow()
        {
            return !TestLayers();
        }
    }

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
            GUIStyle labelStyle = new GUIStyle (EditorStyles.label);
            labelStyle.richText = true;
            labelStyle.wordWrap = true;

            GUILayout.BeginVertical();
            {
                GUI.enabled = !layersValid;
                GUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    GUILayout.Label("Layers", EditorStyles.boldLabel);
                    GUILayout.Label(
                        "For the sample scenes, the following layers must be present in the project: <b>Character (layer index 10), MirroredCharacter (layer index 11), and HiddenMesh</b>. \n\nImport the Layers preset in <b>Edit -> Project Settings -> Tags and Layers</b> by selecting the tiny settings icon located at the top right corner and choosing the <b>Layers</b> preset located in the <b>Samples/Settings</b> folder.",
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
