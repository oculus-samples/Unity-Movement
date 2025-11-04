// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using UnityEditor;
using UnityEngine;
using Meta.XR.Movement.Editor;
#if ISDK_DEFINED
using Oculus.Interaction;
using Oculus.Interaction.Input;
#endif

namespace Meta.XR.Movement.Retargeting.Editor
{
    /// <summary>
    /// Custom property drawer for CCDSkeletalData to handle conditional field display and ISDK hand auto-assignment.
    /// </summary>
    [CustomPropertyDrawer(typeof(CCDSkeletalProcessor.CCDSkeletalData))]
    public class CCDSkeletalDataDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var targetTypeProperty = property.FindPropertyRelative("TargetType");
            var targetProperty = property.FindPropertyRelative("Target");
            var ikChainProperty = property.FindPropertyRelative("IKChain");
            var toleranceProperty = property.FindPropertyRelative("Tolerance");
            var maxIterationsProperty = property.FindPropertyRelative("MaxIterations");

            var currentY = position.y;
            var lineHeight = EditorGUIUtility.singleLineHeight;
            var spacing = EditorGUIUtility.standardVerticalSpacing;

            // Draw IK Chain
            var ikChainRect = new Rect(position.x, currentY, position.width,
                EditorGUI.GetPropertyHeight(ikChainProperty));
            EditorGUI.PropertyField(ikChainRect, ikChainProperty);
            currentY += EditorGUI.GetPropertyHeight(ikChainProperty) + spacing;

            // Draw Target Type
            var targetTypeRect = new Rect(position.x, currentY, position.width, lineHeight);
            EditorGUI.PropertyField(targetTypeRect, targetTypeProperty);
            currentY += lineHeight + spacing;

            var targetType = (CCDSkeletalProcessor.TargetType)targetTypeProperty.enumValueIndex;

            // Draw conditional fields based on target type
            switch (targetType)
            {
                case CCDSkeletalProcessor.TargetType.Transform:
                    var targetRect = new Rect(position.x, currentY, position.width, lineHeight);
                    EditorGUI.PropertyField(targetRect, targetProperty);
                    currentY += lineHeight + spacing;
                    break;
                case CCDSkeletalProcessor.TargetType.TrackedLeftHand:
                case CCDSkeletalProcessor.TargetType.TrackedRightHand:
                case CCDSkeletalProcessor.TargetType.TrackedHead:
                    // No additional fields needed for tracked targets
                    break;
            }

            // Draw Tolerance
            var toleranceRect = new Rect(position.x, currentY, position.width, lineHeight);
            EditorGUI.PropertyField(toleranceRect, toleranceProperty);
            currentY += lineHeight + spacing;

            // Draw Max Iterations
            var maxIterationsRect = new Rect(position.x, currentY, position.width, lineHeight);
            EditorGUI.PropertyField(maxIterationsRect, maxIterationsProperty);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var targetTypeProperty = property.FindPropertyRelative("TargetType");
            var ikChainProperty = property.FindPropertyRelative("IKChain");
            var targetType = (CCDSkeletalProcessor.TargetType)targetTypeProperty.enumValueIndex;

            var height =
                EditorGUI.GetPropertyHeight(ikChainProperty) + EditorGUIUtility.standardVerticalSpacing; // IK Chain
            height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // Target Type

            // Add height for conditional fields
            switch (targetType)
            {
                case CCDSkeletalProcessor.TargetType.Transform:
                    height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    break;
            }

            height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // Tolerance
            height += EditorGUIUtility.singleLineHeight; // Max Iterations

            return height;
        }
    }
}
