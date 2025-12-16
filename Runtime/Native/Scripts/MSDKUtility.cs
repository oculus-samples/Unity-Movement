// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using Meta.XR.Movement.Editor;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Profiling;
using Allocator = Unity.Collections.Allocator;
using Assert = UnityEngine.Assertions.Assert;

namespace Meta.XR.Movement
{
    /// <summary>
    /// The native utility plugin containing extended Movement SDK functionality.
    /// This class provides a comprehensive set of tools for working with skeletal data,
    /// retargeting, serialization, and other Movement SDK operations.
    /// </summary>
    public abstract partial class MSDKUtility
    {
        public static class UnmanagedMarshalFunctions
        {
            public static IntPtr MarshalStringArrayToUnmanagedPtr(string[] managedStringArrayStrings)
            {
                if (managedStringArrayStrings == null || managedStringArrayStrings.Length <= 0)
                {
                    return IntPtr.Zero;
                }

                // Allocate memory for the array of pointers
                IntPtr nativeStringArray = Marshal.AllocHGlobal(managedStringArrayStrings.Length * IntPtr.Size);
                int successfulAllocations = 0;
                try
                {
                    // Iterate over each string in the array
                    for (int i = 0; i < managedStringArrayStrings.Length; i++)
                    {
                        // Convert the string to a byte array
                        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(managedStringArrayStrings[i] == null
                            ? ""
                            : managedStringArrayStrings[i]);
                        // Allocate memory for the current string
                        IntPtr currentString = Marshal.AllocHGlobal(bytes.Length + 1); // +1 for null terminator
                        try
                        {
                            // Copy the bytes into the allocated memory
                            Marshal.Copy(bytes, 0, currentString, bytes.Length);
                            // Set the last byte to zero (null terminator)
                            Marshal.WriteByte(currentString, bytes.Length, 0);
                            // Store the pointer to the current string in the array
                            Marshal.WriteIntPtr(nativeStringArray, i * IntPtr.Size, currentString);
                            successfulAllocations++;
                        }
                        catch
                        {
                            // Free the memory for the current string if an exception occurs
                            Marshal.FreeHGlobal(currentString);
                            throw;
                        }
                    }
                }
                catch
                {
                    // Free all successfully allocated strings
                    for (int i = 0; i < successfulAllocations; i++)
                    {
                        IntPtr stringPtr = Marshal.ReadIntPtr(nativeStringArray, i * IntPtr.Size);
                        if (stringPtr != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(stringPtr);
                        }
                    }

                    // Free the array itself
                    Marshal.FreeHGlobal(nativeStringArray);
                    return IntPtr.Zero;
                }

                return nativeStringArray;
            }

            public static bool FreeUnmanagedObject(ref IntPtr unmanagedObject)
            {
                if (unmanagedObject != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(unmanagedObject);
                    unmanagedObject = IntPtr.Zero;
                    return true;
                }

                return false;
            }

            public static bool FreeUnmanagedStringArray(ref IntPtr unmanagedStringArray, int count)
            {
                if (unmanagedStringArray != IntPtr.Zero && count > 0)
                {
                    // Free each individual string in the array
                    for (int i = 0; i < count; i++)
                    {
                        IntPtr stringPtr = Marshal.ReadIntPtr(unmanagedStringArray, i * IntPtr.Size);
                        if (stringPtr != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(stringPtr);
                        }
                    }

                    // Free the array itself
                    Marshal.FreeHGlobal(unmanagedStringArray);
                    unmanagedStringArray = IntPtr.Zero;
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Invalid Handle value.
        /// </summary>
        public const ulong INVALID_HANDLE = 0u;

        /// <summary>
        /// Invalid Joint index value.
        /// </summary>
        public const int INVALID_JOINT_INDEX = -1;

        /// <summary>
        /// Invalid BlendShape index value.
        /// </summary>
        public const int INVALID_BLENDSHAPE_INDEX = -1;

        /// <summary>
        /// Max Possible Snapshots in Serialization System
        /// </summary>
        public const int SERIALIZATION_MAX_POSSIBLE_SNAPSHOTS = 800;

        /// <summary>
        /// Min Possible Snapshots in Serialization System
        /// </summary>
        public const int SERIALIZATION_MIN_POSSIBLE_SNAPSHOTS = 254;

        /// <summary>
        /// Size in bytes for string fields in the serialization start header.
        /// </summary>
        public const int SERIALIZATION_START_HEADER_STRING_SIZE_BYTES = 32;

        /// <summary>
        /// Total size in bytes for the serialization start header.
        /// </summary>
        public const int SERIALIZATION_START_HEADER_SIZE_BYTES = 156;

        /// <summary>
        /// Total size in bytes for the serialization end header.
        /// </summary>
        public const int SERIALIZATION_END_HEADER_SIZE_BYTES = 8;

        public const double SERIALIZATION_VERSION_CURRENT = 0.03;

        /// <summary>
        /// Static DLL name.
        /// </summary>
        private const string DLL = "MetaMovementSDK_Utility";

        /// <summary>
        /// LogLevel enum used for metaMovementSDK_LogCallback function
        /// </summary>
        public enum LogLevel : uint
        {
            Debug = 0,
            Info = 1,
            Warn = 2,
            Error = 3,
        }

        // Callback delegate type
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LogCallback(LogLevel logLevel, IntPtr message);

        private static LogCallback _logCallback = null;

        /// <summary>
        /// Enum for native plugin results. Represents the possible outcomes of operations
        /// performed by the native plugin, including success and various failure modes.
        /// </summary>
        public enum Result : int
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
        /// Enum for compression type used in serialization operations.
        /// Different compression types offer trade-offs between data size and precision.
        /// </summary>
        public enum SerializationCompressionType : uint
        {
            High = 0, // Compressed with joint lengths
            Medium = 1, // Joint compression, positions use less space
            Low = 2, // Joint compression, positions use more space
        }

        /// <summary>
        /// Option for APIs set/get attributes relative to a skeleton.
        /// Specifies whether operations should be performed on the source or target skeleton.
        /// </summary>
        public enum SkeletonType : uint
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
        /// Defines different reference poses that can be used for skeleton operations.
        /// </summary>
        public enum SkeletonTPoseType : uint
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
            /// Parameter for APIs set/get the source/target Unscaled T-Pose.
            /// </summary>
            UnscaledTPose = 3,

            /// <summary>
            /// Parameter for APIs set/get the source/target CachedPose (if one exists)
            /// </summary>
            ConfigCachedPose = 4,
        }

        /// <summary>
        /// Defines the coordinate space in which joint transforms are expressed.
        /// Parameter for Retargeting API - Returns Target Pose in Root Origin Relative Coordinates,
        /// Parameter for Retargeting API - Returns Target Pose in Local Coordinate space.
        /// </summary>
        public enum JointRelativeSpaceType : uint
        {
            RootOriginRelativeSpace = 0, // Tracking Origin
            LocalSpace = 1,
            RootOriginRelativeWithJointScale = 2,
            LocalSpaceScaled = 3,
        }

        /// <summary>
        /// Defines how pose matching should prioritize scale versus orientation.
        /// Parameter Retargeting API - Matches the scale of the pose, preserves original orientation
        /// Parameter Retargeting API - Matches the orientation of the pose, preserves original scale
        /// </summary>
        public enum MatchPoseBehavior : uint
        {
            MatchScale = 0,
            MatchOrientation = 1,
        }

        /// <summary>
        /// Defines the behavior type for joint mapping operations.
        /// Controls how joints are mapped between source and target skeletons.
        /// </summary>
        public enum JointMappingBehaviorType : int
        {
            /// <summary>
            /// Standard joint mapping behavior.
            /// </summary>
            Normal = 0,

            /// <summary>
            /// Joint mapping behavior with twist calculation.
            /// Aligned parent to twist joint for orientation
            /// </summary>
            Twist = 1,

            /// <summary>
            /// Joint mapping behavior with twist calculation
            /// aligned from the twist joint to it's children
            /// Used for joints that are mapped, but also twist
            /// based on orientation of the target rig.
            /// </summary>
            ChildAlignedTwist = 2,

            /// <summary>
            /// Editor-only placeholder value.
            /// </summary>
            Invalid = -1,
        }

        /// <summary>
        /// Flags that modify the behavior of the Alignment process.
        /// Parameter for Alignment API - Instructs Alignment function which operations to apply
        /// </summary>
        [Flags]
        public enum AlignmentFlags : uint
        {
            None = 0,

            /// <summary>
            /// Applies re-orientation of the skeletons to ensure the same facing
            /// </summary>
            ReorientToSourceFacing = 1 << 0,

            /// <summary>
            /// Re-orients Limbs to match source pose (Rotations only)
            /// </summary>
            LimbRotations = 1 << 1,

            /// <summary>
            /// Re-orients Hands and Fingers match source pose (Rotations only)
            /// </summary>
            HandAndFingerRotations = 1 << 2,

            /// <summary>
            /// Proportionally scales the character to align wrist height
            /// </summary>
            ProportionalScalingToHeight = 1 << 3,

            /// <summary>
            /// Scales/Stretches limbs to match the scale/proportion of the source rig (deformation)
            /// </summary>
            LimbDeformationMatchSourceProportion = 1 << 4,

            /// <summary>
            /// Scales/Stretches hands/finger to match the scale/proportion of the source rig (hand deformation)
            /// </summary>
            MatchHandAndFingerPoseWithDeformation = 1 << 5,

            /// <summary>
            /// Enum value composed of all flags set to true.
            /// </summary>
            All = ReorientToSourceFacing |
                  LimbRotations |
                  HandAndFingerRotations |
                  ProportionalScalingToHeight |
                  LimbDeformationMatchSourceProportion |
                  MatchHandAndFingerPoseWithDeformation,
        }

        /// <summary>
        /// Flags that modify the behavior of the AutoMapping process.
        /// Parameter for Automapping API - Instructs AutoMapper to ignore TwistJoints and not process their mappings
        /// </summary>
        [Flags]
        public enum AutoMappingFlags : uint
        {
            EmptyFlag = 0,
            SkipTwistJoints = 1 << 0,
        }

        /// <summary>
        /// Flags that specify how a joint should be treated during the AutoMapping process.
        /// Can be combined to apply multiple behaviors to a single joint.
        /// </summary>
        [Flags]
        public enum AutoMappingJointFlags : uint
        {
            /// <summary>
            /// No special flags applied to the joint.
            /// </summary>
            EmptyJointFlag = 0,

            /// <summary>
            /// Exclude this joint from being mapped during the AutoMapping process.
            /// </summary>
            Exclude = 1 << 0,

            /// <summary>
            /// Exclude this joint from twist joint mapping operations.
            /// </summary>
            ExcludeFromTwistMappings = 1 << 1,
        }

        /// <summary>
        /// Parameter for APIs get a Joint by a KnownJoint ID.
        /// Represents common joints that exist in most humanoid skeletons.
        /// </summary>
        public enum KnownJointType : int
        {
            Unknown = -1,
            Root = 0,
            Hips = 1,
            RightUpperArm = 2,
            LeftUpperArm = 3,
            RightWrist = 4,
            LeftWrist = 5,
            Chest = 6,
            Neck = 7,
            RightUpperLeg = 8,
            LeftUpperLeg = 9,
            RightAnkle = 10,
            LeftAnkle = 11,
            KnownJointCount = 12,
        }

        /// <summary>
        /// Humanoid limb types. Different from KnownJoints.
        /// Limbs are used to encapsulate sets of joints based on the known joints defined by the skeleton.
        /// </summary>
        public enum HumanoidLimbType : int
        {
            UnknownHumanoidLimb = -1,
            RootToHipLimb = 0,
            SpineAndTorsoLimb = 1,
            ChestToNeckLimb = 2,
            HeadAndFaceLimb = 3,
            LeftArmToHandLimb = 4,
            RightArmToHandLimb = 5,
            LeftLegToFootLimb = 6,
            RightLegToFootLimb = 7,
            LeftHandLimb = 8,
            RightHandLimb = 9,
            LeftFootLimb = 10,
            RightFootLimb = 11,
            HumanoidLimbCount = 12,
        }

        /// <summary>
        /// Helper class for HumanoidLimbType operations.
        /// </summary>
        public static class HumanoidLimbTypeHelper
        {
            /// <summary>
            /// Parent hierarchy for humanoid limbs.
            /// Maps each limb type to its parent limb in the hierarchy.
            /// </summary>
            public static readonly Dictionary<HumanoidLimbType, HumanoidLimbType> Hierarchy = new Dictionary<HumanoidLimbType, HumanoidLimbType>
            {
                { HumanoidLimbType.UnknownHumanoidLimb, HumanoidLimbType.UnknownHumanoidLimb },
                { HumanoidLimbType.RootToHipLimb, HumanoidLimbType.UnknownHumanoidLimb },
                { HumanoidLimbType.SpineAndTorsoLimb, HumanoidLimbType.RootToHipLimb },
                { HumanoidLimbType.ChestToNeckLimb, HumanoidLimbType.SpineAndTorsoLimb },
                { HumanoidLimbType.HeadAndFaceLimb, HumanoidLimbType.ChestToNeckLimb },
                { HumanoidLimbType.LeftArmToHandLimb, HumanoidLimbType.SpineAndTorsoLimb },
                { HumanoidLimbType.RightArmToHandLimb, HumanoidLimbType.SpineAndTorsoLimb },
                { HumanoidLimbType.LeftLegToFootLimb, HumanoidLimbType.RootToHipLimb },
                { HumanoidLimbType.RightLegToFootLimb, HumanoidLimbType.RootToHipLimb },
                { HumanoidLimbType.LeftHandLimb, HumanoidLimbType.LeftArmToHandLimb },
                { HumanoidLimbType.RightHandLimb, HumanoidLimbType.RightArmToHandLimb },
                { HumanoidLimbType.LeftFootLimb, HumanoidLimbType.LeftLegToFootLimb },
                { HumanoidLimbType.RightFootLimb, HumanoidLimbType.RightLegToFootLimb },
            };

            /// <summary>
            /// Gets the parent limb type for a given limb.
            /// Returns UnknownHumanoidLimb if the limb is invalid or has no parent.
            /// </summary>
            /// <param name="limb">The limb type to get the parent for.</param>
            /// <returns>The parent limb type, or UnknownHumanoidLimb if invalid.</returns>
            public static HumanoidLimbType GetParent(HumanoidLimbType limb)
            {
                if (Hierarchy.TryGetValue(limb, out HumanoidLimbType parent))
                {
                    return parent;
                }
                return HumanoidLimbType.UnknownHumanoidLimb;
            }

            /// <summary>
            /// Checks if the limb type is unknown.
            /// </summary>
            public static bool IsUnknown(HumanoidLimbType limb)
            {
                return limb == HumanoidLimbType.UnknownHumanoidLimb;
            }

            /// <summary>
            /// Checks if the limb type is an arm (left or right).
            /// </summary>
            public static bool IsArm(HumanoidLimbType limb)
            {
                return limb == HumanoidLimbType.RightArmToHandLimb || limb == HumanoidLimbType.LeftArmToHandLimb;
            }

            /// <summary>
            /// Checks if the limb type is a hand (left or right).
            /// </summary>
            public static bool IsHand(HumanoidLimbType limb)
            {
                return limb == HumanoidLimbType.RightHandLimb || limb == HumanoidLimbType.LeftHandLimb;
            }

            /// <summary>
            /// Checks if the limb type is a leg (left or right).
            /// </summary>
            public static bool IsLeg(HumanoidLimbType limb)
            {
                return limb == HumanoidLimbType.RightLegToFootLimb || limb == HumanoidLimbType.LeftLegToFootLimb;
            }

            /// <summary>
            /// Checks if the limb type is a foot (left or right).
            /// </summary>
            public static bool IsFoot(HumanoidLimbType limb)
            {
                return limb == HumanoidLimbType.RightFootLimb || limb == HumanoidLimbType.LeftFootLimb;
            }
        }

        /// <summary>
        /// Flags that modify the behavior of the retargeting process.
        /// Parameter for Retargeting API - Applies orientation fixup to joints to maintain child/parent orientation relationship.
        /// </summary>
        [Flags]
        public enum RetargetingBehaviorFlags : uint
        {
            None = 0,
            ApplyJointOrientationFixup = 1 << 0,
            UseTPoseForJointScale = 1 << 1,
            ApplyMSDKSourceHandBugFixup = 1 << 2,
        }

        /// <summary>
        /// Flags that modify the runtime operation of the retargeting process.
        /// Parameter for Retargeting API - Forces Runtime to override the config behavior flags completely (vs additive behavior by default)
        /// Parameter for Retargeting API - Uses the frame behavior flags as a mask for the loaded config and unsets any flags specified in the behavior flags if set
        /// </summary>
        [Flags]
        public enum RetargetingRuntimeFlags : uint
        {
            None = 0,
            ForceOverrideBehaviorFlags = 1 << 0,
            UnsetConfigFlagsWithMask = 1 << 1,
        }

        /// <summary>
        /// Defines the overall strategy for retargeting between skeletons.
        /// Parameter Retargeting API - Position & Rotation Retargeting (with Deformation)
        /// Parameter Retargeting API - Position & Rotation Retargeting (with Deformation) - Preserves Hand proportions
        /// Parameter Retargeting API - Rotation Only Retargeting w/ Uniform Scale - Preserves Target Model proportions
        /// Parameter Retargeting API - Rotation Only Retargeting - No Scaling - Preserves Target Model Scale & Proportions
        /// </summary>
        public enum RetargetingBehavior : uint
        {
            [InspectorName("Source Body Proportions")]
            RotationsAndPositions = 0,

            [InspectorName("Source Body Proportions + Target Hand Proportions")]
            RotationsAndPositionsHandsRotationOnly = 1,

            [InspectorName("Source Body Proportions + Target Scale")]
            RotationAndPositionsUniformScale = 2, // RotationOnlyUniformScale

            [InspectorName("Target Body Proportions")]
            RotationOnlyNoScaling = 3,
        }

        /// <summary>
        /// Controls how root motion is handled during retargeting.
        /// Parameter Retargeting API - Yaw and Flat Translation from Origin combined into Root Joint
        /// Parameter Retargeting API - Flat Translation from Origin in Root, Yaw in Hip (default MSDK OpenXR behavior)
        /// Parameter Retargeting API - Zero out translation and Yaw (Locks character to origin, facing forward)
        /// </summary>
        public enum RetargetingRootMotionBehavior : uint
        {
            CombineHipRotationIntoRoot = 0,
            RootFlatTranslationFullHipRotation = 1,
            ZeroOutAllRootTranslationAndHipYaw = 2,
        }

        /// <summary>
        /// Defines the types of tracker joints available in the system.
        /// Tracker joint type; center eye, left input (hand/controller),
        /// or right input (hand/controller).
        /// </summary>
        public enum TrackerJointType : uint
        {
            CenterEye = 0,
            LeftInput = 1,
            RightInput = 2
        }

        /// <summary>
        /// RetargetingBehaviorInfo Struct.
        /// Used for communicating Retargeting modal data as an API Parameter
        /// </summary>
        [StructLayout(LayoutKind.Sequential), Serializable]
        public struct RetargetingBehaviorInfo
        {
            // Value determines how the joints are represented after
            // retargeted (Local vs Root Origin/Tracking releative)
            public JointRelativeSpaceType TargetOutputJointSpaceType;

            // Retargeting Behavior to apply (Positions/Rotations, scaling, etc)
            public RetargetingBehavior RetargetingBehavior;

            // Root Motion behavior (Where the linear/angular velocity
            // should be stored, and how)
            public RetargetingRootMotionBehavior RootMotionBehavior;

            // Complementary behaviors of the retargeter enabled by individual flags
            public RetargetingBehaviorFlags BehaviorFlags;

            // Instructs the retargeter information as to how to treat behaviors for the frame
            // (Behavior Flags are stored in both the config and passed with the runtime update)
            public RetargetingRuntimeFlags RuntimeFlags;

            /// <summary>
            /// Constructor for <see cref="RetargetingBehaviorInfo"/>.
            /// </summary>
            /// <param name="jointSpaceType"><see cref="TargetOutputJointSpaceType"/></param>
            /// <param name="retargetingBehavior"><see cref="RetargetingBehavior"/></param>
            /// <param name="rootMotionBehavior"><see cref="RootMotionBehavior"/></param>
            /// <param name="behaviorFlags"><see cref="BehaviorFlags"/></param>
            public RetargetingBehaviorInfo(
                JointRelativeSpaceType jointSpaceType,
                RetargetingBehavior retargetingBehavior,
                RetargetingRootMotionBehavior rootMotionBehavior,
                RetargetingBehaviorFlags behaviorFlags,
                RetargetingRuntimeFlags runtimeFlags)
            {
                TargetOutputJointSpaceType = jointSpaceType;
                RetargetingBehavior = retargetingBehavior;
                RootMotionBehavior = rootMotionBehavior;
                BehaviorFlags = behaviorFlags;
                RuntimeFlags = runtimeFlags;
            }

            public override string ToString()
            {
                return $"JointRelativeSpaceType({TargetOutputJointSpaceType}) " +
                       $"RetargetingBehavior({RetargetingBehavior}) " +
                       $"RetargetingRootMotionBehavior({RootMotionBehavior})";
            }

            public static RetargetingBehaviorInfo DefaultRetargetingSettings()
            {
                return new RetargetingBehaviorInfo(
                    JointRelativeSpaceType.RootOriginRelativeSpace,
                    RetargetingBehavior.RotationsAndPositions,
                    RetargetingRootMotionBehavior.CombineHipRotationIntoRoot,
                    RetargetingBehaviorFlags.None,
                    RetargetingRuntimeFlags.None);
            }

            public static RetargetingBehaviorInfo DefaultRetargetingSettingsForMSDK()
            {
                RetargetingBehaviorInfo defaultSettings = DefaultRetargetingSettings();
                defaultSettings.BehaviorFlags =
                    defaultSettings.BehaviorFlags |
                    RetargetingBehaviorFlags.ApplyJointOrientationFixup |
                    RetargetingBehaviorFlags.ApplyMSDKSourceHandBugFixup;
                return defaultSettings;
            }
        }

        /// <summary>
        /// SerializationSettings Struct.
        /// Used for communicating mutable/modifiable settings data
        /// to the Serialization component
        /// </summary>
        [StructLayout(LayoutKind.Sequential), Serializable]
        public struct SerializationSettings
        {
            /// <summary>
            /// The SerializationCompressionType type.
            /// </summary>
            public SerializationCompressionType CompressionType;

            /// <summary>
            /// The position threshold to use.
            /// </summary>
            public float PositionThreshold;

            /// <summary>
            /// The rotation angle threshold to use in Degrees.
            /// </summary>
            public float RotationAngleThresholdDegrees;

            /// <summary>
            /// The shape threshold
            /// </summary>
            public float ShapeThreshold;

            /// <summary>
            /// The number of snapshots
            /// </summary>
            public int NumberOfSnapshots;

            /// <summary>
            /// Constructor for <see cref="SerializationSettings"/>.
            /// </summary>
            /// <param name="compressionType"><see cref="CompressionType"/></param>
            /// <param name="positionThreshold"><see cref="PositionThreshold"/></param>
            /// <param name="rotationAngleThresholdDegrees"><see cref="RotationAngleThresholdDegrees"/></param>
            /// <param name="shapeThreshold"><see cref="ShapeThreshold"/></param>
            /// <param name="numberOfSnapshots"><see cref="NumberOfSnapshots"/></param>
            public SerializationSettings(
                SerializationCompressionType compressionType,
                float positionThreshold,
                float rotationAngleThresholdDegrees,
                float shapeThreshold,
                int numberOfSnapshots)
            {
                CompressionType = compressionType;
                PositionThreshold = positionThreshold;
                RotationAngleThresholdDegrees = rotationAngleThresholdDegrees;
                ShapeThreshold = shapeThreshold;
                NumberOfSnapshots = numberOfSnapshots;
            }

            /// <summary>
            /// String output for the <see cref="SerializationSettings"/> struct.
            /// </summary>
            /// <returns>The string output for the SkeletonInfo struct.</returns>
            public override string ToString()
            {
                return $"CompressionType({CompressionType}) " +
                       $"PositionThreshold({PositionThreshold}) " +
                       $"RotationAngleThresholdDegrees({RotationAngleThresholdDegrees}) " +
                       $"ShapeThreshold({ShapeThreshold}) " +
                       $"NumberOfSnapshots({NumberOfSnapshots})";
            }
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
        /// Contains extent data about a pose.
        /// </summary>
        [StructLayout(LayoutKind.Sequential), Serializable]
        public struct Extents
        {
            /// <summary>
            /// The minimum x,y,z coordinates joints in the pose reach
            /// </summary>
            public Vector3 Min;

            /// <summary>
            /// The maximum x,y,z coordinates joints in the pose reach
            /// </summary>
            public Vector3 Max;

            /// <summary>
            /// Result of max - min for total coordinate space the pose occupies
            /// </summary>
            public Vector3 Range;

            /// <summary>
            /// String output for the <see cref="Extents"/> struct.
            /// </summary>
            /// <returns>The string output for the Extents struct.</returns>
            public override string ToString()
            {
                return $"Min({Min}) " +
                       $"Max({Max}) " +
                       $"Range({Range})";
            }
        }

        /// <summary>
        /// Contains information about a Skeleton Pose.
        /// </summary>
        [StructLayout(LayoutKind.Sequential), Serializable]
        public struct PoseInfo
        {
            /// <summary>
            /// The Coordinate Space information for this pose.
            /// </summary>
            public CoordinateSpace CoordSpace;

            /// <summary>
            /// The extents of the Pose relative to the coordinate space
            /// </summary>
            public Extents Extents;

            /// <summary>
            /// The positions of all known joints relative to coordinate space (Vector3.Zero returned for unspecified known joints)
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)(KnownJointType.KnownJointCount))]
            public Vector3[] KnownJointPositions;

            /// <summary>
            /// String output for the <see cref="PoseInfo"/> struct.
            /// </summary>
            /// <returns>The string output for the PoseInfo struct.</returns>
            public override string ToString()
            {
                return $"CoordSpace({CoordSpace}) " +
                       $"Extents({Extents}) " +
                       $"KnownJointPositions({KnownJointPositions.ToString()})";
            }
        }

        /// <summary>
        /// Contains mapping data between known joint types and their corresponding joint indices in a skeleton.
        /// This structure provides a lookup table from KnownJointType to actual joint index values.
        /// </summary>
        [StructLayout(LayoutKind.Sequential), Serializable]
        public struct KnownJointIndexData
        {
            /// <summary>
            /// Array of joint indices indexed by KnownJointType enum values.
            /// A value of INVALID_JOINT_INDEX (-1) indicates that the known joint type is not present in the skeleton.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)(KnownJointType.KnownJointCount))]
            public int[] JointIndexByType;

            /// <summary>
            /// String output for the <see cref="KnownJointIndexData"/> struct.
            /// </summary>
            /// <returns>The string output for the KnownJointIndexData struct.</returns>
            public override string ToString()
            {
                if (JointIndexByType == null || JointIndexByType.Length == 0)
                {
                    return "KnownJointIndexData: Empty";
                }

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("KnownJointIndexData:");
                for (int i = 0; i < JointIndexByType.Length; i++)
                {
                    if (JointIndexByType[i] != INVALID_JOINT_INDEX)
                    {
                        sb.AppendLine($"  [{(KnownJointType)i}] = {JointIndexByType[i]}");
                    }
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// Contains pointers to joint names for known joint types.
        /// This structure provides a lookup table from KnownJointType to joint name strings.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct KnownJointNameData
        {
            /// <summary>
            /// Fixed array of pointers to joint name strings, indexed by KnownJointType enum values.
            /// A null pointer indicates that the known joint type is not present in the skeleton.
            /// </summary>
            private fixed long _jointNameByType[(int)KnownJointType.KnownJointCount];

            /// <summary>
            /// Gets or sets a joint name pointer at the specified known joint type index.
            /// </summary>
            public byte*[] JointNameByType
            {
                get
                {
                    var result = new byte*[(int)KnownJointType.KnownJointCount];
                    fixed (long* ptr = _jointNameByType)
                    {
                        for (int i = 0; i < (int)KnownJointType.KnownJointCount; i++)
                        {
                            result[i] = (byte*)ptr[i];
                        }
                    }
                    return result;
                }
            }
        }

        /// <summary>
        /// Contains information about how a joint should be treated during AutoMapping.
        /// Maps joint names to their AutoMapping behavior flags.
        /// </summary>
        public struct AutoMappingJointData
        {
            /// <summary>
            /// The name of the joint.
            /// </summary>
            public string JointName;

            /// <summary>
            /// Flags specifying how this joint should be treated during AutoMapping.
            /// </summary>
            public AutoMappingJointFlags Flags;

            public override string ToString()
            {
                return $"JointName({JointName}) Flags({Flags})";
            }
        }

        /// <summary>
        /// Unmanaged structure for AutoMapping joint data used for interop with native code.
        /// </summary>
        [StructLayout(LayoutKind.Sequential), Serializable]
        private struct AutoMappingJointDataUnmanaged
        {
            /// <summary>
            /// Pointer to the joint name string in unmanaged memory.
            /// </summary>
            public IntPtr JointName;

            /// <summary>
            /// Flags specifying how this joint should be treated during AutoMapping.
            /// </summary>
            public AutoMappingJointFlags Flags;
        }

        /// <summary>
        /// Represents a definition of a twist joint with the target jointIndex and the two joints used to influence the twist.
        /// Ratio defines the relative ratio of influence between the start and end influence joint index.
        /// </summary>
        [StructLayout(LayoutKind.Sequential), Serializable]
        public struct TwistJointDefinition
        {
            /// <summary>
            /// The index of the target twist joint (in the target skeleton).
            /// </summary>
            public int TwistJointIndex;

            /// <summary>
            /// The index of source influence start index (either source or target skeleton depending on usage)
            /// </summary>
            public int InfluenceStartJointIndex;

            /// <summary>
            /// The index of source influence end index (either source or target skeleton depending on usage)
            /// Must be the same skeleton (source or target) as the Influence start joint index.
            /// </summary>
            public int InfluenceEndJointIndex;

            /// <summary>
            /// The ratio of influence between the start and end (ie - value we base our lerp(start, end, ratio).
            /// </summary>
            public float Ratio;

            public override string ToString()
            {
                return $"TwistJointIndex({TwistJointIndex}) " +
                       $"InfluenceStartJointIndex({InfluenceStartJointIndex}) " +
                       $"InfluenceEndJointIndex({InfluenceEndJointIndex}) " +
                       $"Ratio({Ratio})";
            }
        }

        /// <summary>
        /// Represents a single entry in a joint mapping, defining how a specific joint should be mapped.
        /// </summary>
        [StructLayout(LayoutKind.Sequential), Serializable]
        public struct JointMappingEntry
        {
            /// <summary>
            /// The index of the joint in the skeleton.
            /// </summary>
            public int JointIndex;

            /// <summary>
            /// The weight applied to rotation mapping for this joint.
            /// </summary>
            public float RotationWeight;

            /// <summary>
            /// The weight applied to position mapping for this joint.
            /// </summary>
            public float PositionWeight;

            public override string ToString()
            {
                return $"JointIndex({JointIndex}) " +
                       $"RotationWeight({RotationWeight}) " +
                       $"PositionWeight({PositionWeight})";
            }
        }

        /// <summary>
        /// Defines mapping information for a joint, including its index, type, behavior, and number of entries.
        /// </summary>
        [StructLayout(LayoutKind.Sequential), Serializable]
        public struct JointMapping
        {
            /// <summary>
            /// The index of the joint in the skeleton.
            /// </summary>
            public int JointIndex;

            /// <summary>
            /// The type of skeleton.
            /// </summary>
            public SkeletonType Type;

            /// <summary>
            /// The behavior type for this joint mapping.
            /// </summary>
            public JointMappingBehaviorType Behavior;

            /// <summary>
            /// The number of entries in this joint mapping.
            /// </summary>
            public int EntriesCount;

            public override string ToString()
            {
                return $"JointIndex({JointIndex}) " +
                       $"Type({Type}) " +
                       $"Behavior({Behavior}) " +
                       $"EntriesCount({EntriesCount})";
            }
        }

        public struct JointMappingDefinition
        {
            public NativeArray<JointMapping> Mappings;
            public NativeArray<JointMappingEntry> MappingEntries;

            public JointMappingDefinition(NativeArray<JointMapping> mappings,
                NativeArray<JointMappingEntry> mappingEntries)
            {
                Mappings = mappings;
                MappingEntries = mappingEntries;
            }

            public override string ToString()
            {
                return $"JointMappingDefinition: Mappings={Mappings.Length}, MappingEntries={MappingEntries.Length}";
            }
        }

        [StructLayout(LayoutKind.Sequential), Serializable]
        private struct JointMappingDefinitionUnmanaged
        {
            public int MappingsCount;
            public int mappingEntryCount;
            public unsafe JointMapping* Mappings;
            public unsafe JointMappingEntry* MappingEntries;

            public unsafe JointMappingDefinitionUnmanaged(JointMappingDefinition safeParams)
            {
                MappingsCount = safeParams.Mappings.IsCreated ? safeParams.Mappings.Length : 0;
                mappingEntryCount = safeParams.MappingEntries.IsCreated ? safeParams.MappingEntries.Length : 0;
                Mappings = safeParams.Mappings.IsCreated ? safeParams.Mappings.GetPtr() : null;
                MappingEntries = safeParams.MappingEntries.IsCreated ? safeParams.MappingEntries.GetPtr() : null;
            }
        }

        /// <summary>
        /// Representation of a native transform, containing information about the orientation,
        /// position, and scale for a transform.
        /// </summary>
        [StructLayout(LayoutKind.Sequential), Serializable]
        public struct NativeTransform : IEquatable<NativeTransform>
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
            public NativeTransform(NativeTransform pose)
            {
                Orientation = pose.Orientation;
                Position = pose.Position;
                Scale = pose.Scale;
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
            /// Constructor for <see cref="NativeTransform"/>.
            /// </summary>
            /// <param name="pose">The transform to be converted.</param>
            public NativeTransform(Transform pose)
            {
                Orientation = pose.rotation;
                Position = pose.position;
                Scale = pose.localScale;
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

            /// <summary>
            /// Equality operator for <see cref="NativeTransform"/>.
            /// </summary>
            /// <param name="other">The other operand.</param>
            /// <returns>True if the two operands are equal; otherwise, false.</returns>
            public bool Equals(NativeTransform other)
            {
                return Orientation == other.Orientation &&
                       Position == other.Position &&
                       Scale == other.Scale;
            }

            public override bool Equals(object obj)
            {
                return obj is NativeTransform other && Equals(other);
            }


            public override int GetHashCode()
            {
                return HashCode.Combine(Orientation, Position, Scale);
            }


            /// <summary>
            /// Equality operator for <see cref="NativeTransform"/>.
            /// </summary>
            /// <param name="left">The left operand.</param>
            /// <param name="right">The right operand.</param>
            /// <returns>True if the two operands are equal; otherwise, false.</returns>
            public static bool operator ==(NativeTransform left, NativeTransform right)
            {
                return left.Equals(right);
            }

            /// <summary>
            /// Inequality operator for <see cref="NativeTransform"/>.
            /// </summary>
            /// <param name="left">The left operand.</param>
            /// <param name="right">The right operand.</param>
            /// <returns>True if the two operands are not equal; otherwise, false.</returns>
            public static bool operator !=(NativeTransform left, NativeTransform right)
            {
                return !left.Equals(right);
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
            /// The scale of the unit space relative to Meters.
            /// </summary>
            public float MetersToUnitScale;

            /// <summary>
            /// Constructor for <see cref="CoordinateSpace"/>.
            /// </summary>
            /// <param name="up"><see cref="Up"/>.</param>
            /// <param name="forward"><see cref="Forward"/></param>
            /// <param name="right"><see cref="Right"/></param>
            public CoordinateSpace(Vector3 up, Vector3 forward, Vector3 right, float metersToUnitScale = 1.0f)
            {
                Up = up;
                Forward = forward;
                Right = right;
                MetersToUnitScale = metersToUnitScale;
            }

            /// <summary>
            /// String output for the <see cref="CoordinateSpace"/> struct.
            /// </summary>
            /// <returns>String output.</returns>
            public override string ToString()
            {
                return $"Up({Up.x:F2},{Up.y:F2},{Up.z:F2}), " +
                       $"Forward({Forward.x:F2},{Forward.y:F2},{Forward.z:F2}, " +
                       $"Right({Right.x:F2},{Right.y:F2},{Right.z:F2})" +
                       $"MetersToUnitScale({MetersToUnitScale:F2})";
            }

            /// <summary>
            /// Compares this CoordinateSpace with another for approximate equality.
            /// </summary>
            /// <param name="other">The other CoordinateSpace to compare against.</param>
            /// <param name="tolerance">The tolerance for floating point comparison (default: 0.0001f).</param>
            /// <returns>True if the coordinate spaces are approximately equal, false otherwise.</returns>
            public bool ApproximatelyEquals(CoordinateSpace other, float tolerance = 0.0001f)
            {
                return (Up - other.Up).sqrMagnitude < tolerance * tolerance &&
                       (Forward - other.Forward).sqrMagnitude < tolerance * tolerance &&
                       (Right - other.Right).sqrMagnitude < tolerance * tolerance &&
                       Mathf.Abs(MetersToUnitScale - other.MetersToUnitScale) < tolerance;
            }
        }

        /// <summary>
        /// Contains Initialization Parameters for a Skeleton
        /// </summary>
        public struct SkeletonInitParams
        {
            public string[] BlendShapeNames;
            public string[] JointNames;
            public string[] ParentJointNames;
            public NativeArray<NativeTransform> MinTPose;
            public NativeArray<NativeTransform> MaxTPose;
            public NativeArray<NativeTransform> UnscaledTPose;
            public string[] OptionalKnownSourceJointNamesById;
            public AutoMappingJointData[] OptionalAutoMapJointData;

            // Number of manifestations in the name and joint counts arrays
            public string[] OptionalManifestationNames;

            // Array of integers
            public int[] OptionalManifestationJointCounts;

            // Buffer of all Manifestation joint names in order of manifestations
            public string[] OptionalManifestationJointNames;

            public override string ToString()
            {
                var sb = new System.Text.StringBuilder();

                sb.AppendLine($"SkeletonInitParams:");
                sb.AppendLine($"  BlendShapes: {BlendShapeNames?.Length ?? 0}");
                sb.AppendLine($"  Joints: {JointNames?.Length ?? 0}");
                sb.AppendLine($"  ParentJoints: {ParentJointNames?.Length ?? 0}");
                sb.AppendLine($"  MinTPose: {MinTPose.Length}");
                sb.AppendLine($"  MaxTPose: {MaxTPose.Length}");
                sb.AppendLine($"  UnscaledTPose: {UnscaledTPose.Length}");
                sb.AppendLine($"  OptionalKnownSourceJointNamesById: {OptionalKnownSourceJointNamesById?.Length ?? 0}");
                sb.AppendLine($"  OptionalAutoMapJointData: {OptionalAutoMapJointData?.Length ?? 0}");
                sb.AppendLine($"  Manifestations: {OptionalManifestationNames?.Length ?? 0}");

                // Add joint names
                if (JointNames is { Length: > 0 })
                {
                    sb.AppendLine("\nJoint Names:");
                    for (var i = 0; i < JointNames.Length; i++)
                    {
                        sb.AppendLine($"  [{i}] {JointNames[i]}");
                    }
                }

                // Add parent joint names
                if (ParentJointNames is { Length: > 0 })
                {
                    sb.AppendLine("\nParent Joint Names:");
                    for (var i = 0; i < ParentJointNames.Length; i++)
                    {
                        sb.AppendLine($"  [{i}] {ParentJointNames[i]}");
                    }
                }

                // Add known joint names
                if (OptionalKnownSourceJointNamesById is { Length: > 0 })
                {
                    sb.AppendLine("\nKnown Joint Names:");
                    for (var i = 0; i < OptionalKnownSourceJointNamesById.Length; i++)
                    {
                        sb.AppendLine($"  [{(KnownJointType)i}] {OptionalKnownSourceJointNamesById[i]}");
                    }
                }

                return sb.ToString();
            }
        }

        /// <summary>
        /// Unmanaged Structure for containing Initialization Parameters for a Skeleton
        /// </summary>
        [StructLayout(LayoutKind.Sequential), Serializable]
        private struct SkeletonInitParamsUnmanaged : IDisposable
        {
            public int BlendShapeCount;
            public int JointCount;

            public IntPtr BlendShapeNames;
            public IntPtr JointNames;
            public IntPtr ParentJointNames;

            public unsafe NativeTransform* MinTPose;
            public unsafe NativeTransform* MaxTPose;
            public unsafe NativeTransform* UnscaledTPose;

            public IntPtr optional_KnownSourceJointNamesById;

            public int optional_autoMapJointDataCount;

            public unsafe AutoMappingJointDataUnmanaged* optional_autoMapJointData;

            // Number of manifestations in the name and joint counts arrays
            public int optional_ManifestationCount;

            public IntPtr optional_ManifestationNames;

            // Array of integers
            public IntPtr optional_ManifestationJointCounts;

            // Buffer of all Manifestation joint names in order of manifestations
            public IntPtr optional_ManifestationJointNames;

            public unsafe SkeletonInitParamsUnmanaged(SkeletonInitParams safeParams)
            {
                BlendShapeCount = safeParams.BlendShapeNames?.Length ?? 0;
                JointCount = safeParams.JointNames?.Length ?? 0;
                BlendShapeNames =
                    UnmanagedMarshalFunctions.MarshalStringArrayToUnmanagedPtr(safeParams.BlendShapeNames);
                JointNames = UnmanagedMarshalFunctions.MarshalStringArrayToUnmanagedPtr(safeParams.JointNames);
                ParentJointNames =
                    UnmanagedMarshalFunctions.MarshalStringArrayToUnmanagedPtr(safeParams.ParentJointNames);
                MinTPose = safeParams.MinTPose.IsCreated ? safeParams.MinTPose.GetPtr() : null;
                MaxTPose = safeParams.MaxTPose.IsCreated ? safeParams.MaxTPose.GetPtr() : null;
                UnscaledTPose = safeParams.UnscaledTPose.IsCreated ? safeParams.UnscaledTPose.GetPtr() : null;
                optional_KnownSourceJointNamesById =
                    UnmanagedMarshalFunctions.MarshalStringArrayToUnmanagedPtr(safeParams
                        .OptionalKnownSourceJointNamesById);

                // Convert AutoMappingJointData from managed to unmanaged
                optional_autoMapJointDataCount = safeParams.OptionalAutoMapJointData?.Length ?? 0;
                optional_autoMapJointData = null;
                if (safeParams.OptionalAutoMapJointData != null && safeParams.OptionalAutoMapJointData.Length > 0)
                {
                    // Allocate array for unmanaged structures
                    optional_autoMapJointData = (AutoMappingJointDataUnmanaged*)Marshal.AllocHGlobal(
                        safeParams.OptionalAutoMapJointData.Length * sizeof(AutoMappingJointDataUnmanaged));

                    // Convert each managed structure to unmanaged
                    for (int i = 0; i < safeParams.OptionalAutoMapJointData.Length; i++)
                    {
                        optional_autoMapJointData[i].JointName =
                            Marshal.StringToHGlobalAnsi(safeParams.OptionalAutoMapJointData[i].JointName);
                        optional_autoMapJointData[i].Flags = safeParams.OptionalAutoMapJointData[i].Flags;
                    }
                }

                optional_ManifestationCount = safeParams.OptionalManifestationNames?.Length ?? 0;
                optional_ManifestationNames =
                    UnmanagedMarshalFunctions.MarshalStringArrayToUnmanagedPtr(safeParams.OptionalManifestationNames);


                optional_ManifestationJointCounts = IntPtr.Zero;

                if (safeParams.OptionalManifestationJointCounts != null)
                {
                    optional_ManifestationJointCounts =
                        Marshal.AllocHGlobal(safeParams.OptionalManifestationJointCounts.Length * sizeof(int));
                    Marshal.Copy(safeParams.OptionalManifestationJointCounts, 0, optional_ManifestationJointCounts,
                        safeParams.OptionalManifestationJointCounts.Length);
                }

                optional_ManifestationJointNames =
                    UnmanagedMarshalFunctions.MarshalStringArrayToUnmanagedPtr(safeParams
                        .OptionalManifestationJointNames);
            }

            public unsafe void Dispose()
            {
                UnmanagedMarshalFunctions.FreeUnmanagedStringArray(ref BlendShapeNames, BlendShapeCount);
                UnmanagedMarshalFunctions.FreeUnmanagedStringArray(ref JointNames, JointCount);
                UnmanagedMarshalFunctions.FreeUnmanagedStringArray(ref ParentJointNames, JointCount);
                UnmanagedMarshalFunctions.FreeUnmanagedStringArray(
                    ref optional_KnownSourceJointNamesById,
                    (int)KnownJointType.KnownJointCount);

                // Free AutoMappingJointData array
                if (optional_autoMapJointData != null)
                {
                    // Free each string pointer allocated by Marshal.StringToHGlobalAnsi
                    for (int i = 0; i < optional_autoMapJointDataCount; i++)
                    {
                        if (optional_autoMapJointData[i].JointName != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(optional_autoMapJointData[i].JointName);
                        }
                    }

                    // Free the array itself
                    Marshal.FreeHGlobal((IntPtr)optional_autoMapJointData);
                    optional_autoMapJointData = null;
                }

                UnmanagedMarshalFunctions.FreeUnmanagedStringArray(
                    ref optional_ManifestationNames,
                    optional_ManifestationCount);

                // Calculate total number of manifestation joint names BEFORE freeing the counts
                int totalManifestationJointNames = 0;
                if (optional_ManifestationJointCounts != IntPtr.Zero && optional_ManifestationCount > 0)
                {
                    for (int i = 0; i < optional_ManifestationCount; i++)
                    {
                        totalManifestationJointNames += Marshal.ReadInt32(
                            optional_ManifestationJointCounts,
                            i * sizeof(int));
                    }
                }

                // Free the counts array
                UnmanagedMarshalFunctions.FreeUnmanagedObject(ref optional_ManifestationJointCounts);

                // Free the manifestation joint names using the calculated total
                UnmanagedMarshalFunctions.FreeUnmanagedStringArray(
                    ref optional_ManifestationJointNames,
                    totalManifestationJointNames);
            }
        }

        /// <summary>
        /// Snapshot data.
        /// </summary>
        public struct SnapshotData
        {
            /// <summary>
            /// Baseline acknowledgement.
            /// </summary>
            public int BaselineAck;
            /// <summary>
            /// Timestamp.
            /// </summary>
            public double Timestamp;

            /// <summary>
            /// Target skeleton pose native array.
            /// </summary>
            public NativeArray<NativeTransform> TargetSkeletonPose;
            /// <summary>
            /// Target skeleton indices native array.
            /// </summary>
            public NativeArray<int> TargetSkeletonIndices;

            /// <summary>
            /// Source skeleton pose native array.
            /// </summary>
            public NativeArray<NativeTransform> SourceSkeletonPose;
            /// <summary>
            /// Source skeleton indices native array.
            /// </summary>
            public NativeArray<int> SourceSkeletonIndices;

            /// <summary>
            /// Face pose native array.
            /// </summary>
            public NativeArray<float> FacePose;
            /// <summary>
            /// Face indices native array.
            /// </summary>
            public NativeArray<int> FaceIndices;

            /// <summary>
            /// Whether to serialize frame data or not.
            /// </summary>
            [MarshalAs(UnmanagedType.U1)]
            public bool SerializeFrameData;
            /// <summary>
            /// Frame data struct.
            /// </summary>
            public FrameData FrameData;
            /// <summary>
            /// Bind pose native array.
            /// </summary>
            public NativeArray<NativeTransform> BindPose;
            /// <summary>
            /// Number of bind pose joints.
            /// </summary>
            public int NumBindPoseJoints;

            /// <summary>
            /// Coordinate space of the recording (source).
            /// </summary>
            public CoordinateSpace RecordingCoordinateSpaceSource;

            /// <summary>
            /// Snapshot data constructor.
            /// </summary>
            /// <param name="baselineAck">Baseline ack.</param>
            /// <param name="timeStamp">Timestamp.</param>
            /// <param name="targetSkeletonPose">Target skeleton pose.</param>
            /// <param name="targetSkeletonIndices">Target skeleton indices.</param>
            /// <param name="sourceSkeletonPose">Source skeleton pose.</param>
            /// <param name="sourceSkeletonIndices">Source skeleton indices.</param>
            /// <param name="facePose">Face pose.</param>
            /// <param name="faceIndices">Face indices.</param>
            /// <param name="serializeFrameData">Whether to serialize framedata or not.</param>
            /// <param name="frameData">The framedata struct.</param>
            /// <param name="bindPose">The bind pose.</param>
            /// <param name="numBindPoseJoints">Number of bind pose joints.</param>
            /// <param name="recordingCoordinateSpace">Coordinate space of the recording (source).</param>
            public SnapshotData(
                int baselineAck,
                double timeStamp,
                NativeArray<NativeTransform> targetSkeletonPose,
                NativeArray<int> targetSkeletonIndices,
                NativeArray<NativeTransform> sourceSkeletonPose,
                NativeArray<int> sourceSkeletonIndices,
                NativeArray<float> facePose,
                NativeArray<int> faceIndices,
                bool serializeFrameData,
                FrameData frameData,
                NativeArray<NativeTransform> bindPose,
                int numBindPoseJoints,
                CoordinateSpace recordingCoordinateSpaceSource)
            {
                BaselineAck = baselineAck;
                Timestamp = timeStamp;

                TargetSkeletonPose = targetSkeletonPose;
                TargetSkeletonIndices = targetSkeletonIndices;

                SourceSkeletonPose = sourceSkeletonPose;
                SourceSkeletonIndices = sourceSkeletonIndices;

                FacePose = facePose;
                FaceIndices = faceIndices;

                SerializeFrameData = serializeFrameData;
                FrameData = frameData;
                BindPose = bindPose;
                NumBindPoseJoints = numBindPoseJoints;

                RecordingCoordinateSpaceSource = recordingCoordinateSpaceSource;
            }
        }

        /// <summary>
        /// Unmanaged snapshot data.
        /// </summary>
        [StructLayout(LayoutKind.Sequential), Serializable]
        private struct SnapshotDataUnmanaged
        {
            /// <summary>
            /// Baseline acknowledgement.
            /// </summary>
            public int BaselineAck;
            /// <summary>
            /// Timestamp.
            /// </summary>
            public double Timestamp;

            /// <summary>
            /// Target skeleton pose pointer.
            /// </summary>
            public unsafe NativeTransform* TargetSkeletonPose;
            /// <summary>
            /// Target skeleton indices pointer.
            /// </summary>
            public unsafe int* TargetSkeletonIndices;
            /// <summary>
            /// Number of target skeleton indices.
            /// </summary>
            public int NumTargetSkeletonIndices;

            /// <summary>
            /// Source skeleton pose pointer.
            /// </summary>
            public unsafe NativeTransform* SourceSkeletonPose;
            /// <summary>
            /// Source skeleton indices pointer.
            /// </summary>
            public unsafe int* SourceSkeletonIndices;
            /// <summary>
            /// Number of source skeleton indices.
            /// </summary>
            public int NumSourceSkeletonIndices;

            /// <summary>
            /// Face pose pointer.
            /// </summary>
            public unsafe float* FacePose;
            /// <summary>
            /// Face indices pointer.
            /// </summary>
            public unsafe int* FaceIndices;
            /// <summary>
            /// Number of face indices.
            /// </summary>
            public int NumOfFaceIndices;

            /// <summary>
            /// Whether to serialize frame data or not.
            /// </summary>
            public bool SerializeFrameData;
            /// <summary>
            /// Frame data struct.
            /// </summary>
            public FrameData FrameData;
            /// <summary>
            /// Bind pose pointer.
            /// </summary>
            public unsafe NativeTransform* BindPose;
            /// <summary>
            /// Number of bind pose joints.
            /// </summary>
            public int NumBindPoseJoints;

            /// <summary>
            /// Coordinate space of the recording (source).
            /// </summary>
            public CoordinateSpace RecordingCoordinateSpaceSource;

            /// <summary>
            /// Unmanaged snapshot data constructor.
            /// </summary>
            /// <param name="snapshotData">SnapshotData container.</param>
            public unsafe SnapshotDataUnmanaged(SnapshotData snapshotData)
            {
                BaselineAck = snapshotData.BaselineAck;
                Timestamp = snapshotData.Timestamp;

                TargetSkeletonPose = snapshotData.TargetSkeletonPose.IsCreated ? snapshotData.TargetSkeletonPose.GetPtr() : null;
                TargetSkeletonIndices = snapshotData.TargetSkeletonIndices.IsCreated ? snapshotData.TargetSkeletonIndices.GetPtr() : null;
                NumTargetSkeletonIndices = snapshotData.TargetSkeletonIndices.IsCreated ? snapshotData.TargetSkeletonIndices.Length : 0;

                SourceSkeletonPose = snapshotData.SourceSkeletonPose.IsCreated ? snapshotData.SourceSkeletonPose.GetPtr() : null;
                SourceSkeletonIndices = snapshotData.SourceSkeletonIndices.IsCreated ? snapshotData.SourceSkeletonIndices.GetPtr() : null;
                NumSourceSkeletonIndices = snapshotData.SourceSkeletonIndices.IsCreated ? snapshotData.SourceSkeletonIndices.Length : 0;

                FacePose = snapshotData.FacePose.IsCreated ? snapshotData.FacePose.GetPtr() : null;
                FaceIndices = snapshotData.FaceIndices.IsCreated ? snapshotData.FaceIndices.GetPtr() : null;
                NumOfFaceIndices = snapshotData.FaceIndices.IsCreated ? snapshotData.FaceIndices.Length : 0;

                SerializeFrameData = snapshotData.SerializeFrameData;
                FrameData = snapshotData.FrameData;
                BindPose = snapshotData.BindPose.IsCreated ? snapshotData.BindPose.GetPtr() : null;
                NumBindPoseJoints = snapshotData.NumBindPoseJoints;

                RecordingCoordinateSpaceSource = snapshotData.RecordingCoordinateSpaceSource;
            }
        }

        /// <summary>
        /// Unmanaged snapshot data used for reading back.
        /// </summary>
        [StructLayout(LayoutKind.Sequential), Serializable]
        private struct DeserializedSnapshotDataUnmanaged
        {
            public double Timestamp;
            public SerializationCompressionType Compression;
            public int Ack;

            public unsafe NativeTransform* TargetSkeletonPose;
            public unsafe float* FacePose;
            public unsafe NativeTransform* SourceSkeletonPose;

            public FrameData FrameData;
            public unsafe NativeTransform* BindPose;
            public int NumBindPoseJoints;
            public CoordinateSpace CoordinateSpaceSource;
        }

        /// <summary>
        /// Contains Initialization Parameters for defining a configuration
        /// </summary>
        public struct ConfigInitParams
        {
            // Skeleton Data
            public SkeletonInitParams SourceSkeleton;
            public SkeletonInitParams TargetSkeleton;

            // Mapping Data
            public JointMappingDefinition MinMappings;
            public JointMappingDefinition MaxMappings;

            // Config Retargeting Flags
            public RetargetingBehaviorFlags optional_RetargetingFlags;

            public override string ToString()
            {
                return "Source: " + SourceSkeleton +
                       "\nTarget: " + TargetSkeleton +
                       "\nMin Mappings: " + MinMappings +
                       "\nMax Mappings:" + MaxMappings +
                       "\nRetargeting Flags:" + optional_RetargetingFlags;
            }
        }

        /// <summary>
        /// Unmanaged Structure containing Parameters for defining a configuration
        /// </summary>
        [StructLayout(LayoutKind.Sequential), Serializable]
        private struct ConfigInitParamsUnmanaged : IDisposable
        {
            // Skeleton Data
            public SkeletonInitParamsUnmanaged SourceSkeleton;
            public SkeletonInitParamsUnmanaged TargetSkeleton;

            // Mapping Data
            public JointMappingDefinitionUnmanaged MinMappings;
            public JointMappingDefinitionUnmanaged MaxMappings;

            // Config Retargeting Flags
            public RetargetingBehaviorFlags optional_RetargetingFlags;

            public ConfigInitParamsUnmanaged(ConfigInitParams safeParams)
            {
                SourceSkeleton = new SkeletonInitParamsUnmanaged(safeParams.SourceSkeleton);
                TargetSkeleton = new SkeletonInitParamsUnmanaged(safeParams.TargetSkeleton);

                MinMappings = new JointMappingDefinitionUnmanaged(safeParams.MinMappings);
                MaxMappings = new JointMappingDefinitionUnmanaged(safeParams.MaxMappings);

                optional_RetargetingFlags = safeParams.optional_RetargetingFlags;
            }

            public void Dispose()
            {
                SourceSkeleton.Dispose();
                TargetSkeleton.Dispose();
            }
        }

        /// <summary>
        /// Profiler scope for measuring performance around a block of code.
        /// Used internally to track performance of various operations.
        /// </summary>
        internal struct ProfilerScope : IDisposable
        {
            /// <summary>
            /// Constructor for <see cref="ProfilerScope"/>.
            /// </summary>
            /// <param name="name">The name of the profiler sample.</param>
            public ProfilerScope(string name) => Profiler.BeginSample(name);

            void IDisposable.Dispose() => Profiler.EndSample();
        }

        /// <summary>
        /// Body tracking frame data that we should record.
        /// Contains information about the current tracking state, including joint positions,
        /// timestamps, confidence values, and other tracking metadata.
        /// </summary>
        [StructLayout(LayoutKind.Sequential), Serializable]
        public struct FrameData
        {
            /// <summary>
            /// Main constructor for FrameData.
            /// Initializes a new instance with all tracking information.
            /// </summary>
            /// <param name="bodyTrackingFidelity">Body tracking fidelity.</param>
            /// <param name="timestamp">Timestamp.</param>
            /// <param name="isValid">Valid state.</param>
            /// <param name="confidence">Data confidence.</param>
            /// <param name="jointSet">Joint set.</param>
            /// <param name="calibrationState">Calibration state.</param>
            /// <param name="skeletonChangeCount">Skeleton change count.</param>
            /// <param name="isUsingHandsLeft">Is using hands (left).</param>
            /// <param name="isUsingHandsRight">Is using hands (right).</param>
            /// <param name="leftInput">Left input.</param>
            /// <param name="rightInput">Right input.</param>
            /// <param name="centerEye">Center eye.</param>
            public FrameData(
                byte bodyTrackingFidelity,
                double timestamp,
                bool isValid,
                float confidence,
                byte jointSet,
                byte calibrationState,
                uint skeletonChangeCount,
                bool isUsingHandsLeft,
                bool isUsingHandsRight,
                NativeTransform leftInput,
                NativeTransform rightInput,
                NativeTransform centerEye)
            {
                BodyTrackingFidelity = bodyTrackingFidelity;
                Timestamp = timestamp;
                IsValid = isValid;
                Confidence = confidence;
                JointSet = jointSet;
                CalibrationState = calibrationState;
                SkeletonChangeCount = skeletonChangeCount;
                IsUsingHandsLeft = isUsingHandsLeft;
                IsUsingHandsRight = isUsingHandsRight;

                LeftInput = leftInput;
                RightInput = rightInput;
                CenterEye = centerEye;
            }

            /// <summary>
            /// Body tracking fidelity level.
            /// Indicates the quality/detail level of the tracking data.
            /// </summary>
            public byte BodyTrackingFidelity;

            /// <summary>
            /// Timestamp of when this frame data was captured.
            /// Measured in seconds since an application-defined epoch.
            /// </summary>
            public double Timestamp;

            /// <summary>
            /// Indicates whether the tracking data in this frame is valid or not.
            /// Invalid data should not be used for animation or other purposes.
            /// </summary>
            [MarshalAs(UnmanagedType.U1)]
            public bool IsValid;

            /// <summary>
            /// Data confidence level between 0.0 and 1.0.
            /// Higher values indicate greater confidence in the tracking data's accuracy.
            /// </summary>
            public float Confidence;

            /// <summary>
            /// Tracking joint set identifier.
            /// Indicates which set of joints is being tracked in this frame.
            /// </summary>
            public byte JointSet;

            /// <summary>
            /// Body tracking calibration state.
            /// Indicates the current calibration status of the tracking system.
            /// </summary>
            public byte CalibrationState;

            /// <summary>
            /// True if left hand is based on hand tracking input (false if controllers).
            /// Determines whether the left hand data comes from hand tracking or controller tracking.
            /// </summary>
            [MarshalAs(UnmanagedType.U1)]
            public bool IsUsingHandsLeft;

            /// <summary>
            /// True if right hand is based on hand tracking input (false if controllers).
            /// Determines whether the right hand data comes from hand tracking or controller tracking.
            /// </summary>
            [MarshalAs(UnmanagedType.U1)]
            public bool IsUsingHandsRight;

            /// <summary>
            /// Skeleton change count.
            /// Increments whenever the skeleton configuration changes, allowing detection of structural changes.
            /// </summary>
            public uint SkeletonChangeCount;

            /// <summary>
            /// Left input (hand or controller) transform.
            /// Contains position, orientation, and scale information for the left hand or controller.
            /// </summary>
            public NativeTransform LeftInput;

            /// <summary>
            /// Right input (hand or controller) transform.
            /// Contains position, orientation, and scale information for the right hand or controller.
            /// </summary>
            public NativeTransform RightInput;

            /// <summary>
            /// Center eye transform.
            /// Contains position, orientation, and scale information for the center eye/head position.
            /// </summary>
            public NativeTransform CenterEye;

            public override string ToString()
            {
                return $"Fidelity: {BodyTrackingFidelity}, timestamp: {Timestamp}, valid: {IsValid}, " +
                       $"Joint set: {JointSet}, calibration state: {CalibrationState}, " +
                       $"skeleton change count: {SkeletonChangeCount}, " +
                       $"is using hands left: {IsUsingHandsLeft}, " +
                       $"is using hands right: {IsUsingHandsRight}, " +
                       $"left input: {LeftInput.ToString()}, " +
                       $"right input: {RightInput.ToString()}, " +
                       $"center: {CenterEye.ToString()}.";
            }
        }

        /// <summary>
        /// Start header for serialization, containing metadata about the recording.
        /// This structure is serialized into native code and marks the beginning of a recording.
        /// </summary>
        [StructLayout(LayoutKind.Sequential), Serializable]
        public struct StartHeader
        {
            /// <summary>
            /// Identifies the version of the data format being used in the recording.
            /// </summary>
            public double DataVersion;

            /// <summary>
            /// Operating system version string.
            /// Identifies the OS version where the recording was created.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr,
                SizeConst = SERIALIZATION_START_HEADER_STRING_SIZE_BYTES)]
            public string OSVersion;

            /// <summary>
            /// Game engine version string.
            /// Identifies the game engine version used to create the recording.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr,
                SizeConst = SERIALIZATION_START_HEADER_STRING_SIZE_BYTES)]
            public string GameEngineVersion;

            /// <summary>
            /// Application bundle ID string.
            /// Identifies the application that created the recording.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr,
                SizeConst = SERIALIZATION_START_HEADER_STRING_SIZE_BYTES)]
            public string BundleID;

            /// <summary>
            /// Meta XR SDK version string.
            /// Identifies the version of the Meta XR SDK used to create the recording.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr,
                SizeConst = SERIALIZATION_START_HEADER_STRING_SIZE_BYTES)]
            public string MetaXRSDKVersion;

            /// <summary>
            /// Recording UTC timestamp.
            /// The time when the recording was started, in UTC milliseconds since epoch.
            /// </summary>
            public long UTCTimestamp;

            /// <summary>
            /// Number of snapshots that exist in the recording.
            /// Each snapshot represents a frame of motion data.
            /// </summary>
            public int NumSnapshots;

            /// <summary>
            /// Total number of bytes used by all snapshots in the recording.
            /// Useful for memory allocation and storage planning.
            /// </summary>
            public int NumTotalSnapshotBytes;

            /// <summary>
            /// Start network time.
            /// The network time when the recording was started, used for synchronization.
            /// </summary>
            public double StartNetworkTime;

            /// <summary>
            /// Number of buffered snapshots.
            /// Indicates how many snapshots are being held in memory before writing to storage.
            /// </summary>
            public int NumBufferedSnapshots;

            /// <summary>
            /// Default start header constructor.
            /// Initializes a new StartHeader with all required metadata for a recording.
            /// </summary>
            /// <param name="dataVersion">Data version.</param>
            /// <param name="osVersion">OS version.</param>
            /// <param name="gameEngineVersion">Game engine version.</param>
            /// <param name="bundleID">Bundle ID.</param>
            /// <param name="metaXRSDKVersion">Meta XR SDK version.</param>
            /// <param name="utcTimeStamp">UTC timestamp.</param>
            /// <param name="numSnapshots">Num frames recorded.</param>
            /// <param name="numTotalSnapshotBytes">Num snapshot bytes recored.</param>
            /// <param name="startNetworkTime">Start network time.</param>
            /// <param name="numberOfBufferedSnapshots">Number of buffered snapshots.</param>
            public StartHeader(
                double dataVersion,
                string osVersion,
                string gameEngineVersion,
                string bundleID,
                string metaXRSDKVersion,
                long utcTimeStamp,
                int numSnapshots,
                int numTotalSnapshotBytes,
                double startNetworkTime,
                int numberOfBufferedSnapshots)
            {
                DataVersion = dataVersion;
                OSVersion = osVersion;
                GameEngineVersion = gameEngineVersion;
                BundleID = bundleID;
                MetaXRSDKVersion = metaXRSDKVersion;
                UTCTimestamp = utcTimeStamp;
                NumSnapshots = numSnapshots;
                NumTotalSnapshotBytes = numTotalSnapshotBytes;
                StartNetworkTime = startNetworkTime;
                NumBufferedSnapshots = numberOfBufferedSnapshots;
            }

            /// <inheritdoc />
            public override string ToString()
            {
                return $"Data: {DataVersion}," +
                       $"OS: {OSVersion}, " +
                       $"Game Engine: {GameEngineVersion}, " +
                       $"BundleID: {BundleID}," +
                       $"MetaXRSDKVersion: {MetaXRSDKVersion}, " +
                       $"UTCTimestamp: {UTCTimestamp}, " +
                       $"NumSnapshots: {NumSnapshots}, " +
                       $"NumSnapshotBytes: {NumTotalSnapshotBytes}, " +
                       $"StartNetworkTime: {StartNetworkTime}, " +
                       $" NumBufferedSnapshots: {NumBufferedSnapshots}.";
            }

            /// <summary>
            /// Creates a clone.
            /// </summary>
            /// <returns>A new clone of the current object.</returns>
            public StartHeader Clone()
            {
                return new StartHeader(
                    dataVersion: DataVersion,
                    osVersion: OSVersion,
                    gameEngineVersion: GameEngineVersion,
                    bundleID: BundleID,
                    metaXRSDKVersion: MetaXRSDKVersion,
                    utcTimeStamp: UTCTimestamp,
                    NumSnapshots,
                    NumTotalSnapshotBytes,
                    StartNetworkTime,
                    NumBufferedSnapshots);
            }
        }

        /// <summary>
        /// Container for start header bytes passed to native code.
        /// Provides a fixed-size byte array for serializing the StartHeader structure.
        /// </summary>
        [StructLayout(LayoutKind.Sequential), Serializable]
        public struct StartHeaderSerializedBytes
        {
            /// <summary>
            /// Serialized start header bytes.
            /// The byte array containing the serialized StartHeader data.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = SERIALIZATION_START_HEADER_SIZE_BYTES)]
            public byte[] SerializedBytes;

            /// <summary>
            /// Constructor with bytes to copy from.
            /// Initializes the structure with a copy of the provided byte array.
            /// </summary>
            /// <param name="bytes">Bytes to copy from.</param>
            public StartHeaderSerializedBytes(byte[] bytes)
            {
                SerializedBytes = new byte[SERIALIZATION_START_HEADER_SIZE_BYTES];
                Array.Copy(bytes, SerializedBytes, SERIALIZATION_START_HEADER_SIZE_BYTES);
            }
        }

        /// <summary>
        /// End header for serialization, containing metadata about the end of a recording.
        /// This structure is serialized into native code and marks the end of a recording.
        /// </summary>
        [StructLayout(LayoutKind.Sequential), Serializable]
        public struct EndHeader
        {
            /// <summary>
            /// Recording end UTC timestamp.
            /// The time when the recording was completed, in UTC milliseconds since epoch.
            /// </summary>
            public long UTCTimestamp;

            public EndHeader(long utcTimestamp)
            {
                UTCTimestamp = utcTimestamp;
            }

            /// <inheritdoc />
            public override string ToString()
            {
                return $"UTCTimestamp: {UTCTimestamp}.";
            }
        }

        /// <summary>
        /// Container for end header bytes passed to native code.
        /// Provides a fixed-size byte array for serializing the EndHeader structure.
        /// </summary>
        [StructLayout(LayoutKind.Sequential), Serializable]
        public struct EndHeaderSerializedBytes
        {
            /// <summary>
            /// Serialized end header bytes.
            /// The byte array containing the serialized EndHeader data.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = SERIALIZATION_END_HEADER_SIZE_BYTES)]
            public byte[] SerializedBytes;

            /// <summary>
            /// Constructor with bytes to copy from.
            /// Initializes the structure with a copy of the provided byte array.
            /// </summary>
            /// <param name="bytes">Bytes to copy from.</param>
            public EndHeaderSerializedBytes(byte[] bytes)
            {
                SerializedBytes = new byte[SERIALIZATION_END_HEADER_SIZE_BYTES];
                Array.Copy(bytes, SerializedBytes, SERIALIZATION_END_HEADER_SIZE_BYTES);
            }
        }

        /// <summary>
        /// Determines if the specified compression type uses joint length compression.
        /// Joint length compression provides higher compression ratios but may reduce precision.
        /// This method helps determine the appropriate decompression approach for serialized data.
        /// </summary>
        /// <param name="compressionType">The compression type to check.</param>
        /// <returns>True if the compression type uses joint lengths; otherwise, false.</returns>
        public static bool CompressionUsesJointLengths(SerializationCompressionType compressionType)
        {
            return compressionType == SerializationCompressionType.High;
        }

        /// <summary>
        /// Interface for DLL calls to the native Movement SDK Utility plugin.
        /// Contains all the P/Invoke method declarations for communicating with the native code.
        /// </summary>
        private static class Api
        {
            /**********************************************************
             *
             *               Lifecycle Functions
             *
             **********************************************************/

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern Result metaMovementSDK_initialize(CoordinateSpace coordinateSpace);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern Result metaMovementSDK_initializeLogging(LogCallback logCallback);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern Result metaMovementSDK_createOrUpdateHandle(string config, out ulong handle);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern Result metaMovementSDK_destroy(ulong handle);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern Result metaMovementSDK_createOrUpdateSimpleUtilityConfig(
                string configName,
                SkeletonType skeletonType,
                ref SkeletonInitParamsUnmanaged initParamsUnmanaged,
                out ulong handle);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern Result metaMovementSDK_createOrUpdateUtilityConfig(
                string configName,
                ref ConfigInitParamsUnmanaged initParamsUnmanaged,
                out ulong handle);

            /**********************************************************
             *
             *               Query Functions
             *
             **********************************************************/

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern Result metaMovementSDK_getCoordinateSpace(out CoordinateSpace coordinateSpace);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_getConfigName(
                ulong handle,
                byte* outBuffer,
                out int inOutBufferSize);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_getConfigRetargetingFlags(
                ulong handle,
                out RetargetingBehaviorFlags retargetingFlags);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern Result metaMovementSDK_getVersion(ulong handle, out double version);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern Result metaMovementSDK_getSerializationVersion(out double version);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern Result metaMovementSDK_isSerializationVersionMinSupported(double version);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern Result metaMovementSDK_getSkeletonInfo(
                ulong handle,
                SkeletonType skeletonType,
                out SkeletonInfo outSkeletonInfo);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_getBlendShapeNames(
                ulong handle,
                SkeletonType skeletonType,
                byte* outBuffer,
                out int inOutBufferSize,
                void* outUnusedBlendShapeNames,
                out int inOutNumBlendShapeNames);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_getJointNames(
                ulong handle,
                SkeletonType skeletonType,
                byte* outBuffer,
                out int inOutBufferSize,
                void* outUnusedJointNames,
                out int inOutNumJointNames);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_getJointNamesFromIndexList(
                ulong handle,
                SkeletonType skeletonType,
                int* inJointIndexList,
                int inJointIndexCount,
                byte* outBuffer,
                out int inOutBufferSize,
                void* optionalOutJointNames);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_getAutoMappingAdditionalJointData(
                ulong handle,
                SkeletonType skeletonType,
                byte* outBuffer,
                out int inOutBufferSize,
                AutoMappingJointDataUnmanaged* outJointData,
                out int inOutNumJoints);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_getSkeletonTPose(
                ulong handle,
                SkeletonType skeletonType,
                SkeletonTPoseType tPoseType,
                JointRelativeSpaceType jointSpaceType,
                NativeTransform* outTransformData,
                out int inOutNumJoints);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_calculateSkeletonTPose(
                ulong handle,
                SkeletonType skeletonType,
                JointRelativeSpaceType jointSpaceType,
                float delta,
                NativeTransform* outTransformData,
                out int inOutNumJoints);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_calculateSkeletonTPoseAtHeight(
                ulong handle,
                SkeletonType skeletonType,
                JointRelativeSpaceType jointSpaceType,
                float height,
                NativeTransform* outTransformData,
                out int inOutNumJoints);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_getSkeletonMappings(
                ulong handle,
                SkeletonTPoseType tPoseType,
                JointMapping* outMappingData,
                out int inOutNumMappings);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_getSkeletonMappingEntries(
                ulong handle,
                SkeletonTPoseType tPoseType,
                JointMappingEntry* outMappingEntryData,
                out int inOutNumEntries);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_getSkeletonMappingTargetJoints(
                ulong handle,
                int* outJointIndexList,
                out int inOutJointCount);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_getBlendShapeName(
                ulong handle,
                SkeletonType skeletonType,
                int blendShapeIndex,
                byte* outBuffer,
                out int inOutBufferSize);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_getJointName(
                ulong handle,
                SkeletonType skeletonType,
                int jointIndex,
                byte* outBuffer,
                out int inOutBufferSize);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern Result metaMovementSDK_getJointIndex(
                ulong handle,
                SkeletonType skeletonType,
                string jointName,
                out int jointIndex);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern Result metaMovementSDK_getJointIndexByKnownJointType(
                ulong handle,
                SkeletonType skeletonType,
                KnownJointType knownJointType,
                out int jointIndex);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern Result metaMovementSDK_getKnownJointIndexes(
                ulong handle,
                SkeletonType skeletonType,
                out KnownJointIndexData outKnownJoints);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern Result metaMovementSDK_getParentJointIndex(
                ulong handle,
                SkeletonType skeletonType,
                int jointIndex,
                out int outParentJointIndex);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_getParentJointIndexes(
                ulong handle,
                SkeletonType skeletonType,
                int* outJointIndexArray,
                out int inOutNumJoints);

            // Humanoid Limbs
            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern Result metaMovementSDK_getHumanoidLimbTypeFromJointName(
                ulong handle,
                SkeletonType skeletonType,
                string jointName,
                out HumanoidLimbType outHumanoidLimbType);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern Result metaMovementSDK_getHumanoidLimbTypeFromJointIndex(
                ulong handle,
                SkeletonType skeletonType,
                int jointIndex,
                out HumanoidLimbType outHumanoidLimbType);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_getJointIndexesInHumanoidLimb(
                ulong handle,
                SkeletonType skeletonType,
                HumanoidLimbType humanoidLimbType,
                int* outJointIndexList,
                out int inOutJointCount);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_getManifestationNames(
                ulong handle,
                SkeletonType skeletonType,
                byte* outBuffer,
                out int inOutBufferSize,
                void* outUnusedManifestationNames,
                out int inOutNumManifestationNames);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_getJointsInManifestation(
                ulong handle,
                SkeletonType skeletonType,
                string manifestationName,
                int* outJointIndexList,
                out int inOutJointCount);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern Result metaMovementSDK_getPoseInfo(
                ulong handle,
                SkeletonType skeletonType,
                SkeletonTPoseType poseType,
                out PoseInfo outPoseInfo);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern Result metaMovementSDK_getLengthBetweenJoints(
                ulong handle,
                SkeletonType skeletonType,
                SkeletonTPoseType poseType,
                int startJointIndex,
                int endJointIndex,
                out float outLength);

            /**********************************************************
             *
             *               Retargeting Functions
             *
             **********************************************************/

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_updateSourceReferenceTPose(
                ulong handle,
                NativeTransform* inTransformData,
                int inNumJoints,
                string manifestation);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_retargetFromSourceFrameData(
                ulong handle,
                RetargetingBehaviorInfo retargetingBehaviorInfo,
                NativeTransform* inSourceTransformData,
                int inNumSourceJoints,
                NativeTransform* outRetargetedTargetTransformData,
                ref int inOutNumTargetJoints,
                string sourceManifestation,
                string targetOutputManifestation);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_getLastProcessedFramePose(
                ulong handle,
                SkeletonType skeletonType,
                JointRelativeSpaceType outJointSpaceType,
                NativeTransform* outTransformData,
                out int inOutNumJoints,
                string manifestation);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_captureLastProcessedFramePoseToConfig(
                ulong handle,
                out ulong captureHandle);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_getLastRetargetedMappingData(
                ulong handle,
                int targetJointIndex,
                out SkeletonType outSourceSkeletonType,
                out NativeTransform outTPoseBlendedTransform,
                out NativeTransform outLastPoseBlendedTransform,
                int* outSourceJointIndexList,
                out int inOutSourceJointCount);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_matchCurrentTPose(
                ulong handle,
                SkeletonType skeletonType,
                MatchPoseBehavior matchBehavior,
                JointRelativeSpaceType jointSpaceType,
                int jointCount,
                NativeTransform* inOutTransformData);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_matchPose(
                ulong handle,
                SkeletonType skeletonType,
                MatchPoseBehavior matchBehavior,
                JointRelativeSpaceType jointSpaceType,
                int jointCount,
                NativeTransform* inTransformSourcePose,
                NativeTransform* inOutTransformData);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_scalePoseToHeight(
                ulong handle,
                SkeletonType skeletonType,
                JointRelativeSpaceType jointSpaceType,
                float height,
                int jointCount,
                NativeTransform* inOutTransformData);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern Result metaMovementSDK_alignTargetToSource(
                string configName,
                AlignmentFlags alignmentBehavior,
                ulong sourceConfigHandle,
                SkeletonType sourceSkeletonType,
                ulong targetConfigHandle,
                out ulong handle);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_alignInputToSource(
                string configName,
                AlignmentFlags alignmentBehavior,
                NativeTransform* inputTargetSkeleton,
                int inputTargetSkeletonJointCount,
                ulong sourceConfigHandle,
                SkeletonType sourceConfigSkeletonType,
                ulong targetConfigHandle,
                out ulong handle);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public unsafe static extern Result metaMovementSDK_generateMappings(
                ulong handle,
                AutoMappingFlags autoMappingBehaviorFlags,
                AutoMappingJointDataUnmanaged* additionalAutoMappingJointData,
                int additionalJointDataCount);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_getTwistJoints(
                ulong handle,
                SkeletonType skeletonType,
                TwistJointDefinition* outTwistJointDefinitions,
                out int inOutNumTwistJoints);

            /**********************************************************
             *
             *               Serialization Functions
             *
             **********************************************************/

            // Configuration information.
            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern Result metaMovementSDK_getSerializationSettings(ulong handle,
                out SerializationSettings outSerializationSettings);

            // Configuration information.
            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern Result metaMovementSDK_setSerializationSettings(ulong handle,
                SerializationSettings inMutableSettings);

            // Serialization.
            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern Result metaMovementSDK_buildSnapshot(
                ulong handle,
                ref SnapshotDataUnmanaged snapshotDataUnmanaged);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_serializeSnapshot(
                ulong handle,
                void* outBuffer,
                out int inOutBufferSizeBytes);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern Result metaMovementSDK_serializeStartHeader(
                StartHeader startHeader,
                ref StartHeaderSerializedBytes headerBytes);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern Result metaMovementSDK_serializeEndHeader(
                EndHeader endHeader,
                ref EndHeaderSerializedBytes headerBytes);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern Result metaMovementSDK_deserializeStartHeader(
                out StartHeader startHeader,
                StartHeaderSerializedBytes headerBytes);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern Result metaMovementSDK_deserializeEndHeader(
                out EndHeader endHeader,
                EndHeaderSerializedBytes headerBytes);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_deserializeSnapshotTimestamp(
                void* snapshotInBytes,
                out double outTimestamp);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_deserializeSnapshotData(
                ulong handle,
                void* snapshotInBytes,
                double dataVersion,
                ref DeserializedSnapshotDataUnmanaged deserializeSnapshotData);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_getInterpolatedSkeletonPose(
                ulong handle,
                SkeletonType skeletonType,
                double timestamp,
                NativeTransform* outSkeletonPose,
                out int inOutNumJoints);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_getInterpolatedFacePose(
                ulong handle,
                SkeletonType skeletonType,
                double timestamp,
                float* outFacePose,
                out int inOutNumShapes);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern Result metaMovementSDK_getInterpolatedTrackerJointPose(
                ulong handle,
                TrackerJointType trackerJointType,
                double timestamp,
                ref NativeTransform nativeTransform);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern Result metaMovementSDK_resetInterpolators(ulong handle);

            /**********************************************************
             *
             *               Tool and Data Functions
             *
             **********************************************************/

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_writeConfigDataToJSON(
                ulong handle,
                CoordinateSpace* optionalCoordinateSpace,
                JointRelativeSpaceType* optional_jointSpaceType,
                byte* outBuffer,
                out int inOutBufferSize);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_applyWorldSpaceCoordinateSpaceConversion(
                CoordinateSpace inCoordinateSpace,
                CoordinateSpace outCoordinateSpace,
                NativeTransform* transformData,
                int jointCount);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_applyCoordinateSpaceConversion(
                ulong handle,
                SkeletonType skeletonType,
                JointRelativeSpaceType jointSpaceType,
                CoordinateSpace inCoordinateSpace,
                CoordinateSpace outCoordinateSpace,
                NativeTransform* skeletonPose,
                int jointCount);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_convertJointPose(
                ulong handle,
                SkeletonType skeletonType,
                JointRelativeSpaceType inJointSpaceType,
                JointRelativeSpaceType outJointSpaceType,
                NativeTransform* skeletonPose,
                int jointCount);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_calculatePoseExtents(
                ulong handle,
                SkeletonType skeletonType,
                JointRelativeSpaceType inJointSpaceType,
                NativeTransform* skeletonPose,
                int jointCount,
                out Extents outExtents);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern Result metaMovementSDK_identifyKnownJointIndexesFromRestPose(
                    ulong handle,
                    SkeletonType skeletonType,
                    out KnownJointIndexData outKnownJoints);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_getKnownJointNamesFromIndexData(
                ulong handle,
                SkeletonType skeletonType,
                KnownJointIndexData knownJointIndexData,
                byte* outBuffer,
                out int inOutBufferSize,
                KnownJointNameData* outKnownJointNames);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_identifyJointsToIncludeInMappingUsingRestPose(
                ulong handle,
                SkeletonType skeletonType,
                byte* outBuffer,
                out int inOutBufferSize,
                void* optionalOutJointNames,
                out int optionalInOutNumJoints);

            [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe Result metaMovementSDK_identifyPossibleExcludeFromMappingUsingRestPose(
                ulong handle,
                SkeletonType skeletonType,
                byte* outBuffer,
                out int inOutBufferSize,
                void* optionalOutJointNames,
                out int optionalInOutNumJoints);

        }

        #region Unity API

        /**********************************************************
         *
         *               Lifecycle Functions
         *
         **********************************************************/

        /// <summary>
        /// Initialize the plugin to use a specific coordinate space.
        /// This must be called before any other operations to set up the coordinate system.
        /// The coordinate space defines how positions and orientations are interpreted.
        /// </summary>
        /// <param name="coordinateSpace">The coordinate space to use, defining up, forward, and right vectors.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool Initialize(CoordinateSpace coordinateSpace)
        {
            Result success;
            using (new ProfilerScope(nameof(Initialize)))
            {
                success = Api.metaMovementSDK_initialize(coordinateSpace);
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Initialize the plugin logging.
        /// </summary>
        /// <returns>True if the function was successfully executed.</returns>
        // Static method for log callback to avoid IL2CPP marshaling issues with lambda expressions
        [AOT.MonoPInvokeCallback(typeof(LogCallback))]
        private static void HandleLogCallback(LogLevel logLevel, IntPtr logMessage)
        {
            // Convert the message pointer to a string
            var message = Marshal.PtrToStringAnsi(logMessage);

            // Forward the message to Unity's logging system based on the log level
            switch (logLevel)
            {
                case LogLevel.Error:
                    Debug.LogError($"[MSDKPlugin]{message}");
                    break;
                case LogLevel.Warn:
                    Debug.LogWarning($"[MSDKPlugin]{message}");
                    break;
                case LogLevel.Info:
                case LogLevel.Debug:
                default:
                    Debug.Log($"[MSDKPlugin]{message}");
                    break;
            }
        }

        public static bool InitializeLogging()
        {
            Result success;
            _logCallback ??= HandleLogCallback;

            using (new ProfilerScope(nameof(InitializeLogging)))
            {
                success = Api.metaMovementSDK_initializeLogging(_logCallback);
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Creates or updates a simple utility config and returns a handle for accessing the result.
        /// This is a simplified version of CreateOrUpdateUtilityConfig for cases where only one skeleton is needed.
        /// Use this method when you need to define a single skeleton without mapping to another skeleton.
        /// </summary>
        /// <param name="configName">The name of the config.</param>
        /// <param name="skeletonType">The type (Source or Target) of the skeleton to create in this config.</param>
        /// <param name="initParams">Initialization parameters to define the skeleton.</param>
        /// <param name="handle">The handle that can be used for accessing the resulting config.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool CreateOrUpdateSimpleUtilityConfig(
            string configName,
            SkeletonType skeletonType,
            SkeletonInitParams initParams,
            out ulong handle)
        {
            Result success;
            using (new ProfilerScope(nameof(CreateOrUpdateUtilityConfig)))
            {
                unsafe
                {
                    SkeletonInitParamsUnmanaged initParamsUnmanaged = new SkeletonInitParamsUnmanaged(initParams);
                    try
                    {
                        success = Api.metaMovementSDK_createOrUpdateSimpleUtilityConfig(
                            configName,
                            skeletonType,
                            ref initParamsUnmanaged,
                            out handle);
                    }
                    finally
                    {
                        initParamsUnmanaged.Dispose();
                    }
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Create or update a config and return a handle for accessing the result.
        /// This comprehensive method sets up both source and target skeletons with their respective
        /// blend shapes, joint hierarchies, and T-poses, along with the mapping between them.
        /// Use this method when you need to define a complete retargeting setup between two different skeletons.
        /// </summary>
        /// <param name="configName">The name of the config.</param>
        /// <param name="initParams">Initialization parameters to define the skeleton.</param>
        /// <param name="handle">The handle that can be used for accessing the resulting config.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool CreateOrUpdateUtilityConfig(string configName, ConfigInitParams initParams, out ulong handle)
        {
            Result success;
            using (new ProfilerScope(nameof(CreateOrUpdateUtilityConfig)))
            {
                unsafe
                {
                    ConfigInitParamsUnmanaged initParamsUnmanaged = new ConfigInitParamsUnmanaged(initParams);
                    try
                    {
                        success = Api.metaMovementSDK_createOrUpdateUtilityConfig(
                            configName,
                            ref initParamsUnmanaged,
                            out handle);
                    }
                    finally
                    {
                        initParamsUnmanaged.Dispose();
                    }
                }
            }

            bool wasSuccesful = success == Result.Success;
            const string CREATE_OR_UPDATE_UTIL_ERROR_MESSAGE = "Failed to create or update utility config.";
            TelemetryManager.SendConfigEvent(TelemetryManager._CREATE_OR_UPDATE_UTILITY_CONFIG_EVENT_NAME,
                wasSuccesful ? null : CREATE_OR_UPDATE_UTIL_ERROR_MESSAGE);
            return wasSuccesful;
        }

        /// <summary>
        /// Creates or updates a handle using a config string.
        /// This method allows for creating a configuration from a JSON string rather than
        /// specifying all parameters individually.
        /// Use this when you have a pre-defined configuration in JSON format, such as from a saved file.
        /// </summary>
        /// <param name="config">The contents of a config in JSON format.</param>
        /// <param name="handle">The handle that can be used for accessing the resulting config.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool CreateOrUpdateHandle(string config, out ulong handle)
        {
            Result success;
            using (new ProfilerScope(nameof(CreateOrUpdateHandle)))
            {
                success = Api.metaMovementSDK_createOrUpdateHandle(config, out handle);
            }
            bool wasSuccesful = success == Result.Success;
            const string CREATE_OR_UPDATE_HANDLE_ERROR_MESSAGE = "Failed to create or update handle.";
            TelemetryManager.SendConfigEvent(TelemetryManager._CREATE_OR_UPDATE_HANDLE_EVENT_NAME,
                wasSuccesful ? null : CREATE_OR_UPDATE_HANDLE_ERROR_MESSAGE);
            return wasSuccesful;
        }

        /// <summary>
        /// Destroy the specified handle instance.
        /// This releases all resources associated with the handle and should be called
        /// when the handle is no longer needed to prevent memory leaks.
        /// Always call this method when you're done with a handle to ensure proper cleanup.
        /// </summary>
        /// <param name="handle">The handle to be destroyed.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool DestroyHandle(ulong handle)
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
        /// Get the coordinate space that the plugin is using.
        /// This retrieves the current coordinate space configuration that was set during initialization.
        /// The coordinate space defines the orientation of the up, forward, and right vectors.
        /// </summary>
        /// <param name="coordinateSpace">Output parameter that receives the coordinate space information.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetCoordinateSpace(out CoordinateSpace coordinateSpace)
        {
            Result success;
            using (new ProfilerScope(nameof(GetCoordinateSpace)))
            {
                success = Api.metaMovementSDK_getCoordinateSpace(out coordinateSpace);
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Get the name of the config for a handle.
        /// This retrieves the name that was assigned to the configuration when it was created.
        /// The config name can be used for identification and debugging purposes.
        /// </summary>
        /// <param name="handle">The handle to get the config name from.</param>
        /// <param name="configName">Output parameter that receives the name of the config.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetConfigName(ulong handle, out string configName)
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
                            configName = Marshal.PtrToStringAnsi((IntPtr)nameBuffer, stringLength).TrimEnd('\0');
                        }
                    }
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Gets the retargeting behavior flags configured for this configuration data.
        /// </summary>
        /// <param name="handle">Handle to the configuration data</param>
        /// <param name="retargetingFlags">Output parameter for the retargeting behavior flags</param>
        /// <returns>True if successful, false otherwise</returns>
        public static bool GetConfigRetargetingFlags(ulong handle, out RetargetingBehaviorFlags retargetingFlags)
        {
            Result success;
            using (new ProfilerScope(nameof(GetConfigRetargetingFlags)))
            {
                success = Api.metaMovementSDK_getConfigRetargetingFlags(handle, out retargetingFlags);
            }
            return success == Result.Success;
        }

        /// <summary>
        /// Gets the version number of the configuration.
        /// This can be used to check compatibility between different configurations.
        /// Version numbers help ensure that configurations are compatible with the current SDK.
        /// </summary>
        /// <param name="handle">The handle to get the version from.</param>
        /// <param name="version">Output parameter that receives the config version number.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetVersion(ulong handle, out double version)
        {
            Result success;
            using (new ProfilerScope(nameof(GetConfigName)))
            {
                unsafe
                {
                    success = Api.metaMovementSDK_getVersion(handle, out version);
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Gets the version of serialized data.
        /// Version numbers help ensure that the bytes deserialized are compatible with the current SDK.
        /// </summary>
        /// <param name="version">Output parameter that receives the serialization version number.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetSerializationVersion(out double version)
        {
            Result success;
            using (new ProfilerScope(nameof(GetConfigName)))
            {
                unsafe
                {
                    success = Api.metaMovementSDK_getSerializationVersion(out version);
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Verifies that the serialization version passed in compatible with the SDK.
        /// Version numbers help ensure that the bytes deserialized are compatible with the current SDK.
        /// </summary>
        /// <param name="version">Serialization version.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetIsSerializationVersionSupported(double version)
        {
            Result success;
            using (new ProfilerScope(nameof(GetIsSerializationVersionSupported)))
            {
                unsafe
                {
                    success = Api.metaMovementSDK_isSerializationVersionMinSupported(version);
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Get the skeleton info for a handle.
        /// This retrieves basic information about the skeleton structure, including the number of joints and blendshapes.
        /// Use this to understand the structure of a skeleton before performing operations on it.
        /// </summary>
        /// <param name="handle">The handle to get the skeleton info from.</param>
        /// <param name="skeletonType">The type of skeleton (source or target) to get info from.</param>
        /// <param name="skeletonInfo">Output parameter that receives the skeleton information.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetSkeletonInfo(ulong handle, SkeletonType skeletonType, out SkeletonInfo skeletonInfo)
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
        /// Blendshapes are used for facial expressions and other deformations that aren't handled by the skeleton joints.
        /// This method returns an array of strings containing all blendshape names defined in the skeleton.
        /// </summary>
        /// <param name="handle">The handle to get the blendshapes from.</param>
        /// <param name="skeletonType">The type of skeleton to get the blendshape names from.</param>
        /// <param name="blendShapeNames">Output array that receives the blendshape names.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetBlendShapeNames(ulong handle, SkeletonType skeletonType, out string[] blendShapeNames)
        {
            Result success;
            // Assign an Empty String to the output array
            blendShapeNames = Array.Empty<string>();
            using (new ProfilerScope(nameof(GetBlendShapeNames)))
            {
                unsafe
                {
                    success = Api.metaMovementSDK_getBlendShapeNames(handle, skeletonType, null, out var bufferSize,
                        null, out var nameCount);
                    if (success == Result.Success)
                    {
                        var buffer = new byte[bufferSize];
                        Span<byte> nameBuffer = buffer;
                        fixed (byte* bytes = &nameBuffer.GetPinnableReference())
                        {
                            success = Api.metaMovementSDK_getBlendShapeNames(handle, skeletonType, bytes,
                                out bufferSize, null, out nameCount);
                            if (success == Result.Success)
                            {
                                ConvertByteBufferToStringArray(bytes, bufferSize, nameCount, out blendShapeNames);
                            }
                        }
                    }
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Get the name of a specific blendshape by its index.
        /// This method retrieves the name of a single blendshape identified by its index in the skeleton.
        /// Use this when you know the index but need the corresponding name for display or mapping purposes.
        /// </summary>
        /// <param name="handle">The handle to get the blendshape from.</param>
        /// <param name="skeletonType">The type of skeleton to get the blendshape name from.</param>
        /// <param name="blendShapeIndex">The index of the blendshape to get the name for.</param>
        /// <param name="blendShapeName">Output parameter that receives the blendshape name.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetBlendShapeName(ulong handle, SkeletonType skeletonType, int blendShapeIndex,
            out string blendShapeName)
        {
            Result success;
            blendShapeName = string.Empty;
            using (new ProfilerScope(nameof(GetBlendShapeName)))
            {
                unsafe
                {
                    success = Api.metaMovementSDK_getBlendShapeName(handle, skeletonType, blendShapeIndex, null,
                        out var stringLength);
                    if (success == Result.Success)
                    {
                        var nameBuffer = stackalloc byte[stringLength];
                        success = Api.metaMovementSDK_getBlendShapeName(handle, skeletonType, blendShapeIndex,
                            nameBuffer, out stringLength);
                        if (success == Result.Success)
                        {
                            blendShapeName = Marshal.PtrToStringAnsi((IntPtr)nameBuffer, stringLength).TrimEnd('\0');
                        }
                    }
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Get the names of all joints for a skeleton type.
        /// These names identify each bone in the skeleton hierarchy and are used for mapping between skeletons.
        /// Joint names are essential for identifying specific parts of the skeleton for retargeting and animation.
        /// </summary>
        /// <param name="handle">The handle to get the joints from.</param>
        /// <param name="skeletonType">The type of skeleton to get the joint names from.</param>
        /// <param name="jointNames">Output array that receives the joint names.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetJointNames(ulong handle, SkeletonType skeletonType, out string[] jointNames)
        {
            Result success;
            jointNames = Array.Empty<string>();
            using (new ProfilerScope(nameof(GetJointNames)))
            {
                unsafe
                {
                    success = Api.metaMovementSDK_getJointNames(handle, skeletonType, null, out var bufferSize, null,
                        out var nameCount);
                    if (success == Result.Success)
                    {
                        var buffer = new byte[bufferSize];
                        Span<byte> nameBuffer = buffer;
                        fixed (byte* bytes = &nameBuffer.GetPinnableReference())
                        {
                            success = Api.metaMovementSDK_getJointNames(handle, skeletonType, bytes, out bufferSize,
                                null, out nameCount);
                            if (success == Result.Success)
                            {
                                ConvertByteBufferToStringArray(bytes, bufferSize, nameCount, out jointNames);
                            }
                        }
                    }
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Gets joint names from a list of joint indices for a given skeleton.
        /// This function retrieves the names of joints based on their indices from the skeleton configuration.
        /// </summary>
        /// <param name="handle">The handle to get the joint names from.</param>
        /// <param name="skeletonType">The type of skeleton to get the joint names for.</param>
        /// <param name="jointIndices">Array of joint indices to get the names for.</param>
        /// <param name="jointNames">Output array that receives the joint names.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetJointNamesFromIndexList(
            ulong handle,
            SkeletonType skeletonType,
            int[] jointIndices,
            out string[] jointNames)
        {
            Result result;
            jointNames = Array.Empty<string>();
            if (jointIndices == null || jointIndices.Length == 0)
            {
                return false;
            }
            using (new ProfilerScope(nameof(GetJointNamesFromIndexList)))
            {

                unsafe
                {
                    int bufferSize = 0;
                    fixed (int* jointIndexPtr = jointIndices)
                    {
                        result = Api.metaMovementSDK_getJointNamesFromIndexList(
                            handle, skeletonType, jointIndexPtr, jointIndices.Length, null, out bufferSize, null);

                        if (result == Result.Success && bufferSize > 0)
                        {
                            var buffer = new byte[bufferSize];
                            Span<byte> nameBuffer = buffer;
                            fixed (byte* bytes = &nameBuffer.GetPinnableReference())
                            {
                                result = Api.metaMovementSDK_getJointNamesFromIndexList(
                                    handle, skeletonType, jointIndexPtr, jointIndices.Length, bytes, out bufferSize, null);
                                if (result == Result.Success)
                                {
                                    ConvertByteBufferToStringArray(bytes, bufferSize, jointIndices.Length, out jointNames);
                                }
                            }
                        }
                    }
                }
            }

            return result == Result.Success;
        }

        /// <summary>
        /// Get the AutoMapping additional joint data for a specific skeleton type.
        /// This includes joint names and their associated AutoMapping flags that control
        /// how joints should be treated during the automapping process.
        /// </summary>
        /// <param name="handle">The handle to get the additional joint data from.</param>
        /// <param name="skeletonType">The type of skeleton to get the additional joint data from.</param>
        /// <param name="jointData">Output array that receives the AutoMapping joint data.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetAutoMappingAdditionalJointData(ulong handle, SkeletonType skeletonType, out AutoMappingJointData[] jointData)
        {
            Result success;
            jointData = Array.Empty<AutoMappingJointData>();
            using (new ProfilerScope(nameof(GetAutoMappingAdditionalJointData)))
            {
                unsafe
                {
                    // First, get the buffer size and count
                    success = Api.metaMovementSDK_getAutoMappingAdditionalJointData(handle, skeletonType, null, out var bufferSize, null, out var jointCount);
                    if (success == Result.Success && jointCount > 0)
                    {
                        var buffer = new byte[bufferSize];
                        var unmanagedJointData = new AutoMappingJointDataUnmanaged[jointCount];
                        fixed (byte* bytes = buffer)
                        fixed (AutoMappingJointDataUnmanaged* jointDataPtr = unmanagedJointData)
                        {
                            success = Api.metaMovementSDK_getAutoMappingAdditionalJointData(handle, skeletonType, bytes, out bufferSize, jointDataPtr, out jointCount);
                            if (success == Result.Success)
                            {
                                // Convert from unmanaged to managed structures
                                jointData = new AutoMappingJointData[jointCount];
                                for (int i = 0; i < jointCount; i++)
                                {
                                    jointData[i] = new AutoMappingJointData
                                    {
                                        JointName = Marshal.PtrToStringAnsi(unmanagedJointData[i].JointName),
                                        Flags = unmanagedJointData[i].Flags
                                    };
                                }
                            }
                        }
                    }
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Get the twist joints for a specific skeleton type.
        /// This method retrieves a list of all identified twist joints including both child aligned twist
        /// and detected twist joints for the skeleton.
        /// </summary>
        /// <param name="handle">The handle to get the twist joints from.</param>
        /// <param name="skeletonType">The type of skeleton to get the twist joints from.</param>
        /// <param name="twistJoints">Output array that receives the twist joint definitions.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetTwistJoints(ulong handle, SkeletonType skeletonType, out TwistJointDefinition[] twistJoints)
        {
            Result success;
            twistJoints = Array.Empty<TwistJointDefinition>();
            using (new ProfilerScope(nameof(GetTwistJoints)))
            {
                unsafe
                {
                    // First, get the count of twist joints
                    success = Api.metaMovementSDK_getTwistJoints(handle, skeletonType, null, out var twistJointCount);
                    if (success == Result.Success && twistJointCount > 0)
                    {
                        // Allocate array and get the twist joint definitions
                        twistJoints = new TwistJointDefinition[twistJointCount];
                        fixed (TwistJointDefinition* twistJointPtr = twistJoints)
                        {
                            success = Api.metaMovementSDK_getTwistJoints(handle, skeletonType, twistJointPtr, out twistJointCount);
                        }
                    }
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Get the name of a specific joint by its index.
        /// This method retrieves the name of a single joint identified by its index in the skeleton.
        /// Use this when you know the index but need the corresponding name for display or mapping purposes.
        /// </summary>
        /// <param name="handle">The handle to get the joint from.</param>
        /// <param name="skeletonType">The type of skeleton to get the joint name from.</param>
        /// <param name="jointIndex">The index of the joint to get the name for.</param>
        /// <param name="jointName">Output parameter that receives the joint name.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetJointName(ulong handle, SkeletonType skeletonType, int jointIndex, out string jointName)
        {
            Result success;
            jointName = string.Empty;
            using (new ProfilerScope(nameof(GetJointName)))
            {
                unsafe
                {
                    success = Api.metaMovementSDK_getJointName(handle, skeletonType, jointIndex, null,
                        out var stringLength);
                    if (success == Result.Success)
                    {
                        var nameBuffer = stackalloc byte[stringLength];
                        success = Api.metaMovementSDK_getJointName(handle, skeletonType, jointIndex, nameBuffer,
                            out stringLength);
                        if (success == Result.Success)
                        {
                            jointName = Marshal.PtrToStringAnsi((IntPtr)nameBuffer, stringLength).TrimEnd('\0');
                        }
                    }
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Get the index for the parent joint for each joint in the skeleton.
        /// This defines the hierarchical structure of the skeleton.
        /// Understanding the parent-child relationships is crucial for proper skeleton manipulation and retargeting.
        /// </summary>
        /// <param name="handle">The handle to get the info from.</param>
        /// <param name="skeletonType">The type of skeleton to get the info from.</param>
        /// <param name="jointIndexArray">Reference to an array that will be filled with parent joint indices.
        /// For each joint at index i, jointIndexArray[i] will contain the index of its parent joint.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetParentJointIndexesByRef(
            ulong handle,
            SkeletonType skeletonType,
            ref NativeArray<int> jointIndexArray)
        {
            Result success;
            using (new ProfilerScope(nameof(GetParentJointIndexesByRef)))
            {
                unsafe
                {
                    var jointIndexArrayLength = jointIndexArray.Length;
                    success = Api.metaMovementSDK_getParentJointIndexes(handle, skeletonType, jointIndexArray.GetPtr(),
                        out jointIndexArrayLength);
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Get the T-pose data for a skeleton.
        /// The T-pose is a reference pose used as a basis for retargeting and other operations.
        /// T-poses provide a standardized starting point for comparing and mapping between different skeletons.
        /// </summary>
        /// <param name="handle">The handle to get the T-pose from.</param>
        /// <param name="skeletonType">The type of skeleton to get the T-pose for.</param>
        /// <param name="tPoseType">The specific T-pose type to retrieve (current, min, max, or unscaled).</param>
        /// <param name="jointSpaceType">The coordinate space to express the transforms in.</param>
        /// <param name="transformArray">Reference to an array that will be filled with the T-pose transforms.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetSkeletonTPoseByRef(
            ulong handle,
            SkeletonType skeletonType,
            SkeletonTPoseType tPoseType,
            JointRelativeSpaceType jointSpaceType,
            ref NativeArray<NativeTransform> transformArray)
        {
            Result success;
            using (new ProfilerScope(nameof(GetSkeletonTPoseByRef)))
            {
                unsafe
                {
                    var transformArrayLength = transformArray.Length;
                    success = Api.metaMovementSDK_getSkeletonTPose(handle, skeletonType, tPoseType, jointSpaceType,
                        transformArray.GetPtr(), out transformArrayLength);
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Calculate a T-pose for a skeleton based on a blend factor between min and max T-poses.
        /// This allows for creating intermediate T-poses for different body proportions.
        /// The delta parameter controls the blend between minimum (0.0) and maximum (1.0) T-poses.
        /// </summary>
        /// <param name="handle">The handle to calculate the T-pose for.</param>
        /// <param name="skeletonType">The type of skeleton to calculate the T-pose for.</param>
        /// <param name="jointSpaceType">The coordinate space to express the transforms in.</param>
        /// <param name="delta">The blend factor between min (0.0) and max (1.0) T-poses.</param>
        /// <param name="transformArray">Reference to an array that will be filled with the calculated T-pose transforms.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool CalculateSkeletonTPoseByRef(
            ulong handle,
            SkeletonType skeletonType,
            JointRelativeSpaceType jointSpaceType,
            float delta,
            ref NativeArray<NativeTransform> transformArray)
        {
            Result success;
            using (new ProfilerScope(nameof(CalculateSkeletonTPoseByRef)))
            {
                unsafe
                {
                    var transformArrayLength = transformArray.Length;
                    success = Api.metaMovementSDK_calculateSkeletonTPose(handle, skeletonType, jointSpaceType, delta,
                        transformArray.GetPtr(), out transformArrayLength);
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Calculate a T-pose for a skeleton scaled to a specific height.
        /// This is useful for adapting a skeleton to match a target character's height.
        /// The height parameter specifies the desired height in meters for the resulting T-pose.
        /// </summary>
        /// <param name="handle">The handle to calculate the T-pose for.</param>
        /// <param name="skeletonType">The type of skeleton to calculate the T-pose for.</param>
        /// <param name="jointSpaceType">The coordinate space to express the transforms in.</param>
        /// <param name="height">The target height in meters.</param>
        /// <param name="transformArray">Reference to an array that will be filled with the calculated T-pose transforms.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool CalculateSkeletonTPoseAtHeightByRef(
            ulong handle,
            SkeletonType skeletonType,
            JointRelativeSpaceType jointSpaceType,
            float height,
            ref NativeArray<NativeTransform> transformArray)
        {
            Result success;
            using (new ProfilerScope(nameof(CalculateSkeletonTPoseAtHeightByRef)))
            {
                unsafe
                {
                    var transformArrayLength = transformArray.Length;
                    success = Api.metaMovementSDK_calculateSkeletonTPoseAtHeight(handle, skeletonType, jointSpaceType,
                        height,
                        transformArray.GetPtr(), out transformArrayLength);
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Get the joint mappings for a skeleton.
        /// Joint mappings define how joints in one skeleton correspond to joints in another skeleton.
        /// </summary>
        /// <param name="handle">The handle to get the mappings from.</param>
        /// <param name="tPoseType">The specific T-pose type to retrieve mappings for.</param>
        /// <param name="mappingsArray">Output array that receives the joint mappings.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetSkeletonMappings(
            ulong handle,
            SkeletonTPoseType tPoseType,
            out NativeArray<JointMapping> mappingsArray)
        {
            Result success;
            using (new ProfilerScope(nameof(GetSkeletonMappings)))
            {
                unsafe
                {
                    success = Api.metaMovementSDK_getSkeletonMappings(handle, tPoseType, null,
                        out var numMappings);
                    if (success == Result.Success && numMappings > 0)
                    {
                        mappingsArray = new NativeArray<JointMapping>(numMappings, Allocator.Temp,
                            NativeArrayOptions.UninitializedMemory);
                        success = Api.metaMovementSDK_getSkeletonMappings(handle, tPoseType,
                            mappingsArray.GetPtr(), out numMappings);
                    }
                    else
                    {
                        mappingsArray =
                            new NativeArray<JointMapping>(0, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                    }
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Get the list of target joint indices that are used in joint mappings.
        /// This identifies which joints in the target skeleton are affected by retargeting.
        /// </summary>
        /// <param name="handle">The handle to get the target joints from.</param>
        /// <param name="jointIndexList">Output array that receives the joint indices.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetSkeletonMappingTargetJoints(ulong handle, out NativeArray<int> jointIndexList)
        {
            Result success;
            using (new ProfilerScope(nameof(GetSkeletonMappingTargetJoints)))
            {
                unsafe
                {
                    int numTargetJoints = 0;
                    success = Api.metaMovementSDK_getSkeletonMappingTargetJoints(handle, null, out numTargetJoints);
                    if (success == Result.Success && numTargetJoints > 0)
                    {
                        jointIndexList = new NativeArray<int>(numTargetJoints, Allocator.Temp,
                            NativeArrayOptions.UninitializedMemory);
                        success = Api.metaMovementSDK_getSkeletonMappingTargetJoints(handle, jointIndexList.GetPtr(),
                            out numTargetJoints);
                    }
                    else
                    {
                        jointIndexList = new NativeArray<int>(0, Allocator.Temp,
                            NativeArrayOptions.UninitializedMemory);
                    }
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Get the index of a joint by its name.
        /// This allows for looking up a joint's index in the skeleton hierarchy when you know its name.
        /// </summary>
        /// <param name="handle">The handle to get the joint index from.</param>
        /// <param name="skeletonType">The type of skeleton to get the joint index from.</param>
        /// <param name="jointName">The name of the joint to find.</param>
        /// <param name="jointIndex">Output parameter that receives the joint index.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetJointIndex(
            ulong handle,
            SkeletonType skeletonType,
            string jointName,
            out int jointIndex)
        {
            Result result;
            using (new ProfilerScope(nameof(GetJointIndex)))
            {
                result = Api.metaMovementSDK_getJointIndex(handle, skeletonType, jointName, out jointIndex);
            }

            return result == Result.Success;
        }

        /// <summary>
        /// Get the index of a joint by its known joint type.
        /// This allows for looking up common joints (like hips, neck, etc.) without knowing their specific names.
        /// </summary>
        /// <param name="handle">The handle to get the joint index from.</param>
        /// <param name="skeletonType">The type of skeleton to get the joint index from.</param>
        /// <param name="knownJointType">The known joint type to find.</param>
        /// <param name="jointIndex">Output parameter that receives the joint index.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetJointIndexByKnownJointType(
            ulong handle,
            SkeletonType skeletonType,
            KnownJointType knownJointType,
            out int jointIndex)
        {
            Result success;
            using (new ProfilerScope(nameof(GetParentJointIndex)))
            {
                success = Api.metaMovementSDK_getJointIndexByKnownJointType(handle, skeletonType, knownJointType,
                    out jointIndex);
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Get the mapping of known joint types to their corresponding joint indices in the skeleton.
        /// This provides the defined/configured mapping from the skeleton's configuration data.
        /// Returns a KnownJointIndexData structure that maps each KnownJointType to its actual joint index.
        /// Joints that are not present will have an index value of INVALID_JOINT_INDEX (-1).
        /// </summary>
        /// <param name="handle">The handle to get the known joint indices from.</param>
        /// <param name="skeletonType">The type of skeleton to get the known joint indices for.</param>
        /// <param name="knownJoints">Output parameter that receives the known joint index data.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetKnownJointIndexes(
            ulong handle,
            SkeletonType skeletonType,
            out KnownJointIndexData knownJoints)
        {
            Result success;
            using (new ProfilerScope(nameof(GetKnownJointIndexes)))
            {
                success = Api.metaMovementSDK_getKnownJointIndexes(handle, skeletonType, out knownJoints);
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Get the index of a joint's parent.
        /// This allows for traversing up the skeleton hierarchy from a specific joint.
        /// </summary>
        /// <param name="handle">The handle to get the parent joint index from.</param>
        /// <param name="skeletonType">The type of skeleton to get the parent joint index from.</param>
        /// <param name="jointIndex">The index of the joint to find the parent for.</param>
        /// <param name="parentJointIndex">Output parameter that receives the parent joint index.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetParentJointIndex(
            ulong handle,
            SkeletonType skeletonType,
            int jointIndex,
            out int parentJointIndex)
        {
            Result success;
            using (new ProfilerScope(nameof(GetParentJointIndex)))
            {
                success = Api.metaMovementSDK_getParentJointIndex(handle, skeletonType, jointIndex,
                    out parentJointIndex);
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Get the humanoid limb type for a joint by its name.
        /// This allows you to determine which anatomical limb a joint belongs to (e.g., left arm, right leg, etc.)
        /// based on the joint's name.
        /// </summary>
        /// <param name="handle">The handle to get the limb type from.</param>
        /// <param name="skeletonType">The type of skeleton to get the limb type from.</param>
        /// <param name="jointName">The name of the joint to find the limb type for.</param>
        /// <param name="humanoidLimbType">Output parameter that receives the humanoid limb type.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetHumanoidLimbTypeFromJointName(
            ulong handle,
            SkeletonType skeletonType,
            string jointName,
            out HumanoidLimbType humanoidLimbType)
        {
            Result success;
            using (new ProfilerScope(nameof(GetHumanoidLimbTypeFromJointName)))
            {
                success = Api.metaMovementSDK_getHumanoidLimbTypeFromJointName(handle, skeletonType, jointName,
                    out humanoidLimbType);
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Get the humanoid limb type for a joint by its index.
        /// This allows you to determine which anatomical limb a joint belongs to (e.g., left arm, right leg, etc.)
        /// based on the joint's index in the skeleton.
        /// </summary>
        /// <param name="handle">The handle to get the limb type from.</param>
        /// <param name="skeletonType">The type of skeleton to get the limb type from.</param>
        /// <param name="jointIndex">The index of the joint to find the limb type for.</param>
        /// <param name="humanoidLimbType">Output parameter that receives the humanoid limb type.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetHumanoidLimbTypeFromJointIndex(
            ulong handle,
            SkeletonType skeletonType,
            int jointIndex,
            out HumanoidLimbType humanoidLimbType)
        {
            Result success;
            using (new ProfilerScope(nameof(GetHumanoidLimbTypeFromJointIndex)))
            {
                success = Api.metaMovementSDK_getHumanoidLimbTypeFromJointIndex(handle, skeletonType, jointIndex,
                    out humanoidLimbType);
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Get all joint indices that belong to a specific humanoid limb.
        /// This returns an array of joint indices for all joints that are part of the specified anatomical limb
        /// (e.g., all joints in the left arm, right leg, etc.).
        /// </summary>
        /// <param name="handle">The handle to get the joint indices from.</param>
        /// <param name="skeletonType">The type of skeleton to get the joint indices from.</param>
        /// <param name="humanoidLimbType">The humanoid limb type to get joint indices for.</param>
        /// <param name="jointIndexList">Output array that receives the joint indices in the humanoid limb.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetJointIndexesInHumanoidLimb(
            ulong handle,
            SkeletonType skeletonType,
            HumanoidLimbType humanoidLimbType,
            out NativeArray<int> jointIndexList)
        {
            Result success;
            using (new ProfilerScope(nameof(GetJointIndexesInHumanoidLimb)))
            {
                int jointCount = 0;
                unsafe
                {
                    success = Api.metaMovementSDK_getJointIndexesInHumanoidLimb(
                        handle, skeletonType, humanoidLimbType, null, out jointCount);
                }

                if (success != Result.Success || jointCount <= 0)
                {
                    jointIndexList = default;
                    return false;
                }

                jointIndexList = new NativeArray<int>(jointCount, Allocator.Persistent);
                unsafe
                {
                    success = Api.metaMovementSDK_getJointIndexesInHumanoidLimb(
                        handle, skeletonType, humanoidLimbType,
                        (int*)jointIndexList.GetUnsafePtr(), out jointCount);
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Get the names of all manifestations for a skeleton type.
        /// Manifestations are subsets of a skeleton that can be used for specific purposes,
        /// such as only retargeting the upper body or only the hands.
        /// </summary>
        /// <param name="handle">The handle to get the manifestations from.</param>
        /// <param name="skeletonType">The type of skeleton to get the manifestation names from.</param>
        /// <param name="manifestationNames">Output array that receives the manifestation names.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetManifestationNames(
            ulong handle,
            SkeletonType skeletonType,
            out string[] manifestationNames)
        {
            Result success;
            manifestationNames = Array.Empty<string>();
            using (new ProfilerScope(nameof(GetManifestationNames)))
            {
                unsafe
                {
                    success = Api.metaMovementSDK_getManifestationNames(handle, skeletonType, null, out var bufferSize,
                        null,
                        out var nameCount);
                    if (success == Result.Success)
                    {
                        var buffer = new byte[bufferSize];
                        Span<byte> nameBuffer = buffer;
                        fixed (byte* bytes = &nameBuffer.GetPinnableReference())
                        {
                            success = Api.metaMovementSDK_getManifestationNames(handle, skeletonType, bytes,
                                out bufferSize,
                                null, out nameCount);
                            if (success == Result.Success)
                            {
                                ConvertByteBufferToStringArray(bytes, bufferSize, nameCount, out manifestationNames);
                            }
                        }
                    }
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Get the joint indexes of all joints in a manifestation for a skeleton type.
        /// This identifies which joints are included in a specific manifestation subset.
        /// </summary>
        /// <param name="handle">The handle to get the joints from.</param>
        /// <param name="skeletonType">The type of skeleton to get the joint indices from.</param>
        /// <param name="manifestationName">The name of the manifestation to get joints for.</param>
        /// <param name="jointIndexList">Output array that receives the joint indices in the manifestation.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetJointsInManifestation(
            ulong handle,
            SkeletonType skeletonType,
            string manifestationName,
            out NativeArray<int> jointIndexList)
        {
            Result success;
            using (new ProfilerScope(nameof(GetJointsInManifestation)))
            {
                unsafe
                {
                    success = Api.metaMovementSDK_getJointsInManifestation(
                        handle, skeletonType, manifestationName, null, out var jointIndexListSize);
                    if (success == Result.Success && jointIndexListSize > 0)
                    {
                        jointIndexList = new NativeArray<int>(jointIndexListSize, Allocator.Temp,
                            NativeArrayOptions.UninitializedMemory);
                        success = Api.metaMovementSDK_getJointsInManifestation(
                            handle, skeletonType, manifestationName, jointIndexList.GetPtr(),
                            out jointIndexListSize);
                    }
                    else
                    {
                        jointIndexList = new NativeArray<int>(0, Allocator.Temp,
                            NativeArrayOptions.UninitializedMemory);
                    }
                }
            }

            return success == Result.Success;
        }

        /**********************************************************
         *
         *               Retargeting Functions
         *
         **********************************************************/

        /// <summary>
        /// Updates the source reference T-Pose to use for Retargeting.
        /// This allows for customizing the reference pose used as the basis for retargeting operations.
        /// </summary>
        /// <param name="handle">The handle to update the reference T-pose for.</param>
        /// <param name="sourceTPose">An array of transforms composing a T-Pose for the source skeleton.</param>
        /// <param name="manifestation">Empty string or the name of a valid manifestation in the source skeleton.
        /// If specified, only updates the T-pose for joints in that manifestation.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool UpdateSourceReferenceTPose(
            ulong handle,
            NativeArray<NativeTransform> sourceTPose,
            string manifestation = null)
        {
            Result success;
            using (new ProfilerScope(nameof(UpdateSourceReferenceTPose)))
            {
                unsafe
                {
                    success = Api.metaMovementSDK_updateSourceReferenceTPose(
                        handle,
                        sourceTPose.GetPtr(),
                        sourceTPose.Length,
                        manifestation);
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Retargets a pose from the source skeleton to the target skeleton.
        /// This is the core function for transferring motion from one skeleton to another with different proportions.
        /// </summary>
        /// <param name="handle">The handle to use for retargeting.</param>
        /// <param name="retargetingBehaviorInfo">A structure of settings that control how retargeting is performed.</param>
        /// <param name="sourceFramePose">Array of transforms representing the current pose of the source skeleton.</param>
        /// <param name="targetRetargetedPose">Reference to an array that will receive the retargeted pose for the target skeleton.</param>
        /// <param name="sourceManifestation">Optional name of a manifestation in the source skeleton to retarget from.
        /// If null or empty, the entire skeleton is used.</param>
        /// <param name="targetManifestation">Optional name of a manifestation in the target skeleton to retarget to.
        /// If null or empty, the entire skeleton is used.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool RetargetFromSourceFrameData(
            ulong handle,
            RetargetingBehaviorInfo retargetingBehaviorInfo,
            NativeArray<NativeTransform> sourceFramePose,
            ref NativeArray<NativeTransform> targetRetargetedPose,
            string sourceManifestation = null,
            string targetManifestation = null)
        {
            Result success = Result.Success;
            using (new ProfilerScope(nameof(RetargetFromSourceFrameData)))
            {
                unsafe
                {
                    int numSourceJoints = sourceFramePose.Length;
                    // If we have a manifestion, update our length to match
                    // our manifestation joint count.
                    if (sourceManifestation != null)
                    {
                        success = Api.metaMovementSDK_getJointsInManifestation(
                            handle, SkeletonType.SourceSkeleton, sourceManifestation, null, out numSourceJoints);

                        if (success == Result.Success && numSourceJoints > sourceFramePose.Length)
                        {
                            success = Result.Failure;
                        }
                    }

                    if (success == Result.Success)
                    {
                        var numTargetJoints = targetRetargetedPose.Length;
                        success = Api.metaMovementSDK_retargetFromSourceFrameData(
                            handle,
                            retargetingBehaviorInfo,
                            sourceFramePose.GetPtr(),
                            numSourceJoints,
                            targetRetargetedPose.GetPtr(),
                            ref numTargetJoints,
                            sourceManifestation,
                            targetManifestation);
                    }
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Gets the last processed frame pose for a skeleton.
        /// This retrieves the most recent pose that was processed by the retargeting system.
        /// </summary>
        /// <param name="handle">The handle to get the pose from.</param>
        /// <param name="skeletonType">The type of skeleton to get the pose for.</param>
        /// <param name="jointSpaceType">The coordinate space to express the transforms in.</param>
        /// <param name="transformArray">Reference to an array that will be filled with the pose transforms.</param>
        /// <param name="manifestation">Optional name of a manifestation to get the pose for.
        /// If null or empty, the entire skeleton pose is returned.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetLastProcessedFramePose(
            ulong handle,
            SkeletonType skeletonType,
            JointRelativeSpaceType jointSpaceType,
            ref NativeArray<NativeTransform> transformArray,
            string manifestation = null)
        {
            Result success = Result.Failure;
            using (new ProfilerScope(nameof(GetLastProcessedFramePose)))
            {
                unsafe
                {
                    var transformArrayLength = transformArray.Length;
                    success = Api.metaMovementSDK_getLastProcessedFramePose(
                        handle,
                        skeletonType,
                        jointSpaceType,
                        transformArray.GetPtr(),
                        out transformArrayLength,
                        manifestation);
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Captures the source and target poses returned from GetLastProcessedFramePose and creates a new
        /// MSDK Utility Config JSON file with the poses stored in the "cachedPose" JSON section of source/target
        /// </summary>
        /// <param name="handle">The handle to get the mapping data from.</param>
        /// <param name="captureHandle">A different handle variable to store the new created handle with the pose</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool CaptureLastProcessedFramePoseToConfig(
            ulong handle,
            out ulong captureHandle)
        {
            Result success = Result.Failure;
            using (new ProfilerScope(nameof(CaptureLastProcessedFramePoseToConfig)))
            {
                success = Api.metaMovementSDK_captureLastProcessedFramePoseToConfig(handle, out captureHandle);
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Gets the mapping data used in the last retargeting operation for a specific target joint.
        /// This provides detailed information about how a target joint was influenced by source joints.
        /// </summary>
        /// <param name="handle">The handle to get the mapping data from.</param>
        /// <param name="targetJointIndex">The index of the target joint to get mapping data for.</param>
        /// <param name="sourceSkeletonType">Output parameter that receives the source skeleton type.</param>
        /// <param name="tPoseBlendedTransform">Output parameter that receives the blended T-pose transform.</param>
        /// <param name="lastPoseBlendedTransform">Output parameter that receives the blended last pose transform.</param>
        /// <param name="sourceJointIndexList">Output array that receives the indices of source joints that influenced this target joint.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetLastRetargetedMappingData(
            ulong handle,
            int targetJointIndex,
            out SkeletonType sourceSkeletonType,
            out NativeTransform tPoseBlendedTransform,
            out NativeTransform lastPoseBlendedTransform,
            out NativeArray<int> sourceJointIndexList)
        {
            Result success;
            using (new ProfilerScope(nameof(GetLastRetargetedMappingData)))
            {
                unsafe
                {
                    success = Api.metaMovementSDK_getLastRetargetedMappingData(
                        handle, targetJointIndex, out sourceSkeletonType, out tPoseBlendedTransform,
                        out lastPoseBlendedTransform, null, out var sourceJointIndexListSize);
                    if (success == Result.Success && sourceJointIndexListSize > 0)
                    {
                        sourceJointIndexList =
                            new NativeArray<int>(sourceJointIndexListSize, Allocator.Temp,
                                NativeArrayOptions.UninitializedMemory);
                        success = Api.metaMovementSDK_getLastRetargetedMappingData(handle, targetJointIndex,
                            out sourceSkeletonType, out tPoseBlendedTransform, out lastPoseBlendedTransform,
                            sourceJointIndexList.GetPtr(), out sourceJointIndexListSize);
                    }
                    else
                    {
                        sourceJointIndexList =
                            new NativeArray<int>(0, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                    }
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Matches a pose to the current T-pose of a skeleton.
        /// This adjusts the provided pose to match the scale or orientation of the current T-pose,
        /// depending on the specified match behavior.
        /// </summary>
        /// <param name="handle">The handle to use for matching.</param>
        /// <param name="skeletonType">The type of skeleton to match against.</param>
        /// <param name="matchBehavior">Controls whether to match scale or orientation.</param>
        /// <param name="jointSpaceType">The coordinate space to express the transforms in.</param>
        /// <param name="inOutTransformData">Reference to an array containing the pose to match, which will be updated with the matched pose.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool MatchCurrentTPose(
            ulong handle,
            SkeletonType skeletonType,
            MatchPoseBehavior matchBehavior,
            JointRelativeSpaceType jointSpaceType,
            ref NativeArray<NativeTransform> inOutTransformData)
        {
            Result success;
            using (new ProfilerScope(nameof(MatchCurrentTPose)))
            {
                unsafe
                {
                    success = Api.metaMovementSDK_matchCurrentTPose(
                        handle,
                        skeletonType,
                        matchBehavior,
                        jointSpaceType,
                        inOutTransformData.Length,
                        inOutTransformData.GetPtr());
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Matches one pose to another pose.
        /// This adjusts the target pose to match the scale or orientation of the source pose,
        /// depending on the specified match behavior.
        /// </summary>
        /// <param name="handle">The handle to use for matching.</param>
        /// <param name="skeletonType">The type of skeleton the poses belong to.</param>
        /// <param name="matchBehavior">Controls whether to match scale or orientation.</param>
        /// <param name="jointSpaceType">The coordinate space to express the transforms in.</param>
        /// <param name="inTransformSourceData">The source pose to match against.</param>
        /// <param name="inOutTransformMutableData">Reference to an array containing the pose to be modified, which will be updated with the matched pose.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool MatchPose(
            ulong handle,
            SkeletonType skeletonType,
            MatchPoseBehavior matchBehavior,
            JointRelativeSpaceType jointSpaceType,
            ref NativeArray<NativeTransform> inTransformSourceData,
            ref NativeArray<NativeTransform> inOutTransformMutableData)
        {
            Result success;
            using (new ProfilerScope(nameof(MatchPose)))
            {
                unsafe
                {
                    Assert.IsTrue(inTransformSourceData.Length == inOutTransformMutableData.Length);
                    success = Api.metaMovementSDK_matchPose(
                        handle,
                        skeletonType,
                        matchBehavior,
                        jointSpaceType,
                        inTransformSourceData.Length,
                        inTransformSourceData.GetPtr(),
                        inOutTransformMutableData.GetPtr());
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Scales a pose to a specific height.
        /// This is useful for adapting a pose to match a target character's height.
        /// </summary>
        /// <param name="handle">The handle to use for scaling.</param>
        /// <param name="skeletonType">The type of skeleton the pose belongs to.</param>
        /// <param name="jointSpaceType">The coordinate space to express the transforms in.</param>
        /// <param name="height">The target height in meters.</param>
        /// <param name="inOutTransformData">Reference to an array containing the pose to be scaled, which will be updated with the scaled pose.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool ScalePoseToHeight(
            ulong handle,
            SkeletonType skeletonType,
            JointRelativeSpaceType jointSpaceType,
            float height,
            ref NativeArray<NativeTransform> inOutTransformData)
        {
            Result success;
            using (new ProfilerScope(nameof(ScalePoseToHeight)))
            {
                unsafe
                {
                    success = Api.metaMovementSDK_scalePoseToHeight(
                        handle,
                        skeletonType,
                        jointSpaceType,
                        height,
                        inOutTransformData.Length,
                        inOutTransformData.GetPtr());
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Aligns the target skeleton to the source skeleton
        /// </summary>
        /// <param name="configName">The name of the new config to be created</param>
        /// <param name="alignmentBehavior">Flags used to instruct operations to process.</param>
        /// <param name="sourceConfigHandle">The config handle with the source skeleton definition</param>
        /// <param name="sourceSkeletonType">Skeleton Type to pull the source skeleton from in the sourceConfigHandle data</param>
        /// <param name="targetConfigHandle">The config handle with the source skeleton definition (Assumed to be the Target skeleton)</param>
        /// <param name="handle">The out parameter used to communicate the new handle with the source skeleton aligned to the target skeleton.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool AlignTargetToSource(
            string configName,
            AlignmentFlags alignmentBehavior,
            ulong sourceConfigHandle,
            SkeletonType sourceSkeletonType,
            ulong targetConfigHandle,
            out ulong handle)
        {
            Result success;
            using (new ProfilerScope(nameof(AlignTargetToSource)))
            {
                success = Api.metaMovementSDK_alignTargetToSource(
                    configName,
                    alignmentBehavior,
                    sourceConfigHandle,
                    sourceSkeletonType,
                    targetConfigHandle,
                    out handle);
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Aligns the input skeleton to a source skeleton
        /// </summary>
        /// <param name="configName">The name of the new config to be created</param>
        /// <param name="alignmentBehavior">Flags used to instruct operations to process.</param>
        /// <param name="inputSkeleton">The input skeleton to align to the source skeleton</param>
        /// <param name="sourceConfigHandle">The config handle with the source skeleton definition</param>
        /// <param name="sourceSkeletonType">Skeleton Type to pull the source skeleton from in the sourceConfigHandle data</param>
        /// <param name="targetConfigHandle">The config handle with the source skeleton definition (Assumed to be the Target skeleton)</param>
        /// <param name="handle">The out parameter used to communicate the new handle with the source skeleton aligned to the target skeleton.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool AlignInputToSource(
            string configName,
            AlignmentFlags alignmentBehavior,
            NativeArray<NativeTransform> inputSkeleton,
            ulong sourceConfigHandle,
            SkeletonType sourceSkeletonType,
            ulong targetConfigHandle,
            out ulong handle)
        {
            Result success;
            using (new ProfilerScope(nameof(AlignTargetToSource)))
            {
                unsafe
                {
                    success = Api.metaMovementSDK_alignInputToSource(
                        configName,
                        alignmentBehavior,
                        inputSkeleton.GetPtr(),
                        inputSkeleton.Length,
                        sourceConfigHandle,
                        sourceSkeletonType,
                        targetConfigHandle,
                        out handle);
                }
            }

            bool wasSuccesful = success == Result.Success;
            const string ALIGN_ERROR_MESSAGE = "Failed to align input to source skeleton.";
            TelemetryManager.SendConfigEvent(TelemetryManager._ALIGN_TARGET_TO_SOURCE_EVENT_NAME,
                wasSuccesful ? null : ALIGN_ERROR_MESSAGE);
            return wasSuccesful;
        }

        /// <summary>
        /// Generates skeleton mappings automatically based on skeleton joint names and structure.
        /// This function uses intelligent name matching and bone hierarchy analysis to automatically
        /// create appropriate mappings between source and target skeletons, including twist joints
        /// and constraint behaviors.
        /// </summary>
        /// <param name="handle">The handle to generate mappings for.</param>
        /// <param name="autoMappingBehavior">Flags used to tell the automapper how to process the request.</param>
        /// <param name="additionalAutoMappingJointData">Optional list of additional joint-specific mapping data. If null or empty, default behavior is used.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GenerateMappings(
            ulong handle,
            AutoMappingFlags autoMappingBehavior,
            AutoMappingJointData[] additionalAutoMappingJointData = null)
        {
            Result success;
            using (new ProfilerScope(nameof(GenerateMappings)))
            {
                unsafe
                {
                    if (additionalAutoMappingJointData == null || additionalAutoMappingJointData.Length == 0)
                    {
                        // No additional data, pass null
                        success = Api.metaMovementSDK_generateMappings(
                            handle,
                            autoMappingBehavior,
                            null,
                            0);
                    }
                    else
                    {
                        // Convert managed array to unmanaged array
                        AutoMappingJointDataUnmanaged* unmanagedData = stackalloc AutoMappingJointDataUnmanaged[additionalAutoMappingJointData.Length];
                        for (int i = 0; i < additionalAutoMappingJointData.Length; i++)
                        {
                            unmanagedData[i].JointName = Marshal.StringToHGlobalAnsi(additionalAutoMappingJointData[i].JointName);
                            unmanagedData[i].Flags = additionalAutoMappingJointData[i].Flags;
                        }

                        try
                        {
                            success = Api.metaMovementSDK_generateMappings(
                                handle,
                                autoMappingBehavior,
                                unmanagedData,
                                additionalAutoMappingJointData.Length);
                        }
                        finally
                        {
                            // Free allocated string memory
                            for (int i = 0; i < additionalAutoMappingJointData.Length; i++)
                            {
                                if (unmanagedData[i].JointName != IntPtr.Zero)
                                {
                                    Marshal.FreeHGlobal(unmanagedData[i].JointName);
                                }
                            }
                        }
                    }
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Gets information about a specific pose.
        /// This retrieves metadata about the pose, including its coordinate space, extents, and known joint positions.
        /// </summary>
        /// <param name="handle">The handle to get the pose info from.</param>
        /// <param name="skeletonType">The type of skeleton the pose belongs to.</param>
        /// <param name="poseType">The specific pose type to get info for.</param>
        /// <param name="poseInfo">Output parameter that receives the pose information.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetPoseInfo(
            ulong handle,
            SkeletonType skeletonType,
            SkeletonTPoseType poseType,
            out PoseInfo poseInfo)
        {
            Result success;
            using (new ProfilerScope(nameof(GetPoseInfo)))
            {
                unsafe
                {
                    success = Api.metaMovementSDK_getPoseInfo(handle, skeletonType, poseType, out poseInfo);
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Gets the distance between two joints in a pose.
        /// This calculates the direct linear distance between the specified joints.
        /// </summary>
        /// <param name="handle">The handle to get the joint length from.</param>
        /// <param name="skeletonType">The type of skeleton the joints belong to.</param>
        /// <param name="poseType">The specific pose type to measure in.</param>
        /// <param name="startJointIndex">The index of the starting joint.</param>
        /// <param name="endJointIndex">The index of the ending joint.</param>
        /// <param name="length">Output parameter that receives the distance between the joints in meters.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetLengthBetweenJoints(
            ulong handle,
            SkeletonType skeletonType,
            SkeletonTPoseType poseType,
            int startJointIndex,
            int endJointIndex,
            out float length)
        {
            Result success;
            using (new ProfilerScope(nameof(GetLengthBetweenJoints)))
            {
                unsafe
                {
                    success = Api.metaMovementSDK_getLengthBetweenJoints(handle, skeletonType, poseType,
                        startJointIndex, endJointIndex, out length);
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
        /// Get the serialization settings.
        /// </summary>
        /// <param name="handle">The handle to get the configuration info.</param>
        /// <param name="outSerializationSettings">The serialization settings.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetSerializationSettings(ulong handle, out SerializationSettings outSerializationSettings)
        {
            Result success;
            using (new ProfilerScope(nameof(GetSerializationSettings)))
            {
                success = Api.metaMovementSDK_getSerializationSettings(handle, out outSerializationSettings);
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Update the serialization settings.
        /// </summary>
        /// <param name="handle">The handle to update the configuration info.</param>
        /// <param name="inMutableSettings">The serialization settings to update.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool SetSerializationSettings(ulong handle, SerializationSettings inMutableSettings)
        {
            Result success;
            using (new ProfilerScope(nameof(SetSerializationSettings)))
            {
                success = Api.metaMovementSDK_setSerializationSettings(handle, inMutableSettings);
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Serializes body and face pose data into a compact byte array.
        /// This creates a snapshot of the current pose that can be stored or transmitted over a network.
        /// The serialization applies compression based on the current serialization settings.
        /// </summary>
        /// <param name="handle">The handle to use for serialization.</param>
        /// <param name="timestamp">The timestamp to associate with this snapshot, in seconds.</param>
        /// <param name="bodyPose">The body pose transforms to be serialized.</param>
        /// <param name="facePose">The face pose blendshape weights to be serialized normalized.</param>
        /// <param name="ack">The acknowledgement number for the data, used for synchronization.</param>
        /// <param name="bodyIndicesToSerialize">The indices of the joints in the body pose that should be serialized.</param>
        /// <param name="faceIndicesToSerialize">The indices of the blendshapes in the face pose that should be serialized.</param>
        /// <param name="output">Reference to a byte array that will be created and filled with the serialized data.</param>
        /// <returns>True if serialization was successful.</returns>
        public static bool SerializeSkeletonAndFace(
            ulong handle,
            float timestamp,
            NativeArray<NativeTransform> bodyPose,
            NativeArray<float> facePose,
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

                SnapshotData snapshotData = new SnapshotData();
                snapshotData.BaselineAck = ack;
                snapshotData.Timestamp = timestamp;

                snapshotData.TargetSkeletonPose = bodyPose;
                snapshotData.TargetSkeletonIndices = bodyIndices;

                snapshotData.FacePose = facePose;
                snapshotData.FaceIndices = faceIndices;

                // Unity coordinate system: Y-up, Z-forward (positive), X-right
                snapshotData.RecordingCoordinateSpaceSource = new CoordinateSpace(
                    up: new Vector3(0.0f, 1.0f, 0.0f),
                    forward: new Vector3(0.0f, 0.0f, 1.0f),
                    right: new Vector3(1.0f, 0.0f, 0.0f),
                    metersToUnitScale: 1.0f);

                if (!BuildSnapshot(handle, snapshotData))
                {
                    Debug.LogError("Could not build snapshot before serialization!");
                    return false;
                }

                Result success;
                unsafe
                {
                    success = Api.metaMovementSDK_serializeSnapshot(handle, null, out var bytes);
                    if (success == Result.Success)
                    {
                        output = new NativeArray<byte>(bytes, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                        success = Api.metaMovementSDK_serializeSnapshot(handle, output.GetPtr(), out bytes);
                    }
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
        /// Creates a snapshot and builds the data associated with it.
        /// </summary>
        /// <param name="handle">The handle associated with serialization.</param>
        /// <param name="snapshotData">Struct containing all possible snapshot data.</param>
        /// <returns>True if call was successful; false if not.</returns>
        public static bool BuildSnapshot(
            ulong handle,
            SnapshotData snapshotData)
        {
            Result success;
            using (new ProfilerScope(nameof(BuildSnapshot)))
            {
                unsafe
                {
                    SnapshotDataUnmanaged snapshotDataUnmanaged = new SnapshotDataUnmanaged(snapshotData);
                    success = Api.metaMovementSDK_buildSnapshot(
                        handle,
                        ref snapshotDataUnmanaged);
                }
            }

            if (success != Result.Success)
            {
                Debug.LogError("Could not build snapshot!");
            }
            return success == Result.Success;
        }

        /// <summary>
        /// Serializes a body skeleton into a snapshot byte array.
        /// This is a simplified version of SerializeSkeletonAndFace that only handles body pose data.
        /// Useful when you don't need to serialize facial expressions.
        /// </summary>
        /// <param name="handle">The handle associated with the serialization.</param>
        /// <param name="timestamp">The timestamp to associate with this snapshot, in seconds.</param>
        /// <param name="ack">The acknowledgment index of the data, used for synchronization.</param>
        /// <param name="bodyTrackingPose">The body pose transforms to serialize.</param>
        /// <param name="bodyTrackingIndices">The indices of the joints in the body pose that should be serialized.</param>
        /// <param name="output">Reference to a byte array that will be created and filled with the serialized data.</param>
        /// <returns>True if serialization was successful.</returns>
        public static bool SerializeBodySkeleton(
            ulong handle,
            float timestamp,
            int ack,
            NativeArray<NativeTransform> bodyTrackingPose,
            NativeArray<int> bodyTrackingIndices,
            ref NativeArray<byte> output)
        {
            using (new ProfilerScope(nameof(SerializeBodySkeleton)))
            {
                SnapshotData snapshotData = new SnapshotData();
                snapshotData.BaselineAck = ack;
                snapshotData.Timestamp = timestamp;

                snapshotData.SourceSkeletonPose = bodyTrackingPose;
                snapshotData.SourceSkeletonIndices = bodyTrackingIndices;

                if (!BuildSnapshot(handle, snapshotData))
                {
                    Debug.LogError("Could not build snapshot before serialization!");
                    return false;
                }

                Result success;
                unsafe
                {
                    success = Api.metaMovementSDK_serializeSnapshot(handle, null, out var bytes);
                    if (success == Result.Success)
                    {
                        output = new NativeArray<byte>(bytes, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                        success = Api.metaMovementSDK_serializeSnapshot(handle, output.GetPtr(), out bytes);
                    }
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
        /// This converts a serialized snapshot back into usable pose data for animation.
        /// </summary>
        /// <param name="handle">The handle to use for deserialization.</param>
        /// <param name="data">The serialized data to be deserialized.</param>
        /// <param name="timestamp">Output parameter that receives the timestamp of the snapshot.</param>
        /// <param name="compressionType">Output parameter that receives the compression type used in the snapshot.</param>
        /// <param name="ack">Output parameter that receives the acknowledgement number from the snapshot.</param>
        /// <param name="outputBodyPose">Reference to an array that will be filled with the deserialized body pose transforms.</param>
        /// <param name="outputFacePose">Reference to an array that will be filled with the deserialized face pose blendshape weights.</param>
        /// <returns>True if deserialization was successful.</returns>
        public static bool DeserializeSkeletonAndFace(
            ulong handle,
            NativeArray<byte> data,
            double dataVersion,
            out double timestamp,
            out SerializationCompressionType compressionType,
            out int ack,
            ref NativeArray<NativeTransform> outputBodyPose,
            ref NativeArray<float> outputFacePose)
        {
            Result success;
            using (new ProfilerScope(nameof(DeserializeSkeletonAndFace)))
            {
                unsafe
                {
                    DeserializedSnapshotDataUnmanaged snapshotDataUnmanaged = new DeserializedSnapshotDataUnmanaged();
                    snapshotDataUnmanaged.SourceSkeletonPose = null;
                    snapshotDataUnmanaged.TargetSkeletonPose = outputBodyPose.GetPtr();
                    snapshotDataUnmanaged.FacePose = outputFacePose.GetPtr();
                    snapshotDataUnmanaged.BindPose = null;

                    success = Api.metaMovementSDK_deserializeSnapshotData(handle, data.GetPtr(), dataVersion,
                        ref snapshotDataUnmanaged);
                    timestamp = snapshotDataUnmanaged.Timestamp;
                    compressionType = snapshotDataUnmanaged.Compression;
                    ack = snapshotDataUnmanaged.Ack;
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Deserializes data into body and face pose data with additional tracking information.
        /// This extended version also extracts body tracking pose and frame metadata from the snapshot.
        /// </summary>
        /// <param name="handle">The handle to use for deserialization.</param>
        /// <param name="data">The serialized data to be deserialized.</param>
        /// <param name="timestamp">Output parameter that receives the timestamp of the snapshot.</param>
        /// <param name="compressionType">Output parameter that receives the compression type used in the snapshot.</param>
        /// <param name="ack">Output parameter that receives the acknowledgement number from the snapshot.</param>
        /// <param name="outputBodyPose">Reference to an array that will be filled with the deserialized body pose transforms.</param>
        /// <param name="outputFacePose">Reference to an array that will be filled with the deserialized face pose blendshape weights.</param>
        /// <param name="outputBodyTrackingPose">Reference to an array that will be filled with the deserialized body tracking pose transforms.</param>
        /// <param name="frameData">Reference to a FrameData structure that will be filled with metadata about the frame.</param>
        /// <param name="outBindPose">Output bind pose.</param>
        /// <param name="outBindPoseCount">Output bind pose count.</param>
        /// <param name="coordinateSpaceSource">Coordinate space (source).</param>
        /// <returns>True if deserialization was successful.</returns>
        public static bool DeserializeSkeletonAndFace(
            ulong handle,
            NativeArray<byte> data,
            double dataVersion,
            out double timestamp,
            out SerializationCompressionType compressionType,
            out int ack,
            ref NativeArray<NativeTransform> outputBodyPose,
            ref NativeArray<float> outputFacePose,
            ref NativeArray<NativeTransform> outputBodyTrackingPose,
            ref FrameData frameData,
            ref NativeArray<NativeTransform> outBindPose,
            out int outBindPoseCount,
            out CoordinateSpace coordinateSpaceSource)
        {
            Result success;
            using (new ProfilerScope(nameof(DeserializeSkeletonAndFace)))
            {
                unsafe
                {
                    DeserializedSnapshotDataUnmanaged snapshotDataUnmanaged = new DeserializedSnapshotDataUnmanaged();
                    snapshotDataUnmanaged.TargetSkeletonPose = outputBodyPose.GetPtr();
                    snapshotDataUnmanaged.FacePose = outputFacePose.GetPtr();
                    snapshotDataUnmanaged.SourceSkeletonPose = outputBodyTrackingPose.GetPtr();
                    snapshotDataUnmanaged.BindPose = outBindPose.GetPtr();

                    success = Api.metaMovementSDK_deserializeSnapshotData(handle, data.GetPtr(), dataVersion,
                        ref snapshotDataUnmanaged);
                    timestamp = snapshotDataUnmanaged.Timestamp;
                    compressionType = snapshotDataUnmanaged.Compression;
                    ack = snapshotDataUnmanaged.Ack;
                    frameData = snapshotDataUnmanaged.FrameData;
                    outBindPoseCount = snapshotDataUnmanaged.NumBindPoseJoints;
                    coordinateSpaceSource = snapshotDataUnmanaged.CoordinateSpaceSource;
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Get the interpolated body pose at a specific time.
        /// This is used for smooth playback of serialized animation data by interpolating between snapshots.
        /// </summary>
        /// <param name="handle">The handle to get the body pose from.</param>
        /// <param name="skeletonType">The type of skeleton to get the pose for.</param>
        /// <param name="interpolatedBodyPose">Reference to an array that will be filled with the interpolated body pose transforms.</param>
        /// <param name="time">The timestamp to interpolate to, in seconds since the recording's epoch.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetInterpolatedSkeleton(
            ulong handle,
            SkeletonType skeletonType,
            ref NativeArray<NativeTransform> interpolatedBodyPose,
            double time)
        {
            Result success;
            using (new ProfilerScope(nameof(GetInterpolatedSkeleton)))
            {
                unsafe
                {
                    var interpolatedBodyPoseLength = interpolatedBodyPose.Length;
                    success = Api.metaMovementSDK_getInterpolatedSkeletonPose(
                        handle,
                        skeletonType,
                        time,
                        interpolatedBodyPose.GetPtr(),
                        out interpolatedBodyPoseLength);
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Gets the interpolated face pose at a specific time.
        /// This is used for smooth playback of serialized facial animation data by interpolating between snapshots.
        /// </summary>
        /// <param name="handle">The handle to get the face pose from.</param>
        /// <param name="skeletonType">The type of skeleton to get the face pose for.</param>
        /// <param name="interpolatedFacePose">Reference to an array that will be filled with the interpolated blendshape weights.</param>
        /// <param name="time">The timestamp to interpolate to, in seconds since the recording's epoch.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetInterpolatedFace(
            ulong handle,
            SkeletonType skeletonType,
            ref NativeArray<float> interpolatedFacePose,
            double time)
        {
            Result success;
            using (new ProfilerScope(nameof(GetInterpolatedFace)))
            {
                unsafe
                {
                    var interpolatedFacePoseLength = interpolatedFacePose.Length;
                    success = Api.metaMovementSDK_getInterpolatedFacePose(
                        handle,
                        skeletonType,
                        time,
                        interpolatedFacePose.GetPtr(),
                        out interpolatedFacePoseLength);
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Gets the interpolated tracker joint pose at a specific time.
        /// This retrieves the interpolated position and orientation of a tracker joint (head or hands) for smooth playback.
        /// </summary>
        /// <param name="handle">The handle to get the tracker pose from.</param>
        /// <param name="trackerJointType">The type of tracker joint to get the pose for (center eye, left input, or right input).</param>
        /// <param name="outputTransform">Reference to a transform that will be filled with the interpolated tracker pose.</param>
        /// <param name="time">The timestamp to interpolate to, in seconds since the recording's epoch.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetInterpolatedJointPose(
            ulong handle,
            TrackerJointType trackerJointType,
            ref NativeTransform outputTransform,
            double time)
        {
            Result success;
            using (new ProfilerScope(nameof(GetInterpolatedJointPose)))
            {
                unsafe
                {
                    success = Api.metaMovementSDK_getInterpolatedTrackerJointPose(
                        handle,
                        trackerJointType,
                        time,
                        ref outputTransform);
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Resets all interpolators used for deserialization. This should be done
        /// if interpolating back to an earlier snapshot.
        /// </summary>
        /// <param name="handle">The handle to use for deserialization.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool ResetInterpolators(ulong handle)
        {
            Result success;
            using (new ProfilerScope(nameof(ResetInterpolators)))
            {
                success = Api.metaMovementSDK_resetInterpolators(handle);
            }

            return success == Result.Success;
        }

        /**********************************************************
         *
         *               Tool and Data Functions
         *
         **********************************************************/

        /// <summary>
        /// Writes the configuration data in a handle to a JSON string.
        /// This is useful for saving configurations to files or for debugging purposes.
        /// The JSON includes all skeleton definitions, joint mappings, and other configuration data.
        /// </summary>
        /// <param name="handle">The handle containing the configuration to export.</param>
        /// <param name="jsonConfigData">Output parameter that receives the JSON configuration data string.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool WriteConfigDataToJson(ulong handle, out string jsonConfigData, JointRelativeSpaceType optional_jointSpaceType = JointRelativeSpaceType.RootOriginRelativeSpace)
        {
            Result success;
            jsonConfigData = "";
            using (new ProfilerScope(nameof(WriteConfigDataToJson)))
            {
                unsafe
                {
                    // First call to get required buffer size
                    success = Api.metaMovementSDK_writeConfigDataToJSON(handle, null, &optional_jointSpaceType, null, out int bufferSize);
                    if (success == Result.Success && bufferSize > 0)
                    {
                        // Use heap allocation to handle large buffers
                        byte[] jsonBuffer = new byte[bufferSize];
                        fixed (byte* jsonBufferPtr = jsonBuffer)
                        {
                            success = Api.metaMovementSDK_writeConfigDataToJSON(handle, null, &optional_jointSpaceType, jsonBufferPtr,
                                out bufferSize);
                            if (success == Result.Success)
                            {
                                // Convert to string, trimming any null terminators
                                jsonConfigData = System.Text.Encoding.ASCII.GetString(jsonBuffer, 0, bufferSize)
                                    .TrimEnd('\0');
                            }
                        }
                    }
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Converts an array of transforms from one Coordinate Space System to another.
        /// This allows converting from data in one coordinate space system to another (such as OpenXR to Unity, etc)
        /// Requires the transforms be in WorldSpace representation (or the same space)
        /// Does not require a handle or a valid skeleton to convert.
        /// </summary>
        /// <param name="inCoordinateSpace">The current Coordinate Space System of the Tranform Array.</param>
        /// <param name="outCoordinateSpace">The target Coordinate Space System to convert the Transform Data to.</param>
        /// <param name="transformData">Reference to an array containing the pose to be converted, which will be updated with the converted pose.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool ApplyWorldSpaceCoordinateSpaceConversionByRef(
            CoordinateSpace inCoordinateSpace,
            CoordinateSpace outCoordinateSpace,
            ref NativeArray<NativeTransform> transformData)
        {
            Result success;
            using (new ProfilerScope(nameof(ApplyWorldSpaceCoordinateSpaceConversionByRef)))
            {
                unsafe
                {
                    success = Api.metaMovementSDK_applyWorldSpaceCoordinateSpaceConversion(
                        inCoordinateSpace,
                        outCoordinateSpace,
                        transformData.GetPtr(),
                        transformData.Length);
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Converts a transform from one Coordinate Space System to another.
        /// This allows converting from data in one coordinate space system to another (such as OpenXR to Unity, etc)
        /// Requires the transform be in WorldSpace representation (or the same space)
        /// Does not require a handle or a valid skeleton to convert.
        /// </summary>
        /// <param name="inCoordinateSpace">The current Coordinate Space System of the Tranform.</param>
        /// <param name="outCoordinateSpace">The target Coordinate Space System to convert the Transform Data to.</param>
        /// <param name="transformData">The transform to convert.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool ApplyWorldSpaceCoordinateSpaceConversion(
            CoordinateSpace inCoordinateSpace,
            CoordinateSpace outCoordinateSpace,
            ref NativeTransform transformData)
        {
            Result success;
            using (new ProfilerScope(nameof(ApplyWorldSpaceCoordinateSpaceConversion)))
            {
                var tempArray = new NativeArray<NativeTransform>(1, Allocator.Temp);
                tempArray[0] = transformData;

                unsafe
                {
                    success = Api.metaMovementSDK_applyWorldSpaceCoordinateSpaceConversion(
                        inCoordinateSpace,
                        outCoordinateSpace,
                        tempArray.GetPtr(),
                        tempArray.Length);
                }

                transformData = tempArray[0];
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Converts a skeleton pose from one Coordinate Space System to another.
        /// This allows converting from data in one coordinate space system to another (such as OpenXR to Unity, etc)
        /// </summary>
        /// <param name="handle">The handle containing the skeleton hierarchy information.</param>
        /// <param name="skeletonType">The type of skeleton the pose belongs to.</param>
        /// <param name="jointSpaceType">The current joint space format of the skeleton pose.</param>
        /// <param name="inCoordinateSpace">The current Coordinate Space System of the skeleton pose.</param>
        /// <param name="outCoordinateSpace">The target Coordinate Space System to convert the skeleton pose to.</param>
        /// <param name="skeletonPose">Reference to an array containing the pose to be converted, which will be updated with the converted pose.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool ApplyCoordinateSpaceConversionByRef(
            ulong handle,
            SkeletonType skeletonType,
            JointRelativeSpaceType jointSpaceType,
            CoordinateSpace inCoordinateSpace,
            CoordinateSpace outCoordinateSpace,
            ref NativeArray<NativeTransform> skeletonPose)
        {
            Result success;
            using (new ProfilerScope(nameof(ApplyCoordinateSpaceConversionByRef)))
            {
                unsafe
                {
                    success = Api.metaMovementSDK_applyCoordinateSpaceConversion(
                        handle,
                        skeletonType,
                        jointSpaceType,
                        inCoordinateSpace,
                        outCoordinateSpace,
                        skeletonPose.GetPtr(),
                        skeletonPose.Length);
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Converts a skeleton pose from one joint space format to another.
        /// This allows transforming between different coordinate representations, such as from local space
        /// (where each joint is relative to its parent) to root-relative space (where each joint is relative to the root).
        /// </summary>
        /// <param name="handle">The handle containing the skeleton hierarchy information.</param>
        /// <param name="skeletonType">The type of skeleton the pose belongs to.</param>
        /// <param name="inJointSpaceType">The current joint space format of the skeleton pose.</param>
        /// <param name="outJointSpaceType">The target joint space format to convert the skeleton pose to.</param>
        /// <param name="skeletonPose">Reference to an array containing the pose to be converted, which will be updated with the converted pose.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool ConvertJointPoseByRef(
            ulong handle,
            SkeletonType skeletonType,
            JointRelativeSpaceType inJointSpaceType,
            JointRelativeSpaceType outJointSpaceType,
            ref NativeArray<NativeTransform> skeletonPose)
        {
            Result success;
            using (new ProfilerScope(nameof(ConvertJointPoseByRef)))
            {
                unsafe
                {
                    success = Api.metaMovementSDK_convertJointPose(
                        handle,
                        skeletonType,
                        inJointSpaceType,
                        outJointSpaceType,
                        skeletonPose.GetPtr(),
                        skeletonPose.Length);
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Calculates the spatial extents (min, max, and range) for a provided skeleton pose.
        /// This is useful for determining the bounding box of a pose, which can be used for
        /// collision detection, camera framing, or other spatial calculations.
        /// </summary>
        /// <param name="handle">The handle containing the skeleton information.</param>
        /// <param name="skeletonType">The type of skeleton the pose belongs to.</param>
        /// <param name="inJointSpaceType">The joint space format of the provided skeleton pose.</param>
        /// <param name="skeletonPose">Reference to an array containing the pose to calculate extents for.</param>
        /// <param name="extentInfo">Output parameter that receives the Extents structure containing min, max, and range values.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool CalculatePoseExtents(
            ulong handle,
            SkeletonType skeletonType,
            JointRelativeSpaceType inJointSpaceType,
            ref NativeArray<NativeTransform> skeletonPose,
            out Extents extentInfo)
        {
            Result success;
            using (new ProfilerScope(nameof(CalculatePoseExtents)))
            {
                unsafe
                {
                    success = Api.metaMovementSDK_calculatePoseExtents(
                        handle,
                        skeletonType,
                        inJointSpaceType,
                        skeletonPose.GetPtr(),
                        skeletonPose.Length,
                        out extentInfo);
                }
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Identify and populate known joint indices by analyzing the skeleton's rest pose.
        /// This function analyzes the spatial relationships and positions of joints in the rest pose
        /// to automatically identify which joints correspond to known joint types (e.g., hips, neck, etc.).
        /// This is useful when working with skeletons that don't have explicit known joint mappings defined,
        /// allowing automatic identification based on anatomical structure.
        /// Returns a KnownJointIndexData structure with identified mappings.
        /// Joints that cannot be identified will have an index value of INVALID_JOINT_INDEX (-1).
        /// </summary>
        /// <param name="handle">The handle to identify known joint indices from.</param>
        /// <param name="skeletonType">The type of skeleton to identify known joint indices for.</param>
        /// <param name="knownJoints">Output parameter that receives the identified known joint index data.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool IdentifyKnownJointIndexesFromRestPose(
            ulong handle,
            SkeletonType skeletonType,
            out KnownJointIndexData knownJoints)
        {
            Result success;
            using (new ProfilerScope(nameof(IdentifyKnownJointIndexesFromRestPose)))
            {
                success = Api.metaMovementSDK_identifyKnownJointIndexesFromRestPose(handle, skeletonType, out knownJoints);
            }

            return success == Result.Success;
        }

        /// <summary>
        /// Get the joint names for the known joints specified in the KnownJointIndexData structure.
        /// This function takes a KnownJointIndexData structure and returns an array of joint names
        /// corresponding to the valid (non-negative) joint indices in the structure.
        /// Only joints with valid indices (not INVALID_JOINT_INDEX) will have their names returned.
        /// </summary>
        /// <param name="handle">The handle to get the joint names from.</param>
        /// <param name="skeletonType">The type of skeleton to get the joint names for.</param>
        /// <param name="knownJointIndexData">The known joint index data containing the joint indices.</param>
        /// <param name="knownJointNames">Output array that receives the joint names for the known joints.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool GetKnownJointNamesFromIndexData(
            ulong handle,
            SkeletonType skeletonType,
            KnownJointIndexData knownJointIndexData,
            out string[] knownJointNames)
        {
            Result result;
            using (new ProfilerScope(nameof(GetKnownJointNamesFromIndexData)))
            {
                unsafe
                {
                    int charBufferSize = 0;
                    result = Api.metaMovementSDK_getKnownJointNamesFromIndexData(
                        handle, skeletonType, knownJointIndexData, null, out charBufferSize, null);

                    if (result == Result.Success && charBufferSize > 0)
                    {
                        var charBuffer = new NativeArray<byte>(charBufferSize, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                        KnownJointNameData jointNameData;
                        result = Api.metaMovementSDK_getKnownJointNamesFromIndexData(
                            handle, skeletonType, knownJointIndexData, charBuffer.GetPtr(), out charBufferSize, &jointNameData);

                        if (result == Result.Success)
                        {
                            knownJointNames = new string[(int)KnownJointType.KnownJointCount];
                            for (int i = 0; i < (int)KnownJointType.KnownJointCount; i++)
                            {
                                if (jointNameData.JointNameByType[i] != null)
                                {
                                    int length = 0;
                                    byte* ptr = jointNameData.JointNameByType[i];
                                    while (ptr[length] != 0) length++;
                                    knownJointNames[i] = System.Text.Encoding.UTF8.GetString(ptr, length);
                                }
                                else
                                {
                                    knownJointNames[i] = string.Empty;
                                }
                            }
                        }
                        else
                        {
                            knownJointNames = Array.Empty<string>();
                        }

                        charBuffer.Dispose();
                    }
                    else
                    {
                        knownJointNames = Array.Empty<string>();
                    }
                }
            }

            return result == Result.Success;
        }

        /// <summary>
        /// Identifies joints to include in mapping using the rest pose.
        /// This function analyzes the skeleton's rest pose to determine which joints should be
        /// included in a mapping operation. Joints are identified based on their rest pose
        /// characteristics and can be used to create more accurate retargeting mappings.
        /// </summary>
        /// <param name="handle">The handle to analyze the skeleton from.</param>
        /// <param name="skeletonType">The type of skeleton to analyze.</param>
        /// <param name="jointNames">Output array that receives the names of joints that should be included in mapping.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool IdentifyJointsToIncludeInMappingUsingRestPose(
            ulong handle,
            SkeletonType skeletonType,
            out string[] jointNames)
        {
            Result result;
            jointNames = Array.Empty<string>();
            using (new ProfilerScope(nameof(IdentifyJointsToIncludeInMappingUsingRestPose)))
            {
                unsafe
                {
                    int bufferSize = 0;
                    int numJoints = 0;
                    result = Api.metaMovementSDK_identifyJointsToIncludeInMappingUsingRestPose(
                        handle, skeletonType, null, out bufferSize, null, out numJoints);

                    if (result == Result.Success && bufferSize > 0)
                    {
                        var buffer = new byte[bufferSize];
                        Span<byte> nameBuffer = buffer;
                        fixed (byte* bytes = &nameBuffer.GetPinnableReference())
                        {
                            result = Api.metaMovementSDK_identifyJointsToIncludeInMappingUsingRestPose(
                                handle, skeletonType, bytes, out bufferSize, null, out numJoints);
                            if (result == Result.Success)
                            {
                                ConvertByteBufferToStringArray(bytes, bufferSize, numJoints, out jointNames);
                            }
                        }
                    }
                }
            }

            return result == Result.Success;
        }

        /// <summary>
        /// Identifies joints that could potentially be excluded from mapping using the rest pose.
        /// This function analyzes the skeleton's rest pose to determine which joints might not
        /// be necessary or desirable for mapping operations, such as end effectors or IK targets.
        /// </summary>
        /// <param name="handle">The handle to analyze the skeleton from.</param>
        /// <param name="skeletonType">The type of skeleton to analyze.</param>
        /// <param name="jointNames">Output array that receives the names of joints that could be excluded from mapping.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool IdentifyPossibleExcludeFromMappingUsingRestPose(
            ulong handle,
            SkeletonType skeletonType,
            out string[] jointNames)
        {
            Result result;
            jointNames = Array.Empty<string>();
            using (new ProfilerScope(nameof(IdentifyPossibleExcludeFromMappingUsingRestPose)))
            {
                unsafe
                {
                    int bufferSize = 0;
                    int numJoints = 0;
                    result = Api.metaMovementSDK_identifyPossibleExcludeFromMappingUsingRestPose(
                        handle, skeletonType, null, out bufferSize, null, out numJoints);

                    if (result == Result.Success && bufferSize > 0)
                    {
                        var buffer = new byte[bufferSize];
                        Span<byte> nameBuffer = buffer;
                        fixed (byte* bytes = &nameBuffer.GetPinnableReference())
                        {
                            result = Api.metaMovementSDK_identifyPossibleExcludeFromMappingUsingRestPose(
                                handle, skeletonType, bytes, out bufferSize, null, out numJoints);
                            if (result == Result.Success)
                            {
                                ConvertByteBufferToStringArray(bytes, bufferSize, numJoints, out jointNames);
                            }
                        }
                    }
                }
            }

            return result == Result.Success;
        }


        /// <summary>
        /// Serializes a recording's <see cref="StartHeader"/> into bytes.
        /// </summary>
        /// <param name="startHeader">The <see cref="StartHeader"/> to serialize.</param>
        /// <param name="headerBytes">The serialized byes.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool SerializeStartHeader(
            StartHeader startHeader,
            out byte[] headerBytes)
        {
            headerBytes = null;
            using (new ProfilerScope(nameof(SerializeStartHeader)))
            {
                var startHeaderArgument = new StartHeaderSerializedBytes
                {
                    SerializedBytes = new byte[SERIALIZATION_START_HEADER_SIZE_BYTES]
                };
                var success = Api.metaMovementSDK_serializeStartHeader(startHeader, ref startHeaderArgument);
                if (success != Result.Success)
                {
                    Debug.LogError("Could not serialize start header!");
                    return false;
                }

                var serializedBytesCreated = startHeaderArgument.SerializedBytes;
                headerBytes = new byte[serializedBytesCreated.Length];
                for (int i = 0; i < headerBytes.Length; i++)
                {
                    headerBytes[i] = serializedBytesCreated[i];
                }
            }

            return true;
        }

        /// <summary>
        /// Deserializes a recording's <see cref="StartHeader"/> from bytes.
        /// </summary>
        /// <param name="startHeader">The deserialized <see cref="StartHeader"/> to update.</param>
        /// <param name="headerBytes">The serialized <see cref="StartHeader"/> bytes.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool DeserializeStartHeader(
            byte[] headerBytes,
            out StartHeader startHeader)
        {
            if (headerBytes == null)
            {
                Debug.LogError("Can't deserialize start header if bytes array is null.");
            }

            if (headerBytes != null && headerBytes.Length != SERIALIZATION_START_HEADER_SIZE_BYTES)
            {
                Debug.LogError($"Can't deserialize start header if bytes array, " +
                               $"length is {headerBytes.Length} bytes, " +
                               $"expected: {SERIALIZATION_START_HEADER_SIZE_BYTES} bytes.");
            }

            using (new ProfilerScope(nameof(DeserializeStartHeader)))
            {
                unsafe
                {
                    var headerBytesStruct = new StartHeaderSerializedBytes(headerBytes);
                    var success = Api.metaMovementSDK_deserializeStartHeader(out startHeader, headerBytesStruct);
                    if (success != Result.Success)
                    {
                        Debug.LogError("Could not serialize start header!");
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Serializes a recording's <see cref="EndHeader"/> into bytes.
        /// </summary>
        /// <param name="endHeader">The <see cref="EndHeader"/> to serialize.</param>
        /// <param name="headerBytes">The serialized bytes to write.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool SerializeEndHeader(
            EndHeader endHeader,
            out byte[] headerBytes)
        {
            headerBytes = null;
            using (new ProfilerScope(nameof(SerializeEndHeader)))
            {
                EndHeaderSerializedBytes endHeaderArgument = new EndHeaderSerializedBytes();
                endHeaderArgument.SerializedBytes = new byte[SERIALIZATION_END_HEADER_SIZE_BYTES];
                var success = Api.metaMovementSDK_serializeEndHeader(endHeader, ref endHeaderArgument);
                if (success != Result.Success)
                {
                    Debug.LogError("Could not serialize the end header!");
                    return false;
                }
                else
                {
                    var serializedBytesCreated = endHeaderArgument.SerializedBytes;
                    headerBytes = new byte[serializedBytesCreated.Length];
                    for (int i = 0; i < headerBytes.Length; i++)
                    {
                        headerBytes[i] = serializedBytesCreated[i];
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Deserializes a recording's <see cref="EndHeader"/> from bytes.
        /// </summary>
        /// <param name="headerBytes">The bytes provided as input.</param>
        /// <param name="endHeader">The deserialized <see cref="EndHeader"/> to update.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool DeserializeEndHeader(
            byte[] headerBytes,
            out EndHeader endHeader)
        {
            endHeader = new EndHeader();
            using (new ProfilerScope(nameof(DeserializeEndHeader)))
            {
                if (headerBytes == null)
                {
                    Debug.LogError("Can't deserialize end header if bytes array is null.");
                    return false;
                }

                if (headerBytes.Length != SERIALIZATION_END_HEADER_SIZE_BYTES)
                {
                    Debug.LogError($"Can't deserialize end header if bytes array, " +
                                   $"length is {headerBytes.Length} bytes, " +
                                   $"expected: {SERIALIZATION_END_HEADER_SIZE_BYTES} bytes.");
                }

                var headerBytesStruct = new EndHeaderSerializedBytes(headerBytes);
                var success = Api.metaMovementSDK_deserializeEndHeader(out endHeader, headerBytesStruct);
                if (success != Result.Success)
                {
                    Debug.LogError("Could not deserialize end header!");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Deserializes snapshot timestamp.
        /// </summary>
        /// <param name="data">The byte array to deserialize.</param>
        /// <param name="timestamp">The timestamp to return.</param>
        /// <returns>True if the function was successfully executed.</returns>
        public static bool DeserializeSnapshotTimestamp(
            NativeArray<byte> data,
            out double timestamp)
        {
            using (new ProfilerScope(nameof(DeserializeSnapshotTimestamp)))
            {
                unsafe
                {
                    var success = Api.metaMovementSDK_deserializeSnapshotTimestamp(data.GetPtr(), out timestamp);
                    if (success != Result.Success)
                    {
                        Debug.LogError("Could not get snapshot timestamp!");
                        return false;
                    }
                }
            }

            return true;
        }

        #endregion


        /**********************************************************
         *
         *               Helper Functions
         *
         **********************************************************/

        private static unsafe bool ConvertByteBufferToStringArray(byte* stringBuffer, int bufferSize, int stringCount,
            out string[] stringArray)
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
                        stringArray[stringsFound] =
                            Marshal.PtrToStringAnsi((IntPtr)(stringBuffer + currentNameStartIndex), stringLength)
                                .TrimEnd('\0');
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
