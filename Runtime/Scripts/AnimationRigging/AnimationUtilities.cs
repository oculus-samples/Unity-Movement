// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// Provides convenient algorithms to assist
    /// with animation rigging work.
    /// </summary>
    public static class AnimationUtilities
    {
        private static Vector3[] _cachedFabrikPositions;
        private static Pose[] _cachedFabrikPoses;

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
            float sqrTolerance = float.MaxValue;
            var effector = bones[0];

            int iterations = 1;
            while (sqrTolerance > tolerance && iterations <= maxIterations)
            {
                for (int i = 2; i < bones.Length; i++)
                {
                    for (int j = 1; j <= i; j++)
                    {
                        bones[j].rotation = GetBoneRotationWithEffectorTowardGoal(effector, bones[j], targetPosition);

                        sqrTolerance = (effector.position - targetPosition).sqrMagnitude;

                        if (sqrTolerance <= tolerance)
                        {
                            return true;
                        }
                    }
                }

                sqrTolerance = (effector.position - targetPosition).sqrMagnitude;
                iterations++;
            }

            return false;
        }

        /// <summary>
        /// Runs FABRIK, or Forward And Backward Reaching Inverse Kinematics,
        /// algorithm on joints so that the end effector moves to the desired
        /// target, and its children move with it. The joints can be an IK chain
        /// representing a finger, where the tip moves toward the target and the
        /// joints behind it are translated as well.
        /// From: Aristidou A, Lasenby J. FABRIK: A fast, iterative solver for
        /// the Inverse Kinematics problem. Graphical Models 2011; 73(5): 243–260.
        /// </summary>
        /// <param name="joints">Joint transforms.</param>
        /// <param name="distanceToNextJoint">Distances from current joint to following.</param>
        /// <param name="target">Target position.</param>
        /// <param name="targetTolerance">If target is reachable, the max distance between end
        /// effector and target.</param>
        /// <param name="maxIterations">Max iterations to run.</param>
        /// <returns>True if succesful, false if not.</returns>
        public static bool SolveFABRIK(
            Transform[] joints,
            float[] distanceToNextJoint,
            Vector3 target,
            float targetTolerance,
            int maxIterations = 10,
            bool solveRotations = false)
        {
            bool successful = true;
            Assert.IsTrue(joints.Length >= 3, "Please run FABRIK on at least three joints.");
            CachePositions(joints, ref _cachedFabrikPositions);

            var distanceRootAndTarget = (target - joints[0].position).magnitude;
            float sumOfAllJointDistances = 0.0f;
            foreach (var jointDistance in distanceToNextJoint)
            {
                sumOfAllJointDistances += jointDistance;
            }

            bool targetIsUnreachable = distanceRootAndTarget > sumOfAllJointDistances;
            int numJoints = joints.Length;
            if (targetIsUnreachable)
            {
                for (int i = 0; i < numJoints - 1; i++)
                {
                    // find the distance between the target and the current joint
                    float jointToTargetDist = (target - joints[i].position).magnitude;
                    float segmentDistanceOverTarget =
                        distanceToNextJoint[i] / jointToTargetDist;
                    // lerp next position toward to target
                    var nextJoint = joints[i + 1];
                    nextJoint.position =
                        Vector3.Lerp(joints[i].position, target,
                            segmentDistanceOverTarget);
                }
            }
            else
            {
                // target is reachable, set initial position of
                // first joint
                Vector3 firstJointPos = joints[0].position;
                float toleranceSquared = targetTolerance * targetTolerance;
                float diffEndTargetSqr = (joints[numJoints - 1].position - target).sqrMagnitude;
                int numIterationsSoFar = 0;
                while (diffEndTargetSqr > toleranceSquared &&
                    numIterationsSoFar < maxIterations)
                {
                    // forward reaching stage.
                    // first set end position to target. the points before
                    // will act like they are reaching toward the target.
                    joints[numJoints - 1].position = target;
                    for (int i = numJoints - 2; i >= 0; i--)
                    {
                        var nextPosition = joints[i + 1].position;
                        var oldCurrentPosition = joints[i].position;
                        float distanceBetweenJoints =
                            (oldCurrentPosition - nextPosition).magnitude;
                        // what fraction is old distance to new distance?
                        float distanceRatio = distanceToNextJoint[i] / distanceBetweenJoints;
                        // re-calculate current point so that matches up with next
                        // point. remember that initially, the end effector is set to the
                        // target.
                        joints[i].position = Vector3.Lerp(nextPosition,
                            oldCurrentPosition, distanceRatio);
                    }

                    // backward reaching, set p[0] to original
                    // the points after will be corrected so that they reach toward
                    // the first point.
                    joints[0].position = firstJointPos;
                    for (int i = 0; i < numJoints - 1; i++)
                    {
                        var oldNextPosition = joints[i + 1].position;
                        var currentPosition = joints[i].position;
                        float distanceToNext =
                            (oldNextPosition - currentPosition).magnitude;
                        float distanceRatio = distanceToNextJoint[i] / distanceToNext;
                        // recalculate next point such that is lies in line between
                        // current point and the next point. Remember that initially,
                        // the first point is set to the beginning of the IK chain.
                        joints[i + 1].position = Vector3.Lerp(currentPosition,
                            oldNextPosition, distanceRatio);
                    }
                    diffEndTargetSqr = (joints[numJoints - 1].position - target).sqrMagnitude;
                    numIterationsSoFar++;
                }
                if (diffEndTargetSqr > toleranceSquared)
                {
                    successful = false;
                }
            }

            if (solveRotations)
            {
                CacheFinalPoses(joints, ref _cachedFabrikPoses);
                FixRotationsOfJoints(joints, _cachedFabrikPositions, _cachedFabrikPoses);
            }
            return successful;
        }

        private static void CachePositions(Transform[] joints, ref Vector3[] positions)
        {
            if (positions == null || positions.Length != joints.Length)
            {
                positions = new Vector3[joints.Length];
            }
            for (int i = 0; i < joints.Length; i++)
            {
                positions[i] = joints[i].position;
            }
        }

        private static void CacheFinalPoses(Transform[] joints, ref Pose[] finalPoses)
        {
            if (finalPoses == null || finalPoses.Length != joints.Length)
            {
                finalPoses = new Pose[joints.Length];
            }
            for (int i = 0; i < joints.Length; i++)
            {
                finalPoses[i].position = joints[i].position;
                finalPoses[i].rotation = joints[i].rotation;
            }
        }

        private static void FixRotationsOfJoints(Transform[] joints, Vector3[] oldPositions,
            Pose[] worldPoses)
        {
            int lastJointIndex = joints.Length - 1;
            // we don't want to fix the rotation of the last joint, just
            // the joints that come beofre it.
            for (int i = 0; i < lastJointIndex; i++)
            {
                var oldJointPosition = oldPositions[i];
                var oldJointTargetPosition = oldPositions[i + 1];

                var joint = joints[i];
                var newJointPosition = joint.position;
                var newJointTargetPosition = joints[i + 1].position;

                // rotate the joint so that it points toward its target
                var rotationChange = GetRotationChange(
                    oldJointPosition, oldJointTargetPosition,
                    newJointPosition, newJointTargetPosition);
                joint.rotation = rotationChange * joint.rotation;
                // we want to change the rotation of this joint so that the mesh looks
                // correct, but leave the children (including last joint) unaffected
                // by the rotation of the current joint.
                for (int j = i + 1; j <= lastJointIndex; j++)
                {
                    joints[j].position = worldPoses[j].position;
                    joints[j].rotation = worldPoses[j].rotation;
                }
            }
        }

        private static Quaternion GetRotationChange(
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
        /// <param name="effector">The effector transform.</param>
        /// <param name="bone">The bone transform.</param>
        /// <param name="goalPosition">The goal position.</param>
        /// <returns>The desired bone rotation.</returns>
        public static Quaternion GetBoneRotationWithEffectorTowardGoal(Transform effector, Transform bone, Vector3 goalPosition)
        {
            var effectorPosition = effector.position;
            var bonePosition = bone.position;
            var boneRotation = bone.rotation;

            var boneToEffector = effectorPosition - bonePosition;
            var boneToGoal = goalPosition - bonePosition;

            return Quaternion.FromToRotation(boneToEffector, boneToGoal) * boneRotation;
        }
    }
}
