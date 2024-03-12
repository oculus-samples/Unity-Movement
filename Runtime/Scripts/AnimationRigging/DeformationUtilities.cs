// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using Oculus.Movement.Utils;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using static OVRUnityHumanoidSkeletonRetargeter;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// Provides convenient algorithms to assist with deformation work.
    /// </summary>
    public static class DeformationUtilities
    {
        /// <summary>
        /// Information about the distance between two bone transforms.
        /// </summary>
        [Serializable]
        public struct BonePairData
        {
            /// <summary>
            /// The start bone transform.
            /// </summary>
            [SyncSceneToStream]
            public Transform StartBone;

            /// <summary>
            /// The end bone transform.
            /// </summary>
            [SyncSceneToStream]
            public Transform EndBone;

            /// <summary>
            /// The distance between the start and end bones.
            /// </summary>
            public float Distance;

            /// <summary>
            /// The proportion of this bone relative to the height.
            /// </summary>
            public float HeightProportion;

            /// <summary>
            /// The proportion of this bone relative to its limb.
            /// </summary>
            public float LimbProportion;
        }

        /// <summary>
        /// Information about an adjustment to be applied on a bone.
        /// </summary>
        [Serializable]
        public struct BoneAdjustmentData
        {
            /// <summary>
            /// The HumanBodyBones bone.
            /// </summary>
            public HumanBodyBones Bone;

            /// <summary>
            /// The adjustment to be applied to the bone.
            /// </summary>
            public Quaternion Adjustment;

            /// <summary>
            /// The first child bone. Will be HumanBodyBones.LastBone if invalid.
            /// The Unity Humanoid has only three possible relevant child bones for deformation:
            /// two arms or two legs, and a child spine bone.
            /// </summary>
            public HumanBodyBones ChildBone1;

            /// <summary>
            /// The second child bone. Will be HumanBodyBones.LastBone if invalid.
            /// The Unity Humanoid has only three possible relevant child bones for deformation:
            /// two arms or two legs, and a child spine bone.
            /// </summary>
            public HumanBodyBones ChildBone2;

            /// <summary>
            /// The third child bone. Will be HumanBodyBones.LastBone if invalid.
            /// The Unity Humanoid has only three possible relevant child bones for deformation:
            /// two arms or two legs, and a child spine bone.
            /// </summary>
            public HumanBodyBones ChildBone3;
        }

        /// <summary>
        /// Information about the positioning of an arm.
        /// </summary>
        [Serializable]
        public struct ArmPosData
        {
            /// <summary>
            /// The shoulder transform.
            /// </summary>
            public Transform ShoulderBone;

            /// <summary>
            /// The upper arm transform.
            /// </summary>
            public Transform UpperArmBone;

            /// <summary>
            /// The lower arm transform.
            /// </summary>
            public Transform LowerArmBone;

            /// <summary>
            /// The hand transform.
            /// </summary>
            public Transform HandBone;

            /// <summary>
            /// The local position of the shoulder.
            /// </summary>
            public Vector3 ShoulderLocalPos;

            /// <summary>
            /// The axis of the lower arm to the hand.
            /// </summary>
            public Vector3 LowerArmToHandAxis;

            /// <summary>
            /// Indicates if initialized or not.
            /// </summary>
            /// <returns></returns>
            public bool IsInitialized =>
                UpperArmBone != null &&
                LowerArmBone != null &&
                HandBone != null &&
                LowerArmToHandAxis != Vector3.zero;

            /// <summary>
            /// Resets all tracked transforms to null.
            /// </summary>
            public void ClearTransformData()
            {
                ShoulderBone = null;
                UpperArmBone = null;
                LowerArmBone = null;
                HandBone = null;
            }
        }

        /// <summary>
        /// Information about the positioning of a leg.
        /// </summary>
        [Serializable]
        public struct LegPosData
        {
            /// <summary>
            /// The hips transform.
            /// </summary>
            public Transform HipsBone;

            /// <summary>
            /// The upper leg transform.
            /// </summary>
            public Transform UpperLegBone;

            /// <summary>
            /// The lower leg transform.
            /// </summary>
            public Transform LowerLegBone;

            /// <summary>
            /// The foot transform.
            /// </summary>
            public Transform FootBone;

            /// <summary>
            /// The toes transform.
            /// </summary>
            public Transform ToesBone;

            /// <summary>
            /// The local position of the toes.
            /// </summary>
            public Vector3 ToesLocalPos;

            /// <summary>
            /// The local rotation of the foot.
            /// </summary>
            public Quaternion FootLocalRot;

            /// <summary>
            /// Indicates if initialized or not.
            /// </summary>
            /// <returns></returns>
            public bool IsInitialized =>
                HipsBone != null &&
                UpperLegBone != null &&
                LowerLegBone != null &&
                FootBone != null;

            /// <summary>
            /// Resets all tracked transforms to null.
            /// </summary>
            public void ClearTransformData()
            {
                HipsBone = null;
                UpperLegBone = null;
                LowerLegBone = null;
                FootBone = null;
                ToesBone = null;
            }
        }

        /// <summary>
        /// Ordered HumanBodyBones for the spine, arms, and legs.
        /// </summary>
        private static readonly HumanBodyBones[] _humanBodyBonesOrder =
        {
            HumanBodyBones.Hips,
            HumanBodyBones.Spine,
            HumanBodyBones.Chest,
            HumanBodyBones.UpperChest,
            HumanBodyBones.Neck,
            HumanBodyBones.Head,
            HumanBodyBones.LeftShoulder,
            HumanBodyBones.LeftUpperArm,
            HumanBodyBones.LeftLowerArm,
            HumanBodyBones.LeftHand,
            HumanBodyBones.RightShoulder,
            HumanBodyBones.RightUpperArm,
            HumanBodyBones.RightLowerArm,
            HumanBodyBones.RightHand,
            HumanBodyBones.LeftUpperLeg,
            HumanBodyBones.LeftLowerLeg,
            HumanBodyBones.LeftFoot,
            HumanBodyBones.LeftToes,
            HumanBodyBones.RightUpperLeg,
            HumanBodyBones.RightLowerLeg,
            HumanBodyBones.RightFoot,
            HumanBodyBones.RightToes
        };

        /// <summary>
        /// HumanBodyBones mapping for the end of bone chains.
        /// </summary>
        private static readonly Dictionary<HumanBodyBones, HumanBodyBones> _endChildBonesMapping = new()
        {
            {
                HumanBodyBones.Head, HumanBodyBones.LastBone
            },
            {
                HumanBodyBones.LeftToes, HumanBodyBones.LastBone
            },
            {
                HumanBodyBones.RightToes, HumanBodyBones.LastBone
            },
            {
                HumanBodyBones.LeftHand, HumanBodyBones.LastBone
            },
            {
                HumanBodyBones.RightHand, HumanBodyBones.LastBone
            }
        };

        /// <summary>
        /// Array of optional spine HumanBodyBones.
        /// </summary>
        private static readonly HumanBodyBones[] _optionalSpineBones =
        {
            HumanBodyBones.Spine, HumanBodyBones.Chest, HumanBodyBones.UpperChest,
        };

        /// <summary>
        /// Array of child HumanBodyBones for the chest and shoulders.
        /// </summary>
        private static readonly HumanBodyBones[] _chestAndShoulderChildBones =
        {
            HumanBodyBones.Neck,
            HumanBodyBones.LeftShoulder, HumanBodyBones.RightShoulder,
            HumanBodyBones.LeftUpperArm, HumanBodyBones.RightUpperArm
        };

        /// <summary>
        /// Returns bone adjustments required for the deformation constraint.
        /// </summary>
        /// <param name="animator">The animator to calculate bone adjustments.</param>
        /// <param name="restPoseObject">The rest pose object to calculate bone adjustments.</param>
        /// <returns>The array of bone adjustments.</returns>
        public static BoneAdjustmentData[] GetDeformationBoneAdjustments(Animator animator,
            RestPoseObjectHumanoid restPoseObject)
        {
            // Calculate spine adjustments.
            var shoulderParentAdjustmentBone = GetValidShoulderParentBone(animator);
            var shoulderParentAdjustment = Quaternion.identity;
            var spineBoneAdjustments = GetSpineJointAdjustments(animator, restPoseObject);

            // If the chest bone is missing, invert the spine rotation as the
            // fallback rotation is the previous rotation.
            if (animator.GetBoneTransform(HumanBodyBones.Chest) == null)
            {
                spineBoneAdjustments[1].Adjustment = Quaternion.Inverse(spineBoneAdjustments[1].Adjustment);
            }
            foreach (var spineBoneAdjustment in spineBoneAdjustments)
            {
                if (spineBoneAdjustment.Bone == shoulderParentAdjustmentBone)
                {
                    shoulderParentAdjustment = spineBoneAdjustment.Adjustment;
                    break;
                }
            }

            // Calculate adjustments for the shoulders (or upper arms, if shoulders are unavailable).
            var shoulderBoneAdjustments =
                GetShoulderAdjustments(animator, restPoseObject, shoulderParentAdjustment);

            // Combine calculated adjustments.
            var boneAdjustments = new List<BoneAdjustmentData>();
            boneAdjustments.AddRange(spineBoneAdjustments);

            // Calculate an adjustment alignment if needed, using the desired right and forward from the
            // rest pose humanoid, which is Vector3.right and Vector3.forward.
            var adjustmentAlignment =
                GetHipsRightForwardAlignmentForAdjustments(animator, Vector3.right, Vector3.forward);
            for (int i = 0; i < boneAdjustments.Count; i++)
            {
                var adjustment = boneAdjustments[i];
                // We use euler angles here as we want to rotate the adjustment point with the alignment rotation,
                // rather than combine the rotations.
                var adjustmentPoint = adjustment.Adjustment.eulerAngles;
                adjustment.Adjustment =
                    Quaternion.Euler(adjustmentAlignment * adjustmentPoint);
                boneAdjustments[i] = adjustment;
            }
            boneAdjustments.AddRange(shoulderBoneAdjustments);
            return boneAdjustments.ToArray();
        }

        /// <summary>
        /// Returns joint adjustments required for retargeting.
        /// </summary>
        /// <param name="animator">The animator to calculate joint adjustments.</param>
        /// <param name="restPoseObject">The rest pose object to calculate joint adjustments.</param>
        /// <param name="constraint">The optional deformation constraint for cached data.</param>
        /// <returns>The array of joint adjustments.</returns>
        public static JointAdjustment[] GetJointAdjustments(Animator animator, RestPoseObjectHumanoid restPoseObject,
            FullBodyDeformationConstraint constraint = null)
        {
            var adjustments = new List<JointAdjustment>();
            BoneAdjustmentData[] boneAdjustmentData;
            if (constraint != null)
            {
                boneAdjustmentData = (constraint.data as IFullBodyDeformationData).BoneAdjustments;
            }
            else
            {
                boneAdjustmentData = GetDeformationBoneAdjustments(animator, restPoseObject);
            }
            foreach (var boneAdjustment in boneAdjustmentData)
            {
                var rotationTweak = boneAdjustment.Adjustment;
                if (boneAdjustment.Bone == HumanBodyBones.UpperChest ||
                    boneAdjustment.Bone == HumanBodyBones.LeftShoulder ||
                    boneAdjustment.Bone == HumanBodyBones.RightShoulder)
                {
                    // Reducing the rotation adjustment on the upper chest and shoulders yields a better result visually.
                    rotationTweak =
                        Quaternion.Slerp(Quaternion.identity, rotationTweak, 0.5f);
                }
                var adjustment = new JointAdjustment()
                {
                    Joint = boneAdjustment.Bone,
                    RotationTweaks = new[]
                    {
                        rotationTweak
                    }
                };
                adjustments.Add(adjustment);
            }

            // Assume that we want the feet to be pointing world space forward at rest if toes are missing.
            if (animator.GetBoneTransform(HumanBodyBones.LeftToes) == null &&
                animator.GetBoneTransform(HumanBodyBones.RightToes) == null)
            {
                var footAdjustments = GetFeetAdjustments(animator, restPoseObject, Vector3.forward);
                adjustments.AddRange(footAdjustments);
            }
            return adjustments.ToArray();
        }

        /// <summary>
        /// Returns joint adjustments for the feet.
        /// </summary>
        /// <param name="animator">The animator to calculate joint adjustments.</param>
        /// <param name="restPoseObject">The rest pose object to calculate joint adjustments.</param>
        /// <param name="desiredFeetDirection">The desired feet direction.</param>
        /// <returns>The array of joint adjustments for the feet.</returns>
        private static JointAdjustment[] GetFeetAdjustments(Animator animator, RestPoseObjectHumanoid restPoseObject,
            Vector3 desiredFeetDirection)
        {
            if (restPoseObject.GetBonePoseData(HumanBodyBones.LeftToes) == null ||
                restPoseObject.GetBonePoseData(HumanBodyBones.RightToes) == null)
            {
                Debug.LogError("Expected valid toes data in the rest pose object for aligning the feet. " +
                               "No foot adjustments will be created.");
                return Array.Empty<JointAdjustment>();
            }

            var footAdjustments = new List<JointAdjustment>();
            var leftLeg = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            var rightLeg = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            var legDotProduct = Vector3.Dot(leftLeg.forward, rightLeg.forward);
            var shouldMirrorLegs = legDotProduct < 0.0f;
            var adjustmentAlignment =
                GetHipsRightForwardAlignmentForAdjustments(animator, Vector3.right, Vector3.forward);

            var leftFootAdjustment =
                GetFootJointAdjustment(animator, restPoseObject, HumanBodyBones.LeftFoot, HumanBodyBones.LeftToes,
                    desiredFeetDirection, adjustmentAlignment, 1.0f);
            var rightFootAdjustment =
                GetFootJointAdjustment(animator, restPoseObject, HumanBodyBones.RightFoot, HumanBodyBones.RightToes,
                    desiredFeetDirection, adjustmentAlignment, shouldMirrorLegs ? 1.0f : -1.0f);
            footAdjustments.Add(leftFootAdjustment);
            footAdjustments.Add(rightFootAdjustment);
            return footAdjustments.ToArray();
        }

        /// <summary>
        /// Get the foot joint adjustment.
        /// </summary>
        /// <param name="animator">The animator to calculate joint adjustments.</param>
        /// <param name="restPoseObject">The rest pose to calculate joint adjustments.</param>
        /// <param name="footBone">The foot HumanBodyBones.</param>
        /// <param name="toesBone">The toes HumanBodyBones.</param>
        /// <param name="desiredFootDirection">The desired direction for the foot.</param>
        /// <param name="adjustmentAlignment">The rotation </param>
        /// <param name="adjustmentAngleModifier"></param>
        /// <returns>The joint adjustment for the specified foot.</returns>
        private static JointAdjustment GetFootJointAdjustment(Animator animator, RestPoseObjectHumanoid restPoseObject,
            HumanBodyBones footBone, HumanBodyBones toesBone, Vector3 desiredFootDirection,
            Quaternion adjustmentAlignment, float adjustmentAngleModifier)
        {
            var foot = animator.GetBoneTransform(footBone);
            // Align the feet in the desired foot direction.
            var footToToes =
                (restPoseObject.GetBonePoseData(toesBone).WorldPose.position -
                 restPoseObject.GetBonePoseData(footBone).WorldPose.position).normalized;
            var footAngle = Vector3.Angle(desiredFootDirection, footToToes);
            // Use the inverse rotation here as the rotation of the feet need to be inverted.
            var adjustment = Quaternion.Inverse(Quaternion.AngleAxis(
                footAngle * adjustmentAngleModifier, foot.up));
            return new JointAdjustment
            {
                Joint = footBone,
                RotationTweaks = new[]
                {
                    Quaternion.Euler(adjustmentAlignment * adjustment.eulerAngles)
                }
            };
        }

        /// <summary>
        /// Get the valid parent HumanBodyBone of the shoulders.
        /// </summary>
        /// <param name="animator">The animator to check.</param>
        /// <returns>The valid HumanBodyBone parent of the shoulders.</returns>
        private static HumanBodyBones GetValidShoulderParentBone(Animator animator)
        {
            if (animator.GetBoneTransform(HumanBodyBones.UpperChest) != null)
            {
                return HumanBodyBones.UpperChest;
            }
            if (animator.GetBoneTransform(HumanBodyBones.Chest) != null)
            {
                return HumanBodyBones.Chest;
            }
            return HumanBodyBones.Spine;
        }

        /// <summary>
        /// Calculate the from to rotation to align the hips right with the alignment right direction.
        /// </summary>
        /// <param name="animator">The animator.</param>
        /// <param name="alignmentRightDirection">The alignment right direction.</param>
        /// <param name="alignmentForwardDirection">The alignment forward direction.</param>
        /// <returns>Rotation to align the hips right with the alignment right direction.</returns>
        private static Quaternion GetHipsRightForwardAlignmentForAdjustments(Animator animator,
            Vector3 alignmentRightDirection,
            Vector3 alignmentForwardDirection)
        {
            var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            return Quaternion.FromToRotation(hips.right, alignmentRightDirection) *
                   Quaternion.FromToRotation(hips.forward, alignmentForwardDirection);
        }

        /// <summary>
        /// Get the spine joint adjustments given an animator and the rest pose.
        /// </summary>
        /// <param name="animator">The animator.</param>
        /// <param name="restPoseObject">The rest pose humanoid object.</param>
        /// <returns>The calculated spine joint adjustments.</returns>
        private static BoneAdjustmentData[] GetSpineJointAdjustments(Animator animator,
            RestPoseObjectHumanoid restPoseObject)
        {
            // Apply auto adjustments for the spine.
            var spineHumanBodyBones = new[]
            {
                HumanBodyBones.Hips,
                HumanBodyBones.Spine,
                HumanBodyBones.Chest,
                HumanBodyBones.UpperChest
            };
            var boneAdjustments = new List<BoneAdjustmentData>();
            for (int i = 0; i < spineHumanBodyBones.Length; i++)
            {
                var spineHumanBodyBone = spineHumanBodyBones[i];
                if (animator.GetBoneTransform(spineHumanBodyBone) != null)
                {
                    var spineChildBone = FindChildHumanBodyBones(animator, spineHumanBodyBone);
                    var mappingChildBone = OVRHumanBodyBonesMappings.BoneToJointPair[spineHumanBodyBone].Item2;
                    var adjustment = restPoseObject.CalculateRotationDifferenceFromRestPoseToAnimatorBonePair(
                        animator, spineHumanBodyBone, mappingChildBone,
                        spineHumanBodyBone, spineChildBone);
                    var adjustmentData = new BoneAdjustmentData
                    {
                        Bone = spineHumanBodyBone,
                        Adjustment = adjustment,
                        ChildBone1 = spineChildBone,
                        ChildBone2 = FindChildHumanBodyBones(animator, spineHumanBodyBone, 1),
                        ChildBone3 = FindChildHumanBodyBones(animator, spineHumanBodyBone, 2)
                    };

                    // If we have the same child bone, discard as invalid.
                    if (adjustmentData.ChildBone2 == adjustmentData.ChildBone3)
                    {
                        adjustmentData.ChildBone2 = HumanBodyBones.LastBone;
                        adjustmentData.ChildBone3 = HumanBodyBones.LastBone;
                    }

                    boneAdjustments.Add(adjustmentData);
                }
            }
            return boneAdjustments.ToArray();
        }

        /// <summary>
        /// Get the shoulder joint adjustments given an animator and the rest pose.
        /// </summary>
        /// <param name="animator">The animator.</param>
        /// <param name="restPoseObject">The rest pose humanoid object.</param>
        /// <param name="shoulderParentAdjustment">The shoulder parent joint adjustment.</param>
        /// <returns>The calculated shoulder joint adjustments.</returns>
        private static BoneAdjustmentData[] GetShoulderAdjustments(Animator animator,
            RestPoseObjectHumanoid restPoseObject, Quaternion shoulderParentAdjustment)
        {
            var shoulderHumanBodyBones = GetValidShoulderBones(animator);

            // If the dot product is opposite (-1), mirror the shoulder adjustment as the shoulders
            // are symmetric in that case.
            var leftShoulder = animator.GetBoneTransform(shoulderHumanBodyBones[0]);
            var rightShoulder = animator.GetBoneTransform(shoulderHumanBodyBones[1]);
            var shoulderDotProduct = Vector3.Dot(leftShoulder.forward, rightShoulder.forward);
            var shouldMirrorShoulderAdjustment = shoulderDotProduct < 0.0f;

            // Calculate shoulder rotations.
            var leftShoulderBone = shoulderHumanBodyBones[0];
            var rightShoulderBone = shoulderHumanBodyBones[1];
            var leftShoulderChildBone = FindChildHumanBodyBones(animator, leftShoulderBone);
            var rightShoulderChildBone = FindChildHumanBodyBones(animator, rightShoulderBone);

            var rightShoulderRotation = restPoseObject.CalculateRotationDifferenceFromRestPoseToAnimatorBonePair(
                animator, rightShoulderBone, rightShoulderChildBone,
                rightShoulderBone, rightShoulderChildBone);
            var rightShoulderAdjustment = Quaternion.Inverse(rightShoulderRotation * shoulderParentAdjustment);

            // We need to remove the Y euler angle portion, as the shoulders are rotated on OVRSkeleton in a way
            // that isn't rotated on the character, and shouldn't be represented.
            var rightShoulderAdjustmentEuler = rightShoulderAdjustment.eulerAngles;
            rightShoulderAdjustmentEuler.y = 0.0f;
            rightShoulderAdjustment = Quaternion.Euler(rightShoulderAdjustmentEuler);
            var rightShoulderData = new BoneAdjustmentData
            {
                Bone = rightShoulderBone,
                Adjustment = rightShoulderAdjustment,
                ChildBone1 = rightShoulderChildBone,
                ChildBone2 = HumanBodyBones.LastBone,
                ChildBone3 = HumanBodyBones.LastBone
            };

            var leftShoulderAdjustment = rightShoulderAdjustment;
            if (!shouldMirrorShoulderAdjustment)
            {
                var leftShoulderRotation = restPoseObject.CalculateRotationDifferenceFromRestPoseToAnimatorBonePair(
                    animator, leftShoulderBone, leftShoulderChildBone,
                    leftShoulderBone, leftShoulderChildBone);
                leftShoulderAdjustment = Quaternion.Inverse(leftShoulderRotation * shoulderParentAdjustment);
                var leftShoulderAdjustmentEuler = leftShoulderAdjustment.eulerAngles;
                leftShoulderAdjustmentEuler.y = 0.0f;
                leftShoulderAdjustment = Quaternion.Euler(leftShoulderAdjustmentEuler);
            }
            var leftShoulderData = new BoneAdjustmentData
            {
                Bone = leftShoulderBone,
                Adjustment = leftShoulderAdjustment,
                ChildBone1 = leftShoulderChildBone,
                ChildBone2 = HumanBodyBones.LastBone,
                ChildBone3 = HumanBodyBones.LastBone
            };

            return new[] { leftShoulderData, rightShoulderData };
        }

        /// <summary>
        /// Get the valid shoulder bones of the animator. Returns the UpperArm if the shoulders
        /// are invalid.
        /// </summary>
        /// <param name="animator">The animator to check.</param>
        /// <returns>The array of valid shoulder bones.</returns>
        private static HumanBodyBones[] GetValidShoulderBones(Animator animator)
        {
            return new[]
            {
                animator.GetBoneTransform(HumanBodyBones.LeftShoulder) != null ?
                    HumanBodyBones.LeftShoulder :
                    HumanBodyBones.LeftUpperArm,
                animator.GetBoneTransform(HumanBodyBones.RightShoulder) != null ?
                    HumanBodyBones.RightShoulder :
                    HumanBodyBones.RightUpperArm
            };
        }

        /// <summary>
        /// Tries to find the child HumanBodyBones according to a fixed mapping.
        /// If the bone child index is not found in the mapping, go through the bones in order.
        /// </summary>
        /// <param name="animator">The animator to check for valid bones in the mapping.</param>
        /// <param name="target">The target HumanBodyBones to find the child HumanBodyBone.</param>
        /// <param name="childIndex">The optional childIndex, if the target HumanBodyBone has multiple children.</param>
        /// <returns>HumanBodyBones corresponding to the child index of the target HumanBodyBones.</returns>
        private static HumanBodyBones FindChildHumanBodyBones(Animator animator, HumanBodyBones target, int childIndex = 0)
        {
            // Handle hips.
            if (target == HumanBodyBones.Hips)
            {
                return childIndex == 0 ? HumanBodyBones.Spine :
                    (childIndex == 1 ? HumanBodyBones.LeftUpperLeg : HumanBodyBones.RightUpperLeg);
            }

            // Handle end bones.
            if (_endChildBonesMapping.ContainsKey(target))
            {
                return _endChildBonesMapping[target];
            }

            // Handle optional spine bones.
            if (((IList)_optionalSpineBones).Contains(target))
            {
                return ValidOptionalSpineChildBone(animator, target, childIndex);
            }
            var childHumanBodyBonesIndex = Array.FindIndex(_humanBodyBonesOrder, bone => bone == target) + 1;
            return _humanBodyBonesOrder[childHumanBodyBonesIndex];
        }

        /// <summary>
        /// Find the child bone for optional spine bones.
        /// </summary>
        /// <param name="animator">The animator to check optional bones on.</param>
        /// <param name="target">The target optional spine bone.</param>
        /// <param name="childIndex">The desired child index.</param>
        /// <returns>The child bone of the optional spine bone.</returns>
        private static HumanBodyBones ValidOptionalSpineChildBone(Animator animator, HumanBodyBones target, int childIndex)
        {
            if (target == HumanBodyBones.Spine)
            {
                if (animator.GetBoneTransform(HumanBodyBones.Chest) != null)
                {
                    return HumanBodyBones.Chest;
                }
            }
            if (target == HumanBodyBones.Spine || target == HumanBodyBones.Chest)
            {
                if (animator.GetBoneTransform(HumanBodyBones.UpperChest) != null)
                {
                    return HumanBodyBones.UpperChest;
                }
            }
            return GetChestChildHumanBodyBones(animator, childIndex);
        }

        /// <summary>
        /// Returns the valid child HumanBodyBones for the chest (neck and shoulders).
        /// </summary>
        /// <param name="animator">The animator corresponding to the bones.</param>
        /// <param name="childIndex">The child index.</param>
        /// <returns>HumanBodyBones child for the chest.</returns>
        private static HumanBodyBones GetChestChildHumanBodyBones(Animator animator, int childIndex)
        {
            // This will grab neck for child index 0, left shoulder (or left upper arm) for child index 1,
            // and right shoulder (or right upper arm) for child index 2.
            return GetPrimaryFallbackBone(animator, _chestAndShoulderChildBones[childIndex],
                childIndex == 0 ? HumanBodyBones.Head : _chestAndShoulderChildBones[childIndex + 2]);
        }

        /// <summary>
        /// Returns the primary bone if valid, and the fallback bone if not valid.
        /// </summary>
        /// <param name="animator">The animator to check on.</param>
        /// <param name="primary">The primary HumanBodyBones.</param>
        /// <param name="fallback">The fallback HumanBodyBones.</param>
        /// <returns>A valid HumanBodyBones for the animator.</returns>
        private static HumanBodyBones GetPrimaryFallbackBone(Animator animator, HumanBodyBones primary, HumanBodyBones fallback)
        {
            return animator.GetBoneTransform(primary) != null ? primary : fallback;
        }

        /// <summary>
        /// When offseting a joint, limit it by stretch and squash amounts.
        /// </summary>
        /// <param name="jointTargetPos">Target joint position.</param>
        /// <param name="currentJointPosition">Current joint position.</param>
        /// <param name="squashStretchReferencePosition">Reference position to measure squash or stretch against.</param>
        /// <param name="stretchLimit">Stretch limit.</param>
        /// <param name="squashLimit">Squash limit.</param>
        /// <returns>Restricted joint position.</returns>
        public static Vector3 GetJointPositionSquashStretch(
            Vector3 jointTargetPos,
            Vector3 currentJointPosition,
            Vector3 squashStretchReferencePosition,
            float stretchLimit,
            float squashLimit)
        {
            var referenceToTarget = jointTargetPos - squashStretchReferencePosition;
            var referenceToCurrent = currentJointPosition - squashStretchReferencePosition;
            var referenceDistanceNew = referenceToTarget.magnitude;
            var referenceDistanceCurrent = referenceToCurrent.magnitude;

            bool stretchedFromReference = referenceDistanceNew > referenceDistanceCurrent;

            var finalJointPosition = Vector3.MoveTowards(
                currentJointPosition, jointTargetPos,
                stretchedFromReference ? stretchLimit : squashLimit);

            return finalJointPosition;
        }
    }
}
