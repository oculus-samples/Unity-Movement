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
        /// <summary>
        /// All character and mirrored character layers should exist based on their indices.
        /// That is because the scene assets are assigned based on layer index.
        /// </summary>
        private static readonly int[] _expectedLayersIndices =
            { 10, 11 };
        private static readonly string[] _expectedLayersNotIndexed = { "HiddenMesh" };

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
            foreach (var expectedLayer in _expectedLayersNotIndexed)
            {
                if (LayerMask.NameToLayer(expectedLayer) == -1)
                {
                    return false;
                }
            }

            foreach (var layerIndex in _expectedLayersIndices)
            {
                if (LayerMask.LayerToName(layerIndex) == null)
                {
                    return false;
                }
            }

            return true;
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
