// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Movement.Effects.Deprecated
{
    /// <summary>
    /// The root of the mirrored character will mirror the entire hierarchy.
    /// We just need to rely on the original eyes to rotate. If we copy their
    /// local transforms, the transforms of the root will effectively mirror
    /// the eyes properly into world space.
    /// </summary>
    public class TransformMirroredEyes : MonoBehaviour
    {
        /// <summary>
        /// The original left eye that will be mirrored.
        /// </summary>
        [SerializeField]
        [Tooltip(TransformMirroredEyesTooltips.LeftEyeOriginal)]
        protected Transform _leftEyeOriginal;

        /// <summary>
        /// The to-be-mirrored left eye.
        /// </summary>
        [SerializeField]
        [Tooltip(TransformMirroredEyesTooltips.LeftEyeMirrored)]
        protected Transform _leftEyeMirrored;

        /// <summary>
        /// The original right eye that will be mirrored.
        /// </summary>
        [SerializeField]
        [Tooltip(TransformMirroredEyesTooltips.RightEyeOriginal)]
        protected Transform _rightEyeOriginal;

        /// <summary>
        /// The to-be-mirrored right eye.
        /// </summary>
        [SerializeField]
        [Tooltip(TransformMirroredEyesTooltips.RightEyeMirrored)]
        protected Transform _rightEyeMirrored;

        private void Awake()
        {
            Assert.IsNotNull(_leftEyeOriginal);
            Assert.IsNotNull(_leftEyeMirrored);
            Assert.IsNotNull(_rightEyeOriginal);
            Assert.IsNotNull(_rightEyeMirrored);
        }

        private void LateUpdate()
        {
            CopyLocalTransform(_leftEyeMirrored, _leftEyeOriginal);
            CopyLocalTransform(_rightEyeMirrored, _rightEyeOriginal);
        }

        private static void CopyLocalTransform(Transform dest, Transform src)
        {
            dest.localPosition = src.localPosition;
            dest.localRotation = src.localRotation;
        }
    }
}
