#ifndef NHN_STENCIL_OUTLINE_PASS_INCLUDED
#define NHN_STENCIL_OUTLINE_PASS_INCLUDED

struct NHNStencilOutlineAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct NHNStencilOutlineVaryings
{
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

NHNStencilOutlineVaryings NHNStencilOutlineVertex(
    NHNStencilOutlineAttributes input)
{
    NHNStencilOutlineVaryings output = (NHNStencilOutlineVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    float3 normalWS = normalize(TransformObjectToWorldNormal(input.normalOS));
    float4 positionCS = TransformWorldToHClip(positionWS);
    float4 normalPositionCS = TransformWorldToHClip(positionWS + normalWS);

    float2 positionNDC = positionCS.xy / max(abs(positionCS.w), 0.000001);
    float2 normalNDC =
        normalPositionCS.xy / max(abs(normalPositionCS.w), 0.000001);
    float2 outlineDirection = normalNDC - positionNDC;
    outlineDirection.x *= _ScreenParams.x / _ScreenParams.y;
    float outlineDirectionLengthSq = dot(outlineDirection, outlineDirection);
    outlineDirection = outlineDirectionLengthSq > 0.00000001
        ? outlineDirection * rsqrt(outlineDirectionLengthSq)
        : 0.0;
    outlineDirection.x *= _ScreenParams.y / _ScreenParams.x;

    positionCS.xy += outlineDirection * max(_StencilOutlineWidth, 0.0h) *
        positionCS.w;
    output.positionCS = positionCS;
    return output;
}

half4 NHNStencilOutlineFragment(NHNStencilOutlineVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

#if !defined(_STENCIL_OUTLINE_ON)
    clip(-1.0h);
#endif
    clip(_StencilOutlineEnabled - 0.5h);

    return _StencilOutlineColor;
}

#endif
