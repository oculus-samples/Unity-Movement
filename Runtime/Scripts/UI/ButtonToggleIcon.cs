// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Movement.UI
{
    /// <summary>
    /// Allows usage of a button toggle icon.
    /// </summary>
    public class ButtonToggleIcon : MonoBehaviour
    {
        /// <summary>
        /// Object that indicates toggle state.
        /// </summary>
        [SerializeField]
        [Tooltip(ButtonToggleIconTooltips.OutlineObject)]
        protected GameObject _toggleObject;

        /// <summary>
        /// Select color.
        /// </summary>
        [SerializeField]
        [Tooltip(ButtonToggleIconTooltips.SelectColor)]
        protected Color _selectColor;

        /// <summary>
        /// Deselected color.
        /// </summary>
        [SerializeField]
        [Tooltip(ButtonToggleIconTooltips.DeselectColor)]
        protected Color _deselectColor;

        private bool _selectState = false;

        private Material _toggleMaterial;

        private int _colorId;

        private void Awake()
        {
            Assert.IsNotNull(_toggleObject);

            _colorId = Shader.PropertyToID("_Color");

            _toggleMaterial = _toggleObject.GetComponent<Renderer>().material;

            _toggleMaterial.SetColor(_colorId, _deselectColor);
        }

        private void OnDestroy()
        {
            if (_toggleMaterial != null)
            {
                Destroy(_toggleMaterial);
            }
        }

        /// <summary>
        /// Toggles the select icon state.
        /// </summary>
        public void ToggleSelectIcon(PointerEvent pointerEvent)
        {
            _selectState = !_selectState;
            _toggleMaterial.SetColor(_colorId, _selectState ?
                _selectColor : _deselectColor);
        }
    }
}
