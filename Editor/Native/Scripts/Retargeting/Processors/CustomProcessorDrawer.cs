// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using UnityEditor;
using UnityEngine;

namespace Meta.XR.Movement.Retargeting.Editor
{
    /// <summary>
    /// Renders an instance of <see cref="CustomProcessor"/>.
    /// </summary>
    [CustomPropertyDrawer(typeof(CustomProcessor))]
    public class CustomProcessorDrawer : PropertyDrawer
    {
        private static readonly (string propertyName, GUIContent label)[] _propertiesToDraw =
        {
            ("_weight", new GUIContent("Weight")),
            ("_customBehavior", new GUIContent("Custom Behavior"))
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
            var elementVerticalSpacing = GetVerticalSpacing();
            for (var i = 0; i < _propertiesToDraw.Length; i++)
            {
                var propertyRect = new Rect(
                    position.x,
                    position.y + elementVerticalSpacing * i,
                    position.width,
                    EditorGUIUtility.singleLineHeight);

                var serializedProperty = property.FindPropertyRelative(_propertiesToDraw[i].propertyName);
                var serializedLabel = _propertiesToDraw[i].label;
                if (i == 0)
                {
                    EditorGUI.PropertyField(propertyRect, serializedProperty, serializedLabel);
                }
                else
                {
                    EditorGUI.ObjectField(propertyRect, serializedProperty, serializedLabel);
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
                return EditorGUIUtility.standardVerticalSpacing;
            }

            var verticalSpacing = GetVerticalSpacing();
            var numberOfProperties = _propertiesToDraw.Length;
            return verticalSpacing * numberOfProperties;
        }

        private float GetVerticalSpacing()
        {
            return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }
    }
}
