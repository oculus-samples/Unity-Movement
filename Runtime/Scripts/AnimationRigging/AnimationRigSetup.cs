// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Assertions;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// Enables the animation rig components once the <see cref="OVRSkeleton"/> has initialized.
    /// This is required because we need to build the animation rig with the updated hierarchy
    /// after the <see cref="OVRSkeleton"/> rearranges the bone hierarchy.
    /// </summary>
    public class AnimationRigSetup : MonoBehaviour
    {
        /// <summary>
        /// Skeletal component of character.
        /// </summary>
        [SerializeField]
        [Tooltip(AnimationRigSetupTooltips.Skeleton)]
        protected OVRSkeleton _skeleton;

        /// <inheritdoc cref="_skeleton"/>
        public OVRSkeleton Skeleton
        {
            get { return _skeleton; }
            set { _skeleton = value; }
        }

        /// <summary>
        /// Animator component of character.
        /// </summary>
        [SerializeField]
        [Tooltip(AnimationRigSetupTooltips.Animator)]
        protected Animator _animator;

        /// <inheritdoc cref="_animator"/>
        public Animator AnimatorComp
        {
            get { return _animator; }
            set { _animator = value; }
        }

        /// <summary>
        /// Rig builder on character supporting Animation rigging.
        /// </summary>
        [SerializeField]
        [Tooltip(AnimationRigSetupTooltips.RigBuilder)]
        protected RigBuilder _rigBuilder;

        /// <inheritdoc cref="_rigBuilder"/>
        public RigBuilder RigbuilderComp
        {
            get { return _rigBuilder; }
            set { _rigBuilder = value; }
        }

        /// <summary>
        /// IOVRSkeletonConstraint-based components.
        /// </summary>
        [Interface(typeof(IOVRSkeletonConstraint)), SerializeField]
        [Tooltip(AnimationRigSetupTooltips.OVRSkeletonConstraints)]
        protected MonoBehaviour[] _ovrSkeletonConstraints;

        /// <summary>
        /// If true, rebind the animator upon a skeletal change.
        /// </summary>
        [SerializeField]
        [Tooltip(AnimationRigSetupTooltips.RebindAnimator)]
        protected bool _rebindAnimator = true;

        /// <inheritdoc cref="_rebindAnimator"/>
        public bool RebindAnimator
        {
            get { return _rebindAnimator; }
            set { _rebindAnimator = value; }
        }

        /// <summary>
        /// If true, disable then re-enable the rig upon a skeletal change.
        /// </summary>
        [SerializeField]
        [Tooltip(AnimationRigSetupTooltips.ReEnableRig)]
        protected bool _reEnableRig = true;

        /// <inheritdoc cref="_reEnableRig"/>
        public bool ReEnableRig
        {
            get { return _reEnableRig; }
            set { _reEnableRig = value; }
        }

        /// <summary>
        /// If true, disable then re-enable the rig upon a focus change.
        /// </summary>
        [SerializeField]
        [Tooltip(AnimationRigSetupTooltips.RigToggleOnFocus)]
        protected bool _rigToggleOnFocus = true;

        /// <inheritdoc cref="_rigToggleOnFocus"/>
        public bool RigToggleOnFocus
        {
            get { return _rigToggleOnFocus; }
            set { _rigToggleOnFocus = value; }
        }

        /// <summary>
        /// Retargeting layer component to get data from.
        /// </summary>
        [SerializeField, Optional]
        [Tooltip(AnimationRigSetupTooltips.RetargetingLayer)]
        protected RetargetingLayer _retargetingLayer;

        /// <inheritdoc cref="_retargetingLayer"/>
        public RetargetingLayer RetargetingLayerComp
        {
            get { return _retargetingLayer; }
            set { _retargetingLayer = value; }
        }

        private bool _ranSetup;
        private int _lastSkeletonChangeCount = -1;
        private bool _pendingSkeletalUpdateFromLastFrame = false;
        private IOVRSkeletonConstraint[] _iovrSkeletonConstraints;

        private int _lastRetargetedTransformCount = -1;

        /// <summary>
        /// Adds skeletal constraint. Valid for use via editor scripts only.
        /// </summary>
        /// <param name="newConstraint">New constraint to add.</param>
        public void AddSkeletalConstraint(MonoBehaviour newConstraint)
        {
            if (_iovrSkeletonConstraints == null)
            {
                _ovrSkeletonConstraints = new MonoBehaviour[1];
                _ovrSkeletonConstraints[0] = newConstraint;
            }
            else
            {
                var oldConstraints = _ovrSkeletonConstraints;
                _ovrSkeletonConstraints =
                    new MonoBehaviour[oldConstraints.Length + 1];
                for(int i = 0; i < oldConstraints.Length; i++)
                {
                    _ovrSkeletonConstraints[i] = oldConstraints[i];
                }
                _ovrSkeletonConstraints[oldConstraints.Length] =
                    newConstraint;
            }
        }

        /// <summary>
        /// Disable the animator and rig until the skeleton is ready to be used with animation rigging.
        /// </summary>
        protected virtual void Awake()
        {
            Assert.IsNotNull(_skeleton);
            Assert.IsNotNull(_animator);
            _animator.enabled = false;
            if (_rigBuilder)
            {
                _rigBuilder.enabled = false;
            }

            if (_ovrSkeletonConstraints != null)
            {
                _iovrSkeletonConstraints = new IOVRSkeletonConstraint[_ovrSkeletonConstraints.Length];
                for (int i = 0; i < _iovrSkeletonConstraints.Length; i++)
                {
                    _iovrSkeletonConstraints[i] =
                        _ovrSkeletonConstraints[i] as IOVRSkeletonConstraint;
                    Assert.IsNotNull(_iovrSkeletonConstraints[i]);
                }
            }
        }

        /// <summary>
        /// Initialize the animation rig if the skeleton is initialized, and check to re-enable the rig
        /// if necessary.
        /// </summary>
        protected virtual void Update()
        {
            RunInitialSetup();

            // Disable and renable the rig if the skeleton change.
            if (_reEnableRig)
            {
                ReEnableRigIfPendingSkeletalChange();
                CheckForSkeletalChanges();
            }
        }

        /// <summary>
        /// Disable and re-enable the rig if <see cref="_rigToggleOnFocus"/> is enabled.
        /// </summary>
        /// <param name="hasFocus">True if the application is currently focused.</param>
        protected virtual void OnApplicationFocus(bool hasFocus)
        {
            if (_rigToggleOnFocus)
            {
                if (!hasFocus)
                {
                    DisableRig();
                    _rigBuilder.Evaluate(Time.deltaTime);
                }
                else
                {
                    EnableRig();
                }
            }
        }

        private void RunInitialSetup()
        {
            if (_ranSetup)
            {
                return;
            }

            if (_skeleton.IsInitialized)
            {
                _animator.enabled = true;
                if (_rebindAnimator)
                {
                    _animator.Rebind();
                    _animator.Update(0.0f);
                }
                if (_rigBuilder)
                {
                    _rigBuilder.enabled = true;
                }
                _ranSetup = true;
            }
        }

        private void DisableRig()
        {
            if (_rigBuilder)
            {
                _rigBuilder.enabled = false;
            }

            if (_iovrSkeletonConstraints != null)
            {
                foreach (var currentConstraint in _iovrSkeletonConstraints)
                {
                    currentConstraint.RegenerateData();
                }
            }

            if (_retargetingLayer != null)
            {
                _lastRetargetedTransformCount = _retargetingLayer.GetNumberOfTransformsRetargeted();
            }
        }

        private void EnableRig()
        {
            if (_rigBuilder)
            {
                _rigBuilder.enabled = true;
            }
            if (_rebindAnimator)
            {
                _animator.Rebind();
                _animator.Update(0.0f);
            }
            _pendingSkeletalUpdateFromLastFrame = false;
        }

        private void ReEnableRigIfPendingSkeletalChange()
        {
            if (!_ranSetup)
            {
                return;
            }
            if (_pendingSkeletalUpdateFromLastFrame)
            {
                EnableRig();
                Debug.LogWarning("Re-enabled rig after skeletal update.");
            }
        }

        private void CheckForSkeletalChanges()
        {
            if (!_ranSetup)
            {
                return;
            }
            if (_lastSkeletonChangeCount != _skeleton.SkeletonChangedCount ||
                HasRetargeterBeenUpdated())
            {
                // Allow rig to initialize next frame so that constraints can
                // catch up to skeletal changes in this frame.
                _pendingSkeletalUpdateFromLastFrame = true;
                _lastSkeletonChangeCount = _skeleton.SkeletonChangedCount;

                DisableRig();
                Debug.LogWarning("Detected skeletal change. Disabling the rig.");
            }
        }

        private bool HasRetargeterBeenUpdated()
        {
            if (_retargetingLayer == null)
            {
                return false;
            }
            return _lastRetargetedTransformCount != _retargetingLayer.GetNumberOfTransformsRetargeted();
        }
    }
}
