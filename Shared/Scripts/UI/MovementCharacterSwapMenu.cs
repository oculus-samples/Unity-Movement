// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

namespace Meta.XR.Movement.Samples
{
    /// <summary>
    /// Allows usage of a toggle icon.
    /// </summary>
    public class MovementCharacterSwapMenu : MonoBehaviour
    {
        /// <summary>
        /// Characters to swap between.
        /// </summary>
        [SerializeField]
        protected GameObject[] _characters;

        [SerializeField]
        protected GameObject[] _mirroredCharacters;

        [SerializeField]
        protected TMP_Text _currentCharacterName;

        [SerializeField, InspectorButton("SwapToNextCharacter")]
        protected bool _toggleButton;

        private int _currentCharacterIndex;

        private void Awake()
        {
            Assert.IsNotNull(_currentCharacterName, "Character name display is unassigned.");
            Assert.IsTrue(_characters is { Length: > 0 }, "Characters are empty.");
            Assert.IsTrue(_mirroredCharacters is { Length: > 0 }, "Mirrored characters are empty.");
            Assert.IsTrue(_characters.Length == _mirroredCharacters.Length, "Character lists should match in size.");
        }

        private void Start()
        {
            EnforceCharacter();
        }

        public void SwapToNextCharacter()
        {
            _currentCharacterIndex++;
            if (_currentCharacterIndex >= _characters.Length)
            {
                _currentCharacterIndex = 0;
            }

            EnforceCharacter();
        }

        private void EnforceCharacter()
        {
            foreach (var character in _characters)
            {
                character.gameObject.SetActive(false);
            }

            foreach (var character in _mirroredCharacters)
            {
                character.gameObject.SetActive(false);
            }

            var targetCharacter = _characters[_currentCharacterIndex];
            var targetMirrorCharacter = _mirroredCharacters[_currentCharacterIndex];
            targetCharacter.SetActive(true);
            targetMirrorCharacter.SetActive(true);
            _currentCharacterName.text = targetCharacter.name.Replace("Character", "");
        }
    }
}
