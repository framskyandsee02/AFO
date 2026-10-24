Shader "BORROWED TIME/TreePhaseDither"
{
    Properties {
        [MainColor] _BaseColor ("Color", Color) = (1,1,1,1)
        _BaseMap ("Albedo", 2D) = "white" {}
        _PhaseVisibility ("Phase visibility", Range(0,1)) = 1
    }
    SubShader {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass {
            Name "TreePhase"
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;
                float _PhaseVisibility;
            CBUFFER_END
            struct A { float4 p:POSITION; float3 n:NORMAL; float2 uv:TEXCOORD0; };
            struct V { float4 p:SV_POSITION; float3 world:TEXCOORD0; half3 n:TEXCOORD1; float2 uv:TEXCOORD2; };
            V Vert(A a) { V v; v.world=TransformObjectToWorld(a.p.xyz);v.p=TransformWorldToHClip(v.world);v.n=TransformObjectToWorldNormal(a.n);v.uv=TRANSFORM_TEX(a.uv,_BaseMap);return v; }
            half4 Frag(V v):SV_Target {
                float noise=frac(sin(dot(floor(v.p.xy),float2(12.9898,78.233)))*43758.5453);
                clip(_PhaseVisibility-noise-0.00001);
                Light light=GetMainLight(TransformWorldToShadowCoord(v.world));
                half3 n=normalize(v.n);
                half3 lighting=SampleSH(n)+light.color*saturate(dot(n,light.direction))*light.shadowAttenuation;
                return half4(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,v.uv).rgb*_BaseColor.rgb*max(lighting,half3(.2,.2,.2)),1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}
