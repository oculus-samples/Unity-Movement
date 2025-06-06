// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using Meta.XR.Movement.Editor;
using UnityEditor;
using UnityEngine;

namespace Meta.XR.Movement.Retargeting.Editor
{
    /// <summary>
    /// Renders an instance of <see cref="HandSkeletalProcessor"/>.
    /// </summary>
    [CustomPropertyDrawer(typeof(HandSkeletalProcessor))]
    public class HandSkeletalProcessorDrawer : PropertyDrawer
    {
        private static readonly (string propertyName, GUIContent label)[] _propertiesToDraw =
        {
            ("_weight", new GUIContent("Weight")),
            ("_handsTransform", new GUIContent("Hands Transform")),
            ("_leftHandTransformName", new GUIContent("Left Hand Transform Name")),
            ("_rightHandTransformName", new GUIContent("Right Hand Transform Name")),
            ("_leftHandJoints", new GUIContent("Left Hand Joints")), // array
            ("_rightHandJoints", new GUIContent("Right Hand Joints")), // array
            ("_useRetargetedHandRotation", new GUIContent("Use Retargeted Hand Rotation")),
            ("_matchFingers", new GUIContent("Match Fingers")),
            ("_limitStretch", new GUIContent("Limit Stretch")),
            ("_ikIterations", new GUIContent("IK Iterations")),
            ("_ikTolerance", new GUIContent("IK Tolerance")),
            ("_runSerial", new GUIContent("Run Serial")),
            ("_algorithm", new GUIContent("Algorithm"))
        };

        private readonly string[] _arrayNames =
        {
            "_leftHandJoints",
            "_rightHandJoints"
        };

        /// <inheritdoc cref="PropertyDrawer.OnGUI"/>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, GUIContent.none, property);
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

            // Draw properties
            var propertyRect = new Rect(
                position.x,
                position.y,
                position.width,
                EditorGUIUtility.singleLineHeight);
            var elementVerticalSpacing = GetVerticalSpacing();
            for (var i = 0; i < _propertiesToDraw.Length; i++)
            {
                var serializedLabel = _propertiesToDraw[i].label;
                var serializedProperty = property.FindPropertyRelative(
                    _propertiesToDraw[i].propertyName);
                bool isArray = false;
                for (int j = 0; j < _arrayNames.Length; j++)
                {
                    if (_arrayNames[j] == _propertiesToDraw[i].propertyName)
                    {
                        isArray = true;
                        break;
                    }
                }

                EditorGUI.PropertyField(propertyRect, serializedProperty, serializedLabel);
                if (isArray)
                {
                    var chainPropertyHeight =
                        serializedProperty.isExpanded
                            ? EditorGUI.GetPropertyHeight(serializedProperty, true)
                            : elementVerticalSpacing;
                    propertyRect.y += chainPropertyHeight;
                }
                else
                {
                    propertyRect.y += elementVerticalSpacing;
                }
            }

            if (property.FindPropertyRelative(_propertiesToDraw[1].propertyName).objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Valid hands transform is required for using the Hand IK processor.",
                    MessageType.Error);
                GUI.enabled = false;
            }

            if (GUI.Button(propertyRect, "Recommended Values"))
            {
                RecommendedValues(property);
            }

            propertyRect.y += elementVerticalSpacing;
            if (GUI.Button(propertyRect, "Initialize"))
            {
                RunInitialize(property);
            }

            GUI.enabled = true;

            EditorGUI.EndProperty();
        }

        /// <inheritdoc cref="PropertyDrawer.GetPropertyHeight"/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.FindPropertyRelative("_isFoldoutExpanded").boolValue)
            {
                return 0.0f;
            }

            var elementVerticalSpacing = GetVerticalSpacing();
            // all but two elements
            var nonArrayElementsHeight = (_propertiesToDraw.Length - 2) * elementVerticalSpacing;
            float arrayHeight = 0.0f;
            foreach (var n in _arrayNames)
            {
                var arrayProperty = property.FindPropertyRelative(n);
                var currArrayHeight = arrayProperty.isExpanded
                    ? EditorGUI.GetPropertyHeight(arrayProperty, true)
                    : elementVerticalSpacing;
                arrayHeight += currArrayHeight;
            }

            var buttonsHeight = elementVerticalSpacing * 2;
            return nonArrayElementsHeight + arrayHeight + buttonsHeight;
        }

        private void RecommendedValues(SerializedProperty property)
        {
            var handProcessor = FindProcessor(property);
            handProcessor.LeftHandTransformName = "LeftHand";
            handProcessor.RightHandTransformName = "RightHand";
            handProcessor.MatchFingers = true;
            handProcessor.LimitStretch = true;
            handProcessor.UseRetargetedHandRotation = true;
            handProcessor.IkIterations = 5;
            handProcessor.IkTolerance = 1e-6f;
            handProcessor.RunSerial = false;
            handProcessor.Algorithm = HandSkeletalProcessor.IKAlgorithm.CCDIK;
            var retargeter = Selection.activeGameObject.GetComponent<CharacterRetargeter>();
            EditorUtility.SetDirty(retargeter);
        }

        private void RunInitialize(SerializedProperty property)
        {
            var handProcessor = FindProcessor(property);
            if (handProcessor == null)
            {
                Debug.LogError("Cannot initialize!");
                return;
            }

            if (handProcessor.HandsTransform == null)
            {
                Debug.LogError("Hand transform null, cannot assign!");
                return;
            }

            Debug.Log($"Caching avatar hands data for {handProcessor.HandsTransform.name}.");
            FindJoints(handProcessor);
            var retargeter = Selection.activeGameObject.GetComponent<CharacterRetargeter>();
            EditorUtility.SetDirty(retargeter);
        }

        private HandSkeletalProcessor FindProcessor(SerializedProperty property)
        {
            if (MSDKUtilityEditor.TryGetProcessorAtPropertyPathIndex<HandSkeletalProcessor>(
                    property, false, out var handProcessor))
            {
                return handProcessor;
            }
            return null;
        }

        private void FindJoints(HandSkeletalProcessor processor)
        {
            var handTransform = processor.HandsTransform;
            var leftHandTransform = handTransform.FindChildRecursive(processor.LeftHandTransformName);
            var rightHandTransform = handTransform.FindChildRecursive(processor.RightHandTransformName);
            var leftHandJoints = leftHandTransform.GetComponentsInChildren<Transform>(true);
            var rightHandJoints = rightHandTransform.GetComponentsInChildren<Transform>(true);
            processor.LeftHandJoints = leftHandJoints;
            processor.RightHandJoints = rightHandJoints;
        }

        private float GetVerticalSpacing()
        {
            return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }
    }
}
