// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using static Meta.XR.Movement.MSDKUtility;
using static Unity.Collections.Allocator;
using static Unity.Collections.NativeArrayOptions;

namespace Meta.XR.Movement.Retargeting
{
    /// <summary>
    /// The retargeting handler that handles running of the retargeting job.
    /// </summary>
    [Serializable]
    public class SkeletonRetargeter
    {
        /// <summary>
        /// True if the pose was applied.
        /// </summary>
        public bool AppliedPose { get; private set; }

        /// <summary>
        /// True if scale should be applied.
        /// </summary>
        public bool ApplyScale
        {
            get => _applyScale;
            set => _applyScale = value;
        }

        /// <summary>
        /// The scale clamp range.
        /// </summary>
        public Vector2 ScaleRange
        {
            get => _scaleRange;
            set => _scaleRange = value;
        }

        /// <summary>
        /// True if this is valid.
        /// </summary>
        public bool IsValid => _isValid;

        /// <summary>
        /// The retargeted pose in world space.
        /// </summary>
        public NativeArray<NativeTransform> RetargetedPose;

        /// <summary>
        /// The retargeted pose in local space.
        /// </summary>
        public NativeArray<NativeTransform> RetargetedPoseLocal;

        /// <summary>
        /// The source pose that's being retargeted.
        /// </summary>
        public NativeArray<NativeTransform> SourcePose => _sourcePose;

        /// <summary>
        /// The min T-Pose.
        /// </summary>
        public NativeArray<NativeTransform> MinTPose => _minTPose;

        /// <summary>
        /// The max T-Pose
        /// </summary>
        public NativeArray<NativeTransform> MaxTPose => _maxTPose;

        /// <summary>
        /// The native retargeting handle.
        /// </summary>
        public ulong NativeHandle => _nativeHandle;

        /// <summary>
        /// The source parent joint indices.
        /// </summary>
        public NativeArray<int> NativeSourceParentIndices => _nativeSourceParentIndices;

        /// <summary>
        /// The target parent joint indices.
        /// </summary>
        public NativeArray<int> NativeTargetParentIndices => _nativeTargetParentIndices;

        /// <summary>
        /// The number of joints in the source skeleton.
        /// </summary>
        public int SourceJointCount => _sourceJointCount;

        /// <summary>
        /// The number of joints in the target skeleton.
        /// </summary>
        public int TargetJointCount => _targetJointCount;

        /// <summary>
        /// The root joint index.
        /// </summary>
        public int RootJointIndex => _rootJointIndex;

        /// <summary>
        /// The head joint index.
        /// </summary>
        public int HeadJointIndex => _headJointIndex;

        /// <summary>
        /// The behavior type for retargeting operations.
        /// </summary>
        [SerializeField]
        private RetargetingBehavior _retargetingBehavior = RetargetingBehavior.RotationsAndPositions;

        /// <summary>
        /// The current scale factor applied to the retargeted skeleton.
        /// </summary>
        [SerializeField, ReadOnly]
        private float _currentScale = 1.0f;

        /// <summary>
        /// Whether to apply scaling to the retargeted skeleton.
        /// </summary>
        [SerializeField]
        private bool _applyScale = true;

        /// <summary>
        /// Scale factor applied to the head joint to prevent it from scaling as much as the rest of the body.
        /// </summary>
        [SerializeField]
        private float _headScaleFactor = 0.95f;

        /// <summary>
        /// Minimum and maximum scale range for the retargeted skeleton.
        /// </summary>
        [SerializeField]
        private Vector2 _scaleRange = new(0.8f, 1.2f);

        private readonly SkeletonDraw _sourceSkeletonDraw = new()
        {
            IndexesToIgnore = new List<int>
            {
                (int)SkeletonData.FullBodyTrackingBoneId.LeftHandWristTwist,
                (int)SkeletonData.FullBodyTrackingBoneId.RightHandWristTwist
            }
        };

        private readonly SkeletonDraw _targetSkeletonDraw = new();
        private ulong _nativeHandle = INVALID_HANDLE;
        private bool _isValid;
        private int _sourceJointCount;
        private int _targetJointCount;
        private int _rootJointIndex;
        private int _headJointIndex;
        private int[] _sourceParentIndices;
        private int[] _targetParentIndices;
        private NativeArray<NativeTransform> _sourcePose;
        private NativeArray<NativeTransform> _minTPose;
        private NativeArray<NativeTransform> _maxTPose;
        private NativeArray<int> _nativeSourceParentIndices;
        private NativeArray<int> _nativeTargetParentIndices;
        private NativeArray<NativeTransform> _scalePoseReference;

        ~SkeletonRetargeter()
        {
            Dispose();
        }


        /// <summary>
        /// Setup the retargeting info arrays.
        /// </summary>
        public void Setup(string config)
        {
            // Create native handle.
            if (!CreateOrUpdateHandle(config, out _nativeHandle))
            {
                throw new Exception("Failed to create a retargeting handle!.");
            }

            // Query skeleton info.
            GetSkeletonJointCount(_nativeHandle, SkeletonType.SourceSkeleton, out _sourceJointCount);
            GetSkeletonJointCount(_nativeHandle, SkeletonType.TargetSkeleton, out _targetJointCount);
            GetJointIndexByKnownJointType(_nativeHandle, SkeletonType.TargetSkeleton, KnownJointType.Root,
                out _rootJointIndex);
            GetJointIndexByKnownJointType(_nativeHandle, SkeletonType.TargetSkeleton, KnownJointType.Neck,
                out _headJointIndex);

            // Query parent indices.
            _nativeSourceParentIndices = new NativeArray<int>(_sourceJointCount, Persistent, UninitializedMemory);
            _nativeTargetParentIndices = new NativeArray<int>(_targetJointCount, Persistent, UninitializedMemory);
            GetParentJointIndexesByRef(_nativeHandle, SkeletonType.SourceSkeleton, ref _nativeSourceParentIndices);
            GetParentJointIndexesByRef(_nativeHandle, SkeletonType.TargetSkeleton, ref _nativeTargetParentIndices);
            _sourceParentIndices = _nativeSourceParentIndices.ToArray();
            _targetParentIndices = _nativeTargetParentIndices.ToArray();

            // Setup empty retargeting pose arrays.
            _sourcePose = new NativeArray<NativeTransform>(SourceJointCount, Persistent, UninitializedMemory);
            _minTPose = new NativeArray<NativeTransform>(SourceJointCount, Persistent, UninitializedMemory);
            _maxTPose = new NativeArray<NativeTransform>(SourceJointCount, Persistent, UninitializedMemory);
            _scalePoseReference = new NativeArray<NativeTransform>(TargetJointCount, Persistent, UninitializedMemory);
            RetargetedPose = new NativeArray<NativeTransform>(TargetJointCount, Persistent, UninitializedMemory);
            RetargetedPoseLocal = new NativeArray<NativeTransform>(TargetJointCount, Persistent, UninitializedMemory);

            // Setup T-Pose.
            GetSkeletonTPoseByRef(NativeHandle, SkeletonType.SourceSkeleton, SkeletonTPoseType.MinTPose,
                JointRelativeSpaceType.RootOriginRelativeSpace, ref _minTPose);
            GetSkeletonTPoseByRef(NativeHandle, SkeletonType.SourceSkeleton, SkeletonTPoseType.MaxTPose,
                JointRelativeSpaceType.RootOriginRelativeSpace, ref _maxTPose);
            UpdateSourceReferenceTPose(NativeHandle, _maxTPose);

            _isValid = true;
        }

        /// <summary>
        /// Dispose the retargeting info arrays.
        /// </summary>
        public void Dispose()
        {
            // Dispose of native arrays.
            _isValid = false;
            if (_sourcePose.IsCreated)
            {
                _sourcePose.Dispose();
            }

            if (_minTPose.IsCreated)
            {
                _minTPose.Dispose();
            }

            if (_maxTPose.IsCreated)
            {
                _maxTPose.Dispose();
            }

            if (_nativeSourceParentIndices.IsCreated)
            {
                _nativeSourceParentIndices.Dispose();
            }

            if (_nativeTargetParentIndices.IsCreated)
            {
                _nativeTargetParentIndices.Dispose();
            }

            if (_scalePoseReference.IsCreated)
            {
                _scalePoseReference.Dispose();
            }

            if (RetargetedPose.IsCreated)
            {
                RetargetedPose.Dispose();
            }

            if (RetargetedPoseLocal.IsCreated)
            {
                RetargetedPoseLocal.Dispose();
            }

            // Destroy native handle.
            if (_nativeHandle == INVALID_HANDLE)
            {
                return;
            }

            if (!DestroyHandle(_nativeHandle))
            {
                Debug.LogError($"Failed to destroy retargeting handle {_nativeHandle}.");
            }
            else
            {
                _nativeHandle = INVALID_HANDLE;
            }
        }

        /// <summary>
        /// Update the scale of the retargeter.
        /// </summary>
        /// <param name="sourcePose">The source pose.</param>
        public void UpdateScale(NativeArray<NativeTransform> sourcePose)
        {
            // Retarget from source t-pose to get scaling.
            RetargetFromSourceFrameData(
                NativeHandle,
                RetargetingBehaviorInfo.DefaultRetargetingSettings(),
                sourcePose,
                ref _scalePoseReference);
            ConvertJointPose(NativeHandle,
                SkeletonType.TargetSkeleton,
                JointRelativeSpaceType.RootOriginRelativeSpace,
                JointRelativeSpaceType.LocalSpaceScaled,
                _scalePoseReference,
                out var jointPoseWithScale);

            // Scale is uniform.
            _currentScale = jointPoseWithScale[0].Scale.x;
        }

        /// <summary>
        /// Run the retargeter and update the retargeted pose.
        /// </summary>
        /// <param name="sourcePose">The sourcePose.</param>
        public void Update(NativeArray<NativeTransform> sourcePose)
        {
            // Get retargeting settings.
            var retargetingBehaviorInfo = RetargetingBehaviorInfo.DefaultRetargetingSettings();
            retargetingBehaviorInfo.RetargetingBehavior = _retargetingBehavior;
            _sourcePose.CopyFrom(sourcePose);

            // Run retargeting.
            AppliedPose = false;
            if (!RetargetFromSourceFrameData(
                    _nativeHandle,
                    retargetingBehaviorInfo,
                    _sourcePose,
                    ref RetargetedPose))
            {
                Debug.LogError("Failed to retarget source frame data!");
                return;
            }

            AppliedPose = true;

            // Clamp and update scales.
            var rootPose = RetargetedPose[RootJointIndex];
            var headPose = RetargetedPose[HeadJointIndex];
            var scaleVal = Mathf.Max(float.Epsilon,
                Mathf.Clamp(_currentScale, _scaleRange.x, _scaleRange.y));
            rootPose.Scale = Vector3.one * scaleVal;
            headPose.Scale = Vector3.one * (1 + (1 - scaleVal) * (1 - _headScaleFactor));
            RetargetedPose[RootJointIndex] = rootPose;
            RetargetedPose[HeadJointIndex] = headPose;
        }

        /// <summary>
        /// Debug draw the source skeleton pose.
        /// </summary>
        public void DrawDebugSourcePose(Transform offset, Color color)
        {
            if (_sourceSkeletonDraw.LineThickness <= float.Epsilon)
            {
                _sourceSkeletonDraw.InitDraw(color, 0.005f);
            }

            ApplyOffset(ref _sourcePose, offset, true);
            _sourceSkeletonDraw.LoadDraw(_sourceJointCount, _sourceParentIndices, _sourcePose);
            _sourceSkeletonDraw.Draw();
        }

        /// <summary>
        /// Debug draw the target skeleton pose.
        /// </summary>
        public void DrawDebugTargetPose(Transform offset, Color color, bool useWorldPose = false)
        {
            if (_targetSkeletonDraw.LineThickness <= float.Epsilon)
            {
                _targetSkeletonDraw.InitDraw(color, 0.005f);
            }

            var worldPose = useWorldPose
                ? new NativeArray<NativeTransform>(RetargetedPose.Length, Temp)
                : GetWorldPoseFromLocalPose(RetargetedPoseLocal);
            if (useWorldPose)
            {
                worldPose.CopyFrom(RetargetedPose);
            }

            ApplyOffset(ref worldPose, offset, false);
            _targetSkeletonDraw.LoadDraw(_targetJointCount, _targetParentIndices, worldPose);
            _targetSkeletonDraw.Draw();
            worldPose.Dispose();
        }

        /// <summary>
        /// Converts a local space pose to a world space pose.
        /// </summary>
        /// <param name="localPose">The local space pose to convert.</param>
        /// <returns>A new NativeArray containing the converted world space pose.</returns>
        public NativeArray<NativeTransform> GetWorldPoseFromLocalPose(NativeArray<NativeTransform> localPose)
        {
            var worldPose = new NativeArray<NativeTransform>(localPose.Length, TempJob);
            var job = new SkeletonJobs.ConvertLocalToWorldPoseJob
            {
                LocalPose = localPose,
                WorldPose = worldPose,
                ParentIndices = _nativeTargetParentIndices,
                RootScale = Vector3.one
            };
            job.Schedule().Complete();
            return worldPose;
        }

        private void ApplyOffset(ref NativeArray<NativeTransform> pose, Transform offset, bool uniformScale)
        {
            if (offset == null)
            {
                return;
            }

            var scale = offset.lossyScale;
            if (uniformScale)
            {
                scale.x = scale.x >= 0.0f ? 1.0f : -1.0f;
                scale.y = scale.y >= 0.0f ? 1.0f : -1.0f;
                scale.z = scale.z >= 0.0f ? 1.0f : -1.0f;
            }

            for (var i = 0; i < pose.Length; i++)
            {
                var currentPose = pose[i];
                var offsetPosition = offset.position +
                                     offset.rotation * Vector3.Scale(scale, currentPose.Position);
                var offsetRotation = offset.rotation * currentPose.Orientation;
                pose[i] = new NativeTransform(offsetRotation, offsetPosition);
            }
        }
    }
}
