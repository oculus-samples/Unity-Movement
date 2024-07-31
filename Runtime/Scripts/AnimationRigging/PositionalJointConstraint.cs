// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

namespace Oculus.Movement.AnimationRigging
{
    /// <summary>
    /// Mimics Position Constraint and can be run manually.
    /// </summary>
    public class PositionalJointConstraint : IJointConstraint
    {
        private float _weight;
        private ConstraintSource[] _sources;
        private Transform _target;

        /// <summary>
        /// Main constructor.
        /// </summary>
        /// <param name="transform">Transform with positional constraint.</param>
        public PositionalJointConstraint(Transform transform)
        {
            var positionalConstraint = transform.GetComponent<PositionConstraint>();
            if (positionalConstraint == null)
            {
                throw new System.Exception($"{transform} does not have a PositionConstraint component.");
            }

            if (!positionalConstraint.constraintActive)
            {
                throw new System.Exception($"{transform} does not have an active PositionConstraint component.");
            }

            var sourceTransform = positionalConstraint.GetSource(0);
            _weight = positionalConstraint.weight;
            var sourcesList = new List<ConstraintSource>();
            positionalConstraint.GetSources(sourcesList);
            _sources = sourcesList.ToArray();
            _target = positionalConstraint.transform;
        }

        /// <summary>
        /// Updates positional constraint at runtime.
        /// </summary>
        public void Update()
        {
            Vector3 finalPosition = Vector3.zero;
            foreach (var source in _sources)
            {
                finalPosition += source.sourceTransform.position * source.weight;
            }
            _target.position = finalPosition * _weight;
        }
    }
}
