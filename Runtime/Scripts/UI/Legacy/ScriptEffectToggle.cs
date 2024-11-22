// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Movement.Effects.Deprecated;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Movement.UI.Deprecated
{
    /// <summary>
    /// Toggles component on/off.
    /// </summary>
    public class ScriptEffectToggle : MonoBehaviour
    {
        /// <summary>
        /// The component to be toggled.
        /// </summary>
        [SerializeField]
        [Tooltip(ScriptEffectToggleTooltips.ComponentToToggle)]
        protected SmileEffect _componentToToggle;

        /// <summary>
        /// The text component to be updated when the component is toggled.
        /// </summary>
        [SerializeField]
        [Tooltip(ScriptEffectToggleTooltips.TextToUpdate)]
        protected TMPro.TextMeshPro _textToUpdate;

        /// <summary>
        /// The feature text.
        /// </summary>
        [SerializeField]
        [Tooltip(ScriptEffectToggleTooltips.FeatureString)]
        protected string _featureString = "Smile effect";

        private void Awake()
        {
            Assert.IsNotNull(_componentToToggle);
            Assert.IsNotNull(_textToUpdate);
        }

        private void Start()
        {
            UpdateButtonText();
        }

        /// <summary>
        /// Toggles the component on/off.
        /// </summary>
        public void Toggle()
        {
            _componentToToggle.SmileEnabled =
                !_componentToToggle.SmileEnabled;
            UpdateButtonText();
        }

        private void UpdateButtonText()
        {
            _textToUpdate.text =
                _componentToToggle.SmileEnabled ?
                $"{_featureString} On" :
                $"{_featureString} Off";
        }
    }
}
