// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;
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
        /// True if this is initialized.
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// True if the pose was applied.
        /// </summary>
        public bool AppliedPose { get; private set; }

        /// <summary>
        /// True if scale should be applied to the root.
        /// </summary>
        public bool ApplyRootScale
        {
            get => _applyRootScale;
            set => _applyRootScale = value;
        }

        /// <summary>
        /// True if scale should be applied to the head.
        /// </summary>
        public bool ApplyHeadScale
        {
            get => _applyHeadScale;
            set => _applyHeadScale = value;
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
        /// The current retargeting behavior.
        /// </summary>
        public RetargetingBehavior RetargetingBehavior
        {
            get => _retargetingBehavior;
            set => _retargetingBehavior = value;
        }

        /// <summary>
        /// The source skeleton data containing joint hierarchies, T-pose data, known joints, and manifestations.
        /// </summary>
        public SkeletonData SourceSkeletonData { get; private set; }

        /// <summary>
        /// The target skeleton data containing joint hierarchies, T-pose data, known joints, and manifestations.
        /// </summary>
        public SkeletonData TargetSkeletonData { get; private set; }

        /// <summary>
        /// The root scale.
        /// </summary>
        public Vector3 RootScale =>
            !_applyRootScale || !IsInitialized ? Vector3.one : RetargetedPose[TargetSkeletonData.RootJointIndex].Scale;

        /// <summary>
        /// The hips scale.
        /// </summary>
        public Vector3 HipsScale;

        /// <summary>
        /// The head scale.
        /// </summary>
        public Vector3 HeadScale =>
            !_applyHeadScale || !IsInitialized ? Vector3.one : RetargetedPose[TargetSkeletonData.HeadJointIndex].Scale;

        /// <summary>
        /// The leg scale when hiding the lower body.
        /// </summary>
        public Vector3 HideLegScale => _hideLegScale;

        /// <summary>
        /// Hide the lower body if body tracking is set to UpperBody.
        /// </summary>
        public bool HideLowerBodyWhenUpperBodyTracking => _hideLowerBodyWhenUpperBodyTracking;

        /// <summary>
        /// The native retargeting handle.
        /// </summary>
        public ulong NativeHandle => _nativeHandle;

        /// <summary>
        /// The number of joints in the source skeleton.
        /// </summary>
        public int SourceJointCount => SourceSkeletonData?.JointCount ?? 0;

        /// <summary>
        /// The number of joints in the target skeleton.
        /// </summary>
        public int TargetJointCount => TargetSkeletonData?.JointCount ?? 0;

        /// <summary>
        /// The retargeted pose in world space.
        /// </summary>
        public NativeArray<NativeTransform> RetargetedPose;

        /// <summary>
        /// The retargeted pose in local space.
        /// </summary>
        public NativeArray<NativeTransform> RetargetedPoseLocal;

        /// <summary>
        /// The T-Pose in local space.
        /// </summary>
        public NativeArray<NativeTransform> TargetReferencePoseLocal;

        /// <summary>
        /// The source pose that's being retargeted.
        /// </summary>
        public NativeArray<NativeTransform> SourcePose;

        /// <summary>
        /// The source reference pose.
        /// </summary>
        public NativeArray<NativeTransform> SourceReferencePose;

        /// <summary>
        /// The min T-Pose.
        /// </summary>
        public NativeArray<NativeTransform> SourceMinTPose;

        /// <summary>
        /// The max T-Pose
        /// </summary>
        public NativeArray<NativeTransform> SourceMaxTPose;

        /// <summary>
        /// The source parent joint indices.
        /// </summary>
        public NativeArray<int> SourceParentIndices;

        /// <summary>
        /// The target parent joint indices.
        /// </summary>
        public NativeArray<int> TargetParentIndices;

        /// <summary>
        /// The root joint index.
        /// </summary>
        public int RootJointIndex => TargetSkeletonData?.RootJointIndex ?? INVALID_JOINT_INDEX;

        /// <summary>
        /// The hips joint index.
        /// </summary>
        public int HipsJointIndex => TargetSkeletonData?.HipsJointIndex ?? INVALID_JOINT_INDEX;

        /// <summary>
        /// The head joint index.
        /// </summary>
        public int HeadJointIndex => TargetSkeletonData?.HeadJointIndex ?? INVALID_JOINT_INDEX;

        /// <summary>
        /// The left leg joint index.
        /// </summary>
        public int LeftUpperLegJointIndex => TargetSkeletonData?.LeftUpperLegJointIndex ?? INVALID_JOINT_INDEX;

        /// <summary>
        /// The right leg joint index.
        /// </summary>
        public int RightUpperLegJointIndex =>
            TargetSkeletonData?.RightUpperLegJointIndex ?? MSDKUtility.INVALID_JOINT_INDEX;

        /// <summary>
        /// The left lower leg index.
        /// </summary>
        public int LeftLowerLegJointIndex =>
            TargetSkeletonData?.LeftLowerLegJointIndex ?? MSDKUtility.INVALID_JOINT_INDEX;

        /// <summary>
        /// The right lower leg  index.
        /// </summary>
        public int RightLowerLegJointIndex =>
            TargetSkeletonData?.RightLowerLegJointIndex ?? MSDKUtility.INVALID_JOINT_INDEX;

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
        private bool _applyRootScale = true;

        /// <summary>
        /// Whether to apply head scaling to the retargeted skeleton.
        /// </summary>
        [SerializeField]
        private bool _applyHeadScale = true;

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
                (int)SkeletonData.FullBodyTrackingBoneId.RightHandWristTwist,
                (int)SkeletonData.FullBodyTrackingBoneId.LeftHandPalm,
                (int)SkeletonData.FullBodyTrackingBoneId.RightHandPalm
            }
        };

        private SkeletonDraw _targetSkeletonDraw = new();
        private ulong _nativeHandle = INVALID_HANDLE;
        private NativeArray<NativeTransform> _targetReferencePose;
        private NativeArray<int> _targetFingerIndices;
        private JobHandle _applyPoseJobHandle;

        /// <summary>
        /// Destructor.
        /// </summary>
        ~SkeletonRetargeter()
        {
            Dispose();
        }

        /// <summary>
        /// Setup the retargeting info arrays using config-based SkeletonData initialization.
        /// </summary>
        public void Setup(string config)
        {
            // Create native handle.
            if (!CreateOrUpdateHandle(config, out _nativeHandle))
            {
                throw new Exception("Failed to create a retargeting handle!.");
            }

            // Create SkeletonData instances from the existing handle
            SourceSkeletonData = SkeletonData.CreateFromHandle(_nativeHandle, SkeletonType.SourceSkeleton);
            TargetSkeletonData = SkeletonData.CreateFromHandle(_nativeHandle, SkeletonType.TargetSkeleton);

            // Setup runtime pose arrays using SkeletonData joint counts
            SourcePose =
                new NativeArray<NativeTransform>(SourceSkeletonData.JointCount, Persistent, UninitializedMemory);
            SourceReferencePose =
                new NativeArray<NativeTransform>(SourceSkeletonData.JointCount, Persistent, UninitializedMemory);

            // Retargeting pose arrays.
            _targetReferencePose =
                new NativeArray<NativeTransform>(TargetSkeletonData.JointCount, Persistent, UninitializedMemory);
            RetargetedPose =
                new NativeArray<NativeTransform>(TargetSkeletonData.JointCount, Persistent, UninitializedMemory);
            RetargetedPoseLocal =
                new NativeArray<NativeTransform>(TargetSkeletonData.JointCount, Persistent, UninitializedMemory);
            TargetReferencePoseLocal =
                new NativeArray<NativeTransform>(TargetSkeletonData.JointCount, Persistent, UninitializedMemory);

            // Create NativeArrays from SkeletonData regular arrays
            _targetFingerIndices = new NativeArray<int>(TargetSkeletonData.FingerIndices, Persistent);
            SourceMinTPose = new NativeArray<NativeTransform>(SourceSkeletonData.MinTPoseArray, Persistent);
            SourceMaxTPose = new NativeArray<NativeTransform>(SourceSkeletonData.MaxTPoseArray, Persistent);
            SourceParentIndices = new NativeArray<int>(SourceSkeletonData.ParentIndices, Persistent);
            TargetParentIndices = new NativeArray<int>(TargetSkeletonData.ParentIndices, Persistent);

            // Setup T-Pose from native API.
            GetSkeletonTPoseByRef(_nativeHandle, SkeletonType.TargetSkeleton, SkeletonTPoseType.UnscaledTPose,
                JointRelativeSpaceType.LocalSpace, ref TargetReferencePoseLocal);
            UpdateSourceReferenceTPose(_nativeHandle, SourceMaxTPose);

            AppliedPose = false;
            IsInitialized = true;
        }

        /// <summary>
        /// Dispose the retargeting info arrays.
        /// </summary>
        public void Dispose()
        {
            // Dispose of native arrays.
            if (!IsInitialized)
            {
                return;
            }

            AppliedPose = false;
            IsInitialized = false;
            _sourceSkeletonDraw = null;
            _targetSkeletonDraw = null;
            _targetReferencePose.Dispose();
            SourcePose.Dispose();
            SourceReferencePose.Dispose();
            RetargetedPose.Dispose();
            RetargetedPoseLocal.Dispose();

            // Dispose NativeArrays created from SkeletonData
            _targetFingerIndices.Dispose();
            SourceMinTPose.Dispose();
            SourceMaxTPose.Dispose();
            SourceParentIndices.Dispose();
            TargetParentIndices.Dispose();

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
                JointRelativeSpaceType.RootOriginRelativeSpace, ref SourceReferencePose, ref sourcePose);
        }

        /// <summary>
        /// Run the retargeter and update the retargeted pose.
        /// </summary>
        /// <param name="sourcePose">The source pose.</param>
        /// <param name="manifestation">The manifestation.</param>
        public bool Update(NativeArray<NativeTransform> sourcePose, string manifestation)
        {
            // Get retargeting settings.
            var retargetingBehaviorInfo = RetargetingBehaviorInfo.DefaultRetargetingSettingsForMSDK();
            retargetingBehaviorInfo.RetargetingBehavior = _retargetingBehavior;
            if (_retargetingBehavior is RetargetingBehavior.RotationAndPositionsUniformScale
                or RetargetingBehavior.RotationOnlyNoScaling)
            {
                retargetingBehaviorInfo.RetargetingBehavior = RetargetingBehavior.RotationsAndPositions;
            }

            // If manifestation caused a change in the pose size, resize source pose.
            if (SourcePose.Length != sourcePose.Length)
            {
                SourcePose.Dispose();
                SourcePose = new NativeArray<NativeTransform>(sourcePose, Persistent);
            }

            SourcePose.CopyFrom(sourcePose);

            // Run retargeting.
            AppliedPose = false;
            if (!RetargetFromSourceFrameData(
                    _nativeHandle,
                    retargetingBehaviorInfo,
                    SourcePose,
                    ref RetargetedPose,
                    manifestation))
            {
                Debug.LogError("Failed to retarget source frame data!");
                return false;
            }

            AppliedPose = true;

            // Clamp and update scales.
            var rootPose = RetargetedPose[RootJointIndex];
            var headPose = RetargetedPose[HeadJointIndex];
            var scaleVal = Mathf.Max(float.Epsilon,
                Mathf.Clamp(_currentScale, _scaleRange.x, _scaleRange.y));
            if (_retargetingBehavior == RetargetingBehavior.RotationOnlyNoScaling ||
                _retargetingBehavior == RetargetingBehavior.RotationAndPositionsUniformScale)
            {
                scaleVal = 1.0f;
            }

            rootPose.Scale = Vector3.one * scaleVal;
            headPose.Scale = Vector3.one * (1 + (1 - scaleVal) * (1 - _headScaleFactor));
            RetargetedPose[RootJointIndex] = rootPose;
            RetargetedPose[HeadJointIndex] = headPose;
            return true;
        }

        /// <summary>
        /// Applies the pose to the skeleton.
        /// </summary>
        /// <param name="joints">The joint access array to apply to.</param>
        public void ApplyPose(ref TransformAccessArray joints)
        {
            // Create job to apply the pose.
            var job = new SkeletonJobs.ApplyPoseJob
            {
                BodyPose = RetargetedPoseLocal,
                RotationOnlyIndices = _targetFingerIndices,
                RootJointIndex = RootJointIndex,
                HipsJointIndex = HipsJointIndex,
                CurrentRotationIndex =
                    _retargetingBehavior == RetargetingBehavior.RotationsAndPositionsHandsRotationOnly ? 0 : -1
            };
            _applyPoseJobHandle = job.Schedule(joints);
            _applyPoseJobHandle.Complete();
        }

        /// <summary>
        /// Update the source reference pose of the retargeter and calculate the scale.
        /// </summary>
        /// <param name="sourcePose">The source pose.</param>
        /// <param name="manifestation">The manifestation.</param>
        public void UpdateSourceReferencePose(NativeArray<NativeTransform> sourcePose, string manifestation)
        {
            // If manifestation caused a change in the pose size, resize source reference pose.
            if (SourceReferencePose.Length != sourcePose.Length)
            {
                SourceReferencePose.Dispose();
                SourceReferencePose = new NativeArray<NativeTransform>(sourcePose, Persistent);
            }

            // Save the source reference pose.
            SourceReferencePose.CopyFrom(sourcePose);

            // Retarget from source t-pose to get scaling.
            RetargetFromSourceFrameData(
                NativeHandle,
                RetargetingBehaviorInfo.DefaultRetargetingSettingsForMSDK(),
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
                    (int)SkeletonData.FullBodyTrackingBoneId.RightHandWristTwist,
                    (int)SkeletonData.FullBodyTrackingBoneId.LeftHandPalm,
                    (int)SkeletonData.FullBodyTrackingBoneId.RightHandPalm
                }
            };
            if (_sourceSkeletonDraw.LineThickness <= float.Epsilon || _sourceSkeletonDraw.TintColor != color)
            {
                _sourceSkeletonDraw.InitDraw(color);
            }

            var scale = offset.lossyScale;
            scale.x = scale.x >= 0.0f ? 1.0f : -1.0f;
            scale.y = scale.y >= 0.0f ? 1.0f : -1.0f;
            scale.z = scale.z >= 0.0f ? 1.0f : -1.0f;
            var debugSourcePose = new NativeArray<NativeTransform>(SourcePose, Temp);
            ApplyOffset(ref debugSourcePose, offset, scale);
            _sourceSkeletonDraw.LoadDraw(debugSourcePose.Length, SourceSkeletonData.ParentIndices, debugSourcePose);
            _sourceSkeletonDraw.Draw();
        }

        /// <summary>
        /// Draws last valid debug source pose.
        /// </summary>
        /// <param name="color"></param>
        public void DrawInvalidSourcePose(Color color)
        {
            _sourceSkeletonDraw ??= new SkeletonDraw();
            if (_sourceSkeletonDraw.LineThickness <= float.Epsilon || _sourceSkeletonDraw.TintColor != color)
            {
                _sourceSkeletonDraw.InitDraw(color);
            }

            _sourceSkeletonDraw.Draw();
        }

        /// <summary>
        /// Draws debug target pose.
        /// </summary>
        /// <param name="offset">Offset applied when drawing based on local transforms.</param>
        /// <param name="color">Color to use.</param>
        /// <param name="useWorldPose">If rendering world transforms.</param>
        public void DrawDebugTargetPose(Transform offset, Color color, bool useWorldPose = false)
        {
            _targetSkeletonDraw ??= new SkeletonDraw();
            if (_targetSkeletonDraw.LineThickness <= float.Epsilon || _targetSkeletonDraw.TintColor != color)
            {
                _targetSkeletonDraw.InitDraw(color);
            }

            var scale = offset.lossyScale;
            var targetWorldPose = useWorldPose
                ? new NativeArray<NativeTransform>(RetargetedPose, Temp)
                : GetWorldPoseFromLocalPose(RetargetedPoseLocal);
            ApplyOffset(ref targetWorldPose, offset, scale);
            _targetSkeletonDraw.LoadDraw(targetWorldPose.Length, TargetSkeletonData.ParentIndices, targetWorldPose);
            _targetSkeletonDraw.Draw();
            targetWorldPose.Dispose();
        }

        /// <summary>
        /// Draws last valid debug target pose.
        /// </summary>
        /// <param name="color"></param>
        public void DrawInvalidTargetPose(Color color)
        {
            _targetSkeletonDraw ??= new SkeletonDraw();
            if (_targetSkeletonDraw.LineThickness <= float.Epsilon || _targetSkeletonDraw.TintColor != color)
            {
                _targetSkeletonDraw.InitDraw(color);
            }

            _targetSkeletonDraw.Draw();
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
                RootIndex = RootJointIndex,
                HipsIndex = HipsJointIndex,
                LocalPose = localPose,
                WorldPose = worldPose,
                ParentIndices = TargetParentIndices,
                RootScale = Vector3.one,
                HipsScale = HipsScale,
                RootPosition = rootPosition ?? Vector3.zero,
                RootRotation = rootRotation ?? Quaternion.identity
            };
            job.Schedule().Complete();
            return worldPose;
        }

        private void ApplyOffset(ref NativeArray<NativeTransform> pose,
            Transform offset,
            Vector3 scale)
        {
            if (offset == null)
            {
                return;
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
