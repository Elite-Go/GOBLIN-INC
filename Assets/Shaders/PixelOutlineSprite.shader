Shader "Custom/Pixel Sprite Outline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (1,0.86,0.25,1)
        _OutlineWidth ("Outline Width (Reference Pixels)", Float) = 1
        [HideInInspector] _OutlineSpriteSize ("Sprite Source Size", Vector) = (1,1,0,0)
        [HideInInspector] _OutlineReferenceResolution ("Outline Reference Resolution", Vector) = (1,1,0,0)
        [HideInInspector] _OutlinePixelLocalSize ("Outline Local Pixel Size", Vector) = (0,0,0,0)
        [HideInInspector] _OutlineMeshCenter ("Sprite Mesh Center", Vector) = (0,0,0,0)
        [HideInInspector] _OutlineUvBounds ("Sprite UV Bounds", Vector) = (0,0,1,1)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "CanUseSpriteAtlas"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "SpriteUnlitOutline"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _OutlineColor;
                float _OutlineWidth;
                float4 _OutlineSpriteSize;
                float4 _OutlineReferenceResolution;
                float4 _OutlinePixelLocalSize;
                float4 _OutlineMeshCenter;
                float4 _OutlineUvBounds;
            CBUFFER_END

            float4 _MainTex_TexelSize;

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

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float2 outward = sign(input.positionOS.xy - _OutlineMeshCenter.xy);
                input.positionOS.xy += outward * _OutlinePixelLocalSize.xy;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color * _Color;
                output.uv = input.uv;
                return output;
            }

            half SampleAlpha(float2 uv)
            {
                if (uv.x < _OutlineUvBounds.x || uv.y < _OutlineUvBounds.y ||
                    uv.x > _OutlineUvBounds.z || uv.y > _OutlineUvBounds.w)
                    return 0;
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 referenceSize = max(_OutlineReferenceResolution.xy, float2(1.0, 1.0));
                float2 sourceSize = max(_OutlineSpriteSize.xy, float2(1.0, 1.0));
                float2 pixelStep = _MainTex_TexelSize.xy * sourceSize / referenceSize * _OutlineWidth;

                bool insideSpriteUv = input.uv.x >= _OutlineUvBounds.x && input.uv.y >= _OutlineUvBounds.y &&
                                      input.uv.x <= _OutlineUvBounds.z && input.uv.y <= _OutlineUvBounds.w;
                half4 source = insideSpriteUv ? SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) : 0;
                half surroundingAlpha = max(
                    max(SampleAlpha(input.uv + float2(pixelStep.x, 0)), SampleAlpha(input.uv - float2(pixelStep.x, 0))),
                    max(SampleAlpha(input.uv + float2(0, pixelStep.y)), SampleAlpha(input.uv - float2(0, pixelStep.y))));
                surroundingAlpha = max(surroundingAlpha,
                    max(max(SampleAlpha(input.uv + pixelStep), SampleAlpha(input.uv - pixelStep)),
                        max(SampleAlpha(input.uv + float2(pixelStep.x, -pixelStep.y)),
                            SampleAlpha(input.uv + float2(-pixelStep.x, pixelStep.y)))));

                half sourceAlpha = source.a * input.color.a;
                half outlineAlpha = saturate(surroundingAlpha - source.a) * _OutlineColor.a * input.color.a;
                half4 sourceColor = source * input.color;
                half outputAlpha = sourceAlpha + outlineAlpha * (1.0h - sourceAlpha);
                half3 premultipliedRgb = sourceColor.rgb * sourceAlpha + _OutlineColor.rgb * outlineAlpha * (1.0h - sourceAlpha);
                half3 outputRgb = outputAlpha > 0.0001h ? premultipliedRgb / outputAlpha : 0;
                return half4(outputRgb, outputAlpha);
            }
            ENDHLSL
        }
    }
}
