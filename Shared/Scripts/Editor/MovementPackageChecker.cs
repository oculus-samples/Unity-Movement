// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace Meta.XR.Movement.Samples
{
    /// <summary>
    /// Checks packages in project and warns the user if a necessary one
    /// is not installed. These would be packages that Runtime doesn't automatically
    /// bring in.
    /// </summary>
    [InitializeOnLoad]
    public class MovementPackageChecker
    {
        private static readonly HashSet<string> _samplePackages = new()
        {
            "com.meta.xr.sdk.interaction.ovr"
        };

        static readonly ListRequest _request;

        /// <summary>
        /// Constructor.
        /// </summary>
        static MovementPackageChecker()
        {
            if (ShouldRunPackageCheck())
            {
                EditorApplication.update += CheckForPackageInstallation;
                _request = Client.List();
            }
        }

        private static bool ShouldRunPackageCheck()
        {
            // Check if specific scripts are present in the scene
            return Object.FindFirstObjectByType<MovementSceneLoader>() != null;
        }

        private static void CheckForPackageInstallation()
        {
            if (!_request.IsCompleted)
            {
                return;
            }

            if (_request.Status == StatusCode.Success)
            {
                var packagesInProject = _request.Result.Select(p => p.name).ToHashSet();
                foreach (var package in _samplePackages)
                {
                    if (!packagesInProject.Contains(package))
                    {
                        Debug.LogError(
                            $"Project is missing package {package}, which is required for the MSDK sample scenes.");
                    }
                }
            }

            EditorApplication.update -= CheckForPackageInstallation;
        }
    }
}
