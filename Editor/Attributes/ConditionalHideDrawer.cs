// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEditor;
using UnityEngine;

namespace Oculus.Movement.Attributes
{
    /// <summary>
    /// Adds an [ConditionalHide] label in the inspector over any SerializedField with this attribute.
    /// Class borrowed from the InteractionSDK.
    /// </summary>
    [CustomPropertyDrawer(typeof(ConditionalHideAttribute))]
    public class ConditionalHideDrawer : PropertyDrawer
    {
        bool FulfillsCondition(SerializedProperty property)
        {
            ConditionalHideAttribute hideAttribute = (ConditionalHideAttribute)attribute;

            int index = property.propertyPath.LastIndexOf('.');
            string containerPath = property.propertyPath.Substring(0, index + 1);
            string conditionPath = containerPath + hideAttribute.ConditionalFieldPath;
            SerializedProperty conditionalProperty = property.serializedObject.FindProperty(conditionPath);

            if (conditionalProperty.type == "Enum")
            {
                return conditionalProperty.enumValueIndex == (int)hideAttribute.HideValue;
            }
            if (conditionalProperty.type == "int")
            {
                return conditionalProperty.intValue == (int)hideAttribute.HideValue;
            }
            if (conditionalProperty.type == "float")
            {
                return conditionalProperty.floatValue == (float)hideAttribute.HideValue;
            }
            if (conditionalProperty.type == "string")
            {
                return conditionalProperty.stringValue == (string)hideAttribute.HideValue;
            }
            if (conditionalProperty.type == "double")
            {
                return conditionalProperty.doubleValue == (double)hideAttribute.HideValue;
            }
            if (conditionalProperty.type == "bool")
            {
                return conditionalProperty.boolValue == (bool)hideAttribute.HideValue;
            }

            return conditionalProperty.objectReferenceValue == (object)hideAttribute.HideValue;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (FulfillsCondition(property))
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (FulfillsCondition(property))
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }
            else
            {
                return -EditorGUIUtility.standardVerticalSpacing;
            }
        }
    }
}
