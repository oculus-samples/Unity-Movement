// Copyright (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using Oculus.Interaction;
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
        /// The OVR Skeleton component for the character.
        /// </summary>
        public OVRCustomSkeleton ConstraintSkeleton { get; }

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
    }

    /// <summary>
    /// Deformation data used by the deformation job.
    /// Implements the deformation data interface.
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
        OVRCustomSkeleton IDeformationData.ConstraintSkeleton => _skeleton;

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
        [NotKeyable, SerializeField]
        [Tooltip(DeformationDataTooltips.Skeleton)]
        private OVRCustomSkeleton _skeleton;

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

        /// <summary>
        /// Setup the deformation data struct for the deformation job.
        /// </summary>
        public void Setup()
        {
            SetupHipsHeadData();
            SetupArmData();
            SetupBonePairs();
        }

        /// <summary>
        /// Assign the OVR Skeleton.
        /// </summary>
        /// <param name="skeleton">The OVRSkeleton</param>
        public void AssignOVRSkeleton(OVRCustomSkeleton skeleton)
        {
            _skeleton = skeleton;
        }

        private void SetupHipsHeadData()
        {
            // Setup hips to head
            var hipToHeadBones = new List<Transform>();
            for (int i = (int)OVRSkeleton.BoneId.Body_Hips; i <= (int)OVRSkeleton.BoneId.Body_Head; i++)
            {
                hipToHeadBones.Add(_skeleton.CustomBones[i].transform);
            }
            _hipsToHeadDistance =
                Vector3.Distance(hipToHeadBones[0].position, hipToHeadBones[^1].position);
            _hipsToHeadBones = hipToHeadBones.ToArray();
        }

        private void SetupArmData()
        {
            // Setup arm data
            _leftArmData = new ArmPosData()
            {
                Weight = _armWeight,
                MoveSpeed = _armMoveSpeed,
                ShoulderBone = _skeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftShoulder],
                UpperArmBone = _skeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftArmUpper],
                LowerArmBone = _skeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_LeftArmLower],
            };
            _rightArmData = new ArmPosData()
            {
                Weight = _armWeight,
                MoveSpeed = _armMoveSpeed,
                ShoulderBone = _skeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightShoulder],
                UpperArmBone = _skeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightArmUpper],
                LowerArmBone = _skeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_RightArmLower],
            };
        }

        private void SetupBonePairs()
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
                var chestBone = _skeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_Chest].transform;
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
            if (_skeleton == null)
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
            _spineTranslationCorrectionType = SpineTranslationCorrectionType.None;
            _applyToArms = false;
            _useMoveTowardsArms = false;
            _correctSpineOnce = false;
            _snapThreshold = 0.1f;
            _leftArmData = new ArmPosData();
            _rightArmData = new ArmPosData();

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
            data.Setup();
        }

        /// <inheritdoc />
        public void RegenerateData()
        {
        }
    }
}
