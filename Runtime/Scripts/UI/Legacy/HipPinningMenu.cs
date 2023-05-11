// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Movement.Effects.Deprecated;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Movement.UI.Deprecated
{
    /// <summary>
    /// Menu for toggling different options for hip pinning
    /// </summary>
    public class HipPinningMenu : MonoBehaviour
    {
        /// <summary>
        /// Information about a body part that is toggleable.
        /// </summary>
        [System.Serializable]
        private class BodyPart
        {
            /// <summary>
            /// The body part object.
            /// </summary>
            [Tooltip(HipPinningMenuTooltips.BodyPartTooltips.BodyPartObject)]
            public GameObject BodyPartObject;

            /// <summary>
            /// The name of the body part.
            /// </summary>
            [Tooltip(HipPinningMenuTooltips.BodyPartTooltips.BodyPartName)]
            public string BodyPartName;

            /// <summary>
            /// If true, the body part is active and visible.
            /// </summary>
            [Tooltip(HipPinningMenuTooltips.BodyPartTooltips.Enabled)]
            public bool Enabled;
        }

        /// <summary>
        /// Main character driven by user.
        /// </summary>
        [SerializeField]
        [Tooltip(HipPinningMenuTooltips.MainCharacter)]
        private HipPinningLogic _mainCharacter;

        /// <summary>
        /// Mirrored character.
        /// </summary>
        [SerializeField]
        [Tooltip(HipPinningMenuTooltips.MirroredCharacter)]
        private HipPinningLogic _mirroredCharacter;

        /// <summary>
        /// Informational text.
        /// </summary>
        [SerializeField]
        [Tooltip(HipPinningMenuTooltips.Text)]
        private TextMeshPro _text;

        /// <summary>
        /// Body parts that can be toggled.
        /// </summary>
        [SerializeField]
        [Tooltip(HipPinningMenuTooltips.BodyParts)]
        private BodyPart[] _bodyParts;

        private bool _movementEnabled;
        private bool _legRotationEnabled;
        private bool _hipPinningEnabled;
        private bool _transformationEnabled;
        private bool _detectionEnabled;

        private void Awake()
        {
            Assert.IsNotNull(_mainCharacter);
            Assert.IsNotNull(_mirroredCharacter);
            Assert.IsNotNull(_text);
            _movementEnabled = _mirroredCharacter.EnableConstrainedMovement;
            _legRotationEnabled = _mirroredCharacter.EnableLegRotation;
            _transformationEnabled = _mirroredCharacter.EnableApplyTransformations;
            _hipPinningEnabled = true;
            _detectionEnabled = true;
            foreach (var bodyPart in _bodyParts)
            {
                bodyPart.BodyPartObject.SetActive(bodyPart.Enabled);
            }
            UpdateDisplayText();
            ToggleHipPinningMenu();
        }

        /// <summary>
        /// Toggle the visibility of the hip pinning options window
        /// </summary>
        public void ToggleHipPinningMenu()
        {
            gameObject.SetActive(!gameObject.activeSelf);
        }

        /// <summary>
        /// Toggle constrained movement for the hip pinned user
        /// </summary>
        public void ToggleMovement()
        {
            _movementEnabled = !_movementEnabled;
            _mainCharacter.EnableConstrainedMovement = _movementEnabled;
            _mirroredCharacter.EnableConstrainedMovement = _movementEnabled;
            UpdateDisplayText();
        }

        /// <summary>
        /// Toggle leg rotation for the hip pinned user
        /// </summary>
        public void ToggleLegRotation()
        {
            _legRotationEnabled = !_legRotationEnabled;
            _mainCharacter.EnableLegRotation = _legRotationEnabled;
            _mirroredCharacter.EnableLegRotation = _legRotationEnabled;
            UpdateDisplayText();
        }

        /// <summary>
        /// Toggle hip pinning for the hip pinned user
        /// </summary>
        public void ToggleHipPinning()
        {
            _hipPinningEnabled = !_hipPinningEnabled;
            _mainCharacter.SetHipPinningActive(_hipPinningEnabled);
            _mirroredCharacter.SetHipPinningActive(_hipPinningEnabled);
            UpdateDisplayText();
        }

        /// <summary>
        /// Toggle transformation for the hip pinned user
        /// </summary>
        public void ToggleTransformation()
        {
            _transformationEnabled = !_transformationEnabled;
            _mirroredCharacter.EnableApplyTransformations = _transformationEnabled;
            UpdateDisplayText();
        }

        /// <summary>
        /// Toggle hip pinning detection for the hip pinned user
        /// </summary>
        public void ToggleHipPinningDetection()
        {
            _detectionEnabled = !_detectionEnabled;
            _mirroredCharacter.EnableHipPinningLeave = _detectionEnabled;
            UpdateDisplayText();
        }

        /// <summary>
        /// Toggle visibility of a body part for the hip pinned user
        /// </summary>
        public void ToggleBodyPart(int index)
        {
            _bodyParts[index].Enabled = !_bodyParts[index].Enabled;
            _bodyParts[index].BodyPartObject.SetActive(_bodyParts[index].Enabled);
            UpdateDisplayText();
        }

        private void UpdateDisplayText()
        {
            var bodyText = "Hands";
            foreach (var bodyPart in _bodyParts)
            {
                if (bodyPart.BodyPartObject != null && bodyPart.Enabled)
                {
                    bodyText += "+ " + bodyPart.BodyPartName + " ";
                }
            }
            _text.text = string.Format(
                "Movement: {0}\nLeg Rotation: {1}\nHip Pinning: {2}\nTransformation: {3}\nHip Pinning Detection: {4}\nBody: {5}\n",
                _movementEnabled ? "On" : "Off",
                _legRotationEnabled ? "On" : "Off",
                _hipPinningEnabled ? "On" : "Off",
                _detectionEnabled ? "On" : "Off",
                _transformationEnabled ? "On" : "Off",
                bodyText);
        }
    }
}
