// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using Meta.XR.Movement.Retargeting;
using Unity.Collections;
using UnityEngine;
using static Meta.XR.Movement.MSDKUtility;

namespace Meta.XR.Movement.Networking
{
    /// <summary>
    /// The network character retargeter. This runs retargeting and contains all the necessary data
    /// to determine how the data should be sent/received when networking.
    /// </summary>
    public class NetworkCharacterRetargeter : CharacterRetargeter
    {
        /// <summary>
        /// The ownership type commonly used for networking - host/client.
        /// </summary>
        public enum Ownership
        {
            None,
            Host,
            Client
        }

        public Ownership Owner
        {
            get => _ownership;
            set => _ownership = value;
        }

        public SerializationCompressionType CompressionType
        {
            get => _compressionType;
            set
            {
                _compressionType = value;
                UpdateSerializationSettings();
            }
        }

        public float PositionThreshold
        {
            get => _positionThreshold;
            set
            {
                _positionThreshold = value;
                UpdateSerializationSettings();
            }
        }

        public float RotationAngleThreshold
        {
            get => _rotationAngleThreshold;
            set
            {
                _rotationAngleThreshold = value;
                UpdateSerializationSettings();
            }
        }

        public float ShapeThreshold
        {
            get => _shapeThreshold;
            set
            {
                _shapeThreshold = value;
                UpdateSerializationSettings();
            }
        }

        public int[] BodyIndicesToSync
        {
            get => _bodyIndicesToSync;
            set => _bodyIndicesToSync = value;
        }

        public int[] BodyIndicesToSend
        {
            get => _bodyIndicesToSend;
            set => _bodyIndicesToSend = value;
        }

        public int[] FaceIndicesToSync
        {
            get => _faceIndicesToSend;
            set => _faceIndicesToSend = value;
        }

        public float IntervalToSendData => _intervalToSendData;

        public float IntervalToSyncData => _intervalToSyncData;

        public bool UseSyncInterval => _useSyncInterval;

        public int MaxBufferSize => _maxBufferSize;

        public bool UseDeltaCompression => _useDeltaCompression;

        public bool UseInterpolation
        {
            get => _useInterpolation;
            set => _useInterpolation = value;
        }

        /// <summary>
        /// The ownership type of this instance.
        /// </summary>
        [SerializeField]
        private Ownership _ownership = Ownership.None;

        /// <summary>
        /// The compression type that should be used when sending/receiving data.
        /// </summary>
        [SerializeField]
        private SerializationCompressionType _compressionType = SerializationCompressionType.High;

        /// <summary>
        /// True if delta compression should be applied and used.
        /// </summary>
        [SerializeField]
        private bool _useDeltaCompression = true;

        /// <summary>
        /// True if all data should be synced at a certain interval, even when using delta compression.
        /// </summary>
        [SerializeField]
        private bool _useSyncInterval = true;

        /// <summary>
        /// The interval to send data at.
        /// </summary>
        [SerializeField]
        private float _intervalToSendData = 0.083333f;

        /// <summary>
        /// The interval to sync data at.
        /// </summary>
        [SerializeField]
        private float _intervalToSyncData = 1.0f;

        /// <summary>
        /// The difference in position for data to get sent.
        /// </summary>
        [SerializeField]
        private float _positionThreshold = 0.01f;

        /// <summary>
        /// The difference in rotation in degrees for data to get sent.
        /// </summary>
        [SerializeField]
        private float _rotationAngleThreshold = 0.5f;

        /// <summary>
        /// The difference in the blendshape weight for data to get sent.
        /// </summary>
        [SerializeField]
        private float _shapeThreshold = 0.01f;

        /// <summary>
        /// The indices for the body that should be synced.
        /// </summary>
        [SerializeField]
        private int[] _bodyIndicesToSync;

        /// <summary>
        /// The indices for the body that should be sent.
        /// </summary>
        [SerializeField]
        private int[] _bodyIndicesToSend;

        /// <summary>
        /// The indices for the face that should be sent.
        /// </summary>
        [SerializeField]
        private int[] _faceIndicesToSend;

        /// <summary>
        /// True if interpolation should be applied when using received data.
        /// </summary>
        [SerializeField]
        private bool _useInterpolation = true;

        /// <summary>
        /// The maximum size of the data buffer.
        /// </summary>
        [SerializeField]
        private int _maxBufferSize = 5;

        /// <summary>
        /// Objects that should be hidden until the spawned instance is ready to be networked.
        /// </summary>
        [SerializeField]
        private GameObject[] _objectsToHideUntilValid;

        private bool _hasValidDebugPose = false;

        public override void Awake()
        {
            base.Awake();
            ToggleObjects(false);
        }

        public override void Start()
        {
            base.Start();
            UpdateSerializationSettings();
        }

        public override void Update()
        {
            if (_ownership == Ownership.Host)
            {
                base.Update();
            }
        }

        public override void LateUpdate()
        {
            if (_ownership == Ownership.Host)
            {
                base.LateUpdate();
            }
            else if (_ownership == Ownership.Client)
            {
                if (_debugDrawTargetSkeleton && _hasValidDebugPose)
                {
                    _skeletonRetargeter.DrawDebugTargetPose(_debugDrawTransform, _debugDrawTargetSkeletonColor, true);
                }
            }
        }

        public void SetDebugPose(NativeArray<NativeTransform> bodyPose)
        {
            if (!_debugDrawTargetSkeleton)
            {
                return;
            }
            _hasValidDebugPose = true;
            var worldPose = _skeletonRetargeter.GetWorldPoseFromLocalPose(bodyPose);
            _skeletonRetargeter.RetargetedPose.CopyFrom(worldPose);
            worldPose.Dispose();
        }

        protected void OnValidate()
        {
            UpdateSerializationSettings();
        }

        /// <summary>
        /// Reflect the updated serialization settings in the native plugin.
        /// </summary>
        public void UpdateSerializationSettings()
        {
            if (RetargetingHandle == 0)
            {
                return;
            }

            if (GetSerializationSettings(RetargetingHandle, out SerializationSettings settings))
            {
                settings.CompressionType = _compressionType;
                settings.PositionThreshold = _positionThreshold;
                settings.RotationAngleThresholdDegrees = _rotationAngleThreshold;
                settings.ShapeThreshold = _shapeThreshold;
                MSDKUtility.UpdateSerializationSettings(RetargetingHandle, settings);
            }
        }

        /// <summary>
        /// Toggle the objects set in this config active or inactive.
        /// </summary>
        /// <param name="isActive">True if the objects should be active.</param>
        public void ToggleObjects(bool isActive)
        {
            foreach (var obj in _objectsToHideUntilValid)
            {
                obj.SetActive(isActive);
            }
        }
    }
}
