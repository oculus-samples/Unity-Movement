// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// Update target transforms with retargeted bone data.
    /// </summary>
    public class RetargetedBoneTargets : MonoBehaviour
    {
        /// <summary>
        /// A retargeted bone target.
        /// </summary>
        [System.Serializable]
        public class RetargetedBoneTarget
        {
            /// <summary>
            /// The OVRSkeleton.BoneId corresponding to the HumanBodyBone.
            /// </summary>
            public OVRSkeleton.BoneId BoneId { get; set; }

            /// <summary>
            /// The human body bone representation of this bone.
            /// </summary>
            [Tooltip(RetargetedBoneTargetsTooltips.RetargetedBoneTargetTooltips.HumanBodyBone)]
            public HumanBodyBones HumanBodyBone;

            /// <summary>
            /// The target transform to update with the retargeted bone data.
            /// </summary>
            [Tooltip(RetargetedBoneTargetsTooltips.RetargetedBoneTargetTooltips.Target)]
            public Transform Target;
        }

        /// <summary>
        /// The array of retargeted bone targets.
        /// </summary>
        [SerializeField]
        [Tooltip(RetargetedBoneTargetsTooltips.RetargetedBoneTargets)]
        protected RetargetedBoneTarget[] _retargetedBoneTargets;

        /// <summary>
        /// Correlate HumanBodyBones to OVRSkeleton.BoneId.
        /// </summary>
        protected virtual void Awake()
        {
            var humanBodyBoneToOVRBoneId =
                CustomMappings.BoneIdToHumanBodyBone.ToDictionary(
                    x => x.Value, x => x.Key);
            foreach (var retargetedBoneTarget in _retargetedBoneTargets)
            {
                retargetedBoneTarget.BoneId = humanBodyBoneToOVRBoneId[retargetedBoneTarget.HumanBodyBone];
            }
        }

        /// <summary>
        /// Update the bone targets with the retargeted bone transform data.
        /// This should be used with <see cref="RetargetingLayer.SkeletonPostProcessing" />.
        /// </summary>
        /// <param name="bones"></param>
        public virtual void UpdateTargetsWithBoneData(IList<OVRBone> bones)
        {
            for (var i = 0; i < bones.Count; i++)
            {
                foreach (var retargetedBoneTarget in _retargetedBoneTargets)
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
