// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Movement.AnimationRigging.Deprecated
{
    /// <summary>
    /// Contains some mappings copied from OVRUnityHumanoidSkeletonRetargeter,
    /// which are currently inaccessible from non-inheriting classes.
    /// </summary>
    public class FullBodyCustomMappings
    {
        /// <summary>
        /// Body tracking bone IDs that should be exposed through the inspector.
        /// BoneId has enum values that map to the same integers, which would not work
        /// with a serialized field that expects unique integers. FullBodyTrackingBoneId
        /// is an enum that restricts BoneId to the values that we care about.
        /// </summary>
        public enum FullBodyTrackingBoneId
        {
            FullBody_Start = OVRPlugin.BoneId.FullBody_Start,
            FullBody_Root = OVRPlugin.BoneId.FullBody_Root,
            FullBody_Hips = OVRPlugin.BoneId.FullBody_Hips,
            FullBody_SpineLower = OVRPlugin.BoneId.FullBody_SpineLower,
            FullBody_SpineMiddle = OVRPlugin.BoneId.FullBody_SpineMiddle,
            FullBody_SpineUpper = OVRPlugin.BoneId.FullBody_SpineUpper,
            FullBody_Chest = OVRPlugin.BoneId.FullBody_Chest,
            FullBody_Neck = OVRPlugin.BoneId.FullBody_Neck,
            FullBody_Head = OVRPlugin.BoneId.FullBody_Head,
            FullBody_LeftShoulder = OVRPlugin.BoneId.FullBody_LeftShoulder,
            FullBody_LeftScapula = OVRPlugin.BoneId.FullBody_LeftScapula,
            FullBody_LeftArmUpper = OVRPlugin.BoneId.FullBody_LeftArmUpper,
            FullBody_LeftArmLower = OVRPlugin.BoneId.FullBody_LeftArmLower,
            FullBody_LeftHandWristTwist = OVRPlugin.BoneId.FullBody_LeftHandWristTwist,
            FullBody_RightShoulder = OVRPlugin.BoneId.FullBody_RightShoulder,
            FullBody_RightScapula = OVRPlugin.BoneId.FullBody_RightScapula,
            FullBody_RightArmUpper = OVRPlugin.BoneId.FullBody_RightArmUpper,
            FullBody_RightArmLower = OVRPlugin.BoneId.FullBody_RightArmLower,
            FullBody_RightHandWristTwist = OVRPlugin.BoneId.FullBody_RightHandWristTwist,
            FullBody_LeftHandPalm = OVRPlugin.BoneId.FullBody_LeftHandPalm,
            FullBody_LeftHandWrist = OVRPlugin.BoneId.FullBody_LeftHandWrist,
            FullBody_LeftHandThumbMetacarpal = OVRPlugin.BoneId.FullBody_LeftHandThumbMetacarpal,
            FullBody_LeftHandThumbProximal = OVRPlugin.BoneId.FullBody_LeftHandThumbProximal,
            FullBody_LeftHandThumbDistal = OVRPlugin.BoneId.FullBody_LeftHandThumbDistal,
            FullBody_LeftHandThumbTip = OVRPlugin.BoneId.FullBody_LeftHandThumbTip,
            FullBody_LeftHandIndexMetacarpal = OVRPlugin.BoneId.FullBody_LeftHandIndexMetacarpal,
            FullBody_LeftHandIndexProximal = OVRPlugin.BoneId.FullBody_LeftHandIndexProximal,
            FullBody_LeftHandIndexIntermediate = OVRPlugin.BoneId.FullBody_LeftHandIndexIntermediate,
            FullBody_LeftHandIndexDistal = OVRPlugin.BoneId.FullBody_LeftHandIndexDistal,
            FullBody_LeftHandIndexTip = OVRPlugin.BoneId.FullBody_LeftHandIndexTip,
            FullBody_LeftHandMiddleMetacarpal = OVRPlugin.BoneId.FullBody_LeftHandMiddleMetacarpal,
            FullBody_LeftHandMiddleProximal = OVRPlugin.BoneId.FullBody_LeftHandMiddleProximal,
            FullBody_LeftHandMiddleIntermediate = OVRPlugin.BoneId.FullBody_LeftHandMiddleIntermediate,
            FullBody_LeftHandMiddleDistal = OVRPlugin.BoneId.FullBody_LeftHandMiddleDistal,
            FullBody_LeftHandMiddleTip = OVRPlugin.BoneId.FullBody_LeftHandMiddleTip,
            FullBody_LeftHandRingMetacarpal = OVRPlugin.BoneId.FullBody_LeftHandRingMetacarpal,
            FullBody_LeftHandRingProximal = OVRPlugin.BoneId.FullBody_LeftHandRingProximal,
            FullBody_LeftHandRingIntermediate = OVRPlugin.BoneId.FullBody_LeftHandRingIntermediate,
            FullBody_LeftHandRingDistal = OVRPlugin.BoneId.FullBody_LeftHandRingDistal,
            FullBody_LeftHandRingTip = OVRPlugin.BoneId.FullBody_LeftHandRingTip,
            FullBody_LeftHandLittleMetacarpal = OVRPlugin.BoneId.FullBody_LeftHandLittleMetacarpal,
            FullBody_LeftHandLittleProximal = OVRPlugin.BoneId.FullBody_LeftHandLittleProximal,
            FullBody_LeftHandLittleIntermediate = OVRPlugin.BoneId.FullBody_LeftHandLittleIntermediate,
            FullBody_LeftHandLittleDistal = OVRPlugin.BoneId.FullBody_LeftHandLittleDistal,
            FullBody_LeftHandLittleTip = OVRPlugin.BoneId.FullBody_LeftHandLittleTip,
            FullBody_RightHandPalm = OVRPlugin.BoneId.FullBody_RightHandPalm,
            FullBody_RightHandWrist = OVRPlugin.BoneId.FullBody_RightHandWrist,
            FullBody_RightHandThumbMetacarpal = OVRPlugin.BoneId.FullBody_RightHandThumbMetacarpal,
            FullBody_RightHandThumbProximal = OVRPlugin.BoneId.FullBody_RightHandThumbProximal,
            FullBody_RightHandThumbDistal = OVRPlugin.BoneId.FullBody_RightHandThumbDistal,
            FullBody_RightHandThumbTip = OVRPlugin.BoneId.FullBody_RightHandThumbTip,
            FullBody_RightHandIndexMetacarpal = OVRPlugin.BoneId.FullBody_RightHandIndexMetacarpal,
            FullBody_RightHandIndexProximal = OVRPlugin.BoneId.FullBody_RightHandIndexProximal,
            FullBody_RightHandIndexIntermediate = OVRPlugin.BoneId.FullBody_RightHandIndexIntermediate,
            FullBody_RightHandIndexDistal = OVRPlugin.BoneId.FullBody_RightHandIndexDistal,
            FullBody_RightHandIndexTip = OVRPlugin.BoneId.FullBody_RightHandIndexTip,
            FullBody_RightHandMiddleMetacarpal = OVRPlugin.BoneId.FullBody_RightHandMiddleMetacarpal,
            FullBody_RightHandMiddleProximal = OVRPlugin.BoneId.FullBody_RightHandMiddleProximal,
            FullBody_RightHandMiddleIntermediate = OVRPlugin.BoneId.FullBody_RightHandMiddleIntermediate,
            FullBody_RightHandMiddleDistal = OVRPlugin.BoneId.FullBody_RightHandMiddleDistal,
            FullBody_RightHandMiddleTip = OVRPlugin.BoneId.FullBody_RightHandMiddleTip,
            FullBody_RightHandRingMetacarpal = OVRPlugin.BoneId.FullBody_RightHandRingMetacarpal,
            FullBody_RightHandRingProximal = OVRPlugin.BoneId.FullBody_RightHandRingProximal,
            FullBody_RightHandRingIntermediate = OVRPlugin.BoneId.FullBody_RightHandRingIntermediate,
            FullBody_RightHandRingDistal = OVRPlugin.BoneId.FullBody_RightHandRingDistal,
            FullBody_RightHandRingTip = OVRPlugin.BoneId.FullBody_RightHandRingTip,
            FullBody_RightHandLittleMetacarpal = OVRPlugin.BoneId.FullBody_RightHandLittleMetacarpal,
            FullBody_RightHandLittleProximal = OVRPlugin.BoneId.FullBody_RightHandLittleProximal,
            FullBody_RightHandLittleIntermediate = OVRPlugin.BoneId.FullBody_RightHandLittleIntermediate,
            FullBody_RightHandLittleDistal = OVRPlugin.BoneId.FullBody_RightHandLittleDistal,
            FullBody_RightHandLittleTip = OVRPlugin.BoneId.FullBody_RightHandLittleTip,
            FullBody_LeftUpperLeg = OVRPlugin.BoneId.FullBody_LeftUpperLeg,
            FullBody_LeftLowerLeg = OVRPlugin.BoneId.FullBody_LeftLowerLeg,
            FullBody_LeftFootAnkleTwist = OVRPlugin.BoneId.FullBody_LeftFootAnkleTwist,
            FullBody_LeftFootAnkle = OVRPlugin.BoneId.FullBody_LeftFootAnkle,
            FullBody_LeftFootSubtalar = OVRPlugin.BoneId.FullBody_LeftFootSubtalar,
            FullBody_LeftFootTransverse = OVRPlugin.BoneId.FullBody_LeftFootTransverse,
            FullBody_LeftFootBall = OVRPlugin.BoneId.FullBody_LeftFootBall,
            FullBody_RightUpperLeg = OVRPlugin.BoneId.FullBody_RightUpperLeg,
            FullBody_RightLowerLeg = OVRPlugin.BoneId.FullBody_RightLowerLeg,
            FullBody_RightFootAnkleTwist = OVRPlugin.BoneId.FullBody_RightFootAnkleTwist,
            FullBody_RightFootAnkle = OVRPlugin.BoneId.FullBody_RightFootAnkle,
            FullBody_RightFootSubtalar = OVRPlugin.BoneId.FullBody_RightFootSubtalar,
            FullBody_RightFootTransverse = OVRPlugin.BoneId.FullBody_RightFootTransverse,
            FullBody_RightFootBall = OVRPlugin.BoneId.FullBody_RightFootBall,
            FullBody_End = OVRPlugin.BoneId.FullBody_End,

            // add new bones here

            NoOverride = OVRPlugin.BoneId.FullBody_End + 1,
            Remove = OVRPlugin.BoneId.FullBody_End + 2
        };

        /// <summary>
        /// Paired OVRSkeleton bones with human body bones.
        /// </summary>
        public static readonly Dictionary<OVRSkeleton.BoneId, HumanBodyBones> FullBodyBoneIdToHumanBodyBone =
            new Dictionary<OVRSkeleton.BoneId, HumanBodyBones>()
            {
                { OVRSkeleton.BoneId.FullBody_Hips, HumanBodyBones.Hips },
                { OVRSkeleton.BoneId.FullBody_SpineLower, HumanBodyBones.Spine },
                { OVRSkeleton.BoneId.FullBody_SpineUpper, HumanBodyBones.Chest },
                { OVRSkeleton.BoneId.FullBody_Chest, HumanBodyBones.UpperChest },
                { OVRSkeleton.BoneId.FullBody_Neck, HumanBodyBones.Neck },
                { OVRSkeleton.BoneId.FullBody_Head, HumanBodyBones.Head },
                { OVRSkeleton.BoneId.FullBody_LeftShoulder, HumanBodyBones.LeftShoulder },
                { OVRSkeleton.BoneId.FullBody_LeftArmUpper, HumanBodyBones.LeftUpperArm },
                { OVRSkeleton.BoneId.FullBody_LeftArmLower, HumanBodyBones.LeftLowerArm },
                { OVRSkeleton.BoneId.FullBody_LeftHandWrist, HumanBodyBones.LeftHand },
                { OVRSkeleton.BoneId.FullBody_RightShoulder, HumanBodyBones.RightShoulder },
                { OVRSkeleton.BoneId.FullBody_RightArmUpper, HumanBodyBones.RightUpperArm },
                { OVRSkeleton.BoneId.FullBody_RightArmLower, HumanBodyBones.RightLowerArm },
                { OVRSkeleton.BoneId.FullBody_RightHandWrist, HumanBodyBones.RightHand },
                { OVRSkeleton.BoneId.FullBody_LeftHandThumbMetacarpal, HumanBodyBones.LeftThumbProximal },
                { OVRSkeleton.BoneId.FullBody_LeftHandThumbProximal, HumanBodyBones.LeftThumbIntermediate },
                { OVRSkeleton.BoneId.FullBody_LeftHandThumbDistal, HumanBodyBones.LeftThumbDistal },
                { OVRSkeleton.BoneId.FullBody_LeftHandIndexProximal, HumanBodyBones.LeftIndexProximal },
                { OVRSkeleton.BoneId.FullBody_LeftHandIndexIntermediate, HumanBodyBones.LeftIndexIntermediate },
                { OVRSkeleton.BoneId.FullBody_LeftHandIndexDistal, HumanBodyBones.LeftIndexDistal },
                { OVRSkeleton.BoneId.FullBody_LeftHandMiddleProximal, HumanBodyBones.LeftMiddleProximal },
                { OVRSkeleton.BoneId.FullBody_LeftHandMiddleIntermediate, HumanBodyBones.LeftMiddleIntermediate },
                { OVRSkeleton.BoneId.FullBody_LeftHandMiddleDistal, HumanBodyBones.LeftMiddleDistal },
                { OVRSkeleton.BoneId.FullBody_LeftHandRingProximal, HumanBodyBones.LeftRingProximal },
                { OVRSkeleton.BoneId.FullBody_LeftHandRingIntermediate, HumanBodyBones.LeftRingIntermediate },
                { OVRSkeleton.BoneId.FullBody_LeftHandRingDistal, HumanBodyBones.LeftRingDistal },
                { OVRSkeleton.BoneId.FullBody_LeftHandLittleProximal, HumanBodyBones.LeftLittleProximal },
                { OVRSkeleton.BoneId.FullBody_LeftHandLittleIntermediate, HumanBodyBones.LeftLittleIntermediate },
                { OVRSkeleton.BoneId.FullBody_LeftHandLittleDistal, HumanBodyBones.LeftLittleDistal },
                { OVRSkeleton.BoneId.FullBody_RightHandThumbMetacarpal, HumanBodyBones.RightThumbProximal },
                { OVRSkeleton.BoneId.FullBody_RightHandThumbProximal, HumanBodyBones.RightThumbIntermediate },
                { OVRSkeleton.BoneId.FullBody_RightHandThumbDistal, HumanBodyBones.RightThumbDistal },
                { OVRSkeleton.BoneId.FullBody_RightHandIndexProximal, HumanBodyBones.RightIndexProximal },
                { OVRSkeleton.BoneId.FullBody_RightHandIndexIntermediate, HumanBodyBones.RightIndexIntermediate },
                { OVRSkeleton.BoneId.FullBody_RightHandIndexDistal, HumanBodyBones.RightIndexDistal },
                { OVRSkeleton.BoneId.FullBody_RightHandMiddleProximal, HumanBodyBones.RightMiddleProximal },
                { OVRSkeleton.BoneId.FullBody_RightHandMiddleIntermediate, HumanBodyBones.RightMiddleIntermediate },
                { OVRSkeleton.BoneId.FullBody_RightHandMiddleDistal, HumanBodyBones.RightMiddleDistal },
                { OVRSkeleton.BoneId.FullBody_RightHandRingProximal, HumanBodyBones.RightRingProximal },
                { OVRSkeleton.BoneId.FullBody_RightHandRingIntermediate, HumanBodyBones.RightRingIntermediate },
                { OVRSkeleton.BoneId.FullBody_RightHandRingDistal, HumanBodyBones.RightRingDistal },
                { OVRSkeleton.BoneId.FullBody_RightHandLittleProximal, HumanBodyBones.RightLittleProximal },
                { OVRSkeleton.BoneId.FullBody_RightHandLittleIntermediate, HumanBodyBones.RightLittleIntermediate },
                { OVRSkeleton.BoneId.FullBody_RightHandLittleDistal, HumanBodyBones.RightLittleDistal },
                { OVRSkeleton.BoneId.FullBody_LeftUpperLeg, HumanBodyBones.LeftUpperLeg },
                { OVRSkeleton.BoneId.FullBody_LeftLowerLeg, HumanBodyBones.LeftLowerLeg },
                { OVRSkeleton.BoneId.FullBody_LeftFootAnkle, HumanBodyBones.LeftFoot },
                { OVRSkeleton.BoneId.FullBody_LeftFootBall, HumanBodyBones.LeftToes },
                { OVRSkeleton.BoneId.FullBody_RightUpperLeg, HumanBodyBones.RightUpperLeg },
                { OVRSkeleton.BoneId.FullBody_RightLowerLeg, HumanBodyBones.RightLowerLeg },
                { OVRSkeleton.BoneId.FullBody_RightFootAnkle, HumanBodyBones.RightFoot },
                { OVRSkeleton.BoneId.FullBody_RightFootBall, HumanBodyBones.RightToes },
            };

        /// <summary>
        /// For each humanoid bone, create a pair that determines the
        /// pair of bones that create the joint pair. Used to
        /// create the "axis" of the bone.
        /// </summary>
        public static readonly Dictionary<OVRSkeleton.BoneId, Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>>
            FullBoneIdToJointPair = new Dictionary<OVRSkeleton.BoneId, Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>>()
            {
                {
                    OVRSkeleton.BoneId.FullBody_Neck, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_Neck,
                        OVRSkeleton.BoneId.FullBody_Head)
                },
                {
                    OVRSkeleton.BoneId.FullBody_Head, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_Head,
                        OVRSkeleton.BoneId.Invalid)
                },
                {
                    OVRSkeleton.BoneId.FullBody_Root, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_Root,
                        OVRSkeleton.BoneId.FullBody_Hips)
                },
                {
                    OVRSkeleton.BoneId.FullBody_Hips, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_Hips,
                        OVRSkeleton.BoneId.FullBody_SpineLower)
                },
                {
                    OVRSkeleton.BoneId.FullBody_SpineLower, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_SpineLower,
                        OVRSkeleton.BoneId.FullBody_SpineMiddle)
                },
                {
                    OVRSkeleton.BoneId.FullBody_SpineMiddle, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_SpineMiddle,
                        OVRSkeleton.BoneId.FullBody_SpineUpper)
                },
                {
                    OVRSkeleton.BoneId.FullBody_SpineUpper, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_SpineUpper,
                        OVRSkeleton.BoneId.FullBody_Chest)
                },
                {
                    OVRSkeleton.BoneId.FullBody_Chest, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_Chest,
                        OVRSkeleton.BoneId.FullBody_Neck)
                },
                {
                    OVRSkeleton.BoneId.FullBody_LeftShoulder, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_LeftShoulder,
                        OVRSkeleton.BoneId.FullBody_LeftArmUpper)
                },
                {
                    OVRSkeleton.BoneId.FullBody_LeftScapula, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_LeftScapula,
                        OVRSkeleton.BoneId.FullBody_LeftArmUpper)
                },
                {
                    OVRSkeleton.BoneId.FullBody_LeftArmUpper, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_LeftArmUpper,
                        OVRSkeleton.BoneId.FullBody_LeftArmLower)
                },
                {
                    OVRSkeleton.BoneId.FullBody_LeftArmLower, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_LeftArmLower,
                        OVRSkeleton.BoneId.FullBody_LeftHandWrist)
                },
                {
                    OVRSkeleton.BoneId.FullBody_LeftHandWrist, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_LeftHandWrist,
                        OVRSkeleton.BoneId.FullBody_LeftHandMiddleMetacarpal)
                },
                {
                    OVRSkeleton.BoneId.FullBody_LeftHandPalm, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_LeftHandPalm,
                        OVRSkeleton.BoneId.FullBody_LeftHandMiddleMetacarpal)
                },
                {
                    OVRSkeleton.BoneId.FullBody_LeftHandWristTwist, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_LeftHandWristTwist,
                        OVRSkeleton.BoneId.FullBody_LeftHandMiddleMetacarpal)
                },
                {
                    OVRSkeleton.BoneId.FullBody_LeftHandThumbMetacarpal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_LeftHandThumbMetacarpal,
                        OVRSkeleton.BoneId.FullBody_LeftHandThumbProximal)
                },
                {
                    OVRSkeleton.BoneId.FullBody_LeftHandThumbProximal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_LeftHandThumbProximal,
                        OVRSkeleton.BoneId.FullBody_LeftHandThumbDistal)
                },
                {
                    OVRSkeleton.BoneId.FullBody_LeftHandThumbDistal, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_LeftHandThumbDistal,
                        OVRSkeleton.BoneId.FullBody_LeftHandThumbTip)
                },
                {
                    OVRSkeleton.BoneId.FullBody_LeftHandThumbTip, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_LeftHandThumbDistal,
                        OVRSkeleton.BoneId.FullBody_LeftHandThumbTip)
                },
                {
                    OVRSkeleton.BoneId.FullBody_LeftHandIndexMetacarpal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_LeftHandIndexMetacarpal,
                        OVRSkeleton.BoneId.FullBody_LeftHandIndexProximal)
                },
                {
                    OVRSkeleton.BoneId.FullBody_LeftHandIndexProximal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_LeftHandIndexProximal,
                        OVRSkeleton.BoneId.FullBody_LeftHandIndexIntermediate)
                },
                {
                    OVRSkeleton.BoneId.FullBody_LeftHandIndexIntermediate,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_LeftHandIndexIntermediate,
                        OVRSkeleton.BoneId.FullBody_LeftHandIndexDistal)
                },
                {
                    OVRSkeleton.BoneId.FullBody_LeftHandIndexDistal, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_LeftHandIndexDistal,
                        OVRSkeleton.BoneId.FullBody_LeftHandIndexTip)
                },
                {
                    OVRSkeleton.BoneId.FullBody_LeftHandIndexTip, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_LeftHandIndexDistal,
                        OVRSkeleton.BoneId.FullBody_LeftHandIndexTip)
                },
                {
                    OVRSkeleton.BoneId.FullBody_LeftHandMiddleMetacarpal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_LeftHandMiddleMetacarpal,
                        OVRSkeleton.BoneId.FullBody_LeftHandMiddleProximal)
                },
                {
                    OVRSkeleton.BoneId.FullBody_LeftHandMiddleProximal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_LeftHandMiddleProximal,
                        OVRSkeleton.BoneId.FullBody_LeftHandMiddleIntermediate)
                },
                {
                    OVRSkeleton.BoneId.FullBody_LeftHandMiddleIntermediate,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_LeftHandMiddleIntermediate,
                        OVRSkeleton.BoneId.FullBody_LeftHandMiddleDistal)
                },
                {
                    OVRSkeleton.BoneId.FullBody_LeftHandMiddleDistal, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_LeftHandMiddleDistal,
                        OVRSkeleton.BoneId.FullBody_LeftHandMiddleTip)
                },
                {
                    OVRSkeleton.BoneId.FullBody_LeftHandMiddleTip, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_LeftHandMiddleDistal,
                        OVRSkeleton.BoneId.FullBody_LeftHandMiddleTip)
                },
                {
                    OVRSkeleton.BoneId.FullBody_LeftHandRingMetacarpal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_LeftHandRingMetacarpal,
                        OVRSkeleton.BoneId.FullBody_LeftHandRingProximal)
                },
                {
                    OVRSkeleton.BoneId.FullBody_LeftHandRingProximal, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_LeftHandRingProximal,
                        OVRSkeleton.BoneId.FullBody_LeftHandRingIntermediate)
                },
                {
                    OVRSkeleton.BoneId.FullBody_LeftHandRingIntermediate,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_LeftHandRingIntermediate,
                        OVRSkeleton.BoneId.FullBody_LeftHandRingDistal)
                },
                {
                    OVRSkeleton.BoneId.FullBody_LeftHandRingDistal, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_LeftHandRingDistal,
                        OVRSkeleton.BoneId.FullBody_LeftHandRingTip)
                },
                {
                    OVRSkeleton.BoneId.FullBody_LeftHandRingTip, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_LeftHandRingDistal,
                        OVRSkeleton.BoneId.FullBody_LeftHandRingTip)
                },
                {
                    OVRSkeleton.BoneId.FullBody_LeftHandLittleMetacarpal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_LeftHandLittleMetacarpal,
                        OVRSkeleton.BoneId.FullBody_LeftHandLittleProximal)
                },
                {
                    OVRSkeleton.BoneId.FullBody_LeftHandLittleProximal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_LeftHandLittleProximal,
                        OVRSkeleton.BoneId.FullBody_LeftHandLittleIntermediate)
                },
                {
                    OVRSkeleton.BoneId.FullBody_LeftHandLittleIntermediate,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_LeftHandLittleIntermediate,
                        OVRSkeleton.BoneId.FullBody_LeftHandLittleDistal)
                },
                {
                    OVRSkeleton.BoneId.FullBody_LeftHandLittleDistal, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_LeftHandLittleDistal,
                        OVRSkeleton.BoneId.FullBody_LeftHandLittleTip)
                },
                {
                    OVRSkeleton.BoneId.FullBody_LeftHandLittleTip, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_LeftHandLittleDistal,
                        OVRSkeleton.BoneId.FullBody_LeftHandRingTip)
                },
                {
                    OVRSkeleton.BoneId.FullBody_RightShoulder, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_RightShoulder,
                        OVRSkeleton.BoneId.FullBody_RightArmUpper)
                },
                {
                    OVRSkeleton.BoneId.FullBody_RightScapula, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_RightScapula,
                        OVRSkeleton.BoneId.FullBody_RightArmUpper)
                },
                {
                    OVRSkeleton.BoneId.FullBody_RightArmUpper, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_RightArmUpper,
                        OVRSkeleton.BoneId.FullBody_RightArmLower)
                },
                {
                    OVRSkeleton.BoneId.FullBody_RightArmLower, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_RightArmLower,
                        OVRSkeleton.BoneId.FullBody_RightHandWrist)
                },
                {
                    OVRSkeleton.BoneId.FullBody_RightHandWrist, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_RightHandWrist,
                        OVRSkeleton.BoneId.FullBody_RightHandMiddleMetacarpal)
                },
                {
                    OVRSkeleton.BoneId.FullBody_RightHandPalm, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_RightHandPalm,
                        OVRSkeleton.BoneId.FullBody_RightHandMiddleMetacarpal)
                },
                {
                    OVRSkeleton.BoneId.FullBody_RightHandWristTwist, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_RightHandWristTwist,
                        OVRSkeleton.BoneId.FullBody_RightHandMiddleMetacarpal)
                },
                {
                    OVRSkeleton.BoneId.FullBody_RightHandThumbMetacarpal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_RightHandThumbMetacarpal,
                        OVRSkeleton.BoneId.FullBody_RightHandThumbProximal)
                },
                {
                    OVRSkeleton.BoneId.FullBody_RightHandThumbProximal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_RightHandThumbProximal,
                        OVRSkeleton.BoneId.FullBody_RightHandThumbDistal)
                },
                {
                    OVRSkeleton.BoneId.FullBody_RightHandThumbDistal, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_RightHandThumbDistal,
                        OVRSkeleton.BoneId.FullBody_RightHandThumbTip)
                },
                {
                    OVRSkeleton.BoneId.FullBody_RightHandThumbTip, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_RightHandThumbDistal,
                        OVRSkeleton.BoneId.FullBody_RightHandThumbTip)
                },
                {
                    OVRSkeleton.BoneId.FullBody_RightHandIndexMetacarpal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_RightHandIndexMetacarpal,
                        OVRSkeleton.BoneId.FullBody_RightHandIndexProximal)
                },
                {
                    OVRSkeleton.BoneId.FullBody_RightHandIndexProximal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_RightHandIndexProximal,
                        OVRSkeleton.BoneId.FullBody_RightHandIndexIntermediate)
                },
                {
                    OVRSkeleton.BoneId.FullBody_RightHandIndexIntermediate,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_RightHandIndexIntermediate,
                        OVRSkeleton.BoneId.FullBody_RightHandIndexDistal)
                },
                {
                    OVRSkeleton.BoneId.FullBody_RightHandIndexDistal, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_RightHandIndexDistal,
                        OVRSkeleton.BoneId.FullBody_RightHandIndexTip)
                },
                {
                    OVRSkeleton.BoneId.FullBody_RightHandIndexTip, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_RightHandIndexDistal,
                        OVRSkeleton.BoneId.FullBody_RightHandIndexTip)
                },
                {
                    OVRSkeleton.BoneId.FullBody_RightHandMiddleMetacarpal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_RightHandMiddleMetacarpal,
                        OVRSkeleton.BoneId.FullBody_RightHandMiddleProximal)
                },
                {
                    OVRSkeleton.BoneId.FullBody_RightHandMiddleProximal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_RightHandMiddleProximal,
                        OVRSkeleton.BoneId.FullBody_RightHandMiddleIntermediate)
                },
                {
                    OVRSkeleton.BoneId.FullBody_RightHandMiddleIntermediate,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_RightHandMiddleIntermediate,
                        OVRSkeleton.BoneId.FullBody_RightHandMiddleDistal)
                },
                {
                    OVRSkeleton.BoneId.FullBody_RightHandMiddleDistal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_RightHandMiddleDistal,
                        OVRSkeleton.BoneId.FullBody_RightHandMiddleTip)
                },
                {
                    OVRSkeleton.BoneId.FullBody_RightHandMiddleTip, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_RightHandMiddleDistal,
                        OVRSkeleton.BoneId.FullBody_RightHandMiddleTip)
                },
                {
                    OVRSkeleton.BoneId.FullBody_RightHandRingMetacarpal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_RightHandRingMetacarpal,
                        OVRSkeleton.BoneId.FullBody_RightHandRingProximal)
                },
                {
                    OVRSkeleton.BoneId.FullBody_RightHandRingProximal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_RightHandRingProximal,
                        OVRSkeleton.BoneId.FullBody_RightHandRingIntermediate)
                },
                {
                    OVRSkeleton.BoneId.FullBody_RightHandRingIntermediate,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_RightHandRingIntermediate,
                        OVRSkeleton.BoneId.FullBody_RightHandRingDistal)
                },
                {
                    OVRSkeleton.BoneId.FullBody_RightHandRingDistal, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_RightHandRingDistal,
                        OVRSkeleton.BoneId.FullBody_RightHandRingTip)
                },
                {
                    OVRSkeleton.BoneId.FullBody_RightHandRingTip, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_RightHandRingDistal,
                        OVRSkeleton.BoneId.FullBody_RightHandRingTip)
                },
                {
                    OVRSkeleton.BoneId.FullBody_RightHandLittleMetacarpal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_RightHandLittleMetacarpal,
                        OVRSkeleton.BoneId.FullBody_RightHandLittleProximal)
                },
                {
                    OVRSkeleton.BoneId.FullBody_RightHandLittleProximal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_RightHandLittleProximal,
                        OVRSkeleton.BoneId.FullBody_RightHandLittleIntermediate)
                },
                {
                    OVRSkeleton.BoneId.FullBody_RightHandLittleIntermediate,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_RightHandLittleIntermediate,
                        OVRSkeleton.BoneId.FullBody_RightHandLittleDistal)
                },
                {
                    OVRSkeleton.BoneId.FullBody_RightHandLittleDistal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_RightHandLittleDistal,
                        OVRSkeleton.BoneId.FullBody_RightHandLittleTip)
                },
                {
                    OVRSkeleton.BoneId.FullBody_RightHandLittleTip, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_RightHandLittleDistal,
                        OVRSkeleton.BoneId.FullBody_RightHandRingTip)
                },
                {
                    OVRSkeleton.BoneId.FullBody_LeftUpperLeg, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_LeftUpperLeg,
                        OVRSkeleton.BoneId.FullBody_LeftLowerLeg)
                },
                {
                    OVRSkeleton.BoneId.FullBody_LeftLowerLeg, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_LeftLowerLeg,
                        OVRSkeleton.BoneId.FullBody_LeftFootAnkle)
                },
                {
                    OVRSkeleton.BoneId.FullBody_LeftFootAnkle, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_LeftFootAnkle,
                        OVRSkeleton.BoneId.FullBody_LeftFootBall)
                },
                {
                    OVRSkeleton.BoneId.FullBody_LeftFootBall, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_LeftFootAnkle,
                        OVRSkeleton.BoneId.FullBody_LeftFootBall)
                },
                {
                    OVRSkeleton.BoneId.FullBody_RightUpperLeg, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_RightUpperLeg,
                        OVRSkeleton.BoneId.FullBody_RightLowerLeg)
                },
                {
                    OVRSkeleton.BoneId.FullBody_RightLowerLeg, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_RightLowerLeg,
                        OVRSkeleton.BoneId.FullBody_RightFootAnkle)
                },
                {
                    OVRSkeleton.BoneId.FullBody_RightFootAnkle, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_RightFootAnkle,
                        OVRSkeleton.BoneId.FullBody_RightFootBall)
                },
                {
                    OVRSkeleton.BoneId.FullBody_RightFootBall, new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.FullBody_RightFootAnkle,
                        OVRSkeleton.BoneId.FullBody_RightFootBall)
                },
            };
    }
}
