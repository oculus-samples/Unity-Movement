// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using Meta.XR.Movement.Retargeting;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using static Meta.XR.Movement.MSDKUtility;

namespace Meta.XR.Movement.Samples
{
    /// <summary>
    /// Allows toggling between different retargeting behaviors.
    /// </summary>
    public class MovementRetargetingBehaviorMenu : MonoBehaviour
    {
        [SerializeField]
        private CharacterRetargeter[] _retargeters;

        /// <summary>
        /// The text to update based on the current retargeting behavior.
        /// </summary>
        [SerializeField]
        protected TMP_Text _worldText;

        [SerializeField, InspectorButton("CycleRetargetingBehavior")]
        private bool _cycleButton;

        [SerializeField, InspectorButton("SetSourceBodyProportions")]
        private bool _sourceBodyProportionsButton;

        [SerializeField, InspectorButton("SetSourceBodyProportionsTargetHands")]
        private bool _sourceBodyProportionsTargetHandsButton;

        [SerializeField, InspectorButton("SetSourceBodyProportionsTargetScale")]
        private bool _sourceBodyProportionsTargetScaleButton;

        [SerializeField, InspectorButton("SetTargetBodyProportions")]
        private bool _targetBodyProportionsButton;

        private void Awake()
        {
            Assert.IsTrue(_retargeters is { Length: > 0 });
            foreach (var retargeter in _retargeters)
            {
                Assert.IsNotNull(retargeter);
            }
            Assert.IsNotNull(_worldText);
        }

        private void Start()
        {
            UpdateText();
        }

        /// <summary>
        /// Cycles through all available retargeting behaviors.
        /// </summary>
        public void CycleRetargetingBehavior()
        {
            foreach (var retargeter in _retargeters)
            {
                var currentBehavior = retargeter.SkeletonRetargeter.RetargetingBehavior;
                var nextBehavior = GetNextRetargetingBehavior(currentBehavior);
                SetRetargetingBehavior(retargeter, nextBehavior);
            }
            UpdateText();
        }

        /// <summary>
        /// Sets retargeting behavior to Source Body Proportions (RotationsAndPositions).
        /// </summary>
        public void SetSourceBodyProportions()
        {
            SetRetargetingBehaviorForAll(RetargetingBehavior.RotationsAndPositions);
            UpdateText();
        }

        /// <summary>
        /// Sets retargeting behavior to Source Body Proportions + Target Hand Proportions.
        /// </summary>
        public void SetSourceBodyProportionsTargetHands()
        {
            SetRetargetingBehaviorForAll(RetargetingBehavior.RotationsAndPositionsHandsRotationOnly);
            UpdateText();
        }

        /// <summary>
        /// Sets retargeting behavior to Source Body Proportions + Target Scale.
        /// </summary>
        public void SetSourceBodyProportionsTargetScale()
        {
            SetRetargetingBehaviorForAll(RetargetingBehavior.RotationAndPositionsUniformScale);
            UpdateText();
        }

        /// <summary>
        /// Sets retargeting behavior to Target Body Proportions (RotationOnlyNoScaling).
        /// </summary>
        public void SetTargetBodyProportions()
        {
            SetRetargetingBehaviorForAll(RetargetingBehavior.RotationOnlyNoScaling);
            UpdateText();
        }

        /// <summary>
        /// Gets the current retargeting behavior as a readable string.
        /// </summary>
        /// <returns>Human-readable string describing the current retargeting behavior.</returns>
        public string GetCurrentBehaviorName()
        {
            if (_retargeters == null || _retargeters.Length == 0)
                return "No Retargeters";

            var behavior = _retargeters[0].SkeletonRetargeter.RetargetingBehavior;
            return GetRetargetingBehaviorName(behavior);
        }

        private void SetRetargetingBehaviorForAll(RetargetingBehavior behavior)
        {
            foreach (var retargeter in _retargeters)
            {
                SetRetargetingBehavior(retargeter, behavior);
            }
        }

        private void SetRetargetingBehavior(CharacterRetargeter retargeter, RetargetingBehavior behavior)
        {
            // We need to recreate the retargeter with the new behavior
            // This is because the retargeting behavior is set during setup
            var config = retargeter.Config;
            retargeter.SkeletonRetargeter.Dispose();
            retargeter.SkeletonRetargeter = new SkeletonRetargeter();

            // Access the private field using reflection to set the behavior
            var field = typeof(SkeletonRetargeter).GetField("_retargetingBehavior",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(retargeter.SkeletonRetargeter, behavior);

            retargeter.Setup(config);
        }

        private RetargetingBehavior GetNextRetargetingBehavior(RetargetingBehavior current)
        {
            return current switch
            {
                RetargetingBehavior.RotationsAndPositions => RetargetingBehavior.RotationsAndPositionsHandsRotationOnly,
                RetargetingBehavior.RotationsAndPositionsHandsRotationOnly => RetargetingBehavior.RotationAndPositionsUniformScale,
                RetargetingBehavior.RotationAndPositionsUniformScale => RetargetingBehavior.RotationOnlyNoScaling,
                RetargetingBehavior.RotationOnlyNoScaling => RetargetingBehavior.RotationsAndPositions,
                _ => RetargetingBehavior.RotationsAndPositions
            };
        }

        private string GetRetargetingBehaviorName(RetargetingBehavior behavior)
        {
            return behavior switch
            {
                RetargetingBehavior.RotationsAndPositions => "Source Body Proportions",
                RetargetingBehavior.RotationsAndPositionsHandsRotationOnly => "Source Body + Target Hands",
                RetargetingBehavior.RotationAndPositionsUniformScale => "Source Body + Target Scale",
                RetargetingBehavior.RotationOnlyNoScaling => "Target Body Proportions",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Updates the text display with the current retargeting behavior.
        /// </summary>
        private void UpdateText()
        {
            if (_worldText != null)
            {
                _worldText.text = GetCurrentBehaviorName();
            }
        }
    }
}
