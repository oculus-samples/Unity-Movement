// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;
using static Meta.XR.Movement.MSDKUtility;

namespace Meta.XR.Movement.Retargeting
{
    public abstract class SkeletonJobs
    {
        /// <summary>
        /// Job that gets a pose.
        /// </summary>
        [BurstCompile]
        public struct GetPoseJob : IJobParallelForTransform
        {
            /// <summary>
            /// Indicates what kind of world space to use.
            /// </summary>
            [ReadOnly]
            public JointType JobJointType;

            /// <summary>
            /// Body poses to write to.
            /// </summary>
            public NativeArray<NativeTransform> BodyPose;

            /// <inheritdoc cref="IJobParallelForTransform.Execute(int, TransformAccess)"/>
            [BurstCompile]
            public void Execute(int index, TransformAccess transform)
            {
                var useWorldSpace = UseWorldSpace(index, JobJointType);

                // TODO: Joint compression works based on the local joint, the parent and the local translation
                // This distance calculation is effectively the length of the localPosition vector.
                // It assumes the joint only rotates.  If world space is used (UseWorldSpace()), then this breaks down.
                // If we need to pack/calculate further data for handling worldposition joints, that needs to be
                // handled in the plugin side (ie - calculate the parent/child joint relationship there).
                // There is no concept of "Length" in the NativeTransform outside of the length of the position vector.
                var pose = BodyPose[index];
                pose.Orientation = useWorldSpace ? transform.rotation : transform.localRotation;
                pose.Position = useWorldSpace ? transform.position : transform.localPosition;
                pose.Scale = transform.localScale;
                BodyPose[index] = pose;
            }

            [BurstCompile]
            private bool UseWorldSpace(int jointIndex, JointType jointType)
            {
                var isRoot = jointIndex == 0;
                var useWorldSpace = jointType switch
                {
                    JointType.WorldSpaceRootOnly => isRoot,
                    JointType.WorldSpaceAllJoints => true,
                    _ => false
                };

                return useWorldSpace;
            }
        }

        /// <summary>
        /// Job that applies a pose.
        /// </summary>
        [BurstCompile]
        public struct ApplyPoseJob : IJobParallelForTransform
        {
            /// <summary>
            /// Body poses to read from.
            /// </summary>
            [ReadOnly]
            public NativeArray<NativeTransform> BodyPose;

            /// <summary>
            /// Rotation only indices.
            /// </summary>
            [ReadOnly]
            public NativeArray<int> RotationOnlyIndices;

            /// <summary>
            /// The root joint index.
            /// </summary>
            [ReadOnly]
            public int RootJointIndex;

            /// <summary>
            /// The hips joint index.
            /// </summary>
            [ReadOnly]
            public int HipsJointIndex;

            /// <summary>
            /// The current rotation index.
            /// </summary>
            public int CurrentRotationIndex;

            /// <inheritdoc cref="IJobParallelForTransform.Execute(int, TransformAccess)"/>
            [BurstCompile]
            public void Execute(int index, TransformAccess transform)
            {
                var bodyPose = BodyPose[index];
                var isRotationOnly = CurrentRotationIndex >= 0 &&
                                     CurrentRotationIndex < RotationOnlyIndices.Length &&
                                     RotationOnlyIndices[CurrentRotationIndex] == index;
                if (isRotationOnly && index != RootJointIndex && index != HipsJointIndex)
                {
                    CurrentRotationIndex++; // Advance to next rotation-only index
                    transform.localRotation = bodyPose.Orientation;
                }
                else
                {
                    transform.SetLocalPositionAndRotation(bodyPose.Position, bodyPose.Orientation);
                }
            }
        }

        /// <summary>
        /// Job that converts world space poses to local space poses.
        /// </summary>
        [BurstCompile]
        public struct ConvertWorldToLocalPoseJob : IJob
        {
            /// <summary>
            /// The index of the root joint in the skeleton.
            /// </summary>
            [ReadOnly]
            public int RootJointIndex;

            /// <summary>
            /// The index of the hips joint in the skeleton.
            /// </summary>
            [ReadOnly]
            public int HipsJointIndex;

            /// <summary>
            /// The optional scale on the hips.
            /// </summary>
            [ReadOnly]
            public Vector3 HipsScale;

            /// <summary>
            /// The retargeting behaviour.
            /// </summary>
            [ReadOnly]
            public RetargetingBehavior RetargetingBehavior;

            /// <summary>
            /// Array of parent indices for each joint in the skeleton.
            /// </summary>
            [ReadOnly]
            public NativeArray<int> ParentIndices;

            /// <summary>
            /// Array of world space poses to convert.
            /// </summary>
            [ReadOnly]
            public NativeArray<NativeTransform> WorldPose;

            /// <summary>
            /// Array of local space poses in T-Pose.
            /// </summary>
            [ReadOnly]
            public NativeArray<NativeTransform> LocalTPose;

            /// <summary>
            /// Output array for the converted local space poses.
            /// </summary>
            [WriteOnly]
            public NativeArray<NativeTransform> LocalPose;

            /// <summary>
            /// Executes the job to convert world space poses to local space poses.
            /// </summary>
            public void Execute()
            {
                var rootScale = WorldPose[RootJointIndex].Scale;
                var inverseRootScale = rootScale.Reciprocal();
                var inverseHipsScale = HipsScale.Reciprocal();

                for (var i = 0; i < WorldPose.Length; i++)
                {
                    var pose = WorldPose[i];
                    var parentIndex = ParentIndices[i];
                    if (i <= RootJointIndex)
                    {
                        pose.Position = Vector3.Scale(pose.Position, inverseRootScale);
                    }
                    else if (i > RootJointIndex)
                    {
                        var parentPose = WorldPose[parentIndex];
                        var localPosition = Quaternion.Inverse(parentPose.Orientation) *
                                            (pose.Position - parentPose.Position);
                        var localRotation = Quaternion.Inverse(parentPose.Orientation) * pose.Orientation;
                        if (RetargetingBehavior == RetargetingBehavior.RotationOnlyNoScaling)
                        {
                            localPosition = LocalTPose[i].Position;
                            if (i == HipsJointIndex)
                            {
                                localPosition.y -= WorldPose[RootJointIndex].Position.y;
                            }
                        }
                        else
                        {
                            localPosition = Vector3.Scale(localPosition, inverseRootScale);
                        }

                        if (i != HipsJointIndex && parentIndex != RootJointIndex)
                        {
                            localPosition = Vector3.Scale(localPosition, inverseHipsScale);
                        }

                        pose.Position = localPosition;
                        pose.Orientation = localRotation;
                    }

                    LocalPose[i] = pose;
                }
            }
        }

        /// <summary>
        /// Job that converts local space poses to world space poses.
        /// </summary>
        [BurstCompile]
        public struct ConvertLocalToWorldPoseJob : IJob
        {
            /// <summary>
            /// Array of local space poses to convert.
            /// </summary>
            [ReadOnly]
            public NativeArray<NativeTransform> LocalPose;

            /// <summary>
            /// Array of parent indices for each joint in the skeleton.
            /// </summary>
            [ReadOnly]
            public NativeArray<int> ParentIndices;

            /// <summary>
            /// The index of the root joint.
            /// </summary>
            [ReadOnly]
            public int RootIndex;

            /// <summary>
            /// The index of the hips joint.
            /// </summary>
            [ReadOnly]
            public int HipsIndex;

            /// <summary>
            /// The scale of the root joint to apply to the skeleton.
            /// </summary>
            [ReadOnly]
            public Vector3 RootScale;

            /// <summary>
            /// The scale of the hips joint to apply to the skeleton.
            /// </summary>
            [ReadOnly]
            public Vector3 HipsScale;

            /// <summary>
            /// Root position.
            /// </summary>
            [ReadOnly]
            public Vector3 RootPosition;

            /// <summary>
            /// Root rotation.
            /// </summary>
            [ReadOnly]
            public Quaternion RootRotation;

            /// <summary>
            /// Output array for the converted world space poses.
            /// </summary>
            public NativeArray<NativeTransform> WorldPose;

            public void Execute()
            {
                // Process joints in order (parents before children)
                for (var i = 0; i < LocalPose.Length; i++)
                {
                    var parentIndex = ParentIndices[i];

                    if (i == RootIndex)
                    {
                        // Root joint - local is already world
                        WorldPose[i] = new NativeTransform(
                            RootRotation * LocalPose[i].Orientation,
                            RootPosition + RootRotation * Vector3.Scale(LocalPose[i].Position, RootScale),
                            LocalPose[i].Scale);
                    }
                    else if (i > RootIndex)
                    {
                        var parentWorldTransform = WorldPose[parentIndex];
                        var scale = RootScale;
                        if (i > HipsIndex)
                        {
                            scale = Vector3.Scale(scale, HipsScale);
                        }

                        var localTransform = LocalPose[i];
                        var worldRotation = parentWorldTransform.Orientation * localTransform.Orientation;
                        var worldPosition = parentWorldTransform.Position +
                                            parentWorldTransform.Orientation *
                                            Vector3.Scale(localTransform.Position, scale);
                        WorldPose[i] = new NativeTransform(worldRotation, worldPosition);
                    }
                }
            }
        }
    }
}
