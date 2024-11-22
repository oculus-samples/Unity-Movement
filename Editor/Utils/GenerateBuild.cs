// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Oculus.Movement.Utils
{
    /// <summary>
    /// This class contains useful menus used for generating sample builds.
    /// </summary>
    public class GenerateBuild
    {
        private static readonly string[] _sceneNames =
            new string[] {
                "t:scene, MovementLina",
                "t:scene, MovementBlendshapeMappingExampleA2E",
                "t:scene, MovementHighFidelity",
                "t:scene, MovementRetargeting",
                "t:scene, MovementBodyTrackingForFitness",
                "t:scene, MovementHipPinning",
                "t:scene, MovementISDKIntegration",
                "t:scene, MovementLocomotion",
                "t:scene, MovementNetworking",
            };

        private const string _MAIN_BUILD_NAME = "movement";

        /// <summary>
        /// Builds an APK with as many samples as possible, depending on whether or not
        /// those samples have been imported into the Assets folder or not.
        /// </summary>
        [MenuItem("Movement/Build Samples APK", priority = 100)]
        public static void CreateSamplesBuildAPK()
        {
            List<string> validScenePaths = new List<string>();
            foreach (string sceneName in _sceneNames)
            {
                var scenePath = PathOfAssetInAssetsFolder(sceneName);
                if (scenePath != String.Empty)
                {
                    validScenePaths.Add(scenePath);
                }
            }

            if (validScenePaths.Count == 0)
            {
                Debug.LogError($"No samples scenes have been imported; cannot build.");
                return;
            }

            GenerateAndroidBuild(validScenePaths.ToArray(), _MAIN_BUILD_NAME,
                "MovementSDK Samples", false);
        }

        private static string PathOfAssetInAssetsFolder(string assetName)
        {
            string[] guids =
                AssetDatabase.FindAssets(assetName, new string[] { "Assets" });
            if (guids.Length == 0)
            {
                return String.Empty;
            }
            return AssetDatabase.GUIDToAssetPath(guids[0]);
        }

        private static void GenerateAndroidBuild(string[] buildScenes, string buildName,
            string productName, bool exitAfterBuild = true)
        {
            string previousAppIdentifier = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
            string previousProductName = PlayerSettings.productName;
            string targetAppId = "com.meta." + buildName;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, targetAppId);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            bool prevForceSDCardPerm = PlayerSettings.Android.forceSDCardPermission;
            PlayerSettings.Android.forceSDCardPermission = false;
            PlayerSettings.productName = productName;

            BuildPlayerOptions buildOptions = new BuildPlayerOptions()
            {
                locationPathName = string.Format("builds/{0}.apk", buildName),
                scenes = buildScenes,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
            };
            buildOptions.options = new BuildOptions();

            try
            {
                var error = BuildPipeline.BuildPlayer(buildOptions);
                RestorePreviousSettings(previousAppIdentifier, previousProductName, prevForceSDCardPerm);
                HandleBuildErrors.Check(error, exitAfterBuild, buildScenes);
            }
            catch
            {
                Debug.Log("Exception while building: exiting with exit code 2");
                RestorePreviousSettings(previousAppIdentifier, previousProductName, prevForceSDCardPerm);
                EditorApplication.Exit(2);
            }
        }

        private static void RestorePreviousSettings(string previousAppIdentifier, string previousProductName,
            bool prevForceSDCardPerm)
        {
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, previousAppIdentifier);
            PlayerSettings.productName = previousProductName;
            PlayerSettings.Android.forceSDCardPermission = prevForceSDCardPerm;
        }
    }

    /// <summary>
    /// Handles errors after build process. This can be used to catch the edge case where
    /// builds don't actually succeed even if they are marked as doing so.
    /// </summary>
    public static class HandleBuildErrors
    {
        /// <summary>
        /// Check for build error edge cases.
        /// </summary>
        /// <param name="buildReport">Build report.</param>
        /// <param name="exitAfterBuild">If we need to exit after the build or not.</param>
        /// <param name="scenesBuilt">Scenes built.</param>
        public static void Check(UnityEditor.Build.Reporting.BuildReport buildReport, bool exitAfterBuild,
            string[] scenesBuilt)
        {
            bool buildSucceeded =
                buildReport.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;
            if (buildReport.summary.platform == BuildTarget.Android)
            {
                // Android can fail to produce the output even if the build is marked as succeeded in some rare
                // scenarios, notably if the Unity directory is read-only. This should be handled.
                buildSucceeded = buildSucceeded && File.Exists(buildReport.summary.outputPath);
            }
            if (buildSucceeded)
            {
                foreach (var scene in scenesBuilt)
                {
                    Debug.Log($"Built scene {scene}.");
                }
                Debug.Log("Exiting with code 0. Success.");

                if (exitAfterBuild)
                {
                    EditorApplication.Exit(0);
                }
            }
            else
            {
                Debug.Log("Exiting with code 1. Failure.");
                if (exitAfterBuild)
                {
                    EditorApplication.Exit(1);
                }
            }
        }
    }
}
