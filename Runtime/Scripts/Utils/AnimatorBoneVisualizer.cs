// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Movement.AnimationRigging;
using UnityEngine;
using UnityEngine.Assertions;
using static OVRUnityHumanoidSkeletonRetargeter;

namespace Oculus.Movement.Utils
{
    /// <summary>
    /// Allows visualizing bones found in an Animator component.
    /// </summary>
    [DefaultExecutionOrder(230)]
    public class AnimatorBoneVisualizer : BoneVisualizer<HumanBodyBones>
    {
        /// <summary>
        /// Animator component to visualize bones for.
        /// </summary>
        [SerializeField]
        [Tooltip(AnimatorBoneVisualizerTooltips.AnimatorComp)]
        protected Animator _animatorComp;

        /// <inheritdoc />
        protected override void Awake()
        {
            base.Awake();
        }

        /// <inheritdoc/>
        protected override void Start()
        {
            base.Start();
            Assert.IsNotNull(_animatorComp);
        }

        /// <inheritdoc />
        protected override int GetBoneCount()
        {
            return (int)HumanBodyBones.LastBone;
        }

        /// <inheritdoc />
        protected override BoneTuple GetBoneTuple(int currentBone)
        {
            var boneTuple = OVRHumanBodyBonesMappings.BoneToJointPair[(HumanBodyBones)currentBone];
            return new BoneTuple((int)boneTuple.Item1, (int)boneTuple.Item2);
        }

        /// <inheritdoc />
        protected override Transform GetBoneTransform(int currentBone)
        {
            return _animatorComp.GetBoneTransform((HumanBodyBones)currentBone);
        }

        /// <inheritdoc />
        protected override bool TryGetBoneTransforms(BoneTuple tupleItem,
            out Transform firstJoint, out Transform secondJoint)
        {
            firstJoint = _animatorComp.GetBoneTransform(tupleItem.FirstBone);
            if (firstJoint == null)
            {
                firstJoint = secondJoint = null;
                return false;
            }
            secondJoint = (tupleItem.SecondBone == HumanBodyBones.LastBone)
                ? firstJoint.GetChild(0)
                : _animatorComp.GetBoneTransform(tupleItem.SecondBone);
            return true;
        }

        /// <inheritdoc />
        protected override AvatarMaskBodyPart GetAvatarBodyPart(int currentBone)
        {
            return BoneMappingsExtension.HumanBoneToAvatarBodyPart[(HumanBodyBones)currentBone];
        }

        /// <inheritdoc />
        public override void SetBody(GameObject body)
        {
            _animatorComp = body.GetComponent<Animator>();
            ResetBoneVisuals();
        }
    }
}
