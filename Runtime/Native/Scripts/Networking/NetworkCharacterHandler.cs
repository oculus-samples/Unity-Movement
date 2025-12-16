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

        // Client data - queue for streamed data (FIFO).
        private Queue<NativeArray<byte>> _streamedData;

        private NativeArray<NativeTransform> _bodyPose;
        private NativeArray<float> _facePose;
        private bool _dataIsValid;

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

                // Set ownership if not already set
                if (_networkCharacterRetargeter.Owner == NetworkCharacterRetargeter.Ownership.None)
                {
                    if (_characterBehaviour.HasInputAuthority)
                    {
                        _networkCharacterRetargeter.Owner = NetworkCharacterRetargeter.Ownership.Host;
                        gameObject.name = "LocalCharacter";
                    }
                    else
                    {
                        _networkCharacterRetargeter.Owner = NetworkCharacterRetargeter.Ownership.Client;
                        gameObject.name = "RemoteCharacter";
                    }
                }
            }

            // Validate ownership is set
            if (_networkCharacterRetargeter.Owner == NetworkCharacterRetargeter.Ownership.None)
            {
                Debug.LogError("[NetworkCharacterHandler] Setup: Ownership is still None after initialization!");
            }

            // Initialize the retargeting system before we check the handle
            // The retargeting handle must be created before we can use serialization/deserialization
            EnsureRetargetingInitialized();

            // Setup the native data arrays for networking.
            _networkCharacterRetargeter.UpdateSerializationSettings();

            var numJoints = _networkCharacterRetargeter.NumberOfJoints;
            var numShapes = _networkCharacterRetargeter.NumberOfShapes;

            if (!_bodyPose.IsCreated)
            {
                _bodyPose = new NativeArray<NativeTransform>(
                    numJoints, Persistent, UninitializedMemory);
            }

            if (!_facePose.IsCreated)
            {
                _facePose = new NativeArray<float>(
                    numShapes, Persistent, UninitializedMemory);
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
            // Check if retargeting is ready to send data.
            // We check IsValid and SkeletonRetargeter.AppliedPose directly because
            // RetargeterValid includes AppliedPose which may not be set for networking scenarios.
            if (!_networkCharacterRetargeter.IsValid ||
                !_networkCharacterRetargeter.SkeletonRetargeter.IsInitialized ||
                !_networkCharacterRetargeter.SkeletonRetargeter.AppliedPose)
            {
                return;
            }

            var localClientId = _characterBehaviour.LocalClientId;
            foreach (var clientId in _characterBehaviour.ClientIds)
            {
                if (clientId == localClientId)
                {
                    continue;
                }

                var lastAck = _clientsLastAck.GetValueOrDefault(clientId, -1);

                SerializeData(lastAck, networkTime);

                if (_serializedData.IsCreated && _serializedData.Length > 0)
                {
                    _characterBehaviour.ReceiveStreamData(clientId, false, _serializedData);
                }
            }

            ResetSendTimers();
        }

        /// <summary>
        /// Implementation of <see cref="INetworkCharacterHandler.ReceiveData"/>. Adds any received data
        /// to the queue of streamed data to be deserialized and interpolated.
        /// </summary>
        /// <param name="data"></param>
        public void ReceiveData(NativeArray<byte> data)
        {
            var maxBufferSize = _networkCharacterRetargeter.MaxBufferSize;
            _streamedData ??= new Queue<NativeArray<byte>>(maxBufferSize);

            while (_streamedData.Count >= maxBufferSize)
            {
                var discarded = _streamedData.Dequeue();
                discarded.Dispose();
            }

            var copy = new NativeArray<byte>(data.Length, Persistent, UninitializedMemory);
            data.CopyTo(copy);
            _streamedData.Enqueue(copy);
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

        /// <summary>
        /// Tries to deserialize received streamed data and apply the body pose to the character.
        /// </summary>
        /// <param name="networkTime">The current network time.</param>
        /// <param name="renderTime">The current render time.</param>
        public void TryReceiveData(float networkTime, float renderTime)
        {
            // Retargeting handle is required for deserialization and interpolation
            if (_networkCharacterRetargeter.RetargetingHandle == INVALID_HANDLE)
            {
                return;
            }

            if (_streamedData is { Count: > 0 })
            {
                DeserializeData();
            }

            if (!ApplyData)
            {
                return;
            }

            // Don't apply data until we have valid deserialized data
            if (!_dataIsValid)
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
                while (_streamedData.Count > 0)
                {
                    _streamedData.Dequeue().Dispose();
                }
                _streamedData = null;
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
            if (_networkCharacterRetargeter.RetargetingHandle == INVALID_HANDLE)
            {
                return;
            }

            if (_shouldSyncData || !_networkCharacterRetargeter.UseDeltaCompression)
            {
                lastAck = -1;
            }

            var bodyPose = _networkCharacterRetargeter.GetCurrentBodyPose(Retargeting.JointType.NoWorldSpace);
            var facePose = _networkCharacterRetargeter.GetCurrentFacePose(true);

            var bodyIndicesToSerialize = lastAck == -1
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

            if (bodyPose.IsCreated)
            {
                bodyPose.Dispose();
            }

            if (facePose.IsCreated)
            {
                facePose.Dispose();
            }
        }

        private void DeserializeData()
        {
            var data = _streamedData.Dequeue();

            if (!DeserializeSkeletonAndFace(
                    _networkCharacterRetargeter.RetargetingHandle,
                    data,
                    SERIALIZATION_VERSION_CURRENT,
                    out var timestamp,
                    out var receivedCompressionType,
                    out var ack,
                    ref _bodyPose,
                    ref _facePose))
            {
                _dataIsValid = false;
                data.Dispose();
                return;
            }

            data.Dispose();
            _networkCharacterRetargeter.DeNormalizeFaceValues(ref _facePose);
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

            if (GetInterpolatedFace(
                    _networkCharacterRetargeter.RetargetingHandle,
                    SkeletonType.TargetSkeleton,
                    ref _facePose,
                    renderTime))
            {
                _networkCharacterRetargeter.DeNormalizeFaceValues(ref _facePose);
                return true;
            }

            return false;
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

        /// <summary>
        /// Ensures the retargeting system is initialized before we attempt to use it.
        /// This must be called before any serialization/deserialization operations.
        /// </summary>
        private void EnsureRetargetingInitialized()
        {
            // If handle is already valid, nothing to do
            if (_networkCharacterRetargeter.RetargetingHandle != INVALID_HANDLE)
            {
                return;
            }

            // Validate config is available
            if (_networkCharacterRetargeter.ConfigAsset == null)
            {
                Debug.LogError("[NetworkCharacterHandler] EnsureRetargetingInitialized: ConfigAsset is null! Character retargeting will not work. Please assign a config TextAsset to the NetworkCharacterRetargeter component.");
                return;
            }

            if (string.IsNullOrEmpty(_networkCharacterRetargeter.Config))
            {
                Debug.LogError("[NetworkCharacterHandler] EnsureRetargetingInitialized: Config is empty! Character retargeting will not work.");
                return;
            }

            // Manually call Setup if it hasn't been called yet
            // This ensures the retargeting handle is created before we need it
            _networkCharacterRetargeter.Setup(_networkCharacterRetargeter.Config);

            // Verify initialization succeeded
            if (_networkCharacterRetargeter.RetargetingHandle == INVALID_HANDLE)
            {
                Debug.LogError($"[NetworkCharacterHandler] EnsureRetargetingInitialized: Setup() was called but handle is still INVALID_HANDLE! This is a critical error.");
            }
        }
    }
}
