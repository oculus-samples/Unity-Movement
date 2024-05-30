// Copyright (c) Meta Platforms, Inc. and affiliates.

using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// The playback animation job. Update the bones based on the transform deltas passed into
    /// the animation job.
    /// </summary>
    [Unity.Burst.BurstCompile]
    public struct PlaybackAnimationJob : IWeightedAnimationJob
    {
        /// <summary>
        /// The animation playback type.
        /// </summary>
        public IntProperty PlaybackType;

        /// <summary>
        /// The animator bones to be updated.
        /// </summary>
        public NativeArray<ReadWriteTransformHandle> Bones;

        /// <summary>
        /// The position deltas to be applied to the bones.
        /// </summary>
        public NativeArray<Vector3> BonePositionDeltas;

        /// <summary>
        /// The rotation deltas to be applied to the bones.
        /// </summary>
        public NativeArray<Quaternion> BoneRotationDeltas;

        /// <summary>
        /// The override bone positions.
        /// </summary>
        public NativeArray<Vector3> OverrideBonePositions;

        /// <summary>
        /// The override bone rotations.
        /// </summary>
        public NativeArray<Quaternion> OverrideBoneRotations;

        /// <summary>
        /// The bones that should be skipped.
        /// </summary>
        public NativeArray<bool> BonesToSkip;

        /// <summary>
        /// Controls if positions from the animation should affect result.
        /// </summary>
        public BoolProperty AffectPositions;

        /// <summary>
        /// Controls if rotations from the animation should affect result.
        /// </summary>
        public BoolProperty AffectRotations;

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
                var playbackType = (PlaybackAnimationData.AnimationPlaybackType)PlaybackType.Get(stream);
                for (int i = 0; i < Bones.Length; i++)
                {
                    if (!Bones[i].IsValid(stream))
                    {
                        continue;
                    }

                    if (BonesToSkip[i])
                    {
                        // Skip setting this bone.
                        continue;
                    }

                    var targetBonePosition = Bones[i].GetLocalPosition(stream);
                    var targetBoneRotation = Bones[i].GetLocalRotation(stream);
                    if (playbackType == PlaybackAnimationData.AnimationPlaybackType.Additive)
                    {
                        targetBonePosition += Vector3.Lerp(Vector3.zero, BonePositionDeltas[i], weight);
                        targetBoneRotation *= Quaternion.Slerp(Quaternion.identity, BoneRotationDeltas[i], weight);
                    }
                    if (playbackType == PlaybackAnimationData.AnimationPlaybackType.Override)
                    {
                        if (i == (int)HumanBodyBones.Hips)
                        {
                            // Skip setting the hips bone.
                            // We want to keep the tracked position and rotation of the character
                            // when we apply the override animation to allow for blending between
                            // tracking and animation playback.
                            continue;
                        }
                        targetBonePosition = Vector3.Lerp(targetBonePosition, OverrideBonePositions[i], weight);
                        targetBoneRotation = Quaternion.Slerp(targetBoneRotation, OverrideBoneRotations[i], weight);
                    }

                    if (AffectPositions.Get(stream))
                    {
                        Bones[i].SetLocalPosition(stream, targetBonePosition);
                    }
                    if (AffectRotations.Get(stream))
                    {
                        Bones[i].SetLocalRotation(stream, targetBoneRotation);
                    }
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
    /// The playback animation job binder.
    /// </summary>
    /// <typeparam name="T">Type to be used for the job.</typeparam>
    public class PlaybackAnimationJobBinder<T> : AnimationJobBinder<PlaybackAnimationJob, T>
        where T : struct, IAnimationJobData, IPlaybackAnimationData
    {
        /// <inheritdoc />
        public override PlaybackAnimationJob Create(Animator animator, ref T data, Component component)
        {
            var job = new PlaybackAnimationJob();

            job.Bones = new NativeArray<ReadWriteTransformHandle>((int)HumanBodyBones.LastBone, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.PlaybackType =
                IntProperty.Bind(animator, component, data.PlaybackTypeIntProperty);
            job.BonePositionDeltas = new NativeArray<Vector3>((int)HumanBodyBones.LastBone, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.BoneRotationDeltas = new NativeArray<Quaternion>((int)HumanBodyBones.LastBone, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.OverrideBonePositions = new NativeArray<Vector3>((int)HumanBodyBones.LastBone, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.OverrideBoneRotations = new NativeArray<Quaternion>((int)HumanBodyBones.LastBone, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.BonesToSkip = new NativeArray<bool>((int)HumanBodyBones.LastBone, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);

            job.AffectPositions =
                BoolProperty.Bind(animator, component, data.AffectPositionsBoolProperty);
            job.AffectRotations =
                BoolProperty.Bind(animator, component, data.AffectRotationsBoolProperty);

            for (var i = HumanBodyBones.Hips; i < HumanBodyBones.LastBone; i++)
            {
                var bone = animator.GetBoneTransform(i);
                job.Bones[(int)i] = bone != null ?
                    ReadWriteTransformHandle.Bind(animator, bone) :
                    new ReadWriteTransformHandle();
                job.BonePositionDeltas[(int)i] = bone != null ? bone.localPosition : Vector3.zero;
                job.BoneRotationDeltas[(int)i] = bone != null ? bone.localRotation : Quaternion.identity;
                job.OverrideBonePositions[(int)i] = bone != null ? bone.localPosition : Vector3.zero;
                job.OverrideBoneRotations[(int)i] = bone != null ? bone.localRotation : Quaternion.identity;
                job.BonesToSkip[(int)i] = bone == null;
            }

            return job;
        }

        /// <inheritdoc />
        public override void Update(PlaybackAnimationJob job, ref T data)
        {
            ICaptureAnimationData captureAnimationData = data.SourceConstraint.data;
            for (var i = HumanBodyBones.Hips; i < HumanBodyBones.LastBone; i++)
            {
                var index = (int)i;
                var playbackType = (PlaybackAnimationData.AnimationPlaybackType)data.PlaybackType;

                if (playbackType == PlaybackAnimationData.AnimationPlaybackType.None ||
                    (data.AnimationMask != null &&
                     !data.AnimationMask.GetHumanoidBodyPartActive(BoneMappingsExtension.HumanBoneToAvatarBodyPartArray[(int)i])))
                {
                    job.BonePositionDeltas[index] = Vector3.zero;
                    job.BoneRotationDeltas[index] = Quaternion.identity;
                    job.OverrideBonePositions[index] = Vector3.zero;
                    job.OverrideBoneRotations[index] = Quaternion.identity;
                    job.BonesToSkip[index] = true;
                    continue;
                }

                if (playbackType == PlaybackAnimationData.AnimationPlaybackType.Additive)
                {
                    job.BonePositionDeltas[index] = data.SourceConstraint.data.GetBonePositionDelta(i);
                    job.BoneRotationDeltas[index] = data.SourceConstraint.data.GetBoneRotationDelta(i);
                    job.BonesToSkip[index] = false;
                }

                if (playbackType == PlaybackAnimationData.AnimationPlaybackType.Override)
                {
                    job.OverrideBonePositions[index] = captureAnimationData.CurrentPose[index].LocalPosition;
                    job.OverrideBoneRotations[index] = captureAnimationData.CurrentPose[index].LocalRotation;
                    job.BonesToSkip[index] = false;
                }
            }

            base.Update(job, ref data);
        }

        /// <inheritdoc />
        public override void Destroy(PlaybackAnimationJob job)
        {
            job.Bones.Dispose();
            job.BonesToSkip.Dispose();
            job.BonePositionDeltas.Dispose();
            job.BoneRotationDeltas.Dispose();
            job.OverrideBonePositions.Dispose();
            job.OverrideBoneRotations.Dispose();
        }
    }
}
