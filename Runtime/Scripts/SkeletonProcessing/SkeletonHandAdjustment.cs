// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction;
using Oculus.Interaction.Body.Input;
using Oculus.Interaction.Input;
using Oculus.Movement.AnimationRigging;
using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// A filter for skeleton data that will apply a hand pose.
    ///
    /// Use this script to apply a hand pose from Interaction SDK onto a skeleton that can be
    /// modified by <see cref="SkeletonPostprocess"/>
    /// </summary>
    public class SkeletonHandAdjustment : SkeletonProcessorBehaviour
    {
        /// <summary>
        /// Named tuple data structure for mapping between Body and Hand indexes
        /// </summary>
        [System.Serializable]
        public struct HandBodyJointPair
        {
            /// <summary>
            /// List of Hand/Body ID key/value pairs. Used when translating *Left*
            /// Hand joints to match Body joints. Ordered by Hand joint index.
            /// </summary>
            public static readonly HandBodyJointPair[] LeftHandBodyJointPairs =
            {
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
            };

            /// <summary>
            /// List of Hand/Body ID key/value pairs. Used when translating *Right*
            /// Hand joints to match Body joints. Ordered by Hand joint index.
            /// </summary>
            public static readonly HandBodyJointPair[] RightHandBodyJointPairs =
            {
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
            };

            public HandJointId hand;

            public BodyJointId body;

            public HandBodyJointPair(HandJointId hand, BodyJointId body)
            {
                this.hand = hand;
                this.body = body;
            }

            /// <summary>
            /// Used for anonymous tuple conversion
            /// </summary>
            public static implicit operator HandBodyJointPair((HandJointId hand, BodyJointId body) tuple)
                => new HandBodyJointPair(tuple.hand, tuple.body);
        }

        /// <summary>
        /// ISDK Hand source. Can be Synthetic hand
        /// </summary>
        [SerializeField]
        [Tooltip(SkeletonHandAdjustmentTooltips.Hand)]
        [Interface(typeof(IHand))]
        protected UnityEngine.Object _hand;

        /// <summary>
        /// The proper <see cref="IHand"/> used to adjust the skeleton in this process
        /// </summary>
        protected IHand _ihand;

        /// <summary>
        /// Should be the OVRCameraRig, which ISDK hands will offset to follow when this
        /// process is <see cref="HandsAreOffset"/>.
        /// </summary>
        [SerializeField]
        [Tooltip(SkeletonHandAdjustmentTooltips.CameraRig)]
        protected Transform _cameraRig;

        /// <summary>
        /// If the camera rig moves but the body is stationary, this should be true.
        /// If the camera rig and body move together, this can stay false.
        /// </summary>
        [SerializeField]
        [Tooltip(SkeletonHandAdjustmentTooltips.HandsAreOffset)]
        protected bool _handsAreOffset;

        private HandBodyJointPair[] _jointPairs;
        private Quaternion _rigRotationOffset;
        private Vector3 _rigPositionOffset;

        /// <summary>
        /// If the camera rig moves but the body is stationary, this should be true
        /// If the camera rig and body move together, this can stay false
        /// </summary>
        public bool HandsAreOffset { get => _handsAreOffset; set => _handsAreOffset = value; }

        public IHand Hand => _ihand != null ? _ihand : _ihand = _hand as IHand;

        /// <summary>
        /// Edit time method that initializes key variables with reasonable defaults.
        /// </summary>
        private void Reset()
        {
            OVRCameraRig ovrRig = GetComponentInParent<OVRCameraRig>();
            if (ovrRig == null)
            {
                ovrRig = FindObjectOfType<OVRCameraRig>();
            }
            _cameraRig = ovrRig != null ? ovrRig.transform : null;
            if (ovrRig == null)
            {
                Debug.LogWarning($"{name} expects {nameof(OVRCameraRig)} in the scene! Please assign {nameof(_cameraRig)} manually");
            }
            _hand = GetComponentInParent<Hand>();
        }

        /// <summary>
        /// Initializes according to handedness of <see cref="_hand"/>
        /// </summary>
        protected void Start()
        {
            if (Hand == null)
            {
                Debug.LogError($"{this} is missing a proper Hand! Have {_hand}");
                enabled = false;
            }
            switch (Hand.Handedness)
            {
                case Handedness.Left:
                    _jointPairs = HandBodyJointPair.LeftHandBodyJointPairs;
                    break;
                case Handedness.Right:
                    _jointPairs = HandBodyJointPair.RightHandBodyJointPairs;
                    break;
            }
        }

        /// <inheritdoc/>
        public override void ProcessSkeleton(OVRSkeleton skeleton)
        {
            if (!enabled || _hand == null || skeleton == null ||
            skeleton.Bones == null || skeleton.Bones.Count == 0 || !Hand.IsTrackedDataValid)
            {
                return;
            }
            IList<OVRBone> bones = skeleton.Bones;
            UpdateRigOffsetCalculations();
            foreach (HandBodyJointPair pair in _jointPairs)
            {
                int joint = (int)pair.body;
                if (pair.hand == HandJointId.Invalid || joint < 0)
                {
                    continue;
                }
                if (joint >= bones.Count)
                {
                    Debug.LogWarning($"{pair.body} is not in skeleton ({bones.Count})");
                }
                Transform bone = bones[joint].Transform;
                Hand.GetJointPose(pair.hand, out Pose pose);
                if (HandsAreOffset && _cameraRig != null)
                {
                    PosePropagationThatUndoesRigTransformation(ref bone, ref pose);
                }
                else
                {
                    SimplePosePropagation(ref bone, ref pose);
                }
            }
        }

        private void UpdateRigOffsetCalculations()
        {
            if (_cameraRig == null)
            {
                return;
            }
            _rigRotationOffset = Quaternion.Inverse(_cameraRig.rotation);
            _rigPositionOffset = -_cameraRig.position;
        }

        private void SimplePosePropagation(ref Transform bone, ref Pose pose)
        {
            bone.position = pose.position;
            bone.rotation = pose.rotation;
        }

        private void PosePropagationThatUndoesRigTransformation(ref Transform bone, ref Pose pose)
        {
            bone.position = _rigRotationOffset * (pose.position + _rigPositionOffset);
            bone.rotation = _rigRotationOffset * pose.rotation;
        }
    }
}
