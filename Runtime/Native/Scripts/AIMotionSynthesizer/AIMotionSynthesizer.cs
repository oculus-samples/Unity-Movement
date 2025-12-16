// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using Meta.XR.Movement.Retargeting;
using Unity.Collections;
using UnityEngine;
using static Meta.XR.Movement.MSDKUtility;

namespace Meta.XR.Movement.AI
{
    /// <summary>
    /// AI Motion Synthesizer Engine - blends AI motion synthesizer with body tracking.
    /// Non-MonoBehaviour; instantiate and call Initialize(), Update(), GetBlendedPose() manually.
    /// </summary>
    public class AIMotionSynthesizer
    {
        /// <summary>
        /// Whether the engine is initialized and ready to process poses.
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Current blend factor between body tracking (0) and AI motion synthesizer (1).
        /// </summary>
        public float CurrentBlendFactor => _currentBlendFactor;

        private readonly AIMotionSynthesizerConfig _config;
        private readonly Transform _rootTransform;

        private bool _isInitialized;
        private IAIMotionSynthesizerInputProvider _runtimeInputProvider;
        private ulong _aiMotionSynthesizerHandle = MSDKAIMotionSynthesizer.INVALID_HANDLE;
        private float _currentBlendFactor;
        private int[] _parentIndices;
        private NativeArray<NativeTransform> _bodyPose;
        private NativeArray<NativeTransform> _blendedPose;
        private NativeTransform _aiMotionSynthesizerRootPose;

        /// <summary>
        /// Creates a new AI Motion Synthesizer engine instance.
        /// </summary>
        /// <param name="config">Configuration settings for AI motion synthesizer behavior.</param>
        /// <param name="rootTransform">Transform to apply root motion to.</param>
        public AIMotionSynthesizer(AIMotionSynthesizerConfig config, Transform rootTransform)
        {
            _config = config;
            _rootTransform = rootTransform;
        }

        /// <summary>
        /// Initializes the AI motion synthesizer system. Must be called before Update() or GetBlendedPose().
        /// </summary>
        /// <returns>True if initialization succeeded.</returns>
        public bool Initialize()
        {
            if (_isInitialized)
            {
                Debug.LogWarning("[AIMotionSynthesizerIntegration] Already initialized");
                return true;
            }

            CastInputProvider();

            if (!_config.Config || !_config.ModelAsset ||
                !MSDKAIMotionSynthesizer.CreateOrUpdateHandle(_config.Config.text, out _aiMotionSynthesizerHandle))
            {
                Debug.LogError("[AIMotionSynthesizerIntegration] Failed to create AIMotionSynthesizer handle");
                return false;
            }

            if (!MSDKAIMotionSynthesizer.Initialize(_aiMotionSynthesizerHandle, _config.ModelAsset.bytes, _config.GuidanceAsset?.bytes))
            {
                Debug.LogError("[AIMotionSynthesizerIntegration] Failed to initialize AIMotionSynthesizer");
                MSDKAIMotionSynthesizer.DestroyHandle(_aiMotionSynthesizerHandle);
                _aiMotionSynthesizerHandle = MSDKAIMotionSynthesizer.INVALID_HANDLE;
                return false;
            }

            if (!MSDKAIMotionSynthesizer.GetSkeletonInfo(_aiMotionSynthesizerHandle, SkeletonType.TargetSkeleton, out var skeletonInfo))
            {
                Debug.LogError("[AIMotionSynthesizerIntegration] Failed to get skeleton info from AIMotionSynthesizer handle");
                MSDKAIMotionSynthesizer.DestroyHandle(_aiMotionSynthesizerHandle);
                _aiMotionSynthesizerHandle = MSDKAIMotionSynthesizer.INVALID_HANDLE;
                return false;
            }

            _bodyPose = new NativeArray<NativeTransform>(skeletonInfo.JointCount, Allocator.Persistent);
            _isInitialized = true;
            return true;
        }

        /// <summary>
        /// Processes AI motion synthesizer for the current frame. Call each frame in LateUpdate.
        /// </summary>
        /// <param name="deltaTime">Time since last frame.</param>
        public void Update(float deltaTime)
        {
            if (!_isInitialized || _aiMotionSynthesizerHandle == MSDKAIMotionSynthesizer.INVALID_HANDLE)
            {
                return;
            }

            var velocity = Vector3.zero;
            var direction = Vector3.forward;
            bool isInputActive = false;

            if (_config.BlendMode == BlendMode.Manual)
            {
                velocity = _config.ManualVelocity;
                direction = _config.ManualDirection;
            }
            else if (_runtimeInputProvider != null)
            {
                velocity = _runtimeInputProvider.GetVelocity();
                direction = _runtimeInputProvider.GetDirection();
                isInputActive = _runtimeInputProvider.IsInputActive();
            }

            UpdateBlendFactor(isInputActive, deltaTime);

            if (!MSDKAIMotionSynthesizer.Process(_aiMotionSynthesizerHandle, deltaTime, direction, velocity))
            {
                Debug.LogWarning("[AIMotionSynthesizerIntegration] AIMotionSynthesizer processing failed");
                return;
            }

            MSDKAIMotionSynthesizer.Predict(_aiMotionSynthesizerHandle, deltaTime);
        }

        /// <summary>
        /// Computes a blended pose from body tracking and AI motion synthesizer.
        /// </summary>
        /// <param name="bodyTrackingPose">Input pose from body tracking.</param>
        /// <returns>Blended pose array, or original pose if blending unavailable.</returns>
        public NativeArray<NativeTransform> GetBlendedPose(NativeArray<NativeTransform> bodyTrackingPose)
        {
            if (!_isInitialized || _aiMotionSynthesizerHandle == MSDKAIMotionSynthesizer.INVALID_HANDLE)
            {
                return bodyTrackingPose;
            }

            if (!bodyTrackingPose.IsCreated)
            {
                return default;
            }

            _bodyPose.CopyFrom(bodyTrackingPose);

            bool success;
            if (_config.EnableSynthesizedStandingPose)
            {
                success = MSDKAIMotionSynthesizer.GetSynthesizedAIMotionSynthesizerPoseByRef(
                    _aiMotionSynthesizerHandle,
                    bodyTrackingPose,
                    1f,
                    ref _blendedPose,
                    out _aiMotionSynthesizerRootPose);

                if (!success)
                {
                    Debug.LogWarning("[AIMotionSynthesizerIntegration] GetSynthesizedAIMotionSynthesizerPose failed");
                    return bodyTrackingPose;
                }
            }
            else
            {
                success = MSDKAIMotionSynthesizer.GetBlendedAIMotionSynthesizerPoseByRef(
                    _aiMotionSynthesizerHandle,
                    bodyTrackingPose,
                    _config.UpperBodySource,
                    _config.LowerBodySource,
                    _currentBlendFactor,
                    ref _blendedPose,
                    out _aiMotionSynthesizerRootPose);

                if (!success)
                {
                    Debug.LogWarning("[AIMotionSynthesizerIntegration] GetBlendedAIMotionSynthesizerPose failed");
                    return bodyTrackingPose;
                }
            }

            return _blendedPose;
        }

        /// <summary>
        /// Applies root motion to the root transform based on <see cref="AIMotionSynthesizerConfig.RootMotionMode"/>.
        /// </summary>
        public void ApplyRootMotion()
        {
            switch (_config.RootMotionMode)
            {
                case RootMotionMode.None:
                    break;

                case RootMotionMode.ApplyFromReference:
                    if (_config.ReferenceTransform != null)
                    {
                        _rootTransform.position = _config.ReferenceTransform.position;
                        _rootTransform.rotation = _config.ReferenceTransform.rotation;
                    }
                    break;

                case RootMotionMode.ApplyRootMotion:
                    _rootTransform.position = _aiMotionSynthesizerRootPose.Position;
                    _rootTransform.rotation = _aiMotionSynthesizerRootPose.Orientation;
                    break;
            }
        }

        /// <summary>
        /// Draws debug visualization of the AI motion synthesizer skeleton.
        /// Only draws if <see cref="AIMotionSynthesizerConfig.DebugDrawAIMotionSynthesizer"/> is enabled.
        /// </summary>
        public void DrawVisualization(NativeArray<NativeTransform> bodyTrackingPose, NativeArray<NativeTransform> blendedPose, Pose rootTransform = default)
        {
            if (!_config.DebugDrawAIMotionSynthesizer)
            {
                return;
            }

            var jointCount = (int)SkeletonData.FullBodyTrackingBoneId.End;
            if (_parentIndices == null)
            {
                _parentIndices = new int[jointCount];
                for (int i = 0; i < jointCount; i++)
                {
                    _parentIndices[i] = (int)SkeletonData.ParentBoneId[i];
                }
            }

            // Draw AIMotionSynthesizer pose only
            MSDKAIMotionSynthesizer.GetBlendedAIMotionSynthesizerPose(
                _aiMotionSynthesizerHandle,
                _bodyPose,
                MSDKAIMotionSynthesizer.PoseSource.AIMotionSynthesizer,
                MSDKAIMotionSynthesizer.PoseSource.AIMotionSynthesizer,
                1.0f,
                out var aiMotionSynthesizerPose,
                out var rootPose);
            MeshDraw.DrawSkeleton(aiMotionSynthesizerPose, _parentIndices, _config.DebugAIMotionSynthesizerColor);
        }

        /// <summary>
        /// Re-validates and caches the input provider. Call when the input provider reference changes.
        /// </summary>
        public void ValidateInputProvider()
        {
            CastInputProvider();
        }

        /// <summary>
        /// Releases all native resources. Call when disposing of the engine.
        /// </summary>
        public void Dispose()
        {
            if (_bodyPose.IsCreated)
            {
                _bodyPose.Dispose();
            }

            if (_blendedPose.IsCreated)
            {
                _blendedPose.Dispose();
            }

            if (_aiMotionSynthesizerHandle != MSDKAIMotionSynthesizer.INVALID_HANDLE)
            {
                MSDKAIMotionSynthesizer.DestroyHandle(_aiMotionSynthesizerHandle);
                _aiMotionSynthesizerHandle = MSDKAIMotionSynthesizer.INVALID_HANDLE;
            }

            _isInitialized = false;
        }

        /// <summary>
        /// Updates the source reference T-pose for the AI Motion Synthesizer retargeting.
        /// This should be called when the target skeleton proportions change to ensure
        /// proper scaling of AI Motion Synthesizer poses.
        /// </summary>
        /// <param name="targetTPose">Target skeleton T-pose to compare against and scale to.</param>
        /// <returns>True if the update was successful, false otherwise.</returns>
        public bool UpdateTPose(NativeArray<NativeTransform> targetTPose)
        {
            if (!_isInitialized || _aiMotionSynthesizerHandle == MSDKAIMotionSynthesizer.INVALID_HANDLE)
            {
                return false;
            }

            return MSDKAIMotionSynthesizer.UpdateTPose(_aiMotionSynthesizerHandle, targetTPose);
        }

        private void UpdateBlendFactor(bool isInputActive, float deltaTime)
        {
            if (_config.BlendMode == BlendMode.Input)
            {
                if (_runtimeInputProvider != null)
                {
                    float targetBlendFactor = isInputActive ? 1f : 0f;
                    float blendTime = isInputActive ? _config.BlendInTime : _config.BlendOutTime;

                    if (blendTime > 0f)
                    {
                        _currentBlendFactor = Mathf.MoveTowards(_currentBlendFactor, targetBlendFactor, deltaTime / blendTime);
                    }
                    else
                    {
                        _currentBlendFactor = targetBlendFactor;
                    }
                }
                else
                {
                    _currentBlendFactor = 0f;
                }
            }
            else
            {
                _currentBlendFactor = _config.BlendFactor;
            }
        }

        private void CastInputProvider()
        {
            if (_config.InputProvider != null)
            {
                _runtimeInputProvider = _config.InputProvider as IAIMotionSynthesizerInputProvider;
                if (_runtimeInputProvider == null)
                {
                    Debug.LogError($"[AIMotionSynthesizerIntegration] MonoBehaviour {_config.InputProvider.name} does not implement IAIMotionSynthesizerInputProvider");
                }
            }
            else
            {
                _runtimeInputProvider = null;
                if (_config.BlendMode == BlendMode.Input)
                {
                    Debug.LogWarning("[AIMotionSynthesizerIntegration] BlendMode is set to Input but no InputProvider is assigned");
                }
            }
        }

    }
}
