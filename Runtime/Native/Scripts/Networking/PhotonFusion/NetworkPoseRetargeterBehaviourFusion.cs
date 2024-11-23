// Copyright (c) Meta Platforms, Inc. and affiliates.

using Fusion;
using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Meta.XR.Movement.Networking.Fusion
{
    /// <summary>
    /// Implementation of <see cref="INetworkPoseRetargeterBehaviour"/> using the Photon Fusion 2
    /// networking framework.
    /// </summary>
    public class NetworkPoseRetargeterBehaviourFusion : NetworkBehaviour, INetworkPoseRetargeterBehaviour,
        IPlayerJoined, IPlayerLeft
    {
        /// <inheritdoc cref="INetworkPoseRetargeterBehaviour.CharacterPrefab"/>
        public GameObject CharacterPrefab =>
            NetworkPoseRetargeterSpawnerFusion.PoseRetargeterPrefabReferences[CharacterId - 1];

        /// <inheritdoc cref="INetworkPoseRetargeterBehaviour.MetaId"/>
        [Networked]
        public ulong MetaId
        {
            get;
            set;
        }

        /// <inheritdoc cref="INetworkPoseRetargeterBehaviour.CharacterId"/>
        [Networked, OnChangedRender(nameof(OnCharacterIdChanged))]
        public int CharacterId
        {
            get;
            set;
        }

        /// <inheritdoc cref="INetworkPoseRetargeterBehaviour.NetworkTime"/>
        public float NetworkTime => Runner.SimulationTime;

        /// <inheritdoc cref="INetworkPoseRetargeterBehaviour.RenderTime"/>
        public float RenderTime => Runner.SimulationTime - Runner.TickRate / _renderRateFactor;

        /// <inheritdoc cref="INetworkPoseRetargeterBehaviour.DeltaTime"/>
        public float DeltaTime => Runner.DeltaTime;

        /// <inheritdoc cref="INetworkPoseRetargeterBehaviour.ClientIds"/>
        public ulong[] ClientIds
        {
            get => _clientIds;
            set => _clientIds = value;
        }

        /// <inheritdoc cref="INetworkPoseRetargeterBehaviour.LocalClientId"/>
        public ulong LocalClientId => (ulong)Runner.LocalPlayer.PlayerId;

        /// <summary>
        /// The factor to be applied to the tick rate when rendering.
        /// </summary>
        [SerializeField]
        private float _renderRateFactor = 250f;

        private readonly List<ulong> _clientIdList = new();
        private const int _maxPlayers = 64;

        private ulong[] _clientIds = Array.Empty<ulong>();
        private Transform _cameraRig;
        private INetworkPoseRetargeter _poseRetargeter;

        /// <inheritdoc cref="INetworkPoseRetargeterBehaviour.ReceiveStreamData"/>
        public void ReceiveStreamData(ulong clientId, bool isReliable, NativeArray<byte> bytes)
        {
            var arrayBytes = new byte[bytes.Length];
            bytes.CopyTo(arrayBytes);
            var target = GetTargetPlayer(clientId);
            if (target.IsNone)
            {
                return;
            }

            if (isReliable)
            {
                ReceiveStreamDataRpc(target, arrayBytes);
            }
            else
            {
                ReceiveStreamDataUnreliableRpc(target, arrayBytes);
            }
        }

        /// <inheritdoc cref="INetworkPoseRetargeterBehaviour.ReceiveStreamAck"/>
        public void ReceiveStreamAck(ulong clientId, int ack)
        {
            ReceiveStreamAckUnreliableRpc(clientId, ack);
        }

        /// <inheritdoc />
        public override void Spawned()
        {
            // Initialize local components.
            _poseRetargeter = GetComponent<INetworkPoseRetargeter>();
            _cameraRig = OVRManager.instance?.GetComponentInChildren<OVRCameraRig>().transform;

            // Initialize network variables.
            OnCharacterIdChanged();
            UpdateClientIds();
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
            if (CharacterId == 0 || _poseRetargeter == null)
            {
                return;
            }

            _poseRetargeter.Setup();
        }

        /// <inheritdoc />
        public override void FixedUpdateNetwork()
        {
            if (!HasInputAuthority || _cameraRig == null)
            {
                return;
            }

            transform.SetPositionAndRotation(_cameraRig.position, _cameraRig.rotation);
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

        private PlayerRef GetTargetPlayer(ulong targetClientId)
        {
            if (Runner == null)
            {
                return PlayerRef.None;
            }

            foreach (var player in Runner.ActivePlayers)
            {
                if ((ulong)player.PlayerId == targetClientId)
                {
                    return player;
                }
            }

            return PlayerRef.None;
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.Proxies, InvokeLocal = false, Channel = RpcChannel.Reliable)]
        private void ReceiveStreamDataRpc([RpcTarget] PlayerRef player, byte[] bytes)
        {
            var arrayBytes = new NativeArray<byte>(bytes.Length, Unity.Collections.Allocator.Temp,
                NativeArrayOptions.UninitializedMemory);
            arrayBytes.CopyFrom(bytes);
            _poseRetargeter.ReceiveData(arrayBytes);
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.Proxies, InvokeLocal = false, Channel = RpcChannel.Unreliable)]
        private void ReceiveStreamDataUnreliableRpc([RpcTarget] PlayerRef player, byte[] bytes)
        {
            var arrayBytes = new NativeArray<byte>(bytes.Length, Unity.Collections.Allocator.Temp,
                NativeArrayOptions.UninitializedMemory);
            arrayBytes.CopyFrom(bytes);
            _poseRetargeter.ReceiveData(arrayBytes);
        }

        [Rpc(RpcSources.All, RpcTargets.InputAuthority, InvokeLocal = false, Channel = RpcChannel.Unreliable)]
        private void ReceiveStreamAckUnreliableRpc(ulong id, int ack)
        {
            _poseRetargeter.ReceiveAck(id, ack);
        }
    }
}
