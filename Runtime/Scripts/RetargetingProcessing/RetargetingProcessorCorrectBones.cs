// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// Retargeting processor used to apply corrected bone positions from the retargeted animation job output.
    /// </summary>
    [CreateAssetMenu(fileName = "Correct Bones", menuName = "Movement Samples/Data/Retargeting Processors/Correct Bones", order = 1)]
    public sealed class RetargetingProcessorCorrectBones : RetargetingProcessor
    {
        /// <summary>
        /// Allows correcting positions for accuracy.
        /// </summary>
        [SerializeField]
        [Tooltip(RetargetingLayerTooltips.CorrectPositionsLateUpdate)]
        private bool _correctPositionsLateUpdate = true;
        /// <inheritdoc cref="_correctPositionsLateUpdate"/>
        public bool CorrectPositionsLateUpdate
        {
            get => _correctPositionsLateUpdate;
            set => _correctPositionsLateUpdate = value;
        }

        /// <summary>
        /// Allow correcting shoulder transforms in LateUpdate. This can produce more
        /// accurate shoulders, for instance.
        /// </summary>
        [Tooltip(RetargetingLayerTooltips.ShoulderCorrectionWeightLateUpdate)]
        [SerializeField, Range(0.0f, 1.0f)]
        private float _shoulderCorrectionWeightLateUpdate = 1.0f;
        /// <inheritdoc cref="_shoulderCorrectionWeightLateUpdate"/>
        public float ShoulderCorrectionWeightLateUpdate
        {
            get => _shoulderCorrectionWeightLateUpdate;
            set => _shoulderCorrectionWeightLateUpdate = value;
        }

        /// <summary>
        /// Allow correcting rotations. This can produce more
        /// accurate hands, for instance.
        /// </summary>
        [Tooltip(RetargetingLayerTooltips.LeftHandCorrectionWeightLateUpdate)]
        [SerializeField, Range(0.0f, 1.0f)]
        private float _leftHandCorrectionWeightLateUpdate = 1.0f;
        /// <inheritdoc cref="_leftHandCorrectionWeightLateUpdate"/>
        public float LeftHandCorrectionWeightLateUpdate
        {
            get => _leftHandCorrectionWeightLateUpdate;
            set => _leftHandCorrectionWeightLateUpdate = value;
        }

        /// <summary>
        /// Allow correcting rotations. This can produce more
        /// accurate hands, for instance.
        /// </summary>
        [Tooltip(RetargetingLayerTooltips.RightHandCorrectionWeightLateUpdate)]
        [SerializeField, Range(0.0f, 1.0f)]
        private float _rightHandCorrectionWeightLateUpdate = 1.0f;
        /// <inheritdoc cref="_rightHandCorrectionWeightLateUpdate"/>
        public float RightHandCorrectionWeightLateUpdate
        {
            get => _rightHandCorrectionWeightLateUpdate;
            set => _rightHandCorrectionWeightLateUpdate = value;
        }

        /// <inheritdoc />
        public override void CopyData(RetargetingProcessor source)
        {
            base.CopyData(source);
            var sourceCorrectBones = source as RetargetingProcessorCorrectBones;
            if (sourceCorrectBones == null)
            {
                Debug.LogError($"Failed to copy properties from {source.name} processor to {name} processor");
                return;
            }
            _correctPositionsLateUpdate = sourceCorrectBones.CorrectPositionsLateUpdate;
            _shoulderCorrectionWeightLateUpdate = sourceCorrectBones.ShoulderCorrectionWeightLateUpdate;
            _leftHandCorrectionWeightLateUpdate = sourceCorrectBones.LeftHandCorrectionWeightLateUpdate;
            _rightHandCorrectionWeightLateUpdate = sourceCorrectBones.RightHandCorrectionWeightLateUpdate;
        }

        /// <inheritdoc />
        public override void ProcessRetargetingLayer(RetargetingLayer retargetingLayer, IList<OVRBone> ovrBones)
        {
            if (Weight <= 0.0f)
            {
                return;
            }

            bool handCorrectionTurnedOn =
                LeftHandCorrectionWeightLateUpdate > Mathf.Epsilon ||
                RightHandCorrectionWeightLateUpdate > Mathf.Epsilon;

            if (ovrBones == null)
            {
                return;
            }

            for (var i = 0; i < ovrBones.Count; i++)
            {
                if (ovrBones[i] == null)
                {
                    continue;
                }
                var nullableHumanBodyBone = retargetingLayer.GetCustomBoneIdToHumanBodyBone(ovrBones[i].Id);
                if (nullableHumanBodyBone == null)
                {
                    continue;
                }
                var humanBodyBone = (HumanBodyBones)nullableHumanBodyBone;

                var nullableTargetCorrectionQuaternion = retargetingLayer.GetCorrectionQuaternion(humanBodyBone);
                if (nullableTargetCorrectionQuaternion == null)
                {
                    continue;
                }
                var targetCorrectionQuaternion = (Quaternion)nullableTargetCorrectionQuaternion;

                var bodyPart = BoneMappingsExtension.HumanBoneToAvatarBodyPart[humanBodyBone];
                var targetJoint = retargetingLayer.GetOriginalJoint(humanBodyBone);

                // Make sure body part passes mask, and bone's position should be updated.
                if (retargetingLayer.CustomPositionsToCorrectLateUpdateMask != null &&
                    !retargetingLayer.CustomPositionsToCorrectLateUpdateMask.GetHumanoidBodyPartActive(bodyPart))
                {
                    continue;
                }
                var adjustment = retargetingLayer.GetFindAdjustment(humanBodyBone);
                if (!retargetingLayer.GetShouldUpdatePositionOfBone(humanBodyBone))
                {
                    continue;
                }

                var currentTargetPosition = targetJoint.position;
                // Make sure the joint position is valid before fixing it.
                if (!RiggingUtilities.IsFiniteVector3(currentTargetPosition))
                {
                    continue;
                }

                var rtWeight = Weight * retargetingLayer.RetargetingConstraint.weight;

                var bodySectionOfJoint =
                    OVRUnityHumanoidSkeletonRetargeter.OVRHumanBodyBonesMappings.BoneToBodySection[humanBodyBone];
                bool isLeftHandFingersOrWrist =
                    bodySectionOfJoint == OVRUnityHumanoidSkeletonRetargeter.OVRHumanBodyBonesMappings.BodySection.LeftHand ||
                    humanBodyBone == HumanBodyBones.LeftHand;
                bool isRightHandFingersOrWrist =
                    bodySectionOfJoint == OVRUnityHumanoidSkeletonRetargeter.OVRHumanBodyBonesMappings.BodySection.RightHand ||
                    humanBodyBone == HumanBodyBones.RightHand;
                bool isHandJoint =
                    isLeftHandFingersOrWrist ||
                    isRightHandFingersOrWrist;
                bool isShoulderJoint =
                    humanBodyBone == HumanBodyBones.LeftShoulder ||
                    humanBodyBone == HumanBodyBones.RightShoulder;

                // Pick the correct hand correction weight based on handedness.
                var handWeight = Weight * (isLeftHandFingersOrWrist ?
                    LeftHandCorrectionWeightLateUpdate :
                    RightHandCorrectionWeightLateUpdate);

                var constraintsPositionOffset = retargetingLayer.ApplyAnimationConstraintsToCorrectedPositions ?
                    retargetingLayer.JointPositionAdjustments[(int)humanBodyBone].GetPositionOffset() : Vector3.zero;
                var currentOVRBonePosition = ovrBones[i].Transform.position;

                if (isHandJoint)
                {
                    var errorRelativeToBodyTracking = (currentOVRBonePosition - currentTargetPosition).sqrMagnitude;

                    // If generally correcting positions only and applying hand correction,
                    // skip positional fix a) if the error relative to body tracking is low,
                    // b) and the position influence due to IK fixes is small.
                    if (!handCorrectionTurnedOn &&
                        errorRelativeToBodyTracking < Mathf.Epsilon &&
                        constraintsPositionOffset.sqrMagnitude < Mathf.Epsilon)
                    {
                        continue;
                    }

                    // Exclude any position offsets from IK if correcting hand joints to
                    // what body tracking indicates they are.
                    constraintsPositionOffset = Vector3.Lerp(constraintsPositionOffset, Vector3.zero, handWeight);
                }

                if (isShoulderJoint)
                {
                    CorrectShoulders(targetJoint,
                        ovrBones[i].Transform.rotation,
                        targetCorrectionQuaternion,
                        adjustment);
                }

                if (adjustment == null)
                {
                    if (handCorrectionTurnedOn && isHandJoint)
                    {
                        targetJoint.rotation =
                            Quaternion.Slerp(targetJoint.rotation,
                                ovrBones[i].Transform.rotation * targetCorrectionQuaternion,
                                handWeight);
                    }

                    if (CorrectPositionsLateUpdate)
                    {
                        targetJoint.position =
                            Vector3.Lerp(currentTargetPosition,
                                currentOVRBonePosition + constraintsPositionOffset, rtWeight);
                    }
                }
                else
                {
                    if (handCorrectionTurnedOn && isHandJoint)
                    {
                        if (!adjustment.DisableRotationTransform)
                        {
                            targetJoint.rotation =
                                Quaternion.Slerp(targetJoint.rotation,
                                    ovrBones[i].Transform.rotation * targetCorrectionQuaternion,
                                    handWeight);
                        }

                        targetJoint.rotation *= adjustment.RotationChange * adjustment.PrecomputedRotationTweaks;
                    }

                    if (CorrectPositionsLateUpdate && !adjustment.DisablePositionTransform)
                    {
                        targetJoint.position =
                            Vector3.Lerp(currentTargetPosition,
                                currentOVRBonePosition + constraintsPositionOffset + adjustment.PositionChange, rtWeight);
                    }
                }
            }
        }

        /// <summary>
        /// Correct the shoulders rotation to not be restricted by muscle space.
        /// </summary>
        private void CorrectShoulders(Transform targetJoint,
            Quaternion boneRotation,
            Quaternion correctionQuaternion,
            OVRUnityHumanoidSkeletonRetargeter.JointAdjustment adjustment)
        {
            // Restore the child transform when correcting shoulders.
            var childJoint = targetJoint.GetChild(0);
            var childAffineTransform = new AffineTransform
            {
                rotation = childJoint != null ? childJoint.rotation : Quaternion.identity,
                translation = childJoint != null ? childJoint.position : Vector3.zero
            };
            var targetWeight = adjustment != null && adjustment.DisableRotationTransform ?
                0.0f : ShoulderCorrectionWeightLateUpdate;
            var rotationChange = adjustment?.PrecomputedRotationTweaks ?? Quaternion.identity;
            targetJoint.rotation =
                Quaternion.Slerp(targetJoint.rotation,
                    boneRotation * correctionQuaternion,
                    targetWeight) *
                    rotationChange;

            if (childJoint != null)
            {
                childJoint.SetPositionAndRotation(childAffineTransform.translation,
                    childAffineTransform.rotation);
            }
        }

    }
}
