// Copyright (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.Build;
using UnityEditor.Rendering;

namespace Oculus.Movement.Utils
{
    /// <summary>
    /// Process which shader variants get included in a build.
    /// </summary>
    public class ShaderBuildPreprocessor : IPreprocessShaders
    {
        public int callbackOrder => 0;
        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            // Exclude URP shader variants from built-in pipeline.
            if (GraphicsSettings.renderPipelineAsset == null && snippet.passType == PassType.ScriptableRenderPipeline)
            {
                for (int i = 0; i < data.Count; ++i)
                {
                    data.RemoveAt(i);
                    i--;
                }
            }
        }
    }
}
