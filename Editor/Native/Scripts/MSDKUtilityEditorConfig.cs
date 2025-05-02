// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using System.IO;
using Meta.XR.Movement.Retargeting;
using Meta.XR.Movement.Utils;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using static Meta.XR.Movement.MSDKUtility;
using static Meta.XR.Movement.MSDKUtilityHelper;

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
            if (ConfigHandle != INVALID_HANDLE)
            {
                DestroyHandle(ConfigHandle);
            }

            // Read config data first.
            CreateOrUpdateHandle(configJson, out ConfigHandle);
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
            GetSkeletonMappings(ConfigHandle, skeletonType, tPoseType, out var jointMappings);
            GetSkeletonMappingEntries(ConfigHandle, skeletonType, tPoseType, out var jointMappingEntries);
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
        /// <param name="previewRetargeter">The character retargeter for preview.</param>
        /// <param name="previewer">The editor previewer.</param>
        /// <param name="utilityConfig">The utility configuration.</param>
        /// <param name="editorMetadataObject">The editor metadata object.</param>
        /// <param name="doDialogue">Whether to show a confirmation dialogue.</param>
        public static void SaveConfig(MSDKUtilityEditorWindow win, CharacterRetargeter previewRetargeter,
            MSDKUtilityEditorPreviewer previewer, MSDKUtilityEditorConfig utilityConfig,
            MSDKUtilityEditorMetadata editorMetadataObject, bool doDialogue = true)
        {
            if (utilityConfig.EditorMetadataObject.ConfigJson != null)
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

                var textAsset = utilityConfig.EditorMetadataObject.ConfigJson;
                var path = AssetDatabase.GetAssetPath(textAsset);
                WriteConfigDataToJson(win.TargetInfo.ConfigHandle, out var data);
                File.WriteAllText(path, data);
                AssetDatabase.SaveAssetIfDirty(utilityConfig.EditorMetadataObject.ConfigJson);
                AssetDatabase.Refresh();
                EditorUtility.SetDirty(AssetDatabase.LoadAssetAtPath<TextAsset>(path));
                AssetDatabase.SaveAssets();
            }
            else
            {
                // Create new json since the json config is missing.
                CreateConfig(win, previewRetargeter, previewer, utilityConfig, editorMetadataObject,
                    utilityConfig.MetadataAssetPath, false);
            }
        }

        /// <summary>
        /// Updates the configuration.
        /// </summary>
        /// <param name="win">The editor window.</param>
        /// <param name="previewRetargeter">The character retargeter for preview.</param>
        /// <param name="previewer">The editor previewer.</param>
        /// <param name="utilityConfig">The utility configuration.</param>
        /// <param name="editorMetadataObject">The editor metadata object.</param>
        /// <param name="saveConfig">Whether to save the configuration after updating.</param>
        /// <param name="updateReferenceTPose">Whether to update the reference T-pose.</param>
        public static void UpdateConfig(MSDKUtilityEditorWindow win, CharacterRetargeter previewRetargeter,
            MSDKUtilityEditorPreviewer previewer, MSDKUtilityEditorConfig utilityConfig,
            MSDKUtilityEditorMetadata editorMetadataObject, bool saveConfig, bool updateReferenceTPose)
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
            GetSkeletonMappings(target.ConfigHandle, SkeletonType.TargetSkeleton, SkeletonTPoseType.MinTPose,
                out var minJointMapping);
            GetSkeletonMappings(target.ConfigHandle, SkeletonType.TargetSkeleton, SkeletonTPoseType.MaxTPose,
                out var maxJointMapping);
            GetSkeletonMappingEntries(target.ConfigHandle, SkeletonType.TargetSkeleton,
                SkeletonTPoseType.MinTPose,
                out var minJointMappingEntries);
            GetSkeletonMappingEntries(target.ConfigHandle, SkeletonType.TargetSkeleton,
                SkeletonTPoseType.MaxTPose,
                out var maxJointMappingEntries);

            // Update selected target information.
            if (target.SkeletonTPoseType == SkeletonTPoseType.UnscaledTPose)
            {
                targetUnscaledTPose.CopyFrom(target.ReferencePose);
            }

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

            // Update reference T-Pose; this is only required while the scaling doesn't allow for rotation changes
            if (updateReferenceTPose)
            {
                // Instantiate reference T-Pose.
                var tempTPose = new GameObject();
                for (var i = 0; i < target.SkeletonInfo.JointCount; i++)
                {
                    var pose = new GameObject
                    {
                        name = target.JointNames[i]
                    };
                    pose.transform.SetPositionAndRotation(targetUnscaledTPose[i].Position,
                        targetUnscaledTPose[i].Orientation);
                    pose.transform.SetParent(tempTPose.transform);
                }

                for (var i = 0; i < target.SkeletonInfo.JointCount; i++)
                {
                    GetParentJointIndex(target.ConfigHandle, SkeletonType.TargetSkeleton, i, out var parentIndex);
                    if (parentIndex == -1)
                    {
                        continue;
                    }

                    var child = FindChildRecursiveExact(tempTPose.transform, target.JointNames[i]);
                    var parent = FindChildRecursiveExact(tempTPose.transform, target.JointNames[parentIndex]);
                    child.transform.SetParent(parent);
                }

                // Update orientations.
                for (var i = 0; i < target.SkeletonInfo.JointCount; i++)
                {
                    if (target.ReferencePose[i].Orientation == targetUnscaledTPose[i].Orientation)
                    {
                        continue;
                    }

                    var tar = FindChildRecursiveExact(tempTPose.transform, target.JointNames[i]);
                    tar.rotation = target.ReferencePose[i].Orientation;
                }

                // Update unscaled T-Pose data.
                for (var i = 0; i < target.SkeletonInfo.JointCount; i++)
                {
                    var tar = FindChildRecursiveExact(tempTPose.transform, target.JointNames[i]);
                    targetUnscaledTPose[i] = new NativeTransform(tar.rotation, tar.position);
                }

                DestroyImmediate(tempTPose);
            }

            // Update target known joint names with transform references.
            for (var i = 0; i < (int)KnownJointType.KnownJointCount; i++)
            {
                target.KnownJointNames[i] = target.KnownSkeletonJoints[i].name;
            }

            for (var i = 0; i < targetMinTPose.Length; i++)
            {
                var pose = targetMinTPose[i];
                pose.Scale = Vector3.one * Mathf.Clamp(pose.Scale.x, 0.001f, float.MaxValue);
                targetMinTPose[i] = pose;
            }

            for (var i = 0; i < targetMaxTPose.Length; i++)
            {
                var pose = targetMaxTPose[i];
                pose.Scale = Vector3.one * Mathf.Clamp(pose.Scale.x, 0.001f, float.MaxValue);
                targetMaxTPose[i] = pose;
            }

            CreateOrUpdateUtilityConfig(source.ConfigName,
                source.BlendshapeNames,
                source.JointNames,
                source.ParentJointNames,
                source.KnownJointNames,
                sourceMinTPose, sourceMaxTPose,
                target.BlendshapeNames,
                target.JointNames,
                target.ParentJointNames, target.KnownJointNames,
                targetUnscaledTPose, targetMinTPose, targetMaxTPose,
                minJointMapping, minJointMappingEntries,
                maxJointMapping, maxJointMappingEntries,
                out var newConfigHandle);
            target.ConfigHandle = source.ConfigHandle = newConfigHandle;
            if (saveConfig)
            {
                SaveConfig(win, previewRetargeter, previewer, utilityConfig, editorMetadataObject, false);
                LoadConfig(win, previewRetargeter, previewer, utilityConfig);
            }
            else
            {
                WriteConfigDataToJson(newConfigHandle, out var jsonConfigData);
                LoadConfig(win, previewRetargeter, previewer, utilityConfig, jsonConfigData);
            }
        }

        /// <summary>
        /// Loads the configuration.
        /// </summary>
        /// <param name="win">The editor window.</param>
        /// <param name="previewRetargeter">The character retargeter for preview.</param>
        /// <param name="previewer">The editor previewer.</param>
        /// <param name="utilityConfig">The utility configuration.</param>
        /// <param name="customConfig">Optional custom configuration JSON string.</param>
        public static void LoadConfig(MSDKUtilityEditorWindow win, CharacterRetargeter previewRetargeter,
            MSDKUtilityEditorPreviewer previewer, MSDKUtilityEditorConfig utilityConfig, string customConfig = null)
        {
            LoadConfig(win, previewRetargeter, previewer, utilityConfig, utilityConfig.Step switch
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
        /// <param name="previewRetargeter">The character retargeter for preview.</param>
        /// <param name="previewer">The editor previewer.</param>
        /// <param name="utilityConfig">The utility configuration.</param>
        /// <param name="targetTPoseType">The target T-pose type.</param>
        /// <param name="customConfig">Optional custom configuration JSON string.</param>
        public static void LoadConfig(MSDKUtilityEditorWindow win, CharacterRetargeter previewRetargeter,
            MSDKUtilityEditorPreviewer previewer, MSDKUtilityEditorConfig utilityConfig,
            SkeletonTPoseType targetTPoseType, string customConfig = null)
        {
            var source = win.SourceInfo;
            var target = win.TargetInfo;
            // Create new config to be cached; this is destructive.
            var config = utilityConfig.EditorMetadataObject.ConfigJson.text;
            if (!string.IsNullOrEmpty(customConfig))
            {
                config = customConfig;
            }

            var tPoseType = utilityConfig.Step switch
            {
                EditorStep.MinTPose => SkeletonTPoseType.MinTPose,
                EditorStep.MaxTPose => SkeletonTPoseType.MaxTPose,
                _ => SkeletonTPoseType.UnscaledTPose
            };
            source.Initialize(config, SkeletonType.SourceSkeleton, tPoseType);
            target.Initialize(config, SkeletonType.TargetSkeleton, targetTPoseType);

            // Store created handles.
            utilityConfig.Handles.Add(source.ConfigHandle);
            utilityConfig.Handles.Add(target.ConfigHandle);

            // Setup preview.
            if (previewRetargeter != null)
            {
                previewRetargeter.Setup(config);
            }

            // Setup scale.
            if (!utilityConfig.CurrentlyEditing && !utilityConfig.SetTPose)
            {
                JointAlignmentUtility.LoadScale(target, utilityConfig);
            }

            previewer.AssociateSceneCharacter(target);
            previewer.ReloadCharacter(target);
        }

        /// <summary>
        /// Resets the configuration.
        /// </summary>
        /// <param name="win">The editor window.</param>
        /// <param name="previewRetargeter">The character retargeter for preview.</param>
        /// <param name="previewer">The editor previewer.</param>
        /// <param name="utilityConfig">The utility configuration.</param>
        /// <param name="customConfig">Optional custom configuration JSON string.</param>
        /// <param name="displayDialogue">Whether to display a confirmation dialogue.</param>
        public static void ResetConfig(MSDKUtilityEditorWindow win, CharacterRetargeter previewRetargeter,
            MSDKUtilityEditorPreviewer previewer, MSDKUtilityEditorConfig utilityConfig, string customConfig = null,
            bool displayDialogue = true)
        {
            if (displayDialogue && !EditorUtility.DisplayDialog("Reset Configuration",
                    "Reset the configuration file?\n\nWarning: Resetting the configuration will discard all changes made.",
                    "Yes", "No"))
            {
                return;
            }

            win.SourceInfo = CreateInstance<MSDKUtilityEditorConfig>();
            win.TargetInfo = CreateInstance<MSDKUtilityEditorConfig>();
            LoadConfig(win, previewRetargeter, previewer, utilityConfig, SkeletonTPoseType.UnscaledTPose,
                customConfig);
        }

        /// <summary>
        /// Creates a new configuration.
        /// </summary>
        /// <param name="win">The editor window.</param>
        /// <param name="previewRetargeter">The character retargeter for preview.</param>
        /// <param name="previewer">The editor previewer.</param>
        /// <param name="utilityConfig">The utility configuration.</param>
        /// <param name="editorMetadataObject">The editor metadata object.</param>
        /// <param name="metadataAssetPath">The path to the metadata asset.</param>
        /// <param name="displayDialogue">Whether to display a confirmation dialogue.</param>
        /// <param name="customData">Optional custom skeleton data.</param>
        public static void CreateConfig(MSDKUtilityEditorWindow win, CharacterRetargeter previewRetargeter,
            MSDKUtilityEditorPreviewer previewer, MSDKUtilityEditorConfig utilityConfig,
            MSDKUtilityEditorMetadata editorMetadataObject, string metadataAssetPath, bool displayDialogue = true,
            SkeletonData customData = null)
        {
            if (displayDialogue && !EditorUtility.DisplayDialog("Create Configuration",
                    "Create configuration file?\n\nWarning: The configuration file will be based on the current state.",
                    "Yes", "No"))
            {
                return;
            }

            var configPath = string.Empty;
            if (editorMetadataObject == null)
            {
                var originalAsset =
                    AssetDatabase.LoadAssetAtPath(metadataAssetPath, typeof(GameObject)) as GameObject;
                editorMetadataObject = MSDKUtilityEditorMetadata.FindMetadataObject(originalAsset);
                configPath = Path.ChangeExtension(AssetDatabase.GetAssetPath(editorMetadataObject), ".json");
            }
            else if (editorMetadataObject.ConfigJson != null)
            {
                configPath = AssetDatabase.GetAssetPath(editorMetadataObject.ConfigJson);
            }

            editorMetadataObject.ConfigJson =
                MSDKUtilityEditor.CreateRetargetingConfig(
                    MSDKUtilityEditor.GetSkeletonRetargetingData("OVRSkeletonRetargetingData"),
                    editorMetadataObject.Model,
                    customData, configPath);
            EditorUtility.SetDirty(editorMetadataObject);
            AssetDatabase.SaveAssets();
            ResetConfig(win, previewRetargeter, previewer, utilityConfig, editorMetadataObject.ConfigJson.text,
                false);
        }

        /// <summary>
        /// Validates the editor information.
        /// </summary>
        /// <param name="previewer">The editor previewer.</param>
        /// <param name="skeletonDrawTPose">The skeleton draw for T-pose.</param>
        /// <param name="config">The utility configuration.</param>
        public static void ValidateMSDKUtilityEditorInfo(MSDKUtilityEditorPreviewer previewer,
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
