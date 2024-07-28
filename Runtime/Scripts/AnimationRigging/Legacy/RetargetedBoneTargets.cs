// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static OVRUnityHumanoidSkeletonRetargeter;

namespace Oculus.Movement.AnimationRigging.Deprecated
{
    /// <summary>
    /// Update target transforms with retargeted bone data.
    /// </summary>
    public class RetargetedBoneTargets : MonoBehaviour, IOVRSkeletonProcessor
    {
        /// <summary>
        /// A retargeted bone target.
        /// </summary>
        [System.Serializable]
        public class RetargetedBoneTarget
        {
            /// <summary>
            /// The OVRSkeleton.BoneId corresponding to the HumanBodyBone.
            /// </summary>
            public OVRSkeleton.BoneId BoneId { get; set; }

            /// <summary>
            /// The human body bone representation of this bone.
            /// </summary>
            [Tooltip(RetargetedBoneTargetsTooltips.RetargetedBoneTargetTooltips.HumanBodyBone)]
            public HumanBodyBones HumanBodyBone;

            /// <summary>
            /// The target transform to update with the retargeted bone data.
            /// </summary>
            [Tooltip(RetargetedBoneTargetsTooltips.RetargetedBoneTargetTooltips.Target)]
            public Transform Target;
        }

        /// <inheritdoc/>
        public bool EnableSkeletonProcessing
        {
            get => enabled;
            set => enabled = value;
        }

        /// <inheritdoc/>
        public string SkeletonProcessorLabel => "Retargeted Bone Targets";

        /// <summary>
        /// The <see cref="IOVRSkeletonProcessorAggregator"/> to give self to
        /// </summary>
        [SerializeField]
        [ContextMenuItem(nameof(FindLocalProcessorAggregator), nameof(FindLocalProcessorAggregator))]
        [Interface(typeof(IOVRSkeletonProcessorAggregator))]
        protected UnityEngine.Object _autoAddTo;
        public Object AutoAddTo
        {
            get => _autoAddTo;
            set => _autoAddTo = value;
        }

        /// <summary>
        /// The array of retargeted bone targets.
        /// </summary>
        [SerializeField]
        [Tooltip(RetargetedBoneTargetsTooltips.RetargetedBoneTargets)]
        protected RetargetedBoneTarget[] _retargetedBoneTargets;
        public RetargetedBoneTarget[] RetargetedBoneTargetsArray
        {
            get => _retargetedBoneTargets;
            set => _retargetedBoneTargets = value;
        }

        private void FindLocalProcessorAggregator()
        {
            _autoAddTo = GetComponentInParent<IOVRSkeletonProcessorAggregator>()
                as UnityEngine.Object;
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// Correlate HumanBodyBones to OVRSkeleton.BoneId.
        /// </summary>
        protected virtual void Awake()
        {
            var humanBodyBoneToOVRBoneId =
                OVRHumanBodyBonesMappings.BoneIdToHumanBodyBone.ToDictionary(
                    x => x.Value, x => x.Key);
            foreach (var retargetedBoneTarget in _retargetedBoneTargets)
            {
                retargetedBoneTarget.BoneId = humanBodyBoneToOVRBoneId[retargetedBoneTarget.HumanBodyBone];
            }
        }

        /// <summary>
        /// Will add self to <see cref="IOVRSkeletonProcessorAggregator"/> <see cref="_autoAddTo"/>
        /// </summary>
        private void Start()
        {
            if (_autoAddTo != null)
            {
                IOVRSkeletonProcessorAggregator aggregator = _autoAddTo
                    as IOVRSkeletonProcessorAggregator;
                aggregator.AddProcessor(this);
            }
        }

        /// <summary>
        /// Update the bone targets with the retargeted bone transform data.
        /// This should be used with <see cref="RetargetingLayer.SkeletonPostProcessingEv" />.
        /// </summary>
        /// <param name="skeleton"></param>
        public void ProcessSkeleton(OVRSkeleton skeleton)
        {
            IList<OVRBone> bones = skeleton.Bones;
            for (var i = 0; i < bones.Count; i++)
            {
                foreach (var retargetedBoneTarget in _retargetedBoneTargets)
                {
                    if (bones[i].Id == retargetedBoneTarget.BoneId)
                    {
                        var targetBone = bones[i].Transform;
                        if (targetBone == null)
                        {
                            continue;
                        }
                        retargetedBoneTarget.Target.SetPositionAndRotation(
                            targetBone.position, targetBone.rotation);
                    }
                }
            }
        }
    }
}
