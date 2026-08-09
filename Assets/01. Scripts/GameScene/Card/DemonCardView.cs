using Border.Audio;
using DiaBlackJack.Content;
using DiaBlackJack.CoreLoop;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.VFX;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    public sealed class DemonCardView : MonoBehaviour
    {
        [SerializeField] private GameObject front;
        [SerializeField] private GameObject back;
        [SerializeField] private TMP_Text englishNameText;
        [SerializeField] private CardContentCatalogSO cardContentCatalog;

        [Header("Satan attack")]
        [SerializeField] private VisualEffect satanAttack;
        [Min(0.01f)]
        [SerializeField] private float satanAttackTravelDuration = 0.75f;
        [Tooltip("Normalized speed curve used while the Satan attack travels from the card to the target deck.")]
        [SerializeField] private AnimationCurve satanAttackTravelCurve =
            DefaultSatanAttackTravelCurve();
        [Min(0f)]
        [SerializeField] private float satanAttackArcHeight = 1.2f;
        [Min(0f)]
        [SerializeField] private float satanAttackReturnDelay = 1f;

        [Header("Hover badge anchor")]
        [Tooltip("World-space anchor projected to the HUD while this demon card is hovered.")]
        [SerializeField] private Transform topPosition;

        [Header("Hover feel")]
        [Tooltip("Visual-only pivot used by hand and selection cards so hover feedback does not move or resize the collider.")]
        [SerializeField] private Transform hoverVisualRoot;
        [SerializeField] private float hoverScale = 1.02f;
        [Min(0.01f)]
        [SerializeField] private float hoverScaleDuration = 0.08f;
        [SerializeField] private AnimationCurve hoverScaleCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private string hoverSfxId = "cardHover";

        [Header("Hover outline")]
        [ColorUsage(true, true)]
        [SerializeField] private Color hoverOutlineColor =
            new Color(3.9999995f, 3.3765495f, 0f, 0.5f);
        [SerializeField] private float hoverOutlineWidth = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float hoverOutlineAlphaThreshold = 0.5f;

        [Header("Round result reveal")]
        [Min(0.01f)]
        [SerializeField] private float revealFlipDuration = 0.7f;
        [Tooltip("Normalized time in the reveal duration when the visible side changes. 0.5 means halfway through.")]
        [Range(0f, 1f)]
        [SerializeField] private float revealFaceSwapNormalizedTime = 0.35f;
        [SerializeField] private bool revealFlipXOnFaceSwap = true;
        [Tooltip("Temporary local-position offset applied during the reveal flip. Curves decide how much of each axis is applied over the total duration.")]
        [SerializeField] private Vector3 revealPositionOffset =
            new Vector3(0.5f, 0f, 0.4f);
        [SerializeField] private AnimationCurve revealPositionXCurve =
            DefaultRevealOffsetCurve();
        [SerializeField] private AnimationCurve revealPositionYCurve =
            DefaultRevealOffsetCurve();
        [SerializeField] private AnimationCurve revealPositionZCurve =
            DefaultRevealOffsetCurve();
        [Tooltip("Temporary local-euler offset applied during the reveal flip. Curves decide how much of each axis is applied over the total duration.")]
        [SerializeField] private Vector3 revealRotationOffset =
            new Vector3(0f, 180f, 0f);
        [SerializeField] private AnimationCurve revealRotationXCurve =
            DefaultRevealOffsetCurve();
        [SerializeField] private AnimationCurve revealRotationYCurve =
            DefaultRevealOffsetCurve();
        [SerializeField] private AnimationCurve revealRotationZCurve =
            DefaultRevealOffsetCurve();

        [Header("Satan orientation")]
        [SerializeField] private float upsideDownTurnDuration = 0.35f;
        [Header("Satan doom counter")]
        [SerializeField] private TMP_Text satanDoomCountText;
        [SerializeField] private float satanDoomCountTilt = -4f;
        [SerializeField] private Color satanDoomCountColor =
            new Color(0.68f, 0.015f, 0.025f, 1f);
        [SerializeField] private Color satanDoomOutlineColor =
            new Color(0.12f, 0f, 0f, 0.95f);
        [Header("Shop sold out")]
        [Tooltip("Authored card-local price and sold-out display. Detached beside the card in shops.")]
        [SerializeField] private ShopCardOfferStatusView shopOfferStatus;
        [SerializeField] private Color shopSoldOutTint =
            new Color(0.35f, 0.35f, 0.35f, 1f);
        private static readonly int LightingModeId =
            Shader.PropertyToID("_LightingMode");
        private static readonly int BrightnessId =
            Shader.PropertyToID("_Brightness");
        private static readonly int PixelOutlineVisibilityId =
            Shader.PropertyToID("_PixelOutlineVisibility");
        private static readonly int PixelOutlineColorId =
            Shader.PropertyToID("_PixelOutlineColor");
        private static readonly int PixelOutlineWidthId =
            Shader.PropertyToID("_PixelOutlineWidth");
        private static readonly int PixelOutlineAlphaThresholdId =
            Shader.PropertyToID("_PixelOutlineAlphaThreshold");
        private static readonly int SpriteFlipXId =
            Shader.PropertyToID("_SpriteFlipX");
        private static readonly int BaseSpriteUvRectId =
            Shader.PropertyToID("_BaseSpriteUVRect");
        private const string UnlitKeyword = "_UNLIT_ON";
        private const string PixelOutlineKeyword = "_PIXEL_OUTLINE_ON";
        private const string UnderlayKeyword = "UNDERLAY_ON";
        private const string FlipSfxId = "flipCard";
        private const string SatanAttackPlayEvent = "OnPlay";
        private const string SatanAttackStopBaseEvent = "OnStopBase";
        private const string SatanAttackSparkEvent = "OnSpark";
        private const string SatanAttackTravelSfxId = "fireWoosh";
        private const string SatanAttackImpactSfxId = "explosion";
        private const float SatanDoomOutlineWidthValue = 0.22f;
        private const float SatanDoomCountFontSizeValue = 11f;
        private const float SatanDoomCountScaleValue = 0.4f;
        private static readonly Vector3 SatanDoomCountOffset =
            new Vector3(0f, 0.12f, 0.002f);
        private static readonly int TextOutlineColorId =
            Shader.PropertyToID("_OutlineColor");
        private static readonly int TextOutlineWidthId =
            Shader.PropertyToID("_OutlineWidth");
        private static readonly int TextUnderlayColorId =
            Shader.PropertyToID("_UnderlayColor");
        private static readonly int TextUnderlayOffsetXId =
            Shader.PropertyToID("_UnderlayOffsetX");
        private static readonly int TextUnderlayOffsetYId =
            Shader.PropertyToID("_UnderlayOffsetY");
        private static readonly int TextUnderlayDilateId =
            Shader.PropertyToID("_UnderlayDilate");

        private SpriteRenderer _frontSpriteRenderer;
        private SpriteRenderer _backSpriteRenderer;
        private Sprite _trackedFrontSprite;
        private Sprite _trackedBackSprite;
        private MaterialPropertyBlock _frontPropertyBlock;
        private MaterialPropertyBlock _backPropertyBlock;
        private Material _presentationFrontMaterial;
        private Material _presentationBackMaterial;
        private Material _hoverFrontMaterial;
        private Material _hoverBackMaterial;
        private Material _shopMaterial;
        private Material _satanDoomMaterial;
        private Vector3 _baseScale = Vector3.one;
        private Vector3 _hoverVisualBaseScale = Vector3.one;
        private Vector3 _targetScale = Vector3.one;
        private Vector3 _revealBaseLocalPosition;
        private Quaternion _revealBaseLocalRotation;
        private bool _hasRevealBaseTransform;
        private bool _hasRevealRendererFlipState;
        private GameSceneDemonCardViewModel _pendingRevealFaceCard;
        private bool _revealFaceSwapped;
        private bool _showingFrontFace = true;
        private bool _showBadgeOnHover;
        private bool _hovered;
        private bool _isEffectSource;
        private bool _useHandHoverVisual;
        private bool _isShopSoldOut;
        private bool _shopColorCaptured;
        private Color _shopFrontColor = Color.white;
        private Sequence _revealSequence;
        private Sequence _satanAttackSequence;
        private Tween _scaleTween;
        private Tween _orientationTween;
        private bool _orientationInitialized;
        private bool _isUpsideDown;
        private bool _hasBoundCard;
        private Transform _satanAttackOriginalParent;
        private Vector3 _satanAttackOriginalLocalPosition;
        private Quaternion _satanAttackOriginalLocalRotation;
        private Vector3 _satanAttackOriginalLocalScale;
        private bool _hasSatanAttackOrigin;

        public int CardId { get; private set; } = -1;

        internal float RevealFlipDuration => revealFlipDuration;

        internal bool IsSatanAttackAnimationPlaying =>
            _satanAttackSequence != null;

        public bool CanUse { get; private set; }

        internal bool IsShopSoldOut => _isShopSoldOut;

        internal bool IsSatanDoomCountVisible =>
            satanDoomCountText != null && satanDoomCountText.gameObject.activeSelf;

        internal string SatanDoomCountLabel =>
            satanDoomCountText != null ? satanDoomCountText.text : string.Empty;

        internal Color SatanDoomCountTextColor =>
            satanDoomCountText != null ? satanDoomCountText.color : Color.clear;

        internal float SatanDoomCountOutlineWidth =>
            satanDoomCountText != null ? SatanDoomOutlineWidthValue : 0f;

        internal float SatanDoomCountFontSize =>
            satanDoomCountText != null ? satanDoomCountText.fontSize : 0f;

        internal Vector3 SatanDoomCountLocalScale =>
            satanDoomCountText != null
                ? satanDoomCountText.rectTransform.localScale
                : Vector3.zero;

        internal Vector2 SatanDoomCountRectSize =>
            satanDoomCountText != null
                ? satanDoomCountText.rectTransform.sizeDelta
                : Vector2.zero;

        internal Vector3 SatanDoomCountLocalPosition =>
            satanDoomCountText != null
                ? satanDoomCountText.rectTransform.localPosition
                : Vector3.zero;

        internal Vector3 SatanDoomCountLocalEulerAngles =>
            satanDoomCountText != null
                ? satanDoomCountText.rectTransform.localEulerAngles
                : Vector3.zero;

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

        internal GameSceneDemonCardViewModel BoundCard { get; private set; }

        public string HoverBadgeTitle { get; private set; } = string.Empty;

        public string HoverBadgeDescription { get; private set; } = string.Empty;

        public string HoverBadgeText => string.IsNullOrEmpty(HoverBadgeDescription)
            ? HoverBadgeTitle
            : $"{HoverBadgeTitle}\n{HoverBadgeDescription}";

        public bool ShouldShowHoverBadge =>
            !_isShopSoldOut &&
            _hovered &&
            _showBadgeOnHover &&
            !string.IsNullOrEmpty(HoverBadgeTitle);

        private void Awake()
        {
            EnsureSatanAttackEffect();
            RestoreSatanAttackEffect();
            EnsureSatanDoomCountText();
            if (shopOfferStatus != null)
            {
                shopOfferStatus.gameObject.SetActive(false);
            }

            _baseScale = transform.localScale;
            if (hoverVisualRoot != null)
            {
                _hoverVisualBaseScale = hoverVisualRoot.localScale;
            }
            _targetScale = HoverRestingScale();
            RefreshSpriteUvRects();
        }

        private void OnValidate()
        {
            revealFlipDuration = Mathf.Max(revealFlipDuration, 0.01f);
            hoverScaleDuration = Mathf.Max(hoverScaleDuration, 0.01f);
            revealFaceSwapNormalizedTime =
                Mathf.Clamp01(revealFaceSwapNormalizedTime);
            upsideDownTurnDuration = Mathf.Max(upsideDownTurnDuration, 0f);
            satanAttackTravelDuration =
                Mathf.Max(satanAttackTravelDuration, 0.01f);
            satanAttackTravelCurve = EnsureCurve(
                satanAttackTravelCurve,
                DefaultSatanAttackTravelCurve());
            satanAttackArcHeight = Mathf.Max(satanAttackArcHeight, 0f);
            satanAttackReturnDelay = Mathf.Max(satanAttackReturnDelay, 0f);
            EnsureRevealCurves();
        }

        private void Update()
        {
            RefreshSpriteUvRects();
        }

        private void LateUpdate()
        {
            AlignSatanDoomCount();
        }

        private void OnDisable()
        {
            StopSatanAttackAnimation();
            StopRevealSequence();
            _orientationTween?.Kill();
            _orientationTween = null;
            _hovered = false;
            _isEffectSource = false;
            ApplyHoverOutline(false);
            ResetHoverScales();
            _targetScale = HoverRestingScale();
        }

        private void OnDestroy()
        {
            StopSatanAttackAnimation();
            _revealSequence?.Kill();
            _scaleTween?.Kill();
            _orientationTween?.Kill();
            DestroyOwnedMaterial(_presentationFrontMaterial);
            DestroyOwnedMaterial(_presentationBackMaterial);
            DestroyOwnedMaterial(_hoverFrontMaterial);
            DestroyOwnedMaterial(_hoverBackMaterial);
            DestroyOwnedMaterial(_shopMaterial);
            DestroyOwnedMaterial(_satanDoomMaterial);
        }

        internal bool PlaySatanAttackAnimation(Vector3 targetPosition)
        {
            if (!Application.isPlaying || !isActiveAndEnabled)
            {
                return false;
            }

            EnsureSatanAttackEffect();
            if (satanAttack == null)
            {
                return false;
            }

            StopSatanAttackAnimation();

            Vector3 startPosition = transform.position;
            Vector3 controlPosition = Vector3.Lerp(
                startPosition,
                targetPosition,
                0.5f);
            controlPosition.y += satanAttackArcHeight;

            Transform effectTransform = satanAttack.transform;
            effectTransform.SetParent(null, worldPositionStays: true);
            effectTransform.position = startPosition;
            satanAttack.gameObject.SetActive(true);
            satanAttack.Reinit();
            satanAttack.SendEvent(SatanAttackPlayEvent);
            SoundManager.Current?.PlaySfx(SatanAttackTravelSfxId);

            float duration = Mathf.Max(satanAttackTravelDuration, 0.01f);
            AnimationCurve travelCurve = EnsureCurve(
                satanAttackTravelCurve,
                DefaultSatanAttackTravelCurve());
            Sequence sequence = null;
            sequence = DOTween.Sequence()
                .SetTarget(this)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            sequence.Append(
                DOVirtual.Float(
                        0f,
                        1f,
                        duration,
                        normalizedTime =>
                        {
                            effectTransform.position = EvaluateQuadraticBezier(
                                startPosition,
                                controlPosition,
                                targetPosition,
                                normalizedTime);
                        })
                    .SetEase(travelCurve));
            sequence.AppendCallback(() =>
            {
                satanAttack.SendEvent(SatanAttackStopBaseEvent);
                satanAttack.SendEvent(SatanAttackSparkEvent);
                SoundManager.Current?.PlaySfx(SatanAttackImpactSfxId);
            });
            sequence.AppendInterval(Mathf.Max(satanAttackReturnDelay, 0f));
            sequence.OnComplete(() => FinishSatanAttackAnimation(sequence));
            sequence.OnKill(() =>
            {
                if (_satanAttackSequence != sequence)
                {
                    return;
                }

                _satanAttackSequence = null;
                RestoreSatanAttackEffect();
            });
            _satanAttackSequence = sequence;
            return true;
        }

        private void EnsureSatanAttackEffect()
        {
            if (satanAttack == null)
            {
                VisualEffect[] effects =
                    GetComponentsInChildren<VisualEffect>(includeInactive: true);
                for (int i = 0; i < effects.Length; i++)
                {
                    VisualEffect candidate = effects[i];
                    if (candidate != null &&
                        candidate.gameObject.name == "SatanAttack")
                    {
                        satanAttack = candidate;
                        break;
                    }
                }
            }

            if (satanAttack == null || _hasSatanAttackOrigin)
            {
                return;
            }

            Transform effect = satanAttack.transform;
            _satanAttackOriginalParent = effect.parent;
            _satanAttackOriginalLocalPosition = effect.localPosition;
            _satanAttackOriginalLocalRotation = effect.localRotation;
            _satanAttackOriginalLocalScale = effect.localScale;
            _hasSatanAttackOrigin = true;
        }

        private void StopSatanAttackAnimation()
        {
            Sequence sequence = _satanAttackSequence;
            _satanAttackSequence = null;
            sequence?.Kill();
            RestoreSatanAttackEffect();
        }

        private void FinishSatanAttackAnimation(Sequence sequence)
        {
            if (_satanAttackSequence != sequence)
            {
                return;
            }

            _satanAttackSequence = null;
            RestoreSatanAttackEffect();
        }

        private void RestoreSatanAttackEffect()
        {
            if (satanAttack == null)
            {
                return;
            }

            satanAttack.Stop();
            if (_hasSatanAttackOrigin)
            {
                Transform effect = satanAttack.transform;
                effect.SetParent(
                    _satanAttackOriginalParent,
                    worldPositionStays: false);
                effect.localPosition = _satanAttackOriginalLocalPosition;
                effect.localRotation = _satanAttackOriginalLocalRotation;
                effect.localScale = _satanAttackOriginalLocalScale;
            }

            satanAttack.gameObject.SetActive(false);
        }

        private static Vector3 EvaluateQuadraticBezier(
            Vector3 start,
            Vector3 control,
            Vector3 end,
            float normalizedTime)
        {
            float inverseTime = 1f - normalizedTime;
            return inverseTime * inverseTime * start +
                2f * inverseTime * normalizedTime * control +
                normalizedTime * normalizedTime * end;
        }

        public void Bind(GameSceneDemonCardViewModel card)
        {
            Bind(card, showTransientEffectSource: true);
        }

        internal void Bind(
            GameSceneDemonCardViewModel card,
            bool showTransientEffectSource)
        {
            if (card == null)
            {
                return;
            }

            bool sameCard = _hasBoundCard && CardId == card.CardId;
            bool wasEmphasized = _hovered || _isEffectSource;
            Transform scaleTarget = HoverScaleTarget();
            Vector3 preservedScale = scaleTarget.localScale;
            bool isEffectSource = card.IsEffectSource &&
                (card.IsEffectSourcePersistent || showTransientEffectSource);
            bool preserveCurrentScale = sameCard &&
                (wasEmphasized || isEffectSource);
            bool animateReveal = Application.isPlaying &&
                _hasBoundCard &&
                CardId == card.CardId &&
                !_showingFrontFace &&
                card.IsFaceUp;

            StopRevealSequence();
            _pendingRevealFaceCard = null;

            bool animateOrientation = _orientationInitialized &&
                CardId == card.CardId &&
                _isUpsideDown != card.IsUpsideDown &&
                Application.isPlaying;
            CardId = card.CardId;
            BoundCard = card;
            CanUse = card.CanUse;
            _isEffectSource = isEffectSource;
            _showingFrontFace = card.IsFaceUp;
            _showBadgeOnHover = CanUse || card.ShowHoverBadgeWhenUnavailable;
            _hasBoundCard = true;

            if (front != null)
            {
                front.SetActive(animateReveal ? false : _showingFrontFace);
            }

            if (_showingFrontFace)
            {
                if (animateReveal)
                {
                    _pendingRevealFaceCard = card;
                }
                else
                {
                    ApplyFaceSprite(card.DefinitionKey);
                }
            }

            if (back != null)
            {
                back.SetActive(animateReveal || !_showingFrontFace);
            }

            string englishName = card.DefinitionKey.ToUpperInvariant();
            if (englishNameText != null)
            {
                englishNameText.text = _showingFrontFace && !animateReveal
                    ? englishName
                    : string.Empty;
                englishNameText.gameObject.SetActive(
                    _showingFrontFace && !animateReveal);
            }

            UpdateSatanDoomCount(card, !animateReveal);

            HoverBadgeTitle = !card.IsFaceUp ? string.Empty : englishName;
            HoverBadgeDescription = !card.IsFaceUp
                ? string.Empty
                : FormatBadgeDescription(card);

            _hovered = false;
            ApplyHoverOutline(false);
            ResetHoverScales();
            if (preserveCurrentScale)
            {
                HoverScaleTarget().localScale = preservedScale;
            }

            Vector3 restingScale = HoverRestingScale();
            _targetScale = _isEffectSource
                ? restingScale * hoverScale
                : restingScale;
            PlayHoverScaleTween(_targetScale);
            ApplyOrientation(card.IsUpsideDown, animateOrientation);
            AlignSatanDoomCount();

            if (animateReveal)
            {
                PlayRevealFlip();
            }
        }

        private void ApplyOrientation(bool isUpsideDown, bool animate)
        {
            _orientationTween?.Kill();
            _orientationTween = null;
            _isUpsideDown = isUpsideDown;
            _orientationInitialized = true;

            if (!animate || upsideDownTurnDuration <= 0f)
            {
                Vector3 euler = transform.localEulerAngles;
                euler.z = isUpsideDown ? 180f : 0f;
                transform.localEulerAngles = euler;
                return;
            }

            Vector3 target = transform.localEulerAngles;
            target.z += 180f;
            _orientationTween = transform
                .DOLocalRotate(
                    target,
                    upsideDownTurnDuration,
                    RotateMode.FastBeyond360)
                .SetEase(Ease.InOutQuad)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                .OnComplete(() =>
                {
                    if (this == null)
                    {
                        return;
                    }

                    Vector3 settled = transform.localEulerAngles;
                    settled.z = _isUpsideDown ? 180f : 0f;
                    transform.localEulerAngles = settled;
                    _orientationTween = null;
                });
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
            ResetHoverScales();
            _hovered = false;
            ApplyHoverOutline(false);
            PlayRevealFlip();
        }
#endif

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

        private void SetShaderFlipX(
            SpriteRenderer renderer,
            bool flipX)
        {
            if (renderer == null)
            {
                return;
            }

            // SpriteRenderer's mesh flip changes the card's physical orientation.
            renderer.flipX = false;

            var propertyBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(SpriteFlipXId, flipX ? 1f : 0f);
            renderer.SetPropertyBlock(propertyBlock);
        }

        private void PlayRevealFlip()
        {
            SoundManager.Current?.PlaySfx(FlipSfxId);
            _orientationTween?.Kill();
            _orientationTween = null;
            _hovered = false;
            ApplyHoverOutline(false);
            ResetHoverScales();
            SetFaceObjects(showFront: false);
            HideFaceDetails();

            EnsureRevealCurves();

            _revealBaseLocalPosition = transform.localPosition;
            _revealBaseLocalRotation = transform.localRotation;
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
                ResetHoverScales();
                SetFaceObjects(showFront: true);
                ApplyFaceDetails();
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
            ResetHoverScales();
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
            transform.localRotation =
                _revealBaseLocalRotation * Quaternion.Euler(rotationOffset);

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
                transform.localRotation = _revealBaseLocalRotation;
                _hasRevealBaseTransform = false;
            }

            RestoreRevealRendererFlipState();
        }

        private void SwapRevealFace()
        {
            _revealFaceSwapped = true;

            if (revealFlipXOnFaceSwap)
            {
                ApplyRevealRendererFlip(invert: true);
            }

            ApplyPendingRevealFace();
            SetFaceObjects(showFront: true);
            ApplyFaceDetails();
        }

        private void ApplyPendingRevealFace()
        {
            if (_pendingRevealFaceCard == null)
            {
                return;
            }

            ApplyFaceSprite(_pendingRevealFaceCard.DefinitionKey);
            _pendingRevealFaceCard = null;
        }

        private void CaptureRevealRendererFlipState()
        {
            _hasRevealRendererFlipState = true;
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

        private static AnimationCurve DefaultSatanAttackTravelCurve()
        {
            return AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        }

        private void HideFaceDetails()
        {
            if (englishNameText != null)
            {
                englishNameText.gameObject.SetActive(false);
            }

            if (satanDoomCountText != null)
            {
                satanDoomCountText.gameObject.SetActive(false);
            }
        }

        private void ApplyFaceDetails()
        {
            if (BoundCard == null || !BoundCard.IsFaceUp)
            {
                return;
            }

            if (englishNameText != null)
            {
                englishNameText.text = BoundCard.DefinitionKey.ToUpperInvariant();
                englishNameText.gameObject.SetActive(true);
            }

            UpdateSatanDoomCount(BoundCard, allowVisible: true);
        }

        public void SetHovered(bool hovered)
        {
            if (_revealSequence != null)
            {
                return;
            }

            if (_isShopSoldOut)
            {
                hovered = false;
            }

            if (_hovered == hovered)
            {
                return;
            }

            _hovered = hovered;
            PlayHoverSfx(hovered);
            Vector3 restingScale = HoverRestingScale();
            _targetScale = hovered || _isEffectSource
                ? restingScale * hoverScale
                : restingScale;
            PlayHoverScaleTween(_targetScale);
            ApplyHoverOutline(hovered);
        }

        internal Transform HoverVisualTransform =>
            hoverVisualRoot != null ? hoverVisualRoot : transform;

        internal void EnableHoverVisualOnly()
        {
            ResetHoverScales();
            _useHandHoverVisual = hoverVisualRoot != null;
            _hovered = false;
            ApplyHoverOutline(false);
            _targetScale = HoverRestingScale();
        }

        internal void EnableHandHoverVisualOnly()
        {
            EnableHoverVisualOnly();
        }

        /// <summary>
        /// Sets the resting scale used by hover feedback. World-space shop layouts call this after
        /// applying their authored transform so hover exit and sold-out refreshes restore that scale.
        /// </summary>
        internal void SetBaseScale(Vector3 baseScale)
        {
            _baseScale = baseScale;
            StopRevealSequence();
            _hovered = false;
            ApplyHoverOutline(false);
            ResetHoverScales();
            _targetScale = HoverRestingScale();
        }

        internal void SetUnlitPresentation()
        {
            CreateUnlitPresentationMaterial(
                FrontSpriteRenderer(),
                ref _presentationFrontMaterial,
                "Demon Card Front Unlit Instance");
            CreateUnlitPresentationMaterial(
                BackSpriteRenderer(),
                ref _presentationBackMaterial,
                "Demon Card Back Unlit Instance");
        }

        private Transform HoverScaleTarget()
        {
            return hoverVisualRoot != null
                ? hoverVisualRoot
                : transform;
        }

        private Vector3 HoverRestingScale()
        {
            return hoverVisualRoot != null
                ? _hoverVisualBaseScale
                : _baseScale;
        }

        private void ResetHoverScales()
        {
            StopScaleTween();
            transform.localScale = _baseScale;
            if (hoverVisualRoot != null)
            {
                hoverVisualRoot.localScale = _hoverVisualBaseScale;
            }
        }

        private void PlayHoverScaleTween(Vector3 targetScale)
        {
            StopScaleTween();
            _scaleTween = HoverScaleTarget()
                .DOScale(targetScale, Mathf.Max(hoverScaleDuration, 0.01f))
                .SetEase(hoverScaleCurve)
                .SetTarget(this)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnKill(() => _scaleTween = null);
        }

        private void StopScaleTween()
        {
            Tween tween = _scaleTween;
            _scaleTween = null;
            tween?.Kill();
        }

        private void ApplyHoverOutline(bool visible)
        {
            ApplyHoverOutline(FrontSpriteRenderer(), visible && _showingFrontFace);
            ApplyHoverOutline(BackSpriteRenderer(), visible && !_showingFrontFace);
        }

        private void ApplyHoverOutline(SpriteRenderer renderer, bool visible)
        {
            if (renderer == null)
            {
                return;
            }

            EnsureHoverMaterialInstance(renderer);
            Material material = renderer.sharedMaterial;
            if (material != null)
            {
                material.EnableKeyword(PixelOutlineKeyword);
            }

            MaterialPropertyBlock properties = PropertyBlockFor(renderer);
            renderer.GetPropertyBlock(properties);
            properties.SetColor(
                PixelOutlineColorId,
                GetMaterialColor(material, PixelOutlineColorId, hoverOutlineColor));
            properties.SetFloat(
                PixelOutlineWidthId,
                GetMaterialFloat(material, PixelOutlineWidthId, hoverOutlineWidth));
            properties.SetFloat(
                PixelOutlineAlphaThresholdId,
                GetMaterialFloat(
                    material,
                    PixelOutlineAlphaThresholdId,
                    hoverOutlineAlphaThreshold));
            properties.SetFloat(PixelOutlineVisibilityId, visible ? 1f : 0f);
            renderer.SetPropertyBlock(properties);
        }

        private void EnsureHoverMaterialInstance(SpriteRenderer renderer)
        {
            if (renderer == null || renderer.sharedMaterial == null ||
                IsOwnedMaterial(renderer.sharedMaterial))
            {
                return;
            }

            if (renderer == FrontSpriteRenderer())
            {
                _hoverFrontMaterial = new Material(renderer.sharedMaterial)
                {
                    name = renderer.sharedMaterial.name + " (Demon Hover Front Instance)"
                };
                renderer.sharedMaterial = _hoverFrontMaterial;
                return;
            }

            _hoverBackMaterial = new Material(renderer.sharedMaterial)
            {
                name = renderer.sharedMaterial.name + " (Demon Hover Back Instance)"
            };
            renderer.sharedMaterial = _hoverBackMaterial;
        }

        private bool IsOwnedMaterial(Material material)
        {
            return material == _presentationFrontMaterial ||
                material == _presentationBackMaterial ||
                material == _hoverFrontMaterial ||
                material == _hoverBackMaterial ||
                material == _shopMaterial;
        }

        private static Color GetMaterialColor(
            Material material,
            int propertyId,
            Color fallback)
        {
            return material != null && material.HasProperty(propertyId)
                ? material.GetColor(propertyId)
                : fallback;
        }

        private static float GetMaterialFloat(
            Material material,
            int propertyId,
            float fallback)
        {
            return material != null && material.HasProperty(propertyId)
                ? material.GetFloat(propertyId)
                : fallback;
        }

        internal void SetShopPresentation()
        {
            SpriteRenderer renderer = FrontSpriteRenderer();
            if (renderer == null)
            {
                return;
            }

            if (!_shopColorCaptured)
            {
                _shopFrontColor = renderer.color;
                _shopColorCaptured = true;
            }

            if (_shopMaterial == null)
            {
                Material source = renderer.sharedMaterial;
                if (source == null)
                {
                    return;
                }

                _shopMaterial = new Material(source)
                {
                    name = source.name + " (Shop Demon Instance)"
                };
                renderer.sharedMaterial = _shopMaterial;
            }

            _shopMaterial.SetFloat(LightingModeId, 1f);
            _shopMaterial.SetFloat(BrightnessId, 1f);
            _shopMaterial.EnableKeyword(UnlitKeyword);
            renderer.color = _shopFrontColor;
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
                _hovered = false;
                _isEffectSource = false;
                ApplyHoverOutline(false);
                ResetHoverScales();
                _targetScale = HoverRestingScale();
            }

            SpriteRenderer renderer = FrontSpriteRenderer();
            if (renderer == null)
            {
                return;
            }

            if (!_shopColorCaptured)
            {
                _shopFrontColor = renderer.color;
                _shopColorCaptured = true;
            }

            renderer.color = isSoldOut
                ? new Color(
                    _shopFrontColor.r * shopSoldOutTint.r,
                    _shopFrontColor.g * shopSoldOutTint.g,
                    _shopFrontColor.b * shopSoldOutTint.b,
                    _shopFrontColor.a * shopSoldOutTint.a)
                : _shopFrontColor;
        }

        internal void SetSortingOrder(int sortingOrder)
        {
            SpriteRenderer[] renderers =
                GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
            foreach (SpriteRenderer renderer in renderers)
            {
                renderer.sortingOrder = sortingOrder;
            }

            if (englishNameText != null && englishNameText.GetComponent<Renderer>() != null)
            {
                englishNameText.GetComponent<Renderer>().sortingOrder = sortingOrder + 1;
            }

            if (satanDoomCountText != null &&
                satanDoomCountText.GetComponent<Renderer>() != null)
            {
                satanDoomCountText.GetComponent<Renderer>().sortingOrder =
                    sortingOrder + 2;
            }
        }

        public Sprite GetFaceSprite(string definitionKey)
        {
            return cardContentCatalog == null
                ? null
                : cardContentCatalog.GetDemonFaceSprite(definitionKey);
        }

        public bool TryGetHoverBadgeScreenPosition(Camera camera, out Vector2 screenPosition)
        {
            screenPosition = default;
            if (camera == null || topPosition == null)
            {
                return false;
            }

            Vector3 projected = camera.WorldToScreenPoint(topPosition.position);
            if (projected.z <= 0f)
            {
                return false;
            }

            screenPosition = new Vector2(projected.x, projected.y);
            return true;
        }

        private static string FormatBadgeDescription(GameSceneDemonCardViewModel card)
        {
            string text = string.Empty;
            if (!string.IsNullOrEmpty(card.Summary))
            {
                text = card.Summary;
            }

            if (!string.IsNullOrEmpty(card.CostSummary))
            {
                text = string.IsNullOrEmpty(text)
                    ? card.CostSummary
                    : text + "\n" + card.CostSummary;
            }

            return text;
        }

        private void ApplyFaceSprite(string definitionKey)
        {
            SpriteRenderer renderer = FrontSpriteRenderer();
            Sprite sprite = GetFaceSprite(definitionKey);
            if (renderer == null || sprite == null)
            {
                return;
            }

            renderer.sprite = sprite;
            RefreshSpriteUvRect(renderer, ref _trackedFrontSprite);
        }

        private void PlayHoverSfx(bool hovered)
        {
            if (!hovered || string.IsNullOrWhiteSpace(hoverSfxId))
            {
                return;
            }

            SoundManager.Current?.PlaySfx(hoverSfxId);
        }

        private SpriteRenderer FrontSpriteRenderer()
        {
            if (_frontSpriteRenderer == null && front != null)
            {
                _frontSpriteRenderer = front.GetComponent<SpriteRenderer>();
            }

            return _frontSpriteRenderer;
        }

        private SpriteRenderer BackSpriteRenderer()
        {
            if (_backSpriteRenderer == null && back != null)
            {
                _backSpriteRenderer = back.GetComponent<SpriteRenderer>();
            }

            return _backSpriteRenderer;
        }

        private void RefreshSpriteUvRects()
        {
            RefreshSpriteUvRect(FrontSpriteRenderer(), ref _trackedFrontSprite);
            RefreshSpriteUvRect(BackSpriteRenderer(), ref _trackedBackSprite);
        }

        private void RefreshSpriteUvRect(
            SpriteRenderer renderer,
            ref Sprite trackedSprite)
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

        private static void CreateUnlitPresentationMaterial(
            SpriteRenderer renderer,
            ref Material materialInstance,
            string instanceName)
        {
            if (renderer == null)
            {
                return;
            }

            if (materialInstance == null)
            {
                Material source = renderer.sharedMaterial;
                if (source == null)
                {
                    return;
                }

                materialInstance = new Material(source)
                {
                    name = source.name + " (" + instanceName + ")"
                };
                renderer.sharedMaterial = materialInstance;
            }

            if (materialInstance.HasProperty(LightingModeId))
            {
                materialInstance.SetFloat(LightingModeId, 1f);
                materialInstance.EnableKeyword(UnlitKeyword);
            }

            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private void EnsureSatanDoomCountText()
        {
            if (satanDoomCountText != null || englishNameText == null)
            {
                return;
            }

            Transform parent = hoverVisualRoot != null
                ? hoverVisualRoot
                : transform;
            GameObject counter = Instantiate(
                englishNameText.gameObject,
                parent,
                worldPositionStays: false);
            counter.name = "SatanDoomCount";
            satanDoomCountText = counter.GetComponent<TMP_Text>();
            satanDoomCountText.text = string.Empty;
            satanDoomCountText.enableAutoSizing = false;
            satanDoomCountText.fontSize = SatanDoomCountFontSizeValue;
            satanDoomCountText.fontStyle = FontStyles.Bold;
            satanDoomCountText.characterSpacing = -5f;
            satanDoomCountText.color = satanDoomCountColor;
            satanDoomCountText.alignment = TextAlignmentOptions.Center;
            satanDoomCountText.raycastTarget = false;

            if (englishNameText.fontSharedMaterial != null)
            {
                _satanDoomMaterial = new Material(
                    englishNameText.fontSharedMaterial)
                {
                    name = "Satan Doom Count (Instance)"
                };
                _satanDoomMaterial.SetColor(
                    TextOutlineColorId,
                    satanDoomOutlineColor);
                _satanDoomMaterial.SetFloat(
                    TextOutlineWidthId,
                    SatanDoomOutlineWidthValue);
                _satanDoomMaterial.SetColor(
                    TextUnderlayColorId,
                    new Color(0.22f, 0f, 0f, 0.8f));
                _satanDoomMaterial.SetFloat(TextUnderlayOffsetXId, 0.12f);
                _satanDoomMaterial.SetFloat(TextUnderlayOffsetYId, -0.12f);
                _satanDoomMaterial.SetFloat(TextUnderlayDilateId, 0.18f);
                _satanDoomMaterial.EnableKeyword(UnderlayKeyword);
                satanDoomCountText.fontSharedMaterial = _satanDoomMaterial;
            }

            RectTransform rect = satanDoomCountText.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(1.6f, 1.1f);
            rect.localScale = new Vector3(
                SatanDoomCountScaleValue,
                SatanDoomCountScaleValue,
                1f);
            Renderer counterRenderer = satanDoomCountText.GetComponent<Renderer>();
            SpriteRenderer frontRenderer = FrontSpriteRenderer();
            if (counterRenderer != null && frontRenderer != null)
            {
                counterRenderer.sortingLayerID = frontRenderer.sortingLayerID;
                counterRenderer.sortingOrder = frontRenderer.sortingOrder + 2;
            }
            counter.SetActive(false);
        }

        private void UpdateSatanDoomCount(
            GameSceneDemonCardViewModel card,
            bool allowVisible = true)
        {
            EnsureSatanDoomCountText();
            if (satanDoomCountText == null)
            {
                return;
            }

            bool visible = allowVisible &&
                card.IsFaceUp &&
                string.Equals(
                    card.DefinitionKey,
                    DemonContractCatalog.SatanKey,
                    System.StringComparison.Ordinal) &&
                card.SatanDoomCount.HasValue;
            satanDoomCountText.gameObject.SetActive(visible);
            satanDoomCountText.text = visible
                ? card.SatanDoomCount.Value.ToString()
                : string.Empty;
        }

        internal void AlignSatanDoomCount()
        {
            if (satanDoomCountText == null ||
                !satanDoomCountText.gameObject.activeSelf)
            {
                return;
            }

            RectTransform rect = satanDoomCountText.rectTransform;
            rect.anchoredPosition3D = SatanDoomCountOffset;

            Vector3 euler = englishNameText != null
                ? englishNameText.rectTransform.localEulerAngles
                : Vector3.zero;
            euler.z = satanDoomCountTilt;
            rect.localEulerAngles = euler;
        }

        private static void DestroyOwnedMaterial(Material material)
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
    }
}
