// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Meta.XR.Movement.MSDKUtility;
using static Meta.XR.Movement.Retargeting.SkeletonData;
using Enumerable = System.Linq.Enumerable;


namespace Meta.XR.Movement.Editor
{
    /// <summary>
    /// Utility class for generating joint mappings between source and target skeletons.
    /// </summary>
    public static class JointMappingGeneration
    {
        private static readonly FullBodyTrackingBoneId[] _invalidJoints =
        {
            FullBodyTrackingBoneId.LeftHandWristTwist,
            FullBodyTrackingBoneId.LeftHandPalm,
            FullBodyTrackingBoneId.LeftHandThumbTip,
            FullBodyTrackingBoneId.LeftHandIndexTip,
            FullBodyTrackingBoneId.LeftHandMiddleTip,
            FullBodyTrackingBoneId.LeftHandRingTip,
            FullBodyTrackingBoneId.LeftHandLittleTip,
            FullBodyTrackingBoneId.RightHandWristTwist,
            FullBodyTrackingBoneId.RightHandPalm,
            FullBodyTrackingBoneId.RightHandThumbTip,
            FullBodyTrackingBoneId.RightHandIndexTip,
            FullBodyTrackingBoneId.RightHandMiddleTip,
            FullBodyTrackingBoneId.RightHandRingTip,
            FullBodyTrackingBoneId.RightHandLittleTip,
            FullBodyTrackingBoneId.LeftFootAnkleTwist,
            FullBodyTrackingBoneId.RightFootAnkleTwist,
        };

        private static readonly FullBodyTrackingBoneId[] _invalidMappingJoints =
        {
            FullBodyTrackingBoneId.RightHandWristTwist,
            FullBodyTrackingBoneId.RightHandPalm,
            FullBodyTrackingBoneId.LeftHandWristTwist,
            FullBodyTrackingBoneId.LeftHandPalm,
            FullBodyTrackingBoneId.RightUpperLeg,
            FullBodyTrackingBoneId.RightFootAnkleTwist,
            FullBodyTrackingBoneId.LeftUpperLeg,
            FullBodyTrackingBoneId.LeftFootAnkleTwist,
            FullBodyTrackingBoneId.Head
        };

        private static readonly KnownJointType[] _singleMappedJoints =
        {
            KnownJointType.Root,
            KnownJointType.RightWrist,
            KnownJointType.LeftWrist,
            KnownJointType.Neck,
            KnownJointType.RightUpperLeg,
            KnownJointType.LeftUpperLeg,
            KnownJointType.RightAnkle,
            KnownJointType.LeftAnkle,
        };

        private const int _mappedJointCount = 4;
        private const float _mappingWeightThreshold = 0.05f;

        /// <summary>
        /// Generates joint weights for mapping between source and target skeletons.
        /// </summary>
        /// <param name="config">The editor window configuration containing source and target skeleton information.</param>
        /// <param name="mapping">The list of joint mappings to populate.</param>
        /// <param name="mappingEntries">The list of joint mapping entries to populate.</param>
        public static void GenerateJointWeights(MSDKUtilityEditorWindow config, ref List<JointMapping> mapping,
            ref List<JointMappingEntry> mappingEntries)
        {
            GetJointIndexByKnownJointType(config.TargetInfo.ConfigHandle, SkeletonType.TargetSkeleton,
                KnownJointType.LeftUpperArm, out var leftUpperArmIndex);
            GetJointIndexByKnownJointType(config.TargetInfo.ConfigHandle, SkeletonType.TargetSkeleton,
                KnownJointType.RightUpperArm, out var rightUpperArmIndex);
            GetParentJointIndex(config.TargetInfo.ConfigHandle, SkeletonType.TargetSkeleton, leftUpperArmIndex,
                out var leftShoulderIndex);
            GetParentJointIndex(config.TargetInfo.ConfigHandle, SkeletonType.TargetSkeleton, rightUpperArmIndex,
                out var rightShoulderIndex);

            for (var index = 0; index < config.TargetInfo.SkeletonJoints.Length; index++)
            {
                var targetJoint = config.TargetInfo.SkeletonJoints[index];
                var jointName = config.TargetInfo.JointNames[index];
                var closestSourceJointIndices = new int[_mappedJointCount];
                var closestDistances = Enumerable.Repeat(float.MaxValue, _mappedJointCount).ToArray();

                // Check if this is a known joint
                if (config.TargetInfo.KnownJointNames.Contains(jointName))
                {
                    if (WriteKnownJointMapping(config, index,
                            ref closestSourceJointIndices, ref closestDistances, ref mapping, ref mappingEntries))
                    {
                        continue;
                    }
                }

                // Find the closest joints without considering the hierarchy
                for (var i = 1; i < config.SourceInfo.ReferencePose.Length; i++)
                {
                    if (_invalidJoints.Contains((FullBodyTrackingBoneId)i))
                    {
                        continue;
                    }

                    var distance = Vector3.Distance(targetJoint.position, config.SourceInfo.ReferencePose[i].Position);
                    for (var j = 0; j < _mappedJointCount; j++)
                    {
                        if (!(distance < closestDistances[j]) || closestSourceJointIndices.Contains(i))
                        {
                            continue;
                        }

                        // Shift the closest joints and distances to make room for the new closest joint
                        for (var k = _mappedJointCount - 1; k > j; k--)
                        {
                            closestSourceJointIndices[k] = closestSourceJointIndices[k - 1];
                            closestDistances[k] = closestDistances[k - 1];
                        }

                        closestSourceJointIndices[j] = i;
                        closestDistances[j] = distance;
                        break;
                    }
                }

                // Get the closest joint
                var closestJointIndex = closestSourceJointIndices[0];

                // Setup invalid joints to prevent shoulders from weighting each other
                var currentInvalidMappingJoints = new List<FullBodyTrackingBoneId>();
                currentInvalidMappingJoints.AddRange(_invalidMappingJoints);
                if (index == leftShoulderIndex)
                {
                    closestJointIndex = (int)FullBodyTrackingBoneId.LeftShoulder;
                    currentInvalidMappingJoints.Add(FullBodyTrackingBoneId.Neck);
                    currentInvalidMappingJoints.Add(FullBodyTrackingBoneId.RightShoulder);
                    currentInvalidMappingJoints.Add(FullBodyTrackingBoneId.RightScapula);
                }

                if (index == rightShoulderIndex)
                {
                    closestJointIndex = (int)FullBodyTrackingBoneId.RightShoulder;
                    currentInvalidMappingJoints.Add(FullBodyTrackingBoneId.Neck);
                    currentInvalidMappingJoints.Add(FullBodyTrackingBoneId.LeftShoulder);
                    currentInvalidMappingJoints.Add(FullBodyTrackingBoneId.LeftScapula);
                }

                // Filter closestSourceJointIndices to only include the closest joint and its child or parent
                var filteredClosestSourceJointIndices = new List<int>
                {
                    closestJointIndex
                };
                if (!IsFinger(config.TargetInfo.ConfigHandle, closestJointIndex))
                {
                    // Get the child and parent indices of the closest joint
                    var grandParentIndex = -1;
                    var childIndexes = Array.Empty<int>();
                    GetParentJointIndex(config.TargetInfo.ConfigHandle, SkeletonType.SourceSkeleton, closestJointIndex,
                        out var parentIndex);
                    if (parentIndex != -1)
                    {
                        GetParentJointIndex(config.TargetInfo.ConfigHandle, SkeletonType.SourceSkeleton, parentIndex,
                            out grandParentIndex);
                        GetChildJointIndexes(config.TargetInfo.ConfigHandle, SkeletonType.SourceSkeleton, parentIndex,
                            out var nativeChildIndexes);
                        childIndexes = nativeChildIndexes.ToArray();
                    }

                    // Add the child indices to the filtered list
                    foreach (var childIndex in childIndexes)
                    {
                        if (!filteredClosestSourceJointIndices.Contains(childIndex) &&
                            !currentInvalidMappingJoints.Contains((FullBodyTrackingBoneId)childIndex))
                        {
                            filteredClosestSourceJointIndices.Add(childIndex);
                        }
                    }

                    // Add the parent index and its children to the filtered list
                    if (parentIndex != -1 &&
                        !filteredClosestSourceJointIndices.Contains(parentIndex) &&
                        !currentInvalidMappingJoints.Contains((FullBodyTrackingBoneId)parentIndex))
                    {
                        filteredClosestSourceJointIndices.Add(parentIndex);
                    }

                    if (grandParentIndex != -1 &&
                        !filteredClosestSourceJointIndices.Contains(grandParentIndex) &&
                        !currentInvalidMappingJoints.Contains((FullBodyTrackingBoneId)grandParentIndex))
                    {
                        filteredClosestSourceJointIndices.Add(grandParentIndex);
                    }
                }

                closestSourceJointIndices = filteredClosestSourceJointIndices.ToArray();
                WriteJointMapping(config, ref mapping, ref mappingEntries, index, closestSourceJointIndices,
                    targetJoint.position);
            }
        }

        private static bool IsFinger(ulong handle, int jointIndex)
        {
            var currentIndex = jointIndex;

            // Define hand joint IDs
            var handJoints = new[]
            {
                (int)FullBodyTrackingBoneId.LeftHandWrist,
                (int)FullBodyTrackingBoneId.RightHandWrist
            };

            // Traverse up the hierarchy until we find a hand joint or reach the root
            while (currentIndex != -1)
            {
                // Check if current joint is a hand joint
                if (handJoints.Contains(currentIndex))
                {
                    return true;
                }

                // Get parent joint
                GetParentJointIndex(handle, SkeletonType.SourceSkeleton, currentIndex,
                    out var parentIndex);

                // If we've reached the root or there's no parent, stop traversing
                if (parentIndex == -1 || parentIndex == currentIndex)
                {
                    break;
                }

                currentIndex = parentIndex;
            }

            return false;
        }

        private static bool WriteKnownJointMapping(MSDKUtilityEditorWindow config,
            int index,
            ref int[] closestSourceJointIndices,
            ref float[] closestDistances,
            ref List<JointMapping> mapping,
            ref List<JointMappingEntry> mappingEntries)
        {
            var knownIndex = Array.IndexOf(config.TargetInfo.KnownJointNames, config.TargetInfo.JointNames[index]);
            var knownJointType = (KnownJointType)knownIndex;
            GetJointIndexByKnownJointType(config.TargetInfo.ConfigHandle, SkeletonType.SourceSkeleton, knownJointType,
                out var sourceIndex);
            closestSourceJointIndices[0] = sourceIndex;
            closestDistances[0] = 0.0f;

            // If this is a single mapped joint, don't map to anything but this joint
            if (!_singleMappedJoints.Contains(knownJointType))
            {
                return false;
            }

            mapping.Add(new JointMapping
            {
                JointIndex = index,
                Type = SkeletonType.SourceSkeleton,
                Behavior = JointMappingBehaviorType.Normal,
                EntriesCount = 1
            });
            mappingEntries.Add(new JointMappingEntry
            {
                JointIndex = sourceIndex,
                PositionWeight = 1.0f,
                RotationWeight = 1.0f
            });
            return true;
        }

        private static void WriteJointMapping(
            MSDKUtilityEditorWindow config,
            ref List<JointMapping> mapping,
            ref List<JointMappingEntry> mappingEntries,
            int index,
            int[] closestSourceJointIndices,
            Vector3 targetJointPosition)
        {
            // Pick the closest source joint to keep its rotation weight by 1 (not blended)
            var filteredJointCount = closestSourceJointIndices.Length;
            var positions = new Vector3[filteredJointCount];
            var distances = new float[filteredJointCount];
            for (var i = 0; i < filteredJointCount; i++)
            {
                positions[i] = config.SourceInfo.ReferencePose[closestSourceJointIndices[i]].Position;
                distances[i] = Vector3.Distance(targetJointPosition, positions[i]);
            }

            // Calculate weights using Inverse Distance Weighting (IDW)
            float sum = 0;
            foreach (var distance in distances)
            {
                if (distance > 0)
                {
                    sum += 1 / Mathf.Pow(distance, 2);
                }
                else
                {
                    sum += float.MaxValue;
                }
            }

            var weights = new float[filteredJointCount];
            for (var i = 0; i < filteredJointCount; i++)
            {
                if (distances[i] > 0)
                {
                    weights[i] = (1 / Mathf.Pow(distances[i], 2)) / sum;
                }
                else
                {
                    weights[i] = 1;
                }
            }

            // Eliminate any weights that are less than a threshold and redistribute the remaining weights.
            var validWeights = new List<float>();
            var validJointIndices = new List<int>();
            for (var i = 0; i < filteredJointCount; i++)
            {
                if (weights[i] >= _mappingWeightThreshold)
                {
                    validWeights.Add(weights[i]);
                    validJointIndices.Add(closestSourceJointIndices[i]);
                }
            }

            // Redistribute the remaining weights so that they sum up to 1.
            var weightSum = validWeights.Sum();
            if (weightSum > 0)
            {
                for (int i = 0; i < validWeights.Count; i++)
                {
                    validWeights[i] /= weightSum;
                }
            }
            else
            {
                // If all weights were eliminated, assign equal weights to all joints.
                float equalWeight = 1f / validJointIndices.Count;
                for (int i = 0; i < validWeights.Count; i++)
                {
                    validWeights[i] = equalWeight;
                }
            }

            // Add the remaining weights and joint indices to the mapping
            mapping.Add(new JointMapping
            {
                JointIndex = index,
                Type = SkeletonType.SourceSkeleton,
                Behavior = JointMappingBehaviorType.Normal,
                EntriesCount = validWeights.Count
            });
            mappingEntries.AddRange(validWeights.Select((t, i) => new JointMappingEntry
            {
                JointIndex = validJointIndices[i],
                PositionWeight = t,
                // RotationWeight = t
                RotationWeight = validJointIndices[i] == closestSourceJointIndices[0] ? 1.0f : 0.0f
            }));
        }


    }
}
