// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using UnityEditor;
using UnityEngine;

namespace Meta.XR.Movement.AI.Editor
{
    [CustomEditor(typeof(AIMotionSynthesizerSourceDataProvider))]
    public class AIMotionSynthesizerSourceDataProviderEditor : UnityEditor.Editor
    {
        private SerializedProperty _configProperty;
        private SerializedProperty _modelAssetProperty;
        private SerializedProperty _guidanceAssetProperty;
        private SerializedProperty _inputProviderProperty;

        private static readonly Color _headerColor = new Color(0.3f, 0.5f, 0.7f, 0.2f);

        private void OnEnable()
        {
            _configProperty = serializedObject.FindProperty("_config");
            _modelAssetProperty = serializedObject.FindProperty("_modelAsset");
            _guidanceAssetProperty = serializedObject.FindProperty("_guidanceAsset");
            _inputProviderProperty = serializedObject.FindProperty("_inputProvider");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((MonoBehaviour)target), GetType(), false);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(4);

            AIMotionSynthesizerEditorUtils.DrawSection("AIMotionSynthesizer Configuration", _headerColor, () =>
            {
                EditorGUILayout.PropertyField(_configProperty);
                EditorGUILayout.PropertyField(_modelAssetProperty);
                EditorGUILayout.PropertyField(_guidanceAssetProperty);

                EditorGUILayout.Space(4);

                if (GUILayout.Button("Load Default Assets"))
                {
                    LoadDefaultAssets();
                }
            });

            EditorGUILayout.Space(4);

            AIMotionSynthesizerEditorUtils.DrawSection("Input Control", _headerColor, () =>
            {
                EditorGUILayout.PropertyField(_inputProviderProperty, new GUIContent("Input Provider"));
            });

            serializedObject.ApplyModifiedProperties();
        }

        private void LoadDefaultAssets()
        {
            bool hasChanges = false;

            if (_configProperty.objectReferenceValue == null)
            {
                var configFile = AssetDatabase.LoadAssetAtPath<TextAsset>("Packages/com.meta.xr.sdk.movement/Runtime/Native/Data/AIMotionSynthesizerSkeletonData.json");
                if (configFile != null)
                {
                    _configProperty.objectReferenceValue = configFile;
                    hasChanges = true;
                }
            }

            if (_modelAssetProperty.objectReferenceValue == null)
            {
                var modelFile = AssetDatabase.LoadAssetAtPath<TextAsset>("Packages/com.meta.xr.sdk.movement/Runtime/Native/Data/AIMotionSynthesizerModel.bytes");
                if (modelFile != null)
                {
                    _modelAssetProperty.objectReferenceValue = modelFile;
                    hasChanges = true;
                }
            }

            if (_guidanceAssetProperty.objectReferenceValue == null)
            {
                var guidanceFile = AssetDatabase.LoadAssetAtPath<TextAsset>("Packages/com.meta.xr.sdk.movement/Runtime/Native/Data/AIMotionSynthesizerGuidance.bytes");
                if (guidanceFile != null)
                {
                    _guidanceAssetProperty.objectReferenceValue = guidanceFile;
                    hasChanges = true;
                }
            }

            if (_inputProviderProperty.objectReferenceValue == null)
            {
                var provider = (AIMotionSynthesizerSourceDataProvider)target;
                var inputProviders = provider.GetComponents<MonoBehaviour>();
                foreach (var component in inputProviders)
                {
                    if (component is IAIMotionSynthesizerInputProvider)
                    {
                        _inputProviderProperty.objectReferenceValue = component;
                        hasChanges = true;
                        break;
                    }
                }
            }

            if (hasChanges)
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssets();
            }
        }
    }
}
