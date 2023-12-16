// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Assertions;

namespace Oculus.Movement.Utils
{
    /// <summary>
    /// If the bones are not valid then turn off constraints specified.
    /// This is useful if a constraint depends on the skeleton code to set the
    /// value of a bone every frame. Like a multi-position constraint that applies
    /// an offset.
    /// </summary>
    public class ToggleConstraintsSkeletonState : MonoBehaviour
    {
        /// <summary>
        /// Constraints to control the weight of.
        /// </summary>
        [SerializeField, Interface(typeof(IRigConstraint))]
        [Tooltip(ToggleConstraintsSkeletonStateTooltips.Constraints)]
        private MonoBehaviour[] _constraints;

        /// <summary>
        /// The skeleton object that needs to be tracked.
        /// </summary>
        [SerializeField]
        [Tooltip(ToggleConstraintsSkeletonStateTooltips.Skeleton)]
        private OVRSkeleton _skeleton;

        private IRigConstraint[] _iRigConstraints;
        private float[] _originalConstraintWeights;

        private void Awake()
        {
            Assert.IsTrue(_constraints != null && _constraints.Length > 0);
            Assert.IsNotNull(_skeleton);

            _iRigConstraints = new IRigConstraint[_constraints.Length];
            _originalConstraintWeights = new float[_constraints.Length];
            for (int i = 0; i < _constraints.Length; i++)
            {
                _iRigConstraints[i] = _constraints[i] as IRigConstraint;
                _originalConstraintWeights[i] = _iRigConstraints[i].weight;
                Assert.IsNotNull(_iRigConstraints[i]);
            }
        }

        private void Update()
        {
            bool skeletonValid = _skeleton.IsInitialized && _skeleton.IsDataValid;
            for (int i = 0; i < _iRigConstraints.Length; i++)
            {
                _iRigConstraints[i].weight = skeletonValid ? _originalConstraintWeights[i] : 0.0f;
            }
        }
    }
}
