// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Assertions;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// Handles the animation rig on the retargeting layer.
    /// </summary>
    public class RetargetingAnimationRig
    {
        /// <summary>
        /// If true, rebind the animator upon a skeletal change.
        /// </summary>
        [SerializeField]
        [Tooltip(AnimationRigSetupTooltips.RebindAnimator)]
        protected bool _rebindAnimator = true;
        /// <inheritdoc cref="_rebindAnimator"/>
        public bool RebindAnimator
        {
            get => _rebindAnimator;
            set => _rebindAnimator = value;
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
            get => _reEnableRig;
            set => _reEnableRig = value;
        }

        /// <summary>
        /// If true, disable then re-enable the rig upon a focus change.
        /// </summary>
        [SerializeField]
        [Tooltip(AnimationRigSetupTooltips.RigToggleOnFocus)]
        protected bool _rigToggleOnFocus;
        /// <inheritdoc cref="_rigToggleOnFocus"/>
        public bool RigToggleOnFocus
        {
            get => _rigToggleOnFocus;
            set => _rigToggleOnFocus = value;
        }

        /// <summary>
        /// Rig builder on character supporting Animation rigging.
        /// </summary>
        [SerializeField]
        [Tooltip(AnimationRigSetupTooltips.RigBuilder)]
        protected RigBuilder _rigBuilder;
        /// <inheritdoc cref="_rigBuilder"/>
        public RigBuilder RigBuilderComp
        {
            get => _rigBuilder;
            set => _rigBuilder = value;
        }

        /// <summary>
        /// IOVRSkeletonConstraint-based components.
        /// </summary>
        [Interface(typeof(IOVRSkeletonConstraint)), SerializeField]
        [Tooltip(AnimationRigSetupTooltips.OVRSkeletonConstraints)]
        protected MonoBehaviour[] _ovrSkeletonConstraints;

        private IOVRSkeletonConstraint[] _skeletonConstraints;
        private bool _ranSetup;
        private bool _pendingSkeletalUpdateFromLastFrame;
        private int _proxyChangeCount;
        private int _lastSkeletonChangeCount = -1;
        private int _lastRetargetedTransformCount = -1;

        /// <summary>
        /// Initialize the animation rig if the skeleton is initialized, and check to re-enable the rig
        /// if necessary.
        /// </summary>
        public virtual void UpdateRig(RetargetingLayer retargetingLayer)
        {
            // Setup the rig.
            SetupRig(retargetingLayer);
            if (!_ranSetup)
            {
                return;
            }

            // Disable and re-enable the rig if the skeleton changed.
            if (_reEnableRig)
            {
                ReEnableRigIfPendingSkeletalChange(retargetingLayer);
                CheckForSkeletalChanges(retargetingLayer);
            }
        }

        /// <summary>
        /// Disable and re-enable the rig if <see cref="_rigToggleOnFocus"/> is enabled.
        /// Do this for builds only. We don't want to stop everything if we run inside of the
        /// editor.
        /// </summary>
        /// <param name="retargetingLayer">The retargeting layer.</param>
        /// <param name="hasFocus">True if the application is currently focused.</param>
        public virtual void OnApplicationFocus(RetargetingLayer retargetingLayer, bool hasFocus)
        {
            // Don't toggle the rig in editor.
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
                DisableRigAndUpdateState(retargetingLayer);
                _rigBuilder.Evaluate(Time.deltaTime);
            }
            else
            {
                EnableRig(retargetingLayer);
            }
        }

        /// <summary>
        /// Setup the animation rig if the skeleton is initialized.
        /// </summary>
        /// <param name="retargetingLayer">The retargeting layer.</param>
        protected virtual void SetupRig(RetargetingLayer retargetingLayer)
        {
            if (_ranSetup)
            {
                return;
            }

            if (!retargetingLayer.IsInitialized)
            {
                return;
            }

            UpdateDependentConstraints();

            var animator = retargetingLayer.GetAnimatorTargetSkeleton();
            if (_rebindAnimator)
            {
                animator.Rebind();
                animator.Update(0.0f);
            }

            _ranSetup = true;
        }

        /// <summary>
        /// Update skeletal constraint interface references.
        /// </summary>
        /// <param name="retargetingLayer"></param>
        protected virtual void UpdateSkeletalConstraintInterfaceReferences(RetargetingLayer retargetingLayer)
        {
            _skeletonConstraints = new IOVRSkeletonConstraint[_ovrSkeletonConstraints.Length];
            for (int i = 0; i < _skeletonConstraints.Length; i++)
            {
                _skeletonConstraints[i] =
                    _ovrSkeletonConstraints[i] as IOVRSkeletonConstraint;
                Assert.IsNotNull(_skeletonConstraints[i]);
            }

            // Add the copy original pose constraint that will be run after retargeting animation constraint.
            if (retargetingLayer.ApplyAnimationConstraintsToCorrectedPositions)
            {
                RetargetingAnimationConstraint retargetConstraint =
                    retargetingLayer.RetargetingConstraint;
                if (retargetConstraint == null)
                {
                    Debug.LogError("Missing required retargeting constraint to add copy pose constraints.");
                    return;
                }

                var copyPoseOriginalConstraint = CheckAndAddMissingCopyPoseAnimationConstraint(
                    retargetConstraint, retargetingLayer, true);
                var copyPoseFinalConstraint = CheckAndAddMissingCopyPoseAnimationConstraint(
                    retargetConstraint, retargetingLayer, false);

                if (copyPoseOriginalConstraint != null)
                {
                    AddSkeletalConstraint(copyPoseOriginalConstraint, retargetingLayer);
                }

                if (copyPoseFinalConstraint != null)
                {
                    AddSkeletalConstraint(copyPoseFinalConstraint, retargetingLayer);
                }
            }
        }

        /// <summary>
        /// Adds skeletal constraint.
        /// </summary>
        /// <param name="newConstraint">New constraint to add.</param>
        /// <param name="retargetingLayer">The retargeting layer.</param>
        protected virtual void AddSkeletalConstraint(MonoBehaviour newConstraint, RetargetingLayer retargetingLayer)
        {
            if (_skeletonConstraints == null)
            {
                _ovrSkeletonConstraints = new MonoBehaviour[1];
                _ovrSkeletonConstraints[0] = newConstraint;
            }
            else
            {
                var oldConstraints = _ovrSkeletonConstraints;
                _ovrSkeletonConstraints =
                    new MonoBehaviour[oldConstraints.Length + 1];
                for (int i = 0; i < oldConstraints.Length; i++)
                {
                    _ovrSkeletonConstraints[i] = oldConstraints[i];
                }

                _ovrSkeletonConstraints[oldConstraints.Length] =
                    newConstraint;
            }

            // Update the interface references in case this was called after awake, but before Update
            UpdateSkeletalConstraintInterfaceReferences(retargetingLayer);
        }

        /// <summary>
        /// Check and add missing copy pose animation constraints.
        /// </summary>
        /// <param name="retargetConstraint">The retargeting constraint for the copy pose constraint.</param>
        /// <param name="retargetingLayer">The retargeting layer.</param>
        /// <param name="shouldCopyPoseToOriginal">True if the pose is the original pose.</param>
        /// <returns>Returns the created copy pose constraint.</returns>
        protected virtual CopyPoseConstraint CheckAndAddMissingCopyPoseAnimationConstraint(
            RetargetingAnimationConstraint retargetConstraint, RetargetingLayer retargetingLayer,
            bool shouldCopyPoseToOriginal)
        {
            var copyPoseConstraints =
                retargetingLayer.GetComponentsInChildren<CopyPoseConstraint>(true);
            foreach (var constraint in copyPoseConstraints)
            {
                if (constraint.data.CopyPoseToOriginal == shouldCopyPoseToOriginal)
                {
                    return null;
                }
            }

            var copyPoseConstraintObj = new GameObject(shouldCopyPoseToOriginal ?
                "CopyOriginalPoseConstraint" :
                "CopyFinalPoseConstraint");
            copyPoseConstraintObj.SetActive(false);

            var copyPoseConstraint = copyPoseConstraintObj.AddComponent<CopyPoseConstraint>();
            copyPoseConstraint.data.CopyPoseToOriginal = shouldCopyPoseToOriginal;
            copyPoseConstraint.data.RetargetingLayerComp = retargetingLayer;
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

        private void DisableRigAndUpdateState(RetargetingLayer retargetingLayer)
        {
            if (_rigBuilder)
            {
                _rigBuilder.enabled = false;
            }

            UpdateDependentConstraints();
            _lastRetargetedTransformCount = retargetingLayer.GetNumberOfTransformsRetargeted();
            _proxyChangeCount = retargetingLayer.ProxyChangeCount;
        }

        private void EnableRig(RetargetingLayer retargetingLayer)
        {
            if (_rigBuilder)
            {
                _rigBuilder.enabled = true;
            }

            if (_rebindAnimator)
            {
                var animator = retargetingLayer.GetAnimatorTargetSkeleton();
                animator.Rebind();
                animator.Update(0.0f);
            }

            _pendingSkeletalUpdateFromLastFrame = false;
        }

        private void UpdateDependentConstraints()
        {
            if (_skeletonConstraints != null)
            {
                foreach (var currentConstraint in _skeletonConstraints)
                {
                    currentConstraint.RegenerateData();
                }
            }
        }

        private void ReEnableRigIfPendingSkeletalChange(RetargetingLayer retargetingLayer)
        {
            if (_pendingSkeletalUpdateFromLastFrame)
            {
                EnableRig(retargetingLayer);
                Debug.LogWarning("Re-enabled rig after skeletal update.");
            }
        }

        private void CheckForSkeletalChanges(RetargetingLayer retargetingLayer)
        {
            if (_lastSkeletonChangeCount != retargetingLayer.SkeletonChangedCount)
            {
                // If checking by proxy, avoid updating rig only if
                // a) skeletal proxies have not been recreated and
                // b) retargeter has not been updated.
                if (!HasSkeletonProxiesBeenRecreated(retargetingLayer) &&
                    !HasRetargeterBeenUpdated(retargetingLayer))
                {
                    _lastSkeletonChangeCount = retargetingLayer.SkeletonChangedCount;
                    return;
                }

                // Allow rig to initialize next frame so that constraints can
                // catch up to skeletal changes in this frame.
                _pendingSkeletalUpdateFromLastFrame = true;
                _lastSkeletonChangeCount = retargetingLayer.SkeletonChangedCount;
                DisableRigAndUpdateState(retargetingLayer);

                // Allow constraints to run one last time.
                _rigBuilder.Evaluate(Time.deltaTime);

                Debug.LogWarning("Detected skeletal change. Disabling the rig.");
            }
        }

        private bool HasSkeletonProxiesBeenRecreated(RetargetingLayer retargetingLayer)
        {
            return _proxyChangeCount != retargetingLayer.ProxyChangeCount;
        }

        private bool HasRetargeterBeenUpdated(RetargetingLayer retargetingLayer)
        {
            return _lastRetargetedTransformCount != retargetingLayer.GetNumberOfTransformsRetargeted();
        }
    }
}
