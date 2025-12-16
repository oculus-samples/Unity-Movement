// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using Meta.XR.Movement.Retargeting;
using Unity.Collections;
using UnityEngine;
using static Meta.XR.Movement.MSDKUtility;

namespace Meta.XR.Movement.AI
{
    /// <summary>
    /// Source data provider that generates poses from the AI Motion Synthesizer system.
    /// Use with <see cref="Meta.XR.Movement.Retargeting.CharacterRetargeter"/> for AI motion synthesizer-driven characters.
    /// </summary>
    public class AIMotionSynthesizerSourceDataProvider : MonoBehaviour, ISourceDataProvider
    {
        [SerializeField]
        [Tooltip("JSON configuration file for the AIMotionSynthesizer skeleton data")]
        private TextAsset _config;

        [SerializeField]
        [Tooltip("Neural network model asset for AIMotionSynthesizer")]
        private TextAsset _modelAsset;

        [SerializeField]
        [Tooltip("Guidance asset for animation variations")]
        private TextAsset _guidanceAsset;

        [SerializeField]
        [Tooltip("Input provider (must implement IAIMotionSynthesizerInputProvider)")]
        private MonoBehaviour _inputProvider;

        [SerializeField]
        [Tooltip("Use manual velocity/direction instead of input provider")]
        private bool _useManualInput;

        [SerializeField]
        [Tooltip("Manual velocity (used when UseManualInput is true)")]
        private Vector3 _manualVelocity = Vector3.zero;

        [SerializeField]
        [Tooltip("Manual direction (used when UseManualInput is true)")]
        private Vector3 _manualDirection = Vector3.forward;

        private IAIMotionSynthesizerInputProvider _inputProviderCasted;
        private ulong _aiMotionSynthesizerHandle = MSDKAIMotionSynthesizer.INVALID_HANDLE;
        private NativeArray<NativeTransform> _currentPose;
        private NativeArray<NativeTransform> _tPose;
        private bool _isPoseValid;

        protected virtual void Awake()
        {
            _inputProviderCasted = _inputProvider as IAIMotionSynthesizerInputProvider;

            if (!_config)
            {
                Debug.LogError("[AIMotionSynthesizerSourceDataProvider] Config asset is missing");
                return;
            }

            if (!_modelAsset)
            {
                Debug.LogError("[AIMotionSynthesizerSourceDataProvider] Model asset is missing");
                return;
            }

            if (!MSDKAIMotionSynthesizer.CreateOrUpdateHandle(_config.text, out _aiMotionSynthesizerHandle))
            {
                Debug.LogError("[AIMotionSynthesizerSourceDataProvider] Failed to create AIMotionSynthesizer handle");
                return;
            }

            if (!MSDKAIMotionSynthesizer.Initialize(_aiMotionSynthesizerHandle, _modelAsset.bytes, _guidanceAsset?.bytes))
            {
                Debug.LogError("[AIMotionSynthesizerSourceDataProvider] Failed to initialize AIMotionSynthesizer");
                MSDKAIMotionSynthesizer.DestroyHandle(_aiMotionSynthesizerHandle);
                _aiMotionSynthesizerHandle = MSDKAIMotionSynthesizer.INVALID_HANDLE;
                return;
            }

            if (!MSDKAIMotionSynthesizer.GetSkeletonInfo(_aiMotionSynthesizerHandle, SkeletonType.TargetSkeleton, out var info))
            {
                Debug.LogError("[AIMotionSynthesizerSourceDataProvider] Failed to get skeleton info");
                MSDKAIMotionSynthesizer.DestroyHandle(_aiMotionSynthesizerHandle);
                _aiMotionSynthesizerHandle = MSDKAIMotionSynthesizer.INVALID_HANDLE;
                return;
            }

            _currentPose = new NativeArray<NativeTransform>(info.JointCount, Allocator.Persistent);
            _tPose = new NativeArray<NativeTransform>(info.JointCount, Allocator.Persistent);
            MSDKAIMotionSynthesizer.GetTPoseByRef(_aiMotionSynthesizerHandle, ref _tPose);
        }

        protected virtual void Update()
        {
            if (_aiMotionSynthesizerHandle == MSDKAIMotionSynthesizer.INVALID_HANDLE)
            {
                return;
            }

            // Use smoothDeltaTime for consistent timing across platforms.
            // Raw deltaTime can vary significantly on Android due to thermal throttling,
            // background processes, and variable refresh rates, causing pose jittering.
            var dt = Time.smoothDeltaTime;
            var velocity = _useManualInput ? _manualVelocity : (_inputProviderCasted?.GetVelocity() ?? Vector3.zero);
            var direction = _useManualInput ? _manualDirection : (_inputProviderCasted?.GetDirection() ?? Vector3.forward);

            if (MSDKAIMotionSynthesizer.Process(_aiMotionSynthesizerHandle, dt, direction, velocity))
            {
                MSDKAIMotionSynthesizer.Predict(_aiMotionSynthesizerHandle, dt);
            }
        }

        protected virtual void OnDestroy()
        {
            if (_currentPose.IsCreated)
            {
                _currentPose.Dispose();
            }

            if (_tPose.IsCreated)
            {
                _tPose.Dispose();
            }

            if (_aiMotionSynthesizerHandle != MSDKAIMotionSynthesizer.INVALID_HANDLE)
            {
                MSDKAIMotionSynthesizer.DestroyHandle(_aiMotionSynthesizerHandle);
            }
        }

        private void Reset()
        {
            AutoFindInputProvider();
            AutoLoadDefaultAssets();
        }

        private void OnValidate()
        {
            _inputProviderCasted = _inputProvider as IAIMotionSynthesizerInputProvider;

            if (_inputProvider == null)
            {
                AutoFindInputProvider();
            }
            AutoLoadDefaultAssets();
        }

        private void AutoFindInputProvider()
        {
            if (_inputProvider == null)
            {
                var inputProviders = GetComponents<MonoBehaviour>();
                foreach (var component in inputProviders)
                {
                    if (component is IAIMotionSynthesizerInputProvider)
                    {
                        _inputProvider = component;
                        _inputProviderCasted = component as IAIMotionSynthesizerInputProvider;
#if UNITY_EDITOR
                        if (!Application.isPlaying)
                        {
                            UnityEditor.EditorUtility.SetDirty(this);
                        }
#endif
                        break;
                    }
                }
            }
        }

        private void AutoLoadDefaultAssets()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                return;
            }

            bool changed = false;

            if (_config == null)
            {
                _config = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>("Packages/com.meta.xr.sdk.movement/Runtime/Native/Data/AIMotionSynthesizerSkeletonData.json");
                if (_config != null)
                {
                    changed = true;
                }
            }

            if (_modelAsset == null)
            {
                _modelAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>("Packages/com.meta.xr.sdk.movement/Runtime/Native/Data/AIMotionSynthesizerModel.bytes");
                if (_modelAsset != null)
                {
                    changed = true;
                }
            }

            if (_guidanceAsset == null)
            {
                _guidanceAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>("Packages/com.meta.xr.sdk.movement/Runtime/Native/Data/AIMotionSynthesizerGuidance.bytes");
                if (_guidanceAsset != null)
                {
                    changed = true;
                }
            }

            if (changed)
            {
                UnityEditor.EditorUtility.SetDirty(this);
            }
#endif
        }

        /// <summary>
        /// Gets the current AI motion synthesizer pose.
        /// </summary>
        /// <returns>Native array of joint transforms in AI motion synthesizer skeleton order.</returns>
        public virtual NativeArray<NativeTransform> GetSkeletonPose()
        {
            _isPoseValid = MSDKAIMotionSynthesizer.GetPoseByRef(_aiMotionSynthesizerHandle, ref _currentPose);
            return _currentPose;
        }

        /// <summary>
        /// Gets the T-pose for the AI motion synthesizer skeleton.
        /// </summary>
        public virtual NativeArray<NativeTransform> GetSkeletonTPose() => _tPose;

        /// <summary>
        /// Gets the manifestation string. Returns null for AI motion synthesizer.
        /// </summary>
        public virtual string GetManifestation() => null;

        /// <summary>
        /// Whether the last pose retrieval succeeded.
        /// </summary>
        public virtual bool IsPoseValid() => _isPoseValid;

        /// <summary>
        /// Whether a new T-pose is available. Always false for AI motion synthesizer.
        /// </summary>
        public virtual bool IsNewTPoseAvailable() => false;
    }
}
