// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Movement.Effects
{
    /// <summary>
    /// Mirrors a skeleton by copying its local transformation values.
    /// </summary>
    [DefaultExecutionOrder(-70)]
    public class MirrorSkeleton : MonoBehaviour
    {
        /// <summary>
        /// The skeleton which transform values are being mirrored from.
        /// </summary>
        [SerializeField]
        [Tooltip(MirrorSkeletonTooltips.SkeletonToCopy)]
        protected OVRCustomSkeleton _skeletonToCopy;

        /// <summary>
        /// The target skeleton which transform values are being mirrored to.
        /// </summary>
        [SerializeField]
        [Tooltip(MirrorSkeletonTooltips.MySkeleton)]
        protected OVRCustomSkeleton _mySkeleton;

        /// <summary>
        /// Returns the original skeleton that the mirrored skeleton is mirroring.
        /// </summary>
        public OVRCustomSkeleton OriginalSkeleton => _skeletonToCopy;

        /// <summary>
        /// Returns the mirrored skeleton.
        /// </summary>
        public OVRCustomSkeleton MirroredSkeleton => _mySkeleton;

        private void Awake()
        {
            Assert.IsNotNull(_skeletonToCopy);
            Assert.IsNotNull(_mySkeleton);
        }

        /// <summary>
        /// Mirror in update right after OVRSkeleton.
        /// </summary>
        private void Update()
        {
            if (_skeletonToCopy.IsDataValid)
            {
                ApplyMirroring();
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
