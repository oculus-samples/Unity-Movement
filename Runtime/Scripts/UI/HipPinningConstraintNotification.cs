// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Movement.AnimationRigging;
using UnityEngine;

namespace Oculus.Movement.UI
{
    /// <summary>
    /// Activates/deactivates this game object based on the hip pinning leave event.
    /// </summary>
    public class HipPinningConstraintNotification : MonoBehaviour
    {
        /// <summary>
        /// The hip pinning constraint.
        /// </summary>
        [SerializeField]
        [Tooltip(HipPinningConstraintNotificationTooltips.HipPinningConstraint)]
        protected HipPinningConstraint _hipPinningConstraint;

        /// <summary>
        /// The amount of time that this notification should be enabled for.
        /// </summary>
        [SerializeField]
        [Tooltip(HipPinningConstraintNotificationTooltips.DisplayTime)]
        protected float _displayTime = 5.0f;

        private float _timer;

        private void Awake()
        {
            if (_hipPinningConstraint)
            {
                _hipPinningConstraint.data.OnEnterHipPinningArea += OnEnterHipPinningArea;
                _hipPinningConstraint.data.OnExitHipPinningArea += OnExitHipPinningArea;
            }
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_hipPinningConstraint)
            {
                _hipPinningConstraint.data.OnEnterHipPinningArea -= OnEnterHipPinningArea;
                _hipPinningConstraint.data.OnExitHipPinningArea -= OnExitHipPinningArea;
            }
        }

        private void Update()
        {
            if (_timer > 0.0f)
            {
                _timer -= Time.deltaTime;
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void OnEnterHipPinningArea(HipPinningConstraintTarget target)
        {
            _timer = 0.0f;
            gameObject.SetActive(false);
        }

        private void OnExitHipPinningArea(HipPinningConstraintTarget target)
        {
            _timer = _displayTime;
            gameObject.SetActive(true);
        }
    }
}
