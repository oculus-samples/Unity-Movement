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
        private const int _headScaleFactorPropertyIndex = 5;
        private const int _scaleRangePropertyIndex = 6;
        private const int _readOnlyScalePropertyIndex = 7;

        /// <inheritdoc cref="PropertyDrawer.OnGUI"/>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            int visiblePropertyCount = 0;
            for (var i = 0; i < _properties.Length; i++)
            {
                if (!IsVisible(i, property))
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
            var visibleProperties = 0;
            for (var i = 0; i < _properties.Length; i++)
            {
                if (IsVisible(i, property))
                {
                    visibleProperties++;
                }
            }

            return EditorGUIUtility.singleLineHeight * visibleProperties +
                   EditorGUIUtility.standardVerticalSpacing * (visibleProperties - 1);
        }

        // Head scale is computed from the head scale factor and the clamped current scale even when root scale is off.
        private static bool IsVisible(int index, SerializedProperty property)
        {
            bool GetBool(int i) => property.FindPropertyRelative(_properties[i].propertyName).boolValue;
            return index switch
            {
                _hideLegScalePropertyIndex => GetBool(_hideLowerBodyPropertyIndex),
                _headScaleFactorPropertyIndex => GetBool(_applyHeadScalePropertyIndex),
                _scaleRangePropertyIndex or _readOnlyScalePropertyIndex =>
                    GetBool(_applyRootScalePropertyIndex) || GetBool(_applyHeadScalePropertyIndex),
                _ => true,
            };
        }
    }
}
