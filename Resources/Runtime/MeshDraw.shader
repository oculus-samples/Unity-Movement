
// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

Shader "Movement/MeshDraw"
{
    Properties
    {
        _Color ("Color", Color) = (1,0,0,1)
        _Power ("Power", Float) = 0.25
        _Filling ("Filling", Float) = 0.0
        _ZTest ("ZTest", Int) = 0
        _ZWrite ("ZWrite", Int) = 0
    }
    SubShader
    {
        Tags
        {
            "Queue"="Overlay" "RenderType"="Transparent" "IgnoreProjector"="True"
        }
        LOD 100
        Pass
        {
            ZTest [_ZTest]
            ZWrite [_ZWrite]
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ STEREO_INSTANCING_ON
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                fixed4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float3 normal : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
                fixed4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;
            float _Power;
            float _Filling;

            v2f vert(appdata v) {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = normalize(v.normal);
                o.viewDir = normalize(ObjSpaceViewDir(v.vertex));
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half saturation = saturate(dot(normalize(i.viewDir), i.normal));
                half Power = (1 - _Filling) * (1 - saturation) + (_Filling * saturation);
                half4 PowerOut = _Color * i.color * pow(Power, _Power);
                return PowerOut;
            }
            ENDHLSL
        }
    }
    FallBack "Mobile/Unlit (Supports Lightmap)"
}
