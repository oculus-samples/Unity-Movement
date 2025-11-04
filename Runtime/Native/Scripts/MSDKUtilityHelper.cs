// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using Meta.XR.Movement.Retargeting;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using static Meta.XR.Movement.MSDKUtility;

namespace Meta.XR.Movement
{
    /// <summary>
    /// Utility helper functions for the native plugin.
    /// </summary>
    public static class MSDKUtilityHelper
    {
        /// <summary>
        /// Create the retargeting config string based on some data.
        /// </summary>
        /// <param name="targetBlendshapeNames">The array of target blendshape names.</param>
        /// <param name="sourceData">The retargeting data of the source skeleton.</param>
        /// <param name="targetData">The retargeting data of the target skeleton.</param>
        /// <param name="configName">The name of the config file.</param>
        /// <returns></returns>
        public static string CreateRetargetingConfig(
            string[] targetBlendshapeNames,
            SkeletonData sourceData,
            SkeletonData targetData,
            string configName)
        {
            var minMappings =
                new NativeArray<JointMapping>(0, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var minMappingEntries =
                new NativeArray<JointMappingEntry>(0, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var maxMappings =
                new NativeArray<JointMapping>(0, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var maxMappingEntries =
                new NativeArray<JointMappingEntry>(0, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            // Use the FillConfigInitParams method for source/target skeleton info
            var initParams = new ConfigInitParams
            {
                SourceSkeleton = sourceData.FillConfigInitParams(),
                TargetSkeleton = targetData.FillConfigInitParams()
            };

            // Override blend shape names for target skeleton
            initParams.TargetSkeleton.BlendShapeNames = targetBlendshapeNames;
            initParams.MinMappings.Mappings = minMappings;
            initParams.MinMappings.MappingEntries = minMappingEntries;
            initParams.MaxMappings.Mappings = maxMappings;
            initParams.MaxMappings.MappingEntries = maxMappingEntries;

            if (!CreateOrUpdateUtilityConfig(configName, initParams, out var configHandle))
            {
                var errorMessage = "Invalid config initialization params!";
                Debug.LogError($"{errorMessage}\n\n" + initParams);
                return "";
            }

            // Try to auto align.
            AlignTargetToSource(configName, AlignmentFlags.All, configHandle, SkeletonType.SourceSkeleton,
                configHandle, out configHandle);

            // Try to auto map.
            GenerateMappings(configHandle, AutoMappingFlags.EmptyFlag);
            WriteConfigDataToJson(configHandle, out var retargetingConfigJson);
            DestroyHandle(configHandle);
            return retargetingConfigJson;
        }

        /// <summary>
        /// Get the child to parent joint transform mappings for a target.
        /// </summary>
        /// <param name="target">The target game object to get mappings for.</param>
        /// <param name="root">The root transform of the game object.</param>
        /// <returns>The dictionary of child parent joint transform mappings.</returns>
        public static Dictionary<Transform, Transform> GetChildParentJointMapping(Transform target, out Transform root)
        {
            // First, check if an animator exists so that we can get the hips.
            Transform hips = null;
            root = null;
            var animator = target.GetComponent<Animator>();
            if (animator != null && animator.avatar != null && animator.avatar.isHuman)
            {
                hips = animator.GetBoneTransform(HumanBodyBones.Hips);
                if (hips != null)
                {
                    root = hips.parent;
                }
            }

            // If we couldn't find hips through the animator, use the valid humanoid rig hierarchy chain
            if (hips == null)
            {
                // Get all transforms in the hierarchy, filtering out non-joint transforms
                var allTransforms = new List<Transform> { target };
                allTransforms.AddRange(target.GetAllChildren());

                // Filter out transforms with skinned mesh renderers as they're not joints
                var jointTransforms = allTransforms
                    .Where(t => t.GetComponent<SkinnedMeshRenderer>() == null)
                    .ToArray();

                // Use the hierarchy validation approach from MSDKUtilityEditor to find valid humanoid hierarchy
                var validHumanoidRoot = FindValidHumanoidHierarchy(target);
                if (validHumanoidRoot != null)
                {
                    hips = validHumanoidRoot;
                    root = validHumanoidRoot.parent;
                }

                // Create joint names arrays using the filtered joint transforms based on valid hierarchy
                var jointNames = jointTransforms.Select(t => t.name).ToArray();
                var jointNameSet = new HashSet<string>(jointNames);
                var parentJointNames = jointTransforms.Select(t =>
                {
                    if (t.parent == null)
                    {
                        return "";
                    }

                    var parentName = t.parent.name;
                    // Only return parent name if it exists in our joint list, otherwise return empty string
                    return jointNameSet.Contains(parentName) ? parentName : "";
                }).ToArray();

                // Fallback - search by name using KnownJointFinder
                if (hips == null)
                {
                    // Use KnownJointFinder to find known joints
                    var knownJoints = KnownJointFinder.FindKnownJoints(jointNames, parentJointNames);

                    // Find the hips joint from the known joints
                    var hipsJointName = knownJoints.Length > (int)KnownJointType.Hips
                        ? knownJoints[(int)KnownJointType.Hips]
                        : "";
                    if (!string.IsNullOrEmpty(hipsJointName))
                    {
                        hips = jointTransforms.FirstOrDefault(t => t.name == hipsJointName);
                    }

                    // Find the root joint from the known joints - prioritize this for joint mapping
                    var rootJointName = knownJoints.Length > (int)KnownJointType.Root
                        ? knownJoints[(int)KnownJointType.Root]
                        : "";
                    if (!string.IsNullOrEmpty(rootJointName))
                    {
                        var knownRoot = jointTransforms.FirstOrDefault(t => t.name == rootJointName);
                        if (knownRoot != null)
                        {
                            // Use the actual known joint root for mapping, not just for hierarchy detection
                            root = knownRoot;
                        }
                    }
                }

                // If we found hips but not root, assume root is parent of hips
                if (hips != null && root == null)
                {
                    root = hips.parent;
                }
            }

            if (root == null)
            {
                Debug.LogError("Could not find root joint! Using the target transform as root");
                root = target;
            }

            // Once we have the root, let's find all child transforms and create a dictionary mapping.
            var jointMapping = new Dictionary<Transform, Transform>();
            FillJointMapping(true, root, ref jointMapping);

            return jointMapping;
        }

        /// <summary>
        /// Get the twist joint mappings for a specific skeleton type.
        /// This method retrieves a dictionary mapping Transform objects to their corresponding twist joint definitions.
        /// </summary>
        /// <param name="handle">The handle to get the twist joints from.</param>
        /// <param name="skeletonType">The type of skeleton to get the twist joints from.</param>
        /// <param name="root">The root transform to search for joints.</param>
        /// <returns>Dictionary mapping Transform objects to their TwistJointDefinition.</returns>
        public static Dictionary<Transform, TwistJointDefinition> GetTwistJointMappings(ulong handle, SkeletonType skeletonType, Transform root)
        {
            var twistJointMappings = new Dictionary<Transform, TwistJointDefinition>();
            // Get joint names for the skeleton type
            if (!GetJointNames(handle, skeletonType, out var jointNames))
            {
                Debug.LogWarning($"Failed to get joint names for skeleton type: {skeletonType}");
                return twistJointMappings;
            }
            // Get twist joints from the native API
            if (!GetTwistJoints(handle, skeletonType, out var twistJoints))
            {
                Debug.LogWarning($"Failed to get twist joints for skeleton type: {skeletonType}");
                return twistJointMappings;
            }
            // Map twist joint definitions to their corresponding transforms
            foreach (var twistJoint in twistJoints)
            {
                // Validate joint index bounds
                if (twistJoint.TwistJointIndex < 0 || twistJoint.TwistJointIndex >= jointNames.Length)
                {
                    Debug.LogWarning($"Invalid twist joint index: {twistJoint.TwistJointIndex}");
                    continue;
                }
                var jointName = jointNames[twistJoint.TwistJointIndex];
                if (string.IsNullOrEmpty(jointName))
                {
                    continue;
                }
                // Find the transform that corresponds to this joint
                var jointTransform = root.FindChildRecursive(jointName);
                if (jointTransform != null)
                {
                    twistJointMappings[jointTransform] = twistJoint;
                }
                else
                {
                    Debug.LogWarning($"Could not find transform for twist joint: {jointName}");
                }
            }
            return twistJointMappings;
        }

        /// <summary>
        /// Gets the root joint in a transform's children.
        /// </summary>
        /// <param name="handle">The handle.</param>
        /// <param name="root">The root transform to search.</param>
        /// <param name="index">The found root index.</param>
        /// <param name="rootJoint">The root joint transform.</param>
        public static void GetRootJoint(ulong handle, Transform root, out int index, out Transform rootJoint)
        {
            rootJoint = null;
            if (!GetJointIndexByKnownJointType(handle, SkeletonType.TargetSkeleton,
                    KnownJointType.Root, out index))
            {
                return;
            }

            GetJointNames(handle, SkeletonType.TargetSkeleton, out var jointNames);

            // Assume root joint is the parent of the hips joint.
            GetJointIndexByKnownJointType(handle, SkeletonType.TargetSkeleton, KnownJointType.Hips,
                out var hipsJointIndex);
            if (hipsJointIndex == -1)
            {
                return;
            }

            var hipsJoint = root.FindChildRecursive(jointNames[hipsJointIndex]);
            if (hipsJoint == null)
            {
                return;
            }

            rootJoint = hipsJoint.parent;

            if (rootJoint != null)
            {
                return;
            }

            var rootJointName = jointNames[index];
            rootJoint = root.FindChildRecursive(rootJointName);
        }

        public static List<Transform> GetAllChildren(this Transform parent)
        {
            var children = new List<Transform>();
            foreach (Transform child in parent)
            {
                children.Add(child);
                children.AddRange(child.GetAllChildren());
            }

            return children;
        }

        public static Transform GetLowestChild(Transform parent)
        {
            if (parent.childCount == 0)
            {
                return parent;
            }

            Transform lowestChild = null;
            foreach (Transform child in parent)
            {
                Transform childLowestChild = GetLowestChild(child);
                if (childLowestChild != null &&
                    (lowestChild == null || childLowestChild.childCount < lowestChild.childCount))
                {
                    lowestChild = childLowestChild;
                }
            }

            return lowestChild;
        }

        public static Transform FindChildRecursiveExact(Transform parent, string name)
        {
            if (parent.name == name)
            {
                return parent;
            }

            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == name)
                {
                    return child;
                }

                var result = FindChildRecursiveExact(child, name);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        public static Vector3 Reciprocal(this Vector3 vector)
        {
            if (vector.sqrMagnitude < float.Epsilon)
            {
                return Vector3.zero;
            }

            return new Vector3(
                1.0f / vector.x,
                1.0f / vector.y,
                1.0f / vector.z
            );
        }

        public static Vector3 InverseTransformVector(NativeTransform t, Vector3 v)
        {
            return Quaternion.Inverse(t.Orientation) * v;
        }

        public static Vector3 TransformVector(NativeTransform t, Vector3 v)
        {
            return t.Orientation * v;
        }

        public static void ScaleSkeletonPose(UInt64 handle, SkeletonType skeletonType,
            ref NativeArray<NativeTransform> scaledTransforms, float scale)
        {
            for (var i = 0; i < scaledTransforms.Length; i++)
            {
                var jointTransform = scaledTransforms[i];
                jointTransform.Position *= scale;
                scaledTransforms[i] = jointTransform;
            }
        }

        public static int GetIndexFromPropertyPath(string propertyPath)
        {
            var pathParts = propertyPath.Split('.');
            foreach (var part in pathParts)
            {
                if (!part.Contains("["))
                {
                    continue;
                }

                int indexOfLeftBracket = part.IndexOf('[');
                int indexOfRightBracket = part.IndexOf(']');
                return int.Parse(part.Substring(
                    indexOfLeftBracket + 1,
                    indexOfRightBracket - indexOfLeftBracket - 1));
            }

            return -1;
        }

        /// <summary>
        /// Determines if childJoint is a descendant (child/grandchild/etc.) of parentJoint.
        /// </summary>
        /// <param name="childJoint">The joint to check as descendant.</param>
        /// <param name="parentJoint">The joint to check as ancestor.</param>
        /// <returns>True if childJoint is a descendant of parentJoint.</returns>
        public static bool IsDescendantOf(Transform childJoint, Transform parentJoint)
        {
            return IsAncestorOf(parentJoint, childJoint);
        }

        /// <summary>
        /// Determines if parentJoint is an ancestor (parent/grandparent/etc.) of childJoint.
        /// </summary>
        /// <param name="parentJoint">The joint to check as ancestor.</param>
        /// <param name="childJoint">The joint to check as descendant.</param>
        /// <returns>True if parentJoint is an ancestor of childJoint.</returns>
        public static bool IsAncestorOf(Transform parentJoint, Transform childJoint)
        {
            var current = childJoint;
            while ((current = current.parent) != null)
            {
                if (current == parentJoint) return true;
            }
            return false;
        }

        /// <summary>
        /// Finds a valid humanoid hierarchy based on the same validation logic used in MSDKUtilityEditor.
        /// Looks for transforms with at least 3 children that each have minimum hierarchy depth.
        /// </summary>
        /// <param name="target">The target transform to validate and search.</param>
        /// <returns>The transform representing a valid humanoid hierarchy root, or null if not found.</returns>
        private static Transform FindValidHumanoidHierarchy(Transform target)
        {
            // Recursively search through all transforms in the hierarchy
            return ValidateHumanoidHierarchyRecursive(target);
        }

        /// <summary>
        /// Recursively validates transforms in the hierarchy for humanoid rig structure.
        /// Checks for structural hierarchy without relying on naming conventions.
        /// </summary>
        /// <param name="transform">The transform to check and recurse through.</param>
        /// <returns>The transform with valid humanoid hierarchy, or null if not found.</returns>
        private static Transform ValidateHumanoidHierarchyRecursive(Transform transform)
        {
            // Check if current transform has exactly 3 children with required depth
            if (transform.childCount >= 3)
            {
                int validChildrenCount = 0;
                foreach (Transform child in transform)
                {
                    // Check if child has minimum hierarchy depth
                    if (HasMinimumHierarchyDepth(child, 2))
                    {
                        // Check if child has different position than the potential hips (parent transform)
                        bool hasDifferentPosition = child.position != transform.position;

                        // Check if child does not have a skinned mesh renderer
                        bool hasNoSkinnedMeshRenderer = child.GetComponent<SkinnedMeshRenderer>() == null;

                        // Only count as valid if all conditions are met
                        if (hasDifferentPosition && hasNoSkinnedMeshRenderer)
                        {
                            validChildrenCount++;
                        }
                    }
                }

                // If at least 3 children meet all the requirements
                if (validChildrenCount >= 3)
                {
                    return transform;
                }
            }

            // Recursively check all children
            foreach (Transform child in transform)
            {
                var validHierarchy = ValidateHumanoidHierarchyRecursive(child);
                if (validHierarchy != null)
                {
                    return validHierarchy;
                }
            }

            return null;
        }

        /// <summary>
        /// Checks if a transform has the minimum required hierarchy depth.
        /// </summary>
        /// <param name="transform">The transform to check.</param>
        /// <param name="minDepth">The minimum depth required (1 = has child, 2 = has child and grandchild, etc.).</param>
        /// <returns>True if the transform has at least the minimum hierarchy depth.</returns>
        private static bool HasMinimumHierarchyDepth(Transform transform, int minDepth)
        {
            if (minDepth <= 0)
            {
                return true;
            }

            if (transform.childCount == 0)
            {
                return false;
            }

            if (minDepth == 1)
            {
                return true;
            }

            // Check if any child has the remaining depth
            foreach (Transform child in transform)
            {
                if (HasMinimumHierarchyDepth(child, minDepth - 1))
                {
                    return true;
                }
            }

            return false;
        }

        private static void FillJointMapping(
            bool isRoot,
            Transform target,
            ref Dictionary<Transform, Transform> jointMapping)
        {
            var targetChildCount = target.childCount;

            // We don't want to record skinned meshes
            if (target.GetComponent<SkinnedMeshRenderer>())
            {
                return;
            }

            jointMapping.Add(target, isRoot ? null : target.parent);
            for (var i = 0; i < targetChildCount; i++)
            {
                var child = target.GetChild(i);
                FillJointMapping(false, child, ref jointMapping);
            }
        }
    }
}
