// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Movement.Effects
{
    /// <summary>
    /// Uses original skinned mesh renderer to drive a skinned mesh
    /// renderer with recalculated normals.
    /// </summary>
    public class RecalculateNormals : MonoBehaviour
    {
        /// <summary>
        /// Fired when duplicate mesh has been generated.
        /// </summary>
        public System.Action GeneratedDuplicateMesh;

        /// <summary>
        /// The layer of the duplicate mesh with recalculate normals, should
        /// be on a visible layer.
        /// </summary>
        public string DuplicateLayerName { get => _duplicateLayerName; set => _duplicateLayerName = value; }

        /// <summary>
        /// Skinned mesh renderer requiring normal recalc.
        /// </summary>
        [SerializeField]
        [Tooltip(RecalculateNormalsTooltips.SkinnedMeshRenderer)]
        protected SkinnedMeshRenderer _skinnedMeshRenderer;

        /// <summary>
        /// Submesh index to recalc normals on. Sometimes a mesh
        /// might have submeshes that shouldn't need normal recalc,
        /// and for performance reasons only the sub mesh of interest
        /// should be the focus on this script.
        /// </summary>
        [SerializeField]
        [Tooltip(RecalculateNormalsTooltips.SubMesh)]
        protected int _subMesh = 0;

        /// <summary>
        /// Allows using Unity's stock recalc instead.
        /// </summary>
        [SerializeField]
        [Tooltip(RecalculateNormalsTooltips.UseUnityFunction)]
        protected bool _useUnityFunction = false;

        /// <summary>
        /// Allows recalculate normals to be calculated independently on
        /// LateUpdate, instead of being driven from DriveSkeletalLateUpdateLogic.
        /// </summary>
        [SerializeField]
        [Tooltip(RecalculateNormalsTooltips.RecalculateIndependently)]
        protected bool _recalculateIndependently = false;

        /// <summary>
        /// The layer of the duplicate mesh with recalculate normals, should
        /// be on a visible layer.
        /// </summary>
        [SerializeField]
        [Tooltip(RecalculateNormalsTooltips.DuplicateLayerName)]
        protected string _duplicateLayerName = "Character";

        /// <summary>
        /// The layer of original mesh with invalid normals, should be
        /// on invisible layer.
        /// </summary>
        [SerializeField]
        [Tooltip(RecalculateNormalsTooltips.HiddenMeshLayerName)]
        protected string _hiddenMeshLayerName = "HiddenMesh";

        /// <summary>
        /// Index of material that needs meta data for normal recalc,
        /// since that is ultimately done in the vertex stage.
        /// </summary>
        [SerializeField]
        [Tooltip(RecalculateNormalsTooltips.RecalculateMaterialIndices)]
        protected int[] _recalculateMaterialIndices = new int[1];

        private const string _recalculateNormalShaderKeyword = "_RECALCULATE_NORMALS";
        private NormalRecalculator _normalRecalculator = new NormalRecalculator();
        private Mesh _meshSnapshot;
        private Mesh _originalSharedMesh;
        private GameObject _recalcObject;
        private Material[] _instantiatedMaterials;
        private List<Material> _recalculateMaterials = new List<Material>();
        private bool _runRecalculation;

        /// <summary>
        /// Allows toggling this scripts functionality on or off.
        /// </summary>
        public bool RunRecalculation
        {
            get => _runRecalculation;
            set
            {
                _runRecalculation = value;
                if (_runRecalculation)
                {
                    foreach (var recalculateMaterial in _recalculateMaterials)
                    {
                        recalculateMaterial.EnableKeyword(_recalculateNormalShaderKeyword);
                    }
                }
                else
                {
                    foreach (var recalculateMaterial in _recalculateMaterials)
                    {
                        recalculateMaterial.DisableKeyword(_recalculateNormalShaderKeyword);
                    }
                }
                ToggleVisibility(_runRecalculation);
            }
        }

        private void Awake()
        {
            Assert.IsNotNull(_skinnedMeshRenderer);
            Assert.IsTrue(_recalculateMaterialIndices.Length > 0);
            Assert.IsTrue(LayerMask.NameToLayer(_hiddenMeshLayerName) > -1);
            Assert.IsTrue(LayerMask.NameToLayer(_duplicateLayerName) > -1);

            _instantiatedMaterials = _skinnedMeshRenderer.materials;
            foreach (var recalculateMaterialIndex in _recalculateMaterialIndices)
            {
                Assert.IsTrue(recalculateMaterialIndex < _recalculateMaterialIndices.Length);
                _recalculateMaterials.Add(_instantiatedMaterials[recalculateMaterialIndex]);
            }

            if (!_useUnityFunction)
            {
                RunRecalculation = true;
            }
        }

        private void Start()
        {
            _originalSharedMesh = _skinnedMeshRenderer.sharedMesh;

            if (!_useUnityFunction)
            {
                ApplyLayerMask();

                MeshFilter duplicateFilter;
                MeshRenderer duplicateMeshRenderer;
                (duplicateFilter, duplicateMeshRenderer) =
                    GetDuplicateMeshForNormalRecalculation();

                ToggleVisibility(_runRecalculation);

                _meshSnapshot = new Mesh();
                _skinnedMeshRenderer.BakeMesh(_meshSnapshot, true);
                duplicateFilter.mesh = _meshSnapshot;
                duplicateFilter.sharedMesh.RecalculateBounds();
                duplicateMeshRenderer.materials = _instantiatedMaterials;

                if (_subMesh >= _meshSnapshot.subMeshCount)
                {
                    _subMesh = 0;
                }

                int[] subMeshTriangles = _meshSnapshot.GetTriangles(_subMesh);
                Vector3[] allVertices = _meshSnapshot.vertices;
                int[] allTriangles = _meshSnapshot.triangles;
                // Create a mapping from vertex *position* to neighbors. This uses
                // a custom VertexKey to hash similar positions to each other.
                Dictionary<VertexKey, List<int>> subMeshVertexToNeighborList =
                    CreateSubMeshVertexToNeighborMapping(subMeshTriangles,
                        allVertices, allTriangles);

                // The list of neighbors will always be even, since when you form
                // a triangle a vertex will have two neighbors.
                foreach (var neighborsValue in subMeshVertexToNeighborList.Values)
                {
                    Assert.IsTrue(neighborsValue.Count % 2 == 0);
                }

                // Create a mapping to map vertex *ID*s to neighbors. This uses
                // the original -> neighbors mapping to create an ID ->
                // neighbors mapping.
                BuildVertexIdToTriangleAssociation(
                    allVertices,
                    allTriangles,
                    subMeshVertexToNeighborList,
                    out Dictionary<int, List<int>> subMeshVertexIdToNeighborList,
                    out HashSet<int> subMeshVertexIndexSet,
                    out int vertexIdToTriangleMapCount);

                _normalRecalculator.Initialize(
                    subMeshVertexIdToNeighborList,
                    subMeshVertexIndexSet,
                    vertexIdToTriangleMapCount,
                    _meshSnapshot,
                    _recalculateMaterials.ToArray());
            }

            GeneratedDuplicateMesh?.Invoke();
        }

        private void LateUpdate()
        {
            if (!_recalculateIndependently)
            {
                return;
            }
            ApplyNormalRecalculation();
        }

        /// <summary>
        /// Applies normal recalculation to the skinned mesh renderer
        /// </summary>
        public void ApplyNormalRecalculation()
        {
            if (_useUnityFunction)
            {
                _originalSharedMesh.RecalculateNormals();
                _originalSharedMesh.RecalculateTangents();
            }
            else
            {
                if (RunRecalculation)
                {
                    _skinnedMeshRenderer.BakeMesh(_meshSnapshot, true);
                    _normalRecalculator.CalculateNormals(_meshSnapshot);
                }
            }
        }

        private void ApplyLayerMask()
        {
            Camera[] cameras = Resources.FindObjectsOfTypeAll<Camera>();

            // Hide the original gameobject from all cameras
            int invisibleMask = GetHiddenMeshMask();
            foreach (var cam in cameras)
            {
                cam.cullingMask &= ~invisibleMask;
            }

#if UNITY_EDITOR
            UnityEditor.Tools.visibleLayers &= ~invisibleMask;
#endif
        }

        private void ToggleVisibility(bool showNormalRecalculation)
        {
            int hiddenMeshLayer = LayerMask.NameToLayer(_hiddenMeshLayerName);
            int visibleMeshLayer = LayerMask.NameToLayer(_duplicateLayerName);
            if (showNormalRecalculation)
            {
                _skinnedMeshRenderer.gameObject.layer = hiddenMeshLayer;
                if (_recalcObject)
                {
                    _recalcObject.gameObject.layer = visibleMeshLayer;
                }
            }
            else
            {
                _skinnedMeshRenderer.gameObject.layer = visibleMeshLayer;
                if (_recalcObject)
                {
                    _recalcObject.gameObject.layer = hiddenMeshLayer;
                }
            }
        }

        private int GetHiddenMeshMask()
        {
            string[] invisibleLayer = { _hiddenMeshLayerName };
            return LayerMask.GetMask(invisibleLayer);
        }

        /// <summary>
        /// Create a duplicate mesh that will recalculate normals. If we could calculate
        /// normals directly on the skinned mesh post-deformation then we wouldn't need
        /// the duplicate object.
        /// </summary>
        /// <returns>The duplicate mesh filter and renderer.</returns>
        private (MeshFilter, MeshRenderer) GetDuplicateMeshForNormalRecalculation()
        {
            _recalcObject = new GameObject(gameObject.name + "_NormalRecalc");
            _recalcObject.layer = LayerMask.NameToLayer(_duplicateLayerName);
            var meshFilterRecalc = _recalcObject.AddComponent<MeshFilter>();
            var meshRendererRecalc = _recalcObject.AddComponent<MeshRenderer>();
            var recalcTransform = _recalcObject.transform;
            var skinnedMeshTransform = _skinnedMeshRenderer.gameObject.transform;
            recalcTransform.SetParent(skinnedMeshTransform.parent, false);
            recalcTransform.localPosition = skinnedMeshTransform.localPosition;
            recalcTransform.localRotation = skinnedMeshTransform.localRotation;
            recalcTransform.localScale = skinnedMeshTransform.localScale;

            return (meshFilterRecalc, meshRendererRecalc);
        }

        /// <summary>
        /// Create an association between vertices and their neighbors. We use a custom
        /// key for vertices that effectively maps the same vertex positions to the
        /// same key value.
        /// </summary>
        /// <param name="subMeshTriangles">Indices of the vertices that making up the relevant
        /// sub-mesh's triangles.</param>
        /// <param name="allVertices">All vertex positions for all sub-meshes.</param>
        /// <param name="allTriangles">All triangles for all sub-meshes.</param>
        /// <returns>The dictionary with the association between vertices and their neighbors.</returns>
        private Dictionary<VertexKey, List<int>> CreateSubMeshVertexToNeighborMapping(
            int[] subMeshTriangles,
            Vector3[] allVertices,
            int[] allTriangles)
        {
            // For the sub-mesh that we care about, create a list of neighbors
            // that we will add to.
            var subMeshVertexToNeighborList =
                new Dictionary<VertexKey, List<int>>(allVertices.Length);
            for (int i = 0; i < subMeshTriangles.Length; i += 3)
            {
                int i1 = subMeshTriangles[i];
                int i2 = subMeshTriangles[i + 1];
                int i3 = subMeshTriangles[i + 2];

                InitializeListOfIndicesForVertex(allVertices[i1],
                    subMeshVertexToNeighborList);
                InitializeListOfIndicesForVertex(allVertices[i2],
                    subMeshVertexToNeighborList);
                InitializeListOfIndicesForVertex(allVertices[i3],
                    subMeshVertexToNeighborList);
            }

            // Find all the triangles that have an effect on the vertices in the sub-mesh
            // by mapping each vertex to its neighbors.
            for (int i = 0; i < allTriangles.Length; i += 3)
            {
                int i1 = allTriangles[i];
                int i2 = allTriangles[i + 1];
                int i3 = allTriangles[i + 2];

                AddNeighborsToVertex(allVertices[i1], i2, i3,
                    subMeshVertexToNeighborList);
                AddNeighborsToVertex(allVertices[i2], i3, i1,
                    subMeshVertexToNeighborList);
                AddNeighborsToVertex(allVertices[i3], i1, i2,
                    subMeshVertexToNeighborList);
            }

            return subMeshVertexToNeighborList;
        }

        /// <summary>
        /// For each vertex, create an entry which will hold the
        /// indices of its neighbors.
        /// </summary>
        /// <param name="vertex">Vertex position.</param>
        /// <param name="vertexToNeighborList">Vertex to neighbor mapping.</param>
        private void InitializeListOfIndicesForVertex(Vector3 vertex,
            Dictionary<VertexKey, List<int>> vertexToNeighborList)
        {
            VertexKey key;
            List<int> neighborList;
            if (!vertexToNeighborList.TryGetValue(key = new VertexKey(vertex),
                out neighborList))
            {
                neighborList = new List<int>();
                vertexToNeighborList.Add(key, neighborList);
            }
        }

        /// <summary>
        /// Store the connected triangle vertices to the lookup if the vertex exists
        /// in the sub-mesh.
        /// </summary>
        /// <param name="vertex">Vertex position.</param>
        /// <param name="i1">First neighbor index.</param>
        /// <param name="i2">Second neighbor index.</param>
        /// <param name="subMeshVertexToNeighborList">Sub-mesh vertex to neighbors mapping.</param>
        private void AddNeighborsToVertex(
            Vector3 vertex,
            int i1,
            int i2,
            Dictionary<VertexKey, List<int>> subMeshVertexToNeighborList)
        {
            List<int> neighborList;
            if (subMeshVertexToNeighborList.TryGetValue(new VertexKey(vertex),
                out neighborList))
            {
                neighborList.Add(i1);
                neighborList.Add(i2);
            }
        }

        private void BuildVertexIdToTriangleAssociation(
            Vector3[] vertices,
            int[] allTriangles,
            Dictionary<VertexKey, List<int>> subMeshVertexToNeighborList,
            out Dictionary<int, List<int>> subMeshVertexIdToNeighborList,
            out HashSet<int> subMeshVertexIndexSet,
            out int vertexIdToTriangleMapCount)
        {
            subMeshVertexIdToNeighborList = new Dictionary<int, List<int>>(vertices.Length);
            subMeshVertexIndexSet = new HashSet<int>();
            vertexIdToTriangleMapCount = 0;
            for (int i = 0; i < allTriangles.Length; i += 3)
            {
                int i1 = allTriangles[i];
                int i2 = allTriangles[i + 1];
                int i3 = allTriangles[i + 2];

                vertexIdToTriangleMapCount = MapVertexIndexToNeighborsAndReturnMappingCount(
                    vertices[i1], i1, subMeshVertexToNeighborList,
                    subMeshVertexIndexSet, subMeshVertexIdToNeighborList, vertexIdToTriangleMapCount);

                vertexIdToTriangleMapCount = MapVertexIndexToNeighborsAndReturnMappingCount(
                    vertices[i2], i2, subMeshVertexToNeighborList,
                    subMeshVertexIndexSet, subMeshVertexIdToNeighborList, vertexIdToTriangleMapCount);

                vertexIdToTriangleMapCount = MapVertexIndexToNeighborsAndReturnMappingCount(
                    vertices[i3], i3, subMeshVertexToNeighborList,
                    subMeshVertexIndexSet, subMeshVertexIdToNeighborList, vertexIdToTriangleMapCount);
            }
        }

        /// <summary>
        /// Map each vertex index in the sub-mesh to all the connected triangle vertices.
        /// The original map maps from vertex to neighbors, while the new maps from
        /// index to neighbors.
        /// </summary>
        /// <param name="vertex">Vertex position.</param>
        /// <param name="vertexIndex">Vertex index.</param>
        /// <param name="subMeshVertexToNeighborList">Sub-mesh vertex to neighbors mapping.</param>
        /// <param name="subMeshVertexIndexSet">HashSet of all the vertex indices in the sub-mesh.</param>
        /// <param name="subMeshVertexIdToNeighborList">Sub-mesh vertex id to connected triangle
        /// vertex id lookup.</param>
        /// <param name="vertexIdToTriangleMapCount">Number of elements in the flattened array.</param>
        /// <returns>The number of vertex index to triangle vertices mappings.</returns>
        private int MapVertexIndexToNeighborsAndReturnMappingCount(
            Vector3 vertex,
            int vertexIndex,
            Dictionary<VertexKey, List<int>> subMeshVertexToNeighborList,
            HashSet<int> subMeshVertexIndexSet,
            Dictionary<int, List<int>> subMeshVertexIdToNeighborList,
            int vertexIdToTriangleMapCount)
        {
            if (subMeshVertexToNeighborList.TryGetValue(new VertexKey(vertex),
                out var triangleVertexList))
            {
                subMeshVertexIndexSet.Add(vertexIndex);
                if (!subMeshVertexIdToNeighborList.TryGetValue(vertexIndex,
                    out var neighborList))
                {
                    neighborList =
                        subMeshVertexIdToNeighborList[vertexIndex] = new List<int>();
                    vertexIdToTriangleMapCount++;
                }
                neighborList.AddRange(triangleVertexList);
                vertexIdToTriangleMapCount += triangleVertexList.Count;
            }
            return vertexIdToTriangleMapCount;
        }

        private void OnDestroy()
        {
            if (!_useUnityFunction)
            {
                _normalRecalculator.ReleaseResources();

#if UNITY_EDITOR
                int invisibleMask = GetHiddenMeshMask();
                UnityEditor.Tools.visibleLayers |= invisibleMask;
#endif
                if (_meshSnapshot)
                {
                    Destroy(_meshSnapshot);
                }
                if (_instantiatedMaterials != null)
                {
                    foreach (var instantiatedMaterial in _instantiatedMaterials)
                    {
                        if (instantiatedMaterial)
                        {
                            Destroy(instantiatedMaterial);
                        }
                    }
                }
            }
        }
    }
}
