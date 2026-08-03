Shader "Hidden/NHN/Post Process Outline Composite"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }

        Pass
        {
            Name "Composite"
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            TEXTURE2D(_NHNPostProcessOutlineMask);

            float4 _NHNPostProcessOutlineMask_TexelSize;
            half4 _NHNPostProcessOutlineColor;
            float _NHNPostProcessOutlineWidthPixels;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = float4(input.positionOS.xy, 0.0, 1.0);
                output.uv = input.uv;
                return output;
            }

            half SampleMask(float2 uv)
            {
                return SAMPLE_TEXTURE2D(
                    _NHNPostProcessOutlineMask,
                    sampler_PointClamp,
                    uv).r;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_LinearClamp, uv);
                half center = SampleMask(uv);
                float width = max(_NHNPostProcessOutlineWidthPixels, 1.0);
                float2 texel = _NHNPostProcessOutlineMask_TexelSize.xy * width;

                half neighbor = 0.0h;
                neighbor = max(neighbor, SampleMask(uv + float2(texel.x, 0.0)));
                neighbor = max(neighbor, SampleMask(uv - float2(texel.x, 0.0)));
                neighbor = max(neighbor, SampleMask(uv + float2(0.0, texel.y)));
                neighbor = max(neighbor, SampleMask(uv - float2(0.0, texel.y)));
                neighbor = max(neighbor, SampleMask(uv + texel));
                neighbor = max(neighbor, SampleMask(uv - texel));
                neighbor = max(neighbor, SampleMask(uv + float2(texel.x, -texel.y)));
                neighbor = max(neighbor, SampleMask(uv + float2(-texel.x, texel.y)));

                half outline = saturate(neighbor - center);
                color.rgb = lerp(
                    color.rgb,
                    _NHNPostProcessOutlineColor.rgb,
                    outline * _NHNPostProcessOutlineColor.a);
                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "Copy"
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = float4(input.positionOS.xy, 0.0, 1.0);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_LinearClamp, input.uv);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
