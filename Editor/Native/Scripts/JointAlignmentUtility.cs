// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using static Meta.XR.Movement.MSDKUtility;
using static Meta.XR.Movement.Retargeting.SkeletonData;

namespace Meta.XR.Movement.Editor
{
    /// <summary>
    /// Utility class for aligning joints in a skeleton.
    /// </summary>
    public class JointAlignmentUtility
    {
        /// <summary>
        /// Aligns finger bones between source and target skeletons using a position-independent approach.
        /// </summary>
        /// <param name="win">The editor window.</param>
        /// <param name="startBoneIds">The array of starting bone IDs for fingers.</param>
        /// <param name="endBoneIds">The array of ending bone IDs for fingers.</param>
        /// <param name="wristType">The wrist joint type.</param>
        public static void PerformFingerMatching(MSDKUtilityEditorWindow win,
            FullBodyTrackingBoneId[] startBoneIds,
            FullBodyTrackingBoneId[] endBoneIds,
            KnownJointType wristType)
        {
            var source = win.SourceInfo;
            var target = win.TargetInfo;

            // Get the wrist joint from the target skeleton
            var wristJointIndex = GetKnownJointIndex(target, wristType);
            var wristTransform = target.SkeletonJoints[wristJointIndex];

            if (wristTransform == null)
            {
                Debug.LogError($"Could not find wrist transform for {wristType}");
                return;
            }

            // Update wrist rotation.
            ApplyWristFingerAlignment(win, startBoneIds, wristType);

            // Get finger chains using the simplified method
            Dictionary<string, List<Transform>> fingerChains = GetFingerChains(wristTransform);

            // Process each finger chain
            foreach (var fingerPair in fingerChains)
            {
                string fingerName = fingerPair.Key;
                List<Transform> fingerChain = fingerPair.Value;

                if (fingerChain.Count < 2)
                {
                    // Need at least two joints for a proper finger
                    continue;
                }

                // Find the corresponding source fingertip
                FullBodyTrackingBoneId? sourceFingertipId = null;

                // Map finger name to source bone ID
                if (fingerName.Equals("Thumb", StringComparison.OrdinalIgnoreCase))
                {
                    sourceFingertipId = wristType == KnownJointType.LeftWrist
                        ? FullBodyTrackingBoneId.LeftHandThumbTip
                        : FullBodyTrackingBoneId.RightHandThumbTip;
                }
                else if (fingerName.Equals("Index", StringComparison.OrdinalIgnoreCase))
                {
                    sourceFingertipId = wristType == KnownJointType.LeftWrist
                        ? FullBodyTrackingBoneId.LeftHandIndexTip
                        : FullBodyTrackingBoneId.RightHandIndexTip;
                }
                else if (fingerName.Equals("Middle", StringComparison.OrdinalIgnoreCase))
                {
                    sourceFingertipId = wristType == KnownJointType.LeftWrist
                        ? FullBodyTrackingBoneId.LeftHandMiddleTip
                        : FullBodyTrackingBoneId.RightHandMiddleTip;
                }
                else if (fingerName.Equals("Ring", StringComparison.OrdinalIgnoreCase))
                {
                    sourceFingertipId = wristType == KnownJointType.LeftWrist
                        ? FullBodyTrackingBoneId.LeftHandRingTip
                        : FullBodyTrackingBoneId.RightHandRingTip;
                }
                else if (fingerName.Equals("Pinky", StringComparison.OrdinalIgnoreCase))
                {
                    sourceFingertipId = wristType == KnownJointType.LeftWrist
                        ? FullBodyTrackingBoneId.LeftHandLittleTip
                        : FullBodyTrackingBoneId.RightHandLittleTip;
                }

                if (!sourceFingertipId.HasValue || (int)sourceFingertipId.Value >= source.ReferencePose.Length)
                {
                    // Skip if we don't have a valid source fingertip
                    continue;
                }

                // Get the source fingertip position
                Vector3 sourceFingertipPosition = source.ReferencePose[(int)sourceFingertipId.Value].Position;

                // Apply simple rotation to align the finger with the source fingertip
                AlignFingerToTarget(fingerChain, sourceFingertipPosition);
            }

            // Update the T-pose data after adjusting the fingers
            UpdateTPoseData(target);
        }

        /// <summary>
        /// Aligns wrist joints between source and target skeletons.
        /// </summary>
        /// <param name="source">The source skeleton configuration.</param>
        /// <param name="target">The target skeleton configuration.</param>
        public static void PerformWristMatching(MSDKUtilityEditorConfig source, MSDKUtilityEditorConfig target)
        {
            // Rotate to match left/right hand.
            var sourceRightHand = source.ReferencePose[(int)FullBodyTrackingBoneId.RightHandWrist];
            var sourceLeftHand = source.ReferencePose[(int)FullBodyTrackingBoneId.LeftHandWrist];
            var leftArmIndex = GetKnownJointIndex(target, KnownJointType.LeftUpperArm);
            var rightArmIndex = GetKnownJointIndex(target, KnownJointType.RightUpperArm);

            var leftUpperArm = target.SkeletonJoints[leftArmIndex];
            var leftHand =
                target.SkeletonJoints[GetKnownJointIndex(target, KnownJointType.LeftWrist)];
            leftUpperArm.rotation =
                GetBestRotation(leftUpperArm, leftHand, sourceLeftHand.Position);

            var rightUpperArm = target.SkeletonJoints[rightArmIndex];
            var rightHand =
                target.SkeletonJoints[GetKnownJointIndex(target, KnownJointType.RightWrist)];
            rightUpperArm.rotation =
                GetBestRotation(rightUpperArm, rightHand, sourceRightHand.Position);

            UpdateTPoseData(target);
        }

        /// <summary>
        /// Performs automatic alignment between source and target skeletons.
        /// </summary>
        /// <param name="win">The editor window.</param>
        public static void AutoAlignment(MSDKUtilityEditorWindow win)
        {
            win.TargetInfo.SkeletonTPoseType = win.Step switch
            {
                MSDKUtilityEditorConfig.EditorStep.MinTPose => SkeletonTPoseType.MinTPose,
                MSDKUtilityEditorConfig.EditorStep.MaxTPose => SkeletonTPoseType.MaxTPose,
                _ => win.TargetInfo.SkeletonTPoseType
            };

            win.ModifyConfig(false, true);
            if (win.FileReader.IsPlaying)
            {
                win.Previewer.DestroyPreviewCharacterRetargeter();
                win.OpenPlaybackFile(win.CurrentPreviewPose);
            }
        }

        /// <summary>
        /// Loads the scale from the target configuration into the utility configuration.
        /// </summary>
        /// <param name="target">The target configuration.</param>
        /// <param name="utilityConfig">The utility configuration to update.</param>
        public static void LoadScale(MSDKUtilityEditorConfig target, MSDKUtilityEditorConfig utilityConfig)
        {
            var targetTPose = new NativeArray<NativeTransform>(target.ReferencePose.Length, Allocator.Temp);
            targetTPose.CopyFrom(target.ReferencePose);
            ConvertJointPose(target.ConfigHandle,
                SkeletonType.TargetSkeleton,
                JointRelativeSpaceType.RootOriginRelativeSpace,
                JointRelativeSpaceType.LocalSpaceScaled,
                targetTPose,
                out var scaledTPose);
            GetJointIndexByKnownJointType(target.ConfigHandle, SkeletonType.TargetSkeleton, KnownJointType.Root,
                out var rootJointIndex);
            utilityConfig.RootScale = utilityConfig.Step != MSDKUtilityEditorConfig.EditorStep.Configuration
                ? scaledTPose[rootJointIndex].Scale
                : Vector3.one;
            // Specific case handling for when the root scale is less than range of values due to invalid setup
            if (utilityConfig.RootScale.x < 0.20f)
            {
                utilityConfig.RootScale *= 10f;
            }
        }

        /// <summary>
        /// Updates the T-pose data in the target configuration based on the current skeleton joints.
        /// </summary>
        /// <param name="target">The target configuration to update.</param>
        public static void UpdateTPoseData(MSDKUtilityEditorConfig target)
        {
            for (var i = 0; i < target.SkeletonJoints.Length; i++)
            {
                var joint = target.SkeletonJoints[i];
                if (joint == null)
                {
                    target.ReferencePose[i] = NativeTransform.Identity();
                }
                else
                {
                    target.ReferencePose[i] = new NativeTransform
                    {
                        Position = joint.position,
                        Orientation = joint.rotation,
                        Scale = Vector3.one * joint.localScale.x
                    };
                }
            }
        }

        /// <summary>
        /// Scales the arms of the target skeleton to match the source skeleton.
        /// </summary>
        /// <param name="source">The source configuration.</param>
        /// <param name="target">The target configuration to modify.</param>
        public static void PerformArmScaling(MSDKUtilityEditorConfig source, MSDKUtilityEditorConfig target)
        {
            // Scale arms - stretch/squash for aligning X-axis.
            var sourceRightHand = source.ReferencePose[(int)FullBodyTrackingBoneId.RightHandWrist];
            var sourceLeftHand = source.ReferencePose[(int)FullBodyTrackingBoneId.LeftHandWrist];
            var targetRightHand = GetTargetTPoseFromKnownJoint(target, KnownJointType.RightWrist);
            var targetLeftHand = GetTargetTPoseFromKnownJoint(target, KnownJointType.LeftWrist);

            var sourceRightWristVal = Mathf.Abs(sourceRightHand.Position.x);
            var targetRightWristVal = Mathf.Abs(targetRightHand.Position.x);
            var sourceLeftWristVal = Mathf.Abs(sourceLeftHand.Position.x);
            var targetLeftWristVal = Mathf.Abs(targetLeftHand.Position.x);

            var xScaleRatioRightWrist = sourceRightWristVal / (targetRightWristVal > 0.0f
                ? targetRightWristVal
                : 1.0f);
            var xScaleRatioLeftWrist = sourceLeftWristVal / (targetLeftWristVal > 0.0f
                ? targetLeftWristVal
                : 1.0f);
            GetJointIndexByKnownJointType(target.ConfigHandle, SkeletonType.TargetSkeleton,
                KnownJointType.RightUpperArm,
                out var rightArmIndex);
            GetJointIndexByKnownJointType(target.ConfigHandle, SkeletonType.TargetSkeleton, KnownJointType.LeftUpperArm,
                out var leftArmIndex);
            GetParentJointIndex(target.ConfigHandle, SkeletonType.TargetSkeleton, rightArmIndex, out var rightIndex);
            GetParentJointIndex(target.ConfigHandle, SkeletonType.TargetSkeleton, leftArmIndex, out var leftIndex);
            ApplyScalingToJointIndexAndChildren(target, rightIndex, xScaleRatioRightWrist, true);
            ApplyScalingToJointIndexAndChildren(target, leftIndex, xScaleRatioLeftWrist, true);
        }

        /// <summary>
        /// Applies scaling to a joint and all its children.
        /// </summary>
        /// <param name="target">The target configuration to modify.</param>
        /// <param name="jointIndex">The index of the joint to scale.</param>
        /// <param name="scaleFactor">The scale factor to apply.</param>
        /// <param name="skip">Whether to skip scaling the specified joint and only scale its children.</param>
        public static void ApplyScalingToJointIndexAndChildren(MSDKUtilityEditorConfig target, int jointIndex,
            float scaleFactor, bool skip = false)
        {
            // Apply the scaling to the current joint.
            if (!skip)
            {
                var joint = target.ReferencePose[jointIndex];
                joint.Position.x *= scaleFactor;
                target.ReferencePose[jointIndex] = joint;
            }

            // Recursively apply the scaling to the children of the current joint.
            for (var i = 0; i < target.ParentIndices.Length; i++)
            {
                if (target.ParentIndices[i] == jointIndex)
                {
                    ApplyScalingToJointIndexAndChildren(target, i, scaleFactor);
                }
            }
        }

        /// <summary>
        /// Gets the index of a known joint type in the target skeleton.
        /// </summary>
        /// <param name="config">The configuration to query.</param>
        /// <param name="jointType">The known joint type to find.</param>
        /// <returns>The index of the joint in the skeleton.</returns>
        public static int GetKnownJointIndex(MSDKUtilityEditorConfig config, KnownJointType jointType)
        {
            GetJointIndexByKnownJointType(config.ConfigHandle, SkeletonType.TargetSkeleton,
                jointType, out var index);
            return index;
        }

        /// <summary>
        /// Gets the T-pose transform for a known joint type in the target skeleton.
        /// </summary>
        /// <param name="target">The target configuration.</param>
        /// <param name="knownJointType">The known joint type to find.</param>
        /// <returns>The native transform of the joint in T-pose.</returns>
        public static NativeTransform GetTargetTPoseFromKnownJoint(MSDKUtilityEditorConfig target,
            KnownJointType knownJointType)
        {
            return target.ReferencePose[GetKnownJointIndex(target, knownJointType)];
        }

        /// <summary>
        /// Calculates the desired scale factor between source and target skeletons.
        /// </summary>
        /// <param name="source">The source configuration.</param>
        /// <param name="target">The target configuration.</param>
        /// <param name="sourceTPoseType">The type of T-pose to use for the source.</param>
        /// <param name="useCurrentPose">Whether to use the current pose instead of the T-pose.</param>
        /// <returns>The calculated scale factor.</returns>
        public static float GetDesiredScale(MSDKUtilityEditorConfig source, MSDKUtilityEditorConfig target,
            SkeletonTPoseType sourceTPoseType, bool useCurrentPose = false)
        {
            if (sourceTPoseType == SkeletonTPoseType.UnscaledTPose)
            {
                return 1.0f;
            }

            GetSkeletonTPose(source.ConfigHandle, SkeletonType.SourceSkeleton, sourceTPoseType,
                JointRelativeSpaceType.RootOriginRelativeSpace, out var sourceTPose);
            GetSkeletonTPose(target.ConfigHandle, SkeletonType.TargetSkeleton, SkeletonTPoseType.UnscaledTPose,
                JointRelativeSpaceType.RootOriginRelativeSpace,
                out var targetTPose);

            var rightHandJointIndex = GetKnownJointIndex(target, KnownJointType.RightWrist);
            var sourceRightHand = sourceTPose[(int)FullBodyTrackingBoneId.RightHandWrist];
            var targetRightHand =
                useCurrentPose ? target.ReferencePose[rightHandJointIndex] : targetTPose[rightHandJointIndex];
            var yScaleRatio = sourceRightHand.Position.y /
                              (targetRightHand.Position.y > 0.0f ? targetRightHand.Position.y : 1.0f);
            return yScaleRatio;
        }

        /// <summary>
        /// Finds the best rotation for a parent transform to position a child transform at a target position.
        /// </summary>
        /// <param name="parent">The parent transform to rotate.</param>
        /// <param name="child">The child transform to position.</param>
        /// <param name="targetPosition">The target position for the child.</param>
        /// <param name="rotationThreshold">The maximum allowed rotation angle difference.</param>
        /// <param name="rotationStep">The step size for rotation testing.</param>
        /// <param name="maxRotation">The maximum rotation angle to test.</param>
        /// <returns>The best rotation for the parent transform.</returns>
        public static Quaternion GetBestRotation(Transform parent, Transform child, Vector3 targetPosition,
            float rotationThreshold = 90f, float rotationStep = 0.1f, float maxRotation = 360f)
        {
            var initialRotation = parent.rotation;
            var childRotation = child.rotation;
            var minDistance = Mathf.Infinity;
            var bestRotation = initialRotation;
            foreach (var axis in new[] { parent.forward, parent.up, parent.right })
            {
                for (var angle = -maxRotation; angle <= maxRotation; angle += rotationStep)
                {
                    parent.rotation = initialRotation * Quaternion.AngleAxis(angle, axis);
                    var distance = Vector3.Distance(child.position, targetPosition);
                    if (distance < minDistance && Quaternion.Angle(childRotation, child.rotation) < rotationThreshold)
                    {
                        minDistance = distance;
                        bestRotation = parent.rotation;
                    }
                }
            }

            return bestRotation;
        }

        private static void ApplyWristFingerAlignment(MSDKUtilityEditorWindow win,
            FullBodyTrackingBoneId[] startBoneIds,
            KnownJointType wristType)
        {
            var source = win.SourceInfo;
            var target = win.TargetInfo;

            // Get the wrist joint from the target skeleton
            var wristJointIndex = GetKnownJointIndex(target, wristType);
            var wristTransform = target.SkeletonJoints[wristJointIndex];

            if (wristTransform == null)
            {
                Debug.LogError($"Could not find wrist transform for {wristType}");
                return;
            }

            // Calculate the average position of source metacarpal joints
            Vector3 sourceMetacarpalAverage = Vector3.zero;
            int validMetacarpalCount = 0;

            foreach (var startBoneId in startBoneIds)
            {
                if ((int)startBoneId >= 0 && (int)startBoneId < source.ReferencePose.Length)
                {
                    sourceMetacarpalAverage += source.ReferencePose[(int)startBoneId].Position;
                    validMetacarpalCount++;
                }
            }

            if (validMetacarpalCount > 0)
            {
                sourceMetacarpalAverage /= validMetacarpalCount;
            }
            else
            {
                Debug.LogError("No valid metacarpal joints found in source skeleton");
                return;
            }

            // Store the initial rotation of the wrist
            Quaternion initialWristRotation = wristTransform.rotation;

            // Find the best rotation for the wrist to align child fingers with source metacarpals
            // We'll test rotations around different axes to find the best match
            float minDistance = float.MaxValue;
            Quaternion bestRotation = initialWristRotation;

            // Get all finger joints under the wrist
            List<Transform> fingerJoints = new List<Transform>();
            for (int i = 0; i < target.ParentIndices.Length; i++)
            {
                if (IsChildOfJoint(target, i, wristJointIndex))
                {
                    fingerJoints.Add(target.SkeletonJoints[i]);
                }
            }

            // Test rotations around different axes
            foreach (var axis in new[] { wristTransform.forward, wristTransform.up, wristTransform.right })
            {
                for (float angle = -180f; angle <= 180f; angle += 5f) // Use a larger step for initial search
                {
                    wristTransform.rotation = initialWristRotation * Quaternion.AngleAxis(angle, axis);

                    // Calculate the average position of all finger joints after rotation
                    Vector3 targetFingerAverage = Vector3.zero;
                    foreach (var joint in fingerJoints)
                    {
                        targetFingerAverage += joint.position;
                    }

                    targetFingerAverage /= fingerJoints.Count;

                    // Calculate distance between averages
                    float distance = Vector3.Distance(targetFingerAverage, sourceMetacarpalAverage);

                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        bestRotation = wristTransform.rotation;
                    }
                }
            }

            // Apply the best rotation
            wristTransform.rotation = bestRotation;

            // Update the T-pose data after rotating the wrist
            UpdateTPoseData(target);
        }

        private static Dictionary<string, List<Transform>> GetFingerChains(Transform wristTransform)
        {
            Dictionary<string, List<Transform>> fingerChains = new Dictionary<string, List<Transform>>();

            // Define finger names to search for
            string[] fingerNames = new[] { "Thumb", "Index", "Middle", "Ring", "Pinky", "Little" };

            // Check each child of the wrist for potential finger roots
            foreach (Transform child in wristTransform)
            {
                // Try to determine which finger this might be
                string matchedFingerName = null;
                foreach (string fingerName in fingerNames)
                {
                    if (child.name.IndexOf(fingerName, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        matchedFingerName = fingerName;
                        break;
                    }
                }

                // If this child doesn't match any finger name, skip it
                if (matchedFingerName == null)
                {
                    continue;
                }

                // Special case for "Little" which is an alternative name for "Pinky"
                if (matchedFingerName == "Little")
                {
                    matchedFingerName = "Pinky";
                }

                // Build the finger chain by following the hierarchy
                List<Transform> chain = new List<Transform>();
                Transform current = child;

                // Add the starting joint
                chain.Add(current);

                // Follow the hierarchy to build the chain
                while (current.childCount > 0)
                {
                    // Simply take the first child to continue the chain
                    current = current.GetChild(0);
                    chain.Add(current);

                    // Limit chain length as a safety measure
                    if (chain.Count >= 5)
                    {
                        break;
                    }
                }

                // Only add the chain if it doesn't already exist or if this one is longer
                if (!fingerChains.ContainsKey(matchedFingerName) ||
                    chain.Count > fingerChains[matchedFingerName].Count)
                {
                    fingerChains[matchedFingerName] = chain;
                }
            }

            return fingerChains;
        }

        private static void AlignFingerToTarget(List<Transform> fingerChain, Vector3 targetPosition)
        {
            if (fingerChain.Count < 2)
            {
                return;
            }

            // Get the base and tip of the finger
            Transform baseJoint = fingerChain[0];
            Transform tipJoint = fingerChain[^1];

            // Calculate the current direction of the finger
            Vector3 currentDirection = (tipJoint.position - baseJoint.position).normalized;

            // Calculate the desired direction to the target
            Vector3 targetDirection = (targetPosition - baseJoint.position).normalized;

            // Create a rotation to align the current direction with the target direction
            Quaternion rotation = Quaternion.FromToRotation(currentDirection, targetDirection);

            // Check if the rotation angle is too large (more than 60 degrees)
            float rotationAngle = Quaternion.Angle(Quaternion.identity, rotation);
            if (rotationAngle > 60f)
            {
                Debug.LogWarning($"Excessive finger rotation detected: {rotationAngle} degrees. Ignoring rotation.");
                return;
            }

            // Store the initial position of the tip joint
            Vector3 initialTipPosition = tipJoint.position;

            // Apply the rotation to the base joint
            Quaternion originalRotation = baseJoint.rotation;
            baseJoint.rotation = rotation * baseJoint.rotation;

            // Check if the rotation actually improved the distance to the target
            float initialDistance = Vector3.Distance(initialTipPosition, targetPosition);
            float newDistance = Vector3.Distance(tipJoint.position, targetPosition);

            // If the new distance is greater than the initial distance, revert the rotation
            if (newDistance > initialDistance)
            {
                Debug.LogWarning($"Finger rotation increased distance to target from {initialDistance} to {newDistance}. Reverting rotation.");
                baseJoint.rotation = originalRotation;
            }
        }

        private static bool IsChildOfJoint(MSDKUtilityEditorConfig config, int childIndex, int parentIndex)
        {
            if (childIndex == parentIndex)
            {
                return false;
            }

            int currentIndex = childIndex;
            while (currentIndex >= 0 && currentIndex < config.ParentIndices.Length)
            {
                int parent = config.ParentIndices[currentIndex];
                if (parent == parentIndex)
                {
                    return true;
                }

                if (parent == currentIndex || parent < 0)
                {
                    break;
                }

                currentIndex = parent;
            }

            return false;
        }
    }
}
