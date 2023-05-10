// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

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

        private void Awake()
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

        private void Update()
        {
            RunInitialSetup();

            // Disable and renable the rig if the skeleton change.
            if (_reEnableRig)
            {
                ReEnableRigIfPendingSkeletalChange();
                CheckForSkeletalChanges();
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

        private void ReEnableRigIfPendingSkeletalChange()
        {
            if (!_ranSetup)
            {
                return;
            }
            if (_pendingSkeletalUpdateFromLastFrame)
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

                if (_rigBuilder)
                {
                    _rigBuilder.enabled = false;
                }
                Debug.LogWarning("Detected skeletal change. Disabling the rig.");

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
