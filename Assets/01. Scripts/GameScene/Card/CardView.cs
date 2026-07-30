using DG.Tweening;
using TMPro;
using Border.Audio;
using DiaBlackJack.Content;
using DiaBlackJack.CoreLoop;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    /// <summary>
    /// Component on the card prefab root. <see cref="front"/> / <see cref="back"/> toggle by whether
    /// the viewer may see the rank; the face-up sprite is swapped by rank. Hover feedback is driven
    /// by <see cref="SetHovered"/> (called from
    /// <c>GameManager</c>'s pointer raycast): any hovered card scales up,
    /// and a card whose public model enables hover information exposes its label and screen position
    /// to the shared HUD badge. Usability is orientation-independent — a face-down player card can be
    /// usable.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CardView : MonoBehaviour
    {
        [SerializeField] private GameObject front;
        [SerializeField] private GameObject back;
        [SerializeField] private TMP_Text rankText;
        [SerializeField] private CardContentCatalogSO cardContentCatalog;

        [Header("Hover badge anchor")]
        [Tooltip("World-space anchor projected to the HUD while this card is hovered.")]
        [SerializeField] private Transform topPosition;
        [Tooltip("World-space anchor projected to the HUD when this card's tooltip extends below it.")]
        [SerializeField] private Transform bottomPosition;

        [Header("Hover feel")]
        [SerializeField] private float hoverScale = 1.15f;
        [Min(0.01f)]
        [SerializeField] private float hoverScaleDuration = 0.08f;
        [SerializeField] private AnimationCurve hoverScaleCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private string hoverSfxId = "cardHover";

        [Header("Hover outline")]
        [SerializeField] private bool useMaterialHoverOutlineSettings = true;
        [ColorUsage(true, true)]
        [SerializeField] private Color hoverOutlineColor = new Color(1f, 0.685f, 0f, 1f);
        [SerializeField] private float hoverOutlineWidth = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float hoverOutlineAlphaThreshold = 0.5f;

        [Header("Player hidden card")]
        [Tooltip("Blends the real face over the card back while only the player may see its rank.")]
        [Range(0f, 1f)]
        [SerializeField] private float hiddenCardBlendAmount = 0.5f;

        private static readonly int BaseSpriteUvRectId = Shader.PropertyToID("_BaseSpriteUVRect");
        private static readonly int CardBlendTextureId = Shader.PropertyToID("_CardBlendTex");
        private static readonly int CardBlendAmountId = Shader.PropertyToID("_CardBlendAmount");
        private static readonly int CardBlendUvRectId = Shader.PropertyToID("_CardBlendUVRect");
        private static readonly int PixelOutlineColorId = Shader.PropertyToID("_PixelOutlineColor");
        private static readonly int PixelOutlineWidthId = Shader.PropertyToID("_PixelOutlineWidth");
        private static readonly int PixelOutlineAlphaThresholdId = Shader.PropertyToID("_PixelOutlineAlphaThreshold");
        private static readonly int PixelOutlineVisibilityId = Shader.PropertyToID("_PixelOutlineVisibility");
        private const string PixelOutlineKeyword = "_PIXEL_OUTLINE_ON";

        private MaterialPropertyBlock _frontPropertyBlock;
        private MaterialPropertyBlock _backPropertyBlock;
        private SpriteRenderer _frontSpriteRenderer;
        private SpriteRenderer _backSpriteRenderer;
        private Renderer _backRenderer;
        private Sprite _frontUvSprite;
        private Sprite _backUvSprite;
        private Tween _scaleTween;
        private Vector3 _baseScale = Vector3.one;
        private bool _showingFrontFace = true;
        private bool _usesHoverCardBlend;
        private bool _showBadgeOnHover;
        private bool _hovered;

        /// <summary>Run card id of the bound card, for pointer routing. -1 when unbound.</summary>
        public int CardId { get; private set; } = -1;

        /// <summary>Whether this card's manual effect can be activated right now (player, usable only).</summary>
        public bool CanUse { get; private set; }

        /// <summary>Title displayed by the shared HUD tooltip while this card is hovered.</summary>
        public string HoverBadgeTitle { get; private set; } = string.Empty;

        /// <summary>Description displayed beneath the shared HUD tooltip title.</summary>
        public string HoverBadgeDescription { get; private set; } = string.Empty;

        /// <summary>Legacy combined tooltip text, retained for presentation tests.</summary>
        public string HoverBadgeText { get; private set; } = string.Empty;

        /// <summary>Whether the shared HUD tooltip should extend below this card.</summary>
        public bool ShowHoverBadgeBelow { get; private set; }

        /// <summary>Whether the shared HUD badge should currently be visible for this card.</summary>
        public bool ShouldShowHoverBadge =>
            _hovered && _showBadgeOnHover && !string.IsNullOrEmpty(HoverBadgeTitle);

        /// <summary>Returns the authored front sprite used for an already-projected card model.</summary>
        internal Sprite GetFaceSprite(GameSceneCardViewModel card)
        {
            return card == null || !card.RevealRank
                ? null
                : SpriteForCard(card.DefinitionKey, card.Rank, card.Suit);
        }

        private void Awake()
        {
            _baseScale = transform.localScale;

            HideRankText();
            RefreshSpriteUvRects();
            ApplyHoverOutline(false);
        }

        private void Update()
        {
            RefreshSpriteUvRects();
        }

        private void OnDisable()
        {
            StopScaleTween();
            transform.localScale = _baseScale;
            _hovered = false;
            ApplyHoverOutline(false);
            ApplyHoverCardBlend(false);
        }

        public void Bind(GameSceneCardViewModel card)
        {
            if (card == null)
            {
                return;
            }

            CardId = card.CardId;
            CanUse = card.CanUse;
            bool showPlayerHiddenBlend = card.RevealRank && !card.IsFaceUp;
            _showingFrontFace = card.RevealRank && !showPlayerHiddenBlend;
            _usesHoverCardBlend = showPlayerHiddenBlend;
            _showBadgeOnHover = CanUse || card.ShowHoverBadgeWhenUnavailable;
            ShowHoverBadgeBelow = card.ShowHoverBadgeBelow;

            Sprite faceSprite = null;
            if (card.RevealRank)
            {
                faceSprite = ApplyFaceSprite(card.DefinitionKey, card.Rank, card.Suit);
            }

            ResetCardBlend(FrontSpriteRenderer());

            if (front != null)
            {
                front.SetActive(_showingFrontFace);
            }

            if (back != null)
            {
                back.SetActive(!_showingFrontFace);
            }

            ApplyCardBlend(
                showPlayerHiddenBlend ? faceSprite : null,
                0f);

            HideRankText();

            HoverBadgeTitle = !card.RevealRank
                ? string.Empty
                : $"{card.Rank}. {card.DisplayName}";
            HoverBadgeDescription = !card.RevealRank
                ? string.Empty
                : card.AbilityDescription;
            HoverBadgeText = !card.RevealRank
                ? string.Empty
                : string.IsNullOrEmpty(card.AbilityDescription)
                    ? $"{card.Rank} {card.DisplayName}"
                    : $"{card.Rank} {card.DisplayName}\n{card.AbilityDescription}";

            // Pooled cards are reused; clear any prior hover state and snap to base size.
            _hovered = false;
            StopScaleTween();
            transform.localScale = _baseScale;
            ApplyHoverOutline(false);
        }

        /// <summary>Called by the pointer raycast when this card gains/loses hover.</summary>
        public void SetHovered(bool hovered)
        {
            if (_hovered == hovered)
            {
                return;
            }

            _hovered = hovered;
            PlayHoverSfx(hovered);
            PlayHoverScaleTween(hovered ? _baseScale * hoverScale : _baseScale);
            ApplyHoverOutline(hovered);
            ApplyHoverCardBlend(hovered);
        }

        /// <summary>
        /// Sets the resting scale used by hover feedback. World-space preview grids call this after
        /// binding so their cards can be smaller without snapping to the hand-card scale on hover.
        /// </summary>
        internal void SetBaseScale(Vector3 baseScale)
        {
            _baseScale = baseScale;
            StopScaleTween();
            transform.localScale = _baseScale;
        }

        internal void SetSortingOrder(int sortingOrder)
        {
            SpriteRenderer frontRenderer = FrontSpriteRenderer();
            if (frontRenderer != null)
            {
                frontRenderer.sortingOrder = sortingOrder;
            }

            SpriteRenderer backRenderer = BackSpriteRenderer();
            if (backRenderer != null)
            {
                backRenderer.sortingOrder = sortingOrder;
            }
        }

        /// <summary>
        /// Projects the authored top-position anchor into screen space.
        /// </summary>
        public bool TryGetHoverBadgeScreenPosition(
            Camera camera,
            bool useBottomAnchor,
            out Vector2 screenPosition)
        {
            screenPosition = default;
            Transform anchor = useBottomAnchor ? bottomPosition : topPosition;
            if (camera == null || anchor == null)
            {
                return false;
            }

            Vector3 projected = camera.WorldToScreenPoint(anchor.position);
            if (projected.z <= 0f)
            {
                return false;
            }

            screenPosition = new Vector2(projected.x, projected.y);
            return true;
        }

        private void PlayHoverScaleTween(Vector3 targetScale)
        {
            StopScaleTween();

            float duration = Mathf.Max(hoverScaleDuration, 0.01f);
            _scaleTween = transform.DOScale(targetScale, duration)
                .SetEase(hoverScaleCurve)
                .SetTarget(this);
        }

        private void StopScaleTween()
        {
            if (_scaleTween == null)
            {
                return;
            }

            _scaleTween.Kill();
            _scaleTween = null;
        }

        private void PlayHoverSfx(bool hovered)
        {
            if (!hovered || string.IsNullOrWhiteSpace(hoverSfxId))
            {
                return;
            }

            SoundManager.Current?.PlaySfx(hoverSfxId);
        }

        private void ApplyHoverOutline(bool visible)
        {
            ApplyOutline(FrontSpriteRenderer(), visible && _showingFrontFace);
            ApplyOutline(BackRenderer(), visible && !_showingFrontFace);
        }

        private void ApplyOutline(Renderer renderer, bool visible)
        {
            if (renderer == null)
            {
                return;
            }

            EnablePixelOutlineKeyword(renderer);

            Color outlineColor = ResolveOutlineColor(renderer);
            if (visible && outlineColor.a <= 0f)
            {
                outlineColor.a = 1f;
            }

            MaterialPropertyBlock propertyBlock = PropertyBlockFor(renderer);
            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(PixelOutlineColorId, outlineColor);
            propertyBlock.SetFloat(PixelOutlineWidthId, ResolveOutlineWidth(renderer));
            propertyBlock.SetFloat(
                PixelOutlineAlphaThresholdId,
                ResolveOutlineAlphaThreshold(renderer));
            propertyBlock.SetFloat(PixelOutlineVisibilityId, visible ? 1f : 0f);
            renderer.SetPropertyBlock(propertyBlock);
        }

        private Color ResolveOutlineColor(Renderer renderer)
        {
            Material material = renderer.sharedMaterial;
            if (useMaterialHoverOutlineSettings &&
                material != null &&
                material.HasProperty(PixelOutlineColorId))
            {
                return material.GetColor(PixelOutlineColorId);
            }

            return hoverOutlineColor;
        }

        private float ResolveOutlineWidth(Renderer renderer)
        {
            Material material = renderer.sharedMaterial;
            if (useMaterialHoverOutlineSettings &&
                material != null &&
                material.HasProperty(PixelOutlineWidthId))
            {
                return material.GetFloat(PixelOutlineWidthId);
            }

            return hoverOutlineWidth;
        }

        private float ResolveOutlineAlphaThreshold(Renderer renderer)
        {
            Material material = renderer.sharedMaterial;
            if (useMaterialHoverOutlineSettings &&
                material != null &&
                material.HasProperty(PixelOutlineAlphaThresholdId))
            {
                return material.GetFloat(PixelOutlineAlphaThresholdId);
            }

            return hoverOutlineAlphaThreshold;
        }

        private static void EnablePixelOutlineKeyword(Renderer renderer)
        {
            Material material = renderer.sharedMaterial;
            if (material != null && !material.IsKeywordEnabled(PixelOutlineKeyword))
            {
                material.EnableKeyword(PixelOutlineKeyword);
            }
        }

        private void ApplyHoverCardBlend(bool hovered)
        {
            if (!_usesHoverCardBlend)
            {
                return;
            }

            SetCardBlendAmount(hovered ? hiddenCardBlendAmount : 0f);
        }

        private Sprite ApplyFaceSprite(string definitionKey, int rank, CardSuit suit)
        {
            SpriteRenderer renderer = FrontSpriteRenderer();
            Sprite sprite = SpriteForCard(definitionKey, rank, suit);
            if (renderer != null && sprite != null)
            {
                renderer.sprite = sprite;
                RefreshSpriteUvRect(renderer, ref _frontUvSprite);
            }

            return sprite;
        }

        private void ApplyCardBlend(Sprite sprite, float amount)
        {
            Renderer renderer = BackRenderer();
            if (renderer == null)
            {
                return;
            }

            MaterialPropertyBlock propertyBlock = PropertyBlockFor(renderer);
            renderer.GetPropertyBlock(propertyBlock);
            if (sprite != null)
            {
                propertyBlock.SetTexture(CardBlendTextureId, sprite.texture);
            }

            propertyBlock.SetVector(CardBlendUvRectId, GetSpriteUvRect(sprite));
            propertyBlock.SetFloat(CardBlendAmountId, Mathf.Clamp01(amount));
            renderer.SetPropertyBlock(propertyBlock);
        }

        private void ResetCardBlend(Renderer renderer)
        {
            if (renderer == null)
            {
                return;
            }

            MaterialPropertyBlock propertyBlock = PropertyBlockFor(renderer);
            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetVector(CardBlendUvRectId, GetSpriteUvRect(null));
            propertyBlock.SetFloat(CardBlendAmountId, 0f);
            renderer.SetPropertyBlock(propertyBlock);
        }

        private void SetCardBlendAmount(float amount)
        {
            Renderer renderer = BackRenderer();
            if (renderer == null)
            {
                return;
            }

            MaterialPropertyBlock propertyBlock = PropertyBlockFor(renderer);
            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(CardBlendAmountId, Mathf.Clamp01(amount));
            renderer.SetPropertyBlock(propertyBlock);
        }

        private Sprite SpriteForCard(string definitionKey, int rank, CardSuit suit)
        {
            return cardContentCatalog == null || string.IsNullOrWhiteSpace(definitionKey)
                ? null
                : cardContentCatalog.GetNormalFaceSprite(definitionKey, suit);
        }

        private void RefreshSpriteUvRects()
        {
            RefreshSpriteUvRect(FrontSpriteRenderer(), ref _frontUvSprite);
            RefreshSpriteUvRect(BackSpriteRenderer(), ref _backUvSprite);
        }

        private void RefreshSpriteUvRect(SpriteRenderer renderer, ref Sprite trackedSprite)
        {
            if (renderer == null || renderer.sprite == trackedSprite)
            {
                return;
            }

            trackedSprite = renderer.sprite;
            Vector4 uvRect = GetSpriteUvRect(trackedSprite);

            MaterialPropertyBlock propertyBlock = PropertyBlockFor(renderer);
            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetVector(BaseSpriteUvRectId, uvRect);
            renderer.SetPropertyBlock(propertyBlock);
        }

        private MaterialPropertyBlock PropertyBlockFor(Renderer renderer)
        {
            if (renderer == _frontSpriteRenderer)
            {
                _frontPropertyBlock ??= new MaterialPropertyBlock();
                return _frontPropertyBlock;
            }

            _backPropertyBlock ??= new MaterialPropertyBlock();
            return _backPropertyBlock;
        }

        private static Vector4 GetSpriteUvRect(Sprite sprite)
        {
            if (sprite == null)
            {
                return new Vector4(0f, 0f, 1f, 1f);
            }

            Vector2[] uvs = sprite.uv;
            if (uvs == null || uvs.Length == 0)
            {
                return new Vector4(0f, 0f, 1f, 1f);
            }

            Vector2 minimum = uvs[0];
            Vector2 maximum = uvs[0];
            for (int i = 1; i < uvs.Length; i++)
            {
                minimum = Vector2.Min(minimum, uvs[i]);
                maximum = Vector2.Max(maximum, uvs[i]);
            }

            Vector2 size = maximum - minimum;
            return new Vector4(
                minimum.x,
                minimum.y,
                Mathf.Max(size.x, 0.00001f),
                Mathf.Max(size.y, 0.00001f));
        }

        private void HideRankText()
        {
            if (rankText == null)
            {
                return;
            }

            rankText.enabled = false;
            rankText.gameObject.SetActive(false);
        }

        private SpriteRenderer FrontSpriteRenderer()
        {
            if (_frontSpriteRenderer == null && front != null)
            {
                _frontSpriteRenderer = front.GetComponent<SpriteRenderer>();
            }

            return _frontSpriteRenderer;
        }

        private Renderer BackRenderer()
        {
            if (_backRenderer == null && back != null)
            {
                _backRenderer = BackSpriteRenderer();
            }

            return _backRenderer;
        }

        private SpriteRenderer BackSpriteRenderer()
        {
            if (_backSpriteRenderer == null && back != null)
            {
                _backSpriteRenderer = back.GetComponent<SpriteRenderer>();
            }

            return _backSpriteRenderer;
        }
    }
}
