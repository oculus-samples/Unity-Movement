// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

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

        public static bool TestPreloadedShaders()
        {
            bool hasPreloadedShaders = false;
            var settings = new SerializedObject(GraphicsSettings.GetGraphicsSettings());
            var preloadedShaders = settings.FindProperty("m_PreloadedShaders");
            var expectedSpecularKeywords = new string[]
            {
                "DIRECTIONAL",
                "LIGHTPROBE_SH",
                "_AREA_LIGHT_SPECULAR",
                "_DIFFUSE_WRAP",
                "_NORMALMAP",
                "_RECALCULATE_NORMALS",
                "_SPECGLOSSMAP",
                "_SPECULAR_AFFECT_BY_NDOTL"
            };
            var expectedMetallicKeywords = new string[]
            {
                "DIRECTIONAL",
                "LIGHTPROBE_SH",
                "_AREA_LIGHT_SPECULAR",
                "_EMISSION",
                "_DIFFUSE_WRAP",
                "_NORMALMAP",
                "_RECALCULATE_NORMALS",
                "_METALLICGLOSSMAP",
                "_SPECULAR_AFFECT_BY_NDOTL"
            };
            for (int i = 0; i < preloadedShaders.arraySize; i++)
            {
                var shaderVariantCollection = (ShaderVariantCollection)preloadedShaders.GetArrayElementAtIndex(i).objectReferenceValue;
                if (shaderVariantCollection != null)
                {
                    var specRecalcVariant = new ShaderVariantCollection.ShaderVariant(Shader.Find("Movement/PBR (Specular)"),
                        PassType.ForwardBase, expectedSpecularKeywords);
                    var metallicRecalcVariant = new ShaderVariantCollection.ShaderVariant(Shader.Find("Movement/PBR (Metallic)"),
                        PassType.ForwardBase, expectedMetallicKeywords);
                    if (shaderVariantCollection.Contains(specRecalcVariant) &&
                        shaderVariantCollection.Contains(metallicRecalcVariant))
                    {
                        hasPreloadedShaders = true;
                    }
                }
            }
            return hasPreloadedShaders;
        }

        private static bool ShouldShowWindow()
        {
            return !TestLayers() || !TestPreloadedShaders();
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
            bool shaderVariantsValid = ProjectValidation.TestPreloadedShaders();
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
                        "For the sample scenes, the following layers must be present in the project: <b>Character (layer index 10), MirroredCharacter (layer index 11), and HiddenMesh</b>. \n\nImport the Layers preset in <b>Edit -> Project Settings -> Tags and Layers</b> by selecting the tiny settings icon located at the top right corner and choosing the <b>Layers</b> preset located in the <b>Samples/Shared/Presets</b> folder.",
                        labelStyle);
                    GUILayout.Space(5);
                }
                GUILayout.EndVertical();
                GUI.enabled = true;

                GUI.enabled = !shaderVariantsValid;
                GUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    GUILayout.Label("Shader Variant Collection", EditorStyles.boldLabel);
                    GUILayout.Label(
                        "For the sample scenes, the Recalculate Normals shader variants must be included in the project.\n\nA shader variant collection (MovementPBRVariants) containing these keywords is located inside of <b>Runtime/Shaders</b>. To include this collection, copy the <b>MovementPBRVariants</b> shader variant collection located in <b>Runtime/Shaders</b> into your project folder. Head to <b>Edit -> Project Settings -> Graphics</b> and in <b>Preloaded Shaders</b> near the bottom of the window, increase the size of the array by 1 and fill the empty entry with the copied <b>MovementPBRVariants</b> shader variant collection.",
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
