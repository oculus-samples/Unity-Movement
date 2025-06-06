// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using Unity.Collections;
using UnityEngine;

namespace Meta.XR.Movement.Networking
{
    /// <summary>
    /// The network retargeter behaviour interface.
    /// </summary>
    public interface INetworkCharacterBehaviour
    {
        /// <summary>
        /// True if this has the input authority.
        /// </summary>
        public bool HasInputAuthority { get; }

        /// <summary>
        /// The character prefab.
        /// </summary>
        public GameObject CharacterPrefab { get; }

        /// <summary>
        /// The MetaId that should be synced over the network. Can be 0 if user not entitled.
        /// </summary>
        public ulong MetaId { get; }

        /// <summary>
        /// The CharacterId that should be synced over the network.
        /// </summary>
        public int CharacterId { get; }

        /// <summary>
        /// The current network time.
        /// </summary>
        public float NetworkTime { get; }

        /// <summary>
        /// The current render time.
        /// </summary>
        public float RenderTime { get; }

        /// <summary>
        /// The current delta time.
        /// </summary>
        public float DeltaTime { get; }

        /// <summary>
        /// An array of client ids connected over the network in this session.
        /// </summary>
        public ulong[] ClientIds { get; }

        /// <summary>
        /// The local client id.
        /// </summary>
        public ulong LocalClientId { get; }

        /// <summary>
        /// Receive the data to be sent over the network. Should use the networking framework
        /// APIs to send this data.
        /// </summary>
        /// <param name="clientId">The client id of the sender.</param>
        /// <param name="isReliable">True if this should be a reliable RPC.</param>
        /// <param name="bytes">The serialized data to be sent.</param>
        public void ReceiveStreamData(ulong clientId, bool isReliable, NativeArray<byte> bytes);

        /// <summary>
        /// Receive the acknowledgement packet sent over the network. Should use the networking
        /// framework APIs to send this data.
        /// </summary>
        /// <param name="clientId">The client id of the sender.</param>
        /// <param name="ack">The acknowledgement packet number.</param>
        public void ReceiveStreamAck(ulong clientId, int ack);
    }
}
