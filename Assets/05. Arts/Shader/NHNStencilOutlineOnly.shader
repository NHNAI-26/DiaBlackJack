Shader "Hidden/NHN/Stencil Outline Only"
{
    Properties
    {
        [Toggle(_STENCIL_OUTLINE_ON)] _StencilOutlineEnabled("Stencil Outline", Float) = 0
        [HDR] _StencilOutlineColor("Outline Color", Color) = (1,0.72,0.08,1)
        _StencilOutlineWidth("Outline Width", Range(0,0.2)) = 0.025
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }

        Pass
        {
            Name "StencilOutline"
            Tags { "LightMode"="UniversalForwardOnly" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
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

            CBUFFER_START(UnityPerMaterial)
                half4 _StencilOutlineColor;
                half _StencilOutlineEnabled;
                half _StencilOutlineWidth;
            CBUFFER_END

            #include "NHNStencilOutlinePass.hlsl"
            ENDHLSL
        }
    }

    FallBack Off
}
