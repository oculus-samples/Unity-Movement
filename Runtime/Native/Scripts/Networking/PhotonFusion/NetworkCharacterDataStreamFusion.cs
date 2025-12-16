// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

#if FUSION2
using System;
using Fusion;
using UnityEngine;

namespace Meta.XR.Movement.Networking.Fusion
{
    /// <summary>
    /// A Fusion-based network data stream that stores a variable sized byte array representing character data
    /// Uses Fusion's NetworkArray to store data.
    /// </summary>
    public class NetworkCharacterDataStreamFusion
    {
        public int DataLength { get; private set; }
        public event Action OnDataChanged;

        private const int _characterDataStreamMaxCapacity = 1024;
        private byte[] _buffer;

        public void SetData(byte[] data, NetworkArray<byte> networkArray, ref int networkDataLength)
        {
            if (data == null || data.Length == 0)
            {
                networkDataLength = 0;
                DataLength = 0;
                return;
            }

            if (data.Length > _characterDataStreamMaxCapacity)
            {
                Debug.LogError($"[NetworkCharacterDataStreamFusion] Cannot send stream data of length {data.Length} " +
                              $"greater than max capacity of {_characterDataStreamMaxCapacity}");
                return;
            }

            networkDataLength = data.Length;
            DataLength = data.Length;
            networkArray.CopyFrom(data, 0, data.Length);
        }

        public byte[] GetData(NetworkArray<byte> networkArray, int networkDataLength)
        {
            if (networkDataLength == 0)
            {
                return null;
            }

            if (_buffer == null || _buffer.Length != networkDataLength)
            {
                _buffer = new byte[networkDataLength];
            }

            networkArray.CopyTo(_buffer);
            DataLength = networkDataLength;

            OnDataChanged?.Invoke();

            return _buffer;
        }
    }
}
#endif
