// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Movement.Effects
{
    /// <summary>
    /// A utility class used to recalculate normals on the GPU or CPU,
    /// if the former can't do it.
    /// </summary>
    public class NormalRecalculator
    {
        private int[] _subMeshVertices;

        /// <summary>
        /// Contains ID of vertex, along with IDs of its neighbors concatenated as a flattened map.
        /// </summary>
        private int[] _vertexIdToTriangleMap;

        /// <summary>
        /// Used to index into the flattened map.
        /// </summary>
        private int[] _vertexIdToTriangleMapOffsets;

        private ComputeBuffer _vertexMetadataCBuffer;
        private ComputeBuffer _subMeshVerticesCBuffer;
        private ComputeBuffer _vertexCBuffer;

        /// <summary>
        /// Used to retrieve baked mesh references without incurring the per-frame GC alloc.
        /// </summary>
        private List<Vector3> _vertsRef = new List<Vector3>();
        private Vector3[] _vertsBuffer;

        /// <summary>
        /// Initialize data that the shader will use. This will
        /// involve flattening the dictionary argument.
        /// </summary>
        /// <param name="subMeshVertexIdToNeighborList">Vertex id to neighbor ids.</param>
        /// <param name="subMeshVerticesSet">Set of vertices in submesh.</param>
        /// <param name="vertexIdToTriangleMapCount">Total number of triangles counted. One index
        /// plus its neighbor count for all triangles.</param>
        /// <param name="bakedMesh">Mesh snapshot obtained from original. This includes all vertices,
        /// not just the sub mesh.</param>
        /// <param name="instantiatedMaterials">Intantiated materials, used to feed.
        /// structured buffers to.</param>
        /// <param name="subMeshIndex">Submesh index to run recalculation on.</param>
        public void Initialize(
            Dictionary<int, List<int>> subMeshVertexIdToNeighborList,
            HashSet<int> subMeshVerticesSet,
            int vertexIdToTriangleMapCount,
            Mesh bakedMesh,
            Material[] instantiatedMaterials,
            int subMeshIndex)
        {
            // Consists of pairs of indices, containing a start and end
            _vertexIdToTriangleMapOffsets = new int[subMeshVerticesSet.Count * 2];
            _vertexIdToTriangleMap = new int[vertexIdToTriangleMapCount];
            int flattenedMapIndex = 0;
            int flattenedMapOffset = 0;

            // Flatten dictionary of vertex ID to neighbors IDs into an array.
            // Since each vertex ID can have any number of neighbors, we need to use
            // an offset array that indexes into it. This offset array consists of
            // pairs, where first item of the pair is the location of a vertex ID in the
            // flattened map. The second item of the pair is the location of the
            // following vertex ID, which would be placed right after the indices
            // of the first vertex's neighbors.
            foreach (KeyValuePair<int, List<int>> kvp in subMeshVertexIdToNeighborList)
            {
                int vertexId = kvp.Key;
                List<int> neighborIndices = kvp.Value;
                _vertexIdToTriangleMapOffsets[flattenedMapOffset++] = flattenedMapIndex;
                _vertexIdToTriangleMap[flattenedMapIndex++] = vertexId;

                // Keep in mind that the list of neighbors will consist of pairs.
                // Where each pair is the other two indices of triangle relative
                // to this vertex ID.
                foreach (int neighborIndex in neighborIndices)
                {
                    _vertexIdToTriangleMap[flattenedMapIndex++] = neighborIndex;
                }

                // Marks start of next triangle
                _vertexIdToTriangleMapOffsets[flattenedMapOffset++] = flattenedMapIndex;
            }

            _subMeshVertices = new int[subMeshVerticesSet.Count];
            subMeshVerticesSet.CopyTo(_subMeshVertices);

            _vertsBuffer = new Vector3[bakedMesh.vertices.Length];

            InitializeShader(bakedMesh, instantiatedMaterials, subMeshIndex);
        }

        /// <summary>
        /// Cleans up resources used by shader for normal recalculation.
        /// </summary>
        public void ReleaseResources()
        {
            if (_vertexMetadataCBuffer != null)
            {
                _vertexMetadataCBuffer.Release();
            }
            if (_subMeshVerticesCBuffer != null)
            {
                _subMeshVerticesCBuffer.Release();
            }
            if (_vertexCBuffer != null)
            {
                _vertexCBuffer.Release();
            }
        }

        private void InitializeShader(Mesh bakedMesh, Material[] instantiatedMaterials, int subMeshIndex)
        {
            // From Unity Docs: (https://docs.unity3d.com/Manual/class-ComputeShader.html)
            // OpenGL ES 3.1 (for (Android, iOS, tvOS platforms) only guarantees support for 4 compute
            // buffers at a time. Actual implementations typically support more, but in general if
            // developing for OpenGL ES, you should consider grouping related data in structs rather
            // than having each data item in its own buffer.
            //
            // This means some buffers will be combined below.

            // Consolidate flatting map, and offset array that is used to
            // index into map.
            var vertexIdMappings = new int[_vertexIdToTriangleMap.Length +
                _vertexIdToTriangleMapOffsets.Length];
            _vertexIdToTriangleMapOffsets.CopyTo(vertexIdMappings, 0);
            _vertexIdToTriangleMap.CopyTo(vertexIdMappings, _vertexIdToTriangleMapOffsets.Length);

            _vertexMetadataCBuffer = new ComputeBuffer(vertexIdMappings.Length, sizeof(int));
            _vertexMetadataCBuffer.SetData(vertexIdMappings);
            foreach (var instantiatedMaterial in instantiatedMaterials)
            {
                instantiatedMaterial.SetBuffer("vertexMetadata", _vertexMetadataCBuffer);
            }

            _subMeshVerticesCBuffer = new ComputeBuffer(_subMeshVertices.Length, sizeof(int));
            _subMeshVerticesCBuffer.SetData(_subMeshVertices);
            foreach (var instantiatedMaterial in instantiatedMaterials)
            {
                instantiatedMaterial.SetBuffer("subsetVertices", _subMeshVerticesCBuffer);
            }

            // Create vertex position buffer
            var vertices = bakedMesh.vertices;
            _vertexCBuffer = new ComputeBuffer(vertices.Length, sizeof(float) * 3);
            _vertexCBuffer.SetData(vertices);
            foreach (var instantiatedMaterial in instantiatedMaterials)
            {
                instantiatedMaterial.SetBuffer("vertices", _vertexCBuffer);
            }

            // Transfer vertexCount to GPU
            var vertexCount = _subMeshVertices.Length;
            foreach (var instantiatedMaterial in instantiatedMaterials)
            {
                instantiatedMaterial.SetInt("vertexCount", vertexCount);
            }

            // In the shader, the vertex ID will be used to index into the flattened vertex meta data.
            // So the beginning of the metadata corresponds to vertex 0, and the indices
            // increase after that. But that could be wrong if the mesh in question is sub mesh index
            // 2. If 100 vertices come before that sub mesh, the vertex ID 100 should be considered
            // to be vertex 0, 101 should vertex 1, and so on. This conversion to proper indices can
            // be done by subtracting the first vertex index from each sub mesh vertex.
            int numCurrentVerts = 0;
            for (int i = 0; i < subMeshIndex; i++)
            {
                numCurrentVerts += bakedMesh.GetSubMesh(i).vertexCount;
            }
            foreach (var instantiatedMaterial in instantiatedMaterials)
            {
                instantiatedMaterial.SetInt("firstSubmeshVertexIndex", numCurrentVerts);
            }
        }

        /// <summary>
        /// Custom normal calculation.  The Unity Mesh.RecalculateNormals creates seams.
        /// See http://schemingdeveloper.com/2014/10/17/better-method-recalculate-normals-unity/
        /// </summary>
        /// <param name="bakedMesh">Mesh to copy data from; feed into mesh that is
        /// rendered with recalculated normals.</param>
        public void CalculateNormals(Mesh bakedMesh)
        {
            bakedMesh.GetVertices(_vertsRef);
            _vertsRef.CopyTo(_vertsBuffer);
            _vertexCBuffer.SetData(_vertsBuffer);
        }
    }
}
