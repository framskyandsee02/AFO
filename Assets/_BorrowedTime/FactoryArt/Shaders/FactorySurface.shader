Shader "Borrowed Time/Factory worn surface" {
 Properties { _BaseColor("Colour",Color)=(.15,.18,.19,1) _Wear("Wear",Range(0,1))=.22 }
 SubShader { Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" } Pass {
  Tags { "LightMode"="UniversalForward" }
  HLSLPROGRAM
  #pragma vertex vert
  #pragma fragment frag
  #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
  #pragma multi_compile_fragment _ _SHADOWS_SOFT
  #pragma multi_compile _ _ADDITIONAL_LIGHTS
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
  struct A {float4 p:POSITION;float3 n:NORMAL;};struct V {float4 p:SV_POSITION;float3 w:TEXCOORD0;float3 n:TEXCOORD1;};
  CBUFFER_START(UnityPerMaterial) float4 _BaseColor;float _Wear; CBUFFER_END
  V vert(A a){V o;o.w=TransformObjectToWorld(a.p.xyz);o.p=TransformWorldToHClip(o.w);o.n=TransformObjectToWorldNormal(a.n);return o;}
  float hash(float3 p){return frac(sin(dot(p,float3(17.31,57.79,29.13)))*43758.5453);}
  float noise(float3 p){float3 q=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(lerp(hash(q),hash(q+float3(1,0,0)),f.x),lerp(hash(q+float3(0,1,0)),hash(q+float3(1,1,0)),f.x),f.y),lerp(lerp(hash(q+float3(0,0,1)),hash(q+float3(1,0,1)),f.x),lerp(hash(q+float3(0,1,1)),hash(q+float3(1,1,1)),f.x),f.y),f.z);}
  half4 frag(V i):SV_Target {float3 n=normalize(i.n);float grime=noise(i.w*.55)*.6+noise(i.w*2.7)*.4;float3 c=_BaseColor.rgb*(.76+.28*grime);float rust=noise(i.w*1.2)*_Wear*.35;c=lerp(c,float3(.19,.105,.065),rust);Light l=GetMainLight(TransformWorldToShadowCoord(i.w));float diffuse=saturate(dot(n,l.direction));float3 lighting=SampleSH(n)+l.color*(.2+.8*diffuse)*l.shadowAttenuation;
  for(uint j=0;j<(uint)GetAdditionalLightsCount();j++){Light extraLight=GetAdditionalLight(j,i.w);lighting+=extraLight.color*saturate(dot(n,extraLight.direction))*extraLight.distanceAttenuation;}
  return half4(c*max(lighting,float3(.26,.26,.29)),1);}
  ENDHLSL
 } UsePass "Universal Render Pipeline/Lit/ShadowCaster" UsePass "Universal Render Pipeline/Lit/DepthOnly" }
}
