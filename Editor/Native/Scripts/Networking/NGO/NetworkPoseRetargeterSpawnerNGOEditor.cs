// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

#if UNITY_NGO_MODULE_DEFINED
using Meta.XR.Movement.Networking.Editor;
using UnityEditor;

namespace Meta.XR.Movement.Networking.NGO.Editor
{
    /// <summary>
    /// Implementation of <see cref="NetworkPoseRetargeterSpawnerEditor"/> for Unity Netcode with GameObjects.
    /// </summary>
    [CustomEditor(typeof(NetworkPoseRetargeterSpawnerNGO), true), CanEditMultipleObjects]
    public class NetworkPoseRetargeterSpawnerNGOEditor : NetworkPoseRetargeterSpawnerEditor
    {
        /// <summary>
        /// The asset name of the character prefab.
        /// </summary>
        public static string CharacterPrefabName => "RetargetedCharacterNGO";

        /// <summary>
        /// The asset name of the spawner prefab.
        /// </summary>
        public static string SpawnerPrefabName => "NetworkPoseRetargeterNGO";

        protected override string GetCharacterPrefabName()
        {
            return CharacterPrefabName;
        }

        protected override void ValidateNetworkingSettings()
        {
            NetworkPoseRetargeterNGOSetupRules.Fix();
        }
    }
}
#endif
