Shader "Toxic Depths/Ore Scan Beam"
{
    Properties
    {
        [HDR] _Tint ("Beam Color", Color) = (0.44, 1, 0.196, 1)
        [PerRendererData] _Opacity ("Opacity", Range(0, 1)) = 1
        [PerRendererData] _Contact ("Surface Contact", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }
        Pass
        {
            Blend SrcAlpha One
            Cull Back
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Tint;
                float _Opacity;
                float _Contact;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float depth : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.depth = input.uv.y;
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float facing = abs(dot(normalize(input.normalWS), view));
                float softEdge = 0.28 + 0.72 * smoothstep(0.015, 0.32, facing);
                float along = smoothstep(0, 0.045, input.depth) *
                    (1 - smoothstep(0.86, 1, input.depth) * (1 - 0.48 * _Contact));
                float pulse = 0.9 + 0.1 * sin(_Time.y * 12 - input.depth * 14);
                return float4(_Tint.rgb, _Opacity * along * softEdge * pulse * 0.21);
            }
            ENDHLSL
        }
    }
}
