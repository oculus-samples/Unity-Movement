// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Movement.Tracking.Deprecated
{
    /// <summary>
    /// Editor class defining interface for FaceExpressionModifier which is a data class of BlendshapeModifier
    /// </summary>
    [CustomPropertyDrawer(typeof(BlendshapeModifier.FaceExpressionModifier))]
    public class FaceExpressionModifierDrawer : PropertyDrawer
    {
        private GUIStyle _labelStyle = GUI.skin.label;
        private GUIContent _multLabel = new GUIContent("MULT");
        private GUIContent _clmpLabel = new GUIContent("CLMP");

        private float _labelWidth;
        private float _valueWidth = EditorGUIUtility.fieldWidth;
        private float _horizontalSpacing = 5f;

        private float _minLimit = typeof(BlendshapeModifier.FaceExpressionModifier)
            .GetField(nameof(BlendshapeModifier.FaceExpressionModifier.MinValue)).GetCustomAttribute<RangeAttribute>()
            .min;

        private float _maxLimit = typeof(BlendshapeModifier.FaceExpressionModifier)
            .GetField(nameof(BlendshapeModifier.FaceExpressionModifier.MaxValue)).GetCustomAttribute<RangeAttribute>()
            .max;

        private string[] _blendshapeNames;
        private string[] _blendshapeNamesFromNone;

        /// <summary>
        /// Editor class defining interface for FaceExpressionModifier which is a data class of BlendshapeModifier
        /// </summary>
        public FaceExpressionModifierDrawer()
        {
            List<String> blendshapeNameList = new List<string>();
            foreach (string e in Enum.GetNames(typeof(OVRFaceExpressions.FaceExpression)))
            {
                blendshapeNameList.Add(ObjectNames.NicifyVariableName(e));
            }

            _blendshapeNames = blendshapeNameList.ToArray();
            blendshapeNameList.Insert(0, "None");
            _blendshapeNamesFromNone = blendshapeNameList.ToArray();
        }

        /// <inheritdoc />
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var dataHeight =
                EditorGUIUtility.singleLineHeight * 3 +
                EditorGUIUtility.standardVerticalSpacing * 2;
            var extraHeight = EditorGUIUtility.singleLineHeight / 2;

            return dataHeight + extraHeight;
        }

        /// <inheritdoc />
        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            _labelWidth = Math.Max(_labelStyle.CalcSize(_multLabel).x, _labelStyle.CalcSize(_clmpLabel).x);

            var faceExpressionsProp =
                property.FindPropertyRelative(
                    nameof(BlendshapeModifier.FaceExpressionModifier.FaceExpressions));
            var multiplierProp =
                property.FindPropertyRelative(
                    nameof(BlendshapeModifier.FaceExpressionModifier.Multiplier));
            var minProp =
                property.FindPropertyRelative(
                    nameof(BlendshapeModifier.FaceExpressionModifier.MinValue));
            var maxProp =
                property.FindPropertyRelative(
                    nameof(BlendshapeModifier.FaceExpressionModifier.MaxValue));

            Assert.IsNotNull(faceExpressionsProp);
            Assert.IsNotNull(multiplierProp);
            Assert.IsNotNull(minProp);
            Assert.IsNotNull(maxProp);

            using (new EditorGUI.PropertyScope(rect, label, property))
            {
                // Face expressions line
                var r = rect;
                r.height = EditorGUIUtility.singleLineHeight;
                r.width = rect.width / 2 - _horizontalSpacing;
                FaceExpressionField(r, faceExpressionsProp, 0, false);

                r.x += r.width + _horizontalSpacing;
                FaceExpressionField(r, faceExpressionsProp, 1, true);

                // Multiplier value line
                r.x = rect.x;
                r.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                r.width = _labelWidth;
                EditorGUI.LabelField(r, _multLabel);

                r.x += r.width + _horizontalSpacing + _valueWidth + _horizontalSpacing;
                r.width = rect.width - _labelWidth - _horizontalSpacing - _valueWidth - _horizontalSpacing;
                EditorGUI.Slider(r, multiplierProp, _minLimit, _maxLimit, GUIContent.none);

                // Clamp values line
                r.x = rect.x;
                r.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                r.width = _labelWidth;
                EditorGUI.LabelField(r, _clmpLabel);

                r.x += r.width + _horizontalSpacing;
                r.width = _valueWidth;
                ValueField(r, minProp);

                r.x += r.width + _horizontalSpacing;
                r.width = rect.width - _labelWidth - _valueWidth * 2 - _horizontalSpacing * 3;
                MinMaxSliderField(r, minProp, maxProp);

                r.x += r.width + _horizontalSpacing;
                r.width = _valueWidth;
                ValueField(r, maxProp);
            }
        }

        private void MinMaxSliderField(Rect r, SerializedProperty minProp, SerializedProperty maxProp)
        {
            float minVal = minProp.floatValue;
            float maxVal = maxProp.floatValue;
            EditorGUI.BeginChangeCheck();
            EditorGUI.MinMaxSlider(r, ref minVal, ref maxVal, _minLimit, _maxLimit);
            if (EditorGUI.EndChangeCheck())
            {
                minProp.floatValue = minVal;
                maxProp.floatValue = maxVal;
            }
        }

        private void ValueField(Rect r, SerializedProperty prop)
        {
            EditorGUI.BeginChangeCheck();
            var val = EditorGUI.FloatField(r, (float)Math.Round(prop.floatValue, 3));
            if (EditorGUI.EndChangeCheck())
            {
                prop.floatValue = val;
            }
        }

        private void FaceExpressionField(Rect r, SerializedProperty prop, int propertyArrayIndex, bool isNoneAllowed)
        {
            int index = -1;
            if (prop.arraySize > propertyArrayIndex)
            {
                index = prop.GetArrayElementAtIndex(propertyArrayIndex).enumValueIndex;
            }

            if (isNoneAllowed)
            {
                index++;
            }

            EditorGUI.BeginChangeCheck();
            var popupIndex = EditorGUI.Popup(r, index, isNoneAllowed ? _blendshapeNamesFromNone : _blendshapeNames);
            if (EditorGUI.EndChangeCheck())
            {
                if (isNoneAllowed)
                {
                    popupIndex--;
                }

                Assert.IsTrue(isNoneAllowed || popupIndex >= 0);

                // "None" is selected
                if (popupIndex < 0)
                {
                    if (prop.arraySize >= propertyArrayIndex)
                    {
                        // cut the propertyArrayIndex-th element from the array
                        prop.arraySize = propertyArrayIndex;
                    }
                }
                else
                {
                    if (prop.arraySize <= propertyArrayIndex)
                    {
                        prop.arraySize = propertyArrayIndex + 1;
                    }

                    var propElement = prop.GetArrayElementAtIndex(propertyArrayIndex);
                    propElement.enumValueIndex = popupIndex;
                }
            }
        }
    }
}
