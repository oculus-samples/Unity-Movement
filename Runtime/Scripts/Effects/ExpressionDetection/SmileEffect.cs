// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using UnityEngine;
using UnityEngine.Assertions;
using static Oculus.Movement.Effects.MacroFacialExpressionDetector;

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
        [SerializeField]
        [Tooltip(SmileEffectTooltips.SmileEnabled)]
        protected bool _smileEnabled = false;
        /// <summary>
        /// Returns the current state of if smile is enabled or disabled.
        /// </summary>
        public bool SmileEnabled { get => _smileEnabled; set => _smileEnabled = value; }

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
        protected SkinnedMeshRenderer _renderer;

        /// <summary>
        /// Glow curve that modulates emission strength on face.
        /// </summary>
        [SerializeField]
        [Tooltip(SmileEffectTooltips.GlowCurve)]
        protected AnimationCurve _glowCurve;

        /// <summary>
        /// Animator to affect when smiling.
        /// </summary>
        [SerializeField]
        [Tooltip(SmileEffectTooltips.Animator)]
        protected Animator _animator;

        /// <summary>
        /// Delay until smile gets triggered (seconds).
        /// </summary>
        [SerializeField]
        [Tooltip(SmileEffectTooltips.SmileDelay)]
        protected float _smileDelay = 0.7f;

        /// <summary>
        /// State name for smile.
        /// </summary>
        [SerializeField]
        [Tooltip(SmileEffectTooltips.SmileStateNake)]
        protected string _smileStateName = "Smile";

        /// <summary>
        /// State name for reverse smile (when it "undoes" itself).
        /// </summary>
        [SerializeField]
        [Tooltip(SmileEffectTooltips.ReverseSmileStateName)]
        protected string _reverseSmileStateName = "ReverseSmile";

        private Material[] _materials = null;
        private float _lastEmissionValue = 0.0f;
        private float _lastLerpValueSmiling;
        private int _emissionStrengthId;

        private int _smileBoolId;
        private float _smileTime = -1.0f;

        private void Awake()
        {
            Assert.IsNotNull(_facialExpressionDetector);
            Assert.IsNotNull(_renderer);
            Assert.IsNotNull(_glowCurve);
            Assert.IsNotNull(_animator);

            _materials = _renderer.materials;
            Assert.IsTrue(_materialIndex < _materials.Length);
            _emissionStrengthId = Shader.PropertyToID("_EmissionStrength");
            _smileBoolId = Animator.StringToHash("Smile");
            _lastEmissionValue = _materials[_materialIndex].GetFloat(_emissionStrengthId);

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
            if (!SmileEnabled)
            {
                return;
            }
            if (eventArgs.Expression != MacroExpressionType.Happy)
            {
                return;
            }

            if (eventArgs.State == MacroExpressionState.Active)
            {
                // delay smile
                _smileTime = Time.time;
            }
            else if (eventArgs.State == MacroExpressionState.Inactive)
            {
                // stop delayed smile if one exists
                _smileTime = -1.0f;
                _animator.SetBool(_smileBoolId, false);
            }
        }

        private void Update()
        {
            if (_smileTime > 0.0f && Time.time > (_smileDelay + _smileTime))
            {
                _animator.SetBool(_smileBoolId, true);
                _smileTime = -1.0f;
            }

            if (!_facialExpressionDetector.MacroExpressionTypeToStrength.ContainsKey(MacroExpressionType.Happy))
            {
                return;
            }

            var currentStateInfo =_animator.GetCurrentAnimatorStateInfo(0);
            bool isSmiling = currentStateInfo.IsName(_smileStateName);
            bool isSmileReversing = currentStateInfo.IsName(_reverseSmileStateName);
            if (isSmiling || isSmileReversing)
            {
                // if unsmiling, use smile's last lerp value as upper-bound
                // This is necessary because someone can unsmile in the middle of the smile
                // animation, and we have to ramp down the lerp value based on the last
                // lerp value obtained while smiling.
                float currentLerpValue = isSmiling ?
                    currentStateInfo.normalizedTime :
                    _lastLerpValueSmiling*(1.0f - currentStateInfo.normalizedTime);
                if (isSmiling)
                {
                    _lastLerpValueSmiling = currentLerpValue;
                }

                var newEmissionValue = _glowCurve.Evaluate(Mathf.Clamp01(currentLerpValue));
                if (_lastEmissionValue != newEmissionValue)
                {
                    _materials[_materialIndex].SetFloat(_emissionStrengthId, newEmissionValue);
                    _lastEmissionValue = newEmissionValue;
                }
            }
        }
    }
}
