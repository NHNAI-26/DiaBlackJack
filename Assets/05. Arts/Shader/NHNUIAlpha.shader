Shader "Shader/UI Alpha"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _AlphaMultiplier ("Alpha Multiplier", Range(0,1)) = 1
        [Toggle] _UseRgbOverride ("Use RGB Override", Float) = 0
        _RgbOverrideColor ("RGB Override Color", Color) = (1,1,1,1)
        [Toggle] _RespectVertexRgbTint ("Respect UI RGB Tint", Float) = 0
        [Toggle(_PIXEL_OUTLINE_ON)] _PixelOutlineEnabled ("Pixel Outline", Float) = 0
        [HDR] _PixelOutlineColor ("Outline Color", Color) = (1,1,1,1)
        _PixelOutlineWidth ("Outline Width (Pixels)", Range(0,4)) = 1
        _PixelOutlineAlphaThreshold ("Outline Alpha Threshold", Range(0,1)) = 0.5
        _PixelOutlineGlowWidth ("Outline Glow Width (Pixels)", Range(0,8)) = 4
        _PixelOutlineGlowAlpha ("Outline Glow Alpha", Range(0,1)) = 0.35
        [HideInInspector] _PixelOutlineVisibility ("Outline Visibility", Range(0,1)) = 0
        [HideInInspector] _BaseSpriteUVRect ("Base Sprite UV Rect", Vector) = (0,0,1,1)
        [HideInInspector] _PixelOutlineMeshPadding ("Outline Mesh Padding", Vector) = (0,0,0,0)
        [Toggle(_UV_ALPHA_FADE_ON)] _UVAlphaFadeEnabled ("UV Fade", Float) = 0
        [Enum(U,0,V,1,Radial,2)] _UVAlphaFadeAxis ("UV Fade Mode", Float) = 1
        [HideInInspector] [PerRendererData] _UVAlphaFadeUVRect ("UV Fade UV Rect", Vector) = (0,0,1,1)
        _UVAlphaFadeUVOffset ("UV Fade UV Offset XY", Vector) = (0,0,0,0)
        _UVAlphaFadeLower ("UV Fade Low Point", Range(0,1)) = 0
        _UVAlphaFadeUpper ("UV Fade High Point", Range(0,1)) = 1
        _UVAlphaFadeOffset ("UV Fade Offset", Range(-1,1)) = 0
        [Toggle] _UVAlphaFadeInvert ("Invert UV Fade", Float) = 0
        _UVAlphaFadeColor ("UV Fade Color", Color) = (1,1,1,0)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Alpha Clipping", Float) = 0
        _Cutoff ("Threshold", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #pragma shader_feature_local _PIXEL_OUTLINE_ON
            #pragma shader_feature_local_fragment _UV_ALPHA_FADE_ON

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
#if defined(UNITY_UI_CLIP_RECT)
                half4 mask : TEXCOORD2;
#endif
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;
            float _AlphaMultiplier;
            float _UseRgbOverride;
            fixed4 _RgbOverrideColor;
            float _RespectVertexRgbTint;
            half4 _PixelOutlineColor;
            half _PixelOutlineWidth;
            half _PixelOutlineAlphaThreshold;
            half _PixelOutlineGlowWidth;
            half _PixelOutlineGlowAlpha;
            half _PixelOutlineVisibility;
            float4 _BaseSpriteUVRect;
            float4 _PixelOutlineMeshPadding;
            float _UVAlphaFadeAxis;
            float4 _UVAlphaFadeUVRect;
            float4 _UVAlphaFadeUVOffset;
            float _UVAlphaFadeLower;
            float _UVAlphaFadeUpper;
            float _UVAlphaFadeOffset;
            float _UVAlphaFadeInvert;
            fixed4 _UVAlphaFadeColor;
            float _Cutoff;

            v2f vert(appdata_t input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                #if defined(_PIXEL_OUTLINE_ON)
                float2 spriteCenter = _BaseSpriteUVRect.xy +
                    _BaseSpriteUVRect.zw * 0.5;
                float2 expandDirection = sign(input.texcoord - spriteCenter);
                input.vertex.xy += expandDirection * _PixelOutlineMeshPadding.xy;
                input.texcoord += expandDirection * _PixelOutlineMeshPadding.zw;
                #endif

                float4 clipPosition = UnityObjectToClipPos(input.vertex);
#if defined(UNITY_UI_CLIP_RECT)
                float2 pixelSize = clipPosition.w;
                pixelSize /= abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));
                float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                output.mask = half4(
                    input.vertex.xy * 2 - clampedRect.xy - clampedRect.zw,
                    0.25 / (0.25 * half2(
                        _UIMaskSoftnessX,
                        _UIMaskSoftnessY) + abs(pixelSize.xy)));
#endif
                output.worldPosition = input.vertex;
                output.vertex = clipPosition;
                output.texcoord = input.texcoord;
                output.color = input.color * _Color;
                return output;
            }

            half4 SampleSpriteColor(float2 uv)
            {
                float2 minimum = _BaseSpriteUVRect.xy;
                float2 maximum = minimum + _BaseSpriteUVRect.zw;
                half inside = step(minimum.x, uv.x) *
                    step(minimum.y, uv.y) *
                    step(uv.x, maximum.x) *
                    step(uv.y, maximum.y);
                float2 clampedUv = clamp(uv, minimum, maximum);
                return (tex2D(_MainTex, clampedUv) + _TextureSampleAdd) * inside;
            }

            half SampleSpriteAlpha(float2 uv)
            {
                return SampleSpriteColor(uv).a;
            }

            void GetPixelOutlineMasks(
                float2 uv,
                half baseAlpha,
                out half outline,
                out half glow)
            {
                outline = 0.0h;
                glow = 0.0h;
                #if defined(_PIXEL_OUTLINE_ON)
                half visibility = saturate(_PixelOutlineVisibility);
                if (visibility <= 0.0h)
                {
                    return;
                }

                half threshold = saturate(_PixelOutlineAlphaThreshold);
                half centerOpaque = step(threshold, baseAlpha);
                half outsideMask = 1.0h - centerOpaque;
                float2 texel = _MainTex_TexelSize.xy;
                half glowWidth = max(_PixelOutlineGlowWidth, _PixelOutlineWidth);
                half glowRange = max(glowWidth - _PixelOutlineWidth, 1.0h);

                [unroll]
                for (int i = 1; i <= 8; i++)
                {
                    half outlineRingEnabled =
                        step((half)i - 0.5h, _PixelOutlineWidth);
                    half glowRingEnabled = step((half)i - 0.5h, glowWidth);
                    float2 offset = texel * (float)i;
                    half ringAlpha = 0.0h;
                    ringAlpha = max(ringAlpha,
                        SampleSpriteAlpha(uv + float2(offset.x, 0.0)));
                    ringAlpha = max(ringAlpha,
                        SampleSpriteAlpha(uv - float2(offset.x, 0.0)));
                    ringAlpha = max(ringAlpha,
                        SampleSpriteAlpha(uv + float2(0.0, offset.y)));
                    ringAlpha = max(ringAlpha,
                        SampleSpriteAlpha(uv - float2(0.0, offset.y)));
                    ringAlpha = max(ringAlpha, SampleSpriteAlpha(uv + offset));
                    ringAlpha = max(ringAlpha, SampleSpriteAlpha(uv - offset));
                    ringAlpha = max(ringAlpha,
                        SampleSpriteAlpha(uv + float2(offset.x, -offset.y)));
                    ringAlpha = max(ringAlpha,
                        SampleSpriteAlpha(uv + float2(-offset.x, offset.y)));

                    half ringMask = outsideMask * step(threshold, ringAlpha);
                    outline = max(outline, ringMask * outlineRingEnabled);

                    half glowDistance = max((half)i - _PixelOutlineWidth, 0.0h);
                    half glowFalloff = saturate(
                        1.0h - glowDistance / (glowRange + 1.0h));
                    glow = max(
                        glow,
                        ringMask * glowRingEnabled * glowFalloff);
                }

                outline *= visibility;
                glow *= (1.0h - outline) * visibility;
                #endif
            }

            half Bayer4x4(float2 screenPosition)
            {
                float2 pixel = floor(frac(screenPosition * 0.25) * 4.0);
                half threshold = 0.0h;

                if (pixel.y < 1.0)
                {
                    if (pixel.x < 1.0)
                        threshold = 0.0h;
                    else if (pixel.x < 2.0)
                        threshold = 8.0h;
                    else if (pixel.x < 3.0)
                        threshold = 2.0h;
                    else
                        threshold = 10.0h;
                }
                else if (pixel.y < 2.0)
                {
                    if (pixel.x < 1.0)
                        threshold = 12.0h;
                    else if (pixel.x < 2.0)
                        threshold = 4.0h;
                    else if (pixel.x < 3.0)
                        threshold = 14.0h;
                    else
                        threshold = 6.0h;
                }
                else if (pixel.y < 3.0)
                {
                    if (pixel.x < 1.0)
                        threshold = 3.0h;
                    else if (pixel.x < 2.0)
                        threshold = 11.0h;
                    else if (pixel.x < 3.0)
                        threshold = 1.0h;
                    else
                        threshold = 9.0h;
                }
                else
                {
                    if (pixel.x < 1.0)
                        threshold = 15.0h;
                    else if (pixel.x < 2.0)
                        threshold = 7.0h;
                    else if (pixel.x < 3.0)
                        threshold = 13.0h;
                    else
                        threshold = 5.0h;
                }

                return (threshold + 0.5h) / 16.0h;
            }

            half4 frag(v2f input) : SV_Target
            {
                half4 sampleColor = SampleSpriteColor(input.texcoord);
                half4 color = sampleColor * input.color;
                color.a *= _AlphaMultiplier;

                if (_UseRgbOverride > 0.5)
                {
                    color.rgb = _RgbOverrideColor.rgb *
                        lerp(fixed3(1.0, 1.0, 1.0), input.color.rgb,
                            saturate(_RespectVertexRgbTint));
                    color.a *= _RgbOverrideColor.a;
                }

                half outline;
                half glow;
                GetPixelOutlineMasks(
                    input.texcoord,
                    sampleColor.a,
                    outline,
                    glow);
                half glowAlpha = glow * _PixelOutlineGlowAlpha *
                    _PixelOutlineColor.a * input.color.a;
                color.rgb = lerp(color.rgb, _PixelOutlineColor.rgb, glowAlpha);
                color.a = max(color.a, glowAlpha);
                color.rgb = lerp(color.rgb, _PixelOutlineColor.rgb, outline);
                color.a = max(
                    color.a,
                    outline * _PixelOutlineColor.a * input.color.a);

                #ifdef _UV_ALPHA_FADE_ON
                float2 localUV = (input.texcoord - _UVAlphaFadeUVRect.xy)
                    / max(_UVAlphaFadeUVRect.zw, float2(0.00001, 0.00001));
                localUV = saturate(localUV);
                float2 fadeUV = localUV + _UVAlphaFadeUVOffset.xy;
                float linearCoordinate = lerp(fadeUV.x, fadeUV.y, step(0.5, _UVAlphaFadeAxis));
                float radialCoordinate = length(fadeUV - 0.5) * 1.41421356;
                float coordinate = lerp(linearCoordinate, radialCoordinate, step(1.5, _UVAlphaFadeAxis));
                coordinate -= _UVAlphaFadeOffset;
                float fadeUpper = _UVAlphaFadeUpper;
                if (abs(fadeUpper - _UVAlphaFadeLower) < 0.0001)
                    fadeUpper = _UVAlphaFadeLower + 0.0001;
                half fade = smoothstep(_UVAlphaFadeLower, fadeUpper, coordinate);
                fade = lerp(fade, 1.0h - fade, step(0.5, _UVAlphaFadeInvert));
                color.rgb = lerp(_UVAlphaFadeColor.rgb, color.rgb, fade);
                clip(lerp(_UVAlphaFadeColor.a, 1.0h, fade) - Bayer4x4(input.vertex.xy));
                #endif

                #ifdef UNITY_UI_CLIP_RECT
                half2 mask = saturate(
                    (_ClipRect.zw - _ClipRect.xy - abs(input.mask.xy)) *
                    input.mask.zw);
                color.a *= mask.x * mask.y;
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - _Cutoff);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
