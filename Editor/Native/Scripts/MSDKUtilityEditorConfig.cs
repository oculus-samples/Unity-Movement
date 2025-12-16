// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Meta.XR.Movement.Retargeting;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using static Meta.XR.Movement.MSDKUtility;

namespace Meta.XR.Movement.Editor
{
    /// <summary>
    /// Configuration class for the Movement SDK utility editor.
    /// </summary>
    [Serializable]
    public class MSDKUtilityEditorConfig : ScriptableObject
    {
        /// <summary>
        /// Represents the different steps in the editor workflow.
        /// </summary>
        public enum EditorStep
        {
            Configuration,
            MinTPose,
            MaxTPose,
            Review,

            End
        }

        /// <summary>
        /// Configuration info that can be cached that relates loaded config json info
        /// to the current skeleton of transforms
        /// </summary>
        public string MetadataAssetPath
        {
            get => _metadataAssetPath;
            set => _metadataAssetPath = value;
        }

        /// <summary>
        /// The current pose loaded and to be updated at this step.
        /// </summary>
        public NativeTransform[] CurrentPose;

        // Config requirements.
        /// <summary>
        /// The handle to the configuration.
        /// </summary>
        public ulong ConfigHandle = INVALID_HANDLE;

        /// <summary>
        /// The name of the configuration.
        /// </summary>
        public string ConfigName;

        /// <summary>
        /// The source skeleton data containing joint hierarchy, T-pose data, and manifestations.
        /// </summary>
        public SkeletonData SourceSkeletonData;

        /// <summary>
        /// The target skeleton data containing joint hierarchy, T-pose data, and manifestations.
        /// </summary>
        public SkeletonData TargetSkeletonData;

        /// <summary>
        /// The joint mappings.
        /// </summary>
        public JointMapping[] JointMappings;

        /// <summary>
        /// The joint mapping entries.
        /// </summary>
        public JointMappingEntry[] JointMappingEntries;

        /// <summary>
        /// The type of T-pose for the skeleton.
        /// </summary>
        public SkeletonTPoseType SkeletonTPoseType;

        /// <summary>
        /// The transforms of skeleton joints.
        /// </summary>
        public Transform[] SkeletonJoints;

        /// <summary>
        /// The transforms of known skeleton joints.
        /// </summary>
        public Transform[] KnownSkeletonJoints;

        /// <summary>
        /// Information about the skeleton.
        /// </summary>
        public SkeletonInfo SkeletonInfo => new() { JointCount = TargetSkeletonData.JointCount };

        // Editor settings.
        /// <summary>
        /// Whether the configuration has been validated and finished.
        /// </summary>
        public bool ValidatedConfigFinish;

        /// <summary>
        /// Whether to draw lines in the editor.
        /// </summary>
        public bool DrawLines = true;

        /// <summary>
        /// Whether the overlay has been initialized.
        /// </summary>
        public bool OverlayInitialized;

        // Preview settings.
        /// <summary>
        /// The list of handles.
        /// </summary>
        public List<ulong> Handles => _handles;

        /// <summary>
        /// The current editor step.
        /// </summary>
        public EditorStep Step;

        /// <summary>
        /// The scale of the root.
        /// </summary>
        public Vector3 RootScale = Vector3.one;

        /// <summary>
        /// Whether the configuration is currently being edited.
        /// </summary>
        public bool CurrentlyEditing;

        /// <summary>
        /// Whether to set the T-pose.
        /// </summary>
        public bool SetTPose;

        /// <summary>
        /// The size of the scale.
        /// </summary>
        public float ScaleSize;

        /// <summary>
        /// Whether the T-pose screen has been entered for the first time.
        /// </summary>
        public bool HasEnteredTPoseScreen;

        /// <summary>
        /// The editor metadata object.
        /// </summary>
        public ref MSDKUtilityEditorMetadata EditorMetadataObject => ref _editorMetadataObject;

        [SerializeField]
        private MSDKUtilityEditorMetadata _editorMetadataObject;

        [SerializeField]
        private string _metadataAssetPath;

        [SerializeField]
        private List<ulong> _handles = new();

        /// <summary>
        /// Checks if the configuration is valid.
        /// </summary>
        /// <returns>True if the configuration is valid, false otherwise.</returns>
        public bool IsValid()
        {
            return true;
        }

        /// <summary>
        /// Initializes the configuration with both source and target skeleton data.
        /// </summary>
        /// <param name="configJson">The configuration JSON string.</param>
        /// <param name="tPoseType">The type of T-pose.</param>
        public void Initialize(string configJson, SkeletonTPoseType tPoseType)
        {
            // Read config data first.
            CreateOrUpdateHandle(configJson, out ConfigHandle);
            GetConfigName(ConfigHandle, out ConfigName);
            SkeletonTPoseType = tPoseType;

            // Create both source and target SkeletonData from the configuration handle
            SourceSkeletonData = SkeletonData.CreateFromHandle(ConfigHandle, SkeletonType.SourceSkeleton);
            TargetSkeletonData = SkeletonData.CreateFromHandle(ConfigHandle, SkeletonType.TargetSkeleton);

            // Get joint mappings (these are still stored separately as they relate to the relationship between source and target)
            GetSkeletonMappings(ConfigHandle, tPoseType, out var jointMappings);
            GetSkeletonMappingEntries(ConfigHandle, tPoseType, out var jointMappingEntries);
            JointMappings = jointMappings.ToArray();
            JointMappingEntries = jointMappingEntries.ToArray();

            // Set the current pose based on the tPoseType parameter
            switch (tPoseType)
            {
                case SkeletonTPoseType.UnscaledTPose:
                    CurrentPose = TargetSkeletonData.TPoseArray;
                    break;
                case SkeletonTPoseType.MinTPose:
                    CurrentPose = TargetSkeletonData.MinTPoseArray;
                    break;
                case SkeletonTPoseType.MaxTPose:
                    CurrentPose = TargetSkeletonData.MaxTPoseArray;
                    break;
            }
        }

        public bool AddHandle(ulong handle)
        {
            if (Handles.Contains(handle))
            {
                return false;
            }

            Handles.Add(handle);
            return true;
        }

        /**********************************************************
         *
         *               Configuration File
         *
         **********************************************************/

        /// <summary>
        /// Saves the configuration.
        /// </summary>
        /// <param name="win">The editor window.</param>
        /// <param name="doDialogue">Whether to show a confirmation dialogue.</param>
        public static void SaveConfig(MSDKUtilityEditorWindow win, bool doDialogue)
        {
            if (win.EditorMetadataObject.ConfigJson != null)
            {
                if (doDialogue)
                {
                    if (!EditorUtility.DisplayDialog("Save Configuration Changes",
                            "Save updates to the configuration?",
                            "Yes", "No"))
                    {
                        return;
                    }
                }

                var configHandle = win.ConfigHandle;
                var configJson = win.EditorMetadataObject.ConfigJson;
                var path = AssetDatabase.GetAssetPath(configJson);
                if (!WriteConfigDataToJson(configHandle, out var data))
                {
                    Debug.LogError("Failed to write config data to JSON");
                    return;
                }

                File.WriteAllText(path, data);
                AssetDatabase.SaveAssetIfDirty(configJson);
                AssetDatabase.Refresh();
                EditorUtility.SetDirty(AssetDatabase.LoadAssetAtPath<TextAsset>(path));
                AssetDatabase.SaveAssets();
            }
            else
            {
                // Create new json since the json config is missing.
                CreateConfig(win, false);
            }
        }

        /// <summary>
        /// Updates the configuration with information from the stored unity objects.
        /// </summary>
        /// <param name="win">The editor window.</param>
        /// <param name="saveConfig">Whether to save the configuration after updating.</param>
        /// <param name="updateMappings"></param>
        /// <param name="performAlignment"></param>
        /// <param name="twistJointOverride">Optional override for twist joint mapping behavior. If null, uses overlay setting.</param>
        public static void UpdateConfig(MSDKUtilityEditorWindow win, bool saveConfig, bool updateMappings,
            bool performAlignment, bool? twistJointOverride = null)
        {
            var config = win.Config;
            JointAlignmentUtility.UpdateTPoseData(config);

            if (config.TargetSkeletonData == null || config.SourceSkeletonData == null)
            {
                Debug.LogError("Missing source/target skeleton data!");
                return;
            }

            GetSkeletonMappings(config.ConfigHandle, SkeletonTPoseType.MinTPose, out var minJointMapping);
            GetSkeletonMappings(config.ConfigHandle, SkeletonTPoseType.MaxTPose, out var maxJointMapping);
            GetSkeletonMappingEntries(config.ConfigHandle, SkeletonTPoseType.MinTPose, out var minJointMappingEntries);
            GetSkeletonMappingEntries(config.ConfigHandle, SkeletonTPoseType.MaxTPose, out var maxJointMappingEntries);

            var (numMappings, _) = (config.JointMappings.Length, config.JointMappingEntries.Length);
            var isMinTPose = config.SkeletonTPoseType == SkeletonTPoseType.MinTPose;

            // Update current T-pose type mappings
            var currentMapping = new NativeArray<JointMapping>(config.JointMappings, Allocator.Temp);
            var currentEntries = new NativeArray<JointMappingEntry>(config.JointMappingEntries, Allocator.Temp);

            // Fill missing mappings for the other T-pose type
            var (otherMapping, otherEntries) = isMinTPose
                ? (maxJointMapping, maxJointMappingEntries)
                : (minJointMapping, minJointMappingEntries);
            if (otherMapping.Length != numMappings)
            {
                otherMapping = new NativeArray<JointMapping>(currentMapping, Allocator.Temp);
                otherEntries = new NativeArray<JointMappingEntry>(currentEntries, Allocator.Temp);
            }

            // Restore to
            if (isMinTPose)
            {
                maxJointMapping = otherMapping;
                maxJointMappingEntries = otherEntries;
            }
            else
            {
                minJointMapping = otherMapping;
                minJointMappingEntries = otherEntries;
            }

            // Update target known joint names
            if (config.TargetSkeletonData != null)
            {
                var knownJoints = (string[])config.TargetSkeletonData.KnownJoints.Clone();
                const string oldRootName = "root";

                for (var i = 0;
                     i < Math.Min((int)KnownJointType.KnownJointCount, config.KnownSkeletonJoints.Length);
                     i++)
                {
                    if (config.KnownSkeletonJoints[i] == null)
                    {
                        if (i < knownJoints.Length) knownJoints[i] = string.Empty;
                        continue;
                    }

                    if (i == 0 && i < knownJoints.Length && knownJoints[0] == oldRootName)
                    {
                        var newRootName = config.KnownSkeletonJoints[0].name;
                        config.TargetSkeletonData.SetJoints(config.TargetSkeletonData.Joints
                            .Select(s => s == oldRootName ? newRootName : s).ToArray());
                        config.TargetSkeletonData.SetParentJoints(config.TargetSkeletonData.ParentJoints
                            .Select(s => s == oldRootName ? newRootName : s).ToArray());
                        knownJoints = knownJoints.Select(s => s == oldRootName ? newRootName : s).ToArray();
                    }
                    else if (i < knownJoints.Length)
                    {
                        knownJoints[i] = config.KnownSkeletonJoints[i]?.name;
                    }
                }

                config.TargetSkeletonData.SetKnownJoints(knownJoints);
            }

            var initParams = new ConfigInitParams
            {
                SourceSkeleton = config.SourceSkeletonData.FillConfigInitParams(),
                TargetSkeleton = config.TargetSkeletonData.FillConfigInitParams()
            };

            var nativePose = new NativeArray<NativeTransform>(config.CurrentPose, Allocator.Temp);
            switch (config.Step)
            {
                case EditorStep.Configuration:
                    initParams.TargetSkeleton.UnscaledTPose = nativePose;
                    break;
                case EditorStep.MinTPose:
                    initParams.TargetSkeleton.MinTPose = nativePose;
                    break;
                case EditorStep.MaxTPose:
                    initParams.TargetSkeleton.MaxTPose = nativePose;
                    break;
            }

            UpdateConfigAlignSave(win, saveConfig, updateMappings, performAlignment, nativePose,
                ref initParams, minJointMapping, maxJointMapping, minJointMappingEntries, maxJointMappingEntries,
                twistJointOverride);
        }

        private static void UpdateConfigAlignSave(MSDKUtilityEditorWindow win, bool saveConfig, bool updateMappings,
            bool performAlignment, NativeArray<NativeTransform> nativePose, ref ConfigInitParams initParams,
            NativeArray<JointMapping> minJointMapping, NativeArray<JointMapping> maxJointMapping,
            NativeArray<JointMappingEntry> minJointMappingEntries, NativeArray<JointMappingEntry> maxJointMappingEntries,
            bool? twistJointOverride = null)
        {
            var config = win.Config;
            switch (performAlignment)
            {
                case true when AlignInputToSource(config.ConfigName, AlignmentFlags.All, nativePose,
                    config.ConfigHandle, SkeletonType.SourceSkeleton, config.ConfigHandle, out var alignedHandle):
                    GetSkeletonTPose(alignedHandle, SkeletonType.TargetSkeleton, SkeletonTPoseType.MinTPose,
                        JointRelativeSpaceType.RootOriginRelativeSpace, out var minTPose);
                    GetSkeletonTPose(alignedHandle, SkeletonType.TargetSkeleton, SkeletonTPoseType.MaxTPose,
                        JointRelativeSpaceType.RootOriginRelativeSpace, out var maxTPose);

                    switch (config.Step)
                    {
                        case EditorStep.Configuration:
                            initParams.TargetSkeleton.MinTPose = minTPose;
                            initParams.TargetSkeleton.MaxTPose = maxTPose;
                            break;
                        case EditorStep.MinTPose:
                            initParams.TargetSkeleton.MinTPose = minTPose;
                            break;
                        case EditorStep.MaxTPose:
                            initParams.TargetSkeleton.MaxTPose = maxTPose;
                            break;
                    }
                    break;
            }

            initParams.MinMappings = new JointMappingDefinition(minJointMapping, minJointMappingEntries);
            initParams.MaxMappings = new JointMappingDefinition(maxJointMapping, maxJointMappingEntries);

            var newConfigHandle = CreateOrUpdateUtilityConfig(config.ConfigName, initParams, out var handle)
                ? handle
                : config.ConfigHandle;
            if (newConfigHandle == config.ConfigHandle)
            {
                Debug.LogError("Was unable to create or update the config with data. Re-using old handle.");
            }
            win.Config.AddHandle(newConfigHandle);

            if (updateMappings)
            {
                var shouldMapTwistJoints = twistJointOverride ?? (win.Overlay?.ShouldMapTwistJoints ?? true);
                var childAlignedTwistBlocklist = win.Overlay?.ChildAlignedTwistBlockList;

                // Convert childAlignedTwistBlocklist to AutoMappingJointData array
                AutoMappingJointData[] additionalJointData = null;
                if (childAlignedTwistBlocklist != null && childAlignedTwistBlocklist.Count > 0)
                {
                    additionalJointData = new AutoMappingJointData[childAlignedTwistBlocklist.Count];
                    for (int i = 0; i < childAlignedTwistBlocklist.Count; i++)
                    {
                        int jointIndex = childAlignedTwistBlocklist[i];
                        additionalJointData[i] = new AutoMappingJointData
                        {
                            JointName = initParams.TargetSkeleton.JointNames[jointIndex],
                            Flags = AutoMappingJointFlags.ExcludeFromTwistMappings
                        };
                    }
                }

                GenerateMappings(newConfigHandle,
                    shouldMapTwistJoints ? AutoMappingFlags.EmptyFlag : AutoMappingFlags.SkipTwistJoints,
                    additionalJointData);
            }

            config.ConfigHandle = newConfigHandle;
            if (saveConfig)
            {
                SaveConfig(win, false);
                LoadConfig(win);
            }
            else
            {
                WriteConfigDataToJson(newConfigHandle, out var data);
                LoadConfig(win, data);
            }
        }

        /// <summary>
        /// Loads the configuration.
        /// </summary>
        /// <param name="win">The editor window.</param>
        /// <param name="customConfig">Optional custom configuration JSON string.</param>
        public static void LoadConfig(MSDKUtilityEditorWindow win, string customConfig = null)
        {
            LoadConfig(win, win.Step switch
            {
                EditorStep.MinTPose => SkeletonTPoseType.MinTPose,
                EditorStep.MaxTPose => SkeletonTPoseType.MaxTPose,
                _ => SkeletonTPoseType.UnscaledTPose
            }, customConfig);
        }

        /// <summary>
        /// Loads the configuration with a specific T-pose type.
        /// </summary>
        /// <param name="win">The editor window.</param>
        /// <param name="targetTPoseType">The target T-pose type.</param>
        /// <param name="customConfig">Optional custom configuration JSON string.</param>
        public static void LoadConfig(
            MSDKUtilityEditorWindow win,
            SkeletonTPoseType targetTPoseType,
            string customConfig = null)
        {
            var config = win.Config;

            // Create new config to be cached; this is destructive.
            var configJson = config.EditorMetadataObject.ConfigJson.text;
            if (!string.IsNullOrEmpty(customConfig))
            {
                configJson = customConfig;
            }

            // Initialize the single config with both source and target data
            config.Initialize(configJson, targetTPoseType);

            // Update the source skeleton data's TPoseArray to point to the correct array based on the current step
            UpdateSourceTPoseForStep(config, win.Step);

            // Store created handle.
            win.Config.AddHandle(config.ConfigHandle);

            // Setup preview.
            if (win.Previewer.Retargeter != null)
            {
                win.Previewer.Retargeter.Setup(configJson);
            }

            // Setup scale only for specific scenarios
            if (!win.Config.CurrentlyEditing && !win.Config.SetTPose)
            {
                // Only auto-scale when entering min/max t-pose screens for the first time
                // or when align skeleton button is pressed (handled in PerformAutoAlignment)
                bool shouldAutoScale = false;

                // Check if this is the first time entering min/max t-pose screens
                if ((win.Step == EditorStep.MinTPose || win.Step == EditorStep.MaxTPose) &&
                    !win.Config.HasEnteredTPoseScreen)
                {
                    shouldAutoScale = true;
                    win.Config.HasEnteredTPoseScreen = true;
                }

                if (shouldAutoScale)
                {
                    JointAlignmentUtility.LoadScale(config);
                }
            }

            win.Previewer.AssociateSceneCharacter(config);
            win.Previewer.ReloadCharacter(config);

            // For preview t-pose, set scale to 1 instead of loading from config
            if (win.Config.Step == EditorStep.Review || win.Config.Step == EditorStep.Configuration)
            {
                // Preview t-pose should have scale set to 1
                win.Config.RootScale = Vector3.one;
            }
            else
            {
                // Apply scaling automatically after character loading is complete in non-preview scenarios
                JointAlignmentUtility.LoadScale(config);
            }
        }

        /// <summary>
        /// Resets the configuration.
        /// </summary>
        /// <param name="win">The editor window.</param>
        /// <param name="customConfig">Optional custom configuration JSON string.</param>
        /// <param name="displayDialogue">Whether to display a confirmation dialogue.</param>
        public static void ResetConfig(
            MSDKUtilityEditorWindow win,
            bool displayDialogue,
            string customConfig = null)
        {
            if (displayDialogue && !EditorUtility.DisplayDialog("Reset Configuration",
                    "Reset the configuration file?\n\nWarning: Resetting the configuration will discard all changes made.",
                    "Yes", "No"))
            {
                return;
            }

            // Use the existing LoadConfig method with the reset configuration
            // Use the appropriate T-pose type based on the current step
            var tPoseType = win.Step switch
            {
                EditorStep.MinTPose => SkeletonTPoseType.MinTPose,
                EditorStep.MaxTPose => SkeletonTPoseType.MaxTPose,
                _ => SkeletonTPoseType.UnscaledTPose
            };
            LoadConfig(win, tPoseType, customConfig);
        }

        /// <summary>
        /// Updates the source skeleton data's TPoseArray to point to the correct array based on the editor step.
        /// This is an editor-only helper that keeps the step-dependent logic out of runtime code.
        /// </summary>
        /// <param name="config">The editor configuration.</param>
        /// <param name="step">The current editor step.</param>
        private static void UpdateSourceTPoseForStep(MSDKUtilityEditorConfig config, EditorStep step)
        {
            if (config?.SourceSkeletonData == null)
            {
                return;
            }

            var tPoseArray = step switch
            {
                EditorStep.MinTPose => config.SourceSkeletonData.MinTPoseArray,
                EditorStep.MaxTPose => config.SourceSkeletonData.MaxTPoseArray,
                _ => config.SourceSkeletonData.TPoseArray // Keep current for other steps
            };

            config.SourceSkeletonData.SetTPoseArray(tPoseArray);
        }

        /// <summary>
        /// Creates a new configuration.
        /// </summary>
        /// <param name="win">The editor window.</param>
        /// <param name="displayDialogue">Whether to display a confirmation dialogue.</param>
        /// <param name="customSourceData">Optional custom skeleton data.</param>
        /// <param name="customTargetData">Optional custom skeleton data.</param>
        public static void CreateConfig(
            MSDKUtilityEditorWindow win,
            bool displayDialogue,
            SkeletonData customSourceData = null,
            SkeletonData customTargetData = null)
        {
            if (displayDialogue && !EditorUtility.DisplayDialog("Create Configuration",
                    "Create configuration file?\n\nWarning: The configuration file will be based on the current state.",
                    "Yes", "No"))
            {
                return;
            }

            var configPath = string.Empty;
            if (win.EditorMetadataObject == null)
            {
                var originalAsset =
                    AssetDatabase.LoadAssetAtPath(win.Config.MetadataAssetPath,
                        typeof(GameObject)) as GameObject;
                win.EditorMetadataObject = MSDKUtilityEditorMetadata.FindMetadataAsset(originalAsset);
                configPath = Path.ChangeExtension(AssetDatabase.GetAssetPath(win.EditorMetadataObject), ".json");
            }
            else if (win.EditorMetadataObject.ConfigJson != null)
            {
                configPath = AssetDatabase.GetAssetPath(win.EditorMetadataObject.ConfigJson);
            }

            // Use the centralized config manager to create the configuration
            // Pass the custom data directly to the CreateConfig method
            // If we have an existing config, pass it so source data can be extracted from it
            win.EditorMetadataObject.ConfigJson = MSDKUtilityEditor.CreateRetargetingConfig(
                win.EditorMetadataObject.Model,
                customSourceData,
                customTargetData,
                configPath,
                false);

            EditorUtility.SetDirty(win.EditorMetadataObject);
            AssetDatabase.SaveAssets();
            ResetConfig(win, false, win.EditorMetadataObject.ConfigJson.text);
        }
    }
}
