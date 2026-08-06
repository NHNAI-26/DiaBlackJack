using System;
using DG.Tweening;
using DiaBlackJack.Rendering;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    public sealed class ContractPaperClickable : MonoBehaviour
    {
        // The paper accelerates away for its entire travel (Ease.InQuad — slow start,
        // fast finish) rather than the deck's own Draw.anim timing (which front-loads
        // a near-static pause then a decelerating arc): that split made the paper's
        // alpha reach 0 at the same moment its position reached rest, so if the drop
        // distance didn't clear the camera's frame it visibly popped out mid-screen.
        // Alpha now only starts fading at FadeStartRatio and finishes exactly when the
        // move finishes, so it can never vanish before it has fully travelled away.
        private const float FadeStartRatio = 0.55f;
        private const float RotationKickDegrees = 10f;

        [SerializeField] private Collider paperCollider;
        [SerializeField] private SpriteRenderer paperRenderer;
        [SerializeField] private float disappearDuration = 0.55f;
        [SerializeField] private float disappearDropDistance = 4.5f;
        [Header("Hover outline")]
        [SerializeField] private bool useMaterialHoverOutlineSettings = true;
        [SerializeField] private Color hoverOutlineColor =
            new Color(1f, 0.72f, 0.08f, 1f);
        [SerializeField] private float hoverOutlineWidthPixels = 4f;

        private static readonly int StencilOutlineColorId =
            Shader.PropertyToID("_StencilOutlineColor");

        private Vector3 _baseLocalPosition;
        private Quaternion _baseLocalRotation;
        private bool _hasBaseLocalPosition;
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

        /// <summary>
        /// Drops the paper down and out of frame — the camera looks down at an
        /// angle here, so "down on screen" and "toward the player" are the same
        /// direction: -camera.transform.up. Falls back to world -Y if no camera is
        /// found. Alpha stays at 1 until the paper is mostly done travelling, so it
        /// cannot read as an abrupt pop before it has actually left the screen.
        /// </summary>
        public void PlayDisappearAnimation(Action onComplete)
        {
            EnsureBaseLocalPosition();
            EnsurePaperRenderer();
            _disappearTween?.Kill();

            Vector3 startPosition = transform.position;
            Vector3 dropOffset = ResolveDropDirection() * disappearDropDistance;
            Vector3 targetPosition = startPosition + dropOffset;

            if (disappearDuration <= 0f)
            {
                transform.position = targetPosition;
                if (paperRenderer != null)
                {
                    SetRendererAlpha(paperRenderer, 0f);
                }

                onComplete?.Invoke();
                return;
            }

            Sequence sequence = DOTween.Sequence();
            sequence.Join(transform
                .DOMove(targetPosition, disappearDuration)
                .SetEase(Ease.InQuad));
            sequence.Join(transform
                .DOLocalRotate(
                    new Vector3(0f, 0f, RotationKickDegrees),
                    disappearDuration,
                    RotateMode.LocalAxisAdd)
                .SetEase(Ease.InQuad));
            if (paperRenderer != null)
            {
                float fadeStartTime = disappearDuration * FadeStartRatio;
                float fadeDuration = disappearDuration - fadeStartTime;
                sequence.Insert(fadeStartTime, DOTween.To(
                        () => paperRenderer.color.a,
                        alpha => SetRendererAlpha(paperRenderer, alpha),
                        0f,
                        fadeDuration)
                    .SetEase(Ease.InQuad));
            }

            sequence.OnComplete(() =>
            {
                _disappearTween = null;
                onComplete?.Invoke();
            });
            _disappearTween = sequence;
        }

        /// <summary>Restores position/alpha for reuse once the stack is re-rendered from scratch.</summary>
        public void ResetVisualState()
        {
            _disappearTween?.Kill();
            _disappearTween = null;
            EnsureBaseLocalPosition();
            transform.localPosition = _baseLocalPosition;
            transform.localRotation = _baseLocalRotation;
            EnsurePaperRenderer();
            if (paperRenderer != null)
            {
                SetRendererAlpha(paperRenderer, 1f);
            }
        }

        private static Vector3 ResolveDropDirection()
        {
            Camera camera = Camera.main;
            return camera != null ? -camera.transform.up : Vector3.down;
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

        private void EnsureBaseLocalPosition()
        {
            if (_hasBaseLocalPosition)
            {
                return;
            }

            _baseLocalPosition = transform.localPosition;
            _baseLocalRotation = transform.localRotation;
            _hasBaseLocalPosition = true;
        }

        private static void SetRendererAlpha(SpriteRenderer renderer, float alpha)
        {
            Color color = renderer.color;
            color.a = alpha;
            renderer.color = color;
        }
    }
}
