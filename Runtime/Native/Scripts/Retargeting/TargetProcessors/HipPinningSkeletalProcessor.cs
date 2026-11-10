// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System;
using Meta.XR.Movement.Retargeting.IK;
using Unity.Collections;
using UnityEngine;

namespace Meta.XR.Movement.Retargeting
{
    /// <summary>
    /// The hip pinning constraint, which pins a retargeted character to a position.
    /// </summary>
    [Serializable]
    public class HipPinningSkeletalProcessor : TargetProcessor
    {
        [Serializable]
        public struct HipPinningData
        {
            [SerializeField]
            public Transform TargetFoot;

            [SerializeField]
            public Transform TargetHint;

            [SerializeField]
            public TargetJointIndex[] LegIndexes;

            [SerializeField]
            public TargetJointIndex MidLegIndex;

            [HideInInspector]
            public Transform[] LegBones;

            [HideInInspector]
            public float[] LegLengths;

            private int _localMidLegIndex;

            public void Initialize(CharacterRetargeter characterRetargeter,
                NativeArray<MSDKUtility.NativeTransform> localTPose)
            {
                LegBones = new Transform[LegIndexes.Length];
                LegLengths = new float[LegIndexes.Length];
                for (var i = 0; i < LegIndexes.Length; i++)
                {
                    var index = LegIndexes[i];
                    LegBones[i] = characterRetargeter.JointPairs[index].Joint;
                    LegLengths[i] = localTPose[index].Position.magnitude;
                }

                for (var i = 0; i < LegIndexes.Length; i++)
                {
                    if (LegIndexes[i] != MidLegIndex)
                    {
                        continue;
                    }

                    _localMidLegIndex = i;
                    break;
                }
            }

            public void Solve()
            {
                for (var i = 0; i < LegBones.Length; i++)
                {
                    LegBones[i].localPosition = LegBones[i].localPosition.normalized * LegLengths[i];
                }

                if (TargetHint != null)
                {
                    // Rotate the parent bone so the knee reaches the hint naturally
                    var kneeBone = LegBones[_localMidLegIndex];
                    var parentBone = LegBones[_localMidLegIndex + 1];

                    // Calculate the rotation needed to align current knee position with hint
                    var fromVector = (kneeBone.position - parentBone.position).normalized;
                    var toVector = (TargetHint.position - parentBone.position).normalized;

                    if (Vector3.Dot(fromVector, toVector) < 0.999f) // Only rotate if vectors are not already aligned
                    {
                        var rotation = Quaternion.FromToRotation(fromVector, toVector);
                        parentBone.rotation = rotation * parentBone.rotation;
                    }
                }

                LegBones[0].localRotation = TargetFoot.localRotation;
                // The solve warm-starts from last frame's leg pose and stops as soon as the foot is within
                // tolerance, so the foot lands somewhere different inside that radius every frame. Keep it tight.
                IKUtilities.SolveCCDIK(LegBones, TargetFoot.position, 0.001f, 10);
            }
        }

        public Action OnEnterHipPinningArea;

        public Action OnExitHipPinningArea;

        [SerializeField]
        private float _hipPinningThreshold = 0.05f;

        [SerializeField]
        private GameObject _hipPinningObject;

        [SerializeField]
        private GameObject _hipPinningTargetParent;

        [SerializeField]
        private Transform _targetHips;

        [SerializeField]
        private HipPinningData _leftData = new();

        [SerializeField]
        private HipPinningData _rightData = new();

        private int _hipsJointIndex;
        private int _rootJointIndex;
        private bool _previouslyActive;
        private CharacterRetargeter _retargeter;
        private Vector3 _initialHipPosition;
        private Quaternion _initialHipRotation;
        private Transform _hipsTransform;
        private Transform _rootTransform;

        /// <summary>
        /// Shows the hip pinning object by setting its scale to Vector3.one.
        /// </summary>
        public void ShowHipPinningObject()
        {
            _hipPinningObject.transform.localScale = Vector3.one;
        }

        /// <summary>
        /// Hides the hip pinning object by setting its scale to Vector3.zero.
        /// </summary>
        public void HideHipPinningObject()
        {
            _hipPinningObject.transform.localScale = Vector3.zero;
        }

        /// <summary>
        /// Calibrates the position of the hip pinning object based on the current hips position and the provided mask.
        /// </summary>
        /// <param name="mask">A Vector3 mask where components set to 0 will keep their current value, and non-zero components will be updated with the current hips position.</param>
        public void CalibrateHipPinningObjectPosition(Vector3 mask)
        {
            var targetPosition = Vector3.Scale(_hipsTransform.position, mask);
            targetPosition.x = Mathf.Approximately(mask.x, 0.0f)
                ? _hipPinningObject.transform.localPosition.x
                : targetPosition.x;
            targetPosition.y = Mathf.Approximately(mask.y, 0.0f)
                ? _hipPinningObject.transform.localPosition.y
                : targetPosition.y;
            targetPosition.z = Mathf.Approximately(mask.z, 0.0f)
                ? _hipPinningObject.transform.localPosition.z
                : targetPosition.z;
            _hipPinningObject.transform.localPosition = targetPosition;
        }

        /// <inheritdoc />
        public override void Initialize(CharacterRetargeter retargeter)
        {
            var handle = retargeter.RetargetingHandle;
            MSDKUtility.GetJointIndexByKnownJointType(retargeter.RetargetingHandle,
                MSDKUtility.SkeletonType.TargetSkeleton, MSDKUtility.KnownJointType.Root,
                out _rootJointIndex);
            MSDKUtility.GetJointIndexByKnownJointType(handle,
                MSDKUtility.SkeletonType.TargetSkeleton, MSDKUtility.KnownJointType.Hips,
                out _hipsJointIndex);
            MSDKUtility.GetSkeletonTPose(handle,
                MSDKUtility.SkeletonType.TargetSkeleton,
                MSDKUtility.SkeletonTPoseType.UnscaledTPose,
                MSDKUtility.JointRelativeSpaceType.RootOriginRelativeSpace, out var tPose);
            MSDKUtility.GetSkeletonTPose(handle,
                MSDKUtility.SkeletonType.TargetSkeleton,
                MSDKUtility.SkeletonTPoseType.UnscaledTPose,
                MSDKUtility.JointRelativeSpaceType.LocalSpace, out var localTPose);

            _retargeter = retargeter;
            _initialHipRotation = tPose[_hipsJointIndex].Orientation;
            _rootTransform = retargeter.JointPairs[_rootJointIndex].Joint;
            _hipsTransform = retargeter.JointPairs[_hipsJointIndex].Joint;
            _leftData.Initialize(retargeter, localTPose);
            _rightData.Initialize(retargeter, localTPose);
        }

        /// <inheritdoc />
        public override void Destroy()
        {
        }

        /// <inheritdoc />
        public override void UpdatePose(ref NativeArray<MSDKUtility.NativeTransform> pose)
        {
        }

        /// <summary>
        /// Blend the pose based on the indices together in late update.
        /// </summary>
        /// <param name="currentPose">The current pose.</param>
        /// <param name="targetPose">The target pose to be blended.</param>
        public override void LateUpdatePose(ref NativeArray<MSDKUtility.NativeTransform> currentPose,
            ref NativeArray<MSDKUtility.NativeTransform> targetPose)
        {
            if (_weight <= 0.0f)
            {
                _previouslyActive = false;
                return;
            }

            // The retargeter applies targetPose to the joint transforms only after the processors run, so they still
            // hold last frame's pose. Sync them to this frame before anything below reads them.
            ApplyPoseToTransform(targetPose, _rootJointIndex, _rootTransform);
            ApplyPoseToTransform(targetPose, _hipsJointIndex, _hipsTransform);

            UpdateHipPinningObjectRotation(_initialHipRotation, _hipsTransform.rotation);
            var targetHipPinningPosition = _rootTransform.InverseTransformPoint(_targetHips.position);

            // Fire the enter/exit events, and stop once the hips have left the hip pinning area.
            var currentHipPinningPosition = targetPose[_rootJointIndex].Position +
                                            targetPose[_rootJointIndex].Orientation *
                                            targetPose[_hipsJointIndex].Position;
            if (!_previouslyActive)
            {
                _previouslyActive = true;
                _initialHipPosition = currentHipPinningPosition;
                OnEnterHipPinningArea.Invoke();
            }
            else if (Vector3.Distance(_initialHipPosition, currentHipPinningPosition) > _hipPinningThreshold)
            {
                OnExitHipPinningArea.Invoke();
                return;
            }

            // Pin the hips.
            var hips = targetPose[_hipsJointIndex];
            hips.Position = Vector3.Lerp(hips.Position, targetHipPinningPosition, _weight);
            targetPose[_hipsJointIndex] = hips;

            // The leg IK chains hang off the hips transform, so it must hold the pinned hips before solving.
            ApplyPoseToTransform(targetPose, _hipsJointIndex, _hipsTransform);

            SolveLeg(ref _leftData, targetPose);
            SolveLeg(ref _rightData, targetPose);
        }

        private void ApplyPoseToTransform(NativeArray<MSDKUtility.NativeTransform> pose, int jointIndex,
            Transform joint)
        {
            // Match the retargeter's ApplyPose, which leaves joints it does not drive untouched.
            var mappedJointMask = _retargeter.SkeletonRetargeter._mappedJointMask;
            if (mappedJointMask.IsCreated && mappedJointMask[jointIndex] == 0)
            {
                return;
            }

            var jointPose = pose[jointIndex];
            joint.SetLocalPositionAndRotation(jointPose.Position, jointPose.Orientation);
        }

        private static void SolveLeg(ref HipPinningData leg, NativeArray<MSDKUtility.NativeTransform> pose)
        {
            leg.Solve();
            for (var i = 0; i < leg.LegIndexes.Length; i++)
            {
                var index = leg.LegIndexes[i];
                var joint = pose[index];
                joint.Orientation = leg.LegBones[i].localRotation;
                joint.Position = leg.LegBones[i].localPosition;
                pose[index] = joint;
            }
        }

        private void UpdateHipPinningObjectRotation(Quaternion initialHipRotation, Quaternion currentHipRotation)
        {
            // Extract the y-axis rotation difference (around up vector)
            var initialForward = initialHipRotation * Vector3.forward;
            var currentForward = currentHipRotation * Vector3.forward;

            // Project onto horizontal plane
            initialForward.y = 0;
            currentForward.y = 0;

            initialForward.Normalize();
            currentForward.Normalize();

            // Calculate angle between initial and current
            var angle = Vector3.SignedAngle(initialForward, currentForward, Vector3.up);
            _hipPinningTargetParent.transform.localRotation = Quaternion.AngleAxis(angle, Vector3.up);
        }
    }
}
