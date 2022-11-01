// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using UnityEngine.Rendering;

namespace Oculus.Movement.Validation
{
    /// <summary>
    /// Identify if layers are missing and identify incorrect graphics API.
    /// </summary>
    public class LayerAndVulkanValidation : RuntimeUnitValidation
    {
        /// <summary>
        /// Layers expected in scene.
        /// </summary>
        [ContextMenuItem(nameof(Test), nameof(Test))]
        [Tooltip(LayerAndVulkanValidationTooltips.ExpectedLayers)]
        [SerializeField]
        protected string[] _expectedLayers = { "HiddenMesh" };

#if UNITY_EDITOR
        /// <summary>
        /// Unity Editor resets component on creation, or after using the vertical "..." button.
        /// </summary>
        public override void Reset()
        {
            TestCases.AddRange(new TestCase[]
            {
                new TestCase(this, nameof(TestLayers)),
                new TestCase(this, nameof(TestVulkan)),
            });
            Transform errorList = transform.GetChild(0);
            for (int i = 0; i < errorList.childCount; i++)
            {
                var errorObject = errorList.GetChild(i).gameObject;
                const string goSetActive = nameof(GameObject.SetActive);
                BindDelegateWithBool(TestCases[i].OnTrue, errorObject, goSetActive, false);
                BindDelegateWithBool(TestCases[i].OnFalse, errorObject, goSetActive, true);
            }
        }
#endif

        /// <summary>
        /// Will show test output object before tests; allows the test UI to be hidden at edit time.
        /// </summary>
        public override void Test()
        {
            transform.GetChild(0).gameObject.SetActive(true);
            base.Test();
        }

        /// <summary>
        /// The Movement scene has a few layers required for rendering (or not-rendering) objects.
        /// </summary>
        /// <param name="vulkanFoundCallback">Handler called when result is known.</param>
        public void TestLayers(TestResultHandler layersFoundCallback)
        {
            bool allLayersArePresent = true;
            for (int i = 0; i < _expectedLayers.Length; i++)
            {
                int number = LayerMask.NameToLayer(_expectedLayers[i]);
                if (number == -1)
                {
                    allLayersArePresent = false;
                }
            }
            layersFoundCallback.Invoke(allLayersArePresent);
        }

        /// <summary>
        /// URP shaders and materials are only expected to work with the Vulkan graphics API.
        /// OpenGLES3 (default rendering) should work too, as should DirectX11 (Unity Editor).
        /// </summary>
        /// /// <param name="vulkanFoundCallback">Handler called when result is known.</param>
        public void TestVulkan(TestResultHandler vulkanFoundCallback)
        {
            bool vulkanFoundOrNotRequired = (GraphicsSettings.renderPipelineAsset == null)
                || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan
                || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11;
            vulkanFoundCallback.Invoke(vulkanFoundOrNotRequired);
        }
    }
}
