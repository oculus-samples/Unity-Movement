// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using Meta.XR.Movement.Retargeting.IK;
using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using static Meta.XR.Movement.MSDKUtility;

namespace Meta.XR.Movement.Retargeting
{
    /// <summary>
    /// Runs CCD on target skeleton's joints. Ideally should have native implementation.
    /// </summary>
    [Serializable]
    public class CCDSkeletalProcessor : TargetProcessor
    {
        /// <summary>
        /// The types of targets available for CCD IK.
        /// </summary>
        public enum TargetType
        {
            Transform,
            TrackedLeftHand,
            TrackedRightHand,
            TrackedHead
        }

        /// <summary>
        /// Container for CCD data related to an IK chain.
        /// </summary>
        [Serializable]
        public struct CCDSkeletalData
        {
            /// <summary>
            /// Chain of IK chain joints.
            /// </summary>
            [SerializeField]
            public TargetJointIndex[] IKChain;

            /// <summary>
            /// Type of target to use for CCD IK.
            /// </summary>
            [SerializeField]
            public TargetType TargetType;

            /// <summary>
            /// Target that the end effector must reach (used when TargetType is Transform).
            /// </summary>
            [SerializeField]
            public Transform Target;

            /// <summary>
            /// Tolerance related to the end effector reaching the target.
            /// It can possibly be 1e-06f.
            /// </summary>
            [SerializeField]
            public float Tolerance;

            /// <summary>
            /// Max iterations used to operate on this IK chain.
            /// It can possibly be 10.
            /// </summary>
            [SerializeField]
            public int MaxIterations;

            /// <summary>
            /// Validates fields on struct and reports any errors.
            /// </summary>
            public void Validate()
            {
                Assert.IsTrue(IKChain is { Length: > 0 });
                if (TargetType == TargetType.Transform)
                {
                    Assert.IsTrue(Target != null, "Target Transform is required when TargetType is Transform");
                }
            }

            /// <summary>
            /// Finds bones related to the target joints indices specified. Initializes other
            /// fields as needed.
            /// </summary>
            /// <param name="characterRetargeter">The <see cref="CharacterRetargeter"/> object that has the
            /// relevant target bones.</param>
            public void Initialize(CharacterRetargeter characterRetargeter)
            {
            }
        }

        /// <summary>
        /// Accessor for skeletal data.
        /// </summary>
        public CCDSkeletalData[] CCDData => _ccdData;

        /// <summary>
        /// Data associated with running CCD.
        /// </summary>
        [SerializeField]
        protected CCDSkeletalData[] _ccdData;

        private CharacterRetargeter _characterRetargeter;

        /// <inheritdoc />
        public override void Initialize(CharacterRetargeter retargeter)
        {
            foreach (var ccdData in _ccdData)
            {
                ccdData.Validate();
                ccdData.Initialize(retargeter);
            }

            _characterRetargeter = retargeter;
        }

        /// <inheritdoc />
        public override void Destroy()
        {
        }

        /// <inheritdoc />
        public override void UpdatePose(ref NativeArray<NativeTransform> pose)
        {
        }

        /// <inheritdoc />
        public override void LateUpdatePose(
            ref NativeArray<NativeTransform> currentPose,
            ref NativeArray<NativeTransform> targetPoseLocal)
        {
            if (_weight <= 0.0f)
            {
                return;
            }

            foreach (var ccdData in _ccdData)
            {
                RunCCDOnSkeletalData(ccdData, ref targetPoseLocal);
            }
        }

        private void RunCCDOnSkeletalData(
            CCDSkeletalData ccdData,
            ref NativeArray<NativeTransform> targetPoseLocal)
        {
            var ikChain = ccdData.IKChain;
            var chainRootIndex = _characterRetargeter.SkeletonRetargeter.TargetParentIndices[ikChain[0]];
            var lastChainIndex = ikChain.Length - 1;
            var endEffectorParentIndex =
                _characterRetargeter.SkeletonRetargeter.TargetParentIndices[ikChain[lastChainIndex]];

            if (chainRootIndex == -1 || endEffectorParentIndex == -1)
            {
                Debug.LogError("Cannot run CCD; invalid parent indices");
                return;
            }

            // Get target position based on target type
            if (!GetTargetPosition(ccdData, out var targetPosition))
            {
                return;
            }

            // Get the current world pose as provided by other processors.
            var targetPose = SkeletonUtilities.ComputeWorldPoses(
                _characterRetargeter.SkeletonRetargeter,
                ref targetPoseLocal,
                _characterRetargeter.transform.position,
                _characterRetargeter.transform.rotation);
            var rootPose = new Pose(targetPose[chainRootIndex].Position, targetPose[chainRootIndex].Orientation);
            var endEffectorTargetIndex = ikChain[lastChainIndex];
            var targetPositionLerped =
                Vector3.Lerp(targetPose[endEffectorTargetIndex].Position, targetPosition, _weight);

            // Build IK chain from end effect up down first item (reversed).
            var ikChainArrayEndFirst = GetIKChainEndFirst(ikChain, ref targetPoseLocal);

            // Run CCD IK solver.
            IKUtilities.SolveCCDIKLocalNativeArray(rootPose, ikChainArrayEndFirst, targetPositionLerped,
                ccdData.Tolerance, ccdData.MaxIterations);

            // Get the final poses, but reverse them since the IK chain is in the opposite order from CCD.
            // When reading from the IK chain always make sure to reference the original indices.
            var endEffectorRotation = targetPose[endEffectorTargetIndex].Orientation;
            for (var i = 0; i < ikChain.Length; i++)
            {
                var currentIndexInTarget = ikChain[i];
                // The chain of poses used for CCD is backwards relative to the IK chain.
                var poseToLerpTo = ikChainArrayEndFirst[lastChainIndex - i];
                var poseToSet = targetPoseLocal[currentIndexInTarget];
                poseToSet.Orientation =
                    Quaternion.Slerp(poseToSet.Orientation, poseToLerpTo.Orientation, _weight);
                targetPoseLocal[currentIndexInTarget] = poseToSet;
            }

            targetPose.Dispose();
            targetPose = SkeletonUtilities.ComputeWorldPoses(
                _characterRetargeter.SkeletonRetargeter,
                ref targetPoseLocal,
                _characterRetargeter.transform.position,
                _characterRetargeter.transform.rotation);

            // Push end effector to target. We need to know what the local position of the target
            // is based on the last recomputed world positions of the IK chain after CCD has
            // modified them.
            var targetLocalPosition = targetPoseLocal[endEffectorTargetIndex].Position;
            var localEndEffectorRotation =
                Quaternion.Inverse(targetPose[endEffectorParentIndex].Orientation) * endEffectorRotation;
            targetPoseLocal[endEffectorTargetIndex] =
                new NativeTransform(localEndEffectorRotation, targetLocalPosition);
            targetPose.Dispose();
        }

        private NativeArray<NativeTransform> GetIKChainEndFirst(TargetJointIndex[] ikChain,
            ref NativeArray<NativeTransform> targetPoseLocal)
        {
            var ikChainArrayEndFirst = new NativeArray<NativeTransform>(ikChain.Length, Allocator.Temp,
                NativeArrayOptions.UninitializedMemory);
            var lastChainIndex = ikChain.Length - 1;
            for (var i = lastChainIndex; i >= 0; i--)
            {
                ikChainArrayEndFirst[lastChainIndex - i] = targetPoseLocal[ikChain[i]];
            }

            return ikChainArrayEndFirst;
        }

        private Vector3 GetTargetRelativeToEffectorParent(int endEffectorParentIndex,
            NativeArray<NativeTransform> targetWorldPose, Vector3 targetPositionWorldLerp, Vector3 rootScale)
        {
            var parentEndEffectorWorld = targetWorldPose[endEffectorParentIndex];
            var targetRelativeToParent = Quaternion.Inverse(parentEndEffectorWorld.Orientation) *
                                         (targetPositionWorldLerp - parentEndEffectorWorld.Position);
            return Vector3.Scale(targetRelativeToParent,
                new Vector3(1f / rootScale.x, 1f / rootScale.y, 1f / rootScale.z));
        }

        private bool GetTargetPosition(CCDSkeletalData ccdData, out Vector3 targetPosition)
        {
            targetPosition = Vector3.zero;

            switch (ccdData.TargetType)
            {
                case TargetType.Transform:
                    if (ccdData.Target == null)
                    {
                        Debug.LogError("Target Transform is null for CCD IK");
                        return false;
                    }

                    targetPosition = ccdData.Target.position;
                    return true;

                case TargetType.TrackedLeftHand:
                    return GetTrackedPosition(SkeletonData.BodyTrackingBoneId.LeftHandWrist, out targetPosition);

                case TargetType.TrackedRightHand:
                    return GetTrackedPosition(SkeletonData.BodyTrackingBoneId.RightHandWrist, out targetPosition);

                case TargetType.TrackedHead:
                    return GetTrackedPosition(SkeletonData.BodyTrackingBoneId.Head, out targetPosition);

                default:
                    Debug.LogError($"Unknown target type: {ccdData.TargetType}");
                    return false;
            }
        }

        private bool GetTrackedPosition(SkeletonData.BodyTrackingBoneId boneId, out Vector3 targetPosition)
        {
            targetPosition = Vector3.zero;
            var currentPose = _characterRetargeter.SkeletonRetargeter.SourcePose;
            var boneIndex = (int)boneId;
            if (boneIndex >= currentPose.Length)
            {
                return false;
            }

            targetPosition = currentPose[boneIndex].Position;
            return true;
        }
    }
}
