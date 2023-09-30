// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace Oculus.Movement.Locomotion
{
    /// <summary>
    /// Respawns the player if they fall off "the world"
    /// </summary>
    public class ReappearAfterFall : MonoBehaviour
    {
        /// <summary>
        /// Should reference the main <see cref="Rigidbody"/> of the character controller
        /// </summary>
        [Tooltip(ReappearAfterFallTooltips.Rigidbody)]
        [SerializeField]
        private Rigidbody _rigidbody;

        /// <summary>
        /// When _rigidbody.transform.position.y is lower than this value, restart it
        /// </summary>
        [Tooltip(ReappearAfterFallTooltips.MinimumY)]
        [SerializeField]
        private float _minimumY = -10;

        private Vector3 _startPosition;
        private Quaternion _startRotation;

        private void Start()
        {
            if (_rigidbody == null)
            {
                _rigidbody = GetComponentInParent<Rigidbody>();
                Debug.LogWarning($"{this} is missing a {nameof(_rigidbody)} value, using \"{_rigidbody}\"");
            }
            _startPosition = _rigidbody.position;
            _startRotation = _rigidbody.rotation;
        }

        private void FixedUpdate()
        {
            if (_rigidbody.position.y < _minimumY)
            {
                ReappearAtStart();
            }
        }

        /// <summary>
        /// Restarts the managed rigibody at it's starting position
        /// </summary>
        public void ReappearAtStart()
        {
            _rigidbody.position = _startPosition;
            _rigidbody.rotation = _startRotation;
            _rigidbody.velocity = _rigidbody.angularVelocity = Vector3.zero;
        }
    }
}
