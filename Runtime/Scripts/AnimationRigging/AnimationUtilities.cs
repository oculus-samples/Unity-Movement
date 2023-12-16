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
