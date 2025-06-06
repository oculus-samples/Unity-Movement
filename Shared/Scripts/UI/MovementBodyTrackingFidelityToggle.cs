// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using static OVRPlugin;

namespace Meta.XR.Movement.Samples
{
    /// <summary>
    /// Allows toggling body tracking fidelity.
    /// </summary>
    public class MovementBodyTrackingFidelityToggle : MonoBehaviour
    {
        /// <summary>
        /// The text to update after body tracking fidelity is changed.
        /// </summary>
        [SerializeField]
        protected TMP_Text _worldText;

        [SerializeField, InspectorButton("SwapFidelity")]
        private bool _toggleButton;

        private const string _lowFidelity = "Three-point BT";
        private const string _highFidelity = "IOBT";

        private void Awake()
        {
            Assert.IsNotNull(_worldText);
        }

        private void Start()
        {
            UpdateText();
        }

        /// <summary>
        /// Changes the body tracking fidelity from low to high or vice versa.
        /// </summary>
        public void SwapFidelity()
        {
            var desiredFidelity = OVRBody.Fidelity == BodyTrackingFidelity2.Low
                ? BodyTrackingFidelity2.High
                : BodyTrackingFidelity2.Low;
            OVRBody.Fidelity = desiredFidelity;
            UpdateText();
        }

        private void UpdateText()
        {
            _worldText.text = OVRBody.Fidelity == BodyTrackingFidelity2.Low ? _lowFidelity : _highFidelity;
        }
    }
}
