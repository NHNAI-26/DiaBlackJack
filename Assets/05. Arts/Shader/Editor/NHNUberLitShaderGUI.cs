using UnityEditor;
using UnityEngine;

public class NHNUberLitShaderGUI : LWGUI.LWGUI
{
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        SeedKeyword(properties, "_Surface", "_SURFACE_TYPE_TRANSPARENT");
        SeedKeyword(properties, "_LightingMode", "_UNLIT_ON");
        SeedKeyword(properties, "_EmissionEnabled", "_EMISSION");
        SeedKeyword(properties, "_RimEnabled", "_RIM_ON");
        SeedKeyword(properties, "_HeightFadeEnabled", "_HEIGHT_FADE_ON");
        SeedKeyword(properties, "_GlassGlowEnabled", "_GLASS_GLOW_ON");
        SeedKeyword(properties, "_DissolveEnabled", "_DISSOLVE_ON");

        base.OnGUI(materialEditor, properties);

        foreach (Object target in materialEditor.targets)
        {
            if (!(target is Material material))
                continue;

            bool usesDirectRenderQueue = material.HasProperty("_BaseSpriteUVRect");
            int renderQueue = material.rawRenderQueue;

            // Sprite/UI transparency must fade the complete source color. URP's preserve-specular
            // mode switches Alpha blending to Src=One and relies on the lit BRDF to premultiply only
            // its diffuse term, which leaves custom sprite output visible even when alpha reaches 0.
            if (usesDirectRenderQueue && material.HasProperty("_BlendModePreserveSpecular"))
                material.SetFloat("_BlendModePreserveSpecular", 0.0f);

            UnityEditor.BaseShaderGUI.SetMaterialKeywords(material);

            if (usesDirectRenderQueue)
                material.renderQueue = renderQueue;

            RestoreKeyword(material, "_LightingMode", "_UNLIT_ON");
            RestoreKeyword(material, "_NormalMapEnabled", "_NORMALMAP");
            RestoreKeyword(material, "_EmissionEnabled", "_EMISSION");
        }
    }

    private static void SeedKeyword(MaterialProperty[] properties, string propertyName, string keyword)
    {
        foreach (MaterialProperty property in properties)
        {
            if (property.name != propertyName)
                continue;

            LWGUI.GUIData.keyWord[keyword] = property.floatValue > 0.5f;
            return;
        }
    }

    private static void RestoreKeyword(Material material, string propertyName, string keyword)
    {
        bool enabled = material.HasProperty(propertyName) && material.GetFloat(propertyName) > 0.5f;
        if (enabled)
            material.EnableKeyword(keyword);
        else
            material.DisableKeyword(keyword);
    }
}
