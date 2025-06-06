// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System.Collections.Generic;

namespace Meta.XR.Movement.FaceTracking.Samples
{
    /// <summary>
    /// Base rig logic interface.
    /// </summary>
    public interface IRigLogic
    {
        /// <summary>
        /// List of driver names.
        /// </summary>
        public string[] Drivers { get; }
        /// <summary>
        /// Creates output signal values based on driver weights.
        /// </summary>
        /// <param name="driverWeights">Driver weights.</param>
        /// <param name="outputSignals">Output signals.</param>
        public void Eval(float[] driverWeights, float[] outputSignals);
    }
}
