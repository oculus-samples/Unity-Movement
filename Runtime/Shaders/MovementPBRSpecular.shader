// Copyright (c) Meta Platforms, Inc. and affiliates.

/*
Unity built-in shader source.

Copyright (c) 2016 Unity Technologies.

MIT license.

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
of the Software, and to permit persons to whom the Software is furnished to do
so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

// PBR shader, specular set up
// Based on Unity Standard shader (specular flow)
// Adds diffuse wrap, area light sampling, stenciling, and shadow displacement
// Also allows specularity to be influenced NdotL as an option
Shader "Movement/PBR (Specular)"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Albedo", 2D) = "white" {}

        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        _Glossiness("Smoothness", Range(0.0, 1.0)) = 0.5
        _GlossMapScale("Smoothness Factor", Range(0.0, 1.0)) = 1.0
        [Enum(Specular Alpha,0,Albedo Alpha,1)] _SmoothnessTextureChannel("Smoothness texture channel", Float) = 0

        _SpecColor("Specular", Color) = (0.2,0.2,0.2)
        _SpecGlossMap("Specular", 2D) = "white" {}

        [ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0
        [ToggleOff] _GlossyReflections("Glossy Reflections", Float) = 1.0

        _BumpScale("Scale", Float) = 1.0
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}

        _OcclusionMap("Occlusion", 2D) = "white" {}
        _OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0

        _EmissionColor("Color", Color) = (0,0,0)
        _EmissionMap("Emission", 2D) = "white" {}
        _EmissionStrength("Emission", Range(0.0, 1.0)) = 1.0

        _AreaLightSampleDistance("Area Light Sample Distance", Range(0.0, 1.0)) = 0.5

        _DiffuseWrapColor("Diffuse Wrap Color", Color) = (1.0, .3, .2)
        _DiffuseWrapColorMult("Diffuse Wrap Color Multiplier", Range(0, 5.0)) = 2.0
        _DiffuseWrapDist("Diffuse Wrap Distance", Range(0, 1.0)) = 0.3

        _VertexDisplShadows("Vert Displ shadows", Range(-1.0, 1.0)) = 0.0

        [HideInInspector] [IntRange] _StencilValue("Stencil value to compare against", Range(0, 255)) = 0
        [HideInInspector] [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp("Stencil Comp", Float) = 3
        [HideInInspector] _Mode("_Mode", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0

        [HideInInspector] _AreaLightSampleToggle("_AreaLightSampleToggle", Float) = 0.0
        [HideInInspector] _DiffuseWrapEnabled("_DiffuseWrapEnabled", Float) = 0.0
        [HideInInspector] _SpecularityNDotL("_SpecularityNDotL", Float) = 0.0
        [HideInInspector] _RecalculateNormalsToggle("_RecalculateNormalsToggle", Float) = 0.0
    }

    CGINCLUDE
        #define MOVEMENT_SETUP_BRDF_INPUT CreateDataForSpecularSetup
    ENDCG
    HLSLINCLUDE
        #define MOVEMENT_SETUP_BRDF_INPUT CreateDataForSpecularSetup
    ENDHLSL

    // URP sub shader
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            PackageRequirements { "com.unity.render-pipelines.universal": "[10.8.1,10.10.0]" }
            Name "FORWARD"
            Tags { "LightMode" = "UniversalForward" }
            Stencil
            {
                Ref [_StencilValue]
                Comp [_StencilComp]
            }

            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]

            HLSLPROGRAM
            #pragma exclude_renderers gles glcore
            #pragma target 4.5

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature _EMISSION
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _RECALCULATE_NORMALS

            #pragma shader_feature_local_fragment _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
            #pragma shader_feature_local_fragment _SPECGLOSSMAP
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            #pragma shader_feature_local_fragment _RECEIVE_SHADOWS_OFF
            #pragma shader_feature_local_fragment _AREA_LIGHT_SPECULAR
            #pragma shader_feature_local_fragment _DIFFUSE_WRAP
            #pragma shader_feature_local_fragment _SPECULAR_AFFECT_BY_NDOTL

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON

            #pragma multi_compile_instancing

            #pragma vertex VertexForwardBase
            #pragma fragment FragmentForwardBase

            #include "MovementForwardBase.hlsl"
            ENDHLSL
        }

        // ------------------------------------------------------------------
        //  Shadow rendering pass
        Pass
        {
            PackageRequirements { "com.unity.render-pipelines.universal": "[10.8.1,10.10.0]" }
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            Stencil
            {
                Ref [_StencilValue]
                Comp [_StencilComp]
            }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma exclude_renderers gles glcore
            #pragma target 4.5

            #pragma shader_feature_local _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
            #pragma shader_feature_local _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local _SPECGLOSSMAP

            #pragma multi_compile_instancing

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

		// ------------------------------------------------------------------
        // Extracts information for lightmapping, GI (emission, albedo, ...)
        // This pass is not used during regular rendering.
        Pass
        {
            PackageRequirements { "com.unity.render-pipelines.universal": "[10.8.1,10.10.0]" }
            Name "META"
            Tags { "LightMode" = "Meta" }

            Cull Off

            HLSLPROGRAM
            #pragma exclude_renderers gles glcore
            #pragma target 4.5

            #pragma shader_feature _EMISSION
            #pragma shader_feature_local_fragment _SPECULAR_SETUP
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            #pragma shader_feature_local_fragment _SPECGLOSSMAP

            #pragma vertex UniversalVertexMeta
            #pragma fragment UniversalFragmentMeta

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitMetaPass.hlsl"
            ENDHLSL
        }
	}

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
        }
        LOD 300

        Pass
        {
            Name "FORWARD"
            Tags { "LightMode" = "ForwardBase" }
            Stencil
            {
                Ref [_StencilValue]
                Comp [_StencilComp]
            }

            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]

            CGPROGRAM
            #pragma target 3.0

            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON

            #pragma shader_feature _EMISSION
            #pragma shader_feature_local _SPECGLOSSMAP
            #pragma shader_feature_local _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            #pragma shader_feature_local _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local _GLOSSYREFLECTIONS_OFF
            #pragma shader_feature_local _AREA_LIGHT_SPECULAR
            #pragma shader_feature_local _DIFFUSE_WRAP
            #pragma shader_feature_local _SPECULAR_AFFECT_BY_NDOTL
            #pragma shader_feature_local _RECALCULATE_NORMALS

            #pragma multi_compile_fwdbase
            #pragma multi_compile_instancing

            #pragma vertex VertexForwardBase
            #pragma fragment FragmentForwardBase
            #include "MovementForwardBase.cginc"

            ENDCG
        }

        Pass
        {
            Name "FORWARD_DELTA"
            Tags { "LightMode" = "ForwardAdd" }
            Stencil
            {
                Ref [_StencilValue]
                Comp [_StencilComp]
            }

            Blend[_SrcBlend] One
            ZWrite Off
            ZTest LEqual

            CGPROGRAM
            #pragma target 3.0

            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
            #pragma shader_feature_local _SPECGLOSSMAP
            #pragma shader_feature_local _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local _SPECULAR_AFFECT_BY_NDOTL
            #pragma shader_feature_local _RECALCULATE_NORMALS

            #pragma multi_compile_fwdadd_fullshadows

            #pragma vertex VertexForwardAdd
            #pragma fragment FragmentForwardAdd
            #include "MovementForwardAdd.cginc"

            ENDCG
        }

        // ------------------------------------------------------------------
        //  Shadow rendering pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            Stencil
            {
                Ref [_StencilValue]
                Comp [_StencilComp]
            }

            ZWrite On ZTest LEqual

            CGPROGRAM
            #pragma target 3.0

            #define UNITY_SETUP_BRDF_INPUT SpecularSetup

            #pragma shader_feature_local _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
            #pragma shader_feature_local _SPECGLOSSMAP
            #pragma shader_feature_local _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            #pragma multi_compile_shadowcaster
            #pragma multi_compile_instancing

            #pragma vertex vertShadowCaster
            #pragma fragment fragShadowCaster

            #include "UnityStandardShadow.cginc"

            ENDCG
        }

        // ------------------------------------------------------------------
        // Extracts information for lightmapping, GI (emission, albedo, ...)
        // This pass it not used during regular rendering.
        Pass
        {
            Name "META"
            Tags { "LightMode" = "Meta" }

            Cull Off

            CGPROGRAM
            #pragma vertex vert_meta
            #pragma fragment frag_meta

            #pragma shader_feature _EMISSION
            #pragma shader_feature_local _SPECGLOSSMAP
            #pragma shader_feature_local _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local _DETAIL_MULX2
            #pragma shader_feature EDITOR_VISUALIZATION

            #include "UnityStandardMeta.cginc"
            ENDCG
        }
    }

    CustomEditor "MovementPBRShaderGUI"
}
