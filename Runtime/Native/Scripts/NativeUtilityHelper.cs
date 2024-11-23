// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using static Meta.XR.Movement.NativeUtilityPlugin;

namespace Meta.XR.Movement
{
    /// <summary>
    /// Utility helper functions for the native plugin.
    /// </summary>
    public class NativeUtilityHelper
    {
        /// <summary>
        /// Create the retargeting config string based on some data.
        /// </summary>
        /// <param name="targetBlendshapeNames">The array of target blendshape names.</param>
        /// <param name="sourceData">The retargeting data of the source skeleton.</param>
        /// <param name="targetData">The retargeting data of the target skeleton.</param>
        /// <param name="configName">The name of the config file.</param>
        /// <returns></returns>
        public static string CreateRetargetingConfig(string[] targetBlendshapeNames, RetargetingBodyData sourceData,
            RetargetingBodyData targetData, string configName)
        {
            // We need the following for the json:
            // 1. Source RetargetingBodyData
            // 2. Target RetargetingBodyData
            var sourceMinTPose = new NativeArray<NativeTransform>(sourceData.Joints.Length, Allocator.Temp,
                NativeArrayOptions.UninitializedMemory);
            var sourceMaxTPose = new NativeArray<NativeTransform>(sourceData.Joints.Length, Allocator.Temp,
                NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < sourceData.Joints.Length; i++)
            {
                sourceMinTPose[i] = sourceData.TPoseMin[i];
                sourceMaxTPose[i] = sourceData.TPoseMax[i];
            }

            var targetUnscaledTPose = new NativeArray<NativeTransform>(targetData.Joints.Length, Allocator.Temp,
                NativeArrayOptions.UninitializedMemory);
            var targetMinTPose = new NativeArray<NativeTransform>(targetData.Joints.Length, Allocator.Temp,
                NativeArrayOptions.UninitializedMemory);
            var targetMaxTPose = new NativeArray<NativeTransform>(targetData.Joints.Length, Allocator.Temp,
                NativeArrayOptions.UninitializedMemory);
            for (var i = 0; i < targetData.Joints.Length; i++)
            {
                targetUnscaledTPose[i] = targetData.TPose[i];
                targetMinTPose[i] = targetData.TPoseMin[i];
                targetMaxTPose[i] = targetData.TPoseMax[i];
            }

            var sourceKnownJoints = new[]
            {
                "Root",
                "Hips",
                "RightShoulder",
                "LeftShoulder",
                "RightWrist",
                "LeftWrist",
                "Chest",
                "Neck",
                "RightUpperLeg",
                "LeftUpperLeg",
                "RightAnkle",
                "LeftAnkle",
            };
            var targetKnownJoints = sourceKnownJoints;

            // 3 Function calls here, then return the retargetingConfigJson variable
            if (CreateOrUpdateUtilityConfig(
                    configName,
                    Array.Empty<string>(),
                    sourceData.Joints,
                    sourceData.ParentJoints,
                    sourceKnownJoints,
                    sourceMinTPose,
                    sourceMaxTPose,
                    targetBlendshapeNames,
                    targetData.Joints,
                    targetData.ParentJoints,
                    targetKnownJoints,
                    targetUnscaledTPose,
                    targetMinTPose,
                    targetMaxTPose,
                    out var configHandle))
            {
                WriteConfigDataToJson(configHandle, out var retargetingConfigJson);
                DestroyHandle(configHandle);
                return retargetingConfigJson;
            }
            return "";
        }

        /// <summary>
        /// Get the child to parent joint transform mappings for a target.
        /// </summary>
        /// <param name="target">The target game object to get mappings for.</param>
        /// <returns>The dictionary of child parent joint transform mappings.</returns>
        public static Dictionary<Transform, Transform> GetChildParentJointMapping(GameObject target)
        {
            // Start with looking for the hips (root) for body tracking.
            Transform hips = null;

            // First, check if an animator exists so that we can get the root.
            var animator = target.GetComponent<Animator>();
            if (animator != null && animator.avatar.isHuman)
            {
                hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            }
            else
            {
                // 1. Try to find hips manually by string matching.
                var hipsName = new[] { "hips" };
                hips = SearchForJointWithNameInChildren(target.transform, hipsName);

                // 2. If we can't find the hips by string matching, look for the legs and infer that the hips
                // is the parent of the legs.
                if (hips == null)
                {
                    var legsName = new[] { "leg" };
                    var leg = SearchForJointWithNameInChildren(target.transform, legsName);
                    if (leg != null)
                    {
                        hips = leg.parent;
                    }
                }
            }

            if (hips == null)
            {
                Debug.LogError("Could not find hips to start with!");
                return null;
            }

            // Once we have the hips, let's find all child transforms and create a dictionary mapping.
            var jointMapping = new Dictionary<Transform, Transform>();
            FillJointMapping(true, hips, ref jointMapping);
            return jointMapping;
        }

        /// <summary>
        /// Returns the joint pair from a given joint in a joint mapping.
        /// </summary>
        /// <param name="joint">The joint to check.</param>
        /// <param name="jointMapping">The joint mapping.</param>
        /// <returns>The joint pair.</returns>
        public static Transform GetJointPairFromMappings(Transform joint, Dictionary<Transform, Transform> jointMapping)
        {
            if (joint == null || jointMapping == null)
            {
                return null;
            }

            return jointMapping[joint];
        }

        private static Transform SearchForJointWithNameInChildren(Transform parent, string[] searchNames)
        {
            var queue = new Queue<Transform>();
            queue.Enqueue(parent);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (searchNames.Any(searchString => current.name.ToLower().Contains(searchString)))
                {
                    // Ensure whatever joint we're returning doesn't have a skinned mesh renderer.
                    if (current.GetComponent<SkinnedMeshRenderer>() == null)
                    {
                        return current;
                    }
                }

                foreach (Transform child in current)
                {
                    queue.Enqueue(child);
                }
            }

            return null;
        }

        private static void FillJointMapping(bool isRoot,
            Transform target, ref Dictionary<Transform, Transform> jointMapping)
        {
            var targetChildCount = target.childCount;
            jointMapping.Add(target, isRoot ? null : target.parent);
            for (var i = 0; i < targetChildCount; i++)
            {
                var child = target.GetChild(i);
                FillJointMapping(false, child, ref jointMapping);
            }
        }
    }
}
