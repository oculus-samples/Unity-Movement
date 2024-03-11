// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Movement.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Oculus.Movement.AnimationRigging.Deprecated
{
    /// <summary>
    /// Data types common to all deformation classes.
    /// </summary>
    public static class DeformationCommon
    {
        /// <summary>
        /// Ordered HumanBodyBones for the spine, arms, and legs.
        /// </summary>
        private static readonly HumanBodyBones[] HumanBodyBonesOrder =
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

        private static readonly Dictionary<HumanBodyBones, HumanBodyBones> EndChildBonesMapping =
            new()
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

        private static readonly HumanBodyBones[] OptionalSpineBones =
        {
            HumanBodyBones.Spine, HumanBodyBones.Chest, HumanBodyBones.UpperChest,
        };

        private static readonly HumanBodyBones[] ChestAndShoulderChildBones = new[]
        {
            HumanBodyBones.Neck,
            HumanBodyBones.LeftShoulder, HumanBodyBones.RightShoulder,
            HumanBodyBones.LeftUpperArm, HumanBodyBones.RightUpperArm
        };

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
        /// Get the valid parent HumanBodyBone of the shoulders.
        /// </summary>
        /// <param name="animator">The animator to check.</param>
        /// <returns>The valid HumanBodyBone parent of the shoulders.</returns>
        public static HumanBodyBones GetValidShoulderParentBone(Animator animator)
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
        /// Get the valid shoulder bones of the animator. Returns the UpperArm if the shoulders
        /// are invalid.
        /// </summary>
        /// <param name="animator">The animator to check.</param>
        /// <returns>The array of valid shoulder bones.</returns>
        public static HumanBodyBones[] GetValidShoulderBones(Animator animator)
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
        /// Calculate the from to rotation to align the hips right with the alignment right direction.
        /// </summary>
        /// <param name="animator">The animator.</param>
        /// <param name="alignmentRightDirection">The alignment right direction.</param>
        /// <param name="alignmentForwardDirection">The alignment forward direction.</param>
        /// <returns>Rotation to align the hips right with the alignment right direction.</returns>
        public static Quaternion GetHipsRightForwardAlignmentForAdjustments(Animator animator,
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
        public static BoneAdjustmentData[] GetSpineJointAdjustments(Animator animator,
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
                    var mappingChildBone = OVRUnityHumanoidSkeletonRetargeter.
                        OVRHumanBodyBonesMappings.BoneToJointPair[spineHumanBodyBone].Item2;
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
        public static BoneAdjustmentData[] GetShoulderAdjustments(Animator animator,
            RestPoseObjectHumanoid restPoseObject, Quaternion shoulderParentAdjustment)
        {
            var shoulderHumanBodyBones = GetValidShoulderBones(animator);

            // If the dot product is opposite (-1), mirror the shoulder adjustment as the shoulders
            // are symmetric in that case.
            var leftShoulder = animator.GetBoneTransform(shoulderHumanBodyBones[0]);
            var rightShoulder = animator.GetBoneTransform(shoulderHumanBodyBones[1]);
            var shoulderDotProduct = Vector3.Dot(leftShoulder.forward, rightShoulder.forward);
            bool shouldMirrorShoulderAdjustment = shoulderDotProduct < 0.0f;

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
        /// Tries to find the child HumanBodyBones according to a fixed mapping.
        /// If the bone child index is not found in the mapping, go through the bones in order.
        /// </summary>
        /// <param name="animator">The animator to check for valid bones in the mapping.</param>
        /// <param name="target">The target HumanBodyBones to find the child HumanBodyBone.</param>
        /// <param name="childIndex">The optional childIndex, if the target HumanBodyBone has multiple children.</param>
        /// <returns>HumanBodyBones corresponding to the child index of the target HumanBodyBones.</returns>
        public static HumanBodyBones FindChildHumanBodyBones(Animator animator, HumanBodyBones target, int childIndex = 0)
        {
            // Handle hips.
            if (target == HumanBodyBones.Hips)
            {
                return childIndex == 0 ? HumanBodyBones.Spine :
                    (childIndex == 1 ? HumanBodyBones.LeftUpperLeg : HumanBodyBones.RightUpperLeg);
            }

            // Handle end bones.
            if (EndChildBonesMapping.ContainsKey(target))
            {
                return EndChildBonesMapping[target];
            }

            // Handle optional spine bones.
            if (OptionalSpineBones.Contains(target))
            {
                return ValidOptionalSpineChildBone(animator, target, childIndex);
            }
            var childHumanBodyBonesIndex = Array.FindIndex(HumanBodyBonesOrder, bone => bone == target) + 1;
            return HumanBodyBonesOrder[childHumanBodyBonesIndex];
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
            return GetPrimaryFallbackBone(animator, ChestAndShoulderChildBones[childIndex],
                childIndex == 0 ? HumanBodyBones.Head : ChestAndShoulderChildBones[childIndex + 2]);
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
    }
}
