// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

#if ISDK_DEFINED
using Oculus.Interaction;
using Oculus.Interaction.Body.Input;
using Oculus.Interaction.Input;
#endif
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using static Meta.XR.Movement.MSDKUtility;

namespace Meta.XR.Movement.Retargeting
{
    /// <summary>
    /// Blends ISDK hand tracking into body joints while preserving body hand geometry.
    /// </summary>
    [System.Serializable]
    public class ISDKSkeletalProcessor : SourceProcessor
    {
#if ISDK_DEFINED
        /// <summary>
        /// Named tuple data structure for mapping between Body and Hand indices.
        /// </summary>
        [System.Serializable]
        public struct HandBodyJointPair
        {
            /// <summary>
            /// List of hand/body ID key/value pairs. Used when translating *left*
            /// hand joints to match body joints. Ordered by hand joint index.
            /// </summary>
            public static readonly HandBodyJointPair[] LeftHandBodyJointPairs =
            {
#if ISDK_78_OR_NEWER || ISDK_OPENXR_HAND
                (HandJointId.HandWristRoot, BodyJointId.Body_LeftHandWrist),
                (HandJointId.HandPalm, BodyJointId.Body_LeftHandPalm),
                (HandJointId.HandThumb1, BodyJointId.Body_LeftHandThumbMetacarpal),
                (HandJointId.HandThumb2, BodyJointId.Body_LeftHandThumbProximal),
                (HandJointId.HandThumb3, BodyJointId.Body_LeftHandThumbDistal),
                (HandJointId.HandIndex0, BodyJointId.Body_LeftHandIndexMetacarpal),
                (HandJointId.HandIndex1, BodyJointId.Body_LeftHandIndexProximal),
                (HandJointId.HandIndex2, BodyJointId.Body_LeftHandIndexIntermediate),
                (HandJointId.HandIndex3, BodyJointId.Body_LeftHandIndexDistal),
                (HandJointId.HandMiddle0, BodyJointId.Body_LeftHandMiddleMetacarpal),
                (HandJointId.HandMiddle1, BodyJointId.Body_LeftHandMiddleProximal),
                (HandJointId.HandMiddle2, BodyJointId.Body_LeftHandMiddleIntermediate),
                (HandJointId.HandMiddle3, BodyJointId.Body_LeftHandMiddleDistal),
                (HandJointId.HandRing0, BodyJointId.Body_LeftHandRingMetacarpal),
                (HandJointId.HandRing1, BodyJointId.Body_LeftHandRingProximal),
                (HandJointId.HandRing2, BodyJointId.Body_LeftHandRingIntermediate),
                (HandJointId.HandRing3, BodyJointId.Body_LeftHandRingDistal),
                (HandJointId.HandPinky0, BodyJointId.Body_LeftHandLittleMetacarpal),
                (HandJointId.HandPinky1, BodyJointId.Body_LeftHandLittleProximal),
                (HandJointId.HandPinky2, BodyJointId.Body_LeftHandLittleIntermediate),
                (HandJointId.HandPinky3, BodyJointId.Body_LeftHandLittleDistal),
                (HandJointId.HandThumbTip, BodyJointId.Body_LeftHandThumbTip),
                (HandJointId.HandIndexTip, BodyJointId.Body_LeftHandIndexTip),
                (HandJointId.HandMiddleTip, BodyJointId.Body_LeftHandMiddleTip),
                (HandJointId.HandRingTip, BodyJointId.Body_LeftHandRingTip),
                (HandJointId.HandPinkyTip, BodyJointId.Body_LeftHandLittleTip),
#else
                (HandJointId.HandWristRoot, BodyJointId.Body_LeftHandWrist),
                (HandJointId.HandForearmStub, BodyJointId.Body_LeftHandPalm),
                (HandJointId.HandThumb0, BodyJointId.Invalid),
                (HandJointId.HandThumb1, BodyJointId.Body_LeftHandThumbMetacarpal),
                (HandJointId.HandThumb2, BodyJointId.Body_LeftHandThumbProximal),
                (HandJointId.HandThumb3, BodyJointId.Body_LeftHandThumbDistal),
                (HandJointId.HandIndex1, BodyJointId.Body_LeftHandIndexProximal),
                (HandJointId.HandIndex2, BodyJointId.Body_LeftHandIndexIntermediate),
                (HandJointId.HandIndex3, BodyJointId.Body_LeftHandIndexDistal),
                (HandJointId.HandMiddle1, BodyJointId.Body_LeftHandMiddleProximal),
                (HandJointId.HandMiddle2, BodyJointId.Body_LeftHandMiddleIntermediate),
                (HandJointId.HandMiddle3, BodyJointId.Body_LeftHandMiddleDistal),
                (HandJointId.HandRing1, BodyJointId.Body_LeftHandRingProximal),
                (HandJointId.HandRing2, BodyJointId.Body_LeftHandRingIntermediate),
                (HandJointId.HandRing3, BodyJointId.Body_LeftHandRingDistal),
                (HandJointId.HandPinky0, BodyJointId.Body_LeftHandLittleMetacarpal),
                (HandJointId.HandPinky1, BodyJointId.Body_LeftHandLittleProximal),
                (HandJointId.HandPinky2, BodyJointId.Body_LeftHandLittleIntermediate),
                (HandJointId.HandPinky3, BodyJointId.Body_LeftHandLittleDistal),
                (HandJointId.HandThumbTip, BodyJointId.Body_LeftHandThumbTip),
                (HandJointId.HandIndexTip, BodyJointId.Body_LeftHandIndexTip),
                (HandJointId.HandMiddleTip, BodyJointId.Body_LeftHandMiddleTip),
                (HandJointId.HandRingTip, BodyJointId.Body_LeftHandRingTip),
                (HandJointId.HandPinkyTip, BodyJointId.Body_LeftHandLittleTip),
                (HandJointId.Invalid, BodyJointId.Body_LeftHandIndexMetacarpal),
                (HandJointId.Invalid, BodyJointId.Body_LeftHandMiddleMetacarpal),
                (HandJointId.Invalid, BodyJointId.Body_LeftHandRingMetacarpal),
#endif
            };

            /// <summary>
            /// List of hand/body ID key/value pairs. Used when translating *right*
            /// hand joints to match body joints. Ordered by hand joint index.
            /// </summary>
            public static readonly HandBodyJointPair[] RightHandBodyJointPairs =
            {
#if ISDK_78_OR_NEWER || ISDK_OPENXR_HAND
                (HandJointId.HandWristRoot, BodyJointId.Body_RightHandWrist),
                (HandJointId.HandPalm, BodyJointId.Body_RightHandPalm),
                (HandJointId.HandThumb1, BodyJointId.Body_RightHandThumbMetacarpal),
                (HandJointId.HandThumb2, BodyJointId.Body_RightHandThumbProximal),
                (HandJointId.HandThumb3, BodyJointId.Body_RightHandThumbDistal),
                (HandJointId.HandIndex0, BodyJointId.Body_RightHandIndexMetacarpal),
                (HandJointId.HandIndex1, BodyJointId.Body_RightHandIndexProximal),
                (HandJointId.HandIndex2, BodyJointId.Body_RightHandIndexIntermediate),
                (HandJointId.HandIndex3, BodyJointId.Body_RightHandIndexDistal),
                (HandJointId.HandMiddle0, BodyJointId.Body_RightHandMiddleMetacarpal),
                (HandJointId.HandMiddle1, BodyJointId.Body_RightHandMiddleProximal),
                (HandJointId.HandMiddle2, BodyJointId.Body_RightHandMiddleIntermediate),
                (HandJointId.HandMiddle3, BodyJointId.Body_RightHandMiddleDistal),
                (HandJointId.HandRing0, BodyJointId.Body_RightHandRingMetacarpal),
                (HandJointId.HandRing1, BodyJointId.Body_RightHandRingProximal),
                (HandJointId.HandRing2, BodyJointId.Body_RightHandRingIntermediate),
                (HandJointId.HandRing3, BodyJointId.Body_RightHandRingDistal),
                (HandJointId.HandPinky0, BodyJointId.Body_RightHandLittleMetacarpal),
                (HandJointId.HandPinky1, BodyJointId.Body_RightHandLittleProximal),
                (HandJointId.HandPinky2, BodyJointId.Body_RightHandLittleIntermediate),
                (HandJointId.HandPinky3, BodyJointId.Body_RightHandLittleDistal),
                (HandJointId.HandThumbTip, BodyJointId.Body_RightHandThumbTip),
                (HandJointId.HandIndexTip, BodyJointId.Body_RightHandIndexTip),
                (HandJointId.HandMiddleTip, BodyJointId.Body_RightHandMiddleTip),
                (HandJointId.HandRingTip, BodyJointId.Body_RightHandRingTip),
                (HandJointId.HandPinkyTip, BodyJointId.Body_RightHandLittleTip),
#else
                (HandJointId.HandWristRoot, BodyJointId.Body_RightHandWrist),
                (HandJointId.HandForearmStub, BodyJointId.Body_RightHandPalm),
                (HandJointId.HandThumb0, BodyJointId.Invalid),
                (HandJointId.HandThumb1, BodyJointId.Body_RightHandThumbMetacarpal),
                (HandJointId.HandThumb2, BodyJointId.Body_RightHandThumbProximal),
                (HandJointId.HandThumb3, BodyJointId.Body_RightHandThumbDistal),
                (HandJointId.HandIndex1, BodyJointId.Body_RightHandIndexProximal),
                (HandJointId.HandIndex2, BodyJointId.Body_RightHandIndexIntermediate),
                (HandJointId.HandIndex3, BodyJointId.Body_RightHandIndexDistal),
                (HandJointId.HandMiddle1, BodyJointId.Body_RightHandMiddleProximal),
                (HandJointId.HandMiddle2, BodyJointId.Body_RightHandMiddleIntermediate),
                (HandJointId.HandMiddle3, BodyJointId.Body_RightHandMiddleDistal),
                (HandJointId.HandRing1, BodyJointId.Body_RightHandRingProximal),
                (HandJointId.HandRing2, BodyJointId.Body_RightHandRingIntermediate),
                (HandJointId.HandRing3, BodyJointId.Body_RightHandRingDistal),
                (HandJointId.HandPinky0, BodyJointId.Body_RightHandLittleMetacarpal),
                (HandJointId.HandPinky1, BodyJointId.Body_RightHandLittleProximal),
                (HandJointId.HandPinky2, BodyJointId.Body_RightHandLittleIntermediate),
                (HandJointId.HandPinky3, BodyJointId.Body_RightHandLittleDistal),
                (HandJointId.HandThumbTip, BodyJointId.Body_RightHandThumbTip),
                (HandJointId.HandIndexTip, BodyJointId.Body_RightHandIndexTip),
                (HandJointId.HandMiddleTip, BodyJointId.Body_RightHandMiddleTip),
                (HandJointId.HandRingTip, BodyJointId.Body_RightHandRingTip),
                (HandJointId.HandPinkyTip, BodyJointId.Body_RightHandLittleTip),
                (HandJointId.Invalid, BodyJointId.Body_RightHandIndexMetacarpal),
                (HandJointId.Invalid, BodyJointId.Body_RightHandMiddleMetacarpal),
                (HandJointId.Invalid, BodyJointId.Body_RightHandRingMetacarpal),
#endif
            };

            /// <summary>
            /// ID of hand joint.
            /// </summary>
            public readonly HandJointId HandJointID;

            /// <summary>
            /// ID of body joint.
            /// </summary>
            public readonly BodyJointId BodyJointID;

            /// <summary>
            /// Constructor for <see cref="HandBodyJointPair"/>.
            /// </summary>
            /// <param name="handJointId">Hand joint ID.</param>
            /// <param name="bodyJointId">Body joint ID.</param>
            public HandBodyJointPair(HandJointId handJointId, BodyJointId bodyJointId)
            {
                HandJointID = handJointId;
                BodyJointID = bodyJointId;
            }

            /// <summary>
            /// Used for anonymous tuple conversion
            /// </summary>
            public static implicit operator HandBodyJointPair((HandJointId handJointId, BodyJointId bodyJointId) tuple)
                => new(tuple.handJointId, tuple.bodyJointId);
        }
#endif

        /// <inheritdoc cref="_leftHand"/>
        public GameObject LeftHand
        {
            get => _leftHand;
            set => _leftHand = value;
        }

        /// <inheritdoc cref="_rightHand"/>
        public GameObject RightHand
        {
            get => _rightHand;
            set => _rightHand = value;
        }

        /// <inheritdoc cref="_cameraRig"/>
        public OVRCameraRig CameraRig
        {
            get => _cameraRig;
            set => _cameraRig = value;
        }

        /// <inheritdoc cref="_maxDisplacementDistance"/>
        public float MaxDisplacementDistance
        {
            get => _maxDisplacementDistance;
            set => _maxDisplacementDistance = value;
        }

        /// <summary>
        /// Camera rig object.
        /// </summary>
        [SerializeField]
        protected OVRCameraRig _cameraRig;

        /// <summary>
        /// Left hand game object that has iHand component.
        /// </summary>
        [SerializeField]
        protected GameObject _leftHand = null;

        /// <summary>
        /// Right hand game object that has iHand component.
        /// </summary>
        [SerializeField]
        protected GameObject _rightHand = null;

        /// <summary>
        /// True if wrist position should be maintained.
        /// </summary>
        [SerializeField]
        protected bool _moveHandBackToOriginalPosition = false;

        /// <summary>
        /// The maximum distance the wrist can be displaced by ISDK.
        /// </summary>
        [SerializeField]
        [Range(0.0f, 1.0f)]
        protected float _maxDisplacementDistance = 0.05f;

#if ISDK_DEFINED
        private HandBodyJointPair[] _jointPairsLeft;
        private HandBodyJointPair[] _jointPairsRight;
        private IHand _iHandLeft, _iHandRight;
#endif


        /// <inheritdoc />
        public override void Initialize(CharacterRetargeter characterRetargeter)
        {
#if ISDK_DEFINED
            SetupHand(_leftHand, out _iHandLeft, out _jointPairsLeft);
            SetupHand(_rightHand, out _iHandRight, out _jointPairsRight);
#endif
        }

        /// <inheritdoc cref="SourceProcessor.ProcessSkeleton(NativeArray{NativeTransform})"/>
        public override void ProcessSkeleton(NativeArray<NativeTransform> trackerPoses)
        {
#if ISDK_DEFINED
            AdjustBodyPosesBasedOnHand(_iHandLeft, _jointPairsLeft, trackerPoses);
            AdjustBodyPosesBasedOnHand(_iHandRight, _jointPairsRight, trackerPoses);
#endif
        }

#if ISDK_DEFINED
        private void SetupHand(GameObject handObject, out IHand handInterface, out HandBodyJointPair[] jointPairs)
        {
            handInterface = null;
            jointPairs = null;

            Assert.IsNotNull(handObject);
            var handVisual = handObject.GetComponentInChildren<HandVisual>(true);
            if (handVisual != null)
            {
                handInterface = handVisual.Hand;
            }
            else
            {
                var handInterfaces = handObject.GetComponentsInChildren<IHand>();
                handInterface = handInterfaces[^1];
            }

            switch (handInterface.Handedness)
            {
                case Handedness.Left:
                    jointPairs = HandBodyJointPair.LeftHandBodyJointPairs;
                    break;
                case Handedness.Right:
                    jointPairs = HandBodyJointPair.RightHandBodyJointPairs;
                    break;
            }
        }

        private void AdjustBodyPosesBasedOnHand(
            IHand handInterface,
            HandBodyJointPair[] jointPairs,
            NativeArray<NativeTransform> trackerPoses)
        {
            if (Weight <= 0.0f || handInterface is not { IsTrackedDataValid: true } ||
                jointPairs is not { Length: > 0 } || trackerPoses.Length == 0)
            {
                return;
            }

            var maxWristDisplacement = _moveHandBackToOriginalPosition
                ? 0.0f
                : Mathf.Max(0.0f, _maxDisplacementDistance);
            if (!TryComputeHandDisplacement(
                    handInterface,
                    jointPairs,
                    trackerPoses,
                    maxWristDisplacement,
                    out var wristBodyJointId,
                    out var isdkWristPose,
                    out var handDisplacement))
            {
                return;
            }

            var weight = Mathf.Clamp01(Weight);
            var weightedHandDisplacement = weight * handDisplacement;
            foreach (var pair in jointPairs)
            {
                var joint = (int)pair.BodyJointID;
                if (joint < 0)
                {
                    continue;
                }

                if (joint >= trackerPoses.Length)
                {
                    Debug.LogWarning($"{pair.BodyJointID} is not in skeleton ({trackerPoses.Length})");
                    continue;
                }

                var bone = trackerPoses[joint];
                // Move the complete body hand, including joints without an ISDK rotation mapping.
                bone.Position += weightedHandDisplacement;
                if (pair.HandJointID == HandJointId.Invalid)
                {
                    trackerPoses[joint] = bone;
                    continue;
                }

                Pose iSDKPose;
                if (pair.BodyJointID == wristBodyJointId)
                {
                    iSDKPose = isdkWristPose;
                }
                else if (!SkeletonUtilities.GetInteractionHandJointWorldPose(
                             handInterface,
                             pair.HandJointID,
                             pair.BodyJointID,
                             out iSDKPose))
                {
                    trackerPoses[joint] = bone;
                    continue;
                }

                bone.Orientation = Quaternion.Slerp(bone.Orientation, iSDKPose.rotation, weight);
                trackerPoses[joint] = bone;
            }
        }

        /// <summary>
        /// Computes one displacement for the complete body hand from the restricted ISDK wrist position.
        /// </summary>
        /// <param name="handInterface"><see cref="IHand"/> reference.</param>
        /// <param name="jointPairs">Hand-body joint pairs.</param>
        /// <param name="trackerPoses">Tracker poses.</param>
        /// <param name="maxWristDisplacement">Maximum wrist displacement from the body pose.</param>
        /// <param name="wristBodyJointId">The body wrist used as the displacement anchor.</param>
        /// <param name="isdkWristPose">The converted ISDK wrist pose.</param>
        /// <param name="displacement">Displacement applied to every body hand joint.</param>
        /// <returns>True if both wrist positions are available.</returns>
        private bool TryComputeHandDisplacement(
            IHand handInterface,
            HandBodyJointPair[] jointPairs,
            NativeArray<NativeTransform> trackerPoses,
            float maxWristDisplacement,
            out BodyJointId wristBodyJointId,
            out Pose isdkWristPose,
            out Vector3 displacement)
        {
            wristBodyJointId = BodyJointId.Invalid;
            isdkWristPose = default;
            displacement = Vector3.zero;
            HandBodyJointPair wristPair = default;
            var hasWristPair = false;
            foreach (var pair in jointPairs)
            {
                if (pair.HandJointID != HandJointId.Invalid &&
                    pair.BodyJointID is BodyJointId.Body_LeftHandWrist or BodyJointId.Body_RightHandWrist)
                {
                    wristPair = pair;
                    wristBodyJointId = pair.BodyJointID;
                    hasWristPair = true;
                    break;
                }
            }

            if (!hasWristPair)
            {
                return false;
            }

            var bodyJointIndex = (int)wristPair.BodyJointID;
            if (bodyJointIndex < 0 || bodyJointIndex >= trackerPoses.Length)
            {
                return false;
            }

            var bodyWristPosition = trackerPoses[bodyJointIndex].Position;
            if (SkeletonUtilities.GetInteractionHandJointWorldPose(
                    handInterface,
                    wristPair.HandJointID,
                    wristPair.BodyJointID,
                    out isdkWristPose))
            {
                var targetWristPosition = Vector3.MoveTowards(
                    bodyWristPosition,
                    isdkWristPose.position,
                    maxWristDisplacement);
                displacement = targetWristPosition - bodyWristPosition;
                return true;
            }

            return false;
        }
#endif
    }
}
