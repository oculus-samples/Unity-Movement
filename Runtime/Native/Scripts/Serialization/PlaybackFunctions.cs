// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System;
using System.Globalization;
using System.IO;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using static Meta.XR.Movement.MSDKUtility;

namespace Meta.XR.Movement.Playback
{
    /// <summary>
    /// Contains types and functions used for deserializing data.
    /// </summary>
    public class PlaybackFunctions
    {
        /// <summary>
        /// Class that contains the file stream to read to as well as its
        /// associated binary reader. Its functions return deserialized data
        /// obtained from the file.
        /// </summary>
        public class ReaderFileStream
        {
            /// <summary>
            /// Size of snapshot's header in terms of bytes.
            /// </summary>
            private const int _SNAPSHOT_HEADER_SIZE_BYTES = 8;

            private FileStream _stream;
            private BinaryReader _reader;

            /// <summary>
            /// Main constructor.
            /// </summary>
            /// <param name="stream">File stream.</param>
            /// <param name="reader">Binary writer.</param>
            public ReaderFileStream(FileStream stream, BinaryReader reader)
            {
                _stream = stream;
                _reader = reader;
            }

            /// <summary>
            /// Parses headers of a recording, along with snapshot byte offsets.
            /// Checks the recording against an expected data version.
            /// </summary>
            /// <param name="startHeader">The recording's start header.</param>
            /// <param name="endHeader">The recording's end header.</param>
            /// <param name="snapshotToByteOffsetAfterHeader">The byte offset of each snapshot
            /// after the header. We can use this to find snapshot i in the file this week, by
            /// doing a seek of HEADER_BYTES added to the snapshot byte offset.</param>
            /// <param name="expectedDataVersion">The expected data version of the recording. An
            /// error is thrown if the snapshot recording does not match it.</param>
            /// <exception cref="Exception">An exception is thrown if an error occurs during
            /// deserialization.</exception>
            public void ParseHeadersAndOffsets(
                ref StartHeader startHeader,
                ref EndHeader endHeader,
                ref int[] snapshotToByteOffsetAfterHeader)
            {
                byte[] startHeaderBytes = new byte[SERIALIZATION_START_HEADER_SIZE_BYTES];
                byte[] endHeaderBytes = new byte[SERIALIZATION_END_HEADER_SIZE_BYTES];
                _reader.Read(startHeaderBytes, 0, startHeaderBytes.Length);
                if (!DeserializeStartHeader(startHeaderBytes, out startHeader))
                {
                    throw new Exception("Could not deserialize start header from file!");
                }
                if (!GetIsSerializationVersionSupported(startHeader.DataVersion))
                {
                    throw new Exception($"Data version of file ({startHeader.DataVersion}) is not supported by this reader.");
                }

                // go through all snapshots and record byte offsets. This will allow scrubbing.
                snapshotToByteOffsetAfterHeader = new int[startHeader.NumSnapshots];
                int snapshotByteOffsetAfterHeader = 0;
                for (int frameIndex = 0; frameIndex < startHeader.NumSnapshots; frameIndex++)
                {
                    snapshotToByteOffsetAfterHeader[frameIndex] = snapshotByteOffsetAfterHeader;
                    SkipSnapshotDataFromCurrentSeek(out int numBytesSkipped);
                    snapshotByteOffsetAfterHeader += numBytesSkipped;
                }

                // process end header after skipping snapshot frames
                _reader.Read(endHeaderBytes, 0, endHeaderBytes.Length);

                if (!DeserializeEndHeader(endHeaderBytes, out endHeader))
                {
                    throw new Exception("Could not deserialize start header from file!");
                }

                //Debug.Log($"Start header ${startHeader.ToString()}");
                //Debug.Log($"End header ${endHeader.ToString()}");
            }

            /// <summary>
            /// Cleans up unmanaged resources.
            /// </summary>
            public void Dispose()
            {
                _reader.Dispose();
                _stream.Close();
                _stream.Dispose();
            }

            private void SkipSnapshotDataFromCurrentSeek(out int numBytesSkipped)
            {
                int snapshotSize = _reader.ReadInt32();
                int snapshotsSinceLastSync = _reader.ReadInt32();
                _stream.Seek(snapshotSize, SeekOrigin.Current);
                numBytesSkipped = _SNAPSHOT_HEADER_SIZE_BYTES + snapshotSize;
            }

            /// <summary>
            /// Reads snapshot bytes.
            /// </summary>
            /// <param name="snapshotByteoffset">Snapshot byte offset to seek.</param>
            /// <param name="snapshotsSinceLastSync">Number of snapshots since last seek (deserialized).</param>
            /// <param name="snapshotBytesRead">Deserialized snaphot bytes.</param>
            /// <param name="numTotalBytesDeserialized">Number of total bytes deserialized, including snapshot header bytes.</param>
            public void ReadSnapshotData(
                int snapshotByteoffset,
                out int snapshotsSinceLastSync,
                out byte[] snapshotBytesRead,
                out int numTotalBytesDeserialized)
            {
                _stream.Seek(SERIALIZATION_START_HEADER_SIZE_BYTES
                    + snapshotByteoffset, SeekOrigin.Begin);
                int snapshotSize = _reader.ReadInt32();
                snapshotsSinceLastSync = _reader.ReadInt32();
                snapshotBytesRead = _reader.ReadBytes(snapshotSize);
                numTotalBytesDeserialized = _SNAPSHOT_HEADER_SIZE_BYTES + snapshotSize;
            }

            /// <summary>
            /// Gets the number snapshots since last sync or last full snapshot,
            /// from the current snapshot offset.
            /// </summary>
            /// <param name="snapshotByteOffset">The snapshot byte offset.</param>
            /// <returns></returns>
            public int GetSnapshotsSinceLastSync(int snapshotByteOffset)
            {
                _stream.Seek(SERIALIZATION_START_HEADER_SIZE_BYTES
                    + snapshotByteOffset, SeekOrigin.Begin);
                // skip the size, then read the correct snapshot size.
                var snapshotSize = _reader.ReadInt32();
                return _reader.ReadInt32();
            }

            /// <summary>
            /// Get network time for snapshot.
            /// </summary>
            /// <param name="snapshotByteOffset">Snapshot byte offset.</param>
            /// <returns>Network time.</returns>
            public double GetNetworkTimeForSnapshot(int snapshotByteOffset)
            {
                ReadSnapshotData(
                    snapshotByteOffset,
                    out int snapshotsSinceLastSync,
                    out byte[] snapshotBytesRead,
                    out int numTotalBytesDeserialized);
                NativeArray<byte> nativeBytesArray = new NativeArray<byte>(
                   snapshotBytesRead.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                for (int i = 0; i < snapshotBytesRead.Length; i++)
                {
                    nativeBytesArray[i] = snapshotBytesRead[i];
                }
                DeserializeSnapshotTimestamp(nativeBytesArray, out double readTimestamp);

                return readTimestamp;
            }

            /// <summary>
            /// Obtains the bytes represented by an initial and final snapshot range,
            /// inclusive.
            /// </summary>
            /// <param name="firstSnapshotIndex">First snapshot of range.</param>
            /// <param name="lastSnapshotIndex">Last snapshot of range.</param>
            /// <param name="snapshotToByteoffsetAfterHeader">Snapshot to byte offset
            /// table, useful for associating snapshot indices to byte offsets.</param>
            /// <returns>Byte array representing range of snapshots, if available.</returns>
            public byte[] GetSnapshotRangeBytes(
                int firstSnapshotIndex,
                int lastSnapshotIndex,
                int[] snapshotToByteoffsetAfterHeader)
            {
                if (firstSnapshotIndex > lastSnapshotIndex ||
                    firstSnapshotIndex < 0 || lastSnapshotIndex < 0 ||
                    firstSnapshotIndex >= snapshotToByteoffsetAfterHeader.Length ||
                    lastSnapshotIndex >= snapshotToByteoffsetAfterHeader.Length)
                {
                    Debug.LogError("Cannot get snapshot bytes due invalid range. Please " +
                        $"make sure it matches ({0}-{snapshotToByteoffsetAfterHeader.Length - 1})");
                    return null;
                }

                // Gather information about snapshot offset and sizes necessary.
                int firstSnapshotOffset = snapshotToByteoffsetAfterHeader[firstSnapshotIndex];
                int lastSnapshotOffset = snapshotToByteoffsetAfterHeader[lastSnapshotIndex];
                _stream.Seek(SERIALIZATION_START_HEADER_SIZE_BYTES
                    + lastSnapshotOffset, SeekOrigin.Begin);
                // include last snapshot size, as well as its header size
                int lastSnapshotSize = _reader.ReadInt32() + _SNAPSHOT_HEADER_SIZE_BYTES;

                // Alocate snapshot bytes.
                Assert.IsTrue(firstSnapshotOffset <= lastSnapshotOffset,
                    "First snapshot offset must be smaller than or equal to the last offset.");
                int sizeToAllocate = (lastSnapshotOffset - firstSnapshotOffset) +
                    lastSnapshotSize;
                byte[] bytesToGrab = new byte[sizeToAllocate];

                // Grab the data from the stream.
                _stream.Seek(SERIALIZATION_START_HEADER_SIZE_BYTES
                    + firstSnapshotOffset, SeekOrigin.Begin);
                bytesToGrab = _reader.ReadBytes(sizeToAllocate);

                return bytesToGrab;
            }
        }

        /// <summary>
        /// Opens a file for playback.
        /// </summary>
        /// <param name="startHeader">Start header to create.</param>
        /// <param name="endHeader">End header to create.</param>
        /// <param name="snapshotToByteOffsetAfterHeader">Snapshot to byte offset after start header.</param>
        /// <param name="playbackPath">Optional path.</param>
        /// <returns></returns>
        public static ReaderFileStream OpenFileForPlayback(
            ref StartHeader startHeader, ref EndHeader endHeader,
            ref int[] snapshotToByteOffsetAfterHeader,
            string playbackPath = null)
        {
            var playbackFile = playbackPath != null ? playbackPath : string.Empty;
#if UNITY_EDITOR
            if (String.IsNullOrEmpty(playbackFile))
            {
                playbackFile = EditorUtility.OpenFilePanel("Select Recording for Playback", "", "sbn");
            }
#endif
            if (string.IsNullOrEmpty(playbackFile))
            {
                Debug.LogError("No playback file selected, bailing.");
                return null;
            }

            ReaderFileStream readerFileStream = null;
            try
            {
                var fileStream = new FileStream(playbackFile, FileMode.Open, FileAccess.Read, FileShare.None);
                var reader = new BinaryReader(fileStream);
                readerFileStream = new ReaderFileStream(fileStream, reader);
                readerFileStream.ParseHeadersAndOffsets(
                    ref startHeader,
                    ref endHeader,
                    ref snapshotToByteOffsetAfterHeader);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Caught exception while trying to read recording: {exception}");
                // can't playback here, probably need to not play anything
                return null;
            }
            return readerFileStream;
        }

        /// <summary>
        /// Reads snapshot at a byte offset.
        /// </summary>
        /// <param name="readFileStream">File stream to use.</param>
        /// <param name="currentSnapshotByteOffset">Current byte offset.</param>
        /// <param name="snapshotIndex">Snapshot index.</param>
        /// <param name="snapshotsSinceLastSync">Snapshots since last sync for the snapshot read.</param>
        /// <param name="numSnapshotsTotal">Number of snapshots total.</param>
        /// <returns></returns>
        public static byte[] ReadSnapshotAtOffset(
            ReaderFileStream readFileStream,
            ref int currentSnapshotByteOffset,
            int snapshotIndex,
            ref int snapshotsSinceLastSync,
            int numSnapshotsTotal)
        {
            byte[] snapshotBytes = null;
            try
            {
                if (snapshotIndex < 0 || numSnapshotsTotal == snapshotIndex)
                {
                    Debug.LogWarning($"Invalid index of {snapshotIndex}; " +
                        $"valid range is only (0, {numSnapshotsTotal - 1}).");
                    return null;
                }
                readFileStream.ReadSnapshotData(
                    currentSnapshotByteOffset,
                    out snapshotsSinceLastSync,
                    out snapshotBytes,
                    out int numBytesDeserialized);
                currentSnapshotByteOffset += numBytesDeserialized;
            }
            catch (Exception exception)
            {
                Debug.LogError($"Caught exception while trying to read snapshot: {exception}");
                return null;
            }
            return snapshotBytes;
        }
    }
}
