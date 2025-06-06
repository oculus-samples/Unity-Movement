// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using Meta.XR.Movement.Editor;
using UnityEditor;
using UnityEngine;

namespace Meta.XR.Movement.Retargeting.Editor
{
    /// <summary>
    /// Custom property drawer for the <see cref="AnimationSkeletalProcessor"/> class.
    /// </summary>
    [CustomPropertyDrawer(typeof(AnimationSkeletalProcessor))]
    public class AnimationSkeletalProcessorDrawer : PropertyDrawer
    {
        private static readonly (string propertyName, GUIContent label)[] _propertiesToDraw =
        {
            ("_weight", new GUIContent("Weight")),
            (_arrayName, new GUIContent("Anim Blend Indices"))
        };

        private const string _arrayName = "_animBlendIndices";

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

            // Draw properties
            var elementVerticalSpacing = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            float currentY = position.y;

            // Draw non-array properties first
            for (var i = 0; i < _propertiesToDraw.Length; i++)
            {
                if (_propertiesToDraw[i].propertyName != _arrayName)
                {
                    var serializedProperty = property.FindPropertyRelative(_propertiesToDraw[i].propertyName);
                    var serializedLabel = _propertiesToDraw[i].label;
                    var propertyRect = new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight);
                    EditorGUI.PropertyField(propertyRect, serializedProperty, serializedLabel);
                    currentY += elementVerticalSpacing;
                }
            }

            // Draw array property
            var arrayRect = new Rect(position.x, currentY, position.width, EditorGUI.GetPropertyHeight(arrayProperty, true));
            EditorGUI.PropertyField(arrayRect, arrayProperty, includeChildren: true);
            currentY += EditorGUI.GetPropertyHeight(arrayProperty, true);

            // Draw button
            var buttonRect = new Rect(
                position.x,
                currentY,
                position.width,
                EditorGUIUtility.singleLineHeight);
            if (GUI.Button(buttonRect, "Update blend indices to lower body"))
            {
                if (MSDKUtilityEditor.TryGetProcessorAtPropertyPathIndex<AnimationSkeletalProcessor>(
                    property, false, out var animSkeletalProcessor))
                {
                    var retargeter = Selection.activeGameObject.GetComponent<CharacterRetargeter>();
                    Undo.RecordObject(retargeter, "Update animation blend indices processor");
                    FindLowerBodyIndices(retargeter, animSkeletalProcessor);
                }
            }

            EditorGUI.EndProperty();
        }


        /// <inheritdoc cref="PropertyDrawer.GetPropertyHeight"/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.FindPropertyRelative("_isFoldoutExpanded").boolValue)
            {
                return 0.0f;
            }

            var elementVerticalSpacing = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            var height = 0f;

            // Add height for non-array properties
            for (var i = 0; i < _propertiesToDraw.Length; i++)
            {
                if (_propertiesToDraw[i].propertyName != _arrayName)
                {
                    height += elementVerticalSpacing;
                }
            }

            // Add height for array property
            var arrayProperty = property.FindPropertyRelative(_arrayName);
            arrayProperty.isExpanded = true;
            height += EditorGUI.GetPropertyHeight(arrayProperty, true);

            // Add height for the button
            height += elementVerticalSpacing;

            return height;
        }

        private void FindLowerBodyIndices(CharacterRetargeter retargeter, AnimationSkeletalProcessor animSkeletalProcessor)
        {
            MSDKUtility.CreateOrUpdateHandle(retargeter.Config, out var handle);
            var hipsIndex = GetKnownJointIndex(handle, MSDKUtility.KnownJointType.Hips);
            var leftLeg = GetKnownJointIndex(handle, MSDKUtility.KnownJointType.LeftUpperLeg);
            var rightLeg = GetKnownJointIndex(handle, MSDKUtility.KnownJointType.RightUpperLeg);
            var leftFoot = GetKnownJointIndex(handle, MSDKUtility.KnownJointType.LeftAnkle);
            var rightFoot = GetKnownJointIndex(handle, MSDKUtility.KnownJointType.RightAnkle);
            leftFoot = GetLowestChildJointIndex(handle, leftFoot);
            rightFoot = GetLowestChildJointIndex(handle, rightFoot);
            var leftFootParents = GetParentJointIndices(handle, leftFoot, leftLeg);
            var rightFootParents = GetParentJointIndices(handle, rightFoot, rightLeg);
            var indices = new List<TargetJointIndex> { new(hipsIndex) };
            indices.AddRange(leftFootParents.Select(i => new TargetJointIndex(i)));
            indices.AddRange(rightFootParents.Select(i => new TargetJointIndex(i)));
            indices.Sort();
            animSkeletalProcessor.AnimBlendIndices = indices.ToArray();
            MSDKUtility.DestroyHandle(handle);
        }

        private int GetKnownJointIndex(ulong handle, MSDKUtility.KnownJointType jointType)
        {
            MSDKUtility.GetJointIndexByKnownJointType(handle, MSDKUtility.SkeletonType.TargetSkeleton,
                jointType, out var index);
            return index;
        }

        private List<int> GetParentJointIndices(ulong handle, int startJointIndex, int targetJointIndex)
        {
            var indices = new List<int> { startJointIndex };
            var currentJointIndex = startJointIndex;
            while (currentJointIndex != targetJointIndex)
            {
                MSDKUtility.GetParentJointIndex(handle, MSDKUtility.SkeletonType.TargetSkeleton,
                    currentJointIndex, out var parentJointIndex);
                if (parentJointIndex == -1)
                {
                    break;
                }

                indices.Add(parentJointIndex);
                currentJointIndex = parentJointIndex;
            }

            return indices;
        }

        private int GetLowestChildJointIndex(ulong handle, int jointIndex)
        {
            var childIndex = -1;
            while (childIndex == -1)
            {
                MSDKUtility.GetChildJointIndexes(handle, MSDKUtility.SkeletonType.TargetSkeleton,
                    jointIndex, out var childIndices);
                if (childIndices.Length > 0)
                {
                    jointIndex = childIndices[0];
                }
                else
                {
                    childIndex = jointIndex;
                }
            }

            return childIndex;
        }
    }
}
