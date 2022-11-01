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

CBUFFER_START(UnityLighting)
  #ifdef USING_DIRECTIONAL_LIGHT
  half4 _WorldSpaceLightPos0;
  #else
  float4 _WorldSpaceLightPos0;
  #endif
CBUFFER_END

struct VertexInputMovement
{
  float4  vertex  : POSITION;
  half3   normal  : NORMAL;
  float2  uv0     : TEXCOORD0;
  float2  uv1     : TEXCOORD1;
#ifdef _TANGENT_TO_WORLD
  half4   tangent : TANGENT;
#endif
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
  // spherical harmonics or lightmap UV
  half4   ambientOrLightmapUV             : TEXCOORD5;
  half4   fogFactorAndVertexLight         : TEXCOORD6; // x: fogFactor, yzw: vertex light
  float4  shadowCoord                     : TEXCOORD7;

  // a more "complete" shader would allow worldposition in fragment space
  float3  posWorld                        : TEXCOORD8;
  float3  posWorldShadow                  : TEXCOORD9;

  UNITY_VERTEX_INPUT_INSTANCE_ID
  UNITY_VERTEX_OUTPUT_STEREO
};

UnityLight MainLight(VertexOutputBaseMovement i, half3 lightDir, half atten)
{
  Light light = GetMainLight();

  UnityLight mainLight;
  mainLight.color = light.color * atten;
  mainLight.dir = light.direction + lightDir;
  #ifndef USING_DIRECTIONAL_LIGHT
    mainLight.dir = normalize((float3)mainLight.dir);
  #endif
  return mainLight;
}

inline UnityGI FragmentGI (FragmentCommonData s, half occlusion, VertexOutputBaseMovement i, half atten, UnityLight mainLight)
{
  UnityGI o_gi;
  ResetUnityGI(o_gi);

  o_gi.light = mainLight;
  o_gi.light.color *= atten;

  // use vertex light or lightmaps
#if defined(LIGHTMAP_ON)
  half4 bakedColorTex = SAMPLE_TEXTURE2D(unity_Lightmap, samplerunity_Lightmap, i.ambientOrLightmapUV.xy);
  half4 decodeInstructions = half4(LIGHTMAP_HDR_MULTIPLIER, LIGHTMAP_HDR_EXPONENT, 0.0h, 0.0h);
  half3 bakedColor = DecodeLightmap(bakedColorTex, decodeInstructions);
  o_gi.indirect.diffuse = bakedColor;
#else
  o_gi.indirect.diffuse = i.ambientOrLightmapUV.rgb;
#endif

  // environment reflections
#if defined(_REFLMODE_SCENECOLOR) //equivalent to Unity Standard _GLOSSYREFLECTIONS_OFF
  o_gi.indirect.specular = unity_IndirectSpecColor.rgb;
#elif defined(_REFLMODE_SCENECUBEMAP) //unity scene skybox or reflection probe
  Unity_GlossyEnvironmentData envData = UnityGlossyEnvironmentSetup(
      s.smoothness, s.eyeVec, s.normalWorld, float3(0, 0, 0)
  );
  o_gi.indirect.specular = Unity_GlossyEnvironment(
      unity_SpecCube0, samplerunity_SpecCube0, unity_SpecCube0_HDR, envData
  );
  // o_gi.indirect.specular = GlossyEnvironmentReflection(envData.reflUVW, envData.perceptualRoughness, 1);
  #endif
  //o_gi.indirect.specular *= _SpecColor.rgb;
  o_gi.indirect.specular *= occlusion;
  o_gi.indirect.diffuse *= occlusion;

  return o_gi;
}

float _VertexDisplShadows;

VertexOutputBaseMovement VertexForwardBase(VertexInputMovement v, uint vid : SV_VertexID)
{
    VertexOutputBaseMovement output = (VertexOutputBaseMovement)0;

    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(v.vertex.xyz);
    float4 posWorld = mul(unity_ObjectToWorld, v.vertex);
    #if UNITY_PACK_WORLDPOS_WITH_TANGENT
        output.tangentToWorldAndPackedData[0].w = posWorld.x;
        output.tangentToWorldAndPackedData[1].w = posWorld.y;
        output.tangentToWorldAndPackedData[2].w = posWorld.z;
    #else
        output.posWorld = posWorld.xyz;
    #endif
    output.pos = vertexInput.positionCS;

    output.tex = TRANSFORM_TEX(v.uv0, _MainTex);
    // this vector normalized in fragment shader.
    output.eyeVec.xyz = posWorld.xyz - _WorldSpaceCameraPos;
    float3 normalWorld = TransformObjectToWorldNormal(v.normal);
#ifdef _RECALCULATE_NORMALS
    normalWorld = GetRecalculatedNormal(vid);
#endif

#ifdef _TANGENT_TO_WORLD
    float4 tangentWorldSpace = float4(TransformObjectToWorldDir(
        v.tangent.xyz),
        v.tangent.w);
    float3x3 tangentMatrix = CreateTangentToWorldPerVertex(
        normalWorld, tangentWorldSpace.xyz, tangentWorldSpace.w);
    output.tangentToWorldAndPackedData[0].xyz = tangentMatrix[0];
    output.tangentToWorldAndPackedData[1].xyz = tangentMatrix[1];
    output.tangentToWorldAndPackedData[2].xyz = tangentMatrix[2];
#else
    output.tangentToWorldAndPackedData[0].xyz = 0;
    output.tangentToWorldAndPackedData[1].xyz = 0;
    output.tangentToWorldAndPackedData[2].xyz = normalWorld;
#endif
    OUTPUT_LIGHTMAP_UV(v.uv1, unity_LightmapST, output.ambientOrLightmapUV);
    OUTPUT_SH(normalWorld, output.ambientOrLightmapUV);
    output.posWorldShadow = posWorld.xyz - normalWorld * _VertexDisplShadows;
    output.shadowCoord = GetShadowCoord(vertexInput);

    float3 lightDir = _WorldSpaceLightPos0.xyz -
        posWorld.xyz * _WorldSpaceLightPos0.w;
    output.tangentToWorldAndPackedData[0].w = lightDir.x;
    output.tangentToWorldAndPackedData[1].w = lightDir.y;
    output.tangentToWorldAndPackedData[2].w = lightDir.z;

    return output;
}

half _EmissionStrength;

// Based on fragForwardBaseInternal in UnityStandardCore
half4 FragmentForwardBase(VertexOutputBaseMovement input) : SV_Target
{
    float2 inputUV = input.tex;
    half3 inWorldPos = half3(input.tangentToWorldAndPackedData[0].w,
                             input.tangentToWorldAndPackedData[1].w,
                             input.tangentToWorldAndPackedData[2].w);
    // fragment set up creates data for metallic or specular flow.
    FragmentCommonData fragData = RunFragmentSetup(
        inputUV,
        input.eyeVec.xyz,
        input.tangentToWorldAndPackedData,
        inWorldPos);

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    half atten = MainLightRealtimeShadow(input.shadowCoord);
    UnityLight mainLight = MainLight(input,
      half3(input.tangentToWorldAndPackedData[0].w,
        input.tangentToWorldAndPackedData[1].w,
        input.tangentToWorldAndPackedData[2].w), atten);

    half occlusion = GetAmbientOcclusion(inputUV);
    UnityGI gi = FragmentGI(fragData, occlusion, input, atten, mainLight);
    half4 color = UnityBRDFModifiedGGX(
        fragData.diffColor, fragData.specColor,
        fragData.oneMinusReflectivity,
        fragData.smoothness, fragData.normalWorld,
        -fragData.eyeVec, gi.light, gi.indirect);

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
