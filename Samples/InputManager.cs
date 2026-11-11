// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System;
using UnityEngine;

namespace Meta.XR.Movement.Samples
{
    public class InputManager : MonoBehaviour
    {
        private const int _numHands = 2;
        private static InputManager Instance { get; set; }
        private readonly OVRPlugin.HandState[] _prevState = new OVRPlugin.HandState[_numHands];
        private readonly OVRPlugin.HandState[] _currentState = new OVRPlugin.HandState[_numHands];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AfterSceneLoad()
        {
            var go = new GameObject(nameof(InputManager));
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<InputManager>();
        }

        private void Update()
        {
            for (int i = 0; i < _numHands; i++)
            {
                _prevState[i] = _currentState[i];
                OVRPlugin.GetHandState(OVRPlugin.Step.Render, (OVRPlugin.Hand)i, ref _currentState[i]);
            }
        }

        public static bool IsButtonADownOrPinchStarted() => OVRInput.GetDown(OVRInput.RawButton.A) || GetPinchStarted(OVRPlugin.HandFingerPinch.Index);
        public static bool IsButtonBDownOrMiddleFingerPinchStarted() => OVRInput.GetDown(OVRInput.RawButton.B) || GetPinchStarted(OVRPlugin.HandFingerPinch.Middle);

        public static bool GetPinchStarted(OVRPlugin.HandFingerPinch finger) => Instance.GetPinchStarted(OVRPlugin.Hand.HandLeft, finger) || Instance.GetPinchStarted(OVRPlugin.Hand.HandRight, finger);

        private bool GetPinchStarted(OVRPlugin.Hand hand, OVRPlugin.HandFingerPinch finger)
        {
            if (hand == OVRPlugin.Hand.None)
            {
                throw new Exception("hand parameter is None");
            }
            int handIndex = (int)hand;
            bool prevPinched = (_prevState[handIndex].Pinches & finger) != 0;
            bool curPinched = (_currentState[handIndex].Pinches & finger) != 0;
            return !prevPinched && curPinched;
        }

        public static bool GetPinchEnded(OVRPlugin.HandFingerPinch finger) => Instance.GetPinchEnded(OVRPlugin.Hand.HandLeft, finger) || Instance.GetPinchEnded(OVRPlugin.Hand.HandRight, finger);

        private bool GetPinchEnded(OVRPlugin.Hand hand, OVRPlugin.HandFingerPinch finger)
        {
            if (hand == OVRPlugin.Hand.None)
            {
                throw new Exception("hand parameter is None");
            }
            int handIndex = (int)hand;
            bool prevPinched = (_prevState[handIndex].Pinches & finger) != 0;
            bool curPinched = (_currentState[handIndex].Pinches & finger) != 0;
            return prevPinched && !curPinched;
        }
    }
}
