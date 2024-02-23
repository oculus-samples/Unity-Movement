// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction;
using System.Collections.Generic;
using UnityEngine;
using static OVRUnityHumanoidSkeletonRetargeter;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// Increases hand accuracy as they appear into view.
    /// </summary>
    [CreateAssetMenu(fileName = "Blend Hands", menuName = "Movement Samples/Data/Retargeting Processors/Blend Hands", order = 2)]

    public sealed class BlendHandConstraintProcessor : RetargetingProcessor
    {
        /// <summary>
        /// Head transform to do distance checks against.
        /// </summary>
        [SerializeField]
        [Tooltip(BlendHandConstraintProcessorTooltips.HeadTransform)]
        private Transform _headTransform;
        public Transform HeadTransform
        {
            get => _headTransform;
            set => _headTransform = value;
        }

        /// <summary>
        /// Distance where constraints are set to 1.0.
        /// </summary>
        [SerializeField]
        [Tooltip(BlendHandConstraintProcessorTooltips.ConstraintsMinDistance)]
        private float _constraintsMinDistance = 0.2f;
        public float ConstraintsMinDistance
        {
            get => _constraintsMinDistance;
            set => _constraintsMinDistance = value;
        }

        /// <summary>
        /// Distance where constraints are set to 0.0.
        /// </summary>
        [SerializeField]
        [Tooltip(BlendHandConstraintProcessorTooltips.ConstraintsMaxDistance)]
        private float _constraintsMaxDistance = 0.5f;
        public float ConstraintsMaxDistance
        {
            get => _constraintsMaxDistance;
            set => _constraintsMaxDistance = value;
        }

        /// <summary>
        /// Multiplier that influences weight interpolation based on distance.
        /// </summary>
        [SerializeField]
        [Tooltip(BlendHandConstraintProcessorTooltips.BlendCurve)]
        private AnimationCurve _blendCurve;
        public AnimationCurve BlendCurve
        {
            get => _blendCurve;
            set => _blendCurve = value;
        }

        /// <summary>
        /// (Full Body) Bone ID, usually the wrist. Can be modified depending
        /// on the skeleton used.
        /// </summary>
        [SerializeField, ConditionalHide("_isFullBody", true)]
        [Tooltip(BlendHandConstraintProcessorTooltips.FullBodyBoneIdToTest)]
        private OVRHumanBodyBonesMappings.FullBodyTrackingBoneId _fullBodyBoneIdToTest =
            OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_LeftHandWrist;
        public OVRHumanBodyBonesMappings.FullBodyTrackingBoneId FullBodyBoneIdToTest
        {
            get => _fullBodyBoneIdToTest;
            set => _fullBodyBoneIdToTest = value;
        }

        /// <summary>
        /// Bone ID, usually the wrist. Can be modified depending
        /// on the skeleton used.
        /// </summary>
        [SerializeField, ConditionalHide("_isFullBody", false)]
        [Tooltip(BlendHandConstraintProcessorTooltips.BoneIdToTest)]
        private OVRHumanBodyBonesMappings.BodyTrackingBoneId _boneIdToTest =
            OVRHumanBodyBonesMappings.BodyTrackingBoneId.Body_LeftHandWrist;
        public OVRHumanBodyBonesMappings.BodyTrackingBoneId BoneIdToTest
        {
            get => _boneIdToTest;
            set => _boneIdToTest = value;
        }

        /// <summary>
        /// Specifies if this is full body or not.
        /// </summary>
        [SerializeField]
        [Tooltip(BlendHandConstraintProcessorTooltips.IsFullBody)]
        private bool _isFullBody;
        public bool IsFullBody
        {
            get => _isFullBody;
            set => _isFullBody = value;
        }

        private RetargetingProcessorCorrectBones _retargetingProcessorCorrectBones;
        private RetargetingProcessorCorrectHand _retargetingProcessorCorrectHand;

        /// <inheritdoc />
        public override void CopyData(RetargetingProcessor source)
        {
            Weight = source.Weight;
            var sourceBlendHand = source as BlendHandConstraintProcessor;

            _headTransform = sourceBlendHand.HeadTransform;
            _constraintsMinDistance = sourceBlendHand.ConstraintsMinDistance;
            _constraintsMaxDistance = sourceBlendHand.ConstraintsMaxDistance;
            _blendCurve = sourceBlendHand.BlendCurve;
            _fullBodyBoneIdToTest = sourceBlendHand._fullBodyBoneIdToTest;
            _boneIdToTest = sourceBlendHand._boneIdToTest;
            _isFullBody = sourceBlendHand._isFullBody;
        }

        /// <inheritdoc />
        public override void SetupRetargetingProcessor(RetargetingLayer retargetingLayer)
        {
            foreach (var retargetingProcessor in retargetingLayer.RetargetingProcessors)
            {
                var correctBonesProcessor = retargetingProcessor as RetargetingProcessorCorrectBones;
                if (correctBonesProcessor != null)
                {
                    _retargetingProcessorCorrectBones = correctBonesProcessor;
                }

                var correctHandProcessor = retargetingProcessor as RetargetingProcessorCorrectHand;
                if (correctHandProcessor != null)
                {
                    if (correctHandProcessor.Handedness == Interaction.Input.Handedness.Left && IsLeftSideOfBody())
                    {
                        _retargetingProcessorCorrectHand = correctHandProcessor;
                    }
                    else if (correctHandProcessor.Handedness == Interaction.Input.Handedness.Right && !IsLeftSideOfBody())
                    {
                        _retargetingProcessorCorrectHand = correctHandProcessor;
                    }
                }
            }
        }

        /// <inheritdoc />
        public override void PrepareRetargetingProcessor(RetargetingLayer retargetingLayer, IList<OVRBone> ovrBones)
        {
        }

        /// <inheritdoc />
        public override void ProcessRetargetingLayer(RetargetingLayer retargetingLayer, IList<OVRBone> ovrBones)
        {
            if (!BonesAreValid(retargetingLayer))
            {
                return;
            }

            float constraintWeight = ComputeCurrentConstraintWeight(retargetingLayer);

            if (IsLeftSideOfBody())
            {
                _retargetingProcessorCorrectBones.LeftHandCorrectionWeightLateUpdate = constraintWeight;
            }
            else
            {
                _retargetingProcessorCorrectBones.RightHandCorrectionWeightLateUpdate = constraintWeight;
            }
            if (_retargetingProcessorCorrectHand != null)
            {
                _retargetingProcessorCorrectHand.Weight = constraintWeight;
            }
        }

        private bool IsLeftSideOfBody()
        {
            return _boneIdToTest < OVRHumanBodyBonesMappings.BodyTrackingBoneId.Body_RightHandPalm;
        }

        private bool BonesAreValid(OVRSkeleton skeleton)
        {
            return skeleton.Bones.Count >= (int)_boneIdToTest;
        }

        private float ComputeCurrentConstraintWeight(OVRSkeleton skeleton)
        {
            var bones = skeleton.Bones;
            var boneToTest = bones[(int)_boneIdToTest].Transform;
            var boneDistanceToViewVector = GetDistanceToViewVector(boneToTest.position);

            var constraintWeight = 0.0f;
            var scaledMaxDistanceForConstraints = _constraintsMaxDistance * skeleton.transform.lossyScale.x;
            var scaledMinDistanceForConstraints = _constraintsMinDistance * skeleton.transform.lossyScale.x;
            // If the hand is close enough to the view vector, start to increase the
            // weight of the constraint.
            if (boneDistanceToViewVector <= scaledMaxDistanceForConstraints)
            {
                if (boneDistanceToViewVector <= scaledMinDistanceForConstraints)
                {
                    constraintWeight = 1.0f;
                }
                else
                {
                    float lerpValue = (scaledMaxDistanceForConstraints - boneDistanceToViewVector) /
                        (scaledMaxDistanceForConstraints - scaledMinDistanceForConstraints);
                    float curveValue = _blendCurve.Evaluate(lerpValue);
                    // amplify lerping before clamping it
                    constraintWeight = Mathf.Clamp01(curveValue);
                }
            }

            return constraintWeight;
        }

        /// <summary>
        /// This is solved via the cross product method, as seen here:
        /// https://qc.edu.hk/math/Advanced%20Level/Point_to_line.htm.
        /// What this function does is create a parallelogram that is located
        /// at the view origin, and has two properties: the base (view vector)
        /// and height (distance from view vector to point passed as an
        /// argument). The magnitude of the cross product of the view vector
        /// and (point - viewOrigin) gives us the area of the parallelogram.
        /// To solve for the height, we divide the area by the magnitude of the base
        /// vector.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        private float GetDistanceToViewVector(Vector3 point)
        {
            var viewVector = GetViewVector();
            var viewOriginToPointVector = GetViewOriginToPointVector(point);

            var parallelogramArea = Vector3.Cross(viewVector, viewOriginToPointVector).magnitude;
            var parallelogramBaseLength = viewVector.magnitude;
            return parallelogramArea / parallelogramBaseLength;
        }

        private Vector3 GetViewVector()
        {
            return _headTransform.forward;
        }

        private Vector3 GetViewOriginToPointVector(Vector3 point)
        {
            var viewOrigin = _headTransform.position;
            return point - viewOrigin;
        }
    }
}
