// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// Information about the distance between two bone transforms.
    /// </summary>
    [Serializable]
    public struct BonePairData
    {
        /// <summary>
        /// The start bone transform.
        /// </summary>
        [SyncSceneToStream]
        public Transform StartBone;

        /// <summary>
        /// The end bone transform.
        /// </summary>
        [SyncSceneToStream]
        public Transform EndBone;

        /// <summary>
        /// The distance between the target and current position before the end bone snaps to the target position.
        /// </summary>
        public float SnapThreshold;

        /// <summary>
        /// The speed of the bone move towards if enabled.
        /// </summary>
        public float MoveTowardsSpeed;

        /// <summary>
        /// The distance between the start and end bones.
        /// </summary>
        public float Distance;

        /// <summary>
        /// If true, the end bone will move towards the deformation target position.
        /// </summary>
        public bool IsMoveTowards;
    }

    /// <summary>
    /// Information about the positioning of an arm.
    /// </summary>
    [Serializable]
    public struct ArmPosData
    {
        /// <summary>
        /// The shoulder transform.
        /// </summary>
        [SyncSceneToStream]
        public Transform ShoulderBone;

        /// <summary>
        /// The upper arm transform.
        /// </summary>
        [SyncSceneToStream]
        public Transform UpperArmBone;

        /// <summary>
        /// The lower arm transform.
        /// </summary>
        [SyncSceneToStream]
        public Transform LowerArmBone;

        /// <summary>
        /// The hand transform.
        /// </summary>
        [SyncSceneToStream]
        public Transform HandBone;

        /// <summary>
        /// The weight for the deformation on arms.
        /// </summary>
        public float ArmWeight;

        /// <summary>
        /// The weight for the deformation on hands.
        /// </summary>
        public float HandWeight;

        /// <summary>
        /// The move towards speed for the arms.
        /// </summary>
        public float MoveSpeed;

        /// <summary>
        /// Indicates if initialized or not.
        /// </summary>
        /// <returns></returns>
        public bool IsInitialized
        {
            get
            {
                return ShoulderBone != null &&
                    UpperArmBone != null &&
                    LowerArmBone != null &&
                    HandBone != null;
            }
        }

        /// <summary>
        /// Resets all tracked transforms to null.
        /// </summary>
        public void ClearTransformData()
        {
            ShoulderBone = null;
            UpperArmBone = null;
            LowerArmBone = null;
            HandBone = null;
        }
    }

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
        /// The array of transforms from the hips to the head bones.
        /// </summary>
        public Transform[] HipsToHeadBones { get; }

        /// <summary>
        /// The position info for the bone pairs used for deformation.
        /// </summary>
        public BonePairData[] BonePairs { get; }

        /// <summary>
        /// The position info for the left arm.
        /// </summary>
        public ArmPosData LeftArm { get; }

        /// <summary>
        /// Indicates if left arm data was initialized or not.
        /// </summary>
        public bool LeftArmDataInitialized { get; }

        /// <summary>
        /// The position info for the right arm.
        /// </summary>
        public ArmPosData RightArm { get; }

        /// <summary>
        /// Indicates if left arm data was initialized or not.
        /// </summary>
        public bool RightArmDataInitialized { get; }

        /// <summary>
        /// The type of spine translation correction that should be applied.
        /// </summary>
        public DeformationData.SpineTranslationCorrectionType SpineCorrectionType { get; }

        /// <summary>
        /// The starting scale of the character, taken from the animator transform.
        /// </summary>
        public Vector3 StartingScale { get; }

        /// <summary>
        /// The distance between the hips and head bones.
        /// </summary>
        public float HipsToHeadDistance { get; }

        /// <summary>
        /// Allows the spine correction to run only once, assuming the skeleton's positions don't get updated multiple times.
        /// </summary>
        public bool CorrectSpineOnce { get; }

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
    /// TODO: allow for case where rig can be enabled, this means sync transform arrays must not be null by default
    /// </summary>
    [System.Serializable]
    public struct DeformationData : IAnimationJobData, IDeformationData
    {
        /// <summary>
        /// The spine translation correction type.
        /// </summary>
        public enum SpineTranslationCorrectionType
        {
            /// <summary>No spine translation correction applied.</summary>
            None,
            /// <summary>Skip the head bone for applying spine translation correction.</summary>
            SkipHead,
            /// <summary>Skip the hips bone for applying spine translation correction.</summary>
            SkipHips,
            /// <summary>Skip both the head bone and hips bone for applying spine translation correction.</summary>
            SkipHipsAndHead
        }

        /// <inheritdoc />
        OVRCustomSkeleton IDeformationData.ConstraintCustomSkeleton => _customSkeleton;

        /// <inheritdoc />
        Animator IDeformationData.ConstraintAnimator => _animator;

        /// <inheritdoc />
        SpineTranslationCorrectionType IDeformationData.SpineCorrectionType => _spineTranslationCorrectionType;

        /// <inheritdoc />
        Transform[] IDeformationData.HipsToHeadBones => _hipsToHeadBones;
        /// <inheritdoc />
        BonePairData[] IDeformationData.BonePairs => _bonePairData;

        /// <inheritdoc />
        ArmPosData IDeformationData.LeftArm => _leftArmData;

        /// <inheritdoc />
        public bool LeftArmDataInitialized => _leftArmData.IsInitialized;

        /// <inheritdoc />
        ArmPosData IDeformationData.RightArm => _rightArmData;

        /// <inheritdoc />
        public bool RightArmDataInitialized => _rightArmData.IsInitialized;

        /// <inheritdoc />
        Vector3 IDeformationData.StartingScale => _startingScale;

        /// <inheritdoc />
        float IDeformationData.HipsToHeadDistance => _hipsToHeadDistance;

        /// <inheritdoc />
        bool IDeformationData.CorrectSpineOnce => _correctSpineOnce;

        /// <inheritdoc cref="IDeformationData.ConstraintCustomSkeleton"/>
        [NotKeyable, SerializeField]
        [Tooltip(DeformationDataTooltips.CustomSkeleton)]
        private OVRCustomSkeleton _customSkeleton;

        /// <inheritdoc cref="IDeformationData.ConstraintAnimator"/>
        [NotKeyable, SerializeField]
        [Tooltip(DeformationDataTooltips.Animator)]
        private Animator _animator;

        /// <inheritdoc cref="IDeformationData.SpineCorrectionType"/>
        [NotKeyable, SerializeField]
        [Tooltip(DeformationDataTooltips.SpineTranslationCorrectionType)]
        private SpineTranslationCorrectionType _spineTranslationCorrectionType;

        /// <inheritdoc cref="IDeformationData.CorrectSpineOnce"/>
        [NotKeyable, SerializeField]
        [Tooltip(DeformationDataTooltips.CorrectSpineOnce)]
        private bool _correctSpineOnce;

        /// <summary>
        /// Apply deformation on arms.
        /// </summary>
        [NotKeyable, SerializeField]
        [Tooltip(DeformationDataTooltips.ApplyToArms)]
        private bool _applyToArms;

        /// <summary>
        /// Apply deformation on hands.
        /// </summary>
        [NotKeyable, SerializeField]
        [Tooltip(DeformationDataTooltips.ApplyToHands)]
        private bool _applyToHands;

        /// <summary>
        /// If true, the arms will move towards the deformation target position.
        /// </summary>
        [NotKeyable, SerializeField]
        [Tooltip(DeformationDataTooltips.MoveTowardsArms)]
        [ConditionalHide("_useMoveTowardsArms", true)]
        private bool _useMoveTowardsArms;

        /// <summary>
        /// The distance between the target and current position before the bone snaps to the target position.
        /// </summary>
        [NotKeyable, SerializeField]
        [Tooltip(DeformationDataTooltips.SnapThreshold)]
        [ConditionalHide("_useMoveTowardsArms", true)]
        private float _snapThreshold;

        /// <summary>
        /// The weight for the deformation on arms.
        /// </summary>
        [NotKeyable, SerializeField]
        [Tooltip(DeformationDataTooltips.ArmWeight)]
        [ConditionalHide("_applyToArms", true)]
        private float _armWeight;

        /// <summary>
        /// The weight for the deformation on hands.
        /// </summary>
        [NotKeyable, SerializeField]
        [Tooltip(DeformationDataTooltips.HandWeight)]
        [ConditionalHide("_applyToHands", true)]
        private int _handWeight;

        /// <summary>
        /// The move towards speed for the arms.
        /// </summary>
        [NotKeyable, SerializeField]
        [Tooltip(DeformationDataTooltips.ArmMoveSpeed)]
        [ConditionalHide("_useMoveTowardsArms", true)]
        private float _armMoveSpeed;

        /// <summary>
        /// Array of transform bones from hips to head.
        /// </summary>
        [SyncSceneToStream, SerializeField]
        [Tooltip(DeformationDataTooltips.HipsToHeadBones)]
        private Transform[] _hipsToHeadBones;

        /// <summary>
        /// Left arm data.
        /// </summary>
        [SerializeField]
        [Tooltip(DeformationDataTooltips.LeftArmData)]
        private ArmPosData _leftArmData;

        /// <summary>
        /// Right arm data.
        /// </summary>
        [SerializeField]
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

        private Transform FindBoneTransform(OVRSkeleton.BoneId boneId)
        {
            if (_customSkeleton != null)
            {
                return RiggingUtilities.FindBoneTransformFromCustomSkeleton(_customSkeleton, boneId);
            }

            if (_animator != null)
            {
                return RiggingUtilities.FindBoneTransformAnimator(_animator, boneId);
            }

            return null;
        }

        /// <inheritdoc />
        public void SetUpHipsAndHeadBones()
        {
            var hipToHeadBones = new List<Transform>();
            for (int boneId = (int)OVRSkeleton.BoneId.Body_Hips; boneId <= (int)OVRSkeleton.BoneId.Body_Head;
                 boneId++)
            {
                var foundBoneTransform = FindBoneTransform((OVRSkeleton.BoneId)boneId);
                if (foundBoneTransform == null)
                {
                    continue;
                }
                hipToHeadBones.Add(foundBoneTransform.transform);
            }

            _hipsToHeadDistance =
                Vector3.Distance(hipToHeadBones[0].position, hipToHeadBones[^1].position);
            _hipsToHeadBones = hipToHeadBones.ToArray();
        }

        /// <inheritdoc />
        public void SetUpLeftArmData()
        {
            _leftArmData = new ArmPosData()
            {
                ArmWeight = _applyToArms ? _armWeight : 0,
                HandWeight = _applyToHands ? _handWeight : 0,
                MoveSpeed = _armMoveSpeed,
                ShoulderBone = FindBoneTransform(OVRSkeleton.BoneId.Body_LeftShoulder),
                UpperArmBone = FindBoneTransform(OVRSkeleton.BoneId.Body_LeftArmUpper),
                LowerArmBone = FindBoneTransform(OVRSkeleton.BoneId.Body_LeftArmLower),
                HandBone = FindBoneTransform(OVRSkeleton.BoneId.Body_LeftHandWrist)
            };
        }

        /// <inheritdoc />
        public void SetUpRightArmData()
        {
            _rightArmData = new ArmPosData()
            {
                ArmWeight = _applyToArms ? _armWeight : 0,
                HandWeight = _applyToHands ? _handWeight : 0,
                MoveSpeed = _armMoveSpeed,
                ShoulderBone = FindBoneTransform(OVRSkeleton.BoneId.Body_RightShoulder),
                UpperArmBone = FindBoneTransform(OVRSkeleton.BoneId.Body_RightArmUpper),
                LowerArmBone = FindBoneTransform(OVRSkeleton.BoneId.Body_RightArmLower),
                HandBone = FindBoneTransform(OVRSkeleton.BoneId.Body_RightHandWrist)
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
            if (!LeftArmDataInitialized)
            {
                Debug.LogError("Please set up left arm data before trying to " +
                    "set up bone pairs");
                return;
            }
            if (!RightArmDataInitialized)
            {
                Debug.LogError("Please set up right arm data before trying to " +
                    "set up bone pairs");
                return;
            }
            // Setup bone pairs
            var bonePairs = new List<BonePairData>();
            for (int i = 0; i < _hipsToHeadBones.Length - 1; i++)
            {
                var bonePair = new BonePairData
                {
                    StartBone = _hipsToHeadBones[i],
                    EndBone = _hipsToHeadBones[i + 1],
                    SnapThreshold = 0,
                    MoveTowardsSpeed = 0,
                    Distance = Vector3.Distance(
                        _hipsToHeadBones[i + 1].position,
                        _hipsToHeadBones[i].position),
                    IsMoveTowards = false
                };
                bonePairs.Add(bonePair);
            }

            var chestBone = FindBoneTransform(OVRSkeleton.BoneId.Body_Chest);
            var chestBonePos = chestBone.position;
            var leftShoulderBonePos = _leftArmData.ShoulderBone.position;
            var rightShoulderBonePos = _rightArmData.ShoulderBone.position;
            var leftUpperArmBonePos = _leftArmData.UpperArmBone.position;
            var rightUpperArmBonePos = _rightArmData.UpperArmBone.position;
            var leftLowerArmBonePos = _leftArmData.LowerArmBone.position;
            var rightLowerArmBonePos = _rightArmData.LowerArmBone.position;

            if (_applyToArms)
            {
                // Chest to shoulder bones.
                bonePairs.Add(new BonePairData
                {
                    StartBone = chestBone,
                    EndBone = _leftArmData.ShoulderBone,
                    SnapThreshold = _snapThreshold,
                    MoveTowardsSpeed = _leftArmData.MoveSpeed,
                    Distance = Vector3.Distance(
                        leftShoulderBonePos,
                        chestBonePos),
                    IsMoveTowards = false
                });
                bonePairs.Add(new BonePairData
                {
                    StartBone = chestBone,
                    EndBone = _rightArmData.ShoulderBone,
                    SnapThreshold = _snapThreshold,
                    MoveTowardsSpeed = _rightArmData.MoveSpeed,
                    Distance = Vector3.Distance(
                        rightShoulderBonePos,
                        chestBonePos),
                    IsMoveTowards = false
                });

                // Shoulder to upper arm bones.
                bonePairs.Add(new BonePairData
                {
                    StartBone = _leftArmData.ShoulderBone,
                    EndBone = _leftArmData.UpperArmBone,
                    SnapThreshold = _snapThreshold,
                    MoveTowardsSpeed = _leftArmData.MoveSpeed,
                    Distance = Vector3.Distance(
                        leftUpperArmBonePos,
                        leftShoulderBonePos),
                    IsMoveTowards = _useMoveTowardsArms
                });
                bonePairs.Add(new BonePairData
                {
                    StartBone = _rightArmData.ShoulderBone,
                    EndBone = _rightArmData.UpperArmBone,
                    SnapThreshold = _snapThreshold,
                    MoveTowardsSpeed = _rightArmData.MoveSpeed,
                    Distance = Vector3.Distance(
                        rightUpperArmBonePos,
                        rightShoulderBonePos),
                    IsMoveTowards = _useMoveTowardsArms
                });

                // Upper arm to lower arm bones.
                bonePairs.Add(new BonePairData
                {
                    StartBone = _leftArmData.UpperArmBone,
                    EndBone = _leftArmData.LowerArmBone,
                    SnapThreshold = _snapThreshold,
                    MoveTowardsSpeed = _leftArmData.MoveSpeed,
                    Distance = Vector3.Distance(
                        leftLowerArmBonePos,
                        leftUpperArmBonePos),
                    IsMoveTowards = _useMoveTowardsArms
                });
                bonePairs.Add(new BonePairData
                {
                    StartBone = _rightArmData.UpperArmBone,
                    EndBone = _rightArmData.LowerArmBone,
                    SnapThreshold = _snapThreshold,
                    MoveTowardsSpeed = _rightArmData.MoveSpeed,
                    Distance = Vector3.Distance(
                        rightLowerArmBonePos,
                        rightUpperArmBonePos),
                    IsMoveTowards = _useMoveTowardsArms
                });
            }

            if (_applyToHands)
            {
                // Lower arm to hand bones.
                bonePairs.Add(new BonePairData
                {
                    StartBone = _leftArmData.LowerArmBone,
                    EndBone = _leftArmData.HandBone,
                    SnapThreshold = _snapThreshold,
                    MoveTowardsSpeed = _leftArmData.MoveSpeed,
                    Distance = Vector3.Distance(
                        _leftArmData.HandBone.position,
                        leftLowerArmBonePos),
                    IsMoveTowards = _useMoveTowardsArms
                });
                bonePairs.Add(new BonePairData
                {
                    StartBone = _rightArmData.LowerArmBone,
                    EndBone = _rightArmData.HandBone,
                    SnapThreshold = _snapThreshold,
                    MoveTowardsSpeed = _rightArmData.MoveSpeed,
                    Distance = Vector3.Distance(
                        _rightArmData.HandBone.position,
                        rightLowerArmBonePos),
                    IsMoveTowards = _useMoveTowardsArms
                });
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
            _hipsToHeadBones = null;
            _leftArmData.ClearTransformData();
            _rightArmData.ClearTransformData();
            _bonePairData = null;
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

            if (_hipsToHeadBones == null || _hipsToHeadBones.Length == 0)
            {
                Debug.LogError("Hips to head bones not set up.");
                return false;
            }

            if (_applyToArms)
            {
                if (_leftArmData.ShoulderBone == null || _leftArmData.UpperArmBone == null || _leftArmData.LowerArmBone == null ||
                    _rightArmData.ShoulderBone == null || _rightArmData.UpperArmBone == null || _rightArmData.LowerArmBone == null)
                {
                    Debug.LogError("Arm data not set up.");
                    return false;
                }
            }

            return true;
        }

        void IAnimationJobData.SetDefaultValues()
        {
            _animator = null;
            _spineTranslationCorrectionType = SpineTranslationCorrectionType.None;
            _applyToArms = false;
            _useMoveTowardsArms = false;
            _correctSpineOnce = false;
            _snapThreshold = 0.1f;
            _startingScale = Vector3.one;
            _leftArmData = new ArmPosData();
            _rightArmData = new ArmPosData();
            _bonePairData = null;
            _hipsToHeadBones = null;
        }
    }

    /// <summary>
    /// Deformation constraint.
    /// </summary>
    [DisallowMultipleComponent]
    public class DeformationConstraint : RigConstraint<
        DeformationJob,
        DeformationData,
        DeformationJobBinder<DeformationData>>,
        IOVRSkeletonConstraint
    {
        private void Start()
        {
        }

        /// <inheritdoc />
        public void RegenerateData()
        {
        }
    }
}
