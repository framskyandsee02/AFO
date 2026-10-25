Shader "Borrowed Time/Quiet Water" {
 Properties {_BaseColor("Water",Color)=(.18,.37,.40,1) _WorldMap("Pollution",2D)="black"{} _MapBounds("Bounds",Vector)=(-140,-110,200,200)}
 SubShader {Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}Cull Off
 Pass {Tags{"LightMode"="UniversalForward"}HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_fog
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_WorldMap);SAMPLER(sampler_WorldMap);
 CBUFFER_START(UnityPerMaterial)half4 _BaseColor;float4 _MapBounds;CBUFFER_END
 struct A{float4 p:POSITION;};struct V{float4 p:SV_POSITION;float3 w:TEXCOORD0;half fog:TEXCOORD1;};
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise(float2 p){float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(a),hash(a+float2(1,0)),f.x),lerp(hash(a+float2(0,1)),hash(a+1),f.x),f.y);}
 V vert(A a){V o;VertexPositionInputs v=GetVertexPositionInputs(a.p.xyz);o.p=v.positionCS;o.w=v.positionWS;o.fog=ComputeFogFactor(o.p.z);return o;}
 half4 frag(V i):SV_Target{
  float2 p=i.w.xz;float infection=SAMPLE_TEXTURE2D(_WorldMap,sampler_WorldMap,(p-_MapBounds.xy)/_MapBounds.zw).r;
  float broad=noise(p*.042+float2(_Time.y*.006,-_Time.y*.004));
  float ripple=noise(p*float2(.38,.11)+float2(_Time.y*.037,0));
  float highlight=smoothstep(.65,.86,ripple)*(.4+.6*noise(p*.09+23));
  half3 clean=lerp(_BaseColor.rgb*.90,_BaseColor.rgb*1.12,broad);
  half3 c=lerp(clean,half3(.30,.32,.34),smoothstep(.22,.92,infection)*.65);
  c+=highlight*.026;
  return half4(MixFog(c,i.fog),1);
 }
 ENDHLSL}}
}
