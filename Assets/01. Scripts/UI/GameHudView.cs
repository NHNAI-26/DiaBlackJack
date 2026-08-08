using System;
using System.Collections.Generic;
using DG.Tweening;
using DiaBlackJack.Content;
using DiaBlackJack.CoreLoop;
using DiaBlackJack.CoreLoop.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiaBlackJack.GameScene
{
    /// <summary>
    /// Drives the scene-placed HUD text. The Canvas and the three <see cref="TMP_Text"/> labels are
    /// authored in the scene (player soul top-left, enemy soul top-right, round top-center); this
    /// only writes their <c>.text</c>. Serialized-text convention follows
    /// <c>Localization/UILocalizeText.cs</c>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameHudView : MonoBehaviour
    {
        [SerializeField] private TMP_Text playerSoulText;
        [SerializeField] private TMP_Text enemySoulText;
        [SerializeField] private TMP_Text roundText;
        [SerializeField] private TMP_Text goldText;

        [Header("Soul Restore Flourish")]
        [Tooltip("Color the player soul counter eases toward and back from on a soul restore (e.g. whiskey).")]
        [SerializeField] private Color soulRestoreFlashColor = new Color(0.45f, 0.95f, 0.45f);
        [SerializeField] private float soulRestoreScaleMultiplier = 1.25f;
        [SerializeField] private float soulRestoreFlashSeconds = 0.7f;

        [Header("Soul Loss Tokens")]
        [ColorUsage(true, true)]
        [SerializeField] private Color soulLossTokenColor = Color.white;
        [SerializeField, Min(0.1f)] private float soulLossTokenFontScale = 1f;
        [SerializeField, Min(1f)] private float soulLossTokenMinimumFontSize = 28f;
        [SerializeField] private Vector2 soulLossTokenSize = new Vector2(220f, 80f);
        [SerializeField, Min(0.01f)] private float soulLossTokenFallSeconds = 0.72f;
        [SerializeField, Min(0f)] private float soulLossTokenStaggerSeconds = 0.12f;
        [SerializeField, Min(0f)] private float soulLossTokenImpactSeconds = 0.42f;
        [SerializeField, Min(0.01f)] private float soulLossTokenFadeSeconds = 0.2f;
        [SerializeField, Min(0f)] private float soulLossTokenStartRandomX = 42f;
        [SerializeField] private Vector2 soulLossTokenStartYRange =
            new Vector2(-8f, 12f);
        [SerializeField, Min(0f)] private float soulLossTokenDriftX = 24f;
        [SerializeField] private Vector2 soulLossTokenFallDistanceRange =
            new Vector2(145f, 185f);
        [SerializeField, Min(0f)] private float soulLossTokenRotation = 14f;
        [SerializeField] private Vector2 soulLossPlayerAnchor =
            new Vector2(0.5f, 0.18f);
        [SerializeField] private Vector2 soulLossEnemyFallbackAnchor =
            new Vector2(0.78f, 0.68f);

        [Header("Shop controls")]
        [SerializeField] private GameObject shopLeaveRoot;
        [SerializeField] private Button shopLeaveButton;

        [Header("Card hover badge")]
        [Tooltip("Runtime moves only this anchor. Header/body layout stays authored in the prefab.")]
        [SerializeField] private RectTransform cardHoverTooltipRoot;
        [SerializeField] private RectTransform cardHoverBadge;
        [SerializeField] private TMP_Text cardHoverBadgeText;
        [SerializeField] private RectTransform cardHoverHeaderBadge;
        [SerializeField] private TMP_Text cardHoverHeaderText;

        [Header("Shop price badges")]
        [SerializeField] private RectTransform shopPriceBadgeContainer;
        [SerializeField] private ShopPriceBadgeView shopPriceBadgePrefab;

        [Header("Combat controls")]
        [SerializeField] private GameObject combatControlsRoot;
        [SerializeField] private RectTransform actionTooltip;
        [SerializeField] private TMP_Text actionTooltipText;
        [SerializeField] private GameObject optionPanel;
        [SerializeField] private TMP_Text combatHeaderText;
        [SerializeField] private CombatPromptView combatPromptView;
        [SerializeField] private ScrollRect optionScrollRect;
        [SerializeField] private GameHudChoiceButton[] optionSlots = Array.Empty<GameHudChoiceButton>();
        [SerializeField] private RevolverNumberSelectorView revolverNumberSelector;
        [SerializeField] private DemonCardHoverDetailView demonCardHoverDetail;
        [SerializeField] private CardContentCatalogSO cardContentCatalog;

        private Canvas _canvas;
        private OptionSlotLayout[] _optionSlotLayouts = Array.Empty<OptionSlotLayout>();
        private Sequence _soulRestoreSequence;
        private Sequence _playerSoulDamageSequence;
        private Sequence _enemySoulDamageSequence;
        private SoulLossPresentation _soulLossPresentation;
        private readonly List<ShopPriceBadgeView> _shopPriceBadges =
            new List<ShopPriceBadgeView>();
        private Color _playerSoulBaseColor;
        private Vector3 _playerSoulBaseScale;
        private bool _hasPlayerSoulBaseState;
        private Color _enemySoulBaseColor;
        private Vector3 _enemySoulBaseScale;
        private bool _hasEnemySoulBaseState;
        private bool _playerSoulOverrideActive;
        private int _playerSoulOverrideCurrent;
        private int _playerSoulOverrideMaximum;
        private bool _enemySoulOverrideActive;
        private int _enemySoulOverrideCurrent;
        private int _enemySoulOverrideMaximum;

        public event Action<GameSceneCombatHudCommand> CombatCommandRequested;

        public event Action ShopLeaveRequested;

        public int CombatOptionSlotCount => optionSlots == null ? 0 : optionSlots.Length;

        public bool HasCombatTooltipReference =>
            actionTooltip != null && actionTooltipText != null;

        public bool HasCombatCandidateContentReference => cardContentCatalog != null;

        internal bool HasRevolverNumberSelectorReference =>
            revolverNumberSelector != null;

        public bool HasCombatContractDetailReference =>
            demonCardHoverDetail != null &&
            demonCardHoverDetail.HasRequiredReferences;

        public bool IsDemonContractDetailVisible =>
            (demonCardHoverDetail != null &&
             demonCardHoverDetail.gameObject.activeInHierarchy);

        internal void SetTutorialRevolverTargetNumber(int? number)
        {
            revolverNumberSelector?.SetTutorialTargetNumber(number);
        }

        public bool IsRevolverNumberSelectionOpen =>
            revolverNumberSelector != null && revolverNumberSelector.IsOpen;

        public bool IsShopLeaveVisible =>
            shopLeaveRoot != null && shopLeaveRoot.activeSelf;

        public bool IsShopLeaveInteractable =>
            shopLeaveButton != null && shopLeaveButton.interactable;

        private void Awake()
        {
            _canvas = GetComponentInParent<Canvas>();
            if (revolverNumberSelector != null)
            {
                revolverNumberSelector.CommandRequested += RaiseCombatCommand;
            }
            CaptureOptionSlotLayouts();
            HideCardHoverBadge();
            HideShopPriceBadges();
            HideDemonContractDetail();
            BindCombatControls();
            HideCombatControls();
            BindShopLeaveControl();
            SetShopLeaveState(visible: false, interactable: false);
            EnsureSoulLossPresentation();
        }

        private void OnDestroy()
        {
            if (revolverNumberSelector != null)
            {
                revolverNumberSelector.CommandRequested -= RaiseCombatCommand;
            }

            _soulRestoreSequence?.Kill();
            CancelSoulLossPresentation();
            _soulLossPresentation?.Dispose();
            _soulLossPresentation = null;
            UnbindCombatControls();
            UnbindShopLeaveControl();
        }

        public void Render(CoreLoopViewModel core)
        {
            Render(core, null);
        }

        public void Render(
            CoreLoopViewModel core,
            GameSceneCombatHudViewModel combat)
        {
            if (core == null)
            {
                HideCombatControls();
                return;
            }

            if (playerSoulText != null && !_playerSoulOverrideActive)
            {
                CurrencyIconText.Set(
                    playerSoulText,
                    $"{CurrencyIconMarkup.SoulTag} {core.PlayerSoul}");
            }

            if (enemySoulText != null && !_enemySoulOverrideActive)
            {
                CurrencyIconText.Set(
                    enemySoulText,
                    $"{CurrencyIconMarkup.SoulTag} {core.EnemySoul}");
            }

            if (roundText != null)
            {
                roundText.text = BuildRoundText(core);
            }

            RenderCombat(core, combat);
        }

        /// <summary>
        /// Writes the player soul counter directly from a preformatted "current / max" string.
        /// Used while the formal shop is open, since there is no active battle view-model then
        /// (<see cref="Render(CoreLoopViewModel, GameSceneCombatHudViewModel)"/> is not called) and
        /// the text would otherwise keep showing the value from the last battle until the next one starts.
        /// </summary>
        public void SetPlayerSoul(string soulText)
        {
            if (playerSoulText != null)
            {
                CurrencyIconText.Set(
                    playerSoulText,
                    $"{CurrencyIconMarkup.SoulTag} {soulText}");
            }
        }

        /// <summary>
        /// One-shot "restored" flourish on the player soul counter: eases toward
        /// <see cref="soulRestoreFlashColor"/> while scaling up, then eases back to its
        /// original color/scale. Used when whiskey restores soul.
        /// </summary>
        public void PlaySoulRestoredFlourish()
        {
            if (playerSoulText == null)
            {
                return;
            }

            CapturePlayerSoulBaseState();
            _soulRestoreSequence?.Kill();
            playerSoulText.color = _playerSoulBaseColor;
            playerSoulText.transform.localScale = _playerSoulBaseScale;

            float halfDuration = Mathf.Max(0.05f, soulRestoreFlashSeconds) * 0.5f;
            Transform textTransform = playerSoulText.transform;
            Vector3 flashScale = _playerSoulBaseScale * soulRestoreScaleMultiplier;
            Sequence sequence = DOTween.Sequence();
            sequence.Append(DOTween.To(
                    () => playerSoulText.color,
                    color => playerSoulText.color = color,
                    soulRestoreFlashColor,
                    halfDuration)
                .SetEase(Ease.OutQuad));
            sequence.Join(textTransform
                .DOScale(flashScale, halfDuration)
                .SetEase(Ease.OutQuad));
            sequence.Append(DOTween.To(
                    () => playerSoulText.color,
                    color => playerSoulText.color = color,
                    _playerSoulBaseColor,
                    halfDuration)
                .SetEase(Ease.InQuad));
            sequence.Join(textTransform
                .DOScale(_playerSoulBaseScale, halfDuration)
                .SetEase(Ease.InQuad));
            sequence.OnComplete(() => _soulRestoreSequence = null);
            _soulRestoreSequence = sequence;
        }

        private void CapturePlayerSoulBaseState()
        {
            if (_hasPlayerSoulBaseState || playerSoulText == null)
            {
                return;
            }

            _playerSoulBaseColor = playerSoulText.color;
            _playerSoulBaseScale = playerSoulText.transform.localScale;
            _hasPlayerSoulBaseState = true;
        }

        internal void BeginSoulLossHold(SoulLossRecord record)
        {
            if (record.TargetSide == CombatantSide.Player)
            {
                if (!_playerSoulOverrideActive)
                {
                    _playerSoulOverrideCurrent = record.SoulBefore;
                    _playerSoulOverrideMaximum = record.MaximumSoul;
                }

                _playerSoulOverrideActive = true;
                WriteSoulValue(
                    playerSoulText,
                    _playerSoulOverrideCurrent,
                    _playerSoulOverrideMaximum);
                return;
            }

            if (!_enemySoulOverrideActive)
            {
                _enemySoulOverrideCurrent = record.SoulBefore;
                _enemySoulOverrideMaximum = record.MaximumSoul;
            }

            _enemySoulOverrideActive = true;
            WriteSoulValue(
                enemySoulText,
                _enemySoulOverrideCurrent,
                _enemySoulOverrideMaximum);
        }

        internal void ApplySoulLossUnit(SoulLossRecord record)
        {
            if (record.TargetSide == CombatantSide.Player)
            {
                _playerSoulOverrideActive = true;
                _playerSoulOverrideMaximum = record.MaximumSoul;
                _playerSoulOverrideCurrent = Mathf.Max(
                    record.SoulAfter,
                    _playerSoulOverrideCurrent - 1);
                WriteSoulValue(
                    playerSoulText,
                    _playerSoulOverrideCurrent,
                    _playerSoulOverrideMaximum);
                PlaySoulDamageFlourish(CombatantSide.Player);
                return;
            }

            _enemySoulOverrideActive = true;
            _enemySoulOverrideMaximum = record.MaximumSoul;
            _enemySoulOverrideCurrent = Mathf.Max(
                record.SoulAfter,
                _enemySoulOverrideCurrent - 1);
            WriteSoulValue(
                enemySoulText,
                _enemySoulOverrideCurrent,
                _enemySoulOverrideMaximum);
            PlaySoulDamageFlourish(CombatantSide.Enemy);
        }

        internal void CompleteSoulLossHold(
            CombatantSide side,
            int current,
            int maximum)
        {
            if (side == CombatantSide.Player)
            {
                _playerSoulOverrideActive = false;
                _playerSoulOverrideCurrent = current;
                _playerSoulOverrideMaximum = maximum;
                WriteSoulValue(playerSoulText, current, maximum);
                return;
            }

            _enemySoulOverrideActive = false;
            _enemySoulOverrideCurrent = current;
            _enemySoulOverrideMaximum = maximum;
            WriteSoulValue(enemySoulText, current, maximum);
        }

        internal Sequence PlaySoulLossTokens(
            IReadOnlyList<SoulLossRecord> records,
            Vector3 enemyWorldAnchor,
            Camera worldCamera,
            Action<SoulLossRecord> onImpact)
        {
            EnsureSoulLossPresentation();
            _soulLossPresentation?.SetSettings(CreateSoulLossTokenSettings());
            return _soulLossPresentation?.Play(
                records,
                enemyWorldAnchor,
                worldCamera,
                onImpact);
        }

        internal void CancelSoulLossPresentation()
        {
            _soulLossPresentation?.Cancel();
            _soulRestoreSequence?.Kill();
            _soulRestoreSequence = null;
            KillSoulDamageFlourish(
                playerSoulText,
                ref _playerSoulDamageSequence,
                _playerSoulBaseColor,
                _playerSoulBaseScale,
                _hasPlayerSoulBaseState);
            KillSoulDamageFlourish(
                enemySoulText,
                ref _enemySoulDamageSequence,
                _enemySoulBaseColor,
                _enemySoulBaseScale,
                _hasEnemySoulBaseState);
            _playerSoulOverrideActive = false;
            _enemySoulOverrideActive = false;
        }

        internal bool IsSoulLossHoldActive(CombatantSide side) =>
            side == CombatantSide.Player
                ? _playerSoulOverrideActive
                : _enemySoulOverrideActive;

        private void EnsureSoulLossPresentation()
        {
            if (_soulLossPresentation != null)
            {
                return;
            }

            _canvas ??= GetComponentInParent<Canvas>();
            TMP_Text template = playerSoulText != null
                ? playerSoulText
                : enemySoulText;
            if (_canvas != null && template != null)
            {
                _soulLossPresentation = new SoulLossPresentation(
                    _canvas,
                    template,
                    CreateSoulLossTokenSettings());
            }
        }

        private SoulLossTokenSettings CreateSoulLossTokenSettings()
        {
            return new SoulLossTokenSettings(
                soulLossTokenColor,
                soulLossTokenFontScale,
                soulLossTokenMinimumFontSize,
                soulLossTokenSize,
                soulLossTokenFallSeconds,
                soulLossTokenStaggerSeconds,
                soulLossTokenImpactSeconds,
                soulLossTokenFadeSeconds,
                soulLossTokenStartRandomX,
                soulLossTokenStartYRange,
                soulLossTokenDriftX,
                soulLossTokenFallDistanceRange,
                soulLossTokenRotation,
                soulLossPlayerAnchor,
                soulLossEnemyFallbackAnchor);
        }

        private void PlaySoulDamageFlourish(CombatantSide side)
        {
            TMP_Text target = side == CombatantSide.Player
                ? playerSoulText
                : enemySoulText;
            if (target == null)
            {
                return;
            }

            CaptureSoulBaseState(side);
            Sequence previous = side == CombatantSide.Player
                ? _playerSoulDamageSequence
                : _enemySoulDamageSequence;
            previous?.Kill();
            Color baseColor = side == CombatantSide.Player
                ? _playerSoulBaseColor
                : _enemySoulBaseColor;
            Vector3 baseScale = side == CombatantSide.Player
                ? _playerSoulBaseScale
                : _enemySoulBaseScale;
            target.color = Color.red;
            target.transform.localScale = baseScale * 1.25f;
            Sequence sequence = DOTween.Sequence()
                .Append(DOTween.To(
                        () => target.color,
                        color => target.color = color,
                        baseColor,
                        0.3f)
                    .SetEase(Ease.OutQuad))
                .Join(target.transform
                    .DOScale(baseScale, 0.3f)
                    .SetEase(Ease.OutBack));
            if (side == CombatantSide.Player)
            {
                sequence.OnComplete(() => _playerSoulDamageSequence = null);
                _playerSoulDamageSequence = sequence;
            }
            else
            {
                sequence.OnComplete(() => _enemySoulDamageSequence = null);
                _enemySoulDamageSequence = sequence;
            }
        }

        private void CaptureSoulBaseState(CombatantSide side)
        {
            if (side == CombatantSide.Player)
            {
                CapturePlayerSoulBaseState();
                return;
            }

            if (_hasEnemySoulBaseState || enemySoulText == null)
            {
                return;
            }

            _enemySoulBaseColor = enemySoulText.color;
            _enemySoulBaseScale = enemySoulText.transform.localScale;
            _hasEnemySoulBaseState = true;
        }

        private static void KillSoulDamageFlourish(
            TMP_Text target,
            ref Sequence sequence,
            Color baseColor,
            Vector3 baseScale,
            bool hasBaseState)
        {
            sequence?.Kill();
            sequence = null;
            if (target != null && hasBaseState)
            {
                target.color = baseColor;
                target.transform.localScale = baseScale;
            }
        }

        private static void WriteSoulValue(
            TMP_Text target,
            int current,
            int maximum)
        {
            CurrencyIconText.Set(
                target,
                $"{CurrencyIconMarkup.SoulTag} {current} / {maximum}");
        }

        /// <summary>
        /// Writes the run gold counter (top-left, beside souls). Separate from <see cref="Render"/>
        /// because gold is GameScene-local state in the MVP, not part of the battle view-model.
        /// </summary>
        public void SetGold(int gold)
        {
            if (goldText != null)
            {
                CurrencyIconText.Set(
                    goldText,
                    $"{CurrencyIconMarkup.GoldTag} {gold}");
            }
        }

        public void SetEnemyStatusVisible(bool isVisible)
        {
            if (enemySoulText != null)
            {
                enemySoulText.gameObject.SetActive(isVisible);
            }
        }

        /// <summary>
        /// Toggles the round/player-soul/gold labels as a group. Used to hide the default
        /// battle-stat HUD on screens that have no battle or run economy to show yet, such as
        /// the starting demon reveal.
        /// </summary>
        public void SetCoreStatsVisible(bool isVisible)
        {
            if (roundText != null)
            {
                roundText.gameObject.SetActive(isVisible);
            }

            if (playerSoulText != null)
            {
                playerSoulText.gameObject.SetActive(isVisible);
            }

            if (goldText != null)
            {
                goldText.gameObject.SetActive(isVisible);
            }
        }

        public void SetShopLeaveState(bool visible, bool interactable)
        {
            if (shopLeaveRoot != null)
            {
                shopLeaveRoot.SetActive(visible);
            }

            if (shopLeaveButton != null)
            {
                shopLeaveButton.interactable = visible && interactable;
            }
        }

        private void BindShopLeaveControl()
        {
            if (shopLeaveButton != null)
            {
                shopLeaveButton.onClick.AddListener(RaiseShopLeaveRequested);
            }
        }

        private void UnbindShopLeaveControl()
        {
            if (shopLeaveButton != null)
            {
                shopLeaveButton.onClick.RemoveListener(RaiseShopLeaveRequested);
            }
        }

        private void RaiseShopLeaveRequested()
        {
            ShopLeaveRequested?.Invoke();
        }

        /// <summary>Shows the shared badge at a screen-space point supplied by a hovered card.</summary>
        public void ShowCardHoverBadge(
            string title,
            string description,
            Vector2 anchorScreenPosition,
            Camera worldCamera,
            bool showBelow)
        {
            ShowCardHoverBadge(
                title,
                description,
                anchorScreenPosition,
                worldCamera,
                new Vector2(0.5f, showBelow ? 1f : 0f));
        }

        public void ShowCardHoverBadge(
            string title,
            string description,
            Vector2 anchorScreenPosition,
            Camera worldCamera,
            Vector2 tooltipPivot)
        {
            if (cardHoverTooltipRoot == null ||
                cardHoverBadge == null ||
                cardHoverBadgeText == null ||
                cardHoverHeaderBadge == null ||
                cardHoverHeaderText == null ||
                string.IsNullOrEmpty(title))
            {
                HideCardHoverBadge();
                return;
            }

            RectTransform parent = cardHoverTooltipRoot.parent as RectTransform;
            if (parent == null)
            {
                HideCardHoverBadge();
                return;
            }

            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>();
            }

            Camera uiCamera = _canvas != null &&
                _canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _canvas.worldCamera != null ? _canvas.worldCamera : worldCamera
                : null;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parent,
                    anchorScreenPosition,
                    uiCamera,
                    out Vector2 localPoint))
            {
                HideCardHoverBadge();
                return;
            }

            CurrencyIconText.Set(cardHoverHeaderText, title);
            CurrencyIconText.Set(cardHoverBadgeText, description);

            bool hasDescription = !string.IsNullOrEmpty(description);
            PositionCardHoverTooltip(localPoint, tooltipPivot);
            cardHoverHeaderBadge.gameObject.SetActive(true);
            cardHoverBadge.gameObject.SetActive(hasDescription);
        }

        public void HideCardHoverBadge()
        {
            if (cardHoverBadge != null)
            {
                cardHoverBadge.gameObject.SetActive(false);
            }

            if (cardHoverHeaderBadge != null)
            {
                cardHoverHeaderBadge.gameObject.SetActive(false);
            }
        }

        public void RenderShopPriceBadges(
            IReadOnlyList<ShopPriceTarget> targets,
            Camera worldCamera)
        {
            if (targets == null ||
                targets.Count == 0 ||
                worldCamera == null ||
                shopPriceBadgeContainer == null ||
                shopPriceBadgePrefab == null)
            {
                HideShopPriceBadges();
                return;
            }

            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>();
            }

            Camera uiCamera = _canvas != null &&
                _canvas.renderMode != RenderMode.ScreenSpaceOverlay
                    ? _canvas.worldCamera != null
                        ? _canvas.worldCamera
                        : worldCamera
                    : null;
            int visibleCount = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                ShopPriceTarget target = targets[i];
                if (target == null ||
                    !target.TryCreateRequest(
                        worldCamera,
                        out ShopPriceBadgeRequest request) ||
                    !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        shopPriceBadgeContainer,
                        request.ScreenPosition,
                        uiCamera,
                        out Vector2 localPosition))
                {
                    continue;
                }

                ShopPriceBadgeView badge =
                    GetOrCreateShopPriceBadge(visibleCount++);
                if (badge == null)
                {
                    continue;
                }

                badge.Render(request);
                badge.SetLocalPosition(
                    localPosition,
                    shopPriceBadgeContainer);
            }

            HideShopPriceBadgesFrom(visibleCount);
        }

        public void HideShopPriceBadges()
        {
            HideShopPriceBadgesFrom(0);
        }

        private ShopPriceBadgeView GetOrCreateShopPriceBadge(
            int index)
        {
            while (_shopPriceBadges.Count <= index)
            {
                ShopPriceBadgeView badge = Instantiate(
                    shopPriceBadgePrefab,
                    shopPriceBadgeContainer,
                    worldPositionStays: false);
                badge.name =
                    $"ShopPriceBadge_{_shopPriceBadges.Count + 1}";
                badge.Hide();
                _shopPriceBadges.Add(badge);
            }

            return _shopPriceBadges[index];
        }

        private void HideShopPriceBadgesFrom(int startIndex)
        {
            for (int i = Mathf.Max(0, startIndex); i < _shopPriceBadges.Count; i++)
            {
                _shopPriceBadges[i]?.Hide();
            }
        }

        public void ShowDemonContractDetail(
            GameSceneCombatHudContractCandidateViewModel candidate)
        {
            if (candidate == null ||
                demonCardHoverDetail == null)
            {
                HideDemonContractDetail();
                return;
            }

            demonCardHoverDetail.Render(
                candidate,
                cardContentCatalog == null
                    ? null
                    : cardContentCatalog.GetDemonFaceSprite(
                        candidate.DefinitionKey));
            demonCardHoverDetail.gameObject.SetActive(true);
        }

        public void ShowDemonContractDetail(GameSceneDemonCardViewModel card)
        {
            if (card == null ||
                demonCardHoverDetail == null)
            {
                HideDemonContractDetail();
                return;
            }

            demonCardHoverDetail.Render(
                card,
                cardContentCatalog == null
                    ? null
                    : cardContentCatalog.GetDemonFaceSprite(card.DefinitionKey));
            demonCardHoverDetail.gameObject.SetActive(true);
        }

        public void HideDemonContractDetail()
        {
            SetActive(demonCardHoverDetail == null
                ? null
                : demonCardHoverDetail.gameObject, false);
        }

        public void ShowCombatActionTooltip(
            string tooltip,
            Vector2 screenPosition,
            Camera worldCamera)
        {
            if (string.IsNullOrEmpty(tooltip) ||
                actionTooltip == null ||
                actionTooltipText == null)
            {
                HideCombatActionTooltip();
                return;
            }

            RectTransform parent = actionTooltip.parent as RectTransform;
            if (parent == null)
            {
                HideCombatActionTooltip();
                return;
            }

            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>();
            }

            Camera uiCamera = _canvas != null &&
                _canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _canvas.worldCamera != null ? _canvas.worldCamera : worldCamera
                : null;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parent,
                    screenPosition,
                    uiCamera,
                    out Vector2 localPosition))
            {
                HideCombatActionTooltip();
                return;
            }

            CurrencyIconText.Set(actionTooltipText, tooltip);
            actionTooltip.pivot = new Vector2(0.5f, 0f);
            actionTooltip.localPosition = localPosition + new Vector2(0f, 14f);
            actionTooltip.gameObject.SetActive(true);
        }

        public void HideCombatActionTooltip()
        {
            if (actionTooltip != null)
            {
                actionTooltip.gameObject.SetActive(false);
            }
        }

        private void BindCombatControls()
        {
            BindChoiceButtons(optionSlots);
        }

        private void UnbindCombatControls()
        {
            UnbindChoiceButtons(optionSlots);
        }

        private void BindChoiceButtons(GameHudChoiceButton[] slots)
        {
            if (slots == null)
            {
                return;
            }

            foreach (GameHudChoiceButton slot in slots)
            {
                if (slot != null)
                {
                    slot.CommandRequested += RaiseCombatCommand;
                }
            }
        }

        private void UnbindChoiceButtons(GameHudChoiceButton[] slots)
        {
            if (slots == null)
            {
                return;
            }

            foreach (GameHudChoiceButton slot in slots)
            {
                if (slot != null)
                {
                    slot.CommandRequested -= RaiseCombatCommand;
                }
            }
        }

        private void RaiseCombatCommand(GameSceneCombatHudCommand command)
        {
            CombatCommandRequested?.Invoke(command);
        }

        private void RenderCombat(
            CoreLoopViewModel core,
            GameSceneCombatHudViewModel combat)
        {
            HideCombatActionTooltip();

            if (combat?.SelectionPrompt is CombatPromptRequest promptRequest)
            {
                combatPromptView?.Render(promptRequest);
            }
            else if (combat?.AutomaticCardResult is
                AutomaticCardResultPromptRequest resultRequest)
            {
                combatPromptView?.Render(
                    resultRequest,
                    core.EnemyDisplayName);
            }
            else
            {
                combatPromptView?.Hide();
            }

            if (combat != null &&
                combat.Mode == GameSceneCombatHudMode.RevolverNumberSelection)
            {
                if (combatControlsRoot != null)
                {
                    combatControlsRoot.SetActive(true);
                }

                SetActive(optionPanel, false);
                revolverNumberSelector?.Render(combat.OptionActions);
                return;
            }

            revolverNumberSelector?.Hide();

            if (combat == null || combat.Mode == GameSceneCombatHudMode.Hidden)
            {
                HideCombatControls();
                return;
            }

            if (combatControlsRoot != null)
            {
                combatControlsRoot.SetActive(true);
            }

            bool isDiegeticSelection =
                combat.Mode == GameSceneCombatHudMode.DiegeticSelection ||
                combat.Mode == GameSceneCombatHudMode.SatanNumberSelection;
            bool hasBottomRightAction = HasBottomRightAction(combat.OptionActions);
            bool hasDefaultAction = HasDefaultAction(combat.OptionActions);
            bool hasOptionAction = combat.OptionActions.Count > 0;
            SetActive(
                optionPanel,
                (hasOptionAction || !string.IsNullOrEmpty(combat.HeaderText)) &&
                (combat.Mode == GameSceneCombatHudMode.Options ||
                 isDiegeticSelection ||
                 combat.Mode == GameSceneCombatHudMode.ReturningToRun ||
                 combat.Mode == GameSceneCombatHudMode.Restart ||
                 combat.Mode == GameSceneCombatHudMode.Actions));
            SetOptionPanelChromeVisible(
                !isDiegeticSelection && !hasBottomRightAction);
            if ((isDiegeticSelection || hasBottomRightAction) &&
                optionScrollRect != null)
            {
                optionScrollRect.gameObject.SetActive(hasDefaultAction);
            }
            if (combat.Mode == GameSceneCombatHudMode.Actions)
            {
                RenderOptionActions(combat.HeaderText, combat.OptionActions);
                return;
            }

            if (combat.Mode == GameSceneCombatHudMode.ContractCandidates)
            {
                return;
            }

            RenderOptionActions(combat.HeaderText, combat.OptionActions);
        }

        private void SetOptionPanelChromeVisible(bool isVisible)
        {
            if (optionPanel != null &&
                optionPanel.TryGetComponent(out Graphic background))
            {
                background.enabled = isVisible;
            }

            if (optionScrollRect != null)
            {
                optionScrollRect.gameObject.SetActive(isVisible);
                if (optionScrollRect.TryGetComponent(out Graphic scrollBackground))
                {
                    scrollBackground.enabled = isVisible;
                }
            }
        }

        private void RenderOptionActions(
            string headerText,
            System.Collections.Generic.IReadOnlyList<GameSceneCombatHudActionViewModel> actions)
        {
            if (combatHeaderText != null)
            {
                CurrencyIconText.Set(combatHeaderText, headerText);
            }

            RestoreOptionSlotLayouts();
            int activeCount = actions == null ? 0 : Mathf.Min(actions.Count, optionSlots.Length);
            int bottomRightIndex = 0;
            for (int i = 0; i < optionSlots.Length; i++)
            {
                GameHudChoiceButton slot = optionSlots[i];
                if (slot == null)
                {
                    continue;
                }

                bool isActive = i < activeCount;
                slot.gameObject.SetActive(isActive);
                if (isActive)
                {
                    slot.Render(actions[i]);
                    if (actions[i].Placement ==
                        GameSceneCombatHudActionPlacement.BottomRight)
                    {
                        PositionBottomRight(slot, bottomRightIndex++);
                    }
                    else if (actions[i].Placement ==
                        GameSceneCombatHudActionPlacement.Center)
                    {
                        PositionCenter(slot);
                    }
                }
            }

            if (optionScrollRect != null && optionScrollRect.content != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(optionScrollRect.content);
            }

            ResetOptionScroll();
        }

        private void CaptureOptionSlotLayouts()
        {
            _optionSlotLayouts = optionSlots == null
                ? Array.Empty<OptionSlotLayout>()
                : new OptionSlotLayout[optionSlots.Length];
            for (int i = 0; i < _optionSlotLayouts.Length; i++)
            {
                RectTransform rect = optionSlots[i] == null
                    ? null
                    : optionSlots[i].transform as RectTransform;
                _optionSlotLayouts[i] = new OptionSlotLayout(rect);
            }
        }

        private void RestoreOptionSlotLayouts()
        {
            int count = Mathf.Min(optionSlots.Length, _optionSlotLayouts.Length);
            for (int i = 0; i < count; i++)
            {
                _optionSlotLayouts[i].Restore(
                    optionSlots[i] == null
                        ? null
                        : optionSlots[i].transform as RectTransform);
            }
        }

        private void PositionBottomRight(GameHudChoiceButton slot, int index)
        {
            if (slot == null || optionPanel == null ||
                !(slot.transform is RectTransform rect))
            {
                return;
            }

            rect.SetParent(optionPanel.transform, false);
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-48f, 48f + index * 76f);
            rect.sizeDelta = new Vector2(380f, 64f);
        }

        private void PositionCenter(GameHudChoiceButton slot)
        {
            if (slot == null || optionPanel == null ||
                !(slot.transform is RectTransform rect))
            {
                return;
            }

            rect.SetParent(optionPanel.transform, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(380f, 64f);
        }

        private static bool HasBottomRightAction(
            System.Collections.Generic.IReadOnlyList<GameSceneCombatHudActionViewModel> actions)
        {
            if (actions == null)
            {
                return false;
            }

            foreach (GameSceneCombatHudActionViewModel action in actions)
            {
                if (action.Placement == GameSceneCombatHudActionPlacement.BottomRight)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasDefaultAction(
            System.Collections.Generic.IReadOnlyList<GameSceneCombatHudActionViewModel> actions)
        {
            if (actions == null)
            {
                return false;
            }

            foreach (GameSceneCombatHudActionViewModel action in actions)
            {
                if (action.Placement == GameSceneCombatHudActionPlacement.Default)
                {
                    return true;
                }
            }

            return false;
        }

        private readonly struct OptionSlotLayout
        {
            private readonly Transform _parent;
            private readonly int _siblingIndex;
            private readonly Vector2 _anchorMin;
            private readonly Vector2 _anchorMax;
            private readonly Vector2 _pivot;
            private readonly Vector2 _anchoredPosition;
            private readonly Vector2 _sizeDelta;

            public OptionSlotLayout(RectTransform rect)
            {
                _parent = rect == null ? null : rect.parent;
                _siblingIndex = rect == null ? 0 : rect.GetSiblingIndex();
                _anchorMin = rect == null ? Vector2.zero : rect.anchorMin;
                _anchorMax = rect == null ? Vector2.zero : rect.anchorMax;
                _pivot = rect == null ? Vector2.zero : rect.pivot;
                _anchoredPosition = rect == null ? Vector2.zero : rect.anchoredPosition;
                _sizeDelta = rect == null ? Vector2.zero : rect.sizeDelta;
            }

            public void Restore(RectTransform rect)
            {
                if (rect == null || _parent == null)
                {
                    return;
                }

                rect.SetParent(_parent, false);
                rect.SetSiblingIndex(_siblingIndex);
                rect.anchorMin = _anchorMin;
                rect.anchorMax = _anchorMax;
                rect.pivot = _pivot;
                rect.anchoredPosition = _anchoredPosition;
                rect.sizeDelta = _sizeDelta;
            }
        }

        private void ResetOptionScroll()
        {
            if (optionScrollRect == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            optionScrollRect.verticalNormalizedPosition = 1f;
        }

        internal void HideCombatControlsImmediately()
        {
            HideCombatControls();
        }

        private void HideCombatControls()
        {
            HideCombatActionTooltip();
            combatPromptView?.Hide();
            revolverNumberSelector?.Hide();
            SetActive(combatControlsRoot, false);
            SetActive(optionPanel, false);
        }

        private static void SetActive(GameObject target, bool isActive)
        {
            if (target != null)
            {
                target.SetActive(isActive);
            }
        }

        private void PositionCardHoverTooltip(
            Vector2 localPoint,
            Vector2 tooltipPivot)
        {
            cardHoverTooltipRoot.pivot = tooltipPivot;
            cardHoverTooltipRoot.localPosition = new Vector3(
                localPoint.x,
                localPoint.y,
                0f);
        }

        private static string BuildRoundText(CoreLoopViewModel core)
        {
            switch (core.Outcome)
            {
                case BattleOutcome.PlayerVictory:
                    return "VICTORY";
                case BattleOutcome.PlayerDefeat:
                    return "DEFEAT";
                default:
                    return $"ROUND {core.RoundNumber}\nTURN {core.TurnNumber}";
            }
        }
    }
}
