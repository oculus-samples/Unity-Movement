// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

#if FUSION2
using Fusion;
#endif
using System;
using Unity.Collections;
using UnityEngine;

namespace Meta.XR.Movement.Networking.Fusion
{
#if FUSION2
    /// <summary>
    /// Implementation of <see cref="INetworkCharacterBehaviour"/> using the Photon Fusion 2
    /// networking framework. Uses NetworkCharacterDataStreamFusion for simplified data synchronization.
    /// </summary>
    public class NetworkCharacterBehaviourFusion : NetworkBehaviour, INetworkCharacterBehaviour,
        IPlayerJoined, IPlayerLeft
    {
        /// <inheritdoc cref="INetworkCharacterBehaviour.CharacterPrefab"/>
        public GameObject CharacterPrefab =>
            NetworkCharacterSpawnerFusion.CharacterPrefabReferences[CharacterId - 1];

        /// <inheritdoc cref="INetworkCharacterBehaviour.MetaId"/>
        [Networked]
        public ulong MetaId
        {
            get;
            set;
        }

        /// <inheritdoc cref="INetworkCharacterBehaviour.CharacterId"/>
        [Networked, OnChangedRender(nameof(OnCharacterIdChanged))]
        public int CharacterId
        {
            get;
            set;
        }

        /// <inheritdoc cref="INetworkCharacterBehaviour.NetworkTime"/>
        public float NetworkTime => Runner.SimulationTime;

        /// <inheritdoc cref="INetworkCharacterBehaviour.RenderTime"/>
        public float RenderTime => Runner.SimulationTime - Runner.TickRate / _renderRateFactor;

        /// <inheritdoc cref="INetworkCharacterBehaviour.DeltaTime"/>
        public float DeltaTime => Runner.DeltaTime;

        /// <inheritdoc cref="INetworkCharacterBehaviour.ClientIds"/>
        public ulong[] ClientIds
        {
            get => _clientIds;
            set => _clientIds = value;
        }

        /// <inheritdoc cref="INetworkCharacterBehaviour.LocalClientId"/>
        public ulong LocalClientId => (ulong)Runner.LocalPlayer.PlayerId;

        [Networked]
        public float CharacterScale
        {
            get;
            set;
        }

        [Networked]
        private int CharacterDataStreamLength { get; set; }

        [Networked]
        [Capacity(_characterDataStreamMaxCapacity)]
        [OnChangedRender(nameof(OnCharacterDataStreamChanged))]
        private NetworkArray<byte> CharacterDataStream => default;

        /// <inheritdoc cref="_followCameraRig"/>
        public bool FollowCameraRig
        {
            get => _followCameraRig;
            set => _followCameraRig = value;
        }

        /// <summary>
        /// True if this transform should follow the OVRCameraRig in scene.
        /// </summary>
        [SerializeField]
        private bool _followCameraRig;

        /// <summary>
        /// The factor to be applied to the tick rate when rendering.
        /// </summary>
        [SerializeField]
        private float _renderRateFactor = 250f;

        private readonly System.Collections.Generic.List<ulong> _clientIdList = new();
        private const int _characterDataStreamMaxCapacity = 1024;
        private const int _maxPlayers = 64;
        private ulong[] _clientIds = Array.Empty<ulong>();
        private Transform _cameraRig;
        private INetworkCharacterHandler _characterHandler;
        private NetworkCharacterDataStreamFusion _characterDataStreamHelper;

        /// <inheritdoc cref="INetworkCharacterBehaviour.ReceiveStreamData"/>
        public void ReceiveStreamData(ulong clientId, bool isReliable, NativeArray<byte> bytes)
        {
            if (!Object.HasStateAuthority)
            {
                return;
            }

            var arrayBytes = new byte[bytes.Length];
            bytes.CopyTo(arrayBytes);

            int tempLength = CharacterDataStreamLength;
            _characterDataStreamHelper.SetData(arrayBytes, CharacterDataStream, ref tempLength);
            CharacterDataStreamLength = tempLength;
        }

        /// <inheritdoc cref="INetworkCharacterBehaviour.ReceiveStreamAck"/>
        public void ReceiveStreamAck(ulong clientId, int ack)
        {
            ReceiveStreamAckUnreliableRpc(clientId, ack);
        }

        /// <inheritdoc />
        public override void Spawned()
        {
            _characterHandler = GetComponent<INetworkCharacterHandler>();
            _cameraRig = OVRManager.instance?.GetComponentInChildren<OVRCameraRig>()?.transform;
            _characterDataStreamHelper = new NetworkCharacterDataStreamFusion();

            OnCharacterIdChanged();
            UpdateClientIds();
        }

        /// <inheritdoc />
        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);
            _characterDataStreamHelper = null;
        }

        /// <summary>
        /// Called when a player joins the session.
        /// </summary>
        /// <param name="player">The player reference.</param>
        public void PlayerJoined(PlayerRef player)
        {
            UpdateClientIds();
        }

        /// <summary>
        /// Called when a player leaves the session.
        /// </summary>
        /// <param name="player">The player reference.</param>
        public void PlayerLeft(PlayerRef player)
        {
            UpdateClientIds();
        }

        /// <summary>
        /// Called when the character id changed.
        /// </summary>
        public void OnCharacterIdChanged()
        {
            if (CharacterId == 0 || _characterHandler == null)
            {
                return;
            }

            _characterHandler.Setup();
        }

        /// <summary>
        /// Called when the character data stream changed.
        /// </summary>
        private void OnCharacterDataStreamChanged()
        {
            if (Object.HasStateAuthority || _characterHandler == null)
            {
                return;
            }

            var data = _characterDataStreamHelper.GetData(CharacterDataStream, CharacterDataStreamLength);
            if (data == null || data.Length == 0)
            {
                return;
            }

            var nativeArray = new NativeArray<byte>(data.Length, Unity.Collections.Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            nativeArray.CopyFrom(data);
            _characterHandler.ReceiveData(nativeArray);
            nativeArray.Dispose();
        }

        /// <inheritdoc />
        public override void FixedUpdateNetwork()
        {
            if (HasInputAuthority && _characterHandler?.Character != null)
            {
                CharacterScale = _characterHandler.Character.transform.localScale.x;
            }

            if (_cameraRig == null || !_followCameraRig)
            {
                return;
            }

            transform.SetPositionAndRotation(_cameraRig.position, _cameraRig.rotation);
        }

        private void FixedUpdate()
        {
            if (!HasInputAuthority && _characterHandler?.Character != null)
            {
                _characterHandler.Character.transform.localScale = Vector3.one * CharacterScale;
            }
        }

        private void UpdateClientIds()
        {
            if (Runner == null)
            {
                return;
            }

            _clientIdList.Clear();
            foreach (var player in Runner.ActivePlayers)
            {
                if (_clientIdList.Count >= _maxPlayers)
                {
                    Debug.LogError($"Up to {_maxPlayers} are supported in this implementation.");
                    return;
                }

                _clientIdList.Add((ulong)player.PlayerId);
            }

            _clientIds = _clientIdList.ToArray();
        }

        [Rpc(RpcSources.All, RpcTargets.InputAuthority, InvokeLocal = false, Channel = RpcChannel.Unreliable)]
        private void ReceiveStreamAckUnreliableRpc(ulong id, int ack)
        {
            _characterHandler?.ReceiveAck(id, ack);
        }
    }
#endif
}
