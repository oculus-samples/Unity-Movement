// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// Constraint interface. Used to manually trigger constraints at runtime instead of
    /// relying on Unity to do that.
    /// </summary>
    public interface IJointConstraint
    {
        /// <summary>
        /// Updates constraint at runtime.
        /// </summary>
        void Update();
    }
}
