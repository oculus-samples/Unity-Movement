// Copyright (c) Meta Platforms, Inc. and affiliates.

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// Interface for skeletal constraint component.
    /// </summary>
    public interface IOVRSkeletonConstraint
    {
        /// <summary>
        /// Regenerate any data for the constraint when it's being recreated.
        /// </summary>
        void RegenerateData();
    }
}
