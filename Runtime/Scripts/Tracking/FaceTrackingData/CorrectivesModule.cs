// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Movement.Tracking
{
    /// <summary>
    /// Face correctives module.
    /// </summary>
    public class CorrectivesModule
    {
        /// <summary>
        /// Defines an in-between.
        /// </summary>
        [System.Serializable]
        public class InBetween
        {
            /// <summary>
            /// The blendshape index to be driven on the skinned mesh renderer.
            /// </summary>
            [Tooltip(CorrectivesModuleTooltips.InBetweenTooltips.DrivenIndex)]
            public int drivenIndex;

            /// <summary>
            /// The target blendshape index used for calculating the blendshape weight.
            /// </summary>
            [Tooltip(CorrectivesModuleTooltips.InBetweenTooltips.DriverIndex)]
            public int driverIndex;

            /// <summary>
            /// The slope from the function curve of the in-between.
            /// </summary>
            [Tooltip(CorrectivesModuleTooltips.InBetweenTooltips.Slope)]
            public float slope;

            /// <summary>
            /// The x offset from the function curve of the in-between.
            /// </summary>
            [Tooltip(CorrectivesModuleTooltips.InBetweenTooltips.OffsetX)]
            public float offset_x;

            /// <summary>
            /// The y offset from the function curve of the in-between.
            /// </summary>
            [Tooltip(CorrectivesModuleTooltips.InBetweenTooltips.OffsetY)]
            public float offset_y;

            /// <summary>
            /// The domain range start from the function curve of the in-between.
            /// </summary>
            [Tooltip(CorrectivesModuleTooltips.InBetweenTooltips.DomainStart)]
            public float domainStart;

            /// <summary>
            /// The domain range end from the function curve of the in-between.
            /// </summary>
            [Tooltip(CorrectivesModuleTooltips.InBetweenTooltips.DomainEnd)]
            public float domainEnd;
        }

        /// <summary>
        /// Defines a combination target.
        /// </summary>
        [System.Serializable]
        public class Combination
        {
            /// <summary>
            /// The blendshape index to be driven on the skinned mesh renderer.
            /// </summary>
            [Tooltip(CorrectivesModuleTooltips.CombinationTooltips.DrivenIndex)]
            public int drivenIndex;

            /// <summary>
            /// The blendshape indices used in calculating the blendshape weight for the driven index.
            /// </summary>
            [Tooltip(CorrectivesModuleTooltips.CombinationTooltips.DriverIndices)]
            public int[] driverIndices;
        }

        /// <summary>
        /// Defines in-betweens and combinations data.
        /// </summary>
        [System.Serializable]
        public class RigLogicData
        {
            /// <summary>
            /// Array of all of the in-betweens data.
            /// </summary>
            [Tooltip(CorrectivesModuleTooltips.RigLogicDataTooltips.InBetweens)]
            public InBetween[] inBetweens;

            /// <summary>
            /// Array of all of the combinations data.
            /// </summary>
            [Tooltip(CorrectivesModuleTooltips.RigLogicDataTooltips.Combinations)]
            public Combination[] combinations;
        }

        private const float _PERCENT_TO_DECIMAL = 0.01f;
        private const float _DECIMAL_TO_PERCENT = 100.0f;

        private RigLogicData _rigLogic;

        /// <summary>
        /// Correctives module constructor.
        /// </summary>
        /// <param name="combinationShapesTextAsset">Correctives text asset deserialized as JSON.</param>
        public CorrectivesModule(TextAsset combinationShapesTextAsset)
        {
            Assert.IsNotNull(combinationShapesTextAsset);

            var assetText = combinationShapesTextAsset.text;
            _rigLogic = JsonUtility.FromJson<RigLogicData>(assetText);
            Assert.IsNotNull(_rigLogic, "Could load combination shape data!");
        }

        /// <summary>
        /// Apply corrective blendshape weights to the skinned mesh renderer
        /// </summary>
        /// <param name="blendShapevalues">Blend shape values to update.</param>
        public void ApplyCorrectives(float[] blendShapevalues)
        {
            if (_rigLogic?.combinations == null)
            {
                return;
            }

            ProcessInBetweens(blendShapevalues);
            ProcessCombinations(blendShapevalues);
        }

        /// <summary>
        /// In-betweens or "incrementals" are calculated based on the function
        /// curve data coming from the Maya rig. The curve math is an absolute
        /// function and requires the slope and domain range of the curve. As
        /// the driver target's weight changes, the in-between target's weight
        /// interpolates based on the curve slope and domain. The X axis
        /// represents the in-between's driver target weight value while the
        /// final value represents the set driven key curve from Maya. The curve
        /// data is exported from Maya and eventually deserialized here.
        /// </summary>
        /// <param name="blendShapeValues">Blend shape values to update.</param>
        private void ProcessInBetweens(float[] blendShapeValues)
        {
            if (_rigLogic.inBetweens == null)
            {
                return;
            }

            int maxIndex = blendShapeValues.Length;
            foreach (var inBetween in _rigLogic.inBetweens)
            {
                if (inBetween.drivenIndex >= maxIndex)
                {
                    continue;
                }
                // "x" is the input into a y = mx + b style equation
                var x = blendShapeValues[inBetween.driverIndex] * _PERCENT_TO_DECIMAL;
                var finalBlendWeight = 0.0f;
                if (x >= inBetween.domainStart &&
                    x <= inBetween.domainEnd)
                {
                    finalBlendWeight = inBetween.slope * -1.0f *
                        Mathf.Abs(x - inBetween.offset_x) +
                        inBetween.offset_y;
                }

                blendShapeValues[inBetween.drivenIndex] =
                    finalBlendWeight * _DECIMAL_TO_PERCENT;
            }
        }

        /// <summary>
        /// For each combination target, get the weight of its driver targets
        /// and multiply each driver's weight together to calculate the weight
        /// of the combination target. Once the weight is calculated we set the
        /// driven index to affect the correct blendshape.
        /// </summary>
        /// <param name="blendShapeValues">Blend shape values to update.</param>
        private void ProcessCombinations(float[] blendShapeValues)
        {
            if (_rigLogic.combinations == null)
            {
                return;
            }

            int maxIndex = blendShapeValues.Length;
            foreach (var combination in _rigLogic.combinations)
            {
                if (combination.drivenIndex >= maxIndex)
                {
                    continue;
                }
                float finalWeight = 1.0f;

                // NOTE: The current combination method is set to multiplication - future support should
                // include smooth and min/max
                int numDrivers = combination.driverIndices.Length;
                for (int i = 0; i < numDrivers; ++i)
                {
                    finalWeight *=
                        blendShapeValues[combination.driverIndices[i]] * _PERCENT_TO_DECIMAL;
                }

                blendShapeValues[combination.drivenIndex] =
                    finalWeight * _DECIMAL_TO_PERCENT;
            }
        }
    }
}
