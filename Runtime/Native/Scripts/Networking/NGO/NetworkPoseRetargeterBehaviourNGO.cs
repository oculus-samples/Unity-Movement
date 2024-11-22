// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Meta.XR.Movement.Networking.NGO
{
    /// <summary>
    /// Implementation of <see cref="INetworkPoseRetargeterBehaviour"/> using the Unity Netcode for
    /// GameObjects networking framework.
    /// </summary>
    public class NetworkPoseRetargeterBehaviourNGO : NetworkBehaviour, INetworkPoseRetargeterBehaviour
    {
        /// <inheritdoc cref="INetworkPoseRetargeterBehaviour.HasInputAuthority"/>
        public bool HasInputAuthority => IsOwner;

        /// <inheritdoc cref="INetworkPoseRetargeterBehaviour.CharacterPrefab"/>
        public GameObject CharacterPrefab =>
            NetworkPoseRetargeterSpawnerNGO.PoseRetargeterPrefabReferences[CharacterId - 1];

        /// <inheritdoc cref="INetworkPoseRetargeterBehaviour.MetaId"/>
        public ulong MetaId
        {
            get => _metaId.Value;
            set => _metaId.Value = value;
        }

        /// <inheritdoc cref="INetworkPoseRetargeterBehaviour.CharacterId"/>
        public int CharacterId
        {
            get => _characterId.Value;
            set => _characterId.Value = value;
        }

        /// <inheritdoc cref="INetworkPoseRetargeterBehaviour.NetworkTime"/>
        public float NetworkTime => NetworkManager.ServerTime.TimeAsFloat;

        /// <inheritdoc cref="INetworkPoseRetargeterBehaviour.RenderTime"/>
        public float RenderTime => NetworkManager.ServerTime.TimeTicksAgo(1).TimeAsFloat;

        /// <inheritdoc cref="INetworkPoseRetargeterBehaviour.DeltaTime"/>
        public float DeltaTime => NetworkManager.ServerTime.FixedDeltaTime;

        /// <inheritdoc cref="INetworkPoseRetargeterBehaviour.ClientIds"/>
        public ulong[] ClientIds
        {
            get => _localClientIds;
            set => _localClientIds = value;
        }

        /// <inheritdoc cref="INetworkPoseRetargeterBehaviour.LocalClientId"/>
        public ulong LocalClientId => NetworkManager.LocalClientId;

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

        private NetworkVariable<ulong> _metaId;
        private NetworkVariable<int> _characterId;
        private NetworkList<ulong> _clientIds;

        private ulong[] _localClientIds = Array.Empty<ulong>();
        private Transform _cameraRig;
        private INetworkPoseRetargeter _poseRetargeter;

        private void Awake()
        {
            // Initialize network variables.
            _metaId = new NetworkVariable<ulong>();
            _characterId = new NetworkVariable<int>();
            _clientIds = new NetworkList<ulong>();

            // Initialize local components.
            _poseRetargeter = GetComponent<INetworkPoseRetargeter>();
            _cameraRig = OVRManager.instance?.GetComponentInChildren<OVRCameraRig>().transform;
        }

        private void FixedUpdate()
        {
            if (IsServer)
            {
                UpdateClientIds();
            }

            if (!IsOwner || _cameraRig == null || !_followCameraRig)
            {
                return;
            }

            transform.SetPositionAndRotation(_cameraRig.position, _cameraRig.rotation);
        }

        /// <inheritdoc />
        public override void OnDestroy()
        {
            base.OnDestroy();
            _metaId.Dispose();
            _characterId.Dispose();
        }

        /// <inheritdoc />
        public override void OnNetworkSpawn()
        {
            _characterId.OnValueChanged += OnCharacterIdChanged;
            _clientIds.OnListChanged += OnClientIdsChanged;
            OnCharacterIdChanged(0, _characterId.Value);
            base.OnNetworkSpawn();
        }

        /// <inheritdoc />
        public override void OnNetworkDespawn()
        {
            _characterId.OnValueChanged -= OnCharacterIdChanged;
            _clientIds.OnListChanged -= OnClientIdsChanged;
            base.OnNetworkDespawn();
        }

        /// <inheritdoc cref="INetworkPoseRetargeterBehaviour.ReceiveStreamData"/>
        public void ReceiveStreamData(ulong clientId, bool isReliable, NativeArray<byte> bytes)
        {
            if (!IsSpawned)
            {
                return;
            }
            var target = RpcTarget.Single(clientId, RpcTargetUse.Temp);
            if (isReliable)
            {
                ReceiveStreamDataRpc(bytes, target);
            }
            else
            {
                ReceiveStreamDataUnreliableRpc(bytes, target);
            }
        }

        /// <inheritdoc cref="INetworkPoseRetargeterBehaviour.ReceiveStreamAck"/>
        public void ReceiveStreamAck(ulong clientId, int ack)
        {
            ReceiveStreamAckUnreliableRpc(clientId, ack);
        }

        private void OnCharacterIdChanged(int previousValue, int newValue)
        {
            if (newValue == 0)
            {
                return;
            }

            _characterId.OnValueChanged -= OnCharacterIdChanged;
            _poseRetargeter.Setup();
        }

        private void OnClientIdsChanged(NetworkListEvent<ulong> changeEvent)
        {
            var index = 0;
            _localClientIds = new ulong[_clientIds.Count];
            foreach (var id in _clientIds)
            {
                _localClientIds[index++] = id;
            }
        }

        private void UpdateClientIds()
        {
            var numberOfClients = NetworkManager.ConnectedClients.Count;
            if (_localClientIds.Length == numberOfClients)
            {
                return;
            }

            _clientIds.Clear();
            foreach (var clientKey in NetworkManager.ConnectedClients.Keys)
            {
                _clientIds.Add(clientKey);
            }
        }

        [Rpc(SendTo.SpecifiedInParams, Delivery = RpcDelivery.Reliable)]
        private void ReceiveStreamDataRpc(NativeArray<byte> bytes, RpcParams rpcParams)
        {
            if (_poseRetargeter == null)
            {
                return;
            }
            _poseRetargeter.ReceiveData(bytes);
        }

        [Rpc(SendTo.SpecifiedInParams, Delivery = RpcDelivery.Unreliable)]
        private void ReceiveStreamDataUnreliableRpc(NativeArray<byte> bytes, RpcParams rpcParams)
        {
            if (_poseRetargeter == null)
            {
                return;
            }
            _poseRetargeter.ReceiveData(bytes);
        }

        [Rpc(SendTo.Everyone, Delivery = RpcDelivery.Unreliable)]
        private void ReceiveStreamAckUnreliableRpc(ulong sender, int ack, RpcParams rpcParams = default)
        {
            if (_poseRetargeter == null)
            {
                return;
            }
            _poseRetargeter.ReceiveAck(sender, ack);
        }
    }
}
