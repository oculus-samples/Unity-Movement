// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION_WEAVER && FUSION2
using Meta.XR.Movement.Networking.Editor;
using UnityEditor;

namespace Meta.XR.Movement.Networking.Fusion.Editor
{
    /// <summary>
    /// Implementation of <see cref="NetworkPoseRetargeterSpawnerEditor"/> for Photon Fusion 2.
    /// </summary>
    [CustomEditor(typeof(NetworkPoseRetargeterSpawnerFusion), true), CanEditMultipleObjects]
    public class NetworkPoseRetargeterSpawnerFusionEditor : NetworkPoseRetargeterSpawnerEditor
    {
        protected override string GetCharacterPrefabName()
        {
            return "RetargetedCharacterFusion";
        }

        protected override void ValidateNetworkingSettings()
        {
            NetworkPoseRetargeterFusionSetupRules.Fix();
        }
    }
}
#endif // FUSION_WEAVER && FUSION2
