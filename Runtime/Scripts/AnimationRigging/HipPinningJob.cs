// Copyright (c) Meta Platforms, Inc. and affiliates.

using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// The Hip Pinning job.
    /// </summary>
    [Unity.Burst.BurstCompile]
    public struct HipPinningJob : IWeightedAnimationJob
    {
        /// <summary>
        /// The hips bone
        /// </summary>
        public ReadWriteTransformHandle Hips;

        /// <summary>
        /// The array of all bones on the skeleton.
        /// </summary>
        public NativeArray<ReadWriteTransformHandle> Bones;

        /// <summary>
        /// The target position for the hips.
        /// </summary>
        public NativeArray<Vector3> TargetHipPos;

        /// <summary>
        /// The array containing 1 element for the current delta time.
        /// </summary>
        public NativeArray<float> DeltaTime;

        /// <summary>
        /// If true, update the bone positions as there is a valid OVR Skeleton.
        /// </summary>
        public bool ValidOvrSkeleton;

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
                // Get delta between hips and target position.
                var trackedHipPos = Hips.GetLocalPosition(stream);
                var hipPosDelta = TargetHipPos[0] - trackedHipPos;

                // Only change bone positions if we have a valid OVR Skeleton Hierarchy.
                if (ValidOvrSkeleton)
                {
                    foreach (var bone in Bones)
                    {
                        if (bone.IsValid(stream))
                        {
                            var bonePos = bone.GetLocalPosition(stream);
                            bone.SetLocalPosition(stream,
                                Vector3.Lerp(bonePos, bonePos + hipPosDelta, weight));
                        }
                    }
                }

                // Set the hips position.
                Hips.SetLocalPosition(stream,
                    Vector3.Lerp(trackedHipPos, TargetHipPos[0], weight));
            }
            else
            {
                foreach (var bone in Bones)
                {
                    if (bone.IsValid(stream))
                    {
                        AnimationRuntimeUtils.PassThrough(stream, bone);
                    }
                }
            }
        }
    }

    /// <summary>
    /// The Hip Pinning job binder.
    /// </summary>
    /// <typeparam name="T">The constraint data type</typeparam>
    public class HipPinningJobBinder<T> : AnimationJobBinder<HipPinningJob, T>
        where T : struct, IAnimationJobData, IHipPinningData
    {
        private Transform _hipsParent;
        private IHipPinningData _data;
        private Quaternion _initialLocalRotation;
        private bool _shouldUpdate;
        private bool _firstFrame = true;

        /// <inheritdoc />
        public override HipPinningJob Create(Animator animator, ref T data, Component component)
        {
            var job = new HipPinningJob();

            var spineLowerIndex = (int)OVRSkeleton.BoneId.Body_SpineLower;
            int bonesCount = data.ConstraintSkeleton.CustomBones.Count;

            job.Hips = ReadWriteTransformHandle.Bind(animator, data.Bones[(int)OVRSkeleton.BoneId.Body_Hips]);
            job.Bones = new NativeArray<ReadWriteTransformHandle>(bonesCount - spineLowerIndex, Allocator.Persistent);
            job.TargetHipPos = new NativeArray<Vector3>(1, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.DeltaTime = new NativeArray<float>(1, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.ValidOvrSkeleton = data.ConstraintSkeleton.IsInitialized;

            int boneIndex = 0;
            for (int i = spineLowerIndex; i < bonesCount; i++)
            {
                if (data.Bones[i] != null)
                {
                    job.Bones[boneIndex] = ReadWriteTransformHandle.Bind(animator, data.Bones[i]);
                    boneIndex++;
                }
            }

            var hipsBone = data.Bones[(int)OVRSkeleton.BoneId.Body_Hips];
            _hipsParent = hipsBone.parent;
            _initialLocalRotation = data.InitialHipLocalRotation;
            _data = data;
            _data.AssignClosestHipPinningTarget(hipsBone.position);
            return job;
        }

        /// <inheritdoc />
        public override void Update(HipPinningJob job, ref T data)
        {
            if (data.ConstraintSkeleton.IsDataValid)
            {
                _shouldUpdate = true;
            }

            Transform hips = data.Bones[(int)OVRSkeleton.BoneId.Body_Hips];
            Vector3 trackedHipPos = hips.position;
            if (data.HipPinningLeave)
            {
                CheckIfHipPinningIsValid(data, trackedHipPos);
            }

            if (data.CurrentHipPinningTarget != null)
            {
                Vector3 closestHipTargetPos = data.CurrentHipPinningTarget.HipTargetTransform.position;
                job.TargetHipPos[0] = _hipsParent.InverseTransformPoint(closestHipTargetPos);
                Quaternion targetRotation = hips.localRotation *
                                            Quaternion.Inverse(_initialLocalRotation) *
                                            data.CurrentHipPinningTarget.HipTargetInitialRotationOffset;

                // Don't set the rotation of the chair on the first frame, so that the feet are angled correctly.
                if (_firstFrame)
                {
                    _firstFrame = false;
                }
                else
                {
                    data.CurrentHipPinningTarget.ChairSeatTransform.localRotation = Quaternion.Euler(0,
                        targetRotation.eulerAngles.y,
                        0);
                }
            }
            else
            {
                _shouldUpdate = false;
            }
            job.DeltaTime[0] = _shouldUpdate ? Time.unscaledDeltaTime : 0.0f;
            base.Update(job, ref data);

            if (!data.ConstraintSkeleton.IsDataValid)
            {
                _shouldUpdate = false;
            }
        }

        /// <inheritdoc />
        public override void Destroy(HipPinningJob job)
        {
            job.Bones.Dispose();
            job.TargetHipPos.Dispose();
            job.DeltaTime.Dispose();
        }

        private void CheckIfHipPinningIsValid(IHipPinningData data, Vector3 trackedHipPos)
        {
            float dist = Vector3.Distance(data.CalibratedHipPos, trackedHipPos);
            if (dist > data.HipPinningLeaveRange && data.CurrentHipPinningTarget != null)
            {
                data.ExitHipPinningArea(data.CurrentHipPinningTarget);
            }
        }
    }
}
