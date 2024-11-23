// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.BuildingBlocks.Editor;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Meta.XR.Movement.Networking.Block.Editor
{
    /// <summary>
    /// Installation helper menu for Movement SDK.
    /// </summary>
    public static class InstallMovementBuildingBlock
    {
        private const string _buildingBlockAssetName = "INetworkPoseRetargeter";
        private const BindingFlags _flags = BindingFlags.NonPublic | BindingFlags.Instance;

        [MenuItem("GameObject/Movement SDK/Networking/Add Networked Retargeted Character")]
        private static void InstallNetworkedRetargetedCharacterBuildingBlock()
        {
            var guids = AssetDatabase.FindAssets(_buildingBlockAssetName);
            if (guids == null || guids.Length == 0)
            {
                Debug.LogError($"Asset {_buildingBlockAssetName} cannot be found.");
                return;
            }

            InterfaceBlockData blockData = null;
            foreach (var guid in guids)
            {
                var pathToAsset = AssetDatabase.GUIDToAssetPath(guid);
                blockData = AssetDatabase.LoadAssetAtPath<InterfaceBlockData>(pathToAsset);
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
