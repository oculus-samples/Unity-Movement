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

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "MovementUnityStandardCore.hlsl"

// A lot of this code has been adapted from UnityStandardCore.cginc etc

#ifdef UNITY_COLORSPACE_GAMMA
  #define unity_ColorSpaceDielectricSpec half4(0.220916301, 0.220916301, 0.220916301, 1.0 - 0.220916301)
#else
  #define unity_ColorSpaceDielectricSpec half4(0.04, 0.04, 0.04, 1.0 - 0.04) // standard dielectric reflectivity coef at incident angle (= 4%)
#endif

// Setup the CBUFFER to make the shader compatible with SRP batching.
// https://blog.unity.com/technology/srp-batcher-speed-up-your-rendering
CBUFFER_START(UnityPerMaterial)
float _VertexDisplShadows;
float _AreaLightSampleDistance;
half3 _DiffuseWrapColor;
half _EmissionStrength;
half _DiffuseWrapColorMult;
half _DiffuseWrapDist;

float4 _MainTex_ST;
half4 _Color;
half4 _SpecColor;
half4 _EmissionColor;
half _Cutoff;
half _Metallic;
half _BumpScale;
half _Glossiness;
half _GlossMapScale;
half _OcclusionStrength;
uint _TexUVSet;
CBUFFER_END

TEXTURE2D(_OcclusionMap);       SAMPLER(sampler_OcclusionMap);
TEXTURE2D(_MetallicGlossMap);   SAMPLER(sampler_MetallicGlossMap);
TEXTURE2D(_SpecGlossMap);       SAMPLER(sampler_SpecGlossMap);
TEXTURE2D(_MainTex);            SAMPLER(sampler_MainTex);

half4 SampleMetallicSpecGloss(float2 uv, half albedoAlpha)
{
  half4 specGloss;

#ifdef _METALLICGLOSSMAP
#ifdef _SPECULAR_SETUP
  specGloss = half4(SAMPLE_TEXTURE2D(_SpecGlossMap, sampler_SpecGlossMap, uv));
#else
  specGloss = half4(SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, uv));
#endif
#ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
  specGloss.a = albedoAlpha * _Glossiness;
#else
  specGloss.a *= _Glossiness;
#endif
#else
#if _SPECGLOSSMAP
  specGloss.rgb = _SpecColor.rgb;
#else
  specGloss.rgb = _Metallic.rrr;
#endif

#ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
  specGloss.a = albedoAlpha * _Glossiness;
#else
  specGloss.a = _Glossiness;
#endif
#endif

  return specGloss;
}

half4 ComputeSpecularGloss(float2 uv)
{
    half4 sg;
#ifdef _SPECGLOSSMAP
#if defined(_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A)
    sg.rgb = SAMPLE_TEXTURE2D(_SpecGlossMap, sampler_SpecGlossMap, uv).rgb;
    sg.a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
#else
    sg = SAMPLE_TEXTURE2D(_SpecGlossMap, sampler_SpecGlossMap, uv);
#endif
    sg.a *= _GlossMapScale;
#else
    sg.rgb = _SpecColor.rgb;
#ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
    sg.a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a * _GlossMapScale;
#else
    sg.a = _Glossiness;
#endif
#endif
    return sg;
}

half2 ComputeMetallicGloss(float2 uv)
{
    half2 mg;
#ifdef _METALLICGLOSSMAP
#ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
    mg.r = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, uv).r;
    mg.g = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
#else
    mg = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, uv).ra;
#endif
    mg.g *= _GlossMapScale;
#else
    mg.r = _Metallic;
#ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
    mg.g = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a * _GlossMapScale;
#else
    mg.g = _Glossiness;
#endif
#endif
    return mg;
}

// Like the UnityStandardUtils version, except we assume SHADER_TARGET is >= 30
half ComputeSpecularStrength(half3 specular)
{
    return max(max(specular.r, specular.g), specular.b);
}

// Diffuse/Spec Energy conservation
inline half3 GetEnergyConservationBetweenDiffuseAndSpecular(
    half3 albedo,
    half3 specColor,
    out half oneMinusReflectivity)
{
    oneMinusReflectivity = 1 - ComputeSpecularStrength(specColor);
#if !UNITY_CONSERVE_ENERGY
    return albedo;
#elif UNITY_CONSERVE_ENERGY_MONOCHROME
    return albedo * oneMinusReflectivity;
#else
    return albedo * (half3(1, 1, 1) - specColor);
#endif
}

inline half GetOneMinusReflectivityFromMetallic(half metallic)
{
    //  (1-dielectricSpec) is in unity_ColorSpaceDielectricSpec.a, so
    //  1-reflectivity = lerp(alpha, 0, metallic) = alpha + metallic*(0 - alpha) =
    //                 = alpha - metallic * alpha
    // unity_ColorSpaceDielectricSpec  defined in UnityCG.cginc
    return unity_ColorSpaceDielectricSpec.a - metallic * unity_ColorSpaceDielectricSpec.a;
}


inline half3 GetDiffuseAndSpecularFromMetallic(half3 albedo,
    half metallic, out half3 specColor, out half oneMinusReflectivity)
{
    specColor = lerp(unity_ColorSpaceDielectricSpec.rgb, albedo, metallic);
    oneMinusReflectivity = GetOneMinusReflectivityFromMetallic(metallic);
    return albedo * oneMinusReflectivity;
}

// no detail albedo here, unline Albedo in UnityStandardInput.cginc
half3 GetAlbedo(float2 texcoords)
{
    half3 albedo = _Color.rgb * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, texcoords.xy).rgb;
#if _DETAIL
#if (SHADER_TARGET < 30)
    // SM20: instruction count limitation
    // SM20: no detail mask
    half mask = 1;
#else
    half mask = DetailMask(texcoords.xy);
#endif
    half3 detailAlbedo = SAMPLE_TEXTURE2D(_DetailAlbedoMap, sampler_DetailAlbedoMap, texcoords.zw).rgb;
#if _DETAIL_MULX2
    albedo *= LerpWhiteTo(detailAlbedo * unity_ColorSpaceDouble.rgb, mask);
#elif _DETAIL_MUL
    albedo *= LerpWhiteTo(detailAlbedo, mask);
#elif _DETAIL_ADD
    albedo += detailAlbedo * mask;
#elif _DETAIL_LERP
    albedo = lerp(albedo, detailAlbedo, mask);
#endif
#endif
    return albedo;
}

inline FragmentCommonData CreateDataForSpecularSetup(float2 iTex)
{
    half4 specGloss = ComputeSpecularGloss(iTex.xy);
    half3 specColor = specGloss.rgb;
    half smoothness = specGloss.a;

    half oneMinusReflectivity;
    half3 diffColor = GetEnergyConservationBetweenDiffuseAndSpecular(
        GetAlbedo(iTex), specColor, /*out*/ oneMinusReflectivity);

    FragmentCommonData output = (FragmentCommonData)0;
    output.diffColor = diffColor;
    output.specColor = specColor;
    output.oneMinusReflectivity = oneMinusReflectivity;
    output.smoothness = smoothness;

    return output;
}

inline FragmentCommonData CreateDataForMetallicSetup(float2 iTex)
{
    half2 metallicGloss = ComputeMetallicGloss(iTex.xy);
    half metallic = metallicGloss.x;
    // this is 1 minus the square root of real roughness m.
    half smoothness = metallicGloss.y;

    half oneMinusReflectivity;
    half3 specColor;
    half3 diffColor = GetDiffuseAndSpecularFromMetallic(
        GetAlbedo(iTex),
        metallic,
        /*out*/ specColor,
        /*out*/ oneMinusReflectivity);

    FragmentCommonData output = (FragmentCommonData)0;
    output.diffColor = diffColor;
    output.specColor = specColor;
    output.oneMinusReflectivity = oneMinusReflectivity;
    output.smoothness = smoothness;
    return output;
}

half GetAlpha(float2 uv)
{
#if defined(_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A)
    return _Color.a;
#else
    return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a * _Color.a;
#endif
}

half GetAmbientOcclusion(float2 uv)
{
    half occ = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, uv).g;
    return lerp(0.0, occ, _OcclusionStrength);
}

// Unlike NormalInTangentSpace, no detail textures used here
#ifdef _NORMALMAP
half3 GetNormalInTangentSpace(float2 texcoords)
{
    const half4 normalTangent = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, texcoords.xy);
    return UnpackNormalScale(normalTangent, _BumpScale);
}
#endif

float3 GetPerPixelWorldNormal(float2 iTex, float4 tangentToWorld[3])
{
#ifdef _NORMALMAP
    half3 tangent = tangentToWorld[0].xyz;
    half3 binormal = tangentToWorld[1].xyz;
    half3 normal = tangentToWorld[2].xyz;

    // Disabled in UnityStandardConfig.cginc, so the define is not included.
    // #if UNITY_TANGENT_ORTHONORMALIZE
    //     normal = normalize(normal);
    //
    //     // ortho-normalize Tangent
    //     tangent = normalize(tangent - normal * dot(tangent, normal));
    //
    //     // recalculate Binormal
    //     half3 newB = cross(normal, tangent);
    //     binormal = newB * sign(dot(newB, binormal));
    // #endif

    half3 normalTangent = GetNormalInTangentSpace(iTex);
    float3 normalWorld = normalize(tangent * normalTangent.x +
        binormal * normalTangent.y + normal * normalTangent.z);
#else
    float3 normalWorld = normalize(tangentToWorld[2].xyz);
#endif
    return normalWorld;
}

inline half3 GetPreMultiplyAlpha(
    half3 diffColor,
    half alpha, half
    oneMinusReflectivity,
    out half outModifiedAlpha)
{
#if defined(_ALPHAPREMULTIPLY_ON)
    // NOTE: shader relies on pre-multiply alpha-blend (_SrcBlend = One, _DstBlend = OneMinusSrcAlpha)

    // Transparency 'removes' from Diffuse component
    diffColor *= alpha;

    // Reflectivity 'removes' from the rest of components, including Transparency
    // outAlpha = 1-(1-alpha)*(1-reflectivity) = 1-(oneMinusReflectivity - alpha*oneMinusReflectivity) =
    //          = 1-oneMinusReflectivity + alpha*oneMinusReflectivity
    outModifiedAlpha = (1 - oneMinusReflectivity) + alpha * oneMinusReflectivity;
#else
    outModifiedAlpha = alpha;
#endif
    return diffColor;
}

inline FragmentCommonData RunFragmentSetup(inout float2 iTex, float3 iEyeVec,
    float4 tangentToWorld[3], float3 iPosWorld)
{
    half alpha = GetAlpha(iTex.xy);
#if defined(_ALPHATEST_ON)
    clip(alpha - _Cutoff);
#endif

    // This is where the BRDF setup for Specular or Metallic flow is called.
    FragmentCommonData fragCommonData = MOVEMENT_SETUP_BRDF_INPUT(iTex);

    fragCommonData.normalWorld = GetPerPixelWorldNormal(iTex, tangentToWorld);
    fragCommonData.eyeVec = normalize(iEyeVec);
    fragCommonData.posWorld = iPosWorld;

    // NOTE: shader relies on pre-multiply alpha-blend (_SrcBlend = One, _DstBlend = OneMinusSrcAlpha)
    fragCommonData.diffColor = GetPreMultiplyAlpha(fragCommonData.diffColor,
        alpha, fragCommonData.oneMinusReflectivity, /*out*/ fragCommonData.alpha);
    return fragCommonData;
}

// From UnityStandardBRDF.cginc
// approximage Schlick with ^4 instead of ^5
inline half3 GetFresnelLerpFast(half3 F0, half3 F90, half cosA)
{
    half t = Pow4(1 - cosA);
    return lerp(F0, F90, t);
}

#ifdef _AREA_LIGHT_SPECULAR
float CalculateAreaLightColor(UnityLight light, float invExposure,
    float3 shadingNormal, float3 worldViewDir, float roughness,
    float NdotV)
{
#ifndef LIGHT_SAMPLE_COUNT
#define LIGHT_SAMPLE_COUNT 5
#endif
#ifndef HALF_LIGHT_SAMPLE_COUNT
#define HALF_LIGHT_SAMPLE_COUNT  (float(LIGHT_SAMPLE_COUNT)*0.5)
#endif
    float lightSamplesSum = 0.0;
    half3 worldSpaceLightDir = light.dir;
    half3 lightColor = light.color;
    float r2 = roughness * roughness;
    float r4 = r2 * r2;
    float invR4 = 1.0 - r4;

    float stepSize = (_AreaLightSampleDistance / float(LIGHT_SAMPLE_COUNT));
    // create a bunch of samples based on light direction
    for (int i = 0; i < LIGHT_SAMPLE_COUNT; i++)
    {
        for (int j = 0; j < LIGHT_SAMPLE_COUNT; j++)
        {
            // go from -HALF_LIGHT_SAMPLE_COUNT to +HALF_LIGHT_SAMPLE_COUNT
            // So if we have 10 samples, we want to go from -5 to 5
            // We modify the light direction as if it is coming from different
            // sample positions
            float3 modifiedLightDir = worldSpaceLightDir;
            modifiedLightDir.x += float(float(i) - HALF_LIGHT_SAMPLE_COUNT) * stepSize;
            modifiedLightDir.y += float(float(j) - HALF_LIGHT_SAMPLE_COUNT) * stepSize;
            //float3 directionalLightColor = lightColor * invExposure;
            // assumes view direction is *toward* eye
            float3 halfVector = normalize(modifiedLightDir + worldViewDir);
            float NdotL = dot(modifiedLightDir, shadingNormal);
            //float NdotLWrap = NdotL; <- not used yet
            NdotL = saturate(NdotL);
            float NdotH = saturate(dot(shadingNormal, halfVector));
            // The following GGX code is based on:
            // https://github.com/KhronosGroup/glTF-Sample-Viewer/blob/master/source/Renderer/shaders/brdf.glsl
            // Which is licensed under Apache-2.0. See https://github.com/KhronosGroup/glTF-Sample-Viewer/blob/main/LICENSE.md
            float ggx = NdotL * sqrt(NdotV * NdotV * invR4 + r2) +
                NdotV * sqrt(NdotL * NdotL * invR4 + r2);
            ggx = ggx > 0.0 ? (.5 / ggx) : 0.;
            // Implementation from
            // "Average Irregularity Representation of a Roughened Surface for Ray Reflection"
            // by T. S. Trowbridge, and K. P. Reitz
            float t = 1. / (1. - NdotH * NdotH * invR4);
            lightSamplesSum += NdotL* t* t* r4* ggx;
        }
    }
    return lightSamplesSum / (float(LIGHT_SAMPLE_COUNT * LIGHT_SAMPLE_COUNT));
}
#endif

inline float3 GetSafeNormalizedVector(float3 inVec)
{
    float dp3 = max(0.001f, dot(inVec, inVec));
    return inVec * rsqrt(dp3);
}

#ifdef _DIFFUSE_WRAP
half3 GetDiffuseWrap(float NDotLUnclamped)
{
    // Blend red into the terminator of the diffuse light
    // scattering causes slight color shift toward red
    half3 whiteColor = float3(1.0, 1.0, 1.0);
    half3 diffuseWrapColor = _DiffuseWrapColorMult * _DiffuseWrapColor;
    half interpValue = saturate((-NDotLUnclamped + _DiffuseWrapDist) /
        (_DiffuseWrapDist + _DiffuseWrapDist));
    half3 finalDiffuseWrapColor = lerp(whiteColor, diffuseWrapColor,
        interpValue);
    half wrapDiffuse = saturate((NDotLUnclamped + _DiffuseWrapDist) /
        (1.0 + _DiffuseWrapDist));
    // Squaring the wrap diffuse is not technically correct,
    // but it makes the diffuse roll off a little better, which may aid in improving the
    // shaping of the surface
    return wrapDiffuse * wrapDiffuse * finalDiffuseWrapColor;
}
#endif

// From UnityStandardBRDF.cginc
// NOTE:
// 1. Uses GGX
// 2. Does not support UNITY_COLORSPACE_GAMMA
// 3. SHADER_API_MOBILE is assumed false
// 4. Does not support _SPECULARHIGHLIGHTS_OFF
// Based on Minimalist CookTorrance BRDF
// Implementation is slightly different from original derivation: http://www.thetenthplanet.de/archives/255
//
// * NDF (depending on UNITY_BRDF_GGX):
//  a) BlinnPhong
//  b) [Modified] GGX
// * Modified Kelemen and Szirmay-​Kalos for Visibility term
// * Fresnel approximated with 1/LdotH
half4 UnityBRDFModifiedGGX(half3 diffColor, half3 specColor, half oneMinusReflectivity,
    half smoothness, float3 normal, float3 viewDirTowardEye,
    UnityLight light, UnityIndirect gi)
{
    float3 halfDir = GetSafeNormalizedVector(
        float3(light.dir) + viewDirTowardEye);

    half NDotLUnclamped = dot(normal, light.dir);
    half NdotL = saturate(NDotLUnclamped);
    float NDotH = saturate(dot(normal, halfDir));
    half NdotV = saturate(dot(normal, viewDirTowardEye));
    float LdotH = saturate(dot(light.dir, halfDir));

    half perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(smoothness);
    half roughness = PerceptualRoughnessToRoughness(perceptualRoughness);

    // GGX Distribution multiplied by combined approximation of Visibility and Fresnel
    // See "Optimizing PBR for Mobile" from Siggraph 2015 moving mobile graphics course
    // https://community.arm.com/events/1155
    half a = roughness;
    float a2 = a * a;

    float d = NDotH * NDotH * (a2 - 1.f) + 1.00001f;
    float specularTerm = 0.0;

    // Incorporate area light contribution for improved specularity.
#if defined(_AREA_LIGHT_SPECULAR)
    float areaLightContrib = CalculateAreaLightColor(
        light, 1.0, normal, viewDirTowardEye,
        roughness, NdotV);
    specularTerm = areaLightContrib;
#else
    specularTerm = a2 / (max(0.1f, LdotH * LdotH)
        * (roughness + 0.5f) * (d * d) * 4);
#endif

    half3 localDiffuseComponent = float3(0, 0, 0);
#if defined(_DIFFUSE_WRAP)
    half3 diffuseWrapColor = GetDiffuseWrap(NDotLUnclamped);
    localDiffuseComponent = diffColor * light.color * diffuseWrapColor;
#else
    localDiffuseComponent = diffColor * light.color * NdotL;
#endif

    // 1-0.28*x^3 as approximation for (1/(x^4+1))^(1/2.2) on the domain [0;1]
    // 1-x^3*(0.6-0.08*x)   approximation for 1/(x^4+1)
    half surfaceReduction = (0.6 - 0.08 * perceptualRoughness);
    surfaceReduction = 1.0 - roughness * perceptualRoughness * surfaceReduction;

#ifdef _SPECULAR_AFFECT_BY_NDOTL
    half3 localSpecTerm = specularTerm * specColor * light.color * NdotL;
#else
    half3 localSpecTerm = specularTerm * specColor;
#endif

    half grazingTerm = saturate(smoothness + (1 - oneMinusReflectivity));
    half3 color = localDiffuseComponent +
        localSpecTerm +
        gi.diffuse * diffColor +
        surfaceReduction * gi.specular * GetFresnelLerpFast(specColor, grazingTerm, NdotV);

    return half4(color, 1);
}


#ifdef _RECALCULATE_NORMALS
StructuredBuffer<int> vertexMetadata;
StructuredBuffer<int> subsetVertices;
StructuredBuffer<float3> vertices;
int vertexCount;
int firstSubmeshVertexIndex;
float3 GetRecalculatedNormal(uint vid : SV_VertexID)
{
  // The flattened metadata assumes that the first vertex is at index 0.
  // If the submesh vertex index starts at a larger,
  // subtract by the submesh's first vertex index to correct it to 0.
  int fixedVertexId = vid - firstSubmeshVertexIndex;
  // Normal Recalculation
  int idOfVertexInMap = vertexMetadata[fixedVertexId * 2];
  int idOfNextVertexInMap = vertexMetadata[fixedVertexId * 2 + 1];
  int offsetIntoVertexMap = vertexCount * 2;
  // vertexMetadata consists of two items:
  // 1. An offsets array
  // 2. A flattened map consisting of a vertex ID, followed immediately by
  //    the IDs of its neighbors.
  float3 vertex1 = vertices[vertexMetadata[offsetIntoVertexMap + idOfVertexInMap]];
  int startOfNeighborIndices = idOfVertexInMap + 1;
  float3 accumulatedNormal = float3(0, 0, 0);

  //  The CS code that generates neighbors will ensure that
  //  the list of neighbors consists of pairs, so it will be an even number
  //  of neighbors total.

  for (int j = startOfNeighborIndices; j < idOfNextVertexInMap - 1; j += 2)
  {
    float3 vertex2 = vertices[vertexMetadata[offsetIntoVertexMap + j]];
    float3 vertex3 = vertices[vertexMetadata[offsetIntoVertexMap + j + 1]];

    // Get the edges of the triangle connected at this vertex
    float3 edge1 = vertex2 - vertex1;
    float3 edge2 = vertex3 - vertex1;

    // Area of a triangle is half the length of the cross-product of two sides.
    // So to area-weight the normal contributions, don't normalize the cross product.
    float3 crossProd = cross(edge1, edge2);

    // Weight the normal by the angle of the triangle at the vertex.
    // Here's a good article that discusses this issue:
    // https://www.bytehazard.com/articles/vertnorm.html
    float angle = atan2(length(crossProd), dot(edge1, edge2));

    accumulatedNormal += crossProd * angle;
  }

  // Normalize normals in the shader used to render the surface.
  return TransformObjectToWorldNormal(accumulatedNormal);
}
#endif
