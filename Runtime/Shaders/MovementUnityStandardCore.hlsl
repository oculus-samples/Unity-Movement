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

#if UNITY_STANDARD_SIMPLE
  half3 reflUVW;
#endif

#if UNITY_STANDARD_SIMPLE
  half3 tangentSpaceNormal;
#endif
};

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

struct UnityGI
{
  UnityLight light;
  UnityIndirect indirect;
};

inline void ResetUnityGI(out UnityGI outGI)
{
  outGI.light.color = 0;
  outGI.light.dir = 0;
  outGI.indirect.diffuse = 0;
  outGI.indirect.specular = 0;
}

half3 UnpackScaleNormal(half4 packednormal, half bumpScale)
{
#if defined(UNITY_NO_DXT5nm)
    half3 normal = packednormal.xyz * 2 - 1;
#if (SHADER_TARGET >= 30)
    // SM2.0: instruction count limitation
    // SM2.0: normal scaler is not supported
    normal.xy *= bumpScale;
#endif
    return normal;
#elif defined(UNITY_ASTC_NORMALMAP_ENCODING)
    half3 normal;
    normal.xy = (packednormal.wy * 2 - 1);
    normal.z = sqrt(1.0 - saturate(dot(normal.xy, normal.xy)));
    normal.xy *= bumpScale;
    return normal;
#else
    // This do the trick
    packednormal.x *= packednormal.w;

    half3 normal;
    normal.xy = (packednormal.xy * 2 - 1);
#if (SHADER_TARGET >= 30)
    // SM2.0: instruction count limitation
    // SM2.0: normal scaler is not supported
    normal.xy *= bumpScale;
#endif
    normal.z = sqrt(1.0 - saturate(dot(normal.xy, normal.xy)));
    return normal;
#endif
}
