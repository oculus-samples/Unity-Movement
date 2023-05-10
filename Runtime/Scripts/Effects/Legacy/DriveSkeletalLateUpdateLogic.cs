// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Movement.Effects.Deprecated
{
    /// <summary>
    /// Run any advanced skeletal animation scripts and normal recalculation in LateUpdate
    /// after OVRSkeleton has updated the bones.
    /// </summary>
    public class DriveSkeletalLateUpdateLogic : MonoBehaviour
    {
        /// <summary>
        /// Normal recalculation components to drive.
        /// </summary>
        [SerializeField]
        [Optional]
        [Tooltip(DriveSkeletalLateUpdateLogicTooltips.RecalculateNormals)]
        protected RecalculateNormals[] _recalculateNormals;

        /// <summary>
        /// Hip pinning components to drive.
        /// </summary>
        [SerializeField]
        [Optional]
        [Tooltip(DriveSkeletalLateUpdateLogicTooltips.HipPinnings)]
        protected HipPinningLogic[] _hipPinnings;

        /// <summary>
        /// Deformation logic components to drive.
        /// </summary>
        [SerializeField]
        [Optional]
        [Tooltip(DriveSkeletalLateUpdateLogicTooltips.DeformationLogics)]
        protected DeformationLogic[] _deformationLogics;

        /// <summary>
        /// Grounding logic components to drive.
        /// </summary>
        [SerializeField]
        [Optional]
        [Tooltip(DriveSkeletalLateUpdateLogicTooltips.Groundings)]
        protected GroundingLogic[] _groundings;

        /// <summary>
        /// Twist distribution components to drive.
        /// </summary>
        [SerializeField]
        [Optional]
        [Tooltip(DriveSkeletalLateUpdateLogicTooltips.TwistDistributions)]
        protected TwistDistribution[] _twistDistributions;

        private void Awake()
        {
            foreach (var deformationLogic in _deformationLogics)
            {
                Assert.IsNotNull(deformationLogic);
            }
            foreach (var hipPinning in _hipPinnings)
            {
                Assert.IsNotNull(hipPinning);
            }
            foreach (var grounding in _groundings)
            {
                Assert.IsNotNull(grounding);
            }
            foreach (var recalculateNormals in _recalculateNormals)
            {
                Assert.IsNotNull(recalculateNormals);
            }
            foreach (var twistDistribution in _twistDistributions)
            {
                Assert.IsNotNull(twistDistribution);
            }
        }

        private void LateUpdate()
        {
            foreach (var hipPinning in _hipPinnings)
            {
                if (hipPinning)
                {
                    hipPinning.ApplyHipPinning();
                }
            }

            foreach (var deformationLogic in _deformationLogics)
            {
                if (deformationLogic)
                {
                    deformationLogic.ApplyDeformation();
                }
            }

            foreach (var twistDistribution in _twistDistributions)
            {
                if (twistDistribution)
                {
                    twistDistribution.ApplyTwist();
                }
            }

            foreach (var grounding in _groundings)
            {
                if (grounding)
                {
                    grounding.ApplyGrounding();
                }
            }

            // Recalculate normals must be applied after skeletal transformations are applied
            foreach (var recalculateNormals in _recalculateNormals)
            {
                if (recalculateNormals)
                {
                    recalculateNormals.ApplyNormalRecalculation();
                }
            }
        }
    }
}
