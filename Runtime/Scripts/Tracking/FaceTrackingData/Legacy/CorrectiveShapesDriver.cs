// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Movement.Tracking.Deprecated
{
    /// <summary>
    /// Applies correctives to blendshapes based on a values obtained in a json file.
    /// The JSON loaded usually corresponds to a specific model.
    /// </summary>
    [DefaultExecutionOrder(10)]
    [ExecuteInEditMode]
    public class CorrectiveShapesDriver : MonoBehaviour
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
            [Tooltip(CorrectiveShapesDriverTooltips.InBetweenTooltips.DrivenIndex)]
            public int drivenIndex;

            /// <summary>
            /// The target blendshape index used for calculating the blendshape weight.
            /// </summary>
            [Tooltip(CorrectiveShapesDriverTooltips.InBetweenTooltips.DriverIndex)]
            public int driverIndex;

            /// <summary>
            /// The slope from the function curve of the in-between.
            /// </summary>
            [Tooltip(CorrectiveShapesDriverTooltips.InBetweenTooltips.Slope)]
            public float slope;

            /// <summary>
            /// The x offset from the function curve of the in-between.
            /// </summary>
            [Tooltip(CorrectiveShapesDriverTooltips.InBetweenTooltips.OffsetX)]
            public float offset_x;

            /// <summary>
            /// The y offset from the function curve of the in-between.
            /// </summary>
            [Tooltip(CorrectiveShapesDriverTooltips.InBetweenTooltips.OffsetY)]
            public float offset_y;

            /// <summary>
            /// The domain range start from the function curve of the in-between.
            /// </summary>
            [Tooltip(CorrectiveShapesDriverTooltips.InBetweenTooltips.DomainStart)]
            public float domainStart;

            /// <summary>
            /// The domain range end from the function curve of the in-between.
            /// </summary>
            [Tooltip(CorrectiveShapesDriverTooltips.InBetweenTooltips.DomainEnd)]
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
            [Tooltip(CorrectiveShapesDriverTooltips.CombinationTooltips.DrivenIndex)]
            public int drivenIndex;

            /// <summary>
            /// The blendshape indices used in calculating the blendshape weight for the driven index.
            /// </summary>
            [Tooltip(CorrectiveShapesDriverTooltips.CombinationTooltips.DriverIndices)]
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
            [Tooltip(CorrectiveShapesDriverTooltips.RigLogicDataTooltips.InBetweens)]
            public InBetween[] inBetweens;

            /// <summary>
            /// Array of all of the combinations data.
            /// </summary>
            [Tooltip(CorrectiveShapesDriverTooltips.RigLogicDataTooltips.Combinations)]
            public Combination[] combinations;
        }

        /// <summary>
        /// The skinned mesh renderer that contains the blendshapes to be corrected.
        /// </summary>
        [SerializeField]
        [Tooltip(CorrectiveShapesDriverTooltips.SkinnedMeshRendererToCorrect)]
        protected SkinnedMeshRenderer _skinnedMeshRendererToCorrect;

        /// <summary>
        /// The json file containing the in-betweens and combinations data.
        /// </summary>
        [SerializeField]
        [Tooltip(CorrectiveShapesDriverTooltips.CombinationShapesTextAsset)]
        protected TextAsset _combinationShapesTextAsset;

        private const float _PERCENT_TO_DECIMAL = 0.01f;
        private const float _DECIMAL_TO_PERCENT = 100.0f;

        private RigLogicData _rigLogic;

        private void Awake()
        {
            Assert.IsNotNull(_skinnedMeshRendererToCorrect);
            Assert.IsNotNull(_combinationShapesTextAsset);

            var assetText = _combinationShapesTextAsset.text;
            _rigLogic = JsonUtility.FromJson<RigLogicData>(assetText);
            Assert.IsNotNull(_rigLogic, "Could load combination shape data!");
        }

        /// <summary>
        /// Apply corrective blendshape weights to the skinned mesh renderer
        /// </summary>
        public void ApplyCorrectives()
        {
            if (_skinnedMeshRendererToCorrect == null || _rigLogic?.combinations == null)
            {
                return;
            }

            ProcessInBetweens();
            ProcessCombinations();
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
        private void ProcessInBetweens()
        {
            if (_rigLogic.inBetweens == null)
            {
                return;
            }

            foreach (var inBetween in _rigLogic.inBetweens)
            {
                if (inBetween.drivenIndex >=
                    _skinnedMeshRendererToCorrect.sharedMesh.blendShapeCount)
                {
                    continue;
                }
                // "x" is the input into a y = mx + b style equation
                var x = _skinnedMeshRendererToCorrect.GetBlendShapeWeight(
                    inBetween.driverIndex) * _PERCENT_TO_DECIMAL;
                var finalBlendWeight = 0.0f;
                if (x >= inBetween.domainStart &&
                    x <= inBetween.domainEnd)
                {
                    finalBlendWeight = inBetween.slope * -1.0f *
                        Mathf.Abs(x - inBetween.offset_x) +
                        inBetween.offset_y;
                }

                _skinnedMeshRendererToCorrect.SetBlendShapeWeight(inBetween.drivenIndex,
                    finalBlendWeight * _DECIMAL_TO_PERCENT);
            }
        }

        /// <summary>
        /// For each combination target, get the weight of its driver targets
        /// and multiply each driver's weight together to calculate the weight
        /// of the combination target. Once the weight is calculated we set the
        /// driven index to affect the correct blendshape.
        /// </summary>
        private void ProcessCombinations()
        {
            if (_rigLogic.combinations == null)
            {
                return;
            }

            foreach (var combination in _rigLogic.combinations)
            {
                if (combination.drivenIndex >=
                    _skinnedMeshRendererToCorrect.sharedMesh.blendShapeCount)
                {
                    continue;
                }
                float finalWeight = 1.0f;

                // NOTE: The current combination method is set to multiplication - future support should
                // include smooth and min/max
                int numDrivers = combination.driverIndices.Length;
                for (int i = 0; i < numDrivers; ++i)
                {
                    finalWeight *= _skinnedMeshRendererToCorrect.GetBlendShapeWeight(
                        combination.driverIndices[i]) * _PERCENT_TO_DECIMAL;
                }

                _skinnedMeshRendererToCorrect.SetBlendShapeWeight(combination.drivenIndex,
                    finalWeight * _DECIMAL_TO_PERCENT);
            }
        }
    }
}
