// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Meta.XR.Movement.FaceTracking.Samples
{
    /// <summary>
    /// An abstract matrix class, with a minimal interface required for
    /// face retargeting functionality.
    /// </summary>
    public abstract class Matrix : IEquatable<Matrix>
    {
        /// <summary>
        /// Number of rows in matrix.
        /// </summary>
        public abstract int Rows { get; }
        /// <summary>
        /// Number of columns in matrix.
        /// </summary>
        public abstract int Cols { get; }

        // Equality operator, used in tests only (this implementation is not efficient for sparse matrices).
        protected const float Eps = 1e-5f;

        /// <summary>
        /// Tests for quality between this matrix and another one.
        /// </summary>
        /// <param name="m">Other matrix to compare against.</param>
        /// <returns>Returns true if equal; false if not.</returns>
        public bool Equals(Matrix m) => Equals(m, Eps);
        /// <summary>
        /// Tests for quality between this matrix and another one.
        /// </summary>
        /// <param name="m">Other matrix.</param>
        /// <param name="eps">Epsilon value use for equality.</param>
        /// <returns></returns>
        public bool Equals(Matrix m, float eps)
        {
            if (m == null || Rows != m.Rows || Cols != m.Cols) return false;

            for (var row = 0; row < Rows; ++row)
            {
                for (var col = 0; col < Cols; ++col)
                {
                    if (Mathf.Abs(this[row, col] - m[row, col]) > eps) return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Array accessor.
        /// </summary>
        /// <param name="row">Row.</param>
        /// <param name="col">Column.</param>
        /// <returns></returns>
        public float this[int row, int col]
        {
            get => GetElement(row, col);
            set => SetElement(row, col, value);
        }
        protected abstract float GetElement(int row, int col);

        protected virtual void SetElement(int row, int col, float value)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Applies matrix multiplication with row vector without allocation, since result is
        /// assumed to be preallocated. Implements result = rowVector * m.
        /// </summary>
        /// <param name="rowVector">Row vector.</param>
        /// <param name="m">Matrix.</param>
        /// <param name="result">Result.</param>
        public static void Mult(IList<float> rowVector, Matrix m, IList<float> result)
        {
            Debug.Assert(rowVector.Count == m.Rows);
            Debug.Assert(result.Count == m.Cols);

            m.MultVectWithMatrix(rowVector, result);
        }
        protected abstract void MultVectWithMatrix(IList<float> rowVector, IList<float> result);
    }
}
