// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace Meta.XR.Movement.Recording
{
    /// <summary>
    /// Keeps track of bandwidth used for serialization.
    /// </summary>
    public class BandwidthRecorder
    {
        /// <summary>
        /// The current bandwidth in kilobits per second.
        /// </summary>
        public float BandwidthKbps { get; private set; }

        private float _currentByteCount = 0.0f;
        private const float _bytesToKilobits = 0.008f;
        private float _debugDataTimer = 0.0f;

        /// <summary>
        /// Increments the current time and measure bandwidth if at least a second has passed.
        /// </summary>
        public void IncrementTimeAndUpdateBandwidth()
        {
            _debugDataTimer += Time.deltaTime;
            if (_debugDataTimer < 1.0f)
            {
                return;
            }

            BandwidthKbps = _currentByteCount * _bytesToKilobits;
            _currentByteCount = 0;
            _debugDataTimer -= 1.0f;
        }

        /// <summary>
        /// Add the number of bytes to count.
        /// </summary>
        /// <param name="numBytes">Number of bytes.</param>
        public void AddNumBytes(int numBytes)
        {
            _currentByteCount += numBytes;
        }
    }
}
