// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEditor;
using UnityEngine;

namespace Meta.XR.Movement.Retargeting.Editor
{
    /// <summary>
    /// Renders an instance of <see cref="HipPinningSkeletalProcessor"/>.
    /// </summary>
    [CustomPropertyDrawer(typeof(HipPinningSkeletalProcessor))]
    public class HipPinningSkeletalProcessorDrawer : PropertyDrawer
    {
        private static readonly (string propertyName, GUIContent label)[] _propertiesToDraw =
        {
            ("_weight", new GUIContent("Weight")),
            ("_hipPinningThreshold", new GUIContent("Hip Pinning Threshold")),
            ("_hipPinningObject", new GUIContent("Hip Pinning Object")),
            ("_hipPinningTargetParent", new GUIContent("Hip Pinning Target Parent")),
            ("_targetHips", new GUIContent("Hip Target")),
            ("_leftData", new GUIContent("Left Data")),
            ("_rightData", new GUIContent("Right Data")),
        };

        private const int _hipPinningDataIndex = 4;

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
                if (i > _hipPinningDataIndex + 1)
                {
                    propertyRect.y = position.y + elementVerticalSpacing * (i + 1) +
                                     EditorGUI.GetPropertyHeight(
                                         property.FindPropertyRelative(_propertiesToDraw[i - 1].propertyName),
                                         true);
                }

                var serializedProperty = property.FindPropertyRelative(_propertiesToDraw[i].propertyName);
                var serializedLabel = _propertiesToDraw[i].label;
                if (i == _hipPinningDataIndex)
                {
                    EditorGUI.PropertyField(propertyRect, serializedProperty, serializedLabel);
                    EditorGUI.indentLevel++;
                    propertyRect.y += elementVerticalSpacing;
                    EditorGUI.LabelField(propertyRect, "Hip Pinning Left Leg Data");
                }
                else if (i == _hipPinningDataIndex + 1)
                {
                    EditorGUI.indentLevel++;
                    propertyRect.y += elementVerticalSpacing;
                    EditorGUI.PropertyField(propertyRect, serializedProperty, serializedLabel);
                    EditorGUI.indentLevel--;
                }
                else if (i == _hipPinningDataIndex + 2)
                {
                    propertyRect.y -= elementVerticalSpacing;
                    EditorGUI.LabelField(propertyRect, "Hip Pinning Right Leg Data");
                    EditorGUI.indentLevel++;
                    propertyRect.y += elementVerticalSpacing;
                    EditorGUI.PropertyField(propertyRect, serializedProperty, serializedLabel);
                    EditorGUI.indentLevel--;
                    EditorGUI.indentLevel--;
                }
                else
                {
                    EditorGUI.PropertyField(propertyRect, serializedProperty, serializedLabel);
                }
            }

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        /// <inheritdoc cref="PropertyDrawer.GetPropertyHeight"/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.FindPropertyRelative("_isFoldoutExpanded").boolValue)
            {
                return 0.0f;
            }

            var elementVerticalSpacing = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            float height = elementVerticalSpacing * (_hipPinningDataIndex + 3);

            // Add height for indented properties
            for (var i = _hipPinningDataIndex; i < _propertiesToDraw.Length; i++)
            {
                var prop = property.FindPropertyRelative(_propertiesToDraw[i].propertyName);
                height += EditorGUI.GetPropertyHeight(prop, true);
            }

            return height;
        }
    }
}
