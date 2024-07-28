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
        /// Fixed hips pose.
        /// </summary>
        public NativeArray<Pose> FixedHipsPose;

        /// <summary>
        /// Whether to use fixed hips pose or not.
        /// </summary>
        public BoolProperty UseFixedHipsPose;

        /// <summary>
        /// Whether to affect hips position Y-value.
        /// </summary>
        public BoolProperty AffectHipsPositionY;

        /// <summary>
        /// Whether to affect hips rotation X-value.
        /// </summary>
        public BoolProperty AffectHipsRotationX;

        /// <summary>
        /// Whether to affect hips rotation Y-value.
        /// </summary>
        public BoolProperty AffectHipsRotationY;

        /// <summary>
        /// Whether to affect hips rotation Z-value.
        /// </summary>
        public BoolProperty AffectHipsRotationZ;

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

                    var originalBonePosition = Bones[i].GetLocalPosition(stream);
                    var originalBoneRotation = Bones[i].GetLocalRotation(stream);
                    var targetBonePosition = originalBonePosition;
                    var targetBoneRotation = originalBoneRotation;

                    if (playbackType == PlaybackAnimationData.AnimationPlaybackType.Additive)
                    {
                        targetBonePosition += Vector3.Lerp(Vector3.zero, BonePositionDeltas[i], weight);
                        targetBoneRotation *= Quaternion.Slerp(Quaternion.identity, BoneRotationDeltas[i], weight);
                    }
                    if (playbackType == PlaybackAnimationData.AnimationPlaybackType.Override)
                    {
                        targetBonePosition = Vector3.Lerp(targetBonePosition, OverrideBonePositions[i], weight);
                        targetBoneRotation = Quaternion.Slerp(targetBoneRotation, OverrideBoneRotations[i], weight);
                    }

                    if (HumanBodyBones.Hips == (HumanBodyBones)i)
                    {
                        targetBonePosition = HandleHipsPosition(stream, targetBonePosition, originalBonePosition);
                        targetBoneRotation = HandleHipsRotation(stream, targetBoneRotation, originalBoneRotation);
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

        private Vector3 HandleHipsPosition(AnimationStream stream, Vector3 targetHipsPosition,
            Vector3 originalHipsPosition)
        {
            var finalHipsPosition = originalHipsPosition;
            // Be careful when setting the hips position; only affect portions specified.
            // If X and Z are modified, then that will put the hips into a horizontal
            // position that does not match tracking.
            if (AffectHipsPositionY.Get(stream))
            {
                finalHipsPosition = new Vector3(
                        originalHipsPosition.x,
                        UseFixedHipsPose.Get(stream) ? FixedHipsPose[0].position.y : targetHipsPosition.y,
                        originalHipsPosition.z
                    );
            }

            return finalHipsPosition;
        }

        private Quaternion HandleHipsRotation(AnimationStream stream, Quaternion targetHipsRotation,
            Quaternion originalHipsRotation)
        {
            var finalHipsRotation = originalHipsRotation;

            bool affectHipsRotationX = AffectHipsRotationX.Get(stream);
            bool affectHipsRotationY = AffectHipsRotationY.Get(stream);
            bool affectHipsRotationZ = AffectHipsRotationZ.Get(stream);

            if (affectHipsRotationX || affectHipsRotationY || affectHipsRotationZ)
            {
                bool useFixedHipsPose = UseFixedHipsPose.Get(stream);
                Vector3 fixedRotEuler = FixedHipsPose[0].rotation.eulerAngles;
                Vector3 originalRotEuler = originalHipsRotation.eulerAngles;
                Vector3 targetRotEuler = targetHipsRotation.eulerAngles;

                if (useFixedHipsPose)
                {
                    finalHipsRotation = Quaternion.Euler(
                        affectHipsRotationX ? fixedRotEuler.x : originalRotEuler.x,
                        affectHipsRotationY ? fixedRotEuler.y : originalRotEuler.y,
                        affectHipsRotationZ ? fixedRotEuler.z : originalRotEuler.z
                        );
                }
                else
                {
                    finalHipsRotation = Quaternion.Euler(
                        affectHipsRotationX ? targetRotEuler.x : originalRotEuler.x,
                        affectHipsRotationY ? targetRotEuler.y : originalRotEuler.y,
                        affectHipsRotationZ ? targetRotEuler.z : originalRotEuler.z
                        );
                }
            }

            return finalHipsRotation;
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

            job.FixedHipsPose =
                new NativeArray<Pose>(1, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            job.FixedHipsPose[0] = new Pose(data.FixedHipsPosition, Quaternion.Euler(data.FixedHipsRotationEuler));
            job.UseFixedHipsPose =
                BoolProperty.Bind(animator, component, data.UseFixedHipsPoseProperty);
            job.AffectHipsPositionY =
                BoolProperty.Bind(animator, component, data.AffectHipsPositionPropertyY);
            job.AffectHipsRotationX =
                BoolProperty.Bind(animator, component, data.AffectHipsRotationPropertyX);
            job.AffectHipsRotationY =
                BoolProperty.Bind(animator, component, data.AffectHipsRotationPropertyY);
            job.AffectHipsRotationZ =
                BoolProperty.Bind(animator, component, data.AffectHipsRotationPropertyZ);

            for (var i = HumanBodyBones.Hips; i < HumanBodyBones.LastBone; i++)
            {
                var bone = animator.GetBoneTransform(i);
                job.Bones[(int)i] = bone != null ?
                    ReadWriteTransformHandle.Bind(animator, bone) :
                    new ReadWriteTransformHandle();
                var defaultPosition = Vector3.zero;
                var defaultRotation = Quaternion.identity;

                if (bone != null)
                {
                    defaultPosition = bone.localPosition;
                    defaultRotation = bone.localRotation;
                }
                job.BonePositionDeltas[(int)i] = defaultPosition;
                job.BoneRotationDeltas[(int)i] = defaultRotation;
                job.OverrideBonePositions[(int)i] = defaultPosition;
                job.OverrideBoneRotations[(int)i] = defaultRotation;
                job.BonesToSkip[(int)i] = bone == null;
            }

            return job;
        }

        /// <inheritdoc />
        public override void Update(PlaybackAnimationJob job, ref T data)
        {
            ICaptureAnimationData captureAnimationData = data.SourceConstraint.data;

            job.FixedHipsPose[0] = new Pose(data.FixedHipsPosition,
                Quaternion.Euler(data.FixedHipsRotationEuler));
            for (var i = HumanBodyBones.Hips; i < HumanBodyBones.LastBone; i++)
            {
                var index = (int)i;
                var playbackType = (PlaybackAnimationData.AnimationPlaybackType)data.PlaybackType;

                bool boneFailsAnimationMask = data.AnimationMask != null &&
                     !data.AnimationMask.GetHumanoidBodyPartActive(BoneMappingsExtension.HumanBoneToAvatarBodyPartArray[(int)i]);
                bool boneFailsArrayMask = false;
                if (data.BonesArrayMask != null && data.BonesArrayMask.Length > 0)
                {
                    // override the other test
                    boneFailsAnimationMask = false;
                    // bone fails array mask UNLESS user specifies bone to be in it
                    boneFailsArrayMask = true;
                    foreach (var bodyBone in data.BonesArrayMask)
                    {
                        if (bodyBone == i)
                        {
                            boneFailsArrayMask = false;
                            break;
                        }
                    }
                }

                if (playbackType == PlaybackAnimationData.AnimationPlaybackType.None ||
                    boneFailsAnimationMask || boneFailsArrayMask)
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
                    job.OverrideBonePositions[index] = captureAnimationData.CurrentPose[index].Position;
                    job.OverrideBoneRotations[index] = captureAnimationData.CurrentPose[index].Rotation;
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
            job.FixedHipsPose.Dispose();
        }
    }
}
