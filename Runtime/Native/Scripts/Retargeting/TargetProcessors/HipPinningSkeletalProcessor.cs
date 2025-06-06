// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System;
using Meta.XR.Movement.Retargeting.IK;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
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

            private int _localMidLegIndex;

            public void Initialize(CharacterRetargeter characterRetargeter)
            {
                LegBones = new Transform[LegIndexes.Length];
                for (var i = 0; i < LegIndexes.Length; i++)
                {
                    var index = LegIndexes[i];
                    LegBones[i] = characterRetargeter.JointPairs[index].Joint;
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
                if (TargetHint != null)
                {
                    var rootBone = LegBones[^1];
                    rootBone.localRotation =
                        Quaternion.Inverse(rootBone.parent.localRotation) * TargetHint.localRotation;
                    LegBones[_localMidLegIndex].localPosition = TargetHint.localPosition;
                }

                LegBones[0].localRotation = TargetFoot.localRotation;
                IKUtilities.SolveCCDIK(LegBones, TargetFoot.position, 0.01f, 5);
            }
        }

        [BurstCompile]
        private struct HipPinningJob : IJob
        {
            [ReadOnly]
            public float Weight;

            [ReadOnly]
            public int HipsIndex;

            public Vector3 TargetPosition;

            public NativeArray<MSDKUtility.NativeTransform> TargetPose;

            public void Execute()
            {
                var hips = TargetPose[HipsIndex];
                hips.Position = Vector3.Lerp(hips.Position, TargetPosition, Weight);
                TargetPose[HipsIndex] = hips;
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
                out var rootJointIndex);
            MSDKUtility.GetJointIndexByKnownJointType(handle,
                MSDKUtility.SkeletonType.TargetSkeleton, MSDKUtility.KnownJointType.Hips,
                out _hipsJointIndex);
            MSDKUtility.GetSkeletonTPose(handle,
                MSDKUtility.SkeletonType.TargetSkeleton,
                MSDKUtility.SkeletonTPoseType.UnscaledTPose,
                MSDKUtility.JointRelativeSpaceType.RootOriginRelativeSpace, out var tPose);

            _initialHipRotation = tPose[_hipsJointIndex].Orientation;
            _rootTransform = retargeter.JointPairs[rootJointIndex].Joint;
            _hipsTransform = retargeter.JointPairs[_hipsJointIndex].Joint;
            _leftData.Initialize(retargeter);
            _rightData.Initialize(retargeter);
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

            // Hip pinning.
            UpdateHipPinningObjectRotation(_initialHipRotation, _hipsTransform.rotation);
            var targetHipPinningPosition = _rootTransform.InverseTransformPoint(_targetHips.position);

            // Check if we should trigger hip pinning events
            var currentHipPinningPosition = targetPose[_rootJointIndex].Position +
                                            targetPose[_rootJointIndex].Orientation *
                                            targetPose[_hipsJointIndex].Position;
            switch (_previouslyActive)
            {
                case true when Vector3.Distance(_initialHipPosition, currentHipPinningPosition) > _hipPinningThreshold:
                    OnExitHipPinningArea.Invoke();
                    return;
                case false:
                    _previouslyActive = true;
                    _initialHipPosition = currentHipPinningPosition;
                    OnEnterHipPinningArea.Invoke();
                    break;
            }

            var job = new HipPinningJob
            {
                Weight = _weight,
                HipsIndex = _hipsJointIndex,
                TargetPosition = targetHipPinningPosition,
                TargetPose = targetPose
            };
            job.Schedule().Complete();

            // Solve for legs.
            _leftData.Solve();
            _rightData.Solve();
            for (var i = 0; i < _leftData.LegIndexes.Length; i++)
            {
                var index = _leftData.LegIndexes[i];
                var joint = targetPose[index];
                if (i == _leftData.MidLegIndex)
                {
                    joint.Position = _leftData.TargetHint.localPosition;
                }

                joint.Orientation = _leftData.LegBones[i].localRotation;
                targetPose[index] = joint;
            }

            for (var i = 0; i < _rightData.LegIndexes.Length; i++)
            {
                var index = _rightData.LegIndexes[i];
                var joint = targetPose[index];
                if (i == _rightData.MidLegIndex)
                {
                    joint.Position = _rightData.TargetHint.localPosition;
                }

                joint.Orientation = _rightData.LegBones[i].localRotation;
                targetPose[index] = joint;
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
