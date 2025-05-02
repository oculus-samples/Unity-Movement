// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using UnityEditor;

namespace Meta.XR.Movement.Retargeting
{
    /// <summary>
    /// Custom property drawer for the SkeletonRetargeter class.
    /// </summary>
    [CustomPropertyDrawer(typeof(SkeletonRetargeter))]
    public class SkeletonRetargeterPropertyDrawer : PropertyDrawer
    {
        private static readonly (string propertyName, GUIContent label)[] _properties =
        {
            ("_retargetingBehavior", new GUIContent("Retargeting Behavior")),
            ("_currentScale", new GUIContent("Current Scale")),
            ("_applyScale", new GUIContent("Apply Scale")),
            ("_headScaleFactor", new GUIContent("Head Scale Multiplier")),
            ("_scaleRange", new GUIContent("Scale Range")),
        };

        private const int _readOnlyScalePropertyIndex = 1;
        private const int _applyScalePropertyIndex = 2;

        /// <inheritdoc cref="PropertyDrawer.OnGUI"/>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var applyScale =
                property.FindPropertyRelative(_properties[_applyScalePropertyIndex].propertyName).boolValue;
            for (var i = 0; i < _properties.Length; i++)
            {
                if (i >= _applyScalePropertyIndex + 1 && !applyScale)
                {
                    continue;
                }

                var rect = new Rect(position.x,
                    position.y + i * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing),
                    position.width, EditorGUIUtility.singleLineHeight);
                if (i == _readOnlyScalePropertyIndex)
                {
                    GUI.enabled = false;
                }
                EditorGUI.PropertyField(rect, property.FindPropertyRelative(_properties[i].propertyName),
                    _properties[i].label);
                GUI.enabled = true;
            }

            EditorGUI.EndProperty();
        }

        /// <inheritdoc cref="PropertyDrawer.GetPropertyHeight(SerializedProperty, GUIContent)"/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var applyScale =
                property.FindPropertyRelative(_properties[_applyScalePropertyIndex].propertyName).boolValue;
            var height = _properties.Length - (applyScale ? 0 : 2);
            return EditorGUIUtility.singleLineHeight * height +
                   EditorGUIUtility.standardVerticalSpacing * (height - 1);
        }
    }
}
