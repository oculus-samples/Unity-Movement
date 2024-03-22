// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using UnityEngine;
using UnityEngine.Events;

namespace Oculus.Movement.BodyTrackingForFitness
{
    /// <summary>
    /// Used to trigger events depending on the passage of time. Very useful with animation.
    /// </summary>
    public class AutomatedTimer : MonoBehaviour
    {
        private static class AutomatedTimerTooltips
        {
            public const string Timer = "How long this timer will wait";

            public const string Repetitions =
                "How many consecutive repetitions this timer will execute";

            public const string TimerIsRunning =
                "If the timer advances during update. Set to false after final repetition finishes";

            public const string OnStart = "What to do when the timer starts";

            public const string OnEnd =
                "What to do when the timer finishes, just after this component is disabled";

            public const string OnProgress =
                "What to do as the timer is updating (passes percentage to callback)";
        }

        /// <summary>
        /// Callback to send progress through the timer
        /// </summary>
        [Serializable]
        public class UnityEvent_float : UnityEvent<float> { }

        /// <summary>
        /// How long this timer will wait
        /// </summary>
        [Tooltip(AutomatedTimerTooltips.Timer)]
        [SerializeField]
        protected float _timer = 5;

        /// <summary>
        /// How many consecutive repetitions this timer will execute
        /// </summary>
        [Tooltip(AutomatedTimerTooltips.Repetitions)]
        [SerializeField]
        protected int _repetitions = 1;

        /// <summary>
        /// If the timer advances during update. Set to false after final repetition finishes
        /// </summary>
        [Tooltip(AutomatedTimerTooltips.TimerIsRunning)]
        [SerializeField]
        private bool _timerIsRunning;

        /// <summary>
        /// What to do when the timer starts
        /// </summary>
        [Tooltip(AutomatedTimerTooltips.OnStart)]
        [SerializeField]
        protected UnityEvent _onStart;

        /// <summary>
        /// What to do when the timer finishes
        /// </summary>
        [Tooltip(AutomatedTimerTooltips.OnEnd)]
        [SerializeField]
        protected UnityEvent _onEnd;

        /// <summary>
        /// What to do as the timer is updating (passes percentage to callback)
        /// </summary>
        [Tooltip(AutomatedTimerTooltips.OnProgress)]
        [SerializeField]
        protected UnityEvent_float _onProgress;

        private float _activeTimer;
        private float _progress;
        private int _repeated;
        private bool _started;

        /// <inheritdoc cref="_timer"/>
        public float Timer
        {
            get => _timer;
            set => _timer = value;
        }

        /// <inheritdoc cref="_repetitions"/>
        public int Repetitions
        {
            get => _repetitions;
            set => _repetitions = value;
        }

        /// <inheritdoc cref="_timerIsRunning"/>
        public bool IsTimerRunning
        {
            get => _timerIsRunning;
            set => _timerIsRunning = value;
        }

        /// <summary>
        /// Restarts the timer.
        /// </summary>
        public void Restart()
        {
            _activeTimer = 0;
            _progress = 0;
            _repeated = 0;
            _started = false;
            _timerIsRunning = true;
        }

        private void Update()
        {
            if (!_timerIsRunning)
            {
                return;
            }
            if (!_started)
            {
                _onStart.Invoke();
                _started = true;
            }
            else if (_repeated >= _repetitions)
            {
                _timerIsRunning = false;
                return;
            }
            if (_timer <= 0)
            {
                Debug.LogError($"{name}.{nameof(AutomatedTimer)}.{nameof(Timer)} <= 0");
                _timerIsRunning = false;
                return;
            }
            _activeTimer += Time.deltaTime;
            UpdateProgress();
            while (_activeTimer >= _timer)
            {
                _activeTimer -= _timer;
                UpdateProgress();
            }
        }

        private void UpdateProgress()
        {
            _progress = Mathf.Clamp01(_activeTimer / _timer);
            _onProgress.Invoke(_progress);
            if (_progress >= 1)
            {
                ++_repeated;
                if (_repeated >= _repetitions)
                {
                    _timerIsRunning = false;
                    _onEnd.Invoke();
                }
            }
        }
    }
}
