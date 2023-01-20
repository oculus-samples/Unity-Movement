// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Oculus.Movement.Tracking
{
    /// <summary>
    /// Skeleton meta data class, associated with each HumanyBodyBone enum.
    /// </summary>
    public class SkeletonMetadata
    {
        /// <summary>
        /// Data associated per bone.
        /// </summary>
        public class BoneData
        {
            /// <summary>
            /// Transform associated with joint.
            /// </summary>
            public Transform OriginalJoint;

            /// <summary>
            /// From position for joint pair, for debugging.
            /// </summary>
            public Vector3 FromPosition;

            /// <summary>
            /// To position for joint pair, for debugging.
            /// </summary>
            public Vector3 ToPosition;

            /// <summary>
            /// Start of joint pair (usually the original joint).
            /// </summary>
            public Transform JointPairStart;

            /// <summary>
            /// End of joint pair.
            /// </summary>
            public Transform JointPairEnd;

            /// <summary>
            /// Orientation or rotation corresponding to joint pair.
            /// If multiplied by forward, produces a coordinate axis.
            /// </summary>
            public Quaternion JointPairOrientation;

            /// <summary>
            /// Offset quaternion, used for retargeting rotations.
            /// </summary>
            public Quaternion? CorrectionQuaternion;

            /// <summary>
            /// Parent transform of joint. This is defined in a special way for OVRSkeleton,
            /// so we have to cache it ahead of time.
            /// </summary>
            public Transform ParentTransform;
        }

        /// <summary>
        /// Human body bone enum to bone data mapping.
        /// </summary>
        public Dictionary<HumanBodyBones, BoneData> BodyToBoneData
            => _bodyToBoneData;

        private Dictionary<HumanBodyBones, BoneData> _bodyToBoneData =
            new Dictionary<HumanBodyBones, BoneData>();

        private HumanBodyBones[] _boneEnumValues =
            (HumanBodyBones[])Enum.GetValues(typeof(HumanBodyBones));

        /// <summary>
        /// Main constructor.
        /// </summary>
        /// <param name="animator">Animator to build meta data from.</param>
        public SkeletonMetadata(Animator animator)
        {
            BuildBoneData(animator);
        }

        /// <summary>
        /// Constructor OVRSkeleton.
        /// </summary>
        /// <param name="skeleton">Skeleton to build meta data from.</param>
        /// <param name="useBindPose">Whether to use bind pose (T-pose) or not.</param>
        /// <param name="customBoneIdToHumanBodyBone">Custom bone ID to human body bone mapping.</param>
        public SkeletonMetadata(OVRSkeleton skeleton, bool useBindPose,
            Dictionary<OVRSkeleton.BoneId, HumanBodyBones> customBoneIdToHumanBodyBone)
        {
            BuildBoneDataSkeleton(skeleton, useBindPose, customBoneIdToHumanBodyBone);
        }

        /// <summary>
        /// Builds body to bone data with the OVRSkeleton.
        /// </summary>
        /// <param name="skeleton">The OVRSkeleton.</param>
        /// <param name="useBindPose">If true, use the bind pose.</param>
        /// <param name="customBoneIdToHumanBodyBone">Custom bone ID to human body bone mapping.</param>
        public void BuildBoneDataSkeleton(OVRSkeleton skeleton, bool useBindPose,
            Dictionary<OVRSkeleton.BoneId, HumanBodyBones> customBoneIdToHumanBodyBone)
        {
            if (_bodyToBoneData.Count != 0)
            {
                _bodyToBoneData.Clear();
            }

            var allBones = useBindPose ? skeleton.BindPoses : skeleton.Bones;
            var numBones = allBones.Count;
            for (int i = 0; i < numBones; i++)
            {
                var bone = allBones[i];
                var boneId = bone.Id;

                if (!customBoneIdToHumanBodyBone.ContainsKey(boneId))
                {
                    continue;
                }
                var humanBodyBone = customBoneIdToHumanBodyBone[boneId];

                BoneData boneData = new BoneData();
                boneData.OriginalJoint = bone.Transform;

                if (!HumanBodyBonesMappings.BoneIdToJointPair.ContainsKey(boneId))
                {
                    Debug.LogError($"CAN'T find {boneId} in bone Id to joint pair map!");
                }

                var jointPair = HumanBodyBonesMappings.BoneIdToJointPair[boneId];
                var startOfPair = jointPair.Item1;
                var endofPair = jointPair.Item2;

                // for some tip transforms, the start of pair starts with one joint before
                boneData.JointPairStart = (startOfPair == boneId) ?
                    bone.Transform : FindBoneWithBoneId(allBones, startOfPair).Transform;
                boneData.JointPairEnd = endofPair != OVRSkeleton.BoneId.Invalid ?
                    FindBoneWithBoneId(allBones, endofPair).Transform :
                    boneData.JointPairStart;
                boneData.ParentTransform = allBones[bone.ParentBoneIndex].Transform;

                if (boneData.JointPairStart == null)
                {
                    Debug.LogWarning($"{boneId} has invalid start joint.");
                }
                if (boneData.JointPairEnd == null)
                {
                    Debug.LogWarning($"{boneId} has invalid end joint.");
                }

                _bodyToBoneData.Add(humanBodyBone, boneData);
            }
        }

        private OVRBone FindBoneWithBoneId(IList<OVRBone> bones, OVRSkeleton.BoneId boneId)
        {
            var numBones = bones.Count;
            for (int i = 0; i < numBones; i++)
            {
                var bone = bones[i];
                if (bone.Id == boneId)
                {
                    return bone;
                }
            }
            return null;
        }

        /// <summary>
        /// Builds body to bone data with an Animator.
        /// </summary>
        /// <param name="animator"></param>
        public void BuildBoneData(Animator animator)
        {
            if (_bodyToBoneData.Count != 0)
            {
                _bodyToBoneData.Clear();
            }
            foreach (HumanBodyBones humanBodyBone in _boneEnumValues)
            {
                if (humanBodyBone == HumanBodyBones.LastBone)
                {
                    continue;
                }

                var currTransform = animator.GetBoneTransform(humanBodyBone);
                if (currTransform == null)
                {
                    continue;
                }

                BoneData boneData = new BoneData();
                boneData.OriginalJoint = currTransform;
                _bodyToBoneData.Add(humanBodyBone, boneData);
            }

            // Find paired joints after all transforms have been tracked.
            // A joint start starts from a joints, ends at its child joint, and serves
            // as the "axis" of the joint.
            foreach (var key in _bodyToBoneData.Keys)
            {
                var boneData = _bodyToBoneData[key];
                var jointPair = HumanBodyBonesMappings.BoneToJointPair[key];

                boneData.JointPairStart = jointPair.Item1 != HumanBodyBones.LastBone ?
                    animator.GetBoneTransform(jointPair.Item1) : boneData.OriginalJoint;

                boneData.JointPairEnd = jointPair.Item2 != HumanBodyBones.LastBone ?
                    animator.GetBoneTransform(jointPair.Item2) :
                    FindFirstChild(boneData.OriginalJoint, boneData.OriginalJoint);

                boneData.ParentTransform = boneData.OriginalJoint.parent;

                if (boneData.JointPairStart == null)
                {
                    Debug.LogWarning($"{key} has invalid start joint.");
                }
                if (boneData.JointPairEnd == null)
                {
                    Debug.LogWarning($"{key} has invalid end joint.");
                }
            }
        }

        /// <summary>
        /// Log joint pairs.
        /// </summary>
        public void PrintJointPairs()
        {
            StringBuilder hiearachyString = new StringBuilder();
            foreach (var key in _bodyToBoneData.Keys)
            {
                var boneData = _bodyToBoneData[key];
                hiearachyString.Append($"Paired bone joint of {key} is " +
                    $"{boneData.JointPairStart}-{boneData.JointPairEnd}.\n");
            }
            Debug.Log(hiearachyString.ToString());
        }

        /// <summary>
        /// Builds coordinate axes for all bones. If a reference bone data
        /// collection has been passed in, use its coordinate system (per
        /// bone) as a reference for computation.
        /// </summary>
        public void BuildCoordinateAxesForAllBones()
        {
            foreach (var key in _bodyToBoneData.Keys)
            {
                var boneData = _bodyToBoneData[key];

                var jointPairStartPosition = boneData.JointPairStart.position;
                var jointPairEndPosition = Vector3.zero;

                // Edge case: joint pair end is null or same node. If that's the case,
                // make joint pair end follow the axis from previous node.
                if (boneData.JointPairEnd == null ||
                    boneData.JointPairEnd == boneData.JointPairStart ||
                    (boneData.JointPairEnd.position - boneData.JointPairStart.position).magnitude <
                    Mathf.Epsilon)
                {
                    var node1 = boneData.ParentTransform;
                    var node2 = boneData.JointPairStart;
                    jointPairStartPosition = node1.position;
                    jointPairEndPosition = node2.position;
                }
                else
                {
                    jointPairEndPosition = boneData.JointPairEnd.position;
                }

                // with some joints like hands, fix the right vector. that's because the hand is nice and
                // flat, and the right vector should point to a thumb bone.
                if (key == HumanBodyBones.LeftHand || key == HumanBodyBones.RightHand)
                {
                    var jointToCreateRightVecWith = key == HumanBodyBones.LeftHand ?
                        HumanBodyBones.LeftThumbIntermediate : HumanBodyBones.RightThumbIntermediate;
                    Vector3 rightVec = _bodyToBoneData[jointToCreateRightVecWith].OriginalJoint.position -
                        jointPairStartPosition;
                    boneData.JointPairOrientation =
                        CreateQuaternionForBoneDataWithRightVec(
                            jointPairStartPosition,
                            jointPairEndPosition,
                            rightVec);
                }
                else
                {
                    boneData.JointPairOrientation =
                        CreateQuaternionForBoneData(
                            jointPairStartPosition,
                            jointPairEndPosition);
                }

                boneData.FromPosition   = boneData.OriginalJoint.position;
                boneData.ToPosition     = boneData.OriginalJoint.position +
                    (jointPairEndPosition - jointPairStartPosition);
            }
        }

        private Transform FindFirstChild(
            Transform startTransform,
            Transform currTransform)
        {
            if (startTransform != currTransform)
            {
                return currTransform;
            }

            // Dead end.
            if (currTransform.childCount == 0)
            {
                return null;
            }

            Transform foundChild = null;
            for (int i = 0; i < currTransform.childCount; i++)
            {
                var currChild = FindFirstChild(startTransform,
                    currTransform.GetChild(i));
                if (currChild != null)
                {
                    foundChild = currChild;
                    break;
                }
            }
            return foundChild;
        }

        private Quaternion CreateQuaternionForBoneDataWithRightVec(
            Vector3 fromPosition,
            Vector3 toPosition,
            Vector3 rightVector)
        {
            Vector3 forwardVec = (toPosition - fromPosition).normalized;
            if (forwardVec.sqrMagnitude < Mathf.Epsilon)
            {
                forwardVec = Vector3.forward;
            }
            Vector3 upVector = Vector3.Cross(forwardVec, rightVector);
            return Quaternion.LookRotation(forwardVec, upVector);
        }

        private Quaternion CreateQuaternionForBoneData(
            Vector3 fromPosition,
            Vector3 toPosition,
            Vector3? refUpVec = null)
        {
            Vector3 forwardVec = (toPosition - fromPosition).normalized;
            if (forwardVec.sqrMagnitude < Mathf.Epsilon)
            {
                forwardVec = Vector3.forward;
            }

            return Quaternion.LookRotation(forwardVec);
        }
    }
}
