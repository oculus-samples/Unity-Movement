// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

namespace Oculus.Movement.AnimationRigging.Utils
{
    /// <summary>
    /// Keeps track of bone transforms, which might get re-allocated, by
    /// using proxies. Since proxies don't typically re-allocate, any jobs that
    /// depend on them do not require re-allocation. Bone ID changes require
    /// reallocation, however.
    /// </summary>
    public class ProxyTransformLogic
    {
        /// <summary>
        /// Job used to quickly store poses from transforms.
        /// </summary>
        [Unity.Burst.BurstCompile]
        public struct GetPosesJob : IJobParallelForTransform
        {
            /// <summary>
            /// Poses to write to.
            /// </summary>
            [WriteOnly]
            public NativeArray<Pose> Poses;

            /// <inheritdoc cref="IJobParallelForTransform.Execute(int, TransformAccess)"/>
            [Unity.Burst.BurstCompile]
            public void Execute(int index, TransformAccess transform)
            {
                Poses[index] = new Pose(transform.position, transform.rotation);
            }
        }

        /// <summary>
        /// Job used to apply stored poses to proxy transforms.
        /// </summary>
        [Unity.Burst.BurstCompile]
        public struct ProxyTransformLogicJob : IJobParallelForTransform
        {
            /// <summary>
            /// Poses to read from.
            /// </summary>
            [ReadOnly]
            public NativeArray<Pose> SourcePoses;

            /// <inheritdoc cref="IJobParallelForTransform.Execute(int, TransformAccess)"/>
            [Unity.Burst.BurstCompile]
            public void Execute(int index, TransformAccess transform)
            {
                var sourcePose = SourcePoses[index];
                transform.SetPositionAndRotation(sourcePose.position, sourcePose.rotation);
            }
        }

        /// <summary>
        /// Class that tracks proxy and source transforms.
        /// </summary>
        public class ProxyTransform
        {
            /// <summary>
            /// Proxy transform constructor.
            /// </summary>
            /// <param name="drivenTransform">Transform to be driven by source;
            ///  the actual proxy itself.</param>
            /// <param name="sourceTransform">Source transform.</param>
            /// <param name="boneId">The bone ID of the source.</param>
            public ProxyTransform(Transform drivenTransform,
                Transform sourceTransform,
                OVRSkeleton.BoneId boneId)
            {
                DrivenTransform = drivenTransform;
                SourceTransform = sourceTransform;
                BoneId = boneId;
            }

            /// <summary>
            ///  Update proxy position to track source.
            /// </summary>
            public void Update()
            {
                if (DrivenTransform == null)
                {
                    return;
                }
                DrivenTransform.SetPositionAndRotation(
                    SourceTransform.position,
                    SourceTransform.rotation);
            }

            /// <summary>
            /// Validate source transform to make sure it's
            /// always correct w.r.t. the bone transform.
            /// </summary>
            /// <param name="bone">Bone to track.</param>
            public void ValidateSource(OVRBone bone)
            {
                SourceTransform = bone.Transform;
            }

            /// <summary>
            /// The driven, proxy transform.
            /// </summary>
            public Transform DrivenTransform { get; private set; }
            /// <summary>
            /// The source transform to drive the proxy.
            /// </summary>
            public Transform SourceTransform { get; private set; }
            /// <summary>
            /// The bone ID of the source.
            /// </summary>
            public OVRSkeleton.BoneId BoneId { get; private set; }
        }

        private ProxyTransform[] _proxyTransforms = null;
        /// <summary>
        /// Accessor for array of proxy transforms that track skeletal bones.
        /// </summary>
        public ProxyTransform[] ProxyTransforms => _proxyTransforms;

        /// <summary>
        /// Triggered if proxy transforms were recreated.
        /// </summary>
        public int ProxyChangeCount { get; private set; } = 0;

        /// <summary>
        /// Whether to C# jobs or not.
        /// </summary>
        public bool UseJobs { get; set; }

        private const string _PROXY_CONTAINER_NAME = "ProxyBones-";
        private const string _PROXY_NAME_PREFIX = "Proxy-Bone-";

        private TransformAccessArray _drivenTransformsArray;
        private TransformAccessArray _sourceTransformsArray;
        private NativeArray<Pose> _sourcesPoses;
        private GetPosesJob _getPosesJob;
        private ProxyTransformLogicJob _proxyTransformJob;

        /// <summary>
        /// Updates the state of the proxies using the skeletal
        /// bones provided.
        /// </summary>
        /// <param name="bones">Bones of the skeleton tracked by
        /// proxy transforms.</param>
        /// <param name="proxyContainerParent">Parent of proxy collection.</param>
        public void UpdateState(IList<OVRBone> bones,
            Transform proxyContainerParent = null)
        {
            ReallocateProxiesIfBoneIdsHaveChanged(bones, proxyContainerParent);

            ValidateProxySourceTransforms(bones);

            if (UseJobs)
            {
                UpdateProxyTransformsJob();
            }
            else
            {
                UpdateProxyTransforms(bones);
            }
        }

        private void ReallocateProxiesIfBoneIdsHaveChanged(IList<OVRBone> bones,
            Transform proxyContainerParent)
        {
            int numBones = bones.Count;
            if (!BoneIdsChanged(bones))
            {
                return;
            }

            if (numBones == 0)
            {
                return;
            }

            Debug.Log($"Creating a new set of {numBones} bone proxies.");
            CleanUpOldProxyTransforms();
            _proxyTransforms = new ProxyTransform[numBones];
            var parentName = proxyContainerParent != null ? proxyContainerParent.name : string.Empty;
            var proxyParent = new GameObject($"{_PROXY_CONTAINER_NAME}{parentName}").transform;

            for (int boneIndex = 0; boneIndex < numBones; boneIndex++)
            {
                var originalBone = bones[boneIndex];
                var drivenTransform = new GameObject($"{_PROXY_NAME_PREFIX}{boneIndex}").transform;
                drivenTransform.SetParent(proxyParent, true);

                _proxyTransforms[boneIndex] =
                    new ProxyTransform(drivenTransform,
                        originalBone.Transform, originalBone.Id);
            }

            AllocateJobData();

            ProxyChangeCount++;
        }

        private void AllocateJobData()
        {
            if (_drivenTransformsArray.isCreated)
            {
                _drivenTransformsArray.Dispose();
            }
            if (_sourceTransformsArray.isCreated)
            {
                _sourceTransformsArray.Dispose();
            }
            int numBones = _proxyTransforms.Length;
            Transform[] drivenTransforms = new Transform[numBones];
            Transform[] sourceTransforms = new Transform[numBones];
            for (int boneIndex = 0; boneIndex < numBones; boneIndex++)
            {
                drivenTransforms[boneIndex] = _proxyTransforms[boneIndex].DrivenTransform;
                sourceTransforms[boneIndex] = _proxyTransforms[boneIndex].SourceTransform;
            }
            _drivenTransformsArray = new TransformAccessArray(drivenTransforms);
            _sourceTransformsArray = new TransformAccessArray(sourceTransforms);
            if (_sourcesPoses.IsCreated)
            {
                _sourcesPoses.Dispose();
            }
            _sourcesPoses = new NativeArray<Pose>(_proxyTransforms.Length, Allocator.Persistent);
            for (int i = 0; i < _sourcesPoses.Length; i++)
            {
                _sourcesPoses[i] = _proxyTransforms[i].SourceTransform.GetPose();
            }

            _getPosesJob = new GetPosesJob()
            {
                Poses = _sourcesPoses
            };
            _proxyTransformJob = new ProxyTransformLogicJob()
            {
                SourcePoses = _sourcesPoses
            };
        }

        private bool BoneIdsChanged(IList<OVRBone> bones)
        {
            int numBones = bones.Count;
            if (_proxyTransforms == null ||
                _proxyTransforms.Length != numBones)
            {
                return true;
            }

            for (int boneIndex = 0; boneIndex < numBones; boneIndex++)
            {
                var proxyBone = _proxyTransforms[boneIndex];
                var originalBone = bones[boneIndex];
                if (proxyBone.BoneId != originalBone.Id)
                {
                    return true;
                }
            }

            return false;
        }

        private void CleanUpOldProxyTransforms()
        {
            if (_proxyTransforms == null || _proxyTransforms.Length == 0)
            {
                return;
            }

            var proxyParent = _proxyTransforms[0].DrivenTransform.parent;
            foreach (var proxyTransform in _proxyTransforms)
            {
                GameObject.Destroy(proxyTransform.DrivenTransform.gameObject);
            }
            _proxyTransforms = null;

            GameObject.Destroy(proxyParent.gameObject);
        }

        /// <summary>
        /// Sometimes source transforms change even though the IDs are the same,
        /// and proxies are only recreated if the bone IDs of the sources change.
        /// Make sure they are set properly.
        /// </summary>
        /// <param name="bones">The list of bones to track.</param>
        private void ValidateProxySourceTransforms(IList<OVRBone> bones)
        {
            if (_proxyTransforms == null)
            {
                return;
            }
            int numBones = bones.Count;
            for (int boneIndex = 0; boneIndex < numBones; boneIndex++)
            {
                _proxyTransforms[boneIndex].ValidateSource(bones[boneIndex]);
            }
        }

        private void UpdateProxyTransforms(IList<OVRBone> bones)
        {
            var boneCount = bones.Count;
            for (int boneIndex = 0; boneIndex < boneCount; boneIndex++)
            {
                _proxyTransforms[boneIndex].Update();
            }
        }

        private void UpdateProxyTransformsJob()
        {
            if (_proxyTransforms == null)
            {
                return;
            }

            JobHandle posesJobHandle = _getPosesJob.ScheduleReadOnly(_sourceTransformsArray, 32);
            posesJobHandle.Complete();
            JobHandle jobHandle = _proxyTransformJob.Schedule(_drivenTransformsArray);
            jobHandle.Complete();
        }

        /// <summary>
        /// Cleans up anything that needs to be manually deallocated.
        /// </summary>
        public void CleanUp()
        {
            if (_drivenTransformsArray.isCreated)
            {
                _drivenTransformsArray.Dispose();
            }
            if (_sourceTransformsArray.isCreated)
            {
                _sourceTransformsArray.Dispose();
            }
            if (_sourcesPoses.IsCreated)
            {
                _sourcesPoses.Dispose();
            }
        }
    }
}
