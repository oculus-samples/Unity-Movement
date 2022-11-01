// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace Oculus.Movement.Effects
{
    /// <summary>
    /// Activates/deactivates this game object based on the hip pinning leave event.
    /// </summary>
    public class HipPinningNotification : MonoBehaviour
    {
        /// <summary>
        /// The hip pinning logic component
        /// </summary>
        [SerializeField]
        [Tooltip(HipPinningNotificationTooltips.HipPinningLogic)]
        protected HipPinningLogic _hipPinningLogic;

        /// <summary>
        /// The amount of time that this notification should be enabled for.
        /// </summary>
        [SerializeField]
        [Tooltip(HipPinningNotificationTooltips.DisplayTime)]
        protected float _displayTime = 5.0f;

        private float _timer;

        private void Awake()
        {
            if (_hipPinningLogic)
            {
                _hipPinningLogic.OnEnterHipPinningArea += OnEnterHipPinningArea;
                _hipPinningLogic.OnExitHipPinningArea += OnExitHipPinningArea;
            }
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_hipPinningLogic)
            {
                _hipPinningLogic.OnEnterHipPinningArea -= OnEnterHipPinningArea;
                _hipPinningLogic.OnExitHipPinningArea -= OnExitHipPinningArea;
            }
        }

        private void OnEnterHipPinningArea(HipPinningTarget target)
        {
            _timer = 0.0f;
            gameObject.SetActive(false);
        }

        private void OnExitHipPinningArea(HipPinningTarget target)
        {
            _timer = _displayTime;
            gameObject.SetActive(true);
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
    }
}
