// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Movement.Playback;
using Meta.XR.Movement.Retargeting;
using System;
using System.IO;
using Unity.Collections;
using UnityEngine;
using static Meta.XR.Movement.MSDKUtility;

namespace Meta.XR.Movement.Recording
{
    /// <summary>
    /// Reads retargeter data from file to apply to a character.
    /// </summary>
    public class SequenceFileReader : IPlaybackBehaviour
    {
        /// <inheritdoc cref="IPlaybackBehaviour.SnapshotIndex"/>
        public int SnapshotIndex => _playbackManager.SnapshotIndex;

        /// <inheritdoc cref="IPlaybackBehaviour.NumSnapshots"/>
        public int NumSnapshots => _playbackManager.NumSnapshots;

        /// <inheritdoc cref="IPlaybackBehaviour.HasOpenedFileForPlayback"/>
        public bool HasOpenedFileForPlayback => _playbackManager.HasActivePlaybackFile;

        /// <inheritdoc cref="IRecordingBehaviour.BandwidthKbps"/>
        public float BandwidthKbps => _bandwidthRecorder.BandwidthKbps;
        private BandwidthRecorder _bandwidthRecorder = new BandwidthRecorder();

        /// <inheritdoc cref="IPlaybackBehaviour.UserActivelyScrubbing"/>
        public bool UserActivelyScrubbing
        {
            get => _activelyScrubbing;
            set
            {
                _activelyScrubbing = value;
            }
        }

        /// <summary>
        /// Gets the current body pose data from the tracker.
        /// </summary>
        public NativeArray<NativeTransform> BodyPose => _deserSourcePose;

        /// <summary>
        /// Gets whether the playback is currently active and not paused.
        /// </summary>
        public bool IsPlaying => _playbackManager.HasActivePlaybackFile && !_pauseState;

        /// <summary>
        /// Gets the current playback time in seconds relative to the start time.
        /// </summary>
        public float PlaybackTime => _playbackManager.NetworkTime - (float)_playbackManager.StartNetworkTime;

        private UInt64 _playbackHandle;

        private SequencePlaybackManager _playbackManager =
                           new SequencePlaybackManager();

        private NativeArray<NativeTransform> _deserSourcePose;
        private SerializationCompressionType _receivedCompressionType;

        private int _numSourceJoints;
        private int[] _sourceParentIndices;
        private bool _activelyScrubbing;
        private bool _pauseState = false;

        ~SequenceFileReader()
        {
            CleanUp();
        }

        /// <summary>
        /// Initializes the sequence file reader with the specified retargeting handle.
        /// </summary>
        /// <param name="handle">The retargeting handle to use for playback.</param>
        public void Init(UInt64 handle)
        {
            _playbackHandle = handle;
            GetSkeletonInfo(_playbackHandle, SkeletonType.SourceSkeleton, out var sourceSkeletonInfo);
            _numSourceJoints = sourceSkeletonInfo.JointCount;

            var nativeSourceParentIndices = new NativeArray<int>(_numSourceJoints,
                Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            GetParentJointIndexesByRef(_playbackHandle, SkeletonType.SourceSkeleton, ref nativeSourceParentIndices);
            _sourceParentIndices = nativeSourceParentIndices.ToArray();
        }

        /// <inheritdoc cref="IPlaybackBehaviour.Seek(int)"/>
        public bool Seek(int snapshotIndex)
        {
            return _playbackManager.Seek(
                _playbackHandle,
                snapshotIndex,
                ProcessSnapshotGetNetworkTimeDelegate,
                DeserializeData);
        }

        /// <inheritdoc cref="IPlaybackBehaviour.PlaybackRecording"/>
        public bool PlayBackRecording(string playbackAssetPath)
        {
            _playbackManager.ClosePlaybackFileIfOpen();
            var finalPath = Path.Combine(Application.dataPath, playbackAssetPath);
            _playbackManager.OpenFileForPlayback(_playbackHandle, finalPath);
            if (_playbackManager.HasActivePlaybackFile)
            {
                MSDKUtility.ResetInterpolators(_playbackHandle);
                _playbackManager.ResetTimestampsToStart();
                _pauseState = false;
            }
            return _playbackManager.HasActivePlaybackFile;
        }

        /// <inheritdoc cref="IPlaybackBehaviour.SetPauseState(bool)"/>
        public void SetPauseState(bool pauseState)
        {
            _pauseState = pauseState;
        }

        /// <inheritdoc cref="IPlaybackBehaviour.ClosePlaybackFile"/>
        public void ClosePlaybackFile()
        {
            _playbackManager.ClosePlaybackFileIfOpen();
        }

        /// <summary>
        /// Plays the next frame from the sequence file, updating the pose data.
        /// This method handles time progression, looping, and interpolation.
        /// </summary>
        public void PlayNextFrame()
        {
            if (_activelyScrubbing)
            {
                return;
            }

            if (_pauseState)
            {
                return;
            }

            _playbackManager.NetworkTime += Time.deltaTime;
            if (_playbackManager.ReadAllSnapshots)
            {
                _playbackManager.ResetTimestampsToStart();
                _playbackManager.RestartSnapshotReading();
                ResetInterpolators(_playbackHandle);
            }

            if (_playbackManager.NetworkTime < _playbackManager.LastTimeStamp && _playbackManager.NetworkTime > 0.0f)
            {
                return;
            }
            byte[] snapshotBytes = _playbackManager.ReadNextSnapshotBytes();
            if (snapshotBytes != null)
            {
                float readTimestamp = ProcessSnapshotGetNetworkTimeDelegate(snapshotBytes);
                _playbackManager.LastTimeStamp = (float)readTimestamp;
            }
        }

        /// <summary>
        /// Processes snapshot bytes and returns the network time.
        /// This method is used as a delegate for snapshot processing during playback.
        /// </summary>
        /// <param name="snapshotBytes">The snapshot bytes to process.</param>
        /// <returns>The network time of the processed snapshot.</returns>
        public float ProcessSnapshotGetNetworkTimeDelegate(
            byte[] snapshotBytes)
        {
            return _playbackManager.ProcessSnapshotBytesAndGetNetworkTime(
                snapshotBytes,
                _activelyScrubbing,
                DeserializeData,
                LerpReceivedData,
                ProcessReceivedData);
        }

        private void LerpReceivedData()
        {
            GetInterpolatedSkeleton(
                _playbackHandle,
                SkeletonType.SourceSkeleton,
                ref _deserSourcePose,
                _playbackManager.NetworkTime);
        }

        private void ProcessReceivedData()
        {
            if (CompressionUsesJointLengths(_receivedCompressionType))
            {
                SkeletonUtilities.ConvertLocalPosesToAbsolute(_sourceParentIndices, _deserSourcePose);
            }
        }

        private bool DeserializeData(byte[] bytes)
        {
            AllocateDeserializationArrays();
            var nativeBytes =
                new NativeArray<byte>(bytes.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < bytes.Length; i++)
            {
                nativeBytes[i] = bytes[i];
            }
            // Create dummy arguments for deserialized data that we don't care about.
            var emptyBody = new NativeArray<NativeTransform>(1, Allocator.Temp);
            var emptyFace = new NativeArray<float>(1, Allocator.Temp);
            var frameData = new FrameData();
            if (!DeserializeSkeletonAndFace(
                    _playbackHandle,
                    nativeBytes,
                    out var timestamp,
                    out _receivedCompressionType,
                    out var ack,
                    ref emptyBody,
                    ref emptyFace,
                    ref _deserSourcePose,
                    ref frameData))
            {
                Debug.LogError("Data deserialized is invalid!");
                return false;
            }
            return true;
        }

        private void AllocateDeserializationArrays()
        {
            if (!_deserSourcePose.IsCreated)
            {
                _deserSourcePose =
                    new NativeArray<NativeTransform>(_numSourceJoints, Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory);
            }
        }

        private void CleanUp()
        {
            if (_deserSourcePose.IsCreated)
            {
                _deserSourcePose.Dispose();
            }

            _playbackManager.ClosePlaybackFileIfOpen();
        }
    }
}
