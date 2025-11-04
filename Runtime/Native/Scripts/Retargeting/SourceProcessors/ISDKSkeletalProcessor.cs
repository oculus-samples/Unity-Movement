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
    /// Overrides joints with poses provided via ISDK.
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
        /// The maximum distance the wrist and fingers can be displaced by ISDK.
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
                trackerPoses.Length == 0)
            {
                return;
            }

            var vectorToPreAdjustedWristPosition =
                ComputeVectorToPreAdjustedWristPosition(
                    handInterface,
                    jointPairs,
                    trackerPoses);
            var vectorToStartingHandPosition = _moveHandBackToOriginalPosition
                ? vectorToPreAdjustedWristPosition
                : Vector3.zero;

            foreach (var pair in jointPairs)
            {
                var joint = (int)pair.BodyJointID;
                if (pair.HandJointID == HandJointId.Invalid || joint < 0)
                {
                    continue;
                }

                if (joint >= trackerPoses.Length)
                {
                    Debug.LogWarning($"{pair.BodyJointID} is not in skeleton ({trackerPoses.Length})");
                    continue;
                }

                var bone = trackerPoses[joint];
                handInterface.GetJointPose(pair.HandJointID, out Pose iSDKPose);
#if ISDK_78_OR_NEWER || ISDK_OPENXR_HAND
                if (OVRPlugin.HandSkeletonVersion == OVRHandSkeletonVersion.OpenXR)
                {
                    SkeletonUtilities.ConvertOpenXRHandToOvrHand(pair.BodyJointID, ref iSDKPose);
                }
#endif

                var originalHandPos = bone.Position;
                var originalHandRot = bone.Orientation;
                var iSDKPosCameraRig =
                    _cameraRig?.transform.InverseTransformPoint(iSDKPose.position) ?? iSDKPose.position;
                var iSDKRotCameraRig = _cameraRig != null
                    ? Quaternion.Inverse(_cameraRig.transform.rotation) * iSDKPose.rotation
                    : iSDKPose.rotation;
                if (_moveHandBackToOriginalPosition)
                {
                    var targetPosition = iSDKPosCameraRig + vectorToStartingHandPosition;
                    bone.Position = Vector3.Lerp(originalHandPos, targetPosition, Weight);
                    var slerpedRotation = Quaternion.Slerp(originalHandRot, iSDKRotCameraRig, Weight);
                    bone.Orientation = slerpedRotation;
                }
                else
                {
                    var targetPosition = iSDKPosCameraRig;
                    var restrictedPosition =
                        Vector3.MoveTowards(originalHandPos, targetPosition, _maxDisplacementDistance);
                    bone.Position = Vector3.Lerp(originalHandPos, restrictedPosition, Weight);
                    // restrict rotation the same way we do position, based on how much position is restricted.
                    var slerpValueBasedOnRestriction = (restrictedPosition - originalHandPos).magnitude /
                                                       (targetPosition - originalHandPos).magnitude;
                    var sourceRotation = bone.Orientation;
                    var restrictedRotation = Quaternion.Slerp(
                        sourceRotation,
                        iSDKRotCameraRig,
                        slerpValueBasedOnRestriction);

                    var slerpedRotation = Quaternion.Slerp(sourceRotation, restrictedRotation, Weight);
                    bone.Orientation = slerpedRotation;
                }

                trackerPoses[joint] = bone;
            }
        }

        /// <summary>
        /// Gets the vector from the adjusted wrist position to its original position.
        /// Use this in case you want the finger to snap to the ISDK pose, but you don't
        /// want the wrist to drift from its original position.
        /// </summary>
        /// <param name="handInterface"><see cref="IHand"/> reference.</param>
        /// <param name="jointPairs">Hand-body joint pairs.</param>
        /// <param name="trackerPoses">Tracker poses.</param>
        /// <returns>Vector from adjusted position back to the original</returns>
        private Vector3 ComputeVectorToPreAdjustedWristPosition(
            IHand handInterface,
            HandBodyJointPair[] jointPairs,
            NativeArray<NativeTransform> trackerPoses)
        {
            var bodyHand = trackerPoses[(int)jointPairs[0].BodyJointID];
            var startingHandPose = new Pose(bodyHand.Position, bodyHand.Orientation);
            if (SkeletonUtilities.GetInteractionHandJointWorldPosition(
                    handInterface,
                    jointPairs[0].HandJointID,
                    jointPairs[0].BodyJointID,
                    _cameraRig,
                    out var targetHandPos))
            {
                return startingHandPose.position - targetHandPos;
            }

            return Vector3.zero;
        }
#endif
    }
}
