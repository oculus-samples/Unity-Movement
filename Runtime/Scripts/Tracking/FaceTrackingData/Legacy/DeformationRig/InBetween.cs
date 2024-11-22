// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DeformationRig.Deprecated
{
    /// <summary>
    /// Defines an in-between. More specifically, defines a single in-between (i.e. jawDrop50) that
    /// has a single peak.
    /// </summary>
    public class InBetween : ICorrectiveShape
    {
        /// <summary>
        /// Constructs a list of independently usable InBetweens by inferring their influence
        /// curves' keyframes based on other Partials for the same driver index.
        /// </summary>
        public static List<InBetween> BuildFromPartials(List<Partial> partials)
        {
            var partitionedPartials = partials
                .GroupBy(partial => partial.DriverIndex)
                .ToDictionary(
                    grouping => grouping.Key,
                    grouping => grouping.OrderBy(partial => partial.PeakValue).ToList());

            var inBetweens = new List<InBetween>(partials.Count);
            foreach (var (driverIndex, subshapes) in partitionedPartials)
            {
                for (int i = 0; i < subshapes.Count; i++)
                {
                    var keyframes = new List<Keyframe>();
                    keyframes.Add(new Keyframe(0, 0));
                    if (i != 0)
                    {
                        keyframes.Add(new Keyframe(subshapes[i - 1].PeakValue, 0));
                    }
                    keyframes.Add(new Keyframe(subshapes[i].PeakValue, 1));
                    if (i < subshapes.Count - 1)
                    {
                        keyframes.Add(new Keyframe(subshapes[i + 1].PeakValue, 0));
                    }
                    keyframes.Add(new Keyframe(1, 0));

                    inBetweens.Add(new InBetween()
                    {
                        DriverIndex = driverIndex,
                        DrivenIndex = subshapes[i].DrivenIndex,
                        InfluenceCurve = new AnimationCurve(keyframes.ToArray()),
                    });
                }
            }
            return inBetweens;
        }

        /// <summary>
        /// The target blendshape index used for calculating the blendshape weight.
        /// </summary>
        public int DriverIndex;

        /// <summary>
        /// The blendshape index to be driven on the skinned mesh renderer.
        /// </summary>
        public int DrivenIndex;

        /// <summary>
        /// A curve describing how this inbetween's weight should vary with the driver.
        /// </summary>
        public AnimationCurve InfluenceCurve;

        // InBetweens should be constructed via BuildFromPartials to ensure the correct curves are
        // being set for groupings of InBetweens.
        private InBetween() { }

        /// <inheritdoc />
        public void Apply(IBlendshapeInterface blendshapeInterface)
        {
            var driverWeight = blendshapeInterface[DriverIndex];
            blendshapeInterface[DrivenIndex] = InfluenceCurve.Evaluate(driverWeight);
        }

        /// <summary>
        /// Parsed data for a single inbetween (that may belong to a group of in-betweens for a
        /// shared driver shape).
        /// </summary>
        public class Partial
        {
            public int DriverIndex;
            public int DrivenIndex;
            public float PeakValue;
        }
    }
}
