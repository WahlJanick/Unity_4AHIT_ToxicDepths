Shader "Toxic Depths/Ore Scan Wireframe"
{
    Properties
    {
        [HDR] _Tint ("Wireframe Color", Color) = (0.44, 1, 0.196, 1)
        _LineWidth ("Line Width (Pixels)", Range(0.5, 3)) = 1.1
        [PerRendererData] _Sweep ("Sweep", Vector) = (0, 1, 0, 0)
        [PerRendererData] _Opacity ("Opacity", Range(0, 1)) = 1
        [PerRendererData] _Impact ("Beam Impact", Vector) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent+10" "RenderType" = "Transparent" }
        Pass
        {
            Blend SrcAlpha One
            Cull Back
            ZWrite Off
            ZTest LEqual
            Offset -1, -1

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Sweep;
                float4 _Tint;
                float _LineWidth;
                float _Opacity;
                float4 _Impact;
                float4x4 _WorldToBeam;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 tangentOS : TANGENT;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 barycentric : TEXCOORD0;
                float height : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.height = (positionWS.y - _Sweep.x) * _Sweep.y;
                output.positionWS = positionWS;
                output.barycentric = input.tangentOS.xyz;
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                // Pixel-distance barycentric mask from the package's Wireframe-Pixel subgraph.
                float3 pixel = max(fwidth(input.barycentric), 0.0001);
                float3 edge = smoothstep(pixel * (_LineWidth * 0.35), pixel * _LineWidth, input.barycentric);
                float wire = 1 - min(edge.x, min(edge.y, edge.z));
                float cutoff = _Sweep.z - input.height;
                float revealed = smoothstep(0, 0.024, cutoff);
                float complete = step(0.9999, _Sweep.z);
                float scanLine = (1 - smoothstep(0.003, 0.024, abs(cutoff))) * (1 - complete);
                float alpha = saturate(wire * max(revealed, complete) * (0.46 + _Sweep.w * 0.43)
                    + scanLine * 0.64) * _Opacity;
                float3 beam = mul(_WorldToBeam, float4(input.positionWS, 1)).xyz;
                float square = max(abs(beam.x), abs(beam.y));
                float depth = abs(beam.z - _Impact.z) * _Impact.y;
                float footprint = _Impact.x * (1 - smoothstep(0.32, 0.5, square)) *
                    (1 - smoothstep(0.10, 1, depth));
                alpha += (1 - alpha) * footprint * (0.20 + wire * 0.23);
                return float4(_Tint.rgb * (1.35 + scanLine * 0.8 + _Sweep.w * 1.1), alpha);
            }
            ENDHLSL
        }
    }
}
