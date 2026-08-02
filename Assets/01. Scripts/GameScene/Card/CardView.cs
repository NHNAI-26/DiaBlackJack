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

        [Header("Round result reveal")]
        [Min(0.1f)]
        [SerializeField] private float revealFlipDuration = 0.36f;

        [Header("Used card mark")]
        [SerializeField] private GameObject usedMark;
        [SerializeField] private SpriteRenderer usedMarkFirstStroke;
        [SerializeField] private SpriteRenderer usedMarkSecondStroke;
        [Min(0f)]
        [SerializeField] private float usedMarkStrokeDuration = 0.175f;

        [Header("Shop sold out")]
        [Tooltip("Authored card-local price and sold-out display. Detached beside the card in shops.")]
        [SerializeField] private ShopCardOfferStatusView shopOfferStatus;
        [SerializeField] private Color shopSoldOutTint =
            new Color(0.35f, 0.35f, 0.35f, 1f);

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
        private Material _cardBackMaterial;
        private Material _shopFrontMaterial;
        private Material _shopBackMaterial;
        private SpriteRenderer _frontSpriteRenderer;
        private SpriteRenderer _backSpriteRenderer;
        private Renderer _backRenderer;
        private Sprite _frontUvSprite;
        private Sprite _backUvSprite;
        private Tween _scaleTween;
        private Sequence _revealSequence;
        private Sequence _usedMarkSequence;
        private Vector3 _baseScale = Vector3.one;
        private Vector3 _usedMarkFirstStrokeScale = Vector3.one;
        private Vector3 _usedMarkSecondStrokeScale = Vector3.one;
        private bool _showingFrontFace = true;
        private bool _usesHoverCardBlend;
        private bool _showBadgeOnHover;
        private bool _hovered;
        private bool _hasBoundCard;
        private bool _isUsed;
        private bool _isEffectHighlighted;
        private bool _isShopSoldOut;
        private bool _shopColorsCaptured;
        private Color _shopFrontColor = Color.white;
        private Color _shopBackColor = Color.white;

        /// <summary>Run card id of the bound card, for pointer routing. -1 when unbound.</summary>
        public int CardId { get; private set; } = -1;

        /// <summary>Whether this card's manual effect can be activated right now (player, usable only).</summary>
        public bool CanUse { get; private set; }

        internal string DefinitionKey { get; private set; } = string.Empty;

        internal bool IsUsedMarkVisible => usedMark != null && usedMark.activeSelf;

        internal bool IsShopSoldOut => _isShopSoldOut;

        internal ShopCardOfferStatusView DetachShopOfferStatus(Transform holder)
        {
            if (shopOfferStatus == null || holder == null)
            {
                return null;
            }

            ShopCardOfferStatusView status = shopOfferStatus;
            shopOfferStatus = null;
            status.DetachFromCard(holder);
            return status;
        }

        /// <summary>Current card-effect option selected by clicking this world-space card.</summary>
        public int? CardEffectChoiceOptionId { get; private set; }

        /// <summary>Current effect command routed by clicking this world-space card.</summary>
        public GameSceneCombatHudCommand? DirectSelectionCommand { get; private set; }

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
            !_isShopSoldOut &&
            _hovered &&
            _showBadgeOnHover &&
            !string.IsNullOrEmpty(HoverBadgeTitle);

        /// <summary>Returns the authored front sprite used for an already-projected card model.</summary>
        internal Sprite GetFaceSprite(GameSceneCardViewModel card)
        {
            return card == null || !card.RevealRank
                ? null
                : SpriteForCard(card.DefinitionKey, card.Rank, card.Suit);
        }

        private void Awake()
        {
            if (shopOfferStatus != null)
            {
                shopOfferStatus.gameObject.SetActive(false);
            }

            _baseScale = transform.localScale;
            CaptureUsedMarkScales();

            HideRankText();
            RefreshSpriteUvRects();
            ApplyHoverOutline(false);
            ShowUsedMarkInstant(false);
        }

        private void Update()
        {
            RefreshSpriteUvRects();
        }

        private void OnDisable()
        {
            StopRevealSequence();
            StopScaleTween();
            StopUsedMarkSequence();
            transform.localScale = _baseScale;
            _hovered = false;
            ApplyHoverOutline(false);
            ApplyHoverCardBlend(false);
            ShowUsedMarkInstant(_isUsed);
        }

        private void OnDestroy()
        {
            DestroyMaterialInstance(_cardBackMaterial);
            DestroyMaterialInstance(_shopFrontMaterial);
            DestroyMaterialInstance(_shopBackMaterial);
        }

        public void Bind(GameSceneCardViewModel card)
        {
            if (card == null)
            {
                return;
            }

            bool animateUsedMark =
                Application.isPlaying &&
                _hasBoundCard &&
                CardId == card.CardId &&
                !_isUsed &&
                card.IsUsed;
            bool animateReveal =
                Application.isPlaying &&
                _hasBoundCard &&
                CardId == card.CardId &&
                !_showingFrontFace &&
                card.RevealRank &&
                card.IsFaceUp;

            StopRevealSequence();

            CardId = card.CardId;
            DefinitionKey = card.DefinitionKey;
            CanUse = card.CanUse;
            CardEffectChoiceOptionId = card.CardEffectChoiceOptionId;
            DirectSelectionCommand = card.DirectSelectionCommand;
            _isEffectHighlighted = card.IsEffectSource ||
                card.DirectSelectionCommand.HasValue;
            _isUsed = card.IsUsed;
            _hasBoundCard = true;
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
            ResetCardBlend(BackSpriteRenderer());

            SetFaceObjects(animateReveal ? false : _showingFrontFace);

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
            ApplyHoverOutline(_isEffectHighlighted);
            ApplyUsedMark(animateUsedMark);
            if (animateReveal)
            {
                PlayRevealFlip();
            }
        }

        /// <summary>Called by the pointer raycast when this card gains/loses hover.</summary>
        public void SetHovered(bool hovered)
        {
            if (_isShopSoldOut)
            {
                hovered = false;
            }

            if (_revealSequence != null)
            {
                return;
            }

            if (_hovered == hovered)
            {
                return;
            }

            _hovered = hovered;
            PlayHoverSfx(hovered);
            PlayHoverScaleTween(hovered ? _baseScale * hoverScale : _baseScale);
            ApplyHoverOutline(hovered || _isEffectHighlighted);
            ApplyHoverCardBlend(hovered);
        }

        /// <summary>
        /// Sets the resting scale used by hover feedback. World-space preview grids call this after
        /// binding so their cards can be smaller without snapping to the hand-card scale on hover.
        /// </summary>
        internal void SetBaseScale(Vector3 baseScale)
        {
            _baseScale = baseScale;
            StopRevealSequence();
            StopScaleTween();
            transform.localScale = _baseScale;
        }

        private void PlayRevealFlip()
        {
            StopScaleTween();
            transform.localScale = _baseScale;
            SetFaceObjects(showFront: false);

            float halfDuration = Mathf.Max(revealFlipDuration, 0.1f) * 0.5f;
            Sequence sequence = DOTween.Sequence()
                .SetTarget(this)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            sequence.Append(
                transform.DOScaleX(0f, halfDuration)
                    .SetEase(Ease.InQuad));
            sequence.AppendCallback(() => SetFaceObjects(showFront: true));
            sequence.Append(
                transform.DOScaleX(_baseScale.x, halfDuration)
                    .SetEase(Ease.OutQuad));
            sequence.OnComplete(() =>
            {
                transform.localScale = _baseScale;
                SetFaceObjects(showFront: true);
            });
            sequence.OnKill(() =>
            {
                if (_revealSequence == sequence)
                {
                    _revealSequence = null;
                }
            });
            _revealSequence = sequence;
        }

        private void StopRevealSequence()
        {
            if (_revealSequence != null)
            {
                _revealSequence.Kill();
                _revealSequence = null;
            }

            transform.localScale = _baseScale;
            SetFaceObjects(_showingFrontFace);
        }

        private void SetFaceObjects(bool showFront)
        {
            if (front != null)
            {
                front.SetActive(showFront);
            }

            if (back != null)
            {
                back.SetActive(!showFront);
            }
        }

        internal void SetShopPresentation()
        {
            CaptureShopColors();
            CreateMaterialInstance(
                FrontSpriteRenderer(),
                ref _shopFrontMaterial,
                "Shop Card Instance");
            CreateMaterialInstance(
                BackSpriteRenderer(),
                ref _shopBackMaterial,
                "Shop Card Instance");
            ApplyHoverOutline(false);
        }

        internal void SetShopSoldOut(bool isSoldOut)
        {
            _isShopSoldOut = isSoldOut;
            if (isSoldOut)
            {
                CanUse = false;
                _showBadgeOnHover = false;
                SetHovered(false);
            }

            CaptureShopColors();
            ApplyShopTint(FrontSpriteRenderer(), _shopFrontColor, isSoldOut);
            ApplyShopTint(BackSpriteRenderer(), _shopBackColor, isSoldOut);
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

            int usedMarkSortingOrder = sortingOrder + 1;
            if (usedMarkFirstStroke != null)
            {
                usedMarkFirstStroke.sortingOrder = usedMarkSortingOrder;
            }

            if (usedMarkSecondStroke != null)
            {
                usedMarkSecondStroke.sortingOrder = usedMarkSortingOrder;
            }
        }

        private void CaptureUsedMarkScales()
        {
            if (usedMarkFirstStroke != null)
            {
                _usedMarkFirstStrokeScale = usedMarkFirstStroke.transform.localScale;
            }

            if (usedMarkSecondStroke != null)
            {
                _usedMarkSecondStrokeScale = usedMarkSecondStroke.transform.localScale;
            }
        }

        private void CaptureShopColors()
        {
            if (_shopColorsCaptured)
            {
                return;
            }

            SpriteRenderer frontRenderer = FrontSpriteRenderer();
            SpriteRenderer backRenderer = BackSpriteRenderer();
            if (frontRenderer != null)
            {
                _shopFrontColor = frontRenderer.color;
            }

            if (backRenderer != null)
            {
                _shopBackColor = backRenderer.color;
            }

            _shopColorsCaptured = true;
        }

        private void ApplyShopTint(
            SpriteRenderer renderer,
            Color baseColor,
            bool isSoldOut)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.color = isSoldOut
                ? new Color(
                    baseColor.r * shopSoldOutTint.r,
                    baseColor.g * shopSoldOutTint.g,
                    baseColor.b * shopSoldOutTint.b,
                    baseColor.a * shopSoldOutTint.a)
                : baseColor;
        }

        private void ApplyUsedMark(bool animate)
        {
            StopUsedMarkSequence();
            if (!_isUsed)
            {
                ShowUsedMarkInstant(false);
                return;
            }

            if (!animate ||
                usedMark == null ||
                usedMarkFirstStroke == null ||
                usedMarkSecondStroke == null ||
                usedMarkStrokeDuration <= 0f)
            {
                ShowUsedMarkInstant(true);
                return;
            }

            usedMark.SetActive(true);
            usedMarkFirstStroke.gameObject.SetActive(true);
            usedMarkSecondStroke.gameObject.SetActive(false);
            SetStrokeScale(usedMarkFirstStroke, _usedMarkFirstStrokeScale, 0f);
            SetStrokeScale(usedMarkSecondStroke, _usedMarkSecondStrokeScale, 0f);

            Sequence sequence = DOTween.Sequence()
                .SetTarget(this);
            sequence.Append(
                usedMarkFirstStroke.transform
                    .DOScaleX(_usedMarkFirstStrokeScale.x, usedMarkStrokeDuration)
                    .SetEase(Ease.Linear));
            sequence.AppendCallback(() =>
            {
                usedMarkSecondStroke.gameObject.SetActive(true);
                SetStrokeScale(
                    usedMarkSecondStroke,
                    _usedMarkSecondStrokeScale,
                    0f);
            });
            sequence.Append(
                usedMarkSecondStroke.transform
                    .DOScaleX(_usedMarkSecondStrokeScale.x, usedMarkStrokeDuration)
                    .SetEase(Ease.Linear));
            sequence.OnComplete(() =>
            {
                SetStrokeScale(
                    usedMarkFirstStroke,
                    _usedMarkFirstStrokeScale,
                    1f);
                SetStrokeScale(
                    usedMarkSecondStroke,
                    _usedMarkSecondStrokeScale,
                    1f);
            });
            sequence.OnKill(() =>
            {
                if (_usedMarkSequence == sequence)
                {
                    _usedMarkSequence = null;
                }
            });
            _usedMarkSequence = sequence;
        }

        private void ShowUsedMarkInstant(bool visible)
        {
            if (usedMarkFirstStroke != null)
            {
                usedMarkFirstStroke.gameObject.SetActive(true);
            }

            if (usedMarkSecondStroke != null)
            {
                usedMarkSecondStroke.gameObject.SetActive(true);
            }

            if (usedMark != null)
            {
                usedMark.SetActive(visible);
            }

            SetStrokeScale(
                usedMarkFirstStroke,
                _usedMarkFirstStrokeScale,
                visible ? 1f : 0f);
            SetStrokeScale(
                usedMarkSecondStroke,
                _usedMarkSecondStrokeScale,
                visible ? 1f : 0f);
        }

        private static void SetStrokeScale(
            SpriteRenderer stroke,
            Vector3 fullScale,
            float progress)
        {
            if (stroke == null)
            {
                return;
            }

            Vector3 scale = fullScale;
            scale.x *= Mathf.Clamp01(progress);
            stroke.transform.localScale = scale;
        }

        private void StopUsedMarkSequence()
        {
            if (_usedMarkSequence == null)
            {
                return;
            }

            _usedMarkSequence.Kill();
            _usedMarkSequence = null;
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

        private static void CreateMaterialInstance(
            Renderer renderer,
            ref Material materialInstance,
            string instanceLabel)
        {
            if (renderer == null || materialInstance != null)
            {
                return;
            }

            Material source = renderer.sharedMaterial;
            if (source == null)
            {
                return;
            }

            materialInstance = new Material(source)
            {
                name = source.name + " (" + instanceLabel + ")"
            };
            renderer.sharedMaterial = materialInstance;
        }

        private static void DestroyMaterialInstance(Material material)
        {
            if (material == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(material);
            }
            else
            {
                DestroyImmediate(material);
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
            SpriteRenderer renderer = BackSpriteRenderer();
            if (renderer == null)
            {
                return;
            }

            Sprite textureSprite = sprite != null ? sprite : renderer.sprite;
            EnsureCardBackMaterialInstance(renderer);
            MaterialPropertyBlock propertyBlock = PropertyBlockFor(renderer);
            renderer.GetPropertyBlock(propertyBlock);
            float blendAmount = sprite == null ? 0f : Mathf.Clamp01(amount);
            SetCardBlendSpriteProperties(
                propertyBlock,
                renderer.sharedMaterial,
                textureSprite);
            SetCardBlendAmountProperty(
                propertyBlock,
                renderer.sharedMaterial,
                blendAmount);
            renderer.SetPropertyBlock(propertyBlock);
        }

        private void ResetCardBlend(SpriteRenderer renderer)
        {
            if (renderer == null)
            {
                return;
            }

            MaterialPropertyBlock propertyBlock = PropertyBlockFor(renderer);
            renderer.GetPropertyBlock(propertyBlock);
            if (renderer == _backSpriteRenderer)
            {
                EnsureCardBackMaterialInstance(renderer);
            }

            Material blendMaterial =
                renderer == _backSpriteRenderer ? renderer.sharedMaterial : null;
            SetCardBlendSpriteProperties(
                propertyBlock,
                blendMaterial,
                renderer.sprite);
            SetCardBlendAmountProperty(propertyBlock, blendMaterial, 0f);
            renderer.SetPropertyBlock(propertyBlock);
        }

        private void EnsureCardBackMaterialInstance(SpriteRenderer renderer)
        {
            if (renderer == null || renderer != _backSpriteRenderer)
            {
                return;
            }

            CreateMaterialInstance(
                renderer,
                ref _cardBackMaterial,
                "Card Back Instance");
        }

        private static void SetCardBlendSpriteProperties(
            MaterialPropertyBlock propertyBlock,
            Material material,
            Sprite sprite)
        {
            if (propertyBlock == null)
            {
                return;
            }

            Texture texture = sprite == null ? Texture2D.whiteTexture : sprite.texture;
            Vector4 uvRect = GetSpriteUvRect(sprite);
            propertyBlock.SetTexture(CardBlendTextureId, texture);
            propertyBlock.SetVector(CardBlendUvRectId, uvRect);
            if (material != null)
            {
                if (material.HasProperty(CardBlendTextureId))
                {
                    material.SetTexture(CardBlendTextureId, texture);
                }

                if (material.HasProperty(CardBlendUvRectId))
                {
                    material.SetVector(CardBlendUvRectId, uvRect);
                }
            }
        }

        private static void SetCardBlendAmountProperty(
            MaterialPropertyBlock propertyBlock,
            Material material,
            float amount)
        {
            float clampedAmount = Mathf.Clamp01(amount);
            propertyBlock.SetFloat(CardBlendAmountId, clampedAmount);
            if (material != null && material.HasProperty(CardBlendAmountId))
            {
                material.SetFloat(CardBlendAmountId, clampedAmount);
            }
        }

        private void SetCardBlendAmount(float amount)
        {
            SpriteRenderer renderer = BackSpriteRenderer();
            if (renderer == null)
            {
                return;
            }

            EnsureCardBackMaterialInstance(renderer);
            MaterialPropertyBlock propertyBlock = PropertyBlockFor(renderer);
            renderer.GetPropertyBlock(propertyBlock);
            SetCardBlendAmountProperty(
                propertyBlock,
                renderer.sharedMaterial,
                amount);
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
            if (renderer == null)
            {
                return;
            }

            MaterialPropertyBlock propertyBlock = PropertyBlockFor(renderer);
            renderer.GetPropertyBlock(propertyBlock);

            Sprite currentSprite = renderer.sprite;
            Vector4 uvRect = GetSpriteUvRect(currentSprite);
            if (currentSprite == trackedSprite &&
                Approximately(propertyBlock.GetVector(BaseSpriteUvRectId), uvRect))
            {
                return;
            }

            trackedSprite = currentSprite;
            propertyBlock.SetVector(BaseSpriteUvRectId, uvRect);
            renderer.SetPropertyBlock(propertyBlock);
        }

        private static bool Approximately(Vector4 left, Vector4 right)
        {
            return Mathf.Approximately(left.x, right.x) &&
                Mathf.Approximately(left.y, right.y) &&
                Mathf.Approximately(left.z, right.z) &&
                Mathf.Approximately(left.w, right.w);
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
