// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using TMPro;
using UnityEngine;
using Unity.Collections;

namespace Meta.XR.Movement.Networking.Local
{
    /// <summary>
    /// An example implementation of <see cref="INetworkCharacterBehaviour"/> that demonstrates how the
    /// streamed data that would be networked is sent/received locally through a buffer.
    /// </summary>
    public class NetworkCharacterBehaviourLocal : MonoBehaviour, INetworkCharacterBehaviour
    {
        /// <inheritdoc cref="INetworkCharacterBehaviour.HasInputAuthority"/>
        public bool HasInputAuthority => _self.Owner == NetworkCharacterRetargeter.Ownership.Host;

        /// <inheritdoc cref="INetworkCharacterBehaviour.CharacterPrefab"/>
        public GameObject CharacterPrefab => gameObject;

        /// <inheritdoc cref="INetworkCharacterBehaviour.MetaId"/>
        public ulong MetaId => 0;

        /// <inheritdoc cref="INetworkCharacterBehaviour.CharacterId"/>
        public int CharacterId => 0;

        /// <inheritdoc cref="INetworkCharacterBehaviour.NetworkTime"/>
        public float NetworkTime => Time.time;

        /// <inheritdoc cref="INetworkCharacterBehaviour.RenderTime"/>
        public float RenderTime => Time.time;

        /// <inheritdoc cref="INetworkCharacterBehaviour.DeltaTime"/>
        public float DeltaTime => Time.deltaTime;

        /// <inheritdoc cref="INetworkCharacterBehaviour.ClientIds"/>
        public ulong[] ClientIds { get; private set; }

        /// <inheritdoc cref="INetworkCharacterBehaviour.LocalClientId"/>
        public ulong LocalClientId { get; private set; }

        /// <summary>
        /// The network character handler that it captures data from.
        /// </summary>
        [SerializeField]
        private NetworkCharacterHandler _self;

        /// <summary>
        /// The target network character handler that it's sending/receiving data to.
        /// </summary>
        [SerializeField]
        private NetworkCharacterHandler _target;

        /// <summary>
        /// Optional text field to display debug data.
        /// </summary>
        [SerializeField]
        private TMP_Text _debugDataText;

        private float _receivedScale;

        // Debug data.
        private float _debugDataTimer;
        private int _debugExpectedSizeInBytes;
        private const float _bytesToKilobits = 0.008f;

        private void Awake()
        {
            _self = GetComponent<NetworkCharacterHandler>();
            LocalClientId = BitConverter.ToUInt64(Guid.NewGuid().ToByteArray());
        }

        private void Start()
        {
            if (_self.Owner == NetworkCharacterRetargeter.Ownership.Host)
            {
                ClientIds = new[]
                {
                    _target.CharacterBehaviour.LocalClientId
                };
            }
            _self.Setup(false);
        }

        /// <summary>
        /// Optionally display debug information.
        /// </summary>
        private void Update()
        {
            if (_debugDataText == null)
            {
                return;
            }

            _debugDataTimer += Time.deltaTime;
            if (_debugDataTimer < 1.0f)
            {
                return;
            }

            var kilobits = _debugExpectedSizeInBytes * _bytesToKilobits;
            _debugDataText.text = $"bandwidth: {kilobits:F3} kb/s";
            _debugExpectedSizeInBytes = 0;
            _debugDataTimer -= 1.0f;
        }


        /// <inheritdoc cref="INetworkCharacterBehaviour.ReceiveStreamData"/>
        public void ReceiveStreamData(ulong clientId, bool isReliable, NativeArray<byte> bytes)
        {
            _target.ReceiveData(bytes);
            // Custom scale implementation for transmitted data.
            var targetScale = _self.transform.localScale;
            if (Mathf.Abs(_receivedScale - targetScale.sqrMagnitude) >= 0.1f)
            {
                _receivedScale = targetScale.sqrMagnitude;
                _target.transform.localScale = targetScale;
            }
            _debugExpectedSizeInBytes += bytes.Length;
        }

        /// <inheritdoc cref="INetworkCharacterBehaviour.ReceiveStreamAck"/>
        public void ReceiveStreamAck(ulong clientId, int ack)
        {
            _target.ReceiveAck(clientId, ack);
        }
    }
}
