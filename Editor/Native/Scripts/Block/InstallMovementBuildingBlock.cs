// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System.Reflection;
using Meta.XR.BuildingBlocks.Editor;
using UnityEditor;
using UnityEngine;

namespace Meta.XR.Movement.Networking.Block.Editor
{
    /// <summary>
    /// Building block installation helper menu for Movement SDK.
    /// </summary>
    public static class InstallMovementBuildingBlock
    {
        private const BindingFlags _flags = BindingFlags.NonPublic | BindingFlags.Instance;

        [MenuItem("GameObject/Movement SDK/Body Tracking/Add Character Retargeter")]
        private static void InstallRetargetedCharacterBuildingBlock()
        {
            if (Selection.activeGameObject == null)
            {
                Debug.LogError("Must select a model to setup the skeleton retargeter on!");
                return;
            }

            InstallBuildingBlock("ICharacterRetargeter");
        }

        [MenuItem("GameObject/Movement SDK/Networking/Add Networked Character Retargeter")]
        private static void InstallNetworkedRetargetedCharacterBuildingBlock()
        {
            InstallBuildingBlock("INetworkCharacterRetargeter");
        }

        private static void InstallBuildingBlock(string buildingBlockAssetName)
        {
            var guids = AssetDatabase.FindAssets(buildingBlockAssetName);
            if (guids == null || guids.Length == 0)
            {
                Debug.LogError($"Asset {buildingBlockAssetName} cannot be found.");
                return;
            }

            BlockData blockData = null;
            foreach (var guid in guids)
            {
                var pathToAsset = AssetDatabase.GUIDToAssetPath(guid);
                blockData = AssetDatabase.LoadAssetAtPath<BlockData>(pathToAsset);
                if (blockData != null)
                {
                    break;
                }
            }
            if (blockData == null)
            {
                Debug.LogError("Could not find block data.");
                return;
            }

            var installMethod = typeof(BlockData).GetMethod("ContextMenuInstall", _flags);
            if (installMethod == null)
            {
                Debug.LogError("Could not find install method.");
                return;
            }
            installMethod.Invoke(blockData, null);
        }
    }
}
