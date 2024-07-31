// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction;
using Oculus.Interaction.Input;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using static OVRUnityHumanoidSkeletonRetargeter;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// Increases hand accuracy as they appear into view.
    /// </summary>
    [CreateAssetMenu(fileName = "Blend Hands", menuName = "Movement Samples/Data/Retargeting Processors/Blend Hands", order = 2)]
    public sealed class RetargetingBlendHandProcessor : RetargetingProcessor
    {
        /// <summary>
        /// Enum used to determine which type of head should be used to blend hands.
        /// </summary>
        public enum HeadView
        {
            BodyTracking,
            OVRCameraRig
        }

        /// <summary>
        /// Distance where weight is set to 1.0.
        /// </summary>
        [SerializeField]
        [Tooltip(RetargetingBlendHandProcessorTooltips.MinDistance)]
        private float _minDistance = 0.2f;
        /// <inheritdoc cref="_minDistance"/>
        public float MinDistance
        {
            get => _minDistance;
            set => _minDistance = value;
        }

        /// <summary>
        /// Distance where weight is set to 0.0.
        /// </summary>
        [SerializeField]
        [Tooltip(RetargetingBlendHandProcessorTooltips.MaxDistance)]
        private float _maxDistance = 0.5f;
        /// <inheritdoc cref="_maxDistance"/>
        public float MaxDistance
        {
            get => _maxDistance;
            set => _maxDistance = value;
        }

        /// <summary>
        /// Multiplier that influences weight interpolation based on distance.
        /// </summary>
        [SerializeField]
        [Tooltip(RetargetingBlendHandProcessorTooltips.BlendCurve)]
        private AnimationCurve _blendCurve;
        /// <inheritdoc cref="_blendCurve"/>
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
        [Tooltip(RetargetingBlendHandProcessorTooltips.FullBodyBoneIdToTest)]
        private OVRHumanBodyBonesMappings.FullBodyTrackingBoneId _fullBodyBoneIdToTest =
            OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_LeftHandWrist;
        /// <inheritdoc cref="_fullBodyBoneIdToTest"/>
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
        [Tooltip(RetargetingBlendHandProcessorTooltips.BoneIdToTest)]
        private OVRHumanBodyBonesMappings.BodyTrackingBoneId _boneIdToTest =
            OVRHumanBodyBonesMappings.BodyTrackingBoneId.Body_LeftHandWrist;
        /// <inheritdoc cref="_boneIdToTest"/>
        public OVRHumanBodyBonesMappings.BodyTrackingBoneId BoneIdToTest
        {
            get => _boneIdToTest;
            set => _boneIdToTest = value;
        }

        /// <summary>
        /// The type of head that should be used to blend hands.
        /// </summary>
        [SerializeField]
        [Tooltip(RetargetingBlendHandProcessorTooltips.HeadView)]
        private HeadView _headView;
        /// <inheritdoc cref="_headView"/>
        public HeadView HeadViewType
        {
            get => _headView;
            set => _headView = value;
        }

        /// <summary>
        /// Specifies if this is full body or not.
        /// </summary>
        [SerializeField]
        [Tooltip(RetargetingBlendHandProcessorTooltips.IsFullBody)]
        private bool _isFullBody;
        /// <inheritdoc cref="_isFullBody"/>
        public bool IsFullBody
        {
            get => _isFullBody;
            set => _isFullBody = value;
        }

        private RetargetingProcessorCorrectHand.HandProcessor _retargetingProcessorCorrectHand;
        private Transform _cachedTransform;
        private Transform _cachedHeadTransform;
        private Transform _ovrCameraRigHead;
        private float _cachedWeight;

        /// <inheritdoc />
        public override void CopyData(RetargetingProcessor source)
        {
            Weight = source.Weight;
            var sourceBlendHand = source as RetargetingBlendHandProcessor;
            Assert.IsNotNull(sourceBlendHand);

            _minDistance = sourceBlendHand._minDistance;
            _maxDistance = sourceBlendHand._maxDistance;
            _blendCurve = sourceBlendHand._blendCurve;
            _fullBodyBoneIdToTest = sourceBlendHand._fullBodyBoneIdToTest;
            _boneIdToTest = sourceBlendHand._boneIdToTest;
            _isFullBody = sourceBlendHand._isFullBody;
        }

        /// <inheritdoc />
        public override void SetupRetargetingProcessor(RetargetingLayer retargetingLayer)
        {
            // Make sure our processor is before others.
            bool foundOurProcessor = false;

            foreach (var retargetingProcessor in retargetingLayer.RetargetingProcessors)
            {
                var blendHandProcessor = retargetingProcessor as RetargetingBlendHandProcessor;
                if (blendHandProcessor != null && blendHandProcessor == this)
                {
                    foundOurProcessor = true;
                }

                var correctHandProcessor = retargetingProcessor as RetargetingProcessorCorrectHand;
                if (correctHandProcessor != null)
                {
                    if (IsLeftSideOfBody())
                    {
                        _retargetingProcessorCorrectHand = correctHandProcessor.LeftHandProcessor;
                    }
                    else if (!IsLeftSideOfBody())
                    {
                        _retargetingProcessorCorrectHand = correctHandProcessor.RightHandProcessor;
                    }
                    if (!foundOurProcessor)
                    {
                        Debug.LogWarning($"{this.GetType()} should be before {correctHandProcessor} in processor " +
                            $"stack as it needs to affect it.");
                    }
                }
            }

            if (OVRManager.instance)
            {
                var ovrCameraRig = OVRManager.instance.gameObject.GetComponent<OVRCameraRig>();
                if (ovrCameraRig != null)
                {
                    _ovrCameraRigHead = ovrCameraRig.centerEyeAnchor;
                }
                else if (_headView == HeadView.OVRCameraRig)
                {
                    Debug.LogError($"OVRCameraRig is missing for " +
                                   $"{retargetingLayer.gameObject.name}'s blend hand processor.");
                }
            }
        }

        /// <inheritdoc />
        public override void ProcessRetargetingLayer(RetargetingLayer retargetingLayer, IList<OVRBone> ovrBones)
        {
            if (!BonesAreValid(retargetingLayer))
            {
                return;
            }

            float blendWeight = ComputeCurrentBlendWeight(retargetingLayer);

            // The weight of this processor can be used to reduce its effect
            if (Weight < float.Epsilon)
            {
                return;
            }

            if (_retargetingProcessorCorrectHand != null)
            {
                _retargetingProcessorCorrectHand.HandIKWeight = blendWeight;
            }
        }

        private bool IsLeftSideOfBody()
        {
            if (IsFullBody)
            {
                return _fullBodyBoneIdToTest < OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_RightHandPalm;
            }
            return _boneIdToTest < OVRHumanBodyBonesMappings.BodyTrackingBoneId.Body_RightHandPalm;
        }

        /// <summary>
        /// Returns handedness value of processor.
        /// </summary>
        /// <returns>Handedness.</returns>
        public Handedness GetHandedness()
        {
            return IsLeftSideOfBody() ? Handedness.Left : Handedness.Right;
        }

        private bool BonesAreValid(OVRSkeleton skeleton)
        {
            return IsFullBody ? skeleton.Bones.Count >= (int)_fullBodyBoneIdToTest : skeleton.Bones.Count >= (int)_boneIdToTest;
        }

        /// <inheritdoc />
        public override void DrawGizmos()
        {
            if (_cachedTransform == null || _cachedHeadTransform == null)
            {
                return;
            }
            var viewVector = GetViewVector();
            var viewOriginToPointVector = GetViewOriginToPointVector(_cachedTransform.position);

            Gizmos.color = new Color(_cachedWeight, _cachedWeight, _cachedWeight);
            Gizmos.DrawRay(_cachedHeadTransform.position, viewOriginToPointVector);

            Gizmos.color = _headView == HeadView.BodyTracking ? Color.white : Color.blue;
            Gizmos.DrawRay(_cachedHeadTransform.transform.position, 0.7f * viewVector);
        }

        private float ComputeCurrentBlendWeight(RetargetingLayer skeleton)
        {
            var bones = skeleton.Bones;
            var boneToTest = bones[GetBoneIndex()].Transform;
            var headBoneId = IsFullBody ? (int)OVRSkeleton.BoneId.FullBody_Head : (int)OVRSkeleton.BoneId.Body_Head;
            _cachedTransform = boneToTest;
            if (_headView == HeadView.BodyTracking)
            {
                _cachedHeadTransform = bones[headBoneId].Transform;
            }
            else if (_headView == HeadView.OVRCameraRig)
            {
                _cachedHeadTransform = _ovrCameraRigHead;
            }
            var boneDistanceToViewVector = GetDistanceToViewVector(boneToTest.position);

            _cachedWeight = 0.0f;
            var lossyScale = skeleton.transform.lossyScale;
            var scaledMaxDistance = _maxDistance * lossyScale.x;
            var scaledMinDistance = _minDistance * lossyScale.x;
            // If the hand is close enough to the view vector, start to increase the
            // weight.
            if (boneDistanceToViewVector <= scaledMaxDistance)
            {
                if (boneDistanceToViewVector <= scaledMinDistance)
                {
                    _cachedWeight = 1.0f;
                }
                else
                {
                    float lerpValue = (scaledMaxDistance - boneDistanceToViewVector) /
                        (scaledMaxDistance - scaledMinDistance);
                    float curveValue = _blendCurve.Evaluate(lerpValue);
                    // Amplify lerping before clamping it.
                    _cachedWeight = Mathf.Clamp01(curveValue);
                }
            }

            return _cachedWeight;
        }

        private int GetBoneIndex()
        {
            return IsFullBody ? (int)_fullBodyBoneIdToTest : (int)_boneIdToTest;
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
        /// </summary>=
        /// <param name="point">Point to evaluate</param>
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
            if (_headView == HeadView.BodyTracking)
            {
                // The local up is what points forward for the OVRSkeleton head.
                return _cachedHeadTransform.up;
            }
            return _cachedHeadTransform.forward;
        }

        private Vector3 GetViewOriginToPointVector(Vector3 point)
        {
            var viewOrigin = _cachedHeadTransform.position;
            return point - viewOrigin;
        }
    }
}
