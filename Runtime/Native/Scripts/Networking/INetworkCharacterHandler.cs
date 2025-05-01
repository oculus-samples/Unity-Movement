// Copyright (c) Meta Platforms, Inc. and affiliates.

using Unity.Collections;
using UnityEngine;

namespace Meta.XR.Movement.Networking
{
    /// <summary>
    /// The network character handler interface.
    /// </summary>
    public interface INetworkCharacterHandler
    {
        /// <summary>
        /// The network behaviour for the character retargeter.
        /// </summary>
        public INetworkCharacterBehaviour CharacterBehaviour { get; }

        /// <summary>
        /// The spawned character.
        /// </summary>
        public GameObject Character { get; }

        /// <summary>
        /// The setup function for the character retargeter.
        /// This should instantiate the character if needed, and setup all requirements to be ready
        /// for networking.
        /// </summary>
        /// <param name="instantiateCharacter">True if the character should be instantiated.</param>
        public void Setup(bool instantiateCharacter = true);

        /// <summary>
        /// The send function for the character retargeter, which should use the serialization APIs
        /// to send the data using the networking framework.
        /// </summary>
        /// <param name="networkTime">The network time.</param>
        public void SendData(float networkTime);

        /// <summary>
        /// The receive function for the character retargeter, which should use the serialization APIs
        /// to deserialize the received data.
        /// </summary>
        /// <param name="bytes">The received data.</param>
        public void ReceiveData(NativeArray<byte> bytes);

        /// <summary>
        /// Send the acknowledgement packet.
        /// </summary>
        /// <param name="ack">The acknowledgment packet number.</param>
        public void SendAck(int ack);

        /// <summary>
        /// Acknowledge receiving the acknowledgement packet.
        /// </summary>
        /// <param name="id">The id of the user that received the packet.</param>
        /// <param name="ack">The acknowledgment packet number.</param>
        public void ReceiveAck(ulong id, int ack);
    }
}
