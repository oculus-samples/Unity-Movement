// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// Contains extra mappins not yet available in the SDK.
    /// </summary>
    public class BoneMappingsExtension
    {
        /// <summary>
        /// Maps HumanBodyBones to avatar mask body part. Includes legs as well.
        /// </summary>
        public static readonly Dictionary<HumanBodyBones, AvatarMaskBodyPart>
            HumanBoneToAvatarBodyPart = new Dictionary<HumanBodyBones, AvatarMaskBodyPart>()
            {
                { HumanBodyBones.Neck, AvatarMaskBodyPart.Head },

                { HumanBodyBones.Head, AvatarMaskBodyPart.Head },
                { HumanBodyBones.LeftEye, AvatarMaskBodyPart.Head },
                { HumanBodyBones.RightEye, AvatarMaskBodyPart.Head },
                { HumanBodyBones.Jaw, AvatarMaskBodyPart.Head },

                { HumanBodyBones.Hips, AvatarMaskBodyPart.Body },

                { HumanBodyBones.Spine, AvatarMaskBodyPart.Body },
                { HumanBodyBones.Chest, AvatarMaskBodyPart.Body },
                { HumanBodyBones.UpperChest, AvatarMaskBodyPart.Body },

                { HumanBodyBones.RightShoulder, AvatarMaskBodyPart.RightArm },
                { HumanBodyBones.RightUpperArm, AvatarMaskBodyPart.RightArm },
                { HumanBodyBones.RightLowerArm, AvatarMaskBodyPart.RightArm },
                { HumanBodyBones.RightHand, AvatarMaskBodyPart.RightArm },

                { HumanBodyBones.LeftShoulder, AvatarMaskBodyPart.LeftArm },
                { HumanBodyBones.LeftUpperArm, AvatarMaskBodyPart.LeftArm },
                { HumanBodyBones.LeftLowerArm, AvatarMaskBodyPart.LeftArm },
                { HumanBodyBones.LeftHand, AvatarMaskBodyPart.LeftArm },

                { HumanBodyBones.LeftUpperLeg, AvatarMaskBodyPart.LeftLeg },
                { HumanBodyBones.LeftLowerLeg, AvatarMaskBodyPart.LeftLeg },

                { HumanBodyBones.LeftFoot, AvatarMaskBodyPart.LeftLeg },
                { HumanBodyBones.LeftToes, AvatarMaskBodyPart.LeftLeg },

                { HumanBodyBones.RightUpperLeg, AvatarMaskBodyPart.RightLeg },
                { HumanBodyBones.RightLowerLeg, AvatarMaskBodyPart.RightLeg },

                { HumanBodyBones.RightFoot, AvatarMaskBodyPart.RightLeg },
                { HumanBodyBones.RightToes, AvatarMaskBodyPart.RightLeg },

                { HumanBodyBones.LeftThumbProximal, AvatarMaskBodyPart.LeftArm },
                { HumanBodyBones.LeftThumbIntermediate, AvatarMaskBodyPart.LeftArm },
                { HumanBodyBones.LeftThumbDistal, AvatarMaskBodyPart.LeftArm },
                { HumanBodyBones.LeftIndexProximal, AvatarMaskBodyPart.LeftArm },
                { HumanBodyBones.LeftIndexIntermediate, AvatarMaskBodyPart.LeftArm },
                { HumanBodyBones.LeftIndexDistal, AvatarMaskBodyPart.LeftArm },
                { HumanBodyBones.LeftMiddleProximal, AvatarMaskBodyPart.LeftArm },
                { HumanBodyBones.LeftMiddleIntermediate, AvatarMaskBodyPart.LeftArm },
                { HumanBodyBones.LeftMiddleDistal, AvatarMaskBodyPart.LeftArm },
                { HumanBodyBones.LeftRingProximal, AvatarMaskBodyPart.LeftArm },
                { HumanBodyBones.LeftRingIntermediate, AvatarMaskBodyPart.LeftArm },
                { HumanBodyBones.LeftRingDistal, AvatarMaskBodyPart.LeftArm },
                { HumanBodyBones.LeftLittleProximal, AvatarMaskBodyPart.LeftArm },
                { HumanBodyBones.LeftLittleIntermediate, AvatarMaskBodyPart.LeftArm },
                { HumanBodyBones.LeftLittleDistal, AvatarMaskBodyPart.LeftArm },

                { HumanBodyBones.RightThumbProximal, AvatarMaskBodyPart.RightArm },
                { HumanBodyBones.RightThumbIntermediate, AvatarMaskBodyPart.RightArm },
                { HumanBodyBones.RightThumbDistal, AvatarMaskBodyPart.RightArm },
                { HumanBodyBones.RightIndexProximal, AvatarMaskBodyPart.RightArm },
                { HumanBodyBones.RightIndexIntermediate, AvatarMaskBodyPart.RightArm },
                { HumanBodyBones.RightIndexDistal, AvatarMaskBodyPart.RightArm },
                { HumanBodyBones.RightMiddleProximal, AvatarMaskBodyPart.RightArm },
                { HumanBodyBones.RightMiddleIntermediate, AvatarMaskBodyPart.RightArm },
                { HumanBodyBones.RightMiddleDistal, AvatarMaskBodyPart.RightArm },
                { HumanBodyBones.RightRingProximal, AvatarMaskBodyPart.RightArm },
                { HumanBodyBones.RightRingIntermediate, AvatarMaskBodyPart.RightArm },
                { HumanBodyBones.RightRingDistal, AvatarMaskBodyPart.RightArm },
                { HumanBodyBones.RightLittleProximal, AvatarMaskBodyPart.RightArm },
                { HumanBodyBones.RightLittleIntermediate, AvatarMaskBodyPart.RightArm },
                { HumanBodyBones.RightLittleDistal, AvatarMaskBodyPart.RightArm }
            };

        /// <summary>
        /// Maps HumanBodyBones to avatar mask body part. Includes legs as well.
        /// </summary>
        public static readonly AvatarMaskBodyPart[] HumanBoneToAvatarBodyPartArray = new AvatarMaskBodyPart[]
            {
                AvatarMaskBodyPart.Body, // Hips
                AvatarMaskBodyPart.LeftLeg, // LeftUpperLeg
                AvatarMaskBodyPart.RightLeg, // RightUpperLeg
                AvatarMaskBodyPart.LeftLeg, // LeftLowerLeg
                AvatarMaskBodyPart.RightLeg, // RightLowerLeg
                AvatarMaskBodyPart.LeftLeg, // LeftFoot
                AvatarMaskBodyPart.RightLeg, // RightFoot
                AvatarMaskBodyPart.Body, // Spine
                AvatarMaskBodyPart.Body, // Chest
                AvatarMaskBodyPart.Head, // Neck
                AvatarMaskBodyPart.Head, // Head
                AvatarMaskBodyPart.LeftArm, // LeftShoulder
                AvatarMaskBodyPart.RightArm, // RightShoulder
                AvatarMaskBodyPart.LeftArm, // LeftUpperArm
                AvatarMaskBodyPart.RightArm, // RightUpperArm
                AvatarMaskBodyPart.LeftArm, // LeftLowerArm
                AvatarMaskBodyPart.RightArm, // RightLowerArm
                AvatarMaskBodyPart.LeftArm, // LeftHand
                AvatarMaskBodyPart.RightArm, // RightHand
                AvatarMaskBodyPart.LeftLeg, // LeftToes
                AvatarMaskBodyPart.RightLeg, // RightToes
                AvatarMaskBodyPart.Head, // LeftEye
                AvatarMaskBodyPart.Head, // RightEye
                AvatarMaskBodyPart.Head, // Jaw
                AvatarMaskBodyPart.LeftArm, // Left Hand Fingers
                AvatarMaskBodyPart.LeftArm,
                AvatarMaskBodyPart.LeftArm,
                AvatarMaskBodyPart.LeftArm,
                AvatarMaskBodyPart.LeftArm,
                AvatarMaskBodyPart.LeftArm,
                AvatarMaskBodyPart.LeftArm,
                AvatarMaskBodyPart.LeftArm,
                AvatarMaskBodyPart.LeftArm,
                AvatarMaskBodyPart.LeftArm,
                AvatarMaskBodyPart.LeftArm,
                AvatarMaskBodyPart.LeftArm,
                AvatarMaskBodyPart.LeftArm,
                AvatarMaskBodyPart.LeftArm,
                AvatarMaskBodyPart.LeftArm,
                AvatarMaskBodyPart.RightArm, // Right Hand Fingers
                AvatarMaskBodyPart.RightArm,
                AvatarMaskBodyPart.RightArm,
                AvatarMaskBodyPart.RightArm,
                AvatarMaskBodyPart.RightArm,
                AvatarMaskBodyPart.RightArm,
                AvatarMaskBodyPart.RightArm,
                AvatarMaskBodyPart.RightArm,
                AvatarMaskBodyPart.RightArm,
                AvatarMaskBodyPart.RightArm,
                AvatarMaskBodyPart.RightArm,
                AvatarMaskBodyPart.RightArm,
                AvatarMaskBodyPart.RightArm,
                AvatarMaskBodyPart.RightArm,
                AvatarMaskBodyPart.RightArm,
                AvatarMaskBodyPart.Body, // UpperChest
            };

        /// <summary>
        /// Maps OVRSkeletonBoneId to avatar mask body part.
        /// </summary>
        public static readonly Dictionary<OVRSkeleton.BoneId, AvatarMaskBodyPart>
            OVRSkeletonBoneIdToAvatarBodyPart = new Dictionary<OVRSkeleton.BoneId, AvatarMaskBodyPart>()
            {
                { OVRSkeleton.BoneId.Body_Neck, AvatarMaskBodyPart.Head },
                { OVRSkeleton.BoneId.Body_Head, AvatarMaskBodyPart.Head },

                { OVRSkeleton.BoneId.Body_Root, AvatarMaskBodyPart.Body },
                { OVRSkeleton.BoneId.Body_Hips, AvatarMaskBodyPart.Body },
                { OVRSkeleton.BoneId.Body_SpineLower, AvatarMaskBodyPart.Body },
                { OVRSkeleton.BoneId.Body_SpineMiddle, AvatarMaskBodyPart.Body },
                { OVRSkeleton.BoneId.Body_SpineUpper, AvatarMaskBodyPart.Body },
                { OVRSkeleton.BoneId.Body_Chest, AvatarMaskBodyPart.Body },

                { OVRSkeleton.BoneId.Body_RightShoulder, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.Body_RightScapula, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.Body_RightArmUpper, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.Body_RightArmLower, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.Body_RightHandWristTwist, AvatarMaskBodyPart.RightArm },

                { OVRSkeleton.BoneId.Body_LeftShoulder, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.Body_LeftScapula, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.Body_LeftArmUpper, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.Body_LeftArmLower, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.Body_LeftHandWristTwist, AvatarMaskBodyPart.LeftArm },

                { OVRSkeleton.BoneId.Body_LeftHandPalm, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.Body_LeftHandWrist, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.Body_LeftHandThumbMetacarpal, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.Body_LeftHandThumbProximal, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.Body_LeftHandThumbDistal, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.Body_LeftHandThumbTip, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.Body_LeftHandIndexMetacarpal, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.Body_LeftHandIndexProximal, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.Body_LeftHandIndexIntermediate, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.Body_LeftHandIndexDistal, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.Body_LeftHandIndexTip, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.Body_LeftHandMiddleMetacarpal, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.Body_LeftHandMiddleProximal, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.Body_LeftHandMiddleIntermediate, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.Body_LeftHandMiddleDistal, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.Body_LeftHandMiddleTip, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.Body_LeftHandRingMetacarpal, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.Body_LeftHandRingProximal, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.Body_LeftHandRingIntermediate, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.Body_LeftHandRingDistal, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.Body_LeftHandRingTip, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.Body_LeftHandLittleMetacarpal, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.Body_LeftHandLittleProximal, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.Body_LeftHandLittleIntermediate, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.Body_LeftHandLittleDistal, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.Body_LeftHandLittleTip, AvatarMaskBodyPart.LeftArm },

                { OVRSkeleton.BoneId.Body_RightHandPalm, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.Body_RightHandWrist, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.Body_RightHandThumbMetacarpal, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.Body_RightHandThumbProximal, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.Body_RightHandThumbDistal, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.Body_RightHandThumbTip, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.Body_RightHandIndexMetacarpal, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.Body_RightHandIndexProximal, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.Body_RightHandIndexIntermediate, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.Body_RightHandIndexDistal, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.Body_RightHandIndexTip, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.Body_RightHandMiddleMetacarpal, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.Body_RightHandMiddleProximal, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.Body_RightHandMiddleIntermediate, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.Body_RightHandMiddleDistal, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.Body_RightHandMiddleTip, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.Body_RightHandRingMetacarpal, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.Body_RightHandRingProximal, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.Body_RightHandRingIntermediate, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.Body_RightHandRingDistal, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.Body_RightHandRingTip, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.Body_RightHandLittleMetacarpal, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.Body_RightHandLittleProximal, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.Body_RightHandLittleIntermediate, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.Body_RightHandLittleDistal, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.Body_RightHandLittleTip, AvatarMaskBodyPart.RightArm },
            };

        /// <summary>
        /// Maps OVRSkeleton.BoneId.FullBody* to avatar mask body part.
        /// </summary>
        public static readonly Dictionary<OVRSkeleton.BoneId, AvatarMaskBodyPart>
            OVRSkeletonFullBodyBoneIdToAvatarBodyPart = new Dictionary<OVRSkeleton.BoneId, AvatarMaskBodyPart>()
            {
                { OVRSkeleton.BoneId.FullBody_Start, AvatarMaskBodyPart.Root },
                { OVRSkeleton.BoneId.FullBody_Hips, AvatarMaskBodyPart.Body },
                { OVRSkeleton.BoneId.FullBody_SpineLower, AvatarMaskBodyPart.Body },
                { OVRSkeleton.BoneId.FullBody_SpineMiddle, AvatarMaskBodyPart.Body },
                { OVRSkeleton.BoneId.FullBody_SpineUpper, AvatarMaskBodyPart.Body },
                { OVRSkeleton.BoneId.FullBody_Chest, AvatarMaskBodyPart.Body },
                { OVRSkeleton.BoneId.FullBody_Neck, AvatarMaskBodyPart.Head },
                { OVRSkeleton.BoneId.FullBody_Head, AvatarMaskBodyPart.Head },

                { OVRSkeleton.BoneId.FullBody_LeftShoulder, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.FullBody_LeftScapula, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.FullBody_LeftArmUpper, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.FullBody_LeftArmLower, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.FullBody_LeftHandWristTwist, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.FullBody_LeftHandPalm, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.FullBody_LeftHandWrist, AvatarMaskBodyPart.LeftArm },

                { OVRSkeleton.BoneId.FullBody_RightShoulder, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.FullBody_RightScapula, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.FullBody_RightArmUpper, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.FullBody_RightArmLower, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.FullBody_RightHandWristTwist, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.FullBody_RightHandPalm, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.FullBody_RightHandWrist, AvatarMaskBodyPart.RightArm },

                { OVRSkeleton.BoneId.FullBody_LeftHandThumbMetacarpal, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.FullBody_LeftHandThumbProximal, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.FullBody_LeftHandThumbDistal, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.FullBody_LeftHandThumbTip, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.FullBody_LeftHandIndexMetacarpal, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.FullBody_LeftHandIndexProximal, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.FullBody_LeftHandIndexIntermediate, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.FullBody_LeftHandIndexDistal, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.FullBody_LeftHandIndexTip, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.FullBody_LeftHandMiddleMetacarpal, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.FullBody_LeftHandMiddleProximal, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.FullBody_LeftHandMiddleIntermediate, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.FullBody_LeftHandMiddleDistal, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.FullBody_LeftHandMiddleTip, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.FullBody_LeftHandRingMetacarpal, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.FullBody_LeftHandRingProximal, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.FullBody_LeftHandRingIntermediate, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.FullBody_LeftHandRingDistal, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.FullBody_LeftHandRingTip, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.FullBody_LeftHandLittleMetacarpal, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.FullBody_LeftHandLittleProximal, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.FullBody_LeftHandLittleIntermediate, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.FullBody_LeftHandLittleDistal, AvatarMaskBodyPart.LeftArm },
                { OVRSkeleton.BoneId.FullBody_LeftHandLittleTip, AvatarMaskBodyPart.LeftArm },

                { OVRSkeleton.BoneId.FullBody_RightHandThumbMetacarpal, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.FullBody_RightHandThumbProximal, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.FullBody_RightHandThumbDistal, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.FullBody_RightHandThumbTip, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.FullBody_RightHandIndexMetacarpal, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.FullBody_RightHandIndexProximal, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.FullBody_RightHandIndexIntermediate, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.FullBody_RightHandIndexDistal, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.FullBody_RightHandIndexTip, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.FullBody_RightHandMiddleMetacarpal, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.FullBody_RightHandMiddleProximal, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.FullBody_RightHandMiddleIntermediate, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.FullBody_RightHandMiddleDistal, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.FullBody_RightHandMiddleTip, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.FullBody_RightHandRingMetacarpal, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.FullBody_RightHandRingProximal, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.FullBody_RightHandRingIntermediate, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.FullBody_RightHandRingDistal, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.FullBody_RightHandRingTip, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.FullBody_RightHandLittleMetacarpal, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.FullBody_RightHandLittleProximal, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.FullBody_RightHandLittleIntermediate, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.FullBody_RightHandLittleDistal, AvatarMaskBodyPart.RightArm },
                { OVRSkeleton.BoneId.FullBody_RightHandLittleTip, AvatarMaskBodyPart.RightArm },

                { OVRSkeleton.BoneId.FullBody_LeftUpperLeg, AvatarMaskBodyPart.LeftLeg },
                { OVRSkeleton.BoneId.FullBody_LeftLowerLeg, AvatarMaskBodyPart.LeftLeg },
                { OVRSkeleton.BoneId.FullBody_LeftFootAnkleTwist, AvatarMaskBodyPart.LeftLeg },
                { OVRSkeleton.BoneId.FullBody_LeftFootAnkle, AvatarMaskBodyPart.LeftLeg },
                { OVRSkeleton.BoneId.FullBody_LeftFootSubtalar, AvatarMaskBodyPart.LeftLeg },
                { OVRSkeleton.BoneId.FullBody_LeftFootTransverse, AvatarMaskBodyPart.LeftLeg },
                { OVRSkeleton.BoneId.FullBody_LeftFootBall, AvatarMaskBodyPart.LeftLeg },

                { OVRSkeleton.BoneId.FullBody_RightUpperLeg, AvatarMaskBodyPart.RightLeg },
                { OVRSkeleton.BoneId.FullBody_RightLowerLeg, AvatarMaskBodyPart.RightLeg },
                { OVRSkeleton.BoneId.FullBody_RightFootAnkleTwist, AvatarMaskBodyPart.RightLeg },
                { OVRSkeleton.BoneId.FullBody_RightFootAnkle, AvatarMaskBodyPart.RightLeg },
                { OVRSkeleton.BoneId.FullBody_RightFootSubtalar, AvatarMaskBodyPart.RightLeg },
                { OVRSkeleton.BoneId.FullBody_RightFootTransverse, AvatarMaskBodyPart.RightLeg },
                { OVRSkeleton.BoneId.FullBody_RightFootBall, AvatarMaskBodyPart.RightLeg },
            };
    }
}
