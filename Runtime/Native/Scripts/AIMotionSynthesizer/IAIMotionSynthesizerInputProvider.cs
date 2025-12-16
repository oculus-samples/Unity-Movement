// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using UnityEngine;

namespace Meta.XR.Movement.AI
{
    /// <summary>
    /// Interface for providing locomotion input data to AI Motion Synthesizer.
    /// Implement this interface to create custom input providers for joysticks, AI navigation, etc.
    /// </summary>
    public interface IAIMotionSynthesizerInputProvider
    {
        /// <summary>
        /// Gets the current movement velocity in world space (meters/second).
        /// </summary>
        Vector3 GetVelocity();

        /// <summary>
        /// Gets the current facing direction in world space.
        /// </summary>
        Vector3 GetDirection();

        /// <summary>
        /// Whether the input provider is actively receiving input.
        /// Used for blend factor transitions.
        /// </summary>
        bool IsInputActive();

        /// <summary>
        /// Reference transform for coordinate space conversion.
        /// Typically the camera rig for camera-relative input, or character root for world-relative.
        /// </summary>
        Transform GetReferenceTransform();
    }
}
