// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using Oculus.Interaction;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using static OVRUnityHumanoidSkeletonRetargeter.OVRHumanBodyBonesMappings;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// Retargeting processor.
    /// </summary>
    [CreateAssetMenu(fileName = "Hand Deformation", menuName = "Movement Samples/Data/Retargeting Processors/Hand Deformation", order = 1)]
    public sealed class RetargetingHandDeformationProcessor : RetargetingProcessor
    {
        /// <summary>
        /// Finger class used for deformation.
        /// </summary>
        [Serializable]
        public class FingerBonePairData
        {
            /// <summary>
            /// The adjustment for the bone transform.
            /// </summary>
            public AffineTransform BoneAdjustment = AffineTransform.identity;

            /// <summary>
            /// The start bone.
            /// </summary>
            public HumanBodyBones StartBone;

            /// <summary>
            /// The end bone.
            /// </summary>
            public HumanBodyBones EndBone;

            /// <summary>
            /// Original distance for bone.
            /// </summary>
            [SerializeField]
            private float _distance;

            private Vector3 _direction;
            private Transform _startBoneTransform;
            private Transform _endBoneTransform;

            /// <summary>
            /// Main constructor.
            /// </summary>
            public FingerBonePairData(HumanBodyBones startBone, HumanBodyBones endBone, float distance)
            {
                StartBone = startBone;
                EndBone = endBone;
                _distance = distance;
            }

            /// <summary>
            /// Cache the bone pair transform references.
            /// </summary>
            /// <param name="animator">The animator to get the references from.</param>
            public void CacheBonePairTransforms(Animator animator)
            {
                _startBoneTransform = animator.GetBoneTransform(StartBone);
                _endBoneTransform = animator.GetBoneTransform(EndBone);
            }

            /// <summary>
            /// Update the direction of the bone pair.
            /// </summary>
            public void UpdateDirection()
            {
                _direction = (_endBoneTransform.position - _startBoneTransform.position).normalized;
            }

            /// <summary>
            /// Updates the end bone transform position with the direction and distance added to the
            /// start bone transform position, applying any defined offsets.
            /// </summary>
            /// <param name="scaleFactor">The scale to be applied.</param>
            /// <param name="weight">The weight to blend to the target position.</param>
            public void UpdatePositions(Vector3 scaleFactor, float weight)
            {
                if (!RiggingUtilities.IsFiniteVector3(_startBoneTransform.position) ||
                    !RiggingUtilities.IsFiniteVector3(_endBoneTransform.position))
                {
                    return;
                }

                var targetPos = _startBoneTransform.position +
                                Vector3.Scale(_direction * _distance, scaleFactor);

                _endBoneTransform.position = Vector3.Lerp(_endBoneTransform.position, targetPos, weight);
            }

            /// <summary>
            /// Apply user defined offsets.
            /// </summary>
            public void ApplyOffsets()
            {
                if (BoneAdjustment.rotation.eulerAngles.sqrMagnitude > float.Epsilon)
                {
                    _endBoneTransform.rotation *= BoneAdjustment.rotation;
                }

                if (BoneAdjustment.translation.sqrMagnitude > float.Epsilon)
                {
                    _endBoneTransform.localPosition += BoneAdjustment.translation;
                }
            }
        }

        /// <summary>
        /// The starting scale used to capture deformation data.
        /// </summary>
        [SerializeField]
        private Vector3 _startingScale;
        /// <inheritdoc cref="_startingScale"/>
        public Vector3 StartingScale
        {
            get => _startingScale;
            set => _startingScale = value;
        }

        /// <summary>
        /// The list of finger bone pairs.
        /// </summary>
        [SerializeField]
        private List<FingerBonePairData> _fingerBonePairs;
        /// <inheritdoc cref="_fingerBonePairs"/>
        public List<FingerBonePairData> FingerBonePairs
        {
            get => _fingerBonePairs;
            set => _fingerBonePairs = value;
        }

        /// <summary>
        /// Inspector button used to calculate finger data in editor.
        /// </summary>
        [SerializeField, InspectorButton("CalculateFingerDataFromSerializedObject")]
        private bool _calculateFingerData;

        private readonly Tuple<HumanBodyBones, HumanBodyBones>[] _leftHandBonePairs =
        {
            new (HumanBodyBones.LeftHand, HumanBodyBones.LeftThumbProximal),
            new (HumanBodyBones.LeftThumbProximal, HumanBodyBones.LeftThumbIntermediate),
            new (HumanBodyBones.LeftThumbIntermediate, HumanBodyBones.LeftThumbDistal),
            new (HumanBodyBones.LeftHand, HumanBodyBones.LeftIndexProximal),
            new (HumanBodyBones.LeftIndexProximal, HumanBodyBones.LeftIndexIntermediate),
            new (HumanBodyBones.LeftIndexIntermediate, HumanBodyBones.LeftIndexDistal),
            new (HumanBodyBones.LeftHand, HumanBodyBones.LeftMiddleProximal),
            new (HumanBodyBones.LeftMiddleProximal, HumanBodyBones.LeftMiddleIntermediate),
            new (HumanBodyBones.LeftMiddleIntermediate, HumanBodyBones.LeftMiddleDistal),
            new (HumanBodyBones.LeftHand, HumanBodyBones.LeftRingProximal),
            new (HumanBodyBones.LeftRingProximal, HumanBodyBones.LeftRingIntermediate),
            new (HumanBodyBones.LeftRingIntermediate, HumanBodyBones.LeftRingDistal),
            new (HumanBodyBones.LeftHand, HumanBodyBones.LeftLittleProximal),
            new (HumanBodyBones.LeftLittleProximal, HumanBodyBones.LeftLittleIntermediate),
            new (HumanBodyBones.LeftLittleIntermediate, HumanBodyBones.LeftLittleDistal)
        };

        private readonly Tuple<HumanBodyBones, HumanBodyBones>[] _rightHandBonePairs =
        {
            new (HumanBodyBones.RightHand, HumanBodyBones.RightThumbProximal),
            new (HumanBodyBones.RightThumbProximal, HumanBodyBones.RightThumbIntermediate),
            new (HumanBodyBones.RightThumbIntermediate, HumanBodyBones.RightThumbDistal),
            new (HumanBodyBones.RightHand, HumanBodyBones.RightIndexProximal),
            new (HumanBodyBones.RightIndexProximal, HumanBodyBones.RightIndexIntermediate),
            new (HumanBodyBones.RightIndexIntermediate, HumanBodyBones.RightIndexDistal),
            new (HumanBodyBones.RightHand, HumanBodyBones.RightMiddleProximal),
            new (HumanBodyBones.RightMiddleProximal, HumanBodyBones.RightMiddleIntermediate),
            new (HumanBodyBones.RightMiddleIntermediate, HumanBodyBones.RightMiddleDistal),
            new (HumanBodyBones.RightHand, HumanBodyBones.RightRingProximal),
            new (HumanBodyBones.RightRingProximal, HumanBodyBones.RightRingIntermediate),
            new (HumanBodyBones.RightRingIntermediate, HumanBodyBones.RightRingDistal),
            new (HumanBodyBones.RightHand, HumanBodyBones.RightLittleProximal),
            new (HumanBodyBones.RightLittleProximal, HumanBodyBones.RightLittleIntermediate),
            new (HumanBodyBones.RightLittleIntermediate, HumanBodyBones.RightLittleDistal)
        };

        /// <inheritdoc />
        public override void CopyData(RetargetingProcessor source)
        {
            base.CopyData(source);
            var sourceHandDeformationProcessor = source as RetargetingHandDeformationProcessor;
            if (sourceHandDeformationProcessor == null)
            {
                Debug.LogError($"Failed to copy properties from {source.name} processor to {name} processor");
                return;
            }

            _startingScale = sourceHandDeformationProcessor._startingScale;
            _fingerBonePairs = sourceHandDeformationProcessor._fingerBonePairs;
        }

        /// <inheritdoc />
        public override void SetupRetargetingProcessor(RetargetingLayer retargetingLayer)
        {
            var animator = retargetingLayer.GetAnimatorTargetSkeleton();
            if (_fingerBonePairs.Count == 0)
            {
                CalculateFingerData(animator);
            }
            foreach (var bonePair in _fingerBonePairs)
            {
                bonePair.CacheBonePairTransforms(animator);
            }
        }

        /// <inheritdoc />
        public override void PrepareRetargetingProcessor(RetargetingLayer retargetingLayer, IList<OVRBone> ovrBones)
        {
        }

        /// <inheritdoc />
        public override void ProcessRetargetingLayer(RetargetingLayer retargetingLayer, IList<OVRBone> ovrBones)
        {
            if (Weight < float.Epsilon)
            {
                return;
            }

            var scale = retargetingLayer.transform.lossyScale;
            scale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            var scaleFactor = RiggingUtilities.DivideVector3(scale, _startingScale);

            foreach (var bonePair in _fingerBonePairs)
            {
                bonePair.UpdateDirection();
            }

            foreach (var bonePair in _fingerBonePairs)
            {
                bonePair.UpdatePositions(scaleFactor, Weight);
                bonePair.ApplyOffsets();
            }
        }

        /// <summary>
        /// Calculate the finger data for an animator.
        /// </summary>
        /// <param name="animator"></param>
        public void CalculateFingerData(Animator animator)
        {
            var handBonePairs = new List<Tuple<HumanBodyBones, HumanBodyBones>>();
            handBonePairs.AddRange(_leftHandBonePairs);
            handBonePairs.AddRange(_rightHandBonePairs);

            _startingScale = animator.transform.lossyScale;
            _fingerBonePairs = new List<FingerBonePairData>();
            foreach (var bonePair in handBonePairs)
            {
                var startBone = GetValidFingerBone(animator, bonePair.Item1);
                var endBone = GetValidFingerBone(animator, bonePair.Item2);
                if (startBone != HumanBodyBones.LastBone &&
                    endBone != HumanBodyBones.LastBone)
                {
                    var startBoneTransform = animator.GetBoneTransform(startBone);
                    var endBoneTransform = animator.GetBoneTransform(endBone);
                    var boneDistance = Vector3.Distance(startBoneTransform.position, endBoneTransform.position);
                    _fingerBonePairs.Add(new FingerBonePairData(startBone, endBone, boneDistance));
                }
            }
        }

        private void CalculateFingerDataFromSerializedObject()
        {
#if UNITY_EDITOR
            var gameObject = Selection.activeObject as GameObject;
            if (gameObject != null)
            {
                var animator = gameObject.GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    CalculateFingerData(animator);
                }
            }
#endif
        }

        private HumanBodyBones GetValidFingerBone(Animator animator, HumanBodyBones fingerBone)
        {
            if (fingerBone == HumanBodyBones.LastBone)
            {
                return HumanBodyBones.LastBone;
            }
            if (animator.GetBoneTransform(fingerBone) != null)
            {
                return fingerBone;
            }
            return GetValidFingerBone(animator, BoneToJointPair[fingerBone].Item2);
        }
    }
}
