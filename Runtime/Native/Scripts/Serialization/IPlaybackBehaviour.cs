// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

namespace Meta.XR.Movement.Playback
{
    /// <summary>
    /// Interface for playback behaviors. This would respond to UI events,
    /// and control the playback as a result.
    /// </summary>
    public interface IPlaybackBehaviour
    {
        /// <summary>
        /// Indicates if has opened file for playback or not.
        /// </summary>
        bool HasOpenedFileForPlayback { get; }

        /// <summary>
        /// Indicates if the user is actively scrubbing.
        /// </summary>
        bool UserActivelyScrubbing { get; set; }

        /// <summary>
        /// Returns the current snapshot index.
        /// </summary>
        int SnapshotIndex { get; }

        /// <summary>
        /// Returns the number of snapshots.
        /// </summary>
        int NumSnapshots { get; }

        /// <summary>
        /// Seeks to a new index and indicates if the
        /// seek was successful or not.
        /// </summary>
        /// <param name="newSeekIndex">New seek index.</param>
        /// <returns>Indicates if the seek is successful or not.</returns>
        bool Seek(int newSeekIndex);

        /// <summary>
        /// Closes the playback file.
        /// </summary>
        void ClosePlaybackFile();

        /// <summary>
        /// Plays back a recording. It's up to the implementing class
        /// to either preload a file or ask the user for one.
        /// </summary>
        /// <param name="playbackPath">Playback path (optional).</param>
        /// <returns>True if recording was loaded; false if not.</returns>
        bool PlayBackRecording(string playbackPath = null);

        /// <summary>
        /// Sets the new pause state.
        /// </summary>
        /// <param name="pauseState">The new pause state.</param>
        void SetPauseState(bool pauseState);

        /// <summary>
        /// Bandwidth of playback in kilobits per second.
        /// </summary>
        float BandwidthKbps { get; }
    }
}
