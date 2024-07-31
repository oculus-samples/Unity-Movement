// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Movement.AnimationRigging;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Oculus.Movement.Locomotion.Deprecated
{
    /// <summary>
    /// Manages animation masking on an <see cref="UnityEngine.Animator"/>, turning masking on
    /// during an activity, and off after the activity stops for a given timeout period.
    /// </summary>
    public class AnimationConstraintMasker : MonoBehaviour
    {
        /// <summary>
        /// Section of body where body tracking continues animating while activity triggers
        /// </summary>
        [Tooltip(AnimationConstraintMaskerTooltips.Mask)]
        [SerializeField]
        private AvatarMask _mask;

        /// <summary>
        /// Seconds of inactivity till entire animator follows body tracking again
        /// </summary>
        [Tooltip(AnimationConstraintMaskerTooltips.ActivityExitTime)]
        [SerializeField]
        private float _activityExitTime = 1;

        /// <summary>
        /// The body being animated
        /// </summary>
        [Tooltip(AnimationConstraintMaskerTooltips.Animator)]
        [SerializeField]
        private Animator _animator;

        /// <summary>
        /// Partial names of constraints to deactivate when animation masking is active. For
        /// example, {"_Leg"} will disable all rig constraints with "_Leg" in the name.
        /// </summary>
        [Tooltip(AnimationConstraintMaskerTooltips.ConstraintsToDeactivate)]
        [SerializeField]
        private string[] _constraintsToDeactivate;

        /// <summary>
        /// Body tracking constraints, populated from <see cref="SetAnimator(Animator)"/>
        /// </summary>
        private RetargetingAnimationConstraint[] _retargetingConstraints;

        /// <summary>
        /// Constraints that need to be turned off only while animating the activity
        /// </summary>
        private List<IRigConstraint> _namedConstraintsToDeactivate = new List<IRigConstraint>();

        /// <summary>
        /// What activities want to apply this animation mask? Needed because multiple activities
        /// can try to apply the same mask, like walk and jump masking legs.
        /// </summary>
        private HashSet<string> _sourcesRequestingMask = new HashSet<string>();

        /// <summary>
        /// when the last request for masking ended
        /// </summary>
        private float _timeOfLastActivity;

        /// <summary>
        /// when to actually stop masking
        /// </summary>
        private float _activityTimeout;

        /// <summary>
        /// True if any sources requested the animation mask to activate
        /// </summary>
        private bool _shouldApplyMask;

        /// <summary>
        /// True if the animation mask is being applied
        /// </summary>
        private bool _isApplyingMask;

        /// <summary>
        /// True if the animation mask is being applied
        /// </summary>
        public bool IsApplyingMask
        {
            get => _isApplyingMask;
            set
            {
                if (value != _isApplyingMask)
                {
                    if (value)
                    {
                        StartApplyingMask();
                    }
                    else
                    {
                        FinishApplyingMask();
                    }
                }
                _isApplyingMask = value;
            }
        }

        /// <summary>
        /// seconds since the last time an event requested the animation mask
        /// </summary>
        public float SecondsSinceLastLocomotionInput
        {
            get
            {
                if (_shouldApplyMask)
                {
                    return 0;
                }
                return Time.time - _timeOfLastActivity;
            }
        }

        /// <summary>
        /// What activities want to apply this animation mask? Needed because multiple activities
        /// can try to apply the same mask, like walk and jump masking legs.
        /// </summary>
        public IEnumerable<string> SourcesRequestingMask => _sourcesRequestingMask;

        /// <summary>
        /// If the mask should apply to the animator. Instead of setting this directly, use
        /// <see cref="StartApplyingMaskFor"/> and <see cref="FinishApplyingMaskFor(string)"/>
        /// </summary>
        public bool ShouldApplyMask
        {
            get => _shouldApplyMask;
            set
            {
                if (!value && _shouldApplyMask)
                {
                    if (_sourcesRequestingMask.Count != 0)
                    {
                        Debug.LogWarning($"Unresolved mask request from: {string.Join(", ", _sourcesRequestingMask)}");
                    }
                    _timeOfLastActivity = Time.time;
                    _activityTimeout = _timeOfLastActivity + _activityExitTime;
                }
                else if (value)
                {
                    IsApplyingMask = true;
                }
                _shouldApplyMask = value;
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
        private void Start()
        {
            SetAnimator(Animator);
        }

        /// <inheritdoc/>
        private void Update()
        {
            if (IsApplyingMask && !ShouldApplyMask && Time.time >= _activityTimeout)
            {
                IsApplyingMask = false;
            }
        }

        /// <summary>
        /// Used to identify that the animation mask should be applied to the animator
        /// </summary>
        /// <param name="source">a unique name for the acitivy applying the animation mask</param>
        public void StartApplyingMaskFor(string source)
        {
            _sourcesRequestingMask.Add(source);
            ShouldApplyMask = true;
        }

        /// <summary>
        /// Used to identify that the animation mask is finished being applied to the animator
        /// </summary>
        /// <param name="source">a unique name for the acitivy applying the animation mask</param>
        public void FinishApplyingMaskFor(string source)
        {
            _sourcesRequestingMask.Remove(source);
            if (_sourcesRequestingMask.Count == 0)
            {
                ShouldApplyMask = false;
            }
        }

        /// <summary>
        /// Sets the animator, and recalculates internal variables, like related rig constraints.
        /// </summary>
        /// <param name="value"></param>
        public void SetAnimator(Animator value)
        {
            _animator = value;
            FindValidRetargetingConstraints(_animator);
            DeactivateMarkedRigConstraints(_animator);
        }

        private void DeactivateMarkedRigConstraints(Animator animator)
        {
            if (animator == null)
            {
                return;
            }
            _namedConstraintsToDeactivate = GetNamedConstraints(
                animator.GetComponentsInChildren<IRigConstraint>(true), _constraintsToDeactivate);
        }

        private List<IRigConstraint> GetNamedConstraints(IRigConstraint[] constraints, string[] nameContains)
        {
            List<IRigConstraint> found = new List<IRigConstraint>();
            for (int i = constraints.Length - 1; i >= 0; --i)
            {
                Object obj = constraints[i] as Object;
                if (obj == null)
                {
                    continue;
                }
                for (int j = 0; j < nameContains.Length; ++j)
                {
                    if (obj.name.Contains(nameContains[j]))
                    {
                        found.Add(constraints[i]);
                    }
                }
            }
            return found;
        }

        private void FindValidRetargetingConstraints(Animator animator)
        {
            if (animator == null)
            {
                return;
            }
            _retargetingConstraints = animator.GetComponentsInChildren<RetargetingAnimationConstraint>(true);
        }

        private void StartApplyingMask()
        {
            SetRetargetingConstraintMask(_mask);
            _namedConstraintsToDeactivate.ForEach(c => c.weight = 0);
        }

        private void FinishApplyingMask()
        {
            SetRetargetingConstraintMask(null);
            _namedConstraintsToDeactivate.ForEach(c => c.weight = 1);
        }

        private void SetRetargetingConstraintMask(AvatarMask mask)
        {
            System.Array.ForEach(_retargetingConstraints, r => r.data.AvatarMaskComp = mask);
        }
    }
}
