// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Movement.AnimationRigging;
using Oculus.Movement.Effects;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Movement.UI
{
    /// <summary>
    /// Setup for performing initial hip pinning calibration.
    /// </summary>
    public class HipPinningConstraintCalibration : MonoBehaviour
    {
        /// <summary>
        /// The hip pinning constraints.
        /// </summary>
        [Header("Hip Pinning Objects")]
        [SerializeField]
        [Tooltip(HipPinningConstraintCalibrationTooltips.HipPinningConstraints)]
        protected HipPinningConstraint[] _hipPinningConstraints;

        /// <summary>
        /// The game object that contains the mesh renderers for the main hip pinning target.
        /// </summary>
        [SerializeField]
        [Tooltip(HipPinningConstraintCalibrationTooltips.MainHipPinningTargetRenderer)]
        protected GameObject _mainHipPinningTargetRenderer;

        /// <summary>
        /// The game object that contains the mesh renderers for the mirrored hip pinning target.
        /// </summary>
        [SerializeField]
        [Tooltip(HipPinningConstraintCalibrationTooltips.MirrorHipPinningTargetRenderer)]
        protected GameObject _mirrorHipPinningTargetRenderer;

        /// <summary>
        /// The game object that contains the mesh renderers for the main character.
        /// </summary>
        [SerializeField]
        [Tooltip(HipPinningConstraintCalibrationTooltips.MainCharacterRenderer)]
        protected GameObject _mainCharacterRenderer;

        /// <summary>
        /// The game object that contains the mesh renderers for the mirrored character.
        /// </summary>
        [SerializeField]
        [Tooltip(HipPinningConstraintCalibrationTooltips.MirroredCharacterRenderer)]
        protected GameObject _mirroredCharacterRenderer;

        /// <summary>
        /// The skeletal tracking data provider for the interface character.
        /// </summary>
        [SerializeField]
        [Tooltip(HipPinningConstraintCalibrationTooltips.Skeleton)]
        protected OVRCustomSkeleton _skeleton;

        /// <summary>
        /// The game object that contains the renderers for this calibration menu.
        /// </summary>
        [Header("UI"), Space(10), SerializeField]
        [Tooltip(HipPinningConstraintCalibrationTooltips.CalibrateMenu)]
        protected GameObject _calibrateMenu;

        private void Start()
        {
            foreach (var hipPinningConstraint in _hipPinningConstraints)
            {
                Assert.IsNotNull(hipPinningConstraint);
            }

            Assert.IsNotNull(_mainCharacterRenderer);
            Assert.IsNotNull(_mirroredCharacterRenderer);
            Assert.IsNotNull(_mainHipPinningTargetRenderer);
            Assert.IsNotNull(_mirrorHipPinningTargetRenderer);
            Assert.IsNotNull(_skeleton);
            Assert.IsNotNull(_calibrateMenu);

            foreach (var hipPinningConstraint in _hipPinningConstraints)
            {
                hipPinningConstraint.data.OnExitHipPinningArea += ExitHipPinning;
            }
            ResetCalibrationMenuScene();
        }

        private void OnDestroy()
        {
            foreach (var hipPinningConstraint in _hipPinningConstraints)
            {
                hipPinningConstraint.data.OnExitHipPinningArea -= ExitHipPinning;
            }
        }

        private void ResetCalibrationMenuScene()
        {
            _mainHipPinningTargetRenderer.SetActive(false);
            _mirrorHipPinningTargetRenderer.SetActive(false);
            _mainCharacterRenderer.SetActive(false);
            _mirroredCharacterRenderer.SetActive(false);
            _calibrateMenu.SetActive(true);
        }

        private void ExitHipPinning(HipPinningConstraintTarget target)
        {
            ResetCalibrationMenuScene();
        }

        /// <summary>
        /// Calibrate the hip pinning target transform with the current tracked hip position
        /// </summary>
        public void Calibrate()
        {
            var isFullBody = _skeleton.GetSkeletonType() == OVRSkeleton.SkeletonType.FullBody;
            Vector3 hipTranslation = _skeleton.CustomBones[isFullBody ?
                (int)OVRSkeleton.BoneId.FullBody_Hips :
                (int)OVRSkeleton.BoneId.Body_Hips].localPosition;
            var mainChairTransform = _mainHipPinningTargetRenderer.transform;
            mainChairTransform.localPosition =
                new Vector3(hipTranslation.x, mainChairTransform.localPosition.y, hipTranslation.z);

            foreach (var hipPinningConstraint in _hipPinningConstraints)
            {
                hipPinningConstraint.data.CalibrateInitialHipHeight(hipTranslation);
                hipPinningConstraint.data.AssignClosestHipPinningTarget(hipTranslation);
            }

            _mainHipPinningTargetRenderer.SetActive(true);
            _mirrorHipPinningTargetRenderer.SetActive(true);
            _mainCharacterRenderer.SetActive(true);
            _mirroredCharacterRenderer.SetActive(true);
            _calibrateMenu.SetActive(false);
        }
    }
}
