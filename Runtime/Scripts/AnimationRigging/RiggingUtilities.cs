// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using UnityEngine;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// Provides convenient methods and occasional extensions to assist
    /// with animation rigging work.
    /// </summary>
    public static class RiggingUtilities
    {
        /// <summary>
        /// Find bone transform from <see cref="OVRCustomSkeleton"/> assuming
        /// it has a transform corresponding to <see cref="OVRSkeleton.BoneId"/>.
        /// </summary>
        /// <param name="skeleton"><see cref="OVRCustomSkeleton"/> to query.</param>
        /// <param name="boneId"><see cref="OVRSkeleton.BoneId"/> of transform to find.</param>
        /// <returns></returns>
        public static Transform FindBoneTransformFromCustomSkeleton(
            OVRCustomSkeleton skeleton,
            OVRSkeleton.BoneId boneId)
        {
            return skeleton.CustomBones[(int)boneId];
        }

        /// <summary>
        /// Find bone transform from <see cref="OVRSkeleton"/> assuming
        /// it has a transform corresponding to <see cref="OVRSkeleton.BoneId"/>.
        /// </summary>
        /// <param name="skeleton"><see cref="OVRSkeleton"/> to query.</param>
        /// <param name="boneId"><see cref="OVRSkeleton.BoneId"/> of transform to find.</param>
        /// <param name="isBindPose">If bone is obtained via bind pose.</param>
        /// <returns>Bone transform.</returns>
        public static Transform FindBoneTransformFromSkeleton(
            OVRSkeleton skeleton,
            OVRSkeleton.BoneId boneId,
            bool isBindPose = false)
        {
            if (!skeleton.IsInitialized ||
                !skeleton.IsDataValid)
            {
                return null;
            }

            var bones = isBindPose ? skeleton.BindPoses : skeleton.Bones;
            for (int boneIndex = 0; boneIndex < bones.Count; boneIndex++)
            {
                if (bones[boneIndex].Id == boneId)
                {
                    return bones[boneIndex].Transform;
                }
            }
            return null;
        }

        /// <summary>
        /// Find bone transform from <see cref="Animator"/> based on
        /// <see cref="OVRSkeleton.BoneId"/>.
        /// </summary>
        /// <param name="animator"><see cref="Animator"/> to query.</param>
        /// <param name="boneId"><see cref="OVRSkeleton.BoneId"/> of transform to find.</param>
        /// <returns></returns>
        public static Transform FindBoneTransformAnimator(
            Animator animator,
            OVRSkeleton.BoneId boneId)
        {
            if (!CustomMappings.BoneIdToHumanBodyBone.ContainsKey(boneId))
            {
                return null;
            }
            return animator.GetBoneTransform(CustomMappings.BoneIdToHumanBodyBone[boneId]);
        }
    }

    public static class AvatarMaskExtensionMethods
    {
        public static AvatarMaskBodyPart[] AvatarMaskBodyParts =
            (AvatarMaskBodyPart[])Enum.GetValues(typeof(AvatarMaskBodyPart));

        /// <summary>
        /// Sets every <see cref="AvatarMaskBodyPart"/> of <see cref="AvatarMask"/>
        /// to a specified active value.
        /// </summary>
        /// <param name="avatarMask"><see cref="AvatarMask"/> under consideration.</param>
        /// <param name="defaultActiveValue">Active value to set.</param>
        public static void InitializeDefaultValues(this AvatarMask avatarMask, bool defaultActiveValue)
        {
            foreach (AvatarMaskBodyPart maskBodyPart in AvatarMaskBodyParts)
            {
                if (maskBodyPart == AvatarMaskBodyPart.LastBodyPart)
                {
                    continue;
                }
                avatarMask.SetHumanoidBodyPartActive(maskBodyPart, defaultActiveValue);
            }
        }

        /// <summary>
        /// Copies all <see cref="AvatarMaskBodyPart"/> active values from another
        /// <see cref="AvatarMask"/> onto the current one.
        /// </summary>
        /// <param name="avatarMask"><see cref="AvatarMask"/> to affect.</param>
        /// <param name="otherMask"><see cref="AvatarMask"/> to copy from.</param>
        public static void CopyOtherMaskBodyActiveValues(this AvatarMask avatarMask,
            AvatarMask otherMask)
        {
            foreach (var maskBodyPart in AvatarMaskBodyParts)
            {
                if (maskBodyPart == AvatarMaskBodyPart.LastBodyPart)
                {
                    continue;
                }
                avatarMask.SetHumanoidBodyPartActive(
                    maskBodyPart,
                    otherMask.GetHumanoidBodyPartActive(maskBodyPart));
            }
        }
    }
}
