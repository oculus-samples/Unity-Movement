// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

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
            ("_hideLowerBodyWhenUpperBodyTracking", new GUIContent("Hide Lower Body when using Upper Body Tracking")),
            ("_hideLegScale", new GUIContent("Leg Scale when using Hide Lower Body")),
            ("_applyScale", new GUIContent("Apply Scale")),
            ("_currentScale", new GUIContent("Current Scale")),
            ("_headScaleFactor", new GUIContent("Head Scale Multiplier")),
            ("_scaleRange", new GUIContent("Scale Range")),
        };

        private const int _hideLowerBodyPropertyIndex = 1;
        private const int _hideLegScalePropertyIndex = 2;
        private const int _applyScalePropertyIndex = 3;
        private const int _readOnlyScalePropertyIndex = 4;

        /// <inheritdoc cref="PropertyDrawer.OnGUI"/>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var applyScale =
                property.FindPropertyRelative(_properties[_applyScalePropertyIndex].propertyName).boolValue;
            var hideLowerBody =
                property.FindPropertyRelative(_properties[_hideLowerBodyPropertyIndex].propertyName).boolValue;

            int visiblePropertyCount = 0;
            for (var i = 0; i < _properties.Length; i++)
            {
                // Skip scale-related properties if scale is not applied
                if (i >= _applyScalePropertyIndex + 1 && !applyScale)
                {
                    continue;
                }

                // Skip leg scale property if hide lower body is not enabled
                if (i == _hideLegScalePropertyIndex && !hideLowerBody)
                {
                    continue;
                }

                var rect = new Rect(position.x,
                    position.y + visiblePropertyCount * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing),
                    position.width, EditorGUIUtility.singleLineHeight);
                if (i == _readOnlyScalePropertyIndex)
                {
                    GUI.enabled = false;
                }
                EditorGUI.PropertyField(rect, property.FindPropertyRelative(_properties[i].propertyName),
                    _properties[i].label);
                GUI.enabled = true;
                visiblePropertyCount++;
            }

            EditorGUI.EndProperty();
        }

        /// <inheritdoc cref="PropertyDrawer.GetPropertyHeight(SerializedProperty, GUIContent)"/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var applyScale =
                property.FindPropertyRelative(_properties[_applyScalePropertyIndex].propertyName).boolValue;
            var hideLowerBody =
                property.FindPropertyRelative(_properties[_hideLowerBodyPropertyIndex].propertyName).boolValue;

            // Calculate how many properties are hidden
            int hiddenProperties = 0;

            // Hide scale-related properties if scale is not applied
            if (!applyScale)
            {
                hiddenProperties += 3; // _currentScale, _headScaleFactor, _scaleRange
            }

            // Hide leg scale property if hide lower body is not enabled
            if (!hideLowerBody)
            {
                hiddenProperties += 1; // _hideLegScale
            }

            var visibleProperties = _properties.Length - hiddenProperties;
            return EditorGUIUtility.singleLineHeight * visibleProperties +
                   EditorGUIUtility.standardVerticalSpacing * (visibleProperties - 1);
        }
    }
}
