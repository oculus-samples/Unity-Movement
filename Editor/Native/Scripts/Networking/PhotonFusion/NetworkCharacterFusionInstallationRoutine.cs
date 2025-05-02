// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using System.Reflection;
using Meta.XR.BuildingBlocks.Editor;
using Meta.XR.Movement.Networking.Editor;
using Meta.XR.Movement.Networking.Fusion.Editor;
using UnityEditor;
using UnityEngine;

namespace Meta.XR.Movement.Networking.Fusion.Blocks.Editor
{
    /// <summary>
    /// Network installation routine for using Photon Fusion 2.
    /// </summary>
    public class NetworkCharacterFusionInstallationRoutine :
        Meta.XR.MultiplayerBlocks.Shared.Editor.NetworkInstallationRoutine
    {
        /// <summary>
        /// Installs the block using the selected game object.
        /// </summary>
        /// <param name="block">The block to be installed</param>
        /// <param name="selectedGameObject">The selected game object</param>
        /// <returns>The installed game object instances.</returns>
        /// <exception cref="OVRConfigurationTaskException">Error with executing this block.</exception>
        public override List<GameObject> Install(BlockData block, GameObject selectedGameObject)
        {
            var characterPrefab = NetworkCharacterSpawnerEditor.CreateCharacterPrefabFromModel();
            if (characterPrefab == null)
            {
                throw new OVRConfigurationTaskException("Must have a configured retargeted character for networking!");
            }

            var installation = base.Install(block, selectedGameObject);

            // Update prefab reference.
            var field = typeof(NetworkCharacterSpawnerFusion)
                .GetField("_characterRetargeterPrefabs", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                return installation;
            }

            var instance = installation[0].GetComponent<NetworkCharacterSpawnerFusion>();
            field.SetValue(instance, new[] { characterPrefab });
            NetworkCharacterFusionSetupRules.Fix();
            Undo.RecordObject(instance, "Update " + instance.name);

            return installation;
        }
    }
}
