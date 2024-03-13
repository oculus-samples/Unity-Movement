// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Linq;
using static OVRUnityHumanoidSkeletonRetargeter;

namespace Oculus.Movement.AnimationRigging.Deprecated
{
    /// <summary>
    /// Similar to <see cref="RetargetedBoneTargets"/> but applied to full body.
    /// </summary>
    public class FullBodyRetargetedBoneTargets : RetargetedBoneTargets
    {
        /// <summary>
        /// Component to auto-add to.
        /// </summary>
        public UnityEngine.Object AutoAdd
        {
            get => _autoAddTo;
            set => _autoAddTo = value;
        }

        /// <summary>
        /// Accessor to base class.
        /// </summary>
        public RetargetedBoneTarget[] RetargetedBoneTargets
        {
            get => _retargetedBoneTargets;
            set => _retargetedBoneTargets = value;
        }


        protected override void Awake()
        {
            var humanBodyBoneToOVRBoneId =
                OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone.ToDictionary(
                    x => x.Value, x => x.Key);
            foreach (var retargetedBoneTarget in _retargetedBoneTargets)
            {
                retargetedBoneTarget.BoneId = humanBodyBoneToOVRBoneId[retargetedBoneTarget.HumanBodyBone];
            }
        }
    }
}
