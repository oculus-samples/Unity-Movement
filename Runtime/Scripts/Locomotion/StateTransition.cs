// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Oculus.Movement.Locomotion
{
    /// <summary>
    /// Facilitate transitions into and out of a temporary state, which can optionally be held by a
    /// specific named source. If the state is not held, it will automatically transition out after
    /// a specified duration. If the state is held, it will automatically transition out after all
    /// holds are released.
    /// <code>
    /// |--------- IsStateActive == true ---------|
    /// |_enterTime| _duration or Hold            |_exitTime |
    /// |##########|##############################|##########|
    /// | Entering |                              | Exiting  |
    /// |          +Entered                       |          +Exited
    /// +EnterStart                               +ExitStart
    /// </code>
    /// </summary>
    public class StateTransition : MonoBehaviour
    {
        /// <summary>
        /// Callback useful for setting RigConstraint weights or opacity associated with a state.
        /// Float passed into the callback is normalized time.
        /// </summary>
        [Serializable] public class UnityEvent_float : UnityEvent<float> { }

        [Serializable]
        public class OnTransitionEvents
        {
            /// <summary>
            /// Callback to change a progress variable as the state transitions in
            /// </summary>
            public UnityEvent_float Entering;
            /// <summary>
            /// Callback to change a progress variable as the state transitions out
            /// </summary>
            public UnityEvent_float Exiting;
            /// <summary>
            /// Callback when the <see cref="Entering"/> transition is about to start
            /// </summary>
            public UnityEvent EnterStart;
            /// <summary>
            /// Callback when the <see cref="Exiting"/> transition is about to start
            /// </summary>
            public UnityEvent ExitStart;
            /// <summary>
            /// Callback identifying when the state has finished transitioning in
            /// </summary>
            public UnityEvent Entered;
            /// <summary>
            /// Callback identifying when the state has finished transitioning out
            /// </summary>
            public UnityEvent Exited;
        }

        /// <summary>
        /// Seconds of inactivity till the temporary state exits
        /// </summary>
        [Tooltip(StateTransitionTooltips.Duration)]
        [SerializeField]
        protected float _duration = 1;

        /// <summary>
        /// Seconds that the <see cref="OnTransitionEvents.Entering"/> callback will be called.
        /// </summary>
        [Tooltip(StateTransitionTooltips.EnterTime)]
        [SerializeField]
        protected float _enterTime = 1f/8;

        /// <summary>
        /// Seconds that the <see cref="OnTransitionEvents.Exiting"/> callback will be called.
        /// </summary>
        [Tooltip(StateTransitionTooltips.ExitTime)]
        [SerializeField]
        protected float _exitTime = 1f/8;

        /// <summary>
        /// Callbacks to trigger at specific points during the transition between states.
        /// </summary>
        [Tooltip(StateTransitionTooltips.OnTransition)]
        [SerializeField]
        protected OnTransitionEvents _onTransition = new OnTransitionEvents();

        /// <summary>
        /// True if the state is on (could be entering, or could be waiting for
        /// <see cref="_duration"/>, or state could be held by <see cref="_sourcesHoldingState"/>).
        /// </summary>
        private bool _isStateActive;

        /// <summary>
        /// True if state has just started and is transitioning to a fully on state.
        /// </summary>
        private bool _isEntering;

        /// <summary>
        /// True if temporary state is over, and is transitioning out.
        /// </summary>
        private bool _isExiting;

        /// <summary>
        /// How long the state has been waiting, or should wait.
        /// If <see cref="_isEntering"/> is true, it counts up from 0 to <see cref="_enterTime"/>
        /// If <see cref="_isExiting"/> is true, it counts down from <see cref="_exitTime"/> to 0
        /// If <see cref="_isStateActive"/>
        /// </summary>
        private float _timer;

        /// <summary>
        /// List of named sources that are requesting this state to persist. This is needed because
        /// a simple boolean is not sufficient if multiple independent sources could have
        /// overlapping needs for this state (eg: animation system used by "Walk" and/or "Jump").
        /// </summary>
        private HashSet<string> _sourcesHoldingState = new HashSet<string>();

        /// <summary>
        /// True if the state is being applied. If the value is changed, it will trigger transition
        /// periods and callbacks (<see cref="TransitionCallbacks"/>).
        /// </summary>
        public bool IsStateActive
        {
            get => _isStateActive;
            set
            {
                bool stateShouldActivate = value;
                bool stateChanging = stateShouldActivate != _isStateActive;
                if (stateChanging)
                {
                    if (stateShouldActivate)
                    {
                        if (_isExiting)
                        {
                            _isExiting = false;
                            // interrupted Exit transition
                            _timer = (_timer / _exitTime) * _enterTime;
                        }
                        else
                        {
                            // if not Exiting, restart the Enter timer
                            _timer = 0;
                        }
                        if (_enterTime > 0)
                        {
                            _isEntering = true;
                            _onTransition.EnterStart.Invoke();
                        }
                    }
                    else
                    {
                        if (_isEntering)
                        {
                            _isEntering = false;
                            // interrupted Enter transition
                            _timer = (_timer / _enterTime) * _exitTime;
                        }
                        else if (_exitTime > 0)
                        {
                            // if not Entering, restart the Exit timer
                            _timer = _exitTime;
                        }
                        if (_exitTime > 0)
                        {
                            _isExiting = true;
                            _onTransition.ExitStart.Invoke();
                        }
                    }
                }
                else if (_isStateActive)
                {
                    if (!_isEntering)
                    {
                        _timer = 0;
                    }
                }
                _isStateActive = stateShouldActivate;
            }
        }

        /// <summary>
        /// Transition callbacks, called at various points of transition between state.
        /// </summary>
        public OnTransitionEvents TransitionCallbacks => _onTransition;

        protected void Update()
        {
            UpdateState(Time.deltaTime);
        }

        protected virtual void UpdateState(float deltaTime)
        {
            if (!_isStateActive && !_isExiting)
            {
                return;
            }
            if (_isEntering)
            {
                _timer += deltaTime;
                if (_timer >= _enterTime)
                {
                    _isEntering = false;
                    _timer -= _enterTime;
                    _onTransition.Entering.Invoke(1);
                    _onTransition.Entered.Invoke();
                }
                else
                {
                    _onTransition.Entering.Invoke(_timer / _enterTime);
                }
            }
            else if (_isExiting)
            {
                _timer -= deltaTime;
                if (_timer <= 0)
                {
                    _isExiting = false;
                    _timer = 0;
                    _onTransition.Exiting.Invoke(0);
                    _onTransition.Exited.Invoke();
                }
                else
                {
                    _onTransition.Exiting.Invoke(_timer / _exitTime);
                }
            }
            else if (_isStateActive)
            {
                _timer += deltaTime;
                if (_timer >= _duration && _sourcesHoldingState.Count == 0)
                {
                    _timer = _exitTime;
                    IsStateActive = false;
                }
            }
        }

        /// <summary>
        /// Name a source wants to maintain the temporary state active
        /// </summary>
        /// <param name="source">a unique name for a source holding the state active</param>
        public void HoldStartedByNamedSource(string source)
        {
            _sourcesHoldingState.Add(source);
            IsStateActive = true;
        }

        /// <summary>
        /// Relinquish the request of a source wanting to hold the state active
        /// </summary>
        /// <param name="source">a unique name for a source holding the state on</param>
        public void HoldReleasedByNamedSource(string source)
        {
            _sourcesHoldingState.Remove(source);
            if (_sourcesHoldingState.Count == 0)
            {
                IsStateActive = false;
            }
        }
    }
}
