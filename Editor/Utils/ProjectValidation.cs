// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEditor;
using UnityEngine;

namespace Oculus.Movement.Utils
{
    /// <summary>
    /// Validates various project settings for the samples to work correctly.
    /// </summary>
    [InitializeOnLoad]
    public class ProjectValidation
    {
        /// <summary>
        /// All character and mirrored character layers should exist based on their indices.
        /// That is because the scene assets are assigned based on layer index.
        /// </summary>
        private static readonly int[] _expectedLayersIndices =
            { 10, 11 };
        private static readonly string[] _expectedLayersNotIndexed = { "HiddenMesh" };

        static ProjectValidation()
        {
            if (!ShouldShowWindow())
            {
                return;
            }

            ProjectValidationWindow.ShowProjectValidationWindow();
        }

        /// <summary>
        /// If all expected layers are in the project, returns true.
        /// </summary>
        /// <returns>True if all expected layers are in the project.</returns>
        public static bool TestLayers()
        {
            foreach (var expectedLayer in _expectedLayersNotIndexed)
            {
                if (LayerMask.NameToLayer(expectedLayer) == -1)
                {
                    return false;
                }
            }

            foreach (var layerIndex in _expectedLayersIndices)
            {
                if (LayerMask.LayerToName(layerIndex) == null)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ShouldShowWindow()
        {
            return !TestLayers();
        }

        [MenuItem("Movement/Check Project Settings", priority = 100)]
        public static void BuildProjectAndroid64()
        {
            ProjectSettingsValidationWindow.ShowProjectSettingsValidationWindow();
        }
    }
}
