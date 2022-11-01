// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace Oculus.Movement.Effects
{
    /// <summary>
    /// Convenient methods that extend the functionality of Matrix.
    /// </summary>
    public static class MatrixUtils
    {
        /// <summary>
        /// Gets world-space rotation of matrix. Uses forward and
        /// up vector to create a look rotation; right vector
        /// of matrix doesn't need to be used.
        /// </summary>
        /// <param name="matrix">Matrix to be evaluated.</param>
        /// <returns>World-space quaternion.</returns>
        public static Quaternion GetRotation(this Matrix4x4 matrix)
        {
            Vector3 forwardVector = matrix.GetColumn(2);
            Vector3 upVector = matrix.GetColumn(1);
            return Quaternion.LookRotation(forwardVector, upVector);
        }

        /// <summary>
        /// Gets world-space position of matrix using last
        /// column of matrix.
        /// </summary>
        /// <param name="matrix">Matrix to be evaluated.</param>
        /// <returns>World-space position.</returns>
        public static Vector3 GetPosition(this Matrix4x4 matrix)
        {
            return matrix.GetColumn(3);
        }

        /// <summary>
        /// Gets scale of matrix using right, up and forward vectors
        /// of matrix.
        /// </summary>
        /// <param name="matrix">Matrix to be evaluated.</param>
        /// <returns>Matrix scale.</returns>
        public static Vector3 GetScale(this Matrix4x4 matrix)
        {
            Vector3 rightVector = matrix.GetColumn(0);
            Vector3 upVector = matrix.GetColumn(1);
            Vector3 forwardVector = matrix.GetColumn(2);
            Vector3 scaleMagnitude = new Vector3(
                rightVector.magnitude,
                upVector.magnitude,
                forwardVector.magnitude
            );

            // We should take sign into account now.
            // If right vector cross product with up does not produce
            // our forward, that means scale's X is negative. Unity's
            // equality tests use an epsilon so we don't need to do that here.
            Vector3 scaleCrossProduct = Vector3.Cross(rightVector, upVector);
            if (scaleCrossProduct != forwardVector)
            {
                scaleMagnitude.x *= -1;
            }

            return scaleMagnitude;
        }
    }
}
