// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using static Meta.XR.Movement.MSDKUtility;
using static Unity.Collections.Allocator;
using static Unity.Collections.NativeArrayOptions;

namespace Meta.XR.Movement.Networking
{
    /// <summary>
    /// The network character handler implements the network character handler interface,
    /// and is the main component for the logic that handles character networking.
    /// This component is agnostic of any networking framework, and uses configuration
    /// data for the APIs to send/receive data.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class NetworkCharacterHandler : MonoBehaviour, INetworkCharacterHandler
    {
        /// <summary>
        /// The behaviour for the network character handler.
        /// </summary>
        public INetworkCharacterBehaviour CharacterBehaviour => _characterBehaviour;

        /// <summary>
        /// The spawned character.
        /// </summary>
        public GameObject Character => _character;

        /// <summary>
        /// The ownership set for the network character handler.
        /// </summary>
        public NetworkCharacterRetargeter.Ownership Owner => _networkCharacterRetargeter.Owner;

        /// <summary>
        /// The network retargeter used by this network character handler.
        /// </summary>
        public NetworkCharacterRetargeter NetworkCharacterRetargeter => _networkCharacterRetargeter;

        /// <summary>
        /// True if received data should be applied to the character.
        /// </summary>
        public bool ApplyData
        {
            get => _applyData;
            set => _applyData = value;
        }

        /// <summary>
        /// Indicates how many bytes were received last.
        /// </summary>
        public Action<int> BytesReceived;

        /// <summary>
        /// The network config used by this network character handler.
        /// </summary>
        [SerializeField]
        private NetworkCharacterRetargeter _networkCharacterRetargeter;

        /// <summary>
        /// True if received data should be applied to the character.
        /// </summary>
        [SerializeField]
        private bool _applyData = true;

        /// <summary>
        /// The delay before the character should be shown and data sent/received.
        /// </summary>
        [SerializeField]
        private float _spawnDelay = 0.5f;

        // Private properties.
        private bool _initialized => _character != null;

        private bool _shouldSendData =>
            _shouldSyncData || _elapsedSendTime >= _networkCharacterRetargeter.IntervalToSendData;

        private bool _shouldSyncData =>
            _networkCharacterRetargeter.UseSyncInterval &&
            _elapsedSyncTime >= _networkCharacterRetargeter.IntervalToSyncData;

        // Runtime data.
        private INetworkCharacterBehaviour _characterBehaviour;
        private GameObject _character;

        // Dictionary to pair clients with last received ack.
        private readonly Dictionary<ulong, int> _clientsLastAck = new();

        // Client data.
        private NativeArray<byte>[] _streamedData;
        private NativeArray<NativeTransform> _bodyPose;
        private NativeArray<float> _facePose;
        private bool _dataIsValid;
        private int _currentStreamIndex;

        // Host data.
        private NativeArray<byte> _serializedData;
        private float _elapsedSendTime;
        private float _elapsedSyncTime;

        private bool _createdHandle;
        private int _dataReadCount;

        #region Unity Functions

        protected void Awake()
        {
            _characterBehaviour = GetComponent<INetworkCharacterBehaviour>();
            if (_networkCharacterRetargeter == null)
            {
                _networkCharacterRetargeter = GetComponentInChildren<NetworkCharacterRetargeter>(true);
            }
        }

        protected void Start()
        {
            Assert.IsNotNull(_characterBehaviour);
        }

        protected void Update()
        {
            if (!_initialized || _characterBehaviour.HasInputAuthority)
            {
                return;
            }

            TryReceiveData(_characterBehaviour.NetworkTime, _characterBehaviour.RenderTime);
        }

        protected void LateUpdate()
        {
            if (!_initialized || !_characterBehaviour.HasInputAuthority)
            {
                return;
            }

            TrySendData(_characterBehaviour.NetworkTime);
        }

        protected void OnDestroy()
        {
            _clientsLastAck.Clear();
            DisposeNativeArrays();
        }

        protected void OnValidate()
        {
            UpdateSerializationSettings();
        }

        #endregion

        #region INetworkCharacterRetargeter

        /// <summary>
        /// Implementation of <see cref="INetworkCharacterHandler.Setup"/>. Instantiate the character
        /// optionally here, then assign the network config and start the native retargeter instance.
        /// </summary>
        /// <param name="instantiateCharacter">True if the character prefab should be instantiated.</param>
        public void Setup(bool instantiateCharacter = true)
        {
            if (instantiateCharacter)
            {
                Assert.IsNotNull(_characterBehaviour.CharacterPrefab,
                    "The Character ID to get the prefab is out of range!");
                InstantiateCharacter();
            }
            else
            {
                _character = gameObject;
                if (_networkCharacterRetargeter == null)
                {
                    _networkCharacterRetargeter = _character.GetComponentInChildren<NetworkCharacterRetargeter>();
                }
            }

            // Setup the native data arrays for networking.
            _networkCharacterRetargeter.UpdateSerializationSettings();
            if (!_bodyPose.IsCreated)
            {
                _bodyPose = new NativeArray<NativeTransform>(
                    _networkCharacterRetargeter.NumberOfJoints, Persistent, UninitializedMemory);
            }

            if (!_facePose.IsCreated)
            {
                _facePose = new NativeArray<float>(
                    _networkCharacterRetargeter.NumberOfShapes, Persistent, UninitializedMemory);
            }

            enabled = true;
        }

        /// <summary>
        /// Implementation of <see cref="INetworkCharacterHandler.SendData"/>. Iterates through the array of
        /// connected clients and sends data if it isn't the host client.
        /// </summary>
        /// <param name="networkTime">The current network time.</param>
        public void SendData(float networkTime)
        {
            var localClientId = _characterBehaviour.LocalClientId;
            foreach (var clientId in _characterBehaviour.ClientIds)
            {
                if (clientId == localClientId)
                {
                    continue;
                }
                // Don't serialize if retargeter doesn't have useful data.
                if (!_networkCharacterRetargeter.RetargeterValid)
                {
                    continue;
                }

                var lastAck = _clientsLastAck.GetValueOrDefault(clientId, -1);

                SerializeData(lastAck, networkTime);
                _characterBehaviour.ReceiveStreamData(clientId, _shouldSyncData, _serializedData);
            }

            ResetSendTimers();
        }

        /// <summary>
        /// Implementation of <see cref="INetworkCharacterHandler.ReceiveData"/>. Adds any received data
        /// to the buffer of streamed data to be 1serialized and interpolated.
        /// </summary>
        /// <param name="data"></param>
        public void ReceiveData(NativeArray<byte> data)
        {
            if (_networkCharacterRetargeter == null)
            {
                return;
            }

            _streamedData ??= new NativeArray<byte>[_networkCharacterRetargeter.MaxBufferSize];
            if (_currentStreamIndex == _streamedData.Length)
            {
                _currentStreamIndex--;
            }

            if (_streamedData[_currentStreamIndex].IsCreated)
            {
                _streamedData[_currentStreamIndex].Dispose();
            }

            _streamedData[_currentStreamIndex] =
                new NativeArray<byte>(data.Length, Persistent, UninitializedMemory);
            data.CopyTo(_streamedData[_currentStreamIndex]);
            _currentStreamIndex++;
            _dataReadCount++;

            BytesReceived?.Invoke(data.Length);

            if (_dataReadCount >= _spawnDelay / _networkCharacterRetargeter.IntervalToSendData)
            {
                _networkCharacterRetargeter.ToggleObjects(true);
            }
        }

        /// <summary>
        /// Implementation of <see cref="INetworkCharacterHandler.SendAck"/>. Calls
        /// <see cref="INetworkCharacterBehaviour.ReceiveStreamAck"/> with the
        /// correct client id on this behaviour.
        /// </summary>
        /// <param name="ack">The acknowledgement packet number.</param>
        public void SendAck(int ack)
        {
            _characterBehaviour.ReceiveStreamAck(_characterBehaviour.LocalClientId, ack);
        }

        /// <summary>
        /// Implementation of <see cref="INetworkCharacterHandler.ReceiveAck"/>. Stores the last received
        /// acknowledgement packet number in a dictionary of client ids.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ack"></param>
        public void ReceiveAck(ulong id, int ack)
        {
            _clientsLastAck[id] = ack;
        }

        #endregion

        /// <summary>
        /// Tries to deserialize received streamed data and apply the body pose to the character.
        /// </summary>
        /// <param name="networkTime">The current network time.</param>
        /// <param name="renderTime">The current render time.</param>
        public void TryReceiveData(float networkTime, float renderTime)
        {
            if (_currentStreamIndex > 0)
            {
                DeserializeData();
                _currentStreamIndex--;
            }

            if (!ApplyData)
            {
                return;
            }

            if (ReadBodyData(renderTime))
            {
                _networkCharacterRetargeter.ApplyBodyPose(_bodyPose, Retargeting.JointType.NoWorldSpace);
                _networkCharacterRetargeter.SetDebugPose(_bodyPose);
            }

            if (ReadFaceData(renderTime))
            {
                _networkCharacterRetargeter.ApplyFacePose(_facePose);
            }
        }

        /// <summary>
        /// Tries to send data if the time between sends is met.
        /// </summary>
        /// <param name="networkTime">The current network time.</param>
        public void TrySendData(float networkTime)
        {
            _elapsedSendTime += _characterBehaviour.DeltaTime;
            _elapsedSyncTime += _characterBehaviour.DeltaTime;

            if (_shouldSendData)
            {
                SendData(networkTime);
            }
        }

        private void DisposeNativeArrays()
        {
            if (_streamedData != null)
            {
                foreach (var data in _streamedData)
                {
                    if (data.IsCreated)
                    {
                        data.Dispose();
                    }
                }
            }

            if (_bodyPose.IsCreated)
            {
                _bodyPose.Dispose();
            }

            if (_facePose.IsCreated)
            {
                _facePose.Dispose();
            }
        }

        private void UpdateSerializationSettings()
        {
            if (_networkCharacterRetargeter != null &&
                _networkCharacterRetargeter.Owner == NetworkCharacterRetargeter.Ownership.Host)
            {
                _networkCharacterRetargeter.UpdateSerializationSettings();
            }
        }

        private void InstantiateCharacter()
        {
            if (_character != null)
            {
                Destroy(_character);
            }

            var prefab = _characterBehaviour.CharacterPrefab;
            var go = Instantiate(prefab, transform, false);
            _character = go;
            if (_networkCharacterRetargeter == null)
            {
                _networkCharacterRetargeter = _character.GetComponentInChildren<NetworkCharacterRetargeter>();
            }

            if (_characterBehaviour.HasInputAuthority)
            {
                _networkCharacterRetargeter.Owner = NetworkCharacterRetargeter.Ownership.Host;
                gameObject.name = "LocalCharacter";
                _networkCharacterRetargeter.ToggleObjects(true);
            }
            else
            {
                _networkCharacterRetargeter.Owner = NetworkCharacterRetargeter.Ownership.Client;
                gameObject.name = "RemoteCharacter";
            }
        }

        private void SerializeData(int lastAck, float networkTime)
        {
            if (_shouldSyncData || !_networkCharacterRetargeter.UseDeltaCompression)
            {
                lastAck = -1;
            }
            var bodyPose = _networkCharacterRetargeter.GetCurrentBodyPose(Retargeting.JointType.NoWorldSpace);
            var facePose = _networkCharacterRetargeter.GetCurrentFacePose();
            var bodyIndicesToSerialize =
                lastAck == -1
                    ? _networkCharacterRetargeter.BodyIndicesToSync
                    : _networkCharacterRetargeter.BodyIndicesToSend;
            var faceIndicesToSerialize = _networkCharacterRetargeter.FaceIndicesToSync;
            _dataIsValid = SerializeSkeletonAndFace(
                _networkCharacterRetargeter.RetargetingHandle,
                networkTime,
                bodyPose,
                facePose,
                lastAck,
                bodyIndicesToSerialize,
                faceIndicesToSerialize,
                ref _serializedData);
            bodyPose.Dispose();
        }

        private void DeserializeData()
        {
            var data = _streamedData[0];
            if (!DeserializeSkeletonAndFace(
                    _networkCharacterRetargeter.RetargetingHandle,
                    data,
                    out var timestamp,
                    out var receivedCompressionType,
                    out var ack,
                    ref _bodyPose,
                    ref _facePose))
            {
                Debug.LogError("Data received is invalid!");
                _dataIsValid = false;
                return;
            }

            _dataIsValid = true;
            SendAck(ack);
        }

        private bool ReadBodyData(float renderTime)
        {
            if (!_dataIsValid)
            {
                return false;
            }

            if (!_networkCharacterRetargeter.UseInterpolation)
            {
                return true;
            }

            return GetInterpolatedSkeleton(
                _networkCharacterRetargeter.RetargetingHandle,
                SkeletonType.TargetSkeleton,
                ref _bodyPose,
                renderTime);
        }

        private bool ReadFaceData(float renderTime)
        {
            if (!_dataIsValid)
            {
                return false;
            }

            if (!_networkCharacterRetargeter.UseInterpolation)
            {
                return true;
            }

            return GetInterpolatedFace(
                _networkCharacterRetargeter.RetargetingHandle,
                SkeletonType.TargetSkeleton,
                ref _facePose,
                renderTime);
        }

        private void ResetSendTimers()
        {
            if (_shouldSyncData)
            {
                _elapsedSyncTime -= _networkCharacterRetargeter.IntervalToSyncData;
                _elapsedSendTime = 0; // Reset send time on sync.
            }
            else if (_shouldSendData)
            {
                _elapsedSendTime -= _networkCharacterRetargeter.IntervalToSendData;
            }
        }
    }
}
