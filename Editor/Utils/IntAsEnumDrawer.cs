// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEditor;
using UnityEngine;

namespace Oculus.Movement.Utils
{
    /// <summary>
    /// Int drawer that allows displaying an int like an enum. This is used for displaying ints that are bound
    /// to an AnimationStream but are functionally used as an enum.
    /// </summary>
    [CustomPropertyDrawer(typeof(IntAsEnumAttribute))]
    public class DisplayIntAsEnumDrawer : PropertyDrawer
    {
        /// <inheritdoc/>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            IntAsEnumAttribute intAsEnumAttribute = (IntAsEnumAttribute)attribute;

            if (property.propertyType == SerializedPropertyType.Integer)
            {
                var names = System.Enum.GetNames(intAsEnumAttribute.Type);
                var values = new int[names.Length];
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = i;
                    names[i] = FormatName(names[i]);
                }
                EditorGUI.BeginProperty(position, GUIContent.none, property);
                property.intValue = EditorGUI.IntPopup(position,
                    FormatName(intAsEnumAttribute.Type.Name), property.intValue, names, values);
                EditorGUI.EndProperty();
            }
        }

        private string FormatName(string name)
        {
            return System.Text.RegularExpressions.Regex.Replace(name, @"([a-z])([A-Z])", "$1 $2"); ;
        }
    }
}
