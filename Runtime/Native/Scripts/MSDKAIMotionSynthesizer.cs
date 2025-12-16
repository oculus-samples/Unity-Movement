// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Profiling;
using static Meta.XR.Movement.MSDKUtility;
using Allocator = Unity.Collections.Allocator;

namespace Meta.XR.Movement
{
    /// <summary>
    /// The native AI Motion Synthesizer plugin containing motion matching functionality.
    /// This class provides AI Motion Synthesizer-specific operations for pose matching and processing,
    /// along with the ability to retrieve utility handles for extended Movement SDK functionality.
    /// </summary>
    public abstract partial class MSDKAIMotionSynthesizer
    {
        /// <summary>
        /// Static DLL name for AI Motion Synthesizer operations.
        /// </summary>
        private const string AI_MOTION_SYNTHESIZER_DLL = "MetaMovementSDK_AIMotionSynthesizer";

        /// <summary>
        /// Invalid Handle value.
        /// </summary>
        public const ulong INVALID_HANDLE = 0u;

        /// <summary>
        /// Options for which pose source to use for body regions.
        /// </summary>
        public enum PoseSource
        {
            /// <summary>Use full body tracking pose</summary>
            BodyTracking = 0,
            /// <summary>Use AI Motion Synthesizer pose</summary>
            AIMotionSynthesizer = 1
        }

        private static LogCallback _aiMotionSynthesizerLogCallback = null;

        private static class AIMotionSynthesizerApi
        {
            /**********************************************************
             *
             *               AIMotionSynthesizer Lifecycle Functions
             *
             **********************************************************/

            [DllImport(AI_MOTION_SYNTHESIZER_DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern Result metaMovementSDK_initializeAIMotionSynthesizerLogging(LogCallback logCallback);

            [DllImport(AI_MOTION_SYNTHESIZER_DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern Result metaMovementSDK_createOrUpdateAIMotionSynthesizerHandle(
                string config,
                out ulong handle);

            [DllImport(AI_MOTION_SYNTHESIZER_DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern Result metaMovementSDK_destroyAIMotionSynthesizerHandle(ulong handle);

            /**********************************************************
             *
             *               AI Motion Synthesizer Functions
             *
             **********************************************************/

            [DllImport(AI_MOTION_SYNTHESIZER_DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_initializeAIMotionSynthesizer(
                ulong handle,
                byte* modelBytes,
                int modelBytesLength,
                byte* guidanceBytes,
                int guidanceBytesLength);

            [DllImport(AI_MOTION_SYNTHESIZER_DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern Result metaMovementSDK_processAIMotionSynthesizer(
                ulong handle,
                float deltaTime,
                Vector3 direction,
                Vector3 velocity);

            [DllImport(AI_MOTION_SYNTHESIZER_DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_getAIMotionSynthesizerPose(
                ulong handle,
                NativeTransform* outputPose,
                NativeTransform* outputRootPose);

            [DllImport(AI_MOTION_SYNTHESIZER_DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_getAIMotionSynthesizerTPose(
                ulong handle,
                NativeTransform* outputTPose);

            [DllImport(AI_MOTION_SYNTHESIZER_DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern Result metaMovementSDK_getAIMotionSynthesizerSkeletonInfo(
                ulong handle,
                SkeletonType skeletonType,
                out SkeletonInfo outSkeletonInfo);

            [DllImport(AI_MOTION_SYNTHESIZER_DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern Result metaMovementSDK_predictAIMotionSynthesizer(ulong handle, float deltaTime);

            [DllImport(AI_MOTION_SYNTHESIZER_DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_getSynthesizedAIMotionSynthesizerPose(
                ulong handle,
                NativeTransform* bodyTrackingPose,
                float blendFactor,
                NativeTransform* outputBlendedPose,
                NativeTransform* outputRootPose);

            [DllImport(AI_MOTION_SYNTHESIZER_DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_getBlendedAIMotionSynthesizerPose(
                    ulong handle,
                    NativeTransform* bodyTrackingPose,
                    PoseSource upperBodySource,
                    PoseSource lowerBodySource,
                    float blendFactor,
                    NativeTransform* outputBlendedPose,
                    NativeTransform* outputRootPose);

            [DllImport(AI_MOTION_SYNTHESIZER_DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_updateAIMotionSynthesizerTPose(
                ulong handle,
                NativeTransform* targetTPose,
                int numJoints);
        }

        #region AIMotionSynthesizer Unity API

        /**********************************************************
         *
         *               AIMotionSynthesizer Lifecycle Functions
         *
         **********************************************************/

        /// <summary>
        /// Initializes logging for the AIMotionSynthesizer plugin.
        /// The AIMotionSynthesizer plugin has its own copy of UtilityContext (statically linked),
        /// so we need to initialize logging separately for the AIMotionSynthesizer DLL.
        /// </summary>
        private static bool InitializeAIMotionSynthesizerLogging()
        {
            Result success;
            _aiMotionSynthesizerLogCallback ??= HandleAIMotionSynthesizerLogCallback;

            using (new ProfilerScope(nameof(InitializeAIMotionSynthesizerLogging)))
            {
                success = AIMotionSynthesizerApi.metaMovementSDK_initializeAIMotionSynthesizerLogging(_aiMotionSynthesizerLogCallback);
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Handles log callbacks from the AIMotionSynthesizer native plugin.
        /// </summary>
        [AOT.MonoPInvokeCallback(typeof(LogCallback))]
        private static void HandleAIMotionSynthesizerLogCallback(LogLevel logLevel, IntPtr logMessage)
        {
            // Convert the message pointer to a string
            var message = Marshal.PtrToStringAnsi(logMessage);

            // Forward the message to Unity's logging system based on the log level
            switch (logLevel)
            {
                case LogLevel.Error:
                    Debug.LogError($"[MSDKAIMotionSynthesizer]{message}");
                    break;
                case LogLevel.Warn:
                    Debug.LogWarning($"[MSDKAIMotionSynthesizer]{message}");
                    break;
                case LogLevel.Info:
                case LogLevel.Debug:
                default:
                    Debug.Log($"[MSDKAIMotionSynthesizer]{message}");
                    break;
            }
        }

        /// <summary>
        /// Create or update an AIMotionSynthesizer handle using a config string.
        /// This method creates a handle for AIMotionSynthesizer operations that can be used for processing poses.
        /// </summary>
        /// <param name="config">The contents of a config in JSON format.</param>
        /// <param name="handle">The handle that can be used for accessing the resulting AIMotionSynthesizer config. Pass an existing handle to update it.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool CreateOrUpdateHandle(string config, out ulong handle)
        {
            InitializeLogging();
            var aiMotionSynthesizerLoggingInitialized = InitializeAIMotionSynthesizerLogging();
            if (!aiMotionSynthesizerLoggingInitialized)
            {
                Debug.LogWarning("[MSDKAIMotionSynthesizer] Failed to initialize AIMotionSynthesizer logging. Plugin may not be loaded.");
            }

            Result success;
            using (new ProfilerScope(nameof(CreateOrUpdateHandle)))
            {
                try
                {
                    success = AIMotionSynthesizerApi.metaMovementSDK_createOrUpdateAIMotionSynthesizerHandle(config, out handle);
                }
                catch (System.DllNotFoundException e)
                {
                    Debug.LogError($"[MSDKAIMotionSynthesizer] DLL not found. The AIMotionSynthesizer plugin is not loaded on this platform. Error: {e.Message}");
                    handle = INVALID_HANDLE;
                    return false;
                }
                catch (System.EntryPointNotFoundException e)
                {
                    Debug.LogError($"[MSDKAIMotionSynthesizer] Function not found in DLL. The AIMotionSynthesizer plugin may be incorrectly configured. Error: {e.Message}");
                    handle = INVALID_HANDLE;
                    return false;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[MSDKAIMotionSynthesizer] Unexpected error when creating AIMotionSynthesizer handle: {e.Message}");
                    handle = INVALID_HANDLE;
                    return false;
                }
            }

            if (success != Result.Success)
            {
                Debug.LogError($"[MSDKAIMotionSynthesizer] CreateOrUpdateHandle failed with result: {success}");
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Destroy the specified AIMotionSynthesizer handle instance.
        /// This releases all resources associated with the AIMotionSynthesizer handle and should be called
        /// when the handle is no longer needed to prevent memory leaks.
        /// </summary>
        /// <param name="handle">The AIMotionSynthesizer handle to be destroyed.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool DestroyHandle(ulong handle)
        {
            Result success;
            using (new ProfilerScope(nameof(DestroyHandle)))
            {
                success = AIMotionSynthesizerApi.metaMovementSDK_destroyAIMotionSynthesizerHandle(handle);
            }

            return success == Result.Success;
        }


        /**********************************************************
         *
         *               AIMotionSynthesizer Processing Functions
         *
         **********************************************************/

        /// <summary>
        /// Initializes the AIMotionSynthesizer component with provided model and provider data.
        /// </summary>
        /// <param name="handle">The handle to use for initialization.</param>
        /// <param name="modelBytes">Byte array containing the model data.</param>
        /// <param name="styleProviderBytes">Optional byte array containing style provider data.</param>
        /// <returns>True if initialization was successful, false otherwise.</returns>
        public static bool Initialize(
            UInt64 handle,
            byte[] modelBytes,
            byte[] styleProviderBytes = null)
        {
            if (modelBytes == null)
            {
                Debug.LogError("[MSDKAIMotionSynthesizer] Initialize: Model bytes cannot be null");
                return false;
            }

            if (modelBytes.Length == 0)
            {
                Debug.LogError("[MSDKAIMotionSynthesizer] Initialize: Model bytes cannot be empty");
                return false;
            }

            Result success;
            using (new ProfilerScope(nameof(Initialize)))
            {
                try
                {
                    unsafe
                    {
                        fixed (byte* modelPtr = modelBytes)
                        fixed (byte* stylePtr = styleProviderBytes)
                        {
                            success = AIMotionSynthesizerApi.metaMovementSDK_initializeAIMotionSynthesizer(
                                handle,
                                modelPtr,
                                modelBytes.Length,
                                stylePtr,
                                styleProviderBytes?.Length ?? 0);
                        }
                    }
                }
                catch (DllNotFoundException e)
                {
                    Debug.LogError($"[MSDKAIMotionSynthesizer] DLL not found during initialization. Error: {e.Message}");
                    return false;
                }
                catch (EntryPointNotFoundException e)
                {
                    Debug.LogError($"[MSDKAIMotionSynthesizer] Function not found in DLL during initialization. Error: {e.Message}");
                    return false;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[MSDKAIMotionSynthesizer] Unexpected error during initialization: {e.Message}");
                    return false;
                }
            }

            if (success != Result.Success)
            {
                Debug.LogError($"[MSDKAIMotionSynthesizer] Initialize failed with result: {success}");
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Process the AIMotionSynthesizer with the given motion parameters.
        /// This method schedules tasks and services instances but does NOT run prediction.
        /// Call Predict() separately at a fixed rate (e.g., 30Hz).
        /// </summary>
        /// <param name="handle">The handle to use for processing.</param>
        /// <param name="deltaTime">The time delta between frames in seconds.</param>
        /// <param name="direction">The direction vector (will be normalized internally).</param>
        /// <param name="velocity">The velocity vector in m/s.</param>
        /// <returns>True if processing was successful, false otherwise.</returns>
        public static bool Process(
            UInt64 handle,
            float deltaTime,
            Vector3 direction,
            Vector3 velocity)
        {
            Result success;
            using (new ProfilerScope(nameof(Process)))
            {
                success = AIMotionSynthesizerApi.metaMovementSDK_processAIMotionSynthesizer(
                    handle,
                    deltaTime,
                    direction,
                    velocity);
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Retrieves the current pose from the AIMotionSynthesizer system after processing.
        /// Note: The output pose size must match the skeleton joint count from the AIMotionSynthesizer configuration.
        /// This version allocates a temp NativeArray for the output.
        /// </summary>
        /// <param name="handle">The handle to use for retrieving the pose.</param>
        /// <param name="outputPose">The output array to write pose data to.</param>
        /// <param name="jointCount">The number of joints expected in the output pose.</param>
        /// <returns>True if retrieval was successful, false otherwise.</returns>
        public static bool GetPose(
            UInt64 handle,
            out NativeArray<NativeTransform> outputPose,
            int jointCount)
        {
            Result success;
            using (new ProfilerScope(nameof(GetPose)))
            {
                unsafe
                {
                    outputPose = new NativeArray<NativeTransform>(jointCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                    success = AIMotionSynthesizerApi.metaMovementSDK_getAIMotionSynthesizerPose(
                        handle,
                        outputPose.GetPtr(),
                        null);
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Retrieves the current pose from the AIMotionSynthesizer system after processing, including the root pose.
        /// The root pose contains XZ translation and yaw rotation extracted from the hips joint.
        /// Note: The output pose size must match the skeleton joint count from the AIMotionSynthesizer configuration.
        /// This version allocates a temp NativeArray for the output.
        /// </summary>
        /// <param name="handle">The handle to use for retrieving the pose.</param>
        /// <param name="outputPose">The output array to write pose data to (root joint will be zeroed).</param>
        /// <param name="outputRootPose">The extracted root pose (XZ translation + yaw rotation).</param>
        /// <param name="jointCount">The number of joints expected in the output pose.</param>
        /// <returns>True if retrieval was successful, false otherwise.</returns>
        public static bool GetPose(
            UInt64 handle,
            out NativeArray<NativeTransform> outputPose,
            out NativeTransform outputRootPose,
            int jointCount)
        {
            Result success;
            using (new ProfilerScope(nameof(GetPose)))
            {
                unsafe
                {
                    outputPose = new NativeArray<NativeTransform>(jointCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                    fixed (NativeTransform* rootPosePtr = &outputRootPose)
                    {
                        success = AIMotionSynthesizerApi.metaMovementSDK_getAIMotionSynthesizerPose(
                            handle,
                            outputPose.GetPtr(),
                            rootPosePtr);
                    }
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Retrieves the current pose from the AIMotionSynthesizer system after processing.
        /// This version uses a pre-allocated persistent NativeArray.
        /// </summary>
        /// <param name="handle">The handle to use for retrieving the pose.</param>
        /// <param name="outputPose">The pre-allocated output array to write pose data to.</param>
        /// <returns>True if retrieval was successful, false otherwise.</returns>
        public static bool GetPoseByRef(
            UInt64 handle,
            ref NativeArray<NativeTransform> outputPose)
        {
            Result success;
            using (new ProfilerScope(nameof(GetPoseByRef)))
            {
                unsafe
                {
                    success = AIMotionSynthesizerApi.metaMovementSDK_getAIMotionSynthesizerPose(
                        handle,
                        outputPose.GetPtr(),
                        null);
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Retrieves the current pose from the AIMotionSynthesizer system after processing, including the root pose.
        /// The root pose contains XZ translation and yaw rotation extracted from the hips joint.
        /// This version uses a pre-allocated persistent NativeArray.
        /// </summary>
        /// <param name="handle">The handle to use for retrieving the pose.</param>
        /// <param name="outputPose">The pre-allocated output array to write pose data to (root joint will be zeroed).</param>
        /// <param name="outputRootPose">The extracted root pose (XZ translation + yaw rotation).</param>
        /// <returns>True if retrieval was successful, false otherwise.</returns>
        public static bool GetPoseByRef(
            UInt64 handle,
            ref NativeArray<NativeTransform> outputPose,
            out NativeTransform outputRootPose)
        {
            Result success;
            using (new ProfilerScope(nameof(GetPoseByRef)))
            {
                unsafe
                {
                    fixed (NativeTransform* rootPosePtr = &outputRootPose)
                    {
                        success = AIMotionSynthesizerApi.metaMovementSDK_getAIMotionSynthesizerPose(
                            handle,
                            outputPose.GetPtr(),
                            rootPosePtr);
                    }
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Gets the T-pose by immediately running getPose to ensure proper retargeting.
        /// If no retargeting was performed, derives the T-pose from the source getPose data.
        /// Note: The output array size must match the skeleton joint count.
        /// This version allocates a temp NativeArray for the output.
        /// </summary>
        /// <param name="handle">The handle to use for retrieving the T-pose.</param>
        /// <param name="outputTPose">The output array to write T-pose data to.</param>
        /// <param name="jointCount">The number of joints expected in the T-pose.</param>
        /// <returns>True if retrieval was successful, false otherwise.</returns>
        public static bool GetTPose(
            UInt64 handle,
            out NativeArray<NativeTransform> outputTPose,
            int jointCount)
        {
            Result success;
            using (new ProfilerScope(nameof(GetTPose)))
            {
                unsafe
                {
                    outputTPose = new NativeArray<NativeTransform>(jointCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                    success = AIMotionSynthesizerApi.metaMovementSDK_getAIMotionSynthesizerTPose(
                        handle,
                        outputTPose.GetPtr());
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Gets the T-pose by immediately running getPose to ensure proper retargeting.
        /// If no retargeting was performed, derives the T-pose from the source getPose data.
        /// This version uses a pre-allocated persistent NativeArray.
        /// </summary>
        /// <param name="handle">The handle to use for retrieving the T-pose.</param>
        /// <param name="outputTPose">The pre-allocated output array to write T-pose data to.</param>
        /// <returns>True if retrieval was successful, false otherwise.</returns>
        public static bool GetTPoseByRef(
            UInt64 handle,
            ref NativeArray<NativeTransform> outputTPose)
        {
            Result success;
            using (new ProfilerScope(nameof(GetTPoseByRef)))
            {
                unsafe
                {
                    success = AIMotionSynthesizerApi.metaMovementSDK_getAIMotionSynthesizerTPose(
                        handle,
                        outputTPose.GetPtr());
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Gets skeleton information for the specified skeleton type from the AIMotionSynthesizer handle.
        /// This retrieves basic information about the skeleton structure, including the number of joints and blendshapes.
        /// Use this to understand the structure of a skeleton before performing operations on it.
        /// </summary>
        /// <param name="handle">The AIMotionSynthesizer handle to get the skeleton info from.</param>
        /// <param name="skeletonType">The type of skeleton (source or target) to get info from.</param>
        /// <param name="skeletonInfo">Output parameter that receives the skeleton information.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetSkeletonInfo(UInt64 handle, SkeletonType skeletonType, out SkeletonInfo skeletonInfo)
        {
            Result success;
            using (new ProfilerScope(nameof(GetSkeletonInfo)))
            {
                success = AIMotionSynthesizerApi.metaMovementSDK_getAIMotionSynthesizerSkeletonInfo(handle, skeletonType, out skeletonInfo);
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Runs neural network prediction for all instances.
        /// This method should be called at a fixed rate (e.g., 30Hz) independently from GetPose().
        /// It performs the neural network inference to generate future animation sequences.
        /// - GetPose() is called every frame to service instances
        /// - Predict() is called at a fixed rate (e.g., 30Hz) to run inference
        /// </summary>
        /// <param name="handle">The handle to use for prediction.</param>
        /// <param name="deltaTime">The time delta between frames in seconds.</param>
        /// <returns>True if prediction was successful, false otherwise.</returns>
        public static bool Predict(UInt64 handle, float deltaTime)
        {
            Result success;
            using (new ProfilerScope(nameof(Predict)))
            {
                success = AIMotionSynthesizerApi.metaMovementSDK_predictAIMotionSynthesizer(handle, deltaTime);
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Get a synthesized pose that blends between body tracking and AIMotionSynthesizer for upper body,
        /// while always using AIMotionSynthesizer for lower body.
        /// This function creates a synthesized pose where:
        /// - Lower body (legs) always uses AIMotionSynthesizer pose
        /// - Upper body (spine and above) blends between body tracking and AIMotionSynthesizer based on the blend factor
        /// - Hips position and rotation come from AIMotionSynthesizer
        ///
        /// Blend factor controls the upper body source:
        /// - 0 = Upper body from body tracking, lower body from AIMotionSynthesizer
        /// - 1 = Both upper and lower body from AIMotionSynthesizer (same as GetBlendedAIMotionSynthesizerPose
        ///       with both pose sources set to AIMotionSynthesizer)
        /// </summary>
        /// <param name="handle">The AIMotionSynthesizer handle to use for blending.</param>
        /// <param name="bodyTrackingPose">Input body tracking pose.</param>
        /// <param name="blendFactor">Blend factor controlling upper body source (0.0 = body tracking upper, 1.0 = AIMotionSynthesizer upper).</param>
        /// <param name="outputBlendedPose">Output synthesized pose (will be allocated if not already created).</param>
        /// <param name="outputRootPose">Output root pose (extracted from AIMotionSynthesizer).</param>
        /// <returns>True if synthesis was successful, false otherwise.</returns>
        public static bool GetSynthesizedAIMotionSynthesizerPose(
            UInt64 handle,
            NativeArray<NativeTransform> bodyTrackingPose,
            float blendFactor,
            out NativeArray<NativeTransform> outputBlendedPose,
            out NativeTransform outputRootPose)
        {
            if (!bodyTrackingPose.IsCreated)
            {
                Debug.LogError("[MSDKAIMotionSynthesizer] GetSynthesizedAIMotionSynthesizerPose: bodyTrackingPose must be created");
                outputBlendedPose = default;
                outputRootPose = default;
                return false;
            }

            int jointCount = bodyTrackingPose.Length;
            outputBlendedPose = new NativeArray<NativeTransform>(jointCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            Result success;
            using (new ProfilerScope(nameof(GetSynthesizedAIMotionSynthesizerPose)))
            {
                unsafe
                {
                    fixed (NativeTransform* rootPosePtr = &outputRootPose)
                    {
                        success = AIMotionSynthesizerApi.metaMovementSDK_getSynthesizedAIMotionSynthesizerPose(
                            handle,
                            bodyTrackingPose.GetPtr(),
                            blendFactor,
                            outputBlendedPose.GetPtr(),
                            rootPosePtr);
                    }
                }
            }

            if (success != Result.Success)
            {
                Debug.LogError($"[MSDKAIMotionSynthesizer] GetSynthesizedAIMotionSynthesizerPose failed with result: {success}");
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Get a synthesized pose that blends between body tracking and AIMotionSynthesizer for upper body,
        /// while always using AIMotionSynthesizer for lower body.
        /// This version uses pre-allocated persistent NativeArrays.
        ///
        /// Blend factor controls the upper body source:
        /// - 0 = Upper body from body tracking, lower body from AIMotionSynthesizer
        /// - 1 = Both upper and lower body from AIMotionSynthesizer (same as GetBlendedAIMotionSynthesizerPose
        ///       with both pose sources set to AIMotionSynthesizer)
        /// </summary>
        /// <param name="handle">The AIMotionSynthesizer handle to use for blending.</param>
        /// <param name="bodyTrackingPose">Input body tracking pose.</param>
        /// <param name="blendFactor">Blend factor controlling upper body source (0.0 = body tracking upper, 1.0 = AIMotionSynthesizer upper).</param>
        /// <param name="outputBlendedPose">Output synthesized pose (pre-allocated).</param>
        /// <param name="outputRootPose">Output root pose (extracted from AIMotionSynthesizer).</param>
        /// <returns>True if synthesis was successful, false otherwise.</returns>
        public static bool GetSynthesizedAIMotionSynthesizerPoseByRef(
            UInt64 handle,
            NativeArray<NativeTransform> bodyTrackingPose,
            float blendFactor,
            ref NativeArray<NativeTransform> outputBlendedPose,
            out NativeTransform outputRootPose)
        {
            if (!bodyTrackingPose.IsCreated)
            {
                Debug.LogError("[MSDKAIMotionSynthesizer] GetSynthesizedAIMotionSynthesizerPose: bodyTrackingPose must be created");
                outputRootPose = default;
                return false;
            }

            int jointCount = bodyTrackingPose.Length;

            // Allocate output array if needed
            if (!outputBlendedPose.IsCreated || outputBlendedPose.Length != jointCount)
            {
                if (outputBlendedPose.IsCreated)
                {
                    outputBlendedPose.Dispose();
                }
                outputBlendedPose = new NativeArray<NativeTransform>(jointCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            }

            Result success;
            using (new ProfilerScope(nameof(GetSynthesizedAIMotionSynthesizerPose)))
            {
                unsafe
                {
                    fixed (NativeTransform* rootPosePtr = &outputRootPose)
                    {
                        success = AIMotionSynthesizerApi.metaMovementSDK_getSynthesizedAIMotionSynthesizerPose(
                            handle,
                            bodyTrackingPose.GetPtr(),
                            blendFactor,
                            outputBlendedPose.GetPtr(),
                            rootPosePtr);
                    }
                }
            }

            if (success != Result.Success)
            {
                Debug.LogError($"[MSDKAIMotionSynthesizer] GetSynthesizedAIMotionSynthesizerPose failed with result: {success}");
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Blend body tracking and AIMotionSynthesizer poses based on upper/lower body options.
        /// This function aligns the AIMotionSynthesizer pose to the body tracking pose by the hips,
        /// then blends between the two poses based on the blend factor and body region settings.
        /// </summary>
        /// <param name="handle">The AIMotionSynthesizer handle to use for blending.</param>
        /// <param name="bodyTrackingPose">Input body tracking pose.</param>
        /// <param name="upperBodySource">Which pose to use for upper body (spine and above).</param>
        /// <param name="lowerBodySource">Which pose to use for lower body (hips and legs).</param>
        /// <param name="blendFactor">Blend factor (0.0 = body tracking only, 1.0 = use upper/lower body options).</param>
        /// <param name="outputBlendedPose">Output blended pose (will be allocated if not already created).</param>
        /// <param name="outputRootPose">Output root pose (extracted from AIMotionSynthesizer).</param>
        /// <returns>True if blending was successful, false otherwise.</returns>
        public static bool GetBlendedAIMotionSynthesizerPose(
            UInt64 handle,
            NativeArray<NativeTransform> bodyTrackingPose,
            PoseSource upperBodySource,
            PoseSource lowerBodySource,
            float blendFactor,
            out NativeArray<NativeTransform> outputBlendedPose,
            out NativeTransform outputRootPose)
        {
            if (!bodyTrackingPose.IsCreated)
            {
                Debug.LogError("[MSDKAIMotionSynthesizer] GetBlendedAIMotionSynthesizerPose: bodyTrackingPose must be created");
                outputBlendedPose = default;
                outputRootPose = default;
                return false;
            }

            int jointCount = bodyTrackingPose.Length;
            outputBlendedPose = new NativeArray<NativeTransform>(jointCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            Result success;
            using (new ProfilerScope(nameof(GetBlendedAIMotionSynthesizerPose)))
            {
                unsafe
                {
                    fixed (NativeTransform* rootPosePtr = &outputRootPose)
                    {
                        success = AIMotionSynthesizerApi.metaMovementSDK_getBlendedAIMotionSynthesizerPose(
                            handle,
                            bodyTrackingPose.GetPtr(),
                            upperBodySource,
                            lowerBodySource,
                            blendFactor,
                            outputBlendedPose.GetPtr(),
                            rootPosePtr);
                    }
                }
            }

            if (success != Result.Success)
            {
                Debug.LogError($"[MSDKAIMotionSynthesizer] GetBlendedAIMotionSynthesizerPose failed with result: {success}");
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Blend body tracking and AIMotionSynthesizer poses based on upper/lower body options.
        /// This function aligns the AIMotionSynthesizer pose to the body tracking pose by the hips,
        /// then blends between the two poses based on the blend factor and body region settings.
        /// This version uses pre-allocated persistent NativeArrays.
        /// </summary>
        /// <param name="handle">The AIMotionSynthesizer handle to use for blending.</param>
        /// <param name="bodyTrackingPose">Input body tracking pose.</param>
        /// <param name="upperBodySource">Which pose to use for upper body (spine and above).</param>
        /// <param name="lowerBodySource">Which pose to use for lower body (hips and legs).</param>
        /// <param name="blendFactor">Blend factor (0.0 = body tracking only, 1.0 = use upper/lower body options).</param>
        /// <param name="outputBlendedPose">Output blended pose (pre-allocated).</param>
        /// <param name="outputRootPose">Output root pose (extracted from AIMotionSynthesizer).</param>
        /// <returns>True if blending was successful, false otherwise.</returns>
        public static bool GetBlendedAIMotionSynthesizerPoseByRef(
            UInt64 handle,
            NativeArray<NativeTransform> bodyTrackingPose,
            PoseSource upperBodySource,
            PoseSource lowerBodySource,
            float blendFactor,
            ref NativeArray<NativeTransform> outputBlendedPose,
            out NativeTransform outputRootPose)
        {
            if (!bodyTrackingPose.IsCreated)
            {
                Debug.LogError("[MSDKAIMotionSynthesizer] GetBlendedAIMotionSynthesizerPose: bodyTrackingPose must be created");
                outputRootPose = default;
                return false;
            }

            int jointCount = bodyTrackingPose.Length;

            // Allocate output array if needed
            if (!outputBlendedPose.IsCreated || outputBlendedPose.Length != jointCount)
            {
                if (outputBlendedPose.IsCreated)
                {
                    outputBlendedPose.Dispose();
                }
                outputBlendedPose = new NativeArray<NativeTransform>(jointCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            }

            Result success;
            using (new ProfilerScope(nameof(GetBlendedAIMotionSynthesizerPose)))
            {
                unsafe
                {
                    fixed (NativeTransform* rootPosePtr = &outputRootPose)
                    {
                        success = AIMotionSynthesizerApi.metaMovementSDK_getBlendedAIMotionSynthesizerPose(
                            handle,
                            bodyTrackingPose.GetPtr(),
                            upperBodySource,
                            lowerBodySource,
                            blendFactor,
                            outputBlendedPose.GetPtr(),
                            rootPosePtr);
                    }
                }
            }

            if (success != Result.Success)
            {
                Debug.LogError($"[MSDKAIMotionSynthesizer] GetBlendedAIMotionSynthesizerPose failed with result: {success}");
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Update the source reference T-pose scale for the AIMotionSynthesizer retargeting.
        ///
        /// This function compares the passed-in body tracking T-pose with the source T-pose
        /// to calculate a scaling factor, then stores it for use during pose retargeting and blending.
        ///
        /// To prevent jittering from inconsistent pose lengths, only call this function when:
        /// - Initializing the AIMotionSynthesizer (scale starts at 1.0)
        /// - When the body tracking T-pose actually changes
        ///
        /// Do NOT call this function every frame, as varying pose lengths can cause jittering.
        /// </summary>
        /// <param name="handle">The AIMotionSynthesizer handle.</param>
        /// <param name="targetTPose">Body tracking T-pose transforms to calculate scale from.</param>
        /// <returns>True if the update was successful, false otherwise.</returns>
        public static bool UpdateTPose(
            UInt64 handle,
            NativeArray<NativeTransform> targetTPose)
        {
            if (!targetTPose.IsCreated)
            {
                Debug.LogError("[MSDKAIMotionSynthesizer] UpdateTPose: targetTPose must be created");
                return false;
            }

            Result success;
            using (new ProfilerScope(nameof(UpdateTPose)))
            {
                unsafe
                {
                    success = AIMotionSynthesizerApi.metaMovementSDK_updateAIMotionSynthesizerTPose(
                        handle,
                        targetTPose.GetPtr(),
                        targetTPose.Length);
                }
            }

            if (success != Result.Success)
            {
                Debug.LogError($"[MSDKAIMotionSynthesizer] UpdateTPose failed with result: {success}");
            }

            return success == Result.Success;
        }
        #endregion
    }
}
