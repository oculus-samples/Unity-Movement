// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using UnityEngine.Assertions;
using static OVRPlugin;

namespace Oculus.Movement.Utils
{
    /// <summary>
    /// Calls calibration to allow setting one's height.
    /// </summary>
    public class SuggestBodyTrackingCalibrationButton : MonoBehaviour
    {
        /// <summary>
        /// The text to modify once height is modified.
        /// </summary>
        [SerializeField]
        [Tooltip(SuggestBodyTrackingCalibrationButtonTooltips.WorldText)]
        protected TMPro.TextMeshPro _worldText;
        /// <summary>
        /// The height to set in meters.
        /// </summary>
        [SerializeField]
        [Tooltip(SuggestBodyTrackingCalibrationButtonTooltips.Height)]
        protected float _height = 1.80f;
        /// <summary>
        /// Allows calibration on startup.
        /// </summary>
        [SerializeField]
        [Tooltip(SuggestBodyTrackingCalibrationButtonTooltips.CalibrateOnStartup)]
        protected bool _calibrateOnStartup = false;

        private const string _BAD_CALIBRATION_TEXT = "Calibration error!";

        private void Awake()
        {
            Assert.IsNotNull(_worldText);
            Assert.IsTrue(_height > Mathf.Epsilon, "Height must be greater than 0 meters.");

            if (_calibrateOnStartup)
            {
                CalibrateHeight();
            }
        }

        /// <summary>
        /// Calibrates height to the value specified by this script's field.
        /// </summary>
        public void CalibrateHeight()
        {
            BodyTrackingCalibrationInfo calibrationInfo;

            calibrationInfo.BodyHeight = _height;
            bool calibrationResult = OVRPlugin.SuggestBodyTrackingCalibrationOverride(calibrationInfo);

            UpdateText(calibrationResult);
        }

        private void UpdateText(bool calibrationResult)
        {
            if (!calibrationResult)
            {
                _worldText.text = _BAD_CALIBRATION_TEXT;
                return;
            }

            _worldText.text = $"Calibrated.\nHeight: {_height} m, imperial: {GetHeightImperial()}.";
        }

        private string GetHeightImperial()
        {
            const float feetPerMeter = 3.28084f;
            float feet = _height * feetPerMeter;
            float inches = 12.0f * (feet - (float)System.Math.Floor(feet));
            return $"{(int)feet}'{(int)inches}''";
        }
    }
}
