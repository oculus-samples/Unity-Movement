// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

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
    /// Implementation of <see cref="INetworkPoseRetargeterSpawner"/> using the Photon Fusion 2
    /// networking framework.
    /// </summary>
    public class NetworkPoseRetargeterSpawnerFusion : MonoBehaviour, INetworkPoseRetargeterSpawner
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

#if META_PLATFORM_SDK_DEFINED
        private PlatformInfo? _platformInfo;
#endif // META_PLATFORM_SDK_DEFINED

        private NetworkRunner _networkRunner;
        private bool _sceneLoaded;
        private bool _entitlementCompleted;

        private void Awake()
        {
            _poseRetargeterPrefabReferences = _poseRetargeterPrefabs;
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
                _networkedCharacter,
                Vector3.zero,
                Quaternion.identity,
                _networkRunner.LocalPlayer,
                (runner, obj) => // onBeforeSpawned
                {
                    var behaviour = obj.GetComponent<NetworkPoseRetargeterBehaviourFusion>();
                    behaviour.MetaId = metaId;
                    behaviour.CharacterId = SelectedCharacterIndex + 1;
                }
            );
        }
    }
}
