// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using UnityEditor;
using UnityEngine;

namespace Meta.XR.Movement.AI.Editor
{
    [CustomPropertyDrawer(typeof(AIMotionSynthesizerConfig))]
    public class AIMotionSynthesizerConfigDrawer : PropertyDrawer
    {
        private const string BasePath = "Packages/com.meta.xr.sdk.movement/Runtime/Native/";

        private static readonly Color HeaderColor = new(0.3f, 0.5f, 0.7f, 0.2f);
        private static readonly Color BlendColor = new(0.4f, 0.7f, 0.4f, 0.15f);
        private static readonly Color MotionColor = new(0.7f, 0.5f, 0.3f, 0.15f);
        private static readonly Color DebugColor = new(0.5f, 0.3f, 0.7f, 0.15f);

        private static readonly (string prop, string path)[] DefaultAssets =
        {
            ("Config", "Data/AIMotionSynthesizerSkeletonData.json"),
            ("ModelAsset", "Data/AIMotionSynthesizerModel.bytes"),
            ("GuidanceAsset", "Data/AIMotionSynthesizerGuidance.bytes")
        };

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => -1;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            AutoAssignFilesAndInputProvider(property);

            property.isExpanded = EditorGUILayout.Foldout(property.isExpanded, label, true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                DrawAllSections(property);
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        private void DrawAllSections(SerializedProperty property)
        {
            AIMotionSynthesizerEditorUtils.DrawSection("Assets", HeaderColor, () =>
            {
                EditorGUILayout.PropertyField(property.FindPropertyRelative("Config"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("ModelAsset"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("GuidanceAsset"));
                EditorGUILayout.Space(4);
                if (GUILayout.Button("Load Default Assets"))
                {
                    LoadAssets(property);
                }
            });

            EditorGUILayout.Space(4);

            AIMotionSynthesizerEditorUtils.DrawSection("Blend Settings", BlendColor, () => DrawBlendSettings(property));

            EditorGUILayout.Space(4);

            AIMotionSynthesizerEditorUtils.DrawSection("Motion", MotionColor, () =>
            {
                var rootMotionModeProp = property.FindPropertyRelative("RootMotionMode");
                if (rootMotionModeProp == null)
                {
                    return;
                }

                EditorGUILayout.PropertyField(rootMotionModeProp);
                if (rootMotionModeProp.enumValueIndex == (int)RootMotionMode.ApplyFromReference)
                {
                    var referenceProp = property.FindPropertyRelative("ReferenceTransform");
                    if (referenceProp != null)
                    {
                        EditorGUILayout.PropertyField(referenceProp);
                    }
                }
            });

            EditorGUILayout.Space(4);

            AIMotionSynthesizerEditorUtils.DrawSection("Debug", DebugColor, () =>
            {
                var debugProp = property.FindPropertyRelative("DebugDrawAIMotionSynthesizer");
                EditorGUILayout.PropertyField(debugProp);
                if (debugProp.boolValue)
                {
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("DebugAIMotionSynthesizerColor"));
                }
            });
        }

        private void AutoAssignFilesAndInputProvider(SerializedProperty property)
        {
            bool changed = false;

            foreach (var (prop, path) in DefaultAssets)
            {
                var p = property.FindPropertyRelative(prop);
                if (p?.objectReferenceValue == null)
                {
                    var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(BasePath + path);
                    if (asset != null)
                    {
                        p.objectReferenceValue = asset;
                        changed = true;
                    }
                }
            }

            var inputProviderProp = property.FindPropertyRelative("InputProvider");
            if (inputProviderProp?.objectReferenceValue == null &&
                property.serializedObject.targetObject is MonoBehaviour mb)
            {
                var inputProvider = mb.GetComponent<IAIMotionSynthesizerInputProvider>();
                if (inputProvider != null)
                {
                    inputProviderProp.objectReferenceValue = inputProvider as MonoBehaviour;
                    changed = true;
                }
            }

            var rootMotionModeProp = property.FindPropertyRelative("RootMotionMode");
            var referenceTransformProp = property.FindPropertyRelative("ReferenceTransform");
            if (rootMotionModeProp != null &&
                rootMotionModeProp.enumValueIndex == (int)RootMotionMode.ApplyFromReference &&
                referenceTransformProp?.objectReferenceValue == null)
            {
                var cameraRig = Object.FindAnyObjectByType<OVRCameraRig>();
                if (cameraRig != null)
                {
                    referenceTransformProp.objectReferenceValue = cameraRig.transform;
                    changed = true;
                }
            }

            if (changed)
            {
                property.serializedObject.ApplyModifiedProperties();
            }
        }

        internal static bool HasDefaultAssets(SerializedProperty property)
        {
            foreach (var (prop, path) in DefaultAssets)
            {
                if (IsDefaultAsset(property.FindPropertyRelative(prop), path))
                {
                    return true;
                }
            }
            return false;
        }

        internal static void ClearDefaultAssets(SerializedProperty property)
        {
            foreach (var (prop, path) in DefaultAssets)
            {
                var p = property.FindPropertyRelative(prop);
                if (IsDefaultAsset(p, path))
                {
                    p.objectReferenceValue = null;
                }
            }
        }

        private static bool IsDefaultAsset(SerializedProperty p, string path) =>
            p?.objectReferenceValue != null && AssetDatabase.GetAssetPath(p.objectReferenceValue) == BasePath + path;

        private void LoadAssets(SerializedProperty property)
        {
            bool changed = false;
            foreach (var (prop, path) in DefaultAssets)
            {
                var p = property.FindPropertyRelative(prop);
                if (p?.objectReferenceValue == null)
                {
                    var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(BasePath + path);
                    if (asset != null)
                    {
                        p.objectReferenceValue = asset;
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                property.serializedObject.ApplyModifiedProperties();
            }
        }

        private void DrawBlendSettings(SerializedProperty property)
        {
            var blendModeProp = property.FindPropertyRelative("BlendMode");
            if (blendModeProp != null)
            {
                EditorGUILayout.PropertyField(blendModeProp);

                if (blendModeProp.enumValueIndex == 0)
                {
                    DrawManualBlendMode(property);
                }
                else if (blendModeProp.enumValueIndex == 1)
                {
                    DrawInputBlendMode(property);
                }
            }

            var synthPoseProp = property.FindPropertyRelative("EnableSynthesizedStandingPose");
            if (synthPoseProp != null)
            {
                EditorGUILayout.PropertyField(synthPoseProp,
                    new GUIContent("Enable Synthesized Standing Pose",
                        "When enabled, uses synthesized standing pose with blend factor always set to 1 instead of the blended pose."));

                if (!synthPoseProp.boolValue)
                {
                    DrawBodySourceSettings(property);
                }
            }

            DrawRootAlignmentDirection(property);
        }

        private void DrawManualBlendMode(SerializedProperty property)
        {
            var blendProp = property.FindPropertyRelative("BlendFactor");
            if (blendProp != null)
            {
                EditorGUILayout.Slider(blendProp, 0f, 1f, new GUIContent("Blend Factor"));
            }

            DrawPropertyIfExists(property, "ManualVelocity");
            DrawPropertyIfExists(property, "ManualDirection");
        }

        private void DrawInputBlendMode(SerializedProperty property)
        {
            var inputProviderProp = property.FindPropertyRelative("InputProvider");
            if (inputProviderProp != null)
            {
                EditorGUILayout.PropertyField(inputProviderProp);

                if (inputProviderProp.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox(
                        "Input Provider is required when using Input blend mode. Click the button below to add a default AIMotionSynthesizerJoystickInput component.",
                        MessageType.Warning);

                    if (GUILayout.Button("Add AIMotionSynthesizerJoystickInput"))
                    {
                        AddDefaultInputProvider(property);
                    }
                }
            }

            DrawPropertyIfExists(property, "BlendInTime");
            DrawPropertyIfExists(property, "BlendOutTime");
            DrawPropertyIfExists(property, "InputActiveThreshold");
        }

        private static void DrawPropertyIfExists(SerializedProperty property, string name)
        {
            var prop = property.FindPropertyRelative(name);
            if (prop != null)
            {
                EditorGUILayout.PropertyField(prop);
            }
        }

        private void DrawBodySourceSettings(SerializedProperty property)
        {
            DrawPropertyIfExists(property, "UpperBodySource");
            DrawPropertyIfExists(property, "LowerBodySource");
        }

        private void DrawRootAlignmentDirection(SerializedProperty property)
        {
            var prop = property.FindPropertyRelative("RootAlignmentDirection");
            if (prop != null)
            {
                EditorGUILayout.PropertyField(prop,
                    new GUIContent("Root Alignment",
                        "Which pose's forward direction to align to during blending. " +
                        "BodyTracking: blended result follows user's facing direction. " +
                        "AIMotionSynthesizer: blended result follows procedural animation direction."));
            }
        }

        private void AddDefaultInputProvider(SerializedProperty property)
        {
            if (property.serializedObject.targetObject is not MonoBehaviour mb)
            {
                return;
            }

            var existingInput = mb.GetComponent<AIMotionSynthesizerJoystickInput>() ??
                               Undo.AddComponent<AIMotionSynthesizerJoystickInput>(mb.gameObject);

            var inputProviderProp = property.FindPropertyRelative("InputProvider");
            if (inputProviderProp != null)
            {
                inputProviderProp.objectReferenceValue = existingInput;
                property.serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
