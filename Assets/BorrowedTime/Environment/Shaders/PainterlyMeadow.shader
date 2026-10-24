Shader "Borrowed Time/Painterly Meadow"
{
    Properties
    {
        _BaseColor("Palette tint", Color) = (1,1,1,1)
        _AmbientTint("Soft sky fill", Color) = (0.90,0.97,0.82,1)
        _AmbientStrength("Ambient fill", Range(0,1)) = 0.60
        _DirectStrength("Sunlight", Range(0,1)) = 0.60
        _WindAmplitude("Gentle grass sway", Range(0,0.06)) = 0.012
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Cull Off
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor, _AmbientTint;
                half _AmbientStrength, _DirectStrength, _WindAmplitude;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; half3 normalWS : TEXCOORD1; half4 color : COLOR; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 position = input.positionOS.xyz;
                float weight = saturate(position.y * 3.0);
                position.x += sin(position.x * 1.8 + position.z * 1.1 + _Time.y * 0.8) * _WindAmplitude * weight;
                VertexPositionInputs p = GetVertexPositionInputs(position);
                output.positionCS = p.positionCS; output.positionWS = p.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                // FBX contains sRGB vertex colors, matching Blender's Color attribute.
                output.color = half4(SRGBToLinear(input.color.rgb), input.color.a);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                Light sun = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half diffuse = max(0.28h, abs(dot(normalize(input.normalWS), sun.direction)));
                half shade = lerp(0.64h, 1.0h, sun.shadowAttenuation);
                half3 lighting = _AmbientTint.rgb * _AmbientStrength + sun.color * diffuse * _DirectStrength * shade;
                return half4(input.color.rgb * _BaseColor.rgb * lighting, 1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
