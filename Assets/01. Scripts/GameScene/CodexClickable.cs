using DiaBlackJack.Rendering;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    public sealed class CodexClickable : MonoBehaviour
    {
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private bool useMaterialHoverOutlineSettings = true;
        [SerializeField] private Color hoverOutlineColor =
            new Color(1f, 0.72f, 0.08f, 1f);
        [SerializeField] private float hoverOutlineWidthPixels = 4f;

        private static readonly int StencilOutlineColorId =
            Shader.PropertyToID("_StencilOutlineColor");

        private bool _isHovered;

        private void Awake()
        {
            AutoBindMissingReferences();
        }

        private void Reset()
        {
            AutoBindMissingReferences();
        }

        private void OnValidate()
        {
            hoverOutlineWidthPixels = Mathf.Max(0f, hoverOutlineWidthPixels);
            AutoBindMissingReferences();
        }

        internal void SetHovered(bool hovered)
        {
            if (_isHovered == hovered)
            {
                return;
            }

            _isHovered = hovered;
            ApplyHoverOutline(hovered);
        }

        private void OnDisable()
        {
            _isHovered = false;
            ApplyHoverOutline(visible: false);
        }

        private void OnDestroy()
        {
            ApplyHoverOutline(visible: false);
        }

        private void ApplyHoverOutline(bool visible)
        {
            AutoBindMissingReferences();
            if (renderers == null)
            {
                return;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                if (!visible)
                {
                    PostProcessOutlineRegistry.Unregister(renderer);
                    continue;
                }

                PostProcessOutlineRegistry.Register(
                    renderer,
                    ResolveOutlineColor(renderer.sharedMaterial),
                    hoverOutlineWidthPixels);
            }
        }

        private Color ResolveOutlineColor(Material material)
        {
            if (useMaterialHoverOutlineSettings &&
                material != null &&
                material.HasProperty(StencilOutlineColorId))
            {
                Color color = material.GetColor(StencilOutlineColorId);
                if (color.a <= 0f)
                {
                    color.a = 1f;
                }

                return color;
            }

            return hoverOutlineColor;
        }

        private void AutoBindMissingReferences()
        {
            if (renderers == null || renderers.Length == 0)
            {
                renderers = GetComponentsInChildren<Renderer>(
                    includeInactive: true);
            }
        }
    }
}
