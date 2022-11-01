// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace Oculus.Movement.Effects
{
    /// <summary>
    /// Applies mirror's transformation to an object that
    /// needs to be reflected.
    /// </summary>
    [ExecuteInEditMode]
    public class MirrorTransformation : MonoBehaviour
    {
        /// <summary>
        /// Mirror normal, perpendicular to mirror face.
        /// </summary>
        [SerializeField]
        [Tooltip(MirrorTransformationTooltips.MirrorNormal)]
        protected Vector3 _mirrorNormal = -Vector3.forward;

        /// <summary>
        /// Allows mirror to be pushed into reflection plane somewhat,
        /// assuming mirror geometry has some thickness.
        /// </summary>
        [SerializeField]
        [Tooltip(MirrorTransformationTooltips.MirrorPlaneOffset)]
        protected float _mirrorPlaneOffset = -0.03f;

        /// <summary>
        /// Transform to be reflected.
        /// </summary>
        [SerializeField]
        [Tooltip(MirrorTransformationTooltips.TransformToMirror)]
        protected Transform _transformToMirror;

        private Matrix4x4 _reflectionMatrix;

        private void LateUpdate()
        {
            _reflectionMatrix = CreateReflectionMatrix();
            if (_transformToMirror != null)
            {
                _transformToMirror.position = _reflectionMatrix.GetPosition();
                _transformToMirror.rotation = _reflectionMatrix.GetRotation();
                _transformToMirror.localScale = _reflectionMatrix.GetScale();
            }
        }

        private Matrix4x4 CreateReflectionMatrix()
        {
            // find out the reflection plane: position and normal in world space
            Vector3 pos = transform.position;
            Vector3 normal = transform.localToWorldMatrix.MultiplyVector(_mirrorNormal);
            // Reflect camera around reflection plane, using
            // this transform's normal and position.
            // Apply an offset as well since the wall of the enclosure
            // that is reflected could have some thickness, and by offsetting
            // the mirror plane we hide that thickness.
            float d = -Vector3.Dot(normal, pos) + _mirrorPlaneOffset;
            var reflectionPlane = new Vector4(normal.x, normal.y, normal.z, d);
            // use reflection plane to create a reflection matrix
            return CalculateReflectionMatrix(reflectionPlane);
        }

        /// <summary>
        /// Gives reflection matrix given reflection plane in world space.
        /// https://en.wikipedia.org/wiki/Transformation_matrix#Reflection
        /// </summary>
        /// <param name="reflectionPlane">World space reflection plane.</param>
        /// <returns>Reflection matrix.</returns>
        private Matrix4x4 CalculateReflectionMatrix(Vector4 reflectionPlane)
        {
            var reflectionMat = Matrix4x4.identity;

            reflectionMat.m00 = 1F - 2F * reflectionPlane[0] * reflectionPlane[0];
            reflectionMat.m01 = -2F * reflectionPlane[0] * reflectionPlane[1];
            reflectionMat.m02 = -2F * reflectionPlane[0] * reflectionPlane[2];
            reflectionMat.m03 = -2F * reflectionPlane[0] * reflectionPlane[3];

            reflectionMat.m10 = -2F * reflectionPlane[0] * reflectionPlane[1];
            reflectionMat.m11 = 1F - 2F * reflectionPlane[1] * reflectionPlane[1];
            reflectionMat.m12 = -2F * reflectionPlane[1] * reflectionPlane[2];
            reflectionMat.m13 = -2F * reflectionPlane[1] * reflectionPlane[3];

            reflectionMat.m20 = -2F * reflectionPlane[0] * reflectionPlane[2];
            reflectionMat.m21 = -2F * reflectionPlane[1] * reflectionPlane[2];
            reflectionMat.m22 = 1F - 2F * reflectionPlane[2] * reflectionPlane[2];
            reflectionMat.m23 = -2F * reflectionPlane[2] * reflectionPlane[3];

            reflectionMat.m30 = 0F;
            reflectionMat.m31 = 0F;
            reflectionMat.m32 = 0F;
            reflectionMat.m33 = 1F;

            return reflectionMat;
        }
    }
}
