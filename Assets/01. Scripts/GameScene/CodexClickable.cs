using Border.Audio;
using DG.Tweening;
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
        [Header("Hover feel")]
        [SerializeField] private Vector3 punchLocation =
            new Vector3(0f, 0.025f, 0f);
        [SerializeField] private Vector3 punchRotation =
            new Vector3(0f, 0f, 2f);
        [SerializeField, Min(0.01f)] private float punchDuration = 0.35f;
        [SerializeField, Min(1)] private int punchVibrato = 3;
        [SerializeField, Range(0f, 1f)] private float punchElasticity = 0.7f;
        [SerializeField] private string hoverSfxId = "bookDrop";

        private static readonly int StencilOutlineColorId =
            Shader.PropertyToID("_StencilOutlineColor");

        private bool _isHovered;
        private Tween _hoverPunchTween;

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
            if (hovered)
            {
                PlayHoverFeedback();
            }
        }

        internal void PlayPunchEffect()
        {
            if (!Application.isPlaying || !gameObject.activeInHierarchy)
            {
                return;
            }

            StopHoverPunch();
            _hoverPunchTween = DOTween.Sequence()
                .Append(transform.DOPunchPosition(
                    punchLocation,
                    Mathf.Max(punchDuration, 0.01f),
                    Mathf.Max(punchVibrato, 1),
                    Mathf.Clamp01(punchElasticity)))
                .Join(transform.DOPunchRotation(
                    punchRotation,
                    Mathf.Max(punchDuration, 0.01f),
                    Mathf.Max(punchVibrato, 1),
                    Mathf.Clamp01(punchElasticity)))
                .SetTarget(this);
        }

        private void OnDisable()
        {
            StopHoverPunch();
            _isHovered = false;
            ApplyHoverOutline(visible: false);
        }

        private void OnDestroy()
        {
            StopHoverPunch();
            ApplyHoverOutline(visible: false);
        }

        private void PlayHoverFeedback()
        {
            if (!Application.isPlaying || !gameObject.activeInHierarchy)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(hoverSfxId))
            {
                SoundManager.Current?.PlaySfx(hoverSfxId);
            }

            PlayPunchEffect();
        }

        private void StopHoverPunch()
        {
            if (_hoverPunchTween == null)
            {
                return;
            }

            _hoverPunchTween.Kill(complete: true);
            _hoverPunchTween = null;
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
