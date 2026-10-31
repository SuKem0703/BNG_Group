Shader "Custom/URPRetroRiverTilemap"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1,1,1,1)
        _PixelSize ("Pixel Grid Size", Float) = 32.0
        _Speed ("Animation Speed", Float) = 2.0
        _Density ("Active Point Density", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "RenderPipeline"="UniversalPipeline"
            "CanUseSpriteAtlas"="True"
            "IgnoreProjector"="True"
        }

        Cull Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            Name "Universal2D"
            Tags { "LightMode"="Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _PixelSize;
                float _Speed;
                float _Density;
            CBUFFER_END

            float Random(float2 st)
            {
                return frac(sin(dot(st.xy, float2(12.9898, 78.233))) * 43758.5453123);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 pixelUV = floor(input.uv * _PixelSize) / _PixelSize;
                float2 cellID = floor(input.uv * _PixelSize);
                
                float randVal = Random(cellID);
                float timeStep = floor(_Time.y * _Speed + randVal * 10.0);
                float activeCheck = Random(cellID + timeStep);
                
                float effectMask = (activeCheck > (1.0 - _Density)) ? 1.0 : 0.0;

                float2 flowUV = pixelUV;
                if (effectMask > 0.0)
                {
                    flowUV.x += sin(_Time.y * 4.0 + randVal * 6.28) * (1.0 / _PixelSize);
                }

                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, flowUV) * _Color * input.color;
                
                if (effectMask > 0.0)
                {
                    col.rgb += half3(0.12, 0.22, 0.32);
                }

                col.rgb *= col.a; // Premultiplied alpha tương thích tuyệt đối với Tilemap URP
                return col;
            }
            ENDHLSL
        }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _PixelSize;
                float _Speed;
                float _Density;
            CBUFFER_END

            float Random(float2 st)
            {
                return frac(sin(dot(st.xy, float2(12.9898, 78.233))) * 43758.5453123);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 pixelUV = floor(input.uv * _PixelSize) / _PixelSize;
                float2 cellID = floor(input.uv * _PixelSize);
                
                float randVal = Random(cellID);
                float timeStep = floor(_Time.y * _Speed + randVal * 10.0);
                float activeCheck = Random(cellID + timeStep);
                
                float effectMask = (activeCheck > (1.0 - _Density)) ? 1.0 : 0.0;

                float2 flowUV = pixelUV;
                if (effectMask > 0.0)
                {
                    flowUV.x += sin(_Time.y * 4.0 + randVal * 6.28) * (1.0 / _PixelSize);
                }

                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, flowUV) * _Color * input.color;
                
                if (effectMask > 0.0)
                {
                    col.rgb += half3(0.12, 0.22, 0.32);
                }

                col.rgb *= col.a;
                return col;
            }
            ENDHLSL
        }
    }
}