// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Meta.XR.Movement.Retargeting
{
    /// <summary>
    /// The animation constraint, which blends animation with retargeting.
    /// </summary>
    [System.Serializable]
    public class AnimationSkeletalProcessor : TargetProcessor
    {
        [BurstCompile]
        private struct BlendAnimationJob : IJob
        {
            [ReadOnly]
            public float Weight;

            [ReadOnly]
            public int RootIndex;

            [ReadOnly]
            public int HipsIndex;

            [ReadOnly]
            public NativeArray<TargetJointIndex> BlendIndices;

            [ReadOnly]
            public NativeArray<MSDKUtility.NativeTransform> SourcePose;

            public NativeArray<MSDKUtility.NativeTransform> TargetPose;

            public void Execute()
            {
                // Blend the hips separately if hips are blended, using the root and hips information.
                var isHips = false;
                for (var i = 0; i < BlendIndices.Length; i++)
                {
                    if (BlendIndices[i].Index != HipsIndex)
                    {
                        continue;
                    }

                    isHips = true;
                    break;
                }

                var sourceHipsPose = SourcePose[HipsIndex];
                var targetHipsPose = TargetPose[HipsIndex];
                if (isHips)
                {
                    var hipsHeight = sourceHipsPose.Position.y - TargetPose[RootIndex].Position.y;
                    sourceHipsPose.Position = new Vector3(
                        targetHipsPose.Position.x, hipsHeight, targetHipsPose.Position.z);
                }

                // Blend the rest of the body.
                var blendIndex = 0;
                for (var i = 0; i < TargetPose.Length; i++)
                {
                    var target = TargetPose[i];
                    if (blendIndex >= BlendIndices.Length || BlendIndices[blendIndex] > i)
                    {
                        continue;
                    }

                    var source = i == HipsIndex && isHips ? sourceHipsPose : SourcePose[i];
                    target.Orientation = Quaternion.Slerp(target.Orientation, source.Orientation, Weight);
                    target.Position = Vector3.Lerp(target.Position, source.Position, Weight);
                    TargetPose[i] = target;
                    blendIndex++;
                }
            }
        }

        /// <summary>
        /// The joint indices to be blended with animation.
        /// </summary>
        public TargetJointIndex[] AnimBlendIndices
        {
            get => _animBlendIndices;
            set => _animBlendIndices = value;
        }

        [SerializeField]
        private TargetJointIndex[] _animBlendIndices;

        private int _rootJointIndex;
        private int _hipsJointIndex;
        private NativeArray<TargetJointIndex> _nativeBlendIndices;

        /// <inheritdoc />
        public override void Initialize(CharacterRetargeter retargeter)
        {
            MSDKUtility.GetJointIndexByKnownJointType(retargeter.RetargetingHandle,
                MSDKUtility.SkeletonType.TargetSkeleton, MSDKUtility.KnownJointType.Root,
                out _rootJointIndex);
            MSDKUtility.GetJointIndexByKnownJointType(retargeter.RetargetingHandle,
                MSDKUtility.SkeletonType.TargetSkeleton, MSDKUtility.KnownJointType.Hips,
                out _hipsJointIndex);
            _nativeBlendIndices = new NativeArray<TargetJointIndex>(_animBlendIndices, Allocator.Persistent);
        }

        /// <inheritdoc />
        public override void Destroy()
        {
            _nativeBlendIndices.Dispose();
        }

        /// <inheritdoc />
        public override void UpdatePose(ref NativeArray<MSDKUtility.NativeTransform> pose)
        {
        }

        /// <summary>
        /// Blend the pose based on the indices together in late update.
        /// </summary>
        /// <param name="currentPose">The current pose.</param>
        /// <param name="targetPose">The target pose to be blended.</param>
        public override void LateUpdatePose(ref NativeArray<MSDKUtility.NativeTransform> currentPose,
            ref NativeArray<MSDKUtility.NativeTransform> targetPose)
        {
            if (_weight <= 0.0f)
            {
                return;
            }

            var job = new BlendAnimationJob
            {
                Weight = _weight,
                RootIndex = _rootJointIndex,
                HipsIndex = _hipsJointIndex,
                BlendIndices = _nativeBlendIndices,
                SourcePose = currentPose,
                TargetPose = targetPose
            };
            job.Schedule().Complete();
        }
    }
}
