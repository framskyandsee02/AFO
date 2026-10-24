Shader "Borrowed Time/Soft River" {
 Properties {_BaseColor("Deep water",Color)=(.02,.22,.32,1) _ShallowColor("Water light",Color)=(.12,.65,.65,1)}
 SubShader {Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"} Pass {
 Tags {"LightMode"="UniversalForward"} HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_fog
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 CBUFFER_START(UnityPerMaterial)half4 _BaseColor,_ShallowColor;CBUFFER_END
 struct A{float4 p:POSITION;};struct V{float4 p:SV_POSITION;float3 w:TEXCOORD0;half fog:TEXCOORD1;};
 V vert(A a){V o;VertexPositionInputs v=GetVertexPositionInputs(a.p.xyz);o.p=v.positionCS;o.w=v.positionWS;o.fog=ComputeFogFactor(o.p.z);return o;}
 half4 frag(V i):SV_Target{float wave=sin(i.w.x*1.7+i.w.z*.8+_Time.y*1.25)*sin(i.w.z*2-i.w.x*.45-_Time.y*.8);float stripe=pow(saturate(wave),10);half3 c=lerp(_BaseColor.rgb,_ShallowColor.rgb,.45+.18*sin(i.w.x*.18+i.w.z*.2))+stripe*.22;return half4(MixFog(c,i.fog),1);}
 ENDHLSL } } }
