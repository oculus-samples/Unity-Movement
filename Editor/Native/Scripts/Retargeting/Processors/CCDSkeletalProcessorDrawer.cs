// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using UnityEditor;
using UnityEngine;

namespace Meta.XR.Movement.Retargeting.Editor
{
    /// <summary>
    /// Renders an instance of <see cref="CCDSkeletalProcessor"/>.
    /// </summary>
    [CustomPropertyDrawer(typeof(CCDSkeletalProcessor))]
    public class CCDSkeletalProcessorDrawer : PropertyDrawer
    {
        private static string _arrayName = "_ccdData";

        private static readonly (string propertyName, GUIContent label)[] _propertiesToDraw =
        {
            ("_weight", new GUIContent("Weight")),
            (_arrayName, new GUIContent("CCD Data"))
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
            int arrayIndex = 1;
            for (var i = 0; i < _propertiesToDraw.Length; i++)
            {
                var propertyRect = new Rect(
                    position.x,
                    position.y + elementVerticalSpacing * i,
                    position.width,
                    EditorGUIUtility.singleLineHeight);
                // for items beyond the CCD data array, take the array's size into account
                if (i > arrayIndex)
                {
                    propertyRect.y = position.y +
                        elementVerticalSpacing * (arrayIndex - 1) +
                        EditorGUI.GetPropertyHeight(
                            property.FindPropertyRelative(_arrayName),
                            true);
                }

                var serializedProperty = property.FindPropertyRelative(_propertiesToDraw[i].propertyName);
                var serializedLabel = _propertiesToDraw[i].label;
                if (i == 1)
                {
                    // expand the ccd array
                    serializedProperty.isExpanded = true;
                    EditorGUI.PropertyField(propertyRect, serializedProperty, includeChildren: true);
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
                return EditorGUIUtility.standardVerticalSpacing;
            }

            var verticalSpacing = GetVerticalSpacing();
            var numberOfProperties = _propertiesToDraw.Length;
            var arrayItem = property.FindPropertyRelative(_arrayName);
            var arrayHeight = EditorGUI.GetPropertyHeight(arrayItem, true);
            return
                // for non-array items
                verticalSpacing * (numberOfProperties - 1) +
                // height for array
                arrayHeight;
        }

        private float GetVerticalSpacing()
        {
            return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }
    }
}
