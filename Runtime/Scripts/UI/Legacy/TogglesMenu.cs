// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction;
using Oculus.Movement.Effects;
using Oculus.Movement.Effects.Deprecated;
using Oculus.Movement.Tracking.Deprecated;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Movement.UI.Deprecated
{
    /// <summary>
    /// Toggles features on and off.
    /// </summary>
    public class TogglesMenu : MonoBehaviour
    {
        /// <summary>
        /// Face tracking system to toggle features on.
        /// </summary>
        [SerializeField]
        [Tooltip(TogglesMenuTooltips.FaceTrackingSystem)]
        private FaceTrackingSystem _faceTrackingSystem;

        /// <summary>
        /// Recalculate normals component to control.
        /// </summary>
        [SerializeField]
        [Tooltip(TogglesMenuTooltips.RecalculateNormals)]
        private RecalculateNormals _recalculateNormals;

        /// <summary>
        /// Deformation logic component to control.
        /// </summary>
        [SerializeField]
        [Optional]
        [Tooltip(TogglesMenuTooltips.DeformationLogic)]
        private DeformationLogic _deformationLogic;

        /// <summary>
        /// Twist distribution logic components to control.
        /// </summary>
        [SerializeField]
        [Optional]
        [Tooltip(TogglesMenuTooltips.TwistDistributions)]
        private TwistDistribution[] _twistDistributions;

        /// <summary>
        /// Recalc normals text to update based on recalc normals
        /// toggle state.
        /// </summary>
        [SerializeField]
        [Tooltip(TogglesMenuTooltips.RecalculateNormalsText)]
        private TMPro.TextMeshPro _recalcNormalsText;

        /// <summary>
        /// Correctives text to update based on correctives
        /// toggle state.
        /// </summary>
        [SerializeField]
        [Tooltip(TogglesMenuTooltips.CorrectivesText)]
        private TMPro.TextMeshPro _correctivesText;

        /// <summary>
        /// Deformation text to update based on deformation
        /// toggle state.
        /// </summary>
        [SerializeField]
        [Optional]
        [Tooltip(TogglesMenuTooltips.DeformationText)]
        private TMPro.TextMeshPro _deformationText;

        /// <summary>
        /// Twist distribution text to update based on twist distribution
        /// toggle state.
        /// </summary>
        [SerializeField]
        [Optional]
        [Tooltip(TogglesMenuTooltips.TwistText)]
        private TMPro.TextMeshPro _twistText;

        private const string _RECALC_NORMALS_ON_TEXT =
            "Toggle Normal Recalc (on)";
        private const string _DEFORMATION_ON_TEXT =
            "Toggle Deformation (on)";
        private const string _TWIST_ON_TEXT =
            "Toggle Twist Distribution (on)";
        private const string _TOGGLE_CORRECTIVES_ON_TEXT =
            "Toggle Correctives (on)";

        private const string _RECALC_NORMALS_OFF_TEXT =
            "Toggle Normal Recalc (off)";
        private const string _DEFORMATION_OFF_TEXT =
            "Toggle Deformation (off)";
        private const string _TWIST_OFF_TEXT =
            "Toggle Twist Distribution (off)";
        private const string _TOGGLE_CORRECTIVES_OFF_TEXT =
            "Toggle Correctives (off)";

        private void Awake()
        {
            Assert.IsNotNull(_faceTrackingSystem);
            Assert.IsNotNull(_recalculateNormals);
            Assert.IsNotNull(_recalcNormalsText);
            Assert.IsNotNull(_correctivesText);
            if (_deformationLogic)
            {
                Assert.IsNotNull(_deformationText);
            }
        }

        private void Start()
        {
            UpdateCorrectivesButtonText();
            UpdateNormalRecalcButtonText();
            if (_deformationLogic)
            {
                UpdateDeformationButtonText();
            }
        }

        /// <summary>
        /// Toggles correctives on and off.
        /// </summary>
        public void ToggleCorrectives()
        {
            _faceTrackingSystem.CorrectivesEnabled =
                !_faceTrackingSystem.CorrectivesEnabled;
            UpdateCorrectivesButtonText();
        }

        private void UpdateCorrectivesButtonText()
        {
            _correctivesText.text =
                _faceTrackingSystem.CorrectivesEnabled ?
                _TOGGLE_CORRECTIVES_ON_TEXT :
                _TOGGLE_CORRECTIVES_OFF_TEXT;
        }

        /// <summary>
        /// Toggles normal recalculation on and off.
        /// </summary>
        public void ToggleNormalRecalc()
        {
            _recalculateNormals.RunRecalculation =
                !_recalculateNormals.RunRecalculation;
            UpdateNormalRecalcButtonText();
        }

        private void UpdateNormalRecalcButtonText()
        {
            _recalcNormalsText.text =
                _recalculateNormals.RunRecalculation ?
                _RECALC_NORMALS_ON_TEXT :
                _RECALC_NORMALS_OFF_TEXT;
        }

        /// <summary>
        /// Toggles deformation on and off.
        /// </summary>
        public void ToggleDeformation()
        {
            _deformationLogic.enabled =
                !_deformationLogic.enabled;
            UpdateDeformationButtonText();
        }

        private void UpdateDeformationButtonText()
        {
            _deformationText.text =
                _deformationLogic.enabled ?
                    _DEFORMATION_ON_TEXT :
                    _DEFORMATION_OFF_TEXT;
        }

        /// <summary>
        /// Toggles twist on and off.
        /// </summary>
        public void ToggleTwist()
        {
            foreach (var twist in _twistDistributions)
            {
                twist.enabled = !twist.enabled;
            }
            UpdateTwistButtonText();
        }

        private void UpdateTwistButtonText()
        {
            _twistText.text =
                _twistDistributions[0].enabled ?
                    _TWIST_ON_TEXT :
                    _TWIST_OFF_TEXT;
        }
    }
}
