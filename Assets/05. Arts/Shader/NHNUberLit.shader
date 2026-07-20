Shader "Shader/Uber Lit"
{
    Properties
    {
        [Title(Uber Lit)]
        [Main(Surface, _, on, off)] _SurfaceOptions("Surface", Float) = 1
        [KWEnum(Surface, Opaque, _, Transparent, _SURFACE_TYPE_TRANSPARENT)] _Surface("Surface Type", Float) = 0
        [KWEnum(Surface_SURFACE_TYPE_TRANSPARENT, Alpha, _, Premultiply, _ALPHAPREMULTIPLY_ON, Additive, _, Multiply, _ALPHAMODULATE_ON)] _Blend("Blend Mode", Float) = 0
        [SubToggle(Surface, _ALPHATEST_ON)] _AlphaClip("Alpha Clipping", Float) = 0
        [Sub(Surface_ALPHATEST_ON)] _Cutoff("Threshold", Range(0,1)) = 0.5
        [SubToggle(Surface, _)] _ReceiveShadows("Receive Shadows", Float) = 1
        [SubToggle(Surface, _)] _CastShadows("Cast Shadows", Float) = 1
        [KWEnum(Surface, Both, _, Front, _, Back, _)] _Cull("Render Face", Float) = 2
        [Sub(Surface)] _QueueOffset("Sorting Priority", Range(-50,50)) = 0

        [Main(SurfaceInputs, _, on, off)] _SurfaceInputs("Surface Inputs", Float) = 1
        [Tex(SurfaceInputs)] [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [Sub(SurfaceInputs)] [MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
        [Sub(SurfaceInputs)] _HueShift("Hue Shift", Range(-180,180)) = 0
        [Sub(SurfaceInputs)] _Saturation("Saturation", Range(0,2)) = 1
        [Sub(SurfaceInputs)] _Brightness("Brightness", Range(0,2)) = 1
        [Sub(SurfaceInputs)] _Contrast("Contrast", Range(0,2)) = 1
        [Sub(SurfaceInputs)] _Metallic("Metallic", Range(0,1)) = 0
        [SubToggle(SurfaceInputs, _METALLICMAP)] _MetallicMapEnabled("Metallic Map", Float) = 0
        [Tex(SurfaceInputs_METALLICMAP)] [NoScaleOffset] _MetallicMap("Metallic Map (R)", 2D) = "white" {}
        [Sub(SurfaceInputs)] _Smoothness("Smoothness", Range(0,1)) = 0.5
        [SubToggle(SurfaceInputs, _SMOOTHNESSMAP)] _SmoothnessMapEnabled("Smoothness Map", Float) = 0
        [Tex(SurfaceInputs_SMOOTHNESSMAP)] [NoScaleOffset] _SmoothnessMap("Smoothness Map (R)", 2D) = "white" {}
        [SubToggle(SurfaceInputs, _NORMALMAP)] _NormalMapEnabled("Normal Map", Float) = 0
        [Tex(SurfaceInputs_NORMALMAP, _BumpScale)] [Normal] [NoScaleOffset] _BumpMap("Normal Map", 2D) = "bump" {}
        [HideInInspector] _BumpScale("Normal Scale", Range(0,2)) = 1
        [SubToggle(SurfaceInputs, _OCCLUSIONMAP)] _OcclusionMapEnabled("Occlusion Map (G)", Float) = 0
        [Tex(SurfaceInputs_OCCLUSIONMAP, _OcclusionStrength)] [NoScaleOffset] _OcclusionMap("Occlusion", 2D) = "white" {}
        [HideInInspector] _OcclusionStrength("Occlusion Strength", Range(0,1)) = 1

        [Main(Emission, _EMISSION, on)] _EmissionEnabled("Emission", Float) = 0
        [Tex(Emission_EMISSION, _EmissionColor)] [NoScaleOffset] _EmissionMap("Emission Map", 2D) = "white" {}
        [HideInInspector] [HDR] _EmissionColor("Emission Color", Color) = (0,0,0,1)
        [Sub(Emission_EMISSION)] _EmissionIntensity("Intensity", Range(0,16)) = 1

        [Main(Rim, _RIM_ON, on)] _RimEnabled("Fresnel Rim", Float) = 0
        [Sub(Rim_RIM_ON)] [HDR] _RimColor("Color", Color) = (1,1,1,1)
        [Sub(Rim_RIM_ON)] _RimPower("Power", Range(0.1,16)) = 4
        [Sub(Rim_RIM_ON)] _RimIntensity("Intensity", Range(0,16)) = 1

        [Main(HeightFade, _HEIGHT_FADE_ON, on)] _HeightFadeEnabled("Height Fade", Float) = 0
        [Sub(HeightFade_HEIGHT_FADE_ON)] _HeightFadeLower("Lower Height", Float) = 0
        [Sub(HeightFade_HEIGHT_FADE_ON)] _HeightFadeUpper("Upper Height", Float) = 1
        [Sub(HeightFade_HEIGHT_FADE_ON)] _HeightFadeTint("Lower Tint", Color) = (0.25,0.25,0.25,1)

        [Main(GlassGlow, _GLASS_GLOW_ON, on)] _GlassGlowEnabled("Glowing Glass", Float) = 0
        [Sub(GlassGlow_GLASS_GLOW_ON)] [HDR] _GlassGlowColor("Glow Color", Color) = (1,1,1,1)
        [Sub(GlassGlow_GLASS_GLOW_ON)] _GlassGlowOffset("Offset", Range(0,1)) = 0

        [Main(Dissolve, _DISSOLVE_ON, on)] _DissolveEnabled("Dissolve", Float) = 0
        [Tex(Dissolve_DISSOLVE_ON)] [NoScaleOffset] _DissolveNoiseMap("Noise Map", 2D) = "white" {}
        [Sub(Dissolve_DISSOLVE_ON)] _DissolveTilingOffset("Tiling XY / Offset ZW", Vector) = (1, 1, 0, 0)
        [Sub(Dissolve_DISSOLVE_ON)] _DissolveAmount("Amount", Range(0,1)) = 0
        [Sub(Dissolve_DISSOLVE_ON)] _DissolveEdgeWidth("Edge Width", Range(0,1)) = 0.05
        [Sub(Dissolve_DISSOLVE_ON)] [HDR] _DissolveEdgeColor("Edge Color", Color) = (1,0.5,0,1)
        [Sub(Dissolve_DISSOLVE_ON)] _DissolveEdgeIntensity("Edge Intensity", Range(0,16)) = 1
        [Sub(Dissolve_DISSOLVE_ON)] _DissolvePanning("Panning XY", Vector) = (0,0,0,0)

        [HideInInspector] _SrcBlend("__src", Float) = 1
        [HideInInspector] _DstBlend("__dst", Float) = 0
        [HideInInspector] _SrcBlendAlpha("__srcA", Float) = 1
        [HideInInspector] _DstBlendAlpha("__dstA", Float) = 0
        [HideInInspector] _ZWrite("__zwrite", Float) = 1
        [HideInInspector] _BlendModePreserveSpecular("__preserveSpecular", Float) = 1
        [HideInInspector] _AlphaToMask("__alphaToMask", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "UniversalMaterialType"="Lit" "IgnoreProjector"="True" }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]
            ZWrite [_ZWrite]
            Cull [_Cull]
            AlphaToMask [_AlphaToMask]
            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex NHNUberLitVertex
            #pragma fragment NHNUberLitFragment
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _METALLICMAP
            #pragma shader_feature_local_fragment _SMOOTHNESSMAP
            #pragma shader_feature_local_fragment _OCCLUSIONMAP
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _RIM_ON
            #pragma shader_feature_local_fragment _DISSOLVE_ON
            #pragma multi_compile_local_fragment _ _HEIGHT_FADE_ON
            #pragma multi_compile_local_fragment _ _GLASS_GLOW_ON
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SURFACE_TYPE_TRANSPARENT
            #pragma shader_feature_local_fragment _ _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include "NHNUberLitForwardPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]
            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex NHNShadowVertex
            #pragma fragment NHNSilhouetteFragment
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local _DISSOLVE_ON
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include "NHNUberLitDepthPasses.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask R
            Cull [_Cull]
            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex NHNDepthOnlyVertex
            #pragma fragment NHNSilhouetteFragment
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local _DISSOLVE_ON
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include "NHNUberLitDepthPasses.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On
            Cull [_Cull]
            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex NHNDepthNormalsVertex
            #pragma fragment NHNDepthNormalsFragment
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local _DISSOLVE_ON
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include "NHNUberLitDepthPasses.hlsl"
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "NHNUberLitShaderGUI"
}
