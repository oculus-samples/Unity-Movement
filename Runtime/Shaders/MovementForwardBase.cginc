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

#include "MovementCommon.cginc"

struct VertexOutputBaseMovement
{
    UNITY_POSITION(pos);
    float2  tex         : TEXCOORD0;
    half3   eyeVec      : TEXCOORD1;
    float4  tangentToWorldAndPackedData[3] : TEXCOORD2;
    // spherical harmonics or lightmap UV
    half4   ambientOrLightmapUV : TEXCOORD5;
    UNITY_LIGHTING_COORDS(6, 7)

    // a more "complete" shader would allow worldposition in fragment space
    float3 posWorld : TEXCOORD8;
    float3 posWorldShadow : TEXCOORD9;

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

float _VertexDisplShadows;

VertexOutputBaseMovement VertexForwardBase(VertexInputMovement v, uint vid : SV_VertexID)
{
    UNITY_SETUP_INSTANCE_ID(v);
    VertexOutputBaseMovement output;
    UNITY_INITIALIZE_OUTPUT(VertexOutputBaseMovement, output);
    UNITY_TRANSFER_INSTANCE_ID(v, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float4 posWorld = mul(unity_ObjectToWorld, v.vertex);
    #if UNITY_PACK_WORLDPOS_WITH_TANGENT
        output.tangentToWorldAndPackedData[0].w = posWorld.x;
        output.tangentToWorldAndPackedData[1].w = posWorld.y;
        output.tangentToWorldAndPackedData[2].w = posWorld.z;
    #else
        output.posWorld = posWorld.xyz;
    #endif
    output.pos = UnityObjectToClipPos(v.vertex);

    output.tex = TRANSFORM_TEX(v.uv0, _MainTex);
    // this vector normalized in fragment shader.
    output.eyeVec.xyz = posWorld.xyz - _WorldSpaceCameraPos;
    float3 normalWorld = UnityObjectToWorldNormal(v.normal);

#ifdef _RECALCULATE_NORMALS
    normalWorld = GetRecalculatedNormal(vid);
#endif

    output.posWorldShadow = posWorld - normalWorld * _VertexDisplShadows;
#ifdef _TANGENT_TO_WORLD
    float4 tangentWorldSpace = float4(UnityObjectToWorldDir(v.tangent.xyz),
        v.tangent.w);
    float3x3 tangentMatrix = CreateTangentToWorldPerVertex(
        normalWorld,
        tangentWorldSpace.xyz,
        tangentWorldSpace.w);
    output.tangentToWorldAndPackedData[0].xyz = tangentMatrix[0];
    output.tangentToWorldAndPackedData[1].xyz = tangentMatrix[1];
    output.tangentToWorldAndPackedData[2].xyz = tangentMatrix[2];
#else
    output.tangentToWorldAndPackedData[0].xyz = 0;
    output.tangentToWorldAndPackedData[1].xyz = 0;
    output.tangentToWorldAndPackedData[2].xyz = normalWorld;
#endif
    // shadow receiving
    UNITY_TRANSFER_LIGHTING(output, v.uv1);

    output.ambientOrLightmapUV = VertexGIForward(v, posWorld, normalWorld);

    return output;
}

fixed _EmissionStrength;

// Based on fragForwardBaseInternal in UnityStandardCore
half4 FragmentForwardBase(VertexOutputBaseMovement input) : SV_Target
{
    float2 inputUV = input.tex;
    // fragment set up creates data for metallic or specular flow.
    FragmentCommonData fragData = RunFragmentSetup(
        inputUV,
        input.eyeVec.xyz,
        input.tangentToWorldAndPackedData,
        IN_WORLDPOS(input));

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    UnityLight mainLight = MainLight();

    UNITY_LIGHT_ATTENUATION(atten, input, input.posWorldShadow);

    half occlusion = GetAmbientOcclusion(inputUV);
    UnityGI gi = FragmentGI(fragData, occlusion, input.ambientOrLightmapUV,
        atten, mainLight);

    half4 color = UnityBRDFModifiedGGX(
        fragData.diffColor, fragData.specColor,
        fragData.oneMinusReflectivity,
        fragData.smoothness, fragData.normalWorld,
        -fragData.eyeVec, gi.light, gi.indirect);

#ifdef _EMISSION
    color.rgb += _EmissionStrength *
        tex2D(_EmissionMap, inputUV).rgb * _EmissionColor.rgb;
#endif

    return OutputForward(color, fragData.alpha);
}

#endif
