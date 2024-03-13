// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// Interface for copy pose data.
    /// </summary>
    public interface ICopyPoseData
    {
        /// <summary>
        /// True if the pose being copied is the original pose. If false, the copied pose
        /// is assumed to be the final pose.
        /// </summary>
        public bool CopyPoseToOriginal { get; }

        /// <summary>
        /// The retargeting layer component.
        /// </summary>
        public RetargetingLayer RetargetingLayerComp { get; }

        /// <summary>
        /// The array of humanoid animator bones.
        /// </summary>
        public Transform[] AnimatorBones { get; }
    }

    /// <summary>
    /// Copy pose data used by the copy pose job.
    /// Implements the copy pose data interface.
    /// </summary>
    [System.Serializable]
    public struct CopyPoseData : IAnimationJobData, ICopyPoseData
    {
        /// <inheritdoc />
        public RetargetingLayer RetargetingLayerComp
        {
            get => _retargetingLayer;
            set => _retargetingLayer = value;
        }

        /// <inheritdoc />
        public bool CopyPoseToOriginal
        {
            get => _copyPoseToOriginal;
            set => _copyPoseToOriginal = value;
        }

        /// <inheritdoc />
        public Transform[] AnimatorBones => _animatorBones;

        /// <inheritdoc cref="ICopyPoseData.RetargetingLayerComp"/>
        [SerializeField]
        [Tooltip(CopyPoseDataTooltips.RetargetingLayer)]
        private RetargetingLayer _retargetingLayer;

        /// <inheritdoc cref="ICopyPoseData.CopyPoseToOriginal"/>
        [SerializeField]
        [Tooltip(CopyPoseDataTooltips.CopyPoseToOriginal)]
        private bool _copyPoseToOriginal;

        [SyncSceneToStream]
        private Transform[] _animatorBones;

        /// <summary>
        /// Caches the array of humanoid animator bones to be copied.
        /// </summary>
        public void Setup()
        {
            var animator = _retargetingLayer.GetComponent<Animator>();
            var animatorBones = new List<Transform>();
            for (var i = HumanBodyBones.Hips; i < HumanBodyBones.LastBone; i++)
            {
                animatorBones.Add(animator.GetBoneTransform(i));
            }
            _animatorBones = animatorBones.ToArray();
        }

        bool IAnimationJobData.IsValid()
        {
            if (_retargetingLayer == null)
            {
                return false;
            }
            return true;
        }

        void IAnimationJobData.SetDefaultValues()
        {
            _retargetingLayer = null;
            _copyPoseToOriginal = false;
            _animatorBones = null;
        }
    }

    /// <summary>
    /// The CopyPose constraint, used to copy the current humanoid animator pose information to be used
    /// when correcting positions in RetargetingLayer.
    /// </summary>
    [DisallowMultipleComponent, AddComponentMenu("Movement Animation Rigging/Copy Pose Constraint")]
    public class CopyPoseConstraint : RigConstraint<
        CopyPoseJob,
        CopyPoseData,
        CopyPoseJobBinder<CopyPoseData>>,
        IOVRSkeletonConstraint
    {
        private void Start()
        {
            data.Setup();
            gameObject.SetActive(true);
        }

        /// <inheritdoc />
        public void RegenerateData()
        {
            data.Setup();
            gameObject.SetActive(true);
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            if (gameObject.activeInHierarchy && !Application.isPlaying)
            {
                Debug.LogWarning($"{name} should be disabled initially; it enables itself when ready.");
            }
        }
    }
}
