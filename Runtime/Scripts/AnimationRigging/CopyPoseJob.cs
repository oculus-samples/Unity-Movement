// Copyright (c) Meta Platforms, Inc. and affiliates.

using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// The CopyPose job.
    /// </summary>
    [Unity.Burst.BurstCompile]
    public struct CopyPoseJob : IWeightedAnimationJob
    {
        /// <summary>
        /// The array of bone positions to be read.
        /// </summary>
        public NativeArray<ReadOnlyTransformHandle> Bones;

        /// <summary>
        /// The stored positions of read bones.
        /// </summary>
        public NativeArray<Vector3> Positions;

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
            if (weight > 0)
            {
                for (int i = 0; i < Bones.Length; i++)
                {
                    var bone = Bones[i];
                    if (!bone.IsValid(stream))
                    {
                        continue;
                    }
                    Positions[i] = bone.GetPosition(stream);
                    Bones[i] = bone;
                }
            }
        }
    }

    /// <summary>
    /// The CopyPose job binder.
    /// </summary>
    /// <typeparam name="T">The constraint data type</typeparam>
    public class CopyPoseJobBinder<T> : AnimationJobBinder<CopyPoseJob, T>
        where T : struct, IAnimationJobData, ICopyPoseData
    {
        /// <inheritdoc cref="IAnimationJobBinder.Create"/>
        public override CopyPoseJob Create(Animator animator, ref T data, Component component)
        {
            var job = new CopyPoseJob();
            job.Bones = new NativeArray<ReadOnlyTransformHandle>(data.AnimatorBones.Length, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.Positions = new NativeArray<Vector3>(data.AnimatorBones.Length, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < data.AnimatorBones.Length; i++)
            {
                job.Positions[i] = Vector3.zero;
                if (data.AnimatorBones[i] != null)
                {
                    job.Bones[i] = ReadOnlyTransformHandle.Bind(animator, data.AnimatorBones[i]);
                }
            }

            return job;
        }

        /// <summary>
        /// Update job with component information.
        /// </summary>
        /// <param name="job"></param>
        /// <param name="data"></param>
        public override void Update(CopyPoseJob job, ref T data)
        {
            base.Update(job, ref data);
            if (data.RetargetingLayerComp.JointPositionAdjustments == null)
            {
                return;
            }
            for (var i = HumanBodyBones.Hips; i < HumanBodyBones.LastBone; i++)
            {
                var bodyAdjustment = data.RetargetingLayerComp.JointPositionAdjustments[(int)i];
                if (bodyAdjustment.Joint == i)
                {
                    var bonePosition = job.Positions[(int)i];
                    if (data.CopyPoseToOriginal)
                    {
                        bodyAdjustment.OriginalPosition = bonePosition;
                    }
                    else
                    {
                        bodyAdjustment.FinalPosition = bonePosition;
                    }
                }
            }
        }

        /// <summary>
        /// Destroy the job and clean up arrays.
        /// </summary>
        /// <param name="job">The job that is being destroyed.</param>
        public override void Destroy(CopyPoseJob job)
        {
            job.Bones.Dispose();
            job.Positions.Dispose();
        }
    }
}
