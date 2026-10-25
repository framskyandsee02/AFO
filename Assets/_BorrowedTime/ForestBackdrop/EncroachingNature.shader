Shader "Borrowed Time/Encroaching Nature" {
 Properties {
  _BaseColor("Tint",Color)=(1,1,1,1)
  _Healthy("Healthy",Color)=(.38,.61,.25,1)
  _Stressed("Stressed",Color)=(.57,.57,.32,1)
  _Damaged("Damaged",Color)=(.40,.43,.32,1)
  _Blight("Blight",Color)=(.30,.29,.32,1)
  _WorldMap("World infection map",2D)="black"{}
  _MapBounds("Map origin and span",Vector)=(-140,-110,200,200)
  _SurfaceKind("Ground=0 foliage=1 water=2",Float)=0
  _BorrowedLife("Borrowed life stress",Float)=0
  _BaseMap("Base",2D)="white"{}
  _Cutoff("Cutoff",Float)=.5
  _Cull("Cull",Float)=2
 }
 SubShader {Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
 Pass {Tags{"LightMode"="UniversalForward"}Cull[_Cull]
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
 #pragma multi_compile_fragment _ _SHADOWS_SOFT
 #pragma multi_compile_fog
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 TEXTURE2D(_WorldMap);SAMPLER(sampler_WorldMap);
 CBUFFER_START(UnityPerMaterial)
 half4 _BaseColor,_Healthy,_Stressed,_Damaged,_Blight;float4 _MapBounds;float _SurfaceKind,_BorrowedLife,_Cutoff;
 CBUFFER_END
 struct A{float4 positionOS:POSITION;float3 normalOS:NORMAL;};
 struct V{float4 positionCS:SV_POSITION;float3 w:TEXCOORD0;half3 n:TEXCOORD1;half fog:TEXCOORD2;};
 V vert(A v){V o;VertexPositionInputs p=GetVertexPositionInputs(v.positionOS.xyz);o.positionCS=p.positionCS;o.w=p.positionWS;o.n=TransformObjectToWorldNormal(v.normalOS);o.fog=ComputeFogFactor(o.positionCS.z);return o;}
 half4 frag(V i):SV_Target{
  float2 uv=(i.w.xz-_MapBounds.xy)/_MapBounds.zw;
  half3 field=SAMPLE_TEXTURE2D(_WorldMap,sampler_WorldMap,uv).rgb;
  float infection=saturate(field.r+_BorrowedLife*.24),patch=field.g;
  half3 c=lerp(_Healthy.rgb,_Stressed.rgb,smoothstep(.10,.57,infection));
  float eaten=smoothstep(.51,.72,patch+infection*.12)*smoothstep(.14,.68,infection);
  c=lerp(c,_Damaged.rgb,eaten*.72);
  c=lerp(c,_Damaged.rgb,smoothstep(.38,.92,infection)*(.40+.60*patch));
  c=lerp(c,_Blight.rgb,smoothstep(.67,1,infection)*smoothstep(.48,.85,patch)*.65);
  float mottling=sin(i.w.x*1.7+sin(i.w.z*.7))*sin(i.w.z*1.2+i.w.x*.4);
  c*=.96+.045*mottling;
  if(_SurfaceKind>1.5){float wave=sin(i.w.x*1.6+i.w.z*.8+_Time.y*.9)*sin(i.w.z*1.8-_Time.y*.65);c+=pow(saturate(wave),12)*.055;}
  half3 n=normalize(i.n);Light l=GetMainLight(TransformWorldToShadowCoord(i.w));
  half diffuse=saturate(dot(n,l.direction));half shade=lerp(.65,1,l.shadowAttenuation);
  c*=.43+(.22+diffuse*.52)*shade;
  c+=SampleSH(n)*.11;c*=_BaseColor.rgb;
  return half4(MixFog(c,i.fog),1);
 }
 ENDHLSL}
 UsePass "Universal Render Pipeline/Lit/ShadowCaster"
 UsePass "Universal Render Pipeline/Lit/DepthOnly"
 }
}
