// Based on com.unity.render-pipelines.universal/Shaders/LitMetaPass.hlsl and
// com.unity.render-pipelines.universal/Shaders/LitInput.hlsl
#ifndef UNIVERSAL_LIT_META_PASS_INCLUDED
#define UNIVERSAL_LIT_META_PASS_INCLUDED

#include "Packages/com.meta.xr.sdk.movement/Shared/Shaders/MovementCommon.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

struct Attributes
{
  float4 positionOS   : POSITION;
  float3 normalOS     : NORMAL;
  float2 uv0          : TEXCOORD0;
  float2 uv1          : TEXCOORD1;
  float2 uv2          : TEXCOORD2;
  UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
  float4 positionCS   : SV_POSITION;
  float2 uv           : TEXCOORD0;
#ifdef EDITOR_VISUALIZATION
  float2 VizUV        : TEXCOORD1;
  float4 LightCoord   : TEXCOORD2;
#endif
};

Varyings UniversalVertexMeta(Attributes input)
{
  Varyings output = (Varyings)0;
  output.positionCS = UnityMetaVertexPosition(input.positionOS.xyz, input.uv1, input.uv2);
  output.uv = TRANSFORM_TEX(input.uv0, _MainTex);
#ifdef EDITOR_VISUALIZATION
  UnityEditorVizData(input.positionOS.xyz, input.uv0, input.uv1, input.uv2, output.VizUV, output.LightCoord);
#endif
  return output;
}

half4 UniversalFragmentMeta(Varyings fragIn, MetaInput metaInput)
{
#ifdef EDITOR_VISUALIZATION
  metaInput.VizUV = fragIn.VizUV;
  metaInput.LightCoord = fragIn.LightCoord;
#endif

  return UnityMetaFragment(metaInput);
}

half4 UniversalFragmentMetaLit(Varyings input) : SV_Target
{
  half4 albedoAlpha =  half4(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv));
  half4 specGloss = SampleMetallicSpecGloss(input.uv, albedoAlpha.a);
  half3 diffuse;
  half3 specular;
  const half perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(specGloss.a);
  const half roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
#if _SPECULAR_SETUP
  diffuse = half(1.0);
  specular = specGloss.rgb;
#else
  diffuse = specGloss.r;
  specular = half3(0.0, 0.0, 0.0);
#endif

  MetaInput metaInput;
  metaInput.Albedo = diffuse + specular * roughness * 0.5;
  metaInput.Emission = _EmissionStrength *
        SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).rgb * _EmissionColor.rgb;
  return UniversalFragmentMeta(input, metaInput);
}
#endif
