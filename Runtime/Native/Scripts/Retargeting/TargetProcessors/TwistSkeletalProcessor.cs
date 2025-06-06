// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System;
using Unity.Collections;
using UnityEngine;
using static Meta.XR.Movement.MSDKUtilityHelper;
using static Meta.XR.Movement.MSDKUtility;

namespace Meta.XR.Movement.Retargeting
{
    /// <summary>
    /// The twist constraint for distributing twist on twist joints.
    /// </summary>
    [Serializable]
    public class TwistSkeletalProcessor : TargetProcessor
    {
        [Serializable]
        public struct TwistData
        {
            public TargetJointIndex Index;
            public float Weight;
        }

        [SerializeField]
        private int _sourceIndex;

        [SerializeField]
        private TwistData[] _targetData;

        [SerializeField]
        private Vector3 _twistForwardAxis;

        [SerializeField]
        private Vector3 _twistUpAxis;

        private Quaternion _twistLocalRotationAxisOffset;
        private Vector3[] _segmentEndUpAxis;

        /// <summary>
        /// Initializes the twist processor by calculating the necessary axes and offsets for twist calculations.
        /// </summary>
        /// <param name="retargeter">The character retargeter that owns this processor.</param>
        public override void Initialize(CharacterRetargeter retargeter)
        {
            var handle = retargeter.RetargetingHandle;
            GetSkeletonTPose(handle, SkeletonType.TargetSkeleton, SkeletonTPoseType.UnscaledTPose,
                JointRelativeSpaceType.RootOriginRelativeSpace, out var tPose);
            var sourcePose = tPose[_sourceIndex];
            _segmentEndUpAxis = new Vector3[_targetData.Length];
            for (var i = 0; i < _targetData.Length; i++)
            {
                var targetPose = tPose[_targetData[i].Index];
                _segmentEndUpAxis[i] = InverseTransformVector(sourcePose, TransformVector(targetPose, _twistUpAxis));
            }

            _twistLocalRotationAxisOffset =
                Quaternion.Inverse(Quaternion.LookRotation(_twistForwardAxis, _twistUpAxis));
        }

        /// <summary>
        /// Cleans up resources when the processor is destroyed.
        /// </summary>
        public override void Destroy()
        {
        }

        /// <summary>
        /// Updates the pose by applying twist calculations to distribute rotation between joints.
        /// </summary>
        /// <param name="pose">The pose to be updated with twist calculations.</param>
        public override void UpdatePose(ref NativeArray<NativeTransform> pose)
        {
            if (_weight <= 0.0f)
            {
                return;
            }

            for (var i = 0; i < _targetData.Length; i++)
            {
                var targetData = _targetData[i];
                var segmentTarget = pose[targetData.Index];
                var segmentSource = pose[_sourceIndex];
                var segmentEndUpAxis = _segmentEndUpAxis[i];
                var currentLookAtDir = (segmentSource.Position - segmentTarget.Position) * 2f;
                var lookDirectionDot = Vector3.Dot(currentLookAtDir, TransformVector(segmentSource, segmentEndUpAxis));

                // Don't apply twists if the look directions are zero or the look directions are parallel.
                if (currentLookAtDir.sqrMagnitude <= Mathf.Epsilon ||
                    TransformVector(segmentSource, segmentEndUpAxis).sqrMagnitude <= Mathf.Epsilon ||
                    lookDirectionDot >= 1 - Mathf.Epsilon ||
                    lookDirectionDot <= -1 + Mathf.Epsilon)
                {
                    return;
                }

                // Blend twist between rest rotation and fully twisted using weight.
                segmentTarget.Orientation = Quaternion.Slerp(segmentTarget.Orientation,
                    Quaternion.LookRotation(currentLookAtDir, TransformVector(segmentSource, segmentEndUpAxis)) *
                    _twistLocalRotationAxisOffset, _weight * targetData.Weight);
                pose[targetData.Index] = segmentTarget;
            }
        }

        /// <summary>
        /// Performs any late update processing on the pose after the main update.
        /// </summary>
        /// <param name="currentPose">The current pose.</param>
        /// <param name="targetPose">The target pose to be updated.</param>
        public override void LateUpdatePose(ref NativeArray<NativeTransform> currentPose, ref NativeArray<NativeTransform> targetPose)
        {
        }
    }
}
