using System;
using DG.Tweening;
using DiaBlackJack.Rendering;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    public sealed class ContractPaperClickable : MonoBehaviour
    {
        [SerializeField] private Collider paperCollider;
        [SerializeField] private SpriteRenderer paperRenderer;
        [SerializeField] private float disappearDuration = 0.35f;
        [Header("Hover outline")]
        [SerializeField] private bool useMaterialHoverOutlineSettings = true;
        [SerializeField] private Color hoverOutlineColor =
            new Color(1f, 0.72f, 0.08f, 1f);
        [SerializeField] private float hoverOutlineWidthPixels = 4f;

        private static readonly int StencilOutlineColorId =
            Shader.PropertyToID("_StencilOutlineColor");

        private Tween _disappearTween;
        private bool _isHovered;

        public bool IsInteractable { get; private set; }

        /// <summary>
        /// The paper's actual draw-order signal (<see cref="SpriteRenderer.sortingOrder"/>)
        /// — a higher value renders in front. <see cref="ContractPaperView"/> uses this,
        /// not the GameObject's name, to decide which paper is visually "on top" of the
        /// stack and therefore the one that should be interactable.
        /// </summary>
        internal int SortingOrder
        {
            get
            {
                EnsurePaperRenderer();
                return paperRenderer != null ? paperRenderer.sortingOrder : 0;
            }
        }

        internal void SetInteractable(bool interactable)
        {
            IsInteractable = interactable;

            // Disabling the collider — not just the flag — means a non-interactable
            // paper (the decorative one underneath) physically can't be raycast-hit at
            // all, so it can't trigger hover or click through any pointer-processing
            // path, present or future.
            EnsurePaperCollider();
            if (paperCollider != null)
            {
                paperCollider.enabled = interactable;
            }
        }

        internal void ConfigureAvailableDemonNames(string availableDemonNames)
        {
            HoverDescriptionTarget descriptionTarget =
                GetComponent<HoverDescriptionTarget>();
            descriptionTarget?.Configure(
                null,
                new HoverDescriptionValue(
                    "availableDemons",
                    availableDemonNames));
        }

        /// <summary>
        /// Same stencil-outline highlight used for the drawable deck
        /// (<see cref="DeckStackView"/>) and the codex (<see cref="CodexClickable"/>),
        /// registered through the shared <see cref="PostProcessOutlineRegistry"/>.
        /// </summary>
        internal void SetHovered(bool hovered)
        {
            if (_isHovered == hovered)
            {
                return;
            }

            _isHovered = hovered;
            ApplyHoverOutline(hovered);
        }

        private void ApplyHoverOutline(bool visible)
        {
            EnsurePaperRenderer();
            if (paperRenderer == null)
            {
                return;
            }

            if (!visible)
            {
                PostProcessOutlineRegistry.Unregister(paperRenderer);
                return;
            }

            PostProcessOutlineRegistry.Register(
                paperRenderer,
                ResolveOutlineColor(paperRenderer.sharedMaterial),
                hoverOutlineWidthPixels);
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

        /// <summary>Fades the paper out in place.</summary>
        public void PlayDisappearAnimation(Action onComplete)
        {
            EnsurePaperRenderer();
            _disappearTween?.Kill();

            if (paperRenderer == null || disappearDuration <= 0f)
            {
                if (paperRenderer != null)
                {
                    SetRendererAlpha(paperRenderer, 0f);
                }

                onComplete?.Invoke();
                return;
            }

            _disappearTween = DOTween.To(
                    () => paperRenderer.color.a,
                    alpha => SetRendererAlpha(paperRenderer, alpha),
                    0f,
                    disappearDuration)
                .OnComplete(() =>
                {
                    _disappearTween = null;
                    onComplete?.Invoke();
                });
        }

        /// <summary>Restores alpha for reuse once the stack is re-rendered from scratch.</summary>
        public void ResetVisualState()
        {
            _disappearTween?.Kill();
            _disappearTween = null;
            EnsurePaperRenderer();
            if (paperRenderer != null)
            {
                SetRendererAlpha(paperRenderer, 1f);
            }
        }

        private void Awake()
        {
            EnsurePaperCollider();
        }

        private void OnDisable()
        {
            _disappearTween?.Kill();
            _disappearTween = null;
            _isHovered = false;
            ApplyHoverOutline(visible: false);
        }

        private void OnDestroy()
        {
            ApplyHoverOutline(visible: false);
        }

        private void OnValidate()
        {
            hoverOutlineWidthPixels = Mathf.Max(0f, hoverOutlineWidthPixels);
            EnsurePaperCollider();
            EnsurePaperRenderer();
        }

        private void EnsurePaperCollider()
        {
            paperCollider ??= GetComponent<Collider>();
        }

        private void EnsurePaperRenderer()
        {
            paperRenderer ??= GetComponent<SpriteRenderer>();
        }

        private static void SetRendererAlpha(SpriteRenderer renderer, float alpha)
        {
            Color color = renderer.color;
            color.a = alpha;
            renderer.color = color;
        }
    }
}
