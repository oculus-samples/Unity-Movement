// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using Meta.XR.Movement.Editor;
using UnityEditor;
using UnityEngine;

namespace Meta.XR.Movement.Retargeting.Editor
{
    /// <summary>
    /// Renders an instance of <see cref="CCDSkeletalProcessor.CCDSkeletalData"/>.
    /// </summary>
    [CustomPropertyDrawer(typeof(CCDSkeletalProcessor.CCDSkeletalData))]
    public class CCDSkeletalDataPropertyDrawer : PropertyDrawer
    {
        private static string _ikChainName = "IKChain";

        private static readonly (string propertyName, GUIContent label)[] _propertiesToDraw =
        {
            (_ikChainName, new GUIContent("IK Chain")),
            ("Target", new GUIContent("Target")),
            ("Tolerance", new GUIContent("Tolerance")),
            ("MaxIterations", new GUIContent("Max Iterations"))
        };

        /// <inheritdoc cref="PropertyDrawer.OnGUI"/>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, GUIContent.none, property);
            var propertyRect = new Rect(
                position.x,
                position.y,
                position.width,
                EditorGUIUtility.singleLineHeight);
            var elementVerticalSpacing = GetVerticalSpacing();
            int chainIndex = 0;
            for (var i = 0; i < _propertiesToDraw.Length; i++)
            {
                var serializedLabel = _propertiesToDraw[i].label;
                var serializedProperty = property.FindPropertyRelative(
                    _propertiesToDraw[i].propertyName);
                if (i == chainIndex)
                {
                    serializedProperty.isExpanded = true;
                }

                EditorGUI.PropertyField(propertyRect, serializedProperty, serializedLabel);
                if (i == chainIndex)
                {
                    var chainPropertyHeight = EditorGUI.GetPropertyHeight(serializedProperty, true);
                    propertyRect.y += chainPropertyHeight;
                }
                else
                {
                    propertyRect.y += elementVerticalSpacing;
                }
            }

            if (GUI.Button(propertyRect, "Initialize"))
            {
                RunInitialize(property);
            }

            EditorGUI.EndProperty();
        }

        /// <inheritdoc cref="PropertyDrawer.GetPropertyHeight"/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var elementVerticalSpacing = GetVerticalSpacing();
            var nonArrayElementsHeight = (_propertiesToDraw.Length - 1) * elementVerticalSpacing;
            var ikChainRelativeProperty = property.FindPropertyRelative(_ikChainName);
            var arrayHeight = EditorGUI.GetPropertyHeight(ikChainRelativeProperty, true);
            var buttonHeight = elementVerticalSpacing;

            return nonArrayElementsHeight + arrayHeight + buttonHeight;
        }

        private float GetVerticalSpacing()
        {
            return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        private void RunInitialize(SerializedProperty property)
        {
            if (MSDKUtilityEditor.TryGetProcessorAtPropertyPathIndex<CCDSkeletalProcessor>(
                    property, false, out var ccdProcessor))
            {
                var retargeter = Selection.activeGameObject.GetComponent<CharacterRetargeter>();
                Undo.RecordObject(retargeter, "Update CCD processor");
                var subProperty = property.FindPropertyRelative(_ikChainName);
                // Cut out the first occurence of []
                var subPropertyPath = subProperty.propertyPath;
                subPropertyPath = subPropertyPath.Substring(subPropertyPath.IndexOf(']') + 1);
                int indexOfSubProperty = MSDKUtilityHelper.GetIndexFromPropertyPath(subPropertyPath);
                ccdProcessor.CCDData[indexOfSubProperty].MaxIterations = 5;
                ccdProcessor.CCDData[indexOfSubProperty].Tolerance = 1e-6f;
            }
        }
    }
}
