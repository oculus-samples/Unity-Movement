// Copyright (c) Meta Platforms, Inc. and affiliates.

using DeformationRig.Utils;
using UnityEngine;

namespace DeformationRig
{
    /// <summary>
    /// An abstraction over the functionality for getting and setting blendshape weights by index.
    ///
    /// This provides a nice seam to both improve testability as well as separate the implementation
    /// of the deformation rig from the exact driving logic.
    /// </summary>
    public interface IBlendshapeInterface
    {
        /// <summary>
        /// Get or set the weight of a blendshape at a given index. The returned value (and the
        /// value being set) should be in the range [0, 1].
        /// </summary>
        public float this[int index] { get; set; }
    }

    /// <summary>
    /// An implementation of IBlendshapeInterface for Unity's SkinnedMeshRenderer.
    /// </summary>
    public class SkinnedMeshRendererBlendshapeInterface : IBlendshapeInterface
    {
        private SkinnedMeshRenderer _renderer;

        public SkinnedMeshRendererBlendshapeInterface(SkinnedMeshRenderer renderer)
        {
            _renderer = renderer;
        }

        /// <inheritdoc />
        public float this[int index]
        {
            get
            {
                return ConversionHelpers.PercentToDecimal(_renderer.GetBlendShapeWeight(index));
            }
            set
            {
                _renderer.SetBlendShapeWeight(index, ConversionHelpers.DecimalToPercent(value));
            }
        }
    }
}
