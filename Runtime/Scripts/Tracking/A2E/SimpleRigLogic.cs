// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Meta.XR.Movement.FaceTracking.Samples
{
    /// <summary>
    /// Simple implementation of <see cref="IRigLogic"/>.
    /// </summary>
    public class SimpleRigLogic : IRigLogic
    {
        /// <inheritdoc />
        public IList<string> Drivers => _drivers;
        private readonly List<string> _drivers;

        /// <summary>
        /// <see cref="SimpleRigLogic"/> constructor. Accepts driver names.
        /// </summary>
        /// <param name="names">Drive names.</param>
        public SimpleRigLogic(IList<string> names)
        {
            _drivers = names.ToList();
        }

        /// <inheritdoc />
        public void Eval(IReadOnlyList<float> driverWeights, IList<float> outputSignals)
        {
            Debug.Assert(driverWeights.Count == _drivers.Count);

            for (var i = 0; i < _drivers.Count; i++)
            {
                outputSignals[i] = driverWeights[i];
            }
        }
    }
}
