// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using UnityEditor;
using UnityEngine;

namespace Meta.XR.Movement.Retargeting.Editor
{
    /// <summary>
    /// Renders an instance of <see cref="SourceProcessorContainer"/>.
    /// </summary>
    [CustomPropertyDrawer(typeof(SourceProcessorContainer))]
    public class SourceProcessorContainerDrawer : PropertyDrawer
    {
        /// <inheritdoc cref="PropertyDrawer.OnGUI"/>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var rect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(rect,
                property.FindPropertyRelative("_currentProcessorType"),
                new GUIContent("Type"));
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            var processorProperty = GetProcessorTypeProperty(property);
            if (processorProperty != null)
            {
                EditorGUI.PropertyField(rect,
                    processorProperty,
                    new GUIContent("Processor"));
            }
        }

        private SerializedProperty GetProcessorTypeProperty(SerializedProperty property)
        {
            var processorTypeProperty = (SourceProcessor.ProcessorType)property.FindPropertyRelative("_currentProcessorType").intValue;
            switch (processorTypeProperty)
            {
                case SourceProcessor.ProcessorType.ISDK:
                    return property.FindPropertyRelative("_isdkProcessor");
                default:
                    return null;
            }
        }

        /// <inheritdoc cref="PropertyDrawer.GetPropertyHeight"/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // The height is set based on the serialized processor type.
            var referenceProcessor = GetProcessorTypeProperty(property);
            var heightFromProcessor = referenceProcessor != null ? EditorGUI.GetPropertyHeight(referenceProcessor) : 0;
            return EditorGUIUtility.singleLineHeight +
                EditorGUIUtility.standardVerticalSpacing +
                heightFromProcessor +
                EditorGUIUtility.standardVerticalSpacing;
        }
    }
}
