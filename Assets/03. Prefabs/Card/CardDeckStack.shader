Shader "DiaBlackJack/CardDeckStack"
{
    Properties
    {
        [MainTexture] _TopTex ("Top Card Sprite", 2D) = "white" {}
        [MainColor] [HDR] _TopColor ("Top Tint", Color) = (1, 1, 1, 1)
        _SideColor ("Side Card Body", Color) = (0.77, 0.72, 0.62, 1)
        _LayerLineColor ("Side Layer Lines", Color) = (0.18, 0.13, 0.1, 1)
        _BottomColor ("Bottom Color", Color) = (0.12, 0.09, 0.08, 1)
        _LayerHeight ("Layer Height", Range(0.005, 0.08)) = 0.035
        _LayerLineWidth ("Layer Line Width", Range(0.02, 0.45)) = 0.16
        _TopNormalThreshold ("Top Normal Threshold", Range(0.1, 0.95)) = 0.65
        _SideShade ("Side Shade", Range(0, 1)) = 0.82
        _BottomShade ("Bottom Shade", Range(0, 1)) = 0.45
        _MinimumLight ("Minimum Light", Range(0, 1)) = 0.22
        _LightInfluence ("Light Influence", Range(0, 1)) = 0.65
        _LightColorInfluence ("Light Color Influence", Range(0, 1)) = 0.45
        _HeightFadeColor ("Height Fade Color", Color) = (0.08, 0.055, 0.035, 1)
        _HeightFadeStrength ("Height Fade Strength", Range(0, 1)) = 0.45
        _HeightFadeLower ("Height Fade Lower", Range(0, 1)) = 0.08
        _HeightFadeUpper ("Height Fade Upper", Range(0, 2)) = 0.95
        [Toggle(_STENCIL_OUTLINE_ON)] _StencilOutlineEnabled ("Stencil Outline", Float) = 0
        [HDR] _StencilOutlineColor ("Outline Color", Color) = (1, 0.72, 0.08, 1)
        _StencilOutlineWidth ("Outline Width", Range(0, 0.2)) = 0.025
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
                WriteMask [_StencilOutlineEnabled]
            }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 normalOS : TEXCOORD2;
                float2 uv : TEXCOORD3;
                float3 positionOS : TEXCOORD4;
            };

            TEXTURE2D(_TopTex);
            SAMPLER(sampler_TopTex);

            CBUFFER_START(UnityPerMaterial)
            float4 _TopTex_ST;
            half4 _TopColor;
            half4 _SideColor;
            half4 _LayerLineColor;
            half4 _BottomColor;
                half _LayerHeight;
                half _LayerLineWidth;
                half _TopNormalThreshold;
                half _SideShade;
                half _BottomShade;
                half _MinimumLight;
                half _LightInfluence;
                half _LightColorInfluence;
                half4 _HeightFadeColor;
                half _HeightFadeStrength;
                half _HeightFadeLower;
                half _HeightFadeUpper;
                half _StencilOutlineEnabled;
                half4 _StencilOutlineColor;
                half _StencilOutlineWidth;
                CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(input.positionOS.xyz);
                output.positionHCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.normalOS = normalize(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _TopTex);
                output.positionOS = input.positionOS.xyz;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 normalOS = normalize(input.normalOS);
                half topMask = smoothstep(
                    _TopNormalThreshold,
                    1.0h,
                    normalOS.y);
                half bottomMask = smoothstep(
                    _TopNormalThreshold,
                    1.0h,
                    -normalOS.y);
                half sideMask = saturate(1.0h - topMask - bottomMask);

                half4 topColor =
                    SAMPLE_TEXTURE2D(_TopTex, sampler_TopTex, input.uv) *
                    _TopColor;

                half layerHeight = max(_LayerHeight, 0.0001h);
                half layerPhase = frac(input.positionWS.y / layerHeight);
                half layerLine =
                    1.0h - smoothstep(
                        _LayerLineWidth,
                        _LayerLineWidth + 0.035h,
                        min(layerPhase, 1.0h - layerPhase));
                half4 sideColor = lerp(_SideColor, _LayerLineColor, layerLine);
                half bottomEdgePhase = frac((input.positionOS.x + input.positionOS.z) * 14.0h);
                half bottomEdgeLine =
                    1.0h - smoothstep(
                        0.12h,
                        0.18h,
                        min(bottomEdgePhase, 1.0h - bottomEdgePhase));
                half4 bottomColor = lerp(_BottomColor, _LayerLineColor, bottomEdgeLine);

                half4 surfaceColor =
                    topColor * topMask +
                    half4(sideColor.rgb * _SideShade, sideColor.a) * sideMask +
                    half4(bottomColor.rgb * _BottomShade, bottomColor.a) * bottomMask;

                half heightSpan = max(_HeightFadeUpper - _HeightFadeLower, 0.0001h);
                half height01 = input.positionOS.y + 0.5h;
                half heightFade = 1.0h -
                    saturate((height01 - _HeightFadeLower) / heightSpan);
                half fadeMask = saturate(sideMask + bottomMask) *
                    heightFade *
                    _HeightFadeStrength;
                surfaceColor.rgb = lerp(
                    surfaceColor.rgb,
                    _HeightFadeColor.rgb,
                    fadeMask);

                Light mainLight = GetMainLight();
                half ndotl = saturate(dot(normalize(input.normalWS), mainLight.direction));
                half lightAmount = max(_MinimumLight, ndotl);
                half3 lightTint = lerp(1.0h.xxx, mainLight.color, _LightColorInfluence);
                half3 litColor = surfaceColor.rgb *
                    lerp(1.0h.xxx, lightAmount * lightTint, _LightInfluence);
                return half4(litColor, surfaceColor.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "StencilOutline"
            Tags { "LightMode" = "UniversalForwardOnly" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Front
            Stencil
            {
                Ref 1
                ReadMask 1
                Comp NotEqual
                Pass Keep
            }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex NHNStencilOutlineVertex
            #pragma fragment NHNStencilOutlineFragment
            #pragma shader_feature_local _STENCIL_OUTLINE_ON
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_TopTex);
            SAMPLER(sampler_TopTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _TopTex_ST;
                half4 _TopColor;
                half4 _SideColor;
                half4 _LayerLineColor;
                half4 _BottomColor;
                half _LayerHeight;
                half _LayerLineWidth;
                half _TopNormalThreshold;
                half _SideShade;
                half _BottomShade;
                half _MinimumLight;
                half _LightInfluence;
                half _LightColorInfluence;
                half4 _HeightFadeColor;
                half _HeightFadeStrength;
                half _HeightFadeLower;
                half _HeightFadeUpper;
                half _StencilOutlineEnabled;
                half4 _StencilOutlineColor;
                half _StencilOutlineWidth;
            CBUFFER_END

            #include "Assets/05. Arts/Shader/NHNStencilOutlinePass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
