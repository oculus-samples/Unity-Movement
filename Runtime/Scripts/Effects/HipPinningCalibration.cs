// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Movement.Effects
{
    /// <summary>
    /// Setup for performing initial hip pinning calibration.
    /// </summary>
    public class HipPinningCalibration : MonoBehaviour
    {
        /// <summary>
        /// The hip pinning target component for the main character.
        /// </summary>
        [Header("Hip Pinning Objects"), SerializeField]
        [Tooltip(HipPinningCalibrationTooltips.MainChairProp)]
        protected HipPinningTarget _mainChairProp;

        /// <summary>
        /// The hip pinning target component for the mirrored character.
        /// </summary>
        [SerializeField]
        [Tooltip(HipPinningCalibrationTooltips.MirroredChairProp)]
        protected HipPinningTarget _mirroredChairProp;

        /// <summary>
        /// The hip pinning logic component for the main character.
        /// </summary>
        [SerializeField]
        [Tooltip(HipPinningCalibrationTooltips.MainCharacterHipPinning)]
        protected HipPinningLogic _mainCharacterHipPinning;

        /// <summary>
        /// The grounding logic component for the main character.
        /// </summary>
        [SerializeField]
        [Tooltip(HipPinningCalibrationTooltips.MainCharacterGrounding)]
        protected GroundingLogic _mainCharacterGrounding;

        /// <summary>
        /// The hip pinning logic component for the mirrored character.
        /// </summary>
        [SerializeField]
        [Tooltip(HipPinningCalibrationTooltips.MirroredCharacterHipPinning)]
        protected HipPinningLogic _mirroredCharacterHipPinning;

        /// <summary>
        /// The grounding logic component for the mirrored character.
        /// </summary>
        [SerializeField]
        [Tooltip(HipPinningCalibrationTooltips.MirroredCharacterGrounding)]
        protected GroundingLogic _mirroredCharacterGrounding;

        /// <summary>
        /// The game object that contains the mesh renderers for the main character.
        /// </summary>
        [SerializeField]
        [Tooltip(HipPinningCalibrationTooltips.MainCharacterRenderer)]
        protected GameObject _mainCharacterRenderer;

        /// <summary>
        /// The game object that contains the mesh renderers for the mirrored character.
        /// </summary>
        [SerializeField]
        [Tooltip(HipPinningCalibrationTooltips.MirroredCharacterRenderer)]
        protected GameObject _mirroredCharacterRenderer;

        /// <summary>
        /// The skeletal tracking data provider for the interface character.
        /// </summary>
        [SerializeField]
        [Tooltip(HipPinningCalibrationTooltips.DataProvider)]
        protected OVRCustomSkeleton _skeleton;

        /// <summary>
        /// The game object that contains the renderers for this calibration menu.
        /// </summary>
        [Header("UI"), Space(10), SerializeField]
        [Tooltip(HipPinningCalibrationTooltips.CalibrateMenu)]
        protected GameObject _calibrateMenu;

        private void Start()
        {
            Assert.IsNotNull(_mainChairProp);
            Assert.IsNotNull(_mirroredChairProp);
            Assert.IsNotNull(_mainCharacterHipPinning);
            Assert.IsNotNull(_mirroredCharacterHipPinning);
            Assert.IsNotNull(_mainCharacterRenderer);
            Assert.IsNotNull(_mirroredCharacterRenderer);
            Assert.IsNotNull(_skeleton);
            Assert.IsNotNull(_calibrateMenu);

            _mirroredCharacterHipPinning.OnExitHipPinningArea += ExitHipPinning;
            ResetCalibrationMenuScene();
        }

        private void OnDestroy()
        {
            _mirroredCharacterHipPinning.OnExitHipPinningArea -= ExitHipPinning;
        }

        private void ResetCalibrationMenuScene()
        {
            _mainChairProp.ChairObject.SetActive(false);
            _mirroredChairProp.ChairObject.SetActive(false);
            _mainCharacterRenderer.SetActive(true);
            _mirroredCharacterRenderer.SetActive(false);
            _mainCharacterHipPinning.SetHipPinningActive(false);
            _calibrateMenu.SetActive(true);
        }

        private void ExitHipPinning(HipPinningTarget target)
        {
            ResetCalibrationMenuScene();
        }

        /// <summary>
        /// Calibrate the hip pinning target transform with the current tracked hip position
        /// </summary>
        public void Calibrate()
        {
            Vector3 hipTranslation = _skeleton.CustomBones[(int)OVRSkeleton.BoneId.Body_Hips].localPosition;
            var mainChairTransform = _mainChairProp.transform;
            var mirroredChairTransform = _mirroredChairProp.transform;
            mainChairTransform.localPosition =
                new Vector3(hipTranslation.x, mainChairTransform.localPosition.y, hipTranslation.z);
            mirroredChairTransform.localPosition =
                new Vector3(-hipTranslation.x, mirroredChairTransform.localPosition.y, hipTranslation.z);

            _mainChairProp.ChairObject.SetActive(true);
            _mirroredChairProp.ChairObject.SetActive(true);

            _mainCharacterRenderer.SetActive(true);
            _mirroredCharacterRenderer.SetActive(true);

            _mainCharacterHipPinning.AssignClosestHipPinningTarget(hipTranslation);
            _mirroredCharacterHipPinning.AssignClosestHipPinningTarget(hipTranslation);
            _mainCharacterHipPinning.CalibrateInitialHipHeight(hipTranslation);
            _mirroredCharacterHipPinning.CalibrateInitialHipHeight(hipTranslation);
            _mainCharacterHipPinning.SetHipPinningActive(true);
            _mirroredCharacterHipPinning.SetHipPinningActive(true);
            _mainCharacterGrounding.Setup();
            _mirroredCharacterGrounding.Setup();

            _calibrateMenu.SetActive(false);
        }
    }
}
