// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine;

namespace Oculus.Movement.Tracking.Deprecated
{
    /// <summary>
    /// Editor class defining interface for BlendshapeMapping.
    /// </summary>
    [CustomEditor(typeof(BlendshapeMapping))]
    public class BlendshapeMappingEditor : Editor
    {
        private StringBuilder _errors = new StringBuilder();
        private static Dictionary<string, OVRFaceExpressions.FaceExpression> _sanitizedNameCache;

        private const string _LIPS_TOWARD = "lipsToward";

        /// <inheritdoc />
        public override void OnInspectorGUI()
        {
            using (new EditorGUI.DisabledGroupScope(true))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
            }
            if (GUILayout.Button("Auto Map"))
            {
                BlendshapeMapping blendshapeMapping = (BlendshapeMapping)target;
                _errors.Clear();
                AutoMap(blendshapeMapping);
                ApplyChanges();
                Debug.Log("Done auto-mapping.");
            }
            DrawErrorText();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Meshes"));
        }

        private void DrawErrorText()
        {
            if (_errors.Length > 0)
            {
                GUIStyle style = new GUIStyle(EditorStyles.textField);
                style.normal.textColor = Color.red;
                GUILayout.TextField(_errors.ToString(), style);
            }
        }

        /// <summary>
        /// Automatically maps recognized blendshapes from skinned mesh renderers
        /// to the specified blendshape mapping.
        /// </summary>
        /// <param name="blendshapeMapping">The blendshape mapping to automap.</param>
        public void AutoMap(BlendshapeMapping blendshapeMapping)
        {
            var allMeshes = blendshapeMapping.GetComponentsInChildren<SkinnedMeshRenderer>();
            blendshapeMapping.Meshes.Clear();
            foreach (var mesh in allMeshes)
            {
                var numBlendShapes = mesh.sharedMesh.blendShapeCount;
                if (numBlendShapes == 0)
                {
                    continue;
                }
                var meshMapping = new BlendshapeMapping.MeshMapping { Mesh = mesh };
                var sharedMesh = mesh.sharedMesh;
                meshMapping.Blendshapes.Clear();
                var usedBlendshapes = new HashSet<OVRFaceExpressions.FaceExpression>();

                for (int i = 0; i < numBlendShapes; i++)
                {
                    var blendshapeName = sharedMesh.GetBlendShapeName(i);
                    blendshapeName = blendshapeName.Substring(blendshapeName.IndexOf('.') + 1);
                    var faceTrackerIndex = EnumForBlendshapeName(blendshapeName);

                    // Edge case: lips toward can be mapped to multiple times.
                    if (usedBlendshapes.Contains(faceTrackerIndex) &&
                        !IsLipsToward(blendshapeName))
                    {
                        _errors.Append($"[{i}] {blendshapeName} leads to duplicate mapping, ignoring.\n");
                        faceTrackerIndex = OVRFaceExpressions.FaceExpression.Max;
                    }
                    else if (faceTrackerIndex == OVRFaceExpressions.FaceExpression.Max)
                    {
                        _errors.Append($"[{i}] {blendshapeName} no tracker value found.\n");
                    }
                    else
                    {
                        usedBlendshapes.Add(faceTrackerIndex);
                    }
                    meshMapping.Blendshapes.Add(faceTrackerIndex);
                }
                blendshapeMapping.Meshes.Add(meshMapping);
            }
        }

        /// <summary>
        /// Returns the blendshape enum from a blendshape name string.
        /// </summary>
        /// <param name="blendshapeName">The blendshape name as a string.</param>
        /// <returns>The blendshape enum.</returns>
        public static OVRFaceExpressions.FaceExpression EnumForBlendshapeName(string blendshapeName)
        {
            blendshapeName = FilterLegacyBlendshapeNames(blendshapeName);

            if (IsLipsToward(blendshapeName))
            {
                blendshapeName = _LIPS_TOWARD;
            }

            var sanitizedName = GetSanitizedBlendshapeName(blendshapeName);
            InitializeSanitizedNameCache();

            if (_sanitizedNameCache.ContainsKey(sanitizedName))
            {
                return _sanitizedNameCache[sanitizedName];
            }
            // On some meshes, the blendshape name has a prefix. E.g. sometimes it's "jaw_drop" and
            // sometimes it's "mouth_jaw_drop." If it fails to map the first time, try again after lopping
            // off the first word
            var prefixIndex = blendshapeName.IndexOf("_");
            if (prefixIndex >= 0)
            {
                blendshapeName = blendshapeName.Substring(prefixIndex);
                sanitizedName = GetSanitizedBlendshapeName(blendshapeName);
                if (_sanitizedNameCache.ContainsKey(sanitizedName))
                {
                    return _sanitizedNameCache[sanitizedName];
                }
            }
            return OVRFaceExpressions.FaceExpression.Max;
        }

        /// <summary>
        /// There's a legacy set of blendshapes that include 4 middles shapes.
        /// Remap the old middle shapes to the equivalent to the left shape
        /// of the canonical list.
        /// </summary>
        /// <param name="blendshapeName">Input blendshape name.</param>
        /// <returns>Filtered blendshape name.</returns>
        private static string FilterLegacyBlendshapeNames(string blendshapeName)
        {
            blendshapeName = blendshapeName.Replace("_M", "_L");
            return blendshapeName;
        }

        private static bool IsLipsToward(string blendshapeName)
        {
            return blendshapeName == "lipsToward_LB" || blendshapeName == "lipsToward_RB" ||
                blendshapeName == "lipsToward_LT" || blendshapeName == "lipsToward_RT";
        }

        /// <summary>
        /// Remove underscores and force lower case.
        /// </summary>
        /// <param name="blendshapeName">Input blendshape name.</param>
        /// <returns>Sanitized blendshape name</returns>
        private static string GetSanitizedBlendshapeName(string blendshapeName)
        {
            return blendshapeName.Replace("_", "").ToLower();
        }

        private static void InitializeSanitizedNameCache()
        {
            if (_sanitizedNameCache == null)
            {
                _sanitizedNameCache = new Dictionary<string, OVRFaceExpressions.FaceExpression>();
                for (OVRFaceExpressions.FaceExpression i = 0; i <
                    OVRFaceExpressions.FaceExpression.Max; i++)
                {
                    var sanitizedEnumName = GetSanitizedBlendshapeName(i.ToString());
                    _sanitizedNameCache[sanitizedEnumName] = i;
                }
            }
        }

        private void ApplyChanges()
        {
            PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                EditorSceneManager.MarkSceneDirty(prefabStage.scene);
            }
        }
    }

    /// <summary>
    /// Editor class defining interface for the mesh mapping drawer showing blendshapes.
    /// </summary>
    [CustomPropertyDrawer(typeof(BlendshapeMapping.MeshMapping))]
    public class MeshMappingDrawer : PropertyDrawer
    {
        /// <summary>
        /// Defines the look of the drawer GUI.
        /// </summary>
        /// <param name="position">The position of the drawer.</param>
        /// <param name="property">The property that the drawer is displaying.</param>
        /// <param name="label">The text/image of the drawer's property.</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var mesh = property.FindPropertyRelative("Mesh").objectReferenceValue
                as SkinnedMeshRenderer;

            if (mesh == null)
            {
                return;
            }

            var blendshapesLabel = new GUIContent();
            blendshapesLabel.text = mesh.name;
            var blendshapes = property.FindPropertyRelative("Blendshapes");

            var headerRect = new Rect(position.x + 10,
                position.y,
                position.width - 10,
                EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(headerRect, blendshapesLabel);

            GUIStyle invalidMeshStyle = new GUIStyle(EditorStyles.textField);
            invalidMeshStyle.normal.textColor = Color.red;
            invalidMeshStyle.hover.textColor = Color.red;

            for (int i = 0; i < blendshapes.arraySize; i++)
            {
                var spIndex = blendshapes.GetArrayElementAtIndex(i);
                var blendshapeEnum = (OVRFaceExpressions.FaceExpression)spIndex.enumValueIndex;
                var blendshapeName = mesh.sharedMesh.GetBlendShapeName(i);
                blendshapeName = blendshapeName.Substring(blendshapeName.LastIndexOf('.') + 1);

                var indexRect = new Rect(
                    position.x - 20,
                    position.y + EditorGUIUtility.singleLineHeight * (i + 1),
                    40,
                    EditorGUIUtility.singleLineHeight);
                var meshNameRect = new Rect(
                    position.x + 10,
                    position.y + EditorGUIUtility.singleLineHeight * (i + 1),
                    (position.width - 5) / 2,
                    EditorGUIUtility.singleLineHeight);
                var enumNameRect = new Rect(
                    position.x + 5 + (position.width - 5) / 2,
                    position.y + EditorGUIUtility.singleLineHeight * (i + 1),
                    (position.width - 5) / 2,
                    EditorGUIUtility.singleLineHeight);

                EditorGUI.LabelField(indexRect, $"{i}");
                if (blendshapeEnum == OVRFaceExpressions.FaceExpression.Max)
                {
                    EditorGUI.TextField(meshNameRect, $"{blendshapeName}", invalidMeshStyle);
                }
                else
                {
                    EditorGUI.TextField(meshNameRect, $"{blendshapeName}");
                }

                EditorGUI.PropertyField(enumNameRect, spIndex, GUIContent.none);
            }

            EditorGUI.EndProperty();
        }

        /// <summary>
        /// Gets the height of this drawer based on the number of blendshapes.
        /// </summary>
        /// <param name="property">The property to calculate height for.</param>
        /// <param name="label">The descriptive text/image of this property.</param>
        /// <returns>The property height needed.</returns>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var blendshapes = property.FindPropertyRelative("Blendshapes");
            // need to make space for next label, along with another two lines as a spacer
            return (1 + blendshapes.arraySize) *
                (EditorGUIUtility.singleLineHeight);
        }
    }
}
