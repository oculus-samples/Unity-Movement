// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Meta.XR.Movement.Editor
{
    /// <summary>
    /// Scans components relevant for telemetry events.
    /// </summary>
    [InitializeOnLoad]
    public class ComponentScanner : MonoBehaviour
    {
        private static bool _foundOVRBody = false;
        private static bool _foundOVRFace = false;
        private static bool _foundOVREye = false;

        static ComponentScanner()
        {
            EditorApplication.delayCall += ScanForObjects;
            EditorSceneManager.sceneOpened += (scene, mode) => ScanForObjects();
            EditorSceneManager.sceneSaved += scene => ScanForObjects();
        }

        /// <summary>
        /// Scans for components and triggers telemetry events related to each one.
        /// </summary>
        public static void ScanForObjects()
        {
            if (ShouldFireTelemetryForComponent<OVRBody>(ref _foundOVRBody))
            {
                TelemetryManager.SendFoundOVRBodyEvent();
            }
            if (ShouldFireTelemetryForComponent<OVREyeGaze>(ref _foundOVREye))
            {
                TelemetryManager.SendFoundOVREyeGazeEvent();
            }
            if (ShouldFireTelemetryForComponent<OVRFaceExpressions>(ref _foundOVRFace))
            {
                TelemetryManager.SendFoundOVRFaceExpressionsEvent();
            }
        }

        private static bool ShouldFireTelemetryForComponent<T>(ref bool foundVariable) where T : Component
        {
            if (foundVariable)
            {
                return false;
            }
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.GetComponentInChildren<T>() != null)
                {
                    foundVariable = true;
                    return true;
                }
            }
            return false;
        }
    }
}
