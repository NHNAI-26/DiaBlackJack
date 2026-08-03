using System;
using System.Collections.Generic;
using DiaBlackJack.CoreLoop;
using DiaBlackJack.CoreLoop.UI;

namespace DiaBlackJack.GameScene
{
    /// <summary>
    /// A coarse per-side visual state for the world-space character sprite. MVP stand-in for the
    /// eventual per-action animations: the view maps each value to a small tint/scale change so a
    /// hit/stand/bust/win/loss is visible at a glance. Derived from public battle state only.
    /// </summary>
    public enum CharacterVisualState
    {
        Idle,
        Active,
        Stand,
        Bust,
        Win,
        Lose,
        UseCard,
        AttackThreatened,
        Attacked,
    }

    internal enum EnemySpeechActionKind
    {
        Hit,
        Stand,
        Change,
        UseCard,
        DemonContract,
    }

    internal sealed class EnemySpeechCue
    {
        public EnemySpeechCue(
            CoreLoopBattle battle,
            int roundNumber,
            int actionOrdinal,
            EnemySpeechActionKind kind,
            string sourceDefinitionKey)
        {
            Battle = battle;
            RoundNumber = roundNumber;
            ActionOrdinal = actionOrdinal;
            Kind = kind;
            SourceDefinitionKey = sourceDefinitionKey ?? string.Empty;
        }

        public int ActionOrdinal { get; }

        public CoreLoopBattle Battle { get; }

        public EnemySpeechActionKind Kind { get; }

        public int RoundNumber { get; }

        public string SourceDefinitionKey { get; }

        public bool IsSameActionAs(EnemySpeechCue other)
        {
            return other != null &&
                ReferenceEquals(Battle, other.Battle) &&
                RoundNumber == other.RoundNumber &&
                ActionOrdinal == other.ActionOrdinal;
        }
    }

    /// <summary>
    /// A single card projected for world-space rendering. <see cref="IsFaceUp"/> is the *physical*
    /// orientation (drives the card back visual). <see cref="RevealRank"/> is whether the rank may be
    /// shown to the viewer: true for all of the player's own cards (a player sees their own hidden
    /// card), but for the enemy only when the card is face-up. When <see cref="RevealRank"/> is false
    /// the <see cref="Rank"/> is forced to 0 — the hidden enemy rank never crosses into the view.
    /// </summary>
    public sealed class GameSceneCardViewModel
    {
        public GameSceneCardViewModel(
            int cardId,
            int rank,
            bool isFaceUp,
            bool revealRank,
            bool canUse,
            string displayName,
            string abilityDescription = "",
            CardSuit suit = CardSuit.Spade,
            bool showHoverBadgeWhenUnavailable = false,
            string definitionKey = "",
            bool showHoverBadgeBelow = false,
            int? cardEffectChoiceOptionId = null,
            bool isUsed = false,
            GameSceneCombatHudCommand? directSelectionCommand = null,
            bool isEffectSource = false)
        {
            CardId = cardId;
            Rank = rank;
            Suit = suit;
            IsFaceUp = isFaceUp;
            RevealRank = revealRank;
            CanUse = canUse;
            DisplayName = displayName ?? string.Empty;
            AbilityDescription = abilityDescription ?? string.Empty;
            ShowHoverBadgeWhenUnavailable = showHoverBadgeWhenUnavailable;
            DefinitionKey = definitionKey ?? string.Empty;
            ShowHoverBadgeBelow = showHoverBadgeBelow;
            CardEffectChoiceOptionId = cardEffectChoiceOptionId;
            IsUsed = isUsed;
            DirectSelectionCommand = directSelectionCommand;
            IsEffectSource = isEffectSource;
        }

        public int CardId { get; }

        public int Rank { get; }

        public CardSuit Suit { get; }

        public bool IsFaceUp { get; }

        public bool RevealRank { get; }

        /// <summary>
        /// Whether this card's manual effect can be activated right now — drives the diegetic click
        /// on the player's hand. Always false for enemy cards (the player never uses those).
        /// </summary>
        public bool CanUse { get; }

        public string DisplayName { get; }

        /// <summary>
        /// One-line Korean description of the card's effect, for the hover badge. Empty for cards
        /// without an effect and for enemy cards whose rank is not public.
        /// </summary>
        public string AbilityDescription { get; }

        public bool ShowHoverBadgeWhenUnavailable { get; }

        /// <summary>
        /// Whether the hover tooltip extends below this card. Enemy cards use this so their public
        /// information stays inside the screen instead of being clipped above the table.
        /// </summary>
        public bool ShowHoverBadgeBelow { get; }

        /// <summary>
        /// Current card-effect option routed by clicking this world-space card. Null when the card
        /// is not a legal target; kept separate from <see cref="CanUse"/> because enemy cards are
        /// selected as targets rather than activated by the player.
        /// </summary>
        public int? CardEffectChoiceOptionId { get; }

        /// <summary>
        /// Whether a public card has completed its effect and should show the used-card mark.
        /// Hidden enemy state is never projected here.
        /// </summary>
        public bool IsUsed { get; }

        public GameSceneCombatHudCommand? DirectSelectionCommand { get; }

        public bool IsEffectSource { get; }

        /// <summary>
        /// Stable card archetype key used only to select authored visuals. It remains empty for an
        /// unrevealed enemy card so the presentation boundary does not leak hidden information.
        /// </summary>
        public string DefinitionKey { get; }
    }

    public sealed class GameSceneDeckCardGroupViewModel
    {
        public GameSceneDeckCardGroupViewModel(
            GameSceneCardViewModel card,
            int count)
        {
            if (count < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            Card = card ?? throw new ArgumentNullException(nameof(card));
            Count = count;
        }

        public GameSceneCardViewModel Card { get; }

        public int Count { get; }
    }

    /// <summary>
    /// Immutable projection for inspecting one player deck pile in the GameScene. Card groups are
    /// already in display order and never reveal the next physical draw order.
    /// </summary>
    public sealed class GameSceneDeckViewModel
    {
        public GameSceneDeckViewModel(
            DeckKind kind,
            string title,
            IReadOnlyList<GameSceneDeckCardGroupViewModel> cardGroups)
        {
            Kind = kind;
            Title = title ?? string.Empty;
            CardGroups = cardGroups ??
                throw new ArgumentNullException(nameof(cardGroups));

            int cardCount = 0;
            foreach (GameSceneDeckCardGroupViewModel group in CardGroups)
            {
                if (group == null)
                {
                    throw new ArgumentException(
                        "Deck card groups cannot contain null.",
                        nameof(cardGroups));
                }

                cardCount += group.Count;
            }

            CardCount = cardCount;
        }

        public DeckKind Kind { get; }

        public string Title { get; }

        public IReadOnlyList<GameSceneDeckCardGroupViewModel> CardGroups { get; }

        public int CardCount { get; }

        public int GroupCount => CardGroups.Count;
    }

    public enum GameSceneRevolverAnimationPhase
    {
        Ready,
        ResolvedWithRetry,
        Resolved,
    }

    public sealed class GameSceneRevolverAnimationCue
    {
        public GameSceneRevolverAnimationCue(
            int roundNumber,
            int sourceCardId,
            CombatantSide actorSide,
            bool succeeded)
            : this(
                roundNumber,
                sourceCardId,
                actorSide,
                GameSceneRevolverAnimationPhase.Resolved,
                succeeded)
        {
        }

        public GameSceneRevolverAnimationCue(
            int roundNumber,
            int sourceCardId,
            CombatantSide actorSide,
            GameSceneRevolverAnimationPhase phase,
            bool succeeded = false)
        {
            if (roundNumber < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(roundNumber));
            }

            if (sourceCardId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceCardId));
            }

            if (!Enum.IsDefined(typeof(CombatantSide), actorSide))
            {
                throw new ArgumentOutOfRangeException(nameof(actorSide));
            }

            if (!Enum.IsDefined(typeof(GameSceneRevolverAnimationPhase), phase))
            {
                throw new ArgumentOutOfRangeException(nameof(phase));
            }

            RoundNumber = roundNumber;
            SourceCardId = sourceCardId;
            ActorSide = actorSide;
            Phase = phase;
            Succeeded = succeeded;
        }

        public int RoundNumber { get; }

        public int SourceCardId { get; }

        public CombatantSide ActorSide { get; }

        public GameSceneRevolverAnimationPhase Phase { get; }

        public bool Succeeded { get; }
    }

    public enum GameSceneKnifeAnimationPhase
    {
        Ready,
        Resolved,
    }

    public sealed class GameSceneKnifeAnimationCue
    {
        public GameSceneKnifeAnimationCue(
            int roundNumber,
            int sourceCardId,
            CombatantSide actorSide,
            GameSceneKnifeAnimationPhase phase,
            bool succeeded = false)
        {
            if (roundNumber < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(roundNumber));
            }

            if (sourceCardId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceCardId));
            }

            if (!Enum.IsDefined(typeof(CombatantSide), actorSide))
            {
                throw new ArgumentOutOfRangeException(nameof(actorSide));
            }

            if (!Enum.IsDefined(typeof(GameSceneKnifeAnimationPhase), phase))
            {
                throw new ArgumentOutOfRangeException(nameof(phase));
            }

            RoundNumber = roundNumber;
            SourceCardId = sourceCardId;
            ActorSide = actorSide;
            Phase = phase;
            Succeeded = succeeded;
        }

        public int RoundNumber { get; }

        public int SourceCardId { get; }

        public CombatantSide ActorSide { get; }

        public GameSceneKnifeAnimationPhase Phase { get; }

        public bool Succeeded { get; }
    }

    public enum GameSceneHammerAnimationPhase
    {
        Ready,
        Smash,
    }

    public sealed class GameSceneHammerAnimationCue
    {
        public GameSceneHammerAnimationCue(
            int roundNumber,
            int sourceCardId,
            CombatantSide actorSide,
            GameSceneHammerAnimationPhase phase,
            int? targetCardId = null)
        {
            if (roundNumber < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(roundNumber));
            }

            if (sourceCardId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceCardId));
            }

            if (!Enum.IsDefined(typeof(CombatantSide), actorSide))
            {
                throw new ArgumentOutOfRangeException(nameof(actorSide));
            }

            if (!Enum.IsDefined(typeof(GameSceneHammerAnimationPhase), phase))
            {
                throw new ArgumentOutOfRangeException(nameof(phase));
            }

            if (targetCardId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(targetCardId));
            }

            RoundNumber = roundNumber;
            SourceCardId = sourceCardId;
            ActorSide = actorSide;
            Phase = phase;
            TargetCardId = targetCardId;
        }

        public int RoundNumber { get; }

        public int SourceCardId { get; }

        public CombatantSide ActorSide { get; }

        public GameSceneHammerAnimationPhase Phase { get; }

        public int? TargetCardId { get; }
    }

    /// <summary>
    /// Read-only projection consumed by <c>GameSceneView</c>. Wraps the shared
    /// <see cref="CoreLoopViewModel"/> (souls, totals, state, player card actions) and adds
    /// world-render-friendly card lists for both sides.
    /// </summary>
    public sealed class GameSceneViewModel
    {
        public GameSceneViewModel(
            CoreLoopViewModel core,
            IReadOnlyList<GameSceneCardViewModel> playerCards,
            IReadOnlyList<GameSceneCardViewModel> enemyCards,
            IReadOnlyList<GameSceneDemonCardViewModel> playerDemonCards,
            IReadOnlyList<GameSceneDemonCardViewModel> enemyDemonCards,
            CharacterVisualState enemyVisual,
            string enemyActionLabel,
            IReadOnlyList<GameSceneCardViewModel> crystalOrbCandidates,
            IReadOnlyList<GameSceneCardViewModel> satanNumberCandidates,
            GameSceneRevolverAnimationCue revolverAnimationCue = null,
            GameSceneHammerAnimationCue hammerAnimationCue = null,
            bool usesDiegeticCardEffectSelection = false,
            bool focusesEnemyCardsForSelection = false,
            string playerTotalsText = null,
            string enemyTotalsText = null,
            GameSceneKnifeAnimationCue knifeAnimationCue = null,
            int? playerMammonDieValue = null,
            int? enemyMammonDieValue = null,
            int? playerMammonSourceCardId = null,
            bool canPlayerRerollMammon = false)
        {
            Core = core ?? throw new ArgumentNullException(nameof(core));
            PlayerCards = playerCards ?? throw new ArgumentNullException(nameof(playerCards));
            EnemyCards = enemyCards ?? throw new ArgumentNullException(nameof(enemyCards));
            PlayerDemonCards = playerDemonCards ??
                throw new ArgumentNullException(nameof(playerDemonCards));
            EnemyDemonCards = enemyDemonCards ??
                throw new ArgumentNullException(nameof(enemyDemonCards));
            EnemyVisual = enemyVisual;
            EnemyActionLabel = enemyActionLabel ?? string.Empty;
            CrystalOrbCandidates = crystalOrbCandidates ??
                throw new ArgumentNullException(nameof(crystalOrbCandidates));
            SatanNumberCandidates = satanNumberCandidates ??
                throw new ArgumentNullException(nameof(satanNumberCandidates));
            RevolverAnimationCue = revolverAnimationCue;
            HammerAnimationCue = hammerAnimationCue;
            UsesDiegeticCardEffectSelection = usesDiegeticCardEffectSelection;
            FocusesEnemyCardsForSelection = focusesEnemyCardsForSelection;
            PlayerTotalsText = playerTotalsText ?? core.PlayerTotalsText;
            EnemyTotalsText = enemyTotalsText ?? core.EnemyVisibleTotalText;
            KnifeAnimationCue = knifeAnimationCue;
            PlayerMammonDieValue = playerMammonDieValue;
            EnemyMammonDieValue = enemyMammonDieValue;
            PlayerMammonSourceCardId = playerMammonSourceCardId;
            CanPlayerRerollMammon = canPlayerRerollMammon;
        }

        public CoreLoopViewModel Core { get; }

        public IReadOnlyList<GameSceneCardViewModel> PlayerCards { get; }

        public IReadOnlyList<GameSceneCardViewModel> EnemyCards { get; }

        public IReadOnlyList<GameSceneDemonCardViewModel> PlayerDemonCards { get; }

        public IReadOnlyList<GameSceneDemonCardViewModel> EnemyDemonCards { get; }

        public CharacterVisualState EnemyVisual { get; }

        /// <summary>Short action token shown above the enemy character. Empty = no label.</summary>
        public string EnemyActionLabel { get; }

        public IReadOnlyList<GameSceneCardViewModel> CrystalOrbCandidates { get; }

        public IReadOnlyList<GameSceneCardViewModel> SatanNumberCandidates { get; }

        public GameSceneRevolverAnimationCue RevolverAnimationCue { get; }

        public GameSceneHammerAnimationCue HammerAnimationCue { get; }

        public GameSceneKnifeAnimationCue KnifeAnimationCue { get; }

        public bool UsesDiegeticCardEffectSelection { get; }

        public bool FocusesEnemyCardsForSelection { get; }

        public string PlayerTotalsText { get; }

        public string EnemyTotalsText { get; }

        public int? PlayerMammonDieValue { get; }

        public int? EnemyMammonDieValue { get; }

        public int? PlayerMammonSourceCardId { get; }

        public bool CanPlayerRerollMammon { get; }

        internal EnemySpeechCue EnemySpeechCue { get; set; }
    }

    public static class GameScenePresenter
    {
        public static GameSceneViewModel Create(CoreLoopBattle battle, string profileKey = null)
        {
            if (battle == null)
            {
                throw new ArgumentNullException(nameof(battle));
            }

            CoreLoopViewModel core = CoreLoopPresenter.Create(battle, profileKey);
            bool revealRoundResult = battle.State == CoreLoopState.ResolvingRound &&
                battle.LastResolution.HasValue;
            (CharacterVisualState enemyVisual, string enemyLabel) =
                ResolveSide(battle, CombatantSide.Enemy);
            ActiveDemonContract playerMammon = FindMammonContract(
                battle.ActivePlayerDemonContracts);
            GameSceneViewModel model = new GameSceneViewModel(
                core,
                CreatePlayerCards(core, battle, revealRoundResult),
                CreateEnemyCards(battle, revealRoundResult),
                CreateActiveDemonCards(
                    battle,
                    battle.ActivePlayerDemonContracts,
                    exposePlayerActions: true),
                CreateActiveDemonCards(
                    battle,
                    battle.ActiveEnemyDemonContracts,
                    exposePlayerActions: false),
                enemyVisual,
                enemyLabel,
                CreateCrystalOrbCandidates(battle),
                CreateSatanNumberCandidates(battle),
                CreateRevolverAnimationCue(battle),
                CreateHammerAnimationCue(battle),
                UsesDiegeticSelection(battle),
                FocusesEnemyCardsForSelection(battle),
                CreatePlayerTotalsText(battle, core, revealRoundResult),
                CreateEnemyTotalsText(battle, core, revealRoundResult),
                CreateKnifeAnimationCue(battle),
                FindMammonDieValue(playerMammon),
                FindMammonDieValue(FindMammonContract(
                    battle.ActiveEnemyDemonContracts)),
                playerMammon?.SourceCardId,
                playerMammon != null &&
                    battle.CanBeginPlayerActiveDemonContractAction(
                        playerMammon.SourceCardId));
            model.EnemySpeechCue = CreateEnemySpeechCue(battle);
            return model;
        }

        internal static EnemySpeechCue CreateEnemySpeechCue(
            int roundNumber,
            int actionOrdinal,
            PublicCombatAction action,
            CoreLoopBattle battle = null)
        {
            if (action == null ||
                action.ActorSide != CombatantSide.Enemy ||
                actionOrdinal <= 0)
            {
                return null;
            }

            EnemySpeechActionKind kind;
            switch (action.ActionType)
            {
                case PublicCombatActionType.Hit:
                    kind = EnemySpeechActionKind.Hit;
                    break;
                case PublicCombatActionType.Stand:
                    kind = EnemySpeechActionKind.Stand;
                    break;
                case PublicCombatActionType.Change:
                    kind = EnemySpeechActionKind.Change;
                    break;
                case PublicCombatActionType.UseCard:
                    kind = EnemySpeechActionKind.UseCard;
                    break;
                case PublicCombatActionType.DemonContract:
                    kind = EnemySpeechActionKind.DemonContract;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action));
            }

            return new EnemySpeechCue(
                battle,
                roundNumber,
                actionOrdinal,
                kind,
                action.SourceCardDefinitionKey);
        }

        private static EnemySpeechCue CreateEnemySpeechCue(CoreLoopBattle battle)
        {
            IReadOnlyList<PublicCombatAction> history = battle.PublicActionHistory;
            int count = history.Count;
            return count == 0
                ? null
                : CreateEnemySpeechCue(
                    battle.RoundNumber,
                    count,
                    history[count - 1],
                    battle);
        }

        private static ActiveDemonContract FindMammonContract(
            IReadOnlyList<ActiveDemonContract> contracts)
        {
            foreach (ActiveDemonContract contract in contracts)
            {
                if (contract.Kind == DemonContractKind.Mammon)
                {
                    return contract;
                }
            }

            return null;
        }

        private static int? FindMammonDieValue(ActiveDemonContract contract)
        {
            return contract?.RuntimeState is MammonRuntimeState mammon
                ? mammon.CurrentDieValue
                : (int?)null;
        }

        private static string CreatePlayerTotalsText(
            CoreLoopBattle battle,
            CoreLoopViewModel core,
            bool revealRoundResult)
        {
            return revealRoundResult
                ? FormatFinalTotals(
                    battle.Player.HandValue.Total,
                    battle.Player.VisibleHandValue.Total)
                : core.PlayerTotalsText;
        }

        private static string CreateEnemyTotalsText(
            CoreLoopBattle battle,
            CoreLoopViewModel core,
            bool revealRoundResult)
        {
            return revealRoundResult
                ? FormatFinalTotals(
                    battle.Enemy.HandValue.Total,
                    battle.Enemy.VisibleHandValue.Total)
                : core.EnemyVisibleTotalText;
        }

        private static string FormatFinalTotals(int total, int publicTotal)
        {
            return $"총합 : {total}\n공개 카드 합 : {publicTotal}";
        }

        private static bool FocusesEnemyCardsForSelection(
            CoreLoopBattle battle)
        {
            PendingDemonContractInteraction interaction =
                battle.PendingPlayerDemonContractInteraction;
            return interaction != null && interaction.Kind ==
                DemonContractInteractionKind.BeelzebubChooseOpponentCard;
        }

        /// <summary>
        /// Projects one of the player's available card piles for inspection. The source deck returns
        /// immutable, non-draw-ordered snapshots, so this view cannot disclose the next card.
        /// </summary>
        public static GameSceneDeckViewModel CreateDeckPreview(
            CoreLoopBattle battle,
            DeckKind kind)
        {
            if (battle == null)
            {
                throw new ArgumentNullException(nameof(battle));
            }

            IReadOnlyList<DeckCardDisplaySnapshot> snapshots = kind == DeckKind.Draw
                ? battle.Player.Deck.GetDrawPileDisplayCards()
                : battle.Player.Deck.GetDiscardPileDisplayCards();
            var groups = new List<GameSceneDeckCardGroupViewModel>(
                snapshots.Count);
            GameSceneCardViewModel representative = null;
            string definitionKey = null;
            CardSuit suit = default;
            int count = 0;
            foreach (DeckCardDisplaySnapshot snapshot in snapshots)
            {
                bool continuesGroup = representative != null &&
                    string.Equals(
                        definitionKey,
                        snapshot.DefinitionKey,
                        StringComparison.Ordinal) &&
                    suit == snapshot.Suit;
                if (!continuesGroup && representative != null)
                {
                    groups.Add(new GameSceneDeckCardGroupViewModel(
                        representative,
                        count));
                }

                if (!continuesGroup)
                {
                    representative = new GameSceneCardViewModel(
                        snapshot.Id,
                        snapshot.Rank,
                        isFaceUp: true,
                        revealRank: true,
                        canUse: false,
                        snapshot.DisplayName,
                        abilityDescription: snapshot.AbilityDescription,
                        suit: snapshot.Suit,
                        showHoverBadgeWhenUnavailable: true,
                        definitionKey: snapshot.DefinitionKey);
                    definitionKey = snapshot.DefinitionKey;
                    suit = snapshot.Suit;
                    count = 1;
                }
                else
                {
                    count++;
                }
            }

            if (representative != null)
            {
                groups.Add(new GameSceneDeckCardGroupViewModel(
                    representative,
                    count));
            }

            string title = kind == DeckKind.Draw ? "뽑을 카드" : "버린 카드";
            return new GameSceneDeckViewModel(kind, title, groups.AsReadOnly());
        }

        private static GameSceneHammerAnimationCue CreateHammerAnimationCue(
            CoreLoopBattle battle)
        {
            PendingCardEffect pendingPlayerEffect = battle.PendingPlayerCardEffect;
            if (pendingPlayerEffect != null &&
                pendingPlayerEffect.EffectKind == CardEffectKind.ThreatHammer)
            {
                return new GameSceneHammerAnimationCue(
                    battle.RoundNumber,
                    pendingPlayerEffect.SourceCardId,
                    CombatantSide.Player,
                    GameSceneHammerAnimationPhase.Ready);
            }

            if (battle.PendingEnemyCardEffect != null ||
                !battle.LastCardEffectResult.HasValue ||
                !battle.LastCardEffectActorSide.HasValue)
            {
                return null;
            }

            CardEffectResult result = battle.LastCardEffectResult.Value;
            if (result.EffectKind != CardEffectKind.ThreatHammer ||
                !result.TargetCardId.HasValue ||
                !IsLastUseCardEffect(battle, result.EffectKind))
            {
                return null;
            }

            return new GameSceneHammerAnimationCue(
                battle.RoundNumber,
                result.SourceCardId,
                battle.LastCardEffectActorSide.Value,
                GameSceneHammerAnimationPhase.Smash,
                result.TargetCardId);
        }

        private static GameSceneRevolverAnimationCue CreateRevolverAnimationCue(
            CoreLoopBattle battle)
        {
            if (battle.HasPendingLeviathanAutoPistolRetry &&
                battle.LastCardEffectResult.HasValue &&
                battle.LastCardEffectActorSide.HasValue)
            {
                CardEffectResult retryResult = battle.LastCardEffectResult.Value;
                return new GameSceneRevolverAnimationCue(
                    battle.RoundNumber,
                    retryResult.SourceCardId,
                    battle.LastCardEffectActorSide.Value,
                    GameSceneRevolverAnimationPhase.ResolvedWithRetry,
                    retryResult.Succeeded);
            }

            PendingCardEffect pendingPlayerEffect = battle.PendingPlayerCardEffect;
            if (pendingPlayerEffect != null &&
                pendingPlayerEffect.EffectKind == CardEffectKind.AutoPistol)
            {
                return new GameSceneRevolverAnimationCue(
                    battle.RoundNumber,
                    pendingPlayerEffect.SourceCardId,
                    CombatantSide.Player,
                    GameSceneRevolverAnimationPhase.Ready);
            }

            if (battle.PendingEnemyCardEffect != null ||
                !battle.LastCardEffectResult.HasValue ||
                !battle.LastCardEffectActorSide.HasValue)
            {
                return null;
            }

            CardEffectResult result = battle.LastCardEffectResult.Value;
            if (result.EffectKind != CardEffectKind.AutoPistol ||
                !IsLastUseCardEffect(battle, result.EffectKind))
            {
                return null;
            }

            return new GameSceneRevolverAnimationCue(
                battle.RoundNumber,
                result.SourceCardId,
                battle.LastCardEffectActorSide.Value,
                result.Succeeded);
        }

        private static GameSceneKnifeAnimationCue CreateKnifeAnimationCue(
            CoreLoopBattle battle)
        {
            if (battle.ActiveCardEffectKind == CardEffectKind.MilitaryKnife &&
                battle.ActiveCardEffectSourceCardId.HasValue &&
                battle.ActiveCardEffectActorSide.HasValue &&
                IsLastUseCardEffect(battle, CardEffectKind.MilitaryKnife))
            {
                return new GameSceneKnifeAnimationCue(
                    battle.RoundNumber,
                    battle.ActiveCardEffectSourceCardId.Value,
                    battle.ActiveCardEffectActorSide.Value,
                    GameSceneKnifeAnimationPhase.Ready);
            }

            if (!battle.LastCardEffectResult.HasValue ||
                !battle.LastCardEffectActorSide.HasValue)
            {
                return null;
            }

            CardEffectResult result = battle.LastCardEffectResult.Value;
            if (result.EffectKind != CardEffectKind.MilitaryKnife ||
                !IsLastUseCardEffect(battle, CardEffectKind.MilitaryKnife))
            {
                return null;
            }

            return new GameSceneKnifeAnimationCue(
                battle.RoundNumber,
                result.SourceCardId,
                battle.LastCardEffectActorSide.Value,
                GameSceneKnifeAnimationPhase.Resolved,
                result.EndedRound);
        }

        private static bool IsLastUseCardEffect(
            CoreLoopBattle battle,
            CardEffectKind effectKind)
        {
            PublicCombatAction last = battle.LastPublicAction;
            if (last == null ||
                last.ActionType != PublicCombatActionType.UseCard ||
                string.IsNullOrWhiteSpace(last.SourceCardDefinitionKey))
            {
                return false;
            }

            return CardDefinitionCatalog
                .GetByKey(last.SourceCardDefinitionKey)
                .Effect == effectKind;
        }

        // MVP presentation: derive one coarse visual + short action label per side from public
        // battle state only. Priority: incoming attack reaction > battle end > round resolution >
        // other card effects > this side's last action > resting. Bust is transient (the hand
        // clears the instant a round resolves), so round results are read from the surviving
        // LastResolution rather than a live hand value.
        private static (CharacterVisualState Visual, string Label) ResolveSide(
            CoreLoopBattle battle,
            CombatantSide side)
        {
            bool hasCardEffect = TryResolveCardEffect(
                battle,
                side,
                out (CharacterVisualState Visual, string Label) effect);
            if (hasCardEffect && IsAttackReaction(effect.Visual))
            {
                return effect;
            }

            if (battle.Outcome != BattleOutcome.InProgress)
            {
                bool won =
                    (side == CombatantSide.Player && battle.Outcome == BattleOutcome.PlayerVictory) ||
                    (side == CombatantSide.Enemy && battle.Outcome == BattleOutcome.PlayerDefeat);
                return won
                    ? (CharacterVisualState.Win, "WIN")
                    : (CharacterVisualState.Lose, "LOSE");
            }

            if (battle.State == CoreLoopState.ResolvingRound && battle.LastResolution.HasValue)
            {
                return ResolveRoundResult(battle.LastResolution.Value.Outcome, side);
            }

            // A card effect surfaces on the actor ("USE: <name>") and on the character it lands on
            // ("GUESS" / "DRAW" / "DISCARD") — during the choosing (pending) phase and the use beat.
            if (hasCardEffect)
            {
                return effect;
            }

            PublicCombatAction last = battle.LastPublicAction;
            if (last != null && last.ActorSide == side)
            {
                switch (last.ActionType)
                {
                    case PublicCombatActionType.Hit:
                        return (CharacterVisualState.Active, "HIT");
                    case PublicCombatActionType.Stand:
                        return (CharacterVisualState.Stand, "STAND");
                    case PublicCombatActionType.Change:
                        return (CharacterVisualState.Active, "CHANGE");
                    case PublicCombatActionType.DemonContract:
                        return (CharacterVisualState.Active, "CONTRACT");
                }
            }

            BattleParticipant self = side == CombatantSide.Player ? battle.Player : battle.Enemy;
            return self.IsStanding
                ? (CharacterVisualState.Stand, "STAND")
                : (CharacterVisualState.Idle, string.Empty);
        }

        // Per-side reaction to a round result: winner "WIN"/"21!", loser "BUST" (busted) or "LOSE".
        private static (CharacterVisualState Visual, string Label) ResolveRoundResult(
            RoundOutcome outcome,
            CombatantSide side)
        {
            switch (outcome)
            {
                case RoundOutcome.PlayerBust:
                    return side == CombatantSide.Player
                        ? (CharacterVisualState.Bust, "BUST")
                        : (CharacterVisualState.Win, "WIN");
                case RoundOutcome.EnemyBust:
                    return side == CombatantSide.Enemy
                        ? (CharacterVisualState.Bust, "BUST")
                        : (CharacterVisualState.Win, "WIN");
                case RoundOutcome.PlayerTwentyOneWin:
                    return side == CombatantSide.Player
                        ? (CharacterVisualState.Win, "21!")
                        : (CharacterVisualState.Lose, "LOSE");
                case RoundOutcome.PlayerWin:
                    return side == CombatantSide.Player
                        ? (CharacterVisualState.Win, "WIN")
                        : (CharacterVisualState.Lose, "LOSE");
                case RoundOutcome.EnemyWin:
                    return side == CombatantSide.Enemy
                        ? (CharacterVisualState.Win, "WIN")
                        : (CharacterVisualState.Lose, "LOSE");
                default:
                    return (CharacterVisualState.Idle, string.Empty);
            }
        }

        // A card effect surfaces on two characters: the ACTOR who played it ("USE: <name>") and the
        // TARGET it lands on. Shown while the player is still choosing (PendingPlayerCardEffect) and on
        // the use beat (LastPublicAction == UseCard). REVOLVER (7,8) guesses the OPPONENT's hidden card;
        // BOWIE KNIFE (9,10) forces the OPPONENT to draw; CRYSTAL ORB (5) draws for SELF; THREAT HAMMER
        // (6) discards an OPPONENT face-up card.
        private static bool TryResolveCardEffect(
            CoreLoopBattle battle,
            CombatantSide side,
            out (CharacterVisualState Visual, string Label) result)
        {
            result = default;

            CardEffectKind kind;
            CombatantSide actor;
            CardEffectResult? completedResult = null;

            PendingCardEffect pending = battle.PendingPlayerCardEffect;
            if (pending != null)
            {
                kind = pending.EffectKind;
                actor = CombatantSide.Player;
            }
            else if (battle.ActiveCardEffectKind.HasValue &&
                     battle.ActiveCardEffectActorSide.HasValue)
            {
                kind = battle.ActiveCardEffectKind.Value;
                actor = battle.ActiveCardEffectActorSide.Value;
            }
            else if (battle.LastPublicAction != null &&
                     battle.LastPublicAction.ActionType == PublicCombatActionType.UseCard &&
                     battle.LastCardEffectResult.HasValue &&
                     battle.LastCardEffectActorSide.HasValue)
            {
                completedResult = battle.LastCardEffectResult.Value;
                kind = completedResult.Value.EffectKind;
                actor = battle.LastCardEffectActorSide.Value;
            }
            else
            {
                return false;
            }

            if (kind == CardEffectKind.None)
            {
                return false;
            }

            CombatantSide target = EffectTargetSide(kind, actor);
            if (side == target)
            {
                // While choosing, show what the effect will do; once resolved, show its outcome —
                // in particular the revolver's hit vs miss, which otherwise has no visible feedback.
                string label = completedResult.HasValue
                    ? EffectResultLabel(kind, completedResult.Value)
                    : EffectActionLabel(kind);
                CharacterVisualState visual = CharacterVisualState.UseCard;
                if (IsAttackEffect(kind))
                {
                    if (!completedResult.HasValue)
                    {
                        visual = CharacterVisualState.AttackThreatened;
                    }
                    else if (DidAttackHit(kind, completedResult.Value))
                    {
                        visual = CharacterVisualState.Attacked;
                    }
                }

                result = (visual, label);
                return true;
            }

            if (side == actor)
            {
                result = (CharacterVisualState.UseCard, "USE: " + CoreLoopPresenter.FormatEffectName(kind));
                return true;
            }

            return false;
        }

        private static bool IsAttackEffect(CardEffectKind kind)
        {
            switch (kind)
            {
                case CardEffectKind.AutoPistol:
                case CardEffectKind.MilitaryKnife:
                    return true;
                default:
                    return false;
            }
        }

        private static bool DidAttackHit(
            CardEffectKind kind,
            CardEffectResult result)
        {
            return kind == CardEffectKind.MilitaryKnife
                ? result.EndedRound
                : result.Succeeded;
        }

        private static bool IsAttackReaction(CharacterVisualState state)
        {
            return state == CharacterVisualState.AttackThreatened ||
                state == CharacterVisualState.Attacked;
        }

        // The character an effect's visible action lands on. REVOLVER / BOWIE KNIFE / THREAT HAMMER
        // hit the opponent; CRYSTAL ORB acts on the actor's own hand.
        private static CombatantSide EffectTargetSide(CardEffectKind kind, CombatantSide actor)
        {
            switch (kind)
            {
                case CardEffectKind.AutoPistol:
                case CardEffectKind.MilitaryKnife:
                case CardEffectKind.ThreatHammer:
                    return actor == CombatantSide.Player ? CombatantSide.Enemy : CombatantSide.Player;
                default:
                    return actor;
            }
        }

        // Short token for what the effect does to its target character.
        private static string EffectActionLabel(CardEffectKind kind)
        {
            switch (kind)
            {
                case CardEffectKind.AutoPistol:
                    return "GUESS";
                case CardEffectKind.MilitaryKnife:
                    return "DRAW";
                case CardEffectKind.CrystalOrb:
                    return "DRAW";
                case CardEffectKind.ThreatHammer:
                    return "DISCARD";
                default:
                    return string.Empty;
            }
        }

        // Target label once the effect has resolved. The revolver's guess distinguishes hit vs miss;
        // every other effect reads the same as its action label.
        private static string EffectResultLabel(CardEffectKind kind, CardEffectResult result)
        {
            if (kind == CardEffectKind.AutoPistol)
            {
                return result.Succeeded ? "HIT!" : "MISS";
            }

            if (kind == CardEffectKind.MilitaryKnife)
            {
                return result.EndedRound ? "HIT!" : "MISS";
            }

            return EffectActionLabel(kind);
        }

        private static IReadOnlyList<GameSceneCardViewModel> CreatePlayerCards(
            CoreLoopViewModel core,
            CoreLoopBattle battle,
            bool revealRoundResult)
        {
            var cards = new List<GameSceneCardViewModel>(core.PlayerCardActions.Count);
            int hiddenCardCount = 0;
            foreach (PlayerCardViewModel card in core.PlayerCardActions)
            {
                BlackjackCard sourceCard = FindCardById(battle.Player.Hand.Cards, card.CardId);
                bool isHiddenCard = battle.Player.Hand.IsHiddenCard(card.CardId);

                // The player sees every one of their own cards, including the face-down one.
                var projectedCard = new GameSceneCardViewModel(
                    card.CardId,
                    card.Rank,
                    card.IsFaceUp || revealRoundResult,
                    revealRank: true,
                    canUse: card.CanUse,
                    card.DisplayName,
                    abilityDescription: ResolveAbilityDescription(sourceCard),
                    suit: sourceCard == null ? CardSuit.Spade : sourceCard.Suit,
                    definitionKey: sourceCard?.DefinitionKey,
                    isUsed: card.UseState == CardUseState.Used,
                    directSelectionCommand:
                        FindPlayerDirectSelectionCommand(battle, card.CardId),
                    isEffectSource: IsPlayerEffectSource(battle, card.CardId));

                // PlayerHand's world orientation makes the highest index land at screen-left.
                // Keep hidden cards last and prepend face-up cards so new draws appear at
                // screen-right from the player's perspective.
                if (!isHiddenCard)
                {
                    cards.Insert(0, projectedCard);
                }
                else
                {
                    cards.Add(projectedCard);
                    hiddenCardCount++;
                }
            }

            return cards.AsReadOnly();
        }

        private static string ResolveAbilityDescription(BlackjackCard card)
        {
            return card?.Definition.Description ?? string.Empty;
        }

        private static IReadOnlyList<GameSceneDemonCardViewModel>
            CreateActiveDemonCards(
                CoreLoopBattle battle,
                IReadOnlyList<ActiveDemonContract> contracts,
                bool exposePlayerActions)
        {
            var cards = new List<GameSceneDemonCardViewModel>(contracts.Count);
            foreach (ActiveDemonContract contract in contracts)
            {
                DemonContractDefinition definition = contract.Definition;
                bool isSatan = contract.Kind == DemonContractKind.Satan;
                bool isUpsideDown = isSatan &&
                    contract.RuntimeState is SatanRuntimeState satanState &&
                    satanState.CurrentFace == SatanContractFace.Lower;
                cards.Add(new GameSceneDemonCardViewModel(
                    contract.SourceCardId,
                    definition.Key,
                    isFaceUp: true,
                    canUse: exposePlayerActions &&
                        battle.CanBeginPlayerActiveDemonContractAction(
                            contract.SourceCardId),
                    definition.DisplayName,
                    definition.Summary,
                    definition.CostSummary,
                    showHoverBadgeWhenUnavailable: true,
                    isUpsideDown: isUpsideDown));
            }

            return cards.AsReadOnly();
        }

        private static BlackjackCard FindCardById(IReadOnlyList<BlackjackCard> cards, int cardId)
        {
            foreach (BlackjackCard card in cards)
            {
                if (card.Id == cardId)
                {
                    return card;
                }
            }

            return null;
        }

        /// <summary>
        /// Composition of the player's <b>draw pile</b> (cards still to draw) for the draw-deck hover
        /// panel — rank×count + total, order not shown. Discarded cards are NOT here (they show in the
        /// discard-deck panel). Rendered in IMGUI, so Korean needs no special TMP font.
        /// </summary>
        public static string FormatDrawDeck(CoreLoopBattle battle)
        {
            if (battle == null)
            {
                return string.Empty;
            }

            return FormatRankCounts(battle.Player.Deck.GetDrawPileRankCounts(), "뽑을 카드");
        }

        /// <summary>
        /// Composition of the player's <b>discard pile</b> (cards discarded this run) for the
        /// discard-deck hover panel. Reshuffled back into the draw pile when it empties.
        /// </summary>
        public static string FormatDiscardDeck(CoreLoopBattle battle)
        {
            if (battle == null)
            {
                return string.Empty;
            }

            return FormatRankCounts(battle.Player.Deck.GetDiscardPileRankCounts(), "버린 카드");
        }

        // rank×count composition, 5 per row, with a "<header>  N장" heading. Shared by both deck panels.
        private static string FormatRankCounts(IReadOnlyList<int> counts, string header)
        {
            var parts = new List<string>();
            int total = 0;
            for (int rank = 1; rank <= 10; rank++)
            {
                int count = counts[rank];
                if (count <= 0)
                {
                    continue;
                }

                parts.Add(rank + " x" + count);
                total += count;
            }

            string body;
            if (parts.Count == 0)
            {
                body = "-";
            }
            else
            {
                var lines = new List<string>();
                for (int i = 0; i < parts.Count; i += 5)
                {
                    lines.Add(string.Join("    ", parts.GetRange(i, Math.Min(5, parts.Count - i))));
                }

                body = string.Join("\n", lines);
            }

            return header + "  " + total + "장\n\n" + body;
        }

        private static IReadOnlyList<GameSceneCardViewModel> CreateEnemyCards(
            CoreLoopBattle battle,
            bool revealRoundResult)
        {
            IReadOnlyList<BlackjackCard> hand = battle.Enemy.Hand.Cards;
            PendingCardEffect pendingEffect = battle.PendingPlayerCardEffect;
            var cards = new List<GameSceneCardViewModel>(hand.Count);
            foreach (BlackjackCard card in hand)
            {
                // Face-down enemy card: emit no rank. This is the information-hiding boundary.
                bool faceUp = card.IsFaceUp || revealRoundResult;
                bool isHiddenCard = battle.Enemy.Hand.IsHiddenCard(card.Id);
                var projectedCard = new GameSceneCardViewModel(
                    card.Id,
                    faceUp ? card.Rank : 0,
                    faceUp,
                    revealRank: faceUp,
                    canUse: false,
                    faceUp ? card.Definition.DisplayName : string.Empty,
                    abilityDescription: faceUp
                        ? ResolveAbilityDescription(card)
                        : string.Empty,
                    suit: card.Suit,
                    showHoverBadgeWhenUnavailable: faceUp,
                    definitionKey: faceUp ? card.DefinitionKey : string.Empty,
                    showHoverBadgeBelow: faceUp,
                    cardEffectChoiceOptionId:
                        FindCardEffectChoiceOptionId(pendingEffect, card.Id),
                    isUsed: faceUp && card.UseState == CardUseState.Used,
                    directSelectionCommand:
                        FindEnemyDirectSelectionCommand(battle, card.Id));

                // EnemyHand is mirrored by the camera just like PlayerHand: its highest model index
                // lands at screen-left. Keep hidden cards last and prepend each public draw so the
                // newest card always lands at the protagonist's screen-right edge.
                if (!isHiddenCard)
                {
                    cards.Insert(0, projectedCard);
                }
                else
                {
                    cards.Add(projectedCard);
                }
            }

            return cards.AsReadOnly();
        }

        private static bool UsesDiegeticCardEffectSelection(PendingCardEffect pendingEffect)
        {
            return pendingEffect != null &&
                (pendingEffect.ChoiceKind ==
                    CardEffectChoiceKind.DiscardOpponentFaceUpCard ||
                 pendingEffect.ChoiceKind ==
                    CardEffectChoiceKind.TakePeekedCard);
        }

        private static IReadOnlyList<GameSceneCardViewModel>
            CreateCrystalOrbCandidates(CoreLoopBattle battle)
        {
            PendingCardEffect pendingEffect = battle.PendingPlayerCardEffect;
            if (pendingEffect == null ||
                pendingEffect.EffectKind != CardEffectKind.CrystalOrb ||
                pendingEffect.ChoiceKind != CardEffectChoiceKind.TakePeekedCard)
            {
                return Array.AsReadOnly(Array.Empty<GameSceneCardViewModel>());
            }

            var candidates = new List<GameSceneCardViewModel>(
                pendingEffect.TemporaryCards.Count);
            foreach (BlackjackCard card in pendingEffect.TemporaryCards)
            {
                CardEffectChoiceOption option = null;
                foreach (CardEffectChoiceOption candidateOption in pendingEffect.Options)
                {
                    if (candidateOption.CardId == card.Id)
                    {
                        option = candidateOption;
                        break;
                    }
                }

                if (option == null)
                {
                    continue;
                }

                candidates.Add(new GameSceneCardViewModel(
                    card.Id,
                    card.Rank,
                    isFaceUp: true,
                    revealRank: true,
                    canUse: false,
                    card.Definition.DisplayName,
                    abilityDescription: ResolveAbilityDescription(card),
                    suit: card.Suit,
                    showHoverBadgeWhenUnavailable: true,
                    definitionKey: card.DefinitionKey,
                    directSelectionCommand: new GameSceneCombatHudCommand(
                        GameSceneCombatHudCommandKind.ResolveCardEffectChoice,
                        option.Id)));
            }

            return candidates.AsReadOnly();
        }

        private static IReadOnlyList<GameSceneCardViewModel>
            CreateSatanNumberCandidates(CoreLoopBattle battle)
        {
            PendingDemonContractInteraction interaction =
                battle.PendingPlayerDemonContractInteraction;
            if (interaction == null ||
                (interaction.Kind !=
                    DemonContractInteractionKind.SatanDeclareFirstNumber &&
                 interaction.Kind !=
                    DemonContractInteractionKind.SatanDeclareSecondNumber))
            {
                return Array.AsReadOnly(Array.Empty<GameSceneCardViewModel>());
            }

            var candidates = new List<GameSceneCardViewModel>(10);
            for (int number = 1; number <= 10; number++)
            {
                DemonContractOption option = null;
                foreach (DemonContractOption candidate in interaction.Options)
                {
                    if (candidate.NumericValue == number)
                    {
                        option = candidate;
                        break;
                    }
                }

                CardDefinition definition =
                    CardDefinitionCatalog.GetDefaultForRank(number);
                bool isBranded = interaction.ContextNumericValue == number;
                GameSceneCombatHudCommand? command = option == null
                    ? null
                    : new GameSceneCombatHudCommand(
                        GameSceneCombatHudCommandKind.ResolveDemonContractChoice,
                        option.OptionId,
                        interaction.InteractionId);
                candidates.Add(new GameSceneCardViewModel(
                    100000 + number,
                    number,
                    isFaceUp: true,
                    revealRank: true,
                    canUse: false,
                    definition.DisplayName,
                    suit: CardSuit.Spade,
                    definitionKey: definition.Key,
                    isUsed: isBranded,
                    directSelectionCommand: command));
            }

            return candidates.AsReadOnly();
        }

        private static bool UsesDiegeticSelection(CoreLoopBattle battle)
        {
            if (UsesDiegeticCardEffectSelection(battle.PendingPlayerCardEffect))
            {
                return true;
            }

            PendingAutomaticCardInteraction automatic =
                battle.PendingPlayerAutomaticInteraction;
            if (automatic != null && HasCardChoice(automatic.Options))
            {
                return true;
            }

            PendingDemonContractInteraction contract =
                battle.PendingPlayerDemonContractInteraction;
            return contract != null &&
                (contract.Kind == DemonContractInteractionKind.BeelzebubChooseOwnerCard ||
                 contract.Kind == DemonContractInteractionKind.BeelzebubChooseOpponentCard);
        }

        private static bool HasCardChoice(
            IReadOnlyList<AutomaticCardChoiceOption> options)
        {
            foreach (AutomaticCardChoiceOption option in options)
            {
                if (option.CardId.HasValue)
                {
                    return true;
                }
            }

            return false;
        }

        private static GameSceneCombatHudCommand? FindPlayerDirectSelectionCommand(
            CoreLoopBattle battle,
            int cardId)
        {
            PendingAutomaticCardInteraction automatic =
                battle.PendingPlayerAutomaticInteraction;
            if (automatic != null)
            {
                foreach (AutomaticCardChoiceOption option in automatic.Options)
                {
                    if (option.CardId == cardId)
                    {
                        return new GameSceneCombatHudCommand(
                            GameSceneCombatHudCommandKind.ResolveAutomaticCardChoice,
                            option.OptionId,
                            automatic.InteractionId);
                    }
                }
            }

            PendingDemonContractInteraction contract =
                battle.PendingPlayerDemonContractInteraction;
            if (contract == null || contract.Kind !=
                    DemonContractInteractionKind.BeelzebubChooseOwnerCard)
            {
                return null;
            }

            return FindDemonContractCardCommand(contract, cardId);
        }

        private static GameSceneCombatHudCommand? FindEnemyDirectSelectionCommand(
            CoreLoopBattle battle,
            int cardId)
        {
            int? cardEffectOptionId = FindCardEffectChoiceOptionId(
                battle.PendingPlayerCardEffect,
                cardId);
            if (cardEffectOptionId.HasValue)
            {
                return new GameSceneCombatHudCommand(
                    GameSceneCombatHudCommandKind.ResolveCardEffectChoice,
                    cardEffectOptionId.Value);
            }

            PendingDemonContractInteraction contract =
                battle.PendingPlayerDemonContractInteraction;
            if (contract == null || contract.Kind !=
                    DemonContractInteractionKind.BeelzebubChooseOpponentCard)
            {
                return null;
            }

            return FindDemonContractCardCommand(contract, cardId);
        }

        private static GameSceneCombatHudCommand? FindDemonContractCardCommand(
            PendingDemonContractInteraction interaction,
            int cardId)
        {
            foreach (DemonContractOption option in interaction.Options)
            {
                if (option.ContractCardId == cardId)
                {
                    return new GameSceneCombatHudCommand(
                        GameSceneCombatHudCommandKind.ResolveDemonContractChoice,
                        option.OptionId,
                        interaction.InteractionId);
                }
            }

            return null;
        }

        private static bool IsPlayerEffectSource(CoreLoopBattle battle, int cardId)
        {
            PendingCardEffect manual = battle.PendingPlayerCardEffect;
            if (manual != null && manual.SourceCardId == cardId)
            {
                return true;
            }

            PendingAutomaticCardInteraction automatic =
                battle.PendingPlayerAutomaticInteraction;
            return automatic != null && automatic.SourceCardId == cardId;
        }

        private static int? FindCardEffectChoiceOptionId(
            PendingCardEffect pendingEffect,
            int cardId)
        {
            if (pendingEffect == null ||
                pendingEffect.ChoiceKind !=
                    CardEffectChoiceKind.DiscardOpponentFaceUpCard)
            {
                return null;
            }

            foreach (CardEffectChoiceOption option in pendingEffect.Options)
            {
                if (option.CardId == cardId)
                {
                    return option.Id;
                }
            }

            return null;
        }
    }
}
