// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Meta.XR.Movement.FaceTracking.Samples
{
    /// <summary>
    /// Sparse matrix implementation of <see cref="Matrix"/>.
    /// </summary>
    public class SparseMatrix : Matrix
    {
        private struct Data
        {
            public int Row, Column;
            public float Value;
        }
        private readonly List<Data> _data = new();

        private readonly int _rows;
        private readonly int _cols;
        /// <inheritdoc />
        public override int Rows => _rows;
        /// <inheritdoc />
        public override int Cols => _cols;

        /// <summary>
        /// Constructor that can be initialized from an instance of
        /// <see cref="DenseMatrix"/>. A threshold parameter is used
        /// to add items from the dense matrix.
        /// </summary>
        /// <param name="m"><see cref="DenseMatrix"/> to copy from.</param>
        /// <param name="threshold">Threshold value.</param>
        public SparseMatrix(DenseMatrix m, float threshold = 1e-3f)
        {
            _cols = m.Cols;
            _rows = m.Rows;

            for (var row = 0; row < _rows; row++)
            {
                for (var col = 0; col < _cols; col++)
                {
                    var val = m[row, col];
                    if (Mathf.Abs(val) > threshold)
                    {
                        _data.Add(new Data() { Row = row, Column = col, Value = val });
                    }
                }
            }
        }

        /// <summary>
        /// Slow linear search - only usable for tests.
        /// </summary>
        /// <param name="row">Row index.</param>
        /// <param name="col">Column index.</param>
        /// <returns></returns>
        protected override float GetElement(int row, int col) =>
            _data.FirstOrDefault((Data d) => d.Row == row && d.Column == col).Value;

        protected override void MultVectWithMatrix(IList<float> rowVector, IList<float> result)
        {
            Debug.Assert(rowVector.Count == this.Rows);
            Debug.Assert(result.Count == this.Cols);

            for (var i = 0; i < result.Count; i++)
            {
                result[i] = 0.0f;
            }

            foreach (var t in _data)
            {
                result[t.Column] += rowVector[t.Row] * t.Value;
            }
        }
    }
}