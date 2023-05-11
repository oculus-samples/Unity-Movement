// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;
using static OVRFaceExpressions;

namespace Oculus.Movement.UI
{
    /// <summary>
    /// Renders face expressions via TMPPro. Filters ones that are
    /// too low.
    /// </summary>
    public class BlendshapesMenuVisual : MonoBehaviour
    {
        /// <summary>
        /// Face expressions component which is queried.
        /// </summary>
        [SerializeField]
        [Tooltip(BlendshapeMenuVisualTooltips.OvrFaceExpressions)]
        protected OVRFaceExpressions _ovrFaceExpressions;

        /// <summary>
        /// Text mesh pro visual for blendshape values.
        /// </summary>
        [SerializeField]
        [Tooltip(BlendshapeMenuVisualTooltips.WorldText)]
        protected TMPro.TextMeshPro _worldText;

        /// <summary>
        /// Threshold that blendshapes must passed before
        /// being rendered.
        /// </summary>
        [SerializeField]
        [Tooltip(BlendshapeMenuVisualTooltips.MinBlendshapeThreshold)]
        protected float _minBlendshapeThreshold = 0.1f;

        /// <summary>
        /// Can be used to filter blendshapes, in case all
        /// don't need to be rendered.
        /// </summary>
        [SerializeField, Optional]
        [Tooltip(BlendshapeMenuVisualTooltips.FilterArray)]
        protected string[] _filterArray;

        /// <summary>
        /// Prefix for rendered text.
        /// </summary>
        [SerializeField]
        [Tooltip(BlendshapeMenuVisualTooltips.ExpressionsPrefix)]
        protected string _expressionsPrefix = "Blendshapes:\n";

        private const string _NO_EXPRESSIONS_TEXT = "N/A";

        private void Awake()
        {
            Assert.IsNotNull(_ovrFaceExpressions);
            Assert.IsNotNull(_worldText);
        }

        private void Update()
        {
            if (!_ovrFaceExpressions.isActiveAndEnabled ||
                !_ovrFaceExpressions.FaceTrackingEnabled ||
                !_ovrFaceExpressions.ValidExpressions)
            {
                _worldText.text = _NO_EXPRESSIONS_TEXT;
                return;
            }

            StringBuilder expressionsText = new StringBuilder();
            expressionsText.Append(_expressionsPrefix);

            for (var faceExpression = FaceExpression.BrowLowererL;
                faceExpression < FaceExpression.Max;
                faceExpression++)
            {
                var expressionValue = _ovrFaceExpressions[faceExpression];
                if (expressionValue < _minBlendshapeThreshold)
                {
                    continue;
                }
                if (!ExpressionPassesFilter(faceExpression.ToString()))
                {
                    continue;
                }
                expressionsText.Append($"{faceExpression}:" +
                    $"{expressionValue.ToString("F4")}\n");
            }

            _worldText.text = expressionsText.ToString();
        }

        private bool ExpressionPassesFilter(string expressionName)
        {
            // Skip if no filter required.
            if (_filterArray == null || _filterArray.Length == 0)
            {
                return true;
            }

            var expressionLowercase = expressionName.ToLower();
            foreach (var filter in _filterArray)
            {
                if (expressionLowercase.Contains(filter))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
