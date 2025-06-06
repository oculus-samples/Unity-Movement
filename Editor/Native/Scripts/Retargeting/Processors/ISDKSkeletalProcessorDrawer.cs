// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

#if ISDK_DEFINED
using Oculus.Interaction.Input;
#endif
using Meta.XR.Movement.Editor;
using UnityEditor;
using UnityEngine;

namespace Meta.XR.Movement.Retargeting.Editor
{
    /// <summary>
    /// Renders an instance of <see cref="ISDKSkeletalProcessor"/>.
    /// </summary>
    [CustomPropertyDrawer(typeof(ISDKSkeletalProcessor))]
    public class ISDKSkeletalProcessorDrawer : PropertyDrawer
    {
        private static readonly (string propertyName, GUIContent label)[] _propertiesToDraw =
        {
            ("_weight", new GUIContent("Weight")),
            ("_cameraRig", new GUIContent("Camera Rig")),
            ("_leftHand", new GUIContent("Left Hand")),
            ("_rightHand", new GUIContent("Right Hand")),
            ("_moveHandBackToOriginalPosition", new GUIContent("Move to original position")),
            ("_maxDisplacementDistance", new GUIContent("Max Displacement"))
        };

        /// <inheritdoc cref="PropertyDrawer.OnGUI"/>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var foldoutRect = new Rect(
                position.x,
                position.y - EditorGUIUtility.singleLineHeight * 1.1f,
                position.width,
                EditorGUIUtility.singleLineHeight);
            var isFoldoutExpanded = property.FindPropertyRelative("_isFoldoutExpanded");
            isFoldoutExpanded.boolValue = EditorGUI.Foldout(foldoutRect, isFoldoutExpanded.boolValue, GUIContent.none);

            if (!isFoldoutExpanded.boolValue)
            {
                EditorGUI.EndProperty();
                return;
            }

            EditorGUI.indentLevel++;

            // Draw properties
            var elementVerticalSpacing = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            for (var i = 0; i < _propertiesToDraw.Length; i++)
            {
                var propertyRect = new Rect(
                    position.x,
                    position.y + elementVerticalSpacing * i,
                    position.width,
                    EditorGUIUtility.singleLineHeight);
                var serializedProperty = property.FindPropertyRelative(_propertiesToDraw[i].propertyName);
                var serializedLabel = _propertiesToDraw[i].label;
                EditorGUI.PropertyField(propertyRect, serializedProperty, serializedLabel);
            }

            var buttonRect = new Rect(
                position.x,
                position.y + elementVerticalSpacing * _propertiesToDraw.Length,
                position.width,
                EditorGUIUtility.singleLineHeight);
            if (GUI.Button(buttonRect, "Setup"))
            {
                RunSetup(property);
            }

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        private void RunSetup(SerializedProperty property)
        {
#if ISDK_DEFINED
            var synthHands = Object.FindObjectsByType<SyntheticHand>(FindObjectsSortMode.None);

            if (synthHands is { Length: >= 2 } &&
                MSDKUtilityEditor.TryGetProcessorAtPropertyPathIndex<ISDKSkeletalProcessor>(
                    property, true, out var isdkProcessor))
            {
                var retargeter = Selection.activeGameObject.GetComponent<CharacterRetargeter>();
                Undo.RecordObject(retargeter, "Update ISDK processor");
                var firstHandIsLeft = synthHands[0].name.ToLower().Contains("left");
                var leftHand = firstHandIsLeft ? synthHands[0].gameObject : synthHands[1].gameObject;
                var rightHand = firstHandIsLeft ? synthHands[1].gameObject : synthHands[0].gameObject;
                isdkProcessor.CameraRig = Object.FindFirstObjectByType<OVRCameraRig>();
                isdkProcessor.LeftHand = leftHand;
                isdkProcessor.RightHand = rightHand;
                isdkProcessor.MaxDisplacementDistance = 0.05f;
            }
            else
            {
                Debug.LogError($"Error! Could not set up ISDK processor.");
            }
#endif
        }

        /// <inheritdoc cref="PropertyDrawer.GetPropertyHeight"/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.FindPropertyRelative("_isFoldoutExpanded").boolValue)
            {
                return 0.0f;
            }

            var numberOfProperties = _propertiesToDraw.Length;
            return
                // first is number of properties plus one for the button
                EditorGUIUtility.singleLineHeight * (numberOfProperties + 1) +
                // next is just number of properties (button excluded since it's last item)
                EditorGUIUtility.standardVerticalSpacing * numberOfProperties;
        }
    }
}
