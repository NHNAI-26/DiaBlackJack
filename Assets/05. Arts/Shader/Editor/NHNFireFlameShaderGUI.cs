using UnityEditor;
using UnityEngine;

public sealed class NHNFireFlameShaderGUI : ShaderGUI
{
    private const int DefaultRenderQueue = 3100;

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        materialEditor.PropertiesDefaultGUI(properties);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Advanced", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        int renderQueue = EditorGUILayout.IntField("Render Queue", GetRenderQueue(materialEditor.targets));
        if (EditorGUI.EndChangeCheck())
        {
            foreach (Object target in materialEditor.targets)
            {
                if (!(target is Material material))
                    continue;

                material.renderQueue = renderQueue;
                EditorUtility.SetDirty(material);
            }
        }
    }

    private static int GetRenderQueue(Object[] targets)
    {
        foreach (Object target in targets)
        {
            if (target is Material material)
                return material.renderQueue >= 0 ? material.renderQueue : DefaultRenderQueue;
        }

        return DefaultRenderQueue;
    }
}
