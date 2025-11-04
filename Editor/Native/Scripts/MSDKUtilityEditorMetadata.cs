// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

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
        /// Finds the metadata object for the specified target model.
        /// </summary>
        /// <param name="targetModel">The target model GameObject.</param>
        /// <returns>The metadata object if found, null otherwise.</returns>
        public static MSDKUtilityEditorMetadata FindMetadataAsset(GameObject targetModel)
        {
            if (targetModel == null)
            {
                return null;
            }

            var targetAsset = targetModel;
            var targetType = typeof(MSDKUtilityEditorMetadata);
            var guids = AssetDatabase.FindAssets($"t:{nameof(MSDKUtilityEditorMetadata)}");
            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath(assetPath, targetType) as MSDKUtilityEditorMetadata;
                if (asset?.Model == targetAsset)
                {
                    return asset;
                }
            }

            return null;
        }
    }
}
