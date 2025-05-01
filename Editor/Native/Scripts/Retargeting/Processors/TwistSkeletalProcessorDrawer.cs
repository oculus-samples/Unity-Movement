// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using UnityEngine;
using UnityEditor;
using static Meta.XR.Movement.MSDKUtility;

namespace Meta.XR.Movement.Retargeting.Editor
{
    /// <summary>
    /// Custom property drawer for the <see cref="TwistSkeletalProcessor"/> class.
    /// </summary>
    [CustomPropertyDrawer(typeof(TwistSkeletalProcessor))]
    public class TwistSkeletalProcessorDrawer : PropertyDrawer
    {
        private static readonly (string propertyName, GUIContent label)[] _propertiesToDraw =
        {
            ("_weight", new GUIContent("Weight")),
            ("_sourceIndex", new GUIContent("Source")),
            (_arrayName, new GUIContent("Targets")),
            ("_twistForwardAxis", new GUIContent("Twist Forward Axis")),
            ("_twistUpAxis", new GUIContent("Twist Up Axis"))
        };

        private const string _arrayName = "_targetData";

        private CharacterRetargeter _retargeter;
        private string[] _jointNames;
        private int[] _jointIndices;

        /// <inheritdoc cref="PropertyDrawer.OnGUI"/>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

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

            var arrayProperty = property.FindPropertyRelative(_arrayName);
            arrayProperty.isExpanded = true;

            // Get joint names and indices
            if (_retargeter == null)
            {
                _retargeter = Selection.activeGameObject?.GetComponent<CharacterRetargeter>();
            }

            if (_jointNames == null || _jointIndices == null)
            {
                _jointNames = Array.Empty<string>();
                _jointIndices = Array.Empty<int>();
                if (_retargeter != null)
                {
                    CreateOrUpdateHandle(_retargeter.Config, out var handle);
                    GetJointNames(handle, SkeletonType.TargetSkeleton, out _jointNames);
                    _jointIndices = new int[_jointNames.Length];
                    for (var i = 0; i < _jointIndices.Length; i++)
                    {
                        _jointIndices[i] = i;
                    }
                }
            }

            // Draw properties
            var elementVerticalSpacing = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            for (var i = 0; i < _propertiesToDraw.Length; i++)
            {
                var verticalOffset = i > 2 ? 2f + arrayProperty.arraySize : 0;
                var propertyRect = new Rect(
                    position.x,
                    position.y + elementVerticalSpacing * (i + verticalOffset),
                    position.width,
                    EditorGUIUtility.singleLineHeight);

                var serializedProperty = property.FindPropertyRelative(_propertiesToDraw[i].propertyName);
                var serializedLabel = _propertiesToDraw[i].label;

                if (_propertiesToDraw[i].propertyName == _arrayName)
                {
                    DrawArray(propertyRect, arrayProperty);
                }
                else if (i == 1) // Source index as popup
                {
                    propertyRect.width -= position.width * 0.75f;
                    EditorGUI.LabelField(propertyRect, serializedLabel);
                    propertyRect.x += position.width * 0.25f;
                    propertyRect.width += position.width * 0.5f;
                    serializedProperty.intValue =
                        EditorGUI.IntPopup(propertyRect, serializedProperty.intValue, _jointNames, _jointIndices);
                }
                else
                {
                    EditorGUI.PropertyField(propertyRect, serializedProperty, serializedLabel);
                }
            }

            var buttonRect = new Rect(
                position.x,
                position.y + elementVerticalSpacing * (_propertiesToDraw.Length + arrayProperty.arraySize + 2),
                position.width,
                EditorGUIUtility.singleLineHeight);
            if (GUI.Button(buttonRect, "Fill Left Arm Indices"))
            {
                FillIndices(_retargeter, KnownJointType.LeftWrist, property);
            }

            buttonRect.y += elementVerticalSpacing;
            if (GUI.Button(buttonRect, "Fill Right Arm Indices"))
            {
                FillIndices(_retargeter, KnownJointType.RightWrist, property);
            }

            buttonRect.y += elementVerticalSpacing;
            if (GUI.Button(buttonRect, "Estimate Twist Axis"))
            {
                EstimateTwistAxis(_retargeter, property, property.FindPropertyRelative("_sourceIndex").intValue,
                    property.FindPropertyRelative("_targetData").GetArrayElementAtIndex(0).FindPropertyRelative("Index")
                        .intValue);
            }

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        /// <inheritdoc cref="PropertyDrawer.GetPropertyHeight"/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.FindPropertyRelative("_isFoldoutExpanded").boolValue)
            {
                return 0.0f;
            }

            var numberOfProperties = _propertiesToDraw.Length;
            var data = property.FindPropertyRelative(_arrayName);
            return
                // Number of properties + initial button
                EditorGUIUtility.singleLineHeight * (numberOfProperties + 1) +
                // Number of properties
                EditorGUIUtility.standardVerticalSpacing * numberOfProperties +
                // Array size + spacing + 3 buttons
                (data.arraySize + 1 + 3) *
                (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
        }

        private static void DrawArray(Rect rect, SerializedProperty property)
        {
            EditorGUI.PropertyField(rect, property, includeChildren: true);
        }

        private static void FillIndices(CharacterRetargeter retargeter, KnownJointType jointType,
            SerializedProperty property)
        {
            if (retargeter == null)
            {
                return;
            }

            CreateOrUpdateHandle(retargeter.Config, out var handle);
            GetJointIndexByKnownJointType(handle, SkeletonType.TargetSkeleton, jointType, out var sourceIndex);
            GetParentJointIndexes(handle, SkeletonType.TargetSkeleton, out var parentIndices);

            var targetIndex = parentIndices[sourceIndex];
            var arrayProperty = property.FindPropertyRelative(_arrayName);
            property.FindPropertyRelative("_sourceIndex").intValue = sourceIndex;
            if (arrayProperty.arraySize == 0)
            {
                arrayProperty.InsertArrayElementAtIndex(0);
            }

            arrayProperty.GetArrayElementAtIndex(0).FindPropertyRelative("Index").FindPropertyRelative("Index")
                .intValue = targetIndex;
            arrayProperty.GetArrayElementAtIndex(0).FindPropertyRelative("Weight").floatValue = 0.5f;
            EstimateTwistAxis(retargeter, property, sourceIndex, targetIndex);
            DestroyHandle(handle);
        }

        private static void EstimateTwistAxis(CharacterRetargeter retargeter, SerializedProperty property, int sourceIndex,
            int targetIndex)
        {
            CreateOrUpdateHandle(retargeter.Config, out var handle);
            GetSkeletonTPose(handle, SkeletonType.TargetSkeleton, SkeletonTPoseType.UnscaledTPose,
                JointRelativeSpaceType.RootOriginRelativeSpace, out var tPose);
            property.FindPropertyRelative("_twistForwardAxis").vector3Value =
                FindClosestAxis(tPose[targetIndex], tPose[sourceIndex].Position);
            property.FindPropertyRelative("_twistUpAxis").vector3Value =
                FindClosestAxis(tPose[targetIndex], tPose[targetIndex].Position + Vector3.up * 2.0f);
            DestroyHandle(handle);
        }

        private static Vector3 FindClosestAxis(NativeTransform pose, Vector3 target)
        {
            Vector3[] axes =
            {
                Vector3.up, Vector3.down, Vector3.right,
                Vector3.left, Vector3.forward, Vector3.back
            };

            var closestAxis = Vector3.zero;
            var closestDist = Mathf.Infinity;

            foreach (var t in axes)
            {
                var direction = pose.Orientation * t;
                var pos = pose.Position + direction;
                var dist = Vector3.Distance(pos, target);

                if (!(dist < closestDist))
                {
                    continue;
                }

                closestDist = dist;
                closestAxis = t;
            }

            return closestAxis;
        }
    }
}
