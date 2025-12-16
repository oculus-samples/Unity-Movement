// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using Meta.XR.Movement.AI;
using Unity.Collections;
using UnityEngine;
using static Meta.XR.Movement.MSDKUtility;

namespace Meta.XR.Movement.Retargeting
{
    /// <summary>
    /// Source data provider for Meta body tracking with optional AI Motion Synthesizer blending.
    /// Inherits from <see cref="OVRBody"/> and implements <see cref="ISourceDataProvider"/>.
    /// </summary>
    public class MetaSourceDataProvider : OVRBody, ISourceDataProvider
    {
        /// <summary>Enable debug skeleton visualization.</summary>
        public bool DebugDrawSkeleton
        {
            get => _debugDrawSkeleton;
            set => _debugDrawSkeleton = value;
        }

        /// <summary>Enable AI Motion Synthesizer blending with body tracking.</summary>
        public bool EnableAIMotionSynthesizer
        {
            get => _enableAIMotionSynthesizer;
            set => _enableAIMotionSynthesizer = value;
        }

        /// <summary>Manifestation name returned when using upper body tracking.</summary>
        public const string HalfBodyManifestation = "halfbody";

        /// <summary>
        /// Delay before body tracking is considered valid. Allows tracking to stabilize on startup.
        /// </summary>
        [SerializeField]
        protected float _validBodyTrackingDelay = 0.25f;

        [Tooltip("Enable debug skeleton visualization")]
        [SerializeField]
        protected bool _debugDrawSkeleton;

        [Tooltip("Color for debug skeleton visualization")]
        [SerializeField]
        protected Color _debugSkeletonColor = Color.white;

        [Tooltip("Enable AI Motion Synthesizer for natural locomotion animations")]
        [SerializeField]
        protected bool _enableAIMotionSynthesizer;

        /// <summary>AI Motion Synthesizer configuration. Only used when <see cref="_enableAIMotionSynthesizer"/> is true.</summary>
        [SerializeField]
        protected AIMotionSynthesizerConfig _aiMotionSynthesizerConfig = new();
        protected AI.AIMotionSynthesizer _aiMotionSynthesizer;

        protected OVRPlugin.BodyJointSet _currentSkeletonType;
        protected int _skeletalChangedCount = -1;
        protected int _currentSkeletalChangeCount = -1;
        protected float _currentValidBodyTrackingTime;
        protected bool _isValid;

        protected virtual void Start()
        {
            _currentSkeletonType = ProvidedSkeletonType;

            if (_enableAIMotionSynthesizer)
            {
                _aiMotionSynthesizer = new AI.AIMotionSynthesizer(_aiMotionSynthesizerConfig, transform);
                _aiMotionSynthesizer.Initialize();
            }
        }

        protected virtual void LateUpdate()
        {
            if (_enableAIMotionSynthesizer)
            {
                _aiMotionSynthesizer.Update(Time.smoothDeltaTime);
                _aiMotionSynthesizer.ApplyRootMotion();
            }
        }

        protected virtual void OnDestroy()
        {
            _aiMotionSynthesizer?.Dispose();
        }

        protected virtual void OnValidate()
        {
            _aiMotionSynthesizer?.ValidateInputProvider();
        }

        /// <inheritdoc />
        public virtual NativeArray<NativeTransform> GetSkeletonPose()
        {
            var sourcePose = SkeletonUtilities.GetPosesFromTheTracker(
                this,
                Pose.identity,
                true,
                out _currentSkeletalChangeCount,
                out _isValid);

            if (_currentValidBodyTrackingTime < _validBodyTrackingDelay)
            {
                _currentValidBodyTrackingTime += Time.smoothDeltaTime;
                _isValid = false;
            }

            NativeArray<NativeTransform> finalPose;
            if (!_enableAIMotionSynthesizer || _aiMotionSynthesizer == null || !_aiMotionSynthesizer.IsInitialized)
            {
                finalPose = sourcePose;
            }
            else
            {
                NativeArray<NativeTransform> fullBodyPose = sourcePose;
                bool createdFullBodyPose = false;
                if (ProvidedSkeletonType == OVRPlugin.BodyJointSet.UpperBody)
                {
                    fullBodyPose = ConstructFullBodyPoseFromUpperBody(sourcePose);
                    createdFullBodyPose = true;
                }

                var blendedPose = _aiMotionSynthesizer.GetBlendedPose(fullBodyPose);
                _aiMotionSynthesizer.DrawVisualization(fullBodyPose, blendedPose, new Pose(transform.position, transform.rotation));

                if (ProvidedSkeletonType == OVRPlugin.BodyJointSet.UpperBody)
                {
                    if (createdFullBodyPose)
                    {
                        fullBodyPose.Dispose();
                    }
                    finalPose = ExtractUpperBodyFromFullBodyPose(blendedPose);

                    if (blendedPose.IsCreated)
                    {
                        blendedPose.Dispose();
                    }
                    if (sourcePose.IsCreated)
                    {
                        sourcePose.Dispose();
                    }
                }
                else
                {
                    if (sourcePose.IsCreated)
                    {
                        sourcePose.Dispose();
                    }
                    finalPose = blendedPose;
                }
            }

            if (_debugDrawSkeleton)
            {
                MeshDraw.DrawOVRSkeleton(this, _debugSkeletonColor);
            }

            return finalPose;
        }

        /// <summary>
        /// Pads upper body pose to full body length with identity transforms for lower body.
        /// </summary>
        private NativeArray<NativeTransform> ConstructFullBodyPoseFromUpperBody(NativeArray<NativeTransform> upperBodyPose)
        {
            const int fullBodyJointCount = (int)SkeletonData.FullBodyTrackingBoneId.End;
            const int upperBodyJointCount = (int)SkeletonData.BodyTrackingBoneId.End;

            var fullBodyPose = new NativeArray<NativeTransform>(fullBodyJointCount, Allocator.Temp);

            for (var i = 0; i < upperBodyJointCount && i < upperBodyPose.Length; i++)
            {
                fullBodyPose[i] = upperBodyPose[i];
            }

            for (var i = upperBodyJointCount; i < fullBodyJointCount; i++)
            {
                fullBodyPose[i] = NativeTransform.Identity();
            }

            return fullBodyPose;
        }

        /// <summary>
        /// Extracts upper body joints from a full body pose array.
        /// </summary>
        private NativeArray<NativeTransform> ExtractUpperBodyFromFullBodyPose(NativeArray<NativeTransform> fullBodyPose)
        {
            const int upperBodyJointCount = (int)SkeletonData.BodyTrackingBoneId.End;

            var upperBodyPose = new NativeArray<NativeTransform>(upperBodyJointCount, Allocator.Temp);

            for (var i = 0; i < upperBodyJointCount && i < fullBodyPose.Length; i++)
            {
                upperBodyPose[i] = fullBodyPose[i];
            }

            return upperBodyPose;
        }

        /// <inheritdoc />
        public virtual NativeArray<NativeTransform> GetSkeletonTPose()
        {
            var sourcePose = SkeletonUtilities.GetBindPoses(this);
            _skeletalChangedCount = _currentSkeletalChangeCount;
            if (_enableAIMotionSynthesizer && _aiMotionSynthesizer.IsInitialized)
            {
                _aiMotionSynthesizer.UpdateTPose(sourcePose);
            }
            return sourcePose;
        }

        /// <inheritdoc />
        public virtual string GetManifestation()
        {
            return _isValid && ProvidedSkeletonType == OVRPlugin.BodyJointSet.UpperBody ? HalfBodyManifestation : null;
        }

        /// <inheritdoc />
        public virtual bool IsPoseValid()
        {
            return _isValid;
        }

        /// <inheritdoc />
        public virtual bool IsNewTPoseAvailable()
        {
            if (_currentSkeletonType == ProvidedSkeletonType)
            {
                return _currentSkeletalChangeCount != _skeletalChangedCount;
            }

            _currentSkeletonType = ProvidedSkeletonType;
            return true;
        }
    }
}
