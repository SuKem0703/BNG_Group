Shader "CKM/2D/Pixel Water URP"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _WaterTint ("Water Tint", Color) = (0.65, 0.85, 1.0, 1.0)
        _TintStrength ("Tint Strength", Range(0,1)) = 0.18

        _WaveSpeed ("Wave Speed", Range(0,5)) = 0.8
        _WaveScale ("Wave Scale", Range(1,40)) = 12
        _WaveStrength ("Wave Strength", Range(0,0.05)) = 0.006

        _ShimmerStrength ("Shimmer Strength", Range(0,1)) = 0.08
        _ShimmerScale ("Shimmer Scale", Range(1,60)) = 18
        _PixelSnap ("Pixel Snap UV", Range(0,1)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "CanUseSpriteAtlas"="True"
            "IgnoreProjector"="True"
        }

        Cull Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            Tags { "LightMode"="Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _WaterTint;
                float _TintStrength;
                float _WaveSpeed;
                float _WaveScale;
                float _WaveStrength;
                float _ShimmerStrength;
                float _ShimmerScale;
                float _PixelSnap;
            CBUFFER_END

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
                float2 objectUV : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.objectUV = input.uv;
                output.color = input.color * _Color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float t = _Time.y * _WaveSpeed;

                // Very small UV distortion. PixelSnap reduces shimmer caused by
                // sub-pixel texture sampling, but does not quantize every screen pixel.
                float2 uv = input.uv;
                float waveA = sin((uv.y * _WaveScale) + t);
                float waveB = sin((uv.x * (_WaveScale * 0.73)) - t * 0.83);
                float2 distortion = float2(waveA, waveB) * _WaveStrength;

                uv += distortion * (1.0 - saturate(_PixelSnap));

                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

                float shimmerPattern = sin((input.objectUV.x * _ShimmerScale) + t * 1.7)
                                     * sin((input.objectUV.y * (_ShimmerScale * 0.67)) - t * 1.2);
                float shimmer = saturate(shimmerPattern * 0.5 + 0.5) * _ShimmerStrength;

                half3 tinted = lerp(tex.rgb, tex.rgb * _WaterTint.rgb, _TintStrength);
                tinted += shimmer * _WaterTint.rgb;

                half4 result;
                result.rgb = tinted * input.color.rgb;
                result.a = tex.a * input.color.a;

                // Premultiplied alpha for correct sprite transparency.
                result.rgb *= result.a;
                return result;
            }
            ENDHLSL
        }

        // Fallback pass for standard unlit sprite rendering paths.
        Pass
        {
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _WaterTint;
                float _TintStrength;
                float _WaveSpeed;
                float _WaveScale;
                float _WaveStrength;
                float _ShimmerStrength;
                float _ShimmerScale;
                float _PixelSnap;
            CBUFFER_END

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
                float2 objectUV : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.objectUV = input.uv;
                output.color = input.color * _Color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float t = _Time.y * _WaveSpeed;
                float2 uv = input.uv;
                float waveA = sin((uv.y * _WaveScale) + t);
                float waveB = sin((uv.x * (_WaveScale * 0.73)) - t * 0.83);
                uv += float2(waveA, waveB) * _WaveStrength * (1.0 - saturate(_PixelSnap));

                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                float shimmerPattern = sin((input.objectUV.x * _ShimmerScale) + t * 1.7)
                                     * sin((input.objectUV.y * (_ShimmerScale * 0.67)) - t * 1.2);
                float shimmer = saturate(shimmerPattern * 0.5 + 0.5) * _ShimmerStrength;
                half3 tinted = lerp(tex.rgb, tex.rgb * _WaterTint.rgb, _TintStrength);
                tinted += shimmer * _WaterTint.rgb;

                half4 result;
                result.rgb = tinted * input.color.rgb;
                result.a = tex.a * input.color.a;
                result.rgb *= result.a;
                return result;
            }
            ENDHLSL
        }
    }
}
