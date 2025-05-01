// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

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
            for (var i = 0; i < _propertiesToDraw.Length; i++)
            {
                var propertyRect = new Rect(position.x,
                    position.y + elementVerticalSpacing * i,
                    position.width, EditorGUIUtility.singleLineHeight);

                if (_propertiesToDraw[i].propertyName == _arrayName)
                {
                    DrawArray(propertyRect, arrayProperty);
                }
                else
                {
                    var serializedProperty = property.FindPropertyRelative(_propertiesToDraw[i].propertyName);
                    var serializedLabel = _propertiesToDraw[i].label;
                    EditorGUI.PropertyField(propertyRect, serializedProperty, serializedLabel);
                }
            }

            var buttonRect = new Rect(
                position.x,
                position.y + elementVerticalSpacing * (_propertiesToDraw.Length + arrayProperty.arraySize + 2),
                position.width,
                EditorGUIUtility.singleLineHeight);
            if (GUI.Button(buttonRect, "Update anim blend indices with lower body"))
            {
                var retargeter = Selection.activeGameObject.GetComponent<CharacterRetargeter>();
                if (retargeter != null)
                {
                    int numProcessors = retargeter.TargetProcessorContainers.Length;

                    // Only operate on the serialized property at our index. Otherwise, we might
                    // run setup on multiple.
                    int indexOfSerializedProperty =
                        MSDKUtilityHelper.GetIndexFromPropertyPath(property.propertyPath);
                    if (indexOfSerializedProperty < 0 || indexOfSerializedProperty >= numProcessors)
                    {
                        Debug.LogError(
                            $"Index of serialized processor is invalid: {indexOfSerializedProperty}. " +
                            $"Valid range is 0-{numProcessors - 1}.");
                    }
                    else
                    {
                        var currentProcessor =
                            retargeter.TargetProcessorContainers[indexOfSerializedProperty]
                                .GetCurrentProcessor();
                        if (currentProcessor is not AnimationSkeletalProcessor animSkeletalProcessor)
                        {
                            Debug.LogError(
                                $"Processor at {indexOfSerializedProperty} is not an animation processor.");
                        }
                        else
                        {
                            FindLowerBodyIndices(retargeter, animSkeletalProcessor);
                        }
                        EditorUtility.SetDirty(retargeter);
                    }
                }
            }

            EditorGUI.EndProperty();
        }

        private static void DrawArray(Rect rect, SerializedProperty property)
        {
            EditorGUI.PropertyField(rect, property, includeChildren: true);
        }

        /// <inheritdoc cref="PropertyDrawer.GetPropertyHeight"/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.FindPropertyRelative("_isFoldoutExpanded").boolValue)
            {
                return 0.0f;
            }

            var height = EditorGUIUtility.singleLineHeight;
            height += EditorGUIUtility.singleLineHeight * _propertiesToDraw.Length +
                      EditorGUIUtility.standardVerticalSpacing * _propertiesToDraw.Length;
            var blendIndicesProperty = property.FindPropertyRelative(_arrayName);
            height += (blendIndicesProperty.arraySize + 2f) *
                      (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);

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
