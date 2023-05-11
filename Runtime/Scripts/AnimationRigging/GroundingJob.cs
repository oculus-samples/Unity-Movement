// Copyright (c) Meta Platforms, Inc. and affiliates.

using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// The Grounding job.
    /// </summary>
    [Unity.Burst.BurstCompile]
    public struct GroundingJob : IWeightedAnimationJob
    {
        /// <summary>
        /// The hips target transform.
        /// </summary>
        public ReadOnlyTransformHandle HipsTarget;

        /// <summary>
        /// The knee target transform.
        /// </summary>
        public ReadOnlyTransformHandle KneeTarget;

        /// <summary>
        /// The foot target transform.
        /// </summary>
        public ReadWriteTransformHandle FootTarget;

        /// <summary>
        /// The hips transform.
        /// </summary>
        public ReadOnlyTransformHandle Hips;

        /// <summary>
        /// The leg transform.
        /// </summary>
        public ReadWriteTransformHandle Leg;

        /// <summary>
        /// The array containing 1 element for the target position for the foot.
        /// </summary>
        public NativeArray<Vector3> TargetFootPos;

        /// <summary>
        /// The array containing 1 element if the foot should be grounded.
        /// </summary>
        public NativeArray<bool> IsGrounding;

        /// <summary>
        /// The array containing 1 element if the foot should be moving.
        /// </summary>
        public NativeArray<bool> IsMovable;

        /// <summary>
        /// The array containing 1 element for the current move progress.
        /// </summary>
        public NativeArray<float> MoveProgress;

        /// <summary>
        /// The array containing 1 element for the current step progress.
        /// </summary>
        public NativeArray<float> StepProgress;

        /// <summary>
        /// The array containing 1 element for the current delta time.
        /// </summary>
        public NativeArray<float> DeltaTime;

        /// <summary>
        /// The leg position offset from the parent.
        /// </summary>
        public Vector3 LegPosOffset;

        /// <summary>
        /// The leg rotation offset from the parent.
        /// </summary>
        public Quaternion LegRotOffset;

        /// <inheritdoc cref="IGroundingData.FootRotationOffset"/>
        public Quaternion FootRotationOffset;

        /// <inheritdoc cref="IGroundingData.StepHeight"/>
        public float StepHeight;

        /// <inheritdoc cref="IGroundingData.StepHeightScaleDist"/>
        public float StepHeightScaleDist;

        private Vector3 _prevFootPos;

        /// <inheritdoc />
        public FloatProperty jobWeight { get; set; }

        /// <inheritdoc />
        public void ProcessRootMotion(AnimationStream stream) { }

        /// <inheritdoc />
        public void ProcessAnimation(AnimationStream stream)
        {
            float weight = jobWeight.Get(stream);
            if (weight > 0f && DeltaTime[0] > 0f)
            {
                // Leg position + rotation.
                var hipsLocalRotation = Hips.GetLocalRotation(stream);
                var hipsLocalPosition = Hips.GetLocalPosition(stream);
                Leg.SetLocalPosition(stream, hipsLocalPosition + hipsLocalRotation * LegPosOffset);
                Leg.SetLocalRotation(stream, hipsLocalRotation * LegRotOffset);

                // Foot position.
                if (IsGrounding[0])
                {
                    var footPos = Vector3.Lerp(_prevFootPos, TargetFootPos[0], MoveProgress[0]);
                    float footDist = Vector3.Distance(_prevFootPos, TargetFootPos[0]);
                    footPos.y += StepHeight * Mathf.Clamp01(footDist / StepHeightScaleDist) * StepProgress[0];
                    FootTarget.SetPosition(stream, footPos);
                }

                // Record foot position.
                if (IsMovable[0])
                {
                    _prevFootPos = FootTarget.GetPosition(stream);
                }

                // Foot rotation.
                var lookRot = Quaternion.LookRotation(HipsTarget.GetLocalPosition(stream) - KneeTarget.GetLocalPosition(stream)) *
                                        FootRotationOffset;
                FootTarget.SetRotation(stream, lookRot);
            }
            else
            {
                AnimationRuntimeUtils.PassThrough(stream, Leg);
                AnimationRuntimeUtils.PassThrough(stream, FootTarget);
            }
        }
    }

    /// <summary>
    /// The job binder for <see cref="GroundingJob"/>.
    /// </summary>
    /// <typeparam name="T">The constraint data type, should be <see cref="GroundingJob"/>.</typeparam>
    public class GroundingJobBinder<T> : AnimationJobBinder<GroundingJob, T>
        where T : struct, IAnimationJobData, IGroundingData
    {
        private RaycastHit _groundRaycastHit;
        private Vector3 _prevKneePos;
        private bool _shouldUpdate;

        /// <inheritdoc />
        public override GroundingJob Create(Animator animator, ref T data, Component component)
        {
            var job = new GroundingJob();

            data.Create();
            data.GenerateThresholdMoveProgress();
            _prevKneePos = data.KneeTarget.position;

            job.HipsTarget = ReadOnlyTransformHandle.Bind(animator, data.HipsTarget);
            job.KneeTarget = ReadOnlyTransformHandle.Bind(animator, data.KneeTarget);
            job.FootTarget = ReadWriteTransformHandle.Bind(animator, data.FootTarget);
            job.Hips = ReadOnlyTransformHandle.Bind(animator, data.Hips);
            job.Leg = ReadWriteTransformHandle.Bind(animator, data.Leg);

            job.TargetFootPos = new NativeArray<Vector3>(1, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.MoveProgress = new NativeArray<float>(1, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.StepProgress = new NativeArray<float>(1, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.IsGrounding = new NativeArray<bool>(1, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.IsMovable = new NativeArray<bool>(1, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.DeltaTime = new NativeArray<float>(1, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);

            job.FootRotationOffset = Quaternion.Euler(data.FootRotationOffset);
            job.StepHeight = data.StepHeight;
            job.StepHeightScaleDist = data.StepHeightScaleDist;
            job.LegPosOffset = data.LegPosOffset;
            job.LegRotOffset = data.LegRotOffset;

            job.IsGrounding[0] = false;
            job.IsMovable[0] = true;
            job.MoveProgress[0] = 1.0f;
            job.StepProgress[0] = 1.0f;
            job.TargetFootPos[0] = data.FootTarget.position;
            job.DeltaTime[0] = Time.deltaTime;
            return job;
        }

        /// <inheritdoc />
        public override void Update(GroundingJob job, ref T data)
        {
            if (data.ConstraintSkeleton.IsDataValid)
            {
                _shouldUpdate = true;
            }

            var kneePos = data.KneeTarget.position;
            job.IsMovable[0] = false;
            job.IsGrounding[0] = false;
            data.Progress = job.MoveProgress[0];

            if (job.MoveProgress[0] < 1.0f)
            {
                job.IsGrounding[0] = true;
                job.MoveProgress[0] += Time.deltaTime * data.StepSpeed;
                job.StepProgress[0] = data.StepCurve.Evaluate(job.MoveProgress[0]);
                if (Physics.Raycast(kneePos, Vector3.down, out _groundRaycastHit, data.GroundRaycastDist,
                        data.GroundLayers))
                {
                    job.TargetFootPos[0] = _groundRaycastHit.point + Vector3.up * data.GroundOffset;
                }
            }
            else
            {
                if (!data.Pair.IsValid() || (data.Pair.FinishedMoving() &&
                    Vector3.Distance(_prevKneePos, kneePos) > data.StepDist))
                {
                    _prevKneePos = kneePos;
                    job.IsMovable[0] = true;
                    job.MoveProgress[0] = 0.0f;
                    data.Progress = 0.0f;
                    data.GenerateThresholdMoveProgress();
                }
            }

            job.DeltaTime[0] = _shouldUpdate ? Time.deltaTime : 0.0f;
            base.Update(job, ref data);

            if (!data.ConstraintSkeleton.IsDataValid)
            {
                _shouldUpdate = false;
            }
        }

        /// <inheritdoc />
        public override void Destroy(GroundingJob job)
        {
            job.TargetFootPos.Dispose();
            job.MoveProgress.Dispose();
            job.StepProgress.Dispose();
            job.IsGrounding.Dispose();
            job.IsMovable.Dispose();
            job.DeltaTime.Dispose();
        }
    }
}
