// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Movement.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using UnityEngine.Serialization;
using static Oculus.Movement.AnimationRigging.DeformationUtilities;
using static OVRUnityHumanoidSkeletonRetargeter;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// Information about the positioning of a leg.
    /// </summary>
    [Serializable]
    public struct LegPosData
    {
        /// <summary>
        /// The hips transform.
        /// </summary>
        public Transform HipsBone;

        /// <summary>
        /// The upper leg transform.
        /// </summary>
        public Transform UpperLegBone;

        /// <summary>
        /// The lower leg transform.
        /// </summary>
        public Transform LowerLegBone;

        /// <summary>
        /// The foot transform.
        /// </summary>
        public Transform FootBone;

        /// <summary>
        /// The toes transform.
        /// </summary>
        public Transform ToesBone;

        /// <summary>
        /// The local position of the toes.
        /// </summary>
        public Vector3 ToesLocalPos;

        /// <summary>
        /// The local rotation of the foot.
        /// </summary>
        public Quaternion FootLocalRot;

        /// <summary>
        /// Indicates if initialized or not.
        /// </summary>
        /// <returns></returns>
        public bool IsInitialized =>
            HipsBone != null &&
            UpperLegBone != null &&
            LowerLegBone != null &&
            FootBone != null;

        /// <summary>
        /// Resets all tracked transforms to null.
        /// </summary>
        public void ClearTransformData()
        {
            HipsBone = null;
            UpperLegBone = null;
            LowerLegBone = null;
            FootBone = null;
            ToesBone = null;
        }
    }

    /// <summary>
    /// Interface for FullBodyDeformation data.
    /// </summary>
    public interface IFullBodyDeformationData
    {
        /// <summary>
        /// The deformation body type for the character.
        /// </summary>
        public int BodyType { get; }

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
        /// The array of transform targets from the feet to the toes bones.
        /// </summary>
        public Transform[] FeetToToesBoneTargets { get; }

        /// <summary>
        /// The position info for the bone pairs used for FullBodyDeformation.
        /// </summary>
        public BonePairData[] BonePairs { get; }

        /// <summary>
        /// The adjustment info for the bones.
        /// </summary>
        public BoneAdjustmentData[] BoneAdjustments { get; }

        /// <summary>
        /// The position info for the left arm.
        /// </summary>
        public ArmPosData LeftArm { get; }

        /// <summary>
        /// The position info for the right arm.
        /// </summary>
        public ArmPosData RightArm { get; }

        /// <summary>
        /// The position info for the left leg.
        /// </summary>
        public LegPosData LeftLeg { get; }

        /// <summary>
        /// The position info for the right leg.
        /// </summary>
        public LegPosData RightLeg { get; }

        /// <summary>
        /// The type of spine translation correction that should be applied.
        /// </summary>
        public int SpineCorrectionType { get; }

        /// <summary>
        /// The starting scale of the character, taken from the animator transform.
        /// </summary>
        public Vector3 StartingScale { get; }

        /// <summary>
        /// The deformation body type int property.
        /// </summary>
        public string DeformationBodyTypeIntProperty { get; }

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
        /// The shoulders height reduction weight float property.
        /// </summary>
        public string ShouldersHeightReductionWeightFloatProperty { get; }

        /// <summary>
        /// The shoulders width reduction weight float property.
        /// </summary>
        public string ShouldersWidthReductionWeightFloatProperty { get; }

        /// <summary>
        /// Affect arms by spine correction bool property.
        /// </summary>
        public string AffectArmsBySpineCorrection { get; }

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
        /// Restricts how much the character should be squashed.
        /// WARNING: restricting too much will prevent the character
        /// from tracking the body accurately.
        /// </summary>
        public string SquashLimitFloatProperty { get; }

        /// <summary>
        /// Restricts how much the character should be stretched.
        /// WARNING: restricting too much will prevent the character
        /// from tracking the body accurately.
        /// </summary>
        public string StretchLimitFloatProperty { get; }

        /// <summary>
        /// The left leg weight float property.
        /// </summary>
        public string LeftLegWeightFloatProperty { get; }

        /// <summary>
        /// The right leg weight float property.
        /// </summary>
        public string RightLegWeightFloatProperty { get; }

        /// <summary>
        /// The left toes weight float property.
        /// </summary>
        public string LeftToesWeightFloatProperty { get; }

        /// <summary>
        /// The right toes weight float property.
        /// </summary>
        public string RightToesWeightFloatProperty { get; }

        /// <summary>
        /// Align feet weight float property.
        /// </summary>
        public string AlignFeetWeightFloatProperty { get; }

        /// <summary>
        /// The distance between the hips and head bones.
        /// </summary>
        public float HipsToHeadDistance { get; }

        /// <summary>
        /// The distance between the hips and foot bones.
        /// </summary>
        public float HipsToFootDistance { get; }

        /// <summary>
        /// Sets up hips and head bones.
        /// </summary>
        public void SetUpHipsAndHeadBones();

        /// <summary>
        /// Sets up left arm data.
        /// </summary>
        public void SetUpLeftArmData();

        /// <summary>
        /// Sets up right arm data.
        /// </summary>
        public void SetUpRightArmData();

        /// <summary>
        /// Sets up left leg data.
        /// </summary>
        public void SetUpLeftLegData();

        /// <summary>
        /// Sets up right leg data.
        /// </summary>
        public void SetUpRightLegData();

        /// <summary>
        /// Sets up upper bone parts after all bones have been found.
        /// </summary>
        public void SetUpBonePairs();

        /// <summary>
        /// Try to set up bone targets.
        /// </summary>
        /// <param name="setupParent">The parent for the bone targets.</param>
        public void SetUpBoneTargets(Transform setupParent);

        /// <summary>
        /// Try to set up adjustments.
        /// </summary>
        /// <param name="restPoseObject">The rest pose object used to calculate adjustments.</param>
        public void SetUpAdjustments(RestPoseObjectHumanoid restPoseObject);

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
    /// FullBodyDeformation data used by the FullBodyDeformation job.
    /// Implements the FullBodyDeformation data interface.
    /// </summary>
    [Serializable]
    public struct FullBodyDeformationData : IAnimationJobData, IFullBodyDeformationData
    {
        /// <summary>
        /// The deformation body type.
        /// </summary>
        public enum DeformationBodyType
        {
            /// <summary>The body type used for deformation is the full body.</summary>
            FullBody,
            /// <summary>The body type used for deformation is the upper body.</summary>
            UpperBody
        }

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
        int IFullBodyDeformationData.BodyType => _deformationBodyType;

        /// <inheritdoc />
        OVRCustomSkeleton IFullBodyDeformationData.ConstraintCustomSkeleton => _customSkeleton;

        /// <inheritdoc />
        Animator IFullBodyDeformationData.ConstraintAnimator => _animator;

        /// <inheritdoc />
        bool IFullBodyDeformationData.ShouldUpdate
        {
            get => _shouldUpdate;
            set => _shouldUpdate = value;
        }

        /// <inheritdoc />
        int IFullBodyDeformationData.SpineCorrectionType => _spineTranslationCorrectionType;

        /// <inheritdoc />
        Transform[] IFullBodyDeformationData.HipsToHeadBones => _hipsToHeadBones;

        /// <inheritdoc />
        Transform[] IFullBodyDeformationData.HipsToHeadBoneTargets => _hipsToHeadBoneTargets;

        /// <inheritdoc />
        Transform[] IFullBodyDeformationData.FeetToToesBoneTargets => _feetToToesBoneTargets;

        /// <inheritdoc />
        BonePairData[] IFullBodyDeformationData.BonePairs => _bonePairData;

        /// <inheritdoc />
        BoneAdjustmentData[] IFullBodyDeformationData.BoneAdjustments => _boneAdjustmentData;

        /// <inheritdoc />
        ArmPosData IFullBodyDeformationData.LeftArm => _leftArmData;

        /// <inheritdoc />
        ArmPosData IFullBodyDeformationData.RightArm => _rightArmData;

        /// <inheritdoc />
        LegPosData IFullBodyDeformationData.LeftLeg => _leftLegData;

        /// <inheritdoc />
        LegPosData IFullBodyDeformationData.RightLeg => _rightLegData;

        /// <inheritdoc />
        Vector3 IFullBodyDeformationData.StartingScale => _startingScale;

        /// <inheritdoc />
        string IFullBodyDeformationData.DeformationBodyTypeIntProperty =>
            ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(_deformationBodyType));

        /// <inheritdoc />
        string IFullBodyDeformationData.SpineCorrectionTypeIntProperty =>
            ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(_spineTranslationCorrectionType));

        /// <inheritdoc />
        string IFullBodyDeformationData.SpineLowerAlignmentWeightFloatProperty =>
            ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(_spineLowerAlignmentWeight));

        /// <inheritdoc />
        string IFullBodyDeformationData.SpineUpperAlignmentWeightFloatProperty =>
            ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(_spineUpperAlignmentWeight));

        /// <inheritdoc />
        string IFullBodyDeformationData.ChestAlignmentWeightFloatProperty =>
            ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(_chestAlignmentWeight));

        /// <inheritdoc />
        string IFullBodyDeformationData.ShouldersHeightReductionWeightFloatProperty =>
            ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(_shouldersHeightReductionWeight));

        /// <inheritdoc />
        string IFullBodyDeformationData.ShouldersWidthReductionWeightFloatProperty =>
            ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(_shouldersWidthReductionWeight));

        /// <inheritdoc />
        string IFullBodyDeformationData.AffectArmsBySpineCorrection =>
            ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(_affectArmsBySpineCorrection));

        /// <inheritdoc />
        string IFullBodyDeformationData.LeftShoulderWeightFloatProperty =>
            ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(_leftShoulderWeight));

        /// <inheritdoc />
        string IFullBodyDeformationData.RightShoulderWeightFloatProperty =>
            ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(_rightShoulderWeight));

        /// <inheritdoc />
        string IFullBodyDeformationData.LeftArmWeightFloatProperty =>
            ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(_leftArmWeight));

        /// <inheritdoc />
        string IFullBodyDeformationData.RightArmWeightFloatProperty =>
            ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(_rightArmWeight));

        /// <inheritdoc />
        string IFullBodyDeformationData.LeftHandWeightFloatProperty =>
            ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(_leftHandWeight));

        /// <inheritdoc />
        string IFullBodyDeformationData.RightHandWeightFloatProperty =>
            ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(_rightHandWeight));

        /// <inheritdoc />
        string IFullBodyDeformationData.SquashLimitFloatProperty =>
            ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(_squashLimit));

        /// <inheritdoc />
        string IFullBodyDeformationData.StretchLimitFloatProperty =>
            ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(_stretchLimit));

        /// <inheritdoc />
        string IFullBodyDeformationData.LeftLegWeightFloatProperty =>
            ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(_alignLeftLegWeight));

        /// <inheritdoc />
        string IFullBodyDeformationData.RightLegWeightFloatProperty =>
            ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(_alignRightLegWeight));

        /// <inheritdoc />
        string IFullBodyDeformationData.LeftToesWeightFloatProperty =>
            ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(_leftToesWeight));

        /// <inheritdoc />
        string IFullBodyDeformationData.RightToesWeightFloatProperty =>
            ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(_rightToesWeight));

        string IFullBodyDeformationData.AlignFeetWeightFloatProperty =>
            ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(_alignFeetWeight));

        /// <inheritdoc />
        float IFullBodyDeformationData.HipsToHeadDistance => _hipsToHeadDistance;

        /// <inheritdoc />
        float IFullBodyDeformationData.HipsToFootDistance => _hipsToFootDistance;

        /// <inheritdoc cref="IFullBodyDeformationData.BodyType"/>
        [SyncSceneToStream, SerializeField, IntAsEnumAttribute(typeof(DeformationBodyType))]
        [Tooltip(DeformationDataTooltips.DeformationBodyType)]
        private int _deformationBodyType;
        public DeformationBodyType DeformationBodyTypeField
        {
            get => (DeformationBodyType)_deformationBodyType;
            set => _deformationBodyType = (int)value;
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
        [Tooltip(DeformationDataTooltips.ChestAlignmentWeight)]
        private float _chestAlignmentWeight;
        public float ChestAlignmentWeight
        {
            get => _chestAlignmentWeight;
            set => _chestAlignmentWeight = value;
        }

        /// <summary>
        /// The weight for shoulders height reduction.
        /// </summary>
        [SyncSceneToStream, SerializeField, Range(0.0f, 1.0f)]
        [Tooltip(DeformationDataTooltips.ShouldersHeightReductionWeight)]
        private float _shouldersHeightReductionWeight;
        public float ShouldersHeightReductionWeight
        {
            get => _shouldersHeightReductionWeight;
            set => _shouldersHeightReductionWeight = value;
        }

        /// <summary>
        /// The weight for shoulders width reduction.
        /// </summary>
        [SyncSceneToStream, SerializeField, Range(0.0f, 1.0f)]
        [Tooltip(DeformationDataTooltips.ShouldersWidthReductionWeight)]
        private float _shouldersWidthReductionWeight;
        public float ShouldersWidthReductionWeight
        {
            get => _shouldersWidthReductionWeight;
            set => _shouldersWidthReductionWeight = value;
        }

        /// <summary>
        /// True if arms should be affected by spine correction.
        /// </summary>
        [SyncSceneToStream, SerializeField]
        [Tooltip(DeformationDataTooltips.AffectArmsBySpineCorrection)]
        private bool _affectArmsBySpineCorrection;
        public bool AffectArmsBySpineCorrection
        {
            get => _affectArmsBySpineCorrection;
            set => _affectArmsBySpineCorrection = value;
        }

        /// <summary>
        /// The weight for the deformation on the left shoulder.
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
        /// The weight for the deformation on the right shoulder.
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
        /// Restricts how much the character should be squashed.
        /// WARNING: restricting too much will prevent the character
        /// from tracking the body accurately.
        /// </summary>
        [SyncSceneToStream, SerializeField, Range(float.Epsilon, 2.0f)]
        [Tooltip(DeformationDataTooltips.SquashLimit)]
        private float _squashLimit;
        public float SquashLimit
        {
            get => _squashLimit;
            set => _squashLimit = value;
        }

        /// <summary>
        /// Restricts how much the character should be stretched.
        /// WARNING: restricting too much will prevent the character
        /// from tracking the body accurately.
        /// </summary>
        [SyncSceneToStream, SerializeField, Range(float.Epsilon, 2.0f)]
        [Tooltip(DeformationDataTooltips.StretchLimit)]
        private float _stretchLimit;
        public float StretchLimit
        {
            get => _stretchLimit;
            set => _stretchLimit = value;
        }

        /// <summary>
        /// The weight for the alignment on the left leg.
        /// </summary>
        [FormerlySerializedAs("_leftLegWeight")]
        [SyncSceneToStream, SerializeField, Range(0.0f, 1.0f)]
        [Tooltip(DeformationDataTooltips.AlignLeftLegWeight)]
        private float _alignLeftLegWeight;
        public float AlignLeftLegWeight
        {
            get => _alignLeftLegWeight;
            set => _alignLeftLegWeight = value;
        }

        /// <summary>
        /// The weight for the alignment on the right leg.
        /// </summary>
        [FormerlySerializedAs("_rightLegWeight")]
        [SyncSceneToStream, SerializeField, Range(0.0f, 1.0f)]
        [Tooltip(DeformationDataTooltips.AlignRightLegWeight)]
        private float _alignRightLegWeight;
        public float AlignRightLegWeight
        {
            get => _alignRightLegWeight;
            set => _alignRightLegWeight = value;
        }

        /// <summary>
        /// The weight for the deformation on the left toe.
        /// </summary>
        [SyncSceneToStream, SerializeField, Range(0.0f, 1.0f)]
        [Tooltip(DeformationDataTooltips.LeftToesWeight)]
        private float _leftToesWeight;
        public float LeftToesWeight
        {
            get => _leftToesWeight;
            set => _leftToesWeight = value;
        }

        /// <summary>
        /// The weight for the deformation on the right toe.
        /// </summary>
        [SyncSceneToStream, SerializeField, Range(0.0f, 1.0f)]
        [Tooltip(DeformationDataTooltips.RightToesWeight)]
        private float _rightToesWeight;
        public float RightToesWeight
        {
            get => _rightToesWeight;
            set => _rightToesWeight = value;
        }

        /// <summary>
        /// Weight used for feet alignment.
        /// </summary>
        [SyncSceneToStream, SerializeField, Range(0.0f, 1.0f)]
        [Tooltip(DeformationDataTooltips.AlignFeetWeight)]
        private float _alignFeetWeight;
        public float AlignFeetWeight
        {
            get => _alignFeetWeight;
            set => _alignFeetWeight = value;
        }

        /// <inheritdoc cref="IFullBodyDeformationData.SpineCorrectionType"/>
        [SyncSceneToStream, SerializeField, IntAsEnumAttribute(typeof(SpineTranslationCorrectionType))]
        [Tooltip(DeformationDataTooltips.SpineTranslationCorrectionType)]
        private int _spineTranslationCorrectionType;
        public SpineTranslationCorrectionType SpineTranslationCorrectionTypeField
        {
            get => (SpineTranslationCorrectionType)_spineTranslationCorrectionType;
            set => _spineTranslationCorrectionType = (int)value;
        }

        /// <inheritdoc cref="IFullBodyDeformationData.ConstraintCustomSkeleton"/>
        [NotKeyable, SerializeField]
        [Tooltip(DeformationDataTooltips.CustomSkeleton)]
        private OVRCustomSkeleton _customSkeleton;

        /// <inheritdoc cref="IFullBodyDeformationData.ConstraintAnimator"/>
        [NotKeyable, SerializeField]
        [Tooltip(DeformationDataTooltips.Animator)]
        private Animator _animator;

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
        /// Array of transform bone targets from feet to toes.
        /// </summary>
        [SyncSceneToStream, SerializeField]
        [Tooltip(DeformationDataTooltips.FeetToToesBoneTargets)]
        private Transform[] _feetToToesBoneTargets;

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
        /// Left leg data.
        /// </summary>
        [SyncSceneToStream, SerializeField]
        [Tooltip(DeformationDataTooltips.LeftLegData)]
        private LegPosData _leftLegData;

        /// <summary>
        /// Right leg data.
        /// </summary>
        [SyncSceneToStream, SerializeField]
        [Tooltip(DeformationDataTooltips.RightLegData)]
        private LegPosData _rightLegData;

        /// <summary>
        /// All bone pair data.
        /// </summary>
        [SerializeField]
        [Tooltip(DeformationDataTooltips.BonePairData)]
        private BonePairData[] _bonePairData;

        /// <summary>
        /// All bone adjustment data.
        /// </summary>
        [SerializeField]
        [Tooltip(DeformationDataTooltips.BoneAdjustmentData)]
        private BoneAdjustmentData[] _boneAdjustmentData;

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

        /// <summary>
        /// Distances between hips and feet.
        /// </summary>
        [NotKeyable, SerializeField]
        [Tooltip(DeformationDataTooltips.HipsToFootDistance)]
        private float _hipsToFootDistance;

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
        public void SetUpHipsAndHeadBones()
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

            var avgUpperLegBonePos = (_leftLegData.UpperLegBone.position + _rightLegData.UpperLegBone.position) / 2f;
            var avgLowerLegBonePos = (_leftLegData.LowerLegBone.position + _rightLegData.LowerLegBone.position) / 2f;
            var avgFootBonePos = (_leftLegData.FootBone.position + _rightLegData.FootBone.position) / 2f;
            _hipsToFootDistance = Vector3.Distance(hipsToHeadBones[0].position, avgUpperLegBonePos) +
                                  Vector3.Distance(avgUpperLegBonePos, avgLowerLegBonePos) +
                                  Vector3.Distance(avgLowerLegBonePos, avgFootBonePos);
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
                ShoulderLocalPos = shoulder != null ? shoulder.localPosition : upperArmBone.localPosition,
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
                ShoulderLocalPos = shoulder != null ? shoulder.localPosition : upperArmBone.localPosition,
                LowerArmToHandAxis =
                    lowerArmBone.InverseTransformDirection(handBone.position - lowerArmBone.position).normalized
            };
        }

        /// <inheritdoc />
        public void SetUpLeftLegData()
        {
            var toes = FindBoneTransform(OVRSkeleton.BoneId.FullBody_LeftFootBall);
            var foot = FindBoneTransform(OVRSkeleton.BoneId.FullBody_LeftFootAnkle);

            _leftLegData = new LegPosData
            {
                HipsBone = FindBoneTransform(OVRSkeleton.BoneId.FullBody_Hips),
                UpperLegBone = FindBoneTransform(OVRSkeleton.BoneId.FullBody_LeftUpperLeg),
                LowerLegBone = FindBoneTransform(OVRSkeleton.BoneId.FullBody_LeftLowerLeg),
                FootBone = foot,
                ToesBone = toes,
                ToesLocalPos = toes != null ? toes.localPosition : Vector3.zero,
                FootLocalRot = foot.localRotation
            };
        }

        /// <inheritdoc />
        public void SetUpRightLegData()
        {
            var toes = FindBoneTransform(OVRSkeleton.BoneId.FullBody_RightFootBall);
            var foot = FindBoneTransform(OVRSkeleton.BoneId.FullBody_RightFootAnkle);

            _rightLegData = new LegPosData
            {
                HipsBone = FindBoneTransform(OVRSkeleton.BoneId.FullBody_Hips),
                UpperLegBone = FindBoneTransform(OVRSkeleton.BoneId.FullBody_RightUpperLeg),
                LowerLegBone = FindBoneTransform(OVRSkeleton.BoneId.FullBody_RightLowerLeg),
                FootBone = foot,
                ToesBone = toes,
                ToesLocalPos = toes != null ? toes.localPosition : Vector3.zero,
                FootLocalRot = foot.localRotation
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
            if (!_leftLegData.IsInitialized)
            {
                Debug.LogError("Please set up left leg data before trying to " +
                               "set up bone pairs");
                return;
            }
            if (!_rightLegData.IsInitialized)
            {
                Debug.LogError("Please set up right leg data before trying to " +
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
            var chestBone = FindBoneTransform(OVRSkeleton.BoneId.FullBody_Chest);
            var spineUpperBone = FindBoneTransform(OVRSkeleton.BoneId.FullBody_SpineUpper);
            var spineLowerBone = FindBoneTransform(OVRSkeleton.BoneId.FullBody_SpineLower);
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

            // Hips to upper leg bones.
            var upperLegIndex = bonePairs.Count;
            bonePairs.Add(new BonePairData
            {
                StartBone = _leftLegData.HipsBone,
                EndBone = _leftLegData.UpperLegBone
            });
            bonePairs.Add(new BonePairData
            {
                StartBone = _rightLegData.HipsBone,
                EndBone = _rightLegData.UpperLegBone
            });

            // Upper leg to lower leg bones.
            bonePairs.Add(new BonePairData
            {
                StartBone = _leftLegData.UpperLegBone,
                EndBone = _leftLegData.LowerLegBone
            });
            bonePairs.Add(new BonePairData
            {
                StartBone = _rightLegData.UpperLegBone,
                EndBone = _rightLegData.LowerLegBone
            });

            // Lower leg to feet bones.
            bonePairs.Add(new BonePairData
            {
                StartBone = _leftLegData.LowerLegBone,
                EndBone = _leftLegData.FootBone
            });
            bonePairs.Add(new BonePairData
            {
                StartBone = _rightLegData.LowerLegBone,
                EndBone = _rightLegData.FootBone
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
                bonePair.HeightProportion = bonePair.Distance / (_hipsToHeadDistance + _hipsToFootDistance);
                bonePair.LimbProportion = bonePair.Distance / _hipsToHeadDistance;
                bonePairs[i] = bonePair;
            }

            // Calculate proportions for the lower body.
            for (int i = upperLegIndex; i < bonePairs.Count; i++)
            {
                var bonePair = bonePairs[i];
                bonePair.HeightProportion = bonePair.Distance / (_hipsToHeadDistance + _hipsToFootDistance);
                bonePair.LimbProportion = bonePair.Distance / _hipsToFootDistance;
                bonePairs[i] = bonePair;
            }

            _bonePairData = bonePairs.ToArray();
        }

        /// <inheritdoc />
        public void SetUpBoneTargets(Transform setupParent)
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

            // Add optional bones, based on the valid animator.
            if (_animator.GetBoneTransform(HumanBodyBones.Chest) != null)
            {
                hipsToHeadBoneTargets.Add(spineUpperTarget);
            }
            if (_animator.GetBoneTransform(HumanBodyBones.UpperChest) != null)
            {
                hipsToHeadBoneTargets.Add(chestTarget);
            }
            if (_animator.GetBoneTransform(HumanBodyBones.Neck) != null)
            {
                hipsToHeadBoneTargets.Add(neckTarget);
            }
            hipsToHeadBoneTargets.Add(headTarget);

            _hipsToHeadBoneTargets = hipsToHeadBoneTargets.ToArray();
            if (DeformationBodyTypeField == DeformationBodyType.FullBody)
            {
                _feetToToesBoneTargets = new[]
                {
                    setupParent.FindChildRecursive("LeftFoot"),
                    setupParent.FindChildRecursive("LeftToes"),
                    setupParent.FindChildRecursive("RightFoot"),
                    setupParent.FindChildRecursive("RightToes")
                };
            }
            else
            {
                _feetToToesBoneTargets = Array.Empty<Transform>();
            }
        }

        /// <inheritdoc />
        public void SetUpAdjustments(RestPoseObjectHumanoid restPoseObject)
        {
            _boneAdjustmentData = GetDeformationBoneAdjustments(_animator, restPoseObject);
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
            _leftArmData.ClearTransformData();
            _rightArmData.ClearTransformData();
            _leftLegData.ClearTransformData();
            _rightArmData.ClearTransformData();
        }

        private Transform FindBoneTransform(OVRSkeleton.BoneId boneId)
        {
            if (_customSkeleton != null)
            {
                return RiggingUtilities.FindBoneTransformFromCustomSkeleton(_customSkeleton, boneId);
            }

            if (_animator != null)
            {
                if (!OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone.ContainsKey(boneId))
                {
                    return null;
                }
                return _animator.GetBoneTransform(OVRHumanBodyBonesMappings.FullBodyBoneIdToHumanBodyBone[boneId]);
            }

            return null;
        }

        bool IAnimationJobData.IsValid()
        {
            if (_animator == null && _customSkeleton == null)
            {
                Debug.LogError($"Animator or skeleton not set up.");
                return false;
            }

            var targetName = _animator != null ? _animator.name : _customSkeleton.name;
            if (_bonePairData == null || _bonePairData.Length == 0)
            {
                Debug.LogError($"Bone pair data not set up on {targetName}.");
                return false;
            }

            if (_hipsToHeadBones == null || _hipsToHeadBones.Length == 0)
            {
                Debug.LogError($"Hips to head bones not set up on {targetName}.");
                return false;
            }

            if (!_leftArmData.IsInitialized || !_rightArmData.IsInitialized)
            {
                Debug.LogError($"Arm data not set up on {targetName}.");
                return false;
            }

            if ((DeformationBodyType)_deformationBodyType == DeformationBodyType.FullBody)
            {
                if (!_leftLegData.IsInitialized || !_rightLegData.IsInitialized)
                {
                    Debug.LogError($"Leg data not set up on {targetName}.");
                    return false;
                }
            }

            if (_squashLimit < float.Epsilon)
            {
                Debug.LogError("Please set squash limit!");
                return false;
            }

            if (_stretchLimit < float.Epsilon)
            {
                Debug.LogError("Please set stretch limit!");
                return false;
            }

            return true;
        }

        void IAnimationJobData.SetDefaultValues()
        {
            _animator = null;
            _customSkeleton = null;
            _spineTranslationCorrectionType = (int)SpineTranslationCorrectionType.None;

            _leftArmWeight = 0.0f;
            _rightArmWeight = 0.0f;
            _leftHandWeight = 0.0f;
            _rightHandWeight = 0.0f;
            _squashLimit = 2.0f;
            _stretchLimit = 2.0f;
            _alignLeftLegWeight = 0.0f;
            _alignRightLegWeight = 0.0f;
            _leftToesWeight = 0.0f;
            _rightToesWeight = 0.0f;

            _startingScale = Vector3.one;
            _bonePairData = null;
            _hipsToHeadBones = null;
            _leftArmData = new ArmPosData();
            _rightArmData = new ArmPosData();
            _leftLegData = new LegPosData();
            _rightLegData = new LegPosData();
        }
    }

    /// <summary>
    /// FullBodyDeformation constraint.
    /// </summary>
    [DisallowMultipleComponent, AddComponentMenu("Movement Animation Rigging/Full Body Deformation Constraint")]
    public class FullBodyDeformationConstraint : RigConstraint<
        FullBodyDeformationJob,
        FullBodyDeformationData,
        FullBodyDeformationJobBinder<FullBodyDeformationData>>,
        IOVRSkeletonConstraint
    {
        /// <inheritdoc />
        public void RegenerateData()
        {
        }
    }
}
