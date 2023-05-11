// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// Interface for grounding data.
    /// </summary>
    public interface IGroundingData
    {
        /// <summary>
        /// The original skeleton.
        /// </summary>
        OVRCustomSkeleton ConstraintSkeleton { get; }

        /// <summary>
        /// Optional. The other leg's grounding constraint, used to check if this leg can move.
        /// </summary>
        public GroundingData Pair { get; }

        /// <summary>
        /// The layers that the raycast will check against for grounding.
        /// </summary>
        public LayerMask GroundLayers { get; }

        /// <summary>
        /// The maximum distance that the raycast will go when checking for grounding.
        /// </summary>
        public float GroundRaycastDist { get; }

        /// <summary>
        /// The height offset from the grounded floor to be applied to the foot.
        /// </summary>
        public float GroundOffset { get; }

        /// <summary>
        /// The hips target transform.
        /// </summary>
        public Transform HipsTarget { get; }

        /// <summary>
        /// The knee target for the leg.
        /// </summary>
        public Transform KneeTarget { get; }

        /// <summary>
        /// The foot target for the leg.
        /// </summary>
        public Transform FootTarget { get; }

        /// <summary>
        /// The hips transform.
        /// </summary>
        public Transform Hips { get; }

        /// <summary>
        /// The leg upper transform.
        /// </summary>
        public Transform Leg { get; }

        /// <summary>
        /// The initial position offset for the leg.
        /// </summary>
        public Vector3 LegPosOffset { get; }

        /// <summary>
        /// The initial rotation offset for the leg.
        /// </summary>
        public Quaternion LegRotOffset { get; }

        /// <summary>
        /// The initial rotation offset for the feet.
        /// </summary>
        public Vector3 FootRotationOffset { get; }

        /// <summary>
        /// The animation curve for evaluating the step height value.
        /// </summary>
        public AnimationCurve StepCurve { get; }

        /// <summary>
        /// The speed of the step for the foot.
        /// </summary>
        public float StepSpeed { get; }

        /// <summary>
        /// The height of the step taken.
        /// </summary>
        public float StepHeight { get; }

        /// <summary>
        /// The maximum distance for the step height to not be scaled.
        /// </summary>
        public float StepHeightScaleDist { get; }

        /// <summary>
        /// The distance before the step is triggered.
        /// </summary>
        public float StepDist { get; }

        /// <summary>
        /// The amount of move progress.
        /// </summary>
        public float Progress { set; }

        /// <summary>
        /// Generates a new value for the threshold move progress.
        /// </summary>
        public void GenerateThresholdMoveProgress();

        /// <summary>
        /// Called on when the animation job is being created.
        /// </summary>
        public void Create();
    }

    [System.Serializable]
    public struct GroundingData : IAnimationJobData, IGroundingData
    {
        // Interface implementation
        /// <inheritdoc />
        OVRCustomSkeleton IGroundingData.ConstraintSkeleton => _skeleton;

        /// <inheritdoc />
        GroundingData IGroundingData.Pair => _pair.data;

        /// <inheritdoc />
        LayerMask IGroundingData.GroundLayers => _groundLayers;

        /// <inheritdoc />
        float IGroundingData.GroundRaycastDist => _groundRaycastDist;

        /// <inheritdoc />
        float IGroundingData.GroundOffset => _groundOffset;

        /// <inheritdoc />
        Transform IGroundingData.HipsTarget => _hipsTarget;

        /// <inheritdoc />
        Transform IGroundingData.KneeTarget => _kneeTarget;

        /// <inheritdoc />
        Transform IGroundingData.FootTarget => _footTarget;

        /// <inheritdoc />
        Transform IGroundingData.Hips => _hips;

        /// <inheritdoc />
        Transform IGroundingData.Leg => _leg;

        /// <inheritdoc />
        Vector3 IGroundingData.LegPosOffset => _legPosOffset;

        /// <inheritdoc />
        Quaternion IGroundingData.LegRotOffset => _legRotOffset;

        /// <inheritdoc />
        Vector3 IGroundingData.FootRotationOffset => _footRotationOffset;

        /// <inheritdoc />
        AnimationCurve IGroundingData.StepCurve => _stepCurve;

        /// <inheritdoc />
        float IGroundingData.StepSpeed => _stepSpeed;

        /// <inheritdoc />
        float IGroundingData.StepHeight => _stepHeight;

        /// <inheritdoc />
        float IGroundingData.StepHeightScaleDist => _stepHeightScaleDist;

        /// <inheritdoc />
        float IGroundingData.StepDist => _stepDist;

        /// <inheritdoc />
        float IGroundingData.Progress
        {
            set => _progress = value;
        }

        /// <inheritdoc cref="IGroundingData.ConstraintSkeleton"/>
        [NotKeyable, SerializeField]
        [Tooltip(GroundingDataTooltips.Skeleton)]
        private OVRCustomSkeleton _skeleton;

        /// <inheritdoc cref="IGroundingData.Pair"/>
        [NotKeyable, SerializeField]
        [Tooltip(GroundingDataTooltips.Pair)]
        private GroundingConstraint _pair;

        /// <inheritdoc cref="IGroundingData.GroundLayers"/>
        [NotKeyable, SerializeField]
        [Tooltip(GroundingDataTooltips.GroundingLayers)]
        private LayerMask _groundLayers;

        /// <inheritdoc cref="IGroundingData.GroundRaycastDist"/>
        [NotKeyable, SerializeField]
        [Tooltip(GroundingDataTooltips.GroundRaycastDist)]
        private float _groundRaycastDist;

        /// <inheritdoc cref="IGroundingData.GroundOffset"/>
        [NotKeyable, SerializeField]
        [Tooltip(GroundingDataTooltips.GroundOffset)]
        private float _groundOffset;

        /// <inheritdoc cref="IGroundingData.HipsTarget"/>
        [Header("Transform References")]
        [SyncSceneToStream, SerializeField]
        [Tooltip(GroundingDataTooltips.HipsTarget)]
        private Transform _hipsTarget;

        /// <inheritdoc cref="IGroundingData.KneeTarget"/>
        [SyncSceneToStream, SerializeField]
        [Tooltip(GroundingDataTooltips.KneeTarget)]
        private Transform _kneeTarget;

        /// <inheritdoc cref="IGroundingData.FootTarget"/>
        [SyncSceneToStream, SerializeField]
        [Tooltip(GroundingDataTooltips.FootTarget)]
        private Transform _footTarget;

        /// <inheritdoc cref="IGroundingData.Leg"/>
        [SyncSceneToStream, SerializeField]
        [Tooltip(GroundingDataTooltips.Leg)]
        private Transform _leg;

        /// <summary>
        /// The foot transform.
        /// </summary>
        [SyncSceneToStream, SerializeField]
        [Tooltip(GroundingDataTooltips.Foot)]
        private Transform _foot;

        /// <inheritdoc cref="IGroundingData.FootRotationOffset"/>
        [Header("Step Settings")]
        [NotKeyable, SerializeField]
        [Tooltip(GroundingDataTooltips.FootRotationOffset)]
        private Vector3 _footRotationOffset;

        /// <inheritdoc cref="IGroundingData.StepCurve"/>
        [NotKeyable, SerializeField]
        [Tooltip(GroundingDataTooltips.StepCurve)]
        private AnimationCurve _stepCurve;

        /// <inheritdoc cref="IGroundingData.StepDist"/>
        [NotKeyable, SerializeField]
        [Tooltip(GroundingDataTooltips.StepDist)]
        private float _stepDist;

        /// <inheritdoc cref="IGroundingData.StepSpeed"/>
        [NotKeyable, SerializeField]
        [Tooltip(GroundingDataTooltips.StepSpeed)]
        private float _stepSpeed;

        /// <inheritdoc cref="IGroundingData.StepHeight"/>
        [NotKeyable, SerializeField]
        [Tooltip(GroundingDataTooltips.StepHeight)]
        private float _stepHeight;

        /// <inheritdoc cref="IGroundingData.StepHeightScaleDist"/>
        [NotKeyable, SerializeField]
        [Tooltip(GroundingDataTooltips.StepHeightScaleDist)]
        private float _stepHeightScaleDist;

        /// <summary>
        /// The lower bound of the move progress before the other foot can take a step.
        /// </summary>
        [NotKeyable, SerializeField, Range(0.0f, 1.0f)]
        [Tooltip(GroundingDataTooltips.MoveLowerThreshold)]
        private float _moveLowerThreshold;

        /// <summary>
        /// The upper bound of the move progress before the other foot can take a step.
        /// </summary>
        [NotKeyable, SerializeField, Range(0.0f, 1.0f)]
        [Tooltip(GroundingDataTooltips.MoveHigherThreshold)]
        private float _moveHigherThreshold;

        [SyncSceneToStream]
        private Transform _hips;
        private float _progress;
        private float _thresholdMoveProgress;
        private Vector3 _legPosOffset;
        private Quaternion _legRotOffset;

        /// <summary>
        /// Setup the grounding constraint.
        /// </summary>
        public void Setup()
        {
            _hips = _skeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_Hips];
            _legPosOffset = _leg.localPosition;
            _legRotOffset = _leg.localRotation;
        }

        /// <summary>
        /// Called on when the animation job is being created.
        /// </summary>
        public void Create()
        {
            if (_leg.parent != _hips.parent)
            {
                _leg.SetParent(_hips.parent);
            }
        }

        /// <summary>
        /// Generates a new value for the threshold move progress.
        /// </summary>
        public void GenerateThresholdMoveProgress()
        {
            _thresholdMoveProgress = Random.Range(_moveLowerThreshold, _moveHigherThreshold);
        }

        /// <summary>
        /// Returns true if the move progress is lower than the threshold move progress.
        /// </summary>
        /// <returns>True if the joint has finished moving.</returns>
        public bool FinishedMoving()
        {
            return _progress >= _thresholdMoveProgress;
        }

        /// <summary>
        /// Returns true if valid.
        /// </summary>
        /// <returns></returns>
        public bool IsValid()
        {
            if (_hipsTarget == null || _kneeTarget == null || _footTarget == null ||
                _leg == null || _foot == null)
            {
                return false;
            }

            if (_skeleton == null)
            {
                return false;
            }

            if (_pair == null)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Set default values.
        /// </summary>
        public void SetDefaultValues()
        {
            _pair = null;
            _groundLayers = new LayerMask();
            _groundRaycastDist = 10.0f;
            _groundOffset = 0.0f;
            _hipsTarget = null;
            _kneeTarget = null;
            _footTarget = null;
            _foot = null;
            _footRotationOffset = Vector3.zero;
            _stepCurve = new AnimationCurve();
            _stepSpeed = 0.0f;
            _stepHeight = 0.0f;
            _stepHeightScaleDist = 0.0f;
            _stepDist = 0.0f;
        }
    }

    /// <summary>
    /// Grounding constraint.
    /// </summary>
    [DisallowMultipleComponent]
    public class GroundingConstraint : RigConstraint<
        GroundingJob,
        GroundingData,
        GroundingJobBinder<GroundingData>>,
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
