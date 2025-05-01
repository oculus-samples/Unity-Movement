// Copyright (c) Meta Platforms, Inc. and affiliates.

using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using static OVRPlugin;

namespace Meta.XR.Movement.Samples
{
    /// <summary>
    /// Allows setting body tracking fidelity.
    /// </summary>
    public class MovementBodyTrackingFidelityToggle : MonoBehaviour
    {
        /// <summary>
        /// The current body tracking fidelity set.
        /// </summary>
        [SerializeField]
        protected BodyTrackingFidelity2 _currentFidelity = BodyTrackingFidelity2.Low;

        /// <summary>
        /// The text to update after body tracking fidelity is changed.
        /// </summary>
        [SerializeField]
        protected TMP_Text _worldText;

        private const string _lowFidelity = "Three-point BT";
        private const string _highFidelity = "IOBT";

        private void Awake()
        {
            Assert.IsNotNull(_worldText);
        }

        private void Start()
        {
            EnforceFidelity();
        }

        /// <summary>
        /// Changes the body tracking fidelity from low to high or vice versa.
        /// </summary>
        public void SwapFidelity()
        {
            _currentFidelity = _currentFidelity == BodyTrackingFidelity2.Low ?
                BodyTrackingFidelity2.High : BodyTrackingFidelity2.Low;
            EnforceFidelity();
        }

        private void EnforceFidelity()
        {
            RequestBodyTrackingFidelity(_currentFidelity);
            _worldText.text = _currentFidelity == BodyTrackingFidelity2.Low ?
                _lowFidelity : _highFidelity;
        }
    }
}
