// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static OVRUnityHumanoidSkeletonRetargeter;

namespace Oculus.Movement.AnimationRigging
{
    [System.Serializable]
    /// <summary>
    /// Update target transforms with bone data.
    /// </summary>
    public class ExternalBoneTargets
    {
        /// <summary>
        /// A retargeted bone target.
        /// </summary>
        [System.Serializable]
        public class BoneTarget
        {
            /// <summary>
            /// The OVRSkeleton.BoneId that must be tracked.
            /// </summary>
            [Tooltip(ExternalBoneTargetsTooltips.BoneTargetTooltips.BoneId)]
            public OVRSkeleton.BoneId BoneId { get; set; }

            /// <summary>
            /// The human body bone representation of this bone.
            /// </summary>
            [Tooltip(ExternalBoneTargetsTooltips.BoneTargetTooltips.HumanBodyBone)]
            public HumanBodyBones HumanBodyBone;

            /// <summary>
            /// The target transform to update with body tracking bone data.
            /// </summary>
            [Tooltip(ExternalBoneTargetsTooltips.BoneTargetTooltips.Target)]
            public Transform Target;
        }

        /// <summary>
        /// The array of bone targets.
        /// </summary>
        [SerializeField]
        [Tooltip(ExternalBoneTargetsTooltips.BoneTargets)]
        protected BoneTarget[] _boneTargets;
        /// <inheritdoc cref="_boneTargets"/>
        public BoneTarget[] BoneTargetsArray
        {
            get => _boneTargets;
            set => _boneTargets = value;
        }

        /// <summary>
        /// Is it full body (or not).
        /// </summary>
        [SerializeField]
        [Tooltip(ExternalBoneTargetsTooltips.IsFullBody)]
        protected bool _fullBody = false;
        /// <inheritdoc cref="_fullBody"/>
        public bool FullBody
        {
            get => _fullBody;
            set => _fullBody = value;
        }

        /// <summary>
        /// Enables or disables functionality.
        /// </summary>
        [SerializeField]
        [Tooltip(ExternalBoneTargetsTooltips.Enabled)]
        protected bool _enabled = false;
        /// <inheritdoc cref="_enabled"/>
        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        private bool _initialized = false;

        /// <summary>
        /// Correlate HumanBodyBones to OVRSkeleton.BoneId.
        /// </summary>
        private void Initialize()
        {
            if (_initialized)
            {
                return;
            }
            var humanBodyBoneToOVRBoneId = _fullBody ?
                OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone.ToDictionary(
                    x => x.Value, x => x.Key) :
                OVRHumanBodyBonesMappings.BoneIdToHumanBodyBone.ToDictionary(
                    x => x.Value, x => x.Key);
            foreach (var retargetedBoneTarget in _boneTargets)
            {
                retargetedBoneTarget.BoneId = humanBodyBoneToOVRBoneId[retargetedBoneTarget.HumanBodyBone];
            }
            _initialized = true;
        }

        /// <summary>
        /// Update the bone targets with the bone transform data.
        /// </summary>
        /// <param name="skeleton"></param>
        public void ProcessSkeleton(OVRSkeleton skeleton)
        {
            if (!_enabled)
            {
                return;
            }
            Initialize();
            IList<OVRBone> bones = skeleton.Bones;
            for (var i = 0; i < bones.Count; i++)
            {
                foreach (var retargetedBoneTarget in _boneTargets)
                {
                    if (bones[i].Id == retargetedBoneTarget.BoneId)
                    {
                        var targetBone = bones[i].Transform;
                        if (targetBone == null)
                        {
                            continue;
                        }
                        retargetedBoneTarget.Target.SetPositionAndRotation(
                            targetBone.position, targetBone.rotation);
                    }
                }
            }
        }
    }
}
