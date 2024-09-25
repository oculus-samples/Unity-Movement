// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace Oculus.Movement.Tracking
{
    /// <summary>
    /// Allows restarting tracking after the HMD is unmounted and remounted.
    /// This can be used to resolve body tracking errors that might be seen after
    /// remounting the HMD.
    /// </summary>
    public class HMDRemountRestartTracking : MonoBehaviour
    {
        private bool _wasUnmountedBefore = false;

        private void Start()
        {
            OVRManager.HMDMounted += HandleHMDMounted;
            OVRManager.HMDUnmounted += HandleHMDUnmounted;
        }

        private void OnDestroy()
        {
            OVRManager.HMDMounted -= HandleHMDMounted;
            OVRManager.HMDUnmounted -= HandleHMDUnmounted;
        }

        private void HandleHMDMounted()
        {
            // If the HMD was removed before a mount event, attempt a reboot of tracking.
            // Otherwise, don't bother.
            if (!_wasUnmountedBefore)
            {
                return;
            }

            Debug.Log("Rebooting body tracking after HMD remounted. Stopping...");
            OVRPlugin.StopBodyTracking();

            Debug.Log("Starting body tracking.");
            OVRPlugin.BodyJointSet currentJointSet =
                OVRRuntimeSettings.GetRuntimeSettings().BodyTrackingJointSet;
            if (!OVRPlugin.StartBodyTracking2(currentJointSet))
            {
                Debug.LogWarning(
                    $"Failed to start body tracking with joint set {currentJointSet} after remounting HMD.");
            }

            OVRPlugin.BodyTrackingFidelity2 currentFidelity =
                OVRRuntimeSettings.GetRuntimeSettings().BodyTrackingFidelity;
            bool fidelityChangeSuccessful = OVRPlugin.RequestBodyTrackingFidelity(currentFidelity);
            if (!fidelityChangeSuccessful)
            {
                Debug.LogWarning($"Failed to set Body Tracking fidelity to: {currentFidelity} after remounting HMD.");
            }

            _wasUnmountedBefore = false;
        }

        private void HandleHMDUnmounted()
        {
            _wasUnmountedBefore = true;
        }
    }
}
