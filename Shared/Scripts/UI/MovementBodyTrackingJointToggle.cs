// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using Meta.XR.Movement.Retargeting;
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

        /// <summary>
        /// The character retargeters.
        /// </summary>
        [SerializeField]
        protected CharacterRetargeter[] _retargeters;

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
                if (_retargeters == null)
                {
                    continue;
                }

                // Set retargeting behavior to avoid scaling when using half body
                if (desiredSkeletonType == BodyJointSet.FullBody)
                {
                    foreach (var retargeter in _retargeters)
                    {
                        retargeter.SkeletonRetargeter.RetargetingBehavior =
                            MSDKUtility.RetargetingBehavior.RotationsAndPositions;
                    }
                }
                else
                {
                    foreach (var retargeter in _retargeters)
                    {
                        retargeter.SkeletonRetargeter.RetargetingBehavior =
                            MSDKUtility.RetargetingBehavior.RotationAndPositionsUniformScale;
                    }
                }
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
