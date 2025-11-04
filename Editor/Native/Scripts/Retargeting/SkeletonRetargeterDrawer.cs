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
            ("_applyRootScale", new GUIContent("Apply Root Scale")),
            ("_applyHeadScale", new GUIContent("Apply Head Scale")),
            ("_headScaleFactor", new GUIContent("Head Scale Multiplier")),
            ("_scaleRange", new GUIContent("Scale Range")),
            ("_currentScale", new GUIContent("Current Scale")),
        };

        private const int _hideLowerBodyPropertyIndex = 1;
        private const int _hideLegScalePropertyIndex = 2;
        private const int _applyRootScalePropertyIndex = 3;
        private const int _applyHeadScalePropertyIndex = 4;
        private const int _readOnlyScalePropertyIndex = 7;

        /// <inheritdoc cref="PropertyDrawer.OnGUI"/>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var applyRootScale =
                property.FindPropertyRelative(_properties[_applyRootScalePropertyIndex].propertyName).boolValue;
            var applyHeadScale =
                property.FindPropertyRelative(_properties[_applyHeadScalePropertyIndex].propertyName).boolValue;
            var hideLowerBody =
                property.FindPropertyRelative(_properties[_hideLowerBodyPropertyIndex].propertyName).boolValue;

            int visiblePropertyCount = 0;
            for (var i = 0; i < _properties.Length; i++)
            {
                // Skip scale-related properties if scale is not applied
                if (i >= _applyRootScalePropertyIndex + 1 && !applyRootScale)
                {
                    continue;
                }

                if (i is >= _applyHeadScalePropertyIndex + 1 and <= _applyHeadScalePropertyIndex + 1 && !applyHeadScale)
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
            var applyRootScale =
                property.FindPropertyRelative(_properties[_applyRootScalePropertyIndex].propertyName).boolValue;
            var applyHeadScale =
                property.FindPropertyRelative(_properties[_applyHeadScalePropertyIndex].propertyName).boolValue;
            var hideLowerBody =
                property.FindPropertyRelative(_properties[_hideLowerBodyPropertyIndex].propertyName).boolValue;

            // Calculate how many properties are hidden
            int hiddenProperties = 0;

            // Hide scale-related properties if scale is not applied
            if (!applyRootScale)
            {
                hiddenProperties += 2; // _scaleRange, _currentScale
            }

            if (!applyRootScale || !applyHeadScale)
            {
                hiddenProperties += 1; // _headScaleFactor
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
