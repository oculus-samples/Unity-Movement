// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using UnityEditor;
using UnityEngine;

namespace Meta.XR.Movement.AI.Editor
{
    [CustomEditor(typeof(AIMotionSynthesizerJoystickInput))]
    public class AIMotionSynthesizerJoystickInputEditor : UnityEditor.Editor
    {
        private SerializedProperty _inputModeProperty, _referenceTransformProperty;
#if USE_UNITY_INPUT_SYSTEM
        private SerializedProperty _moveActionProperty, _lookActionProperty, _sprintActionProperty;
#endif
        private SerializedProperty _mainControllerProperty, _moveAxisProperty, _lookAxisProperty, _sprintButtonProperty;
        private SerializedProperty _speedFactorProperty, _sprintSpeedFactorProperty, _accelerationProperty, _groundDampingProperty, _joystickThresholdProperty;

        private static readonly Color _headerColor = new Color(0.3f, 0.5f, 0.7f, 0.2f);
        private static readonly Color _unityInputColor = new Color(0.4f, 0.7f, 0.4f, 0.15f);
        private static readonly Color _ovrInputColor = new Color(0.7f, 0.5f, 0.3f, 0.15f);

        private void OnEnable()
        {
            _inputModeProperty = serializedObject.FindProperty("_inputMode");
            _referenceTransformProperty = serializedObject.FindProperty("_referenceTransform");
#if USE_UNITY_INPUT_SYSTEM
            _moveActionProperty = serializedObject.FindProperty("_moveAction");
            _lookActionProperty = serializedObject.FindProperty("_lookAction");
            _sprintActionProperty = serializedObject.FindProperty("_sprintAction");
#endif
            _mainControllerProperty = serializedObject.FindProperty("_mainController");
            _moveAxisProperty = serializedObject.FindProperty("_moveAxis");
            _lookAxisProperty = serializedObject.FindProperty("_lookAxis");
            _sprintButtonProperty = serializedObject.FindProperty("_sprintButton");
            _speedFactorProperty = serializedObject.FindProperty("_speedFactor");
            _sprintSpeedFactorProperty = serializedObject.FindProperty("_sprintSpeedFactor");
            _accelerationProperty = serializedObject.FindProperty("_acceleration");
            _groundDampingProperty = serializedObject.FindProperty("_groundDamping");
            _joystickThresholdProperty = serializedObject.FindProperty("_joystickThreshold");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((MonoBehaviour)target), GetType(), false);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(4);

            AIMotionSynthesizerEditorUtils.DrawSection("Input Configuration", _headerColor, () =>
            {
                EditorGUILayout.PropertyField(_inputModeProperty);
                EditorGUILayout.PropertyField(_joystickThresholdProperty, new GUIContent("Dead Zone"));
            });

            EditorGUILayout.Space(4);

            AIMotionSynthesizerEditorUtils.DrawSection("Input Reference", _headerColor, () =>
            {
                EditorGUILayout.PropertyField(_referenceTransformProperty, new GUIContent("Reference Transform"));
                EditorGUILayout.HelpBox("Transform used for input calculations. Determines the coordinate space for velocity and direction. Defaults to camera rig if not set.", MessageType.Info);
            });

            EditorGUILayout.Space(4);

            var mode = (InputMode)_inputModeProperty.enumValueIndex;
#if USE_UNITY_INPUT_SYSTEM
            if (mode is InputMode.UnityInputSystem or InputMode.Both)
            {
                AIMotionSynthesizerEditorUtils.DrawSection("Unity Input System", _unityInputColor, () =>
                {
                    EditorGUILayout.PropertyField(_moveActionProperty, new GUIContent("Move Action"));
                    EditorGUILayout.PropertyField(_lookActionProperty, new GUIContent("Look Action"));
                    EditorGUILayout.PropertyField(_sprintActionProperty, new GUIContent("Sprint Action"));
                });
                EditorGUILayout.Space(4);
            }
#else
            if (mode == InputMode.UnityInputSystem || mode == InputMode.Both)
            {
                EditorGUILayout.HelpBox("Unity Input System is not available. Define USE_UNITY_INPUT_SYSTEM to enable.", MessageType.Warning);
                EditorGUILayout.Space(4);
            }
#endif

            if (mode is InputMode.OVRInput or InputMode.Both)
            {
                AIMotionSynthesizerEditorUtils.DrawSection("OVR Input", _ovrInputColor, () =>
                {
                    EditorGUILayout.PropertyField(_mainControllerProperty);
                    EditorGUILayout.PropertyField(_moveAxisProperty);
                    EditorGUILayout.PropertyField(_lookAxisProperty);
                    EditorGUILayout.PropertyField(_sprintButtonProperty);
                });
                EditorGUILayout.Space(4);
            }

            AIMotionSynthesizerEditorUtils.DrawSection("Locomotion Parameters", _headerColor, () =>
            {
                AIMotionSynthesizerEditorUtils.DrawPropertyWithBar(_speedFactorProperty, 0f, 10f);
                AIMotionSynthesizerEditorUtils.DrawPropertyWithBar(_sprintSpeedFactorProperty, 0f, 10f);
                AIMotionSynthesizerEditorUtils.DrawPropertyWithBar(_accelerationProperty, 0f, 20f);
                AIMotionSynthesizerEditorUtils.DrawPropertyWithBar(_groundDampingProperty, 0f, 100f);
                EditorGUILayout.Space(4);
            });

            serializedObject.ApplyModifiedProperties();
        }
    }
}
