// Copyright (c) Meta Platforms, Inc. and affiliates.

using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// The TwistDistribution job.
    /// </summary>
    [Unity.Burst.BurstCompile]
    public struct TwistDistributionJob : IWeightedAnimationJob
    {
        /// <summary>
        /// The transform handle for the twist source.
        /// </summary>
        public ReadOnlyTransformHandle TwistSource;

        /// <summary>
        /// The twist axis offset.
        /// </summary>
        public Quaternion TwistAxisOffset;

        /// <summary>
        /// List of Transform handles for the twist nodes.
        /// </summary>
        public NativeArray<ReadWriteTransformHandle> TwistTransforms;

        /// <summary>
        /// List of weights for the twist nodes.
        /// </summary>
        public NativeArray<PropertyStreamHandle> TwistWeights;

        /// <summary>
        /// List of cached local rotation for twist nodes.
        /// </summary>
        public NativeArray<Quaternion> TwistBindRotations;

        /// <summary>
        /// List of initial up for twist nodes.
        /// </summary>
        public NativeArray<Vector3> TwistUpDirections;

        /// <summary>
        /// List of segment up for twist nodes.
        /// </summary>
        public NativeArray<Vector3> SegmentUpAxis;

        /// <summary>
        /// Buffer used to store segment direction during job execution.
        /// </summary>
        public NativeArray<Vector3> SegmentDirections;

        /// <summary>
        /// Buffer used to store spacing positions job execution.
        /// </summary>
        public NativeArray<Vector3> SpacingPositions;

        /// <summary>
        /// Buffer used to store weights during job execution.
        /// </summary>
        public NativeArray<float> WeightBuffer;

        /// <summary>
        /// Buffer used to store spacing during job execution.
        /// </summary>
        public NativeArray<float> SpacingBuffer;

        /// <summary>
        /// The array containing 1 element for the current delta time.
        /// </summary>
        public NativeArray<float> DeltaTime;

        /// <summary>
        /// Job weight.
        /// </summary>
        public FloatProperty jobWeight { get; set; }

        /// <summary>
        /// Defines what to do when processing the root motion.
        /// </summary>
        /// <param name="stream">The animation stream to work on.</param>
        public void ProcessRootMotion(AnimationStream stream) { }

        /// <summary>
        /// Defines what to do when processing the animation.
        /// </summary>
        /// <param name="stream">The animation stream to work on.</param>
        public void ProcessAnimation(AnimationStream stream)
        {
            float weight = jobWeight.Get(stream);
            if (weight > 0f && DeltaTime[0] > 0f)
            {
                if (TwistTransforms.Length == 0)
                {
                    return;
                }

                AnimationStreamHandleUtility.ReadFloats(stream, TwistWeights, WeightBuffer);
                var parentRotation = TwistSource.GetLocalRotation(stream);
                var inverseParentRotation = Quaternion.Inverse(parentRotation);
                for (int i = 0; i < TwistTransforms.Length; i++)
                {
                    ReadWriteTransformHandle twistTransform = TwistTransforms[i];

                    // Apply spacing
                    twistTransform.SetPosition(stream,
                        Vector3.Lerp(twistTransform.GetPosition(stream), SpacingPositions[i], weight));

                    // Apply twist
                    Quaternion twistRotation = Quaternion.LookRotation(SegmentDirections[0], SegmentUpAxis[i]);
                    Quaternion targetRotation = Quaternion.Slerp(
                        parentRotation * TwistBindRotations[i],
                        twistRotation * TwistAxisOffset,
                        WeightBuffer[i] * weight);
                    twistTransform.SetLocalRotation(stream,
                        Quaternion.Slerp(twistTransform.GetLocalRotation(stream),
                            inverseParentRotation * targetRotation, weight));
                    TwistTransforms[i] = twistTransform;
                }
            }
            else
            {
                for (int i = 0; i < TwistTransforms.Length; ++i)
                {
                    AnimationRuntimeUtils.PassThrough(stream, TwistTransforms[i]);
                }
            }
        }
    }

    /// <summary>
    /// The TwistDistribution job binder.
    /// </summary>
    /// <typeparam name="T">The constraint data type</typeparam>
    public class TwistDistributionJobBinder<T> : AnimationJobBinder<TwistDistributionJob, T>
        where T : struct, IAnimationJobData, ITwistDistributionData
    {
        /// <inheritdoc cref="IAnimationJobBinder.Create"/>
        public override TwistDistributionJob Create(Animator animator, ref T data, Component component)
        {
            var job = new TwistDistributionJob();

            var twistNodes = data.TwistNodes;
            WeightedTransformArrayBinder.BindReadWriteTransforms(animator, component, twistNodes, out job.TwistTransforms);
            WeightedTransformArrayBinder.BindWeights(animator, component, twistNodes, data.TwistNodesProperty, out job.TwistWeights);

            job.TwistAxisOffset = Quaternion.Inverse(Quaternion.LookRotation(data.TwistForwardAxis, data.TwistUpAxis));
            job.TwistSource = ReadOnlyTransformHandle.Bind(animator, data.SegmentStart);

            job.TwistBindRotations = new NativeArray<Quaternion>(twistNodes.Count, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.TwistUpDirections = new NativeArray<Vector3>(twistNodes.Count, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.SegmentUpAxis = new NativeArray<Vector3>(twistNodes.Count, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.SegmentDirections = new NativeArray<Vector3>(1, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.SpacingPositions = new NativeArray<Vector3>(twistNodes.Count, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.WeightBuffer = new NativeArray<float>(twistNodes.Count, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.SpacingBuffer = new NativeArray<float>(twistNodes.Count, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.DeltaTime = new NativeArray<float>(1, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);

            for (int i = 0; i < twistNodes.Count; ++i)
            {
                var sourceTransform = twistNodes[i].transform;
                job.TwistBindRotations[i] = sourceTransform.localRotation;
                job.TwistUpDirections[i] = data.TwistNodeUps[i];
                job.SpacingBuffer[i] = data.TwistNodeSpacings[i];
            }

            return job;
        }

        /// <summary>
        /// Update job with component information.
        /// </summary>
        /// <param name="job"></param>
        /// <param name="data"></param>
        public override void Update(TwistDistributionJob job, ref T data)
        {
            if (data.IsBoneTransformsDataValid())
            {
                data.ShouldUpdate = true;
            }

            WeightedTransformArray twistNodes = data.TwistNodes;
            for (int i = 0; i < twistNodes.Count; i++)
            {
                var segmentUp = data.SegmentUp.TransformVector(job.TwistUpDirections[i]);
                var lookDirectionDot = Vector3.Dot(job.SegmentDirections[0], segmentUp);
                // Don't apply twists if the look directions are zero or the look directions are parallel.
                if (job.SegmentDirections[0].sqrMagnitude <= Mathf.Epsilon ||
                    segmentUp.sqrMagnitude <= Mathf.Epsilon ||
                    lookDirectionDot >= 1 - Mathf.Epsilon ||
                    lookDirectionDot <= -1 + Mathf.Epsilon)
                {
                    continue;
                }
                job.SegmentUpAxis[i] = segmentUp;
            }

            var segmentEndPos = data.SegmentEnd.position;
            var segmentStartPos = data.SegmentStart.position;
            for (int i = 0; i < twistNodes.Count; i++)
            {
                job.SpacingPositions[i] = segmentStartPos + (segmentEndPos - segmentStartPos) * job.SpacingBuffer[i];
            }
            var segmentDirection = (segmentEndPos - segmentStartPos) * 2f;
            job.SegmentDirections[0] = segmentDirection;

            job.DeltaTime[0] = data.ShouldUpdate ? Time.unscaledDeltaTime : 0.0f;
            base.Update(job, ref data);

            if (!data.IsBoneTransformsDataValid())
            {
                data.ShouldUpdate = false;
            }
        }

        /// <summary>
        /// Destroy the job and clean up arrays.
        /// </summary>
        /// <param name="job">The job that is being destroyed.</param>
        public override void Destroy(TwistDistributionJob job)
        {
            job.TwistTransforms.Dispose();
            job.TwistWeights.Dispose();
            job.TwistBindRotations.Dispose();
            job.TwistUpDirections.Dispose();
            job.SegmentUpAxis.Dispose();
            job.SegmentDirections.Dispose();
            job.SpacingPositions.Dispose();
            job.WeightBuffer.Dispose();
            job.SpacingBuffer.Dispose();
            job.DeltaTime.Dispose();
        }
    }
}
