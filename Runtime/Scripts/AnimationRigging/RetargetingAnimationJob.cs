// Copyright (c) Meta Platforms, Inc. and affiliates.

using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// Retargeting animation job.
    /// </summary>
    [Unity.Burst.BurstCompile]
    public struct RetargetingAnimationJob : IWeightedAnimationJob
    {
        /// <summary>
        /// Source transforms used to affect target during retargeting.
        /// </summary>
        public NativeArray<ReadOnlyTransformHandle> SourceTransforms;

        /// <summary>
        /// Target transforms to affect.
        /// </summary>
        public NativeArray<ReadWriteTransformHandle> TargetTransforms;

        /// <summary>
        /// Boolean indicating if position should be updated or not.
        /// </summary>
        public NativeArray<bool> ShouldUpdatePosition;

        /// <summary>
        /// Boolean indicating if rotation should be updated or not.
        /// </summary>
        public NativeArray<bool> ShouldUpdateRotation;

        /// <summary>
        /// Rotation offsets per joint.
        /// </summary>
        public NativeArray<Quaternion> RotationOffsets;

        /// <summary>
        /// Rotation adjustments per joint.
        /// </summary>
        public NativeArray<Quaternion> RotationAdjustments;

        /// <inheritdoc />
        public FloatProperty jobWeight { get; set; }

        /// <inheritdoc />
        public void ProcessRootMotion(AnimationStream stream)
        {
            float weight = jobWeight.Get(stream);
            // Update hips for root motion.
            if (weight > 0f)
            {
                UpdateTargetTransform(stream, 0, weight);
            }
            else
            {
                AnimationRuntimeUtils.PassThrough(stream, TargetTransforms[0]);
            }
        }

        /// <inheritdoc />
        public void ProcessAnimation(AnimationStream stream)
        {
            float weight = jobWeight.Get(stream);
            if (weight > 0f)
            {
                for (int i = 0; i < TargetTransforms.Length; ++i)
                {
                    UpdateTargetTransform(stream, i, weight);
                }
            }
            else
            {
                for (int i = 0; i < TargetTransforms.Length; ++i)
                {
                    AnimationRuntimeUtils.PassThrough(stream, TargetTransforms[i]);
                }
            }
        }

        private void UpdateTargetTransform(AnimationStream stream, int i, float weight)
        {
            var targetTransform = TargetTransforms[i];
            var sourceTransform = SourceTransforms[i];

            if (ShouldUpdateRotation[i])
            {
                var rotationOffset = RotationOffsets[i];
                var rotationAdjustment = RotationAdjustments[i];
                var originalRotation = sourceTransform.GetRotation(stream);
                var finalRotation = originalRotation * rotationOffset * rotationAdjustment;
                targetTransform.SetRotation(stream,
                   Quaternion.Slerp(targetTransform.GetRotation(stream), finalRotation, weight));
            }

            if (ShouldUpdatePosition[i])
            {
                var originalPosition = targetTransform.GetPosition(stream);
                var finalPosition = sourceTransform.GetPosition(stream);
                targetTransform.SetPosition(stream,
                    Vector3.Lerp(originalPosition, finalPosition, weight));
            }

            // update handles with binding info
            SourceTransforms[i] = sourceTransform;
            TargetTransforms[i] = targetTransform;
        }
    }

    /// <summary>
    /// The retargeting animation job binder.
    /// </summary>
    /// <typeparam name="T">The constraint data type.</typeparam>
    public class RetargetingAnimationJobBinder<T> : AnimationJobBinder<RetargetingAnimationJob, T>
    where T : struct, IAnimationJobData, IRetargetingData
    {
        /// <inheritdoc />
        public override RetargetingAnimationJob Create(Animator animator, ref T data, Component component)
        {
            var retargetingAnimationJob = new RetargetingAnimationJob();

            if (!data.HasDataInitialized())
            {
                SetupInvalidJob(animator, ref retargetingAnimationJob, ref data);
            }
            else
            {
                AllocateJobNativeArrays(animator, ref retargetingAnimationJob, ref data);

                BindTransforms(animator, retargetingAnimationJob, ref data);
                SyncMetaDataInformationToJob(retargetingAnimationJob, ref data);
            }

            return retargetingAnimationJob;
        }

        private void SetupInvalidJob(Animator animator, ref RetargetingAnimationJob job, ref T data)
        {
            job.SourceTransforms =
                    new NativeArray<ReadOnlyTransformHandle>(1,
                        Allocator.Persistent,
                        NativeArrayOptions.UninitializedMemory);
            job.TargetTransforms =
                new NativeArray<ReadWriteTransformHandle>(1,
                    Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory);
            job.ShouldUpdatePosition =
                new NativeArray<bool>(1,
                    Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory);
            job.ShouldUpdateRotation =
                new NativeArray<bool>(1,
                    Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory);
            job.RotationOffsets =
                new NativeArray<Quaternion>(1,
                    Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory);

            job.RotationAdjustments =
                new NativeArray<Quaternion>(1,
                    Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory);

            job.SourceTransforms[0] =
                ReadOnlyTransformHandle.Bind(animator, data.DummyTransform);
            job.TargetTransforms[0] =
                ReadWriteTransformHandle.Bind(animator, data.DummyTransform);
            job.ShouldUpdatePosition[0] = false;
            job.ShouldUpdateRotation[0] = false;
            job.RotationOffsets[0] = Quaternion.identity;
            job.RotationAdjustments[0] = Quaternion.identity;
        }

        private void AllocateJobNativeArrays(Animator animator, ref RetargetingAnimationJob job, ref T data)
        {
            job.SourceTransforms =
                new NativeArray<ReadOnlyTransformHandle>(data.SourceTransforms.Length,
                    Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory);
            job.TargetTransforms =
                new NativeArray<ReadWriteTransformHandle>(data.TargetTransforms.Length,
                    Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory);
            job.ShouldUpdatePosition =
                new NativeArray<bool>(data.ShouldUpdatePosition.Length,
                    Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory);
            job.ShouldUpdateRotation =
                new NativeArray<bool>(data.ShouldUpdateRotation.Length,
                    Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory);
            job.RotationOffsets =
                new NativeArray<Quaternion>(data.RotationOffsets.Length,
                    Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory);

            job.RotationAdjustments =
                new NativeArray<Quaternion>(data.RotationAdjustments.Length,
                    Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory);
        }

        private void BindTransforms(Animator animator, RetargetingAnimationJob job, ref T data)
        {
            var numSourceTransforms = job.SourceTransforms.Length;
            for (int i = 0; i < numSourceTransforms; i++)
            {
                job.SourceTransforms[i] =
                    ReadOnlyTransformHandle.Bind(animator, data.SourceTransforms[i]);
            }

            var numTargetTransforms = job.TargetTransforms.Length;
            for (int i = 0; i < numTargetTransforms; i++)
            {
                job.TargetTransforms[i] =
                    ReadWriteTransformHandle.Bind(animator, data.TargetTransforms[i]);
            }
        }

        private void SyncMetaDataInformationToJob(RetargetingAnimationJob job, ref T data)
        {
            int numItems = job.RotationOffsets.Length;
            for (int i = 0; i < numItems; i++)
            {
                job.RotationOffsets[i] = data.RotationOffsets[i];
                job.ShouldUpdatePosition[i] = data.ShouldUpdatePosition[i];
                job.ShouldUpdateRotation[i] = data.ShouldUpdateRotation[i];
                job.RotationAdjustments[i] = data.RotationAdjustments[i];
            }
        }

        /// <inheritdoc />
        public override void Update(RetargetingAnimationJob job, ref T data)
        {
            base.Update(job, ref data);
            SyncMetaDataInformationToJob(job, ref data);
        }

        /// <inheritdoc />
        public override void Destroy(RetargetingAnimationJob job)
        {
            job.SourceTransforms.Dispose();
            job.TargetTransforms.Dispose();
            job.ShouldUpdatePosition.Dispose();
            job.ShouldUpdateRotation.Dispose();
            job.RotationOffsets.Dispose();
            job.RotationAdjustments.Dispose();
        }
    }
}
