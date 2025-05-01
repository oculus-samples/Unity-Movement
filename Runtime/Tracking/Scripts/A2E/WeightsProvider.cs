// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Meta.XR.Movement.FaceTracking.Samples
{
    /// <summary>
    /// Abstract weights provider implementation.
    /// </summary>
    public abstract class WeightsProvider : MonoBehaviour
    {
        /// <summary>
        /// Indicates if provider is valid or not.
        /// </summary>
        public abstract bool IsValid { get; }

        /// <summary>
        /// Returns a readonly list of weights.
        /// </summary>
        /// <returns>Weights.</returns>
        public abstract float[] GetWeights();

        /// <summary>
        /// Returns a readonly list of weight names.
        /// </summary>
        /// <returns>Weight names.</returns>
        public abstract string[] GetWeightNames();

        /// <summary>
        /// Copys weights from source to destination float array.
        /// </summary>
        /// <param name="src">Source weights.</param>
        /// <param name="dest">Destination weights.</param>
        public static void CopyWeights(float[] src, ref float[] dest)
        {
            int srcLength = src.Length;
            if (dest.Length != srcLength)
            {
                dest = new float[srcLength];
            }

            Buffer.BlockCopy(src, 0, dest, 0, srcLength * sizeof(float));
        }

        /// <summary>
        /// Copies weights from source to destination native array.
        /// </summary>
        /// <param name="src">Source weights.</param>
        /// <param name="dest">Destination weights.</param>
        public static void CopyWeights(float[] src, ref NativeArray<float> dest)
        {
            Debug.Assert(dest.Length == src.Length);
            for (var i = 0; i < src.Length; ++i)
            {
                dest[i] = src[i];
            }
        }
    }
}
