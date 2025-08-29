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
        /// The names of blendshapes in the configuration.
        /// </summary>
        public string[] BlendshapeNames;

        /// <summary>
        /// The names of joints in the configuration.
        /// </summary>
        public string[] JointNames;

        /// <summary>
        /// The names of parent joints in the configuration.
        /// </summary>
        public string[] ParentJointNames;

        /// <summary>
        /// The names of known joints in the configuration.
        /// </summary>
        public string[] KnownJointNames;

        /// <summary>
        /// The reference pose transforms.
        /// </summary>
        public NativeTransform[] ReferencePose;

        /// <summary>
        /// The joint mappings.
        /// </summary>
        public JointMapping[] JointMappings;

        /// <summary>
        /// The joint mapping entries.
        /// </summary>
        public JointMappingEntry[] JointMappingEntries;

        // Unity specific.
        /// <summary>
        /// Information about the skeleton.
        /// </summary>
        public SkeletonInfo SkeletonInfo;

        /// <summary>
        /// The type of T-pose for the skeleton.
        /// </summary>
        public SkeletonTPoseType SkeletonTPoseType;

        /// <summary>
        /// The indices of parent joints.
        /// </summary>
        public int[] ParentIndices;

        /// <summary>
        /// The transforms of skeleton joints.
        /// </summary>
        public Transform[] SkeletonJoints;

        /// <summary>
        /// The transforms of known skeleton joints.
        /// </summary>
        public Transform[] KnownSkeletonJoints;

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
        public List<ulong> Handles = new();

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
        /// The editor metadata object.
        /// </summary>
        public ref MSDKUtilityEditorMetadata EditorMetadataObject => ref _editorMetadataObject;

        [SerializeField]
        private MSDKUtilityEditorMetadata _editorMetadataObject;

        [SerializeField]
        private string _metadataAssetPath;

        /// <summary>
        /// Checks if the configuration is valid.
        /// </summary>
        /// <returns>True if the configuration is valid, false otherwise.</returns>
        public bool IsValid()
        {
            return true;
        }

        /// <summary>
        /// Initializes the configuration with the specified parameters.
        /// </summary>
        /// <param name="configJson">The configuration JSON string.</param>
        /// <param name="skeletonType">The type of skeleton.</param>
        /// <param name="tPoseType">The type of T-pose.</param>
        public void Initialize(string configJson, SkeletonType skeletonType, SkeletonTPoseType tPoseType)
        {
            // Read config data first.
            if (!CreateOrUpdateHandle(configJson, out ConfigHandle))
            {
                TelemetryManager.SendErrorEvent(TelemetryManager._HANDLE_EVENT_NAME, "Failed to create or update handle.");
            }
            GetConfigName(ConfigHandle, out ConfigName);
            GetBlendShapeNames(ConfigHandle, skeletonType, out BlendshapeNames);
            GetSkeletonInfo(ConfigHandle, skeletonType, out SkeletonInfo);
            GetJointNames(ConfigHandle, skeletonType, out JointNames);
            GetParentJointNames(ConfigHandle, skeletonType, out ParentJointNames);
            GetKnownJointNames(ConfigHandle, skeletonType, out KnownJointNames);
            SkeletonTPoseType = tPoseType;

            // Fill out runtime data.
            var count = SkeletonInfo.JointCount;
            var tPose = new NativeArray<NativeTransform>(count, Allocator.Temp,
                NativeArrayOptions.UninitializedMemory);
            var parentIndices = new NativeArray<int>(count, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            GetSkeletonTPoseByRef(ConfigHandle, skeletonType, tPoseType,
                JointRelativeSpaceType.RootOriginRelativeSpace, ref tPose);
            GetParentJointIndexesByRef(ConfigHandle, skeletonType, ref parentIndices);
            GetSkeletonMappings(ConfigHandle, tPoseType, out var jointMappings);
            GetSkeletonMappingEntries(ConfigHandle, tPoseType, out var jointMappingEntries);
            JointMappings = jointMappings.ToArray();
            JointMappingEntries = jointMappingEntries.ToArray();
            ReferencePose = tPose.ToArray();
            ParentIndices = parentIndices.ToArray();
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

                var textAsset = win.EditorMetadataObject.ConfigJson;
                var path = AssetDatabase.GetAssetPath(textAsset);
                WriteConfigDataToJson(win.TargetInfo.ConfigHandle, out var data);
                File.WriteAllText(path, data);
                AssetDatabase.SaveAssetIfDirty(win.EditorMetadataObject.ConfigJson);
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
        public static void UpdateConfig(
            MSDKUtilityEditorWindow win,
            bool saveConfig,
            bool updateMappings,
            bool performAlignment)
        {
            var source = win.SourceInfo;
            var target = win.TargetInfo;

            // First, update our data.
            JointAlignmentUtility.UpdateTPoseData(target);

            // Let's update out the current config by creating a new handle with the current Unity information.
            var sourceMinTPose =
                new NativeArray<NativeTransform>(source.SkeletonInfo.JointCount, Allocator.Temp,
                    NativeArrayOptions.UninitializedMemory);
            var sourceMaxTPose =
                new NativeArray<NativeTransform>(source.SkeletonInfo.JointCount, Allocator.Temp,
                    NativeArrayOptions.UninitializedMemory);
            var targetUnscaledTPose =
                new NativeArray<NativeTransform>(target.SkeletonInfo.JointCount, Allocator.Temp,
                    NativeArrayOptions.UninitializedMemory);
            var targetMinTPose =
                new NativeArray<NativeTransform>(target.SkeletonInfo.JointCount, Allocator.Temp,
                    NativeArrayOptions.UninitializedMemory);
            var targetMaxTPose =
                new NativeArray<NativeTransform>(target.SkeletonInfo.JointCount, Allocator.Temp,
                    NativeArrayOptions.UninitializedMemory);

            // Source T-Poses.
            GetSkeletonTPoseByRef(source.ConfigHandle, SkeletonType.SourceSkeleton, SkeletonTPoseType.MinTPose,
                JointRelativeSpaceType.RootOriginRelativeSpace, ref sourceMinTPose);
            GetSkeletonTPoseByRef(source.ConfigHandle, SkeletonType.SourceSkeleton, SkeletonTPoseType.MaxTPose,
                JointRelativeSpaceType.RootOriginRelativeSpace, ref sourceMaxTPose);

            // Target T-Poses.
            GetSkeletonTPoseByRef(target.ConfigHandle, SkeletonType.TargetSkeleton,
                SkeletonTPoseType.UnscaledTPose,
                JointRelativeSpaceType.RootOriginRelativeSpace, ref targetUnscaledTPose);
            GetSkeletonTPoseByRef(target.ConfigHandle, SkeletonType.TargetSkeleton, SkeletonTPoseType.MinTPose,
                JointRelativeSpaceType.RootOriginRelativeSpace, ref targetMinTPose);
            GetSkeletonTPoseByRef(target.ConfigHandle, SkeletonType.TargetSkeleton, SkeletonTPoseType.MaxTPose,
                JointRelativeSpaceType.RootOriginRelativeSpace, ref targetMaxTPose);

            // Mappings.
            GetSkeletonMappings(target.ConfigHandle, SkeletonTPoseType.MinTPose,
                out var minJointMapping);
            GetSkeletonMappings(target.ConfigHandle, SkeletonTPoseType.MaxTPose,
                out var maxJointMapping);
            GetSkeletonMappingEntries(target.ConfigHandle,
                SkeletonTPoseType.MinTPose,
                out var minJointMappingEntries);
            GetSkeletonMappingEntries(target.ConfigHandle,
                SkeletonTPoseType.MaxTPose,
                out var maxJointMappingEntries);

            // Update selected target information.
            if (target.SkeletonTPoseType == SkeletonTPoseType.UnscaledTPose)
            {
                targetUnscaledTPose.CopyFrom(target.ReferencePose);
            }

            // Update mappings by using current mappings
            var numOfMappings = target.JointMappings.Length;
            var numOfMappingEntries = target.JointMappingEntries.Length;
            switch (target.SkeletonTPoseType)
            {
                case SkeletonTPoseType.MinTPose:
                    minJointMapping = new NativeArray<JointMapping>(numOfMappings,
                        Allocator.Temp,
                        NativeArrayOptions.UninitializedMemory);
                    minJointMappingEntries = new NativeArray<JointMappingEntry>(numOfMappingEntries,
                        Allocator.Temp,
                        NativeArrayOptions.UninitializedMemory);

                    targetMinTPose.CopyFrom(target.ReferencePose);
                    minJointMapping.CopyFrom(target.JointMappings);
                    minJointMappingEntries.CopyFrom(target.JointMappingEntries);

                    // Fill missing mappings.
                    if (maxJointMapping.Length != numOfMappings)
                    {
                        maxJointMapping = new NativeArray<JointMapping>(numOfMappings,
                            Allocator.Temp,
                            NativeArrayOptions.UninitializedMemory);
                        maxJointMappingEntries = new NativeArray<JointMappingEntry>(numOfMappingEntries,
                            Allocator.Temp,
                            NativeArrayOptions.UninitializedMemory);
                        maxJointMapping.CopyFrom(minJointMapping);
                        maxJointMappingEntries.CopyFrom(minJointMappingEntries);
                        targetMaxTPose.CopyFrom(target.ReferencePose);
                    }

                    break;

                case SkeletonTPoseType.MaxTPose:
                    maxJointMapping = new NativeArray<JointMapping>(numOfMappings,
                        Allocator.Temp,
                        NativeArrayOptions.UninitializedMemory);
                    maxJointMappingEntries = new NativeArray<JointMappingEntry>(numOfMappingEntries,
                        Allocator.Temp,
                        NativeArrayOptions.UninitializedMemory);

                    targetMaxTPose.CopyFrom(target.ReferencePose);
                    maxJointMapping.CopyFrom(target.JointMappings);
                    maxJointMappingEntries.CopyFrom(target.JointMappingEntries);

                    // Fill missing mappings.
                    if (minJointMapping.Length != numOfMappings)
                    {
                        minJointMapping = new NativeArray<JointMapping>(numOfMappings,
                            Allocator.Temp,
                            NativeArrayOptions.UninitializedMemory);
                        minJointMappingEntries = new NativeArray<JointMappingEntry>(numOfMappingEntries,
                            Allocator.Temp,
                            NativeArrayOptions.UninitializedMemory);
                        minJointMapping.CopyFrom(maxJointMapping);
                        minJointMappingEntries.CopyFrom(maxJointMappingEntries);
                        targetMinTPose.CopyFrom(target.ReferencePose);
                    }

                    break;
            }

            // Update target known joint names with transform references.
            for (var i = 0; i < (int)KnownJointType.KnownJointCount; i++)
            {
                if (target.KnownSkeletonJoints[i] == null)
                {
                    target.KnownJointNames[i] = string.Empty;
                    continue;
                }

                var oldRootName = "root";
                if (i == 0 && target.KnownJointNames[0] == oldRootName)
                {
                    // Upgrade to use non-root name.
                    var newRootName = target.KnownSkeletonJoints[0].name;
                    target.JointNames = target.JointNames.Select(s => s == oldRootName ? newRootName : s).ToArray();
                    target.ParentJointNames = target.ParentJointNames.Select(s => s == oldRootName ? newRootName : s).ToArray();
                    target.KnownJointNames = target.KnownJointNames.Select(s => s == oldRootName ? newRootName : s).ToArray();
                }
                else
                {
                    target.KnownJointNames[i] = target.KnownSkeletonJoints[i]?.name;
                }
            }

            var initParams = new ConfigInitParams();
            initParams.SourceSkeleton.BlendShapeNames = source.BlendshapeNames;
            initParams.SourceSkeleton.JointNames = source.JointNames;
            initParams.SourceSkeleton.ParentJointNames = source.ParentJointNames;
            initParams.SourceSkeleton.OptionalKnownSourceJointNamesById = source.KnownJointNames;
            initParams.SourceSkeleton.OptionalAutoMapExcludedJointNames = MSDKUtilityHelper.AutoMapExcludedJointNames;
            initParams.SourceSkeleton.OptionalManifestationNames =
                new[] { MetaSourceDataProvider.HalfBodyManifestation };
            initParams.SourceSkeleton.OptionalManifestationJointCounts =
                new[] { (int)SkeletonData.BodyTrackingBoneId.End };
            initParams.SourceSkeleton.OptionalManifestationJointNames =
                MSDKUtilityHelper.HalfBodyManifestationJointNames;
            initParams.SourceSkeleton.MinTPose = sourceMinTPose;
            initParams.SourceSkeleton.MaxTPose = sourceMaxTPose;

            initParams.TargetSkeleton.BlendShapeNames = target.BlendshapeNames;
            initParams.TargetSkeleton.JointNames = target.JointNames;
            initParams.TargetSkeleton.ParentJointNames = target.ParentJointNames;
            initParams.TargetSkeleton.OptionalKnownSourceJointNamesById = target.KnownJointNames;
            initParams.TargetSkeleton.MinTPose = targetMinTPose;
            initParams.TargetSkeleton.MaxTPose = targetMaxTPose;
            initParams.TargetSkeleton.UnscaledTPose = targetUnscaledTPose;

            initParams.MinMappings.Mappings = minJointMapping;
            initParams.MinMappings.MappingEntries = minJointMappingEntries;
            initParams.MaxMappings.Mappings = maxJointMapping;
            initParams.MaxMappings.MappingEntries = maxJointMappingEntries;

            bool configCreated = false;
            if (!(configCreated = CreateOrUpdateUtilityConfig(source.ConfigName, initParams, out var newConfigHandle)))
            {
                var errorMessage = "Was unable to create or update the config with data. Re-using old handle.";
                Debug.LogError(errorMessage);
                newConfigHandle = target.ConfigHandle;
                TelemetryManager.SendErrorEvent(TelemetryManager._CONFIG_EVENT_NAME, errorMessage);
            }
            win.UtilityConfig.Handles.Add(newConfigHandle);

            if (performAlignment)
            {
                bool alignWorked = AlignTargetToSource(source.ConfigName,
                    AlignmentFlags.All,
                    newConfigHandle,
                    SkeletonType.SourceSkeleton,
                    newConfigHandle,
                    out newConfigHandle);
                if (!alignWorked)
                {
                    TelemetryManager.SendErrorEvent(TelemetryManager._ALIGN_TARGET_TO_SOURCE_EVENT_NAME,
                        "Failed to align target to source.");
                }
            }

            if (updateMappings)
            {
                GenerateMappings(newConfigHandle, AutoMappingFlags.EmptyFlag);
            }

            target.ConfigHandle = source.ConfigHandle = newConfigHandle;
            if (saveConfig)
            {
                SaveConfig(win, false);
                LoadConfig(win);
            }
            else
            {
                WriteConfigDataToJson(newConfigHandle, out var jsonConfigData);
                LoadConfig(win, jsonConfigData);
            }
        }

        /// <summary>
        /// Loads the configuration.
        /// </summary>
        /// <param name="win">The editor window.</param>
        /// <param name="customConfig">Optional custom configuration JSON string.</param>
        public static void LoadConfig(MSDKUtilityEditorWindow win, string customConfig = null)
        {
            LoadConfig(win, win.UtilityConfig.Step switch
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
            var source = win.SourceInfo;
            var target = win.TargetInfo;

            // Create new config to be cached; this is destructive.
            var config = win.UtilityConfig.EditorMetadataObject.ConfigJson.text;
            if (!string.IsNullOrEmpty(customConfig))
            {
                config = customConfig;
            }

            var tPoseType = win.UtilityConfig.Step switch
            {
                EditorStep.MinTPose => SkeletonTPoseType.MinTPose,
                EditorStep.MaxTPose => SkeletonTPoseType.MaxTPose,
                _ => SkeletonTPoseType.UnscaledTPose
            };
            source.Initialize(config, SkeletonType.SourceSkeleton, tPoseType);
            target.Initialize(config, SkeletonType.TargetSkeleton, targetTPoseType);

            // Store created handles.
            win.UtilityConfig.Handles.Add(source.ConfigHandle);
            win.UtilityConfig.Handles.Add(target.ConfigHandle);

            // Setup preview.
            if (win.Previewer.Retargeter != null)
            {
                win.Previewer.Retargeter.Setup(config);
            }

            // Setup scale.
            if (!win.UtilityConfig.CurrentlyEditing && !win.UtilityConfig.SetTPose)
            {
                JointAlignmentUtility.LoadScale(target, win.UtilityConfig);
            }

            win.Previewer.AssociateSceneCharacter(target);
            win.Previewer.ReloadCharacter(target);
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

            win.SourceInfo = CreateInstance<MSDKUtilityEditorConfig>();
            win.TargetInfo = CreateInstance<MSDKUtilityEditorConfig>();
            LoadConfig(win, SkeletonTPoseType.UnscaledTPose, customConfig);
        }

        /// <summary>
        /// Creates a new configuration.
        /// </summary>
        /// <param name="win">The editor window.</param>
        /// <param name="displayDialogue">Whether to display a confirmation dialogue.</param>
        /// <param name="customData">Optional custom skeleton data.</param>
        public static void CreateConfig(
            MSDKUtilityEditorWindow win,
            bool displayDialogue,
            SkeletonData customData = null)
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
                    AssetDatabase.LoadAssetAtPath(win.UtilityConfig.MetadataAssetPath,
                        typeof(GameObject)) as GameObject;
                win.EditorMetadataObject = MSDKUtilityEditorMetadata.FindMetadataObject(originalAsset);
                configPath = Path.ChangeExtension(AssetDatabase.GetAssetPath(win.EditorMetadataObject), ".json");
            }
            else if (win.EditorMetadataObject.ConfigJson != null)
            {
                configPath = AssetDatabase.GetAssetPath(win.EditorMetadataObject.ConfigJson);
            }

            win.EditorMetadataObject.ConfigJson =
                MSDKUtilityEditor.CreateRetargetingConfig(
                    MSDKUtilityEditor.GetSkeletonRetargetingData("OVRSkeletonRetargetingData"),
                    win.EditorMetadataObject.Model,
                    customData, configPath);
            EditorUtility.SetDirty(win.EditorMetadataObject);
            AssetDatabase.SaveAssets();
            ResetConfig(win, false, win.EditorMetadataObject.ConfigJson.text);
        }

        /// <summary>
        /// Validates the editor information.
        /// </summary>
        /// <param name="previewer">The editor previewer.</param>
        /// <param name="skeletonDrawTPose">The skeleton draw for T-pose.</param>
        /// <param name="config">The utility configuration.</param>
        public static void Validate(
            MSDKUtilityEditorPreviewer previewer,
            SkeletonDraw skeletonDrawTPose,
            MSDKUtilityEditorConfig config)
        {
            if (skeletonDrawTPose == null || config.JointNames.Length <= 0)
            {
                return;
            }

            previewer.LoadDraw(skeletonDrawTPose, config);
        }
    }
}
