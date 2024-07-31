// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Movement.AnimationRigging;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Movement.UI
{
    /// <summary>
    /// A simple script that allows toggling an animation on and off.
    /// </summary>
    public class CustomAnimToggle : MonoBehaviour
    {
        /// <summary>
        /// Animation clip to play.
        /// </summary>
        [SerializeField]
        private AnimationClip _animClip;
        /// <summary>
        /// Mask to apply.
        /// </summary>
        [SerializeField]
        private AvatarMask _customMask;
        /// <summary>
        /// Retargeting constraints to fix based on animation state.
        /// </summary>
        [SerializeField]
        private RetargetingAnimationConstraint[] _retargetingConstraints;
        /// <summary>
        /// Animators to control.
        /// </summary>
        [SerializeField]
        private Animator[] _animators;
        /// <summary>
        /// True if animation is enabled, false is not.
        /// </summary>
        [SerializeField]
        private bool _customAnimEnabled = false;
        /// <summary>
        /// Text to update to based on animation state.
        /// </summary>
        [SerializeField]
        private TMPro.TextMeshPro _worldText;
        /// <summary>
        /// Animator parameter name.
        /// </summary>
        [SerializeField]
        private string _animParamName = "Wave";

        private const string _ANIM_OFF_TEXT = "Anim off";
        private const string _ANIM_ON_TEXT = "Anim on";

        private void Awake()
        {
            Assert.IsNotNull(_animClip);
            Assert.IsNotNull(_customMask);
            Assert.IsTrue(_retargetingConstraints != null && _retargetingConstraints.Length > 0);
            Assert.IsTrue(_animators != null && _animators.Length > 0);
            Assert.IsNotNull(_worldText);
        }

        private void Start()
        {
            EnforceAnimState();
        }

        private void Update()
        {
            // since the animation rig set up might reboot due to calibration
            // keep setting parameter to the proper value.
            foreach (var animator in _animators)
            {
                animator.SetBool(_animParamName, _customAnimEnabled);
            }
        }

        public void SwapAnimState()
        {
            _customAnimEnabled = !_customAnimEnabled;
            EnforceAnimState();
        }

        private void EnforceAnimState()
        {
            foreach (var retargetConstraint in _retargetingConstraints)
            {
                retargetConstraint.data.AvatarMaskComp =
                    _customAnimEnabled ? _customMask : null;
                retargetConstraint.data.UpdateDataArraysWithAdjustments();
            }
            foreach (var animator in _animators)
            {
                animator.SetBool(_animParamName, _customAnimEnabled);
            }
            _worldText.text = _customAnimEnabled ?
                _ANIM_ON_TEXT : _ANIM_OFF_TEXT;
        }
    }
}
