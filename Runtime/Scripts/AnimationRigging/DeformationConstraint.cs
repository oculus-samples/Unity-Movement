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
    [System.Serializable]
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
        public float SnapThreshold { get; set; }

        /// <summary>
        /// The speed of the bone move towards if enabled.
        /// </summary>
        public float MoveTowardsSpeed { get; set; }

        /// <summary>
        /// The distance between the start and end bones.
        /// </summary>
        public float Distance { get; set; }

        /// <summary>
        /// If true, the end bone will move towards the deformation target position.
        /// </summary>
        public bool IsMoveTowards { get; set; }
    }

    /// <summary>
    /// Information about the positioning of an arm.
    /// </summary>
    [System.Serializable]
    public struct ArmPosData
    {
        /// <summary>
        /// The shoulder transform.
        /// </summary>
        [SyncSceneToStream]
        public Transform ShoulderBone;

        /// <summary>
        /// The upper arm transform
        /// </summary>
        [SyncSceneToStream]
        public Transform UpperArmBone;

        /// <summary>
        /// The lower arm transform.
        /// </summary>
        [SyncSceneToStream]
        public Transform LowerArmBone;

        /// <summary>
        /// The weight for the deformation on arms.
        /// </summary>
        public float Weight;

        /// <summary>
        /// The move towards speed for the arms.
        /// </summary>
        public float MoveSpeed;
    }

    /// <summary>
    /// Interface for deformation data.
    /// </summary>
    public interface IDeformationData
    {
        /// <summary>
        /// [Deprecated] The OVR Skeleton component for the character.
        /// NOTE: not recommended as this was experimental.
        /// </summary>
        public OVRSkeleton ConstraintSkeleton { get; }

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
        /// The position info for the right arm.
        /// </summary>
        public ArmPosData RightArm { get; }

        /// <summary>
        /// The type of spine translation correction that should be applied.
        /// </summary>
        public DeformationData.SpineTranslationCorrectionType SpineCorrectionType { get; }

        /// <summary>
        /// The distance between the hips and head bones.
        /// </summary>
        public float HipsToHeadDistance { get; }

        /// <summary>
        /// Allows the spine correction to run only once, assuming the skeleton's positions don't get updated multiple times.
        /// </summary>
        public bool CorrectSpineOnce { get; }
        
        /// <summary>
        /// Indicates if bone transforms are valid or not.
        /// </summary>
        /// <returns>True if bone transforms are valid, false if not.</returns>
        public bool IsBoneTransformsDataValid();

        /// <summary>
        /// Inidicates if initialization has run again.
        /// </summary>
        /// <returns></returns>
        bool HasInitialized();
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

        // Interface implementation
        /// <inheritdoc />
        OVRSkeleton IDeformationData.ConstraintSkeleton => _skeleton;

        // <inheritdoc />
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
        ArmPosData IDeformationData.RightArm => _rightArmData;

        /// <inheritdoc />
        float IDeformationData.HipsToHeadDistance => _hipsToHeadDistance;

        /// <inheritdoc />
        bool IDeformationData.CorrectSpineOnce => _correctSpineOnce;

        /// <inheritdoc cref="IDeformationData.ConstraintSkeleton"/>
        [NotKeyable, SerializeField, Optional]
        [Tooltip(DeformationDataTooltips.Skeleton)]
        private OVRSkeleton _skeleton;

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
        /// The move towards speed for the arms.
        /// </summary>
        [NotKeyable, SerializeField]
        [Tooltip(DeformationDataTooltips.ArmMoveSpeed)]
        [ConditionalHide("_useMoveTowardsArms", true)]
        private float _armMoveSpeed;

        [SyncSceneToStream]
        private Transform[] _hipsToHeadBones;

        [SyncSceneToStream]
        private ArmPosData _leftArmData;

        [SyncSceneToStream]
        private ArmPosData _rightArmData;

        private BonePairData[] _bonePairData;
        private float _hipsToHeadDistance;

        private bool _hasInitialized;
        private bool _obtainedProperReferences;

        /// <inheritdoc cref="IDeformationData.HasInitialized"/>
        public bool HasInitialized()
        {
            return _hasInitialized;
        }

        /// <summary>
        /// Setup the deformation data struct for the deformation job.
        /// </summary>
        /// <param name="dummyOne">Dummy transform if skeleton is not ready.</param>
        /// <param name="dummyTwo">Dummy transform if skeleton is not ready.</param>
        /// <returns>True if constraint has been properly set-up; false otherwise.</returns>
        public bool Setup(Transform dummyOne, Transform dummyTwo)
        {
            // don't run again if proper references were obtained.
            // this initialization should only run once.
            if (_obtainedProperReferences)
            {
                return true;
            }

            SetupHipsHeadData(dummyOne, dummyTwo);
            SetupArmData(dummyOne, dummyTwo);
            SetupBonePairs(dummyOne);

            _hasInitialized = true;
            _obtainedProperReferences = SkeletonOrAnimatorValid();
            return _obtainedProperReferences;
        }

        /// <summary>
        /// Assign the OVR Skeleton.
        /// </summary>
        /// <param name="skeleton">The OVRSkeleton component.</param>
        [ObsoleteAttribute("This method is absolete. Use AssignOVRCustomSkeleton or AssignAnimator instead.")]
        public void AssignOVRSkeleton(OVRSkeleton skeleton)
        {
            _skeleton = skeleton;
        }

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
        /// <param name="skeleton">The Animator component.</param>
        public void AssignAnimator(Animator animator)
        {
            _animator = animator;
        }

        private bool SkeletonOrAnimatorValid()
        {
            return
                (_customSkeleton != null && _customSkeleton.IsInitialized) ||
                (_animator != null) ||
                (_skeleton != null && _skeleton.IsInitialized);
        }

        /// <inheritdoc />
        public bool IsBoneTransformsDataValid()
        {
            return (_customSkeleton != null && _customSkeleton.IsDataValid) ||
                (_animator != null) ||
                (_skeleton != null && _skeleton.IsDataValid);
        }

        private void SetupHipsHeadData(Transform dummyOne, Transform dummyTwo)
        {
            var hipToHeadBones = new List<Transform>();
            if (SkeletonOrAnimatorValid())
            {
                for (int boneId = (int)OVRSkeleton.BoneId.Body_Hips; boneId <= (int)OVRSkeleton.BoneId.Body_Head;
                    boneId++)
                {
                    var foundBoneTransform = FindBoneTransform((OVRSkeleton.BoneId)boneId);
                    hipToHeadBones.Add(foundBoneTransform != null ?
                        foundBoneTransform.transform : dummyOne);
                }
            }
            else
            {
                hipToHeadBones.Add(dummyOne);
                hipToHeadBones.Add(dummyTwo);
            }

            _hipsToHeadDistance =
                Vector3.Distance(hipToHeadBones[0].position, hipToHeadBones[^1].position);
            _hipsToHeadBones = hipToHeadBones.ToArray();
        }

        private Transform FindBoneTransform(OVRSkeleton.BoneId boneId)
        {
            if (!SkeletonOrAnimatorValid())
            {
                return null;
            }
            if (_customSkeleton != null)
            {
                return RiggingUtilities.FindBoneTransformFromSkeleton(_customSkeleton, boneId);
            }
            else if (_animator != null)
            {
                return RiggingUtilities.FindBoneTransformAnimator(_animator, boneId);
            }
            // deprecated fields are last resort
            else
            {
                return RiggingUtilities.FindBoneTransformFromSkeleton(_skeleton, boneId);
            }
        }

        private void SetupArmData(Transform dummyOne, Transform dummyTwo)
        {
            bool skeletonInitialized = SkeletonOrAnimatorValid();
            // Setup arm data
            _leftArmData = new ArmPosData()
            {
                Weight = _armWeight,
                MoveSpeed = _armMoveSpeed,
                ShoulderBone = skeletonInitialized ?
                    FindBoneTransform(OVRSkeleton.BoneId.Body_LeftShoulder) :
                    dummyOne,
                UpperArmBone = skeletonInitialized ?
                    FindBoneTransform(OVRSkeleton.BoneId.Body_LeftArmUpper) :
                    dummyTwo,
                LowerArmBone = skeletonInitialized ?
                    FindBoneTransform(OVRSkeleton.BoneId.Body_LeftArmLower) :
                    dummyTwo,
            };
            _rightArmData = new ArmPosData()
            {
                Weight = _armWeight,
                MoveSpeed = _armMoveSpeed,
                ShoulderBone = skeletonInitialized ?
                    FindBoneTransform(OVRSkeleton.BoneId.Body_RightShoulder) :
                    dummyOne,
                UpperArmBone = skeletonInitialized ?
                    FindBoneTransform(OVRSkeleton.BoneId.Body_RightArmUpper) :
                    dummyTwo,
                LowerArmBone = skeletonInitialized ?
                    FindBoneTransform(OVRSkeleton.BoneId.Body_RightArmLower) :
                    dummyTwo,
            };
        }

        private void SetupBonePairs(Transform dummyTransform)
        {
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

            if (_applyToArms)
            {
                var chestBone = SkeletonOrAnimatorValid() ?
                    FindBoneTransform(OVRSkeleton.BoneId.Body_Chest) :
                    dummyTransform;
                var chestBonePos = chestBone.position;
                var leftShoulderBonePos = _leftArmData.ShoulderBone.position;
                var rightShoulderBonePos = _rightArmData.ShoulderBone.position;

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
                        _leftArmData.UpperArmBone.position,
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
                        _rightArmData.UpperArmBone.position,
                        rightShoulderBonePos),
                    IsMoveTowards = _useMoveTowardsArms
                });
            }
            _bonePairData = bonePairs.ToArray();
        }

        bool IAnimationJobData.IsValid()
        {
            if (_skeleton == null && _animator == null)
            {
                return false;
            }

            if (_applyToArms)
            {
                if (_leftArmData.ShoulderBone == null || _leftArmData.UpperArmBone == null || _leftArmData.LowerArmBone == null ||
                    _rightArmData.ShoulderBone == null || _rightArmData.UpperArmBone == null || _rightArmData.LowerArmBone == null)
                {
                    return false;
                }
            }

            return true;
        }

        void IAnimationJobData.SetDefaultValues()
        {
            _skeleton = null;
            _animator = null;
            _spineTranslationCorrectionType = SpineTranslationCorrectionType.None;
            _applyToArms = false;
            _useMoveTowardsArms = false;
            _correctSpineOnce = false;
            _snapThreshold = 0.1f;
            _leftArmData = new ArmPosData();
            _rightArmData = new ArmPosData();
            _hasInitialized = false;
            _obtainedProperReferences = false;
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
        private GameObject _dummyOne, _dummyTwo;

        private void Awake()
        {
            CreateDummyGameObjects();
        }

        private void Start()
        {
            if (data.Setup(_dummyOne.transform, _dummyTwo.transform))
            {
                gameObject.SetActive(true);
            }
        }

        /// <inheritdoc />
        public void RegenerateData()
        {
            CreateDummyGameObjects();
            if (data.Setup(_dummyOne.transform, _dummyTwo.transform))
            {
                gameObject.SetActive(true);
            }
        }

        private void CreateDummyGameObjects()
        {
            if (_dummyOne != null && _dummyTwo != null)
            {
                return;
            }
            _dummyOne = new GameObject("Deformation Constraint Dummy 1");
            _dummyOne.transform.SetParent(this.transform);
            _dummyTwo = new GameObject("Deformation Constraint Dummy 2");
            _dummyTwo.transform.SetParent(this.transform);
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            if (gameObject.activeInHierarchy && !Application.isPlaying)
            {
                Debug.LogWarning($"{name} should be disabled initially; it enables itself when ready. Otherwise you" +
                    $" might get an errors regarding invalid sync variables at runtime.");
            }
        }
    }
}
