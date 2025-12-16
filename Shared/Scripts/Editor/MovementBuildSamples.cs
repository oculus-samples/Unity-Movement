// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Meta.XR.Movement.Editor
{
    /// <summary>
    /// Provides scripts to build the Movement SDK samples.
    /// This script is designed for users who import the package from UPM.
    /// </summary>
    public static class MovementBuildSamples
    {
        // Bundle ID and sample configuration
        private const string _bundlePrefix = "com.meta.";
        private const string _bodyTrackingScene = "Scenes/MovementBody.unity";
        private const string _faceTrackingScene = "Scenes/MovementFace.unity";
        private const string _isdkStartingScene = "Scenes/MovementISDKIntegration.unity";

        // Sample folders
        private const string _bodyTrackingFolder = "Body Tracking Samples";
        private const string _faceTrackingFolder = "Face Tracking Samples";
        private const string _advancedSamplesFolder = "Advanced Samples";

        // Advanced sample scenes
        private static readonly string[] _advancedSampleScenes = {
            _isdkStartingScene,
            "Scenes/MovementNetworking.unity",
            "Scenes/MovementISDKLocomotion.unity",
            "Scenes/MovementHipPinning.unity",
            "Scenes/MovementBodyTrackingForFitness.unity"
        };

        /// <summary>
        /// Builds all available Movement SDK samples.
        /// </summary>
        [MenuItem("Meta/Samples/Build Movement SDK Samples")]
        public static void BuildMovementSamples()
        {
            var scenePaths = new List<string>();
            var includeBodySamples = false;
            var includeFaceSamples = false;
            var includeAdvancedSamples = false;
            string startingScene = null;

            var bodyScenePath = FindScenePath(_bodyTrackingFolder, _bodyTrackingScene);
            if (!string.IsNullOrEmpty(bodyScenePath))
            {
                includeBodySamples = true;
                scenePaths.Add(bodyScenePath);
                startingScene = bodyScenePath;
            }

            var faceScenePath = FindScenePath(_faceTrackingFolder, _faceTrackingScene);
            if (!string.IsNullOrEmpty(faceScenePath))
            {
                includeFaceSamples = true;
                scenePaths.Add(faceScenePath);
                startingScene ??= faceScenePath;
            }

            foreach (var sampleScene in _advancedSampleScenes)
            {
                var scenePath = FindScenePath(_advancedSamplesFolder, sampleScene);
                if (!string.IsNullOrEmpty(scenePath))
                {
                    includeAdvancedSamples = true;
                    if (sampleScene == _isdkStartingScene)
                    {
                        startingScene ??= scenePath;
                    }

                    if (!scenePaths.Contains(scenePath))
                    {
                        scenePaths.Add(scenePath);
                    }
                }
            }

            if (scenePaths.Count == 0)
            {
                Debug.LogError("Could not find any sample scenes. Make sure you've imported the Movement SDK samples through the package manager.");
                return;
            }

            // Ensure the starting scene is first in the list
            if (startingScene != null && scenePaths.Count > 0 && scenePaths[0] != startingScene)
            {
                scenePaths.Remove(startingScene);
                scenePaths.Insert(0, startingScene);
            }

            SetAllSamplesSettings(includeBodySamples, includeFaceSamples, includeAdvancedSamples);
            InitializeBuild("movementsamples", "Meta XR Movement SDK Samples");
            Build("movement_samples.apk", scenePaths.ToArray());
        }

        /// <summary>
        /// Finds the path to a scene in the package samples.
        /// </summary>
        private static string FindScenePath(string sampleFolder, string scenePath)
        {
            // Check package cache path
            var packagePath = Path.GetFullPath("Packages/com.meta.xr.sdk.movement");
            var packageFolderPath = sampleFolder.Replace(" ", "");
            var fullPath = Path.Combine(packagePath, "Samples", packageFolderPath, scenePath);
            if (Directory.Exists(packagePath) && File.Exists(fullPath))
            {
                return $"Packages/com.meta.xr.sdk.movement/Samples/{packageFolderPath}/{scenePath}";
            }

            // Check Assets/Samples directory
            var samplesPath = Path.Combine(Application.dataPath, "Samples", "Meta XR Movement SDK");
            if (Directory.Exists(samplesPath))
            {
                var versionDirs = Directory.GetDirectories(samplesPath);
                if (versionDirs.Length > 0)
                {
                    Array.Sort(versionDirs);
                    var latestVersionPath = versionDirs[^1];
                    fullPath = Path.Combine(latestVersionPath, sampleFolder, scenePath);

                    if (File.Exists(fullPath))
                    {
                        return $"Assets/Samples/Meta XR Movement SDK/{Path.GetFileName(latestVersionPath)}/{sampleFolder}/{scenePath}";
                    }
                }
            }

            // Check imported samples in Assets folder
            var foundPaths = Directory
                .GetFiles(Application.dataPath, Path.GetFileName(scenePath), SearchOption.AllDirectories)
                .Where(p => p.Contains(sampleFolder) && p.Contains("Samples") && p.EndsWith(scenePath))
                .Select(p =>
                {
                    // Convert to Assets/... format
                    var assetPath = "Assets" + p.Substring(Application.dataPath.Length);
                    return assetPath;
                })
                .FirstOrDefault();

            return foundPaths;
        }

        /// <summary>
        /// Initializes build settings for a specific build.
        /// </summary>
        public static void InitializeBuild(string identifierSuffix, string productName = null)
        {
            // Configure rendering settings
            PlayerSettings.stereoRenderingPath = StereoRenderingPath.Instancing;
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
            PlayerSettings.colorSpace = ColorSpace.Linear;
            QualitySettings.antiAliasing = 4;

            // Configure Android build settings
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetArchitecture(NamedBuildTarget.Android, 1); // ARM64
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;

            // Set application identifiers
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, $"{_bundlePrefix}{identifierSuffix}");
            if (!string.IsNullOrEmpty(productName))
            {
                PlayerSettings.productName = productName;
            }
        }

        /// <summary>
        /// Builds the specified scenes.
        /// </summary>
        private static void Build(string apkName, string[] scenes)
        {
            var buildPlayerOptions = new BuildPlayerOptions
            {
                target = BuildTarget.Android,
                locationPathName = "Builds/" + apkName,
                scenes = scenes
            };
            Debug.Log($"Building scenes: \n{string.Join("\n  ", scenes)}");
            var buildReport = BuildPipeline.BuildPlayer(buildPlayerOptions);
            if (!Application.isBatchMode && buildReport.summary.result == BuildResult.Succeeded)
            {
                EditorUtility.RevealInFinder(apkName);
            }
        }

        /// <summary>
        /// Sets the required settings for all samples in the OVRProjectConfig.
        /// </summary>
        private static void SetAllSamplesSettings(bool bodySamples, bool faceSamples, bool advancedSamples)
        {
            try
            {
                var projectConfig = OVRProjectConfig.CachedProjectConfig;
                QualitySettings.skinWeights = SkinWeights.FourBones;

                if (bodySamples)
                {
                    OVRRuntimeSettings.Instance.BodyTrackingFidelity = OVRPlugin.BodyTrackingFidelity2.High;
                    OVRRuntimeSettings.Instance.BodyTrackingJointSet = OVRPlugin.BodyJointSet.FullBody;
                    projectConfig.bodyTrackingSupport = OVRProjectConfig.FeatureSupport.Required;
                    projectConfig.handTrackingSupport = OVRProjectConfig.HandTrackingSupport.ControllersAndHands;
                }

                if (faceSamples)
                {
                    OVRRuntimeSettings.Instance.RequestsVisualFaceTracking = true;
                    OVRRuntimeSettings.Instance.RequestsAudioFaceTracking = true;
                    projectConfig.faceTrackingSupport = OVRProjectConfig.FeatureSupport.Required;
                }

                OVRProjectConfig.CommitProjectConfig(projectConfig);
                OVRRuntimeSettings.CommitRuntimeSettings(OVRRuntimeSettings.Instance);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error setting all samples settings: {e.Message}");
            }
        }
    }
}
