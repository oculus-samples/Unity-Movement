// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Oculus.Movement.AnimationRigging
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
