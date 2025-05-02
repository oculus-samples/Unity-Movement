// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Meta.XR.Movement.Editor
{
    /// <summary>
    /// Metadata for the Movement SDK utility editor.
    /// </summary>
    [CreateAssetMenu, Serializable]
    public class MSDKUtilityEditorMetadata : ScriptableObject
    {
        /// <summary>
        /// The model GameObject associated with this metadata.
        /// </summary>
        public GameObject Model
        {
            get => _model;
            set => _model = value;
        }

        /// <summary>
        /// The configuration JSON asset.
        /// </summary>
        public TextAsset ConfigJson
        {
            get => _configJson;
            set => _configJson = value;
        }

        [SerializeField]
        private GameObject _model;

        [SerializeField]
        private TextAsset _configJson;

        /// <summary>
        /// Loads the configuration asset for the specified metadata asset path.
        /// </summary>
        /// <param name="metadataAssetPath">The path to the metadata asset.</param>
        /// <param name="editorMetadataObject">The editor metadata object to update.</param>
        /// <param name="displayedConfig">Whether the configuration has been displayed.</param>
        public static void LoadConfigAsset(string metadataAssetPath, ref MSDKUtilityEditorMetadata editorMetadataObject, ref bool displayedConfig)
        {
            if (editorMetadataObject != null && editorMetadataObject.ConfigJson != null)
            {
                return;
            }

            var originalAsset = AssetDatabase.LoadAssetAtPath(metadataAssetPath, typeof(GameObject)) as GameObject;
            editorMetadataObject = FindMetadataObject(originalAsset);
            if (editorMetadataObject == null)
            {
                editorMetadataObject = CreateInstance<MSDKUtilityEditorMetadata>();
                editorMetadataObject.Model = AssetDatabase.LoadAssetAtPath<GameObject>(metadataAssetPath);
                AssetDatabase.CreateAsset(editorMetadataObject,
                    Path.ChangeExtension(metadataAssetPath, "-metadata.asset")?.Replace(".-metadata", "-metadata"));
            }

            if (editorMetadataObject.ConfigJson == null)
            {
                editorMetadataObject.ConfigJson =
                    AssetDatabase.LoadAssetAtPath<TextAsset>(Path.ChangeExtension(metadataAssetPath, ".json"));

                if (!displayedConfig &&
                    editorMetadataObject.ConfigJson == null &&
                    EditorUtility.DisplayDialog("Create Json", "Config Json not found. Create?",
                        "Create", "Cancel"))
                {
                    editorMetadataObject.ConfigJson = MSDKUtilityEditor.CreateRetargetingConfig(MSDKUtilityEditor.GetSkeletonRetargetingData("OVRSkeletonRetargetingData"), editorMetadataObject.Model);
                }

                displayedConfig = true;
                EditorUtility.SetDirty(editorMetadataObject);
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>
        /// Finds the metadata object for the specified target model.
        /// </summary>
        /// <param name="targetModel">The target model GameObject.</param>
        /// <returns>The metadata object if found, null otherwise.</returns>
        public static MSDKUtilityEditorMetadata FindMetadataObject(GameObject targetModel)
        {
            var targetAsset = targetModel;
            var targetType = typeof(MSDKUtilityEditorMetadata);
            var guids = AssetDatabase.FindAssets($"t:{nameof(MSDKUtilityEditorMetadata)}");
            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath(assetPath, targetType) as MSDKUtilityEditorMetadata;
                if (asset == null || asset.Model == null)
                {
                    continue;
                }

                if (asset.Model == targetAsset)
                {
                    return asset;
                }
            }

            return null;
        }
    }
}
