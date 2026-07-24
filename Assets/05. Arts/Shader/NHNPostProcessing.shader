Shader "Shader/NHN Post Processing"
{
    Properties
    {
        [Toggle(_PIXELATION_ON)] _PixelationEnabled("Pixelation", Float) = 1
        _PixelSize("Pixel Size", Range(1, 64)) = 4
        [Toggle(_COLOR_QUANTIZATION_ON)] _ColorQuantizationEnabled("Color Quantization", Float) = 1
        _ColorLevels("Color Levels", Range(2, 64)) = 16
        [Toggle(_ORDERED_DITHER_ON)] _OrderedDitherEnabled("Ordered Dithering", Float) = 1
        _DitherStrength("Dither Strength", Range(0, 1)) = 0.65
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }

        Pass
        {
            Name "NHN Post Processing"
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma shader_feature_local_fragment _PIXELATION_ON
            #pragma shader_feature_local_fragment _COLOR_QUANTIZATION_ON
            #pragma shader_feature_local_fragment _ORDERED_DITHER_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _PixelSize;
                float _ColorLevels;
                float _DitherStrength;
            CBUFFER_END

            float Bayer4x4(uint2 pixelPosition)
            {
                static const float thresholds[16] =
                {
                     0,  8,  2, 10,
                    12,  4, 14,  6,
                     3, 11,  1,  9,
                    15,  7, 13,  5
                };
                uint index = (pixelPosition.y & 3) * 4 + (pixelPosition.x & 3);
                return (thresholds[index] + 0.5) / 16.0 - 0.5;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                half4 color;
                #if defined(_PIXELATION_ON)
                    float2 blockSize = _BlitTexture_TexelSize.xy * max(round(_PixelSize), 1.0);
                    uv = (floor(uv / blockSize) + 0.5) * blockSize;
                    color = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_PointClamp, uv, 0);
                #else
                    color = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv, 0);
                #endif

                #if defined(_ORDERED_DITHER_ON)
                    color.rgb += Bayer4x4(uint2(input.positionCS.xy)) * (_DitherStrength / 16.0);
                #endif

                #if defined(_COLOR_QUANTIZATION_ON)
                    float levelCount = max(round(_ColorLevels), 2.0);
                    color.rgb = round(saturate(color.rgb) * (levelCount - 1.0)) / (levelCount - 1.0);
                #endif

                return color;
            }
            ENDHLSL
        }
    }
}
