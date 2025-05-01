// Copyright (c) Meta Platforms, Inc. and affiliates.

using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using static Meta.XR.Movement.MSDKUtility;
using static OVRPlugin;
using static OVRSkeleton;

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
        public struct UpdateSourcePoseJob : IJob
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
            public NativeArray<Quatf> InputPoseRotations;

            /// <summary>
            /// Input pose translations to read from.
            /// </summary>
            [ReadOnly]
            public NativeArray<Vector3f> InputPoseTranslations;

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
                    var rotation = Offset.rotation * InputPoseRotations[i].FromFlippedZQuatf();
                    var position = Offset.rotation * InputPoseTranslations[i].FromFlippedZVector3f() + Offset.position;
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
        public struct ComputeLocalFromWorldPosesJob : IJob
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
                Pose result = new Pose();
                Quaternion inverseFromRot = Quaternion.Inverse(fromRotation);
                result.position = inverseFromRot * (toPosition - fromPosition);
                result.rotation = inverseFromRot * toRotation;
                return result;
            }
        }

        private static OVRCameraRig _ovrCameraRig;
        private static NativeArray<NativeTransform> _outputPoses;
        private static BodyState _currentBodyState;
        private static float _timestamp;
        private static SkeletonPoseData _data;

        /// <summary>
        /// Get current body tracking frame data.
        /// </summary>
        /// <param name="dataProvider">Data provider.</param>
        /// <param name="trackerPositionsWorldSpace">Gets tracker positions in world space.</param>
        /// <returns>Latest available <see cref="MSDKUtility.FrameData"/>.</returns>
        public static FrameData GetCurrentFrameData(
            IOVRSkeletonDataProvider dataProvider,
            bool trackerPositionsWorldSpace)
        {
            var skeletonPoseData = dataProvider.GetSkeletonPoseData();
            // provider doesn't give us fidelity, so we have to fetch it ourselves
            BodyState bodyState = new BodyState();
            BodyTrackingFidelity2 fidelity2 = BodyTrackingFidelity2.Low;
            if (!GetBodyState4(Step.Render,
                    dataProvider.GetSkeletonType() == OVRSkeleton.SkeletonType.FullBody
                        ? BodyJointSet.FullBody
                        : BodyJointSet.UpperBody, ref bodyState))
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

            return frameData;
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

        /// <summary>
        /// Gets poses from the tracker. Checks against expected pose count.
        /// </summary>
        /// <param name="expectedPoseCount">Expected pose count.</param>
        /// <param name="dataProvider">Skeleton data provider.</param>
        /// <param name="offset"></param>
        /// <param name="skeletonChangeCount"></param>
        /// <param name="validPoses"></param>
        /// <returns></returns>
        public static NativeArray<NativeTransform> GetPosesFromTheTracker(
            int expectedPoseCount,
            IOVRSkeletonDataProvider dataProvider,
            Pose offset,
            out int skeletonChangeCount,
            out bool validPoses)
        {
            if (Mathf.Abs(Time.time - _timestamp) <= float.Epsilon)
            {
                skeletonChangeCount = _data.SkeletonChangedCount;
                validPoses = _data.IsDataValid;
                return _outputPoses;
            }

            _timestamp = Time.time;
            var allPoses = GetPosesFromTheTracker(dataProvider, offset, ref _currentBodyState);
            validPoses = _data.IsDataValid;
            skeletonChangeCount = _data.SkeletonChangedCount;

            _outputPoses = new NativeArray<NativeTransform>(allPoses.Length, Allocator.Temp);
            _outputPoses.CopyFrom(allPoses);
            allPoses.Dispose();

            if (_outputPoses.Length != expectedPoseCount)
            {
                Debug.LogWarning($"Pose count mismatch! Tracker: {_outputPoses.Length}, " +
                                 $"expected from config: {expectedPoseCount}.");
            }

            // Don't return more poses than requested.
            if (_outputPoses.Length > expectedPoseCount)
            {
                // only provide a subset
                var subsetPoses = new NativeArray<NativeTransform>(expectedPoseCount, Allocator.Temp,
                    NativeArrayOptions.UninitializedMemory);
                for (var i = 0; i < expectedPoseCount; i++)
                {
                    subsetPoses[i] = _outputPoses[i];
                }

                return subsetPoses;
            }

            // If tracker has too few transforms, then provide default transforms for extras
            if (_outputPoses.Length < expectedPoseCount)
            {
                var subsetPoses = new NativeArray<NativeTransform>(expectedPoseCount, Allocator.Temp,
                    NativeArrayOptions.UninitializedMemory);
                for (var i = 0; i < expectedPoseCount; i++)
                {
                    subsetPoses[i] = i >= allPoses.Length
                        ? new NativeTransform(Quaternion.identity, Vector3.zero, Vector3.one)
                        : _outputPoses[i];
                }

                return subsetPoses;
            }

            return _outputPoses;
        }

        /// <summary>
        /// Returns transforms from the tracker.
        /// </summary>
        /// <param name="dataProvider">Data provider to provide transforms.</param>
        /// <param name="offset">Offset to apply.</param>
        /// <param name="bodyState">Body state.</param>
        /// <returns>Tracker poses.</returns>
        public static NativeArray<NativeTransform> GetPosesFromTheTracker(
            IOVRSkeletonDataProvider dataProvider,
            Pose offset,
            ref BodyState bodyState)
        {
            _data = dataProvider.GetSkeletonPoseData();

            if (!_data.IsDataValid)
            {
                // if none available, just provide invalid poses for now so that
                // body snapshot code works
                var numBonesSpec = dataProvider.GetSkeletonType() == OVRSkeleton.SkeletonType.FullBody
                    ? (int)OVRPlugin.BoneId.FullBody_End
                    : (int)OVRPlugin.BoneId.Body_End;

                var invalidPoses = new NativeArray<NativeTransform>(numBonesSpec,
                    Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                for (var i = 0; i < numBonesSpec; i++)
                {
                    invalidPoses[i] = NativeTransform.Identity();
                }

                return invalidPoses;
            }


            var bodyStateBoneCount = 0;
            if (GetBodyState4(Step.Render,
                    dataProvider.GetSkeletonType() == OVRSkeleton.SkeletonType.FullBody
                        ? BodyJointSet.FullBody
                        : BodyJointSet.UpperBody, ref bodyState))
            {
                // TODO: get body state does not set joint set. that should be fixed
                // for now rely on the joint counts returned from the joint set.
                bodyStateBoneCount = bodyState.JointLocations.Length;
            }

            var numPoses = _data.BoneTranslations.Length;
            if (numPoses > bodyStateBoneCount)
            {
                Debug.LogWarning($"The number of valid tracker poses available is {bodyStateBoneCount}, " +
                                 $"less than what is requested ({numPoses}). The remainder will use the first " +
                                 $"joint's transform values.");
            }

            var boneTranslations = GetBoneTranslations(_data.BoneTranslations);
            var boneRotations = GetBoneRotations(_data.BoneRotations);
            var sourcePoses = new NativeArray<NativeTransform>(numPoses, Allocator.TempJob);
            var job = new UpdateSourcePoseJob
            {
                Offset = offset,
                InputPoseRotations = boneRotations,
                InputPoseTranslations = boneTranslations,
                OutputPose = sourcePoses
            };
            job.Schedule().Complete();
            boneTranslations.Dispose();
            boneRotations.Dispose();
            return sourcePoses;
        }

        /// <summary>
        /// Returns the bind pose.
        /// </summary>
        /// <param name="expectedPoseCount">Expected pose count.</param>
        /// <param name="skeleton">The skeleton containing the bind pose data.</param>
        /// <returns></returns>
        public static NativeArray<NativeTransform> GetBindPoses(int expectedPoseCount, ref Skeleton2 skeleton)
        {
            var sourcePoses = new NativeArray<NativeTransform>(expectedPoseCount, Allocator.Temp,
                NativeArrayOptions.UninitializedMemory);
            if (OVRPlugin.GetSkeleton2(OVRPlugin.SkeletonType.FullBody, ref skeleton))
            {
                var numBones = skeleton.Bones.Length;
                for (var i = 0; i < numBones; i++)
                {
                    var skeletonPose = skeleton.Bones[i].Pose;
                    sourcePoses[i] = new NativeTransform(skeletonPose.Orientation.FromFlippedZQuatf(),
                        skeletonPose.Position.FromFlippedZVector3f(), Vector3.one);
                }
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
                Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var parentIndicesNativeArray = new NativeArray<int>(parentIndices.Length,
                Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
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
        public static NativeTransform GetChildTransformAffectedByParent(
            Pose parentPose, Pose childPose)
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
                if (parentIndex < posesToModify.Length - 1)
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
        public static void ProvideInputTrackingState(out bool isHandTracked,
            out Vector3 inputTrackingPosition, out Quaternion inputTrackingRotation,
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

        /// <summary>
        /// Multiplies two poses.
        /// </summary>
        /// <param name="a">First pose.</param>
        /// <param name="b">Second pose.</param>
        /// <param name="result">Resulting pose.</param>
        private static void MultiplyPoses(in Pose a, in Pose b, ref Pose result)
        {
            result.position = a.position + a.rotation * b.position;
            result.rotation = a.rotation * b.rotation;
        }

        private static NativeArray<Quatf> GetBoneRotations(Quatf[] boneRotations)
        {
            unsafe
            {
                var rotations = new NativeArray<Quatf>(boneRotations.Length, Allocator.TempJob,
                    NativeArrayOptions.UninitializedMemory);
                fixed (void* boneRotationsPtr = boneRotations)
                {
                    UnsafeUtility.MemCpy(NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(rotations),
                        boneRotationsPtr, boneRotations.Length * (long)UnsafeUtility.SizeOf<Quatf>());
                }

                return rotations;
            }
        }

        private static NativeArray<Vector3f> GetBoneTranslations(Vector3f[] boneTranslations)
        {
            unsafe
            {
                var translations = new NativeArray<Vector3f>(boneTranslations.Length, Allocator.TempJob,
                    NativeArrayOptions.UninitializedMemory);
                fixed (void* boneTranslationsPtr = boneTranslations)
                {
                    UnsafeUtility.MemCpy(NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(translations),
                        boneTranslationsPtr, boneTranslations.Length * (long)UnsafeUtility.SizeOf<Vector3f>());
                }

                return translations;
            }
        }
    }
}
