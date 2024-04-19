// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using UnityEditor;

namespace Oculus.Movement.Utils
{
    /// <summary>
    /// Use this attribute on lists that are exposed in the Unity inspector to name the elements
    /// after enum values.
    /// </summary>
    [CustomPropertyDrawer(typeof(EnumNamedArrayAttribute))]
    public class EnumNamedArrayDrawer : PropertyDrawer
    {
        /// <inheritdoc/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => EditorGUI.GetPropertyHeight(property, label, property.isExpanded);

        /// <inheritdoc/>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (attribute == null || property == null)
            {
                return;
            }
            try
            {
                EnumNamedArrayAttribute namedAttribute = attribute as EnumNamedArrayAttribute;
                int index = int.Parse(property.propertyPath.Split('[', ']')[1]);
                string[] names = namedAttribute.GetNames();
                string enumLabel = ObjectNames.NicifyVariableName(names[index]);
                label = new GUIContent(enumLabel);
            }
            catch
            {
                // ignored, just use default label
            }
            EditorGUI.PropertyField(position, property, label, property.isExpanded);
        }
    }
}
