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

        [Header("Standalone enemy profile")]
        [SerializeField] private string enemyProfileKey =
            EnemyCombatProfileCatalog.GunslingerKey;

        [Header("Shop (MVP)")]
        [SerializeField] private ShopController shop;

        [Tooltip("Font for the temporary IMGUI buttons/panels. Leave empty to use Unity's default.")]
        [SerializeField] private Font uiFont;

        [Header("Presentation pacing")]
        [SerializeField] private float stepSeconds = 1.0f;
        [SerializeField] private float resolveHoldSeconds = 1.1f;

        [Header("Revolver animation")]
        [SerializeField] private Animator revolverAnimator;
        [SerializeField] private GameObject revolverRoot;
        [SerializeField] private float revolverAnimationSeconds = 8.8f;
        [SerializeField] private string revolverBaseStateName = "Revolver_Base";
        [SerializeField] private string playerReadyTrigger = "PlayerTurnStart";
        [SerializeField] private string playerSuccessTrigger = "PlayerSuccess";
        [SerializeField] private string playerFailTrigger = "PlayerFail";
        [SerializeField] private string enemySuccessTrigger = "EnemySuccess";
        [SerializeField] private string enemyFailTrigger = "EnemyFail";

        [Header("Hammer animation")]
        [SerializeField] private HammerAnimationController hammerAnimation;

        [Header("Cinematic camera")]
        [SerializeField] private GameSceneCameraViewController cameraViewController;

        private CoreLoopSession _session;
        private CoreLoopViewModel _core;
        private Camera _camera;
        private CardView _hoveredCard;
        private DemonCardView _hoveredDemonCard;
        private ShopUtilityItemView _hoveredShopUtilityItem;
        private bool _inputLocked;
        private bool _choosingLighterRemoval;
        private int _battleIndex;
        private string _activeEnemyProfileKey;
        private GUIStyle _buttonStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _panelStyle;
        private GUIStyle _automaticCardPanelStyle;
        private GUIStyle _contractTitleStyle;
        private GUIStyle _contractBodyStyle;
        private GUIStyle _contractCostStyle;
        private GUIStyle _shopPanelStyle;
        private GUIStyle _shopCardButtonStyle;
        private Vector2 _lighterRemovalScroll;
        private DeckClickable _hoveredDeck;
        private bool _showDemonContractConfirmation;
        private bool _hasLastRevolverAnimationCue;
        private int _lastRevolverAnimationRoundNumber;
        private int _lastRevolverAnimationSourceCardId;
        private CombatantSide _lastRevolverAnimationActorSide;
        private GameSceneRevolverAnimationPhase _lastRevolverAnimationPhase;
        private bool _lastRevolverAnimationSucceeded;
        private bool _revolverReadyActive;
        private int _revolverReadyRoundNumber;
        private int _revolverReadySourceCardId;
        private CombatantSide _revolverReadyActorSide;
        private Coroutine _revolverHideRoutine;
        private bool _hammerSwitchInputLocked;
        private bool _returnCameraToCurrentAfterHammer;
        private HammerAnimationController _hammerCameraLockController;
        private HammerAnimationController _playedHammerAnimationController;
        private readonly List<GameSceneViewModel> _timeline = new List<GameSceneViewModel>();
        private readonly List<PurchasedNormalCard> _purchasedNormalCards =
            new List<PurchasedNormalCard>();
        private readonly List<string> _purchasedDemonContractKeys = new List<string>();
        private readonly List<RemovedNormalCard> _removedNormalCards =
            new List<RemovedNormalCard>();

        public CoreLoopBattle Battle => _session?.Battle;

        private void Awake()
        {
            HideRevolverAnimation();
            ResolveHammerAnimation()?.Hide();
            _activeEnemyProfileKey = ResolveEnemyProfileKey();
            if (enemyCharacter != null)
            {
                enemyCharacter.TrySetEnemyProfile(_activeEnemyProfileKey);
            }

            _session = new CoreLoopSession(CreateBattle);
        }

        private void Start()
        {
            RefreshView();
        }

        // Diegetic input: hover any card to enlarge it (usable cards also glow + show a HUD badge), and
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
            ShopUtilityItemView pointedShopUtilityItem = shopOpen && hasHit
                ? hit.collider.GetComponentInParent<ShopUtilityItemView>()
                : null;

            // Hover is visual-only, so it runs even while input is locked (during timeline playback).
            UpdateHover(shopOpen ? pointedShopCard : pointedBattleCard);
            UpdateCardHoverBadge();
            UpdateDemonCardHover(pointedDemonCard);
            UpdateShopUtilityItemHover(pointedShopUtilityItem);

            // A deck's card-list panel shows while the pointer hovers it (draw or discard).
            _hoveredDeck = !shopOpen && hasHit
                ? hit.collider.GetComponentInParent<DeckClickable>()
                : null;

            if (_inputLocked || _choosingLighterRemoval)
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
                return;
            }

            if (pointedShopUtilityItem != null && pointedShopUtilityItem.CanUse)
            {
                UseShopUtilityItem(pointedShopUtilityItem);
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

        private void UpdateCardHoverBadge()
        {
            if (hud == null)
            {
                return;
            }

            if (_camera == null)
            {
                _camera = Camera.main;
            }

            if (_hoveredCard == null ||
                !_hoveredCard.ShouldShowHoverBadge ||
                !_hoveredCard.TryGetHoverBadgeScreenPosition(
                    _camera,
                    out Vector2 screenPosition))
            {
                hud.HideCardHoverBadge();
                return;
            }

            hud.ShowCardHoverBadge(
                _hoveredCard.HoverBadgeText,
                screenPosition,
                _camera);
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

        private void UpdateShopUtilityItemHover(ShopUtilityItemView pointed)
        {
            if (pointed == _hoveredShopUtilityItem)
            {
                return;
            }

            if (_hoveredShopUtilityItem != null)
            {
                _hoveredShopUtilityItem.SetHovered(false);
            }

            _hoveredShopUtilityItem = pointed;
            if (_hoveredShopUtilityItem != null)
            {
                _hoveredShopUtilityItem.SetHovered(true);
            }
        }

        private CoreLoopBattle CreateBattle()
        {
            ResetRevolverAnimationState();
            int battleSeed = seed + (_battleIndex * 2);
            _battleIndex++;
            int enemyDeckSeed = battleSeed + 1;
            EnemyBattleConfiguration enemy =
                EnemyBattleConfigurationFactory.Create(
                    _activeEnemyProfileKey,
                    enemyDeckSeed);
            return new CoreLoopBattle(
                CreatePlayerDeck(battleSeed),
                enemy.CreateEnemyDeck(),
                enemyMaximumSoul: enemy.EnemyMaximumSoul,
                enemyPolicy: enemy.BehaviorPolicy,
                playerDemonDeck: CreatePlayerDemonDeck(battleSeed + 1000),
                enemyDemonDeck: CreateEnemyDemonDeck(
                    enemy.DemonContractDefinitionKeys,
                    enemyDeckSeed ^ unchecked((int)0x4C957F2Du)),
                enemyChangeCostMode: enemy.ChangeCostMode,
                enemyDemonContractCandidateCount:
                    enemy.DemonContractCandidateCount,
                injectsPoisonIntoPlayerDeckEachRound:
                    enemy.InjectsPoisonIntoPlayerDeckEachRound,
                enablesEnemyChange: true,
                fixedEnemyDemonContractPhases:
                    enemy.FixedDemonContractPhases);
        }

        private string ResolveEnemyProfileKey()
        {
            try
            {
                EnemyCombatProfileCatalog.Default.GetByKey(enemyProfileKey);
                return enemyProfileKey;
            }
            catch (Exception exception)
                when (exception is ArgumentException ||
                      exception is KeyNotFoundException)
            {
                Debug.LogWarning(
                    $"Enemy profile '{enemyProfileKey}' is invalid. " +
                    $"Falling back to '{EnemyCombatProfileCatalog.GunslingerKey}'.",
                    this);
                return EnemyCombatProfileCatalog.GunslingerKey;
            }
        }

        private BlackjackDeck CreatePlayerDeck(int deckSeed)
        {
            var cards = new List<BlackjackCard>(20 + _purchasedNormalCards.Count);
            int id = 0;
            for (int rank = 1; rank <= 10; rank++)
            {
                CardDefinition definition = CardDefinitionCatalog.GetDefaultForRank(rank);
                if (!IsBaseNormalCardRemoved(definition.Key, CardSuit.Spade))
                {
                    cards.Add(new BlackjackCard(id++, definition, suit: CardSuit.Spade));
                }

                if (!IsBaseNormalCardRemoved(definition.Key, CardSuit.Clover))
                {
                    cards.Add(new BlackjackCard(id++, definition, suit: CardSuit.Clover));
                }
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

        private static DemonContractDeck CreateEnemyDemonDeck(
            IReadOnlyList<string> definitionKeys,
            int deckSeed)
        {
            List<DemonContractCard> cards =
                new List<DemonContractCard>(definitionKeys.Count);
            DemonContractCatalog catalog = DemonContractCatalog.Default;
            for (int i = 0; i < definitionKeys.Count; i++)
            {
                cards.Add(
                    new DemonContractCard(
                        i,
                        catalog.GetByKey(definitionKeys[i])));
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

        private void UseShopUtilityItem(ShopUtilityItemView item)
        {
            if (item == null)
            {
                return;
            }

            switch (item.Kind)
            {
                case ShopUtilityItemKind.Lighter:
                    BeginLighterRemoval();
                    break;
                case ShopUtilityItemKind.Whiskey:
                    PurchaseWhiskey();
                    break;
            }
        }

        private void BeginLighterRemoval()
        {
            if (shop == null || !shop.IsOpen || BuildRunDeckCardOptions().Count <= 1)
            {
                return;
            }

            _choosingLighterRemoval = true;
            UpdateShopUtilityItemHover(null);
            RefreshShopUtilityItems();
        }

        private bool RemoveCardWithLighter(int optionIndex)
        {
            if (shop == null || !shop.IsOpen)
            {
                return false;
            }

            List<RunDeckCardOption> options = BuildRunDeckCardOptions();
            if (optionIndex < 0 ||
                optionIndex >= options.Count ||
                options.Count <= 1)
            {
                return false;
            }

            RunDeckCardOption option = options[optionIndex];
            if (!CanRemoveRunDeckCard(option) ||
                !shop.TryPurchaseLighterRemoval(options.Count))
            {
                RefreshShopUtilityItems();
                return false;
            }

            RemoveRunDeckCard(option);
            _choosingLighterRemoval = false;
            RefreshView();
            return true;
        }

        private bool CancelLighterRemoval()
        {
            _choosingLighterRemoval = false;
            RefreshShopUtilityItems();
            return true;
        }

        private void PurchaseWhiskey()
        {
            CoreLoopBattle battle = Battle;
            if (shop == null ||
                battle == null ||
                !shop.TryPurchaseWhiskey(
                    battle.Player.Soul.Current,
                    battle.Player.Soul.Maximum,
                    out int restoreAmount))
            {
                RefreshShopUtilityItems();
                return;
            }

            battle.Player.Soul.Restore(restoreAmount);
            RefreshView();
            UpdateShopUtilityItemHover(null);
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

        private List<RunDeckCardOption> BuildRunDeckCardOptions()
        {
            var options = new List<RunDeckCardOption>(20 + _purchasedNormalCards.Count);
            for (int rank = 1; rank <= 10; rank++)
            {
                CardDefinition definition = CardDefinitionCatalog.GetDefaultForRank(rank);
                AddBaseRunDeckCardOption(options, definition, CardSuit.Spade);
                AddBaseRunDeckCardOption(options, definition, CardSuit.Clover);
            }

            for (int i = 0; i < _purchasedNormalCards.Count; i++)
            {
                PurchasedNormalCard card = _purchasedNormalCards[i];
                options.Add(new RunDeckCardOption(
                    card.DefinitionKey,
                    card.Suit,
                    isPurchased: true,
                    purchasedIndex: i));
            }

            return options;
        }

        private void AddBaseRunDeckCardOption(
            List<RunDeckCardOption> options,
            CardDefinition definition,
            CardSuit suit)
        {
            if (definition != null && !IsBaseNormalCardRemoved(definition.Key, suit))
            {
                options.Add(new RunDeckCardOption(
                    definition.Key,
                    suit,
                    isPurchased: false,
                    purchasedIndex: -1));
            }
        }

        private bool CanRemoveRunDeckCard(RunDeckCardOption option)
        {
            if (option.IsPurchased)
            {
                return option.PurchasedIndex >= 0 &&
                    option.PurchasedIndex < _purchasedNormalCards.Count &&
                    _purchasedNormalCards[option.PurchasedIndex].Matches(
                        option.DefinitionKey,
                        option.Suit);
            }

            return !IsBaseNormalCardRemoved(option.DefinitionKey, option.Suit);
        }

        private void RemoveRunDeckCard(RunDeckCardOption option)
        {
            if (option.IsPurchased)
            {
                _purchasedNormalCards.RemoveAt(option.PurchasedIndex);
                return;
            }

            _removedNormalCards.Add(new RemovedNormalCard(
                option.DefinitionKey,
                option.Suit));
        }

        private bool IsBaseNormalCardRemoved(string definitionKey, CardSuit suit)
        {
            foreach (RemovedNormalCard card in _removedNormalCards)
            {
                if (card.Matches(definitionKey, suit))
                {
                    return true;
                }
            }

            return false;
        }

        private string BuildRunDeckCardLabel(RunDeckCardOption option)
        {
            CardDefinition definition = CardDefinitionCatalog.GetByKey(option.DefinitionKey);
            string source = option.IsPurchased ? "BOUGHT" : "BASE";
            return definition.Rank + " " + FormatSuit(option.Suit) +
                "\n" + definition.DisplayName +
                "\n" + source;
        }

        private static string FormatSuit(CardSuit suit)
        {
            return suit == CardSuit.Clover ? "CLOVER" : "SPADE";
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

            DrawAutomaticCardStatusPanel();

            if (_core.State == CoreLoopState.BattleEnded)
            {
                if (shop != null && shop.IsOpen)
                {
                    DrawShopControls();
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
            var labels = new List<string>
            {
                "HIT", "STAND", "CHANGE", "CONTRACT"
            };
            var enabled = new List<bool>
            {
                _core.CanHit,
                _core.CanStand,
                _core.CanChange,
                _core.DemonContract.CanBegin
            };
            var actions = new List<Func<bool>>
            {
                _session.TryPlayerHit,
                _session.TryPlayerStand,
                _session.TryBeginPlayerChange,
                BeginDemonContractConfirmation
            };
            foreach (ActiveDemonContractActionViewModel action in
                _core.DemonContract.ActiveActions)
            {
                int sourceCardId = action.SourceCardId;
                labels.Add(action.Label);
                enabled.Add(true);
                actions.Add(() =>
                    _session.TryBeginPlayerActiveDemonContractAction(
                        sourceCardId));
            }

            DrawButtonRow(
                labels.ToArray(),
                enabled.ToArray(),
                actions.ToArray());
        }

        private void DrawShopControls()
        {
            if (_choosingLighterRemoval)
            {
                DrawLighterRemovalPanel();
                return;
            }

            DrawHeading("SHOP - hover goods and click to buy");
            DrawButtonRow(
                new[] { "나가기" },
                new[] { true },
                new Func<bool>[] { LeaveShop });
        }

        private void DrawLighterRemovalPanel()
        {
            List<RunDeckCardOption> options = BuildRunDeckCardOptions();
            EnsureShopStyles();

            float width = Mathf.Min(760f, Screen.width - 40f);
            float height = Mathf.Min(520f, Screen.height - 120f);
            var panelRect = new Rect(
                (Screen.width - width) * 0.5f,
                70f,
                width,
                height);
            GUI.Box(panelRect, string.Empty, _shopPanelStyle);

            GUI.Label(
                new Rect(panelRect.x + 18f, panelRect.y + 14f, width - 36f, 30f),
                "LIGHTER - CHOOSE 1 CARD TO REMOVE",
                _labelStyle);

            int columns = Mathf.Clamp(Mathf.FloorToInt((width - 36f) / 132f), 3, 5);
            const float gap = 8f;
            float cardWidth = (width - 36f - (columns - 1) * gap) / columns;
            const float cardHeight = 74f;
            int rows = Mathf.CeilToInt(options.Count / (float)columns);
            var scrollRect = new Rect(
                panelRect.x + 18f,
                panelRect.y + 58f,
                width - 36f,
                height - 122f);
            var contentRect = new Rect(
                0f,
                0f,
                scrollRect.width - 18f,
                Mathf.Max(scrollRect.height, rows * (cardHeight + gap)));

            _lighterRemovalScroll = GUI.BeginScrollView(
                scrollRect,
                _lighterRemovalScroll,
                contentRect);
            for (int i = 0; i < options.Count; i++)
            {
                int index = i;
                int row = i / columns;
                int column = i % columns;
                var cardRect = new Rect(
                    column * (cardWidth + gap),
                    row * (cardHeight + gap),
                    cardWidth,
                    cardHeight);

                using (new GUIEnabledScope(!_inputLocked && options.Count > 1))
                {
                    if (GUI.Button(
                        cardRect,
                        BuildRunDeckCardLabel(options[i]),
                        _shopCardButtonStyle))
                    {
                        RemoveCardWithLighter(index);
                    }
                }
            }

            GUI.EndScrollView();

            using (new GUIEnabledScope(!_inputLocked))
            {
                const float footerButtonWidth = 160f;
                const float footerGap = 12f;
                float footerX = panelRect.x +
                    (width - footerButtonWidth * 2f - footerGap) * 0.5f;
                if (GUI.Button(
                    new Rect(
                        footerX,
                        panelRect.yMax - 52f,
                        footerButtonWidth,
                        38f),
                    "CANCEL",
                    _buttonStyle))
                {
                    CancelLighterRemoval();
                }

                if (GUI.Button(
                    new Rect(
                        footerX + footerButtonWidth + footerGap,
                        panelRect.yMax - 52f,
                        footerButtonWidth,
                        38f),
                    "나가기",
                    _buttonStyle))
                {
                    ProcessInput(LeaveShop);
                }
            }
        }

        private void EnsureShopStyles()
        {
            _shopPanelStyle ??= new GUIStyle(GUI.skin.box)
            {
                font = uiFont,
                fontSize = 16,
                alignment = TextAnchor.UpperCenter,
                padding = new RectOffset(14, 14, 14, 14),
                normal = { textColor = Color.white }
            };
            _shopCardButtonStyle ??= new GUIStyle(GUI.skin.button)
            {
                font = uiFont,
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
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

            if (!contract.UsesContractCandidateLayout)
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
                Sprite faceSprite = shop == null
                    ? null
                    : shop.GetDemonCardFaceSprite(choice.DefinitionKey);
                float textX = cardRect.x + inset;
                float textWidth = contentWidth;
                if (faceSprite != null)
                {
                    float artHeight = height - buttonHeight - (compact ? 22f : 26f);
                    float artWidth = Mathf.Min(
                        artHeight * faceSprite.rect.width / faceSprite.rect.height,
                        contentWidth * 0.32f);
                    var artRect = new Rect(
                        cardRect.x + inset,
                        cardRect.y + 7f,
                        artWidth,
                        artHeight);
                    DrawSprite(faceSprite, artRect);
                    textX = artRect.xMax + inset;
                    textWidth = cardRect.xMax - inset - textX;
                }

                GUI.Label(
                    new Rect(textX, cardRect.y + 6f, textWidth, titleHeight),
                    choice.Title,
                    _contractTitleStyle);
                GUI.Label(
                    new Rect(
                        textX,
                        cardRect.y + titleHeight + 8f,
                        textWidth,
                        compact ? 55f : 74f),
                    choice.Ability,
                    _contractBodyStyle);
                GUI.Label(
                    new Rect(
                        textX,
                        cardRect.y + (compact ? 88f : 116f),
                        textWidth,
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

        private static void DrawSprite(Sprite sprite, Rect destination)
        {
            if (sprite == null || sprite.texture == null)
            {
                return;
            }

            Texture2D texture = sprite.texture;
            Vector2[] uvs = sprite.uv;
            if (uvs == null || uvs.Length == 0)
            {
                GUI.DrawTexture(destination, texture, ScaleMode.ScaleToFit, true);
                return;
            }

            Vector2 minimum = uvs[0];
            Vector2 maximum = uvs[0];
            for (int i = 1; i < uvs.Length; i++)
            {
                minimum = Vector2.Min(minimum, uvs[i]);
                maximum = Vector2.Max(maximum, uvs[i]);
            }

            var coordinates = new Rect(
                minimum.x,
                minimum.y,
                maximum.x - minimum.x,
                maximum.y - minimum.y);
            GUI.DrawTextureWithTexCoords(destination, texture, coordinates, true);
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
            _timeline.Add(GameScenePresenter.Create(Battle, _activeEnemyProfileKey));
        }

        private IEnumerator PlayTimeline()
        {
            List<GameSceneViewModel> timeline =
                new List<GameSceneViewModel>(_timeline);
            _timeline.Clear();

            foreach (GameSceneViewModel vm in timeline)
            {
                AppliedAnimationResult playedAnimation = ApplyView(
                    vm,
                    scheduleRevolverRetry: false,
                    deferHammerSmashCardRender: true);

                bool resolveBeat = vm.Core.State == CoreLoopState.ResolvingRound;
                float waitSeconds = resolveBeat ? resolveHoldSeconds : stepSeconds;
                if (playedAnimation.PlayedAny)
                {
                    waitSeconds = Mathf.Max(
                        waitSeconds,
                        playedAnimation.WaitSeconds);
                }

                yield return WaitForAnimationOrSeconds(
                    playedAnimation,
                    waitSeconds);

                if (playedAnimation.DeferredCardRender)
                {
                    RenderHands(playedAnimation.DeferredViewModel);
                }

                GameSceneRevolverAnimationCue revolverCue =
                    vm.RevolverAnimationCue;
                if (playedAnimation.PlayedRevolver &&
                    revolverCue != null &&
                    revolverCue.Phase ==
                        GameSceneRevolverAnimationPhase.ResolvedWithRetry)
                {
                    PrepareRevolverRetry(revolverCue);
                    if (revolverCue.ActorSide == CombatantSide.Enemy &&
                        stepSeconds > 0f)
                    {
                        yield return new WaitForSeconds(stepSeconds);
                    }
                }
            }

            // Land on the true current state — e.g. BattleEnded, which is not itself a step.
            RefreshView();
            UnlockInput();
        }

        private void RefreshView()
        {
            GameSceneViewModel vm =
                GameScenePresenter.Create(Battle, _activeEnemyProfileKey);
            MaybeOpenShop(vm);
            ApplyView(vm);
        }

        private AppliedAnimationResult ApplyView(
            GameSceneViewModel vm,
            bool scheduleRevolverRetry = true,
            bool deferHammerSmashCardRender = false)
        {
            _core = vm.Core;

            if (hud != null)
            {
                hud.Render(vm.Core);
                hud.SetGold(shop != null ? shop.Gold : 0);
            }

            RefreshShopUtilityItems();

            bool playedRevolverAnimation =
                TryPlayRevolverAnimation(
                    vm.RevolverAnimationCue,
                    scheduleRevolverRetry);
            _playedHammerAnimationController = null;
            bool playedHammerAnimation =
                TryPlayHammerAnimation(vm.HammerAnimationCue);
            bool deferredCardRender =
                deferHammerSmashCardRender &&
                playedHammerAnimation &&
                IsHammerSmashCue(vm.HammerAnimationCue);

            // While the shop is open its presentation (merchant, hidden combat objects, goods) is owned
            // by ShopController; skip the combat re-render so it doesn't repaint the enemy over the merchant.
            if (shop != null && shop.IsOpen)
            {
                return CreateAppliedAnimationResult(
                    playedRevolverAnimation,
                    playedHammerAnimation,
                    deferredCardRender: false,
                    deferredViewModel: null);
            }

            if (!deferredCardRender)
            {
                RenderHands(vm);
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
                totals.Render(
                    vm.Core.PlayerTotalsText,
                    vm.Core.EnemyVisibleTotalText);
            }

            return CreateAppliedAnimationResult(
                playedRevolverAnimation,
                playedHammerAnimation,
                deferredCardRender,
                deferredCardRender ? vm : null);
        }

        private void RenderHands(GameSceneViewModel vm)
        {
            if (vm == null)
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
        }

        private void RefreshShopUtilityItems()
        {
            CoreLoopBattle battle = Battle;
            if (shop == null || !shop.IsOpen || battle == null)
            {
                return;
            }

            shop.RefreshUtilityItems(
                BuildRunDeckCardOptions().Count,
                battle.Player.Soul.Current,
                battle.Player.Soul.Maximum);
        }

        private bool TryPlayRevolverAnimation(
            GameSceneRevolverAnimationCue cue,
            bool scheduleRevolverRetry)
        {
            if (cue == null || revolverAnimator == null)
            {
                return false;
            }

            if (IsLastRevolverAnimationCue(cue))
            {
                return false;
            }

            RememberRevolverAnimationCue(cue);

            GameObject root = ResolveRevolverRoot();
            if (root != null && !root.activeSelf)
            {
                root.SetActive(true);
            }

            if (!revolverAnimator.gameObject.activeInHierarchy)
            {
                return false;
            }

            StopRevolverHideRoutine();
            ResetRevolverTriggers();

            if (cue.Phase == GameSceneRevolverAnimationPhase.Ready)
            {
                ResetRevolverAnimatorToBase();
                revolverAnimator.SetTrigger(playerReadyTrigger);
                RememberActiveRevolverReady(cue);
                return false;
            }

            if (cue.ActorSide == CombatantSide.Player &&
                !IsMatchingActiveRevolverReady(cue))
            {
                ResetRevolverAnimatorToBase();
                revolverAnimator.SetTrigger(playerReadyTrigger);
            }

            revolverAnimator.SetTrigger(ResolveRevolverTrigger(cue));
            _revolverReadyActive = false;
            ApplyCinematicCamera(
                GameSceneCameraView.Current,
                revolverAnimationSeconds);

            if (Application.isPlaying &&
                cue.Phase == GameSceneRevolverAnimationPhase.ResolvedWithRetry &&
                scheduleRevolverRetry)
            {
                if (revolverAnimationSeconds > 0f)
                {
                    _revolverHideRoutine =
                        StartCoroutine(PrepareRevolverRetryAfterDelay(cue));
                }
                else
                {
                    PrepareRevolverRetry(cue);
                }
            }
            else if (Application.isPlaying &&
                cue.Phase == GameSceneRevolverAnimationPhase.Resolved &&
                revolverAnimationSeconds > 0f)
            {
                _revolverHideRoutine =
                    StartCoroutine(HideRevolverAnimationAfterDelay());
            }

            return true;
        }

        private bool TryPlayHammerAnimation(GameSceneHammerAnimationCue cue)
        {
            HammerAnimationController controller = ResolveHammerAnimation();
            if (controller == null || !controller.TryPlay(cue, playerHand, enemyHand))
            {
                return false;
            }

            ApplyCinematicCamera(
                cue.ActorSide == CombatantSide.Player
                    ? GameSceneCameraView.EnemyFocus
                    : GameSceneCameraView.Current,
                controller,
                cue.ActorSide == CombatantSide.Player);
            _playedHammerAnimationController = controller;
            return true;
        }

        private HammerAnimationController ResolveHammerAnimation()
        {
            if (hammerAnimation != null)
            {
                return hammerAnimation;
            }

            hammerAnimation =
                FindFirstObjectByType<HammerAnimationController>(
                    FindObjectsInactive.Include);
            if (hammerAnimation != null)
            {
                return hammerAnimation;
            }

            Animator[] animators = FindObjectsByType<Animator>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < animators.Length; i++)
            {
                Animator candidate = animators[i];
                if (candidate != null && candidate.gameObject.name == "Hammer_Anim")
                {
                    hammerAnimation =
                        candidate.GetComponent<HammerAnimationController>() ??
                        candidate.gameObject.AddComponent<HammerAnimationController>();
                    return hammerAnimation;
                }
            }

            return null;
        }

        private void ApplyCinematicCamera(
            GameSceneCameraView view,
            float lockSeconds)
        {
            GameSceneCameraViewController controller =
                ResolveCameraViewController();
            if (controller == null)
            {
                return;
            }

            controller.SetView(view);
            controller.LockSwitchInputForSeconds(lockSeconds);
        }

        private void ApplyCinematicCamera(
            GameSceneCameraView view,
            HammerAnimationController hammerController,
            bool returnToCurrentWhenFinished)
        {
            GameSceneCameraViewController controller =
                ResolveCameraViewController();
            if (controller == null)
            {
                return;
            }

            controller.SetView(view);
            _returnCameraToCurrentAfterHammer = returnToCurrentWhenFinished;
            BeginHammerSwitchInputLock(hammerController);
        }

        private void BeginHammerSwitchInputLock(
            HammerAnimationController hammerController)
        {
            GameSceneCameraViewController controller =
                ResolveCameraViewController();
            if (controller == null)
            {
                return;
            }

            if (!_hammerSwitchInputLocked)
            {
                controller.LockSwitchInput();
                _hammerSwitchInputLocked = true;
            }

            if (_hammerCameraLockController == hammerController)
            {
                return;
            }

            if (_hammerCameraLockController != null)
            {
                _hammerCameraLockController.SmashAnimationFinished -=
                    HandleHammerSmashAnimationFinished;
            }

            _hammerCameraLockController = hammerController;
            if (_hammerCameraLockController != null)
            {
                _hammerCameraLockController.SmashAnimationFinished +=
                    HandleHammerSmashAnimationFinished;
            }
        }

        private void HandleHammerSmashAnimationFinished()
        {
            if (_returnCameraToCurrentAfterHammer)
            {
                GameSceneCameraViewController controller =
                    ResolveCameraViewController();
                controller?.SetView(GameSceneCameraView.Current);
            }

            _returnCameraToCurrentAfterHammer = false;
            EndHammerSwitchInputLock();
        }

        private void EndHammerSwitchInputLock()
        {
            if (_hammerCameraLockController != null)
            {
                _hammerCameraLockController.SmashAnimationFinished -=
                    HandleHammerSmashAnimationFinished;
                _hammerCameraLockController = null;
            }

            _returnCameraToCurrentAfterHammer = false;

            if (!_hammerSwitchInputLocked)
            {
                return;
            }

            GameSceneCameraViewController controller =
                ResolveCameraViewController();
            if (controller != null)
            {
                controller.UnlockSwitchInput();
            }

            _hammerSwitchInputLocked = false;
        }

        private GameSceneCameraViewController ResolveCameraViewController()
        {
            if (cameraViewController != null)
            {
                return cameraViewController;
            }

            cameraViewController =
                FindFirstObjectByType<GameSceneCameraViewController>(
                    FindObjectsInactive.Include);
            return cameraViewController;
        }

        private AppliedAnimationResult CreateAppliedAnimationResult(
            bool playedRevolver,
            bool playedHammer,
            bool deferredCardRender = false,
            GameSceneViewModel deferredViewModel = null)
        {
            float waitSeconds = 0f;
            if (playedRevolver)
            {
                waitSeconds = Mathf.Max(waitSeconds, revolverAnimationSeconds);
            }

            if (playedHammer)
            {
                HammerAnimationController controller = ResolveHammerAnimation();
                waitSeconds = Mathf.Max(
                    waitSeconds,
                    controller != null ? controller.AnimationSeconds : 0f);
            }

            return new AppliedAnimationResult(
                playedRevolver,
                playedHammer,
                waitSeconds,
                playedHammer ? _playedHammerAnimationController : null,
                deferredCardRender,
                deferredViewModel);
        }

        private static bool IsHammerSmashCue(GameSceneHammerAnimationCue cue)
        {
            return cue != null &&
                cue.Phase == GameSceneHammerAnimationPhase.Smash;
        }

        private IEnumerator WaitForAnimationOrSeconds(
            AppliedAnimationResult animation,
            float waitSeconds)
        {
            if (!animation.PlayedHammer ||
                animation.HammerController == null ||
                !animation.HammerController.IsSmashAnimationPlaying)
            {
                yield return new WaitForSeconds(waitSeconds);
                yield break;
            }

            float elapsedSeconds = 0f;
            while (elapsedSeconds < waitSeconds ||
                animation.HammerController.IsSmashAnimationPlaying)
            {
                elapsedSeconds += Time.deltaTime;
                yield return null;
            }
        }

        private bool IsLastRevolverAnimationCue(
            GameSceneRevolverAnimationCue cue)
        {
            return _hasLastRevolverAnimationCue &&
                _lastRevolverAnimationRoundNumber == cue.RoundNumber &&
                _lastRevolverAnimationSourceCardId == cue.SourceCardId &&
                _lastRevolverAnimationActorSide == cue.ActorSide &&
                _lastRevolverAnimationPhase == cue.Phase &&
                _lastRevolverAnimationSucceeded == cue.Succeeded;
        }

        private void RememberRevolverAnimationCue(
            GameSceneRevolverAnimationCue cue)
        {
            _hasLastRevolverAnimationCue = true;
            _lastRevolverAnimationRoundNumber = cue.RoundNumber;
            _lastRevolverAnimationSourceCardId = cue.SourceCardId;
            _lastRevolverAnimationActorSide = cue.ActorSide;
            _lastRevolverAnimationPhase = cue.Phase;
            _lastRevolverAnimationSucceeded = cue.Succeeded;
        }

        private void RememberActiveRevolverReady(
            GameSceneRevolverAnimationCue cue)
        {
            _revolverReadyActive = true;
            _revolverReadyRoundNumber = cue.RoundNumber;
            _revolverReadySourceCardId = cue.SourceCardId;
            _revolverReadyActorSide = cue.ActorSide;
        }

        private bool IsMatchingActiveRevolverReady(
            GameSceneRevolverAnimationCue cue)
        {
            return _revolverReadyActive &&
                _revolverReadyRoundNumber == cue.RoundNumber &&
                _revolverReadySourceCardId == cue.SourceCardId &&
                _revolverReadyActorSide == cue.ActorSide;
        }

        private string ResolveRevolverTrigger(GameSceneRevolverAnimationCue cue)
        {
            if (cue.ActorSide == CombatantSide.Player)
            {
                return cue.Succeeded ? playerSuccessTrigger : playerFailTrigger;
            }

            return cue.Succeeded ? enemySuccessTrigger : enemyFailTrigger;
        }

        private void ResetRevolverAnimationState()
        {
            _hasLastRevolverAnimationCue = false;
            _lastRevolverAnimationRoundNumber = 0;
            _lastRevolverAnimationSourceCardId = 0;
            _lastRevolverAnimationActorSide = CombatantSide.Player;
            _lastRevolverAnimationPhase = GameSceneRevolverAnimationPhase.Ready;
            _lastRevolverAnimationSucceeded = false;
            ClearActiveRevolverReady();
            HideRevolverAnimation();
        }

        private IEnumerator HideRevolverAnimationAfterDelay()
        {
            yield return new WaitForSeconds(revolverAnimationSeconds);
            _revolverHideRoutine = null;
            ResetRevolverAnimatorToBase();

            GameObject root = ResolveRevolverRoot();
            if (root != null)
            {
                root.SetActive(false);
            }

            ClearActiveRevolverReady();
        }

        private IEnumerator PrepareRevolverRetryAfterDelay(
            GameSceneRevolverAnimationCue cue)
        {
            yield return new WaitForSeconds(revolverAnimationSeconds);
            _revolverHideRoutine = null;
            PrepareRevolverRetry(cue);
        }

        private void PrepareRevolverRetry(GameSceneRevolverAnimationCue cue)
        {
            GameObject root = ResolveRevolverRoot();
            if (root != null && !root.activeSelf)
            {
                root.SetActive(true);
            }

            if (revolverAnimator == null ||
                !revolverAnimator.gameObject.activeInHierarchy)
            {
                ClearActiveRevolverReady();
                return;
            }

            ResetRevolverAnimatorToBase();
            ResetRevolverTriggers();
            if (cue.ActorSide == CombatantSide.Player)
            {
                revolverAnimator.SetTrigger(playerReadyTrigger);
            }

            RememberActiveRevolverReady(cue);
        }

        private void HideRevolverAnimation()
        {
            StopRevolverHideRoutine();
            ResetRevolverAnimatorToBase();

            GameObject root = ResolveRevolverRoot();
            if (root != null)
            {
                root.SetActive(false);
            }

            ClearActiveRevolverReady();
        }

        private void StopRevolverHideRoutine()
        {
            if (_revolverHideRoutine == null)
            {
                return;
            }

            StopCoroutine(_revolverHideRoutine);
            _revolverHideRoutine = null;
        }

        private void ResetRevolverAnimatorToBase()
        {
            if (revolverAnimator == null ||
                string.IsNullOrWhiteSpace(revolverBaseStateName) ||
                !revolverAnimator.gameObject.activeInHierarchy)
            {
                return;
            }

            revolverAnimator.Play(revolverBaseStateName, 0, 0f);
            revolverAnimator.Update(0f);
        }

        private void ResetRevolverTriggers()
        {
            ResetRevolverTrigger(playerReadyTrigger);
            ResetRevolverTrigger(playerSuccessTrigger);
            ResetRevolverTrigger(playerFailTrigger);
            ResetRevolverTrigger(enemySuccessTrigger);
            ResetRevolverTrigger(enemyFailTrigger);
        }

        private void ResetRevolverTrigger(string triggerName)
        {
            if (revolverAnimator != null && !string.IsNullOrWhiteSpace(triggerName))
            {
                revolverAnimator.ResetTrigger(triggerName);
            }
        }

        private GameObject ResolveRevolverRoot()
        {
            if (revolverRoot != null)
            {
                return revolverRoot;
            }

            return revolverAnimator != null ? revolverAnimator.gameObject : null;
        }

        private void ClearActiveRevolverReady()
        {
            _revolverReadyActive = false;
            _revolverReadyRoundNumber = 0;
            _revolverReadySourceCardId = 0;
            _revolverReadyActorSide = CombatantSide.Player;
        }

        private readonly struct AppliedAnimationResult
        {
            public AppliedAnimationResult(
                bool playedRevolver,
                bool playedHammer,
                float waitSeconds,
                HammerAnimationController hammerController,
                bool deferredCardRender,
                GameSceneViewModel deferredViewModel)
            {
                PlayedRevolver = playedRevolver;
                PlayedHammer = playedHammer;
                WaitSeconds = waitSeconds;
                HammerController = hammerController;
                DeferredCardRender = deferredCardRender;
                DeferredViewModel = deferredViewModel;
            }

            public bool PlayedRevolver { get; }

            public bool PlayedHammer { get; }

            public bool PlayedAny => PlayedRevolver || PlayedHammer;

            public float WaitSeconds { get; }

            public HammerAnimationController HammerController { get; }

            public bool DeferredCardRender { get; }

            public GameSceneViewModel DeferredViewModel { get; }
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
                _choosingLighterRemoval = false;
                UpdateHover(null);
                UpdateDemonCardHover(null);
                UpdateShopUtilityItemHover(null);
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
                _removedNormalCards.Clear();
                _choosingLighterRemoval = false;
                UpdateHover(null);
                UpdateDemonCardHover(null);
                UpdateShopUtilityItemHover(null);
                shop.Close();
                shop.ResetRunEconomy();
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

            public bool Matches(string definitionKey, CardSuit suit)
            {
                return StringComparer.Ordinal.Equals(DefinitionKey, definitionKey) &&
                    Suit == suit;
            }
        }

        private readonly struct RemovedNormalCard
        {
            public RemovedNormalCard(string definitionKey, CardSuit suit)
            {
                DefinitionKey = definitionKey ?? string.Empty;
                Suit = suit;
            }

            public string DefinitionKey { get; }

            public CardSuit Suit { get; }

            public bool Matches(string definitionKey, CardSuit suit)
            {
                return StringComparer.Ordinal.Equals(DefinitionKey, definitionKey) &&
                    Suit == suit;
            }
        }

        private readonly struct RunDeckCardOption
        {
            public RunDeckCardOption(
                string definitionKey,
                CardSuit suit,
                bool isPurchased,
                int purchasedIndex)
            {
                DefinitionKey = definitionKey ?? string.Empty;
                Suit = suit;
                IsPurchased = isPurchased;
                PurchasedIndex = purchasedIndex;
            }

            public string DefinitionKey { get; }

            public CardSuit Suit { get; }

            public bool IsPurchased { get; }

            public int PurchasedIndex { get; }
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
