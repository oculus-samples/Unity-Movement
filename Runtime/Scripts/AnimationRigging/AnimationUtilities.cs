// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// Provides convenient algorithms to assist
    /// with animation rigging work.
    /// </summary>
    public static class AnimationUtilities
    {
        private static Pose[] _cachedOldPoses;
        private static Pose[] _cachedFabrikPoses;
        private static Quaternion[] _fabrikRotChanges;
        private static Pose[] _cachedCcdPoses;

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
            if (_cachedCcdPoses == null || _cachedCcdPoses.Length != bones.Length)
            {
                _cachedCcdPoses = new Pose[bones.Length];
            }
            var rootPosition = bones[^1].parent.position;
            var rootRotation = bones[^1].parent.rotation;
            for (var i = 0; i < _cachedCcdPoses.Length; i++)
            {
                var bone = bones[i];
                _cachedCcdPoses[i] = new Pose(bone.localPosition, bone.localRotation);
            }

            const int effectorIndex = 0;
            var sqrTolerance = float.MaxValue;
            var effectorPosePosition =
                GetBoneWorldPosition(_cachedCcdPoses, rootPosition, rootRotation, effectorIndex);
            var iterations = 0;
            while (sqrTolerance > tolerance && iterations < maxIterations)
            {
                for (int i = 1; i < _cachedCcdPoses.Length; i++)
                {
                    var pose = _cachedCcdPoses[i];
                    var poseWorldPosition = GetBoneWorldPosition(_cachedCcdPoses, rootPosition, rootRotation, i);
                    var poseWorldRotation = GetBoneWorldRotation(_cachedCcdPoses, rootRotation, i);
                    // This is the target world rotation.
                    var newPoseRotation = GetBoneDeltaRotationWithEffectorTowardGoal(
                        effectorPosePosition, poseWorldPosition, targetPosition) * poseWorldRotation;
                    // Convert to local rotation.
                    var localPoseRotation =
                        Quaternion.Inverse(GetBoneWorldRotation(_cachedCcdPoses, rootRotation, i + 1)) *
                        newPoseRotation;
                    pose.rotation = localPoseRotation;
                    _cachedCcdPoses[i] = pose;
                    // Update effector position after rotation change
                    effectorPosePosition =
                        GetBoneWorldPosition(_cachedCcdPoses, rootPosition, rootRotation, effectorIndex);
                    // Check tolerance after each bone update
                    sqrTolerance = (effectorPosePosition - targetPosition).sqrMagnitude;
                    if (sqrTolerance <= tolerance)
                    {
                        ApplyPosesToTransforms(_cachedCcdPoses, bones);
                        return true;
                    }
                }

                iterations++;
            }

            ApplyPosesToTransforms(_cachedCcdPoses, bones);
            return false;
        }

        /// <summary>
        /// Runs FABRIK, or Forward And Backward Reaching Inverse Kinematics,
        /// algorithm on joints so that the end effector moves to the desired
        /// target, and its children move with it. The joints can be an IK chain
        /// representing a finger, where the tip moves toward the target and the
        /// joints behind it are translated as well. Rotations are solved for.
        /// From: Aristidou A, Lasenby J. FABRIK: A fast, iterative solver for
        /// the Inverse Kinematics problem. Graphical Models 2011; 73(5): 243–260.
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

        private static bool WasFABRIKSuccessful(float targetTolerance,
            Vector3 target, Transform[] joints)
        {
            float toleranceSquared = targetTolerance * targetTolerance;
            int numJoints = joints.Length;
            float diffEndTargetSqr = (joints[numJoints - 1].position - target).sqrMagnitude;
            bool differenceToTargetPasses = diffEndTargetSqr < toleranceSquared;
            return differenceToTargetPasses;
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

        private static Quaternion GetBoneWorldRotation(Pose[] poses, Quaternion rootRotation, int boneIndex)
        {
            var rotation = rootRotation;
            for (var i = poses.Length - 1; i >= boneIndex; i--)
            {
                var pose = poses[i];
                rotation *= pose.rotation;
            }

            return rotation;
        }

        private static void ApplyPosesToTransforms(Pose[] poses, Transform[] bones)
        {
            for (var i = bones.Length - 1; i >= 0; i--)
            {
                bones[i].localRotation = poses[i].rotation;
            }
        }
    }
}
