// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

#if UNITY_NGO_MODULE_DEFINED
using Unity.Netcode;
#endif
#if META_PLATFORM_SDK_DEFINED
using Meta.XR.MultiplayerBlocks.Shared;
#endif // META_PLATFORM_SDK_DEFINED
using System.Collections.Generic;
using UnityEngine;

namespace Meta.XR.Movement.Networking.NGO
{
    /// <summary>
    /// Implementation of <see cref="INetworkCharacterSpawner"/> using the Unity Netcode for
    /// GameObjects networking framework.
    /// </summary>
#if UNITY_NGO_MODULE_DEFINED
    public class NetworkCharacterSpawnerNGO : NetworkBehaviour, INetworkCharacterSpawner
    {
        /// <summary>
        /// Static character prefab references shared across all users (thread-safe).
        /// </summary>
        public static GameObject[] CharacterPrefabReferences
        {
            get { lock (_prefabLock) { return _characterPrefabReferences; } }
        }

        /// <inheritdoc cref="INetworkCharacterSpawner.SelectedCharacterIndex"/>
        public int SelectedCharacterIndex
        {
            get => _selectedCharacterIndex;
            set => _selectedCharacterIndex = value;
        }

        /// <inheritdoc cref="INetworkCharacterSpawner.NetworkedCharacterHandler"/>
        public GameObject NetworkedCharacterHandler => _networkedCharacterHandler;

        /// <inheritdoc cref="INetworkCharacterSpawner.CharacterRetargeterPrefabs"/>
        public GameObject[] CharacterRetargeterPrefabs
        {
            get => _characterRetargeterPrefabs;
            set => _characterRetargeterPrefabs = value;
        }

        /// <summary>
        /// True if the character should be loaded when connected.
        /// </summary>
        [SerializeField]
        [Tooltip("Control when you want to load the character.")]
        private bool _loadCharacterWhenConnected = true;

        /// <inheritdoc cref="INetworkCharacterSpawner.SelectedCharacterIndex"/>
        [SerializeField]
        private int _selectedCharacterIndex = 0;

        /// <inheritdoc cref="INetworkCharacterSpawner.CharacterRetargeterPrefabs"/>
        [SerializeField]
        private GameObject[] _characterRetargeterPrefabs;

        /// <summary>
        /// The base network character handler prefab to contain the instantiated networked character.
        /// </summary>
        [SerializeField]
        private GameObject _networkedCharacterHandler;

        private static readonly object _prefabLock = new object();
        private static GameObject[] _characterPrefabReferences;
        private Dictionary<ulong, GameObject> _idCharacterMapping { get; set; }

#if META_PLATFORM_SDK_DEFINED
        private PlatformInfo? _platformInfo;
#endif // META_PLATFORM_SDK_DEFINED

        private void Awake()
        {
            // Thread-safe initialization of static prefab references
            // Only set if not already initialized or if we have more prefabs
            lock (_prefabLock)
            {
                if (_characterPrefabReferences == null ||
                    _characterPrefabReferences.Length < _characterRetargeterPrefabs.Length)
                {
                    _characterPrefabReferences = _characterRetargeterPrefabs;
                }
            }
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
            if (_idCharacterMapping.TryGetValue(owningId, out var existingCharacter))
            {
                var mappedCharacter = existingCharacter.GetComponent<NetworkObject>();
                mappedCharacter.Despawn();
                _idCharacterMapping.Remove(owningId);
            }
            var targetPosition = Vector3.zero;
            var targetRotation = Quaternion.identity;
            var character = Instantiate(_networkedCharacterHandler, targetPosition, targetRotation);
            var networkCharacter = character.GetComponent<NetworkObject>();
            networkCharacter.SpawnWithOwnership(owningId);
            var behaviour = character.GetComponent<NetworkCharacterBehaviourNGO>();
            behaviour.MetaId = metaId;
            behaviour.CharacterId = characterId + 1;
            _idCharacterMapping[owningId] = character;
        }
    }
#else
    public class NetworkCharacterSpawnerNGO : MonoBehaviour
    {
        /// <inheritdoc cref="INetworkCharacterSpawner.CharacterRetargeterPrefabs"/>
        public GameObject[] CharacterRetargeterPrefabs;
    }
#endif
}
