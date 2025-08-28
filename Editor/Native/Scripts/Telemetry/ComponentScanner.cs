// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System.Linq;
using UnityEditor;
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
            EditorApplication.update += ScanForObjects;
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
            if (ShouldFireTelemetryForComponent<OVREyeGaze>(ref _foundOVRFace))
            {
                TelemetryManager.SendFoundOVREyeGazeEvent();
            }
            if (ShouldFireTelemetryForComponent<OVRFaceExpressions>(ref _foundOVREye))
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
            // OVRProjectSetupUtils.FindComponentInScene seems inaccessible, so
            // just use the same logic here.
            var scene = SceneManager.GetActiveScene();
            var rootGameObjects = scene.GetRootGameObjects();
            var gameObjects = rootGameObjects.FirstOrDefault(go => go.GetComponentInChildren<T>())?.GetComponentInChildren<T>();
            if (gameObjects == null)
            {
                return false;
            }
            foundVariable = true;
            return true;
        }
    }
}
