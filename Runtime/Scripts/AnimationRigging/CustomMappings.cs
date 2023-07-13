// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Movement.AnimationRigging
{
    public class CustomMappings
    {
        /// <summary>
        /// Paired OVRSkeleton bones with human body bones.
        /// Copied from OVRUnityHumanoidSkeletonRetargeter.OVRHumanBodyBonesMappings.
        /// The original mapping is not currently accessible by other classes.
        /// </summary>
        public static readonly Dictionary<OVRSkeleton.BoneId, HumanBodyBones> BoneIdToHumanBodyBone =
            new Dictionary<OVRSkeleton.BoneId, HumanBodyBones>()
            {
                { OVRSkeleton.BoneId.Body_Hips, HumanBodyBones.Hips },
                { OVRSkeleton.BoneId.Body_SpineLower, HumanBodyBones.Spine },
                { OVRSkeleton.BoneId.Body_SpineUpper, HumanBodyBones.Chest },
                { OVRSkeleton.BoneId.Body_Chest, HumanBodyBones.UpperChest },
                { OVRSkeleton.BoneId.Body_Neck, HumanBodyBones.Neck },
                { OVRSkeleton.BoneId.Body_Head, HumanBodyBones.Head },

                { OVRSkeleton.BoneId.Body_LeftShoulder, HumanBodyBones.LeftShoulder },
                { OVRSkeleton.BoneId.Body_LeftArmUpper, HumanBodyBones.LeftUpperArm },
                { OVRSkeleton.BoneId.Body_LeftArmLower, HumanBodyBones.LeftLowerArm },
                { OVRSkeleton.BoneId.Body_LeftHandWrist, HumanBodyBones.LeftHand },

                { OVRSkeleton.BoneId.Body_RightShoulder, HumanBodyBones.RightShoulder },
                { OVRSkeleton.BoneId.Body_RightArmUpper, HumanBodyBones.RightUpperArm },
                { OVRSkeleton.BoneId.Body_RightArmLower, HumanBodyBones.RightLowerArm },
                { OVRSkeleton.BoneId.Body_RightHandWrist, HumanBodyBones.RightHand },

                { OVRSkeleton.BoneId.Body_LeftHandThumbMetacarpal, HumanBodyBones.LeftThumbProximal },
                { OVRSkeleton.BoneId.Body_LeftHandThumbProximal, HumanBodyBones.LeftThumbIntermediate },
                { OVRSkeleton.BoneId.Body_LeftHandThumbDistal, HumanBodyBones.LeftThumbDistal },
                { OVRSkeleton.BoneId.Body_LeftHandIndexProximal, HumanBodyBones.LeftIndexProximal },
                { OVRSkeleton.BoneId.Body_LeftHandIndexIntermediate, HumanBodyBones.LeftIndexIntermediate },
                { OVRSkeleton.BoneId.Body_LeftHandIndexDistal, HumanBodyBones.LeftIndexDistal },
                { OVRSkeleton.BoneId.Body_LeftHandMiddleProximal, HumanBodyBones.LeftMiddleProximal },
                { OVRSkeleton.BoneId.Body_LeftHandMiddleIntermediate, HumanBodyBones.LeftMiddleIntermediate },
                { OVRSkeleton.BoneId.Body_LeftHandMiddleDistal, HumanBodyBones.LeftMiddleDistal },
                { OVRSkeleton.BoneId.Body_LeftHandRingProximal, HumanBodyBones.LeftRingProximal },
                { OVRSkeleton.BoneId.Body_LeftHandRingIntermediate, HumanBodyBones.LeftRingIntermediate },
                { OVRSkeleton.BoneId.Body_LeftHandRingDistal, HumanBodyBones.LeftRingDistal },
                { OVRSkeleton.BoneId.Body_LeftHandLittleProximal, HumanBodyBones.LeftLittleProximal },
                { OVRSkeleton.BoneId.Body_LeftHandLittleIntermediate, HumanBodyBones.LeftLittleIntermediate },
                { OVRSkeleton.BoneId.Body_LeftHandLittleDistal, HumanBodyBones.LeftLittleDistal },

                { OVRSkeleton.BoneId.Body_RightHandThumbMetacarpal, HumanBodyBones.RightThumbProximal },
                { OVRSkeleton.BoneId.Body_RightHandThumbProximal, HumanBodyBones.RightThumbIntermediate },
                { OVRSkeleton.BoneId.Body_RightHandThumbDistal, HumanBodyBones.RightThumbDistal },
                { OVRSkeleton.BoneId.Body_RightHandIndexProximal, HumanBodyBones.RightIndexProximal },
                { OVRSkeleton.BoneId.Body_RightHandIndexIntermediate, HumanBodyBones.RightIndexIntermediate },
                { OVRSkeleton.BoneId.Body_RightHandIndexDistal, HumanBodyBones.RightIndexDistal },
                { OVRSkeleton.BoneId.Body_RightHandMiddleProximal, HumanBodyBones.RightMiddleProximal },
                { OVRSkeleton.BoneId.Body_RightHandMiddleIntermediate, HumanBodyBones.RightMiddleIntermediate },
                { OVRSkeleton.BoneId.Body_RightHandMiddleDistal, HumanBodyBones.RightMiddleDistal },
                { OVRSkeleton.BoneId.Body_RightHandRingProximal, HumanBodyBones.RightRingProximal },
                { OVRSkeleton.BoneId.Body_RightHandRingIntermediate, HumanBodyBones.RightRingIntermediate },
                { OVRSkeleton.BoneId.Body_RightHandRingDistal, HumanBodyBones.RightRingDistal },
                { OVRSkeleton.BoneId.Body_RightHandLittleProximal, HumanBodyBones.RightLittleProximal },
                { OVRSkeleton.BoneId.Body_RightHandLittleIntermediate, HumanBodyBones.RightLittleIntermediate },
                { OVRSkeleton.BoneId.Body_RightHandLittleDistal, HumanBodyBones.RightLittleDistal },
            };

        /// <summary>
        /// For each humanoid bone, create a pair that determines the
        /// pair of bones that create the joint pair. Used to
        /// create the "axis" of the bone.
        /// </summary>
        public static readonly Dictionary<HumanBodyBones, Tuple<HumanBodyBones, HumanBodyBones>>
            BoneToJointPair = new Dictionary<HumanBodyBones, Tuple<HumanBodyBones, HumanBodyBones>>()
            {
                {
                    HumanBodyBones.Neck,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.Neck, HumanBodyBones.Head)
                },
                {
                    HumanBodyBones.Head,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.Neck, HumanBodyBones.Head)
                },
                {
                    HumanBodyBones.LeftEye,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.Head, HumanBodyBones.LeftEye)
                },
                {
                    HumanBodyBones.RightEye,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.Head, HumanBodyBones.RightEye)
                },
                {
                    HumanBodyBones.Jaw,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.Head, HumanBodyBones.Jaw)
                },

                {
                    HumanBodyBones.Hips,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.Hips, HumanBodyBones.Spine)
                },
                {
                    HumanBodyBones.Spine,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.Spine, HumanBodyBones.Chest)
                },
                {
                    HumanBodyBones.Chest,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.Chest, HumanBodyBones.UpperChest)
                },
                {
                    HumanBodyBones.UpperChest,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.UpperChest, HumanBodyBones.Neck)
                },

                {
                    HumanBodyBones.LeftShoulder,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.LeftShoulder, HumanBodyBones.LeftUpperArm)
                },
                {
                    HumanBodyBones.LeftUpperArm,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm)
                },
                {
                    HumanBodyBones.LeftLowerArm,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand)
                },
                {
                    HumanBodyBones.LeftHand,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.LeftHand,
                        HumanBodyBones.LeftMiddleProximal)
                },

                {
                    HumanBodyBones.RightShoulder,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.RightShoulder,
                        HumanBodyBones.RightUpperArm)
                },
                {
                    HumanBodyBones.RightUpperArm,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.RightUpperArm,
                        HumanBodyBones.RightLowerArm)
                },
                {
                    HumanBodyBones.RightLowerArm,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand)
                },
                {
                    HumanBodyBones.RightHand,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.RightHand,
                        HumanBodyBones.RightMiddleProximal)
                },

                {
                    HumanBodyBones.RightUpperLeg,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.RightUpperLeg,
                        HumanBodyBones.RightLowerLeg)
                },
                {
                    HumanBodyBones.RightLowerLeg,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot)
                },
                {
                    HumanBodyBones.RightFoot,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.RightFoot, HumanBodyBones.RightToes)
                },
                {
                    HumanBodyBones.RightToes,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.RightFoot, HumanBodyBones.RightToes)
                },

                {
                    HumanBodyBones.LeftUpperLeg,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg)
                },
                {
                    HumanBodyBones.LeftLowerLeg,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot)
                },
                {
                    HumanBodyBones.LeftFoot,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.LeftFoot, HumanBodyBones.LeftToes)
                },
                {
                    HumanBodyBones.LeftToes,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.LeftFoot, HumanBodyBones.LeftToes)
                },

                {
                    HumanBodyBones.LeftThumbProximal,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.LeftThumbProximal,
                        HumanBodyBones.LeftThumbIntermediate)
                },
                {
                    HumanBodyBones.LeftThumbIntermediate,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.LeftThumbIntermediate,
                        HumanBodyBones.LeftThumbDistal)
                },
                {
                    HumanBodyBones.LeftThumbDistal,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.LeftThumbDistal, HumanBodyBones.LastBone)
                }, // Invalid.

                {
                    HumanBodyBones.LeftIndexProximal,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.LeftIndexProximal,
                        HumanBodyBones.LeftIndexIntermediate)
                },
                {
                    HumanBodyBones.LeftIndexIntermediate,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.LeftIndexIntermediate,
                        HumanBodyBones.LeftIndexDistal)
                },
                {
                    HumanBodyBones.LeftIndexDistal,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.LeftIndexDistal, HumanBodyBones.LastBone)
                }, // Invalid.

                {
                    HumanBodyBones.LeftMiddleProximal,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.LeftMiddleProximal,
                        HumanBodyBones.LeftMiddleIntermediate)
                },
                {
                    HumanBodyBones.LeftMiddleIntermediate,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.LeftMiddleIntermediate,
                        HumanBodyBones.LeftMiddleDistal)
                },
                {
                    HumanBodyBones.LeftMiddleDistal,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.LeftMiddleDistal, HumanBodyBones.LastBone)
                }, // Invalid.

                {
                    HumanBodyBones.LeftRingProximal,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.LeftRingProximal,
                        HumanBodyBones.LeftRingIntermediate)
                },
                {
                    HumanBodyBones.LeftRingIntermediate,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.LeftRingIntermediate,
                        HumanBodyBones.LeftRingDistal)
                },
                {
                    HumanBodyBones.LeftRingDistal,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.LeftRingDistal, HumanBodyBones.LastBone)
                }, // Invalid.

                {
                    HumanBodyBones.LeftLittleProximal,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.LeftLittleProximal,
                        HumanBodyBones.LeftLittleIntermediate)
                },
                {
                    HumanBodyBones.LeftLittleIntermediate,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.LeftLittleIntermediate,
                        HumanBodyBones.LeftLittleDistal)
                },
                {
                    HumanBodyBones.LeftLittleDistal,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.LeftLittleDistal, HumanBodyBones.LastBone)
                }, // Invalid.

                {
                    HumanBodyBones.RightThumbProximal,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.RightThumbProximal,
                        HumanBodyBones.RightThumbIntermediate)
                },
                {
                    HumanBodyBones.RightThumbIntermediate,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.RightThumbIntermediate,
                        HumanBodyBones.RightThumbDistal)
                },
                {
                    HumanBodyBones.RightThumbDistal,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.RightThumbDistal, HumanBodyBones.LastBone)
                }, // Invalid.

                {
                    HumanBodyBones.RightIndexProximal,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.RightIndexProximal,
                        HumanBodyBones.RightIndexIntermediate)
                },
                {
                    HumanBodyBones.RightIndexIntermediate,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.RightIndexIntermediate,
                        HumanBodyBones.RightIndexDistal)
                },
                {
                    HumanBodyBones.RightIndexDistal,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.RightIndexDistal, HumanBodyBones.LastBone)
                }, // Invalid.

                {
                    HumanBodyBones.RightMiddleProximal,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.RightMiddleProximal,
                        HumanBodyBones.RightMiddleIntermediate)
                },
                {
                    HumanBodyBones.RightMiddleIntermediate,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.RightMiddleIntermediate,
                        HumanBodyBones.RightMiddleDistal)
                },
                {
                    HumanBodyBones.RightMiddleDistal,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.RightMiddleDistal, HumanBodyBones.LastBone)
                }, // Invalid.

                {
                    HumanBodyBones.RightRingProximal,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.RightRingProximal,
                        HumanBodyBones.RightRingIntermediate)
                },
                {
                    HumanBodyBones.RightRingIntermediate,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.RightRingIntermediate,
                        HumanBodyBones.RightRingDistal)
                },
                {
                    HumanBodyBones.RightRingDistal,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.RightRingDistal, HumanBodyBones.LastBone)
                }, // Invalid.

                {
                    HumanBodyBones.RightLittleProximal,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.RightLittleProximal,
                        HumanBodyBones.RightLittleIntermediate)
                },
                {
                    HumanBodyBones.RightLittleIntermediate,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.RightLittleIntermediate,
                        HumanBodyBones.RightLittleDistal)
                },
                {
                    HumanBodyBones.RightLittleDistal,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.RightLittleDistal, HumanBodyBones.LastBone)
                }, // Invalid.
            };

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
    }
}
