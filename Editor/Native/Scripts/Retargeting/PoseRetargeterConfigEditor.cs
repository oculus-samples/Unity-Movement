// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using static Meta.XR.Movement.Retargeting.PoseRetargeterConfig;

namespace Meta.XR.Movement.Retargeting.Editor
{
    /// <summary>
    /// Custom editor for all pose retargeter configs.
    /// </summary>
    [CustomEditor(typeof(PoseRetargeterConfig))]
    public class PoseRetargeterConfigEditor : UnityEditor.Editor
    {
        /// <inheritdoc />
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var config = target as PoseRetargeterConfig;
            if (config == null)
            {
                return;
            }

            if (GUILayout.Button("Load Config"))
            {
                LoadConfig(serializedObject, config);
            }

            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
        }

        /// <summary>
        /// Loads config data to Unity using the native utility plugin.
        /// </summary>
        /// <param name="serializedObject">The serialized object to be updated</param>
        /// <param name="config">The config to be updated.</param>
        public static void LoadConfig(SerializedObject serializedObject, PoseRetargeterConfig config)
        {
            NativeUtilityPlugin.CreateOrUpdateHandle(config.Config, out var handle);

            // Load number of joints & Shapes for the Target Skeleton.
            NativeUtilityPlugin.GetSkeletonInfo(handle, NativeUtilityPlugin.SkeletonType.TargetSkeleton, out var skeletonInfo);

            // Find each joint and assign it.
            var jointPairs = serializedObject.FindProperty("_jointPairs");
            jointPairs.arraySize = skeletonInfo.JointCount;

            var rootJoint = config.transform;
            var animator = config.GetComponent<Animator>();
            if (animator != null && animator.avatar.isHuman)
            {
                rootJoint = animator.GetBoneTransform(HumanBodyBones.Hips).parent;
            }

            NativeUtilityPlugin.GetJointNames(handle, NativeUtilityPlugin.SkeletonType.TargetSkeleton, out var jointNames);
            NativeUtilityPlugin.GetParentJointIndexes(handle, NativeUtilityPlugin.SkeletonType.TargetSkeleton, out var parentJointIndexes);
            Debug.Assert(jointNames.Length == skeletonInfo.JointCount && parentJointIndexes.Length == skeletonInfo.JointCount);

            for (var i = 0; i < jointNames.Length; i++)
            {
                var jointName = jointNames[i];
                var joint = FindChildRecursive(jointName, rootJoint);
                var jointParent = parentJointIndexes[i] != NativeUtilityPlugin.INVALID_JOINT_INDEX
                    ? FindChildRecursive(jointNames[parentJointIndexes[i]], rootJoint) : null;

                jointPairs.GetArrayElementAtIndex(i).FindPropertyRelative("Joint").objectReferenceValue = joint;
                jointPairs.GetArrayElementAtIndex(i).FindPropertyRelative("ParentJoint").objectReferenceValue = jointParent;
            }

            var skinnedMeshes = config.transform.GetComponentsInChildren<SkinnedMeshRenderer>();
            var shapePoses = serializedObject.FindProperty("_shapePoseData");
            // Given the blendshapes stored in the config, associate them with skinnedmesh renderers.
            // This is organized into a collection.
            List<ShapePoseData> shapePoseDatas = new List<ShapePoseData>();
            if (skeletonInfo.BlendShapeCount == 0)
            {
                Debug.LogWarning("No blendshapes in config.");
            }
            else
            {
                shapePoseDatas = BuildShapePoseData(handle, skinnedMeshes, skeletonInfo.BlendShapeCount);
                Debug.Log($"Created {shapePoseDatas.Count} for blend shape count {skeletonInfo.BlendShapeCount}.");
            }


            if (shapePoseDatas.Count > 0)
            {
                var shapePoseDataField = serializedObject.FindProperty("_shapePoseData");
                shapePoses.arraySize = shapePoseDatas.Count;
                for (int i = 0; i < shapePoseDatas.Count; i++)
                {
                    var arrayElement = shapePoses.GetArrayElementAtIndex(i);
                    arrayElement.FindPropertyRelative("SkinnedMesh").objectReferenceValue =
                        shapePoseDatas[i].SkinnedMesh;
                    arrayElement.FindPropertyRelative("ShapeName").stringValue =
                        shapePoseDatas[i].ShapeName;
                    arrayElement.FindPropertyRelative("ShapeIndex").intValue =
                        shapePoseDatas[i].ShapeIndex;
                }
            }
            else
            {
                Debug.LogWarning("Could not populate field with blendshape data.");
            }

            parentJointIndexes.Dispose();
            NativeUtilityPlugin.DestroyHandle(handle);
        }

        private static List<ShapePoseData> BuildShapePoseData(UInt64 handle, SkinnedMeshRenderer[] skinnedMeshes, int expectedShapeCount)
        {
            List<ShapePoseData> shapePoseDatas = new List<ShapePoseData>();
            int currentConfigBlendIndex = 0;
            NativeUtilityPlugin.GetBlendShapeNames(handle, NativeUtilityPlugin.SkeletonType.TargetSkeleton, out var blendShapeNames);

            foreach (var skinnedMeshRenderer in skinnedMeshes)
            {
                var sharedMesh = skinnedMeshRenderer.sharedMesh;
                var numBlendshapes = sharedMesh.blendShapeCount;
                string currentShapeNameToMatch = blendShapeNames[currentConfigBlendIndex];
                for (int blendShapeIndex = 0; blendShapeIndex < numBlendshapes; blendShapeIndex++)
                {
                    // See if the skinned mesh's blendshape matches the one in the config.
                    // If so, add that to collection.
                    var currentBlendshapeName = sharedMesh.GetBlendShapeName(blendShapeIndex);
                    if (currentBlendshapeName == currentShapeNameToMatch)
                    {
                        shapePoseDatas.Add(new ShapePoseData(skinnedMeshRenderer, currentBlendshapeName, blendShapeIndex));
                        // go find the next blendshape in the config
                        currentConfigBlendIndex++;
                        // if we don't have anymore blendshapes to find based on the config, stop
                        if (currentConfigBlendIndex >= expectedShapeCount)
                        {
                            break;
                        }
                        currentShapeNameToMatch = blendShapeNames[currentConfigBlendIndex];
                    }
                }

                if (currentConfigBlendIndex >= expectedShapeCount)
                {
                    break;
                }
            }

            return shapePoseDatas;
        }

        private static Transform FindChildRecursive(string nameToFind, Transform rootJoint)
        {
            var t = FindChildRecursiveWithMatch(nameToFind, rootJoint, true);
            return t != null ? t : FindChildRecursiveWithMatch(nameToFind, rootJoint, false);
        }

        private static Transform FindChildRecursiveWithMatch(string nameToFind, Transform rootJoint, bool exactMatch)
        {
            var queue = new Queue<Transform>();
            queue.Enqueue(rootJoint); // Start with the current transform
            while (queue.Count > 0)
            {
                Transform current = queue.Dequeue();
                // Check for exact match or contains based on the exactMatch parameter
                if ((exactMatch && current.name == nameToFind) || (!exactMatch && current.name.Contains(nameToFind)))
                {
                    return current; // Return the matching transform
                }
                // Enqueue all children of the current transform
                foreach (Transform child in current)
                {
                    queue.Enqueue(child);
                }
            }
            return null; // Return null if no matching transform is found
        }
    }
}
