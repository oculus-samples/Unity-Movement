// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using Meta.XR.Movement.Retargeting;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

namespace Meta.XR.Movement.Samples
{
    /// <summary>
    /// A simple script that allows toggling an animation on and off.
    /// </summary>
    public class MovementBodyAnimationToggle : MonoBehaviour
    {
        /// <summary>
        /// Animators to control.
        /// </summary>
        [SerializeField]
        private Animator[] _animators;

        [SerializeField]
        private CharacterRetargeter[] _retargeters;

        /// <summary>
        /// True if animation is enabled, false is not.
        /// </summary>
        [SerializeField]
        private bool _customAnimEnabled;

        /// <summary>
        /// Text to update to based on animation state.
        /// </summary>
        [SerializeField]
        private TMP_Text _worldText;

        [SerializeField, InspectorButton("SwapAnimState")]
        private bool _toggleButton;

        private const string _animOffText = "Anim off";
        private const string _animOnText = "Anim on";
        private AnimationSkeletalProcessor[] _processors;

        private void Awake()
        {
            Assert.IsNotNull(_worldText);
            Assert.IsTrue(_animators is { Length: > 0 });
            foreach (var animator in _animators)
            {
                Assert.IsNotNull(animator);
            }

            var processorIndex = 0;
            _processors = new AnimationSkeletalProcessor[_retargeters.Length];
            foreach (var retargeter in _retargeters)
            {
                _processors[processorIndex] = retargeter.GetTargetProcessor<AnimationSkeletalProcessor>();
                processorIndex++;
            }
        }

        private void Start()
        {
            EnforceAnimState();
        }

        /// <summary>
        /// Swaps the custom animation state on/off.
        /// </summary>
        public void SwapAnimState()
        {
            _customAnimEnabled = !_customAnimEnabled;
            EnforceAnimState();
        }

        private void EnforceAnimState()
        {
            foreach (var animator in _animators)
            {
                animator.enabled = _customAnimEnabled;
            }

            foreach (var processor in _processors)
            {
                processor.Weight = _customAnimEnabled ? 1.0f : 0.0f;
            }

            _worldText.text = _customAnimEnabled ? _animOnText : _animOffText;
        }
    }
}
