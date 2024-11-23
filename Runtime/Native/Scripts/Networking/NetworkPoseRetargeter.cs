// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using static Meta.XR.Movement.NativeUtilityPlugin;
using static OVRSkeleton;

namespace Meta.XR.Movement.Networking
{
    /// <summary>
    /// The network pose retargeter implements the network pose retargeter interface,
    /// and is the main component for the logic that handles character networking.
    /// This component is agnostic of any networking framework, and uses configuration
    /// data for the APIs to send/receive data.
    /// </summary>
    [DefaultExecutionOrder(500)]
    public class NetworkPoseRetargeter : MonoBehaviour, INetworkPoseRetargeter
    {
        /// <summary>
        /// The pose behaviour for the network pose retargeter.
        /// </summary>
        public INetworkPoseRetargeterBehaviour PoseBehaviour => _poseBehaviour;

        /// <summary>
        /// The ownership set for the network pose retargeter.
        /// </summary>
        public NetworkPoseRetargeterConfig.Ownership Owner => _networkConfig.Owner;

        /// <summary>
        /// The network config used by this network pose retargeter.
        /// </summary>
        public NetworkPoseRetargeterConfig NetworkConfig => _networkConfig;

        /// <summary>
        /// True if received data should be applied to the character.
        /// </summary>
        public bool ApplyData
        {
            get => _applyData;
            set => _applyData = value;
        }

        /// <summary>
        /// The network config used by this network pose retargeter.
        /// </summary>
        [SerializeField]
        private NetworkPoseRetargeterConfig _networkConfig;

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
        private bool _shouldSendData => _shouldSyncData || _elapsedSendTime >= _networkConfig.IntervalToSendData;

        private bool _shouldSyncData =>
            _networkConfig.UseSyncInterval && _elapsedSyncTime >= _networkConfig.IntervalToSyncData;

        // Runtime data.
        private INetworkPoseRetargeterBehaviour _poseBehaviour;
        private GameObject _character;

        // Dictionary to pair clients with last received ack.
        private readonly Dictionary<ulong, int> _clientsLastAck = new();

        // Native data.
        private UInt64 _handle;

        // Client data.
        private NativeArray<byte>[] _streamedData;
        private NativeArray<SerializedJointPose> _bodyPose;
        private NativeArray<SerializedShapePose> _facePose;
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
            _poseBehaviour = GetComponent<INetworkPoseRetargeterBehaviour>();
        }

        protected void Start()
        {
            Assert.IsNotNull(_poseBehaviour);
            if (_networkConfig == null)
            {
                enabled = false;
            }
        }

        protected void Update()
        {
            if (!_initialized || _poseBehaviour.HasInputAuthority)
            {
                return;
            }

            TryReceiveData(_poseBehaviour.RenderTime);
        }

        protected void LateUpdate()
        {
            if (!_initialized || !_poseBehaviour.HasInputAuthority)
            {
                return;
            }

            TrySendData(_poseBehaviour.NetworkTime);
        }

        protected void OnDestroy()
        {
            StopPoseRetargeter();
            DisposeNativeArrays();
        }

        protected void OnValidate()
        {
            UpdateSerializationSettings();
        }
        #endregion

        #region INetworkPoseRetargeter

        /// <summary>
        /// Implementation of <see cref="INetworkPoseRetargeter.Setup"/>. Instantiate the character
        /// optionally here, then assign the network config and start the native retargeter instance.
        /// </summary>
        /// <param name="instantiateCharacter">True if the character prefab should be instantiated.</param>
        public void Setup(bool instantiateCharacter = true)
        {
            if (instantiateCharacter)
            {
                Assert.IsNotNull(_poseBehaviour.CharacterPrefab,
                    "The Character ID to get the prefab is out of range!");
                InstantiateCharacter();
            }
            else
            {
                _character = gameObject;
                if (_networkConfig == null)
                {
                    _networkConfig = _character.GetComponentInChildren<NetworkPoseRetargeterConfig>();
                }
            }

            StartPoseRetargeter();
            enabled = true;
        }

        /// <summary>
        /// Implementation of <see cref="INetworkPoseRetargeter.SendData"/>. Iterates through the array of
        /// connected clients and sends data if it isn't the host client.
        /// </summary>
        /// <param name="networkTime">The current network time.</param>
        public void SendData(float networkTime)
        {
            var localClientId = _poseBehaviour.LocalClientId;
            foreach (var clientId in _poseBehaviour.ClientIds)
            {
                if (clientId == localClientId)
                {
                    continue;
                }

                var lastAck = _clientsLastAck.GetValueOrDefault(clientId, -1);

                SerializeData(lastAck, networkTime);
                _poseBehaviour.ReceiveStreamData(clientId, _shouldSyncData, _serializedData);
            }

            ResetSendTimers();
        }

        /// <summary>
        /// Implementation of <see cref="INetworkPoseRetargeter.ReceiveData"/>. Adds any received data
        /// to the buffer of streamed data to be serialized and interpolated.
        /// </summary>
        /// <param name="data"></param>
        public void ReceiveData(NativeArray<byte> data)
        {
            if (_networkConfig == null)
            {
                return;
            }
            _streamedData ??= new NativeArray<byte>[_networkConfig.MaxBufferSize];
            if (_currentStreamIndex == _streamedData.Length)
            {
                _currentStreamIndex--;
            }

            if (_streamedData[_currentStreamIndex].IsCreated)
            {
                _streamedData[_currentStreamIndex].Dispose();
            }

            _streamedData[_currentStreamIndex] =
                new NativeArray<byte>(data.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            data.CopyTo(_streamedData[_currentStreamIndex]);
            _currentStreamIndex++;
            _dataReadCount++;

            if (_dataReadCount >= _spawnDelay / _networkConfig.IntervalToSendData)
            {
                _networkConfig.ToggleObjects(true);
            }
        }

        /// <summary>
        /// Implementation of <see cref="INetworkPoseRetargeter.SendAck"/>. Calls
        /// <see cref="INetworkPoseRetargeterBehaviour.ReceiveStreamAck"/> with the
        /// correct client id on this behaviour.
        /// </summary>
        /// <param name="ack">The acknowledgement packet number.</param>
        public void SendAck(int ack)
        {
            _poseBehaviour.ReceiveStreamAck(_poseBehaviour.LocalClientId, ack);
        }

        /// <summary>
        /// Implementation of <see cref="INetworkPoseRetargeter.ReceiveAck"/>. Stores the last received
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
        /// <param name="renderTime">The current render time.</param>
        public void TryReceiveData(float renderTime)
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
                _networkConfig.ApplyBodyPose(_bodyPose);
            }

            if (ReadFaceData(renderTime))
            {
                _networkConfig.ApplyFacePose(_facePose);
            }
        }

        /// <summary>
        /// Tries to send data if the time between sends is met.
        /// </summary>
        /// <param name="networkTime">The current network time.</param>
        public void TrySendData(float networkTime)
        {
            _elapsedSendTime += _poseBehaviour.DeltaTime;
            _elapsedSyncTime += _poseBehaviour.DeltaTime;

            if (_shouldSendData)
            {
                SendData(networkTime);
            }
        }

        /// <summary>
        /// Restart the native pose retargeter.
        /// </summary>
        public void RestartPoseRetargeter()
        {
            StopPoseRetargeter();
            StartPoseRetargeter();
        }

        /// <summary>
        /// Start the native pose retargeter and initialize the streamed body/face pose arrays.
        /// </summary>
        public void StartPoseRetargeter()
        {
            if (!CreateOrUpdateHandle(_networkConfig.Config, out _handle))
            {
                Debug.LogError("Failed to create a retargeting handle!");
                enabled = false;
                return;
            }

            _createdHandle = true;
            _dataIsValid = false;
            UpdateSerializationSettings();
            if (!_bodyPose.IsCreated)
            {
                _bodyPose = new NativeArray<SerializedJointPose>(
                    _networkConfig.NumberOfJoints, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            }
            if (!_facePose.IsCreated)
            {
                _facePose = new NativeArray<SerializedShapePose>(
                    _networkConfig.NumberOfShapes, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            }
        }

        /// <summary>
        /// Stop the native pose retargeter.
        /// </summary>
        public void StopPoseRetargeter()
        {
            if (!_createdHandle)
            {
                return;
            }
            if (!DestroyHandle(_handle))
            {
                Debug.LogError($"Failed to destroy retargeting handle {_handle}!");
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
            if (_networkConfig != null && _networkConfig.Owner == NetworkPoseRetargeterConfig.Ownership.Host)
            {
                _networkConfig.UpdateSerializationSettings(_handle);
            }
        }

        private void InstantiateCharacter()
        {
            if (_character != null)
            {
                Destroy(_character);
            }

            var prefab = _poseBehaviour.CharacterPrefab;
            var go = Instantiate(prefab, transform, false);
            _character = go;
            if (_networkConfig == null)
            {
                _networkConfig = _character.GetComponentInChildren<NetworkPoseRetargeterConfig>();
            }

            if (_poseBehaviour.HasInputAuthority)
            {
                _networkConfig.Owner = NetworkPoseRetargeterConfig.Ownership.Host;
                gameObject.name = "LocalCharacter";
                ToggleComponents(true);
                _networkConfig.ToggleObjects(true);
            }
            else
            {
                _networkConfig.Owner = NetworkPoseRetargeterConfig.Ownership.Client;
                gameObject.name = "RemoteCharacter";
                ToggleComponents(false);
            }
        }

        private void SerializeData(int lastAck, float networkTime)
        {
            if (_shouldSyncData || !_networkConfig.UseDeltaCompression)
            {
                lastAck = -1;
            }

            var bodyPose = _networkConfig.GetCurrentBodyPose();
            var facePose = _networkConfig.GetCurrentFacePose();
            var bodyIndicesToSerialize =
                lastAck == -1 ? _networkConfig.BodyIndicesToSync : _networkConfig.BodyIndicesToSend;
            var faceIndicesToSerialize = _networkConfig.FaceIndicesToSync;

            _dataIsValid = SerializeSkeletonAndFace(_handle,
                    networkTime,
                    bodyPose, facePose, lastAck,
                    bodyIndicesToSerialize, faceIndicesToSerialize,
                    ref _serializedData);
        }

        private void DeserializeData()
        {
            var data = _streamedData[0];
            if (!DeserializeSkeletonAndFace(
                    _handle, data, out var timestamp,
                    out var compressionType, out var ack,
                    ref _bodyPose, ref _facePose))
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

            if (!_networkConfig.UseInterpolation)
            {
                return true;
            }

            bool lerpedBody = GetInterpolatedSkeleton(_handle, ref _bodyPose, renderTime);

            return lerpedBody;
        }

        private bool ReadFaceData(float renderTime)
        {
            if (!_dataIsValid)
            {
                return false;
            }

            if (!_networkConfig.UseInterpolation)
            {
                return true;
            }
            bool lerpedFace = GetInterpolatedFace(_handle, ref _facePose, renderTime);
            return lerpedFace;
        }

        private void ResetSendTimers()
        {
            if (_shouldSyncData)
            {
                _elapsedSyncTime -= _networkConfig.IntervalToSyncData;
                _elapsedSendTime = 0; // Reset send time on sync.
            }
            else if (_shouldSendData)
            {
                _elapsedSendTime -= _networkConfig.IntervalToSendData;
            }
        }

        private void ToggleComponents(bool isEnabled)
        {
            var components = new Behaviour[]
            {
                _character.GetComponentInChildren<Animator>(),
                _character.GetComponentInChildren<OVRBody>(),
                _character.GetComponentInChildren<OVRSkeleton>()
            };
            foreach (var component in components)
            {
                if (component != null)
                {
                    component.enabled = isEnabled;
                }
            }
        }
    }
}
