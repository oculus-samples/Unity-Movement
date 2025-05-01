// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Movement.Retargeting;
using UnityEngine;

namespace Meta.XR.Movement.Samples
{
    /// <summary>
    /// Setup for performing initial hip pinning calibration.
    /// </summary>
    public class MovementHipPinningCalibration : MonoBehaviour
    {
        [SerializeField]
        private CharacterRetargeter _retargeter;

        /// <summary>
        /// The game object that contains the renderers for this calibration menu.
        /// </summary>
        [SerializeField]
        protected GameObject _calibrateMenu;

        [SerializeField, InspectorButton("Calibrate")]
        private bool _triggerCalibration;

        private HipPinningSkeletalProcessor _hipPinningProcessor;

        private void Start()
        {
            foreach (var processor in _retargeter.TargetProcessorContainers)
            {
                if (processor.CurrentProcessorType == TargetProcessor.ProcessorType.HipPinning)
                {
                    _hipPinningProcessor = processor.GetCurrentProcessor() as HipPinningSkeletalProcessor;
                }
            }

            if (_hipPinningProcessor != null)
            {
                _hipPinningProcessor.OnExitHipPinningArea += OnExitHipPinningArea;
            }

            ResetCalibrationMenuScene();
        }

        private void OnDestroy()
        {
            if (_hipPinningProcessor != null)
            {
                _hipPinningProcessor.OnExitHipPinningArea -= OnExitHipPinningArea;
            }
        }

        private void ResetCalibrationMenuScene()
        {
            _hipPinningProcessor.HideHipPinningObject();

            foreach (var processor in _retargeter.TargetProcessorContainers)
            {
                if (processor.CurrentProcessorType == TargetProcessor.ProcessorType.HipPinning)
                {
                    var hipPinningProcessor = (HipPinningSkeletalProcessor)processor.GetCurrentProcessor();
                    hipPinningProcessor.Weight = 0.0f;
                }
            }

            _calibrateMenu.SetActive(true);
        }

        private void OnExitHipPinningArea()
        {
            ResetCalibrationMenuScene();
        }

        /// <summary>
        /// Calibrate the hip pinning target transform with the current tracked hip position
        /// </summary>
        public void Calibrate()
        {
            _hipPinningProcessor.CalibrateHipPinningObjectPosition(new Vector3(1, 0, 1));
            _hipPinningProcessor.ShowHipPinningObject();

            foreach (var processor in _retargeter.TargetProcessorContainers)
            {
                if (processor.CurrentProcessorType == TargetProcessor.ProcessorType.HipPinning)
                {
                    var hipPinningProcessor = (HipPinningSkeletalProcessor)processor.GetCurrentProcessor();
                    hipPinningProcessor.Weight = 1.0f;
                }
            }

            _calibrateMenu.SetActive(false);
        }
    }
}
