// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction;
using Oculus.Movement.Utils;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Movement.UI
{
    /// <summary>
    /// Menu that allows spawning retargeted characters.
    /// </summary>
    public class RetargetingMenu : MonoBehaviour
    {
        /// <summary>
        /// Main character prefab to spawn.
        /// </summary>
        [SerializeField]
        [Tooltip(RetargetingMenuTooltips.CharacterToSpawn)]
        protected GameObject _characterToSpawn;

        /// <summary>
        /// Parent to spawn under.
        /// </summary>
        [SerializeField, Optional]
        [Tooltip(RetargetingMenuTooltips.SpawnParent)]
        protected Transform _spawnParent;

        /// <summary>
        /// Offset per spawn.
        /// </summary>
        [SerializeField]
        [Tooltip(RetargetingMenuTooltips.SpawnOffset)]
        protected Vector3 _spawnOffset = new Vector3(-1.0f, 0.0f, 0.0f);

        /// <summary>
        /// Positions to correct mask, intended to correct the finger positions of
        /// animation rigged characters during retargeting.
        /// </summary>
        [SerializeField, Optional]
        [Tooltip(RetargetingMenuTooltips.PositionsToCorrectLateUpdateMask)]
        protected AvatarMask _positionsToCorrectLateUpdateMask;

        /// <summary>
        /// Positions to correct mask, intended to set certain joints of
        /// animation rigged characters to T-Pose during retargeting.
        /// </summary>
        [SerializeField, Optional]
        [Tooltip(RetargetingMenuTooltips.TPoseMask)]
        protected AvatarMask _tPoseMask;

        private const int _characterSpawnLimit = 20;
        private Vector3 _currentSpawnOffset;
        private List<GameObject> _charactersSpawned = new List<GameObject>();

        private void Awake()
        {
            Assert.IsNotNull(_characterToSpawn);
            _currentSpawnOffset = _spawnOffset;
        }

        /// <summary>
        /// Spawn retargeted character without animation rigging support.
        /// </summary>
        public void AddNormalRetargetedCharacter()
        {
            if (_charactersSpawned.Count >= _characterSpawnLimit)
            {
                Debug.LogWarning("Reached the limit of characters to spawn.");
                return;
            }
            GameObject newCharacter = Instantiate(_characterToSpawn);
            AddComponentsRuntime.SetupCharacterForRetargeting(newCharacter);

            AdjustSpawnedCharacterTransform(newCharacter);
            _currentSpawnOffset += _spawnOffset;
            _charactersSpawned.Add(newCharacter);
        }

        /// <summary>
        /// Spawn retargeted character with animation riggin gsupport.
        /// </summary>
        public void AddRiggedRetargetedCharacter()
        {
            if (_charactersSpawned.Count >= _characterSpawnLimit)
            {
                Debug.LogWarning("Reached the limit of characters to spawn.");
                return;
            }
            GameObject newCharacter = Instantiate(_characterToSpawn);
            AddComponentsRuntime.SetupCharacterForAnimationRiggingRetargeting(newCharacter, false,
                _positionsToCorrectLateUpdateMask, _tPoseMask);

            AdjustSpawnedCharacterTransform(newCharacter);
            _currentSpawnOffset += _spawnOffset;
            _charactersSpawned.Add(newCharacter);
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
            Destroy(_charactersSpawned[lastCharacterIndex]);
            _charactersSpawned.RemoveAt(lastCharacterIndex);
            _currentSpawnOffset -= _spawnOffset;
        }

        private void AdjustSpawnedCharacterTransform(GameObject newCharacter)
        {
            var characterTransform = newCharacter.transform;
            if (_spawnParent != null)
            {
                characterTransform.SetParent(_spawnParent, false);
            }
            characterTransform.localPosition = _currentSpawnOffset;
        }
    }
}
