using System;
using System.Collections;
using System.Collections.Generic;
using DiaBlackJack.CoreLoop;
using DiaBlackJack.CoreLoop.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DiaBlackJack.GameScene
{
    /// <summary>
    /// Owns and drives one standalone CoreLoop battle for the GameScene. The single coordinator: it
    /// holds the <see cref="CoreLoopSession"/>, takes input (temporary IMGUI buttons — the project is
    /// new-Input-System-only, so legacy OnMouseDown / Input.GetKey do not fire), and on every action
    /// re-presents through <see cref="GameScenePresenter"/> into the HUD and the two hands. Rendering
    /// lives in <see cref="GameHudView"/> and <see cref="CardHand"/>; this type only orchestrates.
    /// MVP surface: hit, stand, restart, and a post-victory shop delegated to <see cref="ShopController"/>
    /// (gold reward + merchant + goods on the table + leave). The shop is GameScene-local (no
    /// StageProgression); leaving it restarts into the next battle with gold kept, while a defeat
    /// restart starts a fresh run with gold reset to 0.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] private int seed = 20260719;
        [SerializeField] private GameHudView hud;
        [SerializeField] private CardHand playerHand;
        [SerializeField] private CardHand enemyHand;
        [SerializeField] private CharacterView playerCharacter;
        [SerializeField] private CharacterView enemyCharacter;
        [SerializeField] private TableTotalsView totals;

        [Header("Shop (MVP)")]
        [SerializeField] private ShopController shop;

        [Tooltip("Font for the temporary IMGUI buttons/panels. Leave empty to use Unity's default.")]
        [SerializeField] private Font uiFont;

        [Header("Presentation pacing")]
        [SerializeField] private float stepSeconds = 1.0f;
        [SerializeField] private float resolveHoldSeconds = 1.1f;

        private CoreLoopSession _session;
        private CoreLoopViewModel _core;
        private Camera _camera;
        private CardView _hoveredCard;
        private DemonCardView _hoveredDemonCard;
        private bool _inputLocked;
        private int _battleIndex;
        private GUIStyle _buttonStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _panelStyle;
        private GUIStyle _automaticCardPanelStyle;
        private GUIStyle _contractTitleStyle;
        private GUIStyle _contractBodyStyle;
        private GUIStyle _contractCostStyle;
        private DeckClickable _hoveredDeck;
        private bool _showDemonContractConfirmation;
        private readonly List<GameSceneViewModel> _timeline = new List<GameSceneViewModel>();
        private readonly List<PurchasedNormalCard> _purchasedNormalCards =
            new List<PurchasedNormalCard>();
        private readonly List<string> _purchasedDemonContractKeys = new List<string>();

        public CoreLoopBattle Battle => _session?.Battle;

        private void Awake()
        {
            _session = new CoreLoopSession(CreateBattle);
        }

        private void Start()
        {
            RefreshView();
        }

        // Diegetic input: hover any card to enlarge it (usable cards also glow + show a badge), and
        // click a usable card to activate its effect. New Input System — legacy OnMouseDown does not
        // fire, so we raycast the pointer ourselves. Hit/Stand/Change and the choices stay as OnGUI.
        private void Update()
        {
            if (_core == null)
            {
                return;
            }

            bool hasHit = RaycastPointer(out RaycastHit hit);
            bool shopOpen = shop != null && shop.IsOpen;
            CardView pointedCard = hasHit
                ? hit.collider.GetComponentInParent<CardView>()
                : null;
            CardView pointedBattleCard = shopOpen ? null : pointedCard;
            CardView pointedShopCard = shopOpen ? pointedCard : null;
            DemonCardView pointedDemonCard = shopOpen && hasHit
                ? hit.collider.GetComponentInParent<DemonCardView>()
                : null;

            // Hover is visual-only, so it runs even while input is locked (during timeline playback).
            UpdateHover(shopOpen ? pointedShopCard : pointedBattleCard);
            UpdateDemonCardHover(pointedDemonCard);

            // A deck's card-list panel shows while the pointer hovers it (draw or discard).
            _hoveredDeck = !shopOpen && hasHit
                ? hit.collider.GetComponentInParent<DeckClickable>()
                : null;

            if (_inputLocked)
            {
                return;
            }

            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            {
                return;
            }

            if (pointedBattleCard != null && pointedBattleCard.CanUse)
            {
                int cardId = pointedBattleCard.CardId;
                ProcessInput(() => _session.TryBeginPlayerCardUse(cardId));
                return;
            }

            if (pointedShopCard != null && pointedShopCard.CanUse)
            {
                PurchaseShopNormalCard(pointedShopCard);
                return;
            }

            if (pointedDemonCard != null && pointedDemonCard.CanUse)
            {
                PurchaseShopDemonCard(pointedDemonCard);
            }
        }

        private bool RaycastPointer(out RaycastHit hit)
        {
            hit = default;
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            Mouse mouse = Mouse.current;
            if (_camera == null || mouse == null)
            {
                return false;
            }

            Ray ray = _camera.ScreenPointToRay(mouse.position.ReadValue());
            return Physics.Raycast(ray, out hit, 200f);
        }

        private void UpdateHover(CardView pointed)
        {
            if (pointed == _hoveredCard)
            {
                return;
            }

            if (_hoveredCard != null)
            {
                _hoveredCard.SetHovered(false);
            }

            _hoveredCard = pointed;
            if (_hoveredCard != null)
            {
                _hoveredCard.SetHovered(true);
            }
        }

        private void UpdateDemonCardHover(DemonCardView pointed)
        {
            if (pointed == _hoveredDemonCard)
            {
                return;
            }

            if (_hoveredDemonCard != null)
            {
                _hoveredDemonCard.SetHovered(false);
            }

            _hoveredDemonCard = pointed;
            if (_hoveredDemonCard != null)
            {
                _hoveredDemonCard.SetHovered(true);
            }
        }

        private CoreLoopBattle CreateBattle()
        {
            int battleSeed = seed + (_battleIndex * 2);
            _battleIndex++;
            return new CoreLoopBattle(
                CreatePlayerDeck(battleSeed),
                BlackjackDeck.CreateStandard(battleSeed + 1),
                playerDemonDeck: CreatePlayerDemonDeck(battleSeed + 1000));
        }

        private BlackjackDeck CreatePlayerDeck(int deckSeed)
        {
            var cards = new List<BlackjackCard>(20 + _purchasedNormalCards.Count);
            int id = 0;
            for (int rank = 1; rank <= 10; rank++)
            {
                cards.Add(new BlackjackCard(id++, rank, suit: CardSuit.Spade));
                cards.Add(new BlackjackCard(id++, rank, suit: CardSuit.Clover));
            }

            foreach (PurchasedNormalCard purchasedCard in _purchasedNormalCards)
            {
                CardDefinition definition =
                    CardDefinitionCatalog.GetByKey(purchasedCard.DefinitionKey);
                cards.Add(new BlackjackCard(id++, definition, suit: purchasedCard.Suit));
            }

            return new BlackjackDeck(cards, deckSeed);
        }

        private DemonContractDeck CreatePlayerDemonDeck(int deckSeed)
        {
            DemonContractCatalog catalog = DemonContractCatalog.Default;
            IReadOnlyList<DemonContractDefinition> definitions = catalog.Definitions;
            var cards = new List<DemonContractCard>(
                definitions.Count + _purchasedDemonContractKeys.Count);
            int id = 0;
            foreach (DemonContractDefinition definition in definitions)
            {
                cards.Add(new DemonContractCard(id++, definition));
            }

            foreach (string definitionKey in _purchasedDemonContractKeys)
            {
                cards.Add(new DemonContractCard(id++, catalog.GetByKey(definitionKey)));
            }

            return new DemonContractDeck(cards, deckSeed);
        }

        private void PurchaseShopDemonCard(DemonCardView card)
        {
            if (shop == null || card == null ||
                !shop.TryPurchaseDemonCard(card.CardId, out string definitionKey))
            {
                return;
            }

            _purchasedDemonContractKeys.Add(definitionKey);
            AddPurchasedDemonContractToCurrentBattle(definitionKey);
            RefreshView();
            UpdateDemonCardHover(null);
        }

        private void PurchaseShopNormalCard(CardView card)
        {
            if (shop == null || card == null ||
                !shop.TryPurchaseNormalCard(
                    card.CardId,
                    out string definitionKey,
                    out CardSuit suit))
            {
                return;
            }

            _purchasedNormalCards.Add(new PurchasedNormalCard(definitionKey, suit));
            AddPurchasedNormalCardToCurrentBattle(definitionKey, suit);
            RefreshView();
            UpdateHover(null);
        }

        private void AddPurchasedNormalCardToCurrentBattle(
            string definitionKey,
            CardSuit suit)
        {
            CoreLoopBattle battle = Battle;
            if (battle == null)
            {
                return;
            }

            CardDefinition definition = CardDefinitionCatalog.GetByKey(definitionKey);
            int cardId = FindNextCardId(battle.Player.Deck);
            var card = new BlackjackCard(cardId, definition, suit: suit);
            if (!battle.Player.Deck.TryAddAvailableCard(card))
            {
                throw new InvalidOperationException(
                    "Purchased card could not be added to the battle deck.");
            }
        }

        private void AddPurchasedDemonContractToCurrentBattle(string definitionKey)
        {
            CoreLoopBattle battle = Battle;
            if (battle == null)
            {
                return;
            }

            DemonContractDefinition definition =
                DemonContractCatalog.Default.GetByKey(definitionKey);
            int cardId = battle.PlayerDemonDeck.TotalCardCount;
            var card = new DemonContractCard(cardId, definition);
            if (!battle.PlayerDemonDeck.TryAddAvailableCard(card))
            {
                throw new InvalidOperationException(
                    "Purchased demon contract could not be added to the battle deck.");
            }
        }

        private static int FindNextCardId(BlackjackDeck deck)
        {
            int cardId = deck.TotalCardCount;
            while (cardId < int.MaxValue && deck.ContainsKnownCardId(cardId))
            {
                cardId++;
            }

            if (deck.ContainsKnownCardId(cardId))
            {
                throw new InvalidOperationException("Player card ids are exhausted.");
            }

            return cardId;
        }

        private void OnGUI()
        {
            if (_core == null)
            {
                return;
            }

            _buttonStyle ??= new GUIStyle(GUI.skin.button)
            {
                font = uiFont,
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };
            _labelStyle ??= new GUIStyle(GUI.skin.label)
            {
                font = uiFont,
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            if (_hoveredDeck != null)
            {
                DrawDeckPanel(_hoveredDeck.Kind);
            }

            DrawDemonContractStatusPanel();
            DrawAutomaticCardStatusPanel();

            if (_core.State == CoreLoopState.BattleEnded)
            {
                if (shop != null && shop.IsOpen)
                {
                    DrawHeading("SHOP — 상품을 둘러보고 나가세요");
                    DrawButtonRow(
                        new[] { "상점 나가기" },
                        new[] { true },
                        new Func<bool>[] { LeaveShop });
                }
                else
                {
                    DrawButtonRow(
                        new[] { "RESTART" },
                        new[] { _core.CanRestart },
                        new Func<bool>[] { RestartRun });
                }

                return;
            }

            if (_core.IsChoosingChangeCard)
            {
                DrawChangeCandidates();
                return;
            }

            if (_core.IsResolvingAutomaticCardEffect)
            {
                DrawAutomaticCardChoices();
                return;
            }

            if (_core.IsResolvingCardEffect)
            {
                DrawCardEffectChoices();
                return;
            }

            if (_core.DemonContract.IsResolving)
            {
                DrawDemonContractChoices();
                return;
            }

            if (_showDemonContractConfirmation)
            {
                DrawDemonContractConfirmation();
                return;
            }

            DrawHeading(
                _core.ChangeActionText + "  ·  " +
                _core.DemonContract.ActionText);
            DrawButtonRow(
                new[] { "HIT", "STAND", "CHANGE", "CONTRACT" },
                new[]
                {
                    _core.CanHit,
                    _core.CanStand,
                    _core.CanChange,
                    _core.DemonContract.CanBegin
                },
                new Func<bool>[]
                {
                    _session.TryPlayerHit,
                    _session.TryPlayerStand,
                    _session.TryBeginPlayerChange,
                    BeginDemonContractConfirmation
                });
        }

        private void DrawChangeCandidates()
        {
            var candidates = _core.ChangeCandidates;
            int count = candidates.Count;
            var labels = new string[count];
            var enabled = new bool[count];
            var actions = new Func<bool>[count];
            for (int i = 0; i < count; i++)
            {
                int index = i;
                labels[i] = $"[ {candidates[i]} ]";
                enabled[i] = true;
                actions[i] = () => _session.TrySelectChangedCard(index);
            }

            DrawHeading("CHOOSE A NEW HIDDEN CARD");
            DrawButtonRow(labels, enabled, actions);
        }

        private void DrawCardEffectChoices()
        {
            var choices = _core.CardEffectChoices;
            int count = choices.Count;
            var labels = new string[count];
            var enabled = new bool[count];
            var actions = new Func<bool>[count];
            for (int i = 0; i < count; i++)
            {
                CardEffectChoiceViewModel choice = choices[i];
                labels[i] = choice.Label;
                enabled[i] = true;
                actions[i] = () => _session.TryResolvePlayerCardChoice(choice.OptionId);
            }

            DrawHeading(_core.CardEffectPrompt);
            DrawButtonRow(labels, enabled, actions);
        }

        private void DrawAutomaticCardChoices()
        {
            AutomaticCardInteractionViewModel interaction =
                _core.AutomaticCardInteraction;
            if (interaction == null)
            {
                DrawHeading("ENEMY AUTOMATIC DECISION");
                return;
            }

            int count = interaction.Choices.Count;
            var labels = new string[count];
            var enabled = new bool[count];
            var actions = new Func<bool>[count];
            for (int i = 0; i < count; i++)
            {
                AutomaticCardChoiceViewModel choice =
                    interaction.Choices[i];
                int interactionId = interaction.InteractionId;
                int optionId = choice.OptionId;
                labels[i] = choice.Label;
                enabled[i] = true;
                actions[i] = () =>
                    _session.TryResolvePlayerAutomaticCardChoice(
                        interactionId,
                        optionId);
            }

            DrawHeading(
                $"{interaction.SourceDisplayName}  |  {interaction.Prompt}");
            DrawButtonRow(labels, enabled, actions);
        }

        private void DrawDemonContractConfirmation()
        {
            DemonContractPanelViewModel contract = _core.DemonContract;
            DrawHeading(
                $"영혼 {contract.SoulCost} 지불 · 계약 후 {contract.SoulAfterCost} · " +
                "지불 뒤 후보 하나 필수 선택");
            DrawButtonRow(
                new[] { "CONFIRM CONTRACT", "CANCEL" },
                new[] { contract.CanBegin, true },
                new Func<bool>[] { ConfirmDemonContract, CancelDemonContract });
        }

        private void DrawDemonContractChoices()
        {
            DemonContractPanelViewModel contract = _core.DemonContract;
            int count = contract.Choices.Count;
            string heading = contract.Prompt;
            if (!string.IsNullOrEmpty(contract.OwnerPreview))
            {
                heading += "  |  " + contract.OwnerPreview;
            }

            if (contract.InteractionKind != DemonContractInteractionKind.ChooseContract)
            {
                var labels = new string[count];
                var enabled = new bool[count];
                var actions = new Func<bool>[count];
                for (int i = 0; i < count; i++)
                {
                    DemonContractChoiceViewModel choice = contract.Choices[i];
                    labels[i] = choice.Title;
                    enabled[i] = choice.CanSelect;
                    actions[i] = () => contract.InteractionId.HasValue &&
                        _session.TryResolvePlayerDemonContract(
                            contract.InteractionId.Value,
                            choice.OptionId);
                }

                const float optionHeight = 64f;
                DrawHeading(heading, optionHeight);
                DrawButtonRow(labels, enabled, actions, optionHeight, maxWidth: 300f);
                return;
            }

            bool compact = Screen.height <= 720;
            float rowHeight = compact ? 166f : 204f;
            DrawHeading(heading, rowHeight);
            DrawDemonContractCandidateCards(contract, rowHeight, compact);
        }

        private void DrawDemonContractCandidateCards(
            DemonContractPanelViewModel contract,
            float height,
            bool compact)
        {
            EnsureDemonContractStyles(compact);
            int count = contract.Choices.Count;
            const float gap = 12f;
            float width = Mathf.Min(
                380f,
                (Screen.width - 40f - (count - 1) * gap) / count);
            float totalWidth = count * width + (count - 1) * gap;
            float x = (Screen.width - totalWidth) * 0.5f;
            float y = Screen.height - height - 24f;

            for (int i = 0; i < count; i++)
            {
                DemonContractChoiceViewModel choice = contract.Choices[i];
                var cardRect = new Rect(x + i * (width + gap), y, width, height);
                GUI.Box(cardRect, string.Empty);
                float inset = compact ? 10f : 14f;
                float titleHeight = compact ? 24f : 30f;
                float buttonHeight = compact ? 30f : 38f;
                float contentWidth = width - inset * 2f;

                GUI.Label(
                    new Rect(cardRect.x + inset, cardRect.y + 6f, contentWidth, titleHeight),
                    choice.Title,
                    _contractTitleStyle);
                GUI.Label(
                    new Rect(
                        cardRect.x + inset,
                        cardRect.y + titleHeight + 8f,
                        contentWidth,
                        compact ? 55f : 74f),
                    choice.Ability,
                    _contractBodyStyle);
                GUI.Label(
                    new Rect(
                        cardRect.x + inset,
                        cardRect.y + (compact ? 88f : 116f),
                        contentWidth,
                        compact ? 38f : 48f),
                    choice.Cost,
                    _contractCostStyle);

                using (new GUIEnabledScope(!_inputLocked && choice.CanSelect))
                {
                    string buttonLabel = choice.CanSelect
                        ? "SELECT"
                        : choice.DisabledReason;
                    if (GUI.Button(
                        new Rect(
                            cardRect.x + inset,
                            cardRect.yMax - buttonHeight - 8f,
                            contentWidth,
                            buttonHeight),
                        buttonLabel,
                        _buttonStyle) &&
                        contract.InteractionId.HasValue)
                    {
                        int interactionId = contract.InteractionId.Value;
                        int optionId = choice.OptionId;
                        ProcessInput(() => _session.TryResolvePlayerDemonContract(
                            interactionId,
                            optionId));
                    }
                }
            }
        }

        private void EnsureDemonContractStyles(bool compact)
        {
            int titleSize = compact ? 18 : 22;
            if (_contractTitleStyle != null &&
                _contractTitleStyle.fontSize == titleSize)
            {
                return;
            }

            _contractTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = titleSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            _contractBodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = compact ? 13 : 16,
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
                normal = { textColor = new Color(0.92f, 0.92f, 0.92f) }
            };
            _contractCostStyle = new GUIStyle(_contractBodyStyle)
            {
                fontSize = compact ? 12 : 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.72f, 0.3f) }
            };
        }

        private bool BeginDemonContractConfirmation()
        {
            if (!_core.DemonContract.CanBegin)
            {
                return false;
            }

            _showDemonContractConfirmation = true;
            return true;
        }

        private bool ConfirmDemonContract()
        {
            _showDemonContractConfirmation = false;
            return _session.TryBeginPlayerDemonContract();
        }

        private bool CancelDemonContract()
        {
            _showDemonContractConfirmation = false;
            return true;
        }

        // Bottom-anchored, screen-centered row. Width shrinks to always fit one row on screen.
        private void DrawButtonRow(
            string[] labels,
            bool[] enabled,
            Func<bool>[] actions,
            float height = 48f,
            float maxWidth = 160f)
        {
            int n = labels.Length;
            if (n == 0)
            {
                return;
            }

            const float gap = 8f;
            float w = Mathf.Min(
                maxWidth,
                (Screen.width - 40f - (n - 1) * gap) / n);
            float totalWidth = n * w + (n - 1) * gap;
            float x0 = (Screen.width - totalWidth) * 0.5f;
            float y = Screen.height - height - 24f;

            for (int i = 0; i < n; i++)
            {
                using (new GUIEnabledScope(!_inputLocked && enabled[i]))
                {
                    if (GUI.Button(
                        new Rect(x0 + i * (w + gap), y, w, height),
                        labels[i],
                        _buttonStyle))
                    {
                        ProcessInput(actions[i]);
                    }
                }
            }
        }

        private void DrawDeckPanel(DeckKind kind)
        {
            _panelStyle ??= new GUIStyle(GUI.skin.box)
            {
                font = uiFont,
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter,
                padding = new RectOffset(18, 18, 18, 18),
                normal = { textColor = Color.white }
            };

            const float w = 430f;
            const float h = 200f;
            bool draw = kind == DeckKind.Draw;
            string content = draw
                ? GameScenePresenter.FormatDrawDeck(Battle)
                : GameScenePresenter.FormatDiscardDeck(Battle);
            // Draw-deck panel on the left, discard-deck panel on the right, so they never overlap.
            float x = draw ? 28f : Screen.width - w - 28f;
            var rect = new Rect(x, (Screen.height - h) * 0.5f, w, h);
            GUI.Box(rect, content, _panelStyle);
        }

        private void DrawDemonContractStatusPanel()
        {
            DemonContractPanelViewModel contract = _core.DemonContract;
            if (contract.ActiveContracts.Count == 0 &&
                string.IsNullOrEmpty(contract.LastContractResult) &&
                string.IsNullOrEmpty(contract.LastEffectResult))
            {
                return;
            }

            _panelStyle ??= new GUIStyle(GUI.skin.box)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft,
                padding = new RectOffset(12, 12, 10, 10),
                normal = { textColor = Color.white }
            };

            var lines = new List<string> { "DEMON CONTRACT" };
            lines.AddRange(contract.ActiveContracts);
            if (!string.IsNullOrEmpty(contract.LastContractResult))
            {
                lines.Add(contract.LastContractResult);
            }

            if (!string.IsNullOrEmpty(contract.LastEffectResult))
            {
                lines.Add(contract.LastEffectResult);
            }

            const float width = 450f;
            float height = 32f + lines.Count * 24f;
            GUI.Box(
                new Rect(Screen.width - width - 20f, 20f, width, height),
                string.Join("\n", lines),
                _panelStyle);
        }

        private void DrawAutomaticCardStatusPanel()
        {
            AutomaticCardResultViewModel result =
                _core.AutomaticCardResult;
            if (result == null)
            {
                return;
            }

            _automaticCardPanelStyle ??=
                new GUIStyle(GUI.skin.box)
                {
                    font = uiFont,
                    fontSize = Screen.height <= 720 ? 14 : 16,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true,
                    padding = new RectOffset(12, 12, 10, 10),
                    normal = { textColor = Color.white }
                };

            string content = "AUTOMATIC CARD\n" +
                result.PublicSummary;
            if (!string.IsNullOrEmpty(result.PrivateSummary))
            {
                content += "\n" + result.PrivateSummary;
            }

            float width = Mathf.Min(720f, Screen.width - 40f);
            float height = string.IsNullOrEmpty(result.PrivateSummary)
                ? 104f
                : 128f;
            GUI.Box(
                new Rect(
                    (Screen.width - width) * 0.5f,
                    88f,
                    width,
                    height),
                content,
                _automaticCardPanelStyle);
        }

        private void DrawHeading(string text, float rowHeight = 48f)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            const float h = 30f;
            float y = Screen.height - rowHeight - 24f - h - 6f;
            GUI.Label(new Rect(0f, y, Screen.width, h), text, _labelStyle);
        }

        private void ProcessInput(Func<bool> action)
        {
            if (_inputLocked || action == null)
            {
                return;
            }

            _inputLocked = true;

            // The battle runs the whole turn synchronously; Stepped fires once per sub-step, so we
            // snapshot each into a timeline and then pace them out over PlayTimeline.
            CoreLoopBattle battle = Battle;
            _timeline.Clear();
            if (battle != null)
            {
                battle.Stepped += OnBattleStepped;
            }

            bool accepted = action();

            if (battle != null)
            {
                battle.Stepped -= OnBattleStepped;
            }

            if (accepted && Application.isPlaying && _timeline.Count > 0)
            {
                StartCoroutine(PlayTimeline());
            }
            else
            {
                RefreshView();
                UnlockInput();
            }
        }

        // Fires synchronously for each sub-step while the battle resolves the turn. Snapshots the
        // public view state at that instant so PlayTimeline can reveal them one beat at a time.
        private void OnBattleStepped()
        {
            _timeline.Add(GameScenePresenter.Create(Battle));
        }

        private IEnumerator PlayTimeline()
        {
            foreach (GameSceneViewModel vm in _timeline)
            {
                ApplyView(vm);

                bool resolveBeat = vm.Core.State == CoreLoopState.ResolvingRound;
                yield return new WaitForSeconds(resolveBeat ? resolveHoldSeconds : stepSeconds);
            }

            // Land on the true current state — e.g. BattleEnded, which is not itself a step.
            RefreshView();
            UnlockInput();
        }

        private void RefreshView()
        {
            GameSceneViewModel vm = GameScenePresenter.Create(Battle);
            MaybeOpenShop(vm);
            ApplyView(vm);
        }

        private void ApplyView(GameSceneViewModel vm)
        {
            _core = vm.Core;

            if (hud != null)
            {
                hud.Render(vm.Core);
                hud.SetGold(shop != null ? shop.Gold : 0);
            }

            // While the shop is open its presentation (merchant, hidden combat objects, goods) is owned
            // by ShopController; skip the combat re-render so it doesn't repaint the enemy over the merchant.
            if (shop != null && shop.IsOpen)
            {
                return;
            }

            if (playerHand != null)
            {
                playerHand.Render(vm.PlayerCards);
            }

            if (enemyHand != null)
            {
                enemyHand.Render(vm.EnemyCards);
            }

            if (playerCharacter != null)
            {
                playerCharacter.Render(vm.PlayerVisual, vm.PlayerActionLabel);
            }

            if (enemyCharacter != null)
            {
                enemyCharacter.Render(vm.EnemyVisual, vm.EnemyActionLabel);
            }

            if (totals != null)
            {
                totals.Render(vm.Core.PlayerTotal, vm.Core.EnemyVisibleTotal);
            }
        }

        // Open the shop the moment a battle is won. Called from RefreshView, which lands on the true
        // post-turn state (BattleEnded is not itself a Stepped beat). ShopController.Open guards against
        // repeat opens, so this fires the shop exactly once per victory; a defeat opens no shop.
        private void MaybeOpenShop(GameSceneViewModel vm)
        {
            if (shop == null || shop.IsOpen ||
                vm.Core.State != CoreLoopState.BattleEnded ||
                vm.Core.Outcome != BattleOutcome.PlayerVictory)
            {
                return;
            }

            shop.Open();
        }

        // Leave the shop and start the next battle. Gold is KEPT by ShopController — it accumulates
        // across the run's battles; only a defeat restart resets it. TryRestart swaps in a fresh battle
        // and emits no Stepped events, so ProcessInput re-presents immediately via RefreshView.
        private bool LeaveShop()
        {
            bool restarted = _session.TryRestart();
            if (restarted && shop != null)
            {
                _showDemonContractConfirmation = false;
                UpdateHover(null);
                UpdateDemonCardHover(null);
                shop.Close();
            }

            return restarted;
        }

        // Restart after a defeat: a fresh run, so the shop closes (a no-op if it was never open) and
        // gold returns to 0.
        private bool RestartRun()
        {
            bool restarted = _session.TryRestart();
            if (restarted && shop != null)
            {
                _showDemonContractConfirmation = false;
                _purchasedNormalCards.Clear();
                _purchasedDemonContractKeys.Clear();
                UpdateHover(null);
                UpdateDemonCardHover(null);
                shop.Close();
                shop.ResetGold();
            }

            return restarted;
        }

        private void UnlockInput()
        {
            _inputLocked = false;
        }

        private readonly struct PurchasedNormalCard
        {
            public PurchasedNormalCard(string definitionKey, CardSuit suit)
            {
                DefinitionKey = definitionKey ?? string.Empty;
                Suit = suit;
            }

            public string DefinitionKey { get; }

            public CardSuit Suit { get; }
        }

        private readonly struct GUIEnabledScope : IDisposable
        {
            private readonly bool _previous;

            public GUIEnabledScope(bool enabled)
            {
                _previous = GUI.enabled;
                GUI.enabled = enabled;
            }

            public void Dispose()
            {
                GUI.enabled = _previous;
            }
        }
    }
}
