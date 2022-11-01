// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Movement.Effects
{
    /// <summary>
    /// Reacts to smile detection by modifying the face material on
    /// the Aura asset.
    /// </summary>
    public class SmileEffect : MonoBehaviour
    {
        /// <summary>
        /// Returns the current state of if smile is enabled or disabled.
        /// </summary>
        public bool SmileEnabled { get; set; }

        /// <summary>
        /// Facial expression detector to query events from.
        /// </summary>
        [SerializeField]
        [Tooltip(SmileEffectTooltips.FacialExpressionDetector)]
        protected MacroFacialExpressionDetector _facialExpressionDetector;

        /// <summary>
        /// Material index to modify.
        /// </summary>
        [SerializeField]
        [Tooltip(SmileEffectTooltips.MaterialIndex)]
        protected int _materialIndex;

        /// <summary>
        /// Renderer of the face.
        /// </summary>
        [SerializeField]
        [Tooltip(SmileEffectTooltips.Renderer)]
        protected Renderer _renderer;

        /// <summary>
        /// Glow curve that modulates emission strength on face.
        /// </summary>
        [SerializeField]
        [Tooltip(SmileEffectTooltips.GlowCurve)]
        protected AnimationCurve _glowCurve;

        private Material[] _materials = null;
        private float _initialEmissionValue = 0.0f;
        private int _emissionStrengthId;

        private void Awake()
        {
            Assert.IsNotNull(_facialExpressionDetector);
            Assert.IsNotNull(_renderer);
            Assert.IsNotNull(_glowCurve);

            SmileEnabled = true;

            _materials = _renderer.materials;
            Assert.IsTrue(_materialIndex < _materials.Length);

            _emissionStrengthId = Shader.PropertyToID("_EmissionStrength");
            _initialEmissionValue = _materials[_materialIndex].GetFloat(_emissionStrengthId);

            _facialExpressionDetector.MacroExpressionStateChange +=
                MacroExpressionStateChange;
        }

        private void OnDestroy()
        {
            if (_materials != null)
            {
                foreach (var material in _materials)
                {
                    if (material)
                    {
                        Destroy(material);
                    }
                }
            }
        }

        private void MacroExpressionStateChange(
            MacroFacialExpressionDetector.MacroExpressionStateChangeEventArgs eventArgs)
        {
            if (eventArgs.State == MacroFacialExpressionDetector.MacroExpressionState.Active ||
                eventArgs.State == MacroFacialExpressionDetector.MacroExpressionState.Maintain)
            {
                _materials[_materialIndex].SetFloat(
                    _emissionStrengthId,
                    SmileEnabled ?
                        _glowCurve.Evaluate(eventArgs.MinExpressionValue) :
                        _initialEmissionValue);
            }
            else
            {
                _materials[_materialIndex].SetFloat(_emissionStrengthId, _initialEmissionValue);
            }
        }
    }
}
