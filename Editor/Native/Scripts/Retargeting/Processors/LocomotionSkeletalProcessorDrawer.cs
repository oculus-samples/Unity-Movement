// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

#if ISDK_DEFINED
using Oculus.Interaction.Locomotion;
#endif
using Meta.XR.Movement.Editor;
using UnityEditor;
using UnityEngine;

namespace Meta.XR.Movement.Retargeting.Editor
{
    /// <summary>
    /// Custom property drawer for the <see cref="LocomotionSkeletalProcessor"/> class.
    /// </summary>
    [CustomPropertyDrawer(typeof(LocomotionSkeletalProcessor))]
    public class LocomotionSkeletalProcessorDrawer : PropertyDrawer
    {
        private static readonly (string propertyName, GUIContent label)[] _propertiesToDraw =
        {
            ("_weight", new GUIContent("Weight")),
            ("_locomotionEventHandlerObject", new GUIContent("Locomotor Event Handler")),
            ("_cameraRig", new GUIContent("Camera Rig")),
            ("_animator", new GUIContent("Animator")),
            ("_animatorVerticalParam", new GUIContent("Animator Vertical Parameter")),
            ("_animatorHorizontalParam", new GUIContent("Animator Horizontal Parameter")),
            ("_animationSpeed", new GUIContent("Animation Speed"))
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
                var propertyRect = new Rect(position.x,
                    position.y + elementVerticalSpacing * i,
                    position.width, EditorGUIUtility.singleLineHeight);

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

            EditorGUI.EndProperty();
        }

        private void RunSetup(SerializedProperty property)
        {
#if ISDK_DEFINED
#if ISDK_74_OR_NEWER
            var locomotor = Object.FindFirstObjectByType<FirstPersonLocomotor>();
#else
            var locomotor = Object.FindFirstObjectByType<PlayerLocomotor>();
#endif
            if (locomotor != null && MSDKUtilityEditor.TryGetProcessorAtPropertyPathIndex<LocomotionSkeletalProcessor>(
                    property, false, out var locomotionProcessor))
            {
                var retargeter = Selection.activeGameObject.GetComponent<CharacterRetargeter>();
                Undo.RecordObject(retargeter, "Update locomotion processor");
                locomotionProcessor.LocomotionEventHandler = locomotor;
                locomotionProcessor.CameraRig = Object.FindFirstObjectByType<OVRCameraRig>().transform;
                locomotionProcessor.Animator = retargeter.GetComponent<Animator>();
                locomotionProcessor.AnimatorVerticalParam = "Vertical";
                locomotionProcessor.AnimatorHorizontalParam = "Horizontal";
                locomotionProcessor.AnimationSpeed = 2.0f;
            }
            else
            {
                Debug.LogError($"Error! Could not set up Locomotion processor.");
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
