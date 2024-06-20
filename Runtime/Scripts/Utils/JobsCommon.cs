// Copyright (c) Meta Platforms, Inc. and affiliates.

using Unity.Collections;
using UnityEngine;
using UnityEngine.Jobs;

namespace Oculus.Movement.Utils
{
    public class JobCommons
    {
        /// <summary>
        /// Job used to quickly store poses from transforms.
        /// </summary>
        [Unity.Burst.BurstCompile]
        public struct GetPosesJob : IJobParallelForTransform
        {
            /// <summary>
            /// Poses to write to.
            /// </summary>
            [WriteOnly]
            public NativeArray<Pose> Poses;

            /// <inheritdoc cref="IJobParallelForTransform.Execute(int, TransformAccess)"/>
            [Unity.Burst.BurstCompile]
            public void Execute(int index, TransformAccess transform)
            {
                Poses[index] = new Pose(transform.position, transform.rotation);
            }
        }

        /// <summary>
        /// Job used to apply stored poses to transforms.
        /// </summary>
        [Unity.Burst.BurstCompile]
        public struct WritePosesToTransformsJob : IJobParallelForTransform
        {
            /// <summary>
            /// Poses to read from.
            /// </summary>
            [ReadOnly]
            public NativeArray<Pose> SourcePoses;

            /// <inheritdoc cref="IJobParallelForTransform.Execute(int, TransformAccess)"/>
            [Unity.Burst.BurstCompile]
            public void Execute(int index, TransformAccess transform)
            {
                var sourcePose = SourcePoses[index];
                transform.SetPositionAndRotation(sourcePose.position, sourcePose.rotation);
            }
        }
    }
}
