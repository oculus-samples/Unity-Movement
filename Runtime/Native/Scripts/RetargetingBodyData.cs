// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Meta.XR.Movement
{
    /// <summary>
    /// Scriptable object used to store data relevant for retargeting.
    /// </summary>
    public class RetargetingBodyData : ScriptableObject
    {
        private enum FullBodyTrackingBoneId
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

        public string[] Joints
        {
            get => _joints;
            set => _joints = value;
        }

        public string[] ParentJoints
        {
            get => _parentJoints;
            set => _parentJoints = value;
        }

        public Pose[] TPose
        {
            get => _tPose;
            set => _tPose = value;
        }

        public Pose[] TPoseMin
        {
            get => _tPoseMin;
            set => _tPoseMin = value;
        }

        public Pose[] TPoseMax
        {
            get => _tPoseMax;
            set => _tPoseMax = value;
        }

        [SerializeField]
        private string[] _joints;

        [SerializeField]
        private string[] _parentJoints;

        [SerializeField]
        private Pose[] _tPose;

        [SerializeField]
        private Pose[] _tPoseMin;

        [SerializeField]
        private Pose[] _tPoseMax;

        public void Initialize(int jointCount)
        {
            _joints = new string[jointCount];
            _parentJoints = new string[jointCount];
            _tPose = new Pose[jointCount];
            _tPoseMin = new Pose[jointCount];
            _tPoseMax = new Pose[jointCount];
        }

        public void SortData()
        {
            // Map joint names to their indices
            var jointIndices = new Dictionary<string, int>();
            for (int i = 0; i < _joints.Length; i++)
            {
                jointIndices.Add(_joints[i], i);
            }

            // Sort the joints using the BoneId enum
            Array.Sort(_joints, (a, b) => GetBoneId(a).CompareTo(GetBoneId(b)));

            // Update the ParentJoints and TPose arrays to match the sorted joints
            for (var i = 0; i < _joints.Length; i++)
            {
                var index = jointIndices[_joints[i]];
                _parentJoints[i] = _parentJoints[index];
                _tPose[i] = _tPose[index];
                _tPoseMin[i] = _tPoseMin[index];
                _tPoseMax[i] = _tPoseMax[index];
            }
        }

        private static int GetBoneId(string jointName)
        {
            foreach (FullBodyTrackingBoneId boneId in Enum.GetValues(typeof(FullBodyTrackingBoneId)))
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
