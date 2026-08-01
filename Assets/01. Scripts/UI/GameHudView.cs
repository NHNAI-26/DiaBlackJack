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

        [Header("Combat controls")]
        [SerializeField] private GameObject combatControlsRoot;
        [SerializeField] private RectTransform actionTooltip;
        [SerializeField] private TMP_Text actionTooltipText;
        [SerializeField] private GameObject optionPanel;
        [SerializeField] private TMP_Text combatPromptText;
        [SerializeField] private ScrollRect optionScrollRect;
        [SerializeField] private GameHudChoiceButton[] optionSlots = Array.Empty<GameHudChoiceButton>();
        [SerializeField] private GameObject contractDetailPanel;
        [SerializeField] private GameHudContractDetailView contractDetailView;
        [SerializeField] private GameObject automaticCardResultPanel;
        [SerializeField] private TMP_Text automaticCardResultText;
        [SerializeField] private CardContentCatalogSO cardContentCatalog;

        private Canvas _canvas;
        private bool _shopDemonDetailActivatedCombatControlsRoot;

        public event Action<GameSceneCombatHudCommand> CombatCommandRequested;

        public int CombatOptionSlotCount => optionSlots == null ? 0 : optionSlots.Length;

        public bool HasCombatTooltipReference =>
            actionTooltip != null && actionTooltipText != null;

        public bool HasCombatCandidateContentReference => cardContentCatalog != null;

        public bool HasCombatContractDetailReference =>
            contractDetailPanel != null &&
            contractDetailView != null &&
            contractDetailView.HasRequiredReferences;

        public bool IsDemonContractDetailVisible =>
            contractDetailPanel != null &&
            contractDetailPanel.activeInHierarchy;

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

        public void SetEnemyStatusVisible(bool isVisible)
        {
            if (enemySoulText != null)
            {
                enemySoulText.gameObject.SetActive(isVisible);
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
                contractDetailPanel == null ||
                contractDetailView == null)
            {
                HideDemonContractDetail();
                return;
            }

            contractDetailView.Render(
                candidate,
                cardContentCatalog == null
                    ? null
                    : cardContentCatalog.GetDemonFaceSprite(
                        candidate.DefinitionKey));
            contractDetailPanel.SetActive(true);
        }

        public void ShowDemonContractDetail(GameSceneDemonCardViewModel card)
        {
            if (card == null ||
                contractDetailPanel == null ||
                contractDetailView == null)
            {
                HideDemonContractDetail();
                return;
            }

            contractDetailView.Render(
                card,
                cardContentCatalog == null
                    ? null
                    : cardContentCatalog.GetDemonFaceSprite(card.DefinitionKey));
            if (combatControlsRoot != null && !combatControlsRoot.activeSelf)
            {
                combatControlsRoot.SetActive(true);
                _shopDemonDetailActivatedCombatControlsRoot = true;
            }

            contractDetailPanel.SetActive(true);
        }

        public void HideDemonContractDetail()
        {
            SetActive(contractDetailPanel, false);
            if (_shopDemonDetailActivatedCombatControlsRoot)
            {
                SetActive(combatControlsRoot, false);
                _shopDemonDetailActivatedCombatControlsRoot = false;
            }
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

            actionTooltipText.text = tooltip;
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

        private void RenderCombat(GameSceneCombatHudViewModel combat)
        {
            HideCombatActionTooltip();

            if (combat == null || combat.Mode == GameSceneCombatHudMode.Hidden)
            {
                HideCombatControls();
                return;
            }

            if (combatControlsRoot != null)
            {
                _shopDemonDetailActivatedCombatControlsRoot = false;
                combatControlsRoot.SetActive(true);
            }

            bool isDiegeticSelection =
                combat.Mode == GameSceneCombatHudMode.DiegeticSelection;
            SetActive(
                optionPanel,
                combat.Mode == GameSceneCombatHudMode.Options ||
                isDiegeticSelection ||
                combat.Mode == GameSceneCombatHudMode.ReturningToRun ||
                combat.Mode == GameSceneCombatHudMode.Restart ||
                (combat.Mode == GameSceneCombatHudMode.Actions &&
                 combat.OptionActions.Count > 0));
            SetOptionPanelChromeVisible(!isDiegeticSelection);
            if (isDiegeticSelection && combat.OptionActions.Count > 0 &&
                optionScrollRect != null)
            {
                optionScrollRect.gameObject.SetActive(true);
            }
            SetActive(
                contractDetailPanel,
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
                RenderOptionActions(combat.Prompt, combat.OptionActions);
                return;
            }

            if (combat.Mode == GameSceneCombatHudMode.ContractCandidates)
            {
                return;
            }

            RenderOptionActions(combat.Prompt, combat.OptionActions);
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

        private void HideCombatControls()
        {
            HideCombatActionTooltip();
            _shopDemonDetailActivatedCombatControlsRoot = false;
            SetActive(combatControlsRoot, false);
            SetActive(optionPanel, false);
            SetActive(contractDetailPanel, false);
            SetActive(automaticCardResultPanel, false);
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
            cardHoverTooltipRoot.pivot = new Vector2(
                cardHoverTooltipRoot.pivot.x,
                showBelow ? 1f : 0f);
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
                    return $"ROUND {core.RoundNumber}";
            }
        }
    }
}
