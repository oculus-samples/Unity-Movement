// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System;
using System.Linq;
using UnityEngine;

namespace Meta.XR.Movement.Retargeting
{
    /// <summary>
    /// Scriptable object used to store data relevant for retargeting.
    /// </summary>
    public class SkeletonData : ScriptableObject
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
        /// Gets or sets the array of joint names in the skeleton.
        /// </summary>
        public string[] Joints
        {
            get => _joints;
            set => _joints = value;
        }

        /// <summary>
        /// Gets or sets the array of parent joint names corresponding to each joint in the skeleton.
        /// </summary>
        public string[] ParentJoints
        {
            get => _parentJoints;
            set => _parentJoints = value;
        }

        /// <summary>
        /// Gets or sets the array of poses representing the T-pose configuration of the skeleton.
        /// </summary>
        public Pose[] TPose
        {
            get => _tPose;
            set => _tPose = value;
        }

        /// <summary>
        /// Gets or sets the array of poses representing the minimum T-pose configuration for joint limits.
        /// </summary>
        public Pose[] TPoseMin
        {
            get => _tPoseMin;
            set => _tPoseMin = value;
        }

        /// <summary>
        /// Gets or sets the array of poses representing the maximum T-pose configuration for joint limits.
        /// </summary>
        public Pose[] TPoseMax
        {
            get => _tPoseMax;
            set => _tPoseMax = value;
        }

        /// <summary>
        /// Array of joint names in the skeleton.
        /// </summary>
        [ContextMenuItem("Sort", "SortData")]
        [SerializeField]
        private string[] _joints;

        /// <summary>
        /// Array of parent joint names corresponding to each joint in the skeleton.
        /// </summary>
        [SerializeField]
        private string[] _parentJoints;

        /// <summary>
        /// Array of poses representing the T-pose configuration of the skeleton.
        /// </summary>
        [SerializeField]
        private Pose[] _tPose;

        /// <summary>
        /// Array of poses representing the minimum T-pose configuration for joint limits.
        /// </summary>
        [SerializeField]
        private Pose[] _tPoseMin;

        /// <summary>
        /// Array of poses representing the maximum T-pose configuration for joint limits.
        /// </summary>
        [SerializeField]
        private Pose[] _tPoseMax;

        /// <summary>
        /// Initializes the skeleton data with arrays of the specified joint count.
        /// </summary>
        /// <param name="jointCount">The number of joints in the skeleton.</param>
        public void Initialize(int jointCount)
        {
            _joints = new string[jointCount];
            _parentJoints = new string[jointCount];
            _tPose = new Pose[jointCount];
            _tPoseMin = new Pose[jointCount];
            _tPoseMax = new Pose[jointCount];
        }

        /// <summary>
        /// Sorts the skeleton data based on bone IDs.
        /// </summary>
        public void SortData()
        {
            var combined =
                _joints.Zip(_parentJoints, (joint, parentJoint) => new { joint, parentJoint })
                .Zip(_tPose, (x, tPose) => new { x.joint, x.parentJoint, tPose })
                .Zip(_tPoseMin, (x, tPoseMin) => new { x.joint, x.parentJoint, x.tPose, tPoseMin })
                .Zip(_tPoseMax, (x, tPoseMax) => new { x.joint, x.parentJoint, x.tPose, x.tPoseMin, tPoseMax });
            var sortedCombined = combined.OrderBy(x => GetBoneId(x.joint.Replace("LittlePinky", "Little")));
            _joints = sortedCombined.Select(x => x.joint).ToArray();
            _parentJoints = sortedCombined.Select(x => x.parentJoint).ToArray();
            _tPose = sortedCombined.Select(x => x.tPose).ToArray();
            _tPoseMin = sortedCombined.Select(x => x.tPoseMin).ToArray();
            _tPoseMax = sortedCombined.Select(x => x.tPoseMax).ToArray();
        }

        /// <summary>
        /// Removes a joint from the skeleton data and updates parent references.
        /// </summary>
        /// <param name="jointToRemove">The name of the joint to remove.</param>
        public void RemoveJoint(string jointToRemove)
        {
            var indexToRemove = Array.IndexOf(_joints, jointToRemove);
            if (indexToRemove != -1)
            {
                _joints = _joints.Where((val, index) => index != indexToRemove).ToArray();
                _parentJoints = _parentJoints.Where((val, index) => index != indexToRemove).ToArray();
                _tPose = _tPose.Where((val, index) => index != indexToRemove).ToArray();
                _tPoseMin = _tPoseMin.Where((val, index) => index != indexToRemove).ToArray();
                _tPoseMax = _tPoseMax.Where((val, index) => index != indexToRemove).ToArray();

                // Reparent the joint.
                for (var i = 0; i < _parentJoints.Length; i++)
                {
                    if (_parentJoints[i] == jointToRemove)
                    {
                        _parentJoints[i] = _parentJoints[indexToRemove];
                    }
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
