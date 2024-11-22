// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

#if META_PLATFORM_SDK_DEFINED
using Meta.XR.MultiplayerBlocks.Shared;
#endif // META_PLATFORM_SDK_DEFINED
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Meta.XR.Movement.Networking.NGO
{
    /// <summary>
    /// Implementation of <see cref="INetworkPoseRetargeterSpawner"/> using the Unity Netcode for
    /// GameObjects networking framework.
    /// </summary>
    public class NetworkPoseRetargeterSpawnerNGO : NetworkBehaviour, INetworkPoseRetargeterSpawner
    {
        /// <summary>
        /// A static array of pose retargeter prefab references that should be the same across all users.
        /// </summary>
        public static GameObject[] PoseRetargeterPrefabReferences => _poseRetargeterPrefabReferences;

        /// <inheritdoc cref="INetworkPoseRetargeterSpawner.SelectedCharacterIndex"/>
        public int SelectedCharacterIndex
        {
            get => _selectedCharacterIndex;
            set => _selectedCharacterIndex = value;
        }

        /// <inheritdoc cref="INetworkPoseRetargeterSpawner.PoseRetargeterPrefabs"/>
        public GameObject[] PoseRetargeterPrefabs
        {
            get => _poseRetargeterPrefabs;
            set => _poseRetargeterPrefabs = value;
        }

        /// <summary>
        /// True if the character should be loaded when connected.
        /// </summary>
        [SerializeField]
        [Tooltip("Control when you want to load the character.")]
        private bool _loadCharacterWhenConnected = true;

        /// <inheritdoc cref="INetworkPoseRetargeterSpawner.SelectedCharacterIndex"/>
        [SerializeField]
        private int _selectedCharacterIndex = 0;

        /// <inheritdoc cref="INetworkPoseRetargeterSpawner.PoseRetargeterPrefabs"/>
        [SerializeField]
        private GameObject[] _poseRetargeterPrefabs;

        /// <summary>
        /// The base networked character prefab to contain the instantiated pose retargeter.
        /// </summary>
        [SerializeField]
        internal GameObject _networkedCharacter;

        private static GameObject[] _poseRetargeterPrefabReferences { get; set; }
        private Dictionary<ulong, GameObject> _idCharacterMapping { get; set; }

#if META_PLATFORM_SDK_DEFINED
        private PlatformInfo? _platformInfo;
#endif // META_PLATFORM_SDK_DEFINED

        private void Awake()
        {
            _poseRetargeterPrefabReferences = _poseRetargeterPrefabs;
            _idCharacterMapping = new Dictionary<ulong, GameObject>();
#if META_PLATFORM_SDK_DEFINED
            PlatformInit.GetEntitlementInformation(OnEntitlementFinished);
#endif // META_PLATFORM_SDK_DEFINED
        }

        /// <inheritdoc cref="OnNetworkSpawn"/>
        public override void OnNetworkSpawn()
        {
#if META_PLATFORM_SDK_DEFINED
            if (_platformInfo.HasValue && _loadCharacterWhenConnected)
            {
                SpawnCharacter();
            }
#else
            if (_loadCharacterWhenConnected)
            {
                SpawnCharacter();
            }
#endif // META_PLATFORM_SDK_DEFINED
        }

#if META_PLATFORM_SDK_DEFINED
        private void OnEntitlementFinished(PlatformInfo info)
        {
            _platformInfo = info;
            Debug.Log(
                $"Entitlement callback: isEntitled: {info.IsEntitled} Name: {info.OculusUser?.OculusID} UserID: {info.OculusUser?.ID}");
            if (IsSpawned && _loadCharacterWhenConnected)
            {
                SpawnCharacter();
            }
        }
#endif // META_PLATFORM_SDK_DEFINED

        /// <summary>
        /// Spawns a character.
        /// </summary>
        public void SpawnCharacter()
        {
            ulong metaId = 0;
#if META_PLATFORM_SDK_DEFINED
            if (_platformInfo is { IsEntitled: true, OculusUser: not null })
            {
                metaId = _platformInfo.Value.OculusUser!.ID;
            }
#endif // META_PLATFORM_SDK_DEFINED

            SpawnCharacterServerRpc(metaId, SelectedCharacterIndex);
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void SpawnCharacterServerRpc(ulong metaId, int characterId, RpcParams rpcParams = default)
        {
            var owningId = rpcParams.Receive.SenderClientId;
            if (_idCharacterMapping.ContainsKey(owningId))
            {
                var mappedCharacter = _idCharacterMapping[owningId].GetComponent<NetworkObject>();
                mappedCharacter.Despawn();
                _idCharacterMapping.Remove(owningId);
            }
            var targetPosition = Vector3.zero;
            var targetRotation = Quaternion.identity;
            var character = Instantiate(_networkedCharacter, targetPosition, targetRotation);
            var networkCharacter = character.GetComponent<NetworkObject>();
            networkCharacter.SpawnWithOwnership(owningId);
            var behaviour = character.GetComponent<NetworkPoseRetargeterBehaviourNGO>();
            behaviour.MetaId = metaId;
            behaviour.CharacterId = characterId + 1;
            _idCharacterMapping.Add(owningId, character);
        }
    }
}
