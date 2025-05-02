// Copyright (c) Meta Platforms, Inc. and affiliates.

Shader "Movement/Procedural Gradient Skybox"
{
    Properties
    {
        _TopColor ("Top Color", Color) = (1, 1, 1, 0)
        _HorizonColor ("Horizon Color", Color) = (1, 1, 1, 0)
        _BottomColor ("Bottom Color", Color) = (1, 1, 1, 0)
        _TopExponent ("Top Exponent", Float) = 0.5
        _BottomExponent ("Bottom Exponent", Float) = 0.5
        _AmplFactor ("Amplification", Float) = 1.0
    }

    // URP sub shader
    SubShader
    {
        Tags
        {
            "RenderType" ="Background"
            "Queue" = "Background"
            "RenderPipeline" = "UniversalPipeline"
        }
        ZWrite Off Cull Off
        Fog { Mode Off }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

#if IS_URP
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#else
            #include "UnityCG.cginc"
#endif

            struct vertIn
            {
                float4 vertex : POSITION;
                float3 uv : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct vertOut
            {
                float4 vertex : SV_POSITION;
                float3 uv: TEXCOORD0;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            vertOut vert (vertIn v)
            {
                vertOut o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(vertOut, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

#if IS_URP
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
#else
                o.vertex = UnityObjectToClipPos(v.vertex);
#endif
                o.uv = v.uv;
                return o;
            }

            half _TopExponent;
            half _BottomExponent;
            half4 _TopColor;
            half4 _HorizonColor;
            half4 _BottomColor;
            half _AmplFactor;

            half4 frag (vertOut i) : SV_Target
            {
                float interpUv = normalize (i.uv).y;
                // top goes from 0->1 going down toward horizon
                float topLerp = 1.0f - pow (min (1.0f, abs(1.0f - interpUv)), _TopExponent);
                // bottom goes from 0->1 going up toward horizon
                float bottomLerp = 1.0f - pow (min (1.0f, abs(1.0f + interpUv)), _BottomExponent);
                // last lerp param is horizon. all must add up to 1.0
                float horizonLerp = 1.0f - topLerp - bottomLerp;
                return (_TopColor * topLerp +
                    _HorizonColor * horizonLerp +
                    _BottomColor * bottomLerp) *
                    _AmplFactor;
            }

            ENDHLSL
        }
    }

    SubShader
    {
        Tags{"RenderType" ="Background" "Queue" = "Background"}
        ZWrite Off Cull Off
        Fog { Mode Off }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct vertIn
            {
                float4 vertex : POSITION;
                float3 uv : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct vertOut
            {
                float4 vertex : SV_POSITION;
                float3 uv : TEXCOORD0;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            vertOut vert (vertIn v)
            {
                vertOut o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(vertOut, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            half _TopExponent;
            half _BottomExponent;
            fixed4 _TopColor;
            fixed4 _HorizonColor;
            fixed4 _BottomColor;
            half _AmplFactor;

            fixed4 frag (vertOut i) : SV_Target
            {
                float interpUv = normalize (i.uv).y;
                // top goes from 0->1 going down toward horizon
                float topLerp = 1.0f - pow (min (1.0f, 1.0f - interpUv), _TopExponent);
                // bottom goes from 0->1 going up toward horizon
                float bottomLerp = 1.0f - pow (min (1.0f, 1.0f + interpUv), _BottomExponent);
                // last lerp param is horizon. all must add up to 1.0
                float horizonLerp = 1.0f - topLerp - bottomLerp;
                return (_TopColor * topLerp +
                    _HorizonColor * horizonLerp +
                    _BottomColor * bottomLerp) *
                    _AmplFactor;
            }

            ENDCG
        }
    }
}
