// Copyright (c) Meta Platforms, Inc. and affiliates.

namespace Oculus.Movement.Utils
{
    /// <summary>
    /// Bone ID helpers for <see cref="OVRSkeleton"/> hand skeletons.
    /// Additive workaround for oculus-samples/Unity-Movement#120: inside
    /// <see cref="OVRSkeleton.BoneId"/>, the legacy OVR hand (<c>Hand_*</c>)
    /// and the OpenXR hand (<c>XRHand_*</c>) members fully overlap
    /// numerically (for example, <c>Hand_IndexTip</c> and
    /// <c>XRHand_RingTip</c> are both 20). OVRSkeleton.BoneLabelFromBoneId
    /// dispatches on numeric value only, so a legacy <c>Hand_*</c> ID passed
    /// while using SkeletonType.XRHandLeft/XRHandRight resolves to the wrong
    /// joint (for example, <c>Hand_IndexTip</c> matches
    /// <c>XRHand_RingTip</c> and returns the ring fingertip instead of the
    /// index fingertip).
    /// </summary>
    public static class OVRSkeletonBoneIdExtensions
    {
        /// <summary>
        /// Converts a legacy OVR hand (<c>Hand_*</c>) bone ID into the
        /// equivalent OpenXR hand (<c>XRHand_*</c>) bone ID, so that lookups
        /// written against SkeletonType.HandLeft/HandRight also resolve to
        /// the correct joint on SkeletonType.XRHandLeft/XRHandRight.
        /// The mapping follows the phalange annotations of
        /// <see cref="OVRSkeleton.BoneId"/> (for example, Hand_Index1 is the
        /// index *proximal* phalange and maps to XRHand_IndexProximal, while
        /// Hand_Pinky0 is the pinky *metacarpal* and maps to
        /// XRHand_LittleMetacarpal; Hand_ForearmStub maps to XRHand_Wrist
        /// because OpenXR hand skeletons have no forearm joint).
        /// Contract: the input is intended to be a legacy <c>Hand_*</c> ID.
        /// Do not pass native <c>XRHand_*</c> values through this method:
        /// because the two ID families overlap numerically (for example,
        /// XRHand_IndexTip == Hand_Middle2 == 10), a native ID would be
        /// translated incorrectly. IDs outside the <c>Hand_*</c> set are
        /// returned unchanged.
        /// </summary>
        /// <param name="handBoneId">Legacy OVR hand (<c>Hand_*</c>) bone ID.</param>
        /// <returns>Equivalent OpenXR hand (<c>XRHand_*</c>) bone ID.</returns>
        public static OVRSkeleton.BoneId ToXRHandBoneId(this OVRSkeleton.BoneId handBoneId)
        {
            switch (handBoneId)
            {
                case OVRSkeleton.BoneId.Hand_WristRoot:
                    return OVRSkeleton.BoneId.XRHand_Wrist;
                case OVRSkeleton.BoneId.Hand_ForearmStub:
                    return OVRSkeleton.BoneId.XRHand_Wrist;
                case OVRSkeleton.BoneId.Hand_Thumb0:
                    return OVRSkeleton.BoneId.XRHand_ThumbMetacarpal;
                case OVRSkeleton.BoneId.Hand_Thumb1:
                    return OVRSkeleton.BoneId.XRHand_ThumbProximal;
                case OVRSkeleton.BoneId.Hand_Thumb2:
                    return OVRSkeleton.BoneId.XRHand_ThumbDistal;
                case OVRSkeleton.BoneId.Hand_Thumb3:
                    return OVRSkeleton.BoneId.XRHand_ThumbTip;
                case OVRSkeleton.BoneId.Hand_Index1:
                    return OVRSkeleton.BoneId.XRHand_IndexProximal;
                case OVRSkeleton.BoneId.Hand_Index2:
                    return OVRSkeleton.BoneId.XRHand_IndexIntermediate;
                case OVRSkeleton.BoneId.Hand_Index3:
                    return OVRSkeleton.BoneId.XRHand_IndexDistal;
                case OVRSkeleton.BoneId.Hand_IndexTip:
                    return OVRSkeleton.BoneId.XRHand_IndexTip;
                case OVRSkeleton.BoneId.Hand_Middle1:
                    return OVRSkeleton.BoneId.XRHand_MiddleProximal;
                case OVRSkeleton.BoneId.Hand_Middle2:
                    return OVRSkeleton.BoneId.XRHand_MiddleIntermediate;
                case OVRSkeleton.BoneId.Hand_Middle3:
                    return OVRSkeleton.BoneId.XRHand_MiddleDistal;
                case OVRSkeleton.BoneId.Hand_MiddleTip:
                    return OVRSkeleton.BoneId.XRHand_MiddleTip;
                case OVRSkeleton.BoneId.Hand_Ring1:
                    return OVRSkeleton.BoneId.XRHand_RingProximal;
                case OVRSkeleton.BoneId.Hand_Ring2:
                    return OVRSkeleton.BoneId.XRHand_RingIntermediate;
                case OVRSkeleton.BoneId.Hand_Ring3:
                    return OVRSkeleton.BoneId.XRHand_RingDistal;
                case OVRSkeleton.BoneId.Hand_RingTip:
                    return OVRSkeleton.BoneId.XRHand_RingTip;
                case OVRSkeleton.BoneId.Hand_Pinky0:
                    return OVRSkeleton.BoneId.XRHand_LittleMetacarpal;
                case OVRSkeleton.BoneId.Hand_Pinky1:
                    return OVRSkeleton.BoneId.XRHand_LittleProximal;
                case OVRSkeleton.BoneId.Hand_Pinky2:
                    return OVRSkeleton.BoneId.XRHand_LittleIntermediate;
                case OVRSkeleton.BoneId.Hand_Pinky3:
                    return OVRSkeleton.BoneId.XRHand_LittleDistal;
                case OVRSkeleton.BoneId.Hand_ThumbTip:
                    return OVRSkeleton.BoneId.XRHand_ThumbTip;
                case OVRSkeleton.BoneId.Hand_PinkyTip:
                    return OVRSkeleton.BoneId.XRHand_LittleTip;
                default:
                    return handBoneId;
            }
        }

        /// <summary>
        /// Returns the finger-tip bone ID that matches the given skeleton
        /// type: an <c>XRHand_*Tip</c> ID for
        /// SkeletonType.XRHandLeft/XRHandRight, or a <c>Hand_*Tip</c> ID for
        /// the legacy OVR hand skeleton types. This mirrors the
        /// per-skeleton-type tip selection that the Meta XR Core SDK itself
        /// performs in OVRVirtualKeyboard. Note that Hand_PinkyTip maps to
        /// XRHand_LittleTip because OpenXR names the little finger "little".
        /// </summary>
        /// <param name="skeletonType">Skeleton type of the OVRSkeleton being queried.</param>
        /// <param name="finger">Finger whose tip bone ID is requested.</param>
        /// <returns>The finger-tip bone ID matching the skeleton type.</returns>
        public static OVRSkeleton.BoneId GetFingerTipBoneId(OVRSkeleton.SkeletonType skeletonType,
            OVRHand.HandFinger finger)
        {
            OVRSkeleton.BoneId legacyTipId;
            switch (finger)
            {
                case OVRHand.HandFinger.Thumb:
                    legacyTipId = OVRSkeleton.BoneId.Hand_ThumbTip;
                    break;
                case OVRHand.HandFinger.Index:
                    legacyTipId = OVRSkeleton.BoneId.Hand_IndexTip;
                    break;
                case OVRHand.HandFinger.Middle:
                    legacyTipId = OVRSkeleton.BoneId.Hand_MiddleTip;
                    break;
                case OVRHand.HandFinger.Ring:
                    legacyTipId = OVRSkeleton.BoneId.Hand_RingTip;
                    break;
                case OVRHand.HandFinger.Pinky:
                    legacyTipId = OVRSkeleton.BoneId.Hand_PinkyTip;
                    break;
                default:
                    legacyTipId = OVRSkeleton.BoneId.Invalid;
                    break;
            }
            bool isOpenXRHand = skeletonType == OVRSkeleton.SkeletonType.XRHandLeft ||
                skeletonType == OVRSkeleton.SkeletonType.XRHandRight;
            return isOpenXRHand ? legacyTipId.ToXRHandBoneId() : legacyTipId;
        }
    }
}
