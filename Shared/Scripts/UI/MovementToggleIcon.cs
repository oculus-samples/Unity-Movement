// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using UnityEngine.Assertions;

namespace Meta.XR.Movement.Samples
{
    /// <summary>
    /// Allows usage of a toggle icon.
    /// </summary>
    public class MovementToggleIcon : MonoBehaviour
    {
        /// <summary>
        /// Renderer that indicates toggle state.
        /// </summary>
        [SerializeField]
        protected Renderer _toggleRenderer;

        /// <summary>
        /// Select color.
        /// </summary>
        [SerializeField]
        protected Color _selectColor;

        /// <summary>
        /// Deselected color.
        /// </summary>
        [SerializeField]
        protected Color _deselectColor;

        private bool _selectState = false;

        private Material _toggleMaterial;

        private int _colorId;

        private void Awake()
        {
            Assert.IsNotNull(_toggleRenderer);

            _colorId = Shader.PropertyToID("_Color");
            _toggleMaterial = _toggleRenderer.material;
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
        public void ToggleSelectIcon()
        {
            _selectState = !_selectState;
            _toggleMaterial.SetColor(_colorId, _selectState ? _selectColor : _deselectColor);
        }

        public void DeselectIcon()
        {
            _selectState = false;
            _toggleMaterial.SetColor(_colorId, _selectState ? _selectColor : _deselectColor);
        }
    }
}
