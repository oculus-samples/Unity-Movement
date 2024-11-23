// Copyright (c) Meta Platforms, Inc. and affiliates.

using Unity.Collections;

namespace Meta.XR.Movement.Networking
{
    /// <summary>
    /// The network pose retargeter interface.
    /// </summary>
    public interface INetworkPoseRetargeter
    {
        /// <summary>
        /// The pose behaviour for the pose retargeter.
        /// </summary>
        public INetworkPoseRetargeterBehaviour PoseBehaviour { get; }

        /// <summary>
        /// The setup function for the pose retargeter.
        /// This should instantiate the character if needed, and setup all requirements to be ready
        /// for networking.
        /// </summary>
        /// <param name="instantiateCharacter">True if the character should be instantiated.</param>
        public void Setup(bool instantiateCharacter = true);

        /// <summary>
        /// The send function for the pose retargeter, which should use the serialization APIs
        /// to send the data using the networking framework.
        /// </summary>
        /// <param name="networkTime">The network time.</param>
        public void SendData(float networkTime);

        /// <summary>
        /// The receive function for the pose retargeter, which should use the serialization APIs
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
