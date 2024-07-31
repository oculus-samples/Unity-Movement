// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction;
using Oculus.Interaction.Input;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Assertions;
using static OVRUnityHumanoidSkeletonRetargeter;

namespace Oculus.Movement.AnimationRigging.Deprecated
{
    /// <summary>
    /// Allows disabling TwoBoneIK constraints when the hands move far away,
    /// which would mitigate issues with the elbows looking incorrect when the hands
    /// are away. While the hands will look more accurate when further away, that
    /// might be ok because they are far from the body.
    /// </summary>
    [DefaultExecutionOrder(225)]
    public class BlendHandConstraints : MonoBehaviour, IOVRSkeletonProcessor
    {
        /// <inheritdoc />
        public bool EnableSkeletonProcessing
        {
            get => enabled;
            set => enabled = value;
        }
        /// <inheritdoc />
        public string SkeletonProcessorLabel => "Blend Hands";

        /// <summary>
        /// Constraints to control the weight of.
        /// </summary>
        [SerializeField, Interface(typeof(IRigConstraint))]
        [Tooltip(BlendHandConstraintsTooltips.Constraints)]
        private MonoBehaviour[] _constraints;
        /// <inheritdoc cref="_constraints"/>
        public MonoBehaviour[] Constraints
        {
            get => _constraints;
            set => _constraints = value;
        }

        /// <summary>
        /// The character's retargeting layer.
        /// </summary>
        [SerializeField]
        [Tooltip(BlendHandConstraintsTooltips.RetargetingLayer)]
        private RetargetingLayer _retargetingLayer;
        /// <inheritdoc cref="_retargetingLayer"/>
        public RetargetingLayer RetargetingLayerComp
        {
            get => _retargetingLayer;
            set => _retargetingLayer = value;
        }

        /// <summary>
        /// Bone ID, usually the wrist. Can be modified depending
        /// on the skeleton used.
        /// </summary>
        [SerializeField]
        [Tooltip(BlendHandConstraintsTooltips.BoneIdToTest)]
        private OVRHumanBodyBonesMappings.BodyTrackingBoneId _boneIdToTest =
            OVRHumanBodyBonesMappings.BodyTrackingBoneId.Body_LeftHandWrist;
        public OVRHumanBodyBonesMappings.BodyTrackingBoneId BoneIdToTest
        {
            get => _boneIdToTest;
            set => _boneIdToTest = value;
        }

        /// <summary>
        /// Head transform to do distance checks against.
        /// </summary>
        [SerializeField]
        [Tooltip(BlendHandConstraintsTooltips.HeadTransform)]
        private Transform _headTransform;
        public Transform HeadTransform
        {
            get => _headTransform;
            set => _headTransform = value;
        }

        /// <summary>
        /// MonoBehaviour to add to.
        /// </summary>
        [Optional, SerializeField]
        [Interface(typeof(IOVRSkeletonProcessorAggregator))]
        [Tooltip(BlendHandConstraintsTooltips.AutoAddTo)]
        protected MonoBehaviour _autoAddTo;
        public MonoBehaviour AutoAddTo
        {
            get => _autoAddTo;
            set => _autoAddTo = value;
        }

        /// <summary>
        /// Distance where constraints are set to 1.0.
        /// </summary>
        [SerializeField]
        [Tooltip(BlendHandConstraintsTooltips.ConstraintsMinDistance)]
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
        [Tooltip(BlendHandConstraintsTooltips.ConstraintsMaxDistance)]
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
        [Tooltip(BlendHandConstraintsTooltips.BlendMultiplier)]
        private AnimationCurve _blendCurve;
        public AnimationCurve BlendCurve
        {
            get => _blendCurve;
            set => _blendCurve = value;
        }

        /// <summary>
        /// Max constraint weight.
        /// </summary>
        [SerializeField]
        [Tooltip(BlendHandConstraintsTooltips.MaxWeight)]
        private float _maxWeight = 1.0f;
        public float MaxWeight
        {
            get => _maxWeight;
            set => _maxWeight = value;
        }

        private IRigConstraint[] _iRigConstraints;
        private Transform _cachedTransform;
        private float _cachedConstraintWeight;
        private RetargetingProcessorCorrectHand.HandProcessor _retargetingProcessorCorrectHand;

        /// <summary>
        /// Adds constraint. Valid for use via editor scripts only.
        /// </summary>
        /// <param name="newConstraint">New constraint to add.</param>
        public void AddSkeletalConstraint(MonoBehaviour newConstraint)
        {
            if (_constraints == null)
            {
                _constraints = new MonoBehaviour[1];
                _constraints[0] = newConstraint;
            }
            else
            {
                var oldConstraints = _constraints;
                _constraints =
                    new MonoBehaviour[oldConstraints.Length + 1];
                for (int i = 0; i < oldConstraints.Length; i++)
                {
                    _constraints[i] = oldConstraints[i];
                }
                _constraints[oldConstraints.Length] =
                    newConstraint;
            }

            // Update the interface references in case this was called after awake, but before Update
            UpdateSkeletalConstraintInterfaceReferences();
        }

        private void Awake()
        {
            if (_constraints != null && _constraints.Length > 0)
            {
                UpdateSkeletalConstraintInterfaceReferences();
            }

            Assert.IsNotNull(_retargetingLayer);
            Assert.IsNotNull(_headTransform);
            Assert.IsNotNull(_blendCurve);

            Assert.IsTrue(_constraintsMinDistance < _constraintsMaxDistance);
        }

        private void UpdateSkeletalConstraintInterfaceReferences()
        {
            _iRigConstraints = new IRigConstraint[_constraints.Length];
            for (int i = 0; i < _constraints.Length; i++)
            {
                _iRigConstraints[i] = _constraints[i] as IRigConstraint;
                Assert.IsNotNull(_iRigConstraints[i]);
            }
        }

        /// <summary>
        /// Will add self to <see cref="IOVRSkeletonProcessorAggregator"/> <see cref="_autoAddTo"/>
        /// </summary>
        private void Start()
        {
            if (_autoAddTo != null && _autoAddTo is IOVRSkeletonProcessorAggregator aggregator)
            {
                aggregator.AddProcessor(this);
            }

            foreach (var retargetingProcessor in _retargetingLayer.RetargetingProcessors)
            {
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
                }
            }
        }

        /// <summary>
        /// Sets max constraint weight.
        /// </summary>
        /// <param name="max">New max value.</param>
        public void SetMaxWeight(float max)
        {
            _maxWeight = max;
        }

        /// <inheritdoc />
        public void ProcessSkeleton(OVRSkeleton skeleton)
        {
            if (!enabled)
            {
                return;
            }

            var bones = skeleton.Bones;
            if (!BonesAreValid(skeleton))
            {
                return;
            }

            float constraintWeight = ComputeCurrentConstraintWeight(skeleton);
            if (_iRigConstraints != null)
            {
                foreach (var constraint in _iRigConstraints)
                {
                    constraint.weight = Mathf.Min(constraintWeight, _maxWeight);
                }
            }

            if (_retargetingProcessorCorrectHand != null)
            {
                _retargetingProcessorCorrectHand.HandIKWeight = constraintWeight;
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

        private void OnDrawGizmos()
        {
            if (_cachedTransform == null)
            {
                return;
            }
            var viewVector = GetViewVector();
            var viewOriginToPointVector = GetViewOriginToPointVector(_cachedTransform.position);

            Gizmos.color = new UnityEngine.Color(_cachedConstraintWeight, _cachedConstraintWeight,
                _cachedConstraintWeight);
            Gizmos.DrawRay(_headTransform.position, viewOriginToPointVector);

            Gizmos.color = UnityEngine.Color.white;
            Gizmos.DrawRay(_headTransform.transform.position, 0.7f * viewVector);
        }

        private float ComputeCurrentConstraintWeight(OVRSkeleton skeleton)
        {
            var bones = skeleton.Bones;
            var boneToTest = bones[(int)_boneIdToTest].Transform;
            _cachedTransform = boneToTest;
            var boneDistanceToViewVector = GetDistanceToViewVector(boneToTest.position);

            _cachedConstraintWeight = 0.0f;
            var scaledMaxDistanceForConstraints = _constraintsMaxDistance * transform.lossyScale.x;
            var scaledMinDistanceForConstraints = _constraintsMinDistance * transform.lossyScale.x;
            // If the hand is close enough to the view vector, start to increase the
            // weight of the constraint.
            if (boneDistanceToViewVector <= scaledMaxDistanceForConstraints)
            {
                if (boneDistanceToViewVector <= scaledMinDistanceForConstraints)
                {
                    _cachedConstraintWeight = 1.0f;
                }
                else
                {
                    float lerpValue = (scaledMaxDistanceForConstraints - boneDistanceToViewVector) /
                        (scaledMaxDistanceForConstraints - scaledMinDistanceForConstraints);
                    float curveValue = _blendCurve.Evaluate(lerpValue);
                    // amplify lerping before clamping it
                    _cachedConstraintWeight = Mathf.Clamp01(curveValue);
                }
            }

            return _cachedConstraintWeight;
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
