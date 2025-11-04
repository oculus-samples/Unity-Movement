// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using static Meta.XR.Movement.MSDKUtility;

namespace Meta.XR.Movement.Retargeting.IK
{
    public static class IKUtilities
    {
        private static Pose[] _cachedCCdPosesLocal;
        private static Pose[] _cachedOldPoses;
        private static Pose[] _cachedFabrikPoses;
        private static Quaternion[] _fabrikRotChanges;

        /// <summary>
        /// Runs FABRIK, or Forward And Backward Reaching Inverse Kinematics,
        /// algorithm on joints so that the end effector moves to the desired
        /// target, and its children move with it. The joints can be an IK chain
        /// representing a finger, where the tip moves toward the target and the
        /// joints behind it are translated as well. Rotations are solved for.
        /// From: Aristidou A, Lasenby J. FABRIK: A fast, iterative solver for
        /// the Inverse Kinematics problem. Graphical Models 2011; 73(5): 243-260.
        /// </summary>
        /// <param name="joints">Joint transforms.</param>
        /// <param name="distanceToNextJoint">Distances from current joint to following.</param>
        /// <param name="target">Target position.</param>
        /// <param name="targetTolerance">If target is reachable, the max distance between end
        /// effector and target.</param>
        /// <param name="maxIterations">Max iterations to run.</param>
        /// <returns>True if successful, false if not.</returns>
        public static bool SolveFABRIK(
            Transform[] joints,
            float[] distanceToNextJoint,
            Vector3 target,
            float targetTolerance,
            int maxIterations = 10,
            bool solveRotations = false)
        {
            if (joints.Length < 3)
            {
                Debug.LogError("Please run FABRIK on at least three joints.");
                return false;
            }

            int numPoses = joints.Length;
            bool targetIsUnreachable = IsTargetUnreachable(target, joints,
                distanceToNextJoint);

            CachePoses(joints, ref _cachedOldPoses, ref _cachedFabrikPoses,
                ref _fabrikRotChanges);
            RunFABRIK(targetIsUnreachable, distanceToNextJoint,
                target, targetTolerance, _cachedFabrikPoses, maxIterations);
            if (solveRotations)
            {
                FixJointRotations(joints, _cachedOldPoses, _cachedFabrikPoses, _fabrikRotChanges);
            }
            else
            {
                WritePosesToJoints(joints, _cachedFabrikPoses);
            }
            return WasFABRIKSuccessful(targetTolerance, target, joints);
        }

        private static bool IsTargetUnreachable(Vector3 target, Transform[] joints,
            float[] distanceToNextJoint)
        {
            var distanceRootAndTarget = (target - joints[0].position).magnitude;
            float sumOfAllJointDistances = 0.0f;
            foreach (var jointDistance in distanceToNextJoint)
            {
                sumOfAllJointDistances += jointDistance;
            }
            return distanceRootAndTarget > sumOfAllJointDistances;
        }

        private static void CachePoses(Transform[] joints, ref Pose[] cachedPoses,
            ref Pose[] finalPoses, ref Quaternion[] rotationalChanges)
        {
            int numJoints = joints.Length;
            if (cachedPoses == null || cachedPoses.Length != numJoints)
            {
                cachedPoses = new Pose[numJoints];
                finalPoses = new Pose[numJoints];
                rotationalChanges = new Quaternion[numJoints];
            }
            for (int i = 0; i < numJoints; i++)
            {
                var currentJoint = joints[i];
                cachedPoses[i] = new Pose(currentJoint.position, currentJoint.rotation);
                finalPoses[i] = cachedPoses[i];
                rotationalChanges[i] = Quaternion.identity;
            }
        }

        private static void RunFABRIK(
            bool targetIsUnreachable,
            float[] distanceToNextJoint,
            Vector3 target,
            float targetTolerance,
            Pose[] fabrikPoses,
            int maxIterations = 10)
        {
            int numJoints = fabrikPoses.Length;
            if (targetIsUnreachable)
            {
                for (int i = 0; i < numJoints - 1; i++)
                {
                    var currentJointPosition = fabrikPoses[i].position;
                    // find the distance between the target and the current joint
                    float jointToTargetDist = (target - currentJointPosition).magnitude;
                    float segmentDistanceOverTarget =
                        distanceToNextJoint[i] / jointToTargetDist;
                    // lerp next position toward to target
                    fabrikPoses[i + 1].position =
                        Vector3.Lerp(currentJointPosition, target,
                            segmentDistanceOverTarget);
                }
            }
            else
            {
                // target is reachable, set initial position of
                // first joint
                Vector3 firstJointPos = fabrikPoses[0].position;
                float toleranceSquared = targetTolerance * targetTolerance;
                float diffEndTargetSqr = (fabrikPoses[numJoints - 1].position - target).sqrMagnitude;
                int numIterationsSoFar = 0;
                while (diffEndTargetSqr > toleranceSquared &&
                    numIterationsSoFar < maxIterations)
                {
                    // forward reaching stage.
                    // first set end position to target. the points before
                    // will act like they are reaching toward the target.
                    fabrikPoses[numJoints - 1].position = target;
                    for (int i = numJoints - 2; i >= 0; i--)
                    {
                        var nextPosition = fabrikPoses[i + 1].position;
                        var oldNextPosition = nextPosition;
                        var oldCurrentPosition = fabrikPoses[i].position;
                        float distanceBetweenJoints =
                            (oldCurrentPosition - nextPosition).magnitude;
                        // what fraction is old distance to new distance?
                        float distanceRatio = distanceToNextJoint[i] / distanceBetweenJoints;
                        // re-calculate current point so that matches up with next
                        // point. remember that initially, the end effector is set to the
                        // target.
                        fabrikPoses[i].position = Vector3.Lerp(nextPosition,
                            oldCurrentPosition, distanceRatio);
                        fabrikPoses[i + 1].position = oldNextPosition;
                    }

                    // backward reaching, set p[0] to original
                    // the points after will be corrected so that they reach toward
                    // the first point.
                    fabrikPoses[0].position = firstJointPos;
                    for (int i = 0; i < numJoints - 1; i++)
                    {
                        var oldNextPosition = fabrikPoses[i + 1].position;
                        var currentPosition = fabrikPoses[i].position;
                        float distanceToNext =
                            (oldNextPosition - currentPosition).magnitude;
                        float distanceRatio = distanceToNextJoint[i] / distanceToNext;
                        // recalculate next point such that is lies in line between
                        // current point and the next point. Remember that initially,
                        // the first point is set to the beginning of the IK chain.
                        fabrikPoses[i + 1].position = Vector3.Lerp(currentPosition,
                            oldNextPosition, distanceRatio);
                    }
                    diffEndTargetSqr = (fabrikPoses[numJoints - 1].position - target).sqrMagnitude;
                    numIterationsSoFar++;
                }
            }
        }

        private static bool WasFABRIKSuccessful(float targetTolerance,
            Vector3 target, Transform[] joints)
        {
            float toleranceSquared = targetTolerance * targetTolerance;
            int numJoints = joints.Length;
            float diffEndTargetSqr = (joints[numJoints - 1].position - target).sqrMagnitude;
            bool differenceToTargetPasses = diffEndTargetSqr < toleranceSquared;
            return differenceToTargetPasses;
        }

        private static void WritePosesToJoints(Transform[] joints,
            Pose[] cachedFabrikPoses)
        {
            int numJoints = joints.Length;
            for (int i = 0; i < numJoints; i++)
            {
                joints[i].SetPositionAndRotation(cachedFabrikPoses[i].position,
                    cachedFabrikPoses[i].rotation);
            }
        }

        private static void FixJointRotations(Transform[] joints,
            Pose[] oldPoses, Pose[] cachedFABRIKPoses, Quaternion[] rotationDeltas)
        {
            int lastJointIndex = oldPoses.Length - 1;
            // we don't want to fix the rotation of the last joint, just
            // the joints that come before it.
            for (int i = 0; i < lastJointIndex; i++)
            {
                var oldJointPosition = oldPoses[i].position;
                var oldJointTargetPosition = oldPoses[i + 1].position;

                var newJointPosition = cachedFABRIKPoses[i].position;
                var newJointTargetPosition = cachedFABRIKPoses[i + 1].position;

                // rotate the joint so that it points toward its new target
                rotationDeltas[i] = GetRotationChange(
                    oldJointPosition, oldJointTargetPosition,
                    newJointPosition, newJointTargetPosition);
            }
            rotationDeltas[lastJointIndex] = Quaternion.identity;

            // Affect joints by rotation deltas computed.
            for (int i = 0; i <= lastJointIndex; i++)
            {
                joints[i].position = cachedFABRIKPoses[i].position;
                joints[i].rotation = rotationDeltas[i] * cachedFABRIKPoses[i].rotation;
            }
        }

        /// <summary>
        /// Cyclic Coordinate Descent IK algorithm implementation. This rotates each bone in the chain so
        /// that the effector bone will reach the target position. An example of this usage is rotating the entire arm
        /// so that the specified hand can match the tracked hand position.
        /// </summary>
        /// <param name="bones">The bones in the chain.</param>
        /// <param name="targetPosition">The target position.</param>
        /// <param name="tolerance">The maximum distance allowed between the effector and target position.</param>
        /// <param name="maxIterations">The maximum number of iterations</param>
        /// <returns>True if the IK was successful at positioning the effector at the target position.</returns>
        public static bool SolveCCDIK(
            Transform[] bones,
            Vector3 targetPosition,
            float tolerance,
            float maxIterations)
        {
            if (bones.Length < 2)
            {
                Debug.LogError("Please run CCDIK on at least two joints.");
                return false;
            }
            if (_cachedCCdPosesLocal == null || _cachedCCdPosesLocal.Length != bones.Length)
            {
                _cachedCCdPosesLocal = new Pose[bones.Length];
            }
            var parentBone = bones[^1].parent;
            var rootPosition = parentBone.position;
            var rootRotation = parentBone.rotation;
            for (var i = bones.Length - 1; i >= 0; i--)
            {
                var bone = bones[i];
                _cachedCCdPosesLocal[i] = new Pose(parentBone.InverseTransformPoint(bone.position),
                    Quaternion.Inverse(parentBone.rotation) * bone.rotation);
                parentBone = bone;
            }

            const int effectorIndex = 0;
            var sqrTolerance = float.MaxValue;
            var effectorPosePosition =
                GetBoneWorldPosition(_cachedCCdPosesLocal, rootPosition, rootRotation, effectorIndex);
            var iterations = 0;
            while (sqrTolerance > tolerance && iterations < maxIterations)
            {
                for (int i = 1; i < _cachedCCdPosesLocal.Length; i++)
                {
                    var pose = _cachedCCdPosesLocal[i];
                    var poseWorldPosition = GetBoneWorldPosition(_cachedCCdPosesLocal, rootPosition, rootRotation, i);
                    var poseWorldRotation = GetBoneWorldRotation(_cachedCCdPosesLocal, rootRotation, i);
                    // This is the target world rotation.
                    var newPoseRotation = GetBoneDeltaRotationWithEffectorTowardGoal(
                        effectorPosePosition, poseWorldPosition, targetPosition) * poseWorldRotation;
                    // Convert to local rotation.
                    var localPoseRotation =
                        Quaternion.Inverse(GetBoneWorldRotation(_cachedCCdPosesLocal, rootRotation, i + 1)) *
                        newPoseRotation;
                    pose.rotation = localPoseRotation;
                    _cachedCCdPosesLocal[i] = pose;
                    // Update effector position after rotation change
                    effectorPosePosition =
                        GetBoneWorldPosition(_cachedCCdPosesLocal, rootPosition, rootRotation, effectorIndex);
                    // Check tolerance after each bone update
                    sqrTolerance = (effectorPosePosition - targetPosition).sqrMagnitude;
                    if (sqrTolerance <= tolerance)
                    {
                        ApplyPosesToTransforms(_cachedCCdPosesLocal, bones);
                        return true;
                    }
                }

                iterations++;
            }
            ApplyPosesToTransforms(_cachedCCdPosesLocal, bones);
            return false;
        }

        public static bool SolveCCDIKLocalNativeArray(
            Pose chainParentWorldSpace,
            NativeArray<NativeTransform> bonesLocalSpace,
            Vector3 targetPositionWorld,
            float tolerance,
            float maxIterations)
        {
            if (bonesLocalSpace.Length < 2)
            {
                Debug.LogError("Please run CCDIK on at least two joints.");
                return false;
            }

            var rootPosition = chainParentWorldSpace.position;
            var rootRotation = chainParentWorldSpace.rotation;

            const int endEffectorIndex = 0;
            var endEffectorPosition =
                GetBoneWorldPosition(bonesLocalSpace, rootPosition, rootRotation, endEffectorIndex);
            var sqrTolerance = (endEffectorPosition - targetPositionWorld).sqrMagnitude;
            var iterations = 0;
            while (sqrTolerance > tolerance && iterations < maxIterations)
            {
                for (int i = 1; i < bonesLocalSpace.Length; i++)
                {
                    var boneTransform = bonesLocalSpace[i];
                    var poseWorldPosition = GetBoneWorldPosition(bonesLocalSpace, rootPosition, rootRotation, i);
                    var poseWorldRotation = GetBoneWorldRotation(bonesLocalSpace, rootRotation, i);
                    // This is the target world rotation.
                    var newPoseRotation = GetBoneDeltaRotationWithEffectorTowardGoal(
                        endEffectorPosition, poseWorldPosition, targetPositionWorld) * poseWorldRotation;
                    // Convert to local rotation. We need to get the next bone in the series, because it is
                    // the parent bone of the current index
                    // (because the bones are arranged from end effector to start bone).
                    var localPoseRotation =
                        Quaternion.Inverse(GetBoneWorldRotation(bonesLocalSpace, rootRotation, i + 1)) *
                        newPoseRotation;
                    boneTransform.Orientation = localPoseRotation;

                    bonesLocalSpace[i] = boneTransform;
                    // Update effector position after rotation change
                    endEffectorPosition =
                        GetBoneWorldPosition(bonesLocalSpace, rootPosition, rootRotation, endEffectorIndex);
                    // See if the effector pose position is within tolerance after each step. Quit
                    // if so.
                    sqrTolerance = (endEffectorPosition - targetPositionWorld).sqrMagnitude;
                    if (sqrTolerance <= tolerance)
                    {
                        return true;
                    }
                }

                iterations++;
            }
            return false;
        }

        /// <summary>
        /// Fix rotation of joint to match new target position.
        /// </summary>
        /// <param name="oldJointPosition">Old joint position.</param>
        /// <param name="oldJointTargetPosition">Old target position.</param>
        /// <param name="newJointPosition">New joint position.</param>
        /// <param name="newTargetPosition">New joint target position.</param>
        /// <returns></returns>
        public static Quaternion GetRotationChange(
            Vector3 oldJointPosition, Vector3 oldJointTargetPosition,
            Vector3 newJointPosition, Vector3 newTargetPosition)
        {
            Vector3 boneToOldTarget = oldJointTargetPosition - oldJointPosition;
            Vector3 boneToNewTarget = newTargetPosition - newJointPosition;
            return Quaternion.FromToRotation(boneToOldTarget, boneToNewTarget);
        }

        /// <summary>
        /// Gets the rotation of a bone toward the goal with an effector.
        /// </summary>
        /// <param name="effectorPosition">The effector position.</param>
        /// <param name="bonePosition">The bone position.</param>
        /// <param name="goalPosition">The goal position.</param>
        /// <returns>The desired delta bone rotation to be applied.</returns>
        private static Quaternion GetBoneDeltaRotationWithEffectorTowardGoal(
            Vector3 effectorPosition, Vector3 bonePosition, Vector3 goalPosition)
        {
            var boneToEffector = effectorPosition - bonePosition;
            var boneToGoal = goalPosition - bonePosition;
            return Quaternion.FromToRotation(boneToEffector, boneToGoal);
        }

        private static Vector3 GetBoneWorldPosition(Pose[] poses, Vector3 rootPosition, Quaternion rootRotation,
            int boneIndex)
        {
            var position = rootPosition;
            var rotation = rootRotation;
            for (var i = poses.Length - 1; i >= boneIndex; i--)
            {
                var pose = poses[i];
                position += rotation * pose.position;
                rotation *= pose.rotation;
            }

            return position;
        }

        private static Vector3 GetBoneWorldPosition(
            NativeArray<NativeTransform> bonesEndEffectorFirst,
            Vector3 rootPosition,
            Quaternion rootRotation,
            int boneIndex)
        {
            var position = rootPosition;
            var rotation = rootRotation;
            for (var i = bonesEndEffectorFirst.Length - 1; i >= boneIndex; i--)
            {
                var currentBone = bonesEndEffectorFirst[i];
                position += rotation * currentBone.Position;
                rotation *= currentBone.Orientation;
            }

            return position;
        }

        private static Quaternion GetBoneWorldRotation(
            Pose[] poses,
            Quaternion rootRotation,
            int boneIndex)
        {
            var rotation = rootRotation;
            for (var i = poses.Length - 1; i >= boneIndex; i--)
            {
                var pose = poses[i];
                rotation *= pose.rotation;
            }

            return rotation;
        }

        private static Quaternion GetBoneWorldRotation(
            NativeArray<NativeTransform> bonesEndEffectorFirst,
            Quaternion rootRotation,
            int boneIndex)
        {
            var rotation = rootRotation;
            for (var i = bonesEndEffectorFirst.Length - 1; i >= boneIndex; i--)
            {
                var currentBone = bonesEndEffectorFirst[i];
                rotation *= currentBone.Orientation;
            }

            return rotation;
        }

        private static void ApplyPosesToTransforms(Pose[] poses, Transform[] bones)
        {
            var parentBone = bones[^1].parent;
            for (var i = bones.Length - 1; i >= 0; i--)
            {
                var bone = bones[i];
                bone.rotation = parentBone.rotation * poses[i].rotation;
                parentBone = bone;
            }
        }
    }
}
