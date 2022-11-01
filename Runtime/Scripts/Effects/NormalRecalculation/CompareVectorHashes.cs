// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Movement.Effects
{
    /// <summary>
    /// Generates a bunch of vertices that are close to each other.
    /// Compares how hash sets consolidate the two. One is based on
    /// Unity's generic Vector3 hash function while another is based on
    /// a fancier VertexKey structure. Unity's can be found here:
    /// https://github.com/Unity-Technologies/UnityCsReference/blob/master/Runtime/Export/Math/Vector3.cs#L229
    /// </summary>
    public class CompareVectorHashes : MonoBehaviour
    {
        /// <summary>
        /// Number of vertices to test with hashes.
        /// </summary>
        [SerializeField]
        [Tooltip(CompareVectorHashesTooltips.NumVerticesToTest)]
        protected int _numVerticesToTest = 10000;
        /// <summary>
        /// Margin of error used to generate random vertex positions.
        /// The closer in value, the more stringent the test.
        /// </summary>
        [SerializeField]
        [Tooltip(CompareVectorHashesTooltips.MarginOfError)]
        protected float _marginOfError = 0.0001f;

        /// <summary>
        /// Test hashes against each other and log output.
        /// </summary>
        public void CompareHashesAgainstEachOther()
        {
            HashSet<Vector3> plainVectorHashSet = new HashSet<Vector3>();
            HashSet<VertexKey> vertexKeyHashSet = new HashSet<VertexKey>();

            // generate a bunch of vertices
            Vector3[] randomVerts = new Vector3[_numVerticesToTest];
            for (int i = 0; i < _numVerticesToTest; i++)
            {
                randomVerts[i] = new Vector3(
                    Random.Range(0.0f, _marginOfError),
                    Random.Range(0.0f, _marginOfError),
                    Random.Range(0.0f, _marginOfError));
                plainVectorHashSet.Add(randomVerts[i]);
                vertexKeyHashSet.Add(new VertexKey(randomVerts[i]));
            }

            Debug.Log($"Generated {_numVerticesToTest} random vertices with " +
                $"margin of error {_marginOfError}. Plain hash has {plainVectorHashSet.Count} " +
                $"values, while VertexKey hash has {vertexKeyHashSet.Count} values.");
        }
    }
}
