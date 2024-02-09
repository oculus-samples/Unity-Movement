// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Movement.AnimationRigging;
using UnityEngine;

namespace Oculus.Movement.Locomotion.Deprecated
{
    /// <summary>
    /// <see cref="AnimationConstraintMasker"/> was refactored into two classes, this class, and
    /// the more generally useful <see cref="StateTransition"/>.
    /// </summary>
    public class TriggerRetargetingConstraintMask : MonoBehaviour
    {
        /// <summary>
        /// Section of body where body tracking continues animating while activity triggers
        /// </summary>
        [Tooltip(TriggerRetargetingConstraintMaskTooltip.Mask)]
        [SerializeField] private AvatarMask _mask;

        /// <summary>
        /// The body being animated
        /// </summary>
        [Tooltip(TriggerRetargetingConstraintMaskTooltip.Animator)]
        [SerializeField] private Animator _animator;

        /// <summary>
        /// Body tracking constraints, populated from <see cref="SetAnimator(Animator)"/>
        /// </summary>
        private RetargetingAnimationConstraint[] _retargetingConstraints;

        /// <summary>
        /// Cached value, remembering if AvatarMask is applied to the body tracking animator.
        /// </summary>
        private bool _isApplyingMask;

        /// <summary>
        /// Animator to apply the animator mask to. Triggered by <see cref="IsApplyingMask"/>.
        /// </summary>
        public Animator Animator
        {
            get => _animator;
            set => SetAnimator(value);
        }

        /// <summary>
        /// AvatarMask to apply to the animator. Triggered by <see cref="IsApplyingMask"/>.
        /// </summary>
        public AvatarMask AvatarMask
        {
            get => _mask;
            set => _mask = value;
        }

        /// <summary>
        /// Set to true to apply the AvatarMask to the body tracking animation class. This will
        /// block some of the body tracking animation done by the
        /// <see cref="RetargetingAnimationConstraint"/>, according to the AvatarMask.
        /// </summary>
        public bool IsApplyingMask
        {
            get => _isApplyingMask;
            set
            {
                _isApplyingMask = value;
                SetRetargetingConstraintMask(_isApplyingMask ? _mask : null);
            }
        }

        private void Start()
        {
            FindValidRetargetingConstraints(_animator);
        }

        /// <summary>
        /// Sets the <see cref="Animator"/>
        /// </summary>
        public void SetAnimator(Animator value)
        {
            _animator = value;
            FindValidRetargetingConstraints(_animator);
        }

        private void FindValidRetargetingConstraints(Animator animator)
        {
            if (animator == null)
            {
                return;
            }
            _retargetingConstraints = animator.GetComponentsInChildren<RetargetingAnimationConstraint>(true);
        }

        private void SetRetargetingConstraintMask(AvatarMask mask)
        {
            System.Array.ForEach(_retargetingConstraints, r => r.data.AvatarMaskComp = mask);
        }
    }
}
