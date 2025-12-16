// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using UnityEditor;
using UnityEngine;

namespace Meta.XR.Movement.AI.Editor
{
    [CustomPropertyDrawer(typeof(AIMotionSynthesizerConfig))]
    public class AIMotionSynthesizerConfigDrawer : PropertyDrawer
    {
        private const string _basePath = "Packages/com.meta.xr.sdk.movement/Runtime/Native/";

        private static readonly Color _headerColor = new Color(0.3f, 0.5f, 0.7f, 0.2f);
        private static readonly Color _blendColor = new Color(0.4f, 0.7f, 0.4f, 0.15f);
        private static readonly Color _motionColor = new Color(0.7f, 0.5f, 0.3f, 0.15f);
        private static readonly Color _debugColor = new Color(0.5f, 0.3f, 0.7f, 0.15f);

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => -1;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            AutoAssignFilesAndInputProvider(property);

            property.isExpanded = EditorGUILayout.Foldout(property.isExpanded, label, true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                AIMotionSynthesizerEditorUtils.DrawSection("Assets", _headerColor, () =>
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

                AIMotionSynthesizerEditorUtils.DrawSection("Blend Settings", _blendColor, () => DrawBlendSettings(property));

                EditorGUILayout.Space(4);

                AIMotionSynthesizerEditorUtils.DrawSection("Motion", _motionColor, () =>
                {
                    var rootMotionModeProp = property.FindPropertyRelative("RootMotionMode");
                    if (rootMotionModeProp != null)
                    {
                        EditorGUILayout.PropertyField(rootMotionModeProp);

                        if (rootMotionModeProp.enumValueIndex == (int)RootMotionMode.ApplyFromReference)
                        {
                            var referenceTransformProp = property.FindPropertyRelative("ReferenceTransform");
                            if (referenceTransformProp != null)
                            {
                                EditorGUILayout.PropertyField(referenceTransformProp);
                            }
                        }
                    }
                });

                EditorGUILayout.Space(4);

                AIMotionSynthesizerEditorUtils.DrawSection("Debug", _debugColor, () =>
                {
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("DebugDrawAIMotionSynthesizer"));

                    if (property.FindPropertyRelative("DebugDrawAIMotionSynthesizer").boolValue)
                    {
                        EditorGUILayout.PropertyField(property.FindPropertyRelative("DebugAIMotionSynthesizerColor"));
                    }
                });

                EditorGUI.indentLevel--;
            }
            EditorGUI.EndProperty();
        }

        private void AutoAssignFilesAndInputProvider(SerializedProperty property)
        {
            bool changed = false;

            var files = new[] {
                ("Config", "Data/AIMotionSynthesizerSkeletonData.json"),
                ("ModelAsset", "Data/AIMotionSynthesizerModel.bytes"),
                ("GuidanceAsset", "Data/AIMotionSynthesizerGuidance.bytes")
            };

            foreach (var (prop, path) in files)
            {
                var p = property.FindPropertyRelative(prop);
                if (p?.objectReferenceValue == null)
                {
                    var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(_basePath + path);
                    if (asset != null)
                    {
                        p.objectReferenceValue = asset;
                        changed = true;
                    }
                }
            }

            var inputProviderProp = property.FindPropertyRelative("InputProvider");
            if (inputProviderProp?.objectReferenceValue == null)
            {
                var serializedObject = property.serializedObject;
                if (serializedObject.targetObject is MonoBehaviour mb)
                {
                    var inputProvider = mb.GetComponent<IAIMotionSynthesizerInputProvider>();
                    if (inputProvider != null)
                    {
                        inputProviderProp.objectReferenceValue = inputProvider as MonoBehaviour;
                        changed = true;
                    }
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

        private void LoadAssets(SerializedProperty property)
        {
            var files = new[] {
                ("Config", "Data/AIMotionSynthesizerSkeletonData.json"),
                ("ModelAsset", "AIMotionSynthesizer/AIMotionSynthesizerModel.bytes"),
                ("StyleAsset", "AIMotionSynthesizer/AIMotionSynthesizerStyle.bytes")
            };

            bool changed = false;
            foreach (var (prop, path) in files)
            {
                var p = property.FindPropertyRelative(prop);
                if (p?.objectReferenceValue == null)
                {
                    var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(_basePath + path);
                    if (asset != null)
                    {
                        p.objectReferenceValue = asset;
                        changed = true;
                    }
                }
            }
            if (changed) property.serializedObject.ApplyModifiedProperties();
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

            var enableSynthesizedStandingPoseProp = property.FindPropertyRelative("EnableSynthesizedStandingPose");
            if (enableSynthesizedStandingPoseProp != null)
            {
                EditorGUILayout.PropertyField(enableSynthesizedStandingPoseProp,
                    new GUIContent("Enable Synthesized Standing Pose",
                        "When enabled, uses synthesized standing pose with blend factor always set to 1 instead of the blended pose."));
            }

            if (enableSynthesizedStandingPoseProp == null || !enableSynthesizedStandingPoseProp.boolValue)
            {
                DrawBodySourceSettings(property);
            }
        }

        private void DrawManualBlendMode(SerializedProperty property)
        {
            var blendProp = property.FindPropertyRelative("BlendFactor");
            if (blendProp != null)
            {
                EditorGUILayout.Slider(blendProp, 0f, 1f, new GUIContent("Blend Factor"));
            }

            var manualVelocityProp = property.FindPropertyRelative("ManualVelocity");
            if (manualVelocityProp != null)
            {
                EditorGUILayout.PropertyField(manualVelocityProp);
            }

            var manualDirectionProp = property.FindPropertyRelative("ManualDirection");
            if (manualDirectionProp != null)
            {
                EditorGUILayout.PropertyField(manualDirectionProp);
            }
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

            var blendInTimeProp = property.FindPropertyRelative("BlendInTime");
            if (blendInTimeProp != null)
            {
                EditorGUILayout.PropertyField(blendInTimeProp);
            }

            var blendOutTimeProp = property.FindPropertyRelative("BlendOutTime");
            if (blendOutTimeProp != null)
            {
                EditorGUILayout.PropertyField(blendOutTimeProp);
            }

            var inputActiveThresholdProp = property.FindPropertyRelative("InputActiveThreshold");
            if (inputActiveThresholdProp != null)
            {
                EditorGUILayout.PropertyField(inputActiveThresholdProp);
            }
        }

        private void DrawBodySourceSettings(SerializedProperty property)
        {
            var upperBodySourceProp = property.FindPropertyRelative("UpperBodySource");
            if (upperBodySourceProp != null)
            {
                EditorGUILayout.PropertyField(upperBodySourceProp);
            }

            var lowerBodySourceProp = property.FindPropertyRelative("LowerBodySource");
            if (lowerBodySourceProp != null)
            {
                EditorGUILayout.PropertyField(lowerBodySourceProp);
            }
        }

        private void AddDefaultInputProvider(SerializedProperty property)
        {
            var serializedObject = property.serializedObject;
            if (serializedObject.targetObject is MonoBehaviour mb)
            {
                var existingJoystickInput = mb.GetComponent<AIMotionSynthesizerJoystickInput>();
                if (existingJoystickInput == null)
                {
                    existingJoystickInput = Undo.AddComponent<AIMotionSynthesizerJoystickInput>(mb.gameObject);
                }

                var inputProviderProp = property.FindPropertyRelative("InputProvider");
                if (inputProviderProp != null)
                {
                    inputProviderProp.objectReferenceValue = existingJoystickInput;
                    property.serializedObject.ApplyModifiedProperties();
                }
            }
        }
    }
}
