// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

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
        /// The root scale.
        /// </summary>
        public Vector3 RootScale =>
            !_applyScale || !_isInitialized ? Vector3.one : RetargetedPose[_rootJointIndex].Scale;

        /// <summary>
        /// The head scale.
        /// </summary>
        public Vector3 HeadScale =>
            !_applyScale || !_isInitialized ? Vector3.one : RetargetedPose[_headJointIndex].Scale;

        /// <summary>
        /// The leg scale when hiding the lower body.
        /// </summary>
        public Vector3 HideLegScale => _hideLegScale;

        /// <summary>
        /// Hide the lower body if body tracking is set to UpperBody.
        /// </summary>
        public bool HideLowerBodyWhenUpperBodyTracking => _hideLowerBodyWhenUpperBodyTracking;

        /// <summary>
        /// True if this is initialized.
        /// </summary>
        public bool IsInitialized => _isInitialized;

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
        /// The left leg joint index.
        /// </summary>
        public int LeftUpperLegJointIndex => _leftUpperLegJointIndex;

        /// <summary>
        /// The right leg joint index.
        /// </summary>
        public int RightUpperLegJointIndex => _rightUpperLegJointIndex;

        /// <summary>
        /// The left lower leg index.
        /// </summary>
        public int LeftLowerLegJointIndex => _leftLowerLegJointIndex;

        /// <summary>
        /// The right lower leg  index.
        /// </summary>
        public int RightLowerLegJointIndex => _rightLowerLegJointIndex;

        /// <summary>
        /// The behavior type for retargeting operations.
        /// </summary>
        [SerializeField]
        private RetargetingBehavior _retargetingBehavior = RetargetingBehavior.RotationsAndPositions;

        /// <summary>
        /// Hide the lower body if body tracking is set to UpperBody.
        /// </summary>
        [SerializeField]
        private bool _hideLowerBodyWhenUpperBodyTracking;

        /// <summary>
        /// The scale for the legs if using <see cref="_hideLowerBodyWhenUpperBodyTracking"/>
        /// </summary>
        [SerializeField]
        private Vector3 _hideLegScale = Vector3.zero;

        /// <summary>
        /// Whether to apply scaling to the retargeted skeleton.
        /// </summary>
        [SerializeField]
        private bool _applyScale = true;

        /// <summary>
        /// The current scale factor applied to the retargeted skeleton.
        /// </summary>
        [SerializeField, ReadOnly]
        private float _currentScale = 1.0f;

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

        private SkeletonDraw _sourceSkeletonDraw = new()
        {
            IndexesToIgnore = new List<int>
            {
                (int)SkeletonData.FullBodyTrackingBoneId.LeftHandWristTwist,
                (int)SkeletonData.FullBodyTrackingBoneId.RightHandWristTwist
            }
        };

        private SkeletonDraw _targetSkeletonDraw = new();
        private ulong _nativeHandle = INVALID_HANDLE;
        private bool _isInitialized;
        private int _sourceJointCount;
        private int _targetJointCount;
        private int _rootJointIndex;
        private int _headJointIndex;
        private int _leftUpperLegJointIndex;
        private int _rightUpperLegJointIndex;
        private int _leftLowerLegJointIndex;
        private int _rightLowerLegJointIndex;
        private string[] _manifestations;
        private int[] _sourceParentIndices;
        private int[] _targetParentIndices;
        private NativeArray<NativeTransform> _sourceReferencePose;
        private NativeArray<NativeTransform> _sourcePose;
        private NativeArray<NativeTransform> _minTPose;
        private NativeArray<NativeTransform> _maxTPose;
        private NativeArray<int> _nativeSourceParentIndices;
        private NativeArray<int> _nativeTargetParentIndices;
        private NativeArray<NativeTransform> _targetReferencePose;

        /// <summary>
        /// Destructor.
        /// </summary>
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
            GetJointIndexByKnownJointType(_nativeHandle, SkeletonType.TargetSkeleton, KnownJointType.LeftUpperLeg,
                out _leftUpperLegJointIndex);
            GetJointIndexByKnownJointType(_nativeHandle, SkeletonType.TargetSkeleton, KnownJointType.RightUpperLeg,
                out _rightUpperLegJointIndex);
            GetChildJointIndexes(_nativeHandle, SkeletonType.TargetSkeleton, _leftUpperLegJointIndex,
                out var leftLowerLegIndices);
            GetChildJointIndexes(_nativeHandle, SkeletonType.TargetSkeleton, _rightUpperLegJointIndex,
                out var rightLowerLegIndices);
            _leftLowerLegJointIndex = leftLowerLegIndices[0];
            _rightLowerLegJointIndex = rightLowerLegIndices[0];

            // Query parent indices.
            _nativeSourceParentIndices = new NativeArray<int>(_sourceJointCount, Persistent, UninitializedMemory);
            _nativeTargetParentIndices = new NativeArray<int>(_targetJointCount, Persistent, UninitializedMemory);
            GetParentJointIndexesByRef(_nativeHandle, SkeletonType.SourceSkeleton, ref _nativeSourceParentIndices);
            GetParentJointIndexesByRef(_nativeHandle, SkeletonType.TargetSkeleton, ref _nativeTargetParentIndices);
            _sourceParentIndices = _nativeSourceParentIndices.ToArray();
            _targetParentIndices = _nativeTargetParentIndices.ToArray();

            // Setup empty retargeting pose arrays.
            _sourcePose = new NativeArray<NativeTransform>(_sourceJointCount, Persistent, UninitializedMemory);
            _sourceReferencePose = new NativeArray<NativeTransform>(_sourceJointCount, Persistent, UninitializedMemory);
            _minTPose = new NativeArray<NativeTransform>(_sourceJointCount, Persistent, UninitializedMemory);
            _maxTPose = new NativeArray<NativeTransform>(_sourceJointCount, Persistent, UninitializedMemory);

            // Retargeting pose arrays.
            _targetReferencePose = new NativeArray<NativeTransform>(_targetJointCount, Persistent, UninitializedMemory);
            RetargetedPose = new NativeArray<NativeTransform>(_targetJointCount, Persistent, UninitializedMemory);
            RetargetedPoseLocal = new NativeArray<NativeTransform>(_targetJointCount, Persistent, UninitializedMemory);

            // Setup T-Pose.
            GetSkeletonTPoseByRef(_nativeHandle, SkeletonType.SourceSkeleton, SkeletonTPoseType.MinTPose,
                JointRelativeSpaceType.RootOriginRelativeSpace, ref _minTPose);
            GetSkeletonTPoseByRef(_nativeHandle, SkeletonType.SourceSkeleton, SkeletonTPoseType.MaxTPose,
                JointRelativeSpaceType.RootOriginRelativeSpace, ref _maxTPose);
            UpdateSourceReferenceTPose(_nativeHandle, _maxTPose);

            _isInitialized = true;
        }

        /// <summary>
        /// Dispose the retargeting info arrays.
        /// </summary>
        public void Dispose()
        {
            // Dispose of native arrays.
            if (!_isInitialized)
            {
                return;
            }

            _isInitialized = false;
            _sourceSkeletonDraw = null;
            _targetSkeletonDraw = null;
            _sourcePose.Dispose();
            _sourceReferencePose.Dispose();
            _minTPose.Dispose();
            _maxTPose.Dispose();
            _nativeSourceParentIndices.Dispose();
            _nativeTargetParentIndices.Dispose();
            _targetReferencePose.Dispose();
            RetargetedPose.Dispose();
            RetargetedPoseLocal.Dispose();

            // Destroy native handle.
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
        /// Aligns the source input pose with the source reference pose.
        /// </summary>
        /// <param name="sourcePose">The source pose.</param>
        public void Align(NativeArray<NativeTransform> sourcePose)
        {
            MatchPose(_nativeHandle, SkeletonType.SourceSkeleton, MatchPoseBehavior.MatchScale,
                JointRelativeSpaceType.RootOriginRelativeSpace, ref _sourceReferencePose, ref sourcePose);
        }

        /// <summary>
        /// Run the retargeter and update the retargeted pose.
        /// </summary>
        /// <param name="sourcePose">The source pose.</param>
        /// <param name="manifestation">The manifestation.</param>
        public void Update(NativeArray<NativeTransform> sourcePose, string manifestation)
        {
            // Get retargeting settings.
            var retargetingBehaviorInfo = RetargetingBehaviorInfo.DefaultRetargetingSettings();
            retargetingBehaviorInfo.RetargetingBehavior = _retargetingBehavior;

            // If manifestation caused a change in the pose size, resize source pose.
            if (_sourcePose.Length != sourcePose.Length)
            {
                _sourcePose.Dispose();
                _sourcePose = new NativeArray<NativeTransform>(sourcePose, Persistent);
            }

            _sourcePose.CopyFrom(sourcePose);

            // Run retargeting.
            AppliedPose = false;
            if (!RetargetFromSourceFrameData(
                    _nativeHandle,
                    retargetingBehaviorInfo,
                    _sourcePose,
                    ref RetargetedPose,
                    manifestation))
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
        /// Update the source reference pose of the retargeter and calculate the scale.
        /// </summary>
        /// <param name="sourcePose">The source pose.</param>
        /// <param name="manifestation">The manifestation.</param>
        public void UpdateSourceReferencePose(NativeArray<NativeTransform> sourcePose, string manifestation)
        {
            // If manifestation caused a change in the pose size, resize source reference pose.
            if (_sourceReferencePose.Length != sourcePose.Length)
            {
                _sourceReferencePose.Dispose();
                _sourceReferencePose = new NativeArray<NativeTransform>(sourcePose, Persistent);
            }

            // Save the source reference pose.
            _sourceReferencePose.CopyFrom(sourcePose);

            // Retarget from source t-pose to get scaling.
            RetargetFromSourceFrameData(
                NativeHandle,
                RetargetingBehaviorInfo.DefaultRetargetingSettings(),
                sourcePose,
                ref _targetReferencePose,
                manifestation);
            ConvertJointPose(NativeHandle,
                SkeletonType.TargetSkeleton,
                JointRelativeSpaceType.RootOriginRelativeSpace,
                JointRelativeSpaceType.LocalSpaceScaled,
                _targetReferencePose,
                out var jointPoseWithScale);

            // Scale is uniform.
            _currentScale = jointPoseWithScale[0].Scale.x;

            // Specific case handling for when the root scale is less than range of values due to invalid setup
            if (_currentScale < 0.20f)
            {
                _currentScale *= 10f;
            }
        }

        /// <summary>
        /// Debug draw the source skeleton pose.
        /// </summary>
        public void DrawDebugSourcePose(Transform offset, Color color)
        {
            _sourceSkeletonDraw ??= new SkeletonDraw
            {
                IndexesToIgnore = new List<int>
                {
                    (int)SkeletonData.FullBodyTrackingBoneId.LeftHandWristTwist,
                    (int)SkeletonData.FullBodyTrackingBoneId.RightHandWristTwist
                }
            };
            if (_sourceSkeletonDraw.LineThickness <= float.Epsilon)
            {
                _sourceSkeletonDraw.InitDraw(color, 0.005f);
            }

            ApplyOffset(ref _sourcePose, offset, true);
            _sourceSkeletonDraw.LoadDraw(_sourcePose.Length, _sourceParentIndices, _sourcePose);
            _sourceSkeletonDraw.Draw();
        }

        /// <summary>
        /// Draws debug target pose.
        /// </summary>
        /// <param name="localOffset">Offset applied when drawing based on local transforms.</param>
        /// <param name="color">Color to use.</param>
        /// <param name="useWorldPose">If rendering world transforms.</param>
        public void DrawDebugTargetPose(Transform localOffset, Color color, bool useWorldPose = false)
        {
            _targetSkeletonDraw ??= new SkeletonDraw();
            if (_targetSkeletonDraw.LineThickness <= float.Epsilon)
            {
                _targetSkeletonDraw.InitDraw(color, 0.005f);
            }

            NativeArray<NativeTransform> targetWorldPose;
            if (useWorldPose)
            {
                targetWorldPose = new NativeArray<NativeTransform>(RetargetedPose.Length, Temp);
                targetWorldPose.CopyFrom(RetargetedPose);
            }
            else
            {
                targetWorldPose = GetWorldPoseFromLocalPose(RetargetedPoseLocal);
            }

            ApplyOffset(ref targetWorldPose, localOffset, false);
            _targetSkeletonDraw.LoadDraw(targetWorldPose.Length, _targetParentIndices, targetWorldPose);
            _targetSkeletonDraw.Draw();
            targetWorldPose.Dispose();
        }

        /// <summary>
        /// Converts a local space pose to a world space pose.
        /// </summary>
        /// <param name="localPose">The local space pose to convert.</param>
        /// <param name="rootPosition">Root position, optional.</param>
        /// <param name="rootRotation">Root rotation, optional.</param>
        /// <returns>A new NativeArray containing the converted world space pose.</returns>
        public NativeArray<NativeTransform> GetWorldPoseFromLocalPose(
            NativeArray<NativeTransform> localPose,
            Vector3? rootPosition = null,
            Quaternion? rootRotation = null)
        {
            var worldPose = new NativeArray<NativeTransform>(localPose.Length, TempJob);
            var job = new SkeletonJobs.ConvertLocalToWorldPoseJob
            {
                LocalPose = localPose,
                WorldPose = worldPose,
                ParentIndices = _nativeTargetParentIndices,
                RootScale = Vector3.one,
                RootPosition = rootPosition ?? Vector3.zero,
                RootRotation = rootRotation ?? Quaternion.identity
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
