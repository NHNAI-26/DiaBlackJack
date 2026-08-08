using DG.Tweening;
using TMPro;
using Border.Audio;
using DiaBlackJack.Content;
using DiaBlackJack.CoreLoop;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

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
        private static readonly Color SelectionFrontTint =
            new Color32(0xB2, 0xA9, 0x92, 0xFF);

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
        [Tooltip("Visual-only pivot used by hand and selection cards so hover feedback does not move or resize the collider.")]
        [SerializeField] private Transform hoverVisualRoot;
        [SerializeField] private float hoverScale = 1.15f;
        [Min(0.01f)]
        [SerializeField] private float hoverScaleDuration = 0.08f;
        [SerializeField] private AnimationCurve hoverScaleCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private string hoverSfxId = "cardHover";

        [Header("Hover outline")]
        [SerializeField] private bool useMaterialHoverOutlineSettings = true;
        [ColorUsage(true, true)]
        [FormerlySerializedAs("hoverOutlineColor")]
        [SerializeField] private Color effectHighlightOutlineColor =
            new Color(1f, 0.685f, 0f, 1f);
        [SerializeField] private float hoverOutlineWidth = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float hoverOutlineAlphaThreshold = 0.5f;

        [Header("Hover outline state colors")]
        [Tooltip("Only replaces the existing pixel-outline shader color. Width, threshold, keyword, and HDR glow path stay unchanged.")]
        [ColorUsage(true, true)]
        [SerializeField] private Color basicHoverOutlineColor =
            new Color(4f, 4f, 4f, 1f);
        [ColorUsage(true, true)]
        [SerializeField] private Color unavailableHoverOutlineColor =
            new Color(4f, 0f, 0f, 1f);
        [ColorUsage(true, true)]
        [SerializeField] private Color availableHoverOutlineColor =
            new Color(0f, 4f, 0f, 1f);
        [ColorUsage(true, true)]
        [SerializeField] private Color automaticHoverOutlineColor =
            new Color(3.9999995f, 3.3765495f, 0f, 1f);
        [ColorUsage(true, true)]
        [SerializeField] private Color usedHoverOutlineColor =
            new Color(2f, 2f, 2f, 1f);

        [Header("Player hidden card")]
        [Tooltip("Blends the real face over the card back while only the player may see its rank.")]
        [Range(0f, 1f)]
        [SerializeField] private float hiddenCardBlendAmount = 0.5f;

        [Header("Round result reveal")]
        [Min(0.01f)]
        [SerializeField] private float revealFlipDuration = 0.36f;
        [Tooltip("Normalized time in the reveal duration when the visible side changes. 0.5 means halfway through.")]
        [Range(0f, 1f)]
        [SerializeField] private float revealFaceSwapNormalizedTime = 0.5f;
        [SerializeField] private bool revealFlipXOnFaceSwap = true;
        [Tooltip("Temporary local-position offset applied during the reveal flip. Curves decide how much of each axis is applied over the total duration.")]
        [SerializeField] private Vector3 revealPositionOffset = Vector3.zero;
        [SerializeField] private AnimationCurve revealPositionXCurve =
            DefaultRevealOffsetCurve();
        [SerializeField] private AnimationCurve revealPositionYCurve =
            DefaultRevealOffsetCurve();
        [SerializeField] private AnimationCurve revealPositionZCurve =
            DefaultRevealOffsetCurve();
        [Tooltip("Temporary local-euler offset applied during the reveal flip. Curves decide how much of each axis is applied over the total duration.")]
        [SerializeField] private Vector3 revealRotationOffset = Vector3.zero;
        [SerializeField] private AnimationCurve revealRotationXCurve =
            DefaultRevealOffsetCurve();
        [SerializeField] private AnimationCurve revealRotationYCurve =
            DefaultRevealOffsetCurve();
        [SerializeField] private AnimationCurve revealRotationZCurve =
            DefaultRevealOffsetCurve();

        [Header("Satan number guess result")]
        [Min(0.01f)]
        [FormerlySerializedAs("satanGuessDissolveDuration")]
        [SerializeField] private float satanGuessBrandDissolveDuration = 3f;
        [SerializeField] private AnimationCurve satanGuessBrandDissolveCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Min(0.01f)]
        [SerializeField] private float satanGuessCardDissolveDuration = 3f;
        [SerializeField] private AnimationCurve satanGuessCardDissolveCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Min(0.01f)]
        [SerializeField] private float satanGuessPunchDuration = 0.55f;
        [SerializeField] private float satanGuessPunchRotation = 18f;
        [Min(1)]
        [SerializeField] private int satanGuessPunchVibrato = 3;
        [Min(0.01f)]
        [SerializeField] private float satanGuessBrandScale = 0.38f;
        [SerializeField] private Texture2D satanGuessDissolveNoiseMap;
        [SerializeField] private Material satanGuessBrandMaterial;

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
        private static readonly int SpriteFlipXId = Shader.PropertyToID("_SpriteFlipX");
        private static readonly int CardBlendAmountId = Shader.PropertyToID("_CardBlendAmount");
        private static readonly int CardBlendUvRectId = Shader.PropertyToID("_CardBlendUVRect");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int PixelOutlineColorId = Shader.PropertyToID("_PixelOutlineColor");
        private static readonly int PixelOutlineWidthId = Shader.PropertyToID("_PixelOutlineWidth");
        private static readonly int PixelOutlineAlphaThresholdId = Shader.PropertyToID("_PixelOutlineAlphaThreshold");
        private static readonly int PixelOutlineVisibilityId = Shader.PropertyToID("_PixelOutlineVisibility");
        private static readonly int LightingModeId = Shader.PropertyToID("_LightingMode");
        private static readonly int DissolveAmountId = Shader.PropertyToID("_DissolveAmount");
        private static readonly int DissolveEnabledId = Shader.PropertyToID("_DissolveEnabled");
        private static readonly int DissolveNoiseMapId = Shader.PropertyToID("_DissolveNoiseMap");
        private static readonly int DissolveEdgeColorId = Shader.PropertyToID("_DissolveEdgeColor");
        private const string UnlitKeyword = "_UNLIT_ON";
        private const string PixelOutlineKeyword = "_PIXEL_OUTLINE_ON";
        private const string DissolveKeyword = "_DISSOLVE_ON";
        private const string FlipSfxId = "flipCard";
        private const string SatanBrandDissolveSfxId = "shapeDissolve";
        private const string SatanCardDissolveSfxId = "fireCard";
        private const string SatanGuessMissSfxId = "cardShake";

        private MaterialPropertyBlock _frontPropertyBlock;
        private MaterialPropertyBlock _backPropertyBlock;
        private Material _cardFrontMaterial;
        private Material _cardBackMaterial;
        private Material _shopFrontMaterial;
        private Material _shopBackMaterial;
        private Material _satanGuessBrandMaterial;
        private SpriteRenderer _frontSpriteRenderer;
        private SpriteRenderer _backSpriteRenderer;
        private Renderer _backRenderer;
        private Color _frontEffectHighlightColor;
        private Color _backEffectHighlightColor;
        private bool _frontEffectHighlightColorCaptured;
        private bool _backEffectHighlightColorCaptured;
        private Sprite _frontUvSprite;
        private Sprite _backUvSprite;
        private Tween _scaleTween;
        private Sequence _revealSequence;
        private Sequence _usedMarkSequence;
        private Sequence _satanGuessSequence;
        private SpriteRenderer _satanGuessBrandRenderer;
        private Vector3 _baseScale = Vector3.one;
        private Vector3 _hoverVisualBaseScale = Vector3.one;
        private Vector3 _usedMarkFirstStrokeScale = Vector3.one;
        private Vector3 _usedMarkSecondStrokeScale = Vector3.one;
        private Vector3 _revealBaseLocalPosition;
        private Vector3 _revealBaseLocalEulerAngles;
        private bool _hasRevealBaseTransform;
        private bool _hasRevealRendererFlipState;
        private GameSceneCardViewModel _pendingRevealFaceCard;
        private bool _revealFaceSwapped;
        private bool _showingFrontFace = true;
        private bool _usesHoverCardBlend;
        private bool _showBadgeOnHover;
        private bool _hovered;
        private bool _useHandHoverVisual;
        private bool _hasBoundCard;
        private bool _isUsed;
        private bool _isEffectSource;
        private bool _isEffectHighlighted;
        private bool _comparisonHighlighted;
        private bool _matchSelectionHoverGlowIntensity;
        private GameSceneCardHoverOutlineState _hoverOutlineState =
            GameSceneCardHoverOutlineState.Basic;
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

        /// <summary>Seconds into the reveal-flip animation when the visible side swaps from
        /// back to front. Callers that need to sync other UI (e.g. a hand total) to the moment
        /// the card actually looks face-up should wait this long, not the full flip duration.</summary>
        public float RevealFaceSwapSeconds =>
            Mathf.Max(revealFlipDuration, 0.01f) *
                Mathf.Clamp01(revealFaceSwapNormalizedTime);

        /// <summary>Full reveal duration used by ordered showdown presentation.</summary>
        public float RevealDurationSeconds =>
            Mathf.Max(revealFlipDuration, 0.01f);

        internal bool IsComparisonHighlighted => _comparisonHighlighted;

        internal bool HasEffectiveHighlight =>
            _hovered || _isEffectHighlighted || _comparisonHighlighted;

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
            if (hoverVisualRoot != null)
            {
                _hoverVisualBaseScale = hoverVisualRoot.localScale;
            }
            CaptureUsedMarkScales();

            HideRankText();
            RefreshSpriteUvRects();
            ApplyHoverOutline(false);
            ShowUsedMarkInstant(false);
        }

        private void OnValidate()
        {
            hoverScaleDuration = Mathf.Max(hoverScaleDuration, 0.01f);
            revealFlipDuration = Mathf.Max(revealFlipDuration, 0.01f);
            revealFaceSwapNormalizedTime =
                Mathf.Clamp01(revealFaceSwapNormalizedTime);
            usedMarkStrokeDuration = Mathf.Max(usedMarkStrokeDuration, 0f);
            satanGuessBrandDissolveDuration =
                Mathf.Max(satanGuessBrandDissolveDuration, 0.01f);
            satanGuessCardDissolveDuration =
                Mathf.Max(satanGuessCardDissolveDuration, 0.01f);
            satanGuessPunchDuration = Mathf.Max(satanGuessPunchDuration, 0.01f);
            satanGuessPunchVibrato = Mathf.Max(satanGuessPunchVibrato, 1);
            satanGuessBrandScale = Mathf.Max(satanGuessBrandScale, 0.01f);
            satanGuessBrandDissolveCurve ??=
                AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            satanGuessCardDissolveCurve ??=
                AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            EnsureRevealCurves();
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
            ResetSatanNumberGuessPresentation();
            ResetHoverScales();
            _hovered = false;
            _isEffectSource = false;
            _comparisonHighlighted = false;
            ApplyHoverOutline(false);
            ApplyHoverCardBlend(false);
            ShowUsedMarkInstant(_isUsed);
        }

        private void OnDestroy()
        {
            DestroyMaterialInstance(_cardFrontMaterial);
            DestroyMaterialInstance(_cardBackMaterial);
            DestroyMaterialInstance(_shopFrontMaterial);
            DestroyMaterialInstance(_shopBackMaterial);
            DestroyMaterialInstance(_satanGuessBrandMaterial);
        }

        /// <summary>
        /// True if binding <paramref name="next"/> right now would trigger the reveal-flip
        /// animation (i.e. this same card is currently shown back-up and would become face-up).
        /// Callers that need to sync other UI (e.g. a hand total) to the actual visual reveal
        /// should check this before <see cref="Bind"/> and, if true, wait
        /// <see cref="RevealFaceSwapSeconds"/> before showing the effect of the new state.
        /// </summary>
        public bool WillAnimateRevealFor(GameSceneCardViewModel next)
        {
            return next != null &&
                Application.isPlaying &&
                _hasBoundCard &&
                CardId == next.CardId &&
                !_showingFrontFace &&
                next.RevealRank &&
                next.IsFaceUp;
        }

        public void Bind(GameSceneCardViewModel card)
        {
            Bind(card, showTransientEffectSource: true);
        }

        internal void Bind(
            GameSceneCardViewModel card,
            bool showTransientEffectSource)
        {
            Bind(
                card,
                showTransientEffectSource,
                showDirectSelectionCommand: true);
        }

        internal void Bind(
            GameSceneCardViewModel card,
            bool showTransientEffectSource,
            bool showDirectSelectionCommand)
        {
            if (card == null)
            {
                return;
            }

            ResetSelectionPresentation();
            ResetSatanNumberGuessPresentation();

            bool sameCard = _hasBoundCard && CardId == card.CardId;
            bool wasEmphasized =
                _hovered || _isEffectSource || _comparisonHighlighted;
            Transform scaleTarget = HoverScaleTarget();
            Vector3 preservedScale = scaleTarget.localScale;
            bool isEffectSource = card.IsEffectSource &&
                (card.IsEffectSourcePersistent || showTransientEffectSource);
            bool preserveCurrentScale = sameCard &&
                (wasEmphasized || isEffectSource);
            bool animateUsedMark =
                Application.isPlaying &&
                _hasBoundCard &&
                CardId == card.CardId &&
                !_isUsed &&
                card.IsUsed;
            bool animateReveal = WillAnimateRevealFor(card);

            StopRevealSequence();
            _pendingRevealFaceCard = null;

            EnsureCardMaterialInstances();
            CardId = card.CardId;
            DefinitionKey = card.DefinitionKey;
            CanUse = card.CanUse;
            CardEffectChoiceOptionId = card.CardEffectChoiceOptionId;
            DirectSelectionCommand = showDirectSelectionCommand
                ? card.DirectSelectionCommand
                : null;
            _isEffectSource = isEffectSource;
            _isEffectHighlighted = _isEffectSource ||
                DirectSelectionCommand.HasValue;
            _isUsed = card.IsUsed;
            _hoverOutlineState = card.HoverOutlineState;
            _hasBoundCard = true;
            bool showPlayerHiddenBlend = card.RevealRank && !card.IsFaceUp;
            _showingFrontFace = card.RevealRank && !showPlayerHiddenBlend;
            _usesHoverCardBlend = showPlayerHiddenBlend;
            _showBadgeOnHover = CanUse || card.ShowHoverBadgeWhenUnavailable;
            ShowHoverBadgeBelow = card.ShowHoverBadgeBelow;

            Sprite faceSprite = null;
            if (card.RevealRank)
            {
                if (animateReveal)
                {
                    _pendingRevealFaceCard = card;
                }
                else
                {
                    faceSprite = ApplyFaceSprite(
                        card.DefinitionKey,
                        card.Rank,
                        card.Suit);
                }
            }

            ResetCardBlend(FrontSpriteRenderer());
            ResetCardBlend(BackSpriteRenderer());

            SetFaceObjects(animateReveal ? false : _showingFrontFace);

            ApplyCardBlend(
                showPlayerHiddenBlend ? faceSprite : null,
                0f);

            HideRankText();

            HoverBadgeTitle = card.RevealRank
                ? $"{card.Rank}. {card.DisplayName}"
                : card.DisplayName;
            HoverBadgeDescription = card.AbilityDescription;
            string hoverBadgeHeading = card.RevealRank
                ? $"{card.Rank} {card.DisplayName}"
                : card.DisplayName;
            HoverBadgeText = string.IsNullOrEmpty(card.AbilityDescription)
                ? hoverBadgeHeading
                : $"{hoverBadgeHeading}\n{card.AbilityDescription}";

            // Pooled cards are reused; clear any prior hover state and snap to base size.
            _hovered = false;
            StopScaleTween();
            ResetHoverScales();
            if (preserveCurrentScale)
            {
                HoverScaleTarget().localScale = preservedScale;
            }

            Vector3 restingScale = HoverRestingScale();
            Vector3 targetScale = _isEffectSource || _comparisonHighlighted
                ? restingScale * hoverScale
                : restingScale;
            if ((HoverScaleTarget().localScale - targetScale).sqrMagnitude >
                0.0000001f)
            {
                PlayHoverScaleTween(targetScale);
            }
            ApplyHoverOutline(
                _isEffectHighlighted || _comparisonHighlighted);
            ApplyUsedMark(animateUsedMark);
            if (animateReveal)
            {
                PlayRevealFlip();
            }
        }

#if UNITY_EDITOR
        public bool CanPlayRevealFlipAnimationTest =>
            Application.isPlaying &&
            isActiveAndEnabled &&
            gameObject.activeInHierarchy;

        public void PlayRevealFlipAnimationTest()
        {
            if (!CanPlayRevealFlipAnimationTest)
            {
                return;
            }

            StopRevealSequence();
            StopScaleTween();
            ResetHoverScales();
            _hovered = false;
            ApplyHoverOutline(
                _isEffectHighlighted || _comparisonHighlighted);
            ApplyHoverCardBlend(false);
            PlayRevealFlip();
        }
#endif

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
            Vector3 restingScale = HoverRestingScale();
            PlayHoverScaleTween(
                hovered || _isEffectSource || _comparisonHighlighted
                    ? restingScale * hoverScale
                    : restingScale);
            ApplyHoverOutline(
                hovered || _isEffectHighlighted || _comparisonHighlighted);
            ApplyHoverCardBlend(hovered);
        }

        /// <summary>
        /// Reuses hover scale and outline for comparison without emitting hover audio or tooltip.
        /// Effect-source emphasis remains authoritative when this temporary layer is removed.
        /// </summary>
        internal void SetComparisonHighlighted(bool highlighted)
        {
            if (_comparisonHighlighted == highlighted)
            {
                return;
            }

            _comparisonHighlighted = highlighted;
            Vector3 restingScale = HoverRestingScale();
            PlayHoverScaleTween(
                _hovered || _isEffectSource || _comparisonHighlighted
                    ? restingScale * hoverScale
                    : restingScale);
            ApplyHoverOutline(
                _hovered || _isEffectHighlighted || _comparisonHighlighted);
        }

        internal bool IsSatanNumberGuessAnimationPlaying =>
            _satanGuessSequence != null && _satanGuessSequence.IsActive();

        internal bool PlaySatanNumberGuessResult(
            bool succeeded,
            Sprite brandSprite)
        {
            if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
            {
                return false;
            }

            ResetSatanNumberGuessPresentation();
            SetHovered(false);
            SetFaceObjects(_showingFrontFace);

            ConfigureSatanGuessBrand(succeeded ? brandSprite : null);
            Sequence sequence = DOTween.Sequence()
                .SetTarget(this)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            if (succeeded)
            {
                EnableSatanDissolve();
                EnableSatanGuessBrandDissolve();
                float brandDuration = Mathf.Max(
                    satanGuessBrandDissolveDuration,
                    0.01f);
                float cardDuration = Mathf.Max(
                    satanGuessCardDissolveDuration,
                    0.01f);
                SoundManager.Current?.PlaySfx(SatanBrandDissolveSfxId);
                sequence.Append(
                    DOVirtual.Float(
                            1f,
                            0f,
                            brandDuration,
                            SetSatanGuessBrandDissolveAmount)
                        .SetEase(satanGuessBrandDissolveCurve));
                sequence.AppendCallback(() =>
                    SoundManager.Current?.PlaySfx(SatanCardDissolveSfxId));
                sequence.Append(
                    DOVirtual.Float(
                            0f,
                            1f,
                            cardDuration,
                            SetSatanDissolveAmount)
                        .SetEase(satanGuessCardDissolveCurve));
                sequence.Join(
                    DOVirtual.Float(
                            0f,
                            1f,
                            cardDuration,
                            SetSatanGuessBrandDissolveAmount)
                        .SetEase(satanGuessCardDissolveCurve));
            }
            else
            {
                SoundManager.Current?.PlaySfx(SatanGuessMissSfxId);
                sequence.Append(
                    transform.DOPunchRotation(
                            new Vector3(0f, 0f, satanGuessPunchRotation),
                            Mathf.Max(satanGuessPunchDuration, 0.01f),
                            satanGuessPunchVibrato,
                            0.8f)
                        .SetEase(Ease.InOutSine));
            }

            sequence.OnComplete(() =>
            {
                if (!succeeded)
                {
                    ResetSatanNumberGuessPresentation();
                }

                if (_satanGuessSequence == sequence)
                {
                    _satanGuessSequence = null;
                }
            });
            sequence.OnKill(() =>
            {
                if (_satanGuessSequence == sequence)
                {
                    _satanGuessSequence = null;
                }
            });
            _satanGuessSequence = sequence;
            return true;
        }

        internal Transform HoverVisualTransform =>
            hoverVisualRoot != null ? hoverVisualRoot : transform;

        internal void EnableHoverVisualOnly()
        {
            StopScaleTween();
            ResetHoverScales();
            _useHandHoverVisual = hoverVisualRoot != null;
            _hovered = false;
        }

        internal void EnableHandHoverVisualOnly()
        {
            EnableHoverVisualOnly();
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
            ResetHoverScales();
        }
        
        private void SetShaderFlipX(
            SpriteRenderer renderer,
            bool flipX)
        {
            if (renderer == null)
            {
                return;
            }

            // 메시 자체가 뒤집히는 것을 방지한다.
            renderer.flipX = false;

            MaterialPropertyBlock propertyBlock =
                PropertyBlockFor(renderer);

            renderer.GetPropertyBlock(propertyBlock);

            propertyBlock.SetFloat(
                SpriteFlipXId,
                flipX ? 1f : 0f);

            renderer.SetPropertyBlock(propertyBlock);
        }

        private void PlayRevealFlip()
        {
            SoundManager.Current?.PlaySfx(FlipSfxId);
            StopScaleTween();
            ResetHoverScales();
            SetFaceObjects(showFront: false);

            EnsureRevealCurves();

            // Jump any still-running move-into-hand tween on this transform to its
            // final position before capturing it as the flip's base — otherwise, if
            // the card is flipped while it's still animating into its hand slot
            // (more likely for the opponent's hand, whose cards can be dealt and
            // revealed in the same beat), the flip captures and then restores to
            // wherever it happened to be mid-move rather than its actual seat.
            DOTween.Complete(transform);
            _revealBaseLocalPosition = transform.localPosition;
            _revealBaseLocalEulerAngles = transform.localEulerAngles;
            _hasRevealBaseTransform = true;
            CaptureRevealRendererFlipState();
            _revealFaceSwapped = false;

            float duration = Mathf.Max(revealFlipDuration, 0.01f);
            Sequence sequence = DOTween.Sequence()
                .SetTarget(this)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            sequence.Append(
                DOVirtual.Float(
                        0f,
                        1f,
                        duration,
                        ApplyRevealMotion)
                    .SetEase(Ease.Linear));
            sequence.OnComplete(() =>
            {
                ApplyPendingRevealFace();
                RestoreRevealTransform();
                ResetHoverVisualScale();
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

            RestoreRevealTransform();
            ResetHoverVisualScale();
            SetFaceObjects(_showingFrontFace);
            _pendingRevealFaceCard = null;
            _revealFaceSwapped = false;
        }

        private void ApplyRevealMotion(float normalizedTime)
        {
            Vector3 positionOffset = new Vector3(
                revealPositionOffset.x * EvaluateCurve(
                    revealPositionXCurve,
                    normalizedTime),
                revealPositionOffset.y * EvaluateCurve(
                    revealPositionYCurve,
                    normalizedTime),
                revealPositionOffset.z * EvaluateCurve(
                    revealPositionZCurve,
                    normalizedTime));
            Vector3 rotationOffset = new Vector3(
                revealRotationOffset.x * EvaluateCurve(
                    revealRotationXCurve,
                    normalizedTime),
                revealRotationOffset.y * EvaluateCurve(
                    revealRotationYCurve,
                    normalizedTime),
                revealRotationOffset.z * EvaluateCurve(
                    revealRotationZCurve,
                    normalizedTime));

            transform.localPosition = _revealBaseLocalPosition + positionOffset;
            transform.localEulerAngles =
                _revealBaseLocalEulerAngles + rotationOffset;

            if (!_revealFaceSwapped &&
                normalizedTime >= Mathf.Clamp01(revealFaceSwapNormalizedTime))
            {
                SwapRevealFace();
            }
        }

        private void RestoreRevealTransform()
        {
            transform.localScale = _baseScale;
            if (_hasRevealBaseTransform)
            {
                transform.localPosition = _revealBaseLocalPosition;
                transform.localEulerAngles = _revealBaseLocalEulerAngles;
                _hasRevealBaseTransform = false;
            }

            RestoreRevealRendererFlipState();
        }

        private void SwapRevealFace()
        {
            // _revealFaceSwapped = true;
            // ApplyPendingRevealFace();
            // SetFaceObjects(showFront: true);
            // if (revealFlipXOnFaceSwap)
            // {
            //     ApplyRevealRendererFlip(invert: true);
            // }
            _revealFaceSwapped = true;

            // 앞면이 표시되기 전에 X Flip 적용
            if (revealFlipXOnFaceSwap)
            {
                ApplyRevealRendererFlip(invert: true);
            }

            ApplyPendingRevealFace();
            SetFaceObjects(showFront: true);
        }

        private void ApplyPendingRevealFace()
        {
            if (_pendingRevealFaceCard == null)
            {
                return;
            }

            ApplyFaceSprite(
                _pendingRevealFaceCard.DefinitionKey,
                _pendingRevealFaceCard.Rank,
                _pendingRevealFaceCard.Suit);
            _pendingRevealFaceCard = null;
        }

        private void CaptureRevealRendererFlipState()
        {
            _hasRevealRendererFlipState = true;

            // SpriteRenderer의 메시 Flip은 사용하지 않는다.
            SetShaderFlipX(FrontSpriteRenderer(), false);
            SetShaderFlipX(BackSpriteRenderer(), false);
        }

        private void ApplyRevealRendererFlip(bool invert)
        {
            if (!_hasRevealRendererFlipState)
            {
                return;
            }

            SetShaderFlipX(FrontSpriteRenderer(), invert);
            SetShaderFlipX(BackSpriteRenderer(), invert);
        }

        private void RestoreRevealRendererFlipState()
        {
            if (!_hasRevealRendererFlipState)
            {
                return;
            }

            SetShaderFlipX(FrontSpriteRenderer(), false);
            SetShaderFlipX(BackSpriteRenderer(), false);

            _hasRevealRendererFlipState = false;

        }

        private void EnsureRevealCurves()
        {
            revealPositionXCurve = EnsureCurve(
                revealPositionXCurve,
                DefaultRevealOffsetCurve());
            revealPositionYCurve = EnsureCurve(
                revealPositionYCurve,
                DefaultRevealOffsetCurve());
            revealPositionZCurve = EnsureCurve(
                revealPositionZCurve,
                DefaultRevealOffsetCurve());
            revealRotationXCurve = EnsureCurve(
                revealRotationXCurve,
                DefaultRevealOffsetCurve());
            revealRotationYCurve = EnsureCurve(
                revealRotationYCurve,
                DefaultRevealOffsetCurve());
            revealRotationZCurve = EnsureCurve(
                revealRotationZCurve,
                DefaultRevealOffsetCurve());
        }

        private static AnimationCurve EnsureCurve(
            AnimationCurve curve,
            AnimationCurve fallback)
        {
            return curve == null || curve.length == 0 ? fallback : curve;
        }

        private static float EvaluateCurve(AnimationCurve curve, float time)
        {
            return curve == null ? 0f : curve.Evaluate(time);
        }

        private static AnimationCurve DefaultRevealOffsetCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.5f, 1f),
                new Keyframe(1f, 0f));
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

        internal void SetUnlitPresentation()
        {
            EnsureCardMaterialInstances();
            ApplyUnlitPresentation(FrontSpriteRenderer());
            ApplyUnlitPresentation(BackSpriteRenderer());
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

        internal void ApplySelectionPresentation()
        {
            _matchSelectionHoverGlowIntensity = true;
            SpriteRenderer frontRenderer = FrontSpriteRenderer();
            if (frontRenderer != null)
            {
                frontRenderer.color = SelectionFrontTint;
            }
        }

        private void ResetSelectionPresentation()
        {
            _matchSelectionHoverGlowIntensity = false;
            SpriteRenderer frontRenderer = FrontSpriteRenderer();
            if (frontRenderer != null)
            {
                frontRenderer.color = Color.white;
            }
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

            if (_satanGuessBrandRenderer != null)
            {
                _satanGuessBrandRenderer.sortingOrder = usedMarkSortingOrder;
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

        private void ResetSatanNumberGuessPresentation()
        {
            if (_satanGuessSequence != null)
            {
                _satanGuessSequence.Kill();
                _satanGuessSequence = null;
            }

            SetSatanDissolveAmount(0f);
            DisableSatanDissolve();
            if (_satanGuessBrandRenderer != null)
            {
                SetSatanGuessBrandDissolveAmount(0f);
                DisableSatanGuessBrandDissolve();
                _satanGuessBrandRenderer.sprite = null;
                _satanGuessBrandRenderer.gameObject.SetActive(false);
            }

            SetFaceObjects(_showingFrontFace);
        }

        private void ConfigureSatanGuessBrand(Sprite brandSprite)
        {
            EnsureSatanGuessBrandRenderer();
            if (_satanGuessBrandRenderer == null)
            {
                return;
            }

            EnsureSatanGuessBrandMaterial();
            _satanGuessBrandRenderer.sprite = brandSprite;
            if (_satanGuessBrandMaterial != null)
            {
                _satanGuessBrandRenderer.sharedMaterial = _satanGuessBrandMaterial;
            }
            _satanGuessBrandRenderer.transform.localScale =
                Vector3.one * satanGuessBrandScale;
            Transform faceTransform = _showingFrontFace
                ? front?.transform
                : back?.transform;
            if (faceTransform != null)
            {
                Transform brandTransform = _satanGuessBrandRenderer.transform;
                brandTransform.SetParent(faceTransform, false);
                brandTransform.localPosition = new Vector3(0f, 0f, -0.003f);
                brandTransform.localRotation = Quaternion.identity;
            }

            SpriteRenderer faceRenderer = _showingFrontFace
                ? FrontSpriteRenderer()
                : BackSpriteRenderer();
            _satanGuessBrandRenderer.sortingOrder =
                faceRenderer != null
                    ? faceRenderer.sortingOrder + 1
                    : 1;
            _satanGuessBrandRenderer.gameObject.SetActive(brandSprite != null);
        }

        private void EnsureSatanGuessBrandRenderer()
        {
            if (_satanGuessBrandRenderer != null)
            {
                return;
            }

            Transform faceTransform = _showingFrontFace
                ? front?.transform
                : back?.transform;
            if (faceTransform == null)
            {
                return;
            }

            GameObject brandObject = new GameObject("SatanGuessBrand");
            Transform brandTransform = brandObject.transform;
            brandTransform.SetParent(faceTransform, false);
            brandTransform.localPosition = new Vector3(0f, 0f, -0.003f);
            brandTransform.localRotation = Quaternion.identity;
            brandTransform.localScale = Vector3.one * satanGuessBrandScale;
            _satanGuessBrandRenderer =
                brandObject.AddComponent<SpriteRenderer>();
            _satanGuessBrandRenderer.gameObject.SetActive(false);
        }

        private void EnsureSatanGuessBrandMaterial()
        {
            if (_satanGuessBrandMaterial != null)
            {
                return;
            }

            Material source = satanGuessBrandMaterial;
            if (source == null)
            {
                Renderer backRenderer = BackRenderer();
                EnsureCardMaterialInstance(backRenderer);
                source = backRenderer?.sharedMaterial;
            }

            if (source == null)
            {
                return;
            }

            _satanGuessBrandMaterial = new Material(source)
            {
                name = source.name + " (Satan Guess Brand Instance)"
            };
            if (_satanGuessBrandMaterial.HasProperty(BaseColorId))
            {
                _satanGuessBrandMaterial.SetColor(BaseColorId, Color.white);
            }

            if (_satanGuessBrandMaterial.HasProperty(DissolveEdgeColorId))
            {
                _satanGuessBrandMaterial.SetColor(
                    DissolveEdgeColorId,
                    Color.white);
            }

            if (satanGuessDissolveNoiseMap != null &&
                _satanGuessBrandMaterial.HasProperty(DissolveNoiseMapId))
            {
                _satanGuessBrandMaterial.SetTexture(
                    DissolveNoiseMapId,
                    satanGuessDissolveNoiseMap);
            }
        }

        private void EnableSatanGuessBrandDissolve()
        {
            EnsureSatanGuessBrandMaterial();
            if (_satanGuessBrandMaterial == null)
            {
                return;
            }

            _satanGuessBrandMaterial.EnableKeyword(DissolveKeyword);
            if (_satanGuessBrandMaterial.HasProperty(DissolveEnabledId))
            {
                _satanGuessBrandMaterial.SetFloat(DissolveEnabledId, 1f);
            }

            if (satanGuessDissolveNoiseMap != null &&
                _satanGuessBrandMaterial.HasProperty(DissolveNoiseMapId))
            {
                _satanGuessBrandMaterial.SetTexture(
                    DissolveNoiseMapId,
                    satanGuessDissolveNoiseMap);
            }

            SetSatanGuessBrandDissolveAmount(1f);
        }

        private void SetSatanGuessBrandDissolveAmount(float amount)
        {
            if (_satanGuessBrandMaterial == null ||
                !_satanGuessBrandMaterial.HasProperty(DissolveAmountId))
            {
                return;
            }

            _satanGuessBrandMaterial.SetFloat(
                DissolveAmountId,
                Mathf.Clamp01(amount));
        }

        private void DisableSatanGuessBrandDissolve()
        {
            if (_satanGuessBrandMaterial == null)
            {
                return;
            }

            _satanGuessBrandMaterial.DisableKeyword(DissolveKeyword);
            if (_satanGuessBrandMaterial.HasProperty(DissolveEnabledId))
            {
                _satanGuessBrandMaterial.SetFloat(DissolveEnabledId, 0f);
            }
        }

        private void EnableSatanDissolve()
        {
            Renderer renderer = BackRenderer();
            EnsureCardMaterialInstance(renderer);
            Material material = renderer?.sharedMaterial;
            if (material == null)
            {
                return;
            }

            material.EnableKeyword(DissolveKeyword);
            if (material.HasProperty(DissolveEnabledId))
            {
                material.SetFloat(DissolveEnabledId, 1f);
            }

            if (satanGuessDissolveNoiseMap != null &&
                material.HasProperty(DissolveNoiseMapId))
            {
                material.SetTexture(
                    DissolveNoiseMapId,
                    satanGuessDissolveNoiseMap);
            }

            SetSatanDissolveAmount(0f);
        }

        private void SetSatanDissolveAmount(float amount)
        {
            Renderer renderer = BackRenderer();
            EnsureCardMaterialInstance(renderer);
            Material material = renderer?.sharedMaterial;
            if (material == null || !material.HasProperty(DissolveAmountId))
            {
                return;
            }

            material.SetFloat(DissolveAmountId, Mathf.Clamp01(amount));
        }

        private void DisableSatanDissolve()
        {
            Renderer renderer = BackRenderer();
            EnsureCardMaterialInstance(renderer);
            Material material = renderer?.sharedMaterial;
            if (material == null)
            {
                return;
            }

            material.DisableKeyword(DissolveKeyword);
            if (material.HasProperty(DissolveEnabledId))
            {
                material.SetFloat(DissolveEnabledId, 0f);
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
            _scaleTween = HoverScaleTarget().DOScale(targetScale, duration)
                .SetEase(hoverScaleCurve)
                .SetTarget(this);
        }

        private Transform HoverScaleTarget()
        {
            return _useHandHoverVisual && hoverVisualRoot != null
                ? hoverVisualRoot
                : transform;
        }

        private Vector3 HoverRestingScale()
        {
            return _useHandHoverVisual && hoverVisualRoot != null
                ? _hoverVisualBaseScale
                : _baseScale;
        }

        private void ResetHoverScales()
        {
            transform.localScale = _baseScale;
            ResetHoverVisualScale();
        }

        private void ResetHoverVisualScale()
        {
            if (hoverVisualRoot != null)
            {
                hoverVisualRoot.localScale = _hoverVisualBaseScale;
            }
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

            EnsureCardMaterialInstance(renderer);
            EnablePixelOutlineKeyword(renderer);

            Color outlineColor = _hovered || _comparisonHighlighted
                ? ResolveHoverOutlineColor()
                : ResolveEffectHighlightOutlineColor(renderer);
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
            SetOutlineMaterialProperties(
                renderer.sharedMaterial,
                outlineColor,
                ResolveOutlineWidth(renderer),
                ResolveOutlineAlphaThreshold(renderer),
                visible ? 1f : 0f);
            renderer.SetPropertyBlock(propertyBlock);
        }

        private Color ResolveEffectHighlightOutlineColor(Renderer renderer)
        {
            if (renderer == _frontSpriteRenderer &&
                _frontEffectHighlightColorCaptured)
            {
                return _frontEffectHighlightColor;
            }

            if (renderer == _backSpriteRenderer &&
                _backEffectHighlightColorCaptured)
            {
                return _backEffectHighlightColor;
            }

            return effectHighlightOutlineColor;
        }

        private Color ResolveHoverOutlineColor()
        {
            Color outlineColor;
            switch (_hoverOutlineState)
            {
                case GameSceneCardHoverOutlineState.ManualUnavailable:
                    outlineColor = unavailableHoverOutlineColor;
                    break;
                case GameSceneCardHoverOutlineState.ManualAvailable:
                    outlineColor = availableHoverOutlineColor;
                    break;
                case GameSceneCardHoverOutlineState.Automatic:
                    outlineColor = automaticHoverOutlineColor;
                    break;
                case GameSceneCardHoverOutlineState.Used:
                    outlineColor = usedHoverOutlineColor;
                    break;
                default:
                    outlineColor = basicHoverOutlineColor;
                    break;
            }

            if (!_matchSelectionHoverGlowIntensity)
            {
                return outlineColor;
            }

            return MatchRgbPeak(outlineColor, basicHoverOutlineColor);
        }

        private static Color MatchRgbPeak(Color source, Color target)
        {
            float sourcePeak = Mathf.Max(
                source.r,
                Mathf.Max(source.g, source.b));
            float targetPeak = Mathf.Max(
                target.r,
                Mathf.Max(target.g, target.b));
            if (sourcePeak <= 0f || targetPeak <= 0f)
            {
                return source;
            }

            float scale = targetPeak / sourcePeak;
            return new Color(
                source.r * scale,
                source.g * scale,
                source.b * scale,
                source.a);
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

        private static void ApplyUnlitPresentation(Renderer renderer)
        {
            if (renderer == null)
            {
                return;
            }

            Material material = renderer.sharedMaterial;
            if (material != null && material.HasProperty(LightingModeId))
            {
                material.SetFloat(LightingModeId, 1f);
                material.EnableKeyword(UnlitKeyword);
            }

            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
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

        private void EnsureCardMaterialInstances()
        {
            EnsureCardMaterialInstance(FrontSpriteRenderer());
            EnsureCardMaterialInstance(BackSpriteRenderer());
        }

        private void EnsureCardMaterialInstance(Renderer renderer)
        {
            if (renderer == null)
            {
                return;
            }

            if (renderer == _frontSpriteRenderer)
            {
                CreateMaterialInstance(
                    renderer,
                    ref _cardFrontMaterial,
                    "Card Front Instance");
                CaptureEffectHighlightColor(
                    renderer,
                    ref _frontEffectHighlightColor,
                    ref _frontEffectHighlightColorCaptured);
                RefreshMaterialSpriteUv(renderer);
                return;
            }

            if (renderer == _backSpriteRenderer)
            {
                CreateMaterialInstance(
                    renderer,
                    ref _cardBackMaterial,
                    "Card Back Instance");
                CaptureEffectHighlightColor(
                    renderer,
                    ref _backEffectHighlightColor,
                    ref _backEffectHighlightColorCaptured);
                RefreshMaterialSpriteUv(renderer);
            }
        }

        private void CaptureEffectHighlightColor(
            Renderer renderer,
            ref Color capturedColor,
            ref bool captured)
        {
            if (captured)
            {
                return;
            }

            Material material = renderer.sharedMaterial;
            capturedColor = useMaterialHoverOutlineSettings &&
                material != null &&
                material.HasProperty(PixelOutlineColorId)
                    ? material.GetColor(PixelOutlineColorId)
                    : effectHighlightOutlineColor;
            captured = true;
        }

        private static void RefreshMaterialSpriteUv(Renderer renderer)
        {
            SpriteRenderer spriteRenderer = renderer as SpriteRenderer;
            if (spriteRenderer == null)
            {
                return;
            }

            SetVectorIfPresent(
                renderer.sharedMaterial,
                BaseSpriteUvRectId,
                GetSpriteUvRect(spriteRenderer.sprite));
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
            EnsureCardMaterialInstance(renderer);
            Material blendMaterial = renderer.sharedMaterial;
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

            EnsureCardMaterialInstance(renderer);
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
            SetVectorIfPresent(renderer.sharedMaterial, BaseSpriteUvRectId, uvRect);
            renderer.SetPropertyBlock(propertyBlock);
        }

        private static void SetOutlineMaterialProperties(
            Material material,
            Color color,
            float width,
            float alphaThreshold,
            float visibility)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty(PixelOutlineColorId))
            {
                material.SetColor(PixelOutlineColorId, color);
            }

            if (material.HasProperty(PixelOutlineWidthId))
            {
                material.SetFloat(PixelOutlineWidthId, width);
            }

            if (material.HasProperty(PixelOutlineAlphaThresholdId))
            {
                material.SetFloat(PixelOutlineAlphaThresholdId, alphaThreshold);
            }

            if (material.HasProperty(PixelOutlineVisibilityId))
            {
                material.SetFloat(PixelOutlineVisibilityId, visibility);
            }
        }

        private static void SetVectorIfPresent(
            Material material,
            int propertyId,
            Vector4 value)
        {
            if (material != null && material.HasProperty(propertyId))
            {
                material.SetVector(propertyId, value);
            }
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
