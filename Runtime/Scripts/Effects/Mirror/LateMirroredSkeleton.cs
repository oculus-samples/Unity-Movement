// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Movement.Effects
{
    /// <summary>
    /// Mirrors a skeleton by copying its local transformation values.
    /// </summary>
    [DefaultExecutionOrder(400)]
    public class LateMirroredSkeleton : MonoBehaviour
    {
        /// <summary>
        /// Contains information about a mirrored bone pair.
        /// </summary>
        [System.Serializable]
        public class MirroredBonePair
        {
            /// <summary>
            /// The name of the mirrored bone pair.
            /// </summary>
            [HideInInspector] public string Name;

            /// <summary>
            /// If true, this mirrored bone should be reparented to match the original bone.
            /// </summary>
            [Tooltip(LateMirroredSkeletonTooltips.MirroredBonePairTooltips.ShouldBeReparented)]
            public bool ShouldBeReparented;

            /// <summary>
            /// The original bone.
            /// </summary>
            [Tooltip(LateMirroredSkeletonTooltips.MirroredBonePairTooltips.OriginalBone)]
            public Transform OriginalBone;

            /// <summary>
            /// The mirrored bone.
            /// </summary>
            [Tooltip(LateMirroredSkeletonTooltips.MirroredBonePairTooltips.MirroredBone)]
            public Transform MirroredBone;
        }

        /// <summary>
        /// Returns the original skeleton that the mirrored skeleton is mirroring.
        /// </summary>
        public OVRCustomSkeleton OriginalSkeleton => _skeletonToCopy;

        /// <summary>
        /// Returns the mirrored skeleton.
        /// </summary>
        public OVRCustomSkeleton MirroredSkeleton => _mySkeleton;

        /// <summary>
        /// The skeleton which transform values are being mirrored from.
        /// </summary>
        [SerializeField]
        [Tooltip(LateMirroredSkeletonTooltips.SkeletonToCopy)]
        protected OVRCustomSkeleton _skeletonToCopy;

        /// <summary>
        /// The target skeleton which transform values are being mirrored to.
        /// </summary>
        [SerializeField]
        [Tooltip(LateMirroredSkeletonTooltips.MySkeleton)]
        protected OVRCustomSkeleton _mySkeleton;

        /// <summary>
        /// The array of mirrored bone pairs.
        /// </summary>
        [SerializeField]
        [Tooltip(LateMirroredSkeletonTooltips.MirroredBonePairs)]
        protected MirroredBonePair[] _mirroredBonePairs;

        private bool _reparentedBones = false;

        private void Awake()
        {
            Assert.IsNotNull(_skeletonToCopy);
            Assert.IsNotNull(_mySkeleton);
            foreach (var mirroredBonePair in _mirroredBonePairs)
            {
                Assert.IsNotNull(mirroredBonePair.OriginalBone);
                Assert.IsNotNull(mirroredBonePair.MirroredBone);
            }
        }

        /// <summary>
        /// Mirror in late update after animation rigging.
        /// </summary>
        private void LateUpdate()
        {
            if (_mySkeleton.IsInitialized  && _skeletonToCopy.IsInitialized)
            {
                // Reparent any bones under OVRSkeleton if required.
                // This is needed for any transforms that aren't updated by OVRSkeleton
                // but should share the same parent as bones updated by OVRSkeleton.
                if (!_reparentedBones)
                {
                    ReparentBones();
                    _reparentedBones = true;
                }

                ApplyMirroring();
                foreach (var bonePair in _mirroredBonePairs)
                {
                    bonePair.MirroredBone.localPosition = bonePair.OriginalBone.localPosition;
                    bonePair.MirroredBone.localRotation = bonePair.OriginalBone.localRotation;
                }
            }
        }

        private void ReparentBones()
        {
            foreach (var bonePair in _mirroredBonePairs)
            {
                if (bonePair.ShouldBeReparented)
                {
                    // Re-parent under OVRSkeleton.
                    bonePair.MirroredBone.SetParent(_mySkeleton.CustomBones[0].parent);
                }
            }
        }

        private void ApplyMirroring()
        {
            var myBones = _mySkeleton.CustomBones;
            var theirBones = _skeletonToCopy.CustomBones;

            if (myBones == null || theirBones == null)
            {
                return;
            }

            for (int i = 0; i < myBones.Count; i++)
            {
                if (i > theirBones.Count - 1)
                {
                    continue;
                }

                if (myBones[i] == null || theirBones[i] == null)
                {
                    continue;
                }

                myBones[i].localPosition = theirBones[i].localPosition;
                myBones[i].localRotation = theirBones[i].localRotation;
            }
        }
    }
}
