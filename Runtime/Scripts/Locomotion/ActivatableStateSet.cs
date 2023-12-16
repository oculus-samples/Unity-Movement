// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using UnityEngine.Events;

namespace Oculus.Movement.Locomotion
{
    /// <summary>
    /// A state machine similar to <see cref="ActivateToggle"/>, except that this state machine
    /// also keeps track of an activation (and deactivation) per state.
    /// </summary>
    public class ActivatableStateSet : MonoBehaviour
    {
        /// <summary>
        /// Callback type that triggers with timer updates
        /// </summary>
        [System.Serializable]
        public class UnityEvent_float : UnityEvent<float> { }

        /// <summary>
        /// A set of callbacks associated with each state
        /// </summary>
        [System.Serializable]
        public class Set
        {
            public string Name;
            public UnityEvent Initialize;
            public UnityEvent Release;
            public UnityEvent Activate;
            public UnityEvent Deactivate;
        }

        /// <summary>
        /// Set of activatable states
        /// </summary>
        [Tooltip(ActivatableStateSetTooltips.Set)]
        [SerializeField]
        private Set[] _set = new Set[0];

        /// <summary>
        /// Index of current activatable state
        /// </summary>
        [Tooltip(ActivatableStateSetTooltips.Index)]
        [SerializeField]
        private int _index = 0;

        /// <summary>
        /// Minimum number of seconds a state can count as active. Exit time.
        /// </summary>
        [Tooltip(ActivatableStateSetTooltips.MinimumActivationDuration)]
        [SerializeField]
        private float _minimumActivateDuration = 1;

        /// <summary>
        /// Callback to notify as the activation timeout timer advances
        /// </summary>
        [Tooltip(ActivatableStateSetTooltips.OnTimerChange)]
        [SerializeField]
        private UnityEvent_float _onTimerChange;

        private bool _deactivateAfterDuration = true;

        private float _activeTimer;

        /// <inheritdoc cref="_index"/>
        public int Index
        {
            get => _index;
            set
            {
                if (_index >= 0 && _index < _set.Length)
                {
                    Release();
                }
                _index = value;
                Initialize();
            }
        }

        private void OnEnable()
        {
            Initialize();
        }

        private void OnDisable()
        {
            Release();
        }

        private void Update()
        {
            if (_deactivateAfterDuration && _activeTimer >= 0)
            {
                _activeTimer -= Time.deltaTime;
                if (_activeTimer <= 0)
                {
                    Deactivate();
                    _activeTimer = 0;
                }
                NotifyTimerListener();
            }
        }

        private void NotifyTimerListener()
        {
            float progress = _deactivateAfterDuration && _activeTimer > 0
                ? 1f - (_activeTimer / _minimumActivateDuration) : 0;
            _onTimerChange.Invoke(progress);
        }

        private void Initialize()
        {
            _set[_index].Initialize.Invoke();
        }

        private void Release()
        {
            _set[_index].Release.Invoke();
        }

        /// <summary>
        /// Activate the current state
        /// </summary>
        public void Activate()
        {
            _set[_index].Activate.Invoke();
            RefreshActiveDuration();
        }

        /// <summary>
        /// Deactivate the current state
        /// </summary>
        public void Deactivate()
        {
            _set[_index].Deactivate.Invoke();
        }

        /// <summary>
        /// Activate the current state without advancing the timeout timer
        /// </summary>
        public void ActivatePersistent()
        {
            _deactivateAfterDuration = false;
            Activate();
        }

        /// <summary>
        /// Ensure the timeout timer starts and deactivates the state when it reaches its duration
        /// </summary>
        public void DeactivateAfterDuration()
        {
            _deactivateAfterDuration = true;
            RefreshActiveDuration();
        }

        /// <summary>
        /// Restart the timeout timer
        /// </summary>
        public void RefreshActiveDuration()
        {
            _activeTimer = _minimumActivateDuration;
            NotifyTimerListener();
        }
    }
}
