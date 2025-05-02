// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Jobs;
using static Meta.XR.Movement.MSDKUtility;
using static Meta.XR.Movement.Retargeting.CharacterRetargeterConfig;

namespace Meta.XR.Movement.Utils
{
    /// <summary>
    /// Component that mirrors transforms from a target hierarchy to this hierarchy.
    /// </summary>
    [DefaultExecutionOrder(150)]
    public class MirrorTransforms : MonoBehaviour
    {
        [BurstCompile]
        private struct GetPoseJob : IJobParallelForTransform
        {
            [WriteOnly]
            public NativeArray<NativeTransform> BonePoses;

            [ReadOnly]
            public bool IsLocal;

            /// <inheritdoc cref="IJobParallelForTransform.Execute(int, TransformAccess)"/>
            [BurstCompile]
            public void Execute(int index, TransformAccess transform)
            {
                BonePoses[index] = IsLocal
                    ? new NativeTransform(transform.localRotation, transform.localPosition, transform.localScale)
                    : new NativeTransform(transform.rotation, transform.position, transform.localScale);
            }
        }

        [BurstCompile]
        private struct CopyPoseJob : IJobParallelForTransform
        {
            [ReadOnly]
            public NativeArray<NativeTransform> BonePoses;

            [ReadOnly]
            public bool IsLocal;

            [ReadOnly]
            public bool MirrorPositions;

            [ReadOnly]
            public bool MirrorRotations;

            [ReadOnly]
            public bool MirrorScales;

            /// <inheritdoc cref="IJobParallelForTransform.Execute(int, TransformAccess)"/>
            [BurstCompile]
            public void Execute(int index, TransformAccess transform)
            {
                var bodyPose = BonePoses[index];
                if (MirrorRotations)
                {
                    if (IsLocal)
                    {
                        transform.localRotation = bodyPose.Orientation;
                    }
                    else
                    {
                        transform.rotation = bodyPose.Orientation;
                    }
                }

                if (MirrorPositions)
                {
                    if (IsLocal)
                    {
                        transform.localPosition = bodyPose.Position;
                    }
                    else
                    {
                        transform.position = bodyPose.Position;
                    }
                }

                if (MirrorScales)
                {
                    transform.localScale = bodyPose.Scale;
                }
            }
        }

        /// <summary>
        /// The target transform hierarchy to mirror from.
        /// </summary>
        [SerializeField]
        private Transform _target;

        /// <summary>
        /// Whether to use local or world space transformations.
        /// When true, mirrors local position, rotation, and scale. When false, mirrors world position and rotation.
        /// </summary>
        [SerializeField]
        private bool _isLocal = true;

        /// <summary>
        /// Whether to use Unity's job system for better performance.
        /// When true, transform operations are processed in parallel using Burst-compiled jobs.
        /// </summary>
        [SerializeField]
        private bool _useJobs = true;

        /// <summary>
        /// Whether to mirror position values from the target transforms.
        /// </summary>
        [SerializeField]
        private bool _mirrorPositions = true;

        /// <summary>
        /// Whether to mirror rotation values from the target transforms.
        /// </summary>
        [SerializeField]
        private bool _mirrorRotations = true;

        /// <summary>
        /// Whether to mirror scale values from the target transforms.
        /// </summary>
        [SerializeField]
        private bool _mirrorScales = true;

        /// <summary>
        /// Array of joint pairs that define the mapping between source and target transforms.
        /// Each pair contains a reference to a transform in this hierarchy and its corresponding transform in the target hierarchy.
        /// </summary>
        [SerializeField]
        private JointPair[] _bonePairs;

        private NativeArray<NativeTransform> _bonePoses;
        private TransformAccessArray _bones;
        private TransformAccessArray _targetBones;

        /// <summary>
        /// Initializes the transform arrays for mirroring when the component starts.
        /// </summary>
        public void Start()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            var bones = new Transform[_bonePairs.Length];
            var targetBones = new Transform[_bonePairs.Length];
            for (var i = 0; i < _bonePairs.Length; i++)
            {
                bones[i] = _bonePairs[i].Joint;
                targetBones[i] = _bonePairs[i].ParentJoint;
            }

            _bones = new TransformAccessArray(bones);
            _targetBones = new TransformAccessArray(targetBones);
            _bonePoses = new NativeArray<NativeTransform>(
                _bonePairs.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        }

        /// <summary>
        /// Updates the transforms each frame after all other updates have completed.
        /// </summary>
        public void LateUpdate()
        {
            if (_bonePairs == null || _bonePairs.Length == 0)
            {
                return;
            }

            if (Application.isPlaying && _useJobs)
            {
                UpdateJobs();
            }
            else
            {
                ManualUpdate();
            }
        }

        private void OnDestroy()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (_bones.isCreated)
            {
                _bones.Dispose();
            }

            if (_targetBones.isCreated)
            {
                _targetBones.Dispose();
            }

            if (_bonePoses.IsCreated)
            {
                _bonePoses.Dispose();
            }
        }

        /// <summary>
        /// Finds matching bone pairs between this hierarchy and the target hierarchy based on name.
        /// </summary>
        private void FindBonePairs()
        {
            if (_target == null)
            {
                return;
            }

            var transforms = GetComponentsInChildren<Transform>();
            var targetTransforms = _target.GetComponentsInChildren<Transform>();
            var bonePairs = new List<JointPair>();
            bonePairs.Add(new JointPair
            {
                Joint = transform,
                ParentJoint = _target
            });
            foreach (var source in transforms)
            {
                foreach (var target in targetTransforms)
                {
                    if (source.name != target.name)
                    {
                        continue;
                    }

                    bonePairs.Add(new JointPair()
                    {
                        Joint = source,
                        ParentJoint = target
                    });
                    break;
                }
            }

            _bonePairs = bonePairs.ToArray();
        }

        private void UpdateJobs()
        {
            var getBonesJob = new GetPoseJob
            {
                IsLocal = _isLocal,
                BonePoses = _bonePoses
            };
            var copyBonesJob = new CopyPoseJob
            {
                IsLocal = _isLocal,
                BonePoses = _bonePoses,
                MirrorPositions = _mirrorPositions,
                MirrorRotations = _mirrorRotations,
                MirrorScales = _mirrorScales
            };
            var getBonesJobHandle = getBonesJob.Schedule(_targetBones);
            copyBonesJob.Schedule(_bones, getBonesJobHandle).Complete();
        }

        private void ManualUpdate()
        {
            foreach (var bonePair in _bonePairs)
            {
                if (_mirrorRotations)
                {
                    var rotation = _isLocal ? bonePair.ParentJoint.localRotation : bonePair.ParentJoint.rotation;
                    if (_isLocal)
                    {
                        bonePair.Joint.localRotation = rotation;
                    }
                    else
                    {
                        bonePair.Joint.rotation = rotation;
                    }
                }

                if (_mirrorPositions)
                {
                    var position = _isLocal ? bonePair.ParentJoint.localPosition : bonePair.ParentJoint.position;
                    if (_isLocal)
                    {
                        bonePair.Joint.localPosition = position;
                    }
                    else
                    {
                        bonePair.Joint.position = position;
                    }
                }

                if (_mirrorScales)
                {
                    bonePair.Joint.localScale = bonePair.ParentJoint.localScale;
                }
            }
        }

#if UNITY_EDITOR
        [UnityEditor.CustomEditor(typeof(MirrorTransforms)), UnityEditor.CanEditMultipleObjects]
        public class MirrorTransformsEditor : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();
                var mirrorTransforms = target as MirrorTransforms;
                if (GUILayout.Button("Find Bone Pairs"))
                {
                    if (mirrorTransforms != null)
                    {
                        UnityEditor.EditorUtility.SetDirty(mirrorTransforms);
                        UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(mirrorTransforms);
                        mirrorTransforms.FindBonePairs();
                    }
                }

                if (GUILayout.Button("Manual Update"))
                {
                    if (mirrorTransforms != null)
                    {
                        mirrorTransforms.LateUpdate();
                        UnityEditor.EditorUtility.SetDirty(mirrorTransforms);
                    }
                }

                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
            }
        }
#endif
    }
}
