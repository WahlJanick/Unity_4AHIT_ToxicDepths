Shader "Toxic Depths/Poison Cloud"
{
    Properties
    {
        [MainTexture] _BaseMap ("Smoke flipbook", 2D) = "white" {}
        [MainColor] _BaseColor ("Tint", Color) = (0.36, 0.86, 0.09, 0.58)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                output.color = input.color * _BaseColor;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 smoke = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                // The imported flipbook stores its smoke mask in grayscale over opaque black.
                return half4(input.color.rgb, input.color.a * smoke.r * smoke.a);
            }
            ENDHLSL
        }
    }
}
