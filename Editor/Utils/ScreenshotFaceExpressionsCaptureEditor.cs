// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEditor;
using UnityEngine;

namespace Oculus.Movement.Utils
{
    /// <summary>
    /// Custom Editor for <see cref="ScreenshotFaceExpressionsCapture"/> component.
    /// </summary>
    [CustomEditor(typeof(ScreenshotFaceExpressionsCapture))]
    public class ScreenshotFaceExpressionsCaptureEditor : Editor
    {
        /// <inheritdoc />
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (GUILayout.Button("Start taking screenshots"))
            {
                var script = target as ScreenshotFaceExpressionsCapture;
                if (script)
                {
                    script.StartTakingFaceExpressionScreenshots();
                }
            }
        }
    }
}
