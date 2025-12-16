// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using static Meta.XR.Movement.MSDKUtility;
using Object = UnityEngine.Object;

namespace Meta.XR.Movement.Retargeting
{
    /// <summary>
    /// Static utility class for drawing primitives in Unity using meshes.
    /// Provides generalized drawing functions for lines, spheres, and other shapes.
    /// Manages all mesh and material resources internally.
    /// </summary>
    public static class MeshDraw
    {
        private static Mesh _sharedLineMesh;
        private static Mesh _sharedSphereMesh;
        private static Mesh _sharedBoneMesh;
        private static readonly Dictionary<Color, Material> _materials = new();

        private static readonly float _boneSphereScale = 0.333f;

        /// <summary>
        /// Draws a line between two points with the specified color and thickness.
        /// This method handles all mesh and material management internally.
        /// </summary>
        /// <param name="color">The color of the line.</param>
        /// <param name="thickness">The thickness of the line.</param>
        /// <param name="startPos">The start position of the line.</param>
        /// <param name="endPos">The end position of the line.</param>
        public static void DrawLine(Color color, float thickness, Vector3 startPos, Vector3 endPos)
        {
            EnsureInitialized();
            var material = GetOrCreateMaterial(color);
            material.color = color;
            material.SetPass(0);

            var lineTransform = CalculateLineTransformMatrix(startPos, endPos, thickness);
            var renderParams = new RenderParams(material);
            DrawMesh(_sharedLineMesh, material, lineTransform, renderParams);
        }

        /// <summary>
        /// Draws a sphere at the specified position with the specified color and scale.
        /// This method handles all mesh and material management internally.
        /// </summary>
        /// <param name="color">The color of the sphere.</param>
        /// <param name="thickness">The scale multiplier for the sphere.</param>
        /// <param name="position">The position of the sphere.</param>
        public static void DrawSphere(Color color, float thickness, Vector3 position)
        {
            EnsureInitialized();
            var material = GetOrCreateMaterial(color);
            material.color = color;
            material.SetPass(0);

            var sphereTransform = CalculateSphereTransformMatrix(position, thickness);
            var renderParams = new RenderParams(material);
            DrawMesh(_sharedSphereMesh, material, sphereTransform, renderParams);
        }

        /// <summary>
        /// Draws a bone shape between two positions using the bone mesh.
        /// </summary>
        /// <param name="color">The color of the bone.</param>
        /// <param name="position">The position of the bone (parent joint).</param>
        /// <param name="rotation">The rotation of the bone.</param>
        /// <param name="width">The width of the bone.</param>
        /// <param name="length">The length of the bone.</param>
        public static void DrawBone(Color color, Vector3 position, Quaternion rotation, float width, float length)
        {
            EnsureInitialized();
            var material = GetOrCreateMaterial(color);

            // Set material color before drawing. This is critical for handling concurrent draws
            // from different lifecycles (e.g., Update vs LateUpdate) that share cached materials.
            material.color = color;
            material.SetPass(0);

            var boneTransform = Matrix4x4.TRS(position, rotation, new Vector3(width, width, length));
            var renderParams = new RenderParams(material);

            DrawSphere(_sharedSphereMesh, material, position, width * _boneSphereScale, renderParams);
            DrawMesh(_sharedBoneMesh, material, boneTransform, renderParams);
        }

        /// <summary>
        /// Draws a skeleton from a pose and parent bone indices.
        /// </summary>
        /// <param name="pose">Array of joint poses.</param>
        /// <param name="parentIndices">Array of parent indices for each joint.</param>
        /// <param name="color">The color of the skeleton.</param>
        /// <param name="thickness">The thickness of the bones.</param>
        public static void DrawSkeleton(NativeArray<NativeTransform> pose, int[] parentIndices, Color color, float thickness = 0.04f)
        {
            if (!pose.IsCreated || parentIndices == null || pose.Length != parentIndices.Length)
            {
                return;
            }

            for (int i = 0; i < pose.Length; i++)
            {
                int parentIndex = parentIndices[i];
                if (parentIndex < 0 || parentIndex >= pose.Length)
                {
                    continue;
                }

                var childPose = pose[i];
                var parentPose = pose[parentIndex];

                if (parentPose.Scale == Vector3.zero)
                {
                    continue;
                }

                var parentPos = parentPose.Position;
                var childPos = childPose.Position;
                var length = Vector3.Distance(parentPos, childPos);

                if (length > 0f)
                {
                    var rotation = Quaternion.FromToRotation(Vector3.forward, childPos - parentPos);
                    DrawBone(color, parentPos, rotation, thickness, length);
                }
            }
        }

        /// <summary>
        /// Draws an OVR skeleton directly from a body tracker data provider.
        /// This is a convenience function that handles getting the pose and parent indices.
        /// </summary>
        /// <param name="dataProvider">The OVR skeleton data provider (e.g., OVRBody).</param>
        /// <param name="color">The color of the skeleton.</param>
        /// <param name="thickness">The thickness of the bones.</param>
        /// <param name="offset">Optional offset to apply to the skeleton pose.</param>
        /// <param name="convertToUnitySpace">Whether to convert to Unity coordinate space.</param>
        public static void DrawOVRSkeleton(
            OVRSkeleton.IOVRSkeletonDataProvider dataProvider,
            Color color,
            float thickness = 0.04f,
            Pose offset = default,
            bool convertToUnitySpace = true)
        {
            if (dataProvider == null)
            {
                return;
            }

            var pose = SkeletonUtilities.GetPosesFromTheTracker(
                dataProvider,
                offset,
                convertToUnitySpace);

            if (!pose.IsCreated || pose.Length == 0)
            {
                return;
            }

            const int jointCount = (int)SkeletonData.FullBodyTrackingBoneId.End;
            if (pose.Length < jointCount)
            {
                return;
            }

            var parentIndices = new int[jointCount];
            for (int i = 0; i < jointCount; i++)
            {
                parentIndices[i] = (int)SkeletonData.ParentBoneId[i];
            }

            DrawSkeleton(pose, parentIndices, color, thickness);
        }

        /// <summary>
        /// Transforms a bounds by a transformation matrix.
        /// </summary>
        /// <param name="bounds">The original bounds.</param>
        /// <param name="matrix">The transformation matrix.</param>
        /// <returns>The transformed bounds.</returns>
        public static Bounds TransformBounds(Bounds bounds, Matrix4x4 matrix)
        {
            Vector3 center = matrix.MultiplyPoint3x4(bounds.center);
            Vector3 extents = Vector3.Scale(bounds.extents, matrix.lossyScale);
            return new Bounds(center, extents * 2.0f);
        }

        /// <summary>
        /// Safely cleans up a mesh object.
        /// </summary>
        /// <param name="mesh">The mesh to destroy.</param>
        public static void CleanUpMesh(Mesh mesh)
        {
            if (mesh == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(mesh);
            }
            else
            {
                Object.DestroyImmediate(mesh);
            }
        }

        /// <summary>
        /// Creates a material using a shader from Resources.
        /// </summary>
        /// <param name="shaderPath">The path to the shader in Resources folder.</param>
        /// <param name="color">The initial color of the material.</param>
        /// <returns>A new material instance.</returns>
        public static Material CreateMaterial(string shaderPath, Color color)
        {
            var material = new Material(Resources.Load<Shader>(shaderPath));
            material.color = color;
            return material;
        }

        /// <summary>
        /// Safely cleans up a material object.
        /// </summary>
        /// <param name="material">The material to destroy.</param>
        public static void CleanUpMaterial(Material material)
        {
            if (material == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(material);
            }
            else
            {
                Object.DestroyImmediate(material);
            }
        }

        /// <summary>
        /// Initializes shared resources for the mesh drawing utility.
        /// This is called automatically on first use.
        /// </summary>
        private static void EnsureInitialized()
        {
            if (_sharedLineMesh == null)
            {
                _sharedLineMesh = CreatePrimitiveMesh(PrimitiveType.Cylinder);
                _sharedLineMesh.RecalculateNormals();
                _sharedLineMesh.RecalculateBounds();
            }

            if (_sharedSphereMesh == null)
            {
                _sharedSphereMesh = CreatePrimitiveMesh(PrimitiveType.Sphere);
                _sharedSphereMesh.RecalculateNormals();
                _sharedSphereMesh.RecalculateBounds();
            }

            if (_sharedBoneMesh == null)
            {
                _sharedBoneMesh = CreateBoneMesh();
                _sharedBoneMesh.RecalculateNormals();
                _sharedBoneMesh.RecalculateBounds();
            }
        }

        /// <summary>
        /// Gets or creates a material for the specified color.
        /// Materials are cached by color for efficiency. The color will be set in the draw methods
        /// to handle concurrent draws from different lifecycles (Update vs LateUpdate).
        /// </summary>
        /// <param name="color">The color of the material.</param>
        /// <returns>A material instance for the color.</returns>
        private static Material GetOrCreateMaterial(Color color)
        {
            if (!_materials.TryGetValue(color, out var material) || material == null)
            {
                material = CreateMaterial("Runtime/MeshDraw", color);
                _materials[color] = material;
            }

            return material;
        }

        /// <summary>
        /// Creates a mesh from a Unity primitive type.
        /// </summary>
        /// <param name="primitiveType">The type of primitive to create.</param>
        /// <returns>A new mesh instance with the primitive's geometry.</returns>
        private static Mesh CreatePrimitiveMesh(PrimitiveType primitiveType)
        {
            var tempObject = GameObject.CreatePrimitive(primitiveType);
            var tempMesh = tempObject.GetComponent<MeshFilter>().sharedMesh;

            var mesh = new Mesh
            {
                vertices = tempMesh.vertices,
                triangles = tempMesh.triangles,
                normals = tempMesh.normals,
                uv = tempMesh.uv
            };

            if (Application.isPlaying)
            {
                Object.Destroy(tempObject);
            }
            else
            {
                Object.DestroyImmediate(tempObject);
            }

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// Creates a bone mesh.
        /// </summary>
        /// <returns>A new bone mesh.</returns>
        private static Mesh CreateBoneMesh()
        {
            const float size = 1f / 7f;
            List<Vector3> vertices = new();
            List<int> triangles = new();
            vertices.Add(new Vector3(-size, -size, 0.200f));
            vertices.Add(new Vector3(-size, size, 0.200f));
            vertices.Add(new Vector3(0.000f, 0.000f, 0.000f));
            vertices.Add(new Vector3(size, size, 0.200f));
            vertices.Add(new Vector3(0.000f, 0.000f, 1.000f));
            vertices.Add(new Vector3(size, -size, 0.200f));
            vertices.Add(new Vector3(-size, size, 0.200f));
            vertices.Add(new Vector3(-size, -size, 0.200f));
            vertices.Add(new Vector3(0.000f, 0.000f, 1.000f));
            vertices.Add(new Vector3(size, -size, 0.200f));
            vertices.Add(new Vector3(0.000f, 0.000f, 1.000f));
            vertices.Add(new Vector3(-size, -size, 0.200f));
            vertices.Add(new Vector3(size, size, 0.200f));
            vertices.Add(new Vector3(-size, size, 0.200f));
            vertices.Add(new Vector3(0.000f, 0.000f, 1.000f));
            vertices.Add(new Vector3(size, size, 0.200f));
            vertices.Add(new Vector3(size, -size, 0.200f));
            vertices.Add(new Vector3(0.000f, 0.000f, 0.000f));
            vertices.Add(new Vector3(size, size, 0.200f));
            vertices.Add(new Vector3(0.000f, 0.000f, 0.000f));
            vertices.Add(new Vector3(-size, size, 0.200f));
            vertices.Add(new Vector3(size, -size, 0.200f));
            vertices.Add(new Vector3(-size, -size, 0.200f));
            vertices.Add(new Vector3(0.000f, 0.000f, 0.000f));
            for (int i = 0; i < 24; i++)
            {
                triangles.Add(i);
            }

            Mesh mesh = new()
            {
                vertices = vertices.ToArray(),
                triangles = triangles.ToArray()
            };
            return mesh;
        }

        /// <summary>
        /// Sets all vertex colors of a mesh to a specified color.
        /// </summary>
        /// <param name="mesh">The mesh to modify.</param>
        /// <param name="color">The color to apply to all vertices.</param>
        private static void SetVertexColors(Mesh mesh, Color color)
        {
            var colors = new Color[mesh.vertexCount];
            for (var i = 0; i < mesh.vertexCount; i++)
            {
                colors[i] = color;
            }

            mesh.colors = colors;
        }

        /// <summary>
        /// Calculates a transformation matrix between two points, oriented along the line connecting them.
        /// </summary>
        /// <param name="start">The start point.</param>
        /// <param name="end">The end point.</param>
        /// <param name="thickness">The thickness of the line.</param>
        /// <returns>A transformation matrix for a cylinder mesh connecting the two points.</returns>
        private static Matrix4x4 CalculateLineTransformMatrix(Vector3 start, Vector3 end, float thickness)
        {
            Vector3 position = (start + end) / 2;
            Vector3 direction = (end - start).normalized;
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, direction);
            float distance = Vector3.Distance(start, end);
            Vector3 scale = new Vector3(thickness, distance / 2, thickness);

            return Matrix4x4.TRS(position, rotation, scale);
        }

        /// <summary>
        /// Calculates a transformation matrix for a sphere at a given position with a specified scale.
        /// </summary>
        /// <param name="position">The position of the sphere.</param>
        /// <param name="scale">The uniform scale of the sphere.</param>
        /// <returns>A transformation matrix for a sphere mesh.</returns>
        private static Matrix4x4 CalculateSphereTransformMatrix(Vector3 position, float scale)
        {
            return Matrix4x4.TRS(position, Quaternion.identity, Vector3.one * scale);
        }

        /// <summary>
        /// Draws a line between two points using a cylinder mesh.
        /// </summary>
        /// <param name="lineMesh">The cylinder mesh to use for the line.</param>
        /// <param name="material">The material to use for rendering.</param>
        /// <param name="start">The start point of the line.</param>
        /// <param name="end">The end point of the line.</param>
        /// <param name="thickness">The thickness of the line.</param>
        /// <param name="renderParams">The render parameters (used only at runtime).</param>
        private static void DrawLine(
            Mesh lineMesh,
            Material material,
            Vector3 start,
            Vector3 end,
            float thickness,
            RenderParams renderParams)
        {
            var lineTransform = CalculateLineTransformMatrix(start, end, thickness);
            DrawMesh(lineMesh, material, lineTransform, renderParams);
        }

        /// <summary>
        /// Draws a sphere at a given position.
        /// </summary>
        /// <param name="sphereMesh">The sphere mesh to use.</param>
        /// <param name="material">The material to use for rendering.</param>
        /// <param name="position">The position of the sphere.</param>
        /// <param name="scale">The uniform scale of the sphere.</param>
        /// <param name="renderParams">The render parameters (used only at runtime).</param>
        private static void DrawSphere(
            Mesh sphereMesh,
            Material material,
            Vector3 position,
            float scale,
            RenderParams renderParams)
        {
            var sphereTransform = CalculateSphereTransformMatrix(position, scale);
            DrawMesh(sphereMesh, material, sphereTransform, renderParams);
        }

        /// <summary>
        /// Draws a mesh at runtime or in edit mode using the appropriate rendering method.
        /// </summary>
        /// <param name="mesh">The mesh to draw.</param>
        /// <param name="material">The material to use for rendering.</param>
        /// <param name="transform">The transformation matrix.</param>
        /// <param name="renderParams">The render parameters (used only at runtime).</param>
        private static void DrawMesh(Mesh mesh, Material material, Matrix4x4 transform, RenderParams renderParams)
        {
            if (Application.isPlaying)
            {
                Graphics.RenderMesh(renderParams, mesh, 0, transform);
            }
            else
            {
                Graphics.DrawMeshNow(mesh, transform);
            }
        }
    }
}
