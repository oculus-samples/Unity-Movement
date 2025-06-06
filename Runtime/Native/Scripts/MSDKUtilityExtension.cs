// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System;
using System.Collections.Generic;
using Unity.Collections;
using static Unity.Collections.NativeArrayOptions;

namespace Meta.XR.Movement
{
    /// <summary>
    /// Utility helper functions for the native plugin.
    /// </summary>
    public abstract partial class MSDKUtility
    {
        /**********************************************************
         *
         *               Extension Functions
         *
         **********************************************************/

        /// <summary>
        /// Get the skeleton joint count for a handle.
        /// </summary>
        /// <param name="handle">The handle to get the skeleton info from.</param>
        /// <param name="skeletonType">The type of skeleton to get info from.</param>
        /// <param name="jointCount">The number of joints.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetSkeletonJointCount(ulong handle, SkeletonType skeletonType, out int jointCount)
        {
            Result success;
            using (new ProfilerScope(nameof(GetConfigName)))
            {
                success = Api.metaMovementSDK_getSkeletonInfo(handle, skeletonType, out var skeletonInfo);
                jointCount = skeletonInfo.JointCount;
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Get the parent joint indexes for a joint as a temp native array.
        /// </summary>
        /// <param name="handle">The handle to get the info from.</param>
        /// <param name="skeletonType">The type of skeleton to get the info from.</param>
        /// <param name="jointIndexArray">The array of parent joint indexes.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetParentJointIndexes(ulong handle, SkeletonType skeletonType,
            out NativeArray<int> jointIndexArray)
        {
            Result success;
            using (new ProfilerScope(nameof(GetParentJointIndexes)))
            {
                unsafe
                {
                    GetSkeletonJointCount(handle, skeletonType, out var jointCount);
                    jointIndexArray = new NativeArray<int>(jointCount, Allocator.Temp, UninitializedMemory);
                    success = Api.metaMovementSDK_getParentJointIndexes(handle,
                        skeletonType, jointIndexArray.GetPtr(), out jointCount);
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Get the child joint indexes for a joint as a temp native array.
        /// </summary>
        /// <param name="handle">The handle to get the info from.</param>
        /// <param name="skeletonType">The type of skeleton to get the info from.</param>
        /// <param name="jointIndex">The index of the joint to get the child indexes for.</param>
        /// <param name="childJointIndexes">The array of child joint indexes.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetChildJointIndexes(ulong handle, SkeletonType skeletonType, int jointIndex,
            out NativeArray<int> childJointIndexes)
        {
            bool success;
            using (new ProfilerScope(nameof(GetChildJointIndexes)))
            {
                success = GetParentJointIndexes(handle, skeletonType, out var parentJointIndexes);
                var childCount = 0;
                // count how many joints consider this node to be a parent.
                foreach (var i in parentJointIndexes)
                {
                    if (i == jointIndex)
                    {
                        childCount++;
                    }
                }

                childJointIndexes = new NativeArray<int>(childCount, Allocator.Temp, UninitializedMemory);
                var currentChildIndex = 0;

                for (int currentJointIndex = 0; currentJointIndex < parentJointIndexes.Length; currentJointIndex++)
                {
                    if (parentJointIndexes[currentJointIndex] == jointIndex)
                    {
                        childJointIndexes[currentChildIndex] = currentJointIndex;
                        currentChildIndex++;
                    }
                }
            }

            return success;
        }

        /// <summary>
        /// Get the lowest child joint index.
        /// </summary>
        /// <param name="handle">The handle to get the info from.</param>
        /// <param name="skeletonType">The type of skeleton to get the info from.</param>
        /// <param name="jointIndex">The joint index to start searching the child indexes from.</param>
        /// <param name="lowestChildIndex">The lowest child index.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetLowestChildJointIndex(ulong handle, SkeletonType skeletonType, int jointIndex, out int lowestChildIndex)
        {
            var stack = new Stack<int>();
            lowestChildIndex = -1;
            stack.Push(jointIndex);
            while (stack.Count > 0)
            {
                var currentJointIndex = stack.Pop();
                GetChildJointIndexes(handle, skeletonType, currentJointIndex, out var childIndices);
                if (childIndices.Length == 0)
                {
                    if (lowestChildIndex == -1 || currentJointIndex > lowestChildIndex)
                    {
                        lowestChildIndex = currentJointIndex;
                    }
                }
                else
                {
                    foreach (var childIndex in childIndices)
                    {
                        stack.Push(childIndex);
                    }
                }
            }
            return lowestChildIndex != -1;
        }

        /// <summary>
        /// Get the names for the parent joint for each joint.
        /// </summary>
        /// <param name="handle">The handle to get the info from.</param>
        /// <param name="skeletonType">The type of skeleton to get the info from.</param>
        /// <param name="parentJointNames">The array of parent joint names.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetParentJointNames(ulong handle, SkeletonType skeletonType, out string[] parentJointNames)
        {
            parentJointNames = Array.Empty<string>();
            using (new ProfilerScope(nameof(GetParentJointNames)))
            {
                if (!GetJointNames(handle, skeletonType, out var jointNames))
                {
                    return false;
                }

                parentJointNames = new string[jointNames.Length];
                for (var i = 0; i < parentJointNames.Length; i++)
                {
                    Api.metaMovementSDK_getParentJointIndex(handle, skeletonType, i, out var parentIndex);
                    if (parentIndex == -1)
                    {
                        parentJointNames[i] = string.Empty;
                        continue;
                    }

                    parentJointNames[i] = jointNames[parentIndex];
                }
            }

            return true;
        }

        /// <summary>
        /// Get the specific skeleton t-pose for a skeleton type as a temp native array.
        /// </summary>
        /// <param name="handle">The handle to get the info from.</param>
        /// <param name="skeletonType">The type of skeleton to get the info from.</param>
        /// <param name="tposeType">The t-pose type.</param>
        /// <param name="jointSpaceType"></param>
        /// <param name="transformArray">The skeleton t-pose.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetSkeletonTPose(ulong handle, SkeletonType skeletonType, SkeletonTPoseType tposeType, JointRelativeSpaceType jointSpaceType,
            out NativeArray<NativeTransform> transformArray)
        {
            Result success;
            using (new ProfilerScope(nameof(GetSkeletonTPoseByRef)))
            {
                unsafe
                {
                    GetSkeletonJointCount(handle, skeletonType, out var jointCount);
                    transformArray = new NativeArray<NativeTransform>(jointCount, Allocator.Temp, UninitializedMemory);
                    success = Api.metaMovementSDK_getSkeletonTPose(handle,
                        skeletonType, tposeType, jointSpaceType, transformArray.GetPtr(), out jointCount);
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Get the names of all known joints for a skeleton type.
        /// </summary>
        /// <param name="handle">The handle to get the info from.</param>
        /// <param name="skeletonType">The type of skeleton to get the info from.</param>
        /// <param name="knownJointNames">The array of joint names.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetKnownJointNames(ulong handle, SkeletonType skeletonType, out string[] knownJointNames)
        {
            knownJointNames = Array.Empty<string>();
            using (new ProfilerScope(nameof(GetKnownJointNames)))
            {
                if (!GetJointNames(handle, skeletonType, out var jointNames))
                {
                    return false;
                }

                knownJointNames = new string[(int)KnownJointType.KnownJointCount];
                for (var i = KnownJointType.Root; i < KnownJointType.KnownJointCount; i++)
                {
                    Api.metaMovementSDK_getJointIndexByKnownJointType(handle, skeletonType, i, out var knownJointIndex);
                    if (knownJointIndex == -1)
                    {
                        knownJointNames[(int)i] = string.Empty;
                        continue;
                    }

                    knownJointNames[(int)i] = jointNames[knownJointIndex];
                }
            }

            return true;
        }

        /// <summary>
        /// Get the name of the joint corresponding to a known joint.
        /// </summary>
        /// <param name="handle">The handle to get the joints from.</param>
        /// <param name="skeletonType">The type of skeleton to get the info from.</param>
        /// <param name="knownJointType">The known joint type.</param>
        /// <param name="jointName">The name of the known joint.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetKnownJointName(ulong handle, SkeletonType skeletonType,
            KnownJointType knownJointType, out string jointName)
        {
            Result success;
            jointName = string.Empty;
            using (new ProfilerScope(nameof(GetJointName)))
            {
                success = Api.metaMovementSDK_getJointIndexByKnownJointType(handle, skeletonType, knownJointType, out var knownJointIndex);
                if (success == Result.Success)
                {
                    if (!GetJointName(handle, skeletonType, knownJointIndex, out jointName))
                    {
                        return false;
                    }
                }
            }
            return success == Result.Success;
        }

        /// <summary>
        /// Get skeleton mapping entries as a temp native array.
        /// </summary>
        /// <param name="handle">The handle to get the joints from.</param>
        /// <param name="tPoseType">The t-pose type.</param>
        /// <param name="mappingEntriesArray">The mapping entries array.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetSkeletonMappingEntries(ulong handle,
            SkeletonTPoseType tPoseType,
            out NativeArray<JointMappingEntry> mappingEntriesArray)
        {
            Result success;
            using (new ProfilerScope(nameof(GetSkeletonMappingEntries)))
            {
                unsafe
                {
                    int numMappings = 0;
                    success = Api.metaMovementSDK_getSkeletonMappingEntries(handle, tPoseType, null,
                        out numMappings);
                    if (success == Result.Success && numMappings > 0)
                    {
                        mappingEntriesArray = new NativeArray<JointMappingEntry>(numMappings, Allocator.Temp,
                            UninitializedMemory);
                        success = Api.metaMovementSDK_getSkeletonMappingEntries(handle, tPoseType,
                            mappingEntriesArray.GetPtr(), out numMappings);
                    }
                    else
                    {
                        mappingEntriesArray = new NativeArray<JointMappingEntry>(0, Allocator.Temp,
                            UninitializedMemory);
                    }
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Converts a skeleton from one joint space format to another.
        /// </summary>
        /// <param name="handle">The handle to get the data from.</param>
        /// <param name="skeletonType">The type of skeleton to get the hierarchy info from.</param>
        /// <param name="inJointSpaceType">The joint space format for the skeletonPose</param>
        /// <param name="outJointSpaceType">The joint space format to convert the skeletonPose to</param>
        /// <param name="inSkeletonPose">The input pose of the skeleton represented in a Native Array</param>
        /// <param name="outSkeletonPose">The output pose of the skeleton represented in a Native Array</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool ConvertJointPose(
            ulong handle,
            SkeletonType skeletonType,
            JointRelativeSpaceType inJointSpaceType,
            JointRelativeSpaceType outJointSpaceType,
            NativeArray<NativeTransform> inSkeletonPose,
            out NativeArray<NativeTransform> outSkeletonPose)
        {
            Result success;
            using (new ProfilerScope(nameof(ConvertJointPose)))
            {
                unsafe
                {
                    outSkeletonPose = new NativeArray<NativeTransform>(inSkeletonPose.Length, Allocator.Temp);
                    outSkeletonPose.CopyFrom(inSkeletonPose);
                    success = Api.metaMovementSDK_convertJointPose(
                        handle,
                        skeletonType,
                        inJointSpaceType,
                        outJointSpaceType,
                        outSkeletonPose.GetPtr(),
                        outSkeletonPose.Length);
                }
            }

            return success == Result.Success;
        }
    }
}
