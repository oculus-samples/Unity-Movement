// Copyright (c) Meta Platforms, Inc. and affiliates.

namespace DeformationRig
{
    /// <summary>
    /// An interface describing a corrective shape.
    ///
    /// A corrective shape generally operates by taking inputs (previously set shape weights) and
    /// updating one or more other shapes' weights. All implementers should implement their version
    /// of this logic in their override of the Apply method.
    /// </summary>
    public interface ICorrectiveShape
    {
        /// <summary>
        /// Calculate the weight of this corrective shape and apply it to the shape (or shapes)
        /// it drives.
        /// </summary>
        void Apply(IBlendshapeInterface blendshapeInterface);
    }
}
