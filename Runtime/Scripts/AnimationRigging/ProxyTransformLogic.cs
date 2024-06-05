// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using UnityEngine;

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

        private const string _PROXY_CONTAINER_NAME = "ProxyBones-";
        private const string _PROXY_NAME_PREFIX = "Proxy-Bone-";

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

            UpdateProxyTransforms(bones);
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
            ProxyChangeCount++;
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
            if (_proxyTransforms == null  || _proxyTransforms.Length == 0)
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
    }
}
