// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction;
using Oculus.Movement.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using static Oculus.Movement.AnimationRigging.Deprecated.DeformationCommon;

namespace Oculus.Movement.AnimationRigging.Deprecated
{
    /// <summary>
    /// Interface for deformation data.
    /// </summary>
    public interface IDeformationData
    {
        /// <summary>
        /// The OVRCustomSkeleton component for the character.
        /// </summary>
        public OVRCustomSkeleton ConstraintCustomSkeleton { get; }

        /// <summary>
        /// The Animator component for the character.
        /// </summary>
        public Animator ConstraintAnimator { get; }

        /// <summary>
        /// If true, update this job.
        /// </summary>
        public bool ShouldUpdate { get; set; }

        /// <summary>
        /// The array of transforms from the hips to the head bones.
        /// </summary>
        public Transform[] HipsToHeadBones { get; }

        /// <summary>
        /// The array of transform targets from the hips to the head bones.
        /// </summary>
        public Transform[] HipsToHeadBoneTargets { get; }

        /// <summary>
        /// The position info for the bone pairs used for Deformation.
        /// </summary>
        public BonePairData[] BonePairs { get; }

        /// <summary>
        /// The position info for the left arm.
        /// </summary>
        public ArmPosData LeftArm { get; }

        /// <summary>
        /// The position info for the right arm.
        /// </summary>
        public ArmPosData RightArm { get; }

        /// <summary>
        /// Indicates if left arm data was initialized or not.
        /// </summary>
        public bool LeftArmDataInitialized { get; }

        /// <summary>
        /// Indicates if left arm data was initialized or not.
        /// </summary>
        public bool RightArmDataInitialized { get; }

        /// <summary>
        /// The type of spine translation correction that should be applied.
        /// </summary>
        public int SpineCorrectionType { get; }

        /// <summary>
        /// The starting scale of the character, taken from the animator transform.
        /// </summary>
        public Vector3 StartingScale { get; }

        /// <summary>
        /// The spine correction type int property.
        /// </summary>
        public string SpineCorrectionTypeIntProperty { get; }

        /// <summary>
        /// The spine alignment weight float property.
        /// </summary>
        public string SpineLowerAlignmentWeightFloatProperty { get; }

        /// <summary>
        /// The spine upper alignment weight float property.
        /// </summary>
        public string SpineUpperAlignmentWeightFloatProperty { get; }

        /// <summary>
        /// The chest alignment weight float property.
        /// </summary>
        public string ChestAlignmentWeightFloatProperty { get; }

        /// <summary>
        /// The left shoulder weight float property.
        /// </summary>
        public string LeftShoulderWeightFloatProperty { get; }

        /// <summary>
        /// The right shoulder weight float property.
        /// </summary>
        public string RightShoulderWeightFloatProperty { get; }

        /// <summary>
        /// The left arm weight float property.
        /// </summary>
        public string LeftArmWeightFloatProperty { get; }

        /// <summary>
        /// The right arm weight float property.
        /// </summary>
        public string RightArmWeightFloatProperty { get; }

        /// <summary>
        /// The left hand weight float property.
        /// </summary>
        public string LeftHandWeightFloatProperty { get; }

        /// <summary>
        /// The right hand weight float property.
        /// </summary>
        public string RightHandWeightFloatProperty { get; }

        /// <summary>
        /// The distance between the hips and head bones.
        /// </summary>
        public float HipsToHeadDistance { get; }

        /// <summary>
        /// Sets up hips to head bones.
        /// </summary>
        public void SetUpHipsToHeadBones();

        /// <summary>
        /// Sets up hips to head bone targets.
        /// </summary>
        public void SetUpHipsToHeadBoneTargets(Transform setupParent);

        /// <summary>
        /// Sets up left arm data.
        /// </summary>
        public void SetUpLeftArmData();

        /// <summary>
        /// Sets up right arm data.
        /// </summary>
        public void SetUpRightArmData();

        /// <summary>
        /// Sets up bone parts after all bones have been found.
        /// </summary>
        public void SetUpBonePairs();

        /// <summary>
        /// Computes initial starting scale.
        /// </summary>
        public void InitializeStartingScale();

        /// <summary>
        /// Clears all transform data stored.
        /// </summary>
        public void ClearTransformData();

        /// <summary>
        /// Indicates if bone transforms are valid or not.
        /// </summary>
        /// <returns>True if bone transforms are valid, false if not.</returns>
        public bool IsBoneTransformsDataValid();
    }

    /// <summary>
    /// Deformation data used by the deformation job.
    /// Implements the deformation data interface.
    /// </summary>
    [Serializable]
    public struct DeformationData : IAnimationJobData, IDeformationData
    {
        /// <summary>
        /// The spine translation correction type.
        /// </summary>
        public enum SpineTranslationCorrectionType
        {
            /// <summary>No spine translation correction applied.</summary>
            None = 0,
            /// <summary>Accurately place the head bone when applying spine translation correction.</summary>
            AccurateHead = 1,
            /// <summary>Accurately place the hips bone when applying spine translation correction.</summary>
            AccurateHips = 2,
            /// <summary>Accurately place the hips and head bone when applying spine translation correction.</summary>
            AccurateHipsAndHead = 3,
        }

        /// <inheritdoc />
        OVRCustomSkeleton IDeformationData.ConstraintCustomSkeleton => _customSkeleton;

        /// <inheritdoc />
        Animator IDeformationData.ConstraintAnimator => _animator;

        /// <inheritdoc />
        bool IDeformationData.ShouldUpdate
        {
            get => _shouldUpdate;
            set => _shouldUpdate = value;
        }

        /// <inheritdoc />
        int IDeformationData.SpineCorrectionType => _spineTranslationCorrectionType;

        /// <inheritdoc />
        Transform[] IDeformationData.HipsToHeadBones => _hipsToHeadBones;

        /// <inheritdoc />
        Transform[] IDeformationData.HipsToHeadBoneTargets => _hipsToHeadBoneTargets;

        /// <inheritdoc />
        BonePairData[] IDeformationData.BonePairs => _bonePairData;

        /// <inheritdoc />
        ArmPosData IDeformationData.LeftArm => _leftArmData;

        /// <inheritdoc />
        ArmPosData IDeformationData.RightArm => _rightArmData;

        /// <inheritdoc />
        bool IDeformationData.LeftArmDataInitialized => _leftArmData.IsInitialized;

        /// <inheritdoc />
        bool IDeformationData.RightArmDataInitialized => _rightArmData.IsInitialized;

        /// <inheritdoc />
        Vector3 IDeformationData.StartingScale => _startingScale;

        /// <inheritdoc />
        string IDeformationData.SpineCorrectionTypeIntProperty =>
            ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(_spineTranslationCorrectionType));

        /// <inheritdoc />
        string IDeformationData.SpineLowerAlignmentWeightFloatProperty =>
            ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(_spineLowerAlignmentWeight));

        /// <inheritdoc />
        string IDeformationData.SpineUpperAlignmentWeightFloatProperty =>
            ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(_spineUpperAlignmentWeight));

        /// <inheritdoc />
        string IDeformationData.ChestAlignmentWeightFloatProperty =>
            ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(_chestAlignmentWeight));

        /// <inheritdoc />
        string IDeformationData.LeftShoulderWeightFloatProperty =>
            ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(_leftShoulderWeight));

        /// <inheritdoc />
        string IDeformationData.RightShoulderWeightFloatProperty =>
            ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(_rightShoulderWeight));

        /// <inheritdoc />
        string IDeformationData.LeftArmWeightFloatProperty =>
            ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(_leftArmWeight));

        /// <inheritdoc />
        string IDeformationData.RightArmWeightFloatProperty =>
            ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(_rightArmWeight));

        /// <inheritdoc />
        string IDeformationData.LeftHandWeightFloatProperty =>
            ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(_leftHandWeight));

        /// <inheritdoc />
        string IDeformationData.RightHandWeightFloatProperty =>
            ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(_rightHandWeight));

        /// <inheritdoc />
        float IDeformationData.HipsToHeadDistance => _hipsToHeadDistance;

        /// <inheritdoc cref="IDeformationData.ConstraintCustomSkeleton"/>
        [NotKeyable, SerializeField, ConditionalHide("_animator", null)]
        [Tooltip(DeformationDataTooltips.CustomSkeleton)]
        private OVRCustomSkeleton _customSkeleton;

        /// <inheritdoc cref="IDeformationData.ConstraintAnimator"/>
        [NotKeyable, SerializeField, ConditionalHide("_customSkeleton", null)]
        [Tooltip(DeformationDataTooltips.Animator)]
        private Animator _animator;

        /// <inheritdoc cref="IDeformationData.SpineCorrectionType"/>
        [SyncSceneToStream, SerializeField, IntAsEnum(typeof(SpineTranslationCorrectionType))]
        [Tooltip(DeformationDataTooltips.SpineTranslationCorrectionType)]
        private int _spineTranslationCorrectionType;
        public SpineTranslationCorrectionType SpineTranslationCorrectionTypeField
        {
            get => (SpineTranslationCorrectionType)_spineTranslationCorrectionType;
            set => _spineTranslationCorrectionType = (int)value;
        }

        /// <summary>
        /// The weight for the spine lower alignment.
        /// </summary>
        [SyncSceneToStream, SerializeField, Range(0.0f, 1.0f)]
        [Tooltip(DeformationDataTooltips.SpineAlignmentWeight)]
        private float _spineLowerAlignmentWeight;
        public float SpineLowerAlignmentWeight
        {
            get => _spineLowerAlignmentWeight;
            set => _spineLowerAlignmentWeight = value;
        }

        /// <summary>
        /// The weight for the spine upper alignment.
        /// </summary>
        [SyncSceneToStream, SerializeField, Range(0.0f, 1.0f)]
        [Tooltip(DeformationDataTooltips.SpineAlignmentWeight)]
        private float _spineUpperAlignmentWeight;
        public float SpineUpperAlignmentWeight
        {
            get => _spineUpperAlignmentWeight;
            set => _spineUpperAlignmentWeight = value;
        }

        /// <summary>
        /// The weight for the chest alignment.
        /// </summary>
        [SyncSceneToStream, SerializeField, Range(0.0f, 1.0f)]
        [Tooltip(DeformationDataTooltips.SpineAlignmentWeight)]
        private float _chestAlignmentWeight;
        public float ChestAlignmentWeight
        {
            get => _chestAlignmentWeight;
            set => _chestAlignmentWeight = value;
        }

        /// <summary>
        /// The weight for the left shoulder deformation.
        /// </summary>
        [SyncSceneToStream, SerializeField, Range(0.0f, 1.0f)]
        [Tooltip(DeformationDataTooltips.LeftShoulderWeight)]
        private float _leftShoulderWeight;
        public float LeftShoulderWeight
        {
            get => _leftShoulderWeight;
            set => _leftShoulderWeight = value;
        }

        /// <summary>
        /// The weight for the right shoulder deformation.
        /// </summary>
        [SyncSceneToStream, SerializeField, Range(0.0f, 1.0f)]
        [Tooltip(DeformationDataTooltips.RightShoulderWeight)]
        private float _rightShoulderWeight;
        public float RightShoulderWeight
        {
            get => _rightShoulderWeight;
            set => _rightShoulderWeight = value;
        }

        /// <summary>
        /// The weight for the deformation on the left arm.
        /// </summary>
        [SyncSceneToStream, SerializeField, Range(0.0f, 1.0f)]
        [Tooltip(DeformationDataTooltips.LeftArmWeight)]
        private float _leftArmWeight;
        public float LeftArmWeight
        {
            get => _leftArmWeight;
            set => _leftArmWeight = value;
        }

        /// <summary>
        /// The weight for the deformation on the right arm.
        /// </summary>
        [SyncSceneToStream, SerializeField, Range(0.0f, 1.0f)]
        [Tooltip(DeformationDataTooltips.RightArmWeight)]
        private float _rightArmWeight;
        public float RightArmWeight
        {
            get => _rightArmWeight;
            set => _rightArmWeight = value;
        }

        /// <summary>
        /// The weight for the deformation on the left hand.
        /// </summary>
        [SyncSceneToStream, SerializeField, Range(0.0f, 1.0f)]
        [Tooltip(DeformationDataTooltips.LeftHandWeight)]
        private float _leftHandWeight;
        public float LeftHandWeight
        {
            get => _leftHandWeight;
            set => _leftHandWeight = value;
        }

        /// <summary>
        /// The weight for the deformation on the right hand.
        /// </summary>
        [SyncSceneToStream, SerializeField, Range(0.0f, 1.0f)]
        [Tooltip(DeformationDataTooltips.RightHandWeight)]
        private float _rightHandWeight;
        public float RightHandWeight
        {
            get => _rightHandWeight;
            set => _rightHandWeight = value;
        }

        /// <summary>
        /// Array of transform bones from hips to head.
        /// </summary>
        [SyncSceneToStream, SerializeField]
        [Tooltip(DeformationDataTooltips.HipsToHeadBones)]
        private Transform[] _hipsToHeadBones;

        /// <summary>
        /// Array of transform bone targets from hips to head.
        /// </summary>
        [SyncSceneToStream, SerializeField]
        [Tooltip(DeformationDataTooltips.HipsToHeadBoneTargets)]
        private Transform[] _hipsToHeadBoneTargets;

        /// <summary>
        /// Left arm data.
        /// </summary>
        [SyncSceneToStream, SerializeField]
        [Tooltip(DeformationDataTooltips.LeftArmData)]
        private ArmPosData _leftArmData;

        /// <summary>
        /// Right arm data.
        /// </summary>
        [SyncSceneToStream, SerializeField]
        [Tooltip(DeformationDataTooltips.RightArmData)]
        private ArmPosData _rightArmData;

        /// <summary>
        /// All bone pair data.
        /// </summary>
        [SerializeField]
        [Tooltip(DeformationDataTooltips.BonePairData)]
        private BonePairData[] _bonePairData;

        /// <summary>
        /// Starting scale of character.
        /// </summary>
        [NotKeyable, SerializeField]
        [Tooltip(DeformationDataTooltips.StartingScale)]
        private Vector3 _startingScale;

        /// <summary>
        /// Distances between head and hips.
        /// </summary>
        [NotKeyable, SerializeField]
        [Tooltip(DeformationDataTooltips.HipsToHeadDistance)]
        private float _hipsToHeadDistance;

        private bool _shouldUpdate;

        /// <summary>
        /// Assign the OVR Custom Skeleton.
        /// </summary>
        /// <param name="skeleton">The OVRCustomSkeleton component.</param>
        public void AssignOVRCustomSkeleton(OVRCustomSkeleton skeleton)
        {
            _customSkeleton = skeleton;
        }

        /// <summary>
        /// Assign the Animator.
        /// </summary>
        /// <param name="animator">The Animator component.</param>
        public void AssignAnimator(Animator animator)
        {
            _animator = animator;
        }

        /// <inheritdoc />
        public bool IsBoneTransformsDataValid()
        {
            return (_customSkeleton != null && _customSkeleton.IsDataValid) ||
                (_animator != null);
        }

        /// <inheritdoc />
        public void SetUpHipsToHeadBones()
        {
            var hipsToHeadBones = new List<Transform>();
            _hipsToHeadDistance = 0.0f;
            for (int boneId = (int)OVRSkeleton.BoneId.Body_Hips; boneId <= (int)OVRSkeleton.BoneId.Body_Head;
                 boneId++)
            {
                var foundBoneTransform = FindBoneTransform((OVRSkeleton.BoneId)boneId);
                if (foundBoneTransform == null)
                {
                    continue;
                }
                hipsToHeadBones.Add(foundBoneTransform.transform);
                if (hipsToHeadBones.Count > 1)
                {
                    _hipsToHeadDistance +=
                        Vector3.Distance(hipsToHeadBones[^1].position, hipsToHeadBones[^2].position);
                }
            }
            _hipsToHeadBones = hipsToHeadBones.ToArray();
        }

        /// <inheritdoc />
        public void SetUpHipsToHeadBoneTargets(Transform setupParent)
        {
            var hipsTarget = setupParent.FindChildRecursive("Hips");
            var spineLowerTarget = setupParent.FindChildRecursive("SpineLower");
            var spineUpperTarget = setupParent.FindChildRecursive("SpineUpper");
            var chestTarget = setupParent.FindChildRecursive("Chest");
            var neckTarget = setupParent.FindChildRecursive("Neck");
            var headTarget = setupParent.FindChildRecursive("Head");

            var hipsToHeadBoneTargets = new List<Transform>
            {
                hipsTarget, spineLowerTarget
            };

            // Add optional bones, based on the length of the original array.
            if (_hipsToHeadBones.Length > 4)
            {
                hipsToHeadBoneTargets.Add(spineUpperTarget);
            }
            if (_hipsToHeadBones.Length > 5)
            {
                hipsToHeadBoneTargets.Add(chestTarget);
            }
            hipsToHeadBoneTargets.Add(neckTarget);
            hipsToHeadBoneTargets.Add(headTarget);

            _hipsToHeadBoneTargets = hipsToHeadBoneTargets.ToArray();
        }

        /// <inheritdoc />
        public void SetUpLeftArmData()
        {
            var shoulder = FindBoneTransform(OVRSkeleton.BoneId.Body_LeftShoulder);
            var upperArmBone = FindBoneTransform(OVRSkeleton.BoneId.Body_LeftArmUpper);
            var lowerArmBone = FindBoneTransform(OVRSkeleton.BoneId.Body_LeftArmLower);
            var handBone = FindBoneTransform(OVRSkeleton.BoneId.Body_LeftHandWrist);
            _leftArmData = new ArmPosData()
            {
                ShoulderBone = shoulder,
                UpperArmBone = upperArmBone,
                LowerArmBone = lowerArmBone,
                HandBone = handBone,
                ShoulderLocalPos = shoulder != null ? shoulder.localPosition : Vector3.zero,
                LowerArmToHandAxis =
                    lowerArmBone.InverseTransformDirection(handBone.position - lowerArmBone.position).normalized
            };
        }

        /// <inheritdoc />
        public void SetUpRightArmData()
        {
            var shoulder = FindBoneTransform(OVRSkeleton.BoneId.Body_RightShoulder);
            var upperArmBone = FindBoneTransform(OVRSkeleton.BoneId.Body_RightArmUpper);
            var lowerArmBone = FindBoneTransform(OVRSkeleton.BoneId.Body_RightArmLower);
            var handBone = FindBoneTransform(OVRSkeleton.BoneId.Body_RightHandWrist);
            _rightArmData = new ArmPosData()
            {
                ShoulderBone = shoulder,
                UpperArmBone = upperArmBone,
                LowerArmBone = lowerArmBone,
                HandBone = handBone,
                ShoulderLocalPos = shoulder != null ? shoulder.localPosition : Vector3.zero,
                LowerArmToHandAxis =
                    lowerArmBone.InverseTransformDirection(handBone.position - lowerArmBone.position).normalized
            };
        }

        /// <inheritdoc />
        public void SetUpBonePairs()
        {
            if (_hipsToHeadBones == null)
            {
                Debug.LogError("Please set up hips to head bones before trying to " +
                    "set up bone pairs");
                return;
            }
            if (!_leftArmData.IsInitialized)
            {
                Debug.LogError("Please set up left arm data before trying to " +
                               "set up bone pairs");
                return;
            }
            if (!_rightArmData.IsInitialized)
            {
                Debug.LogError("Please set up right arm data before trying to " +
                               "set up bone pairs");
                return;
            }

            // Setup bone pairs for hips to head bones.
            var bonePairs = new List<BonePairData>();
            for (int i = 0; i < _hipsToHeadBones.Length - 1; i++)
            {
                var bonePair = new BonePairData
                {
                    StartBone = _hipsToHeadBones[i],
                    EndBone = _hipsToHeadBones[i + 1]
                };
                bonePairs.Add(bonePair);
            }

            // Check for optional bones and update accordingly.
            var chestBone = FindBoneTransform(OVRSkeleton.BoneId.Body_Chest);
            var spineUpperBone = FindBoneTransform(OVRSkeleton.BoneId.Body_SpineUpper);
            var spineLowerBone = FindBoneTransform(OVRSkeleton.BoneId.Body_SpineLower);
            var highestSpineBone = chestBone;
            if (chestBone == null)
            {
                Debug.LogWarning($"Did not find the {HumanBodyBones.UpperChest} bone in {_animator}. The deformation job result will be affected.");
                highestSpineBone = spineUpperBone;
            }
            if (spineUpperBone == null)
            {
                Debug.LogWarning($"Did not find the {HumanBodyBones.Chest} bone in {_animator}. The deformation job result will be affected.");
                highestSpineBone = spineLowerBone;
            }
            var leftShoulderBone = _leftArmData.ShoulderBone;
            var rightShoulderBone = _rightArmData.ShoulderBone;

            // Chest to shoulder bones.
            if (leftShoulderBone != null)
            {
                bonePairs.Add(new BonePairData
                {
                    StartBone = highestSpineBone,
                    EndBone = leftShoulderBone,
                });
            }
            else
            {
                Debug.LogWarning($"Did not find the {HumanBodyBones.LeftShoulder} bone in {_animator}. The deformation job result will be affected.");
            }

            if (rightShoulderBone != null)
            {
                bonePairs.Add(new BonePairData
                {
                    StartBone = highestSpineBone,
                    EndBone = rightShoulderBone,
                });
            }
            else
            {
                Debug.LogWarning($"Did not find the {HumanBodyBones.RightShoulder} bone in {_animator}. The deformation job result will be affected.");
            }

            // Shoulder to upper arm bones.
            bonePairs.Add(new BonePairData
            {
                StartBone = leftShoulderBone != null ? leftShoulderBone : highestSpineBone,
                EndBone = _leftArmData.UpperArmBone
            });
            bonePairs.Add(new BonePairData
            {
                StartBone = rightShoulderBone != null ? rightShoulderBone : highestSpineBone,
                EndBone = _rightArmData.UpperArmBone
            });

            // Upper arm to lower arm bones.
            bonePairs.Add(new BonePairData
            {
                StartBone = _leftArmData.UpperArmBone,
                EndBone = _leftArmData.LowerArmBone
            });
            bonePairs.Add(new BonePairData
            {
                StartBone = _rightArmData.UpperArmBone,
                EndBone = _rightArmData.LowerArmBone
            });

            // Lower arm to hand bones.
            bonePairs.Add(new BonePairData
            {
                StartBone = _leftArmData.LowerArmBone,
                EndBone = _leftArmData.HandBone
            });
            bonePairs.Add(new BonePairData
            {
                StartBone = _rightArmData.LowerArmBone,
                EndBone = _rightArmData.HandBone
            });

            // Calculate original bone pair lengths.
            for (int i = 0; i < bonePairs.Count; i++)
            {
                var bonePair = bonePairs[i];
                if (bonePair.StartBone != null && bonePair.EndBone != null)
                {
                    bonePair.Distance = Vector3.Distance(bonePair.EndBone.position, bonePair.StartBone.position);
                }
                else
                {
                    Debug.LogWarning($"Missing bones in bone pair! Start Bone: {bonePair.StartBone}, End Bone: {bonePair.EndBone}");
                }
                bonePairs[i] = bonePair;
            }

            // Calculate proportions for the upper body.
            for (int i = 0; i < _hipsToHeadBones.Length + 1; i++)
            {
                var bonePair = bonePairs[i];
                bonePair.HeightProportion = bonePair.Distance / _hipsToHeadDistance;
                bonePair.LimbProportion = bonePair.Distance / _hipsToHeadDistance;
                bonePairs[i] = bonePair;
            }

            _bonePairData = bonePairs.ToArray();
        }

        /// <inheritdoc />
        public void InitializeStartingScale()
        {
            if (_animator != null)
            {
                _startingScale = _animator.transform.lossyScale;
            }
            else if (_customSkeleton != null)
            {
                _startingScale = _customSkeleton.transform.lossyScale;
            }
            else
            {
                Debug.LogError("Both animator and custom skeleton are not; " +
                    "could not compute starting scale.");
            }
        }

        /// <inheritdoc />
        public void ClearTransformData()
        {
            _bonePairData = null;
            _hipsToHeadBones = null;
            _hipsToHeadBoneTargets = null;
            _leftArmData.ClearTransformData();
            _rightArmData.ClearTransformData();
        }

        /// <summary>
        /// Find the transform for a specific boneId.
        /// </summary>
        /// <param name="boneId">The boneId to be found.</param>
        /// <returns>The bone transform.</returns>
        private Transform FindBoneTransform(OVRSkeleton.BoneId boneId)
        {
            if (_customSkeleton != null)
            {
                return RiggingUtilities.FindBoneTransformFromCustomSkeleton(_customSkeleton, boneId);
            }

            if (_animator != null)
            {
                return RiggingUtilities.FindBoneTransformAnimator(_animator, boneId, false);
            }

            return null;
        }

        bool IAnimationJobData.IsValid()
        {
            if (_animator == null && _customSkeleton == null)
            {
                Debug.LogError("Animator or skeleton not set up.");
                return false;
            }

            if (_bonePairData == null || _bonePairData.Length == 0)
            {
                Debug.LogError("Bone pair data not set up.");
                return false;
            }

            if (_hipsToHeadBones == null || _hipsToHeadBones.Length == 0 || _hipsToHeadBoneTargets.Length == 0)
            {
                Debug.LogError("Hips to head bones not set up.");
                return false;
            }

            if (!_leftArmData.IsInitialized || !_rightArmData.IsInitialized)
            {
                Debug.LogError("Arm data not set up.");
                return false;
            }

            return true;
        }

        void IAnimationJobData.SetDefaultValues()
        {
            _animator = null;
            _customSkeleton = null;
            _spineTranslationCorrectionType = (int)SpineTranslationCorrectionType.None;

            _spineLowerAlignmentWeight = 0.0f;
            _spineUpperAlignmentWeight = 0.0f;
            _chestAlignmentWeight = 0.0f;
            _leftShoulderWeight = 0.0f;
            _rightShoulderWeight = 0.0f;
            _leftArmWeight = 0.0f;
            _rightArmWeight = 0.0f;
            _leftHandWeight = 0.0f;
            _rightHandWeight = 0.0f;

            _hipsToHeadBones = null;
            _hipsToHeadBoneTargets = null;
            _leftArmData = new ArmPosData();
            _rightArmData = new ArmPosData();
            _bonePairData = null;

            _startingScale = Vector3.one;
            _hipsToHeadDistance = 0.0f;
        }
    }

    /// <summary>
    /// Deformation constraint.
    /// </summary>
    [DisallowMultipleComponent, AddComponentMenu("Movement Animation Rigging/Deformation Constraint")]
    public class DeformationConstraint : RigConstraint<
        DeformationJob,
        DeformationData,
        DeformationJobBinder<DeformationData>>,
        IOVRSkeletonConstraint
    {
        /// <inheritdoc />
        public void RegenerateData()
        {
        }
    }
}
