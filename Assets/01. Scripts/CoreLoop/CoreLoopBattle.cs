using System;
using System.Collections.Generic;

namespace DiaBlackJack.CoreLoop
{
    internal enum CardEffectApplicationResult
    {
        Pending,
        Completed,
        RoundEnded
    }

    internal enum OwnerBustHandlingResult
    {
        NotHandled,
        Prevented,
        PendingSelection,
        Resumed,
        BattleEnded
    }

    public sealed class CoreLoopBattle
    {
        public const int BasePlayerDemonContractSoulCost = 1;
        public const int BasePlayerDemonContractUseLimit = 1;
        public const int BaseEnemyDemonContractSoulCost = 1;
        public const int BaseEnemyDemonContractUseLimit = 1;

        private static readonly IReadOnlyList<BlackjackCard> NoChangeCandidates =
            Array.AsReadOnly(Array.Empty<BlackjackCard>());

        private readonly IEnemyBehaviorPolicy _enemyPolicy;
        private readonly IAutomaticCardDecisionPolicy
            _enemyAutomaticCardDecisionPolicy;
        private readonly CardEffectResolver _cardEffectResolver;
        private readonly AutomaticCardEffectResolver _automaticCardEffectResolver;
        private readonly DemonContractResolver _demonContractResolver;
        private readonly EnemyChangeCostMode _enemyChangeCostMode;
        private readonly bool _enablesEnemyChange;
        private readonly int _enemyDemonContractCandidateCount;
        private readonly bool _injectsPoisonIntoPlayerDeckEachRound;
        private readonly IReadOnlyList<FixedDemonContractPhaseDefinition>
            _fixedEnemyDemonContractPhases;
        private readonly AutomaticCardBattleState _automaticCardBattleState =
            new AutomaticCardBattleState();
        private readonly DemonContractCardState _demonContractCardState =
            new DemonContractCardState();
        private readonly RoundDamageApplier _damageApplier = new RoundDamageApplier();
        private readonly List<ActiveDemonContract> _activePlayerDemonContracts =
            new List<ActiveDemonContract>();
        private readonly List<ActiveDemonContract> _activeEnemyDemonContracts =
            new List<ActiveDemonContract>();
        private readonly List<PublicCombatAction> _publicActionHistory =
            new List<PublicCombatAction>();
        private int? _lastPublicActionSourceCardId;
        private int _lastAutomaticCardResultActionOrdinal = -1;
        private int _lastSatanForcedDrawActionOrdinal = -1;
        private int _nextAutomaticCardActivationOrdinal = 1;
        private readonly List<int> _injectedPoisonCardIds = new List<int>();
        private CardEffectContext _activeCardEffectContext;
        private CombatantSide? _activeCardEffectActorSide;
        private PendingCardEffect _pendingCardEffect;
        private LeviathanCardEffectSequence _activeLeviathanCardEffectSequence;
        private AutomaticCardEffectContext _activeAutomaticCardEffectContext;
        private AutomaticCardContinuation _automaticCardContinuation;
        private PendingAutomaticCardInteraction _pendingAutomaticCardInteraction;
        private bool _isBeginningAutomaticCardEffect;
        private bool _isResolvingEnemyAutomaticChoice;
        private int _nextAutomaticCardInteractionId = 1;
        private int _enemyDecisionOrdinal;
        private int _nextDemonContractInteractionId = 1;
        private PendingBeelzebubBustResolution _pendingBeelzebubBustResolution;
        private bool _isResolvingEnemyBeelzebubChoice;
        private PendingPaimonExileResolution _pendingPaimonExileResolution;
        private bool _isResolvingEnemyPaimonChoice;
        private BelialForcedCardEffectContinuation
            _belialForcedCardEffectContinuation;
        private AzazelCardEffectSequence _activeAzazelCardEffectSequence;
        private bool _pendingFixedEnemyAzazelActivation;
        private readonly HashSet<int> _resolvedPlayerTurnStartContractIds =
            new HashSet<int>();
        private readonly HashSet<int> _resolvedEnemyTurnStartContractIds =
            new HashSet<int>();
        private readonly HashSet<int> _resolvedPaimonOpponentBustContractIds =
            new HashSet<int>();
        private bool _playerAzazelBustPending;
        private bool _enemyAzazelBustPending;
        private PendingDemonContractInteraction _pendingPlayerDemonContractInteraction;
        private PlayerDemonContractPreview _playerDemonContractPreview;
        private IReadOnlyList<DemonContractCard> _playerDemonContractCandidates;
        private int _playerDemonContractSoulAfterCost;
        private PendingDemonContractInteraction _pendingEnemyDemonContractInteraction;
        private PlayerDemonContractPreview _enemyDemonContractPreview;
        private IReadOnlyList<DemonContractCard> _enemyDemonContractCandidates;
        private int _enemyDemonContractSoulAfterCost;
        private int _playerFinalBonusForEnemyChoice;
        private PlayerChangeSelection _playerChangeSelection;
        private int _nextInjectedPoisonCardId = 1000000000;
        private int _fixedEnemyDemonContractPhaseIndex = -1;
        private ActiveDemonContract _activeFixedEnemyDemonContract;

        private sealed class PendingBeelzebubBustResolution
        {
            public PendingBeelzebubBustResolution(
                ActiveDemonContract activeContract,
                CoreLoopState resumeState,
                Action resume)
            {
                ActiveContract = activeContract ??
                    throw new ArgumentNullException(nameof(activeContract));
                ResumeState = resumeState;
                Resume = resume ?? throw new ArgumentNullException(nameof(resume));
            }

            public ActiveDemonContract ActiveContract { get; }

            public int? OwnerCardId { get; set; }

            public CombatantSide OwnerSide => ActiveContract.OwnerSide;

            public Action Resume { get; }

            public CoreLoopState ResumeState { get; }
        }

        private sealed class PendingPaimonExileResolution
        {
            public PendingPaimonExileResolution(
                ActiveDemonContract activeContract,
                RoundResolution roundResolution)
            {
                ActiveContract = activeContract ??
                    throw new ArgumentNullException(nameof(activeContract));
                RoundResolution = roundResolution;
            }

            public ActiveDemonContract ActiveContract { get; }

            public BattleParticipant ChosenDeckOwner { get; set; }

            public CombatantSide? ChosenDeckSide { get; set; }

            public IReadOnlyList<BlackjackCard> PeekedCards { get; set; }

            public RoundResolution RoundResolution { get; }
        }

        private sealed class BelialForcedCardEffectContinuation
        {
            public BelialForcedCardEffectContinuation(
                CombatantSide ownerSide,
                int sourceContractCardId,
                int transferredCardId)
            {
                OwnerSide = ownerSide;
                SourceContractCardId = sourceContractCardId;
                TransferredCardId = transferredCardId;
            }

            public CombatantSide OwnerSide { get; }

            public int SourceContractCardId { get; }

            public int TransferredCardId { get; }
        }

        private sealed class AzazelCardEffectSequence
        {
            public AzazelCardEffectSequence(
                CombatantSide ownerSide,
                IReadOnlyList<int> cardIds,
                Action complete)
            {
                OwnerSide = ownerSide;
                CardIds = cardIds ??
                    throw new ArgumentNullException(nameof(cardIds));
                Complete = complete ??
                    throw new ArgumentNullException(nameof(complete));
            }

            public IReadOnlyList<int> CardIds { get; }

            public Action Complete { get; }

            public int NextIndex { get; set; }

            public CombatantSide OwnerSide { get; }

        }

        public CoreLoopBattle(
            BlackjackDeck playerDeck,
            BlackjackDeck enemyDeck,
            int playerMaximumSoul = 12,
            int enemyMaximumSoul = 3,
            IEnemyBehaviorPolicy enemyPolicy = null,
            DemonContractDeck playerDemonDeck = null,
            DemonContractDeck enemyDemonDeck = null,
            EnemyChangeCostMode enemyChangeCostMode =
                EnemyChangeCostMode.Accumulating,
            int enemyDemonContractCandidateCount =
                DemonContractDeck.MaximumCandidateCount,
            bool injectsPoisonIntoPlayerDeckEachRound = false,
            bool enablesEnemyChange = false,
            IEnumerable<FixedDemonContractPhaseDefinition>
                fixedEnemyDemonContractPhases = null,
            int? demonContractSeed = null)
            : this(
                playerDeck,
                enemyDeck,
                playerMaximumSoul,
                playerMaximumSoul,
                enemyMaximumSoul,
                enemyPolicy,
                playerDemonDeck,
                enemyDemonDeck,
                enemyChangeCostMode,
                enemyDemonContractCandidateCount,
                injectsPoisonIntoPlayerDeckEachRound,
                enablesEnemyChange,
                fixedEnemyDemonContractPhases,
                demonContractSeed)
        {
        }

        public CoreLoopBattle(
            BlackjackDeck playerDeck,
            BlackjackDeck enemyDeck,
            int playerMaximumSoul,
            int playerCurrentSoul,
            int enemyMaximumSoul,
            IEnemyBehaviorPolicy enemyPolicy = null,
            DemonContractDeck playerDemonDeck = null,
            DemonContractDeck enemyDemonDeck = null,
            EnemyChangeCostMode enemyChangeCostMode =
                EnemyChangeCostMode.Accumulating,
            int enemyDemonContractCandidateCount =
                DemonContractDeck.MaximumCandidateCount,
            bool injectsPoisonIntoPlayerDeckEachRound = false,
            bool enablesEnemyChange = false,
            IEnumerable<FixedDemonContractPhaseDefinition>
                fixedEnemyDemonContractPhases = null,
            int? demonContractSeed = null)
            : this(
                playerDeck,
                enemyDeck,
                playerMaximumSoul,
                playerCurrentSoul,
                enemyMaximumSoul,
                enemyPolicy,
                CardEffectResolver.CreateDefault(),
                playerDemonDeck,
                demonContractResolver: DemonContractResolver.CreateDefault(
                    demonContractSeed ??
                        DemonContractResolver.DefaultDemonDieSeed),
                enemyDemonDeck: enemyDemonDeck,
                enemyAutomaticCardDecisionPolicy:
                    DefaultAutomaticCardDecisionPolicy.Instance,
                enemyChangeCostMode: enemyChangeCostMode,
                enemyDemonContractCandidateCount:
                    enemyDemonContractCandidateCount,
                injectsPoisonIntoPlayerDeckEachRound:
                    injectsPoisonIntoPlayerDeckEachRound,
                enablesEnemyChange: enablesEnemyChange,
                fixedEnemyDemonContractPhases:
                    fixedEnemyDemonContractPhases)
        {
        }

        internal CoreLoopBattle(
            BlackjackDeck playerDeck,
            BlackjackDeck enemyDeck,
            int playerMaximumSoul,
            int playerCurrentSoul,
            int enemyMaximumSoul,
            IEnemyBehaviorPolicy enemyPolicy,
            CardEffectResolver cardEffectResolver,
            DemonContractDeck playerDemonDeck = null,
            DemonContractResolver demonContractResolver = null,
            DemonContractDeck enemyDemonDeck = null,
            AutomaticCardEffectResolver automaticCardEffectResolver = null,
            IAutomaticCardDecisionPolicy enemyAutomaticCardDecisionPolicy = null,
            EnemyChangeCostMode enemyChangeCostMode =
                EnemyChangeCostMode.Accumulating,
            int enemyDemonContractCandidateCount =
                DemonContractDeck.MaximumCandidateCount,
            bool injectsPoisonIntoPlayerDeckEachRound = false,
            bool enablesEnemyChange = false,
            IEnumerable<FixedDemonContractPhaseDefinition>
                fixedEnemyDemonContractPhases = null)
        {
            if (!Enum.IsDefined(typeof(EnemyChangeCostMode), enemyChangeCostMode))
            {
                throw new ArgumentOutOfRangeException(nameof(enemyChangeCostMode));
            }

            if (enemyDemonContractCandidateCount <= 0 ||
                enemyDemonContractCandidateCount >
                    DemonContractDeck.LuciferCandidateCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(enemyDemonContractCandidateCount));
            }

            Player = new BattleParticipant(playerDeck, playerMaximumSoul, playerCurrentSoul);
            Enemy = new BattleParticipant(enemyDeck, enemyMaximumSoul);
            PlayerDemonDeck = playerDemonDeck ??
                new DemonContractDeck(Array.Empty<DemonContractCard>(), seed: 0);
            EnemyDemonDeck = enemyDemonDeck ??
                new DemonContractDeck(Array.Empty<DemonContractCard>(), seed: 0);
            _enemyPolicy = enemyPolicy ?? new SimpleEnemyPolicy();
            _cardEffectResolver = cardEffectResolver ??
                throw new ArgumentNullException(nameof(cardEffectResolver));
            _automaticCardEffectResolver = automaticCardEffectResolver ??
                AutomaticCardEffectResolver.CreateDefault();
            _enemyAutomaticCardDecisionPolicy =
                enemyAutomaticCardDecisionPolicy;
            _demonContractResolver = demonContractResolver ??
                DemonContractResolver.CreateDefault();
            _enemyChangeCostMode = enemyChangeCostMode;
            _enablesEnemyChange = enablesEnemyChange;
            _enemyDemonContractCandidateCount = enemyDemonContractCandidateCount;
            _injectsPoisonIntoPlayerDeckEachRound =
                injectsPoisonIntoPlayerDeckEachRound;
            _fixedEnemyDemonContractPhases =
                FixedDemonContractPhaseDefinition.ValidateAndCopy(
                    fixedEnemyDemonContractPhases,
                    enemyMaximumSoul);
            foreach (FixedDemonContractPhaseDefinition phase in
                _fixedEnemyDemonContractPhases)
            {
                if (!EnemyDemonDeck.ContainsAvailableDefinitionKey(
                        phase.ActiveDefinitionKey) ||
                    !EnemyDemonDeck.ContainsAvailableDefinitionKey(
                        phase.DiscardedDefinitionKey))
                {
                    throw new ArgumentException(
                        "The enemy demon deck does not contain every fixed phase card.",
                        nameof(enemyDemonDeck));
                }
            }
            Player.FaceUpCardAdded += card => HandleFaceUpCardAdded(
                CombatantSide.Player,
                card);
            Enemy.FaceUpCardAdded += card => HandleFaceUpCardAdded(
                CombatantSide.Enemy,
                card);
            State = CoreLoopState.Initializing;
        }

        public BattleParticipant Player { get; }

        public DemonContractDeck PlayerDemonDeck { get; }

        public DemonContractDeck EnemyDemonDeck { get; }

        public BattleParticipant Enemy { get; }

        internal IEnemyBehaviorPolicy EnemyBehaviorPolicy => _enemyPolicy;

        public CoreLoopState State { get; private set; }

        public int RoundNumber { get; private set; }

        /// <summary>
        /// How many individual actions (player or enemy) have concluded within the
        /// current round — one hit, stand, change, card use, etc. by either side is one
        /// turn. Resets to 0 at the start of each round; incremented in
        /// <see cref="NotifyNormalTurnEnded"/>, the single choke point every action's
        /// completion already funnels through regardless of how the action concluded.
        /// </summary>
        public int TurnNumber { get; private set; }

        public RoundResolution? LastResolution { get; private set; }

        /// <summary>
        /// The bonus (currently only a Mammon applied die) folded into each side's total for the
        /// resolution captured in <see cref="LastResolution"/>. A stable snapshot, unlike reading a
        /// contract's own runtime state — that gets reset by NotifyRoundEnded before presentation
        /// ever sees the ResolvingRound step.
        /// </summary>
        public int LastResolutionPlayerBonus { get; private set; }

        public int LastResolutionEnemyBonus { get; private set; }

        public RoundTransition? LastRoundTransition { get; private set; }

        /// <summary>
        /// Raised after each observable sub-step of a turn — player/enemy draw or stand, round
        /// resolution (before the hands are cleared), and a fresh deal. A single player action runs
        /// the enemy to completion synchronously, so a view that only re-reads at the end sees just
        /// the final state; subscribing lets it snapshot the public state at each step and pace the
        /// display. Emits no data — the handler reads public state — so it cannot leak hidden info.
        /// </summary>
        public event Action Stepped;

        public CardEffectResult? LastCardEffectResult { get; private set; }

        public CombatantSide? LastCardEffectActorSide { get; private set; }

        public EnemyDecision LastEnemyDecision { get; private set; }

        public bool CanPlayerAct => State == CoreLoopState.PlayerTurn && !Player.IsStanding;

        public bool CanPlayerStand =>
            CanPlayerAct &&
            _demonContractResolver.CanPlayerStand(
                this,
                _activePlayerDemonContracts);

        public bool CanEnemyStand =>
            State == CoreLoopState.EnemyTurn &&
            !Enemy.IsStanding &&
            _demonContractResolver.CanOwnerStand(
                this,
                _activeEnemyDemonContracts,
                CombatantSide.Enemy);

        public bool CanBeginPlayerChange =>
            CanPlayerAct &&
            _playerChangeSelection == null &&
            Player.Hand.HiddenCardCount == 1 &&
            Player.Deck.CanDraw(2) &&
            Player.Soul.Current > NextPlayerChangeSoulCost;

        internal bool CanBeginEnemyChange =>
            _enablesEnemyChange &&
            State == CoreLoopState.EnemyTurn &&
            !Enemy.IsStanding &&
            _pendingEnemyDemonContractInteraction == null &&
            PendingEnemyCardEffect == null &&
            Enemy.Hand.HiddenCardCount == 1 &&
            Enemy.Deck.CanDraw(2) &&
            Enemy.Soul.Current > NextEnemyChangeSoulCost &&
            ShouldEnemyChange();

        public bool CanSelectChangedCard =>
            State == CoreLoopState.PlayerChoosingChangeCard &&
            _playerChangeSelection != null;

        public int CompletedPlayerChangeCount { get; private set; }

        public int NextPlayerChangeSoulCost => CompletedPlayerChangeCount;

        internal int CompletedEnemyChangeCount { get; private set; }

        internal int NextEnemyChangeSoulCost =>
            _enemyChangeCostMode == EnemyChangeCostMode.FixedOne
                ? 1
                : CompletedEnemyChangeCount;

        public IReadOnlyList<BlackjackCard> PlayerChangeCandidates =>
            _playerChangeSelection?.Candidates ?? NoChangeCandidates;

        public PendingCardEffect PendingPlayerCardEffect =>
            _activeCardEffectActorSide == CombatantSide.Player
                ? _pendingCardEffect
                : null;

        public PendingAutomaticCardInteraction PendingPlayerAutomaticInteraction =>
            _pendingAutomaticCardInteraction?.DecisionSide ==
                CombatantSide.Player
                    ? _pendingAutomaticCardInteraction
                    : null;

        internal PendingAutomaticCardInteraction PendingAutomaticInteraction =>
            _pendingAutomaticCardInteraction;

        internal int LastAutomaticCardResultActionOrdinal =>
            _lastAutomaticCardResultActionOrdinal;

        internal int LastSatanForcedDrawActionOrdinal =>
            _lastSatanForcedDrawActionOrdinal;

        internal int LastAutomaticCardActivationOrdinal { get; private set; }

        internal int LastAutomaticCardActivationRoundNumber { get; private set; }

        internal CombatantSide? LastAutomaticCardActivationOwnerSide
        {
            get;
            private set;
        }

        internal string LastAutomaticCardActivationDefinitionKey
        {
            get;
            private set;
        }

        internal int? LastPublicActionSourceCardId =>
            _lastPublicActionSourceCardId;

        internal int PendingPoisonWinRewardCount =>
            _automaticCardBattleState.PendingPoisonWinRewardCount;

        internal int InjectedPoisonCardCount => _injectedPoisonCardIds.Count;

        public AutomaticCardResult? LastAutomaticCardResult { get; private set; }

        public AutomaticCardDecision? LastEnemyAutomaticCardDecision
        {
            get;
            private set;
        }

        public LieDetectorPublicResult? LastLieDetectorPublicResult
        {
            get;
            private set;
        }

        public HiddenCardComparisonKnowledge?
            PlayerHiddenCardComparisonKnowledge =>
                _automaticCardBattleState.GetHiddenCardKnowledge(
                    CombatantSide.Player);

        internal HiddenCardComparisonKnowledge?
            EnemyHiddenCardComparisonKnowledge =>
                _automaticCardBattleState.GetHiddenCardKnowledge(
                    CombatantSide.Enemy);

        public IReadOnlyList<ActiveDemonContract> ActivePlayerDemonContracts =>
            _activePlayerDemonContracts.AsReadOnly();

        public IReadOnlyList<ActiveDemonContract> ActiveEnemyDemonContracts =>
            _activeEnemyDemonContracts.AsReadOnly();

        public int UsedPlayerBaseDemonContractCount { get; private set; }

        public int UsedEnemyBaseDemonContractCount { get; private set; }

        public int FixedEnemyDemonContractPhaseNumber =>
            _fixedEnemyDemonContractPhaseIndex + 1;

        public DemonContractResult LastDemonContractResult { get; private set; }

        public DemonContractEffectResult LastDemonContractEffectResult { get; private set; }

        public LeviathanCardEffectResult LastLeviathanCardEffectResult { get; private set; }

        internal bool HasPendingLeviathanAutoPistolRetry =>
            _activeLeviathanCardEffectSequence != null &&
            _pendingCardEffect != null &&
            _pendingCardEffect.EffectKind == CardEffectKind.AutoPistol &&
            _pendingCardEffect.SourceCardId ==
                _activeLeviathanCardEffectSequence.SourceCardId;

        public PendingDemonContractInteraction PendingPlayerDemonContractInteraction =>
            _pendingPlayerDemonContractInteraction;

        public PlayerDemonContractPreview PlayerDemonContractPreview =>
            _playerDemonContractPreview;

        public PendingDemonContractInteraction PendingEnemyDemonContractInteraction =>
            _pendingEnemyDemonContractInteraction;

        internal PlayerDemonContractPreview EnemyDemonContractPreview =>
            _enemyDemonContractPreview;

        internal bool PendingEnemyPaimonSelectedOwnerDeck =>
            _pendingPaimonExileResolution?.ActiveContract.OwnerSide ==
                CombatantSide.Enemy &&
            _pendingPaimonExileResolution.ChosenDeckSide ==
                CombatantSide.Enemy;

        internal int PaimonExileCount =>
            _demonContractCardState.PaimonExileCount;

        internal int BelialTransferCount =>
            _demonContractCardState.BelialTransferCount;

        internal int BaphometPentagramCount =>
            _demonContractCardState.BaphometPentagramCount;

        internal int BaphometWaveCount =>
            _demonContractCardState.BaphometWaveCount;

        public DemonContractAvailability PlayerDemonContractAvailability
        {
            get
            {
                int remainingBaseUses = Math.Max(
                    0,
                    BasePlayerDemonContractUseLimit - UsedPlayerBaseDemonContractCount);
                int soulAfterCost = Math.Max(
                    0,
                    Player.Soul.Current - BasePlayerDemonContractSoulCost);
                return new DemonContractAvailability(
                    EvaluatePlayerDemonContractFailureReason(),
                    BasePlayerDemonContractSoulCost,
                    soulAfterCost,
                    remainingBaseUses);
            }
        }

        public DemonContractAvailability EnemyDemonContractAvailability
        {
            get
            {
                int remainingBaseUses = Math.Max(
                    0,
                    BaseEnemyDemonContractUseLimit - UsedEnemyBaseDemonContractCount);
                int soulAfterCost = Math.Max(
                    0,
                    Enemy.Soul.Current - BaseEnemyDemonContractSoulCost);
                return new DemonContractAvailability(
                    EvaluateEnemyDemonContractFailureReason(),
                    BaseEnemyDemonContractSoulCost,
                    soulAfterCost,
                    remainingBaseUses);
            }
        }

        public IReadOnlyList<CardUseAvailability> PlayerCardUseAvailability
        {
            get
            {
                var availability = new List<CardUseAvailability>(Player.Hand.Count);
                foreach (BlackjackCard card in Player.Hand.Cards)
                {
                    availability.Add(EvaluatePlayerCardUse(card.Id));
                }

                return availability.AsReadOnly();
            }
        }

        public BattleOutcome Outcome
        {
            get
            {
                if (State != CoreLoopState.BattleEnded)
                {
                    return BattleOutcome.InProgress;
                }

                return Enemy.Soul.IsDepleted
                    ? BattleOutcome.PlayerVictory
                    : BattleOutcome.PlayerDefeat;
            }
        }

        public bool Start()
        {
            if (State != CoreLoopState.Initializing)
            {
                return false;
            }

            AdvanceFixedEnemyDemonContractPhases();
            StartRound();
            return true;
        }

        public bool CanUsePlayerCard(int cardId)
        {
            return EvaluatePlayerCardUse(cardId).CanUse;
        }

        public bool TryBeginPlayerCardUse(int cardId)
        {
            return TryBeginCardUse(CombatantSide.Player, cardId);
        }

        public bool TryResolvePlayerCardChoice(int optionId)
        {
            return TryResolveCardChoice(CombatantSide.Player, optionId);
        }

        public bool TryResolvePlayerAutomaticCardChoice(
            int interactionId,
            int optionId)
        {
            return TryResolveAutomaticCardChoice(
                CombatantSide.Player,
                interactionId,
                optionId);
        }

        public bool TryBeginPlayerDemonContract()
        {
            DemonContractAvailability availability = PlayerDemonContractAvailability;
            if (!availability.CanBegin)
            {
                return false;
            }

            Player.Soul.ApplyDamage(availability.SoulCost);
            UsedPlayerBaseDemonContractCount = checked(
                UsedPlayerBaseDemonContractCount + 1);
            _playerDemonContractSoulAfterCost = Player.Soul.Current;

            IReadOnlyList<DemonContractCard> candidates =
                PlayerDemonDeck.TakeCandidates();
            if (candidates.Count == 0 ||
                candidates.Count > DemonContractDeck.MaximumCandidateCount)
            {
                throw new InvalidOperationException(
                    "Validated demon contract deck returned an invalid candidate count.");
            }

            int interactionId = TakeNextDemonContractInteractionId();
            _playerDemonContractCandidates = candidates;
            _pendingPlayerDemonContractInteraction = CreateContractChoiceInteraction(
                interactionId,
                candidates);
            State = CoreLoopState.PlayerResolvingDemonContract;
            RaiseStepped();
            return true;
        }

        public bool TryResolvePlayerDemonContract(int interactionId, int optionId)
        {
            PendingDemonContractInteraction pending =
                _pendingPlayerDemonContractInteraction;
            if (State != CoreLoopState.PlayerResolvingDemonContract ||
                pending == null ||
                pending.InteractionId != interactionId ||
                !pending.TryGetOption(optionId, out DemonContractOption selectedOption))
            {
                return false;
            }

            switch (pending.Kind)
            {
                case DemonContractInteractionKind.ChooseContract:
                    return TryResolveContractChoice(pending, selectedOption);
                case DemonContractInteractionKind.LuciferChooseAdditionalContract:
                    return TryResolvePlayerLuciferContractChoice(
                        pending,
                        selectedOption);
                case DemonContractInteractionKind.BelphegorTopCard:
                    return TryResolveBelphegorTopCard(pending, selectedOption);
                case DemonContractInteractionKind.MammonReroll:
                    return TryResolveMammonTurnStartChoice(
                        CombatantSide.Player,
                        pending,
                        selectedOption,
                        out _);
                case DemonContractInteractionKind.MammonApplyDie:
                    return TryResolveMammonFinalChoice(pending, selectedOption);
                case DemonContractInteractionKind.SatanDeclareFirstNumber:
                case DemonContractInteractionKind.SatanDeclareSecondNumber:
                    return TryResolveSatanNumberChoice(
                        CombatantSide.Player,
                        pending,
                        selectedOption,
                        out _);
                case DemonContractInteractionKind.BeelzebubChooseOwnerCard:
                case DemonContractInteractionKind.BeelzebubChooseOpponentCard:
                    return TryResolveBeelzebubDiscardChoice(
                        CombatantSide.Player,
                        pending,
                        selectedOption);
                case DemonContractInteractionKind.AsmodeusForceOpponentHit:
                    return TryResolveAsmodeusTurnStartChoice(
                        CombatantSide.Player,
                        pending,
                        selectedOption);
                case DemonContractInteractionKind.PaimonChooseDeck:
                case DemonContractInteractionKind.PaimonChooseExileCard:
                    return TryResolvePaimonExileChoice(
                        CombatantSide.Player,
                        pending,
                        selectedOption);
                case DemonContractInteractionKind.BelialChooseOpponentCard:
                    return TryResolveBelialTurnStartChoice(
                        CombatantSide.Player,
                        pending,
                        selectedOption);
                default:
                    throw new ArgumentOutOfRangeException(nameof(pending));
            }
        }

        public bool TryResolvePlayerSatanNumbers(
            int interactionId,
            int firstNumber,
            int secondNumber)
        {
            PendingDemonContractInteraction pending =
                _pendingPlayerDemonContractInteraction;
            if (State != CoreLoopState.PlayerResolvingDemonContract ||
                pending == null ||
                pending.InteractionId != interactionId ||
                pending.Kind !=
                    DemonContractInteractionKind.SatanDeclareFirstNumber ||
                firstNumber == secondNumber ||
                !ContainsSatanNumberOption(pending, firstNumber) ||
                !ContainsSatanNumberOption(pending, secondNumber) ||
                !TryGetSatanUpperResolutionContext(
                    CombatantSide.Player,
                    pending,
                    out SatanRuntimeState satanState,
                    out BlackjackCard hiddenCard))
            {
                return false;
            }

            return ResolveSatanNumberPair(
                CombatantSide.Player,
                satanState,
                hiddenCard,
                firstNumber,
                secondNumber,
                out _);
        }

        private bool TryBeginEnemyDemonContract()
        {
            DemonContractAvailability availability = EnemyDemonContractAvailability;
            if (!availability.CanBegin)
            {
                return false;
            }

            ApplySoulDamage(CombatantSide.Enemy, availability.SoulCost);
            _enemyDemonContractSoulAfterCost = Enemy.Soul.Current;
            _enemyDemonContractCandidates = EnemyDemonDeck.TakeCandidates(
                _enemyDemonContractCandidateCount);
            if (_enemyDemonContractCandidates.Count == 0 ||
                _enemyDemonContractCandidates.Count >
                    _enemyDemonContractCandidateCount)
            {
                throw new InvalidOperationException(
                    "Validated enemy demon contract deck returned an invalid candidate count.");
            }

            int interactionId = TakeNextDemonContractInteractionId();
            _pendingEnemyDemonContractInteraction = CreateContractChoiceInteraction(
                interactionId,
                _enemyDemonContractCandidates);
            RaiseStepped();
            return true;
        }

        private bool TryResolveEnemyDemonContract(
            int optionId,
            out bool completedOwnerAction)
        {
            completedOwnerAction = false;
            PendingDemonContractInteraction pending =
                _pendingEnemyDemonContractInteraction;
            if (State != CoreLoopState.EnemyTurn ||
                pending == null ||
                !pending.TryGetOption(optionId, out DemonContractOption selectedOption))
            {
                return false;
            }

            switch (pending.Kind)
            {
                case DemonContractInteractionKind.ChooseContract:
                    completedOwnerAction = true;
                    return TryResolveEnemyContractChoice(pending, selectedOption);
                case DemonContractInteractionKind.LuciferChooseAdditionalContract:
                    bool resolvedLucifer = TryResolveEnemyLuciferContractChoice(
                        pending,
                        selectedOption);
                    completedOwnerAction = resolvedLucifer &&
                        _pendingEnemyDemonContractInteraction == null;
                    return resolvedLucifer;
                case DemonContractInteractionKind.BelphegorTopCard:
                    completedOwnerAction = true;
                    return TryResolveEnemyBelphegorTopCard(pending, selectedOption);
                case DemonContractInteractionKind.MammonReroll:
                    return TryResolveMammonTurnStartChoice(
                        CombatantSide.Enemy,
                        pending,
                        selectedOption,
                        out completedOwnerAction);
                case DemonContractInteractionKind.MammonApplyDie:
                    return TryResolveEnemyMammonFinalChoice(pending, selectedOption);
                case DemonContractInteractionKind.SatanDeclareFirstNumber:
                case DemonContractInteractionKind.SatanDeclareSecondNumber:
                    bool resolved = TryResolveSatanNumberChoice(
                        CombatantSide.Enemy,
                        pending,
                        selectedOption,
                        out bool completedSatanAction);
                    completedOwnerAction = completedSatanAction;
                    return resolved;
                case DemonContractInteractionKind.BeelzebubChooseOwnerCard:
                case DemonContractInteractionKind.BeelzebubChooseOpponentCard:
                    return TryResolveBeelzebubDiscardChoice(
                        CombatantSide.Enemy,
                        pending,
                        selectedOption);
                case DemonContractInteractionKind.AsmodeusForceOpponentHit:
                    return TryResolveAsmodeusTurnStartChoice(
                        CombatantSide.Enemy,
                        pending,
                        selectedOption);
                case DemonContractInteractionKind.PaimonChooseDeck:
                case DemonContractInteractionKind.PaimonChooseExileCard:
                    return TryResolvePaimonExileChoice(
                        CombatantSide.Enemy,
                        pending,
                        selectedOption);
                case DemonContractInteractionKind.BelialChooseOpponentCard:
                    return TryResolveBelialTurnStartChoice(
                        CombatantSide.Enemy,
                        pending,
                        selectedOption);
                default:
                    throw new ArgumentOutOfRangeException(nameof(pending));
            }
        }

        private bool TryResolveEnemyContractChoice(
            PendingDemonContractInteraction pending,
            DemonContractOption selectedOption)
        {
            if (_enemyDemonContractCandidates == null ||
                !selectedOption.ContractCardId.HasValue)
            {
                return false;
            }

            DemonContractCard selectedCard = null;
            var discardedCards = new List<DemonContractCard>(
                Math.Max(0, _enemyDemonContractCandidates.Count - 1));
            foreach (DemonContractCard candidate in _enemyDemonContractCandidates)
            {
                if (candidate.Id == selectedOption.ContractCardId.Value)
                {
                    selectedCard = candidate;
                }
                else
                {
                    discardedCards.Add(candidate);
                }
            }

            if (selectedCard == null ||
                discardedCards.Count != _enemyDemonContractCandidates.Count - 1)
            {
                return false;
            }

            EnemyDemonDeck.Discard(discardedCards);
            var activeContract = new ActiveDemonContract(
                selectedCard,
                CombatantSide.Enemy,
                new EmptyDemonContractRuntimeState());
            _activeEnemyDemonContracts.Add(activeContract);
            _enemyDemonContractCandidates = null;
            ClearEnemyDemonContractInteraction();
            int enemySoulBeforeActivation = Enemy.Soul.Current;
            activeContract.SetRuntimeState(
                _demonContractResolver.Activate(this, activeContract));
            RecordDemonContractActivationSoulCost(
                enemySoulBeforeActivation,
                Enemy.Soul.Current);
            UsedEnemyBaseDemonContractCount = checked(
                UsedEnemyBaseDemonContractCount + 1);
            RecordPublicAction(
                CombatantSide.Enemy,
                PublicCombatActionType.DemonContract,
                activeContract.Definition.Key,
                activeContract.SourceCardId);

            bool enemyDepleted = Enemy.Soul.IsDepleted;
            LastDemonContractResult = new DemonContractResult(
                pending.InteractionId,
                activeContract,
                BaseEnemyDemonContractSoulCost,
                _enemyDemonContractSoulAfterCost,
                Enemy.Soul.Current,
                endedBattle: enemyDepleted);

            if (activeContract.RuntimeState is MammonRuntimeState mammonState &&
                mammonState.CurrentDieValue == 6)
            {
                OwnerBustHandlingResult handling = HandleEnemyBust(() =>
                {
                    State = CoreLoopState.EnemyTurn;
                    RaiseStepped();
                });
                if (handling == OwnerBustHandlingResult.NotHandled)
                {
                    LastDemonContractEffectResult = new DemonContractEffectResult(
                        triggered: true,
                        bustedTarget: CombatantSide.Enemy,
                        paidSoulCost: 0);
                    NotifyNormalTurnEnded(CombatantSide.Enemy);
                    CompleteRound(RoundResolver.ResolveContractEffectBust(
                        RoundNumber,
                        playerIsTarget: false));
                    return true;
                }

                if (handling != OwnerBustHandlingResult.Prevented)
                {
                    return true;
                }
            }

            if (State == CoreLoopState.BattleEnded)
            {
                return true;
            }

            if (enemyDepleted)
            {
                EndBattleWithoutRound();
                return true;
            }

            if (TryBeginAzazelCardEffectSequence(
                    activeContract,
                    CompleteEnemyActionAfterAzazelSequence))
            {
                return true;
            }

            if (TryBeginLuciferAdditionalContractChoice(activeContract))
            {
                return true;
            }

            State = CoreLoopState.EnemyTurn;
            RaiseStepped();
            return true;
        }

        private bool TryBeginEnemyBelphegorTopCardPreview(
            ActiveDemonContract previewContract)
        {
            if (previewContract == null ||
                previewContract.Kind != DemonContractKind.Belphegor ||
                !Enemy.Deck.TryPeekTop(out BlackjackCard previewCard))
            {
                return false;
            }

            int interactionId = TakeNextDemonContractInteractionId();
            _pendingEnemyDemonContractInteraction =
                CreateBelphegorTopCardInteraction(
                    interactionId,
                    previewContract.SourceCardId);
            _enemyDemonContractPreview = new PlayerDemonContractPreview(
                interactionId,
                previewContract.SourceCardId,
                DemonContractKind.Belphegor,
                previewCard);
            RaiseStepped();
            return true;
        }

        private bool TryResolveEnemyBelphegorTopCard(
            PendingDemonContractInteraction pending,
            DemonContractOption selectedOption)
        {
            PlayerDemonContractPreview preview = _enemyDemonContractPreview;
            if (pending.ContractKind != DemonContractKind.Belphegor ||
                preview == null ||
                preview.InteractionId != pending.InteractionId ||
                preview.ContractKind != DemonContractKind.Belphegor ||
                !TryGetPendingEnemyActiveContract(
                    pending,
                    DemonContractKind.Belphegor,
                    out _))
            {
                return false;
            }

            switch (selectedOption.OptionId)
            {
                case BelphegorDemonContractHandler.KeepTopCardOptionId:
                    ClearEnemyDemonContractInteraction();
                    RecordPublicAction(CombatantSide.Enemy, PublicCombatActionType.Hit);
                    BlackjackCard drawnCard = Enemy.Draw(faceUp: true);
                    if (drawnCard.Id != preview.CardId)
                    {
                        throw new InvalidOperationException(
                            "Enemy card drawn after a demon preview did not match the preview.");
                    }

                    RaiseStepped();
                    if (ConsumePendingAzazelBust(CombatantSide.Enemy))
                    {
                        OwnerBustHandlingResult azazelHandling = HandleEnemyBust(() =>
                        {
                            State = CoreLoopState.EnemyTurn;
                            CompleteEnemyHitAction();
                            RaiseStepped();
                        });
                        if (azazelHandling == OwnerBustHandlingResult.NotHandled)
                        {
                            LastDemonContractEffectResult =
                                new DemonContractEffectResult(
                                    triggered: true,
                                    bustedTarget: CombatantSide.Enemy,
                                    paidSoulCost: 0);
                            NotifyNormalTurnEnded(CombatantSide.Enemy);
                            CompleteRound(RoundResolver.ResolveContractEffectBust(
                                RoundNumber,
                                playerIsTarget: false));
                            return true;
                        }

                        if (azazelHandling != OwnerBustHandlingResult.Prevented)
                        {
                            return true;
                        }

                        LastDemonContractEffectResult =
                            new DemonContractEffectResult(
                                triggered: true,
                                bustedTarget: null,
                                paidSoulCost: 0);
                        RaiseStepped();
                    }

                    if (Enemy.VisibleHandValue.IsBust)
                    {
                        OwnerBustHandlingResult handling = HandleEnemyBust(() =>
                        {
                            State = CoreLoopState.EnemyTurn;
                            CompleteEnemyHitAction();
                            RaiseStepped();
                        });
                        if (handling == OwnerBustHandlingResult.NotHandled)
                        {
                            NotifyNormalTurnEnded(CombatantSide.Enemy);
                            CompleteRound(RoundResolver.ResolveNumericBust(
                                RoundNumber,
                                playerIsTarget: false));
                            return true;
                        }

                        if (handling != OwnerBustHandlingResult.Prevented)
                        {
                            return true;
                        }
                    }

                    CompleteEnemyHitAction();
                    return true;

                case BelphegorDemonContractHandler.MoveTopCardToBottomOptionId:
                    if (!Enemy.Deck.TryMoveTopToBottom(preview.CardId))
                    {
                        return false;
                    }

                    ClearEnemyDemonContractInteraction();
                    RaiseStepped();
                    return true;

                default:
                    return false;
            }
        }

        private bool TryResolveEnemyMammonFinalChoice(
            PendingDemonContractInteraction pending,
            DemonContractOption selectedOption)
        {
            if (!TryGetPendingEnemyActiveContract(
                pending,
                DemonContractKind.Mammon,
                out ActiveDemonContract activeContract))
            {
                return false;
            }

            int enemyBonus = _demonContractResolver.ResolveOwnerFinalChoice(
                this,
                activeContract,
                selectedOption.OptionId);
            ClearEnemyDemonContractInteraction();
            int playerBonus = _playerFinalBonusForEnemyChoice;
            _playerFinalBonusForEnemyChoice = 0;
            ResolveRoundWithBonuses(playerBonus, enemyBonus);
            return true;
        }

        private bool TryResolveContractChoice(
            PendingDemonContractInteraction pending,
            DemonContractOption selectedOption)
        {
            if (_playerDemonContractCandidates == null ||
                !selectedOption.ContractCardId.HasValue)
            {
                return false;
            }

            DemonContractCard selectedCard = null;
            var discardedCards = new List<DemonContractCard>(
                Math.Max(0, _playerDemonContractCandidates.Count - 1));
            foreach (DemonContractCard candidate in _playerDemonContractCandidates)
            {
                if (candidate.Id == selectedOption.ContractCardId.Value)
                {
                    selectedCard = candidate;
                }
                else
                {
                    discardedCards.Add(candidate);
                }
            }

            if (selectedCard == null ||
                discardedCards.Count != _playerDemonContractCandidates.Count - 1)
            {
                return false;
            }

            PlayerDemonDeck.Discard(discardedCards);
            var activeContract = new ActiveDemonContract(
                selectedCard,
                CombatantSide.Player,
                new EmptyDemonContractRuntimeState());
            _activePlayerDemonContracts.Add(activeContract);
            _pendingPlayerDemonContractInteraction = null;
            _playerDemonContractCandidates = null;

            int playerSoulBeforeActivation = Player.Soul.Current;
            activeContract.SetRuntimeState(
                _demonContractResolver.Activate(this, activeContract));
            RecordDemonContractActivationSoulCost(
                playerSoulBeforeActivation,
                Player.Soul.Current);
            RecordPublicAction(
                CombatantSide.Player,
                PublicCombatActionType.DemonContract,
                activeContract.Definition.Key,
                activeContract.SourceCardId);
            bool playerDepleted = Player.Soul.IsDepleted;
            LastDemonContractResult = new DemonContractResult(
                pending.InteractionId,
                activeContract,
                BasePlayerDemonContractSoulCost,
                _playerDemonContractSoulAfterCost,
                Player.Soul.Current,
                endedBattle: playerDepleted);

            if (activeContract.RuntimeState is MammonRuntimeState mammonState &&
                mammonState.CurrentDieValue == 6)
            {
                OwnerBustHandlingResult handling = HandlePlayerBust(() =>
                {
                    State = CoreLoopState.PlayerTurn;
                    RaiseStepped();
                    CompletePlayerActionAndRunEnemyTurn();
                });
                if (handling == OwnerBustHandlingResult.NotHandled)
                {
                    LastDemonContractEffectResult = new DemonContractEffectResult(
                        triggered: true,
                        bustedTarget: CombatantSide.Player,
                        paidSoulCost: 0);
                    NotifyNormalTurnEnded(CombatantSide.Player);
                    CompleteRound(RoundResolver.ResolveContractEffectBust(
                        RoundNumber,
                        playerIsTarget: true));
                    return true;
                }

                if (handling != OwnerBustHandlingResult.Prevented)
                {
                    return true;
                }
            }

            if (State == CoreLoopState.BattleEnded)
            {
                return true;
            }

            if (playerDepleted)
            {
                EndBattleWithoutRound();
                return true;
            }

            if (TryBeginAzazelCardEffectSequence(
                    activeContract,
                    CompletePlayerActionAfterAzazelSequence))
            {
                return true;
            }

            if (TryBeginLuciferAdditionalContractChoice(activeContract))
            {
                return true;
            }

            State = CoreLoopState.PlayerTurn;
            RaiseStepped();
            CompletePlayerActionAndRunEnemyTurn();
            return true;
        }

        private bool TryBeginLuciferAdditionalContractChoice(
            ActiveDemonContract luciferContract)
        {
            if (luciferContract == null ||
                luciferContract.Kind != DemonContractKind.Lucifer ||
                !(luciferContract.RuntimeState is LuciferRuntimeState) ||
                GetParticipant(luciferContract.OwnerSide).Soul.IsDepleted)
            {
                return false;
            }

            DemonContractDeck deck = luciferContract.OwnerSide ==
                CombatantSide.Player
                    ? PlayerDemonDeck
                    : EnemyDemonDeck;
            if (!deck.CanTakeCandidates)
            {
                return false;
            }

            IReadOnlyList<DemonContractCard> candidates =
                deck.TakeLuciferCandidates();
            if (candidates.Count == 0 ||
                candidates.Count > DemonContractDeck.LuciferCandidateCount)
            {
                throw new InvalidOperationException(
                    "Validated Lucifer deck returned an invalid candidate count.");
            }

            PendingDemonContractInteraction interaction =
                CreateLuciferContractChoiceInteraction(
                    TakeNextDemonContractInteractionId(),
                    luciferContract,
                    candidates);
            if (luciferContract.OwnerSide == CombatantSide.Player)
            {
                _playerDemonContractCandidates = candidates;
                _pendingPlayerDemonContractInteraction = interaction;
                State = CoreLoopState.PlayerResolvingDemonContract;
            }
            else
            {
                _enemyDemonContractCandidates = candidates;
                _pendingEnemyDemonContractInteraction = interaction;
            }

            RaiseStepped();
            return true;
        }

        private bool TryResolvePlayerLuciferContractChoice(
            PendingDemonContractInteraction pending,
            DemonContractOption selectedOption)
        {
            if (!IsValidLuciferChoice(
                    CombatantSide.Player,
                    pending,
                    _playerDemonContractCandidates))
            {
                return false;
            }

            if (selectedOption.OptionId ==
                LuciferDemonContractHandler.SkipAdditionalContractOptionId)
            {
                PlayerDemonDeck.Discard(_playerDemonContractCandidates);
                ClearPlayerDemonContractInteraction();
                State = CoreLoopState.PlayerTurn;
                RaiseStepped();
                CompletePlayerActionAndRunEnemyTurn();
                return true;
            }

            if (!TryPartitionDemonContractCandidates(
                    _playerDemonContractCandidates,
                    selectedOption,
                    out DemonContractCard selectedCard,
                    out IReadOnlyList<DemonContractCard> discardedCards))
            {
                return false;
            }

            PlayerDemonDeck.Discard(discardedCards);
            var activeContract = new ActiveDemonContract(
                selectedCard,
                CombatantSide.Player,
                new EmptyDemonContractRuntimeState());
            _activePlayerDemonContracts.Add(activeContract);
            ClearPlayerDemonContractInteraction();

            int playerSoulBeforeActivation = Player.Soul.Current;
            activeContract.SetRuntimeState(
                _demonContractResolver.Activate(this, activeContract));
            RecordDemonContractActivationSoulCost(
                playerSoulBeforeActivation,
                Player.Soul.Current);

            if (activeContract.RuntimeState is MammonRuntimeState mammonState &&
                mammonState.CurrentDieValue == 6)
            {
                OwnerBustHandlingResult handling = HandlePlayerBust(() =>
                {
                    State = CoreLoopState.PlayerTurn;
                    RaiseStepped();
                    CompletePlayerActionAndRunEnemyTurn();
                });
                if (handling == OwnerBustHandlingResult.NotHandled)
                {
                    LastDemonContractEffectResult = new DemonContractEffectResult(
                        triggered: true,
                        bustedTarget: CombatantSide.Player,
                        paidSoulCost: 0);
                    NotifyNormalTurnEnded(CombatantSide.Player);
                    CompleteRound(RoundResolver.ResolveContractEffectBust(
                        RoundNumber,
                        playerIsTarget: true));
                    return true;
                }

                if (handling != OwnerBustHandlingResult.Prevented)
                {
                    return true;
                }
            }

            if (State == CoreLoopState.BattleEnded)
            {
                return true;
            }

            if (Player.Soul.IsDepleted)
            {
                EndBattleWithoutRound();
                return true;
            }

            if (TryBeginAzazelCardEffectSequence(
                    activeContract,
                    CompletePlayerActionAfterAzazelSequence))
            {
                return true;
            }

            if (TryBeginLuciferAdditionalContractChoice(activeContract))
            {
                return true;
            }

            State = CoreLoopState.PlayerTurn;
            RaiseStepped();
            CompletePlayerActionAndRunEnemyTurn();
            return true;
        }

        private bool TryResolveEnemyLuciferContractChoice(
            PendingDemonContractInteraction pending,
            DemonContractOption selectedOption)
        {
            if (!IsValidLuciferChoice(
                    CombatantSide.Enemy,
                    pending,
                    _enemyDemonContractCandidates))
            {
                return false;
            }

            if (selectedOption.OptionId ==
                LuciferDemonContractHandler.SkipAdditionalContractOptionId)
            {
                EnemyDemonDeck.Discard(_enemyDemonContractCandidates);
                _enemyDemonContractCandidates = null;
                ClearEnemyDemonContractInteraction();
                RaiseStepped();
                return true;
            }

            if (!TryPartitionDemonContractCandidates(
                    _enemyDemonContractCandidates,
                    selectedOption,
                    out DemonContractCard selectedCard,
                    out IReadOnlyList<DemonContractCard> discardedCards))
            {
                return false;
            }

            EnemyDemonDeck.Discard(discardedCards);
            var activeContract = new ActiveDemonContract(
                selectedCard,
                CombatantSide.Enemy,
                new EmptyDemonContractRuntimeState());
            _activeEnemyDemonContracts.Add(activeContract);
            _enemyDemonContractCandidates = null;
            ClearEnemyDemonContractInteraction();

            int enemySoulBeforeActivation = Enemy.Soul.Current;
            activeContract.SetRuntimeState(
                _demonContractResolver.Activate(this, activeContract));
            RecordDemonContractActivationSoulCost(
                enemySoulBeforeActivation,
                Enemy.Soul.Current);
            RecordPublicAction(
                CombatantSide.Enemy,
                PublicCombatActionType.DemonContract,
                activeContract.Definition.Key,
                activeContract.SourceCardId);

            if (activeContract.RuntimeState is MammonRuntimeState mammonState &&
                mammonState.CurrentDieValue == 6)
            {
                OwnerBustHandlingResult handling = HandleEnemyBust(() =>
                {
                    State = CoreLoopState.EnemyTurn;
                    RaiseStepped();
                });
                if (handling == OwnerBustHandlingResult.NotHandled)
                {
                    LastDemonContractEffectResult = new DemonContractEffectResult(
                        triggered: true,
                        bustedTarget: CombatantSide.Enemy,
                        paidSoulCost: 0);
                    NotifyNormalTurnEnded(CombatantSide.Enemy);
                    CompleteRound(RoundResolver.ResolveContractEffectBust(
                        RoundNumber,
                        playerIsTarget: false));
                    return true;
                }

                if (handling != OwnerBustHandlingResult.Prevented)
                {
                    return true;
                }
            }

            if (State == CoreLoopState.BattleEnded)
            {
                return true;
            }

            if (Enemy.Soul.IsDepleted)
            {
                EndBattleWithoutRound();
                return true;
            }

            if (TryBeginAzazelCardEffectSequence(
                    activeContract,
                    CompleteEnemyActionAfterAzazelSequence))
            {
                return true;
            }

            if (TryBeginLuciferAdditionalContractChoice(activeContract))
            {
                return true;
            }

            State = CoreLoopState.EnemyTurn;
            RaiseStepped();
            return true;
        }

        private bool IsValidLuciferChoice(
            CombatantSide ownerSide,
            PendingDemonContractInteraction pending,
            IReadOnlyList<DemonContractCard> candidates)
        {
            return pending.Kind ==
                    DemonContractInteractionKind.LuciferChooseAdditionalContract &&
                pending.ContractKind == DemonContractKind.Lucifer &&
                pending.SourceContractCardId.HasValue &&
                candidates != null &&
                TryGetActiveDemonContract(
                    ownerSide,
                    pending.SourceContractCardId.Value,
                    DemonContractKind.Lucifer,
                    out ActiveDemonContract sourceContract) &&
                sourceContract.RuntimeState is LuciferRuntimeState;
        }

        private static bool TryPartitionDemonContractCandidates(
            IReadOnlyList<DemonContractCard> candidates,
            DemonContractOption selectedOption,
            out DemonContractCard selectedCard,
            out IReadOnlyList<DemonContractCard> discardedCards)
        {
            selectedCard = null;
            var discarded = new List<DemonContractCard>(
                Math.Max(0, candidates.Count - 1));
            if (!selectedOption.ContractCardId.HasValue)
            {
                discardedCards = discarded.AsReadOnly();
                return false;
            }

            foreach (DemonContractCard candidate in candidates)
            {
                if (candidate.Id == selectedOption.ContractCardId.Value)
                {
                    selectedCard = candidate;
                }
                else
                {
                    discarded.Add(candidate);
                }
            }

            discardedCards = discarded.AsReadOnly();
            return selectedCard != null && discarded.Count == candidates.Count - 1;
        }

        private void RecordDemonContractActivationSoulCost(
            int ownerSoulBeforeActivation,
            int ownerSoulAfterActivation)
        {
            int paidSoulCost = ownerSoulBeforeActivation -
                ownerSoulAfterActivation;
            if (paidSoulCost <= 0)
            {
                return;
            }

            LastDemonContractEffectResult = new DemonContractEffectResult(
                triggered: true,
                bustedTarget: null,
                paidSoulCost);
            RaiseStepped();
        }

        public bool TryPlayerHit()
        {
            if (!CanAcceptPlayerAction() || !Player.Deck.CanDraw(1))
            {
                return false;
            }

            if (_demonContractResolver.TryGetPlayerHitPreviewContract(
                this,
                _activePlayerDemonContracts,
                out ActiveDemonContract previewContract))
            {
                return TryBeginBelphegorTopCardPreview(previewContract);
            }

            CompletePlayerHit(expectedCardId: null);
            return true;
        }

        private void CompletePlayerHit(int? expectedCardId)
        {
            RecordPublicAction(CombatantSide.Player, PublicCombatActionType.Hit);
            BlackjackCard drawnCard = Player.Draw(faceUp: true);
            if (expectedCardId.HasValue && drawnCard.Id != expectedCardId.Value)
            {
                throw new InvalidOperationException(
                    "The card drawn after a demon preview did not match the previewed card.");
            }

            RaiseStepped();
            if (TryBeginAutomaticCardEffect(
                CombatantSide.Player,
                drawnCard,
                AutomaticCardContinuation.ForPlayerHit()))
            {
                return;
            }

            CompletePlayerHitAfterAutomaticCard();
        }

        private void CompletePlayerHitAfterAutomaticCard()
        {
            if (ConsumePendingAzazelBust(CombatantSide.Player))
            {
                OwnerBustHandlingResult azazelHandling = HandlePlayerBust(() =>
                {
                    State = CoreLoopState.PlayerTurn;
                    CompletePlayerHitAction();
                });
                if (azazelHandling == OwnerBustHandlingResult.NotHandled)
                {
                    LastDemonContractEffectResult = new DemonContractEffectResult(
                        triggered: true,
                        bustedTarget: CombatantSide.Player,
                        paidSoulCost: 0);
                    NotifyNormalTurnEnded(CombatantSide.Player);
                    CompleteRound(RoundResolver.ResolveContractEffectBust(
                        RoundNumber,
                        playerIsTarget: true));
                    return;
                }

                if (azazelHandling != OwnerBustHandlingResult.Prevented)
                {
                    return;
                }

                LastDemonContractEffectResult = new DemonContractEffectResult(
                    triggered: true,
                    bustedTarget: null,
                    paidSoulCost: 0);
                RaiseStepped();
            }

            if (Player.VisibleHandValue.IsBust)
            {
                OwnerBustHandlingResult handling = HandlePlayerBust(() =>
                {
                    State = CoreLoopState.PlayerTurn;
                    CompletePlayerHitAction();
                });
                if (handling == OwnerBustHandlingResult.NotHandled)
                {
                    NotifyNormalTurnEnded(CombatantSide.Player);
                    CompleteRound(RoundResolver.ResolveNumericBust(
                        RoundNumber,
                        playerIsTarget: true));
                    return;
                }

                if (handling != OwnerBustHandlingResult.Prevented)
                {
                    return;
                }
            }

            if (State == CoreLoopState.BattleEnded)
            {
                return;
            }

            if (TryResolveBaphometExhaustion(
                    CompletePlayerHitAfterAutomaticCard,
                    CombatantSide.Player))
            {
                return;
            }

            CompletePlayerHitAction();
        }

        private void CompletePlayerHitAction()
        {
            if (TryBeginAzazelCardEffectSequence(
                    _activePlayerDemonContracts,
                    CombatantSide.Player,
                    CompletePlayerActionAndRunEnemyTurn))
            {
                return;
            }

            CompletePlayerActionAndRunEnemyTurn();
        }

        private bool TryBeginBelphegorTopCardPreview(
            ActiveDemonContract previewContract)
        {
            if (previewContract == null ||
                previewContract.Kind != DemonContractKind.Belphegor ||
                !Player.Deck.TryPeekTop(out BlackjackCard previewCard))
            {
                return false;
            }

            int interactionId = TakeNextDemonContractInteractionId();
            _pendingPlayerDemonContractInteraction =
                CreateBelphegorTopCardInteraction(
                    interactionId,
                    previewContract.SourceCardId);
            _playerDemonContractPreview = new PlayerDemonContractPreview(
                interactionId,
                previewContract.SourceCardId,
                DemonContractKind.Belphegor,
                previewCard);
            State = CoreLoopState.PlayerResolvingDemonContract;
            RaiseStepped();
            return true;
        }

        private bool TryResolveBelphegorTopCard(
            PendingDemonContractInteraction pending,
            DemonContractOption selectedOption)
        {
            PlayerDemonContractPreview preview = _playerDemonContractPreview;
            if (pending.ContractKind != DemonContractKind.Belphegor ||
                preview == null ||
                preview.InteractionId != pending.InteractionId ||
                preview.ContractKind != DemonContractKind.Belphegor ||
                !HasActivePlayerContract(preview.SourceContractCardId,
                    DemonContractKind.Belphegor) ||
                !Player.Deck.TryPeekTop(out BlackjackCard currentTopCard) ||
                currentTopCard.Id != preview.CardId)
            {
                return false;
            }

            switch (selectedOption.OptionId)
            {
                case BelphegorDemonContractHandler.KeepTopCardOptionId:
                    ClearPlayerDemonContractInteraction();
                    State = CoreLoopState.PlayerTurn;
                    CompletePlayerHit(preview.CardId);
                    return true;

                case BelphegorDemonContractHandler.MoveTopCardToBottomOptionId:
                    if (!Player.Deck.TryMoveTopToBottom(preview.CardId))
                    {
                        return false;
                    }

                    ClearPlayerDemonContractInteraction();
                    State = CoreLoopState.PlayerTurn;
                    RaiseStepped();
                    CompletePlayerActionAndRunEnemyTurn();
                    return true;

                default:
                    return false;
            }
        }

        private bool TryResolveMammonFinalChoice(
            PendingDemonContractInteraction pending,
            DemonContractOption selectedOption)
        {
            if (!TryGetPendingActiveContract(
                pending,
                DemonContractKind.Mammon,
                out ActiveDemonContract activeContract))
            {
                return false;
            }

            int playerBonus = _demonContractResolver.ResolvePlayerFinalChoice(
                this,
                activeContract,
                selectedOption.OptionId);
            ClearPlayerDemonContractInteraction();
            ResolveRoundWithEnemyFinalChoice(playerBonus);
            return true;
        }

        public bool TryPlayerStand()
        {
            if (!CanPlayerStand)
            {
                return false;
            }

            RecordPublicAction(CombatantSide.Player, PublicCombatActionType.Stand);
            Player.Stand();
            RaiseStepped();
            CompletePlayerActionAndRunEnemyTurn();
            return true;
        }

        public bool TryBeginPlayerChange()
        {
            if (!CanBeginPlayerChange)
            {
                return false;
            }

            Player.Soul.ApplyDamage(NextPlayerChangeSoulCost);
            if (!Player.TryBeginChange(out PlayerChangeSelection selection))
            {
                throw new InvalidOperationException(
                    "Validated player change could not begin.");
            }

            _playerChangeSelection = selection;
            _automaticCardBattleState.InvalidateKnowledgeAboutHiddenCard(
                CombatantSide.Player,
                selection.PreviousHiddenCardId);
            State = CoreLoopState.PlayerChoosingChangeCard;
            return true;
        }

        public bool TrySelectChangedCard(int candidateIndex)
        {
            if (!CanSelectChangedCard ||
                !_playerChangeSelection.TrySelectCandidate(candidateIndex))
            {
                return false;
            }

            PlayerChangeSelection completedSelection = _playerChangeSelection;
            Player.CompleteChange(completedSelection);
            _playerChangeSelection = null;
            CompletedPlayerChangeCount = checked(CompletedPlayerChangeCount + 1);
            RecordPublicAction(CombatantSide.Player, PublicCombatActionType.Change);

            State = CoreLoopState.PlayerTurn;
            CompletePlayerActionAndRunEnemyTurn();
            return true;
        }

        private bool ShouldEnemyChange()
        {
            if (!Enemy.Hand.TryGetSingleHiddenCard(out BlackjackCard hiddenCard))
            {
                return false;
            }

            return hiddenCard.IsFaceUp || Enemy.HandValue.IsBust;
        }

        private bool TryExecuteEnemyChange()
        {
            if (!CanBeginEnemyChange)
            {
                return false;
            }

            ApplySoulDamage(CombatantSide.Enemy, NextEnemyChangeSoulCost);
            if (!Enemy.TryBeginChange(out PlayerChangeSelection selection))
            {
                throw new InvalidOperationException(
                    "Validated enemy change could not begin.");
            }

            _automaticCardBattleState.InvalidateKnowledgeAboutHiddenCard(
                CombatantSide.Enemy,
                selection.PreviousHiddenCardId);
            int candidateIndex = EnemyChangeCandidateSelector.Select(
                Enemy.Hand.Cards,
                selection.Candidates);
            if (!selection.TrySelectCandidate(candidateIndex))
            {
                throw new InvalidOperationException(
                    "Validated enemy change candidate could not be selected.");
            }

            Enemy.CompleteChange(selection);
            CompletedEnemyChangeCount = checked(CompletedEnemyChangeCount + 1);
            RecordPublicAction(CombatantSide.Enemy, PublicCombatActionType.Change);
            RaiseStepped();
            CompleteEnemyAction();
            return true;
        }

        private bool CanAcceptPlayerAction()
        {
            return CanPlayerAct;
        }

        private DemonContractFailureReason EvaluatePlayerDemonContractFailureReason()
        {
            if (State == CoreLoopState.PlayerResolvingDemonContract ||
                _pendingPlayerDemonContractInteraction != null ||
                State == CoreLoopState.PlayerChoosingChangeCard ||
                State == CoreLoopState.PlayerResolvingCardEffect)
            {
                return DemonContractFailureReason.PendingInteraction;
            }

            if (State == CoreLoopState.EnemyTurn)
            {
                return DemonContractFailureReason.NotPlayerTurn;
            }

            if (State != CoreLoopState.PlayerTurn)
            {
                return DemonContractFailureReason.BattleNotActive;
            }

            if (Player.IsStanding)
            {
                return DemonContractFailureReason.PlayerStanding;
            }

            if (UsedPlayerBaseDemonContractCount >= BasePlayerDemonContractUseLimit)
            {
                return DemonContractFailureReason.BaseUseLimitReached;
            }

            if (Player.Soul.Current <= BasePlayerDemonContractSoulCost)
            {
                return DemonContractFailureReason.InsufficientSoul;
            }

            if (!PlayerDemonDeck.CanTakeCandidates)
            {
                return DemonContractFailureReason.InsufficientCandidates;
            }

            return DemonContractFailureReason.None;
        }

        private DemonContractFailureReason EvaluateEnemyDemonContractFailureReason()
        {
            if (_fixedEnemyDemonContractPhases.Count > 0)
            {
                return DemonContractFailureReason.BaseUseLimitReached;
            }

            if (_pendingEnemyDemonContractInteraction != null ||
                _pendingPlayerDemonContractInteraction != null ||
                _pendingCardEffect != null ||
                State == CoreLoopState.PlayerResolvingDemonContract ||
                State == CoreLoopState.PlayerChoosingChangeCard ||
                State == CoreLoopState.PlayerResolvingCardEffect)
            {
                return DemonContractFailureReason.PendingInteraction;
            }

            if (State == CoreLoopState.PlayerTurn)
            {
                return DemonContractFailureReason.NotPlayerTurn;
            }

            if (State != CoreLoopState.EnemyTurn)
            {
                return DemonContractFailureReason.BattleNotActive;
            }

            if (Enemy.IsStanding)
            {
                return DemonContractFailureReason.PlayerStanding;
            }

            if (UsedEnemyBaseDemonContractCount >= BaseEnemyDemonContractUseLimit)
            {
                return DemonContractFailureReason.BaseUseLimitReached;
            }

            if (Enemy.Soul.Current <= BaseEnemyDemonContractSoulCost)
            {
                return DemonContractFailureReason.InsufficientSoul;
            }

            if (!EnemyDemonDeck.CanTakeCandidates)
            {
                return DemonContractFailureReason.InsufficientCandidates;
            }

            return DemonContractFailureReason.None;
        }

        private static PendingDemonContractInteraction CreateContractChoiceInteraction(
            int interactionId,
            IReadOnlyList<DemonContractCard> candidates)
        {
            var options = new List<DemonContractOption>(candidates.Count);
            for (int i = 0; i < candidates.Count; i++)
            {
                DemonContractCard candidate = candidates[i];
                options.Add(new DemonContractOption(
                    i,
                    candidate.Id,
                    numericValue: null,
                    candidate.Definition.DisplayName,
                    candidate.DefinitionKey));
            }

            return new PendingDemonContractInteraction(
                interactionId,
                DemonContractInteractionKind.ChooseContract,
                contractKind: null,
                options,
                "계약할 악마를 선택하십시오.");
        }

        private static PendingDemonContractInteraction
            CreateLuciferContractChoiceInteraction(
                int interactionId,
                ActiveDemonContract luciferContract,
                IReadOnlyList<DemonContractCard> candidates)
        {
            var options = new List<DemonContractOption>(candidates.Count + 1);
            for (int i = 0; i < candidates.Count; i++)
            {
                DemonContractCard candidate = candidates[i];
                options.Add(new DemonContractOption(
                    i,
                    candidate.Id,
                    numericValue: null,
                    candidate.Definition.DisplayName,
                    candidate.DefinitionKey));
            }

            options.Add(new DemonContractOption(
                LuciferDemonContractHandler.SkipAdditionalContractOptionId,
                contractCardId: null,
                numericValue: null,
                "추가 계약하지 않음"));

            return new PendingDemonContractInteraction(
                interactionId,
                DemonContractInteractionKind.LuciferChooseAdditionalContract,
                DemonContractKind.Lucifer,
                options,
                "루시퍼로 추가 계약할 악마를 선택하거나 건너뛰십시오.",
                luciferContract.SourceCardId);
        }

        private static PendingDemonContractInteraction CreateBelphegorTopCardInteraction(
            int interactionId,
            int sourceContractCardId)
        {
            var options = new[]
            {
                new DemonContractOption(
                    BelphegorDemonContractHandler.KeepTopCardOptionId,
                    contractCardId: null,
                    numericValue: null,
                    "히트하기"),
                new DemonContractOption(
                    BelphegorDemonContractHandler.MoveTopCardToBottomOptionId,
                    contractCardId: null,
                    numericValue: null,
                    "히트하지 않기")
            };

            return new PendingDemonContractInteraction(
                interactionId,
                DemonContractInteractionKind.BelphegorTopCard,
                DemonContractKind.Belphegor,
                options,
                "확인한 덱 위 카드를 처리하십시오.",
                sourceContractCardId);
        }

        private static PendingDemonContractInteraction CreateMammonFinalChoiceInteraction(
            int interactionId,
            ActiveDemonContract activeContract)
        {
            var options = new[]
            {
                new DemonContractOption(
                    MammonDemonContractHandler.DoNotApplyDieOptionId,
                    contractCardId: null,
                    numericValue: null,
                    "주사위 값 포함하지 않기"),
                new DemonContractOption(
                    MammonDemonContractHandler.ApplyDieOptionId,
                    contractCardId: null,
                    numericValue: null,
                    "주사위 값 포함하기")
            };

            return new PendingDemonContractInteraction(
                interactionId,
                DemonContractInteractionKind.MammonApplyDie,
                DemonContractKind.Mammon,
                options,
                "최종 승부에 현재 주사위 값을 포함할지 선택하십시오.",
                activeContract.SourceCardId);
        }

        private static PendingDemonContractInteraction
            CreateSatanFirstNumberInteraction(
                int interactionId,
                int sourceContractCardId)
        {
            return new PendingDemonContractInteraction(
                interactionId,
                DemonContractInteractionKind.SatanDeclareFirstNumber,
                DemonContractKind.Satan,
                CreateSatanNumberOptions(excludedNumber: null),
                "Declare the first number.",
                sourceContractCardId);
        }

        private static PendingDemonContractInteraction
            CreateSatanSecondNumberInteraction(
                int interactionId,
                int sourceContractCardId,
                int firstNumber)
        {
            return new PendingDemonContractInteraction(
                interactionId,
                DemonContractInteractionKind.SatanDeclareSecondNumber,
                DemonContractKind.Satan,
                CreateSatanNumberOptions(firstNumber),
                "Declare a different second number.",
                sourceContractCardId,
                firstNumber);
        }

        private static IReadOnlyList<DemonContractOption>
            CreateSatanNumberOptions(int? excludedNumber)
        {
            var options = new List<DemonContractOption>(10);
            for (int number = 1; number <= 10; number++)
            {
                if (number == excludedNumber)
                {
                    continue;
                }

                options.Add(new DemonContractOption(
                    number,
                    contractCardId: null,
                    numericValue: number,
                    number.ToString()));
            }

            return options.AsReadOnly();
        }

        private static PendingDemonContractInteraction
            CreateBeelzebubDiscardInteraction(
                int interactionId,
                ActiveDemonContract activeContract,
                IReadOnlyList<BlackjackCard> candidates,
                bool choosingOwnerCard)
        {
            var options = new List<DemonContractOption>(candidates.Count);
            for (int i = 0; i < candidates.Count; i++)
            {
                BlackjackCard candidate = candidates[i];
                options.Add(new DemonContractOption(
                    i,
                    candidate.Id,
                    numericValue: null,
                    $"{candidate.Definition.DisplayName} ({candidate.Rank})"));
            }

            return new PendingDemonContractInteraction(
                interactionId,
                choosingOwnerCard
                    ? DemonContractInteractionKind.BeelzebubChooseOwnerCard
                    : DemonContractInteractionKind.BeelzebubChooseOpponentCard,
                DemonContractKind.Beelzebub,
                options,
                choosingOwnerCard
                    ? "버릴 자신의 공개 카드를 선택하십시오."
                    : "버릴 상대의 공개 카드를 선택하십시오.",
                activeContract.SourceCardId);
        }

        private static PendingDemonContractInteraction
            CreateAsmodeusTurnStartInteraction(
                int interactionId,
                ActiveDemonContract activeContract)
        {
            var options = new[]
            {
                new DemonContractOption(
                    AsmodeusDemonContractHandler.SkipForcedHitOptionId,
                    contractCardId: null,
                    numericValue: null,
                    "능력 사용하지 않기"),
                new DemonContractOption(
                    AsmodeusDemonContractHandler.ForceHitOptionId,
                    contractCardId: null,
                    numericValue: null,
                    "능력 사용하기")
            };

            return new PendingDemonContractInteraction(
                interactionId,
                DemonContractInteractionKind.AsmodeusForceOpponentHit,
                DemonContractKind.Asmodeus,
                options,
                "차례 시작에 상대를 히트시킬지 선택하십시오.",
                activeContract.SourceCardId);
        }

        private static PendingDemonContractInteraction
            CreatePaimonDeckChoiceInteraction(
                int interactionId,
                ActiveDemonContract activeContract,
                BattleParticipant owner,
                BattleParticipant opponent)
        {
            var options = new List<DemonContractOption>(2);
            if (owner.Deck.CanDraw(PaimonDemonContractHandler.PeekCardCount))
            {
                options.Add(new DemonContractOption(
                    PaimonDemonContractHandler.OwnerDeckOptionId,
                    contractCardId: null,
                    numericValue: null,
                    "내 덱 확인"));
            }

            if (opponent.Deck.CanDraw(PaimonDemonContractHandler.PeekCardCount))
            {
                options.Add(new DemonContractOption(
                    PaimonDemonContractHandler.OpponentDeckOptionId,
                    contractCardId: null,
                    numericValue: null,
                    "상대 덱 확인"));
            }

            return new PendingDemonContractInteraction(
                interactionId,
                DemonContractInteractionKind.PaimonChooseDeck,
                DemonContractKind.Paimon,
                options,
                "전투 동안 카드를 추방할 덱을 선택하십시오.",
                activeContract.SourceCardId);
        }

        private static PendingDemonContractInteraction
            CreatePaimonCardChoiceInteraction(
                int interactionId,
                ActiveDemonContract activeContract,
                IReadOnlyList<BlackjackCard> peekedCards)
        {
            if (peekedCards == null ||
                peekedCards.Count != PaimonDemonContractHandler.PeekCardCount)
            {
                throw new ArgumentException(
                    "Paimon requires exactly two peeked cards.",
                    nameof(peekedCards));
            }

            var options = new List<DemonContractOption>(3)
            {
                new DemonContractOption(
                    PaimonDemonContractHandler.SkipExileOptionId,
                    contractCardId: null,
                    numericValue: null,
                    "추방하지 않음")
            };
            for (int i = 0; i < peekedCards.Count; i++)
            {
                BlackjackCard card = peekedCards[i];
                options.Add(new DemonContractOption(
                    PaimonDemonContractHandler.FirstCardOptionId + i,
                    card.Id,
                    card.Rank,
                    $"{card.Definition.DisplayName} ({card.Rank})"));
            }

            return new PendingDemonContractInteraction(
                interactionId,
                DemonContractInteractionKind.PaimonChooseExileCard,
                DemonContractKind.Paimon,
                options,
                "전투 종료까지 추방할 카드를 선택하십시오.",
                activeContract.SourceCardId);
        }

        private static PendingDemonContractInteraction
            CreateBelialTurnStartInteraction(
                int interactionId,
                ActiveDemonContract activeContract,
                IReadOnlyList<BlackjackCard> opponentCards)
        {
            var options = new List<DemonContractOption>(opponentCards.Count + 1)
            {
                new DemonContractOption(
                    BelialDemonContractHandler.SkipTransferOptionId,
                    contractCardId: null,
                    numericValue: null,
                    "가져오지 않음")
            };
            for (int i = 0; i < opponentCards.Count; i++)
            {
                BlackjackCard card = opponentCards[i];
                options.Add(new DemonContractOption(
                    BelialDemonContractHandler.FirstTransferOptionId + i,
                    card.Id,
                    card.Rank,
                    $"{card.Definition.DisplayName} ({card.Rank})"));
            }

            return new PendingDemonContractInteraction(
                interactionId,
                DemonContractInteractionKind.BelialChooseOpponentCard,
                DemonContractKind.Belial,
                options,
                "가져와 즉시 사용할 상대 공개 카드를 선택하십시오.",
                activeContract.SourceCardId);
        }

        private int TakeNextDemonContractInteractionId()
        {
            int interactionId = _nextDemonContractInteractionId;
            _nextDemonContractInteractionId = checked(
                _nextDemonContractInteractionId + 1);
            return interactionId;
        }

        private bool HasActivePlayerContract(
            int sourceContractCardId,
            DemonContractKind kind)
        {
            foreach (ActiveDemonContract activeContract in _activePlayerDemonContracts)
            {
                if (activeContract.SourceCardId == sourceContractCardId &&
                    activeContract.Kind == kind &&
                    activeContract.OwnerSide == CombatantSide.Player)
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryGetPendingActiveContract(
            PendingDemonContractInteraction pending,
            DemonContractKind kind,
            out ActiveDemonContract activeContract)
        {
            if (pending == null ||
                pending.ContractKind != kind ||
                !pending.SourceContractCardId.HasValue)
            {
                activeContract = null;
                return false;
            }

            foreach (ActiveDemonContract candidate in _activePlayerDemonContracts)
            {
                if (candidate.SourceCardId == pending.SourceContractCardId.Value &&
                    candidate.Kind == kind &&
                    candidate.OwnerSide == CombatantSide.Player)
                {
                    activeContract = candidate;
                    return true;
                }
            }

            activeContract = null;
            return false;
        }

        private bool TryGetPendingEnemyActiveContract(
            PendingDemonContractInteraction pending,
            DemonContractKind kind,
            out ActiveDemonContract activeContract)
        {
            if (pending == null ||
                pending.ContractKind != kind ||
                !pending.SourceContractCardId.HasValue)
            {
                activeContract = null;
                return false;
            }

            foreach (ActiveDemonContract candidate in _activeEnemyDemonContracts)
            {
                if (candidate.SourceCardId == pending.SourceContractCardId.Value &&
                    candidate.Kind == kind &&
                    candidate.OwnerSide == CombatantSide.Enemy)
                {
                    activeContract = candidate;
                    return true;
                }
            }

            activeContract = null;
            return false;
        }

        private void ClearPlayerDemonContractInteraction()
        {
            _pendingPlayerDemonContractInteraction = null;
            _playerDemonContractPreview = null;
            _playerDemonContractCandidates = null;
        }

        private void ClearEnemyDemonContractInteraction()
        {
            if (_enemyDemonContractCandidates != null)
            {
                EnemyDemonDeck.Discard(_enemyDemonContractCandidates);
            }

            _pendingEnemyDemonContractInteraction = null;
            _enemyDemonContractPreview = null;
            _enemyDemonContractCandidates = null;
        }

        internal CardUseAvailability EvaluatePlayerCardUse(int cardId)
        {
            return CardUseValidator.Evaluate(this, _cardEffectResolver, cardId);
        }

        internal CardUseAvailability EvaluateCardUse(
            CombatantSide actorSide,
            int cardId)
        {
            return CardUseValidator.EvaluateForActor(
                this,
                _cardEffectResolver,
                actorSide,
                cardId);
        }

        internal bool CanActorUseCard(CombatantSide actorSide)
        {
            switch (actorSide)
            {
                case CombatantSide.Player:
                    return CanPlayerAct;
                case CombatantSide.Enemy:
                    return State == CoreLoopState.EnemyTurn &&
                        !Enemy.IsStanding &&
                        _pendingEnemyDemonContractInteraction == null;
                default:
                    throw new ArgumentOutOfRangeException(nameof(actorSide));
            }
        }

        internal bool HasActiveCardEffect => _activeCardEffectContext != null;

        internal CardEffectKind? ActiveCardEffectKind =>
            _activeCardEffectContext?.SourceCard.Definition.Effect;

        internal int? ActiveCardEffectSourceCardId =>
            _activeCardEffectContext?.SourceCard.Id;

        internal CombatantSide? ActiveCardEffectActorSide =>
            _activeCardEffectActorSide;

        internal PendingCardEffect PendingEnemyCardEffect =>
            _activeCardEffectActorSide == CombatantSide.Enemy
                ? _pendingCardEffect
                : null;

        internal IReadOnlyList<PublicCombatAction> PublicActionHistory =>
            _publicActionHistory.AsReadOnly();

        /// <summary>
        /// The most recent public action of the current round (both sides), or null right after a
        /// deal (the history is cleared each round). Lets a view label what just happened at a step.
        /// </summary>
        public PublicCombatAction LastPublicAction =>
            _publicActionHistory.Count > 0
                ? _publicActionHistory[_publicActionHistory.Count - 1]
                : null;

        internal BattleParticipant GetParticipant(CombatantSide side)
        {
            switch (side)
            {
                case CombatantSide.Player:
                    return Player;
                case CombatantSide.Enemy:
                    return Enemy;
                default:
                    throw new ArgumentOutOfRangeException(nameof(side));
            }
        }

        internal BattleParticipant GetOpponent(CombatantSide side)
        {
            switch (side)
            {
                case CombatantSide.Player:
                    return Enemy;
                case CombatantSide.Enemy:
                    return Player;
                default:
                    throw new ArgumentOutOfRangeException(nameof(side));
            }
        }

        internal bool CanOwnerStandForAutomaticCard(CombatantSide ownerSide)
        {
            BattleParticipant owner = GetParticipant(ownerSide);
            if (owner.IsStanding)
            {
                return false;
            }

            IReadOnlyList<ActiveDemonContract> ownerContracts =
                ownerSide == CombatantSide.Player
                    ? _activePlayerDemonContracts
                    : _activeEnemyDemonContracts;
            return _demonContractResolver.CanOwnerStand(
                this,
                ownerContracts,
                ownerSide);
        }

        internal bool TryStandOwnerForAutomaticCard(CombatantSide ownerSide)
        {
            if (!CanOwnerStandForAutomaticCard(ownerSide))
            {
                return false;
            }

            RecordPublicAction(
                ownerSide,
                PublicCombatActionType.Stand);
            GetParticipant(ownerSide).Stand();
            return true;
        }

        private void AdvanceFixedEnemyDemonContractPhases()
        {
            if (_fixedEnemyDemonContractPhases.Count == 0 ||
                Enemy.Soul.IsDepleted)
            {
                return;
            }

            while (_fixedEnemyDemonContractPhaseIndex + 1 <
                _fixedEnemyDemonContractPhases.Count)
            {
                int nextPhaseIndex = _fixedEnemyDemonContractPhaseIndex + 1;
                FixedDemonContractPhaseDefinition nextPhase =
                    _fixedEnemyDemonContractPhases[nextPhaseIndex];
                if (nextPhaseIndex > 0 &&
                    Enemy.Soul.Current >
                        nextPhase.ActivationSoulThreshold.Value)
                {
                    return;
                }

                if (!EnemyDemonDeck.TryTakeFixedPhaseCards(
                        nextPhase.ActiveDefinitionKey,
                        nextPhase.DiscardedDefinitionKey,
                        out DemonContractCard activeCard,
                        out DemonContractCard discardedCard))
                {
                    throw new InvalidOperationException(
                        "Validated fixed enemy demon contract phase cards are unavailable.");
                }

                EnemyDemonDeck.Discard(discardedCard);
                if (_activeFixedEnemyDemonContract != null)
                {
                    if (!_activeEnemyDemonContracts.Remove(
                        _activeFixedEnemyDemonContract))
                    {
                        throw new InvalidOperationException(
                            "The active fixed enemy demon contract is missing.");
                    }

                    EnemyDemonDeck.Discard(
                        _activeFixedEnemyDemonContract.SourceCard);
                }

                var activeContract = new ActiveDemonContract(
                    activeCard,
                    CombatantSide.Enemy,
                    new EmptyDemonContractRuntimeState());
                _activeEnemyDemonContracts.Add(activeContract);
                activeContract.SetRuntimeState(
                    _demonContractResolver.Activate(this, activeContract));
                _activeFixedEnemyDemonContract = activeContract;
                _fixedEnemyDemonContractPhaseIndex = nextPhaseIndex;
                if (activeContract.Kind == DemonContractKind.Azazel)
                {
                    _pendingFixedEnemyAzazelActivation = true;
                }
                RecordPublicAction(
                    CombatantSide.Enemy,
                    PublicCombatActionType.DemonContract,
                    activeContract.Definition.Key,
                    activeContract.SourceCardId);
                RaiseStepped();
            }
        }

        internal void ApplySoulDamage(CombatantSide ownerSide, int amount)
        {
            GetParticipant(ownerSide).Soul.ApplyDamage(amount);
            if (ownerSide == CombatantSide.Enemy)
            {
                AdvanceFixedEnemyDemonContractPhases();
            }
        }

        internal void RegisterPoisonWinReward(
            int sourceCardId,
            CombatantSide ownerSide,
            int healAmount)
        {
            _automaticCardBattleState.RegisterPoisonWinReward(
                sourceCardId,
                ownerSide,
                RoundNumber,
                healAmount);
        }

        internal void RecordLieDetectorResult(
            int sourceCardId,
            CombatantSide ownerSide,
            int declaredNumber,
            int? subjectHiddenCardId,
            bool? isAtLeastDeclaredNumber)
        {
            bool wasComparable =
                subjectHiddenCardId.HasValue &&
                isAtLeastDeclaredNumber.HasValue;
            if (subjectHiddenCardId.HasValue !=
                isAtLeastDeclaredNumber.HasValue)
            {
                throw new ArgumentException(
                    "Lie detector private result must be complete or absent.");
            }

            LastLieDetectorPublicResult = new LieDetectorPublicResult(
                sourceCardId,
                ownerSide,
                declaredNumber,
                wasComparable);
            _automaticCardBattleState.ClearHiddenCardKnowledgeForObserver(
                ownerSide);
            if (wasComparable)
            {
                _automaticCardBattleState.SetHiddenCardKnowledge(
                    ownerSide,
                    ownerSide == CombatantSide.Player
                        ? CombatantSide.Enemy
                        : CombatantSide.Player,
                    subjectHiddenCardId.Value,
                    declaredNumber,
                    isAtLeastDeclaredNumber.Value,
                    RoundNumber);
            }
        }

        internal void InvalidateHiddenCardKnowledge(
            CombatantSide subjectSide,
            int previousHiddenCardId)
        {
            _automaticCardBattleState.InvalidateKnowledgeAboutHiddenCard(
                subjectSide,
                previousHiddenCardId);
        }

        internal bool TryBeginAutomaticCardEffect(
            CombatantSide ownerSide,
            BlackjackCard sourceCard,
            AutomaticCardContinuation continuation)
        {
            return TryBeginAutomaticCardEffect(
                ownerSide,
                sourceCard,
                continuation,
                out _);
        }

        internal bool TryBeginAutomaticCardEffect(
            CombatantSide ownerSide,
            BlackjackCard sourceCard,
            AutomaticCardContinuation continuation,
            out AutomaticCardResult? immediateResult)
        {
            immediateResult = null;
            if (!Enum.IsDefined(typeof(CombatantSide), ownerSide))
            {
                throw new ArgumentOutOfRangeException(nameof(ownerSide));
            }

            if (sourceCard == null)
            {
                throw new ArgumentNullException(nameof(sourceCard));
            }

            if (continuation == null)
            {
                throw new ArgumentNullException(nameof(continuation));
            }

            if (sourceCard.Definition.Activation != CardActivationKind.Automatic)
            {
                return false;
            }

            if (_activeAutomaticCardEffectContext != null ||
                _pendingAutomaticCardInteraction != null)
            {
                throw new InvalidOperationException(
                    "Only one automatic card effect can resolve at a time.");
            }

            if (!sourceCard.IsFaceUp ||
                !GetParticipant(ownerSide).Hand.TryGetCard(
                    sourceCard.Id,
                    out BlackjackCard heldCard) ||
                !ReferenceEquals(sourceCard, heldCard))
            {
                throw new InvalidOperationException(
                    "Automatic card effects require their face-up physical source card in hand.");
            }

            if (!_automaticCardEffectResolver.Supports(
                sourceCard.Definition.Effect))
            {
                throw new InvalidOperationException(
                    $"Automatic card handler for {sourceCard.Definition.Effect} is not registered.");
            }

            LastAutomaticCardActivationOrdinal =
                _nextAutomaticCardActivationOrdinal;
            LastAutomaticCardActivationRoundNumber = RoundNumber;
            _nextAutomaticCardActivationOrdinal = checked(
                _nextAutomaticCardActivationOrdinal + 1);
            LastAutomaticCardActivationOwnerSide = ownerSide;
            LastAutomaticCardActivationDefinitionKey =
                sourceCard.DefinitionKey;

            _activeAutomaticCardEffectContext =
                new AutomaticCardEffectContext(this, ownerSide, sourceCard);
            _automaticCardContinuation = continuation;
            CoreLoopState stateBeforeAutomaticChoice = State;
            _isBeginningAutomaticCardEffect = true;
            try
            {
                AutomaticCardEffectStep step =
                    _automaticCardEffectResolver.Begin(
                        _activeAutomaticCardEffectContext);
                bool isWaitingForChoice = ApplyAutomaticCardEffectStep(
                    step,
                    resumeContinuation: false);
                if (!isWaitingForChoice)
                {
                    if (State == CoreLoopState.ResolvingAutomaticCardEffect)
                    {
                        State = stateBeforeAutomaticChoice;
                    }

                    immediateResult = LastAutomaticCardResult;
                }

                return isWaitingForChoice;
            }
            finally
            {
                _isBeginningAutomaticCardEffect = false;
            }
        }

        internal bool TryResolveAutomaticCardChoice(
            CombatantSide decisionSide,
            int interactionId,
            int optionId)
        {
            PendingAutomaticCardInteraction pending =
                _pendingAutomaticCardInteraction;
            if (State != CoreLoopState.ResolvingAutomaticCardEffect ||
                pending == null ||
                pending.DecisionSide != decisionSide ||
                pending.InteractionId != interactionId ||
                _activeAutomaticCardEffectContext == null ||
                !_pendingAutomaticCardInteraction.TryGetOption(
                    optionId,
                    out AutomaticCardChoiceOption selectedOption))
            {
                return false;
            }

            AutomaticCardEffectStep step =
                _automaticCardEffectResolver.ResolveChoice(
                    _activeAutomaticCardEffectContext,
                    pending,
                    selectedOption);
            ApplyAutomaticCardEffectStep(
                step,
                resumeContinuation: !_isBeginningAutomaticCardEffect);
            return true;
        }

        private bool ApplyAutomaticCardEffectStep(
            AutomaticCardEffectStep step,
            bool resumeContinuation)
        {
            if (step == null)
            {
                throw new InvalidOperationException(
                    "Automatic card handler returned no step.");
            }

            if (!resumeContinuation &&
                step.CompletionFlow !=
                    AutomaticCardCompletionFlow.ResumeContinuation)
            {
                throw new InvalidOperationException(
                    "An automatic card cannot change round or battle flow before a pending choice resumes.");
            }

            AutomaticCardEffectContext context =
                _activeAutomaticCardEffectContext ??
                    throw new InvalidOperationException(
                        "Automatic card effect has no active context.");
            BlackjackCard sourceCard = context.SourceCard;

            if (step.ChoiceRequest != null)
            {
                if (step.SourceDisposition.HasValue)
                {
                    throw new InvalidOperationException(
                        "Automatic card step cannot be pending and complete.");
                }

                AutomaticCardChoiceRequest request = step.ChoiceRequest;
                _pendingAutomaticCardInteraction =
                    new PendingAutomaticCardInteraction(
                        TakeNextAutomaticCardInteractionId(),
                        sourceCard.Id,
                        sourceCard.Definition.Effect,
                        context.OwnerSide,
                        request.DecisionSide,
                        request.ChoiceKind,
                        request.Prompt,
                        request.Options);
                State = CoreLoopState.ResolvingAutomaticCardEffect;
                RaiseStepped();
                ResolvePendingEnemyAutomaticChoices();
                return _pendingAutomaticCardInteraction != null;
            }

            if (!step.SourceDisposition.HasValue)
            {
                throw new InvalidOperationException(
                    "Automatic card step is neither pending nor complete.");
            }

            AutomaticCardSourceDisposition disposition =
                step.SourceDisposition.Value;
            BattleParticipant owner = GetParticipant(context.OwnerSide);
            switch (disposition)
            {
                case AutomaticCardSourceDisposition.Discard:
                    if (!owner.TryDiscardCard(sourceCard.Id))
                    {
                        throw new InvalidOperationException(
                            "Automatic card source could not be discarded.");
                    }

                    break;
                case AutomaticCardSourceDisposition.RetainFaceUp:
                    if (!owner.Hand.TryGetCard(
                            sourceCard.Id,
                            out BlackjackCard retainedCard) ||
                        !ReferenceEquals(sourceCard, retainedCard) ||
                        !retainedCard.IsFaceUp)
                    {
                        throw new InvalidOperationException(
                            "Retained automatic card source is not face-up in its owner hand.");
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(disposition));
            }

            AutomaticCardResult result = context.CreateResult(disposition);
            AutomaticCardContinuation continuation =
                _automaticCardContinuation ??
                    throw new InvalidOperationException(
                        "Automatic card effect has no continuation.");

            LastAutomaticCardResult = result;
            _lastAutomaticCardResultActionOrdinal = _publicActionHistory.Count;
            _pendingAutomaticCardInteraction = null;
            _activeAutomaticCardEffectContext = null;
            _automaticCardContinuation = null;
            RaiseStepped();

            if (step.CompletionFlow ==
                AutomaticCardCompletionFlow.EndBattle)
            {
                if (continuation.Kind ==
                    AutomaticCardContinuationKind.DemonContract)
                {
                    NotifyNormalTurnEnded(continuation.ActorSide);
                }

                EndBattleWithoutRound();
                return false;
            }

            if (step.CompletionFlow ==
                AutomaticCardCompletionFlow.CancelContinuation)
            {
                if (continuation.Kind ==
                    AutomaticCardContinuationKind.DemonContract)
                {
                    NotifyNormalTurnEnded(continuation.ActorSide);
                }

                CancelPendingEffectResolutions();
                if (continuation.ActorSide == CombatantSide.Enemy)
                {
                    NotifyNormalTurnEnded(CombatantSide.Enemy);
                    BeginPlayerTurn();
                }
                else
                {
                    State = CoreLoopState.PlayerTurn;
                }

                RaiseStepped();
                return false;
            }

            if (resumeContinuation)
            {
                ResumeAfterAutomaticCard(continuation, result);
            }

            return false;
        }

        public bool TryBeginPlayerMammonReroll(int sourceContractCardId)
        {
            return TryBeginMammonReroll(
                CombatantSide.Player,
                sourceContractCardId,
                null);
        }

        public bool TryBeginPlayerMammonReroll(
            int sourceContractCardId,
            int physicalDieValue)
        {
            if (physicalDieValue < 1 || physicalDieValue > 6)
            {
                return false;
            }

            return TryBeginMammonReroll(
                CombatantSide.Player,
                sourceContractCardId,
                physicalDieValue);
        }

        public bool CanBeginPlayerActiveDemonContractAction(
            int sourceContractCardId)
        {
            if (sourceContractCardId < 0 || !CanAcceptPlayerAction())
            {
                return false;
            }

            foreach (ActiveDemonContract activeContract in
                _activePlayerDemonContracts)
            {
                if (activeContract.SourceCardId != sourceContractCardId)
                {
                    continue;
                }

                switch (activeContract.Kind)
                {
                    case DemonContractKind.Mammon:
                        return _demonContractResolver.CanOwnerRerollMammon(
                            this,
                            activeContract);
                    case DemonContractKind.Satan:
                        if (!(activeContract.RuntimeState is SatanRuntimeState
                            satanState))
                        {
                            return false;
                        }

                        return satanState.CurrentFace == SatanContractFace.Upper
                            ? TryGetSingleHiddenCard(Enemy, out _)
                            : Enemy.Deck.CanDraw(1);
                    default:
                        return false;
                }
            }

            return false;
        }

        public bool TryBeginPlayerActiveDemonContractAction(
            int sourceContractCardId)
        {
            if (!CanBeginPlayerActiveDemonContractAction(sourceContractCardId))
            {
                return false;
            }

            foreach (ActiveDemonContract activeContract in
                _activePlayerDemonContracts)
            {
                if (activeContract.SourceCardId != sourceContractCardId)
                {
                    continue;
                }

                switch (activeContract.Kind)
                {
                    case DemonContractKind.Mammon:
                        return TryBeginPlayerMammonReroll(
                            sourceContractCardId);
                    case DemonContractKind.Satan:
                        return TryBeginPlayerSatanContractAction(
                            sourceContractCardId);
                    default:
                        return false;
                }
            }

            return false;
        }

        private bool TryBeginMammonReroll(
            CombatantSide ownerSide,
            int sourceContractCardId,
            int? suppliedDieValue = null)
        {
            if (sourceContractCardId < 0)
            {
                return false;
            }

            bool canAcceptAction = ownerSide == CombatantSide.Player
                ? CanAcceptPlayerAction()
                : State == CoreLoopState.EnemyTurn &&
                    !Enemy.IsStanding &&
                    PendingEnemyCardEffect == null &&
                    _pendingEnemyDemonContractInteraction == null;
            if (!canAcceptAction ||
                !TryGetActiveDemonContract(
                    ownerSide,
                    sourceContractCardId,
                    DemonContractKind.Mammon,
                    out ActiveDemonContract activeContract) ||
                !_demonContractResolver.CanOwnerRerollMammon(
                    this,
                    activeContract))
            {
                return false;
            }

            RecordPublicAction(
                ownerSide,
                PublicCombatActionType.DemonContract,
                activeContract.Definition.Key,
                activeContract.SourceCardId);
            MammonRerollResult result = suppliedDieValue.HasValue
                ? _demonContractResolver.RerollMammon(
                    this,
                    activeContract,
                    suppliedDieValue.Value)
                : _demonContractResolver.RerollMammon(
                    this,
                    activeContract);
            if (result.OwnerBusted)
            {
                OwnerBustHandlingResult handling =
                    ownerSide == CombatantSide.Player
                        ? HandlePlayerBust(() =>
                            CompleteMammonRerollAction(ownerSide))
                        : HandleEnemyBust(() =>
                            CompleteMammonRerollAction(ownerSide));
                if (handling == OwnerBustHandlingResult.NotHandled)
                {
                    LastDemonContractEffectResult =
                        new DemonContractEffectResult(
                            triggered: true,
                            bustedTarget: ownerSide,
                            paidSoulCost: 0);
                    RaiseStepped();
                    NotifyNormalTurnEnded(ownerSide);
                    CompleteRound(RoundResolver.ResolveContractEffectBust(
                        RoundNumber,
                        playerIsTarget: ownerSide == CombatantSide.Player));
                    return true;
                }

                if (handling != OwnerBustHandlingResult.Prevented)
                {
                    return true;
                }

                LastDemonContractEffectResult = new DemonContractEffectResult(
                    triggered: true,
                    bustedTarget: null,
                    paidSoulCost: 0);
                RaiseStepped();
            }
            else
            {
                RaiseStepped();
            }

            CompleteMammonRerollAction(ownerSide);
            return true;
        }

        private void CompleteMammonRerollAction(CombatantSide ownerSide)
        {
            State = ownerSide == CombatantSide.Player
                ? CoreLoopState.PlayerTurn
                : CoreLoopState.EnemyTurn;
            if (ownerSide == CombatantSide.Player)
            {
                CompletePlayerActionAndRunEnemyTurn();
            }
            else
            {
                CompleteEnemyAction();
            }
        }

        internal void CreateBaphometPentagrams(
            ActiveDemonContract activeContract)
        {
            if (activeContract == null ||
                activeContract.Kind != DemonContractKind.Baphomet)
            {
                throw new ArgumentException(
                    "Baphomet pentagrams require an active Baphomet contract.",
                    nameof(activeContract));
            }

            _demonContractCardState.CreateBaphometWaves(
                GetParticipant(activeContract.OwnerSide),
                activeContract.OwnerSide,
                GetOpponent(activeContract.OwnerSide),
                activeContract.SourceCardId);
        }

        private IReadOnlyList<ActiveDemonContract> GetActiveDemonContracts(
            CombatantSide side)
        {
            switch (side)
            {
                case CombatantSide.Player:
                    return _activePlayerDemonContracts;
                case CombatantSide.Enemy:
                    return _activeEnemyDemonContracts;
                default:
                    throw new ArgumentOutOfRangeException(nameof(side));
            }
        }

        private void HandleFaceUpCardAdded(
            CombatantSide ownerSide,
            BlackjackCard addedCard)
        {
            if (!_demonContractResolver.BustsOwnerAfterFaceUpCardAdded(
                    this,
                    GetActiveDemonContracts(ownerSide),
                    ownerSide,
                    addedCard))
            {
                return;
            }

            if (ownerSide == CombatantSide.Player)
            {
                _playerAzazelBustPending = true;
            }
            else
            {
                _enemyAzazelBustPending = true;
            }
        }

        private bool HasPendingAzazelBust(CombatantSide ownerSide)
        {
            return ownerSide == CombatantSide.Player
                ? _playerAzazelBustPending
                : _enemyAzazelBustPending;
        }

        private bool ConsumePendingAzazelBust(CombatantSide ownerSide)
        {
            if (!HasPendingAzazelBust(ownerSide))
            {
                return false;
            }

            if (ownerSide == CombatantSide.Player)
            {
                _playerAzazelBustPending = false;
            }
            else
            {
                _enemyAzazelBustPending = false;
            }

            return true;
        }

        internal bool CanOwnerUseCardByDemonContract(
            CombatantSide ownerSide,
            BlackjackCard card)
        {
            IReadOnlyList<ActiveDemonContract> activeContracts =
                ownerSide == CombatantSide.Player
                    ? _activePlayerDemonContracts
                    : _activeEnemyDemonContracts;
            return _demonContractResolver.CanOwnerUseCard(
                this,
                activeContracts,
                ownerSide,
                card);
        }

        private bool TryBeginNextTurnStartChoice(CombatantSide ownerSide)
        {
            IReadOnlyList<ActiveDemonContract> activeContracts =
                GetActiveDemonContracts(ownerSide);
            ISet<int> resolvedContractIds = ownerSide == CombatantSide.Player
                ? _resolvedPlayerTurnStartContractIds
                : _resolvedEnemyTurnStartContractIds;
            if (!_demonContractResolver.TryGetOwnerTurnStartChoiceContract(
                    this,
                    activeContracts,
                    ownerSide,
                    resolvedContractIds,
                    out ActiveDemonContract activeContract))
            {
                return false;
            }

            resolvedContractIds.Add(activeContract.SourceCardId);
            if (ownerSide == CombatantSide.Player &&
                activeContract.Kind == DemonContractKind.Mammon)
            {
                return TryBeginNextTurnStartChoice(ownerSide);
            }

            PendingDemonContractInteraction pending;
            switch (activeContract.Kind)
            {
                case DemonContractKind.Mammon:
                    pending = CreateMammonRerollInteraction(
                        TakeNextDemonContractInteractionId(),
                        activeContract);
                    break;
                case DemonContractKind.Asmodeus:
                    pending = CreateAsmodeusTurnStartInteraction(
                        TakeNextDemonContractInteractionId(),
                        activeContract);
                    break;
                case DemonContractKind.Belial:
                    pending = CreateBelialTurnStartInteraction(
                        TakeNextDemonContractInteractionId(),
                        activeContract,
                        GetOpponent(ownerSide).Hand.GetPublicCards());
                    break;
                default:
                    throw new InvalidOperationException(
                        "Unsupported owner turn-start contract choice.");
            }

            SetPendingDemonContractInteraction(ownerSide, pending);
            State = ownerSide == CombatantSide.Player
                ? CoreLoopState.PlayerResolvingDemonContract
                : CoreLoopState.EnemyTurn;
            RaiseStepped();
            return true;
        }

        private bool TryResolveAsmodeusTurnStartChoice(
            CombatantSide ownerSide,
            PendingDemonContractInteraction pending,
            DemonContractOption selectedOption)
        {
            if (pending.Kind !=
                    DemonContractInteractionKind.AsmodeusForceOpponentHit ||
                pending.ContractKind != DemonContractKind.Asmodeus ||
                !pending.SourceContractCardId.HasValue ||
                !TryGetActiveDemonContract(
                    ownerSide,
                    pending.SourceContractCardId.Value,
                    DemonContractKind.Asmodeus,
                    out ActiveDemonContract activeContract))
            {
                return false;
            }

            if (selectedOption.OptionId ==
                AsmodeusDemonContractHandler.SkipForcedHitOptionId)
            {
                SetPendingDemonContractInteraction(ownerSide, pending: null);
                ResumeOwnerTurnAfterTurnStartChoice(ownerSide);
                return true;
            }

            BattleParticipant opponent = GetOpponent(ownerSide);
            if (selectedOption.OptionId !=
                    AsmodeusDemonContractHandler.ForceHitOptionId ||
                opponent.IsStanding ||
                !opponent.Deck.CanDraw(1))
            {
                return false;
            }

            SetPendingDemonContractInteraction(ownerSide, pending: null);
            RecordPublicAction(
                ownerSide,
                PublicCombatActionType.DemonContract,
                activeContract.Definition.Key,
                activeContract.SourceCardId);
            BlackjackCard drawnCard = opponent.Draw(faceUp: true);
            RaiseStepped();
            bool isWaitingForAutomaticChoice = TryBeginAutomaticCardEffect(
                GetOppositeSide(ownerSide),
                drawnCard,
                AutomaticCardContinuation.ForDemonContract(
                    ownerSide,
                    DemonContractKind.Asmodeus,
                    activeContract.SourceCardId,
                    drawnCard.Id),
                out AutomaticCardResult? immediateAutomaticResult);
            if (isWaitingForAutomaticChoice)
            {
                return true;
            }

            CompleteAsmodeusForcedHit(
                ownerSide,
                activeContract.SourceCardId,
                drawnCard.Id,
                immediateAutomaticResult?.SourceDisposition ??
                    AutomaticCardSourceDisposition.RetainFaceUp);
            return true;
        }

        private void CompleteAsmodeusForcedHit(
            CombatantSide ownerSide,
            int sourceContractCardId,
            int drawnCardId,
            AutomaticCardSourceDisposition sourceDisposition)
        {
            if (!TryGetActiveDemonContract(
                    ownerSide,
                    sourceContractCardId,
                    DemonContractKind.Asmodeus,
                    out _))
            {
                throw new InvalidOperationException(
                    "Asmodeus forced hit lost its active contract.");
            }

            _ = drawnCardId;
            _ = sourceDisposition;
            CombatantSide targetSide = GetOppositeSide(ownerSide);
            BattleParticipant target = GetParticipant(targetSide);
            bool azazelBust = ConsumePendingAzazelBust(targetSide);
            bool numericBust = target.VisibleHandValue.IsBust;
            if (azazelBust || numericBust)
            {
                OwnerBustHandlingResult handling = targetSide == CombatantSide.Player
                    ? HandlePlayerBust(() =>
                        ResumeOwnerTurnAfterAsmodeusPendingBust(ownerSide))
                    : HandleEnemyBust(() =>
                        ResumeOwnerTurnAfterAsmodeusPendingBust(ownerSide));
                if (handling != OwnerBustHandlingResult.NotHandled &&
                    handling != OwnerBustHandlingResult.Prevented)
                {
                    return;
                }

                bool bustPrevented =
                    handling == OwnerBustHandlingResult.Prevented;
                LastDemonContractEffectResult = new DemonContractEffectResult(
                    triggered: true,
                    bustedTarget: bustPrevented
                        ? null
                        : (CombatantSide?)targetSide,
                    paidSoulCost: 0);
                RaiseStepped();
                if (!bustPrevented)
                {
                    NotifyNormalTurnEnded(ownerSide);
                    CompleteRound(azazelBust
                        ? RoundResolver.ResolveContractEffectBust(
                            RoundNumber,
                            playerIsTarget: targetSide == CombatantSide.Player)
                        : RoundResolver.ResolveNumericBust(
                            RoundNumber,
                            playerIsTarget: targetSide == CombatantSide.Player));
                    return;
                }
            }

            if (TryResolveBaphometExhaustion(
                    () => ResumeOwnerTurnAfterAsmodeusPendingBust(ownerSide)))
            {
                return;
            }

            ResumeOwnerTurnAfterTurnStartChoice(ownerSide);
        }

        private void ResumeOwnerTurnAfterTurnStartChoice(CombatantSide ownerSide)
        {
            if (TryBeginNextTurnStartChoice(ownerSide))
            {
                return;
            }

            State = ownerSide == CombatantSide.Player
                ? CoreLoopState.PlayerTurn
                : CoreLoopState.EnemyTurn;
            RaiseStepped();
        }

        private void ResumeOwnerTurnAfterAsmodeusPendingBust(
            CombatantSide ownerSide)
        {
            ResumeOwnerTurnAfterTurnStartChoice(ownerSide);
            if (ownerSide == CombatantSide.Enemy)
            {
                ContinueEnemyTurnLoop();
            }
        }

        private bool TryResolvePaimonExileChoice(
            CombatantSide ownerSide,
            PendingDemonContractInteraction pending,
            DemonContractOption selectedOption)
        {
            PendingPaimonExileResolution resolution =
                _pendingPaimonExileResolution;
            if (resolution == null ||
                resolution.ActiveContract.OwnerSide != ownerSide ||
                pending.ContractKind != DemonContractKind.Paimon ||
                pending.SourceContractCardId !=
                    resolution.ActiveContract.SourceCardId)
            {
                return false;
            }

            if (pending.Kind == DemonContractInteractionKind.PaimonChooseDeck)
            {
                BattleParticipant chosenDeckOwner;
                CombatantSide chosenDeckSide;
                if (selectedOption.OptionId ==
                    PaimonDemonContractHandler.OwnerDeckOptionId)
                {
                    chosenDeckOwner = GetParticipant(ownerSide);
                    chosenDeckSide = ownerSide;
                }
                else if (selectedOption.OptionId ==
                    PaimonDemonContractHandler.OpponentDeckOptionId)
                {
                    chosenDeckOwner = GetOpponent(ownerSide);
                    chosenDeckSide = GetOppositeSide(ownerSide);
                }
                else
                {
                    return false;
                }

                if (!chosenDeckOwner.Deck.CanDraw(
                    PaimonDemonContractHandler.PeekCardCount))
                {
                    return false;
                }

                IReadOnlyList<BlackjackCard> peekedCards =
                    chosenDeckOwner.Deck.TakeTop(
                        PaimonDemonContractHandler.PeekCardCount);
                resolution.ChosenDeckOwner = chosenDeckOwner;
                resolution.ChosenDeckSide = chosenDeckSide;
                resolution.PeekedCards = peekedCards;
                SetPendingDemonContractInteraction(
                    ownerSide,
                    CreatePaimonCardChoiceInteraction(
                        TakeNextDemonContractInteractionId(),
                        resolution.ActiveContract,
                        peekedCards));
                RaiseStepped();
                return true;
            }

            if (pending.Kind !=
                    DemonContractInteractionKind.PaimonChooseExileCard ||
                resolution.ChosenDeckOwner == null ||
                resolution.PeekedCards == null)
            {
                return false;
            }

            BlackjackCard exiledCard = null;
            if (selectedOption.OptionId !=
                PaimonDemonContractHandler.SkipExileOptionId)
            {
                if (!selectedOption.ContractCardId.HasValue)
                {
                    return false;
                }

                foreach (BlackjackCard card in resolution.PeekedCards)
                {
                    if (card.Id == selectedOption.ContractCardId.Value)
                    {
                        exiledCard = card;
                        break;
                    }
                }

                if (exiledCard == null)
                {
                    return false;
                }
            }

            var returningCards = new List<BlackjackCard>(
                resolution.PeekedCards.Count);
            foreach (BlackjackCard card in resolution.PeekedCards)
            {
                if (!ReferenceEquals(card, exiledCard))
                {
                    returningCards.Add(card);
                }
            }

            resolution.ChosenDeckOwner.Deck.ReturnToTop(returningCards);
            if (exiledCard != null)
            {
                _demonContractCardState.TrackPaimonExile(
                    resolution.ChosenDeckOwner,
                    exiledCard);
            }

            RoundResolution roundResolution = resolution.RoundResolution;
            SetPendingDemonContractInteraction(ownerSide, pending: null);
            _pendingPaimonExileResolution = null;
            LastDemonContractEffectResult = new DemonContractEffectResult(
                triggered: true,
                bustedTarget: null,
                paidSoulCost: 0);
            RaiseStepped();
            if (TryResolveBaphometExhaustion(
                    () => CompleteRound(roundResolution)))
            {
                return true;
            }

            CompleteRound(roundResolution);
            return true;
        }

        private bool TryResolveBelialTurnStartChoice(
            CombatantSide ownerSide,
            PendingDemonContractInteraction pending,
            DemonContractOption selectedOption)
        {
            if (pending.Kind !=
                    DemonContractInteractionKind.BelialChooseOpponentCard ||
                pending.ContractKind != DemonContractKind.Belial ||
                !pending.SourceContractCardId.HasValue ||
                !TryGetActiveDemonContract(
                    ownerSide,
                    pending.SourceContractCardId.Value,
                    DemonContractKind.Belial,
                    out ActiveDemonContract activeContract))
            {
                return false;
            }

            if (selectedOption.OptionId ==
                BelialDemonContractHandler.SkipTransferOptionId)
            {
                SetPendingDemonContractInteraction(ownerSide, pending: null);
                ResumeOwnerTurnAfterTurnStartChoice(ownerSide);
                return true;
            }

            if (!selectedOption.ContractCardId.HasValue ||
                !_demonContractCardState.TryTransferFaceUpCard(
                    GetOpponent(ownerSide),
                    GetParticipant(ownerSide),
                    selectedOption.ContractCardId.Value,
                    activeContract.SourceCardId,
                    out BlackjackCard transferredCard))
            {
                return false;
            }

            SetPendingDemonContractInteraction(ownerSide, pending: null);
            RecordPublicAction(
                ownerSide,
                PublicCombatActionType.DemonContract,
                activeContract.Definition.Key,
                activeContract.SourceCardId);
            RaiseStepped();
            ResolveBelialTransferredCardArrival(
                ownerSide,
                activeContract.SourceCardId,
                transferredCard.Id);
            return true;
        }

        private void ResolveBelialTransferredCardArrival(
            CombatantSide ownerSide,
            int sourceContractCardId,
            int transferredCardId)
        {
            if (ConsumePendingAzazelBust(ownerSide))
            {
                OwnerBustHandlingResult handling = ownerSide ==
                        CombatantSide.Player
                    ? HandlePlayerBust(() =>
                        BeginBelialTransferredCardUse(
                            ownerSide,
                            sourceContractCardId,
                            transferredCardId))
                    : HandleEnemyBust(() =>
                        BeginBelialTransferredCardUse(
                            ownerSide,
                            sourceContractCardId,
                            transferredCardId));
                if (handling == OwnerBustHandlingResult.NotHandled)
                {
                    LastDemonContractEffectResult =
                        new DemonContractEffectResult(
                            triggered: true,
                            bustedTarget: ownerSide,
                            paidSoulCost: 0);
                    NotifyNormalTurnEnded(ownerSide);
                    CompleteRound(RoundResolver.ResolveContractEffectBust(
                        RoundNumber,
                        playerIsTarget:
                            ownerSide == CombatantSide.Player));
                    return;
                }

                if (handling != OwnerBustHandlingResult.Prevented)
                {
                    return;
                }

                LastDemonContractEffectResult =
                    new DemonContractEffectResult(
                        triggered: true,
                        bustedTarget: null,
                        paidSoulCost: 0);
                RaiseStepped();
            }

            BeginBelialTransferredCardUse(
                ownerSide,
                sourceContractCardId,
                transferredCardId);
        }

        private void BeginBelialTransferredCardUse(
            CombatantSide ownerSide,
            int sourceContractCardId,
            int transferredCardId)
        {
            if (State == CoreLoopState.BattleEnded)
            {
                return;
            }

            if (!TryGetActiveDemonContract(
                    ownerSide,
                    sourceContractCardId,
                    DemonContractKind.Belial,
                    out ActiveDemonContract activeContract))
            {
                throw new InvalidOperationException(
                    "Belial forced card use lost its active contract.");
            }

            BattleParticipant owner = GetParticipant(ownerSide);
            if (!owner.Hand.TryGetCard(
                    transferredCardId,
                    out BlackjackCard transferredCard))
            {
                ResumeOwnerTurnAfterTurnStartChoice(ownerSide);
                return;
            }

            if (transferredCard.Definition.Activation ==
                CardActivationKind.Automatic)
            {
                bool waiting = TryBeginAutomaticCardEffect(
                    ownerSide,
                    transferredCard,
                    AutomaticCardContinuation.ForDemonContract(
                        ownerSide,
                        DemonContractKind.Belial,
                        activeContract.SourceCardId,
                        transferredCard.Id),
                    out AutomaticCardResult? immediateResult);
                if (waiting)
                {
                    return;
                }

                CompleteBelialTransferredCardUse(
                    ownerSide,
                    sourceContractCardId,
                    transferredCardId,
                    immediateResult?.SourceDisposition ??
                        AutomaticCardSourceDisposition.RetainFaceUp);
                return;
            }

            if (transferredCard.Definition.Activation !=
                    CardActivationKind.Manual ||
                transferredCard.Definition.Effect == CardEffectKind.None)
            {
                CompleteBelialTransferredCardUse(
                    ownerSide,
                    sourceContractCardId,
                    transferredCardId,
                    AutomaticCardSourceDisposition.RetainFaceUp);
                return;
            }

            if (!transferredCard.TryBeginUse())
            {
                throw new InvalidOperationException(
                    "Belial transferred card could not begin immediate use.");
            }

            _belialForcedCardEffectContinuation =
                new BelialForcedCardEffectContinuation(
                    ownerSide,
                    sourceContractCardId,
                    transferredCardId);
            var context = new CardEffectContext(
                this,
                ownerSide,
                transferredCard);
            _activeCardEffectContext = context;
            _activeCardEffectActorSide = ownerSide;
            RecordPublicAction(
                ownerSide,
                PublicCombatActionType.UseCard,
                transferredCard.DefinitionKey,
                transferredCard.Id);
            CardEffectApplicationResult applicationResult =
                ApplyCardEffectStep(_cardEffectResolver.Begin(context));
            if (applicationResult == CardEffectApplicationResult.Completed)
            {
                CompleteCardEffectOwnerAction(ownerSide);
            }
        }

        private void CompleteBelialTransferredCardUse(
            CombatantSide ownerSide,
            int sourceContractCardId,
            int transferredCardId,
            AutomaticCardSourceDisposition sourceDisposition)
        {
            if (State == CoreLoopState.BattleEnded)
            {
                return;
            }

            if (!TryGetActiveDemonContract(
                    ownerSide,
                    sourceContractCardId,
                    DemonContractKind.Belial,
                    out _))
            {
                throw new InvalidOperationException(
                    "Belial transferred card completion lost its active contract.");
            }

            _ = transferredCardId;
            _ = sourceDisposition;
            BattleParticipant owner = GetParticipant(ownerSide);
            if (owner.VisibleHandValue.IsBust)
            {
                OwnerBustHandlingResult handling = ownerSide ==
                        CombatantSide.Player
                    ? HandlePlayerBust(() =>
                        ResumeOwnerTurnAfterTurnStartChoice(ownerSide))
                    : HandleEnemyBust(() =>
                        ResumeOwnerTurnAfterBelialPendingBust(ownerSide));
                if (handling == OwnerBustHandlingResult.NotHandled)
                {
                    LastDemonContractEffectResult =
                        new DemonContractEffectResult(
                            triggered: true,
                            bustedTarget: ownerSide,
                            paidSoulCost: 0);
                    NotifyNormalTurnEnded(ownerSide);
                    CompleteRound(RoundResolver.ResolveNumericBust(
                        RoundNumber,
                        playerIsTarget:
                            ownerSide == CombatantSide.Player));
                    return;
                }

                if (handling != OwnerBustHandlingResult.Prevented)
                {
                    return;
                }
            }

            if (TryResolveBaphometExhaustion(
                    () => ResumeOwnerTurnAfterBelialPendingBust(ownerSide)))
            {
                return;
            }

            ResumeOwnerTurnAfterTurnStartChoice(ownerSide);
            if (ownerSide == CombatantSide.Enemy)
            {
                ContinueEnemyTurnLoop();
            }
        }

        private void ResumeOwnerTurnAfterBelialPendingBust(
            CombatantSide ownerSide)
        {
            ResumeOwnerTurnAfterTurnStartChoice(ownerSide);
            if (ownerSide == CombatantSide.Enemy)
            {
                ContinueEnemyTurnLoop();
            }
        }

        private bool TryResolveBeelzebubDiscardChoice(
            CombatantSide ownerSide,
            PendingDemonContractInteraction pending,
            DemonContractOption selectedOption)
        {
            PendingBeelzebubBustResolution resolution =
                _pendingBeelzebubBustResolution;
            if (resolution == null ||
                resolution.OwnerSide != ownerSide ||
                pending.ContractKind != DemonContractKind.Beelzebub ||
                !pending.SourceContractCardId.HasValue ||
                pending.SourceContractCardId.Value !=
                    resolution.ActiveContract.SourceCardId ||
                !selectedOption.ContractCardId.HasValue)
            {
                return false;
            }

            int selectedCardId = selectedOption.ContractCardId.Value;
            if (pending.Kind ==
                DemonContractInteractionKind.BeelzebubChooseOwnerCard)
            {
                BattleParticipant owner = GetParticipant(ownerSide);
                if (!owner.Hand.TryGetCard(
                        selectedCardId,
                        out BlackjackCard ownerCard) ||
                    !ownerCard.IsFaceUp ||
                    owner.Hand.IsHiddenCard(selectedCardId))
                {
                    return false;
                }

                resolution.OwnerCardId = selectedCardId;
                IReadOnlyList<BlackjackCard> opponentCards =
                    GetOpponent(ownerSide).Hand.GetPublicCards();
                if (opponentCards.Count == 0)
                {
                    return CompleteBeelzebubBustResolution(
                        ownerSide,
                        resolution,
                        selectedCardId,
                        opponentCardId: null);
                }

                PendingDemonContractInteraction next =
                    CreateBeelzebubDiscardInteraction(
                        TakeNextDemonContractInteractionId(),
                        resolution.ActiveContract,
                        opponentCards,
                        choosingOwnerCard: false);
                SetPendingDemonContractInteraction(ownerSide, next);
                RaiseStepped();
                return true;
            }

            if (pending.Kind !=
                    DemonContractInteractionKind.BeelzebubChooseOpponentCard)
            {
                return false;
            }

            return CompleteBeelzebubBustResolution(
                ownerSide,
                resolution,
                resolution.OwnerCardId,
                selectedCardId);
        }

        private bool CompleteBeelzebubBustResolution(
            CombatantSide ownerSide,
            PendingBeelzebubBustResolution resolution,
            int? ownerCardId,
            int? opponentCardId)
        {
            if (!_demonContractResolver.TryCompleteOwnerBustReplacement(
                    this,
                    resolution.ActiveContract,
                    ownerCardId,
                    opponentCardId))
            {
                return false;
            }

            SetPendingDemonContractInteraction(ownerSide, pending: null);
            _pendingBeelzebubBustResolution = null;
            State = resolution.ResumeState;
            RaiseStepped();
            resolution.Resume();
            return true;
        }

        private bool TryBeginEnemyActiveDemonContractAction(
            int sourceContractCardId)
        {
            foreach (ActiveDemonContract activeContract in
                _activeEnemyDemonContracts)
            {
                if (activeContract.OwnerSide != CombatantSide.Enemy ||
                    activeContract.SourceCardId != sourceContractCardId)
                {
                    continue;
                }

                switch (activeContract.Kind)
                {
                    case DemonContractKind.Mammon:
                        return TryBeginMammonReroll(
                            CombatantSide.Enemy,
                            sourceContractCardId);
                    case DemonContractKind.Satan:
                        return TryBeginSatanContractAction(
                            CombatantSide.Enemy,
                            sourceContractCardId);
                    default:
                        return false;
                }
            }

            return false;
        }

        public bool TryBeginPlayerSatanContractAction(int sourceContractCardId)
        {
            return TryBeginSatanContractAction(
                CombatantSide.Player,
                sourceContractCardId);
        }

        private bool TryBeginSatanContractAction(
            CombatantSide ownerSide,
            int sourceContractCardId)
        {
            if (sourceContractCardId < 0)
            {
                return false;
            }

            bool canAcceptAction = ownerSide == CombatantSide.Player
                ? CanAcceptPlayerAction()
                : State == CoreLoopState.EnemyTurn &&
                    !Enemy.IsStanding &&
                    PendingEnemyCardEffect == null &&
                    _pendingEnemyDemonContractInteraction == null;
            if (!canAcceptAction ||
                !TryGetActiveDemonContract(
                    ownerSide,
                    sourceContractCardId,
                    DemonContractKind.Satan,
                    out ActiveDemonContract activeContract) ||
                !(activeContract.RuntimeState is SatanRuntimeState satanState))
            {
                return false;
            }

            BattleParticipant opponent = GetOpponent(ownerSide);
            if (satanState.CurrentFace == SatanContractFace.Upper)
            {
                if (!TryGetSingleHiddenCard(opponent, out _))
                {
                    return false;
                }

                PendingDemonContractInteraction pending =
                    CreateSatanFirstNumberInteraction(
                        TakeNextDemonContractInteractionId(),
                        sourceContractCardId);
                SetPendingDemonContractInteraction(ownerSide, pending);
                State = ownerSide == CombatantSide.Player
                    ? CoreLoopState.PlayerResolvingDemonContract
                    : CoreLoopState.EnemyTurn;
                RecordPublicAction(
                    ownerSide,
                    PublicCombatActionType.DemonContract,
                    activeContract.Definition.Key,
                    activeContract.SourceCardId);
                RaiseStepped();
                return true;
            }

            if (!opponent.Deck.CanDraw(1))
            {
                return false;
            }

            RecordPublicAction(
                ownerSide,
                PublicCombatActionType.DemonContract,
                activeContract.Definition.Key,
                activeContract.SourceCardId);
            _lastSatanForcedDrawActionOrdinal = _publicActionHistory.Count;
            int roundBeforeForcedDraw = RoundNumber;
            SatanContractFace faceBeforeForcedDraw = satanState.CurrentFace;
            BlackjackCard drawnCard = opponent.Draw(faceUp: true);
            RaiseStepped();
            bool isWaitingForAutomaticChoice = TryBeginAutomaticCardEffect(
                ownerSide == CombatantSide.Player
                    ? CombatantSide.Enemy
                    : CombatantSide.Player,
                drawnCard,
                AutomaticCardContinuation.ForDemonContract(
                    ownerSide,
                    DemonContractKind.Satan,
                    sourceContractCardId,
                    drawnCard.Id),
                out AutomaticCardResult? immediateAutomaticResult);
            if (isWaitingForAutomaticChoice)
            {
                return true;
            }

            if (State == CoreLoopState.BattleEnded ||
                RoundNumber != roundBeforeForcedDraw ||
                !TryGetActiveDemonContract(
                    ownerSide,
                    sourceContractCardId,
                    DemonContractKind.Satan,
                    out ActiveDemonContract currentContract) ||
                !(currentContract.RuntimeState is SatanRuntimeState currentState) ||
                currentState.CurrentFace != faceBeforeForcedDraw)
            {
                return true;
            }

            CompleteSatanLowerContractAction(
                ownerSide,
                sourceContractCardId,
                drawnCard.Id,
                immediateAutomaticResult?.SourceDisposition ??
                    AutomaticCardSourceDisposition.RetainFaceUp);
            return true;
        }

        private bool TryResolveSatanNumberChoice(
            CombatantSide ownerSide,
            PendingDemonContractInteraction pending,
            DemonContractOption selectedOption,
            out bool completedOwnerAction)
        {
            completedOwnerAction = false;
            if (pending.ContractKind != DemonContractKind.Satan ||
                !pending.SourceContractCardId.HasValue ||
                !selectedOption.NumericValue.HasValue ||
                !TryGetActiveDemonContract(
                    ownerSide,
                    pending.SourceContractCardId.Value,
                    DemonContractKind.Satan,
                    out ActiveDemonContract activeContract) ||
                !(activeContract.RuntimeState is SatanRuntimeState satanState) ||
                satanState.CurrentFace != SatanContractFace.Upper)
            {
                return false;
            }

            int selectedNumber = selectedOption.NumericValue.Value;
            if (pending.Kind == DemonContractInteractionKind.SatanDeclareFirstNumber)
            {
                PendingDemonContractInteraction second =
                    CreateSatanSecondNumberInteraction(
                        TakeNextDemonContractInteractionId(),
                        activeContract.SourceCardId,
                        selectedNumber);
                SetPendingDemonContractInteraction(ownerSide, second);
                RaiseStepped();
                return true;
            }

            if (pending.Kind !=
                    DemonContractInteractionKind.SatanDeclareSecondNumber ||
                !pending.ContextNumericValue.HasValue ||
                pending.ContextNumericValue.Value == selectedNumber ||
                !TryGetSingleHiddenCard(
                    GetOpponent(ownerSide),
                    out BlackjackCard hiddenCard))
            {
                return false;
            }

            return ResolveSatanNumberPair(
                ownerSide,
                satanState,
                hiddenCard,
                pending.ContextNumericValue.Value,
                selectedNumber,
                out completedOwnerAction);
        }

        private static bool ContainsSatanNumberOption(
            PendingDemonContractInteraction pending,
            int number)
        {
            if (number < 1 || number > 10)
            {
                return false;
            }

            foreach (DemonContractOption option in pending.Options)
            {
                if (option.NumericValue == number)
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryGetSatanUpperResolutionContext(
            CombatantSide ownerSide,
            PendingDemonContractInteraction pending,
            out SatanRuntimeState satanState,
            out BlackjackCard hiddenCard)
        {
            satanState = null;
            hiddenCard = null;
            if (pending.ContractKind != DemonContractKind.Satan ||
                !pending.SourceContractCardId.HasValue ||
                !TryGetActiveDemonContract(
                    ownerSide,
                    pending.SourceContractCardId.Value,
                    DemonContractKind.Satan,
                    out ActiveDemonContract activeContract) ||
                !(activeContract.RuntimeState is SatanRuntimeState activeState) ||
                activeState.CurrentFace != SatanContractFace.Upper ||
                !TryGetSingleHiddenCard(
                    GetOpponent(ownerSide),
                    out hiddenCard))
            {
                return false;
            }

            satanState = activeState;
            return true;
        }

        private bool ResolveSatanNumberPair(
            CombatantSide ownerSide,
            SatanRuntimeState satanState,
            BlackjackCard hiddenCard,
            int firstNumber,
            int secondNumber,
            out bool completedOwnerAction)
        {
            completedOwnerAction = false;
            bool succeeded = hiddenCard.Rank == firstNumber ||
                hiddenCard.Rank == secondNumber;
            SetPendingDemonContractInteraction(ownerSide, pending: null);
            satanState.CompleteCurrentFaceAbility();
            OwnerBustHandlingResult handling = OwnerBustHandlingResult.NotHandled;
            if (succeeded)
            {
                CombatantSide targetSide = GetOppositeSide(ownerSide);
                handling = targetSide == CombatantSide.Player
                    ? HandlePlayerBust(() =>
                        CompleteSatanAfterPreventedTargetBust(ownerSide))
                    : HandleEnemyBust(() =>
                        CompleteSatanAfterPreventedTargetBust(ownerSide));
            }

            if (handling != OwnerBustHandlingResult.NotHandled &&
                handling != OwnerBustHandlingResult.Prevented)
            {
                completedOwnerAction = true;
                return true;
            }

            bool bustPrevented =
                handling == OwnerBustHandlingResult.Prevented;
            LastDemonContractEffectResult = new DemonContractEffectResult(
                triggered: true,
                bustedTarget: succeeded && !bustPrevented
                    ? (CombatantSide?)GetOppositeSide(ownerSide)
                    : null,
                paidSoulCost: 0);
            RaiseStepped();

            if (succeeded && !bustPrevented)
            {
                NotifyNormalTurnEnded(ownerSide);
                CompleteRound(RoundResolver.ResolveContractEffectBust(
                    RoundNumber,
                    playerIsTarget: ownerSide == CombatantSide.Enemy));
                completedOwnerAction = true;
                return true;
            }

            State = ownerSide == CombatantSide.Player
                ? CoreLoopState.PlayerTurn
                : CoreLoopState.EnemyTurn;
            CompleteSatanContractAction(ownerSide);
            completedOwnerAction = true;
            return true;
        }

        private void CompleteSatanAfterPreventedTargetBust(
            CombatantSide ownerSide)
        {
            State = ownerSide == CombatantSide.Player
                ? CoreLoopState.PlayerTurn
                : CoreLoopState.EnemyTurn;
            CompleteSatanContractAction(ownerSide);
        }

        private void CompleteSatanLowerContractAction(
            CombatantSide ownerSide,
            int sourceContractCardId,
            int drawnCardId,
            AutomaticCardSourceDisposition sourceDisposition)
        {
            if (!TryGetActiveDemonContract(
                    ownerSide,
                    sourceContractCardId,
                    DemonContractKind.Satan,
                    out ActiveDemonContract activeContract) ||
                !(activeContract.RuntimeState is SatanRuntimeState satanState) ||
                satanState.CurrentFace != SatanContractFace.Lower)
            {
                throw new InvalidOperationException(
                    "Satan lower power lost its active lower-face contract.");
            }

            satanState.CompleteCurrentFaceAbility();

            CombatantSide opponentSide = GetOppositeSide(ownerSide);
            BattleParticipant opponent = GetParticipant(opponentSide);
            bool azazelBust = ConsumePendingAzazelBust(opponentSide);
            bool busted = azazelBust || opponent.VisibleHandValue.IsBust;
            if (!busted &&
                sourceDisposition == AutomaticCardSourceDisposition.RetainFaceUp &&
                !opponent.TryDiscardCard(drawnCardId))
            {
                throw new InvalidOperationException(
                    "Satan lower power could not discard its safe forced draw.");
            }

            OwnerBustHandlingResult handling = OwnerBustHandlingResult.NotHandled;
            if (busted)
            {
                handling = opponentSide == CombatantSide.Player
                    ? HandlePlayerBust(() =>
                        CompleteSatanAfterPreventedTargetBust(ownerSide))
                    : HandleEnemyBust(() =>
                        CompleteSatanAfterPreventedTargetBust(ownerSide));
                if (handling != OwnerBustHandlingResult.NotHandled &&
                    handling != OwnerBustHandlingResult.Prevented)
                {
                    return;
                }
            }

            bool bustPrevented =
                handling == OwnerBustHandlingResult.Prevented;
            LastDemonContractEffectResult = new DemonContractEffectResult(
                triggered: true,
                bustedTarget: busted && !bustPrevented
                    ? (CombatantSide?)opponentSide
                    : null,
                paidSoulCost: 0);
            RaiseStepped();

            if (busted && !bustPrevented)
            {
                NotifyNormalTurnEnded(ownerSide);
                CompleteRound(azazelBust
                    ? RoundResolver.ResolveContractEffectBust(
                        RoundNumber,
                        playerIsTarget: opponentSide == CombatantSide.Player)
                    : RoundResolver.ResolveNumericBust(
                        RoundNumber,
                        playerIsTarget: opponentSide == CombatantSide.Player));
                return;
            }

            if (TryResolveBaphometExhaustion(
                    () => CompleteSatanAfterPreventedTargetBust(ownerSide),
                    ownerSide))
            {
                return;
            }

            State = ownerSide == CombatantSide.Player
                ? CoreLoopState.PlayerTurn
                : CoreLoopState.EnemyTurn;
            CompleteSatanContractAction(ownerSide);
        }

        private void CompleteSatanContractAction(CombatantSide ownerSide)
        {
            if (ownerSide == CombatantSide.Player)
            {
                CompletePlayerActionAndRunEnemyTurn();
            }
            else
            {
                CompleteEnemyAction();
            }
        }

        private bool TryGetActiveDemonContract(
            CombatantSide ownerSide,
            int sourceContractCardId,
            DemonContractKind kind,
            out ActiveDemonContract activeContract)
        {
            IReadOnlyList<ActiveDemonContract> contracts =
                ownerSide == CombatantSide.Player
                    ? _activePlayerDemonContracts
                    : _activeEnemyDemonContracts;
            foreach (ActiveDemonContract candidate in contracts)
            {
                if (candidate.OwnerSide == ownerSide &&
                    candidate.SourceCardId == sourceContractCardId &&
                    candidate.Kind == kind)
                {
                    activeContract = candidate;
                    return true;
                }
            }

            activeContract = null;
            return false;
        }

        private static bool TryGetSingleHiddenCard(
            BattleParticipant participant,
            out BlackjackCard hiddenCard)
        {
            return participant.Hand.TryGetSingleHiddenCard(out hiddenCard);
        }

        private void SetPendingDemonContractInteraction(
            CombatantSide ownerSide,
            PendingDemonContractInteraction pending)
        {
            if (ownerSide == CombatantSide.Player)
            {
                _pendingPlayerDemonContractInteraction = pending;
            }
            else
            {
                _pendingEnemyDemonContractInteraction = pending;
            }
        }

        private static CombatantSide GetOppositeSide(CombatantSide side)
        {
            return side == CombatantSide.Player
                ? CombatantSide.Enemy
                : CombatantSide.Player;
        }

        private void ResolvePendingEnemyAutomaticChoices()
        {
            if (_enemyAutomaticCardDecisionPolicy == null ||
                _isResolvingEnemyAutomaticChoice)
            {
                return;
            }

            _isResolvingEnemyAutomaticChoice = true;
            try
            {
                const int maximumChoiceCount = 16;
                for (int choiceIndex = 0;
                    choiceIndex < maximumChoiceCount;
                    choiceIndex++)
                {
                    PendingAutomaticCardInteraction pending =
                        _pendingAutomaticCardInteraction;
                    if (pending == null ||
                        pending.DecisionSide != CombatantSide.Enemy)
                    {
                        return;
                    }

                    AutomaticCardDecisionObservation observation =
                        AutomaticCardDecisionObservationFactory.Create(
                            this,
                            pending);
                    AutomaticCardDecision decision;
                    try
                    {
                        decision =
                            _enemyAutomaticCardDecisionPolicy.Decide(
                                observation);
                    }
                    catch (Exception)
                    {
                        decision = new AutomaticCardDecision(
                            pending.Options[0].OptionId,
                            "policy-error-safe-first");
                    }

                    if (!pending.TryGetOption(
                            decision.OptionId,
                            out AutomaticCardChoiceOption _))
                    {
                        decision = new AutomaticCardDecision(
                            pending.Options[0].OptionId,
                            "invalid-policy-option-safe-first");
                    }

                    LastEnemyAutomaticCardDecision = decision;
                    if (!TryResolveAutomaticCardChoice(
                            CombatantSide.Enemy,
                            pending.InteractionId,
                            decision.OptionId))
                    {
                        throw new InvalidOperationException(
                            "Validated enemy automatic card choice could not be resolved.");
                    }
                }

                if (_pendingAutomaticCardInteraction?.DecisionSide ==
                    CombatantSide.Enemy)
                {
                    throw new InvalidOperationException(
                        "Enemy automatic card choices exceeded the resolution limit.");
                }
            }
            finally
            {
                _isResolvingEnemyAutomaticChoice = false;
            }
        }

        private void ResumeAfterAutomaticCard(
            AutomaticCardContinuation continuation,
            AutomaticCardResult result)
        {
            switch (continuation.Kind)
            {
                case AutomaticCardContinuationKind.PlayerHit:
                    State = CoreLoopState.PlayerTurn;
                    CompletePlayerHitAfterAutomaticCard();
                    return;
                case AutomaticCardContinuationKind.EnemyHit:
                    State = CoreLoopState.EnemyTurn;
                    CompleteEnemyHitAfterAutomaticCard();
                    return;
                case AutomaticCardContinuationKind.CardEffect:
                    if (_activeCardEffectContext == null ||
                        _activeCardEffectActorSide != continuation.ActorSide)
                    {
                        throw new InvalidOperationException(
                            "Automatic card continuation lost its parent card effect.");
                    }

                    State = continuation.ActorSide == CombatantSide.Player
                        ? CoreLoopState.PlayerResolvingCardEffect
                        : CoreLoopState.EnemyTurn;
                    CardEffectStep cardEffectStep =
                        _cardEffectResolver.ResumeAfterAutomaticCard(
                            _activeCardEffectContext,
                            continuation.CardEffectContinuation,
                            result);
                    CardEffectApplicationResult applicationResult =
                        ApplyCardEffectStep(cardEffectStep);
                    if (applicationResult !=
                        CardEffectApplicationResult.Completed)
                    {
                        return;
                    }

                    if (continuation.ActorSide == CombatantSide.Player)
                    {
                        CompleteCardEffectOwnerAction(CombatantSide.Player);
                    }
                    else
                    {
                        CompleteCardEffectOwnerAction(CombatantSide.Enemy);
                    }

                    return;
                case AutomaticCardContinuationKind.DemonContract:
                    if (!continuation.DemonContractKind.HasValue ||
                        !continuation.SourceContractCardId.HasValue ||
                        !continuation.EnteredCardId.HasValue ||
                        continuation.EnteredCardId.Value != result.SourceCardId)
                    {
                        throw new InvalidOperationException(
                            "Automatic card continuation lost its parent demon contract.");
                    }

                    switch (continuation.DemonContractKind.Value)
                    {
                        case DemonContractKind.Satan:
                            CompleteSatanLowerContractAction(
                                continuation.ActorSide,
                                continuation.SourceContractCardId.Value,
                                continuation.EnteredCardId.Value,
                                result.SourceDisposition);
                            break;
                        case DemonContractKind.Asmodeus:
                            CompleteAsmodeusForcedHit(
                                continuation.ActorSide,
                                continuation.SourceContractCardId.Value,
                                continuation.EnteredCardId.Value,
                                result.SourceDisposition);
                            break;
                        case DemonContractKind.Belial:
                            CompleteBelialTransferredCardUse(
                                continuation.ActorSide,
                                continuation.SourceContractCardId.Value,
                                continuation.EnteredCardId.Value,
                                result.SourceDisposition);
                            break;
                        default:
                            throw new InvalidOperationException(
                                "Automatic card continuation has an unsupported demon contract.");
                    }
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(continuation));
            }
        }

        private int TakeNextAutomaticCardInteractionId()
        {
            int interactionId = _nextAutomaticCardInteractionId;
            _nextAutomaticCardInteractionId = checked(
                _nextAutomaticCardInteractionId + 1);
            return interactionId;
        }

        private bool TryBeginCardUse(CombatantSide actorSide, int cardId)
        {
            if (!EvaluateCardUse(actorSide, cardId).CanUse)
            {
                return false;
            }

            BattleParticipant actor = GetParticipant(actorSide);
            if (!actor.Hand.TryGetCard(cardId, out BlackjackCard card))
            {
                return false;
            }

            var context = new CardEffectContext(this, actorSide, card);
            if (!card.TryBeginUse())
            {
                throw new InvalidOperationException("Validated card could not begin use.");
            }

            card.Reveal();
            _activeCardEffectContext = context;
            _activeCardEffectActorSide = actorSide;
            RecordPublicAction(
                actorSide,
                PublicCombatActionType.UseCard,
                card.DefinitionKey,
                card.Id);
            CardEffectStep step = _cardEffectResolver.Begin(context);

            CardEffectApplicationResult applicationResult = ApplyCardEffectStep(step);
            if (applicationResult == CardEffectApplicationResult.Completed &&
                _belialForcedCardEffectContinuation != null)
            {
                CompleteCardEffectOwnerAction(actorSide);
            }
            else if (actorSide == CombatantSide.Player &&
                applicationResult == CardEffectApplicationResult.Completed)
            {
                CompletePlayerActionAndRunEnemyTurn();
            }

            return true;
        }

        private bool TryResolveCardChoice(CombatantSide actorSide, int optionId)
        {
            CoreLoopState expectedState = actorSide == CombatantSide.Player
                ? CoreLoopState.PlayerResolvingCardEffect
                : CoreLoopState.EnemyTurn;
            if (State != expectedState ||
                _activeCardEffectActorSide != actorSide ||
                _pendingCardEffect == null ||
                _activeCardEffectContext == null ||
                !_pendingCardEffect.TryGetOption(
                    optionId,
                    out CardEffectChoiceOption selectedOption))
            {
                return false;
            }

            CardEffectStep step = _cardEffectResolver.ResolveChoice(
                _activeCardEffectContext,
                _pendingCardEffect,
                selectedOption);
            CardEffectApplicationResult applicationResult = ApplyCardEffectStep(step);
            if (applicationResult == CardEffectApplicationResult.Completed &&
                (_activeAzazelCardEffectSequence != null ||
                    _belialForcedCardEffectContinuation != null))
            {
                CompleteCardEffectOwnerAction(actorSide);
            }
            else if (actorSide == CombatantSide.Player &&
                applicationResult == CardEffectApplicationResult.Completed)
            {
                CompletePlayerActionAndRunEnemyTurn();
            }

            return true;
        }

        private CardEffectApplicationResult ApplyCardEffectStep(CardEffectStep step)
        {
            if (step == null)
            {
                throw new InvalidOperationException("Card effect handler returned no step.");
            }

            BlackjackCard sourceCard = _activeCardEffectContext?.SourceCard ??
                throw new InvalidOperationException("Card effect has no active source card.");

            if (step.PendingEffect != null)
            {
                if (step.PendingEffect.SourceCardId != sourceCard.Id ||
                    step.PendingEffect.EffectKind != sourceCard.Definition.Effect)
                {
                    throw new InvalidOperationException(
                        "Pending card effect does not match the active source card.");
                }

                _pendingCardEffect = step.PendingEffect;
                State = _activeCardEffectActorSide == CombatantSide.Player
                    ? CoreLoopState.PlayerResolvingCardEffect
                    : CoreLoopState.EnemyTurn;
                return CardEffectApplicationResult.Pending;
            }

            if (step.Continuation != null)
            {
                AutomaticCardContinuation automaticContinuation =
                    _automaticCardContinuation;
                if (State != CoreLoopState.ResolvingAutomaticCardEffect ||
                    _activeAutomaticCardEffectContext == null ||
                    automaticContinuation == null ||
                    automaticContinuation.Kind !=
                        AutomaticCardContinuationKind.CardEffect ||
                    automaticContinuation.ActorSide !=
                        _activeCardEffectActorSide ||
                    automaticContinuation.CardEffectContinuation.Kind !=
                        step.Continuation.Kind ||
                    automaticContinuation.CardEffectContinuation.EnteredCardId !=
                        step.Continuation.EnteredCardId)
                {
                    throw new InvalidOperationException(
                        "Card effect suspension does not match the pending automatic card.");
                }

                _pendingCardEffect = null;
                return CardEffectApplicationResult.Pending;
            }

            if (!step.Result.HasValue)
            {
                throw new InvalidOperationException("Card effect step is neither pending nor complete.");
            }

            CardEffectResult result = step.Result.Value;
            if (result.SourceCardId != sourceCard.Id ||
                result.EffectKind != sourceCard.Definition.Effect)
            {
                throw new InvalidOperationException(
                    "Card effect result does not match the active source card.");
            }

            RoundResolution? roundResolution = step.RoundResolution;
            CombatantSide? azazelBustSide = null;
            if (_playerAzazelBustPending)
            {
                azazelBustSide = CombatantSide.Player;
            }

            if (_enemyAzazelBustPending)
            {
                if (azazelBustSide.HasValue)
                {
                    throw new InvalidOperationException(
                        "A card effect cannot add duplicate cards to both owners in one step.");
                }

                azazelBustSide = CombatantSide.Enemy;
            }

            if (azazelBustSide.HasValue)
            {
                ConsumePendingAzazelBust(azazelBustSide.Value);
                LastDemonContractEffectResult = new DemonContractEffectResult(
                    triggered: true,
                    bustedTarget: azazelBustSide,
                    paidSoulCost: 0);
                result = new CardEffectResult(
                    result.SourceCardId,
                    result.EffectKind,
                    result.Succeeded,
                    endedRound: true,
                    result.TargetCardId);
                roundResolution = RoundResolver.ResolveContractEffectBust(
                    RoundNumber,
                    playerIsTarget:
                        azazelBustSide.Value == CombatantSide.Player);
            }

            if (roundResolution.HasValue)
            {
                CardEffectResult resumedResult = new CardEffectResult(
                    result.SourceCardId,
                    result.EffectKind,
                    result.Succeeded,
                    endedRound: false,
                    result.TargetCardId);
                OwnerBustHandlingResult handling = HandleRoundBust(
                    roundResolution.Value,
                    () => ResumeCardEffectAfterBeelzebubBust(
                        sourceCard,
                        resumedResult),
                    beforeBustReplacementPublish: () =>
                    {
                        // A distinct beat, published before any bust-replacement contract
                        // (e.g. Beelzebub) can suspend this call for a player choice: without
                        // it, the weapon's own reveal (revolver/knife) would not be visible in
                        // any snapshot until the contract's interaction resolves, so the
                        // interception would appear to happen before the weapon's presentation
                        // ever played.
                        LastCardEffectResult = resumedResult;
                        LastCardEffectActorSide = _activeCardEffectActorSide;
                    });
                if (handling == OwnerBustHandlingResult.Prevented)
                {
                    result = resumedResult;
                    roundResolution = null;
                    if (azazelBustSide.HasValue)
                    {
                        LastDemonContractEffectResult =
                            new DemonContractEffectResult(
                                triggered: true,
                                bustedTarget: null,
                                paidSoulCost: 0);
                    }
                }
                else if (handling != OwnerBustHandlingResult.NotHandled)
                {
                    return handling == OwnerBustHandlingResult.BattleEnded
                        ? CardEffectApplicationResult.RoundEnded
                        : CardEffectApplicationResult.Pending;
                }
            }

            if (State == CoreLoopState.BattleEnded)
            {
                return CardEffectApplicationResult.RoundEnded;
            }

            return CompleteCardEffectResult(
                sourceCard,
                result,
                roundResolution);
        }

        private void ResumeCardEffectAfterBeelzebubBust(
            BlackjackCard sourceCard,
            CardEffectResult result)
        {
            CardEffectApplicationResult applicationResult =
                CompleteCardEffectResult(
                    sourceCard,
                    result,
                    roundResolution: null);
            if (applicationResult != CardEffectApplicationResult.Completed)
            {
                return;
            }

            CombatantSide actorSide = LastCardEffectActorSide ??
                throw new InvalidOperationException(
                    "Resumed card effect lost its actor side.");
            CompleteCardEffectOwnerAction(actorSide);
        }

        private CardEffectApplicationResult CompleteCardEffectResult(
            BlackjackCard sourceCard,
            CardEffectResult result,
            RoundResolution? roundResolution)
        {

            CombatantSide actorSide = _activeCardEffectActorSide ??
                throw new InvalidOperationException("Card effect has no actor side.");
            IReadOnlyList<ActiveDemonContract> ownerContracts =
                actorSide == CombatantSide.Player
                    ? _activePlayerDemonContracts
                    : _activeEnemyDemonContracts;
            LeviathanCardEffectSequence completedLeviathanSequence =
                _activeLeviathanCardEffectSequence;
            if (completedLeviathanSequence != null &&
                (completedLeviathanSequence.OwnerSide != actorSide ||
                    completedLeviathanSequence.SourceCardId != sourceCard.Id))
            {
                throw new InvalidOperationException(
                    "Leviathan continuation does not match its active card effect.");
            }

            if (completedLeviathanSequence == null &&
                _demonContractResolver.TryGetOwnerCardEffectSequenceContract(
                    this,
                    ownerContracts,
                    actorSide,
                    result.EffectKind,
                    out ActiveDemonContract leviathanContract))
            {
                if (_demonContractResolver
                    .RequiresAdditionalOwnerCardEffectActivation(
                        this,
                        leviathanContract,
                        result))
                {
                    CardEffectStep repeatedStep =
                        _cardEffectResolver.Begin(_activeCardEffectContext);
                    if (repeatedStep.PendingEffect == null ||
                        repeatedStep.Result.HasValue ||
                        repeatedStep.RoundResolution.HasValue ||
                        repeatedStep.Continuation != null)
                    {
                        throw new InvalidOperationException(
                            "Leviathan auto-pistol repeat must request a fresh declaration.");
                    }

                    _activeLeviathanCardEffectSequence =
                        new LeviathanCardEffectSequence(
                            actorSide,
                            leviathanContract.SourceCardId,
                            sourceCard.Id,
                            result.Succeeded);
                    LastCardEffectResult = result;
                    LastCardEffectActorSide = actorSide;
                    _pendingCardEffect = repeatedStep.PendingEffect;
                    State = actorSide == CombatantSide.Player
                        ? CoreLoopState.PlayerResolvingCardEffect
                        : CoreLoopState.EnemyTurn;
                    RaiseStepped();
                    return CardEffectApplicationResult.Pending;
                }

                LastLeviathanCardEffectResult = new LeviathanCardEffectResult(
                    new[] { result.Succeeded },
                    roundResolution.HasValue
                        ? (CombatantSide?)GetOppositeSide(actorSide)
                        : null,
                    paidSoulCost: 0);
            }
            else if (completedLeviathanSequence != null &&
                roundResolution.HasValue)
            {
                LastLeviathanCardEffectResult = new LeviathanCardEffectResult(
                    new[]
                    {
                        completedLeviathanSequence.FirstActivationSucceeded,
                        result.Succeeded
                    },
                    GetOppositeSide(actorSide),
                    paidSoulCost: 0);
                _activeLeviathanCardEffectSequence = null;
            }
            else if (completedLeviathanSequence != null && result.Succeeded)
            {
                LastLeviathanCardEffectResult = new LeviathanCardEffectResult(
                    new[]
                    {
                        completedLeviathanSequence.FirstActivationSucceeded,
                        true
                    },
                    bustedTarget: null,
                    paidSoulCost: 0);
                _activeLeviathanCardEffectSequence = null;
            }

            if (!sourceCard.TryCompleteUse())
            {
                throw new InvalidOperationException("Active card effect could not complete its source card.");
            }

            LastCardEffectResult = result;
            LastCardEffectActorSide = actorSide;
            _pendingCardEffect = null;
            _activeCardEffectContext = null;
            _activeCardEffectActorSide = null;
            RaiseStepped();

            if (roundResolution.HasValue)
            {
                _belialForcedCardEffectContinuation = null;
                NotifyNormalTurnEnded(actorSide);
                CompleteRound(roundResolution.Value);
                return CardEffectApplicationResult.RoundEnded;
            }

            if (_demonContractResolver.TryResolveOwnerAfterCardEffect(
                    this,
                    ownerContracts,
                    actorSide,
                    result,
                    out DemonContractAfterCardEffectStep contractStep))
            {
                RoundResolution? contractResolution = contractStep.RoundResolution;
                DemonContractEffectResult contractResult = contractStep.Result;
                if (contractResolution.HasValue)
                {
                    DemonContractEffectResult resumedContractResult =
                        new DemonContractEffectResult(
                            triggered: true,
                            bustedTarget: null,
                            contractResult.PaidSoulCost);
                    OwnerBustHandlingResult handling = HandleRoundBust(
                        contractResolution.Value,
                        () => ResumeCardEffectAfterContractBust(
                            actorSide,
                            completedLeviathanSequence,
                            result,
                            resumedContractResult));
                    if (handling == OwnerBustHandlingResult.Prevented)
                    {
                        contractResolution = null;
                        contractResult = resumedContractResult;
                    }
                    else if (handling != OwnerBustHandlingResult.NotHandled)
                    {
                        return handling == OwnerBustHandlingResult.BattleEnded
                            ? CardEffectApplicationResult.RoundEnded
                            : CardEffectApplicationResult.Pending;
                    }
                }

                if (State == CoreLoopState.BattleEnded)
                {
                    return CardEffectApplicationResult.RoundEnded;
                }

                LastDemonContractEffectResult = contractResult;
                if (completedLeviathanSequence != null)
                {
                    LastLeviathanCardEffectResult =
                        new LeviathanCardEffectResult(
                            new[]
                            {
                                completedLeviathanSequence
                                    .FirstActivationSucceeded,
                                result.Succeeded
                            },
                            contractResult.BustedTarget,
                            contractResult.PaidSoulCost);
                    _activeLeviathanCardEffectSequence = null;
                }
                RaiseStepped();

                if (contractResolution.HasValue)
                {
                    _belialForcedCardEffectContinuation = null;
                    NotifyNormalTurnEnded(actorSide);
                    CompleteRound(contractResolution.Value);
                    return CardEffectApplicationResult.RoundEnded;
                }

                if (GetParticipant(actorSide).Soul.IsDepleted)
                {
                    EndBattleWithoutRound();
                    return CardEffectApplicationResult.RoundEnded;
                }
            }
            else if (completedLeviathanSequence != null && !result.Succeeded)
            {
                throw new InvalidOperationException(
                    "Leviathan continuation completed without its contract result.");
            }

            State = actorSide == CombatantSide.Player
                ? CoreLoopState.PlayerTurn
                : CoreLoopState.EnemyTurn;
            return CardEffectApplicationResult.Completed;
        }

        private void ResumeCardEffectAfterContractBust(
            CombatantSide actorSide,
            LeviathanCardEffectSequence completedLeviathanSequence,
            CardEffectResult result,
            DemonContractEffectResult contractResult)
        {
            if (completedLeviathanSequence != null)
            {
                LastLeviathanCardEffectResult =
                    new LeviathanCardEffectResult(
                        new[]
                        {
                            completedLeviathanSequence
                                .FirstActivationSucceeded,
                            result.Succeeded
                        },
                        bustedTarget: null,
                        contractResult.PaidSoulCost);
                _activeLeviathanCardEffectSequence = null;
            }

            if (GetParticipant(actorSide).Soul.IsDepleted)
            {
                EndBattleWithoutRound();
                return;
            }

            CompleteCardEffectOwnerAction(actorSide);
        }

        private void CompleteCardEffectOwnerAction(CombatantSide actorSide)
        {
            AzazelCardEffectSequence azazelSequence =
                _activeAzazelCardEffectSequence;
            if (azazelSequence != null)
            {
                if (azazelSequence.OwnerSide != actorSide)
                {
                    throw new InvalidOperationException(
                        "Azazel card sequence lost its owner.");
                }

                ContinueAzazelCardEffectSequence(azazelSequence);
                return;
            }

            BelialForcedCardEffectContinuation belialContinuation =
                _belialForcedCardEffectContinuation;
            if (belialContinuation != null)
            {
                if (belialContinuation.OwnerSide != actorSide)
                {
                    throw new InvalidOperationException(
                        "Belial forced card continuation lost its owner.");
                }

                _belialForcedCardEffectContinuation = null;
                CompleteBelialTransferredCardUse(
                    actorSide,
                    belialContinuation.SourceContractCardId,
                    belialContinuation.TransferredCardId,
                    AutomaticCardSourceDisposition.RetainFaceUp);
                return;
            }

            State = actorSide == CombatantSide.Player
                ? CoreLoopState.PlayerTurn
                : CoreLoopState.EnemyTurn;
            if (actorSide == CombatantSide.Player)
            {
                CompletePlayerActionAndRunEnemyTurn();
            }
            else
            {
                CompleteEnemyAction();
            }
        }

        private bool TryBeginAzazelCardEffectSequence(
            IReadOnlyList<ActiveDemonContract> activeContracts,
            CombatantSide ownerSide,
            Action complete)
        {
            ActiveDemonContract azazelContract = null;
            foreach (ActiveDemonContract activeContract in activeContracts)
            {
                if (activeContract.Kind == DemonContractKind.Azazel)
                {
                    azazelContract = activeContract;
                    break;
                }
            }

            return azazelContract != null && TryBeginAzazelCardEffectSequence(
                azazelContract,
                complete);
        }

        private bool TryBeginAzazelCardEffectSequence(
            ActiveDemonContract activeContract,
            Action complete)
        {
            if (activeContract == null ||
                activeContract.Kind != DemonContractKind.Azazel ||
                State == CoreLoopState.BattleEnded)
            {
                return false;
            }

            if (_activeAzazelCardEffectSequence != null)
            {
                throw new InvalidOperationException(
                    "Only one Azazel card sequence can resolve at a time.");
            }

            BattleParticipant owner = GetParticipant(activeContract.OwnerSide);
            var cardIds = new List<int>();
            foreach (BlackjackCard card in owner.Hand.GetPublicCards())
            {
                if (card.Definition.Activation == CardActivationKind.Manual &&
                    card.Definition.Effect != CardEffectKind.None &&
                    (card.UseState == CardUseState.Available ||
                        card.UseState == CardUseState.Used))
                {
                    cardIds.Add(card.Id);
                }
            }

            if (cardIds.Count == 0)
            {
                return false;
            }

            var sequence = new AzazelCardEffectSequence(
                activeContract.OwnerSide,
                cardIds.AsReadOnly(),
                complete);
            _activeAzazelCardEffectSequence = sequence;
            ContinueAzazelCardEffectSequence(sequence);
            return true;
        }

        private void CompletePlayerActionAfterAzazelSequence()
        {
            State = CoreLoopState.PlayerTurn;
            RaiseStepped();
            CompletePlayerActionAndRunEnemyTurn();
        }

        private void CompleteEnemyActionAfterAzazelSequence()
        {
            State = CoreLoopState.EnemyTurn;
            RaiseStepped();
            CompleteEnemyAction();
        }

        private void ContinueAzazelCardEffectSequence(
            AzazelCardEffectSequence sequence)
        {
            if (!ReferenceEquals(_activeAzazelCardEffectSequence, sequence) ||
                State == CoreLoopState.BattleEnded)
            {
                return;
            }

            BattleParticipant owner = GetParticipant(sequence.OwnerSide);
            while (sequence.NextIndex < sequence.CardIds.Count)
            {
                int cardId = sequence.CardIds[sequence.NextIndex++];
                if (!owner.Hand.TryGetCard(cardId, out BlackjackCard card) ||
                    owner.Hand.IsHiddenCard(cardId) ||
                    !card.IsFaceUp ||
                    card.Definition.Activation != CardActivationKind.Manual ||
                    card.Definition.Effect == CardEffectKind.None)
                {
                    continue;
                }

                if (card.UseState != CardUseState.Available &&
                    card.UseState != CardUseState.Used)
                {
                    continue;
                }

                var context = new CardEffectContext(this, sequence.OwnerSide, card);
                if (!_cardEffectResolver.CanStart(context))
                {
                    continue;
                }

                if (card.UseState == CardUseState.Used && !card.TryReactivate())
                {
                    throw new InvalidOperationException(
                        "Azazel could not reactivate a queued public card.");
                }

                if (!card.TryBeginUse())
                {
                    throw new InvalidOperationException(
                        "Azazel could not begin a queued public card effect.");
                }

                _activeCardEffectContext = context;
                _activeCardEffectActorSide = sequence.OwnerSide;
                RecordPublicAction(
                    sequence.OwnerSide,
                    PublicCombatActionType.UseCard,
                    card.DefinitionKey,
                    card.Id);
                CardEffectApplicationResult applicationResult =
                    ApplyCardEffectStep(_cardEffectResolver.Begin(context));
                if (applicationResult != CardEffectApplicationResult.Completed ||
                    !ReferenceEquals(_activeAzazelCardEffectSequence, sequence))
                {
                    return;
                }
            }

            _activeAzazelCardEffectSequence = null;
            sequence.Complete();
        }

        private OwnerBustHandlingResult HandleRoundBust(
            RoundResolution resolution,
            Action resume,
            Action beforeBustReplacementPublish = null)
        {
            switch (resolution.Outcome)
            {
                case RoundOutcome.PlayerBust:
                    return HandlePlayerBust(resume, beforeBustReplacementPublish);
                case RoundOutcome.EnemyBust:
                    return HandleEnemyBust(resume, beforeBustReplacementPublish);
                default:
                    return OwnerBustHandlingResult.NotHandled;
            }
        }

        private bool ApplyRoundStartContracts(
            CombatantSide ownerSide,
            IReadOnlyList<ActiveDemonContract> activeContracts,
            Action resumeAfterBustPrevention)
        {
            BattleParticipant owner = GetParticipant(ownerSide);
            int soulBefore = owner.Soul.Current;
            _demonContractResolver.NotifyRoundStarted(
                this,
                activeContracts,
                ownerSide);

            if (ownerSide == CombatantSide.Player &&
                HasActiveMammonContract(activeContracts))
            {
                // A distinct beat so presentation can animate the fresh round-start die roll
                // before any bust it caused gets revealed, instead of both landing in one frame.
                RaiseStepped();
            }

            int paidSoulCost = soulBefore - owner.Soul.Current;
            if (paidSoulCost > 0)
            {
                LastDemonContractEffectResult =
                    new DemonContractEffectResult(
                        triggered: true,
                        bustedTarget: null,
                        paidSoulCost);
                RaiseStepped();
            }

            if (owner.Soul.IsDepleted)
            {
                EndBattleWithoutRound();
                return false;
            }

            ActiveDemonContract mammonContract = null;
            foreach (ActiveDemonContract contract in activeContracts)
            {
                if (contract.Kind == DemonContractKind.Mammon &&
                    contract.RuntimeState is MammonRuntimeState mammon &&
                    mammon.CurrentDieValue == 6)
                {
                    mammonContract = contract;
                    break;
                }
            }
            if (mammonContract == null)
            {
                return true;
            }

            OwnerBustHandlingResult handling = ownerSide == CombatantSide.Player
                ? HandlePlayerBust(resumeAfterBustPrevention)
                : HandleEnemyBust(resumeAfterBustPrevention);
            if (handling == OwnerBustHandlingResult.NotHandled)
            {
                LastDemonContractEffectResult = new DemonContractEffectResult(
                    triggered: true,
                    bustedTarget: ownerSide,
                    paidSoulCost: 0);
                RaiseStepped();
                CompleteRound(RoundResolver.ResolveContractEffectBust(
                    RoundNumber,
                    playerIsTarget: ownerSide == CombatantSide.Player));
                return false;
            }

            if (handling != OwnerBustHandlingResult.Prevented)
            {
                return false;
            }

            LastDemonContractEffectResult = new DemonContractEffectResult(
                triggered: true,
                bustedTarget: null,
                paidSoulCost: 0);
            RaiseStepped();
            return true;
        }

        private static bool HasActiveMammonContract(
            IReadOnlyList<ActiveDemonContract> activeContracts)
        {
            foreach (ActiveDemonContract contract in activeContracts)
            {
                if (contract.Kind == DemonContractKind.Mammon)
                {
                    return true;
                }
            }

            return false;
        }

        private void StartRound()
        {
            State = CoreLoopState.StartingRound;
            RoundNumber++;
            TurnNumber = 0;
            _activeCardEffectContext = null;
            _activeCardEffectActorSide = null;
            _pendingCardEffect = null;
            _activeLeviathanCardEffectSequence = null;
            _activeAutomaticCardEffectContext = null;
            _automaticCardContinuation = null;
            _pendingAutomaticCardInteraction = null;
            _automaticCardBattleState.ClearRoundState();
            ClearPlayerDemonContractInteraction();
            ClearEnemyDemonContractInteraction();
            _pendingBeelzebubBustResolution = null;
            _pendingPaimonExileResolution = null;
            _belialForcedCardEffectContinuation = null;
            _activeAzazelCardEffectSequence = null;
            _resolvedPaimonOpponentBustContractIds.Clear();
            _playerAzazelBustPending = false;
            _enemyAzazelBustPending = false;
            _playerFinalBonusForEnemyChoice = 0;
            _playerChangeSelection = null;
            _publicActionHistory.Clear();
            _lastPublicActionSourceCardId = null;
            _lastAutomaticCardResultActionOrdinal = -1;
            _lastSatanForcedDrawActionOrdinal = -1;
            _enemyDecisionOrdinal = 0;
            LastResolutionPlayerBonus = 0;
            LastResolutionEnemyBonus = 0;

            if (!ApplyRoundStartContracts(
                    CombatantSide.Player,
                    _activePlayerDemonContracts,
                    ContinueStartingRoundWithEnemyContracts))
            {
                return;
            }

            ContinueStartingRoundWithEnemyContracts();
        }

        private void ContinueStartingRoundWithEnemyContracts()
        {
            if (!ApplyRoundStartContracts(
                    CombatantSide.Enemy,
                    _activeEnemyDemonContracts,
                    DealStartingRoundCards))
            {
                return;
            }

            DealStartingRoundCards();
        }

        private void DealStartingRoundCards()
        {
            InjectPoisonIntoPlayerDeck();

            Player.Draw(faceUp: true);
            Enemy.Draw(faceUp: true);
            Player.Draw(faceUp: false);
            Enemy.Draw(faceUp: false);

            if (TryResolveBaphometExhaustion(CompleteStartingRoundAfterDeal))
            {
                return;
            }

            CompleteStartingRoundAfterDeal();
        }

        private void CompleteStartingRoundAfterDeal()
        {
            if (_pendingFixedEnemyAzazelActivation &&
                _activeFixedEnemyDemonContract != null &&
                _activeFixedEnemyDemonContract.Kind == DemonContractKind.Azazel)
            {
                _pendingFixedEnemyAzazelActivation = false;
                if (TryBeginAzazelCardEffectSequence(
                        _activeFixedEnemyDemonContract,
                        () =>
                        {
                            BeginPlayerTurn();
                            RaiseStepped();
                        }))
                {
                    if (State == CoreLoopState.EnemyTurn)
                    {
                        RunEnemyTurn();
                    }

                    return;
                }
            }

            BeginPlayerTurn();
            RaiseStepped();
        }

        private void CompletePlayerActionAndRunEnemyTurn()
        {
            if (State != CoreLoopState.PlayerTurn)
            {
                throw new InvalidOperationException(
                    "A player action can only complete from the player turn state.");
            }

            if (TryResolveBaphometExhaustion(
                    CompletePlayerActionAndRunEnemyTurn,
                    CombatantSide.Player))
            {
                return;
            }

            if (!Player.IsStanding &&
                CanPlayerStand &&
                _demonContractResolver.TryConsumePlayerAutoStand(
                    this,
                    _activePlayerDemonContracts))
            {
                RecordPublicAction(CombatantSide.Player, PublicCombatActionType.Stand);
                Player.Stand();
                RaiseStepped();
            }

            NotifyNormalTurnEnded(CombatantSide.Player);
            RunEnemyTurn();
        }

        private void BeginPlayerTurn()
        {
            _resolvedPlayerTurnStartContractIds.Clear();
            State = CoreLoopState.PlayerTurn;
            if (!HandleNormalTurnStarted(
                CombatantSide.Player,
                CompletePlayerTurnStarted))
            {
                return;
            }

            CompletePlayerTurnStarted();
        }

        private void CompletePlayerTurnStarted()
        {
            _demonContractResolver.NotifyPlayerTurnStarted(
                this,
                _activePlayerDemonContracts);
            TryBeginNextTurnStartChoice(CombatantSide.Player);
        }

        private void RunEnemyTurn()
        {
            _resolvedEnemyTurnStartContractIds.Clear();
            State = CoreLoopState.EnemyTurn;

            if (!Enemy.IsStanding &&
                PendingEnemyCardEffect == null &&
                _pendingEnemyDemonContractInteraction == null)
            {
                if (!HandleNormalTurnStarted(
                    CombatantSide.Enemy,
                    ResumeEnemyTurnAfterNormalStart))
                {
                    return;
                }

                CompleteEnemyTurnStarted();
            }

            ContinueEnemyTurnLoop();
        }

        private void CompleteEnemyTurnStarted()
        {
            _demonContractResolver.NotifyOwnerTurnStarted(
                this,
                _activeEnemyDemonContracts,
                CombatantSide.Enemy);
            TryBeginNextTurnStartChoice(CombatantSide.Enemy);
        }

        private void ResumeEnemyTurnAfterNormalStart()
        {
            CompleteEnemyTurnStarted();
            ContinueEnemyTurnLoop();
        }

        private void ContinueEnemyTurnLoop()
        {
            while (State == CoreLoopState.EnemyTurn)
            {
                if (Enemy.IsStanding &&
                    PendingEnemyCardEffect == null &&
                    _pendingEnemyDemonContractInteraction == null)
                {
                    if (Player.IsStanding)
                    {
                        ResolveRound();
                    }
                    else
                    {
                        BeginPlayerTurn();
                    }

                    return;
                }

                int decisionSeed = CreateEnemyDecisionSeed();
                EnemyDecision decision = DecideEnemyAction(decisionSeed);
                if (!TryExecuteEnemyDecision(decision, decisionSeed))
                {
                    throw new InvalidOperationException(
                        "Validated enemy decision could not be executed.");
                }
            }
        }

        private EnemyDecision DecideEnemyAction(int decisionSeed)
        {
            EnemyObservation observation = null;
            for (int attempt = 0; attempt < 2; attempt++)
            {
                observation = EnemyObservationFactory.Create(this, decisionSeed);
                EnemyDecision decision;
                if (!EnemyPolicyDecisionSelector.TrySelectCertainAutoPistol(
                        observation,
                        out decision))
                {
                    if (!(_enemyPolicy is IEnemyForcedActionPolicy forcedPolicy) ||
                        !forcedPolicy.TryDecideForcedAction(
                            observation,
                            out decision))
                    {
                        if (!EnemyPolicyDecisionSelector.TrySelectRequiredChange(
                                observation,
                                out decision))
                        {
                            decision = _enemyPolicy.Decide(observation);
                        }
                    }
                }

                if (EnemyDecisionValidator.CanExecute(observation, decision))
                {
                    LastEnemyDecision = decision;
                    return decision;
                }
            }

            EnemyActionCandidate fallback = null;
            foreach (EnemyActionCandidate candidate in observation.ActionCandidates)
            {
                if (fallback == null || candidate.ActionType == EnemyActionType.Stand)
                {
                    fallback = candidate;
                }

                if (candidate.ActionType == EnemyActionType.Stand)
                {
                    break;
                }
            }

            if (fallback == null)
            {
                throw new InvalidOperationException("Enemy turn has no executable fallback action.");
            }

            LastEnemyDecision = EnemyDecision.FromCandidate(
                fallback,
                "fallback-after-invalid-policy-decision");
            return LastEnemyDecision;
        }

        private bool TryExecuteEnemyDecision(EnemyDecision decision, int decisionSeed)
        {
            EnemyObservation currentObservation =
                EnemyObservationFactory.Create(this, decisionSeed);
            if (!EnemyDecisionValidator.CanExecute(currentObservation, decision))
            {
                return false;
            }

            bool executed;
            switch (decision.ActionType)
            {
                case EnemyActionType.Hit:
                    if (_demonContractResolver.TryGetOwnerHitPreviewContract(
                        this,
                        _activeEnemyDemonContracts,
                        CombatantSide.Enemy,
                        out ActiveDemonContract previewContract))
                    {
                        executed = TryBeginEnemyBelphegorTopCardPreview(
                            previewContract);
                        break;
                    }

                    RecordPublicAction(CombatantSide.Enemy, PublicCombatActionType.Hit);
                    BlackjackCard drawnCard = Enemy.Draw(faceUp: true);
                    RaiseStepped();
                    if (!TryBeginAutomaticCardEffect(
                        CombatantSide.Enemy,
                        drawnCard,
                        AutomaticCardContinuation.ForEnemyHit()))
                    {
                        CompleteEnemyHitAfterAutomaticCard();
                    }

                    executed = true;
                    break;

                case EnemyActionType.Stand:
                    RecordPublicAction(CombatantSide.Enemy, PublicCombatActionType.Stand);
                    Enemy.Stand();
                    RaiseStepped();
                    CompleteEnemyAction();

                    executed = true;
                    break;

                case EnemyActionType.UseCard:
                    bool wasPending = PendingEnemyCardEffect != null;
                    bool wasBelialForced =
                        _belialForcedCardEffectContinuation != null;
                    executed = wasPending
                        ? decision.CardEffectOptionId.HasValue &&
                            TryResolveCardChoice(
                                CombatantSide.Enemy,
                                decision.CardEffectOptionId.Value)
                        : decision.CardId.HasValue &&
                            TryBeginCardUse(CombatantSide.Enemy, decision.CardId.Value);

                    if (executed &&
                        !wasBelialForced &&
                        PendingEnemyCardEffect == null &&
                        State == CoreLoopState.EnemyTurn)
                    {
                        CompleteEnemyAction();
                    }

                    break;

                case EnemyActionType.DemonContract:
                    bool completedOwnerAction = false;
                    executed = decision.DemonContractOptionId.HasValue
                        ? TryResolveEnemyDemonContract(
                            decision.DemonContractOptionId.Value,
                            out completedOwnerAction)
                        : decision.DemonContractSourceCardId.HasValue
                            ? TryBeginEnemyActiveDemonContractAction(
                                decision.DemonContractSourceCardId.Value)
                            : TryBeginEnemyDemonContract();

                    if (executed &&
                        completedOwnerAction &&
                        State == CoreLoopState.EnemyTurn &&
                        _pendingEnemyDemonContractInteraction == null)
                    {
                        CompleteEnemyAction();
                    }

                    break;

                case EnemyActionType.Change:
                    executed = TryExecuteEnemyChange();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(decision));
            }

            if (executed)
            {
                _enemyDecisionOrdinal++;
            }

            return executed;
        }

        private void CompleteEnemyHitAfterAutomaticCard()
        {
            if (ConsumePendingAzazelBust(CombatantSide.Enemy))
            {
                OwnerBustHandlingResult azazelHandling = HandleEnemyBust(() =>
                {
                    State = CoreLoopState.EnemyTurn;
                    CompleteEnemyHitAction();
                });
                if (azazelHandling == OwnerBustHandlingResult.NotHandled)
                {
                    LastDemonContractEffectResult = new DemonContractEffectResult(
                        triggered: true,
                        bustedTarget: CombatantSide.Enemy,
                        paidSoulCost: 0);
                    NotifyNormalTurnEnded(CombatantSide.Enemy);
                    CompleteRound(RoundResolver.ResolveContractEffectBust(
                        RoundNumber,
                        playerIsTarget: false));
                    return;
                }

                if (azazelHandling != OwnerBustHandlingResult.Prevented)
                {
                    return;
                }

                LastDemonContractEffectResult = new DemonContractEffectResult(
                    triggered: true,
                    bustedTarget: null,
                    paidSoulCost: 0);
                RaiseStepped();
            }

            if (Enemy.VisibleHandValue.IsBust)
            {
                OwnerBustHandlingResult handling = HandleEnemyBust(() =>
                {
                    State = CoreLoopState.EnemyTurn;
                    CompleteEnemyHitAction();
                });
                if (handling == OwnerBustHandlingResult.NotHandled)
                {
                    NotifyNormalTurnEnded(CombatantSide.Enemy);
                    CompleteRound(RoundResolver.ResolveNumericBust(
                        RoundNumber,
                        playerIsTarget: false));
                    return;
                }

                if (handling != OwnerBustHandlingResult.Prevented)
                {
                    return;
                }
            }

            if (State == CoreLoopState.BattleEnded)
            {
                return;
            }

            if (TryResolveBaphometExhaustion(
                    CompleteEnemyHitAfterAutomaticCard,
                    CombatantSide.Enemy))
            {
                return;
            }

            CompleteEnemyHitAction();
        }

        private void CompleteEnemyHitAction()
        {
            if (TryBeginAzazelCardEffectSequence(
                    _activeEnemyDemonContracts,
                    CombatantSide.Enemy,
                    CompleteEnemyAction))
            {
                return;
            }

            CompleteEnemyAction();
        }

        private void CompleteEnemyAction()
        {
            if (State != CoreLoopState.EnemyTurn ||
                PendingEnemyCardEffect != null ||
                _pendingEnemyDemonContractInteraction != null)
            {
                return;
            }

            if (TryResolveBaphometExhaustion(
                    CompleteEnemyAction,
                    CombatantSide.Enemy))
            {
                return;
            }

            if (!Enemy.IsStanding &&
                CanEnemyStand &&
                _demonContractResolver.TryConsumeOwnerAutoStand(
                    this,
                    _activeEnemyDemonContracts,
                    CombatantSide.Enemy))
            {
                RecordPublicAction(CombatantSide.Enemy, PublicCombatActionType.Stand);
                Enemy.Stand();
                RaiseStepped();
            }

            NotifyNormalTurnEnded(CombatantSide.Enemy);

            if (Enemy.IsStanding)
            {
                if (Player.IsStanding)
                {
                    ResolveRound();
                }
                else
                {
                    BeginPlayerTurn();
                }

                return;
            }

            if (!Player.IsStanding)
            {
                BeginPlayerTurn();
            }
        }

        private int CreateEnemyDecisionSeed()
        {
            unchecked
            {
                return (RoundNumber * 397) ^ _enemyDecisionOrdinal;
            }
        }

        private void RaiseStepped()
        {
            Stepped?.Invoke();
        }

        internal void NotifyCardEffectVisualStep()
        {
            RaiseStepped();
        }

        private void RecordPublicAction(
            CombatantSide actorSide,
            PublicCombatActionType actionType,
            string sourceCardDefinitionKey = null,
            int? sourceCardId = null)
        {
            bool requiresSourceCardId =
                actionType == PublicCombatActionType.UseCard ||
                actionType == PublicCombatActionType.DemonContract;
            if (requiresSourceCardId != sourceCardId.HasValue ||
                (sourceCardId.HasValue && sourceCardId.Value < 0))
            {
                throw new ArgumentException(
                    "Only public card or contract actions require a source card id.",
                    nameof(sourceCardId));
            }

            _publicActionHistory.Add(new PublicCombatAction(
                actorSide,
                actionType,
                sourceCardDefinitionKey));
            _lastPublicActionSourceCardId = sourceCardId;
        }

        private void ResolveRound()
        {
            if (_demonContractResolver.TryGetPlayerFinalChoiceContract(
                this,
                _activePlayerDemonContracts,
                out ActiveDemonContract choiceContract))
            {
                int interactionId = TakeNextDemonContractInteractionId();
                _pendingPlayerDemonContractInteraction =
                    CreateMammonFinalChoiceInteraction(interactionId, choiceContract);
                State = CoreLoopState.PlayerResolvingDemonContract;
                RaiseStepped();
                return;
            }

            ResolveRoundWithEnemyFinalChoice(playerBonus: 0);
        }

        private OwnerBustHandlingResult HandlePlayerBust(
            Action resume,
            Action beforeBustReplacementPublish = null)
        {
            return HandleOwnerBust(
                CombatantSide.Player,
                _activePlayerDemonContracts,
                resume,
                beforeBustReplacementPublish: beforeBustReplacementPublish);
        }

        private OwnerBustHandlingResult HandleEnemyBust(
            Action resume,
            Action beforeBustReplacementPublish = null)
        {
            return HandleOwnerBust(
                CombatantSide.Enemy,
                _activeEnemyDemonContracts,
                resume,
                beforeBustReplacementPublish: beforeBustReplacementPublish);
        }

        private bool TryResolveBaphometExhaustion(
            Action resume,
            CombatantSide? normalTurnActorSide = null)
        {
            if (!_demonContractCardState.TryResetNextExhaustedBaphometWave(
                    out BaphometExhaustion exhaustion))
            {
                return false;
            }

            IReadOnlyList<ActiveDemonContract> targetContracts =
                exhaustion.TargetSide == CombatantSide.Player
                    ? _activePlayerDemonContracts
                    : _activeEnemyDemonContracts;
            Action resumeAfterPrevention = () =>
            {
                if (!TryResolveBaphometExhaustion(
                        resume,
                        normalTurnActorSide))
                {
                    resume();
                }
            };
            OwnerBustHandlingResult handling = HandleOwnerBust(
                exhaustion.TargetSide,
                targetContracts,
                resumeAfterPrevention);
            if (handling == OwnerBustHandlingResult.NotHandled)
            {
                LastDemonContractEffectResult =
                    new DemonContractEffectResult(
                        triggered: true,
                        bustedTarget: exhaustion.TargetSide,
                        paidSoulCost: 0);
                RaiseStepped();
                if (normalTurnActorSide.HasValue)
                {
                    NotifyNormalTurnEnded(normalTurnActorSide.Value);
                }

                CompleteRound(RoundResolver.ResolveContractEffectBust(
                    RoundNumber,
                    playerIsTarget:
                        exhaustion.TargetSide == CombatantSide.Player));
            }
            else if (handling == OwnerBustHandlingResult.Prevented)
            {
                LastDemonContractEffectResult =
                    new DemonContractEffectResult(
                        triggered: true,
                        bustedTarget: null,
                        paidSoulCost: 0);
                RaiseStepped();
                resumeAfterPrevention();
            }

            return true;
        }

        private OwnerBustHandlingResult HandleOwnerBust(
            CombatantSide ownerSide,
            IReadOnlyList<ActiveDemonContract> activeContracts,
            Action resume,
            bool allowAbsolutePrevention = true,
            Action beforeBustReplacementPublish = null)
        {
            if (allowAbsolutePrevention &&
                _demonContractResolver.PreventsOwnerBust(
                this,
                activeContracts,
                ownerSide))
            {
                return OwnerBustHandlingResult.Prevented;
            }

            if (!_demonContractResolver.TryReplaceOwnerBust(
                this,
                activeContracts,
                ownerSide,
                out DemonContractEffectResult result,
                out ActiveDemonContract replacementContract))
            {
                return OwnerBustHandlingResult.NotHandled;
            }

            beforeBustReplacementPublish?.Invoke();
            LastDemonContractEffectResult = result;
            RaiseStepped();
            if (GetParticipant(ownerSide).Soul.IsDepleted)
            {
                EndBattleWithoutRound();
                return OwnerBustHandlingResult.BattleEnded;
            }

            CoreLoopState resumeState = State;
            _pendingBeelzebubBustResolution =
                new PendingBeelzebubBustResolution(
                    replacementContract,
                    resumeState,
                    resume);
            IReadOnlyList<BlackjackCard> ownerCards =
                GetParticipant(ownerSide).Hand.GetPublicCards();
            PendingDemonContractInteraction pending =
                CreateBeelzebubDiscardInteraction(
                    TakeNextDemonContractInteractionId(),
                    replacementContract,
                    ownerCards.Count > 0
                        ? ownerCards
                        : GetOpponent(ownerSide).Hand.GetPublicCards(),
                    choosingOwnerCard: ownerCards.Count > 0);
            SetPendingDemonContractInteraction(ownerSide, pending);
            State = ownerSide == CombatantSide.Player
                ? CoreLoopState.PlayerResolvingDemonContract
                : CoreLoopState.EnemyTurn;
            RaiseStepped();

            if (ownerSide == CombatantSide.Enemy)
            {
                ResolvePendingEnemyBeelzebubChoices();
                return State == CoreLoopState.BattleEnded
                    ? OwnerBustHandlingResult.BattleEnded
                    : OwnerBustHandlingResult.Resumed;
            }

            return OwnerBustHandlingResult.PendingSelection;
        }

        private void ResolvePendingEnemyBeelzebubChoices()
        {
            if (_isResolvingEnemyBeelzebubChoice)
            {
                return;
            }

            _isResolvingEnemyBeelzebubChoice = true;
            try
            {
                const int maximumChoiceCount = 128;
                for (int choiceIndex = 0;
                    choiceIndex < maximumChoiceCount;
                    choiceIndex++)
                {
                    PendingDemonContractInteraction pending =
                        _pendingEnemyDemonContractInteraction;
                    if (_pendingBeelzebubBustResolution == null ||
                        pending == null ||
                        (pending.Kind !=
                            DemonContractInteractionKind
                                .BeelzebubChooseOwnerCard &&
                         pending.Kind !=
                            DemonContractInteractionKind
                                .BeelzebubChooseOpponentCard))
                    {
                        return;
                    }

                    int decisionSeed = CreateEnemyDecisionSeed();
                    EnemyObservation observation =
                        EnemyObservationFactory.Create(this, decisionSeed);
                    EnemyActionCandidate selected = null;
                    foreach (EnemyActionCandidate candidate in
                        observation.ActionCandidates)
                    {
                        if (candidate.DemonContractInteractionKind !=
                                pending.Kind ||
                            !candidate.DemonContractOptionNumericValue.HasValue)
                        {
                            continue;
                        }

                        if (selected == null ||
                            IsBetterBeelzebubDiscardCandidate(
                                pending.Kind,
                                candidate,
                                selected))
                        {
                            selected = candidate;
                        }
                    }

                    if (selected == null)
                    {
                        throw new InvalidOperationException(
                            "Enemy Beelzebub choice has no public card candidate.");
                    }

                    EnemyDecision decision = EnemyDecision.FromCandidate(
                        selected,
                        pending.Kind ==
                            DemonContractInteractionKind
                                .BeelzebubChooseOwnerCard
                            ? "beelzebub-discard-highest-own-card"
                            : "beelzebub-discard-lowest-opponent-card");
                    LastEnemyDecision = decision;
                    if (!TryExecuteEnemyDecision(decision, decisionSeed))
                    {
                        throw new InvalidOperationException(
                            "Validated enemy Beelzebub choice could not be resolved.");
                    }
                }

                if (_pendingBeelzebubBustResolution?.OwnerSide ==
                    CombatantSide.Enemy)
                {
                    throw new InvalidOperationException(
                        "Enemy Beelzebub choices exceeded the resolution limit.");
                }
            }
            finally
            {
                _isResolvingEnemyBeelzebubChoice = false;
            }
        }

        private static bool IsBetterBeelzebubDiscardCandidate(
            DemonContractInteractionKind kind,
            EnemyActionCandidate candidate,
            EnemyActionCandidate current)
        {
            int candidateRank =
                candidate.DemonContractOptionNumericValue.Value;
            int currentRank = current.DemonContractOptionNumericValue.Value;
            if (candidateRank == currentRank)
            {
                return candidate.DemonContractOptionId.Value <
                    current.DemonContractOptionId.Value;
            }

            return kind ==
                DemonContractInteractionKind.BeelzebubChooseOwnerCard
                    ? candidateRank > currentRank
                    : candidateRank < currentRank;
        }

        private bool HandleNormalTurnStarted(
            CombatantSide actorSide,
            Action resumeAfterPendingBust)
        {
            int playerSoulBefore = Player.Soul.Current;
            int enemySoulBefore = Enemy.Soul.Current;
            IReadOnlyList<ActiveDemonContract> endedPlayerContracts =
                _demonContractResolver.NotifyNormalTurnStarted(
                    this,
                    _activePlayerDemonContracts,
                    actorSide);
            IReadOnlyList<ActiveDemonContract> endedEnemyContracts =
                _demonContractResolver.NotifyNormalTurnStarted(
                    this,
                    _activeEnemyDemonContracts,
                    actorSide);
            foreach (ActiveDemonContract endedContract in endedPlayerContracts)
            {
                _activePlayerDemonContracts.Remove(endedContract);
            }

            foreach (ActiveDemonContract endedContract in endedEnemyContracts)
            {
                _activeEnemyDemonContracts.Remove(endedContract);
            }

            int playerPaidSoulCost = playerSoulBefore - Player.Soul.Current;
            if (playerPaidSoulCost > 0)
            {
                LastDemonContractEffectResult = new DemonContractEffectResult(
                    triggered: true,
                    bustedTarget: null,
                    playerPaidSoulCost);
                RaiseStepped();
            }

            int enemyPaidSoulCost = enemySoulBefore - Enemy.Soul.Current;
            if (enemyPaidSoulCost > 0)
            {
                LastDemonContractEffectResult = new DemonContractEffectResult(
                    triggered: true,
                    bustedTarget: null,
                    enemyPaidSoulCost);
                RaiseStepped();
            }

            if (Player.Soul.IsDepleted || Enemy.Soul.IsDepleted)
            {
                EndBattleWithoutRound();
                return false;
            }

            return ContinueNormalTurnStartedAfterContractEnd(
                endedPlayerContracts.Count > 0 &&
                    Player.VisibleHandValue.IsBust,
                endedEnemyContracts.Count > 0 &&
                    Enemy.VisibleHandValue.IsBust,
                resumeAfterPendingBust);
        }

        private bool ContinueNormalTurnStartedAfterContractEnd(
            bool checkPlayerBust,
            bool checkEnemyBust,
            Action resumeAfterPendingBust)
        {
            if (checkPlayerBust)
            {
                OwnerBustHandlingResult handling = HandlePlayerBust(() =>
                {
                    if (ContinueNormalTurnStartedAfterContractEnd(
                        checkPlayerBust: false,
                        checkEnemyBust,
                        resumeAfterPendingBust))
                    {
                        resumeAfterPendingBust();
                    }
                });
                if (handling == OwnerBustHandlingResult.NotHandled)
                {
                    CompleteRound(RoundResolver.ResolveNumericBust(
                        RoundNumber,
                        playerIsTarget: true));
                    return false;
                }

                if (handling != OwnerBustHandlingResult.Prevented)
                {
                    return false;
                }
            }

            if (checkEnemyBust)
            {
                OwnerBustHandlingResult handling = HandleEnemyBust(() =>
                {
                    if (ContinueNormalTurnStartedAfterContractEnd(
                        checkPlayerBust: false,
                        checkEnemyBust: false,
                        resumeAfterPendingBust))
                    {
                        resumeAfterPendingBust();
                    }
                });
                if (handling == OwnerBustHandlingResult.NotHandled)
                {
                    CompleteRound(RoundResolver.ResolveNumericBust(
                        RoundNumber,
                        playerIsTarget: false));
                    return false;
                }

                if (handling != OwnerBustHandlingResult.Prevented)
                {
                    return false;
                }
            }

            return State != CoreLoopState.BattleEnded;
        }

        private void NotifyNormalTurnEnded(CombatantSide actorSide)
        {
            TurnNumber++;

            IReadOnlyList<ActiveDemonContract> activeContracts =
                actorSide == CombatantSide.Player
                    ? _activePlayerDemonContracts
                    : _activeEnemyDemonContracts;
            _demonContractResolver.NotifyNormalTurnEnded(
                this,
                activeContracts,
                actorSide);
        }

        private void EndBattleWithoutRound()
        {
            CancelPendingEffectResolutions();
            _automaticCardBattleState.ClearRoundState();
            ClearPlayerDemonContractInteraction();
            ClearEnemyDemonContractInteraction();
            _pendingBeelzebubBustResolution = null;
            _activeAzazelCardEffectSequence = null;
            _pendingFixedEnemyAzazelActivation = false;
            _playerAzazelBustPending = false;
            _enemyAzazelBustPending = false;
            _demonContractResolver.NotifyRoundEnded(
                this,
                _activePlayerDemonContracts);
            _demonContractResolver.NotifyRoundEnded(
                this,
                _activeEnemyDemonContracts);
            CleanupBattleContracts();
            State = CoreLoopState.BattleEnded;
            RaiseStepped();
        }

        internal bool PayResurrectionHerbSoulAndRedeal(
            CombatantSide side,
            BlackjackCard sourceCard)
        {
            BattleParticipant participant = GetParticipant(side);
            if (participant.Soul.Current <= 0)
            {
                throw new InvalidOperationException(
                    "A depleted participant cannot pay resurrection herb soul.");
            }

            int? previousHiddenCardId = participant.Hand
                .TryGetSingleHiddenCard(out BlackjackCard hiddenCard)
                    ? hiddenCard.Id
                    : (int?)null;
            ApplySoulDamage(side, 1);
            if (participant.Soul.IsDepleted)
            {
                return false;
            }

            if (previousHiddenCardId.HasValue)
            {
                InvalidateHiddenCardKnowledge(side, previousHiddenCardId.Value);
            }

            int? preservedSourceCardId =
                side == _activeAutomaticCardEffectContext.OwnerSide
                    ? sourceCard.Id
                    : (int?)null;
            participant.RedealRoundHand(preservedSourceCardId);
            return true;
        }

        private bool TryResolveMammonTurnStartChoice(
            CombatantSide ownerSide,
            PendingDemonContractInteraction pending,
            DemonContractOption selectedOption,
            out bool completedOwnerAction)
        {
            completedOwnerAction = false;
            if (pending.Kind != DemonContractInteractionKind.MammonReroll ||
                pending.ContractKind != DemonContractKind.Mammon ||
                !pending.SourceContractCardId.HasValue ||
                !TryGetActiveDemonContract(
                    ownerSide,
                    pending.SourceContractCardId.Value,
                    DemonContractKind.Mammon,
                    out ActiveDemonContract activeContract))
            {
                return false;
            }

            if (selectedOption.OptionId ==
                MammonDemonContractHandler.KeepDieOptionId)
            {
                _demonContractResolver.KeepOwnerMammonDie(
                    this,
                    activeContract);
                SetPendingDemonContractInteraction(ownerSide, pending: null);
                ResumeOwnerTurnAfterTurnStartChoice(ownerSide);
                return true;
            }

            if (selectedOption.OptionId !=
                MammonDemonContractHandler.RerollDieOptionId)
            {
                return false;
            }

            SetPendingDemonContractInteraction(ownerSide, pending: null);
            State = ownerSide == CombatantSide.Player
                ? CoreLoopState.PlayerTurn
                : CoreLoopState.EnemyTurn;
            completedOwnerAction = true;
            return TryBeginMammonReroll(
                ownerSide,
                activeContract.SourceCardId);
        }

        private static PendingDemonContractInteraction CreateMammonRerollInteraction(
            int interactionId,
            ActiveDemonContract activeContract)
        {
            var options = new[]
            {
                new DemonContractOption(
                    MammonDemonContractHandler.KeepDieOptionId,
                    contractCardId: null,
                    numericValue: null,
                    "현재 주사위 유지 · 정상 차례 진행"),
                new DemonContractOption(
                    MammonDemonContractHandler.RerollDieOptionId,
                    contractCardId: null,
                    numericValue: null,
                    "주사위 다시 굴리기 · 턴 종료")
            };

            return new PendingDemonContractInteraction(
                interactionId,
                DemonContractInteractionKind.MammonReroll,
                DemonContractKind.Mammon,
                options,
                "현재 값을 유지하고 행동하거나, 다시 굴리고 턴을 종료하십시오.",
                activeContract.SourceCardId);
        }

        private void CancelPendingEffectResolutions()
        {
            if (_activeCardEffectContext?.SourceCard.UseState ==
                CardUseState.Resolving)
            {
                _activeCardEffectContext.SourceCard.TryCompleteUse();
            }

            _activeCardEffectContext = null;
            _activeCardEffectActorSide = null;
            _pendingCardEffect = null;
            _activeLeviathanCardEffectSequence = null;
            _activeAutomaticCardEffectContext = null;
            _automaticCardContinuation = null;
            _pendingAutomaticCardInteraction = null;
            _belialForcedCardEffectContinuation = null;
        }

        private void CleanupBattleContracts()
        {
            RestorePendingPaimonPeek();
            _demonContractCardState.RestoreAll();
            CleanupInjectedPoisonCards();
            _demonContractResolver.NotifyBattleEnded(
                this,
                _activePlayerDemonContracts);
            _demonContractResolver.NotifyBattleEnded(
                this,
                _activeEnemyDemonContracts);
            _activePlayerDemonContracts.Clear();
            _activeEnemyDemonContracts.Clear();
            _resolvedPaimonOpponentBustContractIds.Clear();
        }

        private void InjectPoisonIntoPlayerDeck()
        {
            if (!_injectsPoisonIntoPlayerDeckEachRound)
            {
                return;
            }

            Player.Deck.ResetAvailableCards();
            while (_nextInjectedPoisonCardId >= 0 &&
                Player.Deck.ContainsKnownCardId(_nextInjectedPoisonCardId))
            {
                _nextInjectedPoisonCardId--;
            }

            if (_nextInjectedPoisonCardId < 0)
            {
                throw new InvalidOperationException(
                    "No card id remains for an injected poison card.");
            }

            BlackjackCard poison = new BlackjackCard(
                _nextInjectedPoisonCardId--,
                CardDefinitionCatalog.GetByKey(CardDefinitionCatalog.PoisonKey));
            if (!Player.Deck.TryAddTemporaryAvailableCard(poison))
            {
                throw new InvalidOperationException(
                    "Validated poison card could not be added to the player deck.");
            }

            _injectedPoisonCardIds.Add(poison.Id);
        }

        private void CleanupInjectedPoisonCards()
        {
            foreach (int cardId in _injectedPoisonCardIds)
            {
                if (!Player.TryRemoveTemporaryCard(cardId))
                {
                    throw new InvalidOperationException(
                        $"Injected poison card id {cardId} could not be removed.");
                }
            }

            _injectedPoisonCardIds.Clear();
        }

        private void ResolveRoundWithEnemyFinalChoice(int playerBonus)
        {
            if (_demonContractResolver.TryGetOwnerFinalChoiceContract(
                this,
                _activeEnemyDemonContracts,
                CombatantSide.Enemy,
                out ActiveDemonContract choiceContract))
            {
                _playerFinalBonusForEnemyChoice = playerBonus;
                int interactionId = TakeNextDemonContractInteractionId();
                _pendingEnemyDemonContractInteraction =
                    CreateMammonFinalChoiceInteraction(interactionId, choiceContract);
                State = CoreLoopState.EnemyTurn;
                RaiseStepped();

                int decisionSeed = CreateEnemyDecisionSeed();
                EnemyDecision decision = DecideEnemyAction(decisionSeed);
                if (!TryExecuteEnemyDecision(decision, decisionSeed))
                {
                    throw new InvalidOperationException(
                        "Validated enemy final contract decision could not be executed.");
                }

                return;
            }

            ResolveRoundWithBonuses(playerBonus, enemyBonus: 0);
        }

        private void ResolveRoundWithBonuses(int playerBonus, int enemyBonus)
        {
            LastResolutionPlayerBonus = playerBonus;
            LastResolutionEnemyBonus = enemyBonus;
            RoundResolution resolution = RoundResolver.Resolve(
                RoundNumber,
                Player.Hand.Cards,
                Enemy.Hand.Cards,
                playerBonus,
                enemyBonus);
            OwnerBustHandlingResult handling = HandleShowdownBustReplacement(
                resolution,
                () => ResolveRoundWithBonuses(playerBonus, enemyBonus));
            if (handling != OwnerBustHandlingResult.NotHandled)
            {
                return;
            }

            CompleteRound(resolution);
        }

        private OwnerBustHandlingResult HandleShowdownBustReplacement(
            RoundResolution resolution,
            Action resume)
        {
            switch (resolution.Outcome)
            {
                case RoundOutcome.PlayerBust:
                    return HandleOwnerBust(
                        CombatantSide.Player,
                        _activePlayerDemonContracts,
                        resume,
                        allowAbsolutePrevention: false);
                case RoundOutcome.EnemyBust:
                    return HandleOwnerBust(
                        CombatantSide.Enemy,
                        _activeEnemyDemonContracts,
                        resume,
                        allowAbsolutePrevention: false);
                default:
                    return OwnerBustHandlingResult.NotHandled;
            }
        }

        private void CompleteRound(RoundResolution resolution)
        {
            if (TryBeginPaimonOpponentBustChoice(resolution))
            {
                return;
            }

            ResolvePaimonRoundEndCost(resolution);
        }

        private bool TryBeginPaimonOpponentBustChoice(
            RoundResolution resolution)
        {
            if (_pendingPaimonExileResolution != null)
            {
                throw new InvalidOperationException(
                    "A Paimon exile choice is already pending.");
            }

            if (TryGetPaimonOpponentBustChoice(
                    CombatantSide.Player,
                    resolution,
                    out ActiveDemonContract playerContract))
            {
                BeginPaimonOpponentBustChoice(playerContract, resolution);
                return true;
            }

            if (TryGetPaimonOpponentBustChoice(
                    CombatantSide.Enemy,
                    resolution,
                    out ActiveDemonContract enemyContract))
            {
                BeginPaimonOpponentBustChoice(enemyContract, resolution);
                return true;
            }

            return false;
        }

        private bool TryGetPaimonOpponentBustChoice(
            CombatantSide ownerSide,
            RoundResolution resolution,
            out ActiveDemonContract activeContract)
        {
            activeContract = null;
            if (!DidOpponentBust(ownerSide, resolution))
            {
                return false;
            }

            IReadOnlyList<ActiveDemonContract> activeContracts =
                ownerSide == CombatantSide.Player
                    ? _activePlayerDemonContracts
                    : _activeEnemyDemonContracts;
            return _demonContractResolver
                .TryGetOwnerOpponentBustChoiceContract(
                    this,
                    activeContracts,
                    ownerSide,
                    _resolvedPaimonOpponentBustContractIds,
                    out activeContract);
        }

        private bool DidOpponentBust(
            CombatantSide ownerSide,
            RoundResolution resolution)
        {
            if (ownerSide == CombatantSide.Player &&
                resolution.Outcome == RoundOutcome.EnemyBust)
            {
                return true;
            }

            if (ownerSide == CombatantSide.Enemy &&
                resolution.Outcome == RoundOutcome.PlayerBust)
            {
                return true;
            }

            if (resolution.Cause != RoundEndCause.TotalComparison)
            {
                return false;
            }

            return GetOpponent(ownerSide).HandValue.IsBust;
        }

        private void BeginPaimonOpponentBustChoice(
            ActiveDemonContract activeContract,
            RoundResolution resolution)
        {
            CombatantSide ownerSide = activeContract.OwnerSide;
            _resolvedPaimonOpponentBustContractIds.Add(
                activeContract.SourceCardId);
            _pendingPaimonExileResolution =
                new PendingPaimonExileResolution(
                    activeContract,
                    resolution);
            SetPendingDemonContractInteraction(
                ownerSide,
                CreatePaimonDeckChoiceInteraction(
                    TakeNextDemonContractInteractionId(),
                    activeContract,
                    GetParticipant(ownerSide),
                    GetOpponent(ownerSide)));
            State = ownerSide == CombatantSide.Player
                ? CoreLoopState.PlayerResolvingDemonContract
                : CoreLoopState.EnemyTurn;
            RaiseStepped();

            if (ownerSide == CombatantSide.Enemy)
            {
                ResolvePendingEnemyPaimonChoices();
            }
        }

        private void ResolvePendingEnemyPaimonChoices()
        {
            if (_isResolvingEnemyPaimonChoice)
            {
                return;
            }

            _isResolvingEnemyPaimonChoice = true;
            try
            {
                const int maximumChoiceCount = 2;
                for (int choiceIndex = 0;
                    choiceIndex < maximumChoiceCount;
                    choiceIndex++)
                {
                    PendingDemonContractInteraction pending =
                        _pendingEnemyDemonContractInteraction;
                    if (_pendingPaimonExileResolution == null ||
                        pending == null ||
                        (pending.Kind !=
                            DemonContractInteractionKind.PaimonChooseDeck &&
                         pending.Kind !=
                            DemonContractInteractionKind.PaimonChooseExileCard))
                    {
                        return;
                    }

                    EnemyActionCandidate selected =
                        SelectEnemyPaimonCandidate(pending);
                    int decisionSeed = CreateEnemyDecisionSeed();
                    EnemyDecision decision = EnemyDecision.FromCandidate(
                        selected,
                        pending.Kind ==
                            DemonContractInteractionKind.PaimonChooseDeck
                            ? "paimon-inspect-opponent-deck"
                            : selected.DemonContractOptionNumericValue.HasValue
                                ? "paimon-exile-highest-opponent-card"
                                : "paimon-preserve-own-deck");
                    LastEnemyDecision = decision;
                    if (!TryExecuteEnemyDecision(decision, decisionSeed))
                    {
                        throw new InvalidOperationException(
                            "Validated enemy Paimon choice could not be resolved.");
                    }
                }

                if (_pendingPaimonExileResolution?.ActiveContract.OwnerSide ==
                    CombatantSide.Enemy)
                {
                    throw new InvalidOperationException(
                        "Enemy Paimon choices exceeded the resolution limit.");
                }
            }
            finally
            {
                _isResolvingEnemyPaimonChoice = false;
            }
        }

        private EnemyActionCandidate SelectEnemyPaimonCandidate(
            PendingDemonContractInteraction pending)
        {
            EnemyObservation observation = EnemyObservationFactory.Create(
                this,
                CreateEnemyDecisionSeed());
            EnemyActionCandidate selected = null;
            foreach (EnemyActionCandidate candidate in
                observation.ActionCandidates)
            {
                if (candidate.DemonContractInteractionKind != pending.Kind)
                {
                    continue;
                }

                if (pending.Kind ==
                    DemonContractInteractionKind.PaimonChooseDeck)
                {
                    if (selected == null ||
                        candidate.DemonContractOptionId ==
                            PaimonDemonContractHandler.OpponentDeckOptionId)
                    {
                        selected = candidate;
                    }

                    continue;
                }

                if (_pendingPaimonExileResolution.ChosenDeckSide ==
                    CombatantSide.Enemy)
                {
                    if (!candidate.DemonContractOptionNumericValue.HasValue)
                    {
                        selected = candidate;
                    }

                    continue;
                }

                if (candidate.DemonContractOptionNumericValue.HasValue &&
                    (selected == null ||
                     !selected.DemonContractOptionNumericValue.HasValue ||
                     candidate.DemonContractOptionNumericValue.Value >
                        selected.DemonContractOptionNumericValue.Value))
                {
                    selected = candidate;
                }
            }

            return selected ?? throw new InvalidOperationException(
                "Enemy Paimon choice has no valid candidate.");
        }

        private void ResolvePaimonRoundEndCost(RoundResolution resolution)
        {
            CombatantSide? winnerSide = GetRoundWinner(resolution.Outcome);
            if (!winnerSide.HasValue)
            {
                // A mutual loss has no winner, so Paimon's round-end downside (which
                // targets the winner) has nothing to apply to.
                FinalizeRound(resolution);
                return;
            }

            IReadOnlyList<ActiveDemonContract> winnerContracts =
                winnerSide.Value == CombatantSide.Player
                    ? _activePlayerDemonContracts
                    : _activeEnemyDemonContracts;
            if (!_demonContractResolver.BustsOwnerAtRoundEnd(
                    this,
                    winnerContracts,
                    winnerSide.Value))
            {
                FinalizeRound(resolution);
                return;
            }

            OwnerBustHandlingResult handling = HandleOwnerBust(
                winnerSide.Value,
                winnerContracts,
                () => FinalizeRound(resolution));
            if (handling == OwnerBustHandlingResult.NotHandled)
            {
                LastDemonContractEffectResult =
                    new DemonContractEffectResult(
                        triggered: true,
                        bustedTarget: winnerSide,
                        paidSoulCost: 0);
                RaiseStepped();
                FinalizeRound(RoundResolver.ResolveContractEffectBust(
                    RoundNumber,
                    playerIsTarget: winnerSide == CombatantSide.Player));
            }
            else if (handling == OwnerBustHandlingResult.Prevented)
            {
                FinalizeRound(resolution);
            }
        }

        private static CombatantSide? GetRoundWinner(RoundOutcome outcome)
        {
            switch (outcome)
            {
                case RoundOutcome.EnemyBust:
                case RoundOutcome.PlayerWin:
                case RoundOutcome.PlayerTwentyOneWin:
                    return CombatantSide.Player;
                case RoundOutcome.PlayerBust:
                case RoundOutcome.EnemyWin:
                    return CombatantSide.Enemy;
                case RoundOutcome.MutualLoss:
                    return null;
                default:
                    throw new ArgumentOutOfRangeException(nameof(outcome));
            }
        }

        private void RestorePendingPaimonPeek()
        {
            PendingPaimonExileResolution resolution =
                _pendingPaimonExileResolution;
            if (resolution?.ChosenDeckOwner != null &&
                resolution.PeekedCards != null)
            {
                resolution.ChosenDeckOwner.Deck.ReturnToTop(
                    resolution.PeekedCards);
            }

            _pendingPaimonExileResolution = null;
        }

        private void FinalizeRound(RoundResolution resolution)
        {
            ClearPlayerDemonContractInteraction();
            ClearEnemyDemonContractInteraction();
            _pendingBeelzebubBustResolution = null;
            _activeAzazelCardEffectSequence = null;
            _playerAzazelBustPending = false;
            _enemyAzazelBustPending = false;
            _demonContractResolver.NotifyRoundEnded(
                this,
                _activePlayerDemonContracts);
            _demonContractResolver.NotifyRoundEnded(
                this,
                _activeEnemyDemonContracts);
            State = CoreLoopState.ResolvingRound;
            _damageApplier.TryApply(resolution, Player.Soul, Enemy.Soul);
            AdvanceFixedEnemyDemonContractPhases();
            _automaticCardBattleState.ResolvePoisonWinRewards(
                resolution,
                RoundNumber,
                Player,
                Enemy);
            _automaticCardBattleState.ClearRoundState();
            LastRoundTransition = null;
            LastResolution = resolution;
            RaiseStepped();

            bool battleEnded = Player.Soul.IsDepleted || Enemy.Soul.IsDepleted;
            if (battleEnded)
            {
                CleanupBattleContracts();
            }

            Player.ClearRound();
            Enemy.ClearRound();

            if (battleEnded)
            {
                State = CoreLoopState.BattleEnded;
                return;
            }

            StartRound();
        }
    }
}
