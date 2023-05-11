// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace Oculus.Movement.Effects.Deprecated
{
    /// <summary>
    /// Grounding logic to ground the lower body and procedurally animate its movement.
    /// </summary>
    public class GroundingLogic : MonoBehaviour
    {
        /// <summary>
        /// Leg properties used for grounding logic.
        /// </summary>
        [System.Serializable]
        protected class LegProperties
        {
            /// <summary>
            /// The leg upper transform.
            /// </summary>
            public Transform LegTransform => _ikSolver.UpperTransform;

            /// <summary>
            /// The two-bone IK solver.
            /// </summary>
            public TwoBoneIK IKSolver => _ikSolver;

            /// <summary>
            /// The knee target for the leg.
            /// </summary>
            public Transform KneeTarget => _ikSolver.PoleTransform;

            /// <summary>
            /// The foot target for the leg.
            /// </summary>
            public Transform FootTarget => _ikSolver.TargetTransform;

            /// <summary>
            /// The speed of the step for the foot.
            /// </summary>
            public float StepSpeed => _stepSpeed;

            /// <summary>
            /// The distance before the step is triggered.
            /// </summary>
            public float StepDist => _stepDist;

            /// <summary>
            /// The offset from the floor.
            /// </summary>
            public float FloorOffset => _floorOffset;

            /// <summary>
            /// The initial rotation of the foot.
            /// </summary>
            public Quaternion InitialFootRotation { get; private set; }

            /// <summary>
            /// The initial offset of the legs to the root.
            /// </summary>
            public Vector3 InitialLegPositionOffset { get; private set; }

            /// <summary>
            /// The initial rotation offset of the legs to the root.
            /// </summary>
            public Quaternion InitialLegRotationOffset { get; private set; }

            /// <summary>
            /// The target position for the foot.
            /// </summary>
            public Vector3 TargetFootPos { get; set; }

            /// <summary>
            /// The previous position for the foot.
            /// </summary>
            public Vector3 PrevFootPos { get; set; }

            /// <summary>
            /// The previous position for the knee.
            /// </summary>
            public Vector3 PrevKneePos { get; set; }

            /// <summary>
            /// The current progress of the grounding movement.
            /// </summary>
            public float MoveProgress { get; set; }

            /// <summary>
            /// Returns the step height scaled by how much distance is currently being covered.
            /// </summary>
            /// <param name="dist">The distance to be traversed in the step.</param>
            /// <returns>The step height scaled by the distance to be traversed.</returns>
            public float ScaledStepHeight(float dist) => _stepHeight * Mathf.Clamp01(dist / _footHeightScaleOnDist);

            /// <summary>
            /// Returns the step height percentage based on the progress of the grounding movement.
            /// </summary>
            /// <param name="moveProgress">The current progress of the grounding movement.</param>
            /// <returns>The value of the step curve.</returns>
            public float StepCurveEvaluated(float moveProgress) => _stepCurve.Evaluate(moveProgress);

            /// <summary>
            /// The two-bone IK solver.
            /// </summary>
            [Header("Components")]
            [SerializeField]
            [Tooltip(GroundingLogicTooltips.LegPropertiesTooltips.IkSolver)]
            protected TwoBoneIK _ikSolver;

            /// <summary>
            /// The initial rotation offset for the feet.
            /// </summary>
            [SerializeField]
            [Tooltip(GroundingLogicTooltips.LegPropertiesTooltips.InitialRotationOffset)]
            protected Vector3 _initialRotationOffset;

            /// <summary>
            /// The distance before the step is triggered.
            /// </summary>
            [Header("Step Properties")]
            [SerializeField]
            [Tooltip(GroundingLogicTooltips.LegPropertiesTooltips.StepDist)]
            protected float _stepDist = 0.1f;

            /// <summary>
            /// The height of the step taken.
            /// </summary>
            [SerializeField]
            [Tooltip(GroundingLogicTooltips.LegPropertiesTooltips.StepHeight)]
            protected float _stepHeight = 0.05f;

            /// <summary>
            /// The speed of the step for the foot.
            /// </summary>
            [SerializeField]
            [Tooltip(GroundingLogicTooltips.LegPropertiesTooltips.StepSpeed)]
            protected float _stepSpeed = 1.0f;

            /// <summary>
            /// The height offset from the grounded floor to be applied to the foot.
            /// </summary>
            [SerializeField]
            [Tooltip(GroundingLogicTooltips.LegPropertiesTooltips.FloorOffset)]
            protected float _floorOffset = 0.05f;

            /// <summary>
            /// The maximum distance for the step height to not be scaled.
            /// </summary>
            [SerializeField]
            [Tooltip(GroundingLogicTooltips.LegPropertiesTooltips.FootHeightScaleOnDist)]
            protected float _footHeightScaleOnDist = 0.5f;

            /// <summary>
            /// The lower bound of the move progress before the other foot can take a step.
            /// </summary>
            [SerializeField, Range(0.0f, 1.0f)]
            [Tooltip(GroundingLogicTooltips.LegPropertiesTooltips.LowerThresholdMoveProgress)]
            protected float _lowerThresholdMoveProgress = 0.3f;

            /// <summary>
            /// The upper bound of the move progress before the other foot can take a step.
            /// </summary>
            [SerializeField, Range(0.0f, 1.0f)]
            [Tooltip(GroundingLogicTooltips.LegPropertiesTooltips.HigherThresholdMoveProgress)]
            protected float _higherThresholdMoveProgress = 0.6f;

            /// <summary>
            /// The animation curve for evaluating the step height value.
            /// </summary>
            [SerializeField]
            [Tooltip(GroundingLogicTooltips.LegPropertiesTooltips.StepCurve)]
            protected AnimationCurve _stepCurve;

            private float _thresholdMoveProgress;

            /// <summary>
            /// Setup initial offsets from the skeleton bone information.
            /// </summary>
            public void Setup()
            {
                InitialLegPositionOffset = LegTransform.localPosition;
                InitialLegRotationOffset = LegTransform.localRotation;
                InitialFootRotation = FootTarget.localRotation * Quaternion.Euler(_initialRotationOffset);
                GenerateThresholdMoveProgress();
            }

            /// <summary>
            /// Generates a new value for the threshold move progress.
            /// </summary>
            public void GenerateThresholdMoveProgress()
            {
                _thresholdMoveProgress = Random.Range(_lowerThresholdMoveProgress,
                    _higherThresholdMoveProgress);
            }

            /// <summary>
            /// Returns true if the move progress is less than 1.
            /// </summary>
            /// <returns>True if moving the foot.</returns>
            public bool IsMoving()
            {
                return MoveProgress < 1.0f;
            }

            /// <summary>
            /// Returns true if the move progress is lower than the threshold move progress.
            /// </summary>
            /// <returns>True if the other foot can be moved.</returns>
            public bool CanMoveOtherFoot()
            {
                return MoveProgress >= _thresholdMoveProgress;
            }
        }

        /// <summary>
        /// The OVR Skeleton component.
        /// </summary>
        [SerializeField]
        [Tooltip(GroundingLogicTooltips.Skeleton)]
        protected OVRCustomSkeleton _skeleton;

        /// <summary>
        /// The hips target transform.
        /// </summary>
        [SerializeField]
        [Tooltip(GroundingLogicTooltips.HipsTarget)]
        protected Transform _hipsTarget;

        /// <summary>
        /// The layers that the raycast will check against for grounding.
        /// </summary>
        [SerializeField]
        [Tooltip(GroundingLogicTooltips.GroundingLayers)]
        protected LayerMask _groundLayers;

        /// <summary>
        /// The maximum distance that the raycast will go when checking for grounding.
        /// </summary>
        [SerializeField]
        [Tooltip(GroundingLogicTooltips.GroundRaycastDist)]
        protected float _groundRaycastDist = 10;

        /// <summary>
        /// The leg properties for the left leg.
        /// </summary>
        [SerializeField]
        [Tooltip(GroundingLogicTooltips.LeftLegProperties)]
        protected LegProperties _leftLeg;

        /// <summary>
        /// The leg properties for the right leg.
        /// </summary>
        [SerializeField]
        [Tooltip(GroundingLogicTooltips.RightLegProperties)]
        protected LegProperties _rightLeg;

        private Transform _hipsTransform => _skeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_Hips];
        private bool _initialized;

        private void Start()
        {
            _leftLeg.Setup();
            _rightLeg.Setup();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_leftLeg.PrevKneePos, 0.03f);
            Gizmos.DrawSphere(_leftLeg.PrevFootPos, 0.03f);
            Gizmos.DrawSphere(_rightLeg.PrevKneePos, 0.03f);
            Gizmos.DrawSphere(_rightLeg.PrevFootPos, 0.03f);

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(_leftLeg.KneeTarget.position, 0.03f);
            Gizmos.DrawSphere(_leftLeg.TargetFootPos, 0.03f);
            Gizmos.DrawSphere(_rightLeg.KneeTarget.position, 0.03f);
            Gizmos.DrawSphere(_rightLeg.TargetFootPos, 0.03f);
        }

        /// <summary>
        /// Initializes the grounding logic properties.
        /// </summary>
        public void Setup()
        {
            Initialize(_leftLeg);
            Initialize(_rightLeg);
            _initialized = true;
        }

        /// <summary>
        /// Apply grounding logic.
        /// </summary>
        public void ApplyGrounding()
        {
            if (_skeleton.IsInitialized)
            {
                // Since the leg transforms aren't updated when unparented from
                // the hips, we need to manually update them.
                if (_leftLeg.LegTransform.parent != _hipsTransform.parent)
                {
                    _leftLeg.LegTransform.SetParent(_hipsTransform.parent);
                }
                if (_rightLeg.LegTransform.parent != _hipsTransform.parent)
                {
                    _rightLeg.LegTransform.SetParent(_hipsTransform.parent);
                }
                UpdateLegPos(_leftLeg);
                UpdateLegPos(_rightLeg);
            }

            if (!_initialized)
            {
                return;
            }

            UpdateFootMovement(_leftLeg, _rightLeg);
            UpdateFootMovement(_rightLeg, _leftLeg);
            UpdateFootRotation(_leftLeg);
            UpdateFootRotation(_rightLeg);
        }

        private void Initialize(LegProperties legProperties)
        {
            UpdateGroundPos(legProperties);
            legProperties.PrevKneePos = legProperties.KneeTarget.position;
            legProperties.PrevFootPos = legProperties.TargetFootPos;
            legProperties.FootTarget.position = legProperties.TargetFootPos;
            legProperties.MoveProgress = 1;
            legProperties.IKSolver.enabled = true;
        }

        private void UpdateFootMovement(LegProperties mainLeg, LegProperties otherLeg)
        {
            if (mainLeg.IsMoving())
            {
                UpdateGroundPos(mainLeg);
                UpdateFootPos(mainLeg);
            }
            else if (otherLeg.CanMoveOtherFoot() && ShouldStartGrounding(mainLeg))
            {
                StartGrounding(mainLeg);
            }
        }

        private bool ShouldStartGrounding(LegProperties legProperties)
        {
            return Vector3.Distance(legProperties.PrevKneePos, legProperties.KneeTarget.position) >
                   legProperties.StepDist;
        }

        private void StartGrounding(LegProperties legProperties)
        {
            legProperties.MoveProgress = 0f;
            legProperties.PrevFootPos = legProperties.FootTarget.position;
            legProperties.PrevKneePos = legProperties.KneeTarget.position;
            legProperties.GenerateThresholdMoveProgress();
        }

        private void UpdateLegPos(LegProperties legProperties)
        {
            var hipsLocalRotation = _hipsTransform.localRotation;
            legProperties.LegTransform.localPosition = _hipsTransform.localPosition + hipsLocalRotation * legProperties.InitialLegPositionOffset;
            legProperties.LegTransform.localRotation = hipsLocalRotation * legProperties.InitialLegRotationOffset;
        }

        private void UpdateGroundPos(LegProperties legProperties)
        {
            if (Physics.Raycast(legProperties.KneeTarget.position,
                    Vector3.down, out var raycastHit, _groundRaycastDist, _groundLayers))
            {
                var pos = raycastHit.point + Vector3.up * legProperties.FloorOffset;
                legProperties.TargetFootPos = pos;
            }
        }

        private void UpdateFootPos(LegProperties legProperties)
        {
            var footPos = Vector3.Lerp(legProperties.PrevFootPos, legProperties.TargetFootPos,
                legProperties.MoveProgress);
            float dist = Vector3.Distance(legProperties.TargetFootPos, legProperties.PrevFootPos);
            footPos.y += legProperties.ScaledStepHeight(dist) *
                         legProperties.StepCurveEvaluated(legProperties.MoveProgress);

            legProperties.FootTarget.position = footPos;
            legProperties.MoveProgress += Time.deltaTime * legProperties.StepSpeed;
        }

        private void UpdateFootRotation(LegProperties legProperties)
        {
            var dir = _hipsTarget.position - legProperties.KneeTarget.position;
            var lookRot = Quaternion.LookRotation(dir);
            legProperties.FootTarget.localRotation = Quaternion.Inverse(lookRot) *
                                                     legProperties.InitialFootRotation;
        }

    }
}
