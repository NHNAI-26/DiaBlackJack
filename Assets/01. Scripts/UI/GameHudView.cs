using System;
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

        [Header("Card hover badge")]
        [Tooltip("Runtime moves only this anchor. Header/body layout stays authored in the prefab.")]
        [SerializeField] private RectTransform cardHoverTooltipRoot;
        [SerializeField] private RectTransform cardHoverBadge;
        [SerializeField] private TMP_Text cardHoverBadgeText;
        [SerializeField] private RectTransform cardHoverHeaderBadge;
        [SerializeField] private TMP_Text cardHoverHeaderText;
        [Tooltip("Pixel offset from the hovered card's screen-space anchor.")]
        [SerializeField] private Vector2 cardHoverBadgeScreenOffset = new Vector2(0f, 24f);

        [Header("Combat controls")]
        [SerializeField] private GameObject combatControlsRoot;
        [SerializeField] private GameObject actionRow;
        [SerializeField] private GameHudActionButton hitButton;
        [SerializeField] private GameHudActionButton standButton;
        [SerializeField] private GameHudActionButton changeButton;
        [SerializeField] private GameHudActionButton contractButton;
        [SerializeField] private RectTransform actionTooltip;
        [SerializeField] private TMP_Text actionTooltipText;
        [SerializeField] private GameObject optionPanel;
        [SerializeField] private TMP_Text combatPromptText;
        [SerializeField] private ScrollRect optionScrollRect;
        [SerializeField] private GameHudChoiceButton[] optionSlots = Array.Empty<GameHudChoiceButton>();
        [SerializeField] private GameObject contractCandidatePanel;
        [SerializeField] private TMP_Text contractCandidatePromptText;
        [SerializeField] private GameHudChoiceButton[] contractCandidateSlots = Array.Empty<GameHudChoiceButton>();
        [SerializeField] private GameObject automaticCardResultPanel;
        [SerializeField] private TMP_Text automaticCardResultText;
        [SerializeField] private CardContentCatalogSO cardContentCatalog;

        private Canvas _canvas;
        private GameHudActionButton _hoveredActionButton;

        public event Action<GameSceneCombatHudCommand> CombatCommandRequested;

        public int CombatOptionSlotCount => optionSlots == null ? 0 : optionSlots.Length;

        public int CombatContractCandidateSlotCount =>
            contractCandidateSlots == null ? 0 : contractCandidateSlots.Length;

        public bool HasCombatTooltipReference =>
            actionTooltip != null && actionTooltipText != null;

        public bool HasCombatCandidateContentReference => cardContentCatalog != null;

        public bool IsDemonContractDetailVisible =>
            contractCandidatePanel != null &&
            contractCandidatePanel.activeSelf;

        private void Awake()
        {
            _canvas = GetComponentInParent<Canvas>();
            HideCardHoverBadge();
            HideDemonContractDetail();
            BindCombatControls();
            HideCombatControls();
        }

        private void OnDestroy()
        {
            UnbindCombatControls();
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

            if (playerSoulText != null)
            {
                playerSoulText.text = $"YOU\n{core.PlayerSoul}";
            }

            if (enemySoulText != null)
            {
                enemySoulText.text = $"ENEMY\n{core.EnemySoul}";
            }

            if (roundText != null)
            {
                roundText.text = BuildRoundText(core);
            }

            RenderCombat(combat);
        }

        /// <summary>
        /// Writes the run gold counter (top-left, beside souls). Separate from <see cref="Render"/>
        /// because gold is GameScene-local state in the MVP, not part of the battle view-model.
        /// </summary>
        public void SetGold(int gold)
        {
            if (goldText != null)
            {
                goldText.text = $"GOLD\n{gold}";
            }
        }

        /// <summary>Shows the shared badge at a screen-space point supplied by a hovered card.</summary>
        public void ShowCardHoverBadge(
            string title,
            string description,
            Vector2 cardTopScreenPosition,
            Camera worldCamera,
            bool showBelow)
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
                    cardTopScreenPosition,
                    uiCamera,
                    out Vector2 localPoint))
            {
                HideCardHoverBadge();
                return;
            }

            cardHoverHeaderText.text = title;
            cardHoverBadgeText.text = description ?? string.Empty;

            bool hasDescription = !string.IsNullOrEmpty(description);
            PositionCardHoverTooltip(localPoint, showBelow);
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

        public void ShowDemonContractDetail(
            GameSceneCombatHudContractCandidateViewModel candidate)
        {
            if (candidate == null ||
                contractCandidatePanel == null ||
                contractCandidateSlots == null ||
                contractCandidateSlots.Length == 0 ||
                contractCandidateSlots[0] == null)
            {
                HideDemonContractDetail();
                return;
            }

            float uiScale = CalculateDemonContractDetailScale(
                Screen.width,
                Screen.height);
            if (contractCandidatePromptText != null)
            {
                contractCandidatePromptText.text = string.Empty;
                contractCandidatePromptText.gameObject.SetActive(false);
            }

            GameHudChoiceButton detailSlot = contractCandidateSlots[0];
            detailSlot.gameObject.SetActive(true);
            if (detailSlot.transform is RectTransform detailRect)
            {
                detailRect.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Horizontal,
                    920f * uiScale);
                detailRect.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Vertical,
                    420f * uiScale);
            }

            detailSlot.RenderContractDetail(
                candidate,
                cardContentCatalog == null
                    ? null
                    : cardContentCatalog.GetDemonFaceSprite(
                        candidate.DefinitionKey),
                uiScale);

            for (int i = 1; i < contractCandidateSlots.Length; i++)
            {
                if (contractCandidateSlots[i] != null)
                {
                    contractCandidateSlots[i].gameObject.SetActive(false);
                }
            }

            contractCandidatePanel.SetActive(true);
        }

        internal static float CalculateDemonContractDetailScale(
            int screenWidth,
            int screenHeight)
        {
            if (screenWidth <= 0 || screenHeight <= 0)
            {
                return 1f;
            }

            float widthScale = screenWidth / 1280f;
            float heightScale = screenHeight / 720f;
            return Mathf.Clamp(Mathf.Min(widthScale, heightScale), 0.85f, 1.5f);
        }

        public void HideDemonContractDetail()
        {
            SetActive(contractCandidatePanel, false);
        }

        private void BindCombatControls()
        {
            BindActionButton(hitButton);
            BindActionButton(standButton);
            BindActionButton(changeButton);
            BindActionButton(contractButton);
            BindChoiceButtons(optionSlots);
        }

        private void UnbindCombatControls()
        {
            UnbindActionButton(hitButton);
            UnbindActionButton(standButton);
            UnbindActionButton(changeButton);
            UnbindActionButton(contractButton);
            UnbindChoiceButtons(optionSlots);
        }

        private void BindActionButton(GameHudActionButton actionButton)
        {
            if (actionButton == null)
            {
                return;
            }

            actionButton.CommandRequested += RaiseCombatCommand;
            actionButton.HoverChanged += HandleActionHoverChanged;
        }

        private void UnbindActionButton(GameHudActionButton actionButton)
        {
            if (actionButton == null)
            {
                return;
            }

            actionButton.CommandRequested -= RaiseCombatCommand;
            actionButton.HoverChanged -= HandleActionHoverChanged;
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

        private void HandleActionHoverChanged(GameHudActionButton source, bool isHovering)
        {
            if (isHovering)
            {
                _hoveredActionButton = source;
                ShowActionTooltip(source);
                return;
            }

            if (_hoveredActionButton == source)
            {
                _hoveredActionButton = null;
                HideActionTooltip();
            }
        }

        private void RenderCombat(GameSceneCombatHudViewModel combat)
        {
            HideActionTooltip();
            _hoveredActionButton = null;

            if (combat == null || combat.Mode == GameSceneCombatHudMode.Hidden)
            {
                HideCombatControls();
                return;
            }

            if (combatControlsRoot != null)
            {
                combatControlsRoot.SetActive(true);
            }

            SetActive(actionRow, combat.Mode == GameSceneCombatHudMode.Actions);
            SetActive(
                optionPanel,
                combat.Mode == GameSceneCombatHudMode.Options ||
                combat.Mode == GameSceneCombatHudMode.ReturningToRun ||
                combat.Mode == GameSceneCombatHudMode.Restart ||
                (combat.Mode == GameSceneCombatHudMode.Actions &&
                 combat.OptionActions.Count > 0));
            SetActive(
                contractCandidatePanel,
                false);
            SetActive(
                automaticCardResultPanel,
                !string.IsNullOrEmpty(combat.AutomaticCardResult));

            if (automaticCardResultText != null)
            {
                automaticCardResultText.text = combat.AutomaticCardResult;
            }

            if (combat.Mode == GameSceneCombatHudMode.Actions)
            {
                RenderPrimaryActions(combat.PrimaryActions);
                RenderOptionActions(combat.Prompt, combat.OptionActions);
                return;
            }

            if (combat.Mode == GameSceneCombatHudMode.ContractCandidates)
            {
                RenderPrimaryActions(Array.Empty<GameSceneCombatHudActionViewModel>());
                return;
            }

            RenderPrimaryActions(Array.Empty<GameSceneCombatHudActionViewModel>());
            RenderOptionActions(combat.Prompt, combat.OptionActions);
        }

        private void RenderPrimaryActions(
            System.Collections.Generic.IReadOnlyList<GameSceneCombatHudActionViewModel> actions)
        {
            RenderActionButton(hitButton, GetAction(actions, 0));
            RenderActionButton(standButton, GetAction(actions, 1));
            RenderActionButton(changeButton, GetAction(actions, 2));
            RenderActionButton(contractButton, GetAction(actions, 3));
        }

        private void RenderActionButton(
            GameHudActionButton actionButton,
            GameSceneCombatHudActionViewModel action)
        {
            if (actionButton == null)
            {
                return;
            }

            actionButton.gameObject.SetActive(action != null);
            if (action != null)
            {
                actionButton.Render(action);
            }
        }

        private void RenderOptionActions(
            string prompt,
            System.Collections.Generic.IReadOnlyList<GameSceneCombatHudActionViewModel> actions)
        {
            if (combatPromptText != null)
            {
                combatPromptText.text = prompt ?? string.Empty;
            }

            int activeCount = actions == null ? 0 : Mathf.Min(actions.Count, optionSlots.Length);
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
                }
            }

            if (optionScrollRect != null && optionScrollRect.content != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(optionScrollRect.content);
            }

            ResetOptionScroll();
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

        private void ShowActionTooltip(GameHudActionButton source)
        {
            if (source == null || actionTooltip == null || actionTooltipText == null)
            {
                return;
            }

            actionTooltipText.text = source.Tooltip;
            RectTransform parent = actionTooltip.parent as RectTransform;
            if (parent == null || source.RectTransform == null)
            {
                return;
            }

            Camera uiCamera = _canvas != null &&
                _canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _canvas.worldCamera
                : null;
            Vector3 worldPosition = source.RectTransform.TransformPoint(
                new Vector3(0f, source.RectTransform.rect.height * 0.5f, 0f));
            Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(uiCamera, worldPosition);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parent,
                    screenPosition,
                    uiCamera,
                    out Vector2 localPosition))
            {
                actionTooltip.pivot = new Vector2(0.5f, 0f);
                actionTooltip.localPosition = localPosition + new Vector2(0f, 14f);
                actionTooltip.gameObject.SetActive(true);
            }
        }

        private void HideActionTooltip()
        {
            if (actionTooltip != null)
            {
                actionTooltip.gameObject.SetActive(false);
            }
        }

        private void HideCombatControls()
        {
            _hoveredActionButton = null;
            HideActionTooltip();
            SetActive(combatControlsRoot, false);
            SetActive(actionRow, false);
            SetActive(optionPanel, false);
            SetActive(contractCandidatePanel, false);
            SetActive(automaticCardResultPanel, false);
        }

        private static GameSceneCombatHudActionViewModel GetAction(
            System.Collections.Generic.IReadOnlyList<GameSceneCombatHudActionViewModel> actions,
            int index)
        {
            return actions != null && index >= 0 && index < actions.Count
                ? actions[index]
                : null;
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
            bool showBelow)
        {
            Vector2 screenOffset = new Vector2(
                cardHoverBadgeScreenOffset.x,
                showBelow
                    ? -cardHoverBadgeScreenOffset.y
                    : cardHoverBadgeScreenOffset.y);
            cardHoverTooltipRoot.pivot = new Vector2(
                cardHoverTooltipRoot.pivot.x,
                showBelow ? 1f : 0f);
            Vector2 tooltipPosition = localPoint + screenOffset;
            cardHoverTooltipRoot.localPosition = new Vector3(
                tooltipPosition.x,
                tooltipPosition.y,
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
                    return $"ROUND {core.RoundNumber}";
            }
        }
    }
}
