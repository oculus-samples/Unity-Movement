// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Jobs;
using static Meta.XR.Movement.MSDKUtility;

namespace Meta.XR.Movement.Retargeting
{
    /// <summary>
    /// The joint index that is represented as a name by the character retargeter config.
    /// </summary>
    [Serializable]
    public struct TargetJointIndex : IComparable
    {
        /// <summary>
        /// The index of the target joint.
        /// </summary>
        [SerializeField]
        public int Index;

        /// <summary>
        /// Create a target joint index.
        /// </summary>
        /// <param name="index">The index value</param>
        public TargetJointIndex(int index)
        {
            Index = index;
        }

        /// <summary>
        /// Compares this TargetJointIndex to another object for ordering.
        /// </summary>
        /// <param name="obj">The object to compare with.</param>
        /// <returns>A value indicating the relative order of the objects being compared.</returns>
        /// <exception cref="ArgumentException">Thrown when the object is not a TargetJointIndex.</exception>
        public int CompareTo(object obj)
        {
            if (obj is TargetJointIndex other)
            {
                return Index.CompareTo(other.Index);
            }

            throw new ArgumentException("Object is not a JointIndex", nameof(obj));
        }

        /// <summary>
        /// Determines whether this instance is equal to another TargetJointIndex.
        /// </summary>
        /// <param name="other">The TargetJointIndex to compare with this instance.</param>
        /// <returns>True if the specified TargetJointIndex has the same Index as this instance; otherwise, false.</returns>
        public bool Equals(TargetJointIndex other)
        {
            return Index == other.Index;
        }

        /// <summary>
        /// Implicitly converts a TargetJointIndex to an integer.
        /// </summary>
        /// <param name="targetJointIndex">The TargetJointIndex to convert.</param>
        /// <returns>The integer value of the TargetJointIndex's Index.</returns>
        public static implicit operator int(TargetJointIndex targetJointIndex)
        {
            return targetJointIndex.Index;
        }

        /// <summary>
        /// Implicitly converts an integer to a TargetJointIndex.
        /// </summary>
        /// <param name="index">The integer value to convert.</param>
        /// <returns>A new TargetJointIndex with the specified index.</returns>
        public static implicit operator TargetJointIndex(int index)
        {
            return new TargetJointIndex(index);
        }
    }


    /// <summary>
    /// Indicates what kind of world space to use.
    /// </summary>
    public enum JointType
    {
        NoWorldSpace = 0,
        WorldSpaceRootOnly = 1,
        WorldSpaceAllJoints = 2
    }

    /// <summary>
    /// Contain configuration information for retargeting poses.
    /// </summary>
    public class CharacterRetargeterConfig : MonoBehaviour
    {
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
        }

        /// <summary>
        /// Gets the configuration text.
        /// </summary>
        public string Config => _config != null ? _config.text : string.Empty;

        /// <summary>
        /// Getter and setter for the configuration asset.
        /// </summary>
        public TextAsset ConfigAsset
        {
            get => _config;
            set => _config = value;
        }

        /// <summary>
        /// Gets the number of joints in the configuration.
        /// </summary>
        public int NumberOfJoints => _jointPairs?.Length ?? 0;

        /// <summary>
        /// The joint pairs for the character retargeter corresponding to this object.
        /// </summary>
        public JointPair[] JointPairs => _jointPairs;

        /// <summary>
        /// Gets the number of shapes in the configuration.
        /// </summary>
        public int NumberOfShapes => _shapePoseData.Length;

        /// <summary>
        /// Joints used for retargeted character.
        /// </summary>
        public TransformAccessArray Joints => _joints;

        /// <summary>
        /// The configuration text asset containing retargeting data.
        /// </summary>
        [SerializeField]
        protected TextAsset _config;

        /// <summary>
        /// The joint pair data from the configuration, mapping joints to their parent joints.
        /// </summary>
        [SerializeField]
        protected JointPair[] _jointPairs;

        /// <summary>
        /// The shape pose data from the configuration, used for facial expressions and blendshapes.
        /// </summary>
        [SerializeField]
        protected ShapePoseData[] _shapePoseData;

        protected TransformAccessArray _joints;


        /// <summary>
        /// Initializes the TransformAccessArray with joint transforms when the component starts.
        /// </summary>
        public virtual void Start()
        {
            // Fill out joints and native information.
            var joints = new Transform[_jointPairs.Length];
            for (var i = 0; i < _jointPairs.Length; i++)
            {
                joints[i] = _jointPairs[i].Joint;
            }

            _joints = new TransformAccessArray(joints);
        }

        /// <summary>
        /// Gets the current body pose.
        /// </summary>
        /// <param name="jointType">The joint type.</param>
        /// <returns></returns>
        public NativeArray<NativeTransform> GetCurrentBodyPose(JointType jointType)
        {
            var bodyPose = new NativeArray<NativeTransform>(_jointPairs.Length, Allocator.TempJob);

            var job = new SkeletonJobs.GetPoseJob
            {
                BodyPose = bodyPose,
                JobJointType = jointType
            };

            job.Schedule(_joints).Complete();
            return bodyPose;
        }

        /// <summary>
        /// Gets the current face pose.
        /// </summary>
        /// <returns>The current face pose.</returns>
        public NativeArray<float> GetCurrentFacePose()
        {
            // create flattened array to be used for data serialization.
            int numShapesTotal = NumberOfShapes;
            var faceShapePoses = new NativeArray<float>(numShapesTotal,
                Allocator.Temp,
                NativeArrayOptions.UninitializedMemory);
            int shapePoseIndex = 0;
            foreach (var shape in _shapePoseData)
            {
                Assert.IsNotNull(shape.SkinnedMesh, "Skinned mesh null.");
                faceShapePoses[shapePoseIndex++] = shape.SkinnedMesh.GetBlendShapeWeight(shape.ShapeIndex);
            }

            return faceShapePoses;
        }

        /// <summary>
        /// Applies the specified body pose to the character.
        /// </summary>
        /// <param name="bodyPose">The body pose to apply.</param>
        /// <param name="jointType">The joint type.</param>
        public void ApplyBodyPose(NativeArray<NativeTransform> bodyPose, JointType jointType)
        {
            for (var i = 0; i < _jointPairs.Length; i++)
            {
                var joint = _jointPairs[i].Joint;
                var pose = bodyPose[i];
                var useWorldSpace = UseWorldSpace(i, jointType);
                if (useWorldSpace)
                {
                    joint.SetPositionAndRotation(pose.Position, pose.Orientation);
                }
                else
                {
                    joint.SetLocalPositionAndRotation(pose.Position, pose.Orientation);
                }
            }
        }

        /// <summary>
        /// Applies the given face pose to the character.
        /// </summary>
        /// <param name="facePose">The face pose to apply.</param>
        public void ApplyFacePose(NativeArray<float> facePose)
        {
            var shapePoseIndex = 0;
            foreach (var shape in _shapePoseData)
            {
                shape.SkinnedMesh.SetBlendShapeWeight(
                    shape.ShapeIndex, facePose[shapePoseIndex++]);
            }
        }

        private bool UseWorldSpace(int jointIndex, JointType jointType)
        {
            bool isRoot = jointIndex == 0;
            bool useWorldSpace = false;
            if (jointType == JointType.WorldSpaceRootOnly)
            {
                useWorldSpace = isRoot;
            }
            else if (jointType == JointType.WorldSpaceAllJoints)
            {
                useWorldSpace = true;
            }

            return useWorldSpace;
        }
    }
}
