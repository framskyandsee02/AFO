Shader "Borrowed Time/Overcast Forest Sky" {
 Properties {_Horizon("Horizon",Color)=(.53,.59,.60,1) _Zenith("Overcast zenith",Color)=(.25,.33,.39,1)}
 SubShader {Tags{"Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox"}Cull Off ZWrite Off
 Pass {HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 half4 _Horizon,_Zenith;
 struct A{float4 p:POSITION;};struct V{float4 p:SV_POSITION;float3 direction:TEXCOORD0;};
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise(float2 p){float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(a),hash(a+float2(1,0)),f.x),lerp(hash(a+float2(0,1)),hash(a+1),f.x),f.y);}
 V vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.direction=a.p.xyz;return o;}
 half4 frag(V i):SV_Target{
  float3 d=normalize(i.direction);float2 uv=d.xz/(.32+max(d.y,.04))*1.8+_Time.y*.0018;
  float cloud=noise(uv)*.58+noise(uv*2.13+8)*.29+noise(uv*4.3-11)*.13;
  half3 sky=lerp(_Horizon.rgb,_Zenith.rgb,saturate(d.y*1.1));
  sky=lerp(sky,half3(.66,.68,.66),smoothstep(.36,.74,cloud)*.65);
  sky-=smoothstep(.24,.65,cloud)*.055;
  float haze=pow(saturate(-d.z),4)*pow(1-saturate(d.y*3),2);
  sky=lerp(sky,half3(.46,.43,.49),haze*.55);
  return half4(sky,1);
 }
 ENDHLSL}}
}
