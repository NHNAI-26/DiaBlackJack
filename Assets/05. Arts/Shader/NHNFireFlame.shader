Shader "Shader/Fire Flame"
{
    Properties
    {
        [Header(Shape)]
        _GradientBottom("Gradient Bottom", Float) = 0
        _GradientTop("Gradient Top", Float) = 1
        _BlueHeight("Blue Height", Range(0, 1)) = 0.28
        _CoreUVCenter("Core UV Center", Range(0, 1)) = 0.5
        _CoreWidth("Core Width", Range(0.01, 1)) = 0.42
        _CorePower("Core Power", Range(0.1, 8)) = 2.4

        [Header(Color)]
        [HDR] _CoreColor("Core Color", Color) = (4, 3.65, 2.8, 1)
        [HDR] _FresnelColor("Fresnel Color", Color) = (3.0, 0.78, 0.08, 1)
        [HDR] _BlueColor("Blue Color", Color) = (0.2, 1.1, 3.0, 1)
        [HDR] _OrangeColor("Orange Color", Color) = (2.7, 0.8, 0.12, 1)

        [Header(Fresnel)]
        _FresnelPower("Fresnel Power", Range(0.1, 8)) = 2.2
        _FresnelIntensity("Fresnel Intensity", Range(0, 8)) = 1.8

        [Header(Transparency)]
        _Alpha("Alpha", Range(0, 1)) = 0.58
        _EdgeAlpha("Edge Alpha", Range(0, 1)) = 0.28
        _CoreAlpha("Core Alpha", Range(0, 1)) = 0.36

        [Header(Ignition Reveal)]
        _IgnitionReveal("Ignition Reveal", Range(0, 1)) = 0
        _IgnitionRevealHeight("Reveal Height", Range(0, 1)) = 1
        _IgnitionRevealFeather("Reveal Feather", Range(0.001, 1)) = 0.18
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent+100"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex FireFlameVertex
            #pragma fragment FireFlameFragment
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
                half fogFactor : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float _GradientBottom;
                float _GradientTop;
                float _BlueHeight;
                float _CoreUVCenter;
                float _CoreWidth;
                float _CorePower;
                float4 _CoreColor;
                float4 _FresnelColor;
                float4 _BlueColor;
                float4 _OrangeColor;
                float _FresnelPower;
                float _FresnelIntensity;
                float _Alpha;
                float _EdgeAlpha;
                float _CoreAlpha;
                float _IgnitionReveal;
                float _IgnitionRevealHeight;
                float _IgnitionRevealFeather;
            CBUFFER_END

            Varyings FireFlameVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = positionInputs.positionCS;
                output.positionOS = input.positionOS.xyz;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                return output;
            }

            half4 FireFlameFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float heightRange = max(0.0001, _GradientTop - _GradientBottom);
                float height = saturate((input.positionOS.y - _GradientBottom) / heightRange);
                float blueMask = 1.0 - smoothstep(0.0, max(0.0001, _BlueHeight), height);

                float horizontalCore = saturate(1.0 - abs(input.uv.x - _CoreUVCenter) / max(0.0001, _CoreWidth));
                float verticalCore = smoothstep(0.05, 0.35, height) * (1.0 - smoothstep(0.72, 1.0, height));
                float coreMask = pow(horizontalCore * verticalCore, _CorePower);

                half3 normalWS = NormalizeNormalPerPixel(input.normalWS);
                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float fresnel = pow(saturate(1.0 - dot(normalWS, viewDirWS)), _FresnelPower);

                half3 gradientColor = lerp(_OrangeColor.rgb, _BlueColor.rgb, blueMask);
                half3 color = lerp(gradientColor, _CoreColor.rgb, coreMask);
                color += _FresnelColor.rgb * fresnel * _FresnelIntensity;
                color = MixFog(color, input.fogFactor);

                float alpha = saturate(_Alpha + fresnel * _EdgeAlpha + coreMask * _CoreAlpha);
                float revealFade = 1.0 - smoothstep(
                    _IgnitionRevealHeight,
                    _IgnitionRevealHeight + max(0.0001, _IgnitionRevealFeather),
                    height);
                alpha *= lerp(1.0, revealFade, saturate(_IgnitionReveal));
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "NHNFireFlameShaderGUI"
}
