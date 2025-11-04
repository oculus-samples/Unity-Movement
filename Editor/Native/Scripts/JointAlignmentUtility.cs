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
        /// Aligns wrist joints between source and target skeletons.
        /// First rotates the upper arm joints to get wrists as close as possible,
        /// then snaps the wrist positions to the exact target positions.
        /// </summary>
        /// <param name="config">The configuration.</param>
        public static void PerformWristMatching(MSDKUtilityEditorConfig config)
        {
            // Get source wrist positions
            GetJointIndexByKnownJointType(config.ConfigHandle, SkeletonType.SourceSkeleton,
                KnownJointType.RightWrist, out var rightHandIndex);
            GetJointIndexByKnownJointType(config.ConfigHandle, SkeletonType.SourceSkeleton,
                KnownJointType.LeftWrist, out var leftHandIndex);
            var isMinTPose = config.Step == MSDKUtilityEditorConfig.EditorStep.MinTPose;
            var sourceRightHand = isMinTPose
                ? config.SourceSkeletonData.MinTPoseArray[rightHandIndex]
                : config.SourceSkeletonData.MaxTPoseArray[rightHandIndex];
            var sourceLeftHand = isMinTPose
                ? config.SourceSkeletonData.MinTPoseArray[leftHandIndex]
                : config.SourceSkeletonData.MaxTPoseArray[leftHandIndex];
            var leftArmIndex = GetKnownJointIndex(config, KnownJointType.LeftUpperArm);
            var rightArmIndex = GetKnownJointIndex(config, KnownJointType.RightUpperArm);

            // Step 1: Rotate upper arms to get wrists as close as possible to target positions
            var leftUpperArm = config.SkeletonJoints[leftArmIndex];
            var leftHand = config.SkeletonJoints[GetKnownJointIndex(config, KnownJointType.LeftWrist)];

            // Record undo operations for all transforms that will be modified
            UnityEditor.Undo.RecordObject(leftUpperArm, "Align Wrists");
            UnityEditor.Undo.RecordObject(leftHand, "Align Wrists");

            leftUpperArm.rotation = GetBestRotation(leftUpperArm, leftHand, sourceLeftHand.Position);

            var rightUpperArm = config.SkeletonJoints[rightArmIndex];
            var rightHand = config.SkeletonJoints[GetKnownJointIndex(config, KnownJointType.RightWrist)];

            // Record undo operations for right side transforms
            UnityEditor.Undo.RecordObject(rightUpperArm, "Align Wrists");
            UnityEditor.Undo.RecordObject(rightHand, "Align Wrists");

            rightUpperArm.rotation = GetBestRotation(rightUpperArm, rightHand, sourceRightHand.Position);

            // Step 2: Snap wrist positions to exact target positions
            leftHand.position = sourceLeftHand.Position;
            rightHand.position = sourceRightHand.Position;

            UpdateTPoseData(config);
        }

        /// <summary>
        /// Performs automatic alignment between source and target skeletons.
        /// </summary>
        /// <param name="win">The editor window.</param>
        public static void AutoAlignment(MSDKUtilityEditorWindow win)
        {
            win.Config.SkeletonTPoseType = win.Step switch
            {
                MSDKUtilityEditorConfig.EditorStep.MinTPose => SkeletonTPoseType.MinTPose,
                MSDKUtilityEditorConfig.EditorStep.MaxTPose => SkeletonTPoseType.MaxTPose,
                _ => win.Config.SkeletonTPoseType
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
        /// <param name="config">The target configuration.</param>
        public static void LoadScale(MSDKUtilityEditorConfig config)
        {
            var targetTPose = new NativeArray<NativeTransform>(config.CurrentPose.Length, Allocator.Temp);
            targetTPose.CopyFrom(config.CurrentPose);
            ConvertJointPose(config.ConfigHandle,
                SkeletonType.TargetSkeleton,
                JointRelativeSpaceType.RootOriginRelativeSpace,
                JointRelativeSpaceType.LocalSpaceScaled,
                targetTPose,
                out var scaledTPose);
            GetJointIndexByKnownJointType(config.ConfigHandle, SkeletonType.TargetSkeleton, KnownJointType.Root,
                out var rootJointIndex);

            if (config.Step != MSDKUtilityEditorConfig.EditorStep.Configuration)
            {
                var calculatedScale = scaledTPose[rootJointIndex].Scale;

                // Clamp scale components to the character retargeter range
                config.RootScale = new Vector3(
                    Mathf.Clamp(calculatedScale.x, MSDKUtilityEditorPreviewer.MinScale,
                        MSDKUtilityEditorPreviewer.MaxScale),
                    Mathf.Clamp(calculatedScale.y, MSDKUtilityEditorPreviewer.MinScale,
                        MSDKUtilityEditorPreviewer.MaxScale),
                    Mathf.Clamp(calculatedScale.z, MSDKUtilityEditorPreviewer.MinScale,
                        MSDKUtilityEditorPreviewer.MaxScale)
                );
            }
            else
            {
                config.RootScale = Vector3.one;
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
                    target.CurrentPose[i] = NativeTransform.Identity();
                }
                else
                {
                    target.CurrentPose[i] = new NativeTransform
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
        /// <param name="config">The configuration.</param>
        public static void PerformArmScaling(MSDKUtilityEditorConfig config)
        {
            // Scale arms - stretch/squash for aligning X-axis.
            GetJointIndexByKnownJointType(config.ConfigHandle, SkeletonType.SourceSkeleton,
                KnownJointType.RightWrist, out var rightHandIndex);
            GetJointIndexByKnownJointType(config.ConfigHandle, SkeletonType.SourceSkeleton,
                KnownJointType.LeftWrist, out var leftHandIndex);
            var isMinTPose = config.Step == MSDKUtilityEditorConfig.EditorStep.MinTPose;
            var sourceRightHand = isMinTPose
                ? config.SourceSkeletonData.MinTPoseArray[rightHandIndex]
                : config.SourceSkeletonData.MaxTPoseArray[rightHandIndex];
            var sourceLeftHand = isMinTPose
                ? config.SourceSkeletonData.MinTPoseArray[leftHandIndex]
                : config.SourceSkeletonData.MaxTPoseArray[leftHandIndex];
            var targetRightHand = GetTargetTPoseFromKnownJoint(config, KnownJointType.RightWrist);
            var targetLeftHand = GetTargetTPoseFromKnownJoint(config, KnownJointType.LeftWrist);

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
            GetJointIndexByKnownJointType(config.ConfigHandle, SkeletonType.TargetSkeleton,
                KnownJointType.RightUpperArm,
                out var rightArmIndex);
            GetJointIndexByKnownJointType(config.ConfigHandle, SkeletonType.TargetSkeleton, KnownJointType.LeftUpperArm,
                out var leftArmIndex);
            GetParentJointIndex(config.ConfigHandle, SkeletonType.TargetSkeleton, rightArmIndex, out var rightIndex);
            GetParentJointIndex(config.ConfigHandle, SkeletonType.TargetSkeleton, leftArmIndex, out var leftIndex);
            ApplyScalingToJointIndexAndChildren(config, rightIndex, xScaleRatioRightWrist, true);
            ApplyScalingToJointIndexAndChildren(config, leftIndex, xScaleRatioLeftWrist, true);
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
                var joint = target.CurrentPose[jointIndex];
                joint.Position.x *= scaleFactor;
                target.CurrentPose[jointIndex] = joint;
            }

            // Recursively apply the scaling to the children of the current joint.
            for (var i = 0; i < target.TargetSkeletonData.ParentIndices.Length; i++)
            {
                if (target.TargetSkeletonData.ParentIndices[i] == jointIndex)
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
            return target.CurrentPose[GetKnownJointIndex(target, knownJointType)];
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
            GetJointIndexByKnownJointType(target.ConfigHandle, SkeletonType.SourceSkeleton,
                KnownJointType.RightWrist, out var rightHandIndex);
            var sourceRightHand = sourceTPose[rightHandIndex];
            var targetRightHand =
                useCurrentPose ? target.CurrentPose[rightHandJointIndex] : targetTPose[rightHandJointIndex];

            // Calculate scale ratio using Y position (height-based scaling)
            var yScaleRatio = sourceRightHand.Position.y /
                              (targetRightHand.Position.y > 0.0f ? targetRightHand.Position.y : 1.0f);

            // Clamp the scale ratio to prevent extreme scaling
            return Mathf.Clamp(yScaleRatio, MSDKUtilityEditorPreviewer.MinScale, MSDKUtilityEditorPreviewer.MaxScale);
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
    }
}
