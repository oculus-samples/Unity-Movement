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
        /// The original skeleton for the character.
        /// </summary>
        OVRCustomSkeleton ConstraintSkeleton { get; }

        /// <summary>
        /// The Animator component for the character.
        /// </summary>
        public Animator ConstraintAnimator { get; }

        /// <summary>
        /// If true, update this job.
        /// </summary>
        public bool ShouldUpdate { get; set; }

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
        /// The previous knee position.
        /// </summary>
        public Vector3 PreviousKneePos { get; set; }

        /// <summary>
        /// The ground raycast hit.
        /// </summary>
        public RaycastHit GroundRaycastHit { get; set; }

        /// <summary>
        /// Generates a new value for the threshold move progress.
        /// </summary>
        public void GenerateThresholdMoveProgress();

        /// <summary>
        /// Indicates if bone transforms are valid or not.
        /// </summary>
        /// <returns>True if bone transforms are valid, false if not.</returns>
        public bool IsBoneTransformsDataValid();
    }

    /// <summary>
    /// Grounding data used by grounding job.
    /// TODO: allow for case where rig can be enabled, this means sync transform arrays must not be null by default
    /// </summary>
    [System.Serializable]
    public struct GroundingData : IAnimationJobData, IGroundingData
    {
        // Interface implementation
        /// <inheritdoc />
        OVRCustomSkeleton IGroundingData.ConstraintSkeleton => _skeleton;

        /// <inheritdoc />
        Animator IGroundingData.ConstraintAnimator => _animator;

        /// <inheritdoc />
        bool IGroundingData.ShouldUpdate
        {
            get => _shouldUpdate;
            set => _shouldUpdate = value;
        }

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

        /// <inheritdoc />
        Vector3 IGroundingData.PreviousKneePos
        {
            get => _prevKneePos;
            set => _prevKneePos = value;
        }

        /// <inheritdoc />
        RaycastHit IGroundingData.GroundRaycastHit
        {
            get => _groundRaycastHit;
            set => _groundRaycastHit = value;
        }

        /// <inheritdoc cref="IGroundingData.ConstraintSkeleton"/>
        [NotKeyable, SerializeField]
        [Tooltip(GroundingDataTooltips.Skeleton)]
        private OVRCustomSkeleton _skeleton;

        /// <inheritdoc cref="IGroundingData.ConstraintAnimator"/>
        [NotKeyable, SerializeField]
        [Tooltip(GroundingDataTooltips.Animator)]
        private Animator _animator;

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

        /// <inheritdoc cref="IGroundingData.Hips"/>
        [SyncSceneToStream, SerializeField]
        [Tooltip(GroundingDataTooltips.Hips)]
        private Transform _hips;

        private float _progress;
        private float _thresholdMoveProgress;
        private Vector3 _prevKneePos;
        private RaycastHit _groundRaycastHit;
        private bool _shouldUpdate;

        [NotKeyable, SerializeField, HideInInspector]
        private Vector3 _legPosOffset;
        [NotKeyable, SerializeField, HideInInspector]
        private Quaternion _legRotOffset;
        [NotKeyable, SerializeField, HideInInspector]
        private bool _computedOffsets;

        public bool ComputedOffsets => _computedOffsets;


        /// <summary>
        /// Assign the OVR Skeleton component.
        /// </summary>
        /// <param name="skeleton">The OVRSkeleton to be assigned.</param>
        public void AssignOVRSkeleton(OVRCustomSkeleton skeleton)
        {
            _skeleton = skeleton;
        }

        /// <summary>
        /// Assign the Animator component.
        /// </summary>
        /// <param name="animator">The Animator to be assigned.</param>
        public void AssignAnimator(Animator animator)
        {
            _animator = animator;
        }

        /// <summary>
        /// Assign the hips transform.
        /// </summary>
        /// <param name="hipsTransform">The hips transform to be assigned.</param>
        public void AssignHips(Transform hipsTransform)
        {
            _hips = hipsTransform;
        }

        /// <summary>
        /// Computes offsets necessary for initialization.
        /// </summary>
        public void ComputeOffsets()
        {
            if (_leg == null)
            {
                Debug.LogError("Please assign a leg transform before computing offsets.");
            }
            _legPosOffset = _leg.localPosition;
            _legRotOffset = _leg.localRotation;
            _computedOffsets = true;
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

            if (_skeleton == null && _animator == null)
            {
                return false;
            }

            if (_pair == null)
            {
                return false;
            }

            if (!_computedOffsets)
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
            _hips = null;
            _leg = null;
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
            _computedOffsets = false;
        }

        /// <inheritdoc />
        public bool IsBoneTransformsDataValid()
        {
            return (_skeleton != null && _skeleton.IsDataValid) ||
                (_animator != null);
        }
    }

    /// <summary>
    /// Grounding constraint.
    /// </summary>
    [DisallowMultipleComponent, AddComponentMenu("Movement Animation Rigging/Grounding Constraint")]
    public class GroundingConstraint : RigConstraint<
        GroundingJob,
        GroundingData,
        GroundingJobBinder<GroundingData>>,
        IOVRSkeletonConstraint
    {
        private void Start()
        {
            if (!data.ComputedOffsets)
            {
                Debug.LogError("Constraint needs to compute offsets before running!");
            }
        }

        /// <inheritdoc />
        public void RegenerateData()
        {
        }
    }
}
