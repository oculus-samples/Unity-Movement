// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using UnityEditor;
using UnityEngine;

namespace Meta.XR.Movement.Retargeting.Editor
{
    [CustomEditor(typeof(MetaSourceDataProvider), true)]
    public class MetaSourceDataProviderEditor : UnityEditor.Editor
    {
        private SerializedProperty _validBodyTrackingDelay, _enableAIMotionSynthesizer, _aiMotionSynthesizerConfig;
        private SerializedProperty _debugDrawSkeleton, _debugSkeletonColor;
        private SerializedProperty _providedSkeletonType;

        private static readonly Color _headerColor = new Color(0.3f, 0.5f, 0.7f, 0.2f);
        private static readonly Color _debugColor = new Color(0.4f, 0.6f, 0.4f, 0.15f);
        private static readonly Color _aiMotionSynthesizerColor = new Color(0.7f, 0.5f, 0.3f, 0.15f);

        protected virtual void OnEnable()
        {
            _providedSkeletonType = serializedObject.FindProperty("_providedSkeletonType");
            _validBodyTrackingDelay = serializedObject.FindProperty("_validBodyTrackingDelay");
            _enableAIMotionSynthesizer = serializedObject.FindProperty("_enableAIMotionSynthesizer");
            _aiMotionSynthesizerConfig = serializedObject.FindProperty("_aiMotionSynthesizerConfig");
            _debugDrawSkeleton = serializedObject.FindProperty("_debugDrawSkeleton");
            _debugSkeletonColor = serializedObject.FindProperty("_debugSkeletonColor");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((MonoBehaviour)target), GetType(), false);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(4);

            DrawSection("Body Tracking", _headerColor, () =>
            {
                if (_providedSkeletonType != null)
                {
                    EditorGUILayout.PropertyField(_providedSkeletonType, new GUIContent("Body Joint Set"));
                }
                if (_validBodyTrackingDelay != null)
                {
                    EditorGUILayout.PropertyField(_validBodyTrackingDelay);
                }
            });

            EditorGUILayout.Space(4);

            DrawSection("Debug", _debugColor, () =>
            {
                if (_debugDrawSkeleton != null)
                {
                    EditorGUILayout.PropertyField(_debugDrawSkeleton);
                    if (_debugDrawSkeleton.boolValue)
                    {
                        if (_debugSkeletonColor != null)
                        {
                            EditorGUILayout.PropertyField(_debugSkeletonColor);
                        }
                    }
                }
            });

            EditorGUILayout.Space(4);

            DrawSection("AI Motion Synthesizer", _aiMotionSynthesizerColor, () =>
            {
                if (_enableAIMotionSynthesizer != null)
                {
                    EditorGUILayout.PropertyField(_enableAIMotionSynthesizer);

                    if (_enableAIMotionSynthesizer.boolValue)
                    {
                        // Show info about skeleton type requirements
                        if (_providedSkeletonType != null)
                        {
                            var skeletonType = (OVRPlugin.BodyJointSet)_providedSkeletonType.enumValueIndex;
                            if (skeletonType == OVRPlugin.BodyJointSet.FullBody)
                            {
                                EditorGUILayout.HelpBox("AI Motion Synthesizer will blend with Full Body tracking.", MessageType.Info);
                            }
                            else if (skeletonType == OVRPlugin.BodyJointSet.UpperBody)
                            {
                                EditorGUILayout.HelpBox(
                                    "AI Motion Synthesizer is compatible with Upper Body tracking. Lower body will use AI Motion Synthesizer, upper body will blend based on configuration.",
                                    MessageType.Info);
                            }
                        }

                        if (_aiMotionSynthesizerConfig != null)
                        {
                            if (!_aiMotionSynthesizerConfig.isExpanded)
                            {
                                _aiMotionSynthesizerConfig.isExpanded = true;
                            }
                            EditorGUILayout.PropertyField(_aiMotionSynthesizerConfig, true);
                        }
                    }
                }
            });

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSection(string title, Color color, System.Action content)
        {
            var orig = GUI.backgroundColor;
            GUI.backgroundColor = color;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = orig;

            var style = new GUIStyle(EditorStyles.boldLabel) { fontSize = 11, padding = new RectOffset(4, 0, 1, 1) };
            EditorGUILayout.LabelField(title, style);
            EditorGUI.indentLevel++;
            content();
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }
    }
}
