#ifndef NHN_UBER_LIT_DEPTH_PASSES_INCLUDED
#define NHN_UBER_LIT_DEPTH_PASSES_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#include "NHNUberLitInput.hlsl"

float3 _LightDirection;
float3 _LightPosition;

struct NHNDepthAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct NHNSilhouetteVaryings
{
#if defined(_ALPHATEST_ON) || defined(_DISSOLVE_ON)
    float2 rawUV : TEXCOORD0;
#endif
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

inline void NHNClipSilhouette(float2 rawUV)
{
    half dissolveEdge;
#if defined(_ALPHATEST_ON)
    half baseAlpha = NHNSampleBase(rawUV).a;
#else
    half baseAlpha = 1.0h;
#endif
    NHNApplySurfaceClipping(rawUV, baseAlpha, dissolveEdge);
}

NHNSilhouetteVaryings NHNShadowVertex(NHNDepthAttributes input)
{
    NHNSilhouetteVaryings output = (NHNSilhouetteVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
#if defined(_ALPHATEST_ON) || defined(_DISSOLVE_ON)
    output.rawUV = input.uv;
#endif
    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
#if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
#else
    float3 lightDirectionWS = _LightDirection;
#endif
    output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
    output.positionCS = ApplyShadowClamping(output.positionCS);
    return output;
}

NHNSilhouetteVaryings NHNDepthOnlyVertex(NHNDepthAttributes input)
{
    NHNSilhouetteVaryings output = (NHNSilhouetteVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
#if defined(_ALPHATEST_ON) || defined(_DISSOLVE_ON)
    output.rawUV = input.uv;
#endif
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    return output;
}

half4 NHNSilhouetteFragment(NHNSilhouetteVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
#if defined(_ALPHATEST_ON) || defined(_DISSOLVE_ON)
    NHNClipSilhouette(input.rawUV);
#endif
    return 0.0h;
}

struct NHNDepthNormalsVaryings
{
#if defined(_NORMALMAP) || defined(_ALPHATEST_ON) || defined(_DISSOLVE_ON)
    float2 rawUV : TEXCOORD0;
#endif
    half3 normalWS : TEXCOORD1;
#if defined(_NORMALMAP)
    half4 tangentWS : TEXCOORD2;
#endif
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

NHNDepthNormalsVaryings NHNDepthNormalsVertex(NHNDepthAttributes input)
{
    NHNDepthNormalsVaryings output = (NHNDepthNormalsVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);
#if defined(_NORMALMAP) || defined(_ALPHATEST_ON) || defined(_DISSOLVE_ON)
    output.rawUV = input.uv;
#endif
    output.normalWS = normalInputs.normalWS;
#if defined(_NORMALMAP)
    output.tangentWS = half4(normalInputs.tangentWS, input.tangentOS.w * GetOddNegativeScale());
#endif
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    return output;
}

half4 NHNDepthNormalsFragment(NHNDepthNormalsVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
#if defined(_NORMALMAP)
    float2 surfaceUV;
#endif
    half baseAlpha = 1.0h;
#if defined(_ALPHATEST_ON)
#if defined(_NORMALMAP)
    baseAlpha = NHNSampleBase(input.rawUV, surfaceUV).a;
#else
    baseAlpha = NHNSampleBase(input.rawUV).a;
#endif
#elif defined(_NORMALMAP)
    surfaceUV = TRANSFORM_TEX(input.rawUV, _BaseMap);
#endif
#if defined(_ALPHATEST_ON) || defined(_DISSOLVE_ON)
    half dissolveEdge;
    NHNApplySurfaceClipping(input.rawUV, baseAlpha, dissolveEdge);
#endif
#if defined(_NORMALMAP)
    half3 normalTS = SampleNormal(surfaceUV, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);
    half3 bitangentWS = input.tangentWS.w * cross(input.normalWS, input.tangentWS.xyz);
    half3 normalWS = TransformTangentToWorld(normalTS,
        half3x3(input.tangentWS.xyz, bitangentWS, input.normalWS));
#else
    half3 normalWS = input.normalWS;
#endif
    normalWS = NormalizeNormalPerPixel(normalWS);
#if defined(_GBUFFER_NORMALS_OCT)
    float2 octNormal = PackNormalOctQuadEncode(normalWS);
    half3 packedNormal = PackFloat2To888(saturate(octNormal * 0.5 + 0.5));
    return half4(packedNormal, 0.0h);
#else
    return half4(normalWS, 0.0h);
#endif
}

#endif
