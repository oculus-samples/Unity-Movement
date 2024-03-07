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
        /// The rest pose humanoid object.
        /// </summary>
        [SerializeField]
        [Tooltip(RetargetingMenuTooltips.RestPoseObject)]
        protected RestPoseObjectHumanoid _restPoseObject;

        /// <summary>
        /// The rest T-pose humanoid object.
        /// </summary>
        [SerializeField]
        [Tooltip(RetargetingMenuTooltips.RestTPoseObject)]
        protected RestPoseObjectHumanoid _restTPoseObject;

        /// <summary>
        /// Offset per spawn.
        /// </summary>
        [SerializeField]
        [Tooltip(RetargetingMenuTooltips.SpawnOffset)]
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
        private List<SpawnMetadata> _charactersSpawned = new List<SpawnMetadata>();

        /// <summary>
        /// Move the non-mirrored characters out further so that they don't intersect any menus to the
        /// right of the user.
        /// </summary>
        private const float _NON_MIRRORED_OFFSET_MULTIPLIER = 1.5f;

        private void Awake()
        {
            Assert.IsNotNull(_characterToSpawn);
            _currentSpawnOffset = _spawnOffset;
            _currentSpawnOffsetNotParented = -_spawnOffset * _NON_MIRRORED_OFFSET_MULTIPLIER;
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
            AddComponentsRuntime.SetupCharacterForRetargeting(newCharacter, true);

            AdjustSpawnedCharacterTransform(newCharacter, true, _currentSpawnOffset);
            _currentSpawnOffset += _spawnOffset;
            _charactersSpawned.Add(new SpawnMetadata(true, newCharacter));
        }

        /// <summary>
        /// Spawn retargeted character with animation rigging support.
        /// </summary>
        public void AddRiggedRetargetedCharacter()
        {
            if (_charactersSpawned.Count >= _characterSpawnLimit)
            {
                Debug.LogWarning("Reached the limit of characters to spawn.");
                return;
            }
            GameObject newCharacter = Instantiate(_characterToSpawn);
            AddComponentsRuntime.SetupCharacterForAnimationRiggingRetargeting(newCharacter, true, true);

            AdjustSpawnedCharacterTransform(newCharacter, true, _currentSpawnOffset);
            _currentSpawnOffset += _spawnOffset;
            _charactersSpawned.Add(new SpawnMetadata(true, newCharacter));
        }

        /// <summary>
        /// Spawn retargeted character with animation rigging support, along with constraints.
        /// </summary>
        public void AddRiggedRetargetedCharacterWithConstraints()
        {
            if (_charactersSpawned.Count >= _characterSpawnLimit)
            {
                Debug.LogWarning("Reached the limit of characters to spawn.");
                return;
            }
            GameObject newCharacter = Instantiate(_characterToSpawn);
            RestPoseObjectHumanoid restPoseToUse = CheckIfTPose(newCharacter.GetComponent<Animator>()) ?
                _restTPoseObject :
                _restPoseObject;
            AddComponentsRuntime.SetupCharacterForAnimationRiggingRetargeting(newCharacter, true, true, _restPoseObject);

            AdjustSpawnedCharacterTransform(newCharacter, false, -_currentSpawnOffsetNotParented);
            _currentSpawnOffsetNotParented -= _spawnOffset * _NON_MIRRORED_OFFSET_MULTIPLIER;
            _charactersSpawned.Add(new SpawnMetadata(false, newCharacter));
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
                _currentSpawnOffsetNotParented += _spawnOffset * _NON_MIRRORED_OFFSET_MULTIPLIER;
            }
        }

        private void AdjustSpawnedCharacterTransform(GameObject newCharacter,
            bool reparent, Vector3 offsetToUse)
        {
            var characterTransform = newCharacter.transform;
            if (_spawnParent != null && reparent)
            {
                characterTransform.SetParent(_spawnParent, false);
            }
            characterTransform.localPosition = offsetToUse;
        }

        /// <summary>
        /// Given an animator, determine if the avatar is in T-pose or A-pose.
        /// </summary>
        /// <param name="animator">The animator.</param>
        /// <param name="matchThreshold">The dot product threshold to match for poses.</param>
        /// <returns>True if T-pose.</returns>
        private bool CheckIfTPose(Animator animator, float matchThreshold = 0.95f)
        {
            var shoulder = animator.GetBoneTransform(HumanBodyBones.LeftShoulder);
            var upperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            var lowerArm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            var hand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            if (shoulder == null)
            {
                // Naive approach to check if the lowerArm is placed in A-pose or not when
                // missing a shoulder bone.
                return upperArm.position.y - lowerArm.position.y < matchThreshold / 10.0f;
            }
            var shoulderToUpperArm = (shoulder.position - upperArm.position).normalized;
            var lowerArmToHand = (lowerArm.position - hand.position).normalized;
            var armDirectionMatch = Vector3.Dot(shoulderToUpperArm, lowerArmToHand);
            return armDirectionMatch >= matchThreshold;
        }
    }
}
