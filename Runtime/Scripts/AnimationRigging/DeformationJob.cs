// Copyright (c) Meta Platforms, Inc. and affiliates.

using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// The Deformation job.
    /// </summary>
    [Unity.Burst.BurstCompile]
    public struct DeformationJob : IWeightedAnimationJob
    {
        /// <summary>
        /// Bone animation data for the deformation job.
        /// </summary>
        public struct BoneAnimationData
        {
            /// <summary>
            /// The distance between the start and end bone transforms.
            /// </summary>
            public float Distance;

            /// <summary>
            /// The distance between the target and current position before
            /// the current position snaps to the target.
            /// </summary>
            public float SnapThreshold;

            /// <summary>
            /// The speed to move towards the target position
            /// </summary>
            public float MoveTowardsSpeed;

            /// <summary>
            /// True if it should be moving towards the target position.
            /// </summary>
            public bool IsMoveTowards;
        }

        /// <summary>
        /// The ReadWrite transform handle for the left upper arm bone.
        /// </summary>
        public ReadWriteTransformHandle LeftUpperArmBone;

        /// <summary>
        /// The ReadWrite transform handle for the right upper arm bone.
        /// </summary>
        public ReadWriteTransformHandle RightUpperArmBone;

        /// <summary>
        /// The ReadWrite transform handle for the left lower arm bone.
        /// </summary>
        public ReadWriteTransformHandle LeftLowerArmBone;

        /// <summary>
        /// The ReadWrite transform handle for the right lower arm bone.
        /// </summary>
        public ReadWriteTransformHandle RightLowerArmBone;

        /// <summary>
        /// The ReadWrite transform handle for the left hand bone.
        /// </summary>
        public ReadWriteTransformHandle LeftHandBone;

        /// <summary>
        /// The ReadWrite transform handle for the right hand bone.
        /// </summary>
        public ReadWriteTransformHandle RightHandBone;

        /// <summary>
        /// The inclusive array of bones from the hips to the head.
        /// </summary>
        public NativeArray<ReadWriteTransformHandle> HipsToHeadBones;

        /// <summary>
        /// The array of start bones for deformation.
        /// </summary>
        public NativeArray<ReadWriteTransformHandle> StartBones;

        /// <summary>
        /// The array of end bones for deformation.
        /// </summary>
        public NativeArray<ReadWriteTransformHandle> EndBones;

        /// <summary>
        /// The array of bone animation data for the start and end bone pairs.
        /// </summary>
        public NativeArray<BoneAnimationData> BoneAnimData;

        /// <summary>
        /// The array of directions between the start and end bones.
        /// </summary>
        public NativeArray<Vector3> BoneDirections;

        /// <summary>
        /// The array of current animated bone positions.
        /// </summary>
        public NativeArray<Vector3> BonePositions;

        /// <summary>
        /// The array containing 1 element for the current scale ratio.
        /// </summary>
        public NativeArray<Vector3> ScaleFactor;

        /// <summary>
        /// The array containing 1 element for the current delta time.
        /// </summary>
        public NativeArray<float> DeltaTime;

        /// <summary>
        /// The spine correction type.
        /// </summary>
        public DeformationData.SpineTranslationCorrectionType SpineCorrectionType;

        /// <summary>
        /// Allows the spine correction to run only once, assuming the skeleton's
        /// positions don't get updated multiple times.
        /// </summary>
        public bool CorrectSpineOnce;

        /// <summary>
        /// The index of the hip bone in the hips to head bones array.
        /// </summary>
        public int HipsBonesIndex;

        /// <summary>
        /// The index of the head bone in the hips to head bones array.
        /// </summary>
        public int HeadBonesIndex;

        /// <summary>
        /// The distance of the hip bone to the head bone.
        /// </summary>
        public float HipsToHeadDistance;

        /// <summary>
        /// The weight for the left arm offset.
        /// </summary>
        public float LeftArmOffsetWeight;

        /// <summary>
        /// The weight for the right arm offset.
        /// </summary>
        public float RightArmOffsetWeight;

        /// <summary>
        /// The weight for the left hand offset.
        /// </summary>
        public float LeftHandOffsetWeight;

        /// <summary>
        /// The weight for the right hand offset.
        /// </summary>
        public float RightHandOffsetWeight;

        private Vector3 _originalLeftUpperArmPos;
        private Vector3 _originalRightUpperArmPos;
        private Vector3 _originalLeftLowerArmPos;
        private Vector3 _originalRightLowerArmPos;
        private Vector3 _originalLeftHandPos;
        private Vector3 _originalRightHandPos;
        private bool _correctedSpine;
        private bool _initializedBonePositions;

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
                if (StartBones.Length == 0 || EndBones.Length == 0)
                {
                    return;
                }

                _originalLeftUpperArmPos = LeftUpperArmBone.GetPosition(stream);
                _originalRightUpperArmPos = RightUpperArmBone.GetPosition(stream);
                _originalLeftLowerArmPos = LeftLowerArmBone.GetPosition(stream);
                _originalRightLowerArmPos = RightLowerArmBone.GetPosition(stream);
                _originalLeftHandPos = LeftHandBone.GetPosition(stream);
                _originalRightHandPos = RightHandBone.GetPosition(stream);
                for (int i = 0; i < BoneDirections.Length; i++)
                {
                    var startBone = StartBones[i];
                    var endBone = EndBones[i];
                    BoneDirections[i] = (endBone.GetPosition(stream) - startBone.GetPosition(stream)).normalized;
                    StartBones[i] = startBone;
                    EndBones[i] = endBone;
                }
                if (SpineCorrectionType != DeformationData.SpineTranslationCorrectionType.None &&
                    (!CorrectSpineOnce || (CorrectSpineOnce && !_correctedSpine)))
                {
                    ProcessSpineCorrection(stream, weight);
                    _correctedSpine = true;
                }
                ProcessDeformation(stream, weight);
                ProcessArms(stream, weight);
                ProcessHands(stream, weight);
            }
            else
            {
                for (int i = 0; i < HipsToHeadBones.Length; ++i)
                {
                    AnimationRuntimeUtils.PassThrough(stream, HipsToHeadBones[i]);
                }
                for (int i = 0; i < StartBones.Length; ++i)
                {
                    AnimationRuntimeUtils.PassThrough(stream, StartBones[i]);
                }
                for (int i = 0; i < EndBones.Length; ++i)
                {
                    AnimationRuntimeUtils.PassThrough(stream, EndBones[i]);
                }
            }
        }

        private void ProcessSpineCorrection(AnimationStream stream, float weight)
        {
            var currentDirection = HipsToHeadBones[HipsBonesIndex].GetPosition(stream) -
                                         HipsToHeadBones[HeadBonesIndex].GetPosition(stream);
            var offset = currentDirection.normalized *
                         (HipsToHeadDistance - currentDirection.magnitude) /
                         HipsToHeadBones.Length;
            for (int i = 0; i < HipsToHeadBones.Length; i++)
            {
                if ((SpineCorrectionType == DeformationData.SpineTranslationCorrectionType.SkipHead && i == HeadBonesIndex) ||
                    (SpineCorrectionType == DeformationData.SpineTranslationCorrectionType.SkipHips && i == HipsBonesIndex) ||
                    (SpineCorrectionType == DeformationData.SpineTranslationCorrectionType.SkipHipsAndHead &&
                    (i == HeadBonesIndex || i == HipsBonesIndex)))
                {
                    continue;
                }

                var bone = HipsToHeadBones[i];
                var currentPosition = bone.GetPosition(stream);
                var targetPosition = currentPosition + Vector3.Scale(offset, ScaleFactor[0]);
                bone.SetPosition(stream, Vector3.Lerp(currentPosition, targetPosition, weight));
                HipsToHeadBones[i] = bone;
            }
        }

        private void ProcessDeformation(AnimationStream stream, float weight)
        {
            for (int i = 0; i < StartBones.Length; i++)
            {
                var startBone = StartBones[i];
                var endBone = EndBones[i];
                var startPos = startBone.GetPosition(stream);
                var endPos = endBone.GetPosition(stream);
                var data = BoneAnimData[i];
                var targetPos = startPos + Vector3.Scale(BoneDirections[i] * data.Distance, ScaleFactor[0]);

                // Bone positions are invalid on initialization, which would cause
                // MoveTowards to fail. Initialize to proper values on first frame.
                if (!_initializedBonePositions)
                {
                    BonePositions[i] = startPos;
                }
                if (Vector3.Distance(targetPos, BonePositions[i]) >= data.SnapThreshold)
                {
                    BonePositions[i] = targetPos;
                }

                if (data.IsMoveTowards)
                {
                    BonePositions[i] = Vector3.MoveTowards(BonePositions[i], targetPos,
                       DeltaTime[0] * data.MoveTowardsSpeed);
                }
                else
                {
                    BonePositions[i] = targetPos;
                }
                endBone.SetPosition(stream, Vector3.Lerp(endPos, BonePositions[i], weight));
                StartBones[i] = startBone;
                EndBones[i] = endBone;
            }

            _initializedBonePositions = true;
        }

        private void ProcessArms(AnimationStream stream, float weight)
        {
            var leftLowerArmPos = LeftLowerArmBone.GetPosition(stream);
            var rightLowerArmPos = RightLowerArmBone.GetPosition(stream);
            var leftUpperArmPos = LeftUpperArmBone.GetPosition(stream);
            var rightUpperArmPos = RightUpperArmBone.GetPosition(stream);
            LeftUpperArmBone.SetPosition(stream,
                Vector3.Lerp(_originalLeftUpperArmPos, leftUpperArmPos, weight * LeftArmOffsetWeight));
            RightUpperArmBone.SetPosition(stream,
                Vector3.Lerp(_originalRightUpperArmPos,rightUpperArmPos, weight * RightArmOffsetWeight));
            LeftLowerArmBone.SetPosition(stream,
                Vector3.Lerp(_originalLeftLowerArmPos, leftLowerArmPos, weight * LeftArmOffsetWeight));
            RightLowerArmBone.SetPosition(stream,
                Vector3.Lerp(_originalRightLowerArmPos, rightLowerArmPos, weight * RightArmOffsetWeight));
        }

        private void ProcessHands(AnimationStream stream, float weight)
        {
            var leftHandPos = LeftHandBone.GetPosition(stream);
            var rightHandPos = RightHandBone.GetPosition(stream);
            LeftHandBone.SetPosition(stream,
                Vector3.Lerp(_originalLeftHandPos, leftHandPos, weight * LeftHandOffsetWeight));
            RightHandBone.SetPosition(stream,
                Vector3.Lerp(_originalRightHandPos, rightHandPos, weight * RightHandOffsetWeight));
        }
    }

    /// <summary>
    /// The Deformation job binder.
    /// </summary>
    /// <typeparam name="T">The constraint data type</typeparam>
    public class DeformationJobBinder<T> : AnimationJobBinder<DeformationJob, T>
        where T : struct, IAnimationJobData, IDeformationData
    {
        private bool _shouldUpdate;
        private Transform _animatorTransform;

        /// <inheritdoc />
        public override DeformationJob Create(Animator animator, ref T data, Component component)
        {
            var job = new DeformationJob();

            _animatorTransform = animator.transform;

            job.LeftUpperArmBone = ReadWriteTransformHandle.Bind(animator, data.LeftArm.UpperArmBone);
            job.LeftLowerArmBone = ReadWriteTransformHandle.Bind(animator, data.LeftArm.LowerArmBone);
            job.RightUpperArmBone = ReadWriteTransformHandle.Bind(animator, data.RightArm.UpperArmBone);
            job.RightLowerArmBone = ReadWriteTransformHandle.Bind(animator, data.RightArm.LowerArmBone);
            job.LeftHandBone = ReadWriteTransformHandle.Bind(animator, data.LeftArm.HandBone);
            job.RightHandBone = ReadWriteTransformHandle.Bind(animator, data.RightArm.HandBone);

            job.StartBones = new NativeArray<ReadWriteTransformHandle>(data.BonePairs.Length, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.EndBones = new NativeArray<ReadWriteTransformHandle>(data.BonePairs.Length, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.HipsToHeadBones = new NativeArray<ReadWriteTransformHandle>(data.HipsToHeadBones.Length, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.BoneDirections = new NativeArray<Vector3>(data.BonePairs.Length, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.BonePositions = new NativeArray<Vector3>(data.BonePairs.Length, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.BoneAnimData = new NativeArray<DeformationJob.BoneAnimationData>(data.BonePairs.Length, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.ScaleFactor = new NativeArray<Vector3>(1, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            job.DeltaTime = new NativeArray<float>(1, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);

            for (int i = 0; i < data.HipsToHeadBones.Length; i++)
            {
                job.HipsToHeadBones[i] = ReadWriteTransformHandle.Bind(animator, data.HipsToHeadBones[i]);
            }
            for (int i = 0; i < data.BonePairs.Length; i++)
            {
                var boneAnimData = new DeformationJob.BoneAnimationData
                {
                    Distance = data.BonePairs[i].Distance,
                    SnapThreshold = data.BonePairs[i].SnapThreshold,
                    MoveTowardsSpeed = data.BonePairs[i].MoveTowardsSpeed,
                    IsMoveTowards = data.BonePairs[i].IsMoveTowards
                };
                job.StartBones[i] = ReadWriteTransformHandle.Bind(animator, data.BonePairs[i].StartBone);
                job.EndBones[i] = ReadWriteTransformHandle.Bind(animator, data.BonePairs[i].EndBone);
                job.BoneAnimData[i] = boneAnimData;
            }

            job.SpineCorrectionType = data.SpineCorrectionType;
            job.CorrectSpineOnce = data.CorrectSpineOnce;
            job.HipsBonesIndex = 0;
            job.HeadBonesIndex = data.HipsToHeadBones.Length - 1;
            job.LeftArmOffsetWeight = data.LeftArm.ArmWeight;
            job.RightArmOffsetWeight = data.RightArm.ArmWeight;
            job.LeftHandOffsetWeight = data.LeftArm.HandWeight;
            job.RightHandOffsetWeight = data.RightArm.HandWeight;
            job.HipsToHeadDistance = data.HipsToHeadDistance;
            job.DeltaTime[0] = Time.unscaledDeltaTime;

            return job;
        }

        /// <inheritdoc />
        public override void Update(DeformationJob job, ref T data)
        {
            if (data.IsBoneTransformsDataValid())
            {
                _shouldUpdate = true;
            }

            job.DeltaTime[0] = _shouldUpdate ? Time.unscaledDeltaTime : 0.0f;
            job.ScaleFactor[0] =
                DivideVector3(_animatorTransform.lossyScale, data.StartingScale);
            base.Update(job, ref data);

            if (!data.IsBoneTransformsDataValid())
            {
                _shouldUpdate = false;
            }
        }

        /// <inheritdoc />
        public override void Destroy(DeformationJob job)
        {
            job.StartBones.Dispose();
            job.EndBones.Dispose();
            job.BoneAnimData.Dispose();
            job.HipsToHeadBones.Dispose();
            job.BoneDirections.Dispose();
            job.BonePositions.Dispose();
            job.ScaleFactor.Dispose();
            job.DeltaTime.Dispose();
        }

        private Vector3 DivideVector3(Vector3 dividend, Vector3 divisor)
        {
            Vector3 targetScale = Vector3.one;
            if (IsNonZero(divisor))
            {
                targetScale = new Vector3(
                    dividend.x / divisor.x, dividend.y / divisor.y, dividend.z / divisor.z);
            }
            return targetScale;
        }

        private bool IsNonZero(Vector3 v)
        {
            return v.x != 0 && v.y != 0 && v.z != 0;
        }
    }
}
