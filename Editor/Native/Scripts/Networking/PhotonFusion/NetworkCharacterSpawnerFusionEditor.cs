// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION_WEAVER && FUSION2
using Meta.XR.Movement.Networking.Editor;
using UnityEditor;

namespace Meta.XR.Movement.Networking.Fusion.Editor
{
    /// <summary>
    /// Implementation of <see cref="NetworkCharacterSpawnerEditor"/> for Photon Fusion 2.
    /// </summary>
    [CustomEditor(typeof(NetworkCharacterSpawnerFusion), true), CanEditMultipleObjects]
    public class NetworkCharacterSpawnerFusionEditor : NetworkCharacterSpawnerEditor
    {
        protected override string GetCharacterHandlerPrefabName()
        {
            return "NetworkCharacterHandlerFusion";
        }

        protected override void ValidateNetworkingSettings()
        {
            NetworkCharacterFusionSetupRules.Fix();
        }
    }
}
#endif // FUSION_WEAVER && FUSION2
