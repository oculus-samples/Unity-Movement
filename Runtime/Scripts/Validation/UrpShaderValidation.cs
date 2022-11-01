// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using UnityEngine.Rendering;

namespace Oculus.Movement.Validation
{
    /// <summary>
    /// Enables or disables the URP shader keyword to prevent shader compilation errors
    /// when the URP package is missing from the project.
    /// </summary>
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
#endif
    public static class UrpShaderValidation
    {
        private const string _keywordUrp = "IS_URP";

        /// <summary>
        /// Checks on initialization if URP is enabled.
        /// </summary>
        static UrpShaderValidation()
        {
            SetUrpShaderKeyword();
        }

        /// <summary>
        /// Enables or disables URP package includes by checking if the project has URP enabled.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void SetUrpShaderKeyword()
        {
            bool isUrp = GraphicsSettings.renderPipelineAsset != null;
            if (isUrp)
            {
                Shader.EnableKeyword(_keywordUrp);
            }
            else
            {
                Shader.DisableKeyword(_keywordUrp);
            }
        }
    }
}
