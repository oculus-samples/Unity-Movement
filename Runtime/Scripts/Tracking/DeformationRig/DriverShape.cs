// Copyright (c) Meta Platforms, Inc. and affiliates.

namespace DeformationRig
{
    /// <summary>
    /// A faux-Corrective representing a driver shape.
    /// </summary>
    public class DriverShape : ICorrectiveShape
    {
        /// <summary>
        /// The driver and driven index of this shape.
        /// </summary>
        public int Index { get; private set; }

        public DriverShape(int index) => Index = index;

        /// <inheritdoc />
        public void Apply(IBlendshapeInterface blendshapeInterface) =>
            blendshapeInterface[Index] = blendshapeInterface[Index];
    }
}
