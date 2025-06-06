// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using UnityEditor;
using UnityEngine;

namespace Meta.XR.Movement.Retargeting.Editor
{
    /// <summary>
    /// Renders an instance of <see cref="TargetProcessorContainer"/>.
    /// </summary>
    [CustomPropertyDrawer(typeof(TargetProcessorContainer))]
    public class TargetProcessorContainerDrawer : PropertyDrawer
    {
        /// <inheritdoc cref="PropertyDrawer.OnGUI"/>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var rect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("_currentProcessorType"),
                new GUIContent("Type"));

            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            var processorProperty = GetProcessorTypeProperty(property);
            if (processorProperty != null)
            {
                EditorGUI.PropertyField(rect, processorProperty, new GUIContent("Processor"));
            }
        }

        private SerializedProperty GetProcessorTypeProperty(SerializedProperty property)
        {
            var processorTypeProperty =
                (TargetProcessor.ProcessorType)property.FindPropertyRelative("_currentProcessorType").intValue;
            switch (processorTypeProperty)
            {
                case TargetProcessor.ProcessorType.Twist:
                    return property.FindPropertyRelative("_twistProcessor");
                case TargetProcessor.ProcessorType.Animation:
                    return property.FindPropertyRelative("_animationProcessor");
                case TargetProcessor.ProcessorType.Locomotion:
                    return property.FindPropertyRelative("_locomotionProcessor");
                case TargetProcessor.ProcessorType.CCDIK:
                    return property.FindPropertyRelative("_ccdProcessor");
                case TargetProcessor.ProcessorType.HandIK:
                    return property.FindPropertyRelative("_handProcessor");
                case TargetProcessor.ProcessorType.HipPinning:
                    return property.FindPropertyRelative("_hipPinningProcessor");
                case TargetProcessor.ProcessorType.Custom:
                    return property.FindPropertyRelative("_customProcessor");
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
