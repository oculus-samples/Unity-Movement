// Copyright (c) Meta Platforms, Inc. and affiliates.

using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using static OVRPlugin;

namespace Meta.XR.Movement.Samples
{
    /// <summary>
    /// Calls calibration to allow setting one's height.
    /// </summary>
    public class MovementSuggestBodyTrackingCalibration : MonoBehaviour
    {
        /// <summary>
        /// The text to modify once height is modified.
        /// </summary>
        [SerializeField]
        protected TMP_Text _worldText;

        /// <summary>
        /// The height to set in meters.
        /// </summary>
        [SerializeField]
        protected float _height = 1.80f;

        /// <summary>
        /// Allows calibration on startup.
        /// </summary>
        [SerializeField]
        protected bool _calibrateOnStartup;

        private const string _badCalibrationText = "Calibration error!";

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
            bool calibrationResult = SuggestBodyTrackingCalibrationOverride(calibrationInfo);

            UpdateText(calibrationResult);
        }

        private void UpdateText(bool calibrationResult)
        {
            if (!calibrationResult)
            {
                _worldText.text = _badCalibrationText;
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
