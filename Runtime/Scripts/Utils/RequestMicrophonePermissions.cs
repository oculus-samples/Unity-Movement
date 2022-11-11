// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using UnityEngine;

namespace Oculus.Movement.Utils
{
    /// <summary>
    /// Request microphone permissions to be used as a fallback for OVRFaceExpression blendshapes
    /// when face tracking permissions aren't enabled, or if the face is obscured.
    /// </summary>
    public class RequestMicrophonePermissions : MonoBehaviour
    {
        /// <summary>
        /// True when permissions done.
        /// </summary>
        public bool PermissionsFlowDone { get; private set; } = false;


        private IEnumerator Start()
        {
            // Wait until OVRManager starts permission requests.
            while (!OVRManager.OVRManagerinitialized)
            {
                yield return null;
            }

            // Wait a frame for OVRManager permission requests to finish.
            yield return null;

            var enabledMicrophone = Microphone.devices.Length >= 1;
            Debug.Log($"Microphone is {(enabledMicrophone ? "enabled." : "disabled.")}");

            PermissionsFlowDone = true;
        }
    }
}
