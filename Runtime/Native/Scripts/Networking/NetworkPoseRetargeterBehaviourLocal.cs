// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using System;
using TMPro;
using UnityEngine;
using Unity.Collections;

namespace Meta.XR.Movement.Networking.Local
{
    /// <summary>
    /// An example implementation of <see cref="INetworkPoseRetargeterBehaviour"/> that demonstrates how the
    /// streamed data that would be networked is sent/received locally through a buffer.
    /// </summary>
    public class NetworkPoseRetargeterBehaviourLocal : MonoBehaviour, INetworkPoseRetargeterBehaviour
    {
        /// <inheritdoc cref="INetworkPoseRetargeterBehaviour.HasInputAuthority"/>
        public bool HasInputAuthority => _self.Owner == NetworkPoseRetargeterConfig.Ownership.Host;

        /// <inheritdoc cref="INetworkPoseRetargeterBehaviour.CharacterPrefab"/>
        public GameObject CharacterPrefab => gameObject;

        /// <inheritdoc cref="INetworkPoseRetargeterBehaviour.MetaId"/>
        public ulong MetaId => 0;

        /// <inheritdoc cref="INetworkPoseRetargeterBehaviour.CharacterId"/>
        public int CharacterId => 0;

        /// <inheritdoc cref="INetworkPoseRetargeterBehaviour.NetworkTime"/>
        public float NetworkTime => Time.time;

        /// <inheritdoc cref="INetworkPoseRetargeterBehaviour.RenderTime"/>
        public float RenderTime => Time.time;

        /// <inheritdoc cref="INetworkPoseRetargeterBehaviour.DeltaTime"/>
        public float DeltaTime => Time.deltaTime;

        /// <inheritdoc cref="INetworkPoseRetargeterBehaviour.ClientIds"/>
        public ulong[] ClientIds { get; private set; }

        /// <inheritdoc cref="INetworkPoseRetargeterBehaviour.LocalClientId"/>
        public ulong LocalClientId { get; private set; }

        /// <summary>
        /// The network pose retargeter that it belongs to.
        /// </summary>
        [SerializeField]
        private NetworkPoseRetargeter _self;

        /// <summary>
        /// The target network pose retargeter that it's sending/receiving data to.
        /// </summary>
        [SerializeField]
        private NetworkPoseRetargeter _target;

        /// <summary>
        /// Optional text field to display debug data.
        /// </summary>
        [SerializeField]
        private TMP_Text _debugDataText;

        // Debug data.
        private float _debugDataTimer;
        private int _debugExpectedSizeInBytes;
        private const float _bytesToKilobits = 0.008f;

        private void Awake()
        {
            _self = GetComponent<NetworkPoseRetargeter>();
            LocalClientId = BitConverter.ToUInt64(Guid.NewGuid().ToByteArray());
        }

        private void Start()
        {
            if (_self.Owner == NetworkPoseRetargeterConfig.Ownership.Host)
            {
                ClientIds = new[]
                {
                    _target.PoseBehaviour.LocalClientId
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


        /// <inheritdoc cref="INetworkPoseRetargeterBehaviour.ReceiveStreamData"/>
        public void ReceiveStreamData(ulong clientId, bool isReliable, NativeArray<byte> bytes)
        {
            _target.ReceiveData(bytes);
            _debugExpectedSizeInBytes += bytes.Length;
        }

        /// <inheritdoc cref="INetworkPoseRetargeterBehaviour.ReceiveStreamAck"/>
        public void ReceiveStreamAck(ulong clientId, int ack)
        {
            _target.ReceiveAck(clientId, ack);
        }
    }
}
