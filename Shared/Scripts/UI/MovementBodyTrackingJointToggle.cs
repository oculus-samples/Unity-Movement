// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using static OVRPlugin;

namespace Meta.XR.Movement.Samples
{
    /// <summary>
    /// Allows toggling body tracking joint set.
    /// </summary>
    public class MovementBodyTrackingJointToggle : MonoBehaviour
    {
        /// <summary>
        /// The text to update after body tracking fidelity is changed.
        /// </summary>
        [SerializeField]
        protected TMP_Text _worldText;

        /// <summary>
        /// The OVRBody that dictates the starting body tracking joint set.
        /// </summary>
        [SerializeField]
        protected OVRBody _ovrBody;

        [SerializeField, InspectorButton("SwapJointSet")]
        private bool _toggleButton;

        private const string _fullBody = "Full Body";
        private const string _upperBody = "Upper Body";

        private void Awake()
        {
            Assert.IsNotNull(_worldText);
        }

        private void Start()
        {
            UpdateText();
        }

        /// <summary>
        /// Changes the body joint set from full body to upper body or vice versa.
        /// </summary>
        public void SwapJointSet()
        {
            var desiredSkeletonType = _ovrBody.ProvidedSkeletonType == BodyJointSet.FullBody
                ? BodyJointSet.UpperBody
                : BodyJointSet.FullBody;
            var ovrBodies = FindObjectsByType<OVRBody>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var ovrBody in ovrBodies)
            {
                ovrBody.ProvidedSkeletonType = desiredSkeletonType;
            }
            UpdateText();
        }

        private void UpdateText()
        {
            _worldText.text = _ovrBody.ProvidedSkeletonType == BodyJointSet.FullBody ?
                _fullBody : _upperBody;
        }
    }
}
