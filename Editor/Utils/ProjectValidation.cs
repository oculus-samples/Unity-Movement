// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEditor;

namespace Oculus.Movement.Utils
{
    /// <summary>
    /// Validates various project settings for the samples to work correctly.
    /// </summary>
    [InitializeOnLoad]
    public class ProjectValidation
    {
        [MenuItem("Movement/Check Project Settings", priority = 100)]
        public static void CheckProjectSettings()
        {
            ProjectSettingsValidationWindow.ShowProjectSettingsValidationWindow();
        }
    }
}
