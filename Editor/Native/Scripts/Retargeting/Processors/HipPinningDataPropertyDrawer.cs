// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using UnityEditor;
using UnityEngine;

namespace Meta.XR.Movement.Retargeting.Editor
{
    /// <summary>
    /// Custom property drawer for the <see cref="HipPinningSkeletalProcessor.HipPinningData"/> class.
    /// </summary>
    [CustomPropertyDrawer(typeof(HipPinningSkeletalProcessor.HipPinningData))]
    public class HipPinningDataPropertyDrawer : PropertyDrawer
    {
        /// <inheritdoc cref="PropertyDrawer.OnGUI"/>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, GUIContent.none, property);

            var elementVerticalSpacing = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(position, property.FindPropertyRelative("TargetFoot"));
            position.y += elementVerticalSpacing;
            EditorGUI.PropertyField(position, property.FindPropertyRelative("TargetHint"));
            position.y += elementVerticalSpacing;
            EditorGUI.PropertyField(position, property.FindPropertyRelative("MidLegIndex"));
            position.y += elementVerticalSpacing;
            var legIndexes = property.FindPropertyRelative("LegIndexes");
            legIndexes.isExpanded = true;
            EditorGUI.PropertyField(position, legIndexes);
            position.y += elementVerticalSpacing * legIndexes.arraySize;

            EditorGUI.EndProperty();
        }

        /// <inheritdoc cref="PropertyDrawer.GetPropertyHeight"/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var elementVerticalSpacing = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            var legIndexesProperty = property.FindPropertyRelative("LegIndexes");
            var arrayCount = legIndexesProperty?.arraySize ?? 0;

            // 2 for TargetFoot and TargetHint, plus array elements
            return elementVerticalSpacing * (5 + (arrayCount > 0 ? arrayCount + 1 : 1));
        }
    }
}
