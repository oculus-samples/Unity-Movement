// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
#if INTERACTION_OVR_DEFINED
using Oculus.Interaction;
using Oculus.Interaction.Body.Input;
using Oculus.Interaction.Body.PoseDetection;
using Oculus.Interaction.Collections;
#endif
using UnityEngine;

namespace Meta.XR.Movement.BodyTrackingForFitness
{
    /// <summary>
    /// A collection of bones that connect to each other into a T-Pose.
    /// Bone data can be retrieved using common interfaces for body pose reading.
    /// </summary>
    public class FullBodySkeletonTPose
#if INTERACTION_OVR_DEFINED
        : ISkeletonMapping, IBodyPose
#endif
    {
#if INTERACTION_OVR_DEFINED
        /// <summary>
        /// Static class interface for for getting static T-Pose data
        /// </summary>
        public static class TPose
        {
            /// <summary>
            /// Provides an expected count of bones in a full body skeleton
            /// </summary>
            public static int ExpectedBoneCount => (int)BodyJointId.Body_End;

            /// <inheritdoc cref="BoneLink{TBoneId}.parent"/>
            public static int GetParent(int bodyJointId) => (int)_boneLinks[bodyJointId].parent;

            /// <inheritdoc cref="GetParent(int)"/>
            public static int GetParent(BodyJointId bodyJointId) => GetParent((int)bodyJointId);

            /// <inheritdoc cref="BoneLink{TBoneId}.next"/>
            public static int GetNext(int bodyJointId) => (int)_boneLinks[bodyJointId].next;

            /// <inheritdoc cref="BoneLink{TBoneId}.children"/>
            public static int[] GetChildren(int bodyJointId) => _boneLinks[bodyJointId].children;

            /// <inheritdoc cref="BoneLink{TBoneId}.length"/>
            public static float GetBoneLength(int bodyJointId) => _boneLinks[bodyJointId].length;

            /// <inheritdoc cref="BoneLink{TBoneId}.alignment"/>
            public static Quaternion GetForwardRotation(int boneId) => _boneLinks[boneId].alignment;

            /// <inheritdoc cref="ISkeletonMapping.Joints"/>
            public static IEnumerableHashSet<BodyJointId> Joints =>
                new EnumerableHashSet<BodyJointId>(BoneGroup.All);

            /// <inheritdoc cref="ISkeletonMapping.TryGetParentJointId"/>
            public static bool TryGetParentJointId(BodyJointId jointId, out BodyJointId parent)
            {
                int id = (int)jointId;
                if (id >= 0 && id < ExpectedBoneCount)
                {
                    parent = (BodyJointId)GetParent(id);
                    return true;
                }
                parent = BodyJointId.Invalid;
                return false;
            }

            /// <summary>
            /// Returns the T-pose <see cref="Pose"/> value of the given bone
            /// </summary>
            public static Pose GetTPose(int bodyJointId)
            {
                EnsureBestTPoseDataIsAvailable();
                if (bodyJointId < 0 || bodyJointId >= _boneLinks.Length)
                {
                    return default;
                }
                return _boneLinks[bodyJointId].tPose;
            }

            /// <inheritdoc cref="IBodyPose.GetJointPoseFromRoot"/>
            public static bool GetJointPoseFromRoot(BodyJointId bodyJointId, out Pose pose)
            {
                int id = (int)bodyJointId;
                if (id >= 0 && id < ExpectedBoneCount)
                {
                    pose = GetTPose(id);
                    return true;
                }
                pose = default;
                return false;
            }

            /// <inheritdoc cref="IBodyPose.GetJointPoseLocal"/>
            public static bool GetJointPoseLocal(BodyJointId bodyJointId, out Pose pose)
            {
                return GetJointPoseLocalIfFromRootIsKnown(_instance, bodyJointId, out pose);
            }

            private static void EnsureBestTPoseDataIsAvailable()
            {
                if (!Application.isPlaying || _ovrSkeleton.Bones == null ||
                    _ovrSkeleton.Bones.Length == 0)
                {
                    return;
                }
                for (int i = 0; i < _boneLinks.Length; ++i)
                {
                    OVRPlugin.Posef pose = _ovrSkeleton.Bones[i].Pose;
                    _boneLinks[i].tPose = new Pose(pose.Position.FromFlippedZVector3f(),
                        pose.Orientation.FromFlippedZQuatf());
                }
                // after best geometry is loaded, calculate lengths
                for (int i = 0; i < _boneLinks.Length; ++i)
                {
                    _boneLinks[i].UpdateLength(_boneLinks);
                }
            }
        }

        private static OVRPlugin.Skeleton2 _ovrSkeleton = new OVRPlugin.Skeleton2();
        private static readonly Vector3 EulerUp = new Vector3(-90, 0, 0);
        private static readonly Vector3 EulerRight = new Vector3(0, 90, 0);
        private static readonly Vector3 EulerLeft = new Vector3(0, -90, 0);

        /// <summary>
        /// This list identifies how bones connect to form the skeleton.
        /// </summary>
        private static BoneLink<BodyJointId>[] _boneLinks =
        {
            (BodyJointId.Body_Root, // id
                BodyJointId.Invalid, // parent
                BodyJointId.Invalid, // next (what this bone points at)
                EulerUp, // alignment of bone
                ((0,0,0), // position of bone in T-Pose
                (0,0,0))), // euler rotation of bone in T-Pose
            (BodyJointId.Body_Hips, BodyJointId.Body_Root, BodyJointId.Body_SpineLower,
                new Vector3(58.14f,-90,0), ((0,.923f,0), (0,270,270))),
            (BodyJointId.Body_SpineLower, BodyJointId.Body_Hips, BodyJointId.Body_SpineMiddle,
                new Vector3(-8,-90,0), ((0,.943f,-.033f), (0,270,272.285f))),
            (BodyJointId.Body_SpineMiddle, BodyJointId.Body_SpineLower, BodyJointId.Body_SpineUpper,
                new Vector3(11.06f,-90,0), ((0,1.054f,-.021f), (0,270,272.285f))),
            (BodyJointId.Body_SpineUpper, BodyJointId.Body_SpineMiddle, BodyJointId.Body_Chest,
                new Vector3(-174.51f,90,0), ((0,1.163f,-.047f), (0,270,272.285f))),
            (BodyJointId.Body_Chest, BodyJointId.Body_SpineUpper, BodyJointId.Body_Neck,
                new Vector3(-154.38f,90,0), ((0,1.347f,-.037f), (0,270,272.285f))),
            (BodyJointId.Body_Neck, BodyJointId.Body_Chest, BodyJointId.Body_Head,
                new Vector3(-180,90,0), ((0,1.468f,.015f), (0,270,253.247f))),
            (BodyJointId.Body_Head, BodyJointId.Body_Neck, BodyJointId.Invalid,
                new Vector3(0,270,0), ((0,1.536f,.035f), (0,270,270))),
            (BodyJointId.Body_LeftShoulder, BodyJointId.Body_Chest, BodyJointId.Body_LeftScapula,
                EulerLeft, ((-.028f,1.404f,.064f), (87.909f,242.114f,270))),
            (BodyJointId.Body_LeftScapula, BodyJointId.Body_LeftShoulder, BodyJointId.Body_LeftArmUpper,
                EulerRight, ((-.16f,1.447f,-.032f), (78.424f,280.995f,295.236f))),
            (BodyJointId.Body_LeftArmUpper, BodyJointId.Body_LeftScapula, BodyJointId.Body_LeftArmLower,
                EulerLeft, ((-.18f,1.421f,-.033f), (58.947f,6.052f,7.279f))),
            (BodyJointId.Body_LeftArmLower, BodyJointId.Body_LeftArmUpper, BodyJointId.Body_LeftHandWristTwist,
                EulerLeft, ((-.436f,1.404f,-.035f), (58.947f,6.052f,4.558f))),
            (BodyJointId.Body_LeftHandWristTwist, BodyJointId.Body_LeftArmLower, BodyJointId.Body_LeftHandWrist,
                new Vector3(-35, -78, 0), ((-.667f,1.394f,-.029f), (62.256f,18.931f,17.883f))),
            (BodyJointId.Body_RightShoulder, BodyJointId.Body_Chest, BodyJointId.Body_RightScapula,
                EulerRight, ((.028f,1.404f,.064f), (272.091f,297.886f,90f))),
            (BodyJointId.Body_RightScapula, BodyJointId.Body_RightShoulder, BodyJointId.Body_RightArmUpper,
                EulerLeft, ((.16f,1.447f,-.032f), (281.577f,259.005f,115.236f))),
            (BodyJointId.Body_RightArmUpper, BodyJointId.Body_RightScapula, BodyJointId.Body_RightArmLower,
                EulerRight, ((.18f,1.421f,-.033f), (301.053f,173.948f,187.28f))),
            (BodyJointId.Body_RightArmLower, BodyJointId.Body_RightArmUpper, BodyJointId.Body_RightHandWristTwist,
                EulerRight, ((.436f,1.404f,-.035f), (301.053f,173.948f,184.558f))),
            (BodyJointId.Body_RightHandWristTwist, BodyJointId.Body_RightArmLower, BodyJointId.Body_RightHandWrist,
                new Vector3(35, 101, 0), ((.667f,1.394f,-.029f), (306.327f,165.679f,193.964f))),
            (BodyJointId.Body_LeftHandPalm, BodyJointId.Body_LeftHandWristTwist, BodyJointId.Invalid,
                EulerLeft, ((-.728f,1.391f,-.006f), (7.164f,186.58f,173.242f))),
            (BodyJointId.Body_LeftHandWrist, BodyJointId.Body_LeftHandWristTwist, BodyJointId.Body_LeftHandPalm,
                EulerLeft, ((-.69f,1.397f,-.011f), (7.164f,186.58f,173.242f))),
            (BodyJointId.Body_LeftHandThumbMetacarpal, BodyJointId.Body_LeftHandWrist, BodyJointId.Body_LeftHandThumbProximal,
                EulerLeft, ((-.72f,1.377f,.023f), (307.274f,273.914f,127.926f))),
            (BodyJointId.Body_LeftHandThumbProximal, BodyJointId.Body_LeftHandThumbMetacarpal, BodyJointId.Body_LeftHandThumbDistal,
                EulerLeft, ((-.739f,1.361f,.044f), (309.12f,253.071f,154.084f))),
            (BodyJointId.Body_LeftHandThumbDistal, BodyJointId.Body_LeftHandThumbProximal, BodyJointId.Body_LeftHandThumbTip,
                EulerLeft, ((-.759f,1.352f,.07f), (304.521f,271.922f,145.651f))),
            (BodyJointId.Body_LeftHandThumbTip, BodyJointId.Body_LeftHandThumbDistal, BodyJointId.Invalid,
                EulerLeft, ((-.769f,1.348f,.083f), (304.521f,271.922f,145.651f))),
            (BodyJointId.Body_LeftHandIndexMetacarpal, BodyJointId.Body_LeftHandWrist, BodyJointId.Body_LeftHandIndexProximal,
                EulerLeft, ((-.723f,1.384f,.013f), (7.164f,186.58f,173.242f))),
            (BodyJointId.Body_LeftHandIndexProximal, BodyJointId.Body_LeftHandIndexMetacarpal, BodyJointId.Body_LeftHandIndexIntermediate,
                EulerLeft, ((-.781f,1.381f,.025f), (3.344f,185.01f,168.133f))),
            (BodyJointId.Body_LeftHandIndexIntermediate, BodyJointId.Body_LeftHandIndexProximal, BodyJointId.Body_LeftHandIndexDistal,
                EulerLeft, ((-.818f,1.373f,.029f), (6.07f,183.587f,167.639f))),
            (BodyJointId.Body_LeftHandIndexDistal, BodyJointId.Body_LeftHandIndexIntermediate, BodyJointId.Body_LeftHandIndexTip,
                EulerLeft, ((-.842f,1.368f,.031f), (7.434f,180.216f,175.508f))),
            (BodyJointId.Body_LeftHandIndexTip, BodyJointId.Body_LeftHandIndexDistal, BodyJointId.Invalid,
                EulerLeft, ((-.854f,1.366f,.032f), (7.434f,180.216f,175.508f))),
            (BodyJointId.Body_LeftHandMiddleMetacarpal, BodyJointId.Body_LeftHandWrist, BodyJointId.Body_LeftHandMiddleProximal,
                EulerLeft, ((-.753f,1.381f,.003f), (7.164f,186.58f,173.242f))),
            (BodyJointId.Body_LeftHandMiddleProximal, BodyJointId.Body_LeftHandMiddleMetacarpal, BodyJointId.Body_LeftHandMiddleIntermediate,
                EulerLeft, ((-.784f,1.383f,.003f), (7.152f,180.542f,166.538f))),
            (BodyJointId.Body_LeftHandMiddleIntermediate, BodyJointId.Body_LeftHandMiddleProximal, BodyJointId.Body_LeftHandMiddleDistal,
                EulerLeft, ((-.826f,1.373f,.005f), (8.287f,179.749f,166.658f))),
            (BodyJointId.Body_LeftHandMiddleDistal, BodyJointId.Body_LeftHandMiddleIntermediate, BodyJointId.Body_LeftHandMiddleTip,
                EulerLeft, ((-.853f,1.367f,.005f), (12.107f,178.654f,177.144f))),
            (BodyJointId.Body_LeftHandMiddleTip, BodyJointId.Body_LeftHandMiddleDistal, BodyJointId.Invalid,
                EulerLeft, ((-.866f,1.364f,.006f), (12.107f,178.654f,177.144f))),
            (BodyJointId.Body_LeftHandRingMetacarpal, BodyJointId.Body_LeftHandWrist, BodyJointId.Body_LeftHandRingProximal,
                EulerLeft, ((-.726f,1.385f,-.021f), (7.164f,186.58f,173.242f))),
            (BodyJointId.Body_LeftHandRingProximal, BodyJointId.Body_LeftHandRingMetacarpal, BodyJointId.Body_LeftHandRingIntermediate,
                EulerLeft, ((-.779f,1.378f,-.016f), (10.53f,171.356f,165.123f))),
            (BodyJointId.Body_LeftHandRingIntermediate, BodyJointId.Body_LeftHandRingProximal, BodyJointId.Body_LeftHandRingDistal,
                EulerLeft, ((-.816f,1.368f,-.02f), (14.163f,169.996f,164.181f))),
            (BodyJointId.Body_LeftHandRingDistal, BodyJointId.Body_LeftHandRingIntermediate, BodyJointId.Body_LeftHandRingTip,
                EulerLeft, ((-.842f,1.361f,-.023f), (15.357f,173.252f,167.881f))),
            (BodyJointId.Body_LeftHandRingTip, BodyJointId.Body_LeftHandRingDistal, BodyJointId.Invalid,
                EulerLeft, ((-.855f,1.357f,-.024f), (15.357f,173.252f,167.881f))),
            (BodyJointId.Body_LeftHandLittleMetacarpal, BodyJointId.Body_LeftHandWrist, BodyJointId.Body_LeftHandLittleProximal,
                EulerLeft, ((-.725f,1.38f,-.028f), (27.8f,165.1f,164.445f))),
            (BodyJointId.Body_LeftHandLittleProximal, BodyJointId.Body_LeftHandLittleMetacarpal, BodyJointId.Body_LeftHandLittleIntermediate,
                EulerLeft, ((-.769f,1.37f,-.034f), (17.901f,168.795f,162.648f))),
            (BodyJointId.Body_LeftHandLittleIntermediate, BodyJointId.Body_LeftHandLittleProximal, BodyJointId.Body_LeftHandLittleDistal,
                EulerLeft, ((-.799f,1.361f,-.037f), (20.523f,162.443f,162.081f))),
            (BodyJointId.Body_LeftHandLittleDistal, BodyJointId.Body_LeftHandLittleIntermediate, BodyJointId.Body_LeftHandLittleTip,
                EulerLeft, ((-.818f,1.355f,-.041f), (21.957f,168.293f,166.958f))),
            (BodyJointId.Body_LeftHandLittleTip, BodyJointId.Body_LeftHandLittleDistal, BodyJointId.Invalid,
                EulerLeft, ((-.827f,1.352f,-.043f), (21.957f,168.293f,166.958f))),
            (BodyJointId.Body_RightHandPalm, BodyJointId.Body_RightHandWristTwist, BodyJointId.Invalid,
                EulerRight, ((.728f,1.391f,-.006f), (352.836f,353.42f,353.242f))),
            (BodyJointId.Body_RightHandWrist, BodyJointId.Body_RightHandWristTwist, BodyJointId.Body_RightHandPalm,
                EulerRight, ((.69f,1.397f,-.011f), (352.836f,353.42f,353.242f))),
            (BodyJointId.Body_RightHandThumbMetacarpal, BodyJointId.Body_RightHandWrist, BodyJointId.Body_RightHandThumbProximal,
                EulerRight, ((.72f,1.377f,.023f), (52.726f,266.086f,307.926f))),
            (BodyJointId.Body_RightHandThumbProximal, BodyJointId.Body_RightHandThumbMetacarpal, BodyJointId.Body_RightHandThumbDistal,
                EulerRight, ((.739f,1.361f,.044f), (50.88f,286.93f,334.084f))),
            (BodyJointId.Body_RightHandThumbDistal, BodyJointId.Body_RightHandThumbProximal, BodyJointId.Body_RightHandThumbTip,
                EulerRight, ((.759f,1.352f,.07f), (55.479f,268.078f,325.652f))),
            (BodyJointId.Body_RightHandThumbTip, BodyJointId.Body_RightHandThumbDistal, BodyJointId.Invalid,
                EulerRight, ((.769f,1.348f,.083f), (55.479f,268.078f,325.652f))),
            (BodyJointId.Body_RightHandIndexMetacarpal, BodyJointId.Body_RightHandWrist, BodyJointId.Body_RightHandIndexProximal,
                EulerRight, ((.723f,1.384f,.013f), (352.836f,353.42f,353.242f))),
            (BodyJointId.Body_RightHandIndexProximal, BodyJointId.Body_RightHandIndexMetacarpal, BodyJointId.Body_RightHandIndexIntermediate,
                EulerRight, ((.781f,1.381f,.025f), (356.656f,354.99f,348.133f))),
            (BodyJointId.Body_RightHandIndexIntermediate, BodyJointId.Body_RightHandIndexProximal, BodyJointId.Body_RightHandIndexDistal,
                EulerRight, ((.818f,1.373f,.029f), (353.93f,356.414f,347.639f))),
            (BodyJointId.Body_RightHandIndexDistal, BodyJointId.Body_RightHandIndexIntermediate, BodyJointId.Body_RightHandIndexTip,
                EulerRight, ((.842f,1.368f,.031f), (352.566f,359.784f,355.508f))),
            (BodyJointId.Body_RightHandIndexTip, BodyJointId.Body_RightHandIndexDistal, BodyJointId.Invalid,
                EulerRight, ((.854f,1.366f,.032f), (352.566f,359.784f,355.508f))),
            (BodyJointId.Body_RightHandMiddleMetacarpal, BodyJointId.Body_RightHandWrist, BodyJointId.Body_RightHandMiddleProximal,
                EulerRight, ((.753f,1.381f,.003f), (352.836f,353.42f,353.242f))),
            (BodyJointId.Body_RightHandMiddleProximal, BodyJointId.Body_RightHandMiddleMetacarpal, BodyJointId.Body_RightHandMiddleIntermediate,
                EulerRight, ((.784f,1.383f,.003f), (352.848f,359.458f,346.538f))),
            (BodyJointId.Body_RightHandMiddleIntermediate, BodyJointId.Body_RightHandMiddleProximal, BodyJointId.Body_RightHandMiddleDistal,
                EulerRight, ((.826f,1.373f,.005f), (351.713f,.251f,346.658f))),
            (BodyJointId.Body_RightHandMiddleDistal, BodyJointId.Body_RightHandMiddleIntermediate, BodyJointId.Body_RightHandMiddleTip,
                EulerRight, ((.853f,1.367f,.005f), (347.893f,1.346f,357.144f))),
            (BodyJointId.Body_RightHandMiddleTip, BodyJointId.Body_RightHandMiddleDistal, BodyJointId.Invalid,
                EulerRight, ((.866f,1.364f,.006f), (347.893f,1.346f,357.144f))),
            (BodyJointId.Body_RightHandRingMetacarpal, BodyJointId.Body_RightHandWrist, BodyJointId.Body_RightHandRingProximal,
                EulerRight, ((.726f,1.385f,-.021f), (352.836f,353.42f,353.242f))),
            (BodyJointId.Body_RightHandRingProximal, BodyJointId.Body_RightHandRingMetacarpal, BodyJointId.Body_RightHandRingIntermediate,
                EulerRight, ((.779f,1.378f,-.016f), (349.47f,8.644f,345.123f))),
            (BodyJointId.Body_RightHandRingIntermediate, BodyJointId.Body_RightHandRingProximal, BodyJointId.Body_RightHandRingDistal,
                EulerRight, ((.816f,1.368f,-.02f), (345.837f,10.004f,344.181f))),
            (BodyJointId.Body_RightHandRingDistal, BodyJointId.Body_RightHandRingIntermediate, BodyJointId.Body_RightHandRingTip,
                EulerRight, ((.842f,1.361f,-.023f), (344.643f,6.748f,347.881f))),
            (BodyJointId.Body_RightHandRingTip, BodyJointId.Body_RightHandRingDistal, BodyJointId.Invalid,
                EulerRight, ((.855f,1.357f,-.024f), (344.643f,6.748f,347.881f))),
            (BodyJointId.Body_RightHandLittleMetacarpal, BodyJointId.Body_RightHandWrist, BodyJointId.Body_RightHandLittleProximal,
                EulerRight, ((.725f,1.38f,-.028f), (332.2f,14.9f,344.445f))),
            (BodyJointId.Body_RightHandLittleProximal, BodyJointId.Body_RightHandLittleMetacarpal, BodyJointId.Body_RightHandLittleIntermediate,
                EulerRight, ((.769f,1.37f,-.034f), (342.099f,11.205f,342.648f))),
            (BodyJointId.Body_RightHandLittleIntermediate, BodyJointId.Body_RightHandLittleProximal, BodyJointId.Body_RightHandLittleDistal,
                EulerRight, ((.799f,1.361f,-.037f), (339.477f,17.557f,342.081f))),
            (BodyJointId.Body_RightHandLittleDistal, BodyJointId.Body_RightHandLittleIntermediate, BodyJointId.Body_RightHandLittleTip,
                EulerRight, ((.818f,1.355f,-.041f), (338.043f,11.707f,346.958f))),
            (BodyJointId.Body_RightHandLittleTip, BodyJointId.Body_RightHandLittleDistal, BodyJointId.Invalid,
                EulerRight, ((.827f,1.352f,-.043f), (338.043f,11.707f,346.958f))),
            (BodyJointId.Body_LeftLegUpper, BodyJointId.Body_Hips, BodyJointId.Body_LeftLegLower,
                EulerLeft, ((-.08f,.898f,-.005f), (1.849f,268.189f,85.825f))),
            (BodyJointId.Body_LeftLegLower, BodyJointId.Body_LeftLegUpper, BodyJointId.Body_LeftFootAnkleTwist,
                EulerLeft, ((-.066f,.479f,-.028f), (2.085f,268.152f,84.316f))),
            (BodyJointId.Body_LeftFootAnkleTwist, BodyJointId.Body_LeftLegLower, BodyJointId.Body_LeftFootAnkle,
                new Vector3(-4.9f,-86.05f,-4.73f), ((-.049f,.061f,-.069f), (7.665f,269.645f,84.046f))),
            (BodyJointId.Body_LeftFootAnkle, BodyJointId.Body_LeftFootAnkleTwist, BodyJointId.Body_LeftFootSubtalar,
                new Vector3(63.5f,23.59f,-30.74f), ((-.049f,.061f,-.069f), (19.738f,263.068f,359.157f))),
            (BodyJointId.Body_LeftFootSubtalar, BodyJointId.Body_LeftFootAnkle, BodyJointId.Body_LeftFootTransverse,
                new Vector3(32.09f,105.46f,-4.54f), ((-.053f,.026f,-.063f), (20.222f,256.742f,38.951f))),
            (BodyJointId.Body_LeftFootTransverse, BodyJointId.Body_LeftFootSubtalar, BodyJointId.Body_LeftFootBall,
            new Vector3(37.28f,93.7f,-6.16f), ((-.056f,.037f,-.003f), (12.01f,260.427f,13.547f))),
            (BodyJointId.Body_LeftFootBall, BodyJointId.Body_LeftFootTransverse, BodyJointId.Invalid,
                new Vector3(18.93f,98.5f,-5.85f), ((-.057f,.007f,.069f), (9.558f,255.847f,357.958f))),
            (BodyJointId.Body_RightLegUpper, BodyJointId.Body_Hips, BodyJointId.Body_RightLegLower,
                EulerRight, ((.08f,.898f,-.005f), (358.151f,271.811f,265.825f))),
            (BodyJointId.Body_RightLegLower, BodyJointId.Body_RightLegUpper, BodyJointId.Body_RightFootAnkleTwist,
                EulerRight, ((.066f,.479f,-.028f), (357.915f,271.848f,264.316f))),
            (BodyJointId.Body_RightFootAnkleTwist, BodyJointId.Body_RightLegLower, BodyJointId.Body_RightFootAnkle,
                new Vector3(2.92f,100.25f,-128.0f), ((.049f,.061f,-.069f), (352.335f,270.356f,264.046f))),
            (BodyJointId.Body_RightFootAnkle, BodyJointId.Body_RightFootAnkleTwist, BodyJointId.Body_RightFootSubtalar,
                new Vector3(-116.5f,23.59f,68.6f), ((.049f,.061f,-.069f), (340.262f,276.932f,179.157f))),
            (BodyJointId.Body_RightFootSubtalar, BodyJointId.Body_RightFootAnkle, BodyJointId.Body_RightFootTransverse,
                new Vector3(-147.92f,105.46f,4.34f), ((.053f,.026f,-.063f), (339.778f,283.258f,218.951f))),
            (BodyJointId.Body_RightFootTransverse, BodyJointId.Body_RightFootSubtalar, BodyJointId.Body_RightFootBall,
                new Vector3(-142.72f,93.7f,5.94f), ((.056f,.037f,-.003f), (347.99f,279.573f,193.547f))),
            (BodyJointId.Body_RightFootBall, BodyJointId.Body_RightFootTransverse, BodyJointId.Invalid,
                new Vector3(-18.34f,-81.51f,185.11f), ((.057f,.007f,.069f), (350.442f,284.153f,177.958f))),
        };

        /// <summary>
        /// Which skeleton is being provided here
        /// </summary>
        public int SkeletonId => (int)OVRPlugin.SkeletonType.FullBody;

        /// <summary>
        /// Returns the <see cref="Enum"/> type used to count and map this skeleton.
        /// </summary>
        public Type BoneEnum => typeof(BodyJointId);

        /// <summary>
        /// Provides an expected count of bones in a full body skeleton
        /// </summary>
        public int ExpectedBoneCount => TPose.ExpectedBoneCount;

        /// <inheritdoc cref="BoneLink{TBoneId}.parent"/>
        public int GetParent(int bodyJointId) => TPose.GetParent(bodyJointId);

        /// <inheritdoc cref="GetParent(int)"/>
        public int GetParent(BodyJointId bodyJointId) => TPose.GetParent(bodyJointId);

        /// <inheritdoc cref="BoneLink{TBoneId}.next"/>
        public int GetNext(int bodyJointId) => TPose.GetNext(bodyJointId);

        /// <inheritdoc cref="BoneLink{TBoneId}.children"/>
        public int[] GetChildren(int bodyJointId) => TPose.GetChildren(bodyJointId);

        /// <inheritdoc cref="BoneLink{TBoneId}.length"/>
        public float GetBoneLength(int bodyJointId) => TPose.GetBoneLength(bodyJointId);

        /// <inheritdoc cref="BoneLink{TBoneId}.alignment"/>
        public Quaternion GetForwardRotation(int boneId) => TPose.GetForwardRotation(boneId);

        /// <inheritdoc cref="IBody.SkeletonMapping"/>
        public ISkeletonMapping SkeletonMapping => this;

        /// <inheritdoc cref="IBodyPose.WhenBodyPoseUpdated"/>
        public event Action WhenBodyPoseUpdated = delegate { };

        /// <inheritdoc cref="ISkeletonMapping.Joints"/>
        public IEnumerableHashSet<BodyJointId> Joints => TPose.Joints;

        /// <summary>
        /// Needed to access static data through the <see cref="IBodyPose"/> interface
        /// </summary>
        private static FullBodySkeletonTPose _instance;

        /// <summary>
        /// Static constructor. Called just once per runtime.
        /// </summary>
        static FullBodySkeletonTPose()
        {
            _instance = new FullBodySkeletonTPose();
            for (int i = 0; i < _boneLinks.Length; ++i)
            {
                if (_boneLinks[i].id != (BodyJointId)i)
                {
                    throw new Exception($"Bone links must be in order. " +
                        $"{_boneLinks[i].id} should be at index {(int)_boneLinks[i].id}");
                }
            }
            Dictionary<BodyJointId, List<int>> childrenOfBones =
                new Dictionary<BodyJointId, List<int>>();
            for (int i = 0; i < _boneLinks.Length; ++i)
            {
                BodyJointId parentId = _boneLinks[i].parent;
                if (parentId != BodyJointId.Invalid)
                {
                    if (!childrenOfBones.TryGetValue(parentId, out List<int> childList))
                    {
                        childrenOfBones[parentId] = childList = new List<int>();
                    }
                    childList.Add(i);
                }
            }
            for (int i = 0; i < _boneLinks.Length; ++i)
            {
                _boneLinks[i].UpdateLength(_boneLinks);
                if (!childrenOfBones.TryGetValue((BodyJointId)i, out List<int> childList))
                {
                    _boneLinks[i].children = Array.Empty<int>();
                }
                else
                {
                    _boneLinks[i].children = childList.ToArray();
                }
            }
        }

        /// <inheritdoc cref="ISkeletonMapping.TryGetParentJointId"/>
        public bool TryGetParentJointId(BodyJointId jointId, out BodyJointId parent) =>
            TPose.TryGetParentJointId(jointId, out parent);

        /// <inheritdoc cref="TPose.GetTPose"/>
        public Pose GetTPose(int bodyJointId) => TPose.GetTPose(bodyJointId);

        /// <inheritdoc cref="IBodyPose.GetJointPoseFromRoot"/>
        public bool GetJointPoseFromRoot(BodyJointId bodyJointId, out Pose pose) =>
            TPose.GetJointPoseFromRoot(bodyJointId, out pose);

        /// <inheritdoc cref="IBodyPose.GetJointPoseLocal"/>
        public bool GetJointPoseLocal(BodyJointId bodyJointId, out Pose pose) =>
            TPose.GetJointPoseLocal(bodyJointId, out pose);

        /// <summary>
        /// Common method when a local pose is calculated for an object already implementing
        /// <see cref="IBodyPose.GetJointPoseFromRoot"/>
        /// </summary>
        public static bool GetJointPoseLocalIfFromRootIsKnown(
            IBodyPose posesFromRoot, BodyJointId bodyJointId, out Pose pose)
        {
            int id = (int)bodyJointId;
            if (id < 0 || id >= TPose.ExpectedBoneCount)
            {
                pose = default;
                return false;
            }
            bool hasBone = posesFromRoot.GetJointPoseFromRoot(bodyJointId, out pose);
            if (!hasBone)
            {
                return false;
            }
            int parent = (int)_boneLinks[id].parent;
            if (parent >= 0)
            {
                hasBone = posesFromRoot.GetJointPoseFromRoot(
                    (BodyJointId)parent, out Pose parentPose);
                if (!hasBone)
                {
                    return false;
                }
                Pose inverseParent = default;
                PoseUtils.Inverse(parentPose, ref inverseParent);
                pose = new Pose(
                    inverseParent.rotation * pose.position + inverseParent.position,
                    inverseParent.rotation * pose.rotation);
            }
            return true;
        }
#endif
    }
}
