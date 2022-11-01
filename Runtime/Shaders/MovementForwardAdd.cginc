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

#ifndef MOVEMENT_FORWARD_ADD
#define MOVEMENT_FORWARD_ADD

#include "MovementCommon.cginc"

struct VertexOutputAddMovement
{
    UNITY_POSITION(pos);
    float2 tex                          : TEXCOORD0;
    // eyeVec.xyz | fogCoord
    float4 eyeVec                       : TEXCOORD1;
    // [3x3:tangentToWorld | 1x3:lightDir]
    float4 tangentToWorldAndLightDir[3] : TEXCOORD2;
    float3 posWorld                     : TEXCOORD5;
    float3 posWorldShadow               : TEXCOORD8;
    UNITY_LIGHTING_COORDS(6, 7)

    UNITY_VERTEX_OUTPUT_STEREO
};

float _VertexDisplShadows;

VertexOutputAddMovement VertexForwardAdd(VertexInputMovement v, uint vid : SV_VertexID)
{
    UNITY_SETUP_INSTANCE_ID(v);
    VertexOutputAddMovement output;
    UNITY_INITIALIZE_OUTPUT(VertexOutputAddMovement, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float4 posWorld = mul(unity_ObjectToWorld, v.vertex);
    output.pos = UnityObjectToClipPos(v.vertex);

    output.tex = TRANSFORM_TEX(v.uv0, _MainTex);
    // normalized in the fragment shader
    output.eyeVec.xyz = posWorld.xyz - _WorldSpaceCameraPos;
    output.posWorld = posWorld.xyz;
    float3 normalWorld = UnityObjectToWorldNormal(v.normal);

#ifdef _RECALCULATE_NORMALS
    normalWorld = GetRecalculatedNormal(vid);
#endif

#ifdef _TANGENT_TO_WORLD
    float4 tangentWorld = float4(UnityObjectToWorldDir(
        v.tangent.xyz),
        v.tangent.w);

    float3x3 tangentToWorld = CreateTangentToWorldPerVertex(
        normalWorld, tangentWorld.xyz, tangentWorld.w);
    output.tangentToWorldAndLightDir[0].xyz = tangentToWorld[0];
    output.tangentToWorldAndLightDir[1].xyz = tangentToWorld[1];
    output.tangentToWorldAndLightDir[2].xyz = tangentToWorld[2];
#else
    output.tangentToWorldAndLightDir[0].xyz = 0;
    output.tangentToWorldAndLightDir[1].xyz = 0;
    output.tangentToWorldAndLightDir[2].xyz = normalWorld;
#endif
    // shadow receiving
    UNITY_TRANSFER_LIGHTING(output, v.uv1);
    output.posWorldShadow = posWorld - normalWorld * _VertexDisplShadows;

    // normalized in the fragment shader
    float3 lightDir = _WorldSpaceLightPos0.xyz -
        posWorld.xyz * _WorldSpaceLightPos0.w;
    output.tangentToWorldAndLightDir[0].w = lightDir.x;
    output.tangentToWorldAndLightDir[1].w = lightDir.y;
    output.tangentToWorldAndLightDir[2].w = lightDir.z;

    return output;
}

// Based on fragForwardAddInternal in UnityStandardCore.cginc
half4 FragmentForwardAdd(VertexOutputAddMovement input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    // fragment set up creates data for metallic or specular flow.
    FragmentCommonData fragData = RunFragmentSetup(
        input.tex,
        input.eyeVec.xyz,
        input.tangentToWorldAndLightDir,
        IN_WORLDPOS_FWDADD(input));

    UNITY_LIGHT_ATTENUATION(atten, input, input.posWorldShadow)
    UnityLight light = AdditiveLight(IN_LIGHTDIR_FWDADD(input), atten);
    UnityIndirect noIndirect = ZeroIndirect();

    half4 color = UnityBRDFModifiedGGX(
        fragData.diffColor, fragData.specColor,
        fragData.oneMinusReflectivity,
        fragData.smoothness, fragData.normalWorld,
        -fragData.eyeVec, light, noIndirect);

    return OutputForward(color, fragData.alpha);
}

#endif
