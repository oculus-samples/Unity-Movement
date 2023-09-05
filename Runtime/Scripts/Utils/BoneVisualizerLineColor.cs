// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace Oculus.Movement.Utils
{
    /// <summary>
    /// Add next to a <see cref="BoneVisualizer"/> script to change it's color.
    /// </summary>
    public class BoneVisualizerLineColor : MonoBehaviour
    {
        /// <summary>
        /// <see cref="BoneVisualizer"/> to change the color of
        /// </summary>
        [SerializeField]
        [Tooltip(BoneVisualizerLineColorTooltips.BoneVisualizer)]
        private BoneVisualizer _boneVisualizer;

        /// <summary>
        /// The color to change the <see cref="BoneVisualizer"/> to
        /// </summary>
        [SerializeField]
        [Tooltip(BoneVisualizerLineColorTooltips.LineColor)]
        private Color _lineColor = Color.white;

        /// <summary>
        /// Used to account for Unity's Material copy during get access
        /// </summary>
        private Material _cachedLineMaterial;

        /// <summary>
        /// gets/sets color of all <see cref="_boneVisualizer"/>'s bones
        /// </summary>
        public Color LineColor
        {
            get
            {
                return _lineColor;
            }
            set
            {
                _lineColor = value;
                if (_boneVisualizer != null)
                {
                    foreach (var kvp in _boneVisualizer.BoneVisualRenderers)
                    {
                        SetLineColorToCachedMaterial(kvp.Value);
                    }
                }
            }
        }

        private void Reset()
        {
            _boneVisualizer = GetComponent<BoneVisualizer>();
        }

        private void OnValidate()
        {
            LineColor = _lineColor;
        }

        private void Awake()
        {
            if (_boneVisualizer == null)
            {
                Debug.LogWarning($"{nameof(_boneVisualizer)} not initialized, auto-populating");
                _boneVisualizer = GetComponent<BoneVisualizer>();
            }
            _boneVisualizer.OnNewLine += SetLineColor;
        }

        /// <summary>
        /// Will change every bone's color to <see cref="_lineColor"/>
        /// </summary>
        /// <param name="boneA"></param>
        /// <param name="boneB"></param>
        /// <param name="gameObject"></param>
        public void SetLineColor(int boneA, int boneB, GameObject gameObject)
        {
            SetLineColorToCachedMaterial(gameObject.GetComponent<Renderer>());
        }

        private void SetLineColorToCachedMaterial(Renderer renderer)
        {
            if (_cachedLineMaterial == null)
            {
                _cachedLineMaterial = renderer.material;
            }
            _cachedLineMaterial.color = _lineColor;
            renderer.material = _cachedLineMaterial;
        }

        private void OnDestroy()
        {
            Destroy(_cachedLineMaterial);
        }
    }
}
