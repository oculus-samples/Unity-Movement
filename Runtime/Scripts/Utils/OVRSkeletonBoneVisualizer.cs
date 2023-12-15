// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Movement.AnimationRigging;
using UnityEngine;
using UnityEngine.Assertions;
using static OVRUnityHumanoidSkeletonRetargeter;

namespace Oculus.Movement.Utils
{
    /// <summary>
    /// Allows visualizing bones found in an OVRSkeleton component.
    /// </summary>
    [DefaultExecutionOrder(230)]
    public class OVRSkeletonBoneVisualizer
        : BoneVisualizer<OVRHumanBodyBonesMappings.BodyTrackingBoneId>
    {
        /// <summary>
        /// OVRSkeleton component to visualize bones for.
        /// </summary>
        [SerializeField]
        [Tooltip(OVRSkeletonBoneVisualizerTooltips.OVRSkeletonComp)]
        protected OVRSkeleton _ovrSkeletonComp;

        /// <summary>
        /// Whether to visualize bind pose or not.
        /// </summary>
        [SerializeField]
        [Tooltip(OVRSkeletonBoneVisualizerTooltips.VisualizeBindPose)]
        protected bool _visualizeBindPose = false;

        /// </ inheritdoc>
        protected override void Awake()
        {
            base.Awake();
        }

        protected override void Start()
        {
            base.Start();
            Assert.IsNotNull(_ovrSkeletonComp);
        }

        /// <inheritdoc />
        protected override int GetBoneCount()
        {
            return (int)OVRSkeleton.BoneId.Body_End;
        }

        /// <inheritdoc />
        protected override BoneTuple GetBoneTuple(int currentBone)
        {
            var boneTuple = OVRHumanBodyBonesMappings.BoneIdToJointPair[(OVRSkeleton.BoneId)currentBone];
            return new BoneTuple((int)boneTuple.Item1, (int)boneTuple.Item2);
        }

        /// <inheritdoc />
        protected override Transform GetBoneTransform(int currentBone)
        {
            return RiggingUtilities.FindBoneTransformFromSkeleton(_ovrSkeletonComp,
                (OVRSkeleton.BoneId)currentBone, _visualizeBindPose);
        }

        /// <inheritdoc />
        protected override bool TryGetBoneTransforms(BoneTuple tupleItem,
            out Transform firstJoint, out Transform secondJoint)
        {
            if (!_ovrSkeletonComp.IsDataValid)
            {
                firstJoint = secondJoint = null;
                return false;
            }
            firstJoint = RiggingUtilities.FindBoneTransformFromSkeleton(
                _ovrSkeletonComp,
                (OVRSkeleton.BoneId)tupleItem.FirstBoneId,
                _visualizeBindPose);
            secondJoint = (tupleItem.SecondBoneId >= (int)OVRHumanBodyBonesMappings.BodyTrackingBoneId.Body_End)
                ? firstJoint.GetChild(0)
                : RiggingUtilities.FindBoneTransformFromSkeleton(_ovrSkeletonComp,
                    (OVRSkeleton.BoneId)tupleItem.SecondBoneId, _visualizeBindPose);
            return true;
        }

        /// <inheritdoc />
        protected override AvatarMaskBodyPart GetAvatarBodyPart(int currentBone)
        {
            return BoneMappingsExtension.OVRSkeletonBoneIdToAvatarBodyPart[(OVRSkeleton.BoneId)currentBone];
        }

        /// <inheritdoc />
        public override void SetBody(GameObject body)
        {
            _ovrSkeletonComp = body.GetComponent<OVRSkeleton>();
            ResetBoneVisuals();
        }
    }
}
