// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Movement.Attributes;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Movement.Effects
{
    /// <summary>
    /// Deforms the rig to preserve its original proportions after tracking output has been applied.
    /// </summary>
    public class DeformationLogic : MonoBehaviour
    {
        /// <summary>
        /// Contains information about the distance between two bone transforms.
        /// </summary>
        [System.Serializable]
        public class BoneDistanceInfo
        {
            /// <summary>
            /// The start bone transform.
            /// </summary>
            [Tooltip(DeformationLogicTooltips.BoneDistanceInfo.StartBoneTransform)]
            public Transform StartBoneTransform;

            /// <summary>
            /// The end bone transform.
            /// </summary>
            [Tooltip(DeformationLogicTooltips.BoneDistanceInfo.EndBoneTransform)]
            public Transform EndBoneTransform;

            private float _distance;
            private float _snapThreshold = 0.5f;
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
                _direction = (EndBoneTransform.position - StartBoneTransform.position).normalized;
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
                if (Vector3.Distance(EndBoneTransform.position, _currentPos) >= _snapThreshold)
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
        }

        /// <summary>
        /// Contains information about the positioning of an arm.
        /// </summary>
        [System.Serializable]
        public class ArmPositionInfo
        {
            /// <summary>
            /// The shoulder transform.
            /// </summary>
            [SerializeField]
            [Tooltip(DeformationLogicTooltips.ArmPositionInfo.Shoulder)]
            protected Transform _shoulder;

            /// <summary>
            /// The upper arm transform.
            /// </summary>
            [SerializeField]
            [Tooltip(DeformationLogicTooltips.ArmPositionInfo.UpperArm)]
            protected Transform _upperArm;

            /// <summary>
            /// The lower arm transform.
            /// </summary>
            [SerializeField]
            [Tooltip(DeformationLogicTooltips.ArmPositionInfo.LowerArm)]
            protected Transform _lowerArm;

            /// <summary>
            /// The weight of the offset position.
            /// </summary>
            [SerializeField]
            [Tooltip(DeformationLogicTooltips.ArmPositionInfo.Weight)]
            protected float _weight;

            /// <summary>
            /// The speed of the arm move towards if enabled.
            /// </summary>
            [SerializeField]
            [Tooltip(DeformationLogicTooltips.ArmPositionInfo.MoveSpeed)]
            protected float _moveSpeed = 1.0f;

            /// <summary>
            /// The shoulder transform.
            /// </summary>
            public Transform Shoulder => _shoulder;

            /// <summary>
            /// The upper arm transform.
            /// </summary>
            public Transform UpperArm => _upperArm;

            /// <summary>
            /// The lower arm transform.
            /// </summary>
            public Transform LowerArm => _lowerArm;

            /// <summary>
            /// The cached position of the upper arm.
            /// </summary>
            public Vector3 UpperArmPos { get; set; }

            /// <summary>
            /// The weight of the offset position.
            /// </summary>
            public float Weight => _weight;

            /// <summary>
            /// The speed of the move towards.
            /// </summary>
            public float MoveSpeed => _moveSpeed;
        }

        /// <summary>
        /// The spine translation correction type.
        /// </summary>
        protected enum SpineTranslationCorrectionType
        {
            None,
            SkipHead,
            SkipHips,
            SkipHipsAndHead
        }

        /// <summary>
        /// The OVR Skeleton component.
        /// </summary>
        [SerializeField]
        [ConditionalHide("_mirrorSkeleton", null)]
        [Tooltip(DeformationLogicTooltips.Skeleton)]
        protected OVRCustomSkeleton _skeleton;

        /// <summary>
        /// The Mirror Skeleton component.
        /// </summary>
        [SerializeField]
        [ConditionalHide("_skeleton", null)]
        [Tooltip(DeformationLogicTooltips.MirrorSkeleton)]
        protected MirrorSkeleton _mirrorSkeleton;

        /// <summary>
        /// Animator component. Setting this will cause this script
        /// to ignore the skeleton field.
        /// </summary>
        [SerializeField]
        [Tooltip(DeformationLogicTooltips.Animator)]
        protected Animator _animator;

        /// <summary>
        /// The type of spine translation correction that should be applied.
        /// If set to SkipHips, the spine translation correction offset will be applied to the head.
        /// If set to SkipHead, the spine translation correction offset will be applied to the hips.
        /// </summary>
        [SerializeField]
        [Tooltip(DeformationLogicTooltips.SpineTranslationCorrectionType)]
        protected SpineTranslationCorrectionType _spineTranslationCorrectionType;

        /// <summary>
        /// The position info for the left arm.
        /// </summary>
        [SerializeField]
        [Tooltip(DeformationLogicTooltips.LeftArmPositionInfo)]
        protected ArmPositionInfo _leftArmPositionInfo;

        /// <summary>
        /// The position info for the right arm.
        /// </summary>
        [SerializeField]
        [Tooltip(DeformationLogicTooltips.RightArmPositionInfo)]
        protected ArmPositionInfo _rightArmPositionInfo;

        /// <summary>
        /// Fix arms toggle.
        /// </summary>
        [SerializeField]
        [Tooltip(DeformationLogicTooltips.FixArms)]
        protected bool _fixArms = true;

        /// <summary>
        /// Allows the spine correction to run only once, assuming the skeleton's
        /// positions don't get updated multiple times.
        /// </summary>
        [SerializeField]
        [Tooltip(DeformationLogicTooltips.CorrectSpineOnce)]
        protected bool _correctSpineOnce;

        /// <summary>
        /// If true, the arms will move towards the deformation target position.
        /// </summary>
        [SerializeField]
        [Tooltip(DeformationLogicTooltips.MoveTowardsArms)]
        protected bool _moveTowardsArms;

        private List<Transform> _hipToHeadTransforms = new List<Transform>();
        private List<BoneDistanceInfo> _bones = new List<BoneDistanceInfo>();
        private OVRCustomSkeleton _currentSkeleton;
        private OVRCustomSkeleton _originalSkeleton;
        private Transform _hipTransform;
        private Transform _chestTransform;
        private Transform _headTransform;
        private float _hipToHeadDistance;
        private bool _shouldUpdate = true;
        private bool _hasRunSpineCorrection = false;

        /// <summary>
        /// The hips to head transform list is filled from information in OVRCustomSkeleton.
        /// </summary>
        private void Awake()
        {
            Assert.IsTrue(_skeleton != null || _mirrorSkeleton != null || _animator != null);
            if (_mirrorSkeleton != null)
            {
                _currentSkeleton = _mirrorSkeleton.MirroredSkeleton;
                _originalSkeleton = _mirrorSkeleton.OriginalSkeleton;
            }
            else
            {
                _currentSkeleton = _skeleton;
                _originalSkeleton = _skeleton;
            }

            if (_animator == null)
            {
                for (int i = (int)OVRSkeleton.BoneId.Body_Hips; i <= (int)OVRSkeleton.BoneId.Body_Head; i++)
                {
                    _hipToHeadTransforms.Add(_currentSkeleton.CustomBones[i].transform);
                }
            }
            else
            {
                _hipToHeadTransforms.Add(_animator.GetBoneTransform(HumanBodyBones.Hips).transform);
                _hipToHeadTransforms.Add(_animator.GetBoneTransform(HumanBodyBones.Spine).transform);
                _hipToHeadTransforms.Add(_animator.GetBoneTransform(HumanBodyBones.Chest).transform);
                _hipToHeadTransforms.Add(_animator.GetBoneTransform(HumanBodyBones.UpperChest).transform);
                _hipToHeadTransforms.Add(_animator.GetBoneTransform(HumanBodyBones.Neck).transform);
                _hipToHeadTransforms.Add(_animator.GetBoneTransform(HumanBodyBones.Head).transform);
            }

            _hipTransform =
                _animator == null ?
                _currentSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_Hips].transform :
                _animator.GetBoneTransform(HumanBodyBones.Hips).transform;
            _chestTransform =
                _animator == null ?
                _currentSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_Chest].transform :
                _animator.GetBoneTransform(HumanBodyBones.Chest).transform;
            _headTransform =
                _animator == null ?
                _currentSkeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_Head].transform :
                _animator.GetBoneTransform(HumanBodyBones.Head).transform;

            Assert.IsTrue(_hipToHeadTransforms.Count > 0);
            foreach(var transformItem in _hipToHeadTransforms)
            {
                Assert.IsNotNull(transformItem);
            }
            Assert.IsNotNull(_chestTransform);
            Assert.IsNotNull(_hipTransform);
            Assert.IsNotNull(_headTransform);

            if (_fixArms)
            {
                Assert.IsNotNull(_leftArmPositionInfo.Shoulder);
                Assert.IsNotNull(_leftArmPositionInfo.UpperArm);
                Assert.IsNotNull(_leftArmPositionInfo.LowerArm);
                Assert.IsNotNull(_rightArmPositionInfo.Shoulder);
                Assert.IsNotNull(_rightArmPositionInfo.UpperArm);
                Assert.IsNotNull(_rightArmPositionInfo.LowerArm);
            }
        }

        /// <summary>
        /// The initial distances between bones are calculated and stored here.
        /// </summary>
        private void Start()
        {
            _hipToHeadDistance = Vector3.Distance(_hipTransform.position, _headTransform.position);

            if (_fixArms)
            {
                InitializeBonesArray();
            }
        }

        private void InitializeBonesArray()
        {
            // Add hip to head bones.
            for (var i = 0; i < _hipToHeadTransforms.Count - 1; i++)
            {
                var boneInfo = new BoneDistanceInfo
                {
                    StartBoneTransform = _hipToHeadTransforms[i],
                    EndBoneTransform = _hipToHeadTransforms[i + 1]
                };
                _bones.Add(boneInfo);
            }

            // Add chest to shoulders bones.
            _bones.Add(new BoneDistanceInfo()
            {
                StartBoneTransform = _chestTransform,
                EndBoneTransform = _leftArmPositionInfo.Shoulder
            });
            _bones.Add(new BoneDistanceInfo()
            {
                StartBoneTransform = _chestTransform,
                EndBoneTransform = _rightArmPositionInfo.Shoulder
            });

            // Add shoulders to upper arm bones.
            _bones.Add(new BoneDistanceInfo()
            {
                StartBoneTransform = _leftArmPositionInfo.Shoulder,
                EndBoneTransform = _leftArmPositionInfo.UpperArm
            });
            _bones.Add(new BoneDistanceInfo()
            {
                StartBoneTransform = _rightArmPositionInfo.Shoulder,
                EndBoneTransform = _rightArmPositionInfo.UpperArm
            });

            foreach (var bone in _bones)
            {
                bone.UpdateDistance();
            }
        }

        /// <summary>
        ///  Apply deformation logic if the component is enabled.
        /// </summary>
        public void ApplyDeformation()
        {
            if (!enabled)
            {
                return;
            }

            if (_originalSkeleton.IsDataValid)
            {
                _shouldUpdate = true;
            }

            if (_shouldUpdate)
            {
                foreach (var bone in _bones)
                {
                    bone.UpdateDirection();
                }

                if (_spineTranslationCorrectionType != SpineTranslationCorrectionType.None &&
                    (!_correctSpineOnce || (_correctSpineOnce && !_hasRunSpineCorrection)))
                {
                    SpineTranslationCorrection();
                }

                if (_fixArms)
                {
                    FixArms();
                }

                if (!_originalSkeleton.IsDataValid)
                {
                    _shouldUpdate = false;
                }
            }
        }

        private void FixArms()
        {
            _leftArmPositionInfo.UpperArmPos = _leftArmPositionInfo.UpperArm.position;
            _rightArmPositionInfo.UpperArmPos = _rightArmPositionInfo.UpperArm.position;
            foreach (var bone in _bones)
            {
                if (bone.EndBoneTransform == _leftArmPositionInfo.UpperArm)
                {
                    bone.UpdateBonePosition(_moveTowardsArms, _leftArmPositionInfo.MoveSpeed);
                }
                else if (bone.EndBoneTransform == _rightArmPositionInfo.UpperArm)
                {
                    bone.UpdateBonePosition(_moveTowardsArms, _rightArmPositionInfo.MoveSpeed);
                }
                else
                {
                    bone.UpdateBonePosition(false, 1.0f);
                }
            }

            var leftArmOffset = _leftArmPositionInfo.UpperArm.position - _leftArmPositionInfo.UpperArmPos;
            _leftArmPositionInfo.LowerArm.position += leftArmOffset * _leftArmPositionInfo.Weight;

            var rightArmOffset = _rightArmPositionInfo.UpperArm.position - _rightArmPositionInfo.UpperArmPos;
            _rightArmPositionInfo.LowerArm.position += rightArmOffset * _rightArmPositionInfo.Weight;
        }

        private void SpineTranslationCorrection()
        {
            var currentDir = _hipTransform.position - _headTransform.position;
            var offset = currentDir.normalized * (_hipToHeadDistance - currentDir.magnitude);

            // The bone positions are adjusted so that the length of the spine matches the initial length.
            foreach (var bone in _hipToHeadTransforms)
            {
                if ((_spineTranslationCorrectionType == SpineTranslationCorrectionType.SkipHead && bone == _headTransform) ||
                    (_spineTranslationCorrectionType == SpineTranslationCorrectionType.SkipHips && bone == _hipTransform) ||
                    (_spineTranslationCorrectionType == SpineTranslationCorrectionType.SkipHipsAndHead &&
                    (bone == _headTransform || bone == _hipTransform)))
                {
                    continue;
                }
                bone.position += offset;
            }

            _hasRunSpineCorrection = true;
        }
    }
}
