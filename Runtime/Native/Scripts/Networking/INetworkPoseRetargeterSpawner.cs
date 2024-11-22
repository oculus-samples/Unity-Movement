// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using UnityEngine;

namespace Meta.XR.Movement.Networking
{
    /// <summary>
    /// Interface that should be implemented for spawning networked pose retargeters
    /// using a specific networking framework.
    /// </summary>
    public interface INetworkPoseRetargeterSpawner
    {
        /// <summary>
        /// The index of the character that is selected and should be spawned.
        /// </summary>
        public int SelectedCharacterIndex { get; set; }

        /// <summary>
        /// The array of character prefabs that possibly could be spawned.
        /// </summary>
        public GameObject[] PoseRetargeterPrefabs { get; set; }
    }
}
