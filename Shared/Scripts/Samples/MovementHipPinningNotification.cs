// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using Meta.XR.Movement.Retargeting;
using Meta.XR.Movement.Utils;
using UnityEngine;

namespace Meta.XR.Movement.Samples
{
    /// <summary>
    /// Activates/deactivates this game object based on the hip pinning leave event.
    /// </summary>
    public class MovementHipPinningNotification : MonoBehaviour
    {
        [SerializeField]
        protected CharacterRetargeter _retargeter;

        /// <summary>
        /// The amount of time that this notification should be enabled for.
        /// </summary>
        [SerializeField]
        protected float _displayTime = 5.0f;

        private float _timer;
        private HipPinningSkeletalProcessor _hipPinningProcessor;

        private void Awake()
        {
            _hipPinningProcessor = _retargeter.GetTargetProcessor<HipPinningSkeletalProcessor>();

            if (_hipPinningProcessor != null)
            {
                _hipPinningProcessor.OnEnterHipPinningArea += OnEnterHipPinningArea;
                _hipPinningProcessor.OnExitHipPinningArea += OnExitHipPinningArea;
            }

            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_hipPinningProcessor != null)
            {
                _hipPinningProcessor.OnEnterHipPinningArea -= OnEnterHipPinningArea;
                _hipPinningProcessor.OnExitHipPinningArea -= OnExitHipPinningArea;
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

        private void OnEnterHipPinningArea()
        {
            _timer = 0.0f;
            gameObject.SetActive(false);
        }

        private void OnExitHipPinningArea()
        {
            _timer = _displayTime;
            gameObject.SetActive(true);
        }
    }
}
