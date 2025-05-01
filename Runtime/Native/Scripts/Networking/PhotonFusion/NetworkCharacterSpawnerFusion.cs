// Copyright (c) Meta Platforms, Inc. and affiliates.

using Fusion;
#if META_PLATFORM_SDK_DEFINED
using Meta.XR.MultiplayerBlocks.Shared;
#endif // META_PLATFORM_SDK_DEFINED
using Meta.XR.MultiplayerBlocks.Fusion;
using System.Collections;
using UnityEngine;

namespace Meta.XR.Movement.Networking.Fusion
{
    /// <summary>
    /// Implementation of <see cref="INetworkCharacterSpawner"/> using the Photon Fusion 2
    /// networking framework.
    /// </summary>
    public class NetworkCharacterSpawnerFusion : MonoBehaviour, INetworkCharacterSpawner
    {
        /// <summary>
        /// A static array of character prefab references that should be the same across all users.
        /// </summary>
        public static GameObject[] CharacterPrefabReferences => _characterPrefabReferences;

        /// <inheritdoc cref="INetworkCharacterSpawner.SelectedCharacterIndex"/>
        public int SelectedCharacterIndex
        {
            get => _selectedCharacterIndex;
            set => _selectedCharacterIndex = value;
        }

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

        private static GameObject[] _characterPrefabReferences { get; set; }

#if META_PLATFORM_SDK_DEFINED
        private PlatformInfo? _platformInfo;
#endif // META_PLATFORM_SDK_DEFINED

        private NetworkRunner _networkRunner;
        private bool _sceneLoaded;
        private bool _entitlementCompleted;

        private void Awake()
        {
            _characterPrefabReferences = _characterRetargeterPrefabs;
#if META_PLATFORM_SDK_DEFINED
            PlatformInit.GetEntitlementInformation(OnEntitlementFinished);
#else
            if (_loadCharacterWhenConnected)
            {
                _entitlementCompleted = true;
                SpawnCharacter();
            }
#endif // META_PLATFORM_SDK_DEFINED
        }

        private void OnEnable()
        {
            FusionBBEvents.OnSceneLoadDone += OnLoaded;
        }

        private void OnDisable()
        {
            FusionBBEvents.OnSceneLoadDone -= OnLoaded;
        }

        private void OnLoaded(NetworkRunner networkRunner)
        {
            _sceneLoaded = true;
            _networkRunner = networkRunner;
        }

        /// <summary>
        /// Spawns a character.
        /// </summary>
        public void SpawnCharacter()
        {
            StartCoroutine(SpawnCharacterRoutine());
        }

#if META_PLATFORM_SDK_DEFINED
        private void OnEntitlementFinished(PlatformInfo info)
        {
            _platformInfo = info;
            Debug.Log(
                $"Entitlement callback: isEntitled: {info.IsEntitled} Name: {info.OculusUser?.OculusID} UserID: {info.OculusUser?.ID}");
            _entitlementCompleted = true;

            if (_loadCharacterWhenConnected)
            {
                SpawnCharacter();
            }
        }
#endif // META_PLATFORM_SDK_DEFINED

        private IEnumerator SpawnCharacterRoutine()
        {
            while (_networkRunner == null || !_sceneLoaded || !_entitlementCompleted)
            {
                yield return null;
            }

            ulong metaId = 0;
#if META_PLATFORM_SDK_DEFINED
            if (_platformInfo is { IsEntitled: true, OculusUser: not null })
            {
                metaId = _platformInfo.Value.OculusUser!.ID;
            }
#endif // META_PLATFORM_SDK_DEFINED

            _networkRunner.Spawn(
                _networkedCharacterHandler,
                Vector3.zero,
                Quaternion.identity,
                _networkRunner.LocalPlayer,
                (runner, obj) => // onBeforeSpawned
                {
                    var behaviour = obj.GetComponent<NetworkCharacterBehaviourFusion>();
                    behaviour.MetaId = metaId;
                    behaviour.CharacterId = SelectedCharacterIndex + 1;
                }
            );
        }
    }
}
