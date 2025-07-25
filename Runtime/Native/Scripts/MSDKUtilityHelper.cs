// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Meta.XR.Movement.Retargeting;
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
        public static readonly string[] AutoMapExcludedJointNames =
        {
            "LeftHandPalm",
            "LeftHandWristTwist",
            "RightHandPalm",
            "RightHandWristTwist"
        };

        public static readonly string[] HalfBodyManifestationJointNames =
        {
            "Root",
            "Hips",
            "SpineLower",
            "SpineMiddle",
            "SpineUpper",
            "Chest",
            "Neck",
            "Head",
            "LeftShoulder",
            "LeftScapula",
            "LeftArmUpper",
            "LeftArmLower",
            "LeftHandWristTwist",
            "RightShoulder",
            "RightScapula",
            "RightArmUpper",
            "RightArmLower",
            "RightHandWristTwist",
            "LeftHandPalm",
            "LeftHandWrist",
            "LeftHandThumbMetacarpal",
            "LeftHandThumbProximal",
            "LeftHandThumbDistal",
            "LeftHandThumbTip",
            "LeftHandIndexMetacarpal",
            "LeftHandIndexProximal",
            "LeftHandIndexIntermediate",
            "LeftHandIndexDistal",
            "LeftHandIndexTip",
            "LeftHandMiddleMetacarpal",
            "LeftHandMiddleProximal",
            "LeftHandMiddleIntermediate",
            "LeftHandMiddleDistal",
            "LeftHandMiddleTip",
            "LeftHandRingMetacarpal",
            "LeftHandRingProximal",
            "LeftHandRingIntermediate",
            "LeftHandRingDistal",
            "LeftHandRingTip",
            "LeftHandLittlePinkyMetacarpal",
            "LeftHandLittlePinkyProximal",
            "LeftHandLittlePinkyIntermediate",
            "LeftHandLittlePinkyDistal",
            "LeftHandLittlePinkyTip",
            "RightHandPalm",
            "RightHandWrist",
            "RightHandThumbMetacarpal",
            "RightHandThumbProximal",
            "RightHandThumbDistal",
            "RightHandThumbTip",
            "RightHandIndexMetacarpal",
            "RightHandIndexProximal",
            "RightHandIndexIntermediate",
            "RightHandIndexDistal",
            "RightHandIndexTip",
            "RightHandMiddleMetacarpal",
            "RightHandMiddleProximal",
            "RightHandMiddleIntermediate",
            "RightHandMiddleDistal",
            "RightHandMiddleTip",
            "RightHandRingMetacarpal",
            "RightHandRingProximal",
            "RightHandRingIntermediate",
            "RightHandRingDistal",
            "RightHandRingTip",
            "RightHandLittlePinkyMetacarpal",
            "RightHandLittlePinkyProximal",
            "RightHandLittlePinkyIntermediate",
            "RightHandLittlePinkyDistal",
            "RightHandLittlePinkyTip"
        };

        private static readonly string[] _sourceKnownJoints =
        {
            "Root",
            "Hips",
            "RightArmUpper",
            "LeftArmUpper",
            "RightHandWrist",
            "LeftHandWrist",
            "Chest",
            "Neck",
            "RightUpperLeg",
            "LeftUpperLeg",
            "RightFootAnkle",
            "LeftFootAnkle",
        };

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
            // We need the following for the json:
            // 1. Source RetargetingBodyData
            // 2. Target RetargetingBodyData
            var sourceMinTPose = new NativeArray<NativeTransform>(sourceData.Joints.Length, Allocator.Temp,
                NativeArrayOptions.UninitializedMemory);
            var sourceMaxTPose = new NativeArray<NativeTransform>(sourceData.Joints.Length, Allocator.Temp,
                NativeArrayOptions.UninitializedMemory);
            for (var i = 0; i < sourceData.Joints.Length; i++)
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

            var minMappings =
                new NativeArray<JointMapping>(0, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var minMappingEntries =
                new NativeArray<JointMappingEntry>(0, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var maxMappings =
                new NativeArray<JointMapping>(0, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var maxMappingEntries =
                new NativeArray<JointMappingEntry>(0, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var targetKnownJoints = KnownJointFinder.FindKnownJoints(targetData.Joints, targetData.ParentJoints);

            // 3. Function calls here, then return the retargetingConfigJson variable
            var initParams = new ConfigInitParams();
            initParams.SourceSkeleton.JointNames = sourceData.Joints;
            initParams.SourceSkeleton.ParentJointNames = sourceData.ParentJoints;
            initParams.SourceSkeleton.OptionalKnownSourceJointNamesById = _sourceKnownJoints;
            initParams.SourceSkeleton.OptionalAutoMapExcludedJointNames = AutoMapExcludedJointNames;
            initParams.SourceSkeleton.OptionalManifestationNames =
                new[] { MetaSourceDataProvider.HalfBodyManifestation };
            initParams.SourceSkeleton.OptionalManifestationJointCounts =
                new[] { (int)SkeletonData.BodyTrackingBoneId.End };
            initParams.SourceSkeleton.OptionalManifestationJointNames = HalfBodyManifestationJointNames;
            initParams.SourceSkeleton.MinTPose = sourceMinTPose;
            initParams.SourceSkeleton.MaxTPose = sourceMaxTPose;

            initParams.TargetSkeleton.BlendShapeNames = targetBlendshapeNames;
            initParams.TargetSkeleton.JointNames = targetData.Joints;
            initParams.TargetSkeleton.ParentJointNames = targetData.ParentJoints;
            initParams.TargetSkeleton.OptionalKnownSourceJointNamesById = targetKnownJoints;
            initParams.TargetSkeleton.MinTPose = targetMinTPose;
            initParams.TargetSkeleton.MaxTPose = targetMaxTPose;
            initParams.TargetSkeleton.UnscaledTPose = targetUnscaledTPose;

            initParams.MinMappings.Mappings = minMappings;
            initParams.MinMappings.MappingEntries = minMappingEntries;
            initParams.MaxMappings.Mappings = maxMappings;
            initParams.MaxMappings.MappingEntries = maxMappingEntries;

            if (!CreateOrUpdateUtilityConfig(configName, initParams, out var configHandle))
            {
                Debug.LogError("Invalid config initialization params!\n\n" + initParams);
                return "";
            }

            // 4. Try to auto align.
            AlignTargetToSource(configName, AlignmentFlags.All, configHandle, SkeletonType.SourceSkeleton,
                configHandle, out configHandle);

            // 5. Try to auto map.
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

            // If we couldn't find hips through the animator, use KnownJointFinder
            if (hips == null)
            {
                // Get all transforms in the hierarchy
                var allTransforms = new List<Transform> { target };
                allTransforms.AddRange(target.GetAllChildren());

                // Filter out transforms with skinned mesh renderers as they're not joints
                var jointTransforms = allTransforms
                    .Where(t => t.GetComponent<SkinnedMeshRenderer>() == null)
                    .ToArray();

                // Create joint names and parent joint names arrays
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

                if (hips == null)
                {
                    foreach (var jointTransform in jointTransforms)
                    {
                        // Check if position is greater than 0, 0, 0
                        if (!(jointTransform.position.x > 0) && !(jointTransform.position.y > 0) &&
                            !(jointTransform.position.z > 0))
                        {
                            continue;
                        }
                        // Count children that are different in position or rotation
                        var validChildrenCount = 0;
                        for (var i = 0; i < jointTransform.childCount; i++)
                        {
                            var child = jointTransform.GetChild(i);
                            // Check if child is different in position or rotation from parent
                            if (child.localPosition != Vector3.zero || child.localRotation != Quaternion.identity)
                            {
                                validChildrenCount++;
                            }
                        }

                        // If this transform has 3 or more valid children, consider it as hips
                        if (validChildrenCount < 3)
                        {
                            continue;
                        }
                        hips = jointTransform;
                        break;
                    }
                }

                // Fallback - search by name.
                if (hips == null)
                {
                    // Use KnownJointFinder to find known joints
                    var knownJoints = KnownJointFinder.FindKnownJoints(jointNames, parentJointNames);

                    // Find the hips joint from the known joints
                    var hipsJointName = knownJoints.Length > (int)KnownJointType.Hips ? knownJoints[(int)KnownJointType.Hips] : "";
                    if (!string.IsNullOrEmpty(hipsJointName))
                    {
                        hips = jointTransforms.FirstOrDefault(t => t.name == hipsJointName);
                    }

                    // Find the root joint from the known joints
                    var rootJointName = knownJoints.Length > (int)KnownJointType.Root ? knownJoints[(int)KnownJointType.Root] : "";
                    if (!string.IsNullOrEmpty(rootJointName))
                    {
                        root = jointTransforms.FirstOrDefault(t => t.name == rootJointName);
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
