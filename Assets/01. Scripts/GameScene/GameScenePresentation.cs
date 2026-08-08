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

    internal enum EnemySpeechEventKind
    {
        PublicAction,
        AutomaticCardActivation,
    }

    internal enum EnemySpeechBeat
    {
        Immediate,
        BeforeEffect,
        AfterEffect,
    }

    internal sealed class EnemySpeechCue
    {
        public EnemySpeechCue(
            CoreLoopBattle battle,
            int roundNumber,
            int actionOrdinal,
            string cueKey,
            string sourceDefinitionKey)
            : this(
                battle,
                roundNumber,
                actionOrdinal,
                cueKey,
                sourceDefinitionKey,
                EnemySpeechEventKind.PublicAction,
                EnemySpeechBeat.Immediate)
        {
        }

        public EnemySpeechCue(
            CoreLoopBattle battle,
            int roundNumber,
            int actionOrdinal,
            string cueKey,
            string sourceDefinitionKey,
            EnemySpeechEventKind eventKind,
            EnemySpeechBeat beat,
            string fallbackCueKey = null,
            int sequenceIndex = 0)
        {
            Battle = battle;
            RoundNumber = roundNumber;
            ActionOrdinal = actionOrdinal;
            CueKey = cueKey ?? throw new ArgumentNullException(nameof(cueKey));
            SourceDefinitionKey = sourceDefinitionKey ?? string.Empty;
            EventKind = eventKind;
            Beat = beat;
            FallbackCueKey = fallbackCueKey ?? string.Empty;
            SequenceIndex = sequenceIndex;
        }

        public int ActionOrdinal { get; }

        public CoreLoopBattle Battle { get; }

        public string CueKey { get; }

        public EnemySpeechBeat Beat { get; }

        public EnemySpeechEventKind EventKind { get; }

        public string FallbackCueKey { get; }

        public int RoundNumber { get; }

        public string SourceDefinitionKey { get; }

        public int SequenceIndex { get; }

        public bool RequiresOrderedPlayback => Beat != EnemySpeechBeat.Immediate;

        public bool IsSameActionAs(EnemySpeechCue other)
        {
            return other != null &&
                ReferenceEquals(Battle, other.Battle) &&
                RoundNumber == other.RoundNumber &&
                ActionOrdinal == other.ActionOrdinal &&
                EventKind == other.EventKind;
        }


        public bool IsSameBeatAs(EnemySpeechCue other)
        {
            return IsSameActionAs(other) &&
                Beat == other.Beat &&
                SequenceIndex == other.SequenceIndex;
        }
    }

    internal sealed class EnemySpeechObservation
    {
        public EnemySpeechObservation(
            CoreLoopBattle battle,
            int roundNumber,
            int actionOrdinal,
            int enemySoulCurrent,
            int enemySoulMaximum,
            RoundResolution? lastResolution,
            BattleOutcome outcome,
            EnemySpeechCue actionCue)
            : this(
                battle,
                roundNumber,
                actionOrdinal,
                enemySoulCurrent,
                enemySoulMaximum,
                lastResolution,
                outcome,
                actionCue == null
                    ? Array.Empty<EnemySpeechCue>()
                    : Array.AsReadOnly(new[] { actionCue }))
        {
        }

        public EnemySpeechObservation(
            CoreLoopBattle battle,
            int roundNumber,
            int actionOrdinal,
            int enemySoulCurrent,
            int enemySoulMaximum,
            RoundResolution? lastResolution,
            BattleOutcome outcome,
            IReadOnlyList<EnemySpeechCue> actionCues)
        {
            Battle = battle ?? throw new ArgumentNullException(nameof(battle));
            RoundNumber = roundNumber;
            ActionOrdinal = actionOrdinal;
            EnemySoulCurrent = enemySoulCurrent;
            EnemySoulMaximum = enemySoulMaximum;
            LastResolution = lastResolution;
            Outcome = outcome;
            ActionCues = actionCues ?? throw new ArgumentNullException(
                nameof(actionCues));
        }

        public int ActionOrdinal { get; }

        public EnemySpeechCue ActionCue =>
            ActionCues.Count == 0 ? null : ActionCues[0];

        public IReadOnlyList<EnemySpeechCue> ActionCues { get; }

        public CoreLoopBattle Battle { get; }

        public int EnemySoulCurrent { get; }

        public int EnemySoulMaximum { get; }

        public RoundResolution? LastResolution { get; }

        public BattleOutcome Outcome { get; }

        public int RoundNumber { get; }
    }

    internal enum GameSceneCardHoverOutlineState
    {
        Basic,
        ManualUnavailable,
        ManualAvailable,
        Automatic,
        Used,
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
            bool showHoverBadgeWhenUnavailable = true,
            string definitionKey = "",
            bool showHoverBadgeBelow = false,
            int? cardEffectChoiceOptionId = null,
            bool isUsed = false,
            GameSceneCombatHudCommand? directSelectionCommand = null,
            bool isEffectSource = false,
            bool isSatanBranded = false,
            bool isEffectSourcePersistent = false)
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
            IsEffectSourcePersistent =
                isEffectSource && isEffectSourcePersistent;
            IsSatanBranded = isSatanBranded;
            HoverOutlineState = ResolveHoverOutlineState(
                DefinitionKey,
                CanUse,
                IsUsed);
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

        internal bool IsEffectSourcePersistent { get; }

        /// <summary>
        /// Whether Satan's upper-face declaration has branded this rank candidate.
        /// Kept separate from <see cref="IsUsed"/> so the normal used-card X mark is not shown.
        /// </summary>
        public bool IsSatanBranded { get; }

        internal GameSceneCardHoverOutlineState HoverOutlineState { get; }

        /// <summary>
        /// Stable card archetype key used only to select authored visuals. It remains empty for an
        /// unrevealed enemy card so the presentation boundary does not leak hidden information.
        /// </summary>
        public string DefinitionKey { get; }

        internal static GameSceneCardHoverOutlineState ResolveHoverOutlineState(
            string definitionKey,
            bool canUse,
            bool isUsed)
        {
            if (isUsed)
            {
                return GameSceneCardHoverOutlineState.Used;
            }

            CardActivationKind activation = CardActivationKind.None;
            foreach (CardDefinition definition in CardDefinitionCatalog.All)
            {
                if (string.Equals(
                    definition.Key,
                    definitionKey,
                    StringComparison.Ordinal))
                {
                    activation = definition.Activation;
                    break;
                }
            }

            switch (activation)
            {
                case CardActivationKind.Manual:
                    return canUse
                        ? GameSceneCardHoverOutlineState.ManualAvailable
                        : GameSceneCardHoverOutlineState.ManualUnavailable;
                case CardActivationKind.Automatic:
                    return GameSceneCardHoverOutlineState.Automatic;
                default:
                    return GameSceneCardHoverOutlineState.Basic;
            }
        }
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
            bool succeeded,
            int actionOrdinal = 0)
            : this(
                roundNumber,
                sourceCardId,
                actorSide,
                GameSceneRevolverAnimationPhase.Resolved,
                succeeded,
                actionOrdinal)
        {
        }

        public GameSceneRevolverAnimationCue(
            int roundNumber,
            int sourceCardId,
            CombatantSide actorSide,
            GameSceneRevolverAnimationPhase phase,
            bool succeeded = false,
            int actionOrdinal = 0)
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

            if (actionOrdinal < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(actionOrdinal));
            }

            RoundNumber = roundNumber;
            SourceCardId = sourceCardId;
            ActorSide = actorSide;
            Phase = phase;
            Succeeded = succeeded;
            ActionOrdinal = actionOrdinal;
        }

        public int ActionOrdinal { get; }

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
            bool succeeded = false,
            int actionOrdinal = 0)
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

            if (actionOrdinal < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(actionOrdinal));
            }

            RoundNumber = roundNumber;
            SourceCardId = sourceCardId;
            ActorSide = actorSide;
            Phase = phase;
            Succeeded = succeeded;
            ActionOrdinal = actionOrdinal;
        }

        public int ActionOrdinal { get; }

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
            int actionOrdinal,
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

            if (actionOrdinal < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(actionOrdinal));
            }

            RoundNumber = roundNumber;
            SourceCardId = sourceCardId;
            ActorSide = actorSide;
            Phase = phase;
            ActionOrdinal = actionOrdinal;
            TargetCardId = targetCardId;
        }

        public int RoundNumber { get; }

        public int SourceCardId { get; }

        public CombatantSide ActorSide { get; }

        public GameSceneHammerAnimationPhase Phase { get; }

        public int ActionOrdinal { get; }

        public int? TargetCardId { get; }
    }

    public sealed class GameSceneSatanAttackAnimationCue
    {
        public GameSceneSatanAttackAnimationCue(
            int roundNumber,
            int sourceCardId,
            CombatantSide actorSide,
            int actionOrdinal)
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

            if (actionOrdinal < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(actionOrdinal));
            }

            RoundNumber = roundNumber;
            SourceCardId = sourceCardId;
            ActorSide = actorSide;
            ActionOrdinal = actionOrdinal;
        }

        public int RoundNumber { get; }

        public int SourceCardId { get; }

        public CombatantSide ActorSide { get; }

        public int ActionOrdinal { get; }
    }

    public sealed class GameSceneSatanNumberGuessAnimationCue
    {
        public GameSceneSatanNumberGuessAnimationCue(
            int roundNumber,
            int sourceCardId,
            CombatantSide actorSide,
            int targetCardId,
            bool succeeded,
            int actionOrdinal)
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

            if (targetCardId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(targetCardId));
            }

            if (actionOrdinal < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(actionOrdinal));
            }

            RoundNumber = roundNumber;
            SourceCardId = sourceCardId;
            ActorSide = actorSide;
            TargetCardId = targetCardId;
            Succeeded = succeeded;
            ActionOrdinal = actionOrdinal;
        }

        public int RoundNumber { get; }

        public int SourceCardId { get; }

        public CombatantSide ActorSide { get; }

        public int TargetCardId { get; }

        public bool Succeeded { get; }

        public int ActionOrdinal { get; }
    }

    /// <summary>
    /// One-shot cue for the Enforcer's per-round poison injection: a poison card was just added
    /// to the player's deck this round. Keyed only by <see cref="RoundNumber"/> (mirroring the
    /// weapon cues) since the injection itself carries no per-card visual identity — the view
    /// layer just announces "a poison card entered the deck" once per round.
    /// </summary>
    public sealed class GameScenePoisonInjectionAnimationCue
    {
        public GameScenePoisonInjectionAnimationCue(int roundNumber)
        {
            if (roundNumber < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(roundNumber));
            }

            RoundNumber = roundNumber;
        }

        public int RoundNumber { get; }
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
            int playerDrawPileCount,
            int playerDiscardPileCount,
            int enemyDrawPileCount,
            int enemyDiscardPileCount,
            GameSceneRevolverAnimationCue revolverAnimationCue = null,
            GameSceneHammerAnimationCue hammerAnimationCue = null,
            bool usesDiegeticCardEffectSelection = false,
            bool focusesEnemyCardsForSelection = false,
            string playerTotalsText = null,
            string enemyTotalsText = null,
            GameSceneKnifeAnimationCue knifeAnimationCue = null,
            GameSceneSatanAttackAnimationCue satanAttackAnimationCue = null,
            int? playerMammonDieValue = null,
            int? enemyMammonDieValue = null,
            int? playerMammonSourceCardId = null,
            bool canPlayerRerollMammon = false,
            GameScenePoisonInjectionAnimationCue poisonInjectionAnimationCue = null,
            GameSceneSatanNumberGuessAnimationCue satanNumberGuessAnimationCue = null)
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
            PlayerDrawPileCount = playerDrawPileCount;
            PlayerDiscardPileCount = playerDiscardPileCount;
            EnemyDrawPileCount = enemyDrawPileCount;
            EnemyDiscardPileCount = enemyDiscardPileCount;
            RevolverAnimationCue = revolverAnimationCue;
            HammerAnimationCue = hammerAnimationCue;
            UsesDiegeticCardEffectSelection = usesDiegeticCardEffectSelection;
            FocusesEnemyCardsForSelection = focusesEnemyCardsForSelection;
            PlayerTotalsText = playerTotalsText ?? core.PlayerTotalsText;
            EnemyTotalsText = enemyTotalsText ?? core.EnemyVisibleTotalText;
            KnifeAnimationCue = knifeAnimationCue;
            SatanAttackAnimationCue = satanAttackAnimationCue;
            PlayerMammonDieValue = playerMammonDieValue;
            EnemyMammonDieValue = enemyMammonDieValue;
            PlayerMammonSourceCardId = playerMammonSourceCardId;
            CanPlayerRerollMammon = canPlayerRerollMammon;
            PoisonInjectionAnimationCue = poisonInjectionAnimationCue;
            SatanNumberGuessAnimationCue = satanNumberGuessAnimationCue;
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

        public int PlayerDrawPileCount { get; }

        public int PlayerDiscardPileCount { get; }

        public int EnemyDrawPileCount { get; }

        public int EnemyDiscardPileCount { get; }

        public GameSceneRevolverAnimationCue RevolverAnimationCue { get; }

        public GameSceneHammerAnimationCue HammerAnimationCue { get; }

        public GameSceneKnifeAnimationCue KnifeAnimationCue { get; }

        public GameSceneSatanAttackAnimationCue SatanAttackAnimationCue { get; }

        public GameScenePoisonInjectionAnimationCue PoisonInjectionAnimationCue { get; }

        public GameSceneSatanNumberGuessAnimationCue SatanNumberGuessAnimationCue { get; }

        public bool UsesDiegeticCardEffectSelection { get; }

        public bool FocusesEnemyCardsForSelection { get; }

        public string PlayerTotalsText { get; }

        public string EnemyTotalsText { get; }

        public int? PlayerMammonDieValue { get; }

        public int? EnemyMammonDieValue { get; }

        public int? PlayerMammonSourceCardId { get; }

        public bool CanPlayerRerollMammon { get; }

        internal EnemySpeechCue EnemySpeechCue { get; set; }

        internal EnemySpeechObservation EnemySpeechObservation { get; set; }

        internal PlayerMammonComparisonPlan PlayerMammonComparisonPlan { get; set; }

        internal RoundComparisonPlan RoundComparisonPlan { get; set; }

        internal EnemyDecision EnemyActionSkullDecision { get; set; }

        internal int EnemyActionSkullDecisionOrdinal { get; set; }

        internal PublicCombatAction LastPublicAction { get; set; }

        internal int? LastPublicActionSourceCardId { get; set; }

        internal int PublicActionOrdinal { get; set; }

        internal bool PlayerIsStanding { get; set; }

        internal bool EnemyIsStanding { get; set; }

        internal IReadOnlyList<SoulLossRecord> SoulLossHistory { get; set; } =
            Array.AsReadOnly(Array.Empty<SoulLossRecord>());
    }

    public static class GameScenePresenter
    {
        private enum EffectSourceCardKind
        {
            Normal,
            Demon,
        }

        private readonly struct EffectSourceProjection
        {
            public EffectSourceProjection(
                int cardId,
                CombatantSide ownerSide,
                EffectSourceCardKind cardKind,
                bool isPersistent)
            {
                CardId = cardId;
                OwnerSide = ownerSide;
                CardKind = cardKind;
                IsPersistent = isPersistent;
            }

            public int CardId { get; }

            public EffectSourceCardKind CardKind { get; }

            public bool IsPersistent { get; }

            public CombatantSide OwnerSide { get; }

            public bool Matches(
                int cardId,
                CombatantSide ownerSide,
                EffectSourceCardKind cardKind)
            {
                return CardId == cardId &&
                    OwnerSide == ownerSide &&
                    CardKind == cardKind;
            }
        }

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
            enemyLabel = FilterEnemyActionLabel(enemyLabel);
            ActiveDemonContract playerMammon = FindMammonContract(
                battle.ActivePlayerDemonContracts);
            ActiveDemonContract enemyMammon = FindMammonContract(
                battle.ActiveEnemyDemonContracts);
            EffectSourceProjection? effectSource =
                CreateEffectSourceProjection(battle);
            GameSceneViewModel model = new GameSceneViewModel(
                core,
                CreatePlayerCards(
                    core,
                    battle,
                    revealRoundResult,
                    effectSource),
                CreateEnemyCards(
                    battle,
                    revealRoundResult,
                    effectSource),
                CreateActiveDemonCards(
                    battle,
                    battle.ActivePlayerDemonContracts,
                    exposePlayerActions: true,
                    effectSource: effectSource),
                CreateActiveDemonCards(
                    battle,
                    battle.ActiveEnemyDemonContracts,
                    exposePlayerActions: false,
                    effectSource: effectSource),
                enemyVisual,
                enemyLabel,
                CreateCrystalOrbCandidates(battle),
                CreateSatanNumberCandidates(battle),
                battle.Player.Deck.DrawCount,
                battle.Player.Deck.DiscardCount,
                battle.Enemy.Deck.DrawCount,
                battle.Enemy.Deck.DiscardCount,
                CreateRevolverAnimationCue(battle),
                CreateHammerAnimationCue(battle),
                UsesDiegeticSelection(battle),
                FocusesEnemyCardsForSelection(battle),
                CreatePlayerTotalsText(
                    battle,
                    core,
                    revealRoundResult,
                    revealRoundResult ? battle.LastResolutionPlayerBonus : 0),
                CreateEnemyTotalsText(
                    battle,
                    core,
                    revealRoundResult,
                    revealRoundResult ? battle.LastResolutionEnemyBonus : 0),
                CreateKnifeAnimationCue(battle),
                CreateSatanAttackAnimationCue(battle),
                FindMammonDieValue(playerMammon),
                FindMammonDieValue(enemyMammon),
                playerMammon?.SourceCardId,
                playerMammon != null &&
                    battle.CanBeginPlayerActiveDemonContractAction(
                        playerMammon.SourceCardId),
                CreatePoisonInjectionAnimationCue(battle),
                CreateSatanNumberGuessAnimationCue(battle));
            IReadOnlyList<EnemySpeechCue> speechCues =
                CreateEnemySpeechCues(battle);
            model.EnemySpeechCue = speechCues.Count == 0
                ? null
                : speechCues[0];
            model.EnemySpeechObservation = CreateEnemySpeechObservation(
                battle,
                speechCues);
            model.PlayerMammonComparisonPlan =
                RoundComparisonPresenter.CreatePlayerMammonPending(
                    battle,
                    model.PlayerCards);
            model.RoundComparisonPlan = RoundComparisonPresenter.CreateResolved(
                battle,
                model.PlayerCards,
                model.EnemyCards,
                model.RevolverAnimationCue,
                model.SatanNumberGuessAnimationCue,
                model.KnifeAnimationCue);
            model.SoulLossHistory = new List<SoulLossRecord>(
                battle.SoulLossHistory).AsReadOnly();
            model.EnemyActionSkullDecision = battle.LastEnemyDecision;
            model.EnemyActionSkullDecisionOrdinal = battle.EnemyDecisionOrdinal;
            model.LastPublicAction = battle.LastPublicAction;
            model.LastPublicActionSourceCardId =
                battle.LastPublicActionSourceCardId;
            model.PublicActionOrdinal = battle.PublicActionHistory.Count;
            model.PlayerIsStanding = battle.Player.IsStanding;
            model.EnemyIsStanding = battle.Enemy.IsStanding;
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

            string cueKey;
            switch (action.ActionType)
            {
                case PublicCombatActionType.Hit:
                    cueKey = SpeechCueKeys.ActionHit;
                    break;
                case PublicCombatActionType.Stand:
                    cueKey = SpeechCueKeys.ActionStand;
                    break;
                case PublicCombatActionType.Change:
                    cueKey = SpeechCueKeys.ActionChange;
                    break;
                case PublicCombatActionType.UseCard:
                    cueKey = SpeechCueKeys.ActionUseCard;
                    break;
                case PublicCombatActionType.DemonContract:
                    cueKey = SpeechCueKeys.ActionDemonContract;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action));
            }

            return new EnemySpeechCue(
                battle,
                roundNumber,
                actionOrdinal,
                cueKey,
                action.SourceCardDefinitionKey);
        }

        private static EnemySpeechObservation CreateEnemySpeechObservation(
            CoreLoopBattle battle,
            IReadOnlyList<EnemySpeechCue> actionCues)
        {
            return new EnemySpeechObservation(
                battle,
                battle.RoundNumber,
                battle.PublicActionHistory.Count,
                battle.Enemy.Soul.Current,
                battle.Enemy.Soul.Maximum,
                battle.LastResolution,
                battle.Outcome,
                actionCues);
        }

        private static IReadOnlyList<EnemySpeechCue> CreateEnemySpeechCues(
            CoreLoopBattle battle)
        {
            var cues = new List<EnemySpeechCue>();
            IReadOnlyList<PublicCombatAction> history = battle.PublicActionHistory;
            int count = history.Count;
            if (count > 0)
            {
                AddPublicActionSpeechCues(
                    battle,
                    history[count - 1],
                    count,
                    cues);
            }

            EnemySpeechCue automaticCue = CreateAutomaticCardSpeechCue(battle);
            if (automaticCue != null)
            {
                int afterEffectIndex = cues.FindIndex(
                    cue => cue.Beat == EnemySpeechBeat.AfterEffect);
                if (afterEffectIndex >= 0)
                {
                    cues.Insert(afterEffectIndex, automaticCue);
                }
                else
                {
                    cues.Add(automaticCue);
                }
            }

            return cues.AsReadOnly();
        }

        private static void AddPublicActionSpeechCues(
            CoreLoopBattle battle,
            PublicCombatAction action,
            int actionOrdinal,
            List<EnemySpeechCue> cues)
        {
            if (action.ActorSide == CombatantSide.Player)
            {
                if (action.ActionType == PublicCombatActionType.UseCard)
                {
                    cues.Add(CreateOrderedCue(
                        battle,
                        battle.RoundNumber,
                        actionOrdinal,
                        SpeechCueKeys.ReactionPlayerManualCard,
                        action.SourceCardDefinitionKey,
                        EnemySpeechBeat.BeforeEffect,
                        SpeechCueKeys.ActionUseCard));
                }
                else if (action.ActionType ==
                    PublicCombatActionType.DemonContract)
                {
                    cues.Add(CreateOrderedCue(
                        battle,
                        battle.RoundNumber,
                        actionOrdinal,
                        SpeechCueKeys.ReactionPlayerDemonContract,
                        action.SourceCardDefinitionKey,
                        EnemySpeechBeat.BeforeEffect,
                        SpeechCueKeys.ActionDemonContract));
                }

                return;
            }

            if (action.ActionType == PublicCombatActionType.DemonContract)
            {
                cues.Add(CreateOrderedCue(
                    battle,
                    battle.RoundNumber,
                    actionOrdinal,
                    SpeechCueKeys.GetDemonContractAction(
                        action.SourceCardDefinitionKey),
                    action.SourceCardDefinitionKey,
                    EnemySpeechBeat.BeforeEffect,
                    SpeechCueKeys.ActionDemonContract));
                return;
            }

            if (action.ActionType != PublicCombatActionType.UseCard)
            {
                EnemySpeechCue generic = CreateEnemySpeechCue(
                    battle.RoundNumber,
                    actionOrdinal,
                    action,
                    battle);
                if (generic != null)
                {
                    cues.Add(generic);
                }

                return;
            }

            CardEffectKind effectKind = CardDefinitionCatalog
                .GetByKey(action.SourceCardDefinitionKey)
                .Effect;
            switch (effectKind)
            {
                case CardEffectKind.AutoPistol:
                    AddManualEffectSpeechCues(
                        battle,
                        action,
                        actionOrdinal,
                        SpeechCueKeys.ActionRevolverBefore,
                        SpeechCueKeys.ActionRevolverHit,
                        SpeechCueKeys.ActionRevolverMiss,
                        result => result.Succeeded,
                        ResolveRevolverSpeechSequenceIndex(battle),
                        cues);
                    break;
                case CardEffectKind.MilitaryKnife:
                    AddManualEffectSpeechCues(
                        battle,
                        action,
                        actionOrdinal,
                        SpeechCueKeys.ActionKnifeBefore,
                        SpeechCueKeys.ActionKnifeBust,
                        SpeechCueKeys.ActionKnifeNoBust,
                        result => result.EndedRound,
                        1,
                        cues);
                    break;
                case CardEffectKind.ThreatHammer:
                    AddManualEffectSpeechCues(
                        battle,
                        action,
                        actionOrdinal,
                        SpeechCueKeys.ActionHammerBefore,
                        SpeechCueKeys.ActionHammerBust,
                        SpeechCueKeys.ActionHammerNoBust,
                        result => result.EndedRound,
                        1,
                        cues);
                    break;
                default:
                    cues.Add(new EnemySpeechCue(
                        battle,
                        battle.RoundNumber,
                        actionOrdinal,
                        SpeechCueKeys.ActionUseCard,
                        action.SourceCardDefinitionKey));
                    break;
            }
        }

        private static void AddManualEffectSpeechCues(
            CoreLoopBattle battle,
            PublicCombatAction action,
            int actionOrdinal,
            string beforeKey,
            string successfulKey,
            string unsuccessfulKey,
            Func<CardEffectResult, bool> isSuccessful,
            int sequenceIndex,
            List<EnemySpeechCue> cues)
        {
            cues.Add(CreateOrderedCue(
                battle,
                battle.RoundNumber,
                actionOrdinal,
                beforeKey,
                action.SourceCardDefinitionKey,
                EnemySpeechBeat.BeforeEffect,
                SpeechCueKeys.ActionUseCard,
                sequenceIndex));

            if (!battle.LastCardEffectResult.HasValue ||
                battle.LastCardEffectActorSide != CombatantSide.Enemy ||
                !battle.LastPublicActionSourceCardId.HasValue)
            {
                return;
            }

            CardEffectResult result = battle.LastCardEffectResult.Value;
            if (result.SourceCardId !=
                battle.LastPublicActionSourceCardId.Value)
            {
                return;
            }

            cues.Add(CreateOrderedCue(
                battle,
                battle.RoundNumber,
                actionOrdinal,
                isSuccessful(result) ? successfulKey : unsuccessfulKey,
                action.SourceCardDefinitionKey,
                EnemySpeechBeat.AfterEffect,
                SpeechCueKeys.ActionUseCard,
                sequenceIndex));
        }

        private static EnemySpeechCue CreateAutomaticCardSpeechCue(
            CoreLoopBattle battle)
        {
            if (battle.LastAutomaticCardActivationOrdinal <= 0 ||
                !battle.LastAutomaticCardActivationOwnerSide.HasValue ||
                string.IsNullOrWhiteSpace(
                    battle.LastAutomaticCardActivationDefinitionKey))
            {
                return null;
            }

            CombatantSide ownerSide =
                battle.LastAutomaticCardActivationOwnerSide.Value;
            string definitionKey =
                battle.LastAutomaticCardActivationDefinitionKey;
            return new EnemySpeechCue(
                battle,
                battle.LastAutomaticCardActivationRoundNumber,
                battle.LastAutomaticCardActivationOrdinal,
                ownerSide == CombatantSide.Enemy
                    ? SpeechCueKeys.GetAutomaticCardAction(definitionKey)
                    : SpeechCueKeys.ReactionPlayerAutomaticCard,
                definitionKey,
                EnemySpeechEventKind.AutomaticCardActivation,
                EnemySpeechBeat.BeforeEffect,
                SpeechCueKeys.ActionUseCard);
        }

        private static EnemySpeechCue CreateOrderedCue(
            CoreLoopBattle battle,
            int roundNumber,
            int actionOrdinal,
            string cueKey,
            string sourceDefinitionKey,
            EnemySpeechBeat beat,
            string fallbackCueKey,
            int sequenceIndex = 0)
        {
            return new EnemySpeechCue(
                battle,
                roundNumber,
                actionOrdinal,
                cueKey,
                sourceDefinitionKey,
                EnemySpeechEventKind.PublicAction,
                beat,
                fallbackCueKey,
                sequenceIndex);
        }

        private static int ResolveRevolverSpeechSequenceIndex(
            CoreLoopBattle battle)
        {
            if (battle.HasPendingLeviathanAutoPistolRetry)
            {
                return 2;
            }

            return battle.LastLeviathanCardEffectResult != null &&
                battle.LastLeviathanCardEffectResult.ActivationSuccesses.Count == 2
                    ? 2
                    : 1;
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
            bool revealRoundResult,
            int appliedMammonBonus)
        {
            return revealRoundResult
                ? FormatFinalTotals(
                    battle.Player.KnownHandValue.Total + appliedMammonBonus,
                    battle.Player.VisibleHandValue.Total)
                : core.PlayerTotalsText;
        }

        private static string CreateEnemyTotalsText(
            CoreLoopBattle battle,
            CoreLoopViewModel core,
            bool revealRoundResult,
            int appliedMammonBonus)
        {
            return revealRoundResult
                ? FormatFinalTotals(
                    battle.Enemy.KnownHandValue.Total + appliedMammonBonus,
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
                    GameSceneHammerAnimationPhase.Ready,
                    battle.PublicActionHistory.Count);
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
                battle.PublicActionHistory.Count,
                result.TargetCardId);
        }

        private static GameSceneRevolverAnimationCue CreateRevolverAnimationCue(
            CoreLoopBattle battle)
        {
            int actionOrdinal = ResolveLastUseCardActionOrdinal(
                battle,
                CardEffectKind.AutoPistol);
            if (actionOrdinal < 0)
            {
                return null;
            }

            if (battle.HasPendingLeviathanAutoPistolRetry &&
                battle.LastCardEffectResult.HasValue &&
                battle.LastCardEffectActorSide.HasValue &&
                battle.LastCardEffectResultActionOrdinal == actionOrdinal)
            {
                CardEffectResult retryResult = battle.LastCardEffectResult.Value;
                return new GameSceneRevolverAnimationCue(
                    battle.RoundNumber,
                    retryResult.SourceCardId,
                    battle.LastCardEffectActorSide.Value,
                    GameSceneRevolverAnimationPhase.ResolvedWithRetry,
                    retryResult.Succeeded,
                    actionOrdinal);
            }

            PendingCardEffect pendingPlayerEffect = battle.PendingPlayerCardEffect;
            if (pendingPlayerEffect != null &&
                pendingPlayerEffect.EffectKind == CardEffectKind.AutoPistol)
            {
                return new GameSceneRevolverAnimationCue(
                    battle.RoundNumber,
                    pendingPlayerEffect.SourceCardId,
                    CombatantSide.Player,
                    GameSceneRevolverAnimationPhase.Ready,
                    actionOrdinal: actionOrdinal);
            }

            if (battle.PendingEnemyCardEffect != null ||
                !battle.LastCardEffectResult.HasValue ||
                !battle.LastCardEffectActorSide.HasValue)
            {
                return null;
            }

            CardEffectResult result = battle.LastCardEffectResult.Value;
            if (result.EffectKind != CardEffectKind.AutoPistol ||
                !IsLastUseCardEffect(battle, result.EffectKind) ||
                battle.LastCardEffectResultActionOrdinal != actionOrdinal)
            {
                return null;
            }

            return new GameSceneRevolverAnimationCue(
                battle.RoundNumber,
                result.SourceCardId,
                battle.LastCardEffectActorSide.Value,
                result.Succeeded,
                actionOrdinal);
        }

        private static GameSceneKnifeAnimationCue CreateKnifeAnimationCue(
            CoreLoopBattle battle)
        {
            int actionOrdinal = ResolveLastUseCardActionOrdinal(
                battle,
                CardEffectKind.MilitaryKnife);
            if (actionOrdinal < 0)
            {
                return null;
            }

            // Checked before the "still active" branch below: a bust-replacement contract
            // (e.g. Beelzebub) can publish the knife's result and then suspend the effect
            // for a player choice before CompleteCardEffectResult ever clears
            // ActiveCardEffectKind — without checking the result first, that in-flight
            // context would keep reporting Ready and the hit/miss reveal would never show
            // until after the contract's choice UI had already appeared.
            if (battle.LastCardEffectResult.HasValue &&
                battle.LastCardEffectActorSide.HasValue)
            {
                CardEffectResult resolvedResult = battle.LastCardEffectResult.Value;
                if (resolvedResult.EffectKind == CardEffectKind.MilitaryKnife &&
                    IsLastUseCardEffect(battle, CardEffectKind.MilitaryKnife) &&
                    battle.LastCardEffectResultActionOrdinal == actionOrdinal)
                {
                    return new GameSceneKnifeAnimationCue(
                        battle.RoundNumber,
                        resolvedResult.SourceCardId,
                        battle.LastCardEffectActorSide.Value,
                        GameSceneKnifeAnimationPhase.Resolved,
                        resolvedResult.EndedRound,
                        actionOrdinal);
                }
            }

            if (battle.ActiveCardEffectKind == CardEffectKind.MilitaryKnife &&
                battle.ActiveCardEffectSourceCardId.HasValue &&
                battle.ActiveCardEffectActorSide.HasValue &&
                IsLastUseCardEffect(battle, CardEffectKind.MilitaryKnife))
            {
                return new GameSceneKnifeAnimationCue(
                    battle.RoundNumber,
                    battle.ActiveCardEffectSourceCardId.Value,
                    battle.ActiveCardEffectActorSide.Value,
                    GameSceneKnifeAnimationPhase.Ready,
                    actionOrdinal: actionOrdinal);
            }

            return null;
        }

        private static GameSceneSatanAttackAnimationCue
            CreateSatanAttackAnimationCue(CoreLoopBattle battle)
        {
            PublicCombatAction action = battle.LastPublicAction;
            if (action == null ||
                action.ActionType != PublicCombatActionType.DemonContract ||
                !String.Equals(
                    action.SourceCardDefinitionKey,
                    DemonContractCatalog.SatanKey,
                    StringComparison.Ordinal) ||
                !battle.LastPublicActionSourceCardId.HasValue ||
                battle.LastSatanForcedDrawActionOrdinal !=
                    battle.PublicActionHistory.Count)
            {
                return null;
            }

            int sourceCardId = battle.LastPublicActionSourceCardId.Value;
            IReadOnlyList<ActiveDemonContract> contracts =
                action.ActorSide == CombatantSide.Player
                    ? battle.ActivePlayerDemonContracts
                    : battle.ActiveEnemyDemonContracts;
            foreach (ActiveDemonContract contract in contracts)
            {
                if (contract.SourceCardId == sourceCardId &&
                    contract.Kind == DemonContractKind.Satan &&
                    contract.RuntimeState is SatanRuntimeState satanState &&
                    satanState.CurrentFace == SatanContractFace.Lower)
                {
                    return new GameSceneSatanAttackAnimationCue(
                        battle.RoundNumber,
                        sourceCardId,
                        action.ActorSide,
                        battle.PublicActionHistory.Count);
                }
            }

            return null;
        }

        private static GameSceneSatanNumberGuessAnimationCue
            CreateSatanNumberGuessAnimationCue(CoreLoopBattle battle)
        {
            if (battle.LastSatanNumberGuessActionOrdinal < 0 ||
                battle.LastSatanNumberGuessActionOrdinal !=
                    battle.PublicActionHistory.Count)
            {
                return null;
            }

            PublicCombatAction action = battle.LastPublicAction;
            if (action == null ||
                action.ActionType != PublicCombatActionType.DemonContract ||
                !String.Equals(
                    action.SourceCardDefinitionKey,
                    DemonContractCatalog.SatanKey,
                    StringComparison.Ordinal) ||
                !battle.LastPublicActionSourceCardId.HasValue)
            {
                return null;
            }

            return new GameSceneSatanNumberGuessAnimationCue(
                battle.RoundNumber,
                battle.LastPublicActionSourceCardId.Value,
                battle.LastSatanNumberGuessActorSide,
                battle.LastSatanNumberGuessTargetCardId,
                battle.LastSatanNumberGuessSucceeded,
                battle.LastSatanNumberGuessActionOrdinal);
        }

        private static GameScenePoisonInjectionAnimationCue
            CreatePoisonInjectionAnimationCue(CoreLoopBattle battle)
        {
            // Injection happens once at the start of a round (DealStartingRoundCards) and the
            // injected card is cleaned up at round end, so a nonzero count for the whole round
            // is exactly "this round got a poison card" — keyed by round like the weapon cues so
            // the view layer plays it once per round rather than once per re-render.
            return battle.InjectedPoisonCardCount > 0
                ? new GameScenePoisonInjectionAnimationCue(battle.RoundNumber)
                : null;
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

        private static int ResolveLastUseCardActionOrdinal(
            CoreLoopBattle battle,
            CardEffectKind effectKind)
        {
            IReadOnlyList<PublicCombatAction> history =
                battle.PublicActionHistory;
            for (int index = history.Count - 1; index >= 0; index--)
            {
                PublicCombatAction action = history[index];
                if (action == null ||
                    action.ActionType != PublicCombatActionType.UseCard ||
                    string.IsNullOrWhiteSpace(action.SourceCardDefinitionKey))
                {
                    continue;
                }

                CardDefinition definition = CardDefinitionCatalog.GetByKey(
                    action.SourceCardDefinitionKey);
                if (definition.Effect == effectKind)
                {
                    return index + 1;
                }
            }

            return -1;
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
                case RoundOutcome.MutualLoss:
                    return (CharacterVisualState.Lose, "LOSE");
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
                    if (kind == CardEffectKind.AutoPistol)
                    {
                        // The revolver keeps the target's tense/threatened expression through
                        // the whole resolution — a hit or a miss looks the same until the round
                        // itself reacts, so it never flips back to neutral mid-sequence.
                        visual = CharacterVisualState.AttackThreatened;
                    }
                    else if (!completedResult.HasValue)
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
            bool revealRoundResult,
            EffectSourceProjection? effectSource)
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
                    isEffectSource: IsEffectSource(
                        effectSource,
                        card.CardId,
                        CombatantSide.Player,
                        EffectSourceCardKind.Normal),
                    isEffectSourcePersistent:
                        IsPersistentEffectSource(
                            effectSource,
                            card.CardId,
                            CombatantSide.Player,
                            EffectSourceCardKind.Normal));

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
                bool exposePlayerActions,
                EffectSourceProjection? effectSource)
        {
            var cards = new List<GameSceneDemonCardViewModel>(contracts.Count);
            foreach (ActiveDemonContract contract in contracts)
            {
                DemonContractDefinition definition = contract.Definition;
                bool isSatan = contract.Kind == DemonContractKind.Satan;
                bool isUpsideDown = isSatan &&
                    contract.RuntimeState is SatanRuntimeState satanState &&
                    satanState.CurrentFace == SatanContractFace.Lower;
                int? satanDoomCount = isSatan &&
                    contract.RuntimeState is SatanRuntimeState doomState
                        ? doomState.RemainingDoomCount
                        : (int?)null;
                // Mammon's reroll now only triggers from the physical table die, not the contract
                // card itself; the card's own click is inert regardless of what the shared
                // CanBeginPlayerActiveDemonContractAction check would otherwise allow. Satan's
                // ability is inert here too, but for a different reason: it no longer has a
                // pressable entry point at all — CanBeginPlayerActiveDemonContractAction already
                // returns false for it, offered instead via the once-per-turn "use ability?"
                // choice (same UI pattern as Asmodeus/Mammon's own turn-start choices).
                bool isMammon = contract.Kind == DemonContractKind.Mammon;
                cards.Add(new GameSceneDemonCardViewModel(
                    contract.SourceCardId,
                    definition.Key,
                    isFaceUp: true,
                    canUse: exposePlayerActions &&
                        !isMammon &&
                        battle.CanBeginPlayerActiveDemonContractAction(
                            contract.SourceCardId),
                    definition.DisplayName,
                    definition.Summary,
                    definition.CostSummary,
                    showHoverBadgeWhenUnavailable: true,
                    isUpsideDown: isUpsideDown,
                    satanDoomCount: satanDoomCount,
                    isEffectSource: IsEffectSource(
                        effectSource,
                        contract.SourceCardId,
                        contract.OwnerSide,
                        EffectSourceCardKind.Demon),
                    isEffectSourcePersistent:
                        IsPersistentEffectSource(
                            effectSource,
                            contract.SourceCardId,
                            contract.OwnerSide,
                            EffectSourceCardKind.Demon)));
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
            bool revealRoundResult,
            EffectSourceProjection? effectSource)
        {
            IReadOnlyList<BlackjackCard> hand = battle.Enemy.Hand.Cards;
            PendingCardEffect pendingEffect = battle.PendingPlayerCardEffect;
            var cards = new List<GameSceneCardViewModel>(hand.Count);
            foreach (BlackjackCard card in hand)
            {
                // Face-down enemy card: emit only safe placeholder copy. This is the
                // information-hiding boundary for rank, definition, and real card name.
                bool faceUp = card.IsFaceUp || revealRoundResult;
                bool isHiddenCard = battle.Enemy.Hand.IsHiddenCard(card.Id);
                var projectedCard = new GameSceneCardViewModel(
                    card.Id,
                    faceUp ? card.Rank : 0,
                    faceUp,
                    revealRank: faceUp,
                    canUse: false,
                    faceUp ? card.Definition.DisplayName : "비공개 카드",
                    abilityDescription: faceUp
                        ? ResolveAbilityDescription(card)
                        : "공개되기 전에는 정보를 확인할 수 없습니다.",
                    suit: card.Suit,
                    showHoverBadgeWhenUnavailable: true,
                    definitionKey: faceUp ? card.DefinitionKey : string.Empty,
                    showHoverBadgeBelow: true,
                    cardEffectChoiceOptionId:
                        FindCardEffectChoiceOptionId(pendingEffect, card.Id),
                    isUsed: faceUp && card.UseState == CardUseState.Used,
                    directSelectionCommand:
                        FindEnemyDirectSelectionCommand(battle, card.Id),
                    isEffectSource: IsEffectSource(
                        effectSource,
                        card.Id,
                        CombatantSide.Enemy,
                        EffectSourceCardKind.Normal),
                    isEffectSourcePersistent:
                        IsPersistentEffectSource(
                            effectSource,
                            card.Id,
                            CombatantSide.Enemy,
                            EffectSourceCardKind.Normal));

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
            if (battle.State == CoreLoopState.PlayerChoosingChangeCard)
            {
                var changeCandidates = new List<GameSceneCardViewModel>(
                    battle.PlayerChangeCandidates.Count);
                for (int i = 0; i < battle.PlayerChangeCandidates.Count; i++)
                {
                    BlackjackCard card = battle.PlayerChangeCandidates[i];
                    changeCandidates.Add(new GameSceneCardViewModel(
                        card.Id,
                        card.Rank,
                        isFaceUp: true,
                        revealRank: true,
                        canUse: true,
                        card.Definition.DisplayName,
                        abilityDescription: ResolveAbilityDescription(card),
                        suit: card.Suit,
                        showHoverBadgeWhenUnavailable: true,
                        definitionKey: card.DefinitionKey,
                        directSelectionCommand: new GameSceneCombatHudCommand(
                            GameSceneCombatHudCommandKind.SelectChangedCard,
                            i)));
                }

                return changeCandidates.AsReadOnly();
            }

            PendingCardEffect pendingEffect = battle.PendingPlayerCardEffect;
            if (pendingEffect == null ||
                pendingEffect.EffectKind != CardEffectKind.CrystalOrb ||
                pendingEffect.ChoiceKind != CardEffectChoiceKind.TakePeekedCard)
            {
                return CreateBelphegorPreviewCandidate(battle);
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
            CreateBelphegorPreviewCandidate(CoreLoopBattle battle)
        {
            PlayerDemonContractPreview preview = battle.PlayerDemonContractPreview;
            if (preview == null ||
                preview.ContractKind != DemonContractKind.Belphegor)
            {
                return Array.AsReadOnly(Array.Empty<GameSceneCardViewModel>());
            }

            CardDefinition definition = CardDefinitionCatalog.GetByKey(
                preview.DefinitionKey);
            return Array.AsReadOnly(new[]
            {
                new GameSceneCardViewModel(
                    preview.CardId,
                    preview.Rank,
                    isFaceUp: true,
                    revealRank: true,
                    canUse: false,
                    definition.DisplayName,
                    abilityDescription: definition.Description,
                    suit: preview.Suit,
                    showHoverBadgeWhenUnavailable: true,
                    definitionKey: preview.DefinitionKey)
            });
        }

        internal static string FilterEnemyActionLabel(string label)
        {
            switch (label)
            {
                case "HIT":
                case "STAND":
                case "CHANGE":
                case "CONTRACT":
                    return label;
                default:
                    return string.Empty;
            }
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
                    showHoverBadgeWhenUnavailable: false,
                    definitionKey: definition.Key,
                    isUsed: false,
                    directSelectionCommand: command,
                    isSatanBranded: isBranded));
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

        private static EffectSourceProjection? CreateEffectSourceProjection(
            CoreLoopBattle battle)
        {
            PendingAutomaticCardInteraction automatic =
                battle.PendingAutomaticInteraction;
            if (automatic != null)
            {
                return new EffectSourceProjection(
                    automatic.SourceCardId,
                    automatic.OwnerSide,
                    EffectSourceCardKind.Normal,
                    isPersistent: true);
            }

            PendingCardEffect manual = battle.PendingPlayerCardEffect ??
                battle.PendingEnemyCardEffect;
            if (manual != null && battle.ActiveCardEffectActorSide.HasValue)
            {
                return new EffectSourceProjection(
                    manual.SourceCardId,
                    battle.ActiveCardEffectActorSide.Value,
                    EffectSourceCardKind.Normal,
                    isPersistent: true);
            }

            PendingDemonContractInteraction demon =
                battle.PendingPlayerDemonContractInteraction ??
                battle.PendingEnemyDemonContractInteraction;
            if (demon != null)
            {
                CombatantSide ownerSide =
                    ReferenceEquals(
                        demon,
                        battle.PendingPlayerDemonContractInteraction)
                        ? CombatantSide.Player
                        : CombatantSide.Enemy;
                int? sourceCardId = demon.SourceContractCardId;
                PublicCombatAction lastDemonAction = battle.LastPublicAction;
                if (!sourceCardId.HasValue &&
                    lastDemonAction?.ActionType ==
                        PublicCombatActionType.DemonContract &&
                    lastDemonAction.ActorSide == ownerSide)
                {
                    sourceCardId = battle.LastPublicActionSourceCardId;
                }

                if (sourceCardId.HasValue)
                {
                    return new EffectSourceProjection(
                        sourceCardId.Value,
                        ownerSide,
                        EffectSourceCardKind.Demon,
                        isPersistent: true);
                }
            }

            if (battle.LastAutomaticCardResult.HasValue &&
                battle.LastAutomaticCardResultActionOrdinal ==
                    battle.PublicActionHistory.Count)
            {
                AutomaticCardResult result =
                    battle.LastAutomaticCardResult.Value;
                return new EffectSourceProjection(
                    result.SourceCardId,
                    result.OwnerSide,
                    EffectSourceCardKind.Normal,
                    isPersistent: false);
            }

            PublicCombatAction lastAction = battle.LastPublicAction;
            if (lastAction == null ||
                !battle.LastPublicActionSourceCardId.HasValue)
            {
                return null;
            }

            EffectSourceCardKind? cardKind = lastAction.ActionType switch
            {
                PublicCombatActionType.UseCard => EffectSourceCardKind.Normal,
                PublicCombatActionType.DemonContract => EffectSourceCardKind.Demon,
                _ => null,
            };
            return cardKind.HasValue
                ? new EffectSourceProjection(
                    battle.LastPublicActionSourceCardId.Value,
                    lastAction.ActorSide,
                    cardKind.Value,
                    isPersistent: false)
                : (EffectSourceProjection?)null;
        }

        private static bool IsEffectSource(
            EffectSourceProjection? source,
            int cardId,
            CombatantSide ownerSide,
            EffectSourceCardKind cardKind)
        {
            return source.HasValue &&
                source.Value.Matches(cardId, ownerSide, cardKind);
        }

        private static bool IsPersistentEffectSource(
            EffectSourceProjection? source,
            int cardId,
            CombatantSide ownerSide,
            EffectSourceCardKind cardKind)
        {
            return source.HasValue &&
                source.Value.IsPersistent &&
                source.Value.Matches(cardId, ownerSide, cardKind);
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
