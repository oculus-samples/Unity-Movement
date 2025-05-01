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

#ifndef MOVEMENT_FORWARD_BASE
#define MOVEMENT_FORWARD_BASE

#include "MovementCommon.hlsl"

struct VertexInputMovement
{
  float4  vertex  : POSITION;
  half3   normal  : NORMAL;
  half4   tangent : TANGENT;
  float2  uv0     : TEXCOORD0;
  float2  uv1     : TEXCOORD1;

  // We always want to use tangent to world space.
  //#ifdef _TANGENT_TO_WORLD
#if defined(DYNAMICLIGHTMAP_ON) || defined(UNITY_PASS_META)
  float2  uv2     : TEXCOORD2;
#endif
  UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutputBaseMovement
{
  float4  pos                             : SV_POSITION;
  float2  tex                             : TEXCOORD0;
  half3   eyeVec                          : TEXCOORD1;
  float4  tangentToWorldAndPackedData[3]  : TEXCOORD2;
  // Spherical harmonics or lightmap UV.
  DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 5);
  half4   fogFactorAndVertexLight         : TEXCOORD6;
  float3  posWorld                        : TEXCOORD7;
  UNITY_VERTEX_INPUT_INSTANCE_ID
  UNITY_VERTEX_OUTPUT_STEREO
};

VertexOutputBaseMovement VertexForwardBase(VertexInputMovement v, uint vid : SV_VertexID)
{
    VertexOutputBaseMovement output = (VertexOutputBaseMovement)0;

    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(v.vertex.xyz);
    float3 posWorld = mul(unity_ObjectToWorld, v.vertex).xyz;
#if UNITY_PACK_WORLDPOS_WITH_TANGENT
    output.tangentToWorldAndPackedData[0].w = posWorld.x;
    output.tangentToWorldAndPackedData[1].w = posWorld.y;
    output.tangentToWorldAndPackedData[2].w = posWorld.z;
#else
    output.posWorld = posWorld;
#endif
    output.pos = vertexInput.positionCS;

    output.tex = TRANSFORM_TEX(v.uv0, _MainTex);
    // This vector is normalized in the fragment shader.
    output.eyeVec.xyz = posWorld - _WorldSpaceCameraPos;
    float3 normalWorld = TransformObjectToWorldNormal(v.normal);

#ifdef _RECALCULATE_NORMALS
    normalWorld = GetRecalculatedNormal(vid);
#endif

    // We always want to use tangent to world space, as the shader is designed to be used with a normal map.
    //#if _TANGENT_TO_WORLD
    float4 tangentWorldSpace = float4(TransformObjectToWorldDir(
        v.tangent.xyz),
        v.tangent.w);
    const float3x3 tangentMatrix = CreateTangentToWorldPerVertex(
        normalWorld, tangentWorldSpace.xyz, tangentWorldSpace.w);
    output.tangentToWorldAndPackedData[0].xyz = tangentMatrix[0];
    output.tangentToWorldAndPackedData[1].xyz = tangentMatrix[1];
    output.tangentToWorldAndPackedData[2].xyz = tangentMatrix[2];

    OUTPUT_LIGHTMAP_UV(v.uv1, unity_LightmapST, output.lightmapUV);
    OUTPUT_SH(normalWorld, output.vertexSH);
    half3 vertexLight = VertexLighting(posWorld.xyz, normalWorld);
    half fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
    output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);

    return output;
}

// Based on fragForwardBaseInternal in UnityStandardCore
half4 FragmentForwardBase(VertexOutputBaseMovement input) : SV_Target
{
    float2 inputUV = input.tex;
#if UNITY_PACK_WORLDPOS_WITH_TANGENT
    half3 inWorldPos = half3(input.tangentToWorldAndPackedData[0].w,
                             input.tangentToWorldAndPackedData[1].w,
                             input.tangentToWorldAndPackedData[2].w);
#else
    half3 inWorldPos = input.posWorld;
#endif
    // fragment set up creates data for metallic or specular flow.
    FragmentCommonData fragData = RunFragmentSetup(
        inputUV,
        input.eyeVec.xyz,
        input.tangentToWorldAndPackedData,
        inWorldPos);

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    half3 bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, fragData.normalWorld);
    const half4 shadowMask = SAMPLE_SHADOWMASK(input.lightmapUV);
    const half occlusion = GetAmbientOcclusion(inputUV);
    const half perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(fragData.smoothness);
    const half3 reflectVector = reflect(-fragData.eyeVec, fragData.normalWorld);
    const half4 shadowCoord = TransformWorldToShadowCoord(inWorldPos) -
      float4(fragData.normalWorld * _VertexDisplShadows, 1);

    Light mainLight = GetMainLight(shadowCoord, inWorldPos, shadowMask);
    MixRealtimeAndBakedGI(mainLight, fragData.normalWorld, bakedGI);

    UnityLight mainUnityLight;
    mainUnityLight.color = mainLight.color * (mainLight.distanceAttenuation * mainLight.shadowAttenuation);
    mainUnityLight.dir = mainLight.direction;
    UnityIndirect indirect;
    indirect.diffuse = bakedGI * occlusion;
    indirect.specular = GlossyEnvironmentReflection(reflectVector, perceptualRoughness, occlusion);

    half4 color = UnityBRDFModifiedGGX(
        fragData.diffColor, fragData.specColor,
        fragData.oneMinusReflectivity,
        fragData.smoothness, fragData.normalWorld,
        -fragData.eyeVec, mainUnityLight, indirect);

    uint pixelLightCount = GetAdditionalLightsCount();
    for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
    {
      Light light = GetAdditionalLight(lightIndex, inWorldPos, shadowMask);

      UnityLight unityLight;
      unityLight.color = light.color * (light.distanceAttenuation * light.shadowAttenuation);
      unityLight.dir = light.direction;
      UnityIndirect unityIndirect;
      unityIndirect.diffuse = half3(0, 0, 0);
      unityIndirect.specular = half3(0, 0, 0);

      color += UnityBRDFModifiedGGX(
        fragData.diffColor, fragData.specColor,
        fragData.oneMinusReflectivity,
        fragData.smoothness, fragData.normalWorld,
        -fragData.eyeVec, unityLight, unityIndirect);
    }

#ifdef _EMISSION
    color.rgb += _EmissionStrength *
        SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, inputUV).rgb * _EmissionColor.rgb;
#endif

#if defined(_ALPHABLEND_ON) || defined(_ALPHAPREMULTIPLY_ON)
    color.a = fragData.alpha;
#else
    color.a = 1.0f;
#endif
    return color;
}
#endif
