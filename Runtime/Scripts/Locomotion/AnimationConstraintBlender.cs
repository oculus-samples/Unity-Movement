// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Oculus.Movement.Locomotion
{
    /// <summary>
    /// Blends animation playback on an <see cref="UnityEngine.Animator"/>, turning animation on
    /// during an activity, and off after the activity stops for a given timeout period.
    /// </summary>
    public class AnimationConstraintBlender : MonoBehaviour
    {
        /// <summary>
        /// Seconds of inactivity till entire animator follows body tracking again
        /// </summary>
        [Tooltip(AnimationConstraintBlenderTooltips.ActivityExitTime)]
        [SerializeField]
        protected float _activityExitTime = 1;

        /// <summary>
        /// The body being animated
        /// </summary>
        [Tooltip(AnimationConstraintBlenderTooltips.Animator)]
        [SerializeField]
        protected Animator _animator;

        /// <summary>
        /// Constraints to deactivate when animation is active.
        /// </summary>
        [Tooltip(AnimationConstraintBlenderTooltips.ConstraintsToDeactivate)]
        [Interface(typeof(IRigConstraint)), SerializeField]
        protected MonoBehaviour[] _constraintsToDeactivate;

        /// <summary>
        /// Constraints to blend when animation is active.
        /// </summary>
        [Tooltip(AnimationConstraintBlenderTooltips.ConstraintsToBlend)]
        [Interface(typeof(IRigConstraint)), SerializeField]
        protected MonoBehaviour[] _constraintsToBlend;

        /// <summary>
        /// Constraints that need to be turned off only while animating the activity
        /// </summary>
        private List<IRigConstraint> _rigConstraintsToDeactivate;

        /// <summary>
        /// Constraints that need the weight to be blended while animating the activity
        /// </summary>
        private List<IRigConstraint> _rigConstraintsToBlend;

        /// <summary>
        /// when the last request for masking ended
        /// </summary>
        private float _timeOfLastActivity;

        /// <summary>
        /// when to actually stop masking
        /// </summary>
        private float _activityTimeout;

        /// <summary>
        /// True if any sources requested the animation to activate
        /// </summary>
        private bool _shouldApplyAnim;

        /// <summary>
        /// True if the animation is being applied
        /// </summary>
        private bool _isApplyingAnim;

        /// <summary>
        /// True if the animation is being applied
        /// </summary>
        public bool IsApplyingAnimation
        {
            get => _isApplyingAnim;
            set
            {
                if (value != _isApplyingAnim)
                {
                    if (value)
                    {
                        StartApplyingAnim();
                    }
                    else
                    {
                        FinishApplyingAnim();
                    }
                }
                _isApplyingAnim = value;
            }
        }

        /// <summary>
        /// If the mask should apply to the animator. Instead of setting this directly, use
        /// <see cref="StartApplyingAnimFor"/> and <see cref="FinishApplyingAnimFor"/>
        /// </summary>
        public bool ShouldApplyAnim
        {
            get => _shouldApplyAnim;
            set
            {
                if (!value && _shouldApplyAnim)
                {
                    _timeOfLastActivity = Time.time;
                    _activityTimeout = _timeOfLastActivity + _activityExitTime;
                }
                else if (value)
                {
                    IsApplyingAnimation = true;
                }
                _shouldApplyAnim = value;
            }
        }

        /// <summary>
        /// The animator to apply the animator mask to
        /// </summary>
        public Animator Animator
        {
            get => _animator;
            set => SetAnimator(value);
        }

#if UNITY_EDITOR
        private void Reset()
        {
            Animator = GetComponentInParent<Animator>();
        }
#endif

        /// <inheritdoc/>
        protected void Start()
        {
            SetAnimator(Animator);
        }

        /// <inheritdoc/>
        protected void Update()
        {
            if (!IsApplyingAnimation || ShouldApplyAnim)
            {
                return;
            }

            foreach (var rigConstraint in _rigConstraintsToBlend)
            {
                rigConstraint.weight = Mathf.Clamp01((_activityTimeout - Time.time) / _activityExitTime);
            }
            if (Time.time >= _activityTimeout)
            {
                IsApplyingAnimation = false;
            }
        }

        /// <summary>
        /// Used to identify that the animation should be applied to the animator
        /// </summary>
        public void StartApplyingAnimFor()
        {
            ShouldApplyAnim = true;
        }

        /// <summary>
        /// Used to identify that the animation is finished being applied to the animator
        /// </summary>
        public void FinishApplyingAnimFor()
        {
            ShouldApplyAnim = false;
        }

        /// <summary>
        /// Sets the animator, and recalculates internal variables, like related rig constraints.
        /// </summary>
        /// <param name="value"></param>
        private void SetAnimator(Animator value)
        {
            _animator = value;
            FindRigConstraints(_animator);
        }

        private void FindRigConstraints(Animator animator)
        {
            if (animator == null)
            {
                return;
            }

            _rigConstraintsToDeactivate = new List<IRigConstraint>();
            foreach (var constraint in _constraintsToDeactivate)
            {
                _rigConstraintsToDeactivate.Add(constraint as IRigConstraint);
            }

            _rigConstraintsToBlend = new List<IRigConstraint>();
            foreach (var constraint in _constraintsToBlend)
            {
                _rigConstraintsToBlend.Add(constraint as IRigConstraint);
            }
        }

        private void StartApplyingAnim()
        {
            _rigConstraintsToDeactivate.ForEach(c => c.weight = 0);
            _rigConstraintsToBlend.ForEach(c => c.weight = 1);
        }

        private void FinishApplyingAnim()
        {
            _rigConstraintsToDeactivate.ForEach(c => c.weight = 1);
        }
    }
}
