// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Movement.Effects
{
    /// <summary>
    /// Mirrors an object by copying its local transformation values.
    /// </summary>
    [DefaultExecutionOrder(300)]
    public class LateMirroredObject : MonoBehaviour
    {
        /// <summary>
        /// Contains information about a mirrored transform pair.
        /// </summary>
        [System.Serializable]
        public class MirroredTransformPair
        {
            /// <summary>
            /// The name of the mirrored transform pair.
            /// </summary>
            [HideInInspector] public string Name;

            /// <summary>
            /// The original transform.
            /// </summary>
            [Tooltip(LateMirroredObjectTooltips.MirroredTransformPairTooltips.OriginalTransform)]
            public Transform OriginalTransform;

            /// <summary>
            /// The mirrored transform.
            /// </summary>
            [Tooltip(LateMirroredObjectTooltips.MirroredTransformPairTooltips.MirroredTransform)]
            public Transform MirroredTransform;
        }

        /// <summary>
        /// Returns the original transform.
        /// </summary>
        public Transform OriginalTransform => _transformToCopy;

        /// <summary>
        /// Returns the mirrored transform.
        /// </summary>
        public Transform MirroredTransform => _myTransform;

        /// <summary>
        /// The transform which transform values are being mirrored from.
        /// </summary>
        [SerializeField]
        [Tooltip(LateMirroredObjectTooltips.TransformToCopy)]
        protected Transform _transformToCopy;

        /// <summary>
        /// The target transform which transform values are being mirrored to.
        /// </summary>
        [SerializeField]
        [Tooltip(LateMirroredObjectTooltips.MyTransform)]
        protected Transform _myTransform;

        /// <summary>
        /// The array of mirrored transform pairs.
        /// </summary>
        [SerializeField]
        [Tooltip(LateMirroredObjectTooltips.MirroredTransformPairs)]
        protected MirroredTransformPair[] _mirroredTransformPairs;

        private void Awake()
        {
            Assert.IsNotNull(_transformToCopy);
            Assert.IsNotNull(_myTransform);
            foreach (var mirroredTransformPair in _mirroredTransformPairs)
            {
                Assert.IsNotNull(mirroredTransformPair.OriginalTransform);
                Assert.IsNotNull(mirroredTransformPair.MirroredTransform);
            }
        }

        /// <summary>
        /// Mirror in late update.
        /// </summary>
        private void LateUpdate()
        {
            _myTransform.localPosition = _transformToCopy.localPosition;
            _myTransform.localRotation = _transformToCopy.localRotation;
            foreach (var transformPair in _mirroredTransformPairs)
            {
                transformPair.MirroredTransform.localPosition = transformPair.OriginalTransform.localPosition;
                transformPair.MirroredTransform.localRotation = transformPair.OriginalTransform.localRotation;
            }
        }
    }
}
