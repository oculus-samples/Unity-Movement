// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Assertions;

namespace Oculus.Movement.AnimationRigging.HipPinning
{
    /// <summary>
    /// Useful if we need to have a tip transform for
    /// two-bone IK follow a target before two bone IK runs.
    /// If the two are not aligned, then two-bone IK creates a
    /// rotational offset around the y-axis.
    /// We need to eliminate the offset such that target follows the
    /// tip before two bone IK initializes.
    /// </summary>
    public class TargetOffsetCorrection : MonoBehaviour
    {
        /// <summary>
        /// Rig builder of character.
        /// </summary>
        [SerializeField]
        [Tooltip(TargetOffsetCorrectionTooltips.RigBuilder)]
        private RigBuilder _rigBuilder;

        /// <summary>
        /// Ankle bone.
        /// </summary>
        [SerializeField]
        [Tooltip(TargetOffsetCorrectionTooltips.AnkleBone)]
        private Transform _ankleBone;

        /// <summary>
        /// Foot tip bone.
        /// </summary>
        [SerializeField]
        [Tooltip(TargetOffsetCorrectionTooltips.TipBone)]
        private Transform _tipBone;

        /// <summary>
        /// Foot grounding constraint.
        /// </summary>
        [SerializeField]
        [Tooltip(TargetOffsetCorrectionTooltips.GroundingConstraint)]
        private GroundingConstraint _groundingConstraint;

        private void Awake()
        {
            Assert.IsNotNull(_rigBuilder);
            Assert.IsNotNull(_ankleBone);
            Assert.IsNotNull(_tipBone);
            Assert.IsNotNull(_groundingConstraint);
        }

        private void LateUpdate()
        {
            if (!_rigBuilder.enabled)
            {
                return;
            }

            AlignAnkleWithTargets();
        }

        private void AlignAnkleWithTargets()
        {
            IGroundingData groundingData = _groundingConstraint.data;

            var lookRotationTargets = Quaternion.LookRotation(groundingData.KneeTarget.position -
                groundingData.HipsTarget.position);
            var lookRotationFoot = Quaternion.LookRotation(_tipBone.position - _ankleBone.position);

            var rotationChange = Quaternion.FromToRotation(lookRotationFoot * Vector3.forward,
                lookRotationTargets * Vector3.forward);
            // We want the feet to be aligned horizontally with the look direction of the targets
            // horizontally (around the Y-axis), so that two-bone IK does not precompute
            // a Y-axis offset before it runs. Otherwise, the foot will look twisted around Y if
            // body tracking foot joint and the targets are not aligned around the Y-axis.
            var rotationChangeEuler = rotationChange.eulerAngles;
            rotationChange = Quaternion.Euler(0.0f, rotationChangeEuler.y, 0.0f);
            _ankleBone.rotation = groundingData.FootRotationOffset * rotationChange * _ankleBone.rotation;
        }
    }
}
