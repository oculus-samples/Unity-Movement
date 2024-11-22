// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using System;
using Meta.XR.Movement.Retargeting;
using UnityEngine;
using static Meta.XR.Movement.NativeUtilityPlugin;

namespace Meta.XR.Movement.Networking
{
    /// <summary>
    /// The network pose retargeter configuration. This contains all the necessary data
    /// to determine how the data should be sent/received when networking using the pose
    /// retargeter.
    /// </summary>
    public class NetworkPoseRetargeterConfig : PoseRetargeterConfig
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

        public NativeUtilityPlugin.CompressionType CompressionType
        {
            get => _compressionType;
            set
            {
                _compressionType = value;
                UpdateSerializationSettings(_handle);
            }
        }

        public float PositionThreshold
        {
            get => _positionThreshold;
            set
            {
                _positionThreshold = value;
                UpdateSerializationSettings(_handle);
            }
        }

        public float RotationAngleThreshold
        {
            get => _rotationAngleThreshold;
            set
            {
                _rotationAngleThreshold = value;
                UpdateSerializationSettings(_handle);
            }
        }

        public float ShapeThreshold
        {
            get => _shapeThreshold;
            set
            {
                _shapeThreshold = value;
                UpdateSerializationSettings(_handle);
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

        public Ownership Owner
        {
            get => _ownership;
            set => _ownership = value;
        }

        public float IntervalToSendData => _intervalToSendData;

        public float IntervalToSyncData => _intervalToSyncData;

        public bool UseSyncInterval => _useSyncInterval;

        public int MaxBufferSize => _maxBufferSize;

        public bool UseDeltaCompression => _useDeltaCompression;

        public bool UseInterpolation => _useInterpolation;


        /// <summary>
        /// The ownership type of this instance.
        /// </summary>
        [SerializeField]
        private Ownership _ownership = Ownership.None;

        /// <summary>
        /// The compression type that should be used when sending/receiving data.
        /// </summary>
        [SerializeField]
        private NativeUtilityPlugin.CompressionType _compressionType =
            NativeUtilityPlugin.CompressionType.CompressedWithBoneLengths;

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
        private float _intervalToSendData = 0.08f;

        /// <summary>
        /// The interval to sync data at.
        /// </summary>
        [SerializeField]
        private float _intervalToSyncData = 1.0f;

        /// <summary>
        /// The difference in position for data to get sent.
        /// </summary>
        [SerializeField]
        private float _positionThreshold = 0.001f;

        /// <summary>
        /// The difference in rotation in degrees for data to get sent.
        /// </summary>
        [SerializeField]
        private float _rotationAngleThreshold = 0.01f;

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


        private UInt64 _handle;

        protected void Awake()
        {
            ToggleObjects(false);
        }

        protected void OnValidate()
        {
            UpdateSerializationSettings(_handle);
        }

        /// <summary>
        /// Reflect the updated serialization settings in the native plugin.
        /// </summary>
        /// <param name="handle">The native retargeter handle.</param>
        public void UpdateSerializationSettings(UInt64 handle)
        {
            _handle = handle;
            if (_handle == 0)
            {
                return;
            }

            SetCompressionType(_handle, _compressionType);
            SetPositionThreshold(_handle, _positionThreshold);
            SetRotationAngleThreshold(_handle, _rotationAngleThreshold);
            SetShapeThreshold(_handle, _shapeThreshold);
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
