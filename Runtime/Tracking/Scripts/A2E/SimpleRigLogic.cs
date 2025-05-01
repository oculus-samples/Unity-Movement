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
        public string[] Drivers => _drivers;
        private readonly string[] _drivers;

        /// <summary>
        /// <see cref="SimpleRigLogic"/> constructor. Accepts driver names.
        /// </summary>
        /// <param name="names">Drive names.</param>
        public SimpleRigLogic(IList<string> names)
        {
            _drivers = names.ToArray();
        }

        /// <inheritdoc />
        public void Eval(float[] driverWeights, float[] outputSignals)
        {
            Debug.Assert(driverWeights.Length == _drivers.Length);

            for (var i = 0; i < _drivers.Length; i++)
            {
                outputSignals[i] = driverWeights[i];
            }
        }
    }
}
