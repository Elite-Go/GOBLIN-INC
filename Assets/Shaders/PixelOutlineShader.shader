Shader "Custom/PixelOutlineShader"
{
    Properties
    {
        _MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
        _OutlineColor("Outline Color", Color) = (0,0,0,1)
        _OutlineThickness("Outline Thickness", Range(1, 5)) = 1
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "RenderType"="Transparent" 
            "RenderPipeline"="UniversalPipeline" 
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "PixelOutline"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

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

            Texture2D _MainTex;
            SamplerState sampler_MainTex;
            float4 _MainTex_TexelSize; // 텍스처 한 픽셀의 크기 정보 (자동 제공)

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _OutlineColor;
                float _OutlineThickness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color * _Color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 texColor = _MainTex.Sample(sampler_MainTex, input.uv) * input.color;

                // 현재 픽셀이 이미 채워져 있다면 기존 스프라이트 색상 반환
                if (texColor.a > 0.01)
                {
                    return texColor;
                }

                // 픽셀 아트 크기에 맞춘 텍셀 단위 오프셋 계산
                float w = _MainTex_TexelSize.x * _OutlineThickness;
                float h = _MainTex_TexelSize.y * _OutlineThickness;

                // 상하좌우 인접 픽셀의 알파값 체크
                float alpha = 0;
                alpha += _MainTex.Sample(sampler_MainTex, input.uv + float2(w, 0)).a;
                alpha += _MainTex.Sample(sampler_MainTex, input.uv + float2(-w, 0)).a;
                alpha += _MainTex.Sample(sampler_MainTex, input.uv + float2(0, h)).a;
                alpha += _MainTex.Sample(sampler_MainTex, input.uv + float2(0, -h)).a;

                // 주변에 불투명한 픽셀이 있다면 아웃라인 색상 출력
                if (alpha > 0.01)
                {
                    return _OutlineColor;
                }

                return texColor;
            }
            ENDHLSL
        }
    }
}