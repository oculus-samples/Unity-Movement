// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEditor;
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
        /// Aligns an arm in the target skeleton to match a specific direction.
        /// </summary>
        /// <param name="source">The source configuration.</param>
        /// <param name="target">The target configuration to modify.</param>
        /// <param name="upperArm">The index of the upper arm joint.</param>
        /// <param name="lowerArm">The index of the lower arm joint.</param>
        /// <param name="wrist">The index of the wrist joint.</param>
        /// <param name="direction">The direction to align the arm to.</param>
        /// <param name="upDirection">The up direction for alignment.</param>
        public static void AlignArm(MSDKUtilityEditorConfig source, MSDKUtilityEditorConfig target, int upperArm,
            int lowerArm, int wrist, Vector3 direction, Vector3 upDirection)
        {
            // Rotate arms to align horizontally if not aligned.
            var upperArmDir = (target.ReferencePose[lowerArm].Position - target.ReferencePose[upperArm].Position)
                .normalized;
            var upperArmRot = Quaternion.FromToRotation(upperArmDir, direction);
            target.SkeletonJoints[upperArm].rotation = upperArmRot * target.SkeletonJoints[upperArm].rotation;
            UpdateTPoseData(target);
            var lowerArmDir = (target.SkeletonJoints[wrist].position - target.SkeletonJoints[lowerArm].position)
                .normalized;
            var lowerArmRot = Quaternion.FromToRotation(lowerArmDir, direction);
            target.SkeletonJoints[lowerArm].rotation = lowerArmRot * target.SkeletonJoints[lowerArm].rotation;
            UpdateTPoseData(target);
        }

        /// <summary>
        /// Aligns the root of the target skeleton with the source skeleton.
        /// </summary>
        /// <param name="source">The source configuration.</param>
        /// <param name="target">The target configuration to modify.</param>
        public static void AlignRoot(MSDKUtilityEditorConfig source, MSDKUtilityEditorConfig target)
        {
            var sourceRightWrist = source.ReferencePose[(int)FullBodyTrackingBoneId.RightHandWrist];
            var targetRightWrist = target.ReferencePose[GetKnownJointIndex(target, KnownJointType.RightWrist)];

            // 1. Align Z-axis.
            Undo.RecordObject(target, "Align Arms");
            var zShift = new Vector3(0.0f, 0.0f, sourceRightWrist.Position.z - targetRightWrist.Position.z);
            for (var i = 0; i < target.ReferencePose.Length; i++)
            {
                var joint = target.ReferencePose[i];
                joint.Position += zShift;
                target.ReferencePose[i] = joint;
            }
        }

        /// <summary>
        /// Aligns the wrists of the target skeleton with the source skeleton.
        /// </summary>
        /// <param name="source">The source configuration.</param>
        /// <param name="target">The target configuration to modify.</param>
        public static void AlignWrists(MSDKUtilityEditorConfig source, MSDKUtilityEditorConfig target)
        {
            var prevLeftWristRot = GetTargetTransformFromKnownJoint(target, KnownJointType.LeftWrist).rotation;
            var prevRightWristRot = GetTargetTransformFromKnownJoint(target, KnownJointType.RightWrist).rotation;

            // 1. Stretch arms
            PerformArmScaling(source, target);

            // 2. Match wrists.
            JointMappingUtility.PerformWristMatching(source, target);

            // 3. Align fingers based on wrist to middle.
            foreach (var wristType in new[] { KnownJointType.LeftWrist, KnownJointType.RightWrist })
            {
                var tar = target;
                GetJointIndexByKnownJointType(target.ConfigHandle, SkeletonType.TargetSkeleton, wristType,
                    out var wristIndex);
                GetChildJointIndexes(target.ConfigHandle, SkeletonType.TargetSkeleton, wristIndex,
                    out var childIndexes);
                var middleFingerName = childIndexes.Select(i => tar.JointNames[i])
                    .FirstOrDefault(n => n.ToLower().Contains("middle"));
                var middleFingerIndex = Array.IndexOf(target.JointNames, middleFingerName);
                if (middleFingerIndex == -1)
                {
                    continue;
                }

                var middleFingerJoint = target.SkeletonJoints[middleFingerIndex];
                var sourceBoneId = wristType == KnownJointType.LeftWrist
                    ? FullBodyTrackingBoneId.LeftHandMiddleDistal
                    : FullBodyTrackingBoneId.RightHandMiddleDistal;

                var targetHand = GetTargetTransformFromKnownJoint(target, wristType);
                var sourceMiddleFinger = source.ReferencePose[(int)sourceBoneId];
                var sourceAlignmentVector = targetHand.position - sourceMiddleFinger.Position;
                var targetAlignmentVector = targetHand.position - middleFingerJoint.position;

                var deltaRotation = Quaternion.FromToRotation(targetAlignmentVector, sourceAlignmentVector);
                var prevDist = Vector3.Distance(middleFingerJoint.position, sourceMiddleFinger.Position);
                var wristRot = targetHand.rotation;

                targetHand.rotation = wristRot * deltaRotation;
                var poseDist = Vector3.Distance(middleFingerJoint.position, sourceMiddleFinger.Position);
                var inversePoseDist = poseDist;
                if (prevDist < poseDist)
                {
                    targetHand.rotation = wristRot * Quaternion.Inverse(deltaRotation);
                    inversePoseDist = Vector3.Distance(middleFingerJoint.position, sourceMiddleFinger.Position);
                }

                if (!(prevDist < inversePoseDist) || !(prevDist < poseDist))
                {
                    continue;
                }

                targetHand.rotation = wristType switch
                {
                    KnownJointType.LeftWrist => prevLeftWristRot,
                    KnownJointType.RightWrist => prevRightWristRot,
                    _ => targetHand.rotation
                };
            }

            UpdateTPoseData(target);
        }

        /// <summary>
        /// Translates the hips of the target skeleton to match the source skeleton.
        /// </summary>
        /// <param name="source">The source configuration.</param>
        /// <param name="target">The target configuration to modify.</param>
        public static void TranslateHips(MSDKUtilityEditorConfig source, MSDKUtilityEditorConfig target)
        {
            GetJointIndexByKnownJointType(target.ConfigHandle,
                SkeletonType.TargetSkeleton, KnownJointType.Hips, out var hipsIndex);
            GetJointIndexByKnownJointType(target.ConfigHandle,
                SkeletonType.TargetSkeleton, KnownJointType.Neck, out var neckIndex);

            // Roughly estimate height.
            var height = target.ReferencePose[neckIndex].Position.y - target.ReferencePose[hipsIndex].Position.y;

            // Max translation based on height.
            var maxTranslationPercentage = 0.05f;
            var translationThreshold = height * maxTranslationPercentage;
            var sourceHips =
                source.ReferencePose[(int)FullBodyTrackingBoneId.Hips];
            var targetHips = target.ReferencePose[hipsIndex];

            // We want to ground the character though by ensuring the feet are grounded after the translation,
            // so apply the offset to the body but move the feet to the grounded position.
            var leftFootIndex = GetKnownJointIndex(target, KnownJointType.LeftAnkle);
            var rightFootIndex = GetKnownJointIndex(target, KnownJointType.RightAnkle);
            var targetHipsHeight = targetHips.Position.y;
            var hipsTranslation = sourceHips.Position.y - targetHipsHeight;
            hipsTranslation = Mathf.Clamp(hipsTranslation, -translationThreshold, translationThreshold);
            for (var i = hipsIndex; i < target.ReferencePose.Length; i++)
            {
                var tar = target.ReferencePose[i];
                tar.Position += Vector3.up * hipsTranslation;
                target.ReferencePose[i] = tar;
            }

            var indicesToUndo = new List<int>();
            GetLowestChildJointIndex(target.ConfigHandle, SkeletonType.TargetSkeleton, leftFootIndex,
                out var lowestLeftIndex);
            GetLowestChildJointIndex(target.ConfigHandle, SkeletonType.TargetSkeleton, rightFootIndex,
                out var lowestRightIndex);
            while (lowestLeftIndex != leftFootIndex || lowestLeftIndex == -1)
            {
                indicesToUndo.Add(lowestLeftIndex);
                GetParentJointIndex(target.ConfigHandle, SkeletonType.TargetSkeleton, lowestLeftIndex,
                    out lowestLeftIndex);
            }

            while (lowestRightIndex != rightFootIndex || lowestRightIndex == -1)
            {
                indicesToUndo.Add(lowestRightIndex);
                GetParentJointIndex(target.ConfigHandle, SkeletonType.TargetSkeleton, lowestRightIndex,
                    out lowestRightIndex);
            }

            foreach (var indice in indicesToUndo)
            {
                var pose = target.ReferencePose[indice];
                pose.Position -= Vector3.up * hipsTranslation;
                target.ReferencePose[indice] = pose;
            }
        }

        /// <summary>
        /// Sets the target skeleton to a T-pose configuration.
        /// </summary>
        /// <param name="source">The source configuration.</param>
        /// <param name="target">The target configuration to modify.</param>
        public static void SetToTPose(MSDKUtilityEditorConfig source, MSDKUtilityEditorConfig target)
        {
            var leftWrist = GetKnownJointIndex(target, KnownJointType.LeftWrist);
            var rightWrist = GetKnownJointIndex(target, KnownJointType.RightWrist);
            GetParentJointIndex(target.ConfigHandle, SkeletonType.TargetSkeleton, leftWrist, out var leftLowerArm);
            GetParentJointIndex(target.ConfigHandle, SkeletonType.TargetSkeleton, leftLowerArm, out var leftUpperArm);
            GetParentJointIndex(target.ConfigHandle, SkeletonType.TargetSkeleton, rightWrist, out var rightLowerArm);
            GetParentJointIndex(target.ConfigHandle, SkeletonType.TargetSkeleton, rightLowerArm, out var rightUpperArm);

            var leftShoulderRotation =
                GetTargetTPoseFromKnownJoint(target, KnownJointType.LeftUpperArm).Orientation.eulerAngles;
            var rightShoulderRotation =
                GetTargetTPoseFromKnownJoint(target, KnownJointType.RightUpperArm).Orientation.eulerAngles;
            var isMirrored = Vector3.Distance(leftShoulderRotation, rightShoulderRotation) > 170f;

            AlignArm(source, target, leftUpperArm, leftLowerArm, leftWrist, Vector3.left, Vector3.up);
            AlignArm(source, target, rightUpperArm, rightLowerArm, rightWrist, Vector3.right,
                isMirrored ? Vector3.down : Vector3.up);
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
            ConvertJointPose(target.ConfigHandle, SkeletonType.TargetSkeleton,
                JointRelativeSpaceType.RootOriginRelativeSpace,
                JointRelativeSpaceType.LocalSpaceScaled, targetTPose, out var scaledTPose);
            GetJointIndexByKnownJointType(target.ConfigHandle, SkeletonType.TargetSkeleton, KnownJointType.Root,
                out var rootJointIndex);
            utilityConfig.RootScale = utilityConfig.Step != MSDKUtilityEditorConfig.EditorStep.Configuration ? scaledTPose[rootJointIndex].Scale : Vector3.one;
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
        /// Gets the Unity Transform component for a known joint type in the target skeleton.
        /// </summary>
        /// <param name="target">The target configuration.</param>
        /// <param name="knownJointType">The known joint type to find.</param>
        /// <returns>The Unity Transform of the joint.</returns>
        public static Transform GetTargetTransformFromKnownJoint(MSDKUtilityEditorConfig target,
            KnownJointType knownJointType)
        {
            GetJointIndexByKnownJointType(target.ConfigHandle,
                SkeletonType.TargetSkeleton, knownJointType, out var targetJointIndex);
            if (targetJointIndex == -1)
            {
                return null;
            }

            return target.SkeletonJoints[targetJointIndex];
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
    }
}
