// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using static Meta.XR.Movement.MSDKUtility;

namespace Meta.XR.Movement.Retargeting
{
    /// <summary>
    /// Serializable class used to store data relevant for retargeting.
    /// Contains skeleton hierarchy, T-pose data, known joints, and manifestations.
    /// </summary>
    [Serializable]
    public class SkeletonData
    {
        /// <summary>
        /// Full body tracking bone id.
        /// </summary>
        public enum FullBodyTrackingBoneId
        {
            Start = OVRPlugin.BoneId.FullBody_Start,
            Root = OVRPlugin.BoneId.FullBody_Root,
            Hips = OVRPlugin.BoneId.FullBody_Hips,
            SpineLower = OVRPlugin.BoneId.FullBody_SpineLower,
            SpineMiddle = OVRPlugin.BoneId.FullBody_SpineMiddle,
            SpineUpper = OVRPlugin.BoneId.FullBody_SpineUpper,
            Chest = OVRPlugin.BoneId.FullBody_Chest,
            Neck = OVRPlugin.BoneId.FullBody_Neck,
            Head = OVRPlugin.BoneId.FullBody_Head,
            LeftShoulder = OVRPlugin.BoneId.FullBody_LeftShoulder,
            LeftScapula = OVRPlugin.BoneId.FullBody_LeftScapula,
            LeftArmUpper = OVRPlugin.BoneId.FullBody_LeftArmUpper,
            LeftArmLower = OVRPlugin.BoneId.FullBody_LeftArmLower,
            LeftHandWristTwist = OVRPlugin.BoneId.FullBody_LeftHandWristTwist,
            RightShoulder = OVRPlugin.BoneId.FullBody_RightShoulder,
            RightScapula = OVRPlugin.BoneId.FullBody_RightScapula,
            RightArmUpper = OVRPlugin.BoneId.FullBody_RightArmUpper,
            RightArmLower = OVRPlugin.BoneId.FullBody_RightArmLower,
            RightHandWristTwist = OVRPlugin.BoneId.FullBody_RightHandWristTwist,
            LeftHandPalm = OVRPlugin.BoneId.FullBody_LeftHandPalm,
            LeftHandWrist = OVRPlugin.BoneId.FullBody_LeftHandWrist,
            LeftHandThumbMetacarpal = OVRPlugin.BoneId.FullBody_LeftHandThumbMetacarpal,
            LeftHandThumbProximal = OVRPlugin.BoneId.FullBody_LeftHandThumbProximal,
            LeftHandThumbDistal = OVRPlugin.BoneId.FullBody_LeftHandThumbDistal,
            LeftHandThumbTip = OVRPlugin.BoneId.FullBody_LeftHandThumbTip,
            LeftHandIndexMetacarpal = OVRPlugin.BoneId.FullBody_LeftHandIndexMetacarpal,
            LeftHandIndexProximal = OVRPlugin.BoneId.FullBody_LeftHandIndexProximal,
            LeftHandIndexIntermediate = OVRPlugin.BoneId.FullBody_LeftHandIndexIntermediate,
            LeftHandIndexDistal = OVRPlugin.BoneId.FullBody_LeftHandIndexDistal,
            LeftHandIndexTip = OVRPlugin.BoneId.FullBody_LeftHandIndexTip,
            LeftHandMiddleMetacarpal = OVRPlugin.BoneId.FullBody_LeftHandMiddleMetacarpal,
            LeftHandMiddleProximal = OVRPlugin.BoneId.FullBody_LeftHandMiddleProximal,
            LeftHandMiddleIntermediate = OVRPlugin.BoneId.FullBody_LeftHandMiddleIntermediate,
            LeftHandMiddleDistal = OVRPlugin.BoneId.FullBody_LeftHandMiddleDistal,
            LeftHandMiddleTip = OVRPlugin.BoneId.FullBody_LeftHandMiddleTip,
            LeftHandRingMetacarpal = OVRPlugin.BoneId.FullBody_LeftHandRingMetacarpal,
            LeftHandRingProximal = OVRPlugin.BoneId.FullBody_LeftHandRingProximal,
            LeftHandRingIntermediate = OVRPlugin.BoneId.FullBody_LeftHandRingIntermediate,
            LeftHandRingDistal = OVRPlugin.BoneId.FullBody_LeftHandRingDistal,
            LeftHandRingTip = OVRPlugin.BoneId.FullBody_LeftHandRingTip,
            LeftHandLittleMetacarpal = OVRPlugin.BoneId.FullBody_LeftHandLittleMetacarpal,
            LeftHandLittleProximal = OVRPlugin.BoneId.FullBody_LeftHandLittleProximal,
            LeftHandLittleIntermediate = OVRPlugin.BoneId.FullBody_LeftHandLittleIntermediate,
            LeftHandLittleDistal = OVRPlugin.BoneId.FullBody_LeftHandLittleDistal,
            LeftHandLittleTip = OVRPlugin.BoneId.FullBody_LeftHandLittleTip,
            RightHandPalm = OVRPlugin.BoneId.FullBody_RightHandPalm,
            RightHandWrist = OVRPlugin.BoneId.FullBody_RightHandWrist,
            RightHandThumbMetacarpal = OVRPlugin.BoneId.FullBody_RightHandThumbMetacarpal,
            RightHandThumbProximal = OVRPlugin.BoneId.FullBody_RightHandThumbProximal,
            RightHandThumbDistal = OVRPlugin.BoneId.FullBody_RightHandThumbDistal,
            RightHandThumbTip = OVRPlugin.BoneId.FullBody_RightHandThumbTip,
            RightHandIndexMetacarpal = OVRPlugin.BoneId.FullBody_RightHandIndexMetacarpal,
            RightHandIndexProximal = OVRPlugin.BoneId.FullBody_RightHandIndexProximal,
            RightHandIndexIntermediate = OVRPlugin.BoneId.FullBody_RightHandIndexIntermediate,
            RightHandIndexDistal = OVRPlugin.BoneId.FullBody_RightHandIndexDistal,
            RightHandIndexTip = OVRPlugin.BoneId.FullBody_RightHandIndexTip,
            RightHandMiddleMetacarpal = OVRPlugin.BoneId.FullBody_RightHandMiddleMetacarpal,
            RightHandMiddleProximal = OVRPlugin.BoneId.FullBody_RightHandMiddleProximal,
            RightHandMiddleIntermediate = OVRPlugin.BoneId.FullBody_RightHandMiddleIntermediate,
            RightHandMiddleDistal = OVRPlugin.BoneId.FullBody_RightHandMiddleDistal,
            RightHandMiddleTip = OVRPlugin.BoneId.FullBody_RightHandMiddleTip,
            RightHandRingMetacarpal = OVRPlugin.BoneId.FullBody_RightHandRingMetacarpal,
            RightHandRingProximal = OVRPlugin.BoneId.FullBody_RightHandRingProximal,
            RightHandRingIntermediate = OVRPlugin.BoneId.FullBody_RightHandRingIntermediate,
            RightHandRingDistal = OVRPlugin.BoneId.FullBody_RightHandRingDistal,
            RightHandRingTip = OVRPlugin.BoneId.FullBody_RightHandRingTip,
            RightHandLittleMetacarpal = OVRPlugin.BoneId.FullBody_RightHandLittleMetacarpal,
            RightHandLittleProximal = OVRPlugin.BoneId.FullBody_RightHandLittleProximal,
            RightHandLittleIntermediate = OVRPlugin.BoneId.FullBody_RightHandLittleIntermediate,
            RightHandLittleDistal = OVRPlugin.BoneId.FullBody_RightHandLittleDistal,
            RightHandLittleTip = OVRPlugin.BoneId.FullBody_RightHandLittleTip,
            LeftUpperLeg = OVRPlugin.BoneId.FullBody_LeftUpperLeg,
            LeftLowerLeg = OVRPlugin.BoneId.FullBody_LeftLowerLeg,
            LeftFootAnkleTwist = OVRPlugin.BoneId.FullBody_LeftFootAnkleTwist,
            LeftFootAnkle = OVRPlugin.BoneId.FullBody_LeftFootAnkle,
            LeftFootSubtalar = OVRPlugin.BoneId.FullBody_LeftFootSubtalar,
            LeftFootTransverse = OVRPlugin.BoneId.FullBody_LeftFootTransverse,
            LeftFootBall = OVRPlugin.BoneId.FullBody_LeftFootBall,
            RightUpperLeg = OVRPlugin.BoneId.FullBody_RightUpperLeg,
            RightLowerLeg = OVRPlugin.BoneId.FullBody_RightLowerLeg,
            RightFootAnkleTwist = OVRPlugin.BoneId.FullBody_RightFootAnkleTwist,
            RightFootAnkle = OVRPlugin.BoneId.FullBody_RightFootAnkle,
            RightFootSubtalar = OVRPlugin.BoneId.FullBody_RightFootSubtalar,
            RightFootTransverse = OVRPlugin.BoneId.FullBody_RightFootTransverse,
            RightFootBall = OVRPlugin.BoneId.FullBody_RightFootBall,
            End = OVRPlugin.BoneId.FullBody_End,

            // add new bones here
            NoOverride = OVRPlugin.BoneId.FullBody_End + 1,
            Remove = OVRPlugin.BoneId.FullBody_End + 2
        };

        /// <summary>
        /// Half body tracking bone id.
        /// </summary>
        public enum BodyTrackingBoneId
        {
            Start = OVRPlugin.BoneId.Body_Start,
            Root = OVRPlugin.BoneId.Body_Root,
            Hips = OVRPlugin.BoneId.Body_Hips,
            SpineLower = OVRPlugin.BoneId.Body_SpineLower,
            SpineMiddle = OVRPlugin.BoneId.Body_SpineMiddle,
            SpineUpper = OVRPlugin.BoneId.Body_SpineUpper,
            Chest = OVRPlugin.BoneId.Body_Chest,
            Neck = OVRPlugin.BoneId.Body_Neck,
            Head = OVRPlugin.BoneId.Body_Head,
            LeftShoulder = OVRPlugin.BoneId.Body_LeftShoulder,
            LeftScapula = OVRPlugin.BoneId.Body_LeftScapula,
            LeftArmUpper = OVRPlugin.BoneId.Body_LeftArmUpper,
            LeftArmLower = OVRPlugin.BoneId.Body_LeftArmLower,
            LeftHandWristTwist = OVRPlugin.BoneId.Body_LeftHandWristTwist,
            RightShoulder = OVRPlugin.BoneId.Body_RightShoulder,
            RightScapula = OVRPlugin.BoneId.Body_RightScapula,
            RightArmUpper = OVRPlugin.BoneId.Body_RightArmUpper,
            RightArmLower = OVRPlugin.BoneId.Body_RightArmLower,
            RightHandWristTwist = OVRPlugin.BoneId.Body_RightHandWristTwist,
            LeftHandPalm = OVRPlugin.BoneId.Body_LeftHandPalm,
            LeftHandWrist = OVRPlugin.BoneId.Body_LeftHandWrist,
            LeftHandThumbMetacarpal = OVRPlugin.BoneId.Body_LeftHandThumbMetacarpal,
            LeftHandThumbProximal = OVRPlugin.BoneId.Body_LeftHandThumbProximal,
            LeftHandThumbDistal = OVRPlugin.BoneId.Body_LeftHandThumbDistal,
            LeftHandThumbTip = OVRPlugin.BoneId.Body_LeftHandThumbTip,
            LeftHandIndexMetacarpal = OVRPlugin.BoneId.Body_LeftHandIndexMetacarpal,
            LeftHandIndexProximal = OVRPlugin.BoneId.Body_LeftHandIndexProximal,
            LeftHandIndexIntermediate = OVRPlugin.BoneId.Body_LeftHandIndexIntermediate,
            LeftHandIndexDistal = OVRPlugin.BoneId.Body_LeftHandIndexDistal,
            LeftHandIndexTip = OVRPlugin.BoneId.Body_LeftHandIndexTip,
            LeftHandMiddleMetacarpal = OVRPlugin.BoneId.Body_LeftHandMiddleMetacarpal,
            LeftHandMiddleProximal = OVRPlugin.BoneId.Body_LeftHandMiddleProximal,
            LeftHandMiddleIntermediate = OVRPlugin.BoneId.Body_LeftHandMiddleIntermediate,
            LeftHandMiddleDistal = OVRPlugin.BoneId.Body_LeftHandMiddleDistal,
            LeftHandMiddleTip = OVRPlugin.BoneId.Body_LeftHandMiddleTip,
            LeftHandRingMetacarpal = OVRPlugin.BoneId.Body_LeftHandRingMetacarpal,
            LeftHandRingProximal = OVRPlugin.BoneId.Body_LeftHandRingProximal,
            LeftHandRingIntermediate = OVRPlugin.BoneId.Body_LeftHandRingIntermediate,
            LeftHandRingDistal = OVRPlugin.BoneId.Body_LeftHandRingDistal,
            LeftHandRingTip = OVRPlugin.BoneId.Body_LeftHandRingTip,
            LeftHandLittleMetacarpal = OVRPlugin.BoneId.Body_LeftHandLittleMetacarpal,
            LeftHandLittleProximal = OVRPlugin.BoneId.Body_LeftHandLittleProximal,
            LeftHandLittleIntermediate = OVRPlugin.BoneId.Body_LeftHandLittleIntermediate,
            LeftHandLittleDistal = OVRPlugin.BoneId.Body_LeftHandLittleDistal,
            LeftHandLittleTip = OVRPlugin.BoneId.Body_LeftHandLittleTip,
            RightHandPalm = OVRPlugin.BoneId.Body_RightHandPalm,
            RightHandWrist = OVRPlugin.BoneId.Body_RightHandWrist,
            RightHandThumbMetacarpal = OVRPlugin.BoneId.Body_RightHandThumbMetacarpal,
            RightHandThumbProximal = OVRPlugin.BoneId.Body_RightHandThumbProximal,
            RightHandThumbDistal = OVRPlugin.BoneId.Body_RightHandThumbDistal,
            RightHandThumbTip = OVRPlugin.BoneId.Body_RightHandThumbTip,
            RightHandIndexMetacarpal = OVRPlugin.BoneId.Body_RightHandIndexMetacarpal,
            RightHandIndexProximal = OVRPlugin.BoneId.Body_RightHandIndexProximal,
            RightHandIndexIntermediate = OVRPlugin.BoneId.Body_RightHandIndexIntermediate,
            RightHandIndexDistal = OVRPlugin.BoneId.Body_RightHandIndexDistal,
            RightHandIndexTip = OVRPlugin.BoneId.Body_RightHandIndexTip,
            RightHandMiddleMetacarpal = OVRPlugin.BoneId.Body_RightHandMiddleMetacarpal,
            RightHandMiddleProximal = OVRPlugin.BoneId.Body_RightHandMiddleProximal,
            RightHandMiddleIntermediate = OVRPlugin.BoneId.Body_RightHandMiddleIntermediate,
            RightHandMiddleDistal = OVRPlugin.BoneId.Body_RightHandMiddleDistal,
            RightHandMiddleTip = OVRPlugin.BoneId.Body_RightHandMiddleTip,
            RightHandRingMetacarpal = OVRPlugin.BoneId.Body_RightHandRingMetacarpal,
            RightHandRingProximal = OVRPlugin.BoneId.Body_RightHandRingProximal,
            RightHandRingIntermediate = OVRPlugin.BoneId.Body_RightHandRingIntermediate,
            RightHandRingDistal = OVRPlugin.BoneId.Body_RightHandRingDistal,
            RightHandRingTip = OVRPlugin.BoneId.Body_RightHandRingTip,
            RightHandLittleMetacarpal = OVRPlugin.BoneId.Body_RightHandLittleMetacarpal,
            RightHandLittleProximal = OVRPlugin.BoneId.Body_RightHandLittleProximal,
            RightHandLittleIntermediate = OVRPlugin.BoneId.Body_RightHandLittleIntermediate,
            RightHandLittleDistal = OVRPlugin.BoneId.Body_RightHandLittleDistal,
            RightHandLittleTip = OVRPlugin.BoneId.Body_RightHandLittleTip,
            End = OVRPlugin.BoneId.Body_End,

            // add new bones here
            NoOverride = OVRPlugin.BoneId.Body_End + 1,
            Remove = OVRPlugin.BoneId.Body_End + 2
        };

        /// <summary>
        /// Gets the number of joints in the skeleton.
        /// </summary>
        public int JointCount => Joints?.Length ?? 0;

        [SerializeField] private NativeTransform[] _tPoseArray;
        [SerializeField] private NativeTransform[] _minTPoseArray;
        [SerializeField] private NativeTransform[] _maxTPoseArray;
        [SerializeField] private string[] _joints;
        [SerializeField] private string[] _parentJoints;
        [SerializeField] private string[] _knownJoints;
        [SerializeField] private string[] _autoMapExcludedJointNames;
        [SerializeField] private string[] _manifestationNames;
        [SerializeField] private int[] _manifestationJointCounts;
        [SerializeField] private string[] _manifestationJointNames;
        [SerializeField] private int[] _parentIndices;
        [SerializeField] private int _rootJointIndex = -1;
        [SerializeField] private int _hipsJointIndex = -1;
        [SerializeField] private int _headJointIndex = -1;
        [SerializeField] private int _leftUpperLegJointIndex = -1;
        [SerializeField] private int _rightUpperLegJointIndex = -1;
        [SerializeField] private int _leftLowerLegJointIndex = -1;
        [SerializeField] private int _rightLowerLegJointIndex = -1;
        [SerializeField] private int[] _fingerIndices;
        [SerializeField] private string[] _manifestations;

        /// <summary>
        /// Array of poses representing the T-pose configuration of the skeleton.
        /// </summary>
        public NativeTransform[] TPoseArray => _tPoseArray;

        /// <summary>
        /// Gets the minimum T-pose transforms as regular array.
        /// </summary>
        public NativeTransform[] MinTPoseArray => _minTPoseArray;

        /// <summary>
        /// Gets the maximum T-pose transforms as regular array.
        /// </summary>
        public NativeTransform[] MaxTPoseArray => _maxTPoseArray;

        /// <summary>
        /// Array of joint names in the skeleton.
        /// </summary>
        public string[] Joints => _joints;

        /// <summary>
        /// Array of parent joint names corresponding to each joint in the skeleton.
        /// </summary>
        public string[] ParentJoints => _parentJoints;

        /// <summary>
        /// Array of known joint names that have been identified and mapped in the skeleton.
        /// </summary>
        public string[] KnownJoints => _knownJoints;

        /// <summary>
        /// Array of joint names that should be excluded from automatic mapping operations.
        /// </summary>
        public string[] AutoMapExcludedJointNames => _autoMapExcludedJointNames;

        /// <summary>
        /// Array of manifestation names available in the skeleton configuration.
        /// </summary>
        public string[] ManifestationNames => _manifestationNames;

        /// <summary>
        /// Array containing the number of joints in each manifestation.
        /// </summary>
        public int[] ManifestationJointCounts => _manifestationJointCounts;

        /// <summary>
        /// Array of joint names organized by manifestation.
        /// </summary>
        public string[] ManifestationJointNames => _manifestationJointNames;

        /// <summary>
        /// Array of parent joint indices corresponding to each joint in the skeleton hierarchy.
        /// </summary>
        public int[] ParentIndices => _parentIndices;

        /// <summary>
        /// Index of the root joint in the skeleton hierarchy, or -1 if not found.
        /// </summary>
        public int RootJointIndex => _rootJointIndex;

        /// <summary>
        /// Index of the hips joint in the skeleton, or -1 if not found.
        /// </summary>
        public int HipsJointIndex => _hipsJointIndex;

        /// <summary>
        /// Index of the head joint in the skeleton, or -1 if not found.
        /// </summary>
        public int HeadJointIndex => _headJointIndex;

        /// <summary>
        /// Index of the left upper leg joint in the skeleton, or -1 if not found.
        /// </summary>
        public int LeftUpperLegJointIndex => _leftUpperLegJointIndex;

        /// <summary>
        /// Index of the right upper leg joint in the skeleton, or -1 if not found.
        /// </summary>
        public int RightUpperLegJointIndex => _rightUpperLegJointIndex;

        /// <summary>
        /// Index of the left lower leg joint in the skeleton, or -1 if not found.
        /// </summary>
        public int LeftLowerLegJointIndex => _leftLowerLegJointIndex;

        /// <summary>
        /// Index of the right lower leg joint in the skeleton, or -1 if not found.
        /// </summary>
        public int RightLowerLegJointIndex => _rightLowerLegJointIndex;

        /// <summary>
        /// Array of joint indices that represent finger joints (descendants of wrist joints).
        /// </summary>
        public int[] FingerIndices => _fingerIndices;

        /// <summary>
        /// Array of manifestation names available for this skeleton.
        /// </summary>
        public string[] Manifestations => _manifestations;

        /// <summary>
        /// Creates a SkeletonData instance from configuration JSON using MSDKUtility API.
        /// </summary>
        /// <param name="configJson">The configuration JSON string.</param>
        /// <param name="skeletonType">The type of skeleton (Source or Target).</param>
        /// <returns>A new SkeletonData instance initialized from the config.</returns>
        public static SkeletonData CreateFromConfig(string configJson, SkeletonType skeletonType)
        {
            if (!CreateOrUpdateHandle(configJson, out var configHandle))
            {
                throw new Exception("Failed to create configuration handle from JSON.");
            }

            try
            {
                return CreateFromHandle(configHandle, skeletonType);
            }
            finally
            {
                // Clean up the handle
                DestroyHandle(configHandle);
            }
        }

        /// <summary>
        /// Creates a SkeletonData instance from an existing configuration handle using MSDKUtility API.
        /// </summary>
        /// <param name="configHandle">The existing configuration handle.</param>
        /// <param name="skeletonType">The type of skeleton (Source or Target).</param>
        /// <returns>A new SkeletonData instance initialized from the handle.</returns>
        public static SkeletonData CreateFromHandle(ulong configHandle, SkeletonType skeletonType)
        {
            var skeletonData = new SkeletonData();

            // Get skeleton info
            GetSkeletonInfo(configHandle, skeletonType, out var skeletonInfo);
            var jointCount = skeletonInfo.JointCount;

            // Extract joint hierarchy data using MSDKUtility API
            GetJointNames(configHandle, skeletonType, out var joints);
            GetParentJointNames(configHandle, skeletonType, out var parentJoints);
            GetKnownJointNames(configHandle, skeletonType, out var knownJoints);
            GetManifestationNames(configHandle, skeletonType, out var manifestationNames);
            GetAutoMappingExcludedJoints(configHandle, skeletonType, out var autoMapExcludedJoints);

            skeletonData._joints = joints;
            skeletonData._parentJoints = parentJoints;
            skeletonData._knownJoints = knownJoints;
            skeletonData._manifestationNames = manifestationNames;
            skeletonData._autoMapExcludedJointNames = autoMapExcludedJoints;

            // Get manifestation data
            var manifestationJointCounts = new List<int>();
            var manifestationJointNames = new List<string>();
            foreach (var manifestationName in skeletonData.ManifestationNames)
            {
                GetJointsInManifestation(configHandle, skeletonType, manifestationName, out var jointsInManifestation);
                manifestationJointCounts.Add(jointsInManifestation.Length);
                manifestationJointNames.AddRange(jointsInManifestation.Select(jointIndex =>
                    skeletonData.Joints[jointIndex]));
            }

            skeletonData._manifestationJointCounts = manifestationJointCounts.ToArray();
            skeletonData._manifestationJointNames = manifestationJointNames.ToArray();

            // Extract T-pose data using MSDKUtility API
            var tPose = new NativeArray<NativeTransform>(jointCount, Allocator.Temp,
                NativeArrayOptions.UninitializedMemory);
            var tPoseMin =
                new NativeArray<NativeTransform>(jointCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var tPoseMax =
                new NativeArray<NativeTransform>(jointCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            GetSkeletonTPoseByRef(configHandle, skeletonType, SkeletonTPoseType.UnscaledTPose,
                JointRelativeSpaceType.RootOriginRelativeSpace, ref tPose);
            GetSkeletonTPoseByRef(configHandle, skeletonType, SkeletonTPoseType.MinTPose,
                JointRelativeSpaceType.RootOriginRelativeSpace, ref tPoseMin);
            GetSkeletonTPoseByRef(configHandle, skeletonType, SkeletonTPoseType.MaxTPose,
                JointRelativeSpaceType.RootOriginRelativeSpace, ref tPoseMax);

            // Get parent indices
            var parentIndices =
                new NativeArray<int>(jointCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            GetParentJointIndexesByRef(configHandle, skeletonType, ref parentIndices);
            skeletonData._parentIndices = parentIndices.ToArray();

            // Get known joint indices and set properties
            GetJointIndexByKnownJointType(configHandle, skeletonType, KnownJointType.Root, out var rootJointIndex);
            GetJointIndexByKnownJointType(configHandle, skeletonType, KnownJointType.Hips, out var hipsJointIndex);
            GetJointIndexByKnownJointType(configHandle, skeletonType, KnownJointType.Neck, out var headJointIndex);
            GetJointIndexByKnownJointType(configHandle, skeletonType, KnownJointType.LeftUpperLeg,
                out var leftUpperLegJointIndex);
            GetJointIndexByKnownJointType(configHandle, skeletonType, KnownJointType.RightUpperLeg,
                out var rightUpperLegJointIndex);

            // Set the properties
            skeletonData._rootJointIndex = rootJointIndex;
            skeletonData._hipsJointIndex = hipsJointIndex;
            skeletonData._headJointIndex = headJointIndex;
            skeletonData._leftUpperLegJointIndex = leftUpperLegJointIndex;
            skeletonData._rightUpperLegJointIndex = rightUpperLegJointIndex;

            // Get child joint indices for lower legs
            GetChildJointIndexes(configHandle, skeletonType, leftUpperLegJointIndex, out var leftLowerLegIndices);
            GetChildJointIndexes(configHandle, skeletonType, rightUpperLegJointIndex, out var rightLowerLegIndices);
            skeletonData._leftLowerLegJointIndex = leftLowerLegIndices.Length > 0
                ? leftLowerLegIndices[0]
                : INVALID_JOINT_INDEX;
            skeletonData._rightLowerLegJointIndex = rightLowerLegIndices.Length > 0
                ? rightLowerLegIndices[0]
                : INVALID_JOINT_INDEX;

            // Calculate finger indices (joints that have wrist parents)
            GetJointIndexByKnownJointType(configHandle, skeletonType, KnownJointType.LeftWrist, out var leftWristIndex);
            GetJointIndexByKnownJointType(configHandle, skeletonType, KnownJointType.RightWrist,
                out var rightWristIndex);

            var fingerIndicesList = new List<int>();
            for (var i = 0; i < jointCount; i++)
            {
                // Check if this joint or any of its parents are wrist joints
                var currentJoint = i;
                while (currentJoint != -1)
                {
                    if (currentJoint == leftWristIndex || currentJoint == rightWristIndex)
                    {
                        fingerIndicesList.Add(i);
                        break;
                    }

                    currentJoint = skeletonData.ParentIndices[currentJoint];
                }
            }

            skeletonData._fingerIndices = fingerIndicesList.ToArray();

            // Convert T-pose NativeArrays to regular arrays for storage
            skeletonData._tPoseArray = tPose.ToArray();
            skeletonData._minTPoseArray = tPoseMin.ToArray();
            skeletonData._maxTPoseArray = tPoseMax.ToArray();

            // Set other properties
            skeletonData._manifestations = skeletonData.ManifestationNames;

            parentIndices.Dispose();
            tPose.Dispose();
            tPoseMin.Dispose();
            tPoseMax.Dispose();

            return skeletonData;
        }

        /// <summary>
        /// Creates a SkeletonData instance from a Unity Transform hierarchy.
        /// Extracts joint hierarchy and T-pose data from the transform tree.
        /// </summary>
        /// <param name="target">The root transform of the skeleton hierarchy.</param>
        /// <returns>A new SkeletonData instance created from the transform hierarchy, or null if the hierarchy is invalid.</returns>
        public static SkeletonData CreateFromTransform(Transform target)
        {
            var jointMapping = MSDKUtilityHelper.GetChildParentJointMapping(target, out var root);
            if (jointMapping == null)
            {
                return null;
            }

            var data = new SkeletonData();
            var positionScale = 1.0f / root.localScale.x;
            var index = 0;
            var jointCount = jointMapping.Keys.Count;

            // Build parent indices array and joint mappings
            var jointToIndexMap = new Dictionary<Transform, int>();
            data._parentIndices = new int[jointCount];
            data._joints = new string[jointCount];
            data._parentJoints = new string[jointCount];
            data._tPoseArray = new NativeTransform[jointCount];
            data._minTPoseArray = new NativeTransform[jointCount];
            data._maxTPoseArray = new NativeTransform[jointCount];

            // First pass: assign indices to all joints
            foreach (var jointPair in jointMapping)
            {
                jointToIndexMap[jointPair.Key] = index;
                index++;
            }

            // Second pass: populate data arrays
            index = 0;
            foreach (var jointPair in jointMapping)
            {
                var joint = jointPair.Key;
                var parentJoint = jointPair.Value;

                data._joints[index] = joint.name;
                if (parentJoint == null)
                {
                    data._parentJoints[index] = string.Empty;
                    data._parentIndices[index] = -1;
                }
                else
                {
                    data._parentJoints[index] = parentJoint.name;
                    data._parentIndices[index] = jointToIndexMap.GetValueOrDefault(parentJoint, -1);
                }

                data._tPoseArray[index] = new Pose(joint.position * positionScale, joint.rotation);
                index++;
            }

            // Set T-pose min/max to the same as T-pose (no joint limits from transform)
            data._minTPoseArray = data._tPoseArray;
            data._maxTPoseArray = data._tPoseArray;
            data._knownJoints = KnownJointFinder.FindKnownJoints(data.Joints, data.ParentJoints);
            data._autoMapExcludedJointNames = Array.Empty<string>();
            data._manifestationNames = Array.Empty<string>();
            data._manifestationJointCounts = Array.Empty<int>();
            data._manifestationJointNames = Array.Empty<string>();
            data._manifestations = Array.Empty<string>();

            return data;
        }

        public void SetJoints(string[] joints)
        {
            _joints = joints;
        }

        public void SetParentJoints(string[] parentJoints)
        {
            _parentJoints = parentJoints;
        }

        public void SetKnownJoints(string[] knownJoints)
        {
            _knownJoints = knownJoints;
        }

        /// <summary>
        /// Filters the skeleton to only include joints specified in the filter array.
        /// Removes joints not found in the filter and updates parent references accordingly.
        /// </summary>
        /// <param name="jointsToFilter">Array of joint names to keep in the skeleton.</param>
        public void FilterJoints(string[] jointsToFilter)
        {
            // Filter out any extra joints not found in config.
            foreach (var joint in Joints)
            {
                if (!jointsToFilter.Contains(joint))
                {
                    RemoveJoint(joint);
                }
            }

            for (var i = 0; i < ParentJoints.Length; i++)
            {
                var parentJoint = ParentJoints[i];
                if (!jointsToFilter.Contains(parentJoint))
                {
                    ParentJoints[i] = string.Empty;
                }
            }
        }

        /// <summary>
        /// Fills a SkeletonInitParams structure with data from this SkeletonData instance.
        /// </summary>
        /// <returns>A SkeletonInitParams populated with this skeleton's data.</returns>
        public SkeletonInitParams FillConfigInitParams()
        {
            var initParams = new SkeletonInitParams
            {
                // Basic skeleton structure
                JointNames = Joints,
                ParentJointNames = ParentJoints,
                // T-pose data
                UnscaledTPose = new NativeArray<NativeTransform>(TPoseArray, Allocator.Temp),
                MinTPose = new NativeArray<NativeTransform>(MinTPoseArray, Allocator.Temp),
                MaxTPose = new NativeArray<NativeTransform>(MaxTPoseArray, Allocator.Temp),
                // Optional data
                OptionalKnownSourceJointNamesById = KnownJoints,
                OptionalAutoMapExcludedJointNames = AutoMapExcludedJointNames,
                OptionalManifestationNames = ManifestationNames,
                OptionalManifestationJointCounts = ManifestationJointCounts,
                OptionalManifestationJointNames = ManifestationJointNames,
                // Blend shape data (empty for skeleton data)
                BlendShapeNames = Array.Empty<string>()
            };

            return initParams;
        }

        /// <summary>
        /// Removes a joint from the skeleton data and updates parent references.
        /// </summary>
        /// <param name="jointToRemove">The name of the joint to remove.</param>
        public void RemoveJoint(string jointToRemove)
        {
            var indexToRemove = Array.IndexOf(Joints, jointToRemove);
            if (indexToRemove == -1)
            {
                return;
            }

            _joints = _joints.Where((_, index) => index != indexToRemove).ToArray();
            _parentJoints = _parentJoints.Where((_, index) => index != indexToRemove).ToArray();
            _tPoseArray = _tPoseArray.Where((_, index) => index != indexToRemove).ToArray();
            _minTPoseArray = _minTPoseArray.Where((_, index) => index != indexToRemove).ToArray();
            _maxTPoseArray = _maxTPoseArray.Where((_, index) => index != indexToRemove).ToArray();

            // Re-parent the joint.
            for (var i = 0; i < ParentJoints.Length; i++)
            {
                if (ParentJoints[i] == jointToRemove)
                {
                    _parentJoints[i] = ParentJoints[indexToRemove];
                }
            }
        }

        private static int GetBoneId(string jointName)
        {
            foreach (var boneId in Enum.GetValues(typeof(FullBodyTrackingBoneId)))
            {
                if (boneId.ToString() == jointName)
                {
                    return (int)boneId;
                }
            }

            return -1;
        }
    }
}
