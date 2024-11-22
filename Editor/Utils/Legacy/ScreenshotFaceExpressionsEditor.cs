// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEditor;
using UnityEngine;

namespace Oculus.Movement.Utils.Deprecated
{
    /// <summary>
    /// Editor class defining interface for screenshot face expressions.
    /// </summary>
    [CustomEditor(typeof(ScreenshotFaceExpressions))]
    public class ScreenshotFaceExpressionsEditor : Editor
    {
        /// <summary>
        /// Defines the look of the script's GUI.
        /// </summary>
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (GUILayout.Button("Start taking screenshots"))
            {
                var script = target as ScreenshotFaceExpressions;
                if (script)
                {
                    script.StartTakingBlendshapeScreenshots();
                }
            }
        }
    }
}
