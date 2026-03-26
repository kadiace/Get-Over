Shader "Unlit/TwoSidedDissolve"
{
    Properties
    {
        _DissolveGuide("Dissolve guide", 2D) = "white" {}
        _DissolveThreshold("Dissolve threshold", Range(0, 1)) = 1
        [Toggle]_UseVertexAlpha("Use vertex alpha", Float) = 1
        _FrontPatternScale("Front pattern scale", Float) = 1
        _BackPatternScale("Back pattern scale", Float) = 1.35
        _FrontScroll("Front scroll", Vector) = (0.12, 0.06, 0, 0)
        _BackScroll("Back scroll", Vector) = (-0.09, 0.11, 0, 0)
        _FrontSeed("Front seed", Float) = 2.17
        _BackSeed("Back seed", Float) = 7.83
        _FrontThresholdBias("Front threshold bias", Range(-1, 1)) = 0
        _BackThresholdBias("Back threshold bias", Range(-1, 1)) = 0
        _FrontColor("Front color", color) = (1,1,1,1)
        _BackColor("Back color", color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "FrontFace"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragFront

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
                float _FrontPatternScale;
                float _BackPatternScale;
                float4 _FrontScroll;
                float4 _BackScroll;
                float _FrontSeed;
                float _BackSeed;
                float _FrontThresholdBias;
                float _BackThresholdBias;
                half4 _FrontColor;
                half4 _BackColor;
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

            half ResolveFrontThreshold(Varyings input)
            {
                half baseThreshold = lerp(_DissolveThreshold, input.color.a, saturate(_UseVertexAlpha));
                return saturate(baseThreshold + _FrontThresholdBias);
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

            half4 FragFront(Varyings input) : SV_Target
            {
                half guide = SampleGuide(input.uv, _FrontPatternScale, _FrontScroll.xy, _FrontSeed);
                clip(guide - ResolveFrontThreshold(input));
                return _FrontColor;
            }

            ENDHLSL
        }

        Pass
        {
            Name "BackFace"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Front
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragBack

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
                float _FrontPatternScale;
                float _BackPatternScale;
                float4 _FrontScroll;
                float4 _BackScroll;
                float _FrontSeed;
                float _BackSeed;
                float _FrontThresholdBias;
                float _BackThresholdBias;
                half4 _FrontColor;
                half4 _BackColor;
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

            half ResolveBackThreshold(Varyings input)
            {
                half baseThreshold = lerp(_DissolveThreshold, input.color.a, saturate(_UseVertexAlpha));
                return saturate(baseThreshold + _BackThresholdBias);
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

            half4 FragBack(Varyings input) : SV_Target
            {
                float2 backUV = float2(1 - input.uv.x, input.uv.y);
                half guide = 1 - SampleGuide(backUV, _BackPatternScale, _BackScroll.xy, _BackSeed);
                clip(guide - ResolveBackThreshold(input));
                return _BackColor;
            }

            ENDHLSL
        }
    }
}
