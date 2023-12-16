// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using UnityEngine.Events;

namespace Oculus.Movement.Utils
{
    /// <summary>
    /// Recognizes if runtime has functional body tracking, and notifies with callbacks.
    /// </summary>
    public class OVRBodyTrackingStateListener : MonoBehaviour
    {
        /// <summary>
        /// General availability state of body tracking
        /// </summary>
        public enum BodyInputState
        {
            /// <summary>
            /// Calculation is not yet finished
            /// </summary>
            None,
            /// <summary>
            /// The headset is not even connected
            /// </summary>
            HeadsetNotConnected,
            /// <summary>
            /// Headset is connected, but body tracking is missing (permissions, OS, Link version)
            /// </summary>
            BodyTrackingNotAvailable,
            /// <summary>
            /// Body tracking data is available, all is well.
            /// </summary>
            BodyTrackingAvailable
        }

        /// <summary>
        /// Named body tracking state listeners
        /// </summary>
        [System.Serializable]
        public class StateListeners
        {
            public UnityEvent HeadsetNotConnected = new UnityEvent();
            public UnityEvent BodyTrackingNotWorking = new UnityEvent();
            public UnityEvent BodyTrackingWorking = new UnityEvent();

            public void ExecuteCallbacksByState(BodyInputState state)
            {
                switch (state)
                {
                    case BodyInputState.HeadsetNotConnected:
                        HeadsetNotConnected.Invoke();
                        break;
                    case BodyInputState.BodyTrackingNotAvailable:
                        BodyTrackingNotWorking.Invoke();
                        break;
                    case BodyInputState.BodyTrackingAvailable:
                        BodyTrackingWorking.Invoke();
                        break;
                }
            }
        }

        private BodyInputState _currentState;

        /// <summary>
        /// Named body tracking state listeners
        /// </summary>
        [Tooltip(OVRBodyTrackingStateListenerTooltips.StateListeners)]
        [SerializeField]
        protected StateListeners _stateListeners;

        /// <summary>
        /// How many seconds before body tracking times out and is considered disconnected
        /// </summary>
        [Tooltip(OVRBodyTrackingStateListenerTooltips.MaxWaitTimeForBodyTracking)]
        [SerializeField]
        private float _maxWaitTimeForBodyTracking = 2;

        private float _durationBodyTrackingIsntWorking;

        private static OVRPlugin.BodyState _bodyState;

        private void Start()
        {
            _currentState = BodyInputState.None;
            _durationBodyTrackingIsntWorking = 0;
        }

        private void Refresh()
        {
            if (_currentState == BodyInputState.BodyTrackingAvailable)
            {
                return;
            }
            BodyInputState nextState = _currentState;
            if (nextState == BodyInputState.None)
            {
                nextState = BodyInputState.HeadsetNotConnected;
            }
            if (nextState == BodyInputState.HeadsetNotConnected && IsHeadsetConnected())
            {
                nextState = BodyInputState.BodyTrackingNotAvailable;
            }
            if (nextState == BodyInputState.BodyTrackingNotAvailable && IsBodyWorking())
            {
                nextState = BodyInputState.BodyTrackingAvailable;
            }
            if (_durationBodyTrackingIsntWorking < _maxWaitTimeForBodyTracking)
            {
                // ignore lag spikes, like when a scene is loading in the editor
                if (Time.unscaledDeltaTime > 1)
                {
                    return;
                }
                _durationBodyTrackingIsntWorking += Time.unscaledDeltaTime;
                if (nextState != BodyInputState.BodyTrackingAvailable)
                {
                    return;
                }
            }
            if (_currentState != nextState)
            {
                _stateListeners.ExecuteCallbacksByState(nextState);
                _currentState = nextState;
                enabled = false;
            }
        }

        /// <summary>
        /// Check if the headset is connected in this scene. TODO validate with other experts
        /// </summary>
        /// <returns></returns>
        public static bool IsHeadsetConnected()
        {
            return Application.platform == RuntimePlatform.Android || OVRManager.runtimeSettings != null;
        }

        /// <summary>
        /// Check if the body tracking is working in this scene.
        /// </summary>
        /// <returns></returns>
        private static bool IsBodyWorking()
        {
            return OVRPlugin.GetBodyState(OVRPlugin.Step.Render, ref _bodyState);
        }

        private void Update()
        {
            Refresh();
        }
    }
}
