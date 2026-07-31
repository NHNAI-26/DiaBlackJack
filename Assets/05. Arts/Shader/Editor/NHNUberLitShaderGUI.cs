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
        SeedKeyword(properties, "_PixelOutlineEnabled", "_PIXEL_OUTLINE_ON");
        SeedEnumKeyword(properties, "_UVAlphaFadeAxis", "_UV_ALPHA_FADE_U", "_UV_ALPHA_FADE_V");
        SeedKeyword(properties, "_HeightFadeEnabled", "_HEIGHT_FADE_ON");
        SeedKeyword(properties, "_GlassGlowEnabled", "_GLASS_GLOW_ON");
        SeedKeyword(properties, "_DissolveEnabled", "_DISSOLVE_ON");
        SeedEnumKeyword(properties, "_DissolveMode", "_DISSOLVE_RADIAL", null);
        SeedEnumKeyword(properties, "_DissolveSpace", "_DISSOLVE_OBJECT_SPACE", null);

        base.OnGUI(materialEditor, properties);

        foreach (Object target in materialEditor.targets)
        {
            if (!(target is Material material))
                continue;

            bool usesDirectRenderQueue = material.HasProperty("_BaseSpriteUVRect");
            int renderQueue = material.rawRenderQueue;
            bool preserveExplicitRenderQueue = usesDirectRenderQueue && renderQueue >= 0;

            // Sprite/UI transparency must fade the complete source color. URP's preserve-specular
            // mode switches Alpha blending to Src=One and relies on the lit BRDF to premultiply only
            // its diffuse term, which leaves custom sprite output visible even when alpha reaches 0.
            if (usesDirectRenderQueue && material.HasProperty("_BlendModePreserveSpecular"))
                material.SetFloat("_BlendModePreserveSpecular", 0.0f);

            UnityEditor.BaseShaderGUI.SetMaterialKeywords(material);

            if (preserveExplicitRenderQueue)
                material.renderQueue = renderQueue;

            RestoreKeyword(material, "_LightingMode", "_UNLIT_ON");
            RestoreKeyword(material, "_NormalMapEnabled", "_NORMALMAP");
            RestoreKeyword(material, "_EmissionEnabled", "_EMISSION");
            RestoreKeyword(material, "_PixelOutlineEnabled", "_PIXEL_OUTLINE_ON");
            RestoreEnumKeyword(material, "_UVAlphaFadeAxis", "_UV_ALPHA_FADE_U", "_UV_ALPHA_FADE_V");
            RestoreEnumKeyword(material, "_DissolveMode", "_DISSOLVE_RADIAL", null);
            RestoreEnumKeyword(material, "_DissolveSpace", "_DISSOLVE_OBJECT_SPACE", null);
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

    private static void SeedEnumKeyword(MaterialProperty[] properties, string propertyName,
        string firstKeyword, string secondKeyword)
    {
        foreach (MaterialProperty property in properties)
        {
            if (property.name != propertyName)
                continue;

            int selected = Mathf.RoundToInt(property.floatValue);
            LWGUI.GUIData.keyWord[firstKeyword] = selected == 1;
            if (!string.IsNullOrEmpty(secondKeyword))
                LWGUI.GUIData.keyWord[secondKeyword] = selected == 2;
            return;
        }
    }

    private static void RestoreEnumKeyword(Material material, string propertyName,
        string firstKeyword, string secondKeyword)
    {
        int selected = material.HasProperty(propertyName)
            ? Mathf.RoundToInt(material.GetFloat(propertyName))
            : 0;

        if (selected == 1)
            material.EnableKeyword(firstKeyword);
        else
            material.DisableKeyword(firstKeyword);

        if (selected == 2)
        {
            if (!string.IsNullOrEmpty(secondKeyword))
                material.EnableKeyword(secondKeyword);
        }
        else
        {
            if (!string.IsNullOrEmpty(secondKeyword))
                material.DisableKeyword(secondKeyword);
        }
    }
}
