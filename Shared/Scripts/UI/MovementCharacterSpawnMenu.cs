// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Meta.XR.Movement.Samples
{
    /// <summary>
    /// Menu that allows spawning characters.
    /// </summary>
    public class MovementCharacterSpawnMenu : MonoBehaviour
    {
        /// <summary>
        /// Stylized character prefab to spawn.
        /// </summary>
        [SerializeField]
        protected GameObject _stylizedCharacterToSpawn;

        /// <summary>
        /// High fidelity character prefab to spawn.
        /// </summary>
        [SerializeField]
        protected GameObject _highFidelityCharacterToSpawn;

        /// <summary>
        /// Parent to spawn under.
        /// </summary>
        [SerializeField]
        protected Transform _spawnParent;

        /// <summary>
        /// Offset per spawn.
        /// </summary>
        [SerializeField]
        protected Vector3 _spawnOffset = new Vector3(-1.0f, 0.0f, 0.0f);

        private class SpawnMetadata
        {
            /// <summary>
            /// Constructor for spawned character.
            /// </summary>
            /// <param name="isParentedCharacter">Is parented character.</param>
            /// <param name="spawnedObject">Spawned GameObject.</param>
            public SpawnMetadata(bool isParentedCharacter,
                GameObject spawnedObject)
            {
                IsParentedCharacter = isParentedCharacter;
                SpawnedObject = spawnedObject;
            }

            /// <summary>
            /// Is parented to a transform or not.
            /// </summary>
            public readonly bool IsParentedCharacter;

            /// <summary>
            /// Actual spawned character.
            /// </summary>
            public readonly GameObject SpawnedObject;
        }

        private const int _characterSpawnLimit = 20;
        private Vector3 _currentSpawnOffset, _currentSpawnOffsetNotParented;
        private readonly List<SpawnMetadata> _charactersSpawned = new();

        /// <summary>
        /// Move the non-mirrored characters out further so that they don't intersect any menus to the
        /// right of the user.
        /// </summary>
        private const float _nonMirroredOffsetMultiplier = 1.5f;

        private void Awake()
        {
            Assert.IsNotNull(_stylizedCharacterToSpawn);
            _currentSpawnOffset = _spawnOffset;
            _currentSpawnOffsetNotParented = -_spawnOffset * _nonMirroredOffsetMultiplier;
        }

        /// <summary>
        /// Add a stylized character to the scene.
        /// </summary>
        public void AddStylizedCharacter()
        {
            if (_charactersSpawned.Count >= _characterSpawnLimit)
            {
                Debug.LogWarning("Reached the limit of characters to spawn.");
                return;
            }
            GameObject newCharacter = Instantiate(_stylizedCharacterToSpawn);
            AdjustSpawnedCharacterTransform(newCharacter, true, _currentSpawnOffset);
            _currentSpawnOffset += _spawnOffset;
            _charactersSpawned.Add(new SpawnMetadata(true, newCharacter));
        }

        /// <summary>
        /// Add a high fidelity character to the scene.
        /// </summary>
        public void AddHighFidelityCharacter()
        {
            if (_charactersSpawned.Count >= _characterSpawnLimit)
            {
                Debug.LogWarning("Reached the limit of characters to spawn.");
                return;
            }
            GameObject newCharacter = Instantiate(_highFidelityCharacterToSpawn);
            AdjustSpawnedCharacterTransform(newCharacter, true, _currentSpawnOffset);
            _currentSpawnOffset += _spawnOffset;
            _charactersSpawned.Add(new SpawnMetadata(true, newCharacter));
        }

        /// <summary>
        /// Removes last character spawned.
        /// </summary>
        public void RemoveLastCharacter()
        {
            if (_charactersSpawned.Count == 0)
            {
                return;
            }
            int lastCharacterIndex = _charactersSpawned.Count - 1;
            var isLastCharacterParented = _charactersSpawned[lastCharacterIndex].IsParentedCharacter;
            Destroy(_charactersSpawned[lastCharacterIndex].SpawnedObject);
            _charactersSpawned.RemoveAt(lastCharacterIndex);
            if (isLastCharacterParented)
            {
                _currentSpawnOffset -= _spawnOffset;
            }
            else
            {
                _currentSpawnOffsetNotParented += _spawnOffset * _nonMirroredOffsetMultiplier;
            }
        }

        private void AdjustSpawnedCharacterTransform(GameObject newCharacter, bool reparent, Vector3 offsetToUse)
        {
            var characterTransform = newCharacter.transform;
            if (_spawnParent != null && reparent)
            {
                characterTransform.SetParent(_spawnParent, false);
            }
            characterTransform.localPosition = offsetToUse;
        }
    }
}
