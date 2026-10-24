Shader "Borrowed Time/Storybook Sky" {Properties{_Horizon("Horizon",Color)=(.67,.84,.9,1) _Zenith("Blue sky",Color)=(.29,.57,.82,1)}
 SubShader{Tags{"Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox"}Cull Off ZWrite Off Pass{HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 half4 _Horizon,_Zenith;struct A{float4 p:POSITION;};struct V{float4 p:SV_POSITION;float3 direction:TEXCOORD0;};
 V vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.direction=a.p.xyz;return o;}
 half4 frag(V i):SV_Target{float3 d=normalize(i.direction);half3 c=lerp(_Horizon.rgb,_Zenith.rgb,saturate(d.y*1.45));float sun=pow(saturate(dot(d,normalize(float3(-.4,.55,-.6)))),500);return half4(c+sun*.6,1);}
 ENDHLSL}}}
