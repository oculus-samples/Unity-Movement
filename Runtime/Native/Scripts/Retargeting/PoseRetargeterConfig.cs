// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using static Meta.XR.Movement.NativeUtilityPlugin;
using static OVRPlugin;
using static OVRSkeleton;
using static OVRUnityHumanoidSkeletonRetargeter;
using static OVRUnityHumanoidSkeletonRetargeter.OVRHumanBodyBonesMappings;

namespace Meta.XR.Movement.Retargeting
{
    /// <summary>
    /// Contain configuration information for retargeting poses.
    /// </summary>
    public class PoseRetargeterConfig : MonoBehaviour
    {
        /// <summary>
        /// Gets the configuration text.
        /// </summary>
        public string Config => _config.text;

        /// <summary>
        /// Gets the number of joints in the configuration.
        /// </summary>
        public int NumberOfJoints => _jointPairs.Length;

        /// <summary>
        /// Gets the number of shapes in the configuration.
        /// </summary>
        public int NumberOfShapes => _shapePoseData.Length;

        /// <summary>
        /// Store information about a pair of joint transforms.
        /// </summary>
        [Serializable]
        public struct JointPair
        {
            /// <summary>
            /// The joint transform.
            /// </summary>
            public Transform Joint;

            /// <summary>
            /// The parent joint transform.
            /// </summary>
            public Transform ParentJoint;
        }

        /// <summary>
        /// Represents shape pose data.
        /// </summary>
        [Serializable]
        public struct ShapePoseData
        {
            /// <summary>
            /// Initializes the <see cref="ShapePoseData"/> struct.
            /// </summary>
            /// <param name="skinnedMesh">The skinned mesh renderer.</param>
            /// <param name="shapeName">The shape name.</param>
            /// <param name="shapeIndex">The shape index.</param>
            public ShapePoseData(SkinnedMeshRenderer skinnedMesh, string shapeName, int shapeIndex)
            {
                SkinnedMesh = skinnedMesh;
                ShapeName = shapeName;
                ShapeIndex = shapeIndex;
            }

            /// <summary>
            /// The skinned mesh renderer.
            /// </summary>
            public SkinnedMeshRenderer SkinnedMesh;

            /// <summary>
            /// The shape name.
            /// </summary>
            public string ShapeName;

            /// <summary>
            /// The shape index.
            /// </summary>
            public int ShapeIndex;
        }

        /// <summary>
        /// The configuration text asset.
        /// </summary>
        [SerializeField]
        protected TextAsset _config;

        /// <summary>
        /// The joint pair data from the configuration.
        /// </summary>
        [SerializeField]
        protected JointPair[] _jointPairs;

        /// <summary>
        /// The shape pose data from the configuration.
        /// </summary>
        [SerializeField]
        protected ShapePoseData[] _shapePoseData;


        /// <summary>
        /// Gets the current body pose.
        /// </summary>
        /// <returns>The current body pose.</returns>
        public NativeArray<SerializedJointPose> GetCurrentBodyPose()
        {
            var bodyPose = new NativeArray<SerializedJointPose>(_jointPairs.Length, Allocator.Temp,
                NativeArrayOptions.UninitializedMemory);
            for (var i = 0; i < _jointPairs.Length; i++)
            {
                var joint = _jointPairs[i].Joint;
                var jointParent = _jointPairs[i].ParentJoint;
                var distance = jointParent == null ? 0.0f : Vector3.Distance(joint.position, jointParent.position);

                bodyPose[i] = new SerializedJointPose
                {
                    Orientation =
                        joint.localRotation,
                    Position =
                        joint.localPosition,
                    Length = distance
                };
            }

            return bodyPose;
        }


        /// <summary>
        /// Gets the current face pose.
        /// </summary>
        /// <returns>The current face pose.</returns>
        public NativeArray<SerializedShapePose> GetCurrentFacePose()
        {
            // create flattened array to be used for data serialization.
            int numShapesTotal = NumberOfShapes;
            var faceShapePoses = new NativeArray<SerializedShapePose>(numShapesTotal,
                Allocator.Temp,
                NativeArrayOptions.UninitializedMemory);
            int shapePoseIndex = 0;
            foreach (var shape in _shapePoseData)
            {
                Assert.IsNotNull(shape.SkinnedMesh, "Skinned mesh null.");
                faceShapePoses[shapePoseIndex++] = new SerializedShapePose
                {
                    Weight = shape.SkinnedMesh.GetBlendShapeWeight(shape.ShapeIndex)
                };
            }

            return faceShapePoses;
        }

        /// <summary>
        /// Applies the specified body pose to the character.
        /// </summary>
        /// <param name="bodyPose">The body pose to apply.</param>
        public void ApplyBodyPose(NativeArray<SerializedJointPose> bodyPose)
        {
            for (var i = 0; i < _jointPairs.Length; i++)
            {
                var joint = _jointPairs[i].Joint;
                var pose = bodyPose[i];
                bool isHips = i == 0;
                joint.SetLocalPositionAndRotation(pose.Position, pose.Orientation);
            }
        }

        /// <summary>
        /// Applies the given face pose to the character.
        /// </summary>
        /// <param name="facePose">The face pose to apply.</param>
        public void ApplyFacePose(NativeArray<SerializedShapePose> facePose)
        {
            var shapePoseIndex = 0;
            foreach (var shape in _shapePoseData)
            {
                shape.SkinnedMesh.SetBlendShapeWeight(
                    shape.ShapeIndex, facePose[shapePoseIndex++].Weight);
            }
        }
    }
}
