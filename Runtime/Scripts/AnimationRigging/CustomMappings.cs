// Copyright (c) Meta Platforms, Inc. and affiliates.

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
    }
}
