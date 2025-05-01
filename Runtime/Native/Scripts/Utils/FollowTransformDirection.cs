// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace Meta.XR.Movement.Utils
{
    /// <summary>
    /// Component that positions this GameObject in the direction of a target transform with an offset.
    /// </summary>
    public class FollowTransformDirection : MonoBehaviour
    {
        /// <summary>
        /// The transform to follow.
        /// </summary>
        [SerializeField]
        private Transform _followTarget;

        /// <summary>
        /// The percentage of the direction vector to use as offset.
        /// </summary>
        [SerializeField]
        private float _offsetPercentage = 0.5f;

        /// <summary>
        /// The direction vector in the target's local space.
        /// </summary>
        [SerializeField]
        private Vector3 _direction = Vector3.forward;

        private void LateUpdate()
        {
            transform.position = _followTarget.TransformDirection(_direction) * _offsetPercentage;
        }
    }
}
