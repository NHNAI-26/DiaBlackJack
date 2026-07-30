#ifndef NHN_UBER_LIT_INPUT_INCLUDED
#define NHN_UBER_LIT_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

// Keep this layout identical for every keyword variant so the SRP Batcher can
// upload one stable material constant buffer.
CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    float4 _DissolveTilingOffset;
#if defined(NHN_SPRITE_UBER)
    float4 _BaseSpriteUVRect;
    float4 _CardBlendUVRect;
#endif
    half4 _BaseColor;
    half _HueShift;
    half _Saturation;
    half _Brightness;
    half _Contrast;
    half4 _EmissionColor;
    half4 _RimColor;
    half4 _HeightFadeTint;
    half4 _GlassGlowColor;
    half4 _DissolveEdgeColor;
    half4 _DissolvePanning;
#if defined(NHN_SPRITE_UBER)
    half _AlphaMultiplier;
    half _CardBlendAmount;
    half4 _PixelOutlineColor;
    half _PixelOutlineWidth;
    half _PixelOutlineAlphaThreshold;
    half _PixelOutlineVisibility;
    half _UVAlphaFadeOpaque;
    half _UVAlphaFadeTransparent;
#endif
    half _Cutoff;
    half _Metallic;
    half _Smoothness;
    half _BumpScale;
    half _OcclusionStrength;
    half _EmissionIntensity;
    half _RimPower;
    half _RimIntensity;
    float _HeightFadeLower;
    float _HeightFadeUpper;
    float _HeightFadeOffset;
    half _GlassGlowOffset;
    half _DissolveAmount;
    half _DissolveMinOffset;
    half _DissolveMaxOffset;
    half _DissolveEdgeWidth;
    half _DissolveEdgeIntensity;
    half _Surface;
    half _Cull;
CBUFFER_END

TEXTURE2D(_MetallicMap);
SAMPLER(sampler_MetallicMap);
TEXTURE2D(_SmoothnessMap);
SAMPLER(sampler_SmoothnessMap);
TEXTURE2D(_OcclusionMap);
SAMPLER(sampler_OcclusionMap);
TEXTURE2D(_DissolveNoiseMap);
SAMPLER(sampler_DissolveNoiseMap);
#if defined(NHN_SPRITE_UBER)
TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
float4 _MainTex_TexelSize;
TEXTURE2D(_CardBlendTex);
SAMPLER(sampler_CardBlendTex);
#endif

#if defined(NHN_SPRITE_UBER)
inline float2 NHNGetBaseSpriteUVUnclamped(float2 rawUV)
{
    return (rawUV - _BaseSpriteUVRect.xy)
        / max(_BaseSpriteUVRect.zw, float2(0.00001, 0.00001));
}

inline float2 NHNGetBaseSpriteUV(float2 rawUV)
{
    return saturate(NHNGetBaseSpriteUVUnclamped(rawUV));
}

inline half NHNGetSpriteUVInside(float2 baseSpriteUV)
{
    half2 lower = step(half2(0.0h, 0.0h), baseSpriteUV);
    half2 upper = step(baseSpriteUV, half2(1.0h, 1.0h));
    return lower.x * lower.y * upper.x * upper.y;
}
#endif

// Public entry points accept raw mesh UV. Surface textures share _BaseMap_ST;
// dissolve clipping transforms the raw UV independently with its own ST.
inline half4 NHNSampleBase(float2 rawUV, out float2 surfaceUV)
{
#if defined(NHN_SPRITE_UBER)
    surfaceUV = rawUV;
    half4 baseSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, rawUV);
    float2 baseSpriteUV = NHNGetBaseSpriteUV(rawUV);
    float2 blendSpriteUV = _CardBlendUVRect.xy + baseSpriteUV * _CardBlendUVRect.zw;
    half4 blendSample = SAMPLE_TEXTURE2D(_CardBlendTex, sampler_CardBlendTex, blendSpriteUV);
    return lerp(baseSample, blendSample, saturate(_CardBlendAmount));
#else
    surfaceUV = TRANSFORM_TEX(rawUV, _BaseMap);
    return SampleAlbedoAlpha(surfaceUV, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
#endif
}

inline half4 NHNSampleBase(float2 rawUV)
{
    float2 surfaceUV;
    return NHNSampleBase(rawUV, surfaceUV);
}

#if defined(NHN_SPRITE_UBER)
inline half NHNSampleSpriteBaseAlpha(float2 rawUV)
{
    float2 baseSpriteUV = NHNGetBaseSpriteUVUnclamped(rawUV);
    half inside = NHNGetSpriteUVInside(baseSpriteUV);
    half mainAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, rawUV).a;
    half blendAlpha = SAMPLE_TEXTURE2D(_CardBlendTex, sampler_CardBlendTex,
        saturate(baseSpriteUV)).a;
    return lerp(mainAlpha, blendAlpha, saturate(_CardBlendAmount)) * inside;
}

inline half NHNGetPixelOutlineMask(float2 rawUV, half baseAlpha)
{
#if defined(_PIXEL_OUTLINE_ON)
    half visibility = saturate(_PixelOutlineVisibility);
    if (visibility <= 0.0h)
    {
        return 0.0h;
    }

    half threshold = saturate(_PixelOutlineAlphaThreshold);
    half centerOpaque = step(threshold, baseAlpha);
    half neighborAlpha = 0.0h;
    half neighborOpaque = 1.0h;
    float2 texel = _MainTex_TexelSize.xy;

    [unroll]
    for (int i = 1; i <= 4; i++)
    {
        half ringEnabled = step((half)i - 0.5h, _PixelOutlineWidth);
        float2 offset = texel * (float)i;
        half sampleAlpha = NHNSampleSpriteBaseAlpha(rawUV + float2(offset.x, 0.0));
        neighborAlpha = max(neighborAlpha, sampleAlpha * ringEnabled);
        neighborOpaque = min(neighborOpaque, lerp(1.0h, step(threshold, sampleAlpha), ringEnabled));

        sampleAlpha = NHNSampleSpriteBaseAlpha(rawUV - float2(offset.x, 0.0));
        neighborAlpha = max(neighborAlpha, sampleAlpha * ringEnabled);
        neighborOpaque = min(neighborOpaque, lerp(1.0h, step(threshold, sampleAlpha), ringEnabled));

        sampleAlpha = NHNSampleSpriteBaseAlpha(rawUV + float2(0.0, offset.y));
        neighborAlpha = max(neighborAlpha, sampleAlpha * ringEnabled);
        neighborOpaque = min(neighborOpaque, lerp(1.0h, step(threshold, sampleAlpha), ringEnabled));

        sampleAlpha = NHNSampleSpriteBaseAlpha(rawUV - float2(0.0, offset.y));
        neighborAlpha = max(neighborAlpha, sampleAlpha * ringEnabled);
        neighborOpaque = min(neighborOpaque, lerp(1.0h, step(threshold, sampleAlpha), ringEnabled));

        sampleAlpha = NHNSampleSpriteBaseAlpha(rawUV + offset);
        neighborAlpha = max(neighborAlpha, sampleAlpha * ringEnabled);
        neighborOpaque = min(neighborOpaque, lerp(1.0h, step(threshold, sampleAlpha), ringEnabled));

        sampleAlpha = NHNSampleSpriteBaseAlpha(rawUV - offset);
        neighborAlpha = max(neighborAlpha, sampleAlpha * ringEnabled);
        neighborOpaque = min(neighborOpaque, lerp(1.0h, step(threshold, sampleAlpha), ringEnabled));

        sampleAlpha = NHNSampleSpriteBaseAlpha(rawUV + float2(offset.x, -offset.y));
        neighborAlpha = max(neighborAlpha, sampleAlpha * ringEnabled);
        neighborOpaque = min(neighborOpaque, lerp(1.0h, step(threshold, sampleAlpha), ringEnabled));

        sampleAlpha = NHNSampleSpriteBaseAlpha(rawUV + float2(-offset.x, offset.y));
        neighborAlpha = max(neighborAlpha, sampleAlpha * ringEnabled);
        neighborOpaque = min(neighborOpaque, lerp(1.0h, step(threshold, sampleAlpha), ringEnabled));
    }

    half outsideMask = (1.0h - centerOpaque) * step(threshold, neighborAlpha);
    half insideMask = centerOpaque * (1.0h - neighborOpaque);
    return max(outsideMask, insideMask) * visibility;
#else
    return 0.0h;
#endif
}

inline half NHNGetPixelOutlineAlpha(float2 rawUV, half baseAlpha)
{
    return NHNGetPixelOutlineMask(rawUV, baseAlpha) * _PixelOutlineColor.a;
}

inline half NHNEvaluateUVAlphaFade(float2 rawUV)
{
#if defined(_UV_ALPHA_FADE_U) || defined(_UV_ALPHA_FADE_V)
    float2 baseSpriteUV = NHNGetBaseSpriteUV(rawUV);
#if defined(_UV_ALPHA_FADE_U)
    float coordinate = baseSpriteUV.x;
#else
    float coordinate = baseSpriteUV.y;
#endif
    float range = _UVAlphaFadeOpaque - _UVAlphaFadeTransparent;
    float safeRange = abs(range) < 0.0001
        ? (range < 0.0 ? -0.0001 : 0.0001)
        : range;
    return saturate((coordinate - _UVAlphaFadeTransparent) / safeRange);
#else
    return 1.0h;
#endif
}
#endif

inline half3 NHNAdjustBaseColor(half3 color)
{
    half3 hsv = RgbToHsv(saturate(color));
    hsv.x = frac(hsv.x + _HueShift / 360.0h);
    hsv.y = saturate(hsv.y * max(_Saturation, 0.0h));
    hsv.z *= max(_Brightness, 0.0h);
    half3 adjusted = HsvToRgb(hsv);
    adjusted = (adjusted - 0.5h) * max(_Contrast, 0.0h) + 0.5h;
    return saturate(adjusted);
}

inline half2 NHNSampleMetallicSmoothness(float2 surfaceUV)
{
    half metallic = _Metallic;
    half smoothness = _Smoothness;
#if defined(_METALLICMAP)
    metallic *= SAMPLE_TEXTURE2D(_MetallicMap, sampler_MetallicMap, surfaceUV).r;
#endif
#if defined(_SMOOTHNESSMAP)
    smoothness *= SAMPLE_TEXTURE2D(_SmoothnessMap, sampler_SmoothnessMap, surfaceUV).r;
#endif
    return saturate(half2(metallic, smoothness));
}

inline half NHNSampleOcclusion(float2 surfaceUV)
{
#if defined(_OCCLUSIONMAP)
    half occlusion = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, surfaceUV).g;
    return LerpWhiteTo(occlusion, saturate(_OcclusionStrength));
#else
    return 1.0h;
#endif
}

inline half3 NHNSampleEmission(float2 surfaceUV)
{
#if defined(_EMISSION)
    half3 map = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, surfaceUV).rgb;
    return map * _EmissionColor.rgb * max(_EmissionIntensity, 0.0h);
#else
    return 0.0h;
#endif
}

// Shared by ForwardLit, ShadowCaster, DepthOnly, and DepthNormals. The caller
// supplies the already-sampled base alpha so ForwardLit does not sample twice.
inline half NHNApplySurfaceClipping(float2 rawUV, half baseAlpha, half vertexAlpha,
    out half dissolveEdge)
{
#if defined(NHN_SPRITE_UBER)
    baseAlpha = max(baseAlpha, NHNGetPixelOutlineAlpha(rawUV, baseAlpha));
#endif
    half alpha = baseAlpha * _BaseColor.a * vertexAlpha;
#if defined(NHN_SPRITE_UBER)
    alpha *= NHNEvaluateUVAlphaFade(rawUV);
#endif

#if defined(_ALPHATEST_ON)
    clip(alpha - _Cutoff);
#endif

#if defined(_DISSOLVE_ON)
    float2 dissolveUV = rawUV * _DissolveTilingOffset.xy + _DissolveTilingOffset.zw;
    dissolveUV += _DissolvePanning.xy * _Time.y;
    half noise = SAMPLE_TEXTURE2D(_DissolveNoiseMap, sampler_DissolveNoiseMap, dissolveUV).r;
    half amount = saturate(_DissolveAmount);
    half threshold = lerp(_DissolveMinOffset, 1.0h + _DissolveMaxOffset, amount);
    clip(noise - threshold);
    dissolveEdge = 1.0h - saturate((noise - threshold) / max(_DissolveEdgeWidth, 0.0001h));
#else
    dissolveEdge = 0.0h;
#endif

    return alpha;
}

inline half NHNApplySurfaceClipping(float2 rawUV, half baseAlpha, out half dissolveEdge)
{
    return NHNApplySurfaceClipping(rawUV, baseAlpha, 1.0h, dissolveEdge);
}

inline half3 NHNGetDissolveEdgeEmission(half dissolveEdge)
{
#if defined(_DISSOLVE_ON)
    return _DissolveEdgeColor.rgb * max(_DissolveEdgeIntensity, 0.0h) * dissolveEdge;
#else
    return 0.0h;
#endif
}

inline half3 NHNEvaluateRim(half3 normalWS, half3 viewDirectionWS)
{
#if defined(_RIM_ON)
    half fresnel = pow(saturate(1.0h - dot(normalize(normalWS), normalize(viewDirectionWS))),
        max(_RimPower, 0.0001h));
    return _RimColor.rgb * max(_RimIntensity, 0.0h) * fresnel;
#else
    return 0.0h;
#endif
}

inline half NHNEvaluateHeightFadeFactor(float coordinate)
{
#if defined(_HEIGHT_FADE_ON)
    float shiftedCoordinate = coordinate - _HeightFadeOffset;
    return saturate((shiftedCoordinate - _HeightFadeLower) /
        max(_HeightFadeUpper - _HeightFadeLower, 0.0001));
#else
    return 1.0h;
#endif
}

inline half3 NHNEvaluateHeightFade(float worldY)
{
#if defined(_HEIGHT_FADE_ON)
    return lerp(_HeightFadeTint.rgb, half3(1.0h, 1.0h, 1.0h),
        NHNEvaluateHeightFadeFactor(worldY));
#else
    return half3(1.0h, 1.0h, 1.0h);
#endif
}

inline half NHNEvaluateHeightFadeAlpha(float worldY)
{
#if defined(_HEIGHT_FADE_ON)
    return lerp(_HeightFadeTint.a, 1.0h, NHNEvaluateHeightFadeFactor(worldY));
#else
    return 1.0h;
#endif
}

#if defined(NHN_SPRITE_UBER)
inline half3 NHNEvaluateUVHeightFade(float2 rawUV)
{
    return NHNEvaluateHeightFade(NHNGetBaseSpriteUV(rawUV).y);
}

inline half NHNEvaluateUVHeightFadeAlpha(float2 rawUV)
{
    return NHNEvaluateHeightFadeAlpha(NHNGetBaseSpriteUV(rawUV).y);
}
#endif

inline half3 NHNEvaluateGlassGlow(half3 baseColor)
{
#if defined(_GLASS_GLOW_ON)
    half safeOffset = min(_GlassGlowOffset, 0.999h);
    half luminance = Luminance(baseColor);
    half mask = saturate((luminance - safeOffset) / max(1.0h - safeOffset, 0.0001h));
    return _GlassGlowColor.rgb * mask;
#else
    return 0.0h;
#endif
}

inline void InitializeNHNUberLitSurfaceData(float2 rawUV, half4 vertexColor,
    out SurfaceData surfaceData, out half dissolveEdge)
{
    float2 surfaceUV;
    half4 baseSample = NHNSampleBase(rawUV, surfaceUV);
    half2 metallicSmoothness = NHNSampleMetallicSmoothness(surfaceUV);
    half outlineMask = 0.0h;
#if defined(NHN_SPRITE_UBER)
    outlineMask = NHNGetPixelOutlineMask(rawUV, baseSample.a);
#endif

    surfaceData.alpha = NHNApplySurfaceClipping(rawUV, baseSample.a, vertexColor.a, dissolveEdge);
#if defined(NHN_SPRITE_UBER)
    surfaceData.alpha *= _AlphaMultiplier;
#endif
    surfaceData.albedo = NHNAdjustBaseColor(baseSample.rgb * _BaseColor.rgb * vertexColor.rgb);
#if defined(NHN_SPRITE_UBER)
    surfaceData.albedo = lerp(surfaceData.albedo, _PixelOutlineColor.rgb, outlineMask);
#endif
    surfaceData.albedo = AlphaModulate(surfaceData.albedo, surfaceData.alpha);
    surfaceData.metallic = metallicSmoothness.x;
    surfaceData.specular = half3(0.0h, 0.0h, 0.0h);
    surfaceData.smoothness = metallicSmoothness.y;
    surfaceData.normalTS = SampleNormal(surfaceUV,
        TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);
    surfaceData.occlusion = NHNSampleOcclusion(surfaceUV);
    surfaceData.emission = NHNSampleEmission(surfaceUV) + NHNGetDissolveEdgeEmission(dissolveEdge)
        + NHNEvaluateGlassGlow(baseSample.rgb);
    surfaceData.clearCoatMask = 0.0h;
    surfaceData.clearCoatSmoothness = 0.0h;
}


inline void InitializeNHNUberLitSurfaceData(float2 rawUV, out SurfaceData surfaceData,
    out half dissolveEdge)
{
    InitializeNHNUberLitSurfaceData(rawUV, half4(1.0h, 1.0h, 1.0h, 1.0h),
        surfaceData, dissolveEdge);
}

#endif // NHN_UBER_LIT_INPUT_INCLUDED
