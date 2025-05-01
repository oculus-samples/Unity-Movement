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

// Defines copied from UnityStandardConfig.cginc so we can still use the defines in an .hlsl shader.
//---------------------------------------
// Energy conservation for Specular workflow is Monochrome. For instance: Red metal will make diffuse Black not Cyan
#ifndef UNITY_CONSERVE_ENERGY
#define UNITY_CONSERVE_ENERGY 1
#endif
#ifndef UNITY_CONSERVE_ENERGY_MONOCHROME
#define UNITY_CONSERVE_ENERGY_MONOCHROME 1
#endif

// Should we pack worldPos along tangent (saving an interpolator)
// We want to skip this on mobile platforms, because worldpos gets packed into mediump
#if !defined(SHADER_API_MOBILE)
    #define UNITY_PACK_WORLDPOS_WITH_TANGENT 1
#else
    #define UNITY_PACK_WORLDPOS_WITH_TANGENT 0
#endif

// Functions and structures copied from UnityStandardCore.cginc so we can still use them in an .hlsl shader.
// Some functions have been changed with compatible hlsl calls.

struct FragmentCommonData
{
  half3 diffColor, specColor;
  // Note: smoothness & oneMinusReflectivity for optimization purposes, mostly for DX9 SM2.0 level.
  // Most of the math is being done on these (1-x) values, and that saves a few precious ALU slots.
  half oneMinusReflectivity, smoothness;
  float3 normalWorld;
  float3 eyeVec;
  half alpha;
  float3 posWorld;
};

//-------------------------------------------------------------------------------------
struct UnityLight
{
  half3 color;
  half3 dir;
};

struct UnityIndirect
{
  half3 diffuse;
  half3 specular;
};

//-------------------------------------------------------------------------------------
half3x3 CreateTangentToWorldPerVertex(half3 normal, half3 tangent, half tangentSign)
{
  // For odd-negative scale transforms we need to flip the sign
  half sign = tangentSign * unity_WorldTransformParams.w;
  half3 binormal = cross(normal, tangent) * sign;
  return half3x3(tangent, binormal, normal);
}
