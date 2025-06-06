// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using UnityEditor;
using UnityEngine;

namespace Meta.XR.Movement.Retargeting.Editor
{
    /// <summary>
    /// Custom property drawer for the <see cref="TwistSkeletalProcessor.TwistData"/> class.
    /// </summary>
    [CustomPropertyDrawer(typeof(TwistSkeletalProcessor.TwistData))]
    public class TwistDataPropertyDrawer : PropertyDrawer
    {
        /// <inheritdoc cref="PropertyDrawer.OnGUI"/>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, GUIContent.none, property);

            var indexProp = property.FindPropertyRelative("Index");
            var weightProp = property.FindPropertyRelative("Weight");

            // Split the rect for joint dropdown and weight slider
            var indexRect = new Rect(position.x, position.y, position.width * 0.6f, position.height);
            var weightRect = new Rect(position.x + position.width * 0.62f, position.y, position.width * 0.38f,
                position.height);

            // Draw the JointIndex property using its own drawer
            EditorGUI.PropertyField(indexRect, indexProp, GUIContent.none);

            // Weight slider
            EditorGUI.BeginChangeCheck();
            float weight = EditorGUI.Slider(weightRect, weightProp.floatValue, 0f, 1f);
            if (EditorGUI.EndChangeCheck())
            {
                weightProp.floatValue = weight;
            }

            EditorGUI.EndProperty();
        }

        /// <inheritdoc cref="PropertyDrawer.GetPropertyHeight"/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
