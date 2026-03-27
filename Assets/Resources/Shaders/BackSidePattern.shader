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
        _CycleSpeed("Cycle speed", Float) = 0.5
        _SweepWidth("Sweep width", Range(0.05, 0.6)) = 0.3
        _GrowDuration("Grow duration", Range(0.02, 0.45)) = 0.18
        _GrowStartRadius("Grow start radius", Range(0.0, 0.35)) = 0.06

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
                float _CycleSpeed;
                float _SweepWidth;
                float _GrowDuration;
                float _GrowStartRadius;
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

            float Hash21(float2 p)
            {
                p = frac(p * float2(443.897, 441.423));
                p += dot(p, p + 19.19);
                return frac(p.x * p.y);
            }

            float2 Hash22(float2 p)
            {
                float x = sin(dot(p, float2(127.1, 311.7))) * 43758.5453;
                float y = sin(dot(p, float2(269.5, 183.3))) * 43758.5453;
                return frac(float2(x, y));
            }

            half SampleFoamLayer(float2 uv, float seed, float t)
            {
                float2 cellBase = floor(uv);
                float2 local = frac(uv) - 0.5;
                half layer = 0.0;

                [unroll]
                for (int y = -1; y <= 1; y++)
                {
                    [unroll]
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 cell = cellBase + float2(x, y);
                        float2 jitter = (Hash22(cell + seed * 0.37) - 0.5) * 0.95;
                        float sizeRnd = Hash21(cell + seed * 1.13 + 3.7);
                        float phaseRnd = Hash21(cell.yx + seed * 2.31 + 9.1);

                        float2 drift = float2(
                            sin(t * (1.2 + phaseRnd * 2.4) + phaseRnd * 6.2831),
                            cos(t * (1.45 + phaseRnd * 1.9) + phaseRnd * 6.2831)
                        ) * 0.08;

                        float2 center = float2(x, y) + jitter + drift;
                        float radius = lerp(0.2, 0.55, sizeRnd);
                        float dist = length(local - center);
                        half bubble = 1.0 - smoothstep(radius * 0.65, radius, dist);

                        layer = max(layer, bubble);
                    }
                }

                return layer;
            }

            half SampleBubblePattern(float2 uv, float patternScale, float2 scroll, float seed)
            {
                float t = _Time.y;
                float2 baseUV = uv * patternScale + scroll * t;
                float2 jitter = float2(cos(seed * 2.11), sin(seed * 1.73)) * 0.19;

                half guideA = SAMPLE_TEXTURE2D(_DissolveGuide, sampler_DissolveGuide, baseUV + jitter).r;
                half guideB = SAMPLE_TEXTURE2D(_DissolveGuide, sampler_DissolveGuide, baseUV * 1.37 + float2(seed * 0.13, seed * 0.29)).r;
                half guide = saturate(lerp(guideA, guideB, 0.42));

                half foamLarge = SampleFoamLayer(baseUV * 4.8 + float2(seed * 0.31, -seed * 0.27), seed + 1.7, t);
                half foamSmall = SampleFoamLayer(baseUV * 8.6 + float2(-seed * 0.23, seed * 0.41), seed + 5.3, t * 1.17);
                half foam = saturate(max(foamLarge, foamSmall * 0.78));

                half resistance = 1.0 - foam * 0.88;
                resistance = lerp(resistance, guide, 0.36);
                return saturate(resistance);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half phaseOffset = ResolveThreshold(input);
                half phase = frac(_Time.y * _CycleSpeed + phaseOffset);
                half growDuration = saturate(_GrowDuration);
                half growPhase = saturate(phase / max(growDuration, 0.0001));

                float2 ndc = input.positionHCS.xy / max(input.positionHCS.w, 0.0001);
                float2 screenUV = ndc * 0.5 + 0.5;
                float2 radialScreen = (screenUV - 0.5) * 2.0;
                radialScreen.x *= _ScreenParams.x / _ScreenParams.y;
                half radialDist = length(radialScreen);
                half growRadius = lerp(_GrowStartRadius, 1.42, growPhase);
                half growMask = 1.0 - smoothstep(growRadius, growRadius + 0.02, radialDist);
                half introOnly = 1.0 - step(growDuration, phase);
                half introMask = lerp(1.0, growMask, introOnly);

                half dissolvePhase = saturate((phase - growDuration) / max(1.0 - growDuration, 0.0001));
                half dissolveEnabled = step(growDuration, phase);

                half pattern = SampleBubblePattern(input.uv, _PatternScale, _Scroll.xy, _Seed);
                half dissolved = step(pattern, dissolvePhase) * dissolveEnabled;
                half phaseOut = 1.0 - smoothstep(0.88, 1.0, phase);
                half visibleBack = introMask * (1.0 - dissolved) * phaseOut;

                clip(visibleBack - 0.5);

                half4 col = _Color;
                col.a *= _Opacity;

                return col;
            }
            ENDHLSL
        }
    }
}
