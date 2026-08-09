using UnityEditor;
using UnityEngine;

namespace DiaBlackJack.Editor
{
    [InitializeOnLoad]
    internal static class ShaderVariantCollectionPreloader
    {
        private const string CollectionPath =
            "Assets/05. Arts/Shader/ShaderVariants.shadervariants";

        static ShaderVariantCollectionPreloader()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode)
            {
                return;
            }

            ShaderVariantCollection collection =
                AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(CollectionPath);
            if (collection == null)
            {
                Debug.LogWarning(
                    $"Shader variant collection was not found at '{CollectionPath}'.");
                return;
            }

            if (!collection.isWarmedUp)
            {
                collection.WarmUp();
            }
        }
    }
}
