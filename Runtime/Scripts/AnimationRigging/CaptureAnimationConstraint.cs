// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction;
using System;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// Interface for capture animation data.
    /// </summary>
    public interface ICaptureAnimationData
    {
        /// <summary>
        /// The animator used by the constraint.
        /// </summary>
        public Animator ConstraintAnimator { get; }

        /// <summary>
        /// The target animator layer to capture animations on.
        /// </summary>
        public int TargetAnimatorLayer { get; }

        /// <summary>
        /// The normalized time from which the reference pose should be captured from.
        /// </summary>
        public float ReferencePoseTime { get; }

        /// <summary>
        /// The bone data for the reference pose.
        /// </summary>
        public CaptureAnimationData.PoseBone[] ReferencePose { get; }

        /// <summary>
        /// The bone data for the current pose.
        /// </summary>
        public CaptureAnimationData.PoseBone[] CurrentPose { get; }

        /// <summary>
        /// The current animator state info hash.
        /// </summary>
        public int CurrentAnimatorStateInfoHash { get; set; }
    }

    /// <summary>
    /// Data to store captured bone data from the current animation.
    /// </summary>
    [Serializable]
    public struct CaptureAnimationData : IAnimationJobData, ICaptureAnimationData
    {
        /// <summary>
        /// The bone information for a bone in a pose.
        /// </summary>
        [Serializable]
        public class PoseBone
        {
            /// <summary>
            /// The bone in the pose.
            /// </summary>
            public HumanBodyBones Bone = HumanBodyBones.LastBone;

            /// <summary>
            /// The local position of the bone.
            /// </summary>
            public Vector3 LocalPosition = Vector3.zero;

            /// <summary>
            /// The local rotation of the bone.
            /// </summary>
            public Quaternion LocalRotation = Quaternion.identity;
        }

        /// <inheritdoc />
        Animator ICaptureAnimationData.ConstraintAnimator => _animator;

        /// <inheritdoc />
        int ICaptureAnimationData.TargetAnimatorLayer => _targetAnimatorLayer;

        /// <inheritdoc />
        float ICaptureAnimationData.ReferencePoseTime => _referencePoseTime;

        /// <inheritdoc />
        PoseBone[] ICaptureAnimationData.ReferencePose => _referencePose;

        /// <inheritdoc />
        PoseBone[] ICaptureAnimationData.CurrentPose => _currentPose;

        /// <inheritdoc />
        int ICaptureAnimationData.CurrentAnimatorStateInfoHash
        {
            get => _currentAnimatorStateInfoHash;
            set => _currentAnimatorStateInfoHash = value;
        }

        /// <inheritdoc cref="ICaptureAnimationData.ConstraintAnimator" />
        [SerializeField]
        [Tooltip(CaptureAnimationDataTooltips.ConstraintAnimator)]
        private Animator _animator;

        /// <inheritdoc cref="ICaptureAnimationData.TargetAnimatorLayer" />
        [SyncSceneToStream, SerializeField]
        [Tooltip(CaptureAnimationDataTooltips.TargetAnimatorLayer)]
        private int _targetAnimatorLayer;

        /// <inheritdoc cref="ICaptureAnimationData.ReferencePoseTime" />
        [SyncSceneToStream, SerializeField, Range(0.0f, 1.0f)]
        [Tooltip(CaptureAnimationDataTooltips.ReferencePoseTime)]
        private float _referencePoseTime;

        /// <inheritdoc cref="ICaptureAnimationData.ReferencePose" />
        [SerializeField]
        [Tooltip(CaptureAnimationDataTooltips.ReferencePose)]
        private PoseBone[] _referencePose;

        /// <inheritdoc cref="ICaptureAnimationData.CurrentPose" />
        [SerializeField]
        [Tooltip(CaptureAnimationDataTooltips.CurrentPose)]
        private PoseBone[] _currentPose;

        private int _currentAnimatorStateInfoHash;

        /// <summary>
        /// Assign the animator.
        /// </summary>
        /// <param name="animator">The animator to be assigned.</param>
        public void AssignAnimator(Animator animator)
        {
            _animator = animator;
        }

        /// <summary>
        /// Setup the reference and current pose arrays.
        /// </summary>
        public void SetupPoseArrays()
        {
            _referencePose = new PoseBone[(int)HumanBodyBones.LastBone];
            _currentPose = new PoseBone[(int)HumanBodyBones.LastBone];

            for (HumanBodyBones i = HumanBodyBones.Hips; i < HumanBodyBones.LastBone; i++)
            {
                _referencePose[(int)i] = new PoseBone
                {
                    Bone = i,
                    LocalPosition = Vector3.zero,
                    LocalRotation = Quaternion.identity
                };
            }
            for (HumanBodyBones i = HumanBodyBones.Hips; i < HumanBodyBones.LastBone; i++)
            {
                _currentPose[(int)i] = new PoseBone
                {
                    Bone = i,
                    LocalPosition = Vector3.zero,
                    LocalRotation = Quaternion.identity
                };
            }
        }

        /// <summary>
        /// Return the position delta for a bone between the reference and current pose.
        /// </summary>
        /// <param name="humanBodyBones">The bone to check.</param>
        /// <returns>The position delta for a bone between the reference and current pose.</returns>
        public Vector3 GetBonePositionDelta(HumanBodyBones humanBodyBones)
        {
            var referencePoseBone = _referencePose[(int)humanBodyBones];
            var currentPoseBone = _currentPose[(int)humanBodyBones];
            return currentPoseBone.LocalPosition -
                   referencePoseBone.LocalPosition;
        }

        /// <summary>
        /// Return the rotation delta for a bone between the reference and current pose.
        /// </summary>
        /// <param name="humanBodyBones">The bone to check.</param>
        /// <returns>The rotation delta for a bone between the reference and current pose.</returns>
        public Quaternion GetBoneRotationDelta(HumanBodyBones humanBodyBones)
        {
            var referencePoseBone = _referencePose[(int)humanBodyBones];
            var currentPoseBone = _currentPose[(int)humanBodyBones];
            return Quaternion.Inverse(referencePoseBone.LocalRotation) *
                   currentPoseBone.LocalRotation;
        }

        bool IAnimationJobData.IsValid()
        {
            if (_animator == null || _referencePose == null || _currentPose == null)
            {
                return false;
            }
            return true;
        }

        void IAnimationJobData.SetDefaultValues()
        {
            _referencePose = null;
            _currentPose = null;
            _referencePoseTime = 0f;
        }
    }

    /// <summary>
    /// Capture animation constraint. Captures the current animator's reference pose and current pose,
    /// to be used to blend animation playback and tracking in another animation job.
    /// </summary>
    [DisallowMultipleComponent, AddComponentMenu("Movement Animation Rigging/Capture Animation Constraint")]
    public class CaptureAnimationConstraint : RigConstraint<
            CaptureAnimationJob,
            CaptureAnimationData,
            CaptureAnimationJobBinder<CaptureAnimationData>>,
            IOVRSkeletonConstraint
    {
        /// <inheritdoc />
        public void RegenerateData()
        {
        }
    }
}
