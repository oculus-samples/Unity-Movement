// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Movement.UI
{
    /// <summary>
    /// Turns blend shapes manus based on function call,
    /// usually hooked up via UI.
    /// </summary>
    public class BlendshapesMenuToggle : MonoBehaviour
    {
        /// <summary>
        /// Blend shapes menus to turn on/off.
        /// </summary>
        [SerializeField]
        [Tooltip(BlendshapesMenuToggleTooltips.BlendShapesMenus)]
        protected GameObject[] _blendShapesMenus;

        private void Awake()
        {
            Assert.IsTrue(_blendShapesMenus != null &&
                _blendShapesMenus.Length > 0);
        }

        /// <summary>
        /// Toggles blendshapes menus on and off. Called via UI in scene.
        /// </summary>
        public void ToggleBlendshapesMenuEnableState(PointerEvent pointerEvent)
        {
            foreach (var blendShapesMenu in _blendShapesMenus)
            {
                bool wasActive = blendShapesMenu.activeInHierarchy;
                blendShapesMenu.SetActive(!wasActive);
            }
        }
    }
}
