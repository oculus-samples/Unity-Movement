// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

#if UNITY_NGO_MODULE_DEFINED
using Meta.XR.Movement.Networking.Editor;
using UnityEditor;

namespace Meta.XR.Movement.Networking.NGO.Editor
{
    /// <summary>
    /// Implementation of <see cref="NetworkCharacterSpawnerEditor"/> for Unity Netcode with GameObjects.
    /// </summary>
    [CustomEditor(typeof(NetworkCharacterSpawnerNGO), true), CanEditMultipleObjects]
    public class NetworkCharacterSpawnerNGOEditor : NetworkCharacterSpawnerEditor
    {
        /// <summary>
        /// The asset name of the character prefab.
        /// </summary>
        public static string CharacterPrefabName => "NetworkCharacterHandlerNGO";

        /// <summary>
        /// The asset name of the spawner prefab.
        /// </summary>
        public static string SpawnerPrefabName => "NetworkCharacterSpawnerNGO";

        protected override string GetCharacterHandlerPrefabName()
        {
            return CharacterPrefabName;
        }

        protected override void ValidateNetworkingSettings()
        {
            NetworkCharacterNGOSetupRules.Fix();
        }
    }
}
#endif
