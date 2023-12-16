// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using Oculus.Interaction;
using UnityEngine;
using static OVRUnityHumanoidSkeletonRetargeter;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// Used to try to maintain the same proportions in the fingers for both hands. Copied from HandDeformation,
    /// but using the full body bone set.
    /// </summary>
    [DefaultExecutionOrder(225)]
    public class FullBodyHandDeformation : MonoBehaviour
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
            /// <param name="startBoneId">Id of start bone.</param>
            /// <param name="endBoneId">Id of end bone.</param>
            public FingerInfo(Transform startTransform, Transform endTransform,
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId startBoneId,
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId endBoneId)
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
            public OVRHumanBodyBonesMappings.FullBodyTrackingBoneId StartBoneId;

            /// <summary>
            /// The end transform bone id.
            /// </summary>
            public OVRHumanBodyBonesMappings.FullBodyTrackingBoneId EndBoneId;

            /// <summary>
            /// The position offset based on local space to apply.
            /// </summary>
            public Vector3 EndPosOffset = Vector3.zero;

            /// <summary>
            /// The rotation offset to apply.
            /// </summary>
            public Quaternion EndRotOffset = Quaternion.identity;

            /// <summary>
            /// Original distance for bone.
            /// </summary>
            public float Distance;

            /// <summary>
            /// The direction of the finger bone.
            /// </summary>
            private Vector3 _direction;

            /// <summary>
            /// Updates the distance between the start and end bone transforms.
            /// </summary>
            public void UpdateDistance()
            {
                if (!IsValid())
                {
                    return;
                }
                Distance = Vector3.Distance(StartBoneTransform.position, EndBoneTransform.position);
            }

            /// <summary>
            /// Updates the direction from the start to the end bone transform.
            /// </summary>
            public void UpdateDirection()
            {
                if (!IsValid())
                {
                    return;
                }
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
            /// <param name="scaleFactor">The scale to be applied.</param>
            public void UpdateBonePosition(Vector3 scaleFactor)
            {
                if (!IsValid())
                {
                    return;
                }
                if (!RiggingUtilities.IsFiniteVector3(StartBoneTransform.position) ||
                    !RiggingUtilities.IsFiniteVector3(EndBoneTransform.position))
                {
                    return;
                }

                var targetPos = StartBoneTransform.position + Vector3.Scale(_direction * Distance, scaleFactor);

                // Make sure to undo the extra bone position.
                if (EndBoneId == OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_LeftHandThumbMetacarpal ||
                    EndBoneId == OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_RightHandThumbMetacarpal)
                {
                    targetPos -= EndBoneTransform.parent.position - StartBoneTransform.position;
                }

                EndBoneTransform.position = targetPos;
            }

            /// <summary>
            /// Update rotation based on offset relative to bind pose.
            /// </summary>
            public void UpdateRotationBasedOnOffset()
            {
                if (!IsValid())
                {
                    return;
                }
                EndBoneTransform.rotation *= EndRotOffset;
            }

            private bool IsValid()
            {
                return StartBoneTransform != null && EndBoneTransform != null;
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
            public OVRHumanBodyBonesMappings.FullBodyTrackingBoneId FingerId;

            /// <summary>
            /// Optional finger bone.
            /// </summary>
            [Optional]
            public Transform Finger;

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
        /// If a finger bone isn't mapped, interpolate the finger bone between two fingers.
        /// </summary>
        [Serializable]
        public class InterpolateFinger
        {
            /// <summary>
            /// Start of finger.
            /// </summary>
            public Transform StartFinger;
            /// <summary>
            /// Target finger to influence.
            /// </summary>
            public Transform TargetFinger;
            /// <summary>
            /// End of finger.
            /// </summary>
            public Transform EndFinger;
            /// <summary>
            /// Distance between start and target fingers transforms.
            /// </summary>
            public float Distance;

            /// <summary>
            /// Update distance between start finger and update finger.
            /// </summary>
            public void UpdateDistance()
            {
                if (StartFinger == null || EndFinger == null)
                {
                    return;
                }

                Distance = Vector3.Distance(StartFinger.position, TargetFinger.position);
            }

            public void UpdateTargetFinger()
            {
                if (StartFinger == null || EndFinger == null || TargetFinger == null)
                {
                    return;
                }

                var endPosition = EndFinger.position;
                var endRotation = EndFinger.rotation;
                var startPosition = StartFinger.position;
                var startRotation = StartFinger.rotation;
                var ratio = Distance / Vector3.Distance(startPosition, endPosition);
                TargetFinger.position = Vector3.Lerp(startPosition, endPosition, ratio);
                TargetFinger.rotation = Quaternion.Slerp(startRotation, endRotation, ratio);
                EndFinger.position = endPosition;
                EndFinger.rotation = endRotation;
            }
        }

        /// <summary>
        /// The character's animator.
        /// </summary>
        [SerializeField]
        [Tooltip(HandDeformationTooltips.Animator)]
        protected Animator _animator;
        /// <summary>
        /// The character's animator.
        /// </summary>
        public Animator AnimatorComp
        {
            get => _animator;
            set => _animator = value;
        }

        /// <summary>
        /// The source skeleton.
        /// </summary>
        [SerializeField]
        [Tooltip(HandDeformationTooltips.Skeleton)]
        protected OVRSkeleton _skeleton;
        /// <summary>
        /// The source skeleton.
        /// </summary>
        public OVRSkeleton Skeleton
        {
            get => _skeleton;
            set => _skeleton = value;
        }

        /// <summary>
        /// The character's left hand bone.
        /// </summary>
        [SerializeField]
        [Tooltip(HandDeformationTooltips.LeftHand)]
        protected Transform _leftHand;
        /// <summary>
        /// The character's left hand bone.
        /// </summary>
        public Transform LeftHand
        {
            get => _leftHand;
            set => _leftHand = value;
        }

        /// <summary>
        /// The character's right hand bone.
        /// </summary>
        [SerializeField]
        [Tooltip(HandDeformationTooltips.RightHand)]
        protected Transform _rightHand;
        /// <summary>
        /// The character's right hand bone.
        /// </summary>
        public Transform RightHand
        {
            get => _rightHand;
            set => _rightHand = value;
        }

        /// <summary>
        /// If true, copy the finger offsets data into FingerInfo during every update.
        /// </summary>
        [SerializeField]
        [Tooltip(HandDeformationTooltips.CopyFingerDataInUpdate)]
        protected bool _copyFingerOffsetsInUpdate;
        /// <summary>
        /// If true, copy the finger offsets data into FingerInfo during every update.
        /// </summary>
        public bool CopyFingerOffsetsInUpdate
        {
            get => _copyFingerOffsetsInUpdate;
            set => _copyFingerOffsetsInUpdate = value;
        }

        /// <summary>
        /// The array of finger offsets to be applied.
        /// </summary>
        [SerializeField]
        [Tooltip(HandDeformationTooltips.FingerOffsets)]
        protected FingerOffset[] _fingerOffsets;
        /// <summary>
        /// The array of finger offsets to be applied.
        /// </summary>
        public FingerOffset[] FingerOffsets
        {
            get => _fingerOffsets;
            set => _fingerOffsets = value;
        }

        /// <summary>
        /// Possible metacarpal bones.
        /// </summary>
        [SerializeField]
        [Tooltip(HandDeformationTooltips.InterpolatedFingers)]
        protected InterpolateFinger[] _interpolatedFingers;
        /// <summary>
        /// Possible metacarpal bones.
        /// </summary>
        public InterpolateFinger[] InterpolatedFingers
        {
            get => _interpolatedFingers;
            set => _interpolatedFingers = value;
        }

        /// <summary>
        /// All finger joints.
        /// </summary>
        [SerializeField]
        [Tooltip(HandDeformationTooltips.Fingers)]
        protected FingerInfo[] _fingers;
        /// <summary>
        /// All finger joints.
        /// </summary>
        public FingerInfo[] Fingers
        {
            get => _fingers;
            set => _fingers = value;
        }

        /// <summary>
        /// If finger data has been calculated or not.
        /// </summary>
        [SerializeField, InspectorButton("CalculateFingerData")]
        [Tooltip(HandDeformationTooltips.CalculateFingerData)]
        protected bool _calculateFingerData;

        private Vector3 _startingScale;

        /// <summary>
        /// Initialize the finger offsets.
        /// </summary>
        protected void Awake()
        {
            if (_fingers == null || _fingers.Length == 0)
            {
                CalculateFingerData();
            }
        }

        /// <summary>
        /// Calculates all finger data necessary for deformation.
        /// </summary>
        public void CalculateFingerData()
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(this, "Undo Hand Deformation Setup");
#endif
            List<FingerInfo> leftFingers = SetUpLeftHand();
            List<FingerInfo> rightFingers = SetUpRightHand();
            List<FingerInfo> allFingers = new List<FingerInfo>();
            allFingers.AddRange(leftFingers);
            allFingers.AddRange(rightFingers);
            _fingers = allFingers.ToArray();
            CopyFingerOffsetData();

            _skeleton = GetComponent<OVRSkeleton>();
            _startingScale = transform.lossyScale;
            _leftHand = _animator.GetBoneTransform(HumanBodyBones.LeftHand);
            _rightHand = _animator.GetBoneTransform(HumanBodyBones.RightHand);
            SetupInterpolatedFingers();
            foreach (var finger in _fingers)
            {
                finger.UpdateDistance();
            }
#if UNITY_EDITOR
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(this);
#endif
        }

        private void SetupInterpolatedFingers()
        {
            List<InterpolateFinger> interpolatedFingers = new List<InterpolateFinger>();
            var leftThumbMetacarpal = CheckPossibleMetacarpal(_leftHand, HumanBodyBones.LeftThumbProximal);
            if (leftThumbMetacarpal != null)
            {
                interpolatedFingers.Add(leftThumbMetacarpal);
            }
            var leftIndexMetacarpal = CheckPossibleMetacarpal(_leftHand, HumanBodyBones.LeftIndexProximal);
            if (leftIndexMetacarpal != null)
            {
                interpolatedFingers.Add(leftIndexMetacarpal);
            }
            var leftMiddleMetacarpal = CheckPossibleMetacarpal(_leftHand, HumanBodyBones.LeftMiddleProximal);
            if (leftMiddleMetacarpal != null)
            {
                interpolatedFingers.Add(leftMiddleMetacarpal);
            }
            var leftRingMetacarpal = CheckPossibleMetacarpal(_leftHand, HumanBodyBones.LeftRingProximal);
            if (leftRingMetacarpal != null)
            {
                interpolatedFingers.Add(leftRingMetacarpal);
            }
            var leftLittleMetacarpal = CheckPossibleMetacarpal(_leftHand, HumanBodyBones.LeftLittleProximal);
            if (leftLittleMetacarpal != null)
            {
                interpolatedFingers.Add(leftLittleMetacarpal);
            }

            var rightThumbMetacarpal = CheckPossibleMetacarpal(_rightHand, HumanBodyBones.RightThumbProximal);
            if (rightThumbMetacarpal != null)
            {
                interpolatedFingers.Add(rightThumbMetacarpal);
            }
            var rightIndexMetacarpal = CheckPossibleMetacarpal(_rightHand, HumanBodyBones.RightIndexProximal);
            if (rightIndexMetacarpal != null)
            {
                interpolatedFingers.Add(rightIndexMetacarpal);
            }
            var rightMiddleMetacarpal = CheckPossibleMetacarpal(_rightHand, HumanBodyBones.RightMiddleProximal);
            if (rightMiddleMetacarpal != null)
            {
                interpolatedFingers.Add(rightMiddleMetacarpal);
            }
            var rightRingMetacarpal = CheckPossibleMetacarpal(_rightHand, HumanBodyBones.RightRingProximal);
            if (rightRingMetacarpal != null)
            {
                interpolatedFingers.Add(rightRingMetacarpal);
            }
            var rightLittleMetacarpal = CheckPossibleMetacarpal(_rightHand, HumanBodyBones.RightLittleProximal);
            if (rightLittleMetacarpal != null)
            {
                interpolatedFingers.Add(rightLittleMetacarpal);
            }

            _interpolatedFingers = interpolatedFingers.ToArray();
            foreach (var interpolatedFinger in _interpolatedFingers)
            {
                interpolatedFinger.UpdateDistance();
            }
        }

        private InterpolateFinger CheckPossibleMetacarpal(Transform hand, HumanBodyBones targetBoneId)
        {
            var targetBone = _animator.GetBoneTransform(targetBoneId);
            var targetBoneParent = targetBone.parent;
            if (targetBoneParent != hand)
            {
                return new InterpolateFinger
                {
                    StartFinger = hand,
                    TargetFinger = targetBoneParent,
                    EndFinger = targetBone
                };
            }
            return null;
        }

        private List<FingerInfo> SetUpLeftHand()
        {
            List<FingerInfo> fingers = new List<FingerInfo>();
            fingers.Add(new FingerInfo
            (_animator.GetBoneTransform(OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[OVRSkeleton.BoneId.FullBody_LeftHandWrist]),
                _animator.GetBoneTransform(
                    OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[
                        OVRSkeleton.BoneId.FullBody_LeftHandThumbMetacarpal]),
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_LeftHandWrist,
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_LeftHandThumbMetacarpal));
            fingers.Add(new FingerInfo
            (_animator.GetBoneTransform(OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[OVRSkeleton.BoneId.FullBody_LeftHandThumbMetacarpal]),
                _animator.GetBoneTransform(
                    OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[
                        OVRSkeleton.BoneId.FullBody_LeftHandThumbProximal]),
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_LeftHandThumbMetacarpal,
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_LeftHandThumbProximal));
            fingers.Add(new FingerInfo
            (_animator.GetBoneTransform(OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[OVRSkeleton.BoneId.FullBody_LeftHandThumbProximal]),
                _animator.GetBoneTransform(
                    OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[
                        OVRSkeleton.BoneId.FullBody_LeftHandThumbDistal]),
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_LeftHandThumbProximal,
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_LeftHandThumbDistal));

            fingers.Add(new FingerInfo
            (_animator.GetBoneTransform(OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[OVRSkeleton.BoneId.FullBody_LeftHandWrist]),
                _animator.GetBoneTransform(
                    OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[
                        OVRSkeleton.BoneId.FullBody_LeftHandIndexProximal]),
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_LeftHandWrist,
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_LeftHandIndexProximal));
            fingers.Add(new FingerInfo
            (_animator.GetBoneTransform(OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[OVRSkeleton.BoneId.FullBody_LeftHandIndexProximal]),
                _animator.GetBoneTransform(
                    OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[
                        OVRSkeleton.BoneId.FullBody_LeftHandIndexIntermediate]),
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_LeftHandIndexProximal,
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_LeftHandIndexIntermediate));
            fingers.Add(new FingerInfo
            (_animator.GetBoneTransform(OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[OVRSkeleton.BoneId.FullBody_LeftHandIndexIntermediate]),
                _animator.GetBoneTransform(
                    OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[
                        OVRSkeleton.BoneId.FullBody_LeftHandIndexDistal]),
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_LeftHandIndexIntermediate,
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_LeftHandIndexDistal));

            fingers.Add(new FingerInfo
            (_animator.GetBoneTransform(OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[OVRSkeleton.BoneId.FullBody_LeftHandWrist]),
                _animator.GetBoneTransform(
                    OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[
                        OVRSkeleton.BoneId.FullBody_LeftHandMiddleProximal]),
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_LeftHandWrist,
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_LeftHandMiddleProximal));
            fingers.Add(new FingerInfo
            (_animator.GetBoneTransform(OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[OVRSkeleton.BoneId.FullBody_LeftHandMiddleProximal]),
                _animator.GetBoneTransform(
                    OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[
                        OVRSkeleton.BoneId.FullBody_LeftHandMiddleIntermediate]),
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_LeftHandMiddleProximal,
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_LeftHandMiddleIntermediate));
            fingers.Add(new FingerInfo
            (_animator.GetBoneTransform(OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[OVRSkeleton.BoneId.FullBody_LeftHandMiddleIntermediate]),
                _animator.GetBoneTransform(
                    OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[
                        OVRSkeleton.BoneId.FullBody_LeftHandMiddleDistal]),
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_LeftHandMiddleIntermediate,
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_LeftHandMiddleDistal));

            fingers.Add(new FingerInfo
            (_animator.GetBoneTransform(OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[OVRSkeleton.BoneId.FullBody_LeftHandWrist]),
                _animator.GetBoneTransform(
                    OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[
                        OVRSkeleton.BoneId.FullBody_LeftHandRingProximal]),
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_LeftHandWrist,
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_LeftHandRingProximal));
            fingers.Add(new FingerInfo
            (_animator.GetBoneTransform(OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[OVRSkeleton.BoneId.FullBody_LeftHandRingProximal]),
                _animator.GetBoneTransform(
                    OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[
                        OVRSkeleton.BoneId.FullBody_LeftHandRingIntermediate]),
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_LeftHandRingProximal,
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_LeftHandRingIntermediate));
            fingers.Add(new FingerInfo
            (_animator.GetBoneTransform(OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[OVRSkeleton.BoneId.FullBody_LeftHandRingIntermediate]),
                _animator.GetBoneTransform(
                    OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone
                        [OVRSkeleton.BoneId.FullBody_LeftHandRingDistal]),
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_LeftHandRingIntermediate,
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_LeftHandRingDistal));

            fingers.Add(new FingerInfo
            (_animator.GetBoneTransform(OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[OVRSkeleton.BoneId.FullBody_LeftHandWrist]),
                _animator.GetBoneTransform(
                    OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[
                        OVRSkeleton.BoneId.FullBody_LeftHandLittleProximal]),
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_LeftHandWrist,
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_LeftHandLittleProximal));
            fingers.Add(new FingerInfo
            (_animator.GetBoneTransform(OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[OVRSkeleton.BoneId.FullBody_LeftHandLittleProximal]),
                _animator.GetBoneTransform(
                    OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[
                        OVRSkeleton.BoneId.FullBody_LeftHandLittleIntermediate]),
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_LeftHandLittleProximal,
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_LeftHandLittleIntermediate));
            fingers.Add(new FingerInfo
            (_animator.GetBoneTransform(OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[OVRSkeleton.BoneId.FullBody_LeftHandLittleIntermediate]),
                _animator.GetBoneTransform(
                    OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[
                        OVRSkeleton.BoneId.FullBody_LeftHandLittleDistal]),
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_LeftHandLittleIntermediate,
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_LeftHandLittleDistal));

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
            (_animator.GetBoneTransform(OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[OVRSkeleton.BoneId.FullBody_RightHandWrist]),
                _animator.GetBoneTransform(
                    OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[
                        OVRSkeleton.BoneId.FullBody_RightHandThumbMetacarpal]),
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_RightHandWrist,
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_RightHandThumbMetacarpal));
            fingers.Add(new FingerInfo
            (_animator.GetBoneTransform(OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[OVRSkeleton.BoneId.FullBody_RightHandThumbMetacarpal]),
                _animator.GetBoneTransform(
                    OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[
                        OVRSkeleton.BoneId.FullBody_RightHandThumbProximal]),
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_RightHandThumbMetacarpal,
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_RightHandThumbProximal));
            fingers.Add(new FingerInfo
            (_animator.GetBoneTransform(OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[OVRSkeleton.BoneId.FullBody_RightHandThumbProximal]),
                _animator.GetBoneTransform(
                    OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[
                        OVRSkeleton.BoneId.FullBody_RightHandThumbDistal]),
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_RightHandThumbProximal,
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_RightHandThumbDistal));

            fingers.Add(new FingerInfo
            (_animator.GetBoneTransform(OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[OVRSkeleton.BoneId.FullBody_RightHandWrist]),
                _animator.GetBoneTransform(
                    OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[
                        OVRSkeleton.BoneId.FullBody_RightHandIndexProximal]),
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_RightHandWrist,
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_RightHandIndexProximal));
            fingers.Add(new FingerInfo
            (_animator.GetBoneTransform(OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[OVRSkeleton.BoneId.FullBody_RightHandIndexProximal]),
                _animator.GetBoneTransform(
                    OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[
                        OVRSkeleton.BoneId.FullBody_RightHandIndexIntermediate]),
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_RightHandIndexProximal,
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_RightHandIndexIntermediate));
            fingers.Add(new FingerInfo
            (_animator.GetBoneTransform(OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[OVRSkeleton.BoneId.FullBody_RightHandIndexIntermediate]),
                _animator.GetBoneTransform(
                    OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[
                        OVRSkeleton.BoneId.FullBody_RightHandIndexDistal]),
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_RightHandIndexIntermediate,
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_RightHandIndexDistal));

            fingers.Add(new FingerInfo
            (_animator.GetBoneTransform(OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[OVRSkeleton.BoneId.FullBody_RightHandWrist]),
                _animator.GetBoneTransform(
                    OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[
                        OVRSkeleton.BoneId.FullBody_RightHandMiddleProximal]),
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_RightHandWrist,
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_RightHandMiddleProximal));
            fingers.Add(new FingerInfo
            (_animator.GetBoneTransform(OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[OVRSkeleton.BoneId.FullBody_RightHandMiddleProximal]),
                _animator.GetBoneTransform(
                    OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[
                        OVRSkeleton.BoneId.FullBody_RightHandMiddleIntermediate]),
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_RightHandMiddleProximal,
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_RightHandMiddleIntermediate));
            fingers.Add(new FingerInfo
            (_animator.GetBoneTransform(OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[OVRSkeleton.BoneId.FullBody_RightHandMiddleIntermediate]),
                _animator.GetBoneTransform(
                    OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[
                        OVRSkeleton.BoneId.FullBody_RightHandMiddleDistal]),
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_RightHandMiddleIntermediate,
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_RightHandMiddleDistal));

            fingers.Add(new FingerInfo
            (_animator.GetBoneTransform(OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[OVRSkeleton.BoneId.FullBody_RightHandWrist]),
                _animator.GetBoneTransform(
                    OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[
                        OVRSkeleton.BoneId.FullBody_RightHandRingProximal]),
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_RightHandWrist,
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_RightHandRingProximal));
            fingers.Add(new FingerInfo
            (_animator.GetBoneTransform(OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[OVRSkeleton.BoneId.FullBody_RightHandRingProximal]),
                _animator.GetBoneTransform(
                    OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[
                        OVRSkeleton.BoneId.FullBody_RightHandRingIntermediate]),
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_RightHandRingProximal,
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_RightHandRingIntermediate));
            fingers.Add(new FingerInfo
            (_animator.GetBoneTransform(OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[OVRSkeleton.BoneId.FullBody_RightHandRingIntermediate]),
                _animator.GetBoneTransform(
                    OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[
                        OVRSkeleton.BoneId.FullBody_RightHandRingDistal]),
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_RightHandRingIntermediate,
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_RightHandRingDistal));

            fingers.Add(new FingerInfo
            (_animator.GetBoneTransform(OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[OVRSkeleton.BoneId.FullBody_RightHandWrist]),
                _animator.GetBoneTransform(
                    OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[
                        OVRSkeleton.BoneId.FullBody_RightHandLittleProximal]),
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_RightHandWrist,
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_RightHandLittleProximal));
            fingers.Add(new FingerInfo
            (_animator.GetBoneTransform(OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[OVRSkeleton.BoneId.FullBody_RightHandLittleProximal]),
                _animator.GetBoneTransform(
                    OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[
                        OVRSkeleton.BoneId.FullBody_RightHandLittleIntermediate]),
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_RightHandLittleProximal,
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_RightHandLittleIntermediate));
            fingers.Add(new FingerInfo
            (_animator.GetBoneTransform(OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[OVRSkeleton.BoneId.FullBody_RightHandLittleIntermediate]),
                _animator.GetBoneTransform(
                    OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[
                        OVRSkeleton.BoneId.FullBody_RightHandLittleDistal]),
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_RightHandLittleIntermediate,
                OVRHumanBodyBonesMappings.FullBodyTrackingBoneId.FullBody_RightHandLittleDistal));

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
            var scaleFactor = DivideVector3(transform.lossyScale, _startingScale);
            if (_copyFingerOffsetsInUpdate)
            {
                CopyFingerOffsetData();
            }

            if (_skeleton.Bones == null || _skeleton.Bones.Count == 0)
            {
                return;
            }

            foreach (var fingerOffset in _fingerOffsets)
            {
                if (fingerOffset.Finger != null)
                {
                    var child = fingerOffset.Finger.GetChild(0);
                    var childPos = child.position;
                    var childRot = child.rotation;
                    var posOffset = fingerOffset.Finger.right * fingerOffset.FingerPosOffset.x +
                                    fingerOffset.Finger.up * fingerOffset.FingerPosOffset.y +
                                    fingerOffset.Finger.forward * fingerOffset.FingerPosOffset.z;
                    fingerOffset.Finger.localPosition += posOffset;
                    fingerOffset.Finger.localRotation *= Quaternion.Euler(fingerOffset.FingerRotOffset);
                    child.position = childPos;
                    child.rotation = childRot;
                }
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
                finger.UpdateBonePosition(scaleFactor);
            }
            foreach (var interpolatedFinger in _interpolatedFingers)
            {
                interpolatedFinger.UpdateTargetFinger();
            }
        }

        private void CopyFingerOffsetData()
        {
            foreach (var fingerOffset in _fingerOffsets)
            {
                if (fingerOffset.Finger != null)
                {
                    continue;
                }

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

        private Vector3 DivideVector3(Vector3 dividend, Vector3 divisor)
        {
            Vector3 targetScale = Vector3.one;
            if (IsNonZero(divisor))
            {
                targetScale = new Vector3(
                    dividend.x / divisor.x, dividend.y / divisor.y, dividend.z / divisor.z);
            }

            return targetScale;
        }

        private bool IsNonZero(Vector3 v)
        {
            return v.x != 0 && v.y != 0 && v.z != 0;
        }
    }
}
