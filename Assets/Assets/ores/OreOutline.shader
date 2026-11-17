Shader "Toxic Depths/Ore Outline"
{
    Properties
    {
        _Width ("Konturbreite in Pixeln", Range(1, 6)) = 3
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float _Width;
        CBUFFER_END

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
        };

        Varyings VertMask(Attributes input)
        {
            Varyings output;
            output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
            return output;
        }

        Varyings VertOutline(Attributes input)
        {
            Varyings output;
            float4 positionCS = TransformObjectToHClip(input.positionOS.xyz);
            float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
            float4 normalCS = mul(UNITY_MATRIX_VP, float4(normalWS, 0.0));
            float2 screenSize = GetScaledScreenParams().xy;
            float2 direction = (normalCS.xy * positionCS.w - positionCS.xy * normalCS.w) * screenSize;
            direction *= rsqrt(max(dot(direction, direction), 1e-8));
            positionCS.xy += direction * (2.0 * _Width / screenSize) * positionCS.w;
            output.positionCS = positionCS;
            return output;
        }

        half4 Frag() : SV_Target
        {
            return half4(1, 1, 1, 1);
        }
        ENDHLSL

        Pass
        {
            Tags { "LightMode" = "SRPDefaultUnlit" }
            ColorMask 0
            ZWrite Off
            ZTest LEqual
            Stencil
            {
                Ref 1
                WriteMask 1
                Pass Replace
            }

            HLSLPROGRAM
            #pragma vertex VertMask
            #pragma fragment Frag
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            Cull Front
            ZWrite Off
            ZTest LEqual
            Stencil
            {
                Ref 1
                ReadMask 1
                Comp NotEqual
            }

            HLSLPROGRAM
            #pragma vertex VertOutline
            #pragma fragment Frag
            ENDHLSL
        }
    }
}