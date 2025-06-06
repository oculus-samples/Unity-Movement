// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using UnityEngine;

namespace Meta.XR.Movement.Networking
{
    /// <summary>
    /// Interface that should be implemented for spawning networked retargeters
    /// using a specific networking framework.
    /// </summary>
    public interface INetworkCharacterSpawner
    {
        /// <summary>
        /// The index of the character that is selected and should be spawned.
        /// </summary>
        public int SelectedCharacterIndex { get; set; }

        /// <summary>
        /// The array of character prefabs that possibly could be spawned.
        /// </summary>
        public GameObject[] CharacterRetargeterPrefabs { get; set; }
    }
}
