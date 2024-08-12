// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using UnityEngine;
using static OVRUnityHumanoidSkeletonRetargeter.OVRHumanBodyBonesMappings;

namespace Oculus.Movement.Utils
{
    /// <summary>
    /// Scriptable object that contains custom bind pose information that can be applied
    /// onto a bind pose to configure a custom bind pose. This can be used to fix artifacts
    /// that are a result of retargeting and deformation.
    /// </summary>
    public class BindPoseObjectSkeleton : ScriptableObject
    {
        /// <summary>
        /// Holds delta pose information for each bone.
        /// </summary>
        [Serializable]
        public class BonePoseData
        {
            /// <summary>
            /// Constructor for BonePoseData, taking in a bone transform and comparing it against
            /// the target transform to store delta information.
            /// </summary>
            /// <param name="boneTransform"></param>
            /// <param name="targetTransform"></param>
            /// <param name="boneId"></param>
            public BonePoseData(
                Transform boneTransform,
                Transform targetTransform,
                FullBodyTrackingBoneId boneId)
            {
                var jointAxisOther = boneTransform.parent.position - boneTransform.position;
                var jointAxisReference = targetTransform.parent.position - targetTransform.position;
                DeltaRot = Quaternion.FromToRotation(jointAxisReference, jointAxisOther);
                WorldPose = new Pose(boneTransform.position, boneTransform.rotation);
                BoneId = boneId;
            }

            /// <summary>
            /// The bone id corresponding to the delta pose information.
            /// </summary>
            public FullBodyTrackingBoneId BoneId;

            /// <summary>
            /// The world pose of this bone at rest.
            /// </summary>
            public Pose WorldPose;

            /// <summary>
            /// The delta rotation between the two bone transforms that were compared.
            /// </summary>
            public Quaternion DeltaRot = Quaternion.identity;

            /// <summary>
            /// The adjustment rotation to be applied to the bone transform.
            /// </summary>
            public Quaternion AdjustmentRot = Quaternion.identity;
        }

        /// <inheritdoc cref="_bonePoses"/>>
        public BonePoseData[] BonePoses
        {
            get => _bonePoses;
            set => _bonePoses = value;
        }

        /// <summary>
        /// Array containing all the stored bone pose information.
        /// </summary>
        [SerializeField]
        protected BonePoseData[] _bonePoses;

        /// <summary>
        /// Initializes the bind pose data from two skeletons, capturing the deltas between
        /// the source skeleton and the target skeleton.
        /// </summary>
        /// <param name="source">The source skeleton.</param>
        /// <param name="target">The target skeleton.</param>
        public void InitializeBindPoseDataFromSkeletons(OVRSkeleton source, OVRSkeleton target)
        {
            var bonePoses = new List<BonePoseData>();
            for (var i = FullBodyTrackingBoneId.FullBody_Root; i < FullBodyTrackingBoneId.FullBody_End; i++)
            {
                bonePoses.Add(new BonePoseData(
                    source.Bones[(int)i].Transform,
                    target.Bones[(int)i].Transform, i));
            }

            _bonePoses = bonePoses.ToArray();
        }

        /// <summary>
        /// Returns bone pose data for the given bone.
        /// </summary>
        /// <returns>Bone pose data, if bone is found in the map.</returns>
        public BonePoseData GetBonePoseData(FullBodyTrackingBoneId boneId)
        {
            return (int)boneId >= _bonePoses.Length ? null : _bonePoses[(int)boneId];
        }
    }
}
