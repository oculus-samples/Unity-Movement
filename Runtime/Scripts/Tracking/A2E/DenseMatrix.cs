// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Meta.XR.Movement.FaceTracking.Samples
{
    /// <summary>
    /// DenseMatrix implementation implements <see cref="Matrix"/>.
    /// </summary>
    public class DenseMatrix : Matrix
    {
        private List<List<float>> _rows;
        private int _cols;

        /// <inheritdoc />
        public override int Rows => _rows.Count;
        /// <inheritdoc />
        public override int Cols => _cols;

        /// <summary>
        /// <see cref="DenseMatrix"/> constructor that creates
        /// an empty rows by columns matrix.
        /// </summary>
        /// <param name="rows">Number of rows.</param>
        /// <param name="cols">Number of columns.</param>
        public DenseMatrix(int rows, int cols)
        {
            _cols = cols;
            _rows = new List<List<float>>();

            for (var row = 0; row < rows; row++)
            {
                _rows.Add(Enumerable.Repeat(0.0f, cols).ToList());
            }
        }

        /// <summary>
        /// <see cref="DenseMatrix"/> constructor that creates
        /// a matrix from a collections of values.
        /// </summary>
        /// <param name="vals">List-of-List of floats.</param>
        public DenseMatrix(List<List<float>> vals)
        {
            _rows = new List<List<float>>();
            _cols = vals.Count > 0 ? vals[0].Count : 0;

            for (var row = 0; row < _rows.Count; row++)
            {
                _rows.Add(vals[row].ToList());
            }
        }

        protected override float GetElement(int row, int col)
        {
            return _rows[row][col];
        }

        protected override void SetElement(int row, int col, float value)
        {
            _rows[row][col] = value;
        }

        /// <summary>
        /// Retrieves row of matrix.
        /// </summary>
        /// <param name="row">Row index.</param>
        /// <returns>Row of matrix.</returns>
        public IList<float> Row(int row) => _rows[row];

        public void SetRow(int row, IList<float> vals)
        {
            Debug.Assert(vals.Count == _rows[row].Count);
            _rows[row] = vals.ToList();
        }

        /// <summary>
        /// Performs matrix inversion using Gaussian Elimination.
        /// Even though this is a rather trivial method, the use-case we have guarantees
        /// that the diagonal of our matrix is always 1, and that matrix is invertible.
        /// The matrix is also very sparse. Under these conditions, Gaussian
        /// Elimination works reasonably well.
        /// </summary>
        public void Invert()
        {
            Debug.Assert(Rows == Cols);

            // Initialise the augmented matrix to identity
            var augmented = new DenseMatrix(Rows, Cols * 2);
            for (var r = 0; r < Rows; ++r)
            {
                for (var c = 0; c < Cols; ++c)
                {
                    augmented[r, c] = this[r, c];
                }

                augmented[r, r + Rows] = 1.0f;
            }

            // Forward elimination to get a triangular matrix
            augmented.ForwardElimination();
            // And normalize the diagonal matrix
            augmented.NormalizeDiagonal();
            // Backwards substitution to get a diagonal matrix
            augmented.BackSubstitution();

            // Extract the result
            for (var r = 0; r < Rows; ++r)
            {
                for (var c = 0; c < Cols; ++c)
                {
                    this[r, c] = augmented[r, c + Rows];
                }
            }
        }

        /// <summary>
        /// Transposes the matrix.
        /// </summary>
        public void Transpose()
        {
            Debug.Assert(Rows == Cols, "Only supporting transpose of square matrices for now.");

            for (var r = 0; r < Rows; ++r)
            {
                for (var c = r + 1; c < Cols; ++c)
                {
                    (this[r, c], this[c, r]) = (this[c, r], this[r, c]);
                }
            }
        }

        private void AddRow(int destRow, int srcRow, float mult)
        {
            Debug.Assert(destRow != srcRow);

            if (Mathf.Abs(mult) < Eps) return;
            for (var c = 0; c < Cols; ++c)
            {
                this[destRow, c] += this[srcRow, c] * mult;
            }
        }

        private void ForwardElimination()
        {
            Debug.Assert(Rows * 2 == Cols);

            for (var r = 1; r < Rows; ++r)
            {
                for (var c = 0; c < r; ++c)
                {
                    AddRow(r, c, -this[r, c] / this[c, c]);
                }
            }
        }

        private void BackSubstitution()
        {
            Debug.Assert(Rows * 2 == Cols);

            for (var r = 0; r < Rows - 1; ++r)
            {
                for (var c = r + 1; c < Rows; ++c)
                {
                    AddRow(r, c, -this[r, c] / this[c, c]);
                }
            }
        }

        private void NormalizeDiagonal()
        {
            Debug.Assert(Rows <= Cols);

            for (var r = 0; r < Rows; ++r)
            {
                var n = this[r, r];
                if (Mathf.Abs(n) < Eps)
                {
                    throw new ArithmeticException("Non-invertible matrix found");
                }

                for (var c = 0; c < Cols; ++c)
                {
                    this[r, c] /= n;
                }
            }
        }

        protected override void MultVectWithMatrix(IList<float> rowVector, IList<float> result)
        {
            Debug.Assert(rowVector.Count == this.Rows);
            Debug.Assert(result.Count == this.Cols);

            for (var c = 0; c < this.Cols; ++c)
            {
                var val = 0f;
                for (var r = 0; r < this.Rows; ++r)
                {
                    val += rowVector[r] * this[r, c];
                }
                result[c] = val;
            }
        }

        /// <summary>
        /// Returns the result of two dense matrices being multiplied together.
        /// </summary>
        /// <param name="m1">First matrix.</param>
        /// <param name="m2">Second matrix.</param>
        /// <returns></returns>
        public static DenseMatrix Mult(DenseMatrix m1, DenseMatrix m2)
        {
            Debug.Assert(m1.Cols == m2.Rows);

            var result = new DenseMatrix(m1.Rows, m2.Cols);
            for (var r = 0; r < m1.Rows; ++r)
            {
                Mult(m1._rows[r], m2, result._rows[r]);
            }

            return result;
        }
    }
}
