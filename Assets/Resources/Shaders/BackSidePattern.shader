Shader "Unlit/BackBlackBlotches_URP_Fixed"
{
    Properties
    {
        _DissolveGuide("Dissolve guide", 2D) = "white" {}
        _DissolveThreshold("Dissolve threshold", Range(0, 1)) = 0.5
        [Toggle]_UseVertexAlpha("Use vertex alpha", Float) = 0

        _PatternScale("Pattern scale", Float) = 1.35
        _Scroll("Scroll", Vector) = (-0.09, 0.11, 0, 0)
        _Seed("Seed", Float) = 7.83
        _ThresholdBias("Threshold bias", Range(-1, 1)) = 0

        _Opacity("Opacity", Range(0, 1)) = 0.85
        _Color("Color", Color) = (0,0,0,1)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Transparent"
            "Queue"="Transparent-1"
        }

        Pass
        {
            Name "BackOnly"
            Tags { "LightMode"="SRPDefaultUnlit" }

            Cull Front
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _DissolveGuide_ST;
                float _DissolveThreshold;
                float _UseVertexAlpha;
                float _PatternScale;
                float4 _Scroll;
                float _Seed;
                float _ThresholdBias;
                float _Opacity;
                half4 _Color;
            CBUFFER_END

            TEXTURE2D(_DissolveGuide);
            SAMPLER(sampler_DissolveGuide);

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _DissolveGuide);
                output.color = input.color;
                return output;
            }

            half ResolveThreshold(Varyings input)
            {
                half baseThreshold = lerp(_DissolveThreshold, input.color.a, saturate(_UseVertexAlpha));
                return saturate(baseThreshold + _ThresholdBias);
            }

            half SampleGuide(float2 uv, float patternScale, float2 scroll, float seed)
            {
                float t = _Time.y;
                float2 baseUV = uv * patternScale + scroll * t;

                float2 jitter = float2(cos(seed * 2.11), sin(seed * 1.73)) * 0.19;

                half g0 = SAMPLE_TEXTURE2D(_DissolveGuide, sampler_DissolveGuide, baseUV + jitter).r;
                half g1 = SAMPLE_TEXTURE2D(_DissolveGuide, sampler_DissolveGuide, baseUV * 1.37 + float2(seed * 0.13, seed * 0.29)).r;

                return saturate(lerp(g0, g1, 0.42));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half guide = SampleGuide(input.uv, _PatternScale, _Scroll.xy, _Seed);
                clip(ResolveThreshold(input) - guide);

                half4 col = _Color;
                col.a *= _Opacity;

                return col;
            }
            ENDHLSL
        }
    }
}
