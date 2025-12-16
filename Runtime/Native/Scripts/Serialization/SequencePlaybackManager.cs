// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using static Meta.XR.Movement.MSDKUtility;

namespace Meta.XR.Movement.Playback
{
    /// <summary>
    /// Manages the state of a snapshot file used for playback.
    /// </summary>
    public class SequencePlaybackManager
    {
        /// <summary>
        /// Number of snapshots available.
        /// </summary>
        public int NumSnapshots => _startHeader.NumSnapshots;

        /// <summary>
        /// Start network time.
        /// </summary>
        public double StartNetworkTime => _startHeader.StartNetworkTime;

        /// <summary>
        /// Num buffered snapshots used for serialization.
        /// </summary>
        public int NumBufferedSnapshots => _startHeader.NumBufferedSnapshots;

        /// <summary>
        /// If has file open for playback.
        /// </summary>
        public bool HasActivePlaybackFile => (_playbackFile != null);

        /// <summary>
        /// If read through all snapshots or not.
        /// </summary>
        public bool ReadAllSnapshots => (_snapshotIndex == _startHeader.NumSnapshots - 1);

        /// <summary>
        /// Snapshot index currently being read.
        /// </summary>
        public int SnapshotIndex => _snapshotIndex;

        /// <summary>
        /// Current playback network time of serialized data.
        /// </summary>
        public float NetworkTime
        {
            get => _networkTime;
            set => _networkTime = value;
        }

        /// <summary>
        /// Last network time.
        /// </summary>
        public float LastTimeStamp
        {
            get => _lastTimestamp;
            set => _lastTimestamp = value;
        }

        /// <summary>
        /// Returns data version of the opened recording file.
        /// </summary>
        public double DataVersion => _startHeader.DataVersion;

        /// <summary>
        /// Delegate for processing a snapshot.
        /// </summary>
        /// <param name="snapshotBytes">Snapshot bytes to process.</param>
        /// <returns>The timestamp of the snapshot.</returns>
        public delegate float ProcessSnapshotGetTimestamp(byte[] snapshotBytes);
        /// <summary>
        /// Delegate for deserializing a snapshot.
        /// </summary>
        /// <param name="snapshotBytes">Snapshot bytes.</param>
        /// <returns>If the snapshot was deserialized or not.</returns>
        public delegate bool DeserializeSnapshot(byte[] snapshotBytes);

        /// <summary>
        /// Deserialize delegate.
        /// </summary>
        /// <param name="snapshotBytes">Snapshot bytes.</param>
        /// <returns>True if deserialized, false if not.</returns>
        public delegate bool Deserialize(byte[] snapshotBytes);
        /// <summary>
        /// Lerp delegate.
        /// </summary>
        public delegate void LerpReceivedTrackingData();
        /// <summary>
        /// Delegate for processing deserialized data.
        /// </summary>
        public delegate void ProcessReceivedData();

        private PlaybackFunctions.ReaderFileStream _playbackFile = null;
        private StartHeader _startHeader;
        private EndHeader _endHeader;
        private int _snapshotIndex = 0, _numSnapshotBytesReadSoFar = 0;
        private bool _playedFirstFrame = false;
        private int[] _snapshotToByteoffsetAfterHeader;

        private float _networkTime = 0.0f;
        private float _lastTimestamp = 0.0f;

        /// <summary>
        /// Restart all timestamps to the start timestamp.
        /// </summary>
        public void ResetTimestampsToStart()
        {
            _lastTimestamp = (float)StartNetworkTime;
            _networkTime = (float)StartNetworkTime;
        }

        /// <summary>
        /// Opens file for playback.
        /// </summary>
        /// <param name="handle">Native plugin handle.</param>
        /// <param name="playbackPath">Optional playback path.</param>
        public void OpenFileForPlayback(ulong handle, string playbackPath = null)
        {
            _playbackFile = PlaybackFunctions.OpenFileForPlayback(
                ref _startHeader,
                ref _endHeader,
                ref _snapshotToByteoffsetAfterHeader,
                playbackPath);
            _snapshotIndex = 0;
            _numSnapshotBytesReadSoFar = 0;
            _playedFirstFrame = false;
        }

        /// <summary>
        /// Gets byte offset of snapshot (after header) as well as baseline
        /// sync index for specified snapshot.
        /// </summary>
        /// <param name="snapshotIndex">Snapshot index to query.</param>
        /// <returns>Byte offset (after) header for snapshot as well
        /// as last baseline in terms of n snapshots in past. If 0, that means
        /// that current snapshot is the baseline.</returns>
        public (int, int) GetByteOffsetAndLastSyncForSnapshotIndex(int snapshotIndex)
        {
            int destinationSnapOffset = _snapshotToByteoffsetAfterHeader[snapshotIndex];
            int snapshotsSinceLastSync = _playbackFile.GetSnapshotsSinceLastSync(
                destinationSnapOffset);
            return (destinationSnapOffset, snapshotsSinceLastSync);
        }

        /// <summary>
        /// Reads next snapshot bytes. Modifies internal state by moving forward
        /// in the plabyack file.
        /// </summary>
        /// <returns>Snapshot bytes read, if any.</returns>
        public byte[] ReadNextSnapshotBytes()
        {
            if (_playbackFile == null)
            {
                Debug.LogError("Can't fetch snapshot bytes because no file has been " +
                    "opened yet.");
                return null;
            }

            // if we haven't played first first, then assume snapshot index is 0
            // otherwise, move to the next index
            if (!_playedFirstFrame)
            {
                _snapshotIndex = 0;
            }
            else
            {
                _snapshotIndex++;
            }

            int snapshotsSinceBase = 0;
            byte[] snapshotBytes = PlaybackFunctions.ReadSnapshotAtOffset(
               _playbackFile,
               ref _numSnapshotBytesReadSoFar,
               _snapshotIndex,
               ref snapshotsSinceBase,
               _startHeader.NumSnapshots);
            if (snapshotBytes != null)
            {
                _playedFirstFrame = true;
            }

            return snapshotBytes;
        }

        /// <summary>
        /// Gets snapshot byte offset after header.
        /// </summary>
        /// <param name="snapshotIndex">Snapshot index.</param>
        /// <returns>Offset of snapshot in terms of bytes after the start header
        /// in the playback file.</returns>
        public int GetSnapshotByteOffset(int snapshotIndex)
        {
            if (snapshotIndex >= _snapshotToByteoffsetAfterHeader.Length)
            {
                Debug.LogError($"Cannot return offset at index {snapshotIndex} because " +
                    $"the length of {_snapshotToByteoffsetAfterHeader.Length} is too small.");
                return 0;
            }

            return _snapshotToByteoffsetAfterHeader[snapshotIndex];
        }

        /// <summary>
        /// Gets snapshot at specific index.
        /// </summary>
        /// <param name="byteOffset">Byte offset to seek.</param>
        /// <param name="snapshotIndex">Snapshot index.</param>
        /// <param name="moveCurrentTrackedSnapshotToIndex">Whether or not to set currently
        /// seeked snapshot to offset specified. If true, then functions like
        /// <see cref="ReadNextSnapshotBytes"/> from the offset called into this function.</param>
        /// <returns>Bytes deserialized, if any.</returns>
        public byte[] GetBytesAtSpecificSnapshotIndex(int byteOffset, int snapshotIndex,
            bool moveCurrentTrackedSnapshotToIndex = false)
        {
            var numSnapshotBytesReadSoFar = byteOffset;
            int snapshotsSinceBase = 0;
            var snapshotBytes = PlaybackFunctions.ReadSnapshotAtOffset(
                _playbackFile,
                ref numSnapshotBytesReadSoFar,
                snapshotIndex,
                ref snapshotsSinceBase,
                _startHeader.NumSnapshots);

            if (moveCurrentTrackedSnapshotToIndex)
            {
                _numSnapshotBytesReadSoFar = numSnapshotBytesReadSoFar;
                _snapshotIndex = snapshotIndex;
            }

            return snapshotBytes;
        }

        /// <summary>
        /// Processes snapshot bytes and returns network time.
        /// </summary>
        /// <param name="snapshotBytes">Snapshot bytes to process.</param>
        /// <param name="isScrubbingData">If data is being scrubbed or not.</param>
        /// <param name="deserializeDelegate">Deserialize delegate.</param>
        /// <param name="lerpReceivedData">Lerp delegate.</param>
        /// <param name="processReceivedData">Process received data delegate.</param>
        /// <returns>Network time.</returns>
        public float ProcessSnapshotBytesAndGetNetworkTime(
            byte[] snapshotBytes,
            bool isScrubbingData,
            Deserialize deserializeDelegate,
            LerpReceivedTrackingData lerpReceivedData,
            ProcessReceivedData processReceivedData)
        {
            NativeArray<byte> nativeBytesArray = new NativeArray<byte>(
                   snapshotBytes.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < snapshotBytes.Length; i++)
            {
                nativeBytesArray[i] = snapshotBytes[i];
            }
            double readTimestamp = 0.0f;
            DeserializeSnapshotTimestamp(nativeBytesArray, out readTimestamp);
            if (deserializeDelegate(snapshotBytes))
            {
                if (!isScrubbingData)
                {
                    lerpReceivedData();
                }
                processReceivedData();
            }
            return (float)readTimestamp;
        }

        /// <summary>
        /// Public-version of the seek function.
        /// </summary>
        /// <param name="handle">Native handle.</param>
        /// <param name="snapshotIndex">Snapshot index.</param>
        /// <param name="processSnapshot">Delegate that processes the target snapshot and gets the timestamp.</param>
        /// <param name="deserializeSnapshot">Network time that should be modified based on seeking.</param>
        /// <returns>True if seek worked; false if not.</returns>
        public bool Seek(
            UInt64 handle,
            int snapshotIndex,
            ProcessSnapshotGetTimestamp processSnapshot,
            DeserializeSnapshot deserializeSnapshot)
        {
            // Avoid seek if one can avoid it.
            if (snapshotIndex == SnapshotIndex)
            {
                return false;
            }
            float newNetworkTime = NetworkTime;
            bool seekSuccesful = Seek(
                handle,
                snapshotIndex,
                processSnapshot,
                deserializeSnapshot,
                ref newNetworkTime);
            if (!seekSuccesful)
            {
                return false;
            }
            // On next tick, the next frame will be seeked based on these
            // time values.
            NetworkTime = newNetworkTime;
            LastTimeStamp = newNetworkTime;
            return true;
        }

        /// <summary>
        /// Convenient function used for seeking to a snapshot.
        /// </summary>
        /// <param name="handle">Native handle.</param>
        /// <param name="snapshotIndex">Snapshot index.</param>
        /// <param name="processSnapshot">Delegate that processes the target snapshot and gets the timestamp.</param>
        /// <param name="deserializeSnapshot">Delegate that deserializes the snapshot.</param>
        /// <param name="networkTime">Network time that should be modified based on seeking.</param>
        /// <returns>True if seek worked; false if not.</returns>
        private bool Seek(
            UInt64 handle,
            int snapshotIndex,
            ProcessSnapshotGetTimestamp processSnapshot,
            DeserializeSnapshot deserializeSnapshot,
            ref float networkTime)
        {
            if (!HasActivePlaybackFile)
            {
                Debug.LogError("Cannot seek without playing back.");
                return false;
            }

            if (snapshotIndex > NumSnapshots)
            {
                Debug.LogError($"Cannot seek to {snapshotIndex} because there are only " +
                    $"0-{NumSnapshots - 1} snapshots available.");
                return false;
            }

            int snapshotsSinceLastSync, destinationSnapshotOffset;
            (destinationSnapshotOffset, snapshotsSinceLastSync) =
                GetByteOffsetAndLastSyncForSnapshotIndex(snapshotIndex);
            byte[] snapshotBytes = null;
            // If there have been x snapshots since the baseline, deserialize the snapshots
            // before us first.
            if (snapshotsSinceLastSync > 0)
            {
                int baselineSnapshot = snapshotIndex - snapshotsSinceLastSync;
                Assert.IsTrue(baselineSnapshot >= 0);
                // go from baseline to destination
                for (int i = 0; i < snapshotsSinceLastSync; ++i)
                {
                    int currentSnapshotIndex = baselineSnapshot + i;
                    int currentSnapshotByteOffset = GetSnapshotByteOffset(currentSnapshotIndex);
                    // get snapshot to ensure delta computation
                    snapshotBytes = GetBytesAtSpecificSnapshotIndex(
                        currentSnapshotByteOffset, currentSnapshotIndex, false);
                    if (snapshotBytes == null || !deserializeSnapshot(snapshotBytes))
                    {
                        Debug.LogError($"Could not get snapshot {currentSnapshotIndex} for " +
                            $"destination snapshot {snapshotIndex} during seeking, " +
                            $"looked for {i} snapshots back from destination. Baseline is " +
                            $"{snapshotsSinceLastSync} from destination.");
                        return false;
                    }
                }
            }
            // Process final snapshot.
            snapshotBytes = GetBytesAtSpecificSnapshotIndex(
                destinationSnapshotOffset, snapshotIndex, true);
            float targetSnapshotTimestamp = 0.0f;
            if (snapshotBytes != null)
            {
                targetSnapshotTimestamp = processSnapshot(snapshotBytes);
            }
            else
            {
                Debug.LogError($"Could not seek to destination snapshot {snapshotIndex}.");
                return false;
            }

            // go to final timestamp, offset by delta time so that render time is set to network time
            networkTime = targetSnapshotTimestamp + Time.deltaTime;
            _networkTime = networkTime;

            // reset all interpolated data since we are seeking to a new point. We do
            // not want to interpolate from data that comes before the destination snapshot.
            MSDKUtility.ResetInterpolators(handle);

            return true;
        }

        /// <summary>
        /// Obtains trimmed data for a specified range of snapshots. This includes
        /// the max snapshot (as opposed to just the bytes up to it).
        /// </summary>
        /// <param name="minTrimmedSnapshot">Min snapshot index.</param>
        /// <param name="maxTrimmedSnapshot">Max snapshot index.</param>
        /// <param name="newStartHeader">New start header for trimmed data.</param>
        /// <param name="newEndHeader">New end header for trimmed data.</param>
        /// <param name="trimmedBytes">Trimmed snapshot bytes.</param>
        /// <returns>True if trim operation worked; false if not.</returns>
        public bool AssembleTrimmedData(
            int minTrimmedSnapshot,
            int maxTrimmedSnapshot,
            out StartHeader newStartHeader,
            out EndHeader newEndHeader,
            out byte[] trimmedBytes)
        {
            newEndHeader = new EndHeader();
            newStartHeader = new StartHeader();
            trimmedBytes = null;
            // Do error checks first.
            if (!HasActivePlaybackFile)
            {
                Debug.LogError("Can't get trimmed data because no playback file has been currently loaded.");
                return false;
            }
            if (minTrimmedSnapshot < 0 || maxTrimmedSnapshot < 0)
            {
                Debug.LogError($"Trim range ({minTrimmedSnapshot}-{maxTrimmedSnapshot}) " +
                    $"has at least one negative value, which is invalid.");
                return false;
            }
            if (maxTrimmedSnapshot < minTrimmedSnapshot ||
                minTrimmedSnapshot >= NumSnapshots ||
                maxTrimmedSnapshot >= NumSnapshots)
            {
                Debug.LogError("Please make sure your min and max trimmed snapshot " +
                    $"range is between ({0}-{NumSnapshots - 1}), and " +
                    $"that the max is greater than or equal to the min. Your specified range " +
                    $"is ({minTrimmedSnapshot}-{maxTrimmedSnapshot}).");
                return false;
            }

            // make sure the min baseline is a full one
            int snapshotsSinceLastSync, destinationSnapshotOffset;
            (destinationSnapshotOffset, snapshotsSinceLastSync) =
                GetByteOffsetAndLastSyncForSnapshotIndex(minTrimmedSnapshot);
            if (snapshotsSinceLastSync > 0)
            {
                Debug.LogWarning($"Min trimmed snapshot index {minTrimmedSnapshot} is not a full one. "
                    + $"The baseline is {snapshotsSinceLastSync} backwards in time, which would be "
                    + $"{minTrimmedSnapshot - snapshotsSinceLastSync}. Will use that.");
                minTrimmedSnapshot -= snapshotsSinceLastSync;
            }

            trimmedBytes = _playbackFile.GetSnapshotRangeBytes(
                minTrimmedSnapshot,
                maxTrimmedSnapshot,
                _snapshotToByteoffsetAfterHeader);
            // the header of the clone need to be updated too to indicate restricted range.
            newStartHeader = new StartHeader(
                dataVersion: _startHeader.DataVersion,
                osVersion: SystemInfo.operatingSystem,
                gameEngineVersion: Application.unityVersion,
                bundleID: Application.identifier,
                metaXRSDKVersion: OVRPlugin.version.ToString(),
                utcTimeStamp: DateTime.UtcNow.Ticks,
                numSnapshots: (maxTrimmedSnapshot - minTrimmedSnapshot) + 1,
                numTotalSnapshotBytes: trimmedBytes.Length,
                startNetworkTime: _playbackFile.GetNetworkTimeForSnapshot(_snapshotToByteoffsetAfterHeader[minTrimmedSnapshot]),
                numberOfBufferedSnapshots: _startHeader.NumBufferedSnapshots);
            // re-use the old UTC timestamp to maintain some association with the original recording.
            newEndHeader = new EndHeader(_endHeader.UTCTimestamp);
            return true;
        }

        /// <summary>
        /// Restarts snapshot reading to the beginning.
        /// </summary>
        public void RestartSnapshotReading()
        {
            _snapshotIndex = 0;
            _numSnapshotBytesReadSoFar = 0;
            _playedFirstFrame = false;
        }

        /// <summary>
        /// Closest playbackfile if already open.
        /// </summary>
        public void ClosePlaybackFileIfOpen()
        {
            if (_playbackFile != null)
            {
                _playbackFile.Dispose();
                _playbackFile = null;
            }
        }
    }
}
