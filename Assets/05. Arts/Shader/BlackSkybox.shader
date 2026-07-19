Shader "NHNA/Black Skybox"
{
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = UnityObjectToClipPos(input.positionOS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return half4(0.0, 0.0, 0.0, 1.0);
            }
            ENDHLSL
        }
    }
}
