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

        private static readonly string[][] _knownJointNames =
        {
            new[] { "reference", "root", "armature" },
            new[] { "hips", "pelvis", "spine0", "root" },
            new[]
            {
                "rightupperarm", "upperarmright", "upperarmr", "rupperarm", "rightarm", "rightarmupper", "armright",
                "rightshoulder", "shoulderright", "shoulderr", "rshoulder", "armr"
            },
            new[]
            {
                "leftupperarm", "upperarmleft", "upperarml", "lupperarm", "leftarm", "leftarmupper", "armleft",
                "leftshoulder", "shoulderleft", "shoulderl", "lshoulder", "arml"
            },
            new[]
            {
                "righthandwrist", "righthand", "rightwrist", "handright", "wristright", "handr", "rhand", "rwrist"
            },
            new[] { "lefthandwrist", "lefthand", "leftwrist", "handleft", "wristleft", "handl", "lhand", "lwrist" },
            new[] { "chest", "spine3", "spine2", "spine1", "spineupper", "spinelower", "spine" },
            new[] { "neck" },
            new[]
            {
                "rightupperleg", "rightupleg", "rightleg", "rightlegupper", "rightlegup", "rthigh", "thighr", "legr",
                "rleg", "rhip"
            },
            new[]
            {
                "leftupperleg", "leftupleg", "leftleg", "leftlegupper", "leftlegup", "lthigh", "thighl", "legl", "lleg",
                "lhip"
            },
            new[]
            {
                "rightfootankle", "rightfoot", "rightankle", "footright", "ankleright", "footr", "rfoot", "rankle",
                "ankler"
            },
            new[]
            {
                "leftfootankle", "leftfoot", "leftankle", "footleft", "ankleleft", "footl", "lfoot", "lankle", "anklel"
            },
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
            var targetKnownJoints = FindKnownJoints(targetData);

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
        /// <param name="isOvrSkeleton">True if we should also include the root in the mapping.</param>
        /// <returns>The dictionary of child parent joint transform mappings.</returns>
        public static Dictionary<Transform, Transform> GetChildParentJointMapping(Transform target, bool isOvrSkeleton)
        {
            // First, check if an animator exists so that we can get the hips.
            Transform hips;
            var animator = target.GetComponent<Animator>();
            if (animator != null && animator.avatar != null && animator.avatar.isHuman)
            {
                hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            }
            else
            {
                // 1. Try to find hips manually by string matching.
                var hipsNames = _knownJointNames[(int)KnownJointType.Hips];
                hips = SearchForJointWithNameInChildren(target, hipsNames, 3);

                // 2. If we can't find the hips by string matching, look for the legs and infer that the hips
                // is the parent of the legs.
                if (hips == null)
                {
                    var legNames = _knownJointNames[(int)KnownJointType.LeftUpperLeg];
                    var leg = SearchForJointWithNameInChildren(target, legNames, 1);
                    if (leg != null)
                    {
                        hips = leg.parent;
                    }
                }
                else
                {
                    // Keep traversing upward - should be a zero-ed out root.
                    while (hips.parent.transform.position.sqrMagnitude > float.Epsilon)
                    {
                        hips = hips.parent;
                    }
                }
            }
            if (hips == null)
            {
                Debug.LogError("Could not find hips to start with!");
                return null;
            }

            // Assign root after finding hips
            var root = hips.parent;

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

        public static List<(Transform, float)> FindClosestMatches(
            Transform[] transforms, string[] namesToMatch, float threshold = 90f)
        {
            // Remove shared prefix and suffix from joints array
            var trimmedJoints = TrimSharedPrefixAndSuffix(transforms.Select(n => n.name).ToArray());
            var matches = new List<(Transform, float, int)>(); // Added index to track position in namesToMatch
            for (int i = 0; i < namesToMatch.Length; i++)
            {
                var targetJoint = namesToMatch[i];
                var matched = false;
                var bestMatch = (default(Transform), 0.0f, i);
                foreach (var joint in trimmedJoints)
                {
                    // Get the corresponding transform for the current joint
                    var jointTransform = transforms.FirstOrDefault(n => n.name == joint.Key);
                    if (jointTransform == null)
                    {
                        continue;
                    }

                    // Calculate the similarity between the target joint and the current joint
                    var jointName = Regex.Replace(joint.Value, @"[^a-zA-Z0-9]", "").ToLower();
                    var similarity = CalculateLevenshteinDistance(targetJoint, jointName);

                    // If the similarity is above the threshold, add it to the matches list and mark it as matched
                    if (similarity >= threshold)
                    {
                        matches.Add((jointTransform, similarity, i));
                        matched = true;
                        break;
                    }

                    // Pick the most similar match.
                    if (similarity > bestMatch.Item2)
                    {
                        bestMatch = (jointTransform, similarity, i);
                    }
                }

                if (matched)
                {
                    continue;
                }

                // Add the best match if an exact match couldn't be found.
                if (bestMatch.Item1 != default(Transform))
                {
                    matches.Add(bestMatch);
                }
            }

            // Group matches by similarity within 5% of each other
            var groupedMatches = new List<(Transform, float)>();
            var processedMatches = new List<(Transform, float, int)>(matches);

            while (processedMatches.Count > 0)
            {
                // Find the match with the highest similarity
                var bestMatch = processedMatches.OrderByDescending(x => x.Item2).First();
                var similarMatches = processedMatches
                    .Where(m => Math.Abs(m.Item2 - bestMatch.Item2) <= 5.0f)
                    .ToList();

                // If multiple matches are within 5%, prioritize by order in namesToMatch (lower index = higher priority)
                if (similarMatches.Count > 1)
                {
                    var prioritizedMatch = similarMatches.OrderBy(m => m.Item3).First();
                    groupedMatches.Add((prioritizedMatch.Item1, prioritizedMatch.Item2));
                }
                else
                {
                    groupedMatches.Add((bestMatch.Item1, bestMatch.Item2));
                }

                // Remove processed matches
                foreach (var match in similarMatches)
                {
                    processedMatches.Remove(match);
                }
            }

            return groupedMatches;
        }

        public static void ScaleSkeletonPose(UInt64 handle, SkeletonType skeletonType,
            ref NativeArray<NativeTransform> scaledTransforms, float scale)
        {
            GetJointIndexByKnownJointType(handle, skeletonType, KnownJointType.Root, out var rootIndex);
            if (rootIndex == INVALID_JOINT_INDEX)
            {
                // Sanity check in case the root isn't specified
                rootIndex = 0;
            }

            var rootTransform = scaledTransforms[rootIndex];
            var rootPosition = rootTransform.Position;
            for (var i = 0; i < scaledTransforms.Length; i++)
            {
                var jointTransform = scaledTransforms[i];
                jointTransform.Position = (jointTransform.Position - rootPosition) * scale;
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

        private static Transform SearchForJointWithNameInChildren(Transform parent, string[] searchNames,
            int expectedChildCount)
        {
            // Get all children transforms
            var allTransforms = new List<Transform> { parent };
            allTransforms.AddRange(parent.GetAllChildren());

            // Filter out transforms with skinned mesh renderers or insufficient child count
            var validTransforms = allTransforms
                .Where(t => t.GetComponent<SkinnedMeshRenderer>() == null && t.childCount >= expectedChildCount)
                .ToArray();

            if (validTransforms.Length == 0)
            {
                return null;
            }

            // Use FindClosestMatches to find the best match based on Levenshtein distance
            var matches = FindClosestMatches(validTransforms, searchNames);

            // Return the best match if any was found
            return matches.Count > 0 ? matches[0].Item1 : null;
        }

        private static void FillJointMapping(
            bool isRoot,
            Transform target,
            ref Dictionary<Transform, Transform> jointMapping)
        {
            var targetChildCount = target.childCount;
            jointMapping.Add(target, isRoot ? null : target.parent);
            for (var i = 0; i < targetChildCount; i++)
            {
                var child = target.GetChild(i);
                FillJointMapping(false, child, ref jointMapping);
            }
        }

        private static string[] FindKnownJoints(SkeletonData data)
        {
            var knownJoints = new string[(int)KnownJointType.KnownJointCount];
            var matchPercentages = new float[knownJoints.Length];
            knownJoints[0] = data.Joints[0];

            for (var i = KnownJointType.Hips; i < KnownJointType.KnownJointCount; i++)
            {
                var typeIndex = (int)i;
                var isSpecialJoint = i is KnownJointType.Hips or KnownJointType.Chest or
                    KnownJointType.LeftUpperArm or KnownJointType.RightUpperArm or
                    KnownJointType.LeftUpperLeg or KnownJointType.RightUpperLeg;

                var results = FindClosestMatches(data.Joints, _knownJointNames[typeIndex],
                    isSpecialJoint ? 30f : 75f);
                if (results.Count == 0)
                {
                    Debug.LogError($"Could not find any known joint for {i}.");
                    continue;
                }

                var bestResult = results[0];

                // Filter results based on joint type
                if (results.Count > 1)
                {
                    var filtered = i switch
                    {
                        KnownJointType.Hips or KnownJointType.Chest => results
                            .Where(j => data.ParentJoints.Count(p => p == j.Item1) >= 3)
                            .ToList(),
                        KnownJointType.RightUpperLeg or KnownJointType.LeftUpperLeg when
                            knownJoints[(int)KnownJointType.Hips] != null => results.Where(j =>
                            {
                                var jointIndex = Array.IndexOf(data.Joints, j.Item1);
                                var oppositeJointIndex = Array.IndexOf(data.Joints,
                                    knownJoints[
                                        (int)(i == KnownJointType.LeftUpperLeg
                                            ? KnownJointType.RightUpperLeg
                                            : KnownJointType.LeftUpperLeg)]);

                                return jointIndex >= 0 &&
                                       jointIndex != oppositeJointIndex &&
                                       data.ParentJoints[jointIndex] == knownJoints[(int)KnownJointType.Hips];
                            }).ToList(),
                        KnownJointType.RightUpperArm or KnownJointType.LeftUpperArm when
                            knownJoints[(int)KnownJointType.Chest] != null => results.Where(j =>
                            {
                                var jointIndex = Array.IndexOf(data.Joints, j.Item1);
                                var oppositeJointIndex = Array.IndexOf(data.Joints,
                                    knownJoints[
                                        (int)(i == KnownJointType.LeftUpperArm
                                            ? KnownJointType.RightUpperArm
                                            : KnownJointType.LeftUpperArm)]);

                                return jointIndex >= 0 &&
                                       jointIndex != oppositeJointIndex &&
                                       data.ParentJoints[jointIndex] != knownJoints[(int)KnownJointType.Chest];
                            }).ToList(),
                        _ => null
                    };

                    if (filtered?.Count > 0)
                    {
                        bestResult = filtered[0];
                    }
                }

                knownJoints[typeIndex] = bestResult.Item1;
                matchPercentages[typeIndex] = bestResult.Item2;
            }

            return knownJoints;
        }

        /// <summary>
        /// Find the closest matches for each name in namesToMatch and sorts them by similarity.
        /// </summary>
        /// <param name="names">The names to search.</param>
        /// <param name="namesToMatch">The names to match.</param>
        /// <param name="threshold">The name similarity threshold.</param>
        /// <returns>List of closest matches.</returns>
        public static List<(string, float)> FindClosestMatches(
            string[] names, string[] namesToMatch, float threshold = 90f)
        {
            // Remove shared prefix and suffix from joints array
            var trimmedJoints = TrimSharedPrefixAndSuffix(names);
            var matches = new List<(string, float, int)>(); // Added index to track position in namesToMatch
            for (int i = 0; i < namesToMatch.Length; i++)
            {
                var targetJoint = namesToMatch[i];
                var bestMatch = (string.Empty, 0.0f, i);
                foreach (var joint in trimmedJoints)
                {
                    // Calculate the similarity between the target joint and the current joint
                    var jointName = Regex.Replace(joint.Value, @"[^a-zA-Z0-9]", "").ToLower();
                    var similarity = CalculateLevenshteinDistance(targetJoint, jointName);

                    // If the similarity is above the threshold, add it to the matches list and mark it as matched
                    if (similarity >= threshold)
                    {
                        matches.Add((joint.Key, similarity, i));
                    }

                    // Pick the most similar match.
                    if (similarity > bestMatch.Item2)
                    {
                        bestMatch = (joint.Key, similarity, i);
                    }
                }

                // Add the best match if an exact match couldn't be found.
                if (!string.IsNullOrEmpty(bestMatch.Item1))
                {
                    matches.Add(bestMatch);
                }
            }

            // Group matches by similarity within 5% of each other
            var groupedMatches = new List<(string, float)>();
            var processedMatches = new List<(string, float, int)>(matches);

            while (processedMatches.Count > 0)
            {
                // Find the match with the highest similarity
                var bestMatch = processedMatches.OrderByDescending(x => x.Item2).First();
                var similarMatches = processedMatches
                    .Where(m => Math.Abs(m.Item2 - bestMatch.Item2) <= 5.0f)
                    .ToList();

                // If multiple matches are within 5%, prioritize by order in namesToMatch (lower index = higher priority)
                if (similarMatches.Count > 1)
                {
                    var prioritizedMatch = similarMatches.OrderBy(m => m.Item3).First();
                    groupedMatches.Add((prioritizedMatch.Item1, prioritizedMatch.Item2));
                }
                else
                {
                    groupedMatches.Add((bestMatch.Item1, bestMatch.Item2));
                }

                // Remove processed matches
                foreach (var match in similarMatches)
                {
                    processedMatches.Remove(match);
                }
            }

            return groupedMatches;
        }

        private static Dictionary<string, string> TrimSharedPrefixAndSuffix(string[] joints)
        {
            if (joints.Length == 0)
            {
                return new Dictionary<string, string>();
            }

            // Take up to first three joints to derive prefix and suffix
            var sampleJoints = new[]
            {
                joints[1], joints[2], joints[3]
            };

            // Find common prefix from sample joints
            var prefix = sampleJoints[0];
            foreach (var joint in sampleJoints)
            {
                var i = 0;
                var len = Math.Min(prefix.Length, joint.Length);
                while (i < len && prefix[i] == joint[i])
                {
                    i++;
                }

                prefix = prefix[..i];
                if (string.IsNullOrEmpty(prefix))
                {
                    break;
                }
            }

            // Find common suffix from sample joints
            var suffix = sampleJoints[0];
            foreach (var joint in sampleJoints)
            {
                var i = 0;
                var len = Math.Min(suffix.Length, joint.Length);
                while (i < len && suffix[suffix.Length - 1 - i] == joint[joint.Length - 1 - i])
                {
                    i++;
                }

                suffix = suffix[^i..];
                if (string.IsNullOrEmpty(suffix)) break;
            }

            // Check if prefix/suffix is shared by at least 90% of items
            var prefixCount = joints.Count(j => prefix != null && j.StartsWith(prefix));
            var suffixCount = joints.Count(j => suffix != null && j.EndsWith(suffix));
            var prefixThreshold = joints.Length * 0.9f;
            var suffixThreshold = joints.Length * 0.9f;

            var usePrefix = !string.IsNullOrEmpty(prefix) && prefixCount >= prefixThreshold;
            var useSuffix = !string.IsNullOrEmpty(suffix) && suffixCount >= suffixThreshold;

            return joints.ToDictionary(
                j => j,
                j =>
                {
                    var result = j;
                    if (prefix != null && usePrefix && j.StartsWith(prefix))
                    {
                        result = result[prefix.Length..];
                    }

                    if (suffix != null && useSuffix && result.EndsWith(suffix))
                    {
                        result = result[..^suffix.Length];
                    }

                    return result;
                }
            );
        }

        private static float CalculateLevenshteinDistance(string s1, string s2)
        {
            var d = new int[s1.Length + 1, s2.Length + 1];
            for (var i = 0; i <= s1.Length; i++)
            {
                d[i, 0] = i;
            }

            for (var j = 0; j <= s2.Length; j++)
            {
                d[0, j] = j;
            }

            for (var i = 1; i <= s1.Length; i++)
            {
                for (var j = 1; j <= s2.Length; j++)
                {
                    var cost = (s1[i - 1] == s2[j - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            var maxLength = Math.Max(s1.Length, s2.Length);
            var similarity = 1 - (float)d[s1.Length, s2.Length] / maxLength;
            return similarity * 100;
        }
    }
}
