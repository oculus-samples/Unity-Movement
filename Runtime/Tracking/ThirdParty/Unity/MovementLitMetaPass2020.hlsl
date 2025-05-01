// Based on com.unity.render-pipelines.universal/Shaders/LitMetaPass.hlsl
#ifndef UNIVERSAL_LIT_META_PASS_INCLUDED
#define UNIVERSAL_LIT_META_PASS_INCLUDED

#include "Packages/com.meta.movement/Runtime/Shaders/MovementCommon.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

struct Attributes
{
  float4 positionOS   : POSITION;
  float3 normalOS     : NORMAL;
  float2 uv0          : TEXCOORD0;
  float2 uv1          : TEXCOORD1;
  float2 uv2          : TEXCOORD2;
#ifdef _TANGENT_TO_WORLD
  float4 tangentOS     : TANGENT;
#endif
};

struct Varyings
{
  float4 positionCS   : SV_POSITION;
  float2 uv           : TEXCOORD0;
};

Varyings UniversalVertexMeta(Attributes input)
{
  Varyings output;
  output.positionCS = MetaVertexPosition(input.positionOS, input.uv1, input.uv2, unity_LightmapST, unity_DynamicLightmapST);
  output.uv = TRANSFORM_TEX(input.uv0, _MainTex);
  return output;
}

void InitializeStandardLitSurfaceData(float2 uv, out SurfaceData outSurfaceData)
{
  half4 albedoAlpha = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
  outSurfaceData.alpha = Alpha(albedoAlpha.a, _Color, _Cutoff);

  half4 specGloss = ComputeSpecularGloss(uv);
  outSurfaceData.albedo = albedoAlpha.rgb * _Color.rgb;

#if _SPECULAR_SETUP
  outSurfaceData.metallic = 1.0h;
  outSurfaceData.specular = specGloss.rgb;
#else
  outSurfaceData.metallic = specGloss.r;
  outSurfaceData.specular = half3(0.0h, 0.0h, 0.0h);
#endif

  outSurfaceData.smoothness = specGloss.a;
  outSurfaceData.normalTS = SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);
  outSurfaceData.occlusion = GetAmbientOcclusion(uv);
  outSurfaceData.emission = SampleEmission(uv, _EmissionColor.rgb, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap));

#if defined(_CLEARCOAT) || defined(_CLEARCOATMAP)
  half2 clearCoat = SampleClearCoat(uv);
  outSurfaceData.clearCoatMask       = clearCoat.r;
  outSurfaceData.clearCoatSmoothness = clearCoat.g;
#else
  outSurfaceData.clearCoatMask       = 0.0h;
  outSurfaceData.clearCoatSmoothness = 0.0h;
#endif

#if defined(_DETAIL)
  half detailMask = SAMPLE_TEXTURE2D(_DetailMask, sampler_DetailMask, uv).a;
  float2 detailUv = uv * _DetailAlbedoMap_ST.xy + _DetailAlbedoMap_ST.zw;
  outSurfaceData.albedo = ApplyDetailAlbedo(detailUv, outSurfaceData.albedo, detailMask);
  outSurfaceData.normalTS = ApplyDetailNormal(detailUv, outSurfaceData.normalTS, detailMask);

#endif
}

half4 UniversalFragmentMeta(Varyings input) : SV_Target
{
  SurfaceData surfaceData;
  InitializeStandardLitSurfaceData(input.uv, surfaceData);

  BRDFData brdfData;
  InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.alpha, brdfData);

  MetaInput metaInput;
  metaInput.Albedo = brdfData.diffuse + brdfData.specular * brdfData.roughness * 0.5;
  metaInput.SpecularColor = surfaceData.specular;
  metaInput.Emission = surfaceData.emission;

  return MetaFragment(metaInput);
}

//LWRP -> Universal Backwards Compatibility
Varyings LightweightVertexMeta(Attributes input)
{
  return UniversalVertexMeta(input);
}

half4 LightweightFragmentMeta(Varyings input) : SV_Target
{
  return UniversalFragmentMeta(input);
}

#endif
