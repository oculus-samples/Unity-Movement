// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using static Meta.XR.Movement.MSDKUtility;
using static Meta.XR.Movement.Retargeting.SkeletonData;
using static Unity.Collections.Allocator;
using static Unity.Collections.NativeArrayOptions;
#if ISDK_DEFINED
using Oculus.Interaction.Body.Input;
using Oculus.Interaction.Input;
#endif

namespace Meta.XR.Movement.Retargeting
{
    /// <summary>
    /// Provides convenient functions that obtain data from the tracker.
    /// </summary>
    public static class SkeletonUtilities
    {
        /// <summary>
        /// Updates an array of source poses from arrays of
        /// positions and rotations. An offset can be provided to
        /// transform the entire set of poses.
        /// </summary>
        [BurstCompile]
        private struct UpdateSourcePoseJob : IJob
        {
            /// <summary>
            /// Offset to apply to the source poses.
            /// </summary>
            [ReadOnly]
            public Pose Offset;

            /// <summary>
            /// Input pose rotations to read from.
            /// </summary>
            [ReadOnly]
            public NativeArray<OVRPlugin.Quatf> InputPoseRotations;

            /// <summary>
            /// Input pose translations to read from.
            /// </summary>
            [ReadOnly]
            public NativeArray<OVRPlugin.Vector3f> InputPoseTranslations;

            /// <summary>
            /// Whether to convert to Unity space or not.
            /// </summary>
            [ReadOnly]
            public bool ConvertToUnitySpace;

            /// <summary>
            /// Output poses to write to.
            /// </summary>
            public NativeArray<NativeTransform> OutputPose;

            /// <summary>
            /// Execute the job. Updates an array of source poses from input
            /// position and rotations arrays.
            /// </summary>
            [BurstCompile]
            public void Execute()
            {
                var numberOfPoses = OutputPose.Length;
                for (var i = 0; i < numberOfPoses; i++)
                {
                    var sourceQuat = ConvertToUnitySpace ?
                        InputPoseRotations[i].FromFlippedZQuatf() : InputPoseRotations[i].FromQuatf();
                    var sourcePose = ConvertToUnitySpace ?
                        InputPoseTranslations[i].FromFlippedZVector3f() : InputPoseTranslations[i].FromVector3f();
                    var rotation = Offset.rotation * sourceQuat;
                    var position = Offset.rotation * sourcePose + Offset.position;
                    var pose = OutputPose[i];
                    pose.Position = position;
                    pose.Orientation = rotation;
                    pose.Scale = Vector3.one;
                    OutputPose[i] = pose;
                }
            }
        }

        /// <summary>
        /// Computes local poses from the world poses provided.
        /// </summary>
        [BurstCompile]
        private struct ComputeLocalFromWorldPosesJob : IJob
        {
            /// <summary>
            /// World poses to read from.
            /// </summary>
            [ReadOnly]
            public NativeArray<NativeTransform> WorldPoses;

            /// <summary>
            /// Parent indices to read from. Maps a joint index to
            /// its parent index. This is necessary because a joint's
            /// local pose must be computed relative to its parent's.
            /// </summary>
            [ReadOnly]
            public NativeArray<int> ParentIndices;

            /// <summary>
            /// Local poses to write to.
            /// </summary>
            public NativeArray<NativeTransform> LocalPoses;

            /// <summary>
            /// Computes local poses of each joint but compute the
            /// pose relative to its parent's pose.
            /// </summary>
            [BurstCompile]
            public void Execute()
            {
                var numberOfPoses = LocalPoses.Length;
                for (var i = 0; i < numberOfPoses; i++)
                {
                    // do not transform if:
                    // a) it's the hips/root
                    // b) the number of parent indices provided by the config
                    // is fewer than the number of poses provided
                    // c) the parent index is out of bounds
                    if (i == 0 || i >= ParentIndices.Length ||
                        ParentIndices[i] >= WorldPoses.Length)
                    {
                        LocalPoses[i] = WorldPoses[i];
                        continue;
                    }

                    var parentIndex = ParentIndices[i];
                    var parentPose = WorldPoses[parentIndex];
                    var currentPose = WorldPoses[i];
                    Vector3 fromPosition = parentPose.Position,
                        toPosition = currentPose.Position;
                    Quaternion fromRotation = parentPose.Orientation,
                        toRotation = currentPose.Orientation;


                    Pose localPose = ComputeDeltaPose(fromPosition, fromRotation, toPosition, toRotation);
                    LocalPoses[i] = new NativeTransform(
                        localPose.rotation,
                        localPose.position,
                        Vector3.one);
                }
            }

            private static Pose ComputeDeltaPose(
                Vector3 fromPosition,
                Quaternion fromRotation,
                Vector3 toPosition,
                Quaternion toRotation)
            {
                var result = new Pose();
                var inverseFromRot = Quaternion.Inverse(fromRotation);
                result.position = inverseFromRot * (toPosition - fromPosition);
                result.rotation = inverseFromRot * toRotation;
                return result;
            }
        }

        private static readonly Quaternion _openXRLeftHandRotOffset = Quaternion.Euler(180, 90, 0);
        private static readonly Quaternion _openXRRightHandRotOffset = Quaternion.Euler(0, 270, 0);
        private static OVRCameraRig _ovrCameraRig;
        private static OVRPlugin.Skeleton2 _skeleton;
        private static OVRSkeleton.SkeletonPoseData _data;
        private static NativeArray<NativeTransform> _outputPoses;
        private static float _timestamp;

        /// <summary>
        /// Computes world poses using SkeletonRetargeter for automatic parameter filling.
        /// </summary>
        /// <param name="skeletonRetargeter">The skeleton retargeter containing all necessary parameters.</param>
        /// <param name="targetPoses">Target poses.</param>
        /// <param name="rootPosition">Root position, optional.</param>
        /// <param name="rootRotation">Root rotation, optional.</param>
        /// <returns></returns>
        public static NativeArray<NativeTransform> ComputeWorldPoses(
            SkeletonRetargeter skeletonRetargeter,
            ref NativeArray<NativeTransform> targetPoses,
            Vector3? rootPosition = null,
            Quaternion? rootRotation = null)
        {
            var targetPoseWorld = new NativeArray<NativeTransform>(targetPoses.Length, Allocator.TempJob);
            using var parentIndices = new NativeArray<int>(skeletonRetargeter.TargetParentIndices, Allocator.TempJob);

            var job = new SkeletonJobs.ConvertLocalToWorldPoseJob
            {
                LocalPose = targetPoses,
                WorldPose = targetPoseWorld,
                ParentIndices = parentIndices,
                RootIndex = skeletonRetargeter.RootJointIndex,
                HipsIndex = skeletonRetargeter.HipsJointIndex,
                RootScale = skeletonRetargeter.RootScale,
                HipsScale = skeletonRetargeter.HipsScale,
                RootPosition = rootPosition ?? Vector3.zero,
                RootRotation = rootRotation ?? Quaternion.identity
            };
            job.Schedule().Complete();
            return targetPoseWorld;
        }

        /// <summary>
        /// Get current body tracking frame data.
        /// </summary>
        /// <param name="dataProvider">Data provider.</param>
        /// <param name="trackerPositionsWorldSpace">Gets tracker positions in world space.</param
        /// <param name="lastSkeletalChangeCount">If last skeletal change count has been updated, provide a new bind pose.</param>
        /// <returns>Latest available <see cref="MSDKUtility.FrameData"/>.</returns>
        public static FrameData GetCurrentFrameData(
            OVRSkeleton.IOVRSkeletonDataProvider dataProvider,
            bool trackerPositionsWorldSpace,
            ref int lastSkeletalChangeCount,
            out NativeArray<NativeTransform> bindPoses,
            out int numBindPoseJoints)
        {
            var skeletonPoseData = dataProvider.GetSkeletonPoseData();
            // provider doesn't give us fidelity, so we have to fetch it ourselves
            OVRPlugin.BodyState bodyState = new OVRPlugin.BodyState();
            OVRPlugin.BodyTrackingFidelity2 fidelity2 = OVRPlugin.BodyTrackingFidelity2.Low;
            if (!OVRPlugin.GetBodyState4(OVRPlugin.Step.Render,
                    dataProvider.GetSkeletonType() == OVRSkeleton.SkeletonType.FullBody
                        ? OVRPlugin.BodyJointSet.FullBody
                        : OVRPlugin.BodyJointSet.UpperBody, ref bodyState))
            {
                fidelity2 = bodyState.Fidelity;
            }

            ProvideInputTrackingState(out var leftSideHand, out var leftSideInputPos,
                out var leftSideInputRot, true);
            ProvideInputTrackingState(out var rightSideHand, out var rightSideInputPos,
                out var rightSideInputRot, false);

            Vector3 centerEyeTrackingPos = Vector3.zero;
            Quaternion centerEyeTrackingRot = Quaternion.identity;
            Transform centerEye = GetCenterEyeTransform();
            if (centerEye == null)
            {
                Debug.Log("Center eye transform was not found..");
            }
            else
            {
                centerEyeTrackingPos = centerEye.localPosition;
                centerEyeTrackingRot = centerEye.localRotation;
            }

            if (trackerPositionsWorldSpace)
            {
                var trackerSpaceTransform = GetTrackingSpaceTransform();
                if (trackerSpaceTransform == null)
                {
                    Debug.LogError("Cannot get tracker positions in world space because " +
                                   "the tracking space transform is null.");
                }
                else
                {
                    (leftSideInputPos, leftSideInputRot) = TransformPositionAndRotationToWorld(
                        trackerSpaceTransform, leftSideInputPos, leftSideInputRot);
                    (rightSideInputPos, rightSideInputRot) = TransformPositionAndRotationToWorld(
                        trackerSpaceTransform, rightSideInputPos, rightSideInputRot);
                    (centerEyeTrackingPos, centerEyeTrackingRot) = TransformPositionAndRotationToWorld(
                        trackerSpaceTransform, centerEyeTrackingPos, centerEyeTrackingRot);
                }
            }
            var frameData = new FrameData(
                (byte)fidelity2,
                bodyState.Time,
                skeletonPoseData.IsDataValid,
                bodyState.Confidence,
                (byte)bodyState.JointSet,
                (byte)bodyState.CalibrationStatus,
                bodyState.SkeletonChangedCount,
                leftSideHand,
                rightSideHand,
                new NativeTransform(leftSideInputRot, leftSideInputPos),
                new NativeTransform(rightSideInputRot, rightSideInputPos),
                new NativeTransform(centerEyeTrackingRot, centerEyeTrackingPos)
            );

            bool provideBindPose = false;
            if (skeletonPoseData.SkeletonChangedCount != lastSkeletalChangeCount)
            {
                lastSkeletalChangeCount = skeletonPoseData.SkeletonChangedCount;
                provideBindPose = true;
            }
            if (provideBindPose)
            {
                var newBindPoses = GetBindPoses(dataProvider, false);
                numBindPoseJoints = newBindPoses.Length;
                bindPoses = new NativeArray<NativeTransform>(numBindPoseJoints, Allocator.TempJob);
                for (int i = 0; i < numBindPoseJoints; i++)
                {
                    bindPoses[i] = newBindPoses[i];
                }
            }
            else
            {
                numBindPoseJoints = 0;
                bindPoses = new NativeArray<NativeTransform>(1, Allocator.TempJob);
            }

            return frameData;
        }

        /// <summary>
        /// Gets poses from the tracker. Checks against expected pose count.
        /// </summary>
        /// <param name="dataProvider">Data provider.</param>
        /// <param name="offset"></param>
        /// <param name="convertToUnitySpace">Convert to Unity space.</param>
        /// <param name="skeletonChangeCount"></param>
        /// <param name="validPoses"></param>
        /// <returns>Array of transforms.</returns>
        public static NativeArray<NativeTransform> GetPosesFromTheTracker(
            OVRSkeleton.IOVRSkeletonDataProvider dataProvider,
            Pose offset,
            bool convertToUnitySpace,
            out int skeletonChangeCount,
            out bool validPoses)
        {
            if (Mathf.Approximately(Time.time - _timestamp, 0.0f))
            {
                skeletonChangeCount = _data.SkeletonChangedCount;
                validPoses = _data.IsDataValid;
                return _outputPoses;
            }

            var allPoses = GetPosesFromTheTracker(dataProvider, offset, convertToUnitySpace);
            _timestamp = Time.time;
            _outputPoses = new NativeArray<NativeTransform>(allPoses.Length, Temp);
            _outputPoses.CopyFrom(allPoses);
            validPoses = _data.IsDataValid;
            skeletonChangeCount = _data.SkeletonChangedCount;
            allPoses.Dispose();
            return _outputPoses;
        }

        /// <summary>
        /// Returns transforms from the tracker.
        /// </summary>
        /// <param name="dataProvider">Data provider to provide transforms.</param>
        /// <param name="offset">Offset to apply.</param>
        /// <param name="convertToUnitySpace">Convert to Unity space.</param>
        /// <returns>Tracker poses.</returns>
        public static NativeArray<NativeTransform> GetPosesFromTheTracker(
            OVRSkeleton.IOVRSkeletonDataProvider dataProvider,
            Pose offset,
            bool convertToUnitySpace)
        {
            // Get body tracking data
            _data = dataProvider.GetSkeletonPoseData();
            var isFullBody = dataProvider.GetSkeletonType() == OVRSkeleton.SkeletonType.FullBody;

            // If data isn't valid, just provide default poses
            if (!_data.IsDataValid)
            {
                var numBonesSpec = isFullBody ? (int)FullBodyTrackingBoneId.End : (int)BodyTrackingBoneId.End;
                var invalidPoses = new NativeArray<NativeTransform>(numBonesSpec, TempJob, UninitializedMemory);
                for (var i = 0; i < numBonesSpec; i++)
                {
                    invalidPoses[i] = NativeTransform.Identity();
                }

                return invalidPoses;
            }

            // Convert to native arrays
            var boneTranslations = GetBoneTranslations(_data.BoneTranslations);
            var boneRotations = GetBoneRotations(_data.BoneRotations);
            var sourcePoses = new NativeArray<NativeTransform>(_data.BoneTranslations.Length, TempJob);
            var job = new UpdateSourcePoseJob
            {
                Offset = offset,
                InputPoseRotations = boneRotations,
                InputPoseTranslations = boneTranslations,
                OutputPose = sourcePoses,
                ConvertToUnitySpace = convertToUnitySpace
            };
            job.Schedule().Complete();
            boneTranslations.Dispose();
            boneRotations.Dispose();
            return sourcePoses;
        }

        /// <summary>
        /// Returns the bind pose.
        /// </summary>
        /// <param name="dataProvider">Data provider.</param>
        /// <param name="convertToUnitySpace">Convert to Unity space.</param>
        /// <returns></returns>
        public static NativeArray<NativeTransform> GetBindPoses(
            OVRSkeleton.IOVRSkeletonDataProvider dataProvider,
            bool convertToUnitySpace = true)
        {
            var sourcePoses = new NativeArray<NativeTransform>(0, Temp, UninitializedMemory);
            var skeletonType = dataProvider.GetSkeletonType() == OVRSkeleton.SkeletonType.FullBody
                ? OVRPlugin.SkeletonType.FullBody
                : OVRPlugin.SkeletonType.Body;
            if (!OVRPlugin.GetSkeleton2(skeletonType, ref _skeleton))
            {
                return sourcePoses;
            }

            var numBones = _skeleton.Bones.Length;
            sourcePoses = new NativeArray<NativeTransform>(numBones, Temp, UninitializedMemory);
            for (var i = 0; i < numBones; i++)
            {
                var skeletonPose = _skeleton.Bones[i].Pose;
                sourcePoses[i] = new NativeTransform(
                    convertToUnitySpace ? skeletonPose.Orientation.FromFlippedZQuatf() : skeletonPose.Orientation.FromQuatf(),
                    convertToUnitySpace ? skeletonPose.Position.FromFlippedZVector3f() : skeletonPose.Position.FromVector3f(),
                    Vector3.one);
            }

            return sourcePoses;
        }

        /// <summary>
        /// Computes and returns transforms relative to their parents.
        /// </summary>
        /// <param name="parentIndices">Parent indices.</param>
        /// <param name="trackerPosesWorldSpace">Tracker positions in world space.</param>
        /// <returns>Poses relative to their parents.</returns>
        public static NativeArray<NativeTransform> GetTransformsRelativeToParents(
            int[] parentIndices,
            NativeArray<NativeTransform> trackerPosesWorldSpace)
        {
            // don't modify the original poses; we need to have an untouched array of world positions
            // that we compute local poses from
            var localPoses = new NativeArray<NativeTransform>(trackerPosesWorldSpace.Length,
                TempJob, UninitializedMemory);
            var parentIndicesNativeArray = new NativeArray<int>(parentIndices.Length,
                TempJob, UninitializedMemory);
            for (int i = 0; i < parentIndices.Length; i++)
            {
                parentIndicesNativeArray[i] = parentIndices[i];
            }

            var localPosesJob = new ComputeLocalFromWorldPosesJob
            {
                WorldPoses = trackerPosesWorldSpace,
                ParentIndices = parentIndicesNativeArray,
                LocalPoses = localPoses
            };
            localPosesJob.Schedule().Complete();
            parentIndicesNativeArray.Dispose();

            return localPoses;
        }

        /// <summary>
        /// Computes and returns the child transform affected by the parent.
        /// </summary>
        /// <param name="parentPose">Parent pose.</param>
        /// <param name="childPose">Child pose.</param>
        /// <returns>Native transform representing the child transform.</returns>
        public static NativeTransform GetChildTransformAffectedByParent(Pose parentPose, Pose childPose)
        {
            Matrix4x4 parentMatrix = Matrix4x4.TRS(parentPose.position, parentPose.rotation, Vector3.one);
            Vector3 finalPosition = parentMatrix.MultiplyPoint3x4(childPose.position);
            Quaternion finalRotation = parentMatrix.rotation * childPose.rotation;
            return new NativeTransform(
                finalRotation,
                finalPosition);
        }

        /// <summary>
        /// Converts local poses to absolute space. Root is the pivot.
        /// </summary>
        /// <param name="parentIndices">Array of parent indices for each joint in the skeleton.</param>
        /// <param name="posesToModify">Array of poses in local space to be converted to absolute space. They must be ordered
        /// from parents down to children, because the function updates transforms in that order.</param>
        /// <param name="anchorPose">Optional root pose to apply to the root to anchor the character
        /// around a different point.</param>
        public static void ConvertLocalPosesToAbsolute(
            int[] parentIndices,
            NativeArray<NativeTransform> posesToModify,
            Pose? anchorPose = null)
        {
            if (anchorPose.HasValue)
            {
                var finalPose = new Pose();
                var childPose = new Pose(posesToModify[0].Position,
                    posesToModify[0].Orientation);
                MultiplyPoses(anchorPose.Value, childPose, ref finalPose);
                posesToModify[0] = GetChildTransformAffectedByParent(
                    anchorPose.Value,
                    new Pose(posesToModify[0].Position, posesToModify[0].Orientation));
            }

            // all joints are relative to parent, except for root.
            // This cannot be done as a job, because it computes the world poses for the parents
            // at the lower indices first, and then proceeds with the children.
            // Note that some indices can have parents at later indices; skip these
            // first. Process them after the rest of the "normal" nodes have been processed.
            bool foundIndexWithLaterParent = false;
            for (int i = 0; i < posesToModify.Length; i++)
            {
                // first joint is root, just skip.
                if (i == 0)
                {
                    continue;
                }

                if (i >= parentIndices.Length)
                {
                    continue;
                }

                // parent index
                var parentIndex = parentIndices[i];
                // Skip -- this node is unusual.
                if (parentIndex > i)
                {
                    foundIndexWithLaterParent = true;
                    continue;
                }
                if (parentIndex < posesToModify.Length)
                {
                    var parentTransform = posesToModify[parentIndex];
                    var currentTransform = posesToModify[i];
                    posesToModify[i] = GetChildTransformAffectedByParent(
                        new Pose(parentTransform.Position, parentTransform.Orientation),
                        new Pose(currentTransform.Position, currentTransform.Orientation));
                }
            }
            if (!foundIndexWithLaterParent)
            {
                return;
            }
            // Now process the nodes with parent indices that are greater.
            for (int i = 0; i < posesToModify.Length; i++)
            {
                if (i == 0)
                {
                    continue;
                }

                if (i >= parentIndices.Length)
                {
                    continue;
                }

                // parent index
                var parentIndex = parentIndices[i];
                // Skip normal nodes. Focus on the unusual ones.
                if (parentIndex < i)
                {
                    continue;
                }
                if (parentIndex < posesToModify.Length)
                {
                    var parentTransform = posesToModify[parentIndex];
                    var currentTransform = posesToModify[i];
                    posesToModify[i] = GetChildTransformAffectedByParent(
                        new Pose(parentTransform.Position, parentTransform.Orientation),
                        new Pose(currentTransform.Position, currentTransform.Orientation));
                }
            }
        }

        /// <summary>
        /// Get input state of hand or controller, depending
        /// on which one is active.
        /// </summary>
        /// <param name="isHandTracked">If hand is active or not.</param>
        /// <param name="inputTrackingPosition">Input tracking position.</param>
        /// <param name="inputTrackingRotation">Input tracking rotation.</param>
        /// <param name="isLeftSide">If left side or not.</param>
        public static void ProvideInputTrackingState(
            out bool isHandTracked,
            out Vector3 inputTrackingPosition,
            out Quaternion inputTrackingRotation,
            bool isLeftSide)
        {
            OVRPlugin.HandState handState = new OVRPlugin.HandState();
            if (OVRPlugin.GetHandState(OVRPlugin.Step.Render,
                    isLeftSide ? OVRPlugin.Hand.HandLeft : OVRPlugin.Hand.HandRight, ref handState))
            {
                isHandTracked = true;
                inputTrackingPosition = handState.PointerPose.Position.FromFlippedZVector3f();
                inputTrackingRotation = handState.PointerPose.Orientation.FromFlippedZQuatf();
            }
            else
            {
                isHandTracked = false;
                inputTrackingPosition =
                    OVRInput.GetLocalControllerPosition(isLeftSide
                        ? OVRInput.Controller.LTouch
                        : OVRInput.Controller.RTouch);
                inputTrackingRotation =
                    OVRInput.GetLocalControllerRotation(isLeftSide
                        ? OVRInput.Controller.LTouch
                        : OVRInput.Controller.RTouch);
                var toOpenXRPos = inputTrackingPosition;
                var toOpenXRQuat = inputTrackingRotation;
                inputTrackingPosition = new Vector3(toOpenXRPos.x, toOpenXRPos.y, toOpenXRPos.z);
                inputTrackingRotation = new Quaternion(toOpenXRQuat.x, toOpenXRQuat.y, toOpenXRQuat.z, toOpenXRQuat.w);
            }
        }

        /// <summary>
        /// Returns center eye transform.
        /// </summary>
        /// <returns>Center eye.</returns>
        public static Transform GetCenterEyeTransform()
        {
            if (_ovrCameraRig == null)
            {
                _ovrCameraRig = Object.FindAnyObjectByType<OVRCameraRig>();
            }

            if (_ovrCameraRig == null)
            {
                Debug.LogError("OVRCameraRig is null; one needs to be present in the scene for the center eye.");
                return null;
            }

            return _ovrCameraRig.centerEyeAnchor;
        }

        /// <summary>
        /// Returns tracking space transform.
        /// </summary>
        /// <returns>Tracking space transform.</returns>
        public static Transform GetTrackingSpaceTransform()
        {
            if (_ovrCameraRig == null)
            {
                _ovrCameraRig = Object.FindAnyObjectByType<OVRCameraRig>();
            }

            if (_ovrCameraRig == null)
            {
                Debug.LogError("OVRCameraRig is null; one needs to be present in the scene for the anchor.");
                return null;
            }

            return _ovrCameraRig.trackingSpace;
        }

        private static (Vector3, Quaternion) TransformPositionAndRotationToWorld(
            Transform worldTransform,
            Vector3 position,
            Quaternion rotation)
        {
            position = worldTransform.TransformPoint(position);
            rotation = worldTransform.rotation * rotation;
            return (position, rotation);
        }

        private static void MultiplyPoses(in Pose a, in Pose b, ref Pose result)
        {
            result.position = a.position + a.rotation * b.position;
            result.rotation = a.rotation * b.rotation;
        }

        private static NativeArray<OVRPlugin.Quatf> GetBoneRotations(OVRPlugin.Quatf[] boneRotations)
        {
            unsafe
            {
                var rotations = new NativeArray<OVRPlugin.Quatf>(boneRotations.Length, TempJob,
                    UninitializedMemory);
                fixed (void* boneRotationsPtr = boneRotations)
                {
                    UnsafeUtility.MemCpy(NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(rotations),
                        boneRotationsPtr, boneRotations.Length * (long)UnsafeUtility.SizeOf<OVRPlugin.Quatf>());
                }

                return rotations;
            }
        }

        private static NativeArray<OVRPlugin.Vector3f> GetBoneTranslations(OVRPlugin.Vector3f[] boneTranslations)
        {
            unsafe
            {
                var translations = new NativeArray<OVRPlugin.Vector3f>(boneTranslations.Length, TempJob,
                    UninitializedMemory);
                fixed (void* boneTranslationsPtr = boneTranslations)
                {
                    UnsafeUtility.MemCpy(NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(translations),
                        boneTranslationsPtr,
                        boneTranslations.Length * (long)UnsafeUtility.SizeOf<OVRPlugin.Vector3f>());
                }

                return translations;
            }
        }

#if ISDK_DEFINED
        /// <summary>
        /// Gets the world pose of an ISDK hand joint.
        /// </summary>
        /// <param name="hand">The IHand interface to get joint data from.</param>
        /// <param name="jointId">The hand joint ID to get the position for.</param>
        /// <param name="bodyJointId">The body joint ID corresponding to the hand joint ID.</param>
        /// <param name="cameraRig">Optional camera rig for coordinate transformation.</param>
        /// <param name="worldPosition">Output world position of the joint.</param>
        /// <returns>True if the joint position was successfully retrieved, false otherwise.</returns>
        public static bool GetInteractionHandJointWorldPosition(
            IHand hand,
            HandJointId jointId,
            BodyJointId bodyJointId,
            OVRCameraRig cameraRig,
            out Vector3 worldPosition)
        {
            worldPosition = Vector3.zero;

            if (hand is not { IsTrackedDataValid: true })
            {
                return false;
            }

            // Get the joint pose from the hand
            hand.GetJointPose(jointId, out var iSDKPose);

#if ISDK_78_OR_NEWER || ISDK_OPENXR_HAND
            // Apply OpenXR to OVR conversion if needed
            if (OVRPlugin.HandSkeletonVersion == OVRHandSkeletonVersion.OpenXR)
            {
                ConvertOpenXRHandToOvrHand(bodyJointId, ref iSDKPose);
            }
#endif

            // Transform the position using camera rig if available
            worldPosition = cameraRig?.transform.InverseTransformPoint(iSDKPose.position) ?? iSDKPose.position;
            return true;
        }

        /// <summary>
        /// Converts OpenXR hand pose to OVR hand pose by applying appropriate rotation offsets.
        /// </summary>
        /// <param name="bodyJointId">The body joint ID to determine which hand and rotation offset to apply.</param>
        /// <param name="pose">The pose to convert (modified in place).</param>
        public static void ConvertOpenXRHandToOvrHand(BodyJointId bodyJointId, ref Pose pose)
        {
            switch (bodyJointId)
            {
                case BodyJointId.Body_LeftHandWrist:
                    pose.rotation *= _openXRLeftHandRotOffset;
                    break;
                case BodyJointId.Body_RightHandWrist:
                    pose.rotation *= _openXRRightHandRotOffset;
                    break;
                case > BodyJointId.Body_LeftHandWrist and < BodyJointId.Body_LeftHandLittleTip:
                    pose.rotation *= _openXRLeftHandRotOffset;
                    break;
                case > BodyJointId.Body_RightHandWrist and < BodyJointId.Body_RightHandLittleTip:
                    pose.rotation *= _openXRRightHandRotOffset;
                    break;
            }
        }
#endif
    }
}
