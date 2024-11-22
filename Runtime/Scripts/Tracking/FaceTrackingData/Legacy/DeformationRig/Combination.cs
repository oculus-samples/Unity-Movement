// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Linq;

namespace DeformationRig.Deprecated
{
    /// <summary>
    /// Defines a combination target.
    /// </summary>
    public class Combination : ICorrectiveShape
    {
        /// <summary>
        /// The blendshape index to be driven on the skinned mesh renderer.
        /// </summary>
        public int DrivenIndex;

        /// <summary>
        /// The blendshape indices used in calculating the blendshape weight for the driven index.
        /// </summary>
        public int[] DriverIndices;

        /// <inheritdoc />
        public void Apply(IBlendshapeInterface blendshapeInterface) =>
            blendshapeInterface[DrivenIndex] = DriverIndices
                .Select(idx => blendshapeInterface[idx])
                .Aggregate(1f, (cur, next) => cur * next);
    }
}
