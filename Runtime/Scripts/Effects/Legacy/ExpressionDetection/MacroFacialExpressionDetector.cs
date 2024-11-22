// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Movement.Effects.Deprecated
{
    /// <summary>
    /// This rudimentary macro expression detector (macro being smile, etc).
    /// It is not meant to be robust or advanced.
    /// </summary>
    public class MacroFacialExpressionDetector : MonoBehaviour
    {
        /// <summary>
        /// A list of macro expressions to test against.
        /// </summary>
        public enum MacroExpressionType
        {
            /// <summary>
            /// Smile
            /// </summary>
            Smile = 0,
            /// <summary>
            /// Frowning
            /// </summary>
            Frown = 1
        }

        /// <summary>
        /// States for macro expressions test for.
        /// </summary>
        public enum MacroExpressionState
        {
            /// <summary>
            /// When state becomes active.
            /// </summary>
            Active = 0,
            /// <summary>
            /// When state maintains itself.
            /// </summary>
            Maintain = 1,
            /// <summary>
            /// When state becomes inactive.
            /// </summary>
            Inactive = 2
        }

        /// <summary>
        /// Entry and exit thresholds for expressions.
        /// </summary>
        [Serializable]
        private class ExpressionThresholds
        {
            /// <summary>
            /// Face expression blendshape to evaluate.
            /// </summary>
            [Tooltip(FacialExpressionDetectorTooltips.ExpressionThresholdsTooltips.FaceExpression)]
            public OVRFaceExpressions.FaceExpression FaceExpression;

            /// <summary>
            /// Threshold to enter expression, usually high.
            /// </summary>
            [Tooltip(FacialExpressionDetectorTooltips.ExpressionThresholdsTooltips.EntryThreshold)]
            public float EntryThreshold;

            /// <summary>
            /// Threshold to exit expression, usually low.
            /// </summary>
            [Tooltip(FacialExpressionDetectorTooltips.ExpressionThresholdsTooltips.ExitThreshold)]
            public float ExitThreshold;
        };

        /// <summary>
        /// Expression metadata associated with macro expression.
        /// </summary>
        [Serializable]
        private class MacroExpressionData
        {
            /// <summary>
            /// Array of thresholds to test for macro expression detection.
            /// </summary>
            [Tooltip(FacialExpressionDetectorTooltips.ExpressionDataTooltips.Thresholds)]
            public ExpressionThresholds[] Thresholds;

            /// <summary>
            /// Expression type (macro) detected.
            /// </summary>
            [Tooltip(FacialExpressionDetectorTooltips.ExpressionDataTooltips.ExpressionTypeDetected)]
            public MacroExpressionType MacroExpressionTypeDetected;
        }

        /// <summary>
        /// OVRFaceExpressions component.
        /// </summary>
        [SerializeField]
        [Tooltip(FacialExpressionDetectorTooltips.OvrFaceExpressions)]
        protected OVRFaceExpressions _ovrFaceExpressions;

        /// <summary>
        /// Array of macro expressions test for.
        /// </summary>
        [SerializeField]
        [Tooltip(FacialExpressionDetectorTooltips.ExpressionsToEvaluate)]
        private MacroExpressionData[] _macroExpressionsToEvaluate;

        /// <summary>
        /// State change event args for macro expression.
        /// </summary>
        public class MacroExpressionStateChangeEventArgs
        {
            /// <summary>
            /// Macro expression type associated with state change.
            /// </summary>
            public readonly MacroExpressionType Expression;

            /// <summary>
            /// Current macro expression state.
            /// </summary>
            public readonly MacroExpressionState State;

            /// <summary>
            /// Previous macro expression state.
            /// </summary>
            public readonly MacroExpressionState PreviousState;

            /// <summary>
            /// Min value of blendshapes associated with macro expression.
            /// This "floor" gives an indication of how strong the macro expression is.
            /// </summary>
            public readonly float MinExpressionValue;

            /// <summary>
            /// Constructor for MacroExpressionStateChangeEventArgs.
            /// </summary>
            /// <param name="expression">Macro expression type associated with state change.</param>
            /// <param name="state">Current macro expression state.</param>
            /// <param name="previousState">Previous macro expression state.</param>
            /// <param name="minExpressionValue">Min value of blendshapes associated with macro expression.</param>
            public MacroExpressionStateChangeEventArgs(
                MacroExpressionType expression,
                MacroExpressionState state,
                MacroExpressionState previousState,
                float minExpressionValue)
            {
                Expression = expression;
                State = state;
                PreviousState = previousState;
                MinExpressionValue = minExpressionValue;
            }
        }

        /// <summary>
        /// Fires when expressions state changes.
        /// </summary>
        public event Action<MacroExpressionStateChangeEventArgs> MacroExpressionStateChange;

        private Dictionary<MacroExpressionType, MacroExpressionState> _macroExpressionTypeToState =
            new Dictionary<MacroExpressionType, MacroExpressionState>();

        /// <summary>
        /// Returns the strength of a macro expression.
        /// </summary>
        public Dictionary<MacroExpressionType, float> MacroExpressionTypeToStrength { get; private set; }

        private void Awake()
        {
            Assert.IsNotNull(_ovrFaceExpressions);
            Assert.IsTrue(_macroExpressionsToEvaluate != null &&
                _macroExpressionsToEvaluate.Length > 0);

            MacroExpressionTypeToStrength = new Dictionary<MacroExpressionType, float>();
            foreach (var macroExpr in _macroExpressionsToEvaluate)
            {
                MacroExpressionTypeToStrength.Add(macroExpr.MacroExpressionTypeDetected, 0.0f);
            }
        }

        private void Update()
        {
            if (!_ovrFaceExpressions.FaceTrackingEnabled ||
                !_ovrFaceExpressions.ValidExpressions)
            {
                return;
            }

            foreach (var macroExpression in _macroExpressionsToEvaluate)
            {
                UpdateExpressionState(macroExpression);
            }
        }

        private void UpdateExpressionState(MacroExpressionData macroExpressionData)
        {
            var macroExpressionType = macroExpressionData.MacroExpressionTypeDetected;

            var currentMacroExpressionState = MacroExpressionState.Inactive;
            if (!_macroExpressionTypeToState.ContainsKey(macroExpressionType))
            {
                _macroExpressionTypeToState[macroExpressionType] = currentMacroExpressionState;
            }
            else
            {
                currentMacroExpressionState = _macroExpressionTypeToState[macroExpressionType];
            }

            var newMacroExpressionState = GetNewMacroExpressionState(
                macroExpressionData.Thresholds,
                currentMacroExpressionState);

            _macroExpressionTypeToState[macroExpressionType] = newMacroExpressionState;
            float lowestFaceExpressionValue = GetLowestFaceExpressionValue(
                macroExpressionData.Thresholds);
            MacroExpressionTypeToStrength[macroExpressionType] = lowestFaceExpressionValue;
            CheckExpressionStateEventChange(
                currentMacroExpressionState,
                newMacroExpressionState,
                macroExpressionType,
                lowestFaceExpressionValue);
        }

        private MacroExpressionState GetNewMacroExpressionState(
            ExpressionThresholds[] thresholds,
            MacroExpressionState currentExpressionState)
        {
            bool thresholdsPass = CheckAgainstThresholds(
                thresholds,
                currentExpressionState == MacroExpressionState.Inactive);
            if (currentExpressionState == MacroExpressionState.Inactive &&
                thresholdsPass)
            {
                currentExpressionState = MacroExpressionState.Active;
            }
            else if (currentExpressionState == MacroExpressionState.Maintain &&
                !thresholdsPass)
            {
                currentExpressionState = MacroExpressionState.Inactive;
            }
            else if (currentExpressionState == MacroExpressionState.Active)
            {
                currentExpressionState = thresholdsPass ?
                    MacroExpressionState.Maintain :
                    MacroExpressionState.Inactive;
            }

            return currentExpressionState;
        }

        private bool CheckAgainstThresholds(
            ExpressionThresholds[] thresholds,
            bool comingFromInactiveState)
        {
            bool thresholdsPass = true;

            // If we are in inactive state, check fails if
            // one threshold does not pass enter threshold.
            // If we are not in an inactive state, check fails if
            // one threshold goes lower than exit threshold.
            foreach (var threshold in thresholds)
            {
                var faceExpression = threshold.FaceExpression;
                var expressionValue = _ovrFaceExpressions[faceExpression];

                if (comingFromInactiveState &&
                    expressionValue < threshold.EntryThreshold)
                {
                    thresholdsPass = false;
                }
                else if (!comingFromInactiveState &&
                    expressionValue < threshold.ExitThreshold)
                {
                    thresholdsPass = false;
                }
            }

            return thresholdsPass;
        }

        private float GetLowestFaceExpressionValue(
            ExpressionThresholds[] thresholds)
        {
            float? lowestValue = null;
            foreach (var threshold in thresholds)
            {
                var faceExpression = threshold.FaceExpression;
                var expressionValue = _ovrFaceExpressions[faceExpression];
                if (!lowestValue.HasValue ||
                    lowestValue.Value > expressionValue)
                {
                    lowestValue = expressionValue;
                }
            }
            return lowestValue.HasValue ? lowestValue.Value : 0.0f;
        }

        /// <summary>
        /// Fire a new event if it's a state change, or if
        /// it's maintain event. Latter is useful in case subscribers wish
        /// to keep themselves updated w.r.t. the lowest expression value.
        /// </summary>
        /// <param name="currentMacroExpressionState">Current macro expression state.</param>
        /// <param name="newMacroExpressionState">New macro expression state.</param>
        /// <param name="macroExpressionType">Macro expression type.</param>
        /// <param name="minFaceExpressionValue">Min face expression value.</param>
        private void CheckExpressionStateEventChange(
            MacroExpressionState currentMacroExpressionState,
            MacroExpressionState newMacroExpressionState,
            MacroExpressionType macroExpressionType,
            float minFaceExpressionValue)
        {
            if (currentMacroExpressionState != newMacroExpressionState ||
                newMacroExpressionState == MacroExpressionState.Maintain)
            {
                MacroExpressionStateChange?.Invoke(new MacroExpressionStateChangeEventArgs(
                        macroExpressionType,
                        newMacroExpressionState,
                        currentMacroExpressionState,
                        minFaceExpressionValue)
                    );
            }
        }
    }
}
