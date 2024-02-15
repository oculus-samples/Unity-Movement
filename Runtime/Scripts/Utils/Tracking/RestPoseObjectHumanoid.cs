// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Movement.AnimationRigging;
using System;
using System.Collections.Generic;
using UnityEngine;
using static OVRUnityHumanoidSkeletonRetargeter;

namespace Oculus.Movement.Utils
{
    /// <summary>
    /// Stores the rest pose of a humanoid and allows querying
    /// properties of it.
    /// </summary>
    public class RestPoseObjectHumanoid : ScriptableObject
    {
        /// <summary>
        /// Holds pose information for each bone.
        /// </summary>
        [Serializable]
        public class BonePoseData
        {
            public BonePoseData(
                Transform boneTransform,
                HumanBodyBones humanBone)
            {
                WorldPose = new Pose(boneTransform.position, boneTransform.rotation);
                LocalPose = new Pose(boneTransform.localPosition, boneTransform.localRotation);
                HumanBone = humanBone;
            }

            /// <summary>
            /// World transform.
            /// </summary>
            public Pose WorldPose;

            /// <summary>
            /// Local transform.
            /// </summary>
            public Pose LocalPose;

            /// <summary>
            /// Human body bone enum, if any. If it's equal to
            /// <see cref="HumanBodyBones"/>, then it's not mapped to a humanoid bone
            /// in the original character's avatar description. These bones might
            /// still be useful to track since they describe all of the bones in-between
            /// mapped bones.
            /// </summary>
            public HumanBodyBones HumanBone;
        }

        /// <summary>
        /// A flat array containing all bone pose data.
        /// There should be a dictionary mapping from body bone to pose data,
        /// but it's currently not possible to serialize dictionaries.
        /// </summary>
        [SerializeField]
        protected BonePoseData[] _bonePoseDataArray;

        /// <summary>
        /// Initializes the rest pose data for all bones from the animator.
        /// </summary>
        /// <param name="animator">The humanoid animator.</param>
        /// <exception cref="Exception">Exception thrown if the incoming animator is invalid.</exception>
        public void InitializePoseDataFromAnimator(Animator animator)
        {
            if (!RiggingUtilities.IsHumanoidAnimator(animator))
            {
                throw new Exception($"Cannot produce humanoid rest pose from " +
                    $"non-humanoid {animator.gameObject.name}.");
            }

            var hipsTransform = animator.GetBoneTransform(HumanBodyBones.Hips);
            if (hipsTransform == null)
            {
                throw new Exception($"Humanoid {animator.gameObject.name} is missing " +
                    "a hips transform, a bone necessary for creating rest pose data.");
            }

            List<BonePoseData> bonePoseDataList = new List<BonePoseData>();
            BuildBonePoseDataFromTransform(animator,
                hipsTransform, bonePoseDataList);
            _bonePoseDataArray = bonePoseDataList.ToArray();
        }

        private void BuildBonePoseDataFromTransform(Animator animator,
            Transform originalTransform, List<BonePoseData> bonePoseDataList)
        {
            var humanBodyBone = FindHumanBodyBoneFromTransform(animator, originalTransform);
            BonePoseData newBonePoseData = new BonePoseData(
                originalTransform, humanBodyBone);
            bonePoseDataList.Add(newBonePoseData);

            foreach (Transform originalChild in originalTransform)
            {
                BuildBonePoseDataFromTransform(animator,
                    originalChild, bonePoseDataList);
            }
        }

        private HumanBodyBones FindHumanBodyBoneFromTransform(Animator animator,
            Transform candidateTransform)
        {
            for (var boneIndex = HumanBodyBones.Hips; boneIndex < HumanBodyBones.LastBone; boneIndex++)
            {
                if (animator.GetBoneTransform(boneIndex) == candidateTransform)
                {
                    return boneIndex;
                }
            }

            return HumanBodyBones.LastBone;
        }

        /// <summary>
        /// Returns bone pose data for the given humanoid bone.
        /// </summary>
        /// <param name="humanBone">HumanBodyBones used for the query.</param>
        /// <returns>Bone pose data, if bone is found in the map.</returns>
        public BonePoseData GetBonePoseData(HumanBodyBones humanBone)
        {
            if (_bonePoseDataArray == null)
            {
                throw new Exception("Humanoid rest pose object has not be initialized yet!");
            }

            BonePoseData foundPoseData = null;
            foreach(var bonePoseData in _bonePoseDataArray)
            {
                if (bonePoseData.HumanBone == humanBone)
                {
                    foundPoseData = bonePoseData;
                    break;
                }
            }

            return foundPoseData;
        }

        /// <summary>
        /// Calculates the rotation difference between the bone in the animator
        /// character and the bone in the reference rest pose. This script uses
        /// mappings from the SDK. This is angle from the rest pose to the animator.
        /// </summary>
        /// <param name="otherAnimator">Animator to compare against.</param>
        /// <param name="humanBodyBone">HumanBodyBones joint to be referenced for the
        /// angle comparison.</param>
        /// <returns>Rotation difference.</returns>
        public Quaternion CalculateRotationDifferenceFromRestPoseToAnimatorJoint(
            Animator otherAnimator, HumanBodyBones humanBodyBone)
        {
            // To compute the rotation difference, an axis is required for the
            // animator character and rest pose. Find the joints of the axis first.
            if (!OVRHumanBodyBonesMappings.BoneToJointPair.TryGetValue(
                humanBodyBone, out Tuple<HumanBodyBones, HumanBodyBones> jointPair))
            {
                Debug.LogError($"Could not find the joint pair for {humanBodyBone} in map.");
                return Quaternion.identity;
            }

            return CalculateRotationDifferenceFromRestPoseToAnimatorBonePair(
                otherAnimator, jointPair.Item1, jointPair.Item2);
        }

        /// <summary>
        /// Calculates the rotation difference between the
        /// specified bone pair in the animator character and
        /// the bone pair in the reference rest pose.
        /// </summary>
        /// <param name="otherAnimator">Animator to compare against.</param>
        /// <param name="humanBodyBone">HumanBodyBones joint to be referenced for the
        /// angle comparison.</param>
        /// <param name="otherHumanBodyBone">Other HumanBodyBones joint to be referenced for the
        /// angle comparison.</param>
        /// <returns>Rotation difference.</returns>
        public Quaternion CalculateRotationDifferenceFromRestPoseToAnimatorBonePair(
            Animator otherAnimator, HumanBodyBones humanBodyBone, HumanBodyBones otherHumanBodyBone)
        {
            if (!RiggingUtilities.IsHumanoidAnimator(otherAnimator))
            {
                Debug.LogError("Reference pose is not humanoid character");
                return Quaternion.identity;
            }

            Transform startJointOther = otherAnimator.GetBoneTransform(humanBodyBone),
                endJointOther = otherAnimator.GetBoneTransform(otherHumanBodyBone);
            if (startJointOther == null || endJointOther == null)
            {
                Debug.LogError("Other animator has at least one null joint pair: " +
                    $"{startJointOther}, {endJointOther}. Are the joints properly mapped for " +
                    $"{humanBodyBone}-{otherHumanBodyBone}?");
                return Quaternion.identity;
            }

            var startJointReferenceData = GetBonePoseData(humanBodyBone);
            var endJointReferenceData = GetBonePoseData(otherHumanBodyBone);
            if (startJointReferenceData == null || endJointReferenceData == null)
            {
                Debug.LogError("Reference has at least one null joint pair: " +
                    $"{startJointReferenceData}, {endJointReferenceData}. Are the joints properly mapped for " +
                    $"{humanBodyBone}-{otherHumanBodyBone}?");
                return Quaternion.identity;
            }

            Vector3 jointAxisOther = (endJointOther.position - startJointOther.position);
            Vector3 jointAxisReference =
                (endJointReferenceData.WorldPose.position - startJointReferenceData.WorldPose.position);

            return Quaternion.FromToRotation(jointAxisReference, jointAxisOther);
        }
    }
}
