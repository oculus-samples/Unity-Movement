// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Meta.XR.Movement
{
    /// <summary>
    /// Attribute that conditionally shows/hides a field in the Inspector based on another field's value.
    /// Can be combined with other PropertyAttributes like InspectorButtonAttribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class ShowIfAttribute : PropertyAttribute
    {
        /// <summary>
        /// Name of the field to check
        /// </summary>
        public string ConditionalFieldName { get; private set; }

        /// <summary>
        /// Value that the field must match to show this property
        /// </summary>
        public object CompareValue { get; private set; }

        /// <summary>
        /// If true, inverts the condition (shows when NOT equal)
        /// </summary>
        public bool Inverse { get; private set; }

        /// <summary>
        /// Creates a ShowIf attribute that shows the field when the condition field equals the compare value
        /// </summary>
        /// <param name="conditionalFieldName">Name of the field to check</param>
        /// <param name="compareValue">Value to compare against (bool, enum, int, etc.)</param>
        /// <param name="inverse">If true, shows when NOT equal</param>
        public ShowIfAttribute(string conditionalFieldName, object compareValue, bool inverse = false)
        {
            ConditionalFieldName = conditionalFieldName;
            CompareValue = compareValue;
            Inverse = inverse;
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Property drawer for ShowIfAttribute that conditionally shows/hides fields in the Inspector
    /// </summary>
    [CustomPropertyDrawer(typeof(ShowIfAttribute))]
    public class ShowIfAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ShowIfAttribute showIfAttribute = (ShowIfAttribute)attribute;

            if (ShouldShow(property, showIfAttribute))
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            ShowIfAttribute showIfAttribute = (ShowIfAttribute)attribute;

            if (ShouldShow(property, showIfAttribute))
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }

            return -EditorGUIUtility.standardVerticalSpacing;
        }

        private bool ShouldShow(SerializedProperty property, ShowIfAttribute showIfAttribute)
        {
            SerializedProperty conditionalProperty = property.serializedObject.FindProperty(showIfAttribute.ConditionalFieldName);

            if (conditionalProperty == null)
            {
                Debug.LogWarning($"ShowIf: Could not find field '{showIfAttribute.ConditionalFieldName}' on {property.serializedObject.targetObject.GetType()}");
                return true;
            }

            bool conditionMet = CheckCondition(conditionalProperty, showIfAttribute.CompareValue);

            return showIfAttribute.Inverse ? !conditionMet : conditionMet;
        }

        private bool CheckCondition(SerializedProperty property, object compareValue)
        {
            if (compareValue == null)
            {
                return false;
            }

            switch (property.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    if (compareValue is bool boolValue)
                    {
                        return property.boolValue == boolValue;
                    }
                    break;

                case SerializedPropertyType.Enum:
                    if (compareValue is System.Enum enumValue)
                    {
                        return property.enumValueIndex == System.Convert.ToInt32(enumValue);
                    }
                    break;

                case SerializedPropertyType.Integer:
                    if (compareValue is int intValue)
                    {
                        return property.intValue == intValue;
                    }
                    break;

                case SerializedPropertyType.Float:
                    if (compareValue is float floatValue)
                    {
                        return Mathf.Approximately(property.floatValue, floatValue);
                    }
                    break;

                case SerializedPropertyType.String:
                    if (compareValue is string stringValue)
                    {
                        return property.stringValue == stringValue;
                    }
                    break;
            }

            Debug.LogWarning($"ShowIf: Unsupported property type '{property.propertyType}' or mismatched compare value type");
            return true;
        }
    }
#endif
}
