Shader "Borrowed Time/Anchored Waterfall" {
 Properties {_BaseColor("Waterfall",Color)=(.48,.68,.68,.85)}
 SubShader {Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent"}Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 Pass {Tags{"LightMode"="UniversalForward"}HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_fog
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 CBUFFER_START(UnityPerMaterial)half4 _BaseColor;CBUFFER_END
 struct A{float4 p:POSITION;float2 uv:TEXCOORD0;};struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;half fog:TEXCOORD1;};
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise(float2 p){float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(a),hash(a+float2(1,0)),f.x),lerp(hash(a+float2(0,1)),hash(a+1),f.x),f.y);}
 V vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.uv=a.uv;o.fog=ComputeFogFactor(o.p.z);return o;}
 half4 frag(V i):SV_Target{
  float flow=i.uv.y*9-_Time.y*2.0;
  float streak=noise(float2(i.uv.x*17,flow*.4))*.65+noise(float2(i.uv.x*33+21,flow*.18))*.35;
  float broken=noise(float2(i.uv.x*5+17,flow));
  float edge=smoothstep(0,.12,i.uv.x)*(1-smoothstep(.88,1,i.uv.x));
  half3 c=_BaseColor.rgb+streak*.12+broken*.04;
  return half4(MixFog(c,i.fog),_BaseColor.a*edge*(.65+.3*streak));
 }
 ENDHLSL}}
}
