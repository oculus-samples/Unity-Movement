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
        [SerializeField, Range(0.0f, 1.0f)]
        [Tooltip(RetargetingLayerTooltips.ShoulderCorrectionWeightLateUpdate)]
        private float _shoulderCorrectionWeightLateUpdate = 1.0f;
        /// <inheritdoc cref="_shoulderCorrectionWeightLateUpdate"/>
        public float ShoulderCorrectionWeightLateUpdate
        {
            get => _shoulderCorrectionWeightLateUpdate;
            set => _shoulderCorrectionWeightLateUpdate = value;
        }

        /// <summary>
        /// Finger position correction weight. For some characters, we might want to correct
        /// all bones but reduce the positional accuracy of the fingers to maintain the
        /// character's original hand shape.
        /// </summary>
        [SerializeField, Range(0.0f, 1.0f)]
        [Tooltip(RetargetingLayerTooltips.FingerPositionCorrectionWeight)]
        private float _fingerPositionCorrectionWeight = 1.0f;
        public float FingerPositionCorrectionWeight
        {
            get => _fingerPositionCorrectionWeight;
            set => _fingerPositionCorrectionWeight = value;
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
        }

        /// <inheritdoc />
        public override void ProcessRetargetingLayer(RetargetingLayer retargetingLayer, IList<OVRBone> ovrBones)
        {
            if (Weight <= 0.0f)
            {
                return;
            }

            if (ovrBones == null)
            {
                return;
            }

            var animator = retargetingLayer.GetAnimatorTargetSkeleton();
            var leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            var rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            var startingLeftHandPos = leftHand.localPosition;
            var startingRightHandPos = rightHand.localPosition;

            for (var i = 0; i < ovrBones.Count; i++)
            {
                if (ovrBones[i] == null)
                {
                    continue;
                }

                // Check if there's a valid bone mapping to the HumanBodyBone.
                var nullableHumanBodyBone = retargetingLayer.GetCustomBoneIdToHumanBodyBone(ovrBones[i].Id);
                if (nullableHumanBodyBone == null)
                {
                    continue;
                }
                var humanBodyBone = (HumanBodyBones)nullableHumanBodyBone;

                // Check if there's a retargeting correction quaternion for the HumanBodyBone.
                var nullableTargetCorrectionQuaternion = retargetingLayer.GetCorrectionQuaternion(humanBodyBone);
                if (nullableTargetCorrectionQuaternion == null)
                {
                    continue;
                }
                var targetCorrectionQuaternion = (Quaternion)nullableTargetCorrectionQuaternion;

                // Make sure body part passes mask, and bone's position should be updated.
                var bodyPart = BoneMappingsExtension.HumanBoneToAvatarBodyPart[humanBodyBone];
                var targetJoint = retargetingLayer.GetOriginalJoint(humanBodyBone);
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

                // Make sure the joint position is valid before fixing it.
                var currentTargetPosition = targetJoint.position;
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
                bool isHandJoint = isLeftHandFingersOrWrist || isRightHandFingersOrWrist;
                bool isShoulderJoint = humanBodyBone == HumanBodyBones.LeftShoulder ||
                                       humanBodyBone == HumanBodyBones.RightShoulder;

                var constraintsPositionOffset = retargetingLayer.ApplyAnimationConstraintsToCorrectedPositions ?
                    retargetingLayer.JointPositionAdjustments[(int)humanBodyBone].GetPositionOffset() : Vector3.zero;
                var currentOVRBonePosition = ovrBones[i].Transform.position;

                // Move the hand and fingers to the tracked positions by setting the position offset to zero.
                if (isHandJoint)
                {
                    constraintsPositionOffset = Vector3.zero;
                }

                // Used to dial back finger position correction, if required.
                bool isAFingerJoint =
                    bodySectionOfJoint == OVRUnityHumanoidSkeletonRetargeter.OVRHumanBodyBonesMappings.BodySection.LeftHand ||
                    bodySectionOfJoint == OVRUnityHumanoidSkeletonRetargeter.OVRHumanBodyBonesMappings.BodySection.RightHand;

                // Remove muscle space restrictions for the shoulders.
                if (isShoulderJoint)
                {
                    CorrectShoulders(targetJoint,
                        ovrBones[i].Transform.rotation,
                        targetCorrectionQuaternion,
                        adjustment);
                }

                if (adjustment == null)
                {
                    if (isHandJoint)
                    {
                        targetJoint.rotation = Quaternion.Slerp(targetJoint.rotation,
                                ovrBones[i].Transform.rotation * targetCorrectionQuaternion, rtWeight);
                    }

                    if (CorrectPositionsLateUpdate)
                    {
                        targetJoint.position =
                            Vector3.Lerp(currentTargetPosition,
                                currentOVRBonePosition + constraintsPositionOffset,
                                isAFingerJoint ? rtWeight * _fingerPositionCorrectionWeight : rtWeight);
                    }
                }
                else
                {
                    if (isHandJoint)
                    {
                        if (!adjustment.DisableRotationTransform)
                        {
                            var finalRotation = ovrBones[i].Transform.rotation * targetCorrectionQuaternion;
                            finalRotation *= adjustment.RotationChange * adjustment.PrecomputedRotationTweaks;

                            targetJoint.rotation = Quaternion.Slerp(targetJoint.rotation, finalRotation, rtWeight);
                        }
                    }

                    if (CorrectPositionsLateUpdate && !adjustment.DisablePositionTransform)
                    {
                        targetJoint.position =
                            Vector3.Lerp(currentTargetPosition,
                                currentOVRBonePosition + constraintsPositionOffset + adjustment.PositionChange,
                                isAFingerJoint ? rtWeight * _fingerPositionCorrectionWeight : rtWeight);
                    }
                }
            }

            // Revert to the original hand position, as this should be used to only correct the fingers.
            leftHand.localPosition = startingLeftHandPos;
            rightHand.localPosition = startingRightHandPos;
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
                0.0f : ShoulderCorrectionWeightLateUpdate * Weight;
            var rotationChange = adjustment?.PrecomputedRotationTweaks ?? Quaternion.identity;
            targetJoint.rotation =
                Quaternion.Slerp(targetJoint.rotation,
                    boneRotation * correctionQuaternion * rotationChange,
                    targetWeight);

            if (childJoint != null)
            {
                childJoint.SetPositionAndRotation(childAffineTransform.translation,
                    childAffineTransform.rotation);
            }
        }
    }
}
