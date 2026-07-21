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
#endif

// Public entry points accept raw mesh UV. Surface textures share _BaseMap_ST;
// dissolve clipping transforms the raw UV independently with its own ST.
inline half4 NHNSampleBase(float2 rawUV, out float2 surfaceUV)
{
#if defined(NHN_SPRITE_UBER)
    surfaceUV = rawUV;
    return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, rawUV);
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
    half alpha = baseAlpha * _BaseColor.a * vertexAlpha;

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

inline half3 NHNEvaluateHeightFade(float worldY)
{
#if defined(_HEIGHT_FADE_ON)
    float heightFactor = saturate((worldY - _HeightFadeLower) /
        max(_HeightFadeUpper - _HeightFadeLower, 0.0001));
    return lerp(_HeightFadeTint.rgb, half3(1.0h, 1.0h, 1.0h), heightFactor);
#else
    return half3(1.0h, 1.0h, 1.0h);
#endif
}

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

    surfaceData.alpha = NHNApplySurfaceClipping(rawUV, baseSample.a, vertexColor.a, dissolveEdge);
    surfaceData.albedo = NHNAdjustBaseColor(baseSample.rgb * _BaseColor.rgb * vertexColor.rgb);
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
