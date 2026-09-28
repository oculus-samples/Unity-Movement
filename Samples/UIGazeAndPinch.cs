// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System;
using System.Collections;
using Oculus.Interaction;
using UnityEngine;

namespace Meta.XR.Movement.Samples
{
    public class UIGazeAndPinch : MonoBehaviour, ISelector
    {
        [SerializeField] private RayInteractor _rayInteractor;

        public event Action WhenSelected = delegate { };
        public event Action WhenUnselected = delegate { };

        private bool _hasPermission;

        private void OnEnable() => StartCoroutine(WaitForPermission());

        private IEnumerator WaitForPermission()
        {
            if (!OVRPermissionsRequester.IsPermissionGranted(OVRPermissionsRequester.Permission.EyeTracking))
            {
                Debug.Log($"[UIGazeAndPinch]: {nameof(OVRPermissionsRequester.Permission.EyeTracking)} is not granted");
                yield return null;
            }
            while (!OVRPermissionsRequester.IsPermissionGranted(OVRPermissionsRequester.Permission.EyeTracking))
            {
                yield return null;
            }
            _hasPermission = true;
        }

        private void OnDisable()
        {
            _hasPermission = false;
            StopAllCoroutines();
        }

        private void Update()
        {
            const OVRPlugin.HandFingerPinch handFingerPinch = OVRPlugin.HandFingerPinch.Index;
            if (InputManager.GetPinchStarted(handFingerPinch))
            {
                WhenSelected();
            }
            if (InputManager.GetPinchEnded(handFingerPinch))
            {
                WhenUnselected();
            }

            if (_hasPermission)
            {
                OVRPlugin.EyeGazeInteractionState gazeState = default;
                bool hasGazeState = OVRPlugin.GetEyeGazeInteractionState(OVRPlugin.Step.Render, -1, ref gazeState) && gazeState.IsValid;
                _rayInteractor.enabled = hasGazeState;
                if (hasGazeState)
                {
                    OVRPose gazePose = gazeState.Pose.ToOVRPose();
                    _rayInteractor.transform.SetPositionAndRotation(gazePose.position, gazePose.orientation);
                }
            }
            else
            {
                _rayInteractor.enabled = false;
            }
        }
    }
}
