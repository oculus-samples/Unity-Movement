// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Assertions;

namespace Oculus.Movement.AnimationRigging.Deprecated
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
        protected bool _rigToggleOnFocus = false;

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

        /// <summary>
        /// Use proxy transforms to check skeletal changes.
        /// Proxy transforms can be used in case the original
        /// skeleton updates too much.
        /// </summary>
        [SerializeField]
        [Tooltip(AnimationRigSetupTooltips.CheckSkeletalUpdatesByProxy)]
        protected bool _checkSkeletalUpdatesByProxy = false;

        /// <inheritdoc cref="_checkSkeletalUpdatesByProxy"/>
        public bool CheckSkeletalUpdatesByProxy
        {
            get { return _checkSkeletalUpdatesByProxy; }
            set { _checkSkeletalUpdatesByProxy = value;  }
        }

        private bool _ranSetup;
        private int _lastSkeletonChangeCount = -1;
        private bool _pendingSkeletalUpdateFromLastFrame = false;
        private IOVRSkeletonConstraint[] _iovrSkeletonConstraints;

        private int _proxyChangeCount = 0;
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

            // Update the interface references in case this was called after awake, but before Update
            UpdateSkeletalConstraintInterfaceReferences();
        }

        /// <summary>
        /// Disable the animator and rig until the skeleton is ready to be used with animation rigging.
        /// </summary>
        protected virtual void Awake()
        {
            Assert.IsNotNull(_skeleton);
            Assert.IsNotNull(_animator);

            if (_ovrSkeletonConstraints != null)
            {
                UpdateSkeletalConstraintInterfaceReferences();
            }

            if (_retargetingLayer != null)
            {
                if (_retargetingLayer.EnableTrackingByProxy != CheckSkeletalUpdatesByProxy)
                {
                    Debug.LogError($"The proxy tracking setting should be the same in " +
                        $"retargeting layer and animation rig set up for the object {this.name}.");
                }
            }
        }

        private void UpdateSkeletalConstraintInterfaceReferences()
        {
            _iovrSkeletonConstraints = new IOVRSkeletonConstraint[_ovrSkeletonConstraints.Length];
            for (int i = 0; i < _iovrSkeletonConstraints.Length; i++)
            {
                _iovrSkeletonConstraints[i] =
                    _ovrSkeletonConstraints[i] as IOVRSkeletonConstraint;
                Assert.IsNotNull(_iovrSkeletonConstraints[i]);
            }

            // Add the copy original pose constraint that will be run after retargeting animation constraint.
            if (_retargetingLayer != null && _retargetingLayer.ApplyAnimationConstraintsToCorrectedPositions)
            {
                RetargetingAnimationConstraint retargetConstraint =
                    GetComponentInChildren<RetargetingAnimationConstraint>(true);
                if (retargetConstraint != null)
                {
                    var copyPoseOriginalConstraint =
                        CheckAndAddMissingCopyPoseAnimationConstraint(retargetConstraint, true);
                    var copyPoseFinalConstraint =
                        CheckAndAddMissingCopyPoseAnimationConstraint(retargetConstraint, false);

                    if (copyPoseOriginalConstraint != null)
                    {
                        AddSkeletalConstraint(copyPoseOriginalConstraint);
                    }

                    if (copyPoseFinalConstraint != null)
                    {
                        AddSkeletalConstraint(copyPoseFinalConstraint);
                    }
                }
            }
        }

        private CopyPoseConstraint CheckAndAddMissingCopyPoseAnimationConstraint(
            RetargetingAnimationConstraint retargetConstraint, bool shouldCopyPoseToOriginal)
        {
            CopyPoseConstraint[] copyPoseConstraints =
                GetComponentsInChildren<CopyPoseConstraint>(true);
            foreach (var constraint in copyPoseConstraints)
            {
                if (constraint.data.CopyPoseToOriginal == shouldCopyPoseToOriginal)
                {
                    return null;
                }
            }

            GameObject copyPoseConstraintObj = new GameObject(shouldCopyPoseToOriginal ?
                    "CopyOriginalPoseConstraint" : "CopyFinalPoseConstraint");
            copyPoseConstraintObj.SetActive(false);
            CopyPoseConstraint copyPoseConstraint = copyPoseConstraintObj.AddComponent<CopyPoseConstraint>();
            copyPoseConstraint.data.CopyPoseToOriginal = shouldCopyPoseToOriginal;
            copyPoseConstraint.data.RetargetingLayerComp = _retargetingLayer;
            if (shouldCopyPoseToOriginal)
            {
                copyPoseConstraintObj.transform.SetParent(retargetConstraint.transform);
                copyPoseConstraintObj.transform.SetAsFirstSibling();
                Debug.Log("CopyPoseConstraint for the original pose has been added " +
                          "to the animation rig for retargeting.");
            }
            else
            {
                copyPoseConstraintObj.transform.SetParent(retargetConstraint.transform.parent);
                copyPoseConstraintObj.transform.SetAsLastSibling();
                Debug.Log("CopyPoseConstraint for the final pose has been added " +
                          "to the animation rig for retargeting.");
            }
            return copyPoseConstraint;
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
        /// Do this for builds only. We don't want to stop everything if we run inside of the
        /// editor.
        /// </summary>
        /// <param name="hasFocus">True if the application is currently focused.</param>
        protected virtual void OnApplicationFocus(bool hasFocus)
        {
            if (Application.isEditor)
            {
                return;
            }

            // Bail if we don't want the rig to toggle during focus events.
            if (!_rigToggleOnFocus)
            {
                return;
            }

            // Don't do anything if the setup process has not run yet. We don't
            // want to trigger the creation of any animation rigging jobs.
            if (!_ranSetup)
            {
                return;
            }

            if (!hasFocus)
            {
                // Run the constraints one more time so when paused, the
                // constraints are applied to the latest skeleton.
                DisableRigAndUpdateState();
                _rigBuilder.Evaluate(Time.deltaTime);
            }
            else
            {
                EnableRig();
            }
        }

        private void RunInitialSetup()
        {
            if (_ranSetup)
            {
                return;
            }

            if (!_skeleton.IsInitialized)
            {
                return;
            }

            UpdateDependentConstraints();

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

        private void DisableRigAndUpdateState()
        {
            if (_rigBuilder)
            {
                _rigBuilder.enabled = false;
            }

            UpdateDependentConstraints();

            if (_retargetingLayer != null)
            {
                _lastRetargetedTransformCount = _retargetingLayer.GetNumberOfTransformsRetargeted();
            }

            if (_checkSkeletalUpdatesByProxy && _retargetingLayer != null)
            {
                _proxyChangeCount = _retargetingLayer.ProxyChangeCount;
            }
        }

        private void UpdateDependentConstraints()
        {
            if (_iovrSkeletonConstraints != null)
            {
                foreach (var currentConstraint in _iovrSkeletonConstraints)
                {
                    currentConstraint.RegenerateData();
                }
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
            if (_lastSkeletonChangeCount != _skeleton.SkeletonChangedCount)
            {
                // If checking by proxy, avoid updating rig only if
                // a) skeletal proxies have not been recreated and
                // b) retargeter has not been updated.
                if (_checkSkeletalUpdatesByProxy &&
                    !HasSkeletonProxiesBeenRecreated() &&
                    !HasRetargeterBeenUpdated())
                {
                    _lastSkeletonChangeCount = _skeleton.SkeletonChangedCount;
                    return;
                }

                // Allow rig to initialize next frame so that constraints can
                // catch up to skeletal changes in this frame.
                _pendingSkeletalUpdateFromLastFrame = true;
                _lastSkeletonChangeCount = _skeleton.SkeletonChangedCount;

                DisableRigAndUpdateState();

                // allow constraints to run one last time
                _rigBuilder.Evaluate(Time.deltaTime);

                Debug.LogWarning("Detected skeletal change. Disabling the rig.");
            }
        }

        private bool HasSkeletonProxiesBeenRecreated()
        {
            if (_retargetingLayer == null)
            {
                return false;
            }
            return _proxyChangeCount != _retargetingLayer.ProxyChangeCount;
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
