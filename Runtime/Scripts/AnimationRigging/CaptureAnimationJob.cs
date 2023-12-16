// Copyright (c) Meta Platforms, Inc. and affiliates.

using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// The capture animation job, which will write bone local position and rotation data
    /// into accessible native arrays.
    /// </summary>
    [Unity.Burst.BurstCompile]
    public struct CaptureAnimationJob : IWeightedAnimationJob
    {
        /// <summary>
        /// The array of read write transform handles for animator bones.
        /// </summary>
        public NativeArray<ReadWriteTransformHandle> Bones;

        /// <summary>
        /// The array of bone local positions to write to.
        /// </summary>
        public NativeArray<Vector3> BoneLocalPositions;

        /// <summary>
        /// The array of bone local rotations to write to.
        /// </summary>
        public NativeArray<Quaternion> BoneLocalRotations;

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
                    if (!Bones[i].IsValid(stream))
                    {
                        continue;
                    }

                    var bonePosition = Bones[i].GetLocalPosition(stream);
                    var boneRotation = Bones[i].GetLocalRotation(stream);
                    BoneLocalPositions[i] = bonePosition;
                    BoneLocalRotations[i] = boneRotation;
                }
            }
            else
            {
                for (int i = 0; i < Bones.Length; i++)
                {
                    if (!Bones[i].IsValid(stream))
                    {
                        continue;
                    }
                    AnimationRuntimeUtils.PassThrough(stream, Bones[i]);
                }
            }
        }
    }

    /// <summary>
    /// The capture animation job binder.
    /// </summary>
    /// <typeparam name="T">Type to be used for the job.</typeparam>
    public class CaptureAnimationJobBinder<T> : AnimationJobBinder<CaptureAnimationJob, T>
        where T : struct, IAnimationJobData, ICaptureAnimationData
    {
        /// <inheritdoc />
        public override CaptureAnimationJob Create(Animator animator, ref T data, Component component)
        {
            var job = new CaptureAnimationJob();

            job.Bones = new NativeArray<ReadWriteTransformHandle>(data.CurrentPose.Length, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.BoneLocalPositions = new NativeArray<Vector3>(data.CurrentPose.Length, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.BoneLocalRotations = new NativeArray<Quaternion>(data.CurrentPose.Length, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);

            for (var i = HumanBodyBones.Hips; i < HumanBodyBones.LastBone; i++)
            {
                var bone = animator.GetBoneTransform(i);
                job.Bones[(int)i] = bone != null ?
                    ReadWriteTransformHandle.Bind(animator, bone) :
                    new ReadWriteTransformHandle();
                job.BoneLocalPositions[(int)i] = Vector3.zero;
                job.BoneLocalRotations[(int)i] = Quaternion.identity;
            }

            return job;
        }

        /// <inheritdoc />
        public override void Update(CaptureAnimationJob job, ref T data)
        {
            base.Update(job, ref data);

            var currentAnimatorStateInfo =
                data.ConstraintAnimator.GetCurrentAnimatorStateInfo(data.TargetAnimatorLayer);

            // Store the reference pose data, if the current animator state is different.
            if (currentAnimatorStateInfo.normalizedTime >= data.ReferencePoseTime &&
                currentAnimatorStateInfo.normalizedTime > 0.0f)
            {
                if (data.CurrentAnimatorStateInfoHash != currentAnimatorStateInfo.fullPathHash)
                {
                    data.CurrentAnimatorStateInfoHash = currentAnimatorStateInfo.fullPathHash;
                    for (var i = HumanBodyBones.Hips; i < HumanBodyBones.LastBone; i++)
                    {
                        var referencePoseBone = data.ReferencePose[(int)i];
                        var bonePosition = job.BoneLocalPositions[(int)i];
                        var boneRotation = job.BoneLocalRotations[(int)i];
                        referencePoseBone.LocalPosition = bonePosition;
                        referencePoseBone.LocalRotation = boneRotation;
                        data.ReferencePose[(int)i] = referencePoseBone;
                    }
                }
            }

            // Store the current pose data.
            for (var i = HumanBodyBones.Hips; i < HumanBodyBones.LastBone; i++)
            {
                var currentPoseBone = data.CurrentPose[(int)i];
                var bonePosition = job.BoneLocalPositions[(int)i];
                var boneRotation = job.BoneLocalRotations[(int)i];
                currentPoseBone.LocalPosition = bonePosition;
                currentPoseBone.LocalRotation = boneRotation;
                data.CurrentPose[(int)i] = currentPoseBone;
            }
        }

        /// <inheritdoc />
        public override void Destroy(CaptureAnimationJob job)
        {
            job.Bones.Dispose();
            job.BoneLocalPositions.Dispose();
            job.BoneLocalRotations.Dispose();
        }
    }
}
