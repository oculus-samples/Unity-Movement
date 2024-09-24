// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using UnityEngine;
using static OVRUnityHumanoidSkeletonRetargeter;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// Provides convenient methods and occasional extensions to assist
    /// with animation rigging work.
    /// </summary>
    public static class RiggingUtilities
    {
        /// <summary>
        /// Returns true if animator is a humanoid.
        /// </summary>
        /// <param name="animator">Animator to check.</param>
        /// <returns>True if humanoid, false if not.</returns>
        public static bool IsHumanoidAnimator(
            Animator animator)
        {
            if (animator.avatar == null)
            {
                return false;
            }

            var avatar = animator.avatar;
            if (!avatar.isValid ||
                !avatar.isHuman)
            {
                return false;
            }

            return true;
        }

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
            int boneId,
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
                if (boneIndex == boneId)
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
        /// <param name="isFullBody">If is full body bone or not.</param>
        /// <returns></returns>
        public static Transform FindBoneTransformAnimator(
            Animator animator,
            OVRSkeleton.BoneId boneId,
            bool isFullBody)
        {
            if ((isFullBody && !OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone.ContainsKey(boneId)) ||
                (!isFullBody && !OVRHumanBodyBonesMappings.BoneIdToHumanBodyBone.ContainsKey(boneId))
                )
            {
                return null;
            }

            if (isFullBody)
            {
                return animator.GetBoneTransform(OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[boneId]);
            }
            return animator.GetBoneTransform(OVRHumanBodyBonesMappings.BoneIdToHumanBodyBone[boneId]);
        }

        /// <summary>
        /// Returns true if this vector3 is finite and not NaN.
        /// </summary>
        /// <param name="v">The Vector3 to be checked.</param>
        /// <returns>True if valid.</returns>
        public static bool IsFiniteVector3(Vector3 v)
        {
            return float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z) &&
                   !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z);
        }

        /// <summary>
        /// Returns the result of dividing a Vector3 by another Vector3.
        /// </summary>
        /// <param name="dividend">The Vector3 dividend.</param>
        /// <param name="divisor">The Vector3 divisor.</param>
        /// <returns>The divided Vector3.</returns>
        public static Vector3 DivideVector3(Vector3 dividend, Vector3 divisor)
        {
            Vector3 targetScale;
            if (Vector3IsNonZero(divisor))
            {
                targetScale = new Vector3(
                    dividend.x / divisor.x, dividend.y / divisor.y, dividend.z / divisor.z);
            }
            else
            {
                Debug.LogError("Zero detected inside divisor. Returning dividend.");
                return dividend;
            }

            return targetScale;
        }

        /// <summary>
        /// Returns true if this Vector3 contains no zero values.
        /// </summary>
        /// <param name="v">The Vector3 to test</param>
        /// <returns>True if this Vector3 contains no zero values.</returns>
        public static bool Vector3IsNonZero(Vector3 v)
        {
            return v.x != 0.0f && v.y != 0.0f && v.z != 0.0f;
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
