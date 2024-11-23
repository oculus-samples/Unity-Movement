// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Profiling;

namespace Meta.XR.Movement
{
    /// <summary>
    /// The native utility plugin containing extended Movement SDK functionality.
    /// </summary>
    public abstract class NativeUtilityPlugin
    {
        /// <summary>
        /// Invalid Handle value.
        /// </summary>
        public const UInt64 INVALID_HANDLE = 0u;

        /// <summary>
        /// Invalid Joint index value.
        /// </summary>
        public const int INVALID_JOINT_INDEX = -1;

        /// <summary>
        /// Invalid BlendShape index value.
        /// </summary>
        public const int INVALID_BLENDSHAPE_INDEX = -1;

        /// <summary>
        /// Enum for native plugin results.
        /// </summary>
        public enum Result
        {
            // Generic failure.
            Failure = 0,

            // Specific failures.
            Failure_ConfigNull = -1000,
            Failure_ConfigCannotParse = -1001,
            Failure_ConfigInvalid = -1002,
            Failure_HandleInvalid = -1003,
            Failure_Initialization = -1004,
            Failure_InsufficientSize = -1005,
            Failure_WriteOutputNull = -1006,
            Failure_RequiredParameterNull = -1007,
            Failure_InvalidData = -1008,

            // Success.
            Success = 1,
        }

        /// <summary>
        /// Enum for compression type.
        /// </summary>
        public enum CompressionType
        {
            Compressed = 0,
            CompressedWithBoneLengths = 1
        }


        /// <summary>
        /// Option for APIs set/get attributes relative to a skeleton.
        /// </summary>
        public enum SkeletonType : int
        {
            /// <summary>
            /// Parameter for APIs set/get attributes relative to the source skeleton.
            /// </summary>
            SourceSkeleton = 0,

            /// <summary>
            /// Parameter for APIs set/get attributes relative to the target skeleton,
            /// </summary>
            TargetSkeleton = 1,
        }

        /// <summary>
        /// Parameter for APIs set/get a T-Pose type.
        /// </summary>
        public enum SkeletonTPoseType
        {
            /// <summary>
            /// Parameter for APIs set/get the current frame/state T-Pose.
            /// </summary>
            CurrentTPose = 0,

            /// <summary>
            /// Parameter for APIs set/get the source/target Minimum T-Pose.
            /// </summary>
            MinTPose = 1,

            /// <summary>
            /// Parameter for APIs set/get the source/target Maximum T-Pose.
            /// </summary>
            MaxTPose = 2,

            /// <summary>
            /// Parameter for APIs set/get the target Unscaled T-Pose.
            /// </summary>
            UnscaledTPose = 3,
        }

        /// <summary>
        /// Contains information about the skeleton type, number of joints, and number of blendshapes.
        /// </summary>
        [StructLayout(LayoutKind.Sequential), Serializable]
        public struct SkeletonInfo
        {
            /// <summary>
            /// The type of skeleton.
            /// </summary>
            public SkeletonType Type;

            /// <summary>
            /// The number of joints.
            /// </summary>
            public int JointCount;

            /// <summary>
            /// The number of blendshapes.
            /// </summary>
            public int BlendShapeCount;

            /// <summary>
            /// Constructor for <see cref="SkeletonInfo"/>.
            /// </summary>
            /// <param name="type"><see cref="Type"/></param>
            /// <param name="jointCount"><see cref="JointCount"/></param>
            /// <param name="blendShapeCount"><see cref="BlendShapeCount"/></param>
            public SkeletonInfo(SkeletonType type, int jointCount, int blendShapeCount)
            {
                Type = type;
                JointCount = jointCount;
                BlendShapeCount = blendShapeCount;
            }

            /// <summary>
            /// String output for the <see cref="SkeletonInfo"/> struct.
            /// </summary>
            /// <returns>The string output for the SkeletonInfo struct.</returns>
            public override string ToString()
            {
                return $"SkeletonType({Type}) " +
                       $"JointCount({JointCount}) " +
                       $"BlendShapeCount({BlendShapeCount})";
            }
        }

        /// <summary>
        /// Representation of a native transform, containing information about the orientation,
        /// position, and scale for a transform.
        /// </summary>
        [StructLayout(LayoutKind.Sequential), Serializable]
        public struct NativeTransform
        {
            /// <summary>
            /// The transform orientation.
            /// </summary>
            public Quaternion Orientation;

            /// <summary>
            /// The transform position.
            /// </summary>
            public Vector3 Position;

            /// <summary>
            /// The transform scale.
            /// </summary>
            public Vector3 Scale;

            /// <summary>
            /// Constructor for <see cref="NativeTransform"/>.
            /// </summary>
            /// <param name="orientation"><see cref="Orientation"/></param>
            /// <param name="position"><see cref="Position"/></param>
            public NativeTransform(Quaternion orientation, Vector3 position)
            {
                Orientation = orientation;
                Position = position;
                Scale = Vector3.one;
            }

            /// <summary>
            /// Constructor for <see cref="NativeTransform"/>.
            /// </summary>
            /// <param name="orientation"><see cref="Orientation"/></param>
            /// <param name="position"><see cref="Position"/></param>
            /// <param name="scale"><see cref="Scale"/></param>
            public NativeTransform(Quaternion orientation, Vector3 position, Vector3 scale)
            {
                Orientation = orientation;
                Position = position;
                Scale = scale;
            }

            /// <summary>
            /// Constructor for <see cref="NativeTransform"/>.
            /// </summary>
            /// <param name="pose">The pose to be converted.</param>
            public NativeTransform(Pose pose)
            {
                Orientation = pose.rotation;
                Position = pose.position;
                Scale = Vector3.one;
            }

            /// <summary>
            /// Implicit conversion from <see cref="Pose"/> to <see cref="NativeTransform"/>.
            /// </summary>
            /// <param name="pose">The pose to be converted.</param>
            /// <returns>The native transform equivalent to the pose.</returns>
            public static implicit operator NativeTransform(Pose pose)
            {
                return new NativeTransform(pose);
            }

            /// <summary>
            /// The identity transform
            /// (orientation = Quaternion.identity, position = Vector3.zero, scale = Vector3.one).
            /// </summary>
            /// <returns>The identity transform.</returns>
            public static NativeTransform Identity()
            {
                return new NativeTransform(Quaternion.identity, Vector3.zero, Vector3.one);
            }

            /// <summary>
            /// String output for the <see cref="NativeTransform"/> struct.
            /// </summary>
            /// <returns>The string output for the <see cref="NativeTransform"/> struct.</returns>
            public override string ToString()
            {
                return $"Pos({Position.x:F3},{Position.y:F3},{Position.z:F3}), " +
                       $"Rot({Orientation.x:F3},{Orientation.y:F3},{Orientation.z:F3},{Orientation.w:F3}), " +
                       $"Scale({Scale.x:F3},{Scale.y:F3},{Scale.z:F3})";
            }
        }

        /// <summary>
        /// Contains information about the coordinate space for a config.
        /// </summary>
        [StructLayout(LayoutKind.Sequential), Serializable]
        public struct CoordinateSpace
        {
            /// <summary>
            /// The representation of the up vector in this coordinate space.
            /// </summary>
            public Vector3 Up;

            /// <summary>
            /// The representation of the forward vector in this coordinate space.
            /// </summary>
            public Vector3 Forward;

            /// <summary>
            /// The representation of the right vector in this coordinate space.
            /// </summary>
            public Vector3 Right;

            /// <summary>
            /// Constructor for <see cref="CoordinateSpace"/>.
            /// </summary>
            /// <param name="up"><see cref="Up"/>.</param>
            /// <param name="forward"><see cref="Forward"/></param>
            /// <param name="right"><see cref="Right"/></param>
            public CoordinateSpace(Vector3 up, Vector3 forward, Vector3 right)
            {
                Up = up;
                Forward = forward;
                Right = right;
            }

            /// <summary>
            /// String output for the <see cref="CoordinateSpace"/> struct.
            /// </summary>
            /// <returns>String output.</returns>
            public override string ToString()
            {
                return $"Up({Up.x:F3},{Up.y:F3},{Up.z:F3}), " +
                       $"Forward({Forward.x:F3},{Forward.y:F3},{Forward.z:F3}, " +
                       $"Right({Right.x:F3},{Right.y:F3},{Right.z:F3})";
            }
        }

        /// <summary>
        /// Contains information about the pose for a joint.
        /// </summary>
        [StructLayout(LayoutKind.Sequential), Serializable]
        public struct SerializedJointPose
        {
            /// <summary>
            /// The orientation of the joint.
            /// </summary>
            public Quaternion Orientation;

            /// <summary>
            /// The position of the joint.
            /// </summary>
            public Vector3 Position;

            /// <summary>
            /// The length of the joint.
            /// </summary>
            public float Length;

            /// <summary>
            /// Constructor for <see cref="SerializedJointPose"/>.
            /// </summary>
            /// <param name="orientation"><see cref="Orientation"/></param>
            /// <param name="position"><see cref="Position"/></param>
            /// <param name="length"><see cref="Length"/></param>
            public SerializedJointPose(Quaternion orientation, Vector3 position, float length)
            {
                Orientation = orientation;
                Position = position;
                Length = length;
            }

            /// <summary>
            /// Constructor for <see cref="SerializedJointPose"/> using a <see cref="Pose"/>.
            /// </summary>
            /// <param name="pose">The pose for the joint.</param>
            public SerializedJointPose(Pose pose)
            {
                Orientation = pose.rotation;
                Position = pose.position;
                Length = 0.0f;
            }

            /// <summary>
            /// Get the identity pose for a joint.
            /// </summary>
            /// <returns>The identity pose for a joint.</returns>
            public static SerializedJointPose Identity()
            {
                return new SerializedJointPose(Quaternion.identity, Vector3.zero, 0.0f);
            }

            /// <summary>
            /// String output for the <see cref="SerializedJointPose"/> struct.
            /// </summary>
            /// <returns>String output.</returns>
            public override string ToString()
            {
                return $"Pos({Position.x:F3},{Position.y:F3},{Position.z:F3}), " +
                       $"Rot({Orientation.x:F3},{Orientation.y:F3},{Orientation.z:F3},{Orientation.w:F3})";
            }
        }

        /// <summary>
        /// Contains information about a blendshape.
        /// </summary>
        [StructLayout(LayoutKind.Sequential), Serializable]
        public struct SerializedShapePose
        {
            /// <summary>
            /// The weight of the blendshape.
            /// </summary>
            public float Weight;

            /// <summary>
            /// Constructor for the <see cref="SerializedShapePose"/>.
            /// </summary>
            /// <param name="weight"><see cref="Weight"/></param>
            public SerializedShapePose(float weight)
            {
                Weight = weight;
            }

            /// <summary>
            /// String output for the <see cref="SerializedShapePose"/>
            /// </summary>
            /// <returns>String output.</returns>
            public override string ToString()
            {
                return $"Weight({Weight:F3})";
            }
        }

        /// <summary>
        /// Profiler scope for measuring performance around a block of code.
        /// </summary>
        private struct ProfilerScope : IDisposable
        {
            /// <summary>
            /// Constructor for <see cref="ProfilerScope"/>.
            /// </summary>
            /// <param name="name">The name of the profiler sample.</param>
            public ProfilerScope(string name) => Profiler.BeginSample(name);

            void IDisposable.Dispose() => Profiler.EndSample();
        }

        /// <summary>
        /// Static DLL name.
        /// </summary>
        private const string DLL = "MetaMovementSDK_Utility";

        /// <summary>
        /// Interface for DLL calls.
        /// </summary>
        private abstract class Api
        {
            /**********************************************************
            *
            *               Lifecycle Functions
            *
            **********************************************************/

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern Result metaMovementSDK_createOrUpdateHandle(string config, out UInt64 handle);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern Result metaMovementSDK_destroy(UInt64 handle);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_createOrUpdateUtilityConfig(
                string configName,
                string[] sourceBlendshapeNames,
                string[] sourceJointNames,
                string[] sourceParentJointNames,
                string[] sourceKnownJoints,
                NativeTransform* sourceMinTPose,
                NativeTransform* sourceMaxTPose,
                int sourceBlendShapeCount,
                int sourceJointCount,
                string[] targetBlendshapeNames,
                string[] targetJointNames,
                string[] targetParentJointNames,
                string[] targetKnownJoints,
                NativeTransform* targetUnscaledTPose,
                NativeTransform* targetMinTPose,
                NativeTransform* targetMaxTPose,
                int targetBlendShapeCount,
                int targetJointCount,
                out UInt64 handle);

            /**********************************************************
            *
            *               Query Functions
            *
            **********************************************************/

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_getConfigName(
                UInt64 handle,
                byte* outBuffer,
                out int inOutBufferSize);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern Result metaMovementSDK_getSkeletonInfo(
                UInt64 handle,
                SkeletonType skeletonType,
                out SkeletonInfo outSkeletonInfo);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_getBlendShapeNames(
                UInt64 handle,
                SkeletonType skeletonType,
                byte* outBuffer,
                out int inOutBufferSize,
                void* outUnusedBlendShapeNames,
                out int inOutNumBlendShapeNames);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_getJointNames(
                UInt64 handle,
                SkeletonType skeletonType,
                byte* outBuffer,
                out int inOutBufferSize,
                void* outUnusedJointNames,
                out int inOutNumJointNames);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_getParentJointIndexes(
                UInt64 handle,
                SkeletonType skeletonType,
                int* outJointIndexArray,
                out int inOutNumJoints);

            /**********************************************************
            *
            *               Serialization Functions
            *
            **********************************************************/

            // Configuration information.
            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern Result metaMovementSDK_setCompressionType(UInt64 handle, int compressionType);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern Result metaMovementSDK_setPositionThreshold(UInt64 handle, float threshold);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern Result metaMovementSDK_setRotationAngleThreshold(UInt64 handle, float threshold);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern Result metaMovementSDK_setShapeThreshold(UInt64 handle, float threshold);

            // Serialization.
            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern Result metaMovementSDK_createSnapshot(
                UInt64 handle,
                int baselineAck,
                double timestamp);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_snapshotBody(
                UInt64 handle,
                void* bodyPose,
                void* bodyIndices,
                int numberOfBodyIndices);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_snapshotFace(
                UInt64 handle,
                void* facePose,
                void* faceIndices,
                int numberOfFaceIndices);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern Result metaMovementSDK_getSnapshotSize(
                UInt64 handle,
                out UInt64 bytes);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_serializeSnapshot(UInt64 handle, void* data);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_deserializeSnapshot(
                UInt64 handle,
                void* data,
                out double timestamp,
                out int compressionType,
                out int ack,
                void* bodyPose,
                void* facePose);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_getInterpolatedBodyPose(
                UInt64 handle,
                void* bodyPose,
                double timestamp);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_getInterpolatedFacePose(
                UInt64 handle,
                void* facePose,
                double timestamp);

            /**********************************************************
            *
            *               Tool and Data Functions
            *
            **********************************************************/

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_writeConfigDataToJSON(
                UInt64 handle,
                CoordinateSpace* optionalCoordinateSpace,
                byte* outBuffer,
                out int inOutBufferSize);

        }

        #region Unity API
        /**********************************************************
        *
        *               Lifecycle Functions
        *
        **********************************************************/

        /// <summary>
        /// Create or update a config and return a handle for accessing the result.
        /// </summary>
        /// <param name="configName">The name of the config.</param>
        /// <param name="sourceBlendshapeNames">An array of source blendshape names.</param>
        /// <param name="sourceJointNames">An array of source joint names.</param>
        /// <param name="sourceParentJointNames">An array of source parent joint names.</param>
        /// <param name="sourceKnownJoints">An array of source known joint names.</param>
        /// <param name="sourceMinTPose">The source min T-pose.</param>
        /// <param name="sourceMaxTPose">The source max T-pose</param>
        /// <param name="targetBlendshapeNames">An array of target blendshape names.</param>
        /// <param name="targetJointNames">An array of target joint names.</param>
        /// <param name="targetParentJointNames">An array of target parent joint names.</param>
        /// <param name="targetKnownJoints">An array of target known joint names.</param>
        /// <param name="targetUnscaledTPose">The target unscaled T-pose.</param>
        /// <param name="targetMinTPose">The target min T-pose.</param>
        /// <param name="targetMaxTPose">The target max T-pose.</param>
        /// <param name="handle">The handle that can be used for accessing the resulting config.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool CreateOrUpdateUtilityConfig(
            string configName,
            string[] sourceBlendshapeNames,
            string[] sourceJointNames,
            string[] sourceParentJointNames,
            string[] sourceKnownJoints,
            NativeArray<NativeTransform> sourceMinTPose,
            NativeArray<NativeTransform> sourceMaxTPose,
            string[] targetBlendshapeNames,
            string[] targetJointNames,
            string[] targetParentJointNames,
            string[] targetKnownJoints,
            NativeArray<NativeTransform> targetUnscaledTPose,
            NativeArray<NativeTransform> targetMinTPose,
            NativeArray<NativeTransform> targetMaxTPose,
            out UInt64 handle)
        {
            Result success;
            using (new ProfilerScope(nameof(CreateOrUpdateUtilityConfig)))
            {
                unsafe
                {
                    success = Api.metaMovementSDK_createOrUpdateUtilityConfig(
                        configName,
                        sourceBlendshapeNames,
                        sourceJointNames,
                        sourceParentJointNames,
                        sourceKnownJoints,
                        sourceMinTPose.GetPtr(),
                        sourceMaxTPose.GetPtr(),
                        sourceBlendshapeNames.Length,
                        sourceJointNames.Length,
                        targetBlendshapeNames,
                        targetJointNames,
                        targetParentJointNames,
                        targetKnownJoints,
                        targetUnscaledTPose.GetPtr(),
                        targetMinTPose.GetPtr(),
                        targetMaxTPose.GetPtr(),
                        targetBlendshapeNames.Length,
                        targetJointNames.Length,
                        out handle);
                }
            }
            return success == Result.Success;
        }

        /// <summary>
        /// Creates or updates a handle using a config string.
        /// </summary>
        /// <param name="config">The contents of a config.</param>
        /// <param name="handle">The handle that can be used for accessing the resulting config.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool CreateOrUpdateHandle(string config, out UInt64 handle)
        {
            Result success;
            using (new ProfilerScope(nameof(CreateOrUpdateHandle)))
            {
                success = Api.metaMovementSDK_createOrUpdateHandle(config, out handle);
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Destroy the specified handle instance.
        /// </summary>
        /// <param name="handle">The handle to be destroyed.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool DestroyHandle(UInt64 handle)
        {
            Result success;
            using (new ProfilerScope(nameof(DestroyHandle)))
            {
                success = Api.metaMovementSDK_destroy(handle);
            }

            return success == Result.Success;
        }

        /**********************************************************
        *
        *               Query Functions
        *
        **********************************************************/

        /// <summary>
        /// Get the name of the config for a handle.
        /// </summary>
        /// <param name="handle">The handle to get the config name from.</param>
        /// <param name="configName">The name of the config.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetConfigName(UInt64 handle, out string configName)
        {
            Result success;
            configName = string.Empty;
            using (new ProfilerScope(nameof(GetConfigName)))
            {
                unsafe
                {
                    success = Api.metaMovementSDK_getConfigName(handle, null, out var stringLength);
                    if (success == Result.Success)
                    {
                        var nameBuffer = stackalloc byte[stringLength];
                        success = Api.metaMovementSDK_getConfigName(handle, nameBuffer, out stringLength);
                        if (success == Result.Success)
                        {
                            configName = Marshal.PtrToStringAnsi((IntPtr)nameBuffer, stringLength);
                        }
                    }
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Get the skeleton info for a handle.
        /// </summary>
        /// <param name="handle">The handle to get the skeleton info from.</param>
        /// <param name="skeletonType">The type of skeleton to get info from.</param>
        /// <param name="skeletonInfo">The skeleton info.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetSkeletonInfo(UInt64 handle, SkeletonType skeletonType, out SkeletonInfo skeletonInfo)
        {
            Result success;
            using (new ProfilerScope(nameof(GetConfigName)))
            {
                success = Api.metaMovementSDK_getSkeletonInfo(handle, skeletonType, out skeletonInfo);
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Get the names of all blendshapes for a skeleton type.
        /// </summary>
        /// <param name="handle">The handle to get the blendshapes from.</param>
        /// <param name="skeletonType">The type of skeleton to get the blendshape names from.</param>
        /// <param name="blendShapeNames">The array of blendshape names.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetBlendShapeNames(UInt64 handle, SkeletonType skeletonType, out string[] blendShapeNames)
        {
            Result success;
            // Assign an Empty String to the output array
            blendShapeNames = Array.Empty<string>();
            using (new ProfilerScope(nameof(GetBlendShapeNames)))
            {
                unsafe
                {
                    success = Api.metaMovementSDK_getBlendShapeNames(handle, skeletonType, null, out var bufferSize, null, out var nameCount);
                    if (success == Result.Success)
                    {
                        var nameBuffer = stackalloc byte[bufferSize];
                        success = Api.metaMovementSDK_getBlendShapeNames(handle, skeletonType, nameBuffer, out bufferSize, null, out nameCount);
                        if (success == Result.Success)
                        {
                            ConvertByteBufferToStringArray(nameBuffer, bufferSize, nameCount, out blendShapeNames);
                        }
                    }
                }
            }
            return success == Result.Success;
        }

        /// <summary>
        /// Get the names of all joints for a skeleton type.
        /// </summary>
        /// <param name="handle">The handle to get the joints from.</param>
        /// <param name="skeletonType">The type of skeleton to get the joint names from.</param>
        /// <param name="jointNames">The array of joint names.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetJointNames(UInt64 handle, SkeletonType skeletonType, out string[] jointNames)
        {
            Result success;
            jointNames = Array.Empty<string>();
            using (new ProfilerScope(nameof(GetJointNames)))
            {
                unsafe
                {
                    success = Api.metaMovementSDK_getJointNames(handle, skeletonType, null, out var bufferSize, null, out var nameCount);
                    if (success == Result.Success)
                    {
                        var nameBuffer = stackalloc byte[bufferSize];
                        success = Api.metaMovementSDK_getJointNames(handle, skeletonType, nameBuffer, out bufferSize, null, out nameCount);
                        if (success == Result.Success)
                        {
                            ConvertByteBufferToStringArray(nameBuffer, bufferSize, nameCount, out jointNames);
                        }
                    }
                }
            }
            return success == Result.Success;
        }

        /// <summary>
        /// Get the index for the parent joint for each joint.
        /// </summary>
        /// <param name="handle">The handle to get the info from.</param>
        /// <param name="skeletonType">The type of skeleton to get the info from.</param>
        /// <param name="jointIndexArray">The array of parent joint indexes.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetParentJointIndexes(UInt64 handle, SkeletonType skeletonType, out NativeArray<int> jointIndexArray)
        {
            Result success;
            using (new ProfilerScope(nameof(GetParentJointIndexes)))
            {
                unsafe
                {
                    success = Api.metaMovementSDK_getParentJointIndexes(handle, skeletonType, null, out int numJoints);
                    if (success == Result.Success && numJoints > 0)
                    {
                        jointIndexArray = new NativeArray<int>(numJoints, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                        success = Api.metaMovementSDK_getParentJointIndexes(handle, skeletonType, jointIndexArray.GetPtr(), out numJoints);
                    }
                    else
                    {
                        jointIndexArray = new NativeArray<int>(0, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                    }
                }
            }

            return success == Result.Success;
        }

        /**********************************************************
        *
        *               Serialization Functions
        *
        **********************************************************/

        /// <summary>
        /// Set the compression type.
        /// </summary>
        /// <param name="handle">The handle to set the configuration info.</param>
        /// <param name="compressionType">The compression type to set.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool SetCompressionType(UInt64 handle, CompressionType compressionType)
        {
            Result success;
            using (new ProfilerScope(nameof(SetCompressionType)))
            {
                success = Api.metaMovementSDK_setCompressionType(handle, (int)compressionType);
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Set the delta position threshold for joints before getting serialized.
        /// </summary>
        /// <param name="handle">The handle to set the configuration info.</param>
        /// <param name="threshold">The threshold to set.</param>
        /// <returns></returns>
        public static bool SetPositionThreshold(UInt64 handle, float threshold)
        {
            Result success;
            using (new ProfilerScope(nameof(SetPositionThreshold)))
            {
                success = Api.metaMovementSDK_setPositionThreshold(handle, threshold);
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Set the delta angle threshold for joints before getting serialized.
        /// </summary>
        /// <param name="handle">The handle to set the configuration info.</param>
        /// <param name="threshold">The threshold to set.</param>
        /// <returns></returns>
        public static bool SetRotationAngleThreshold(UInt64 handle, float threshold)
        {
            Result success;
            using (new ProfilerScope(nameof(SetRotationAngleThreshold)))
            {
                success = Api.metaMovementSDK_setRotationAngleThreshold(handle, threshold);
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Set the delta threshold for the shape before it gets serialized.
        /// </summary>
        /// <param name="handle">The handle to set the configuration info.</param>
        /// <param name="threshold">The threshold to set.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool SetShapeThreshold(UInt64 handle, float threshold)
        {
            Result success;
            using (new ProfilerScope(nameof(SetShapeThreshold)))
            {
                success = Api.metaMovementSDK_setShapeThreshold(handle, threshold);
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Serializes body and face pose data into a byte array.
        /// </summary>
        /// <param name="handle">The handle to use for serialization.</param>
        /// <param name="timestamp">The timestamp of the data to be serialized.</param>
        /// <param name="bodyPose">The body pose to be serialized.</param>
        /// <param name="facePose">The face pose to be serialized.</param>
        /// <param name="ack">The acknowledgement number for the data.</param>
        /// <param name="bodyIndicesToSerialize">The indices of the body pose that should be serialized.</param>
        /// <param name="faceIndicesToSerialize">The indices of the face pose that should be serialized.</param>
        /// <param name="output">The serialized body and face pose data as a byte array.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool SerializeSkeletonAndFace(UInt64 handle,
            float timestamp,
            NativeArray<SerializedJointPose> bodyPose,
            NativeArray<SerializedShapePose> facePose,
            int ack,
            int[] bodyIndicesToSerialize,
            int[] faceIndicesToSerialize,
            ref NativeArray<byte> output)
        {
            using (new ProfilerScope(nameof(SerializeSkeletonAndFace)))
            {
                var bodyIndices = new NativeArray<int>(bodyIndicesToSerialize.Length, Allocator.Temp,
                    NativeArrayOptions.UninitializedMemory);
                var faceIndices = new NativeArray<int>(faceIndicesToSerialize.Length, Allocator.Temp,
                    NativeArrayOptions.UninitializedMemory);
                bodyIndices.CopyFrom(bodyIndicesToSerialize);
                faceIndices.CopyFrom(faceIndicesToSerialize);

                var success = Api.metaMovementSDK_createSnapshot(handle, ack,
                     timestamp);
                if (success != Result.Success)
                {
                    Debug.LogError("Could not create snapshot!");
                    return false;
                }

                unsafe
                {
                    success = Api.metaMovementSDK_snapshotBody(
                            handle, bodyPose.GetPtr(),
                            bodyIndices.GetPtr(),
                            bodyIndices.Length);
                }
                if (success != Result.Success)
                {
                    Debug.LogError("Could not snapshot current skeleton!");
                    return false;
                }

                unsafe
                {
                    success = Api.metaMovementSDK_snapshotFace(
                            handle,
                            facePose.GetPtr(),
                            faceIndices.GetPtr(),
                            faceIndices.Length);
                }
                if (success != Result.Success)
                {
                    Debug.LogError("Could not snapshot current skeleton!");
                    return false;
                }

                Api.metaMovementSDK_getSnapshotSize(handle, out var bytes);
                output = new NativeArray<byte>((int)bytes, Allocator.Temp,
                    NativeArrayOptions.UninitializedMemory);

                unsafe
                {
                    success = Api.metaMovementSDK_serializeSnapshot(handle, output.GetPtr());
                }

                if (success == Result.Success)
                {
                    return true;
                }
                Debug.LogError("Could not serialize snapshot!");
                output.Dispose();
                output = default;
                return false;
            }
        }

        /// <summary>
        /// Deserializes data into body and face pose data.
        /// </summary>
        /// <param name="handle">The handle to use for deserialization.</param>
        /// <param name="data">The data to be deserialized.</param>
        /// <param name="timestamp">The timestamp of the data.</param>
        /// <param name="compressionType">The compression type of the data.</param>
        /// <param name="ack">The acknowledgement number for the data.</param>
        /// <param name="outputBodyPose">The output body pose.</param>
        /// <param name="outputFacePose">The output face pose.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool DeserializeSkeletonAndFace(UInt64 handle,
            NativeArray<byte> data,
            out double timestamp,
            out CompressionType compressionType,
            out int ack,
            ref NativeArray<SerializedJointPose> outputBodyPose,
            ref NativeArray<SerializedShapePose> outputFacePose)
        {
            Result success;
            using (new ProfilerScope(nameof(DeserializeSkeletonAndFace)))
            {
                unsafe
                {
                    success = Api.metaMovementSDK_deserializeSnapshot(handle, data.GetPtr(),
                        out timestamp, out var compression, out ack, outputBodyPose.GetPtr(),
                        outputFacePose.GetPtr());
                    compressionType = (CompressionType)compression;
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Get the interpolated body pose.
        /// </summary>
        /// <param name="handle">The handle to get the body pose from.</param>
        /// <param name="interpolatedBodyPose">The interpolated body pose.</param>
        /// <param name="time">The time of interpolation.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetInterpolatedSkeleton(UInt64 handle, ref NativeArray<SerializedJointPose> interpolatedBodyPose,
            double time)
        {
            Result success;
            using (new ProfilerScope(nameof(GetInterpolatedSkeleton)))
            {
                unsafe
                {
                    success = Api.metaMovementSDK_getInterpolatedBodyPose(handle,
                        interpolatedBodyPose.GetPtr(), time);
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Gets the interpolated face pose.
        /// </summary>
        /// <param name="handle">The handle to get the face pose from.</param>
        /// <param name="outputFacePose">The interpolated face pose.</param>
        /// <param name="time">The time of interpolation.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetInterpolatedFace(UInt64 handle, ref NativeArray<SerializedShapePose> outputFacePose,
            double time)
        {
            Result success;
            using (new ProfilerScope(nameof(GetInterpolatedFace)))
            {
                unsafe
                {
                    success = Api.metaMovementSDK_getInterpolatedFacePose(handle,
                        outputFacePose.GetPtr(), time);
                }
            }
            return success == Result.Success;
        }

        /**********************************************************
        *
        *               Tool and Data Functions
        *
        **********************************************************/

        /// <summary>
        /// Writes the config data in a handle to a json.
        /// </summary>
        /// <param name="handle">The handle to get the data from.</param>
        /// <param name="jsonConfigData">The json config data string.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool WriteConfigDataToJson(UInt64 handle, out string jsonConfigData)
        {
            Result success;
            // Empty String
            jsonConfigData = "";
            using (new ProfilerScope(nameof(WriteConfigDataToJson)))
            {
                unsafe
                {
                    // NOTE: Second Parameter is an optional coordinate space.
                    // null is passed so that the JSON config will be written in Unity
                    // Coordinate Space (Z Forward, Y Up, LH)
                    success = Api.metaMovementSDK_writeConfigDataToJSON(handle, null, null, out int bufferSize);
                    if (success == Result.Success && bufferSize > 0)
                    {
                        var jsonBuffer = stackalloc byte[bufferSize];
                        success = Api.metaMovementSDK_writeConfigDataToJSON(handle, null, jsonBuffer, out bufferSize);
                        if (success == Result.Success)
                        {
                            jsonConfigData = Marshal.PtrToStringAnsi((IntPtr)jsonBuffer, bufferSize);
                        }
                    }
                }
            }

            return success == Result.Success;
        }


        /**********************************************************
        *
        *               Helper Functions
        *
        **********************************************************/

        private static unsafe bool ConvertByteBufferToStringArray(byte* stringBuffer, int bufferSize, int stringCount, out string[] stringArray)
        {
            int currentNameStartIndex = 0;
            int stringsFound = 0;
            stringArray = new string[stringCount];

            for (int i = 0; i < bufferSize; i++)
            {
                if (stringBuffer[i] == '\0')
                {
                    if (stringsFound >= stringCount)
                    {
                        // LOG A WARNING - This should NEVER happen
                        return false;
                    }
                    // Found a Name
                    int stringLength = i - currentNameStartIndex;
                    if (stringLength > 0)
                    {
                        stringArray[stringsFound] = Marshal.PtrToStringAnsi((IntPtr)(stringBuffer + currentNameStartIndex), stringLength);
                    }
                    else
                    {
                        // Empty String
                        stringArray[stringsFound] = "";
                    }
                    stringsFound++;
                    currentNameStartIndex = i + 1;
                }
            }

            return true;
        }

        #endregion
    }

    /// <summary>
    /// Helper methods for the retargeting plugin.
    /// </summary>
    public static class NativeUtilityPluginHelper
    {
        /// <summary>
        /// Get the unsafe pointer for a native array.
        /// </summary>
        /// <param name="array">The native array.</param>
        /// <typeparam name="T">The type.</typeparam>
        /// <returns>The unsafe pointer of the native array.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe T* GetPtr<T>(in this NativeArray<T> array) where T : unmanaged
        {
            return (T*)array.GetUnsafePtr();
        }
    }
}
