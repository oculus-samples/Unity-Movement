// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction;
using Oculus.Interaction.Input;
using System;
using System.Collections.Generic;
using UnityEngine;
using static OVRUnityHumanoidSkeletonRetargeter;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// Retargeting processor used to fix the arm via an IK algorithm so that the retargeted hand position matches
    /// the tracked hand position.
    /// </summary>
    [CreateAssetMenu(fileName = "Correct Hand", menuName = "Movement Samples/Data/Retargeting Processors/Correct Hand", order = 1)]
    public sealed class RetargetingProcessorCorrectHand : RetargetingProcessor
    {
        /// <summary>
        /// Processor for blending and correcting a hand with IK.
        /// </summary>
        [Serializable]
        public class HandProcessor
        {
            /// <summary>
            /// The weight of the hand blending.
            /// </summary>
            [Range(0.0f, 1.0f)]
            [Tooltip(RetargetingProcessorCorrectHandTooltips.BlendHandWeight)]
            public float BlendHandWeight = 1.0f;

            /// <summary>
            /// The weight of the hand IK.
            /// </summary>
            [Range(0.0f, 1.0f)]
            [Tooltip(RetargetingProcessorCorrectHandTooltips.HandIKWeight)]
            public float HandIKWeight = 1.0f;

            /// <summary>
            /// If true, use the world hand position for placing the hand instead of the scaled position.
            /// </summary>
            [SerializeField]
            [Tooltip(RetargetingLayerTooltips.UseWorldHandPosition)]
            private bool _useWorldHandPosition = true;
            /// <inheritdoc cref="_useWorldHandPosition" />
            public bool UseWorldHandPosition
            {
                get => _useWorldHandPosition;
                set => _useWorldHandPosition = value;
            }

            /// <summary>
            /// If true, use the custom hand target position for the target position.
            /// </summary>
            [SerializeField]
            [Tooltip(RetargetingLayerTooltips.UseCustomHandTargetPosition)]
            private bool _useCustomHandTargetPosition = true;
            /// <inheritdoc cref="_useCustomHandTargetPosition" />
            public bool UseCustomHandTargetPosition
            {
                get => _useCustomHandTargetPosition;
                set => _useCustomHandTargetPosition = value;
            }

            /// <summary>
            /// If true, use the secondary bone position before solving for the target position.
            /// </summary>
            [SerializeField]
            [Tooltip(RetargetingLayerTooltips.UseSecondaryBondId)]
            private bool _useSecondaryBondId = true;
            /// <inheritdoc cref="_useSecondaryBondId" />
            public bool UseSecondaryBondId
            {
                get => _useSecondaryBondId;
                set => _useSecondaryBondId = value;
            }

            /// <summary>
            /// The custom hand target position.
            /// </summary>
            private Vector3? _customHandTargetPosition;
            /// <inheritdoc cref="_customHandTargetPosition" />
            public Vector3? CustomHandTargetPosition
            {
                get => _customHandTargetPosition;
                set => _customHandTargetPosition = value;
            }

            /// <summary>
            /// (Full Body) Secondary Bone ID, usually the lower arm. This is the target bone
            /// that the upper arm will pre-rotate to for a more accurate IK solve. Can be modified depending
            /// on the skeleton used.
            /// </summary>
            [SerializeField]
            [Tooltip(RetargetingBlendHandProcessorTooltips.FullBodySecondBoneIdToTest)]
            private OVRHumanBodyBonesMappings.FullBodyTrackingBoneId _fullBodySecondBoneIdToTest =
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.NoOverride;
            public OVRHumanBodyBonesMappings.FullBodyTrackingBoneId FullBodySecondBoneIdToTest
            {
                get => _fullBodyBoneIdToTest;
                set => _fullBodyBoneIdToTest = value;
            }

            /// <summary>
            /// (Full Body) Bone ID, usually the wrist. Can be modified depending
            /// on the skeleton used.
            /// </summary>
            [SerializeField]
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
            [SerializeField]
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
            /// Arm chain bones to affect. WARNING: Advanced feature.
            /// </summary>
            [SerializeField, Optional]
            [Tooltip(RetargetingProcessorCorrectHandTooltips.ArmChainBones)]
            private HumanBodyBones[] _armChainBones;
            /// <inheritdoc cref="_armChainBones"/>
            public HumanBodyBones[] ArmChainBones
            {
                get => _armChainBones;
                set => _armChainBones = value;
            }

            private Transform[] _armBones;
            private Transform[] _armBonesEndEffectorLast;
            private float[] _distanceToNextBoneEndEffectorLast;
            private Vector3 _originalHandPosition;

            private Transform _cachedTransform;
            private Transform _cachedHeadTransform;
            private Transform _ovrCameraRigHead;
            private float _cachedWeight;

            /// <inheritdoc cref="RetargetingProcessorCorrectHand.SetupRetargetingProcessor"/>
            public void SetupRetargetingProcessor(RetargetingLayer retargetingLayer,
                RetargetingProcessorCorrectHand parentProcessor, Handedness handedness)
            {
                // Skip the finger bones.
                var armBones = new List<Transform>();
                var animator = retargetingLayer.GetAnimatorTargetSkeleton();

                // We iterate from the jaw downward, as the first bone is the effector, which is the hand.
                // Hand -> Lower Arm -> Upper Arm -> Shoulder.
                for (var i = HumanBodyBones.Jaw; i >= HumanBodyBones.Hips; i--)
                {
                    var boneTransform = animator.GetBoneTransform(i);
                    if (boneTransform == null)
                    {
                        continue;
                    }

                    if ((handedness == Handedness.Left &&
                         BoneMappingsExtension.HumanBoneToAvatarBodyPartArray[(int)i] != AvatarMaskBodyPart.LeftArm) ||
                        (handedness == Handedness.Right &&
                         BoneMappingsExtension.HumanBoneToAvatarBodyPartArray[(int)i] != AvatarMaskBodyPart.RightArm))
                    {
                        continue;
                    }

                    if (!BonePassesChainTest(i))
                    {
                        continue;
                    }

                    armBones.Add(boneTransform);
                }
                _armBones = armBones.ToArray();
                armBones.Reverse();

                // If the secondary bone id is not set, set it to use the appropriate side
                if (_useSecondaryBondId)
                {
                    if (_fullBodySecondBoneIdToTest == OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.NoOverride)
                    {
                        if (_fullBodyBoneIdToTest ==
                            OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_LeftHandWrist)
                        {
                            _fullBodySecondBoneIdToTest =
                                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_LeftArmLower;
                        }
                        if (_fullBodyBoneIdToTest ==
                            OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_RightHandWrist)
                        {
                            _fullBodySecondBoneIdToTest =
                                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_RightArmLower;
                        }
                    }
                }

                // Some IK solutions require end effector to be last.
                _armBonesEndEffectorLast = armBones.ToArray();
                _distanceToNextBoneEndEffectorLast = new float[_armBones.Length];
                UpdateBoneDistances();

                if (OVRManager.instance)
                {
                    var ovrCameraRig = OVRManager.instance.gameObject.GetComponent<OVRCameraRig>();
                    if (ovrCameraRig != null)
                    {
                        _ovrCameraRigHead = ovrCameraRig.centerEyeAnchor;
                    }
                    else if (parentProcessor.HeadViewType == HeadView.OVRCameraRig)
                    {
                        Debug.LogError($"OVRCameraRig is missing for " +
                                       $"{retargetingLayer.gameObject.name}'s blend hand processor.");
                    }
                }
            }

            private bool BonePassesChainTest(HumanBodyBones humanBodyBone)
            {
                if (_armChainBones == null || _armChainBones.Length == 0)
                {
                    return true;
                }
                bool boneFound = false;
                foreach (var bone in _armChainBones)
                {
                    if (bone == humanBodyBone)
                    {
                        boneFound = true;
                        break;
                    }
                }
                return boneFound;
            }

            /// <inheritdoc cref="RetargetingProcessorCorrectHand.PrepareRetargetingProcessor"/>
            public void PrepareRetargetingProcessor()
            {
                _originalHandPosition = _armBones[0].position;
            }

            /// <inheritdoc cref="RetargetingProcessorCorrectHand.ProcessRetargetingLayer"/>
            public void ProcessRetargetingLayer(RetargetingLayer retargetingLayer, Transform targetHand,
                RetargetingProcessorCorrectHand parentProcessor)
            {
                // Handle blend hand.
                if (BonesAreValid(parentProcessor.IsFullBody, retargetingLayer))
                {
                    // The blend hand weight can be used to reduce its effect.
                    if (BlendHandWeight > float.Epsilon)
                    {
                        HandIKWeight = ComputeCurrentBlendWeight(retargetingLayer, parentProcessor) * BlendHandWeight;
                    }
                }

                // Handle correct hand.
                var handIKWeight = HandIKWeight * parentProcessor.Weight;
                var targetHandPosition = targetHand.position;
                if (_useWorldHandPosition)
                {
                    var localScale = retargetingLayer.transform.localScale;
                    // Divide by absolute value of scale to avoid sign of scale.
                    localScale = new Vector3(Mathf.Abs(localScale.x), Mathf.Abs(localScale.y), Mathf.Abs(localScale.z));
                    targetHandPosition = RiggingUtilities.DivideVector3(targetHandPosition, localScale);
                }

                if (_useCustomHandTargetPosition && _customHandTargetPosition.HasValue)
                {
                    targetHandPosition = _customHandTargetPosition.Value;
                }

                // Capture hand rotation.
                var handBone = _armBones[0];
                var handRotation = handBone.rotation;

                // Rotate upper arm toward the tracked lower arm.
                if (_useSecondaryBondId)
                {
                    var upperArm = _armBones[2];
                    var upperArmPos = upperArm.position;
                    var lowerArmPos = _armBones[1].position;
                    var targetLowerArmPos =
                        retargetingLayer.Bones[(int)_fullBodySecondBoneIdToTest].Transform.position;
                    var rotationChange =
                        AnimationUtilities.GetRotationChange(upperArmPos, lowerArmPos,
                            upperArmPos, targetLowerArmPos);
                    upperArm.rotation = Quaternion.Slerp(Quaternion.identity, rotationChange, handIKWeight) *
                                        upperArm.rotation;
                }
                else
                {
                    handBone.position = _originalHandPosition;
                }

                Vector3 targetPosition = Vector3.Lerp(handBone.position, targetHandPosition, handIKWeight);
                bool solvedIK = false;
                if (handIKWeight > 0.0f)
                {
                    if (parentProcessor.HandIKType == IKType.CCDIK)
                    {
                        solvedIK = AnimationUtilities.SolveCCDIK(
                            _armBones, targetPosition, parentProcessor.IKTolerance, parentProcessor.IKIterations);
                    }
                    else if (parentProcessor.HandIKType == IKType.FABRIK)
                    {
                        UpdateBoneDistances();
                        solvedIK = AnimationUtilities.SolveFABRIK(
                            _armBonesEndEffectorLast, _distanceToNextBoneEndEffectorLast,
                            targetPosition, parentProcessor.IKTolerance, parentProcessor.IKIterations, true);
                    }
                }
                handBone.position = Vector3.MoveTowards(
                    handBone.position, targetPosition, parentProcessor.MaxHandStretch);
                handBone.rotation = handRotation;

                if (!solvedIK && parentProcessor.MaxShoulderStretch > 0.0f)
                {
                    var shoulderStretchDirection = targetPosition - handBone.position;
                    var shoulderStretchMagnitude = Mathf.Clamp(
                        shoulderStretchDirection.magnitude, 0, parentProcessor.MaxShoulderStretch);
                    var shoulderStretchVector = shoulderStretchDirection.normalized * shoulderStretchMagnitude;
                    _armBones[^1].position += shoulderStretchVector;
                }
            }

            /// <inheritdoc cref="RetargetingProcessorCorrectHand.DrawGizmos"/>
            public void DrawGizmos(HeadView headView)
            {
                if (_cachedTransform == null || _cachedHeadTransform == null)
                {
                    return;
                }

                var viewVector = GetViewVector(headView);
                var viewOriginToPointVector = GetViewOriginToPointVector(_cachedTransform.position);

                Gizmos.color = new Color(_cachedWeight, _cachedWeight, _cachedWeight);
                Gizmos.DrawRay(_cachedHeadTransform.position, viewOriginToPointVector);

                Gizmos.color = headView == HeadView.BodyTracking ? Color.white : Color.blue;
                Gizmos.DrawRay(_cachedHeadTransform.transform.position, 0.7f * viewVector);
            }

            /// <summary>
            /// Computes the current blend hand weight.
            /// </summary>
            /// <param name="skeleton">The skeleton to check.</param>
            /// <param name="parentProcessor">The parent processor.</param>
            /// <returns>The blend hand weight.</returns>
            private float ComputeCurrentBlendWeight(RetargetingLayer skeleton,
                RetargetingProcessorCorrectHand parentProcessor)
            {
                var bones = skeleton.Bones;
                var boneToTest = bones[parentProcessor.IsFullBody ?
                    (int)_fullBodyBoneIdToTest : (int)_boneIdToTest].Transform;
                var headBoneId = parentProcessor.IsFullBody ? (int)OVRSkeleton.BoneId.FullBody_Head : (int)OVRSkeleton.BoneId.Body_Head;
                _cachedTransform = boneToTest;

                if (parentProcessor.HeadViewType == HeadView.BodyTracking)
                {
                    _cachedHeadTransform = bones[headBoneId].Transform;
                }
                else if (parentProcessor.HeadViewType == HeadView.OVRCameraRig)
                {
                    _cachedHeadTransform = _ovrCameraRigHead;
                }
                var boneDistanceToViewVector =
                    GetDistanceToViewVector(boneToTest.position, parentProcessor.HeadViewType);

                _cachedWeight = 0.0f;
                var lossyScale = skeleton.transform.lossyScale;
                var scaledMaxDistance = parentProcessor.MaxDistance * lossyScale.x;
                var scaledMinDistance = parentProcessor.MinDistance * lossyScale.x;

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
                        var lerpValue = (scaledMaxDistance - boneDistanceToViewVector) /
                                          (scaledMaxDistance - scaledMinDistance);
                        var curveValue = parentProcessor.BlendCurve.Evaluate(lerpValue);
                        // Amplify lerping before clamping it.
                        _cachedWeight = Mathf.Clamp01(curveValue);
                    }
                }

                return _cachedWeight;
            }

            private bool BonesAreValid(bool isFullBody, OVRSkeleton skeleton)
            {
                return isFullBody ?
                    skeleton.Bones.Count >= (int)_fullBodyBoneIdToTest :
                    skeleton.Bones.Count >= (int)_boneIdToTest;
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
            /// <param name="point">Point to evaluate.</param>
            /// <param name="headView">The head view type.</param>
            /// <returns>The distance to the view vector.</returns>
            private float GetDistanceToViewVector(Vector3 point, HeadView headView)
            {
                var viewVector = GetViewVector(headView);
                var viewOriginToPointVector = GetViewOriginToPointVector(point);

                var parallelogramArea = Vector3.Cross(viewVector, viewOriginToPointVector).magnitude;
                var parallelogramBaseLength = viewVector.magnitude;
                return parallelogramArea / parallelogramBaseLength;
            }

            private Vector3 GetViewVector(HeadView headView)
            {
                if (headView == HeadView.BodyTracking)
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

            private void UpdateBoneDistances()
            {
                for (int i = 0; i < _armBonesEndEffectorLast.Length; i++)
                {
                    bool lastJoint = i == _armBonesEndEffectorLast.Length - 1;
                    if (lastJoint)
                    {
                        _distanceToNextBoneEndEffectorLast[i] = 0f;
                    }
                    else
                    {
                        _distanceToNextBoneEndEffectorLast[i] = (_armBonesEndEffectorLast[i + 1].position -
                                                                 _armBonesEndEffectorLast[i].position).magnitude;
                    }
                }
            }
        }

        /// <summary>
        /// Enum used to determine which type of head should be used to blend hands.
        /// </summary>
        public enum HeadView
        {
            BodyTracking,
            OVRCameraRig
        }

        /// <summary>
        /// The types of IK available to be used.
        /// </summary>
        public enum IKType
        {
            None,
            CCDIK,
            FABRIK
        }

        /// <summary>
        /// The type of IK that should be applied to modify the arm bones toward the
        /// correct hand target.
        /// </summary>
        [SerializeField, Header("IK Settings")]
        [Tooltip(RetargetingLayerTooltips.HandIKType)]
        private IKType _handIKType = IKType.None;
        /// <inheritdoc cref="_handIKType" />
        public IKType HandIKType
        {
            get => _handIKType;
            set => _handIKType = value;
        }

        /// <summary>
        /// The maximum distance between the resulting position and target position that is allowed.
        /// </summary>
        [SerializeField]
        [Tooltip(RetargetingLayerTooltips.IKTolerance)]
        private float _ikTolerance = 1e-6f;
        /// <inheritdoc cref="_ikTolerance" />
        public float IKTolerance
        {
            get => _ikTolerance;
            set => _ikTolerance = value;
        }

        /// <summary>
        /// The maximum number of iterations allowed for the IK algorithm.
        /// </summary>
        [SerializeField]
        [Tooltip(RetargetingLayerTooltips.IKIterations)]
        private int _ikIterations = 10;
        /// <inheritdoc cref="_ikIterations" />
        public int IKIterations
        {
            get => _ikIterations;
            set => _ikIterations = value;
        }

        /// <summary>
        /// The maximum stretch for the hand to reach the target position that is allowed.
        /// </summary>
        [SerializeField]
        [Tooltip(RetargetingLayerTooltips.MaxHandStretch)]
        private float _maxHandStretch = 0.01f;
        /// <inheritdoc cref="_maxHandStretch" />
        public float MaxHandStretch
        {
            get => _maxHandStretch;
            set => _maxHandStretch = value;
        }

        /// <summary>
        /// The maximum stretch for the shoulder to help the hand reach the target position that is allowed.
        /// </summary>
        [SerializeField]
        [Tooltip(RetargetingLayerTooltips.MaxShoulderStretch)]
        private float _maxShoulderStretch;
        /// <inheritdoc cref="_maxShoulderStretch" />
        public float MaxShoulderStretch
        {
            get => _maxShoulderStretch;
            set => _maxShoulderStretch = value;
        }

        /// <summary>
        /// Distance where weight is set to 1.0.
        /// </summary>
        [SerializeField, Header("Blend Settings")]
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
        private AnimationCurve _blendCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        /// <inheritdoc cref="_blendCurve"/>
        public AnimationCurve BlendCurve
        {
            get => _blendCurve;
            set => _blendCurve = value;
        }

        /// <summary>
        /// The type of head that should be used to blend hands.
        /// </summary>
        [SerializeField]
        [Tooltip(RetargetingBlendHandProcessorTooltips.HeadView)]
        private HeadView _headView = HeadView.BodyTracking;
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
        private bool _isFullBody = true;
        /// <inheritdoc cref="_isFullBody"/>
        public bool IsFullBody
        {
            get => _isFullBody;
            set => _isFullBody = value;
        }

        /// <summary>
        /// Left hand processor.
        /// </summary>
        [SerializeField, Header("Processors")]
        [Tooltip(RetargetingProcessorCorrectHandTooltips.LeftHandProcessor)]
        private HandProcessor _leftHandProcessor;
        /// <inheritdoc cref="_leftHandProcessor" />
        public HandProcessor LeftHandProcessor
        {
            get => _leftHandProcessor;
            set => _leftHandProcessor = value;
        }

        /// <summary>
        /// Right hand processor.
        /// </summary>
        [SerializeField]
        private HandProcessor _rightHandProcessor;
        [Tooltip(RetargetingProcessorCorrectHandTooltips.RightHandProcessor)]
        /// <inheritdoc cref="_rightHandProcessor" />
        public HandProcessor RightHandProcessor
        {
            get => _rightHandProcessor;
            set => _rightHandProcessor = value;
        }

        /// <inheritdoc />
        public override void CopyData(RetargetingProcessor source)
        {
            base.CopyData(source);
            var sourceCorrectHand = source as RetargetingProcessorCorrectHand;
            if (sourceCorrectHand == null)
            {
                Debug.LogError($"Failed to copy properties from {source.name} processor to {name} processor");
                return;
            }

            _handIKType = sourceCorrectHand.HandIKType;
            _ikTolerance = sourceCorrectHand.IKTolerance;
            _ikIterations = sourceCorrectHand.IKIterations;
            _maxHandStretch = sourceCorrectHand.MaxHandStretch;
            _maxShoulderStretch = sourceCorrectHand.MaxShoulderStretch;

            _minDistance = sourceCorrectHand.MinDistance;
            _maxDistance = sourceCorrectHand.MaxDistance;
            _blendCurve = sourceCorrectHand.BlendCurve;
            _isFullBody = sourceCorrectHand.IsFullBody;

            _leftHandProcessor = new HandProcessor();
            _rightHandProcessor = new HandProcessor();

            _leftHandProcessor.HandIKWeight = sourceCorrectHand.LeftHandProcessor.HandIKWeight;
            _leftHandProcessor.BlendHandWeight = sourceCorrectHand.LeftHandProcessor.BlendHandWeight;
            _leftHandProcessor.UseWorldHandPosition = sourceCorrectHand.LeftHandProcessor.UseWorldHandPosition;
            _leftHandProcessor.UseCustomHandTargetPosition = sourceCorrectHand.LeftHandProcessor.UseCustomHandTargetPosition;
            _leftHandProcessor.FullBodySecondBoneIdToTest =
                sourceCorrectHand.LeftHandProcessor.FullBodySecondBoneIdToTest;
            _leftHandProcessor.FullBodyBoneIdToTest = sourceCorrectHand.LeftHandProcessor.FullBodyBoneIdToTest;
            _leftHandProcessor.BoneIdToTest = sourceCorrectHand.LeftHandProcessor.BoneIdToTest;

            _rightHandProcessor.HandIKWeight = sourceCorrectHand.RightHandProcessor.HandIKWeight;
            _rightHandProcessor.BlendHandWeight = sourceCorrectHand.RightHandProcessor.BlendHandWeight;
            _rightHandProcessor.UseWorldHandPosition = sourceCorrectHand.RightHandProcessor.UseWorldHandPosition;
            _rightHandProcessor.UseCustomHandTargetPosition = sourceCorrectHand.RightHandProcessor.UseCustomHandTargetPosition;
            _rightHandProcessor.FullBodySecondBoneIdToTest =
                sourceCorrectHand.RightHandProcessor.FullBodySecondBoneIdToTest;
            _rightHandProcessor.FullBodyBoneIdToTest = sourceCorrectHand.RightHandProcessor.FullBodyBoneIdToTest;
            _rightHandProcessor.BoneIdToTest = sourceCorrectHand.RightHandProcessor.BoneIdToTest;
        }

        /// <inheritdoc />
        public override void SetupRetargetingProcessor(RetargetingLayer retargetingLayer)
        {
            _leftHandProcessor.SetupRetargetingProcessor(retargetingLayer, this, Handedness.Left);
            _rightHandProcessor.SetupRetargetingProcessor(retargetingLayer, this, Handedness.Right);
        }

        /// <inheritdoc />
        public override void PrepareRetargetingProcessor(RetargetingLayer retargetingLayer, IList<OVRBone> ovrBones)
        {
            _leftHandProcessor.PrepareRetargetingProcessor();
            _rightHandProcessor.PrepareRetargetingProcessor();
        }

        /// <inheritdoc />
        public override void ProcessRetargetingLayer(RetargetingLayer retargetingLayer, IList<OVRBone> ovrBones)
        {
            if (Weight < float.Epsilon)
            {
                return;
            }

            var isFullBody = retargetingLayer.GetSkeletonType() == OVRSkeleton.SkeletonType.FullBody;
            var leftHandWristIndex = isFullBody ? (int)OVRSkeleton.BoneId.FullBody_LeftHandWrist :
                (int)OVRSkeleton.BoneId.Body_LeftHandWrist;
            var rightHandWristIndex = isFullBody ? (int)OVRSkeleton.BoneId.FullBody_RightHandWrist :
                (int)OVRSkeleton.BoneId.Body_RightHandWrist;

            if (ovrBones.Count < leftHandWristIndex ||
                 ovrBones.Count < rightHandWristIndex)
            {
                return;
            }

            var leftTargetHand = ovrBones[leftHandWristIndex]?.Transform;
            var rightTargetHand = ovrBones[rightHandWristIndex]?.Transform;
            if (leftTargetHand == null || rightTargetHand == null)
            {
                return;
            }

            _leftHandProcessor.ProcessRetargetingLayer(retargetingLayer, leftTargetHand, this);
            _rightHandProcessor.ProcessRetargetingLayer(retargetingLayer, rightTargetHand, this);
        }

        /// <inheritdoc />
        public override void DrawGizmos()
        {
            _leftHandProcessor.DrawGizmos(_headView);
            _rightHandProcessor.DrawGizmos(_headView);
        }
    }
}
