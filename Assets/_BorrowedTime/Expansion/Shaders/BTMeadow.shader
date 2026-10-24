Shader "Borrowed Time/Meadow Surface" {
 Properties {_BaseColor("Color",Color)=(.2,.5,.1,1) _Wind("Leaf movement",Float)=0}
 SubShader {Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"} Pass {
 Tags {"LightMode"="UniversalForward"} HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
 #pragma multi_compile_fragment _ _SHADOWS_SOFT
 #pragma multi_compile_fog
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 CBUFFER_START(UnityPerMaterial) half4 _BaseColor;float _Wind; CBUFFER_END
 struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;};
 struct V {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;half fog:TEXCOORD2;};
 V vert(A v){V o;float3 p=v.positionOS.xyz;p.x+=sin(_Time.y*1.25+p.z*.7+p.y*.4)*.025*_Wind;VertexPositionInputs inputs=GetVertexPositionInputs(p);o.positionCS=inputs.positionCS;o.positionWS=inputs.positionWS;o.normalWS=TransformObjectToWorldNormal(v.normalOS);o.fog=ComputeFogFactor(o.positionCS.z);return o;}
 half4 frag(V i):SV_Target {half3 n=normalize(i.normalWS);Light l=GetMainLight(TransformWorldToShadowCoord(i.positionWS));half d=saturate(dot(n,l.direction));half shadow=lerp(.58,1,l.shadowAttenuation);half noise=sin(i.positionWS.x*.27+sin(i.positionWS.z*.15))*sin(i.positionWS.z*.19);half3 c=_BaseColor.rgb*(.90+.10*noise);half slope=saturate(n.y);c*=lerp(.70,1,slope);c*=.35+(d*.8+.12)*shadow;c+=SampleSH(n)*_BaseColor.rgb*.15;return half4(MixFog(c,i.fog),1);}
 ENDHLSL }
 UsePass "Universal Render Pipeline/Lit/ShadowCaster"
 UsePass "Universal Render Pipeline/Lit/DepthOnly"
 } }
