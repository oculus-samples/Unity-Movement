// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Movement.Effects
{
    /// <summary>
    /// Used to try to maintain the same proportions in the fingers for both hands.
    /// </summary>
    [DefaultExecutionOrder(250)]
    public class HandDeformation : MonoBehaviour
    {
        /// <summary>
        /// Finger class used for deformation.
        /// </summary>
        [Serializable]
        public class FingerInfo
        {
            /// <summary>
            /// Main constructor.
            /// </summary>
            /// <param name="startTransform">Start transform for finger.</param>
            /// <param name="endTransform">End transform for finger.</param>
            /// <param name="endBoneId">Id of start bone.</param>
            public FingerInfo(Transform startTransform, Transform endTransform,
                OVRSkeleton.BoneId startBoneId, OVRSkeleton.BoneId endBoneId)
            {
                StartBoneTransform = startTransform;
                EndBoneTransform = endTransform;
                StartBoneId = startBoneId;
                EndBoneId = endBoneId;
            }

            /// <summary>
            /// The start bone transform.
            /// </summary>
            public Transform StartBoneTransform;

            /// <summary>
            /// The end bone transform.
            /// </summary>
            public Transform EndBoneTransform;

            /// <summary>
            /// The start transform bone id.
            /// </summary>
            public OVRSkeleton.BoneId StartBoneId;

            /// <summary>
            /// The end transform bone id.
            /// </summary>
            public OVRSkeleton.BoneId EndBoneId;

            /// <summary>
            /// The position offset based on local space to apply.
            /// </summary>
            public Vector3 EndPosOffset = Vector3.zero;

            /// <summary>
            /// The rotation offset to apply.
            /// </summary>
            public Quaternion EndRotOffset = Quaternion.identity;

            private float _distance;
            private const float _SNAP_THRESHOLD = 0.5f;
            private Vector3 _direction;
            private Vector3 _targetPos;
            private Vector3 _currentPos;

            /// <summary>
            /// Updates the distance between the start and end bone transforms.
            /// </summary>
            public void UpdateDistance()
            {
                _distance = Vector3.Distance(StartBoneTransform.position, EndBoneTransform.position);
            }

            /// <summary>
            /// Updates the direction from the start to the end bone transform.
            /// </summary>
            public void UpdateDirection()
            {
                Vector3 endPos = EndBoneTransform.position +
                                 EndBoneTransform.right * EndPosOffset.x +
                                 EndBoneTransform.up * EndPosOffset.y +
                                 EndBoneTransform.forward * EndPosOffset.z;
                _direction = (endPos - StartBoneTransform.position).normalized;
            }

            /// <summary>
            /// Updates the end bone transform position with the direction and distance added to the
            /// start bone transform position.
            /// </summary>
            /// <param name="useMoveTowards">True if we should move towards the target position.</param>
            /// <param name="moveSpeed">The move towards speed.</param>
            public void UpdateBonePosition(bool useMoveTowards, float moveSpeed)
            {
                _targetPos = StartBoneTransform.position + _direction * _distance;
                if (Vector3.Distance(EndBoneTransform.position, _currentPos) >= _SNAP_THRESHOLD)
                {
                    _currentPos = EndBoneTransform.position;
                }
                if (useMoveTowards)
                {
                    _currentPos = Vector3.MoveTowards(_currentPos, _targetPos, Time.deltaTime * moveSpeed);
                }
                else
                {
                    _currentPos = _targetPos;
                }
                EndBoneTransform.position = _currentPos;
            }

            /// <summary>
            /// Update rotation based on offset relative to bind pose.
            /// </summary>
            public void UpdateRotationBasedOnOffset()
            {
                EndBoneTransform.rotation *= EndRotOffset;
            }
        }

        /// <summary>
        /// Finger offset class used for deformation.
        /// </summary>
        [Serializable]
        public class FingerOffset
        {
            /// <summary>
            /// The id of the finger to apply the offset to.
            /// </summary>
            [Tooltip(HandDeformationTooltips.FingerOffset.FingerId)]
            public OVRSkeleton.BoneId FingerId;

            /// <summary>
            /// The finger position offset.
            /// </summary>
            [Tooltip(HandDeformationTooltips.FingerOffset.FingerPosOffset)]
            public Vector3 FingerPosOffset;

            /// <summary>
            /// The finger rotation offset.
            /// </summary>
            [Tooltip(HandDeformationTooltips.FingerOffset.FingerRotOffset)]
            public Vector3 FingerRotOffset;
        }

        /// <summary>
        /// Custom skeleton to reference.
        /// </summary>
        [SerializeField]
        [Tooltip(HandDeformationTooltips.CustomSkeleton)]
        protected OVRCustomSkeleton _customSkeleton;

        /// <summary>
        /// If true, copy the finger offsets data into FingerInfo during every update.
        /// </summary>
        [SerializeField]
        [Tooltip(HandDeformationTooltips.CopyFingerDataInUpdate)]
        protected bool _copyFingerDataInUpdate;

        /// <summary>
        /// The array of finger offsets to be applied.
        /// </summary>
        [SerializeField]
        [Tooltip(HandDeformationTooltips.FingerOffsets)]
        protected FingerOffset[] _fingerOffsets;

        private FingerInfo[] _fingers;
        private bool _shouldUpdate = true;

        /// <summary>
        /// Initialize the finger offsets.
        /// </summary>
        protected void Awake()
        {
            List<FingerInfo> leftFingers = SetUpLeftHand();
            List<FingerInfo> rightFingers = SetUpRightHand();
            List<FingerInfo> allFingers = new List<FingerInfo>();
            allFingers.AddRange(leftFingers);
            allFingers.AddRange(rightFingers);
            _fingers = allFingers.ToArray();
            CopyFingerOffsetData();

            foreach (var finger in _fingers)
            {
                finger.UpdateDistance();
            }
        }

        private List<FingerInfo> SetUpLeftHand()
        {
            List<FingerInfo> fingers = new List<FingerInfo>();
            fingers.Add(new FingerInfo
            (_customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftHandWrist],
                _customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftHandThumbMetacarpal],
                OVRSkeleton.BoneId.Body_LeftHandWrist, OVRSkeleton.BoneId.Body_LeftHandThumbMetacarpal));
            fingers.Add(new FingerInfo
                (_customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftHandThumbMetacarpal],
                _customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftHandThumbProximal],
                OVRSkeleton.BoneId.Body_LeftHandThumbMetacarpal, OVRSkeleton.BoneId.Body_LeftHandThumbProximal));
            fingers.Add(new FingerInfo
                (_customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftHandThumbProximal],
                _customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftHandThumbDistal],
                OVRSkeleton.BoneId.Body_LeftHandThumbProximal, OVRSkeleton.BoneId.Body_LeftHandThumbDistal));
            fingers.Add(new FingerInfo
                (_customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftHandThumbDistal],
                _customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftHandThumbTip],
                OVRSkeleton.BoneId.Body_LeftHandThumbDistal, OVRSkeleton.BoneId.Body_LeftHandThumbTip));

            fingers.Add(new FingerInfo
            (_customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftHandWrist],
                _customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftHandIndexProximal],
                OVRSkeleton.BoneId.Body_LeftHandWrist, OVRSkeleton.BoneId.Body_LeftHandIndexProximal));
            fingers.Add(new FingerInfo
                (_customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftHandIndexMetacarpal],
                _customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftHandIndexProximal],
                OVRSkeleton.BoneId.Body_LeftHandIndexMetacarpal, OVRSkeleton.BoneId.Body_LeftHandIndexProximal));
            fingers.Add(new FingerInfo
                (_customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftHandIndexProximal],
                _customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftHandIndexIntermediate],
                OVRSkeleton.BoneId.Body_LeftHandIndexProximal, OVRSkeleton.BoneId.Body_LeftHandIndexIntermediate));
            fingers.Add(new FingerInfo
                (_customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftHandIndexIntermediate],
                _customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftHandIndexDistal],
                OVRSkeleton.BoneId.Body_LeftHandIndexIntermediate, OVRSkeleton.BoneId.Body_LeftHandIndexDistal));
            fingers.Add(new FingerInfo
                (_customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftHandIndexDistal],
                _customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftHandIndexTip],
                OVRSkeleton.BoneId.Body_LeftHandIndexDistal, OVRSkeleton.BoneId.Body_LeftHandIndexTip));

            fingers.Add(new FingerInfo
            (_customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftHandWrist],
                _customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftHandMiddleProximal],
                OVRSkeleton.BoneId.Body_LeftHandWrist, OVRSkeleton.BoneId.Body_LeftHandMiddleProximal));
            fingers.Add(new FingerInfo
                (_customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftHandMiddleMetacarpal],
                _customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftHandMiddleProximal],
                OVRSkeleton.BoneId.Body_LeftHandMiddleMetacarpal, OVRSkeleton.BoneId.Body_LeftHandMiddleProximal));
            fingers.Add(new FingerInfo
                (_customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftHandMiddleProximal],
                _customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftHandMiddleIntermediate],
                OVRSkeleton.BoneId.Body_LeftHandMiddleProximal, OVRSkeleton.BoneId.Body_LeftHandMiddleIntermediate));
            fingers.Add(new FingerInfo
                (_customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftHandMiddleIntermediate],
                _customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftHandMiddleDistal],
                OVRSkeleton.BoneId.Body_LeftHandMiddleIntermediate, OVRSkeleton.BoneId.Body_LeftHandMiddleDistal));
            fingers.Add(new FingerInfo
                (_customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftHandMiddleDistal],
                _customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftHandMiddleTip],
                OVRSkeleton.BoneId.Body_LeftHandMiddleDistal, OVRSkeleton.BoneId.Body_LeftHandMiddleTip));

            fingers.Add(new FingerInfo
            (_customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftHandWrist],
                _customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftHandRingProximal],
                OVRSkeleton.BoneId.Body_LeftHandWrist, OVRSkeleton.BoneId.Body_LeftHandRingProximal));
            fingers.Add(new FingerInfo
                (_customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftHandRingMetacarpal],
                _customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftHandRingProximal],
                OVRSkeleton.BoneId.Body_LeftHandRingMetacarpal, OVRSkeleton.BoneId.Body_LeftHandRingProximal));
            fingers.Add(new FingerInfo
                (_customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftHandRingProximal],
                _customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftHandRingIntermediate],
                OVRSkeleton.BoneId.Body_LeftHandRingProximal, OVRSkeleton.BoneId.Body_LeftHandRingIntermediate));
            fingers.Add(new FingerInfo
                (_customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftHandRingIntermediate],
                _customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftHandRingDistal],
                OVRSkeleton.BoneId.Body_LeftHandRingIntermediate, OVRSkeleton.BoneId.Body_LeftHandRingDistal));
            fingers.Add(new FingerInfo
                (_customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftHandRingDistal],
                _customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftHandRingTip],
                OVRSkeleton.BoneId.Body_LeftHandRingDistal, OVRSkeleton.BoneId.Body_LeftHandRingTip));

            fingers.Add(new FingerInfo
            (_customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftHandWrist],
                _customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftHandLittleMetacarpal],
                OVRSkeleton.BoneId.Body_LeftHandWrist, OVRSkeleton.BoneId.Body_LeftHandLittleMetacarpal));
            fingers.Add(new FingerInfo
                (_customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftHandLittleMetacarpal],
                _customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftHandLittleProximal],
                OVRSkeleton.BoneId.Body_LeftHandLittleMetacarpal, OVRSkeleton.BoneId.Body_LeftHandLittleProximal));
            fingers.Add(new FingerInfo
                (_customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftHandLittleProximal],
                _customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftHandLittleIntermediate],
                OVRSkeleton.BoneId.Body_LeftHandLittleProximal, OVRSkeleton.BoneId.Body_LeftHandLittleIntermediate));
            fingers.Add(new FingerInfo
                (_customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftHandLittleIntermediate],
                _customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftHandLittleDistal],
                OVRSkeleton.BoneId.Body_LeftHandLittleIntermediate, OVRSkeleton.BoneId.Body_LeftHandLittleDistal));
            fingers.Add(new FingerInfo
                (_customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftHandLittleDistal],
                _customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftHandLittleTip],
                OVRSkeleton.BoneId.Body_LeftHandLittleDistal, OVRSkeleton.BoneId.Body_LeftHandLittleTip));

            for (int i = fingers.Count - 1; i >= 0; i--)
            {
                if (fingers[i].StartBoneTransform == null ||
                    fingers[i].EndBoneTransform == null)
                {
                    fingers.RemoveAt(i);
                }
            }

            return fingers;
        }

        private List<FingerInfo> SetUpRightHand()
        {
            List<FingerInfo> fingers = new List<FingerInfo>();
            fingers.Add(new FingerInfo
            (_customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightHandWrist],
                _customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightHandThumbMetacarpal],
                OVRSkeleton.BoneId.Body_RightHandWrist, OVRSkeleton.BoneId.Body_RightHandThumbMetacarpal));
            fingers.Add(new FingerInfo
                (_customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightHandThumbMetacarpal],
                _customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightHandThumbProximal],
                OVRSkeleton.BoneId.Body_RightHandThumbMetacarpal, OVRSkeleton.BoneId.Body_RightHandThumbProximal));
            fingers.Add(new FingerInfo
                (_customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightHandThumbProximal],
                _customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightHandThumbDistal],
                OVRSkeleton.BoneId.Body_RightHandThumbProximal, OVRSkeleton.BoneId.Body_RightHandThumbDistal));
            fingers.Add(new FingerInfo
                (_customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightHandThumbDistal],
                _customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightHandThumbTip],
                OVRSkeleton.BoneId.Body_RightHandThumbDistal, OVRSkeleton.BoneId.Body_RightHandThumbTip));

            fingers.Add(new FingerInfo
            (_customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightHandWrist],
                _customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightHandIndexProximal],
                OVRSkeleton.BoneId.Body_RightHandWrist, OVRSkeleton.BoneId.Body_RightHandIndexProximal));
            fingers.Add(new FingerInfo
                (_customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightHandIndexMetacarpal],
                _customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightHandIndexProximal],
                OVRSkeleton.BoneId.Body_RightHandIndexMetacarpal, OVRSkeleton.BoneId.Body_RightHandIndexProximal));
            fingers.Add(new FingerInfo
                (_customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightHandIndexProximal],
                _customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightHandIndexIntermediate],
                OVRSkeleton.BoneId.Body_RightHandIndexProximal, OVRSkeleton.BoneId.Body_RightHandIndexIntermediate));
            fingers.Add(new FingerInfo
                (_customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightHandIndexIntermediate],
                _customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightHandIndexDistal],
                OVRSkeleton.BoneId.Body_RightHandIndexIntermediate, OVRSkeleton.BoneId.Body_RightHandIndexDistal));
            fingers.Add(new FingerInfo
                (_customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightHandIndexDistal],
                _customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightHandIndexTip],
                OVRSkeleton.BoneId.Body_RightHandIndexDistal, OVRSkeleton.BoneId.Body_RightHandIndexTip));

            fingers.Add(new FingerInfo
            (_customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightHandWrist],
                _customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightHandMiddleProximal],
                OVRSkeleton.BoneId.Body_RightHandWrist, OVRSkeleton.BoneId.Body_RightHandMiddleProximal));
            fingers.Add(new FingerInfo
                (_customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightHandMiddleMetacarpal],
                _customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightHandMiddleProximal],
                OVRSkeleton.BoneId.Body_RightHandMiddleMetacarpal, OVRSkeleton.BoneId.Body_RightHandMiddleProximal));
            fingers.Add(new FingerInfo
                (_customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightHandMiddleProximal],
                _customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightHandMiddleIntermediate],
                OVRSkeleton.BoneId.Body_RightHandMiddleProximal, OVRSkeleton.BoneId.Body_RightHandMiddleIntermediate));
            fingers.Add(new FingerInfo
                (_customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightHandMiddleIntermediate],
                _customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightHandMiddleDistal],
                OVRSkeleton.BoneId.Body_RightHandMiddleIntermediate, OVRSkeleton.BoneId.Body_RightHandMiddleDistal));
            fingers.Add(new FingerInfo
                (_customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightHandMiddleDistal],
                _customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightHandMiddleTip],
                OVRSkeleton.BoneId.Body_RightHandMiddleDistal, OVRSkeleton.BoneId.Body_RightHandMiddleTip));

            fingers.Add(new FingerInfo
            (_customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightHandWrist],
                _customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightHandRingProximal],
                OVRSkeleton.BoneId.Body_RightHandWrist, OVRSkeleton.BoneId.Body_RightHandRingProximal));
            fingers.Add(new FingerInfo
                (_customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightHandRingMetacarpal],
                _customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightHandRingProximal],
                OVRSkeleton.BoneId.Body_RightHandRingMetacarpal, OVRSkeleton.BoneId.Body_RightHandRingProximal));
            fingers.Add(new FingerInfo
                (_customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightHandRingProximal],
                _customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightHandRingIntermediate],
                OVRSkeleton.BoneId.Body_RightHandRingProximal, OVRSkeleton.BoneId.Body_RightHandRingIntermediate));
            fingers.Add(new FingerInfo
                (_customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightHandRingIntermediate],
                _customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightHandRingDistal],
                OVRSkeleton.BoneId.Body_RightHandRingIntermediate, OVRSkeleton.BoneId.Body_RightHandRingDistal));
            fingers.Add(new FingerInfo
                (_customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightHandRingDistal],
                _customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightHandRingTip],
                OVRSkeleton.BoneId.Body_RightHandRingDistal, OVRSkeleton.BoneId.Body_RightHandRingTip));

            fingers.Add(new FingerInfo
            (_customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightHandWrist],
                _customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightHandLittleMetacarpal],
                OVRSkeleton.BoneId.Body_RightHandWrist, OVRSkeleton.BoneId.Body_RightHandLittleMetacarpal));
            fingers.Add(new FingerInfo
                (_customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightHandLittleMetacarpal],
                _customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightHandLittleProximal],
                OVRSkeleton.BoneId.Body_RightHandLittleMetacarpal, OVRSkeleton.BoneId.Body_RightHandLittleProximal));
            fingers.Add(new FingerInfo
                (_customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightHandLittleProximal],
                _customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightHandLittleIntermediate],
                OVRSkeleton.BoneId.Body_RightHandLittleProximal, OVRSkeleton.BoneId.Body_RightHandLittleIntermediate));
            fingers.Add(new FingerInfo
                (_customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightHandLittleIntermediate],
                _customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightHandLittleDistal],
                OVRSkeleton.BoneId.Body_RightHandLittleIntermediate, OVRSkeleton.BoneId.Body_RightHandLittleDistal));
            fingers.Add(new FingerInfo
                (_customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightHandLittleDistal],
                _customSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightHandLittleTip],
                OVRSkeleton.BoneId.Body_RightHandLittleDistal, OVRSkeleton.BoneId.Body_RightHandLittleTip));

            for (int i = fingers.Count - 1; i >= 0; i--)
            {
                if (fingers[i].StartBoneTransform == null ||
                    fingers[i].EndBoneTransform == null)
                {
                    fingers.RemoveAt(i);
                }
            }

            return fingers;
        }

        /// <summary>
        /// Apply the finger offsets.
        /// </summary>
        protected void LateUpdate()
        {
            if (!_customSkeleton.IsInitialized)
            {
                return;
            }

            if (_customSkeleton.IsDataValid)
            {
                _shouldUpdate = true;
            }

            if (_shouldUpdate)
            {
                if (_copyFingerDataInUpdate)
                {
                    CopyFingerOffsetData();
                }

                foreach (var finger in _fingers)
                {
                    finger.UpdateDirection();
                }

                foreach (var finger in _fingers)
                {
                    finger.UpdateRotationBasedOnOffset();
                }

                foreach (var finger in _fingers)
                {
                    finger.UpdateBonePosition(false, 1.0f);
                }

                if (!_customSkeleton.IsDataValid)
                {
                    _shouldUpdate = false;
                }
            }
        }

        private void CopyFingerOffsetData()
        {
            foreach (var fingerOffset in _fingerOffsets)
            {
                foreach (var finger in _fingers)
                {
                    if (finger.EndBoneId == fingerOffset.FingerId)
                    {
                        finger.EndPosOffset = fingerOffset.FingerPosOffset;
                        finger.EndRotOffset = Quaternion.Euler(fingerOffset.FingerRotOffset);
                    }
                }
            }
        }
    }
}
