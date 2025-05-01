// Copyright (c) Meta Platforms, Inc. and affiliates.

#if INTERACTION_OVR_DEFINED
using Oculus.Interaction.Locomotion;
#endif
using UnityEngine;
using UnityEditor;

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
            ("_firstPersonLocomotor", new GUIContent("First Person Locomotor")),
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
#if INTERACTION_OVR_DEFINED
            var retargeter = Selection.activeGameObject.GetComponent<CharacterRetargeter>();
            var locomotor = Object.FindObjectOfType<FirstPersonLocomotor>();

            if (retargeter != null && locomotor != null)
            {
                int numProcessors = retargeter.TargetProcessorContainers.Length;

                // Only operate on the serialized property at our index. Otherwise, we might
                // run setup on multiple.
                int indexOfSerializedProperty = MSDKUtilityHelper.GetIndexFromPropertyPath(property.propertyPath);
                if (indexOfSerializedProperty < 0 || indexOfSerializedProperty >= numProcessors)
                {
                    Debug.LogError($"Index of serialized processor is invalid: {indexOfSerializedProperty}. " +
                                   $"Valid range is 0-{numProcessors - 1}.");
                }
                else
                {
                    var currentProcessor =
                        retargeter.TargetProcessorContainers[indexOfSerializedProperty].GetCurrentProcessor();
                    if (currentProcessor is not LocomotionSkeletalProcessor locomotionProcessor)
                    {
                        Debug.LogError($"Processor at {indexOfSerializedProperty} is not a Locomotion processor.");
                    }
                    else
                    {
                        locomotionProcessor.FirstPersonLocomotor = locomotor;
                        locomotionProcessor.CameraRig = Object.FindObjectOfType<OVRCameraRig>().transform;
                        locomotionProcessor.Animator = retargeter.GetComponent<Animator>();
                        EditorUtility.SetDirty(retargeter);
                    }
                }
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
