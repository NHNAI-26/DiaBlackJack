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

    public sealed class CoreLoopBattle
    {
        public const int BasePlayerDemonContractSoulCost = 1;
        public const int BasePlayerDemonContractUseLimit = 1;
        public const int BaseEnemyDemonContractSoulCost = 1;
        public const int BaseEnemyDemonContractUseLimit = 1;

        private static readonly IReadOnlyList<BlackjackCard> NoChangeCandidates =
            Array.AsReadOnly(Array.Empty<BlackjackCard>());

        private readonly IEnemyBehaviorPolicy _enemyPolicy;
        private readonly CardEffectResolver _cardEffectResolver;
        private readonly AutomaticCardEffectResolver _automaticCardEffectResolver;
        private readonly DemonContractResolver _demonContractResolver;
        private readonly AutomaticCardBattleState _automaticCardBattleState =
            new AutomaticCardBattleState();
        private readonly RoundDamageApplier _damageApplier = new RoundDamageApplier();
        private readonly List<ActiveDemonContract> _activePlayerDemonContracts =
            new List<ActiveDemonContract>();
        private readonly List<ActiveDemonContract> _activeEnemyDemonContracts =
            new List<ActiveDemonContract>();
        private readonly List<PublicCombatAction> _publicActionHistory =
            new List<PublicCombatAction>();
        private CardEffectContext _activeCardEffectContext;
        private CombatantSide? _activeCardEffectActorSide;
        private PendingCardEffect _pendingCardEffect;
        private AutomaticCardEffectContext _activeAutomaticCardEffectContext;
        private AutomaticCardContinuation _automaticCardContinuation;
        private PendingAutomaticCardInteraction _pendingAutomaticCardInteraction;
        private int _nextAutomaticCardInteractionId = 1;
        private int _nextTemporaryCardId = int.MaxValue;
        private int _enemyDecisionOrdinal;
        private int _nextDemonContractInteractionId = 1;
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

        public CoreLoopBattle(
            BlackjackDeck playerDeck,
            BlackjackDeck enemyDeck,
            int playerMaximumSoul = 12,
            int enemyMaximumSoul = 3,
            IEnemyBehaviorPolicy enemyPolicy = null,
            DemonContractDeck playerDemonDeck = null,
            DemonContractDeck enemyDemonDeck = null)
            : this(
                playerDeck,
                enemyDeck,
                playerMaximumSoul,
                playerMaximumSoul,
                enemyMaximumSoul,
                enemyPolicy,
                playerDemonDeck,
                enemyDemonDeck)
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
            DemonContractDeck enemyDemonDeck = null)
            : this(
                playerDeck,
                enemyDeck,
                playerMaximumSoul,
                playerCurrentSoul,
                enemyMaximumSoul,
                enemyPolicy,
                CardEffectResolver.CreateDefault(),
                playerDemonDeck,
                enemyDemonDeck: enemyDemonDeck)
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
            AutomaticCardEffectResolver automaticCardEffectResolver = null)
        {
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
            _demonContractResolver = demonContractResolver ??
                DemonContractResolver.CreateDefault();
            State = CoreLoopState.Initializing;
        }

        public BattleParticipant Player { get; }

        public DemonContractDeck PlayerDemonDeck { get; }

        public DemonContractDeck EnemyDemonDeck { get; }

        public BattleParticipant Enemy { get; }

        internal IEnemyBehaviorPolicy EnemyBehaviorPolicy => _enemyPolicy;

        public CoreLoopState State { get; private set; }

        public int RoundNumber { get; private set; }

        public RoundResolution? LastResolution { get; private set; }

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

        public bool CanSelectChangedCard =>
            State == CoreLoopState.PlayerChoosingChangeCard &&
            _playerChangeSelection != null;

        public int CompletedPlayerChangeCount { get; private set; }

        public int NextPlayerChangeSoulCost => CompletedPlayerChangeCount;

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

        internal int PendingPoisonWinRewardCount =>
            _automaticCardBattleState.PendingPoisonWinRewardCount;

        public AutomaticCardResult? LastAutomaticCardResult { get; private set; }

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

        public DemonContractResult LastDemonContractResult { get; private set; }

        public DemonContractEffectResult LastDemonContractEffectResult { get; private set; }

        public PendingDemonContractInteraction PendingPlayerDemonContractInteraction =>
            _pendingPlayerDemonContractInteraction;

        public PlayerDemonContractPreview PlayerDemonContractPreview =>
            _playerDemonContractPreview;

        public PendingDemonContractInteraction PendingEnemyDemonContractInteraction =>
            _pendingEnemyDemonContractInteraction;

        internal PlayerDemonContractPreview EnemyDemonContractPreview =>
            _enemyDemonContractPreview;

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
            if (candidates.Count != DemonContractDeck.CandidateCount)
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
                case DemonContractInteractionKind.BelphegorTopCard:
                    return TryResolveBelphegorTopCard(pending, selectedOption);
                case DemonContractInteractionKind.MammonReroll:
                    return TryResolveMammonReroll(pending, selectedOption);
                case DemonContractInteractionKind.MammonApplyDie:
                    return TryResolveMammonFinalChoice(pending, selectedOption);
                default:
                    throw new ArgumentOutOfRangeException(nameof(pending));
            }
        }

        private bool TryBeginEnemyDemonContract()
        {
            DemonContractAvailability availability = EnemyDemonContractAvailability;
            if (!availability.CanBegin)
            {
                return false;
            }

            Enemy.Soul.ApplyDamage(availability.SoulCost);
            UsedEnemyBaseDemonContractCount = checked(
                UsedEnemyBaseDemonContractCount + 1);
            _enemyDemonContractSoulAfterCost = Enemy.Soul.Current;
            _enemyDemonContractCandidates = EnemyDemonDeck.TakeCandidates();
            if (_enemyDemonContractCandidates.Count != DemonContractDeck.CandidateCount)
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
                case DemonContractInteractionKind.BelphegorTopCard:
                    completedOwnerAction = true;
                    return TryResolveEnemyBelphegorTopCard(pending, selectedOption);
                case DemonContractInteractionKind.MammonReroll:
                    return TryResolveEnemyMammonReroll(pending, selectedOption);
                case DemonContractInteractionKind.MammonApplyDie:
                    return TryResolveEnemyMammonFinalChoice(pending, selectedOption);
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
                DemonContractDeck.CandidateCount - 1);
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
                discardedCards.Count != DemonContractDeck.CandidateCount - 1)
            {
                return false;
            }

            EnemyDemonDeck.Discard(discardedCards);
            var activeContract = new ActiveDemonContract(
                selectedCard,
                CombatantSide.Enemy,
                new EmptyDemonContractRuntimeState());
            _activeEnemyDemonContracts.Add(activeContract);
            ClearEnemyDemonContractInteraction();
            activeContract.SetRuntimeState(
                _demonContractResolver.Activate(this, activeContract));
            RecordPublicAction(
                CombatantSide.Enemy,
                PublicCombatActionType.DemonContract,
                activeContract.Definition.Key);

            bool enemyDepleted = Enemy.Soul.IsDepleted;
            LastDemonContractResult = new DemonContractResult(
                pending.InteractionId,
                activeContract,
                BaseEnemyDemonContractSoulCost,
                _enemyDemonContractSoulAfterCost,
                Enemy.Soul.Current,
                endedBattle: enemyDepleted);

            if (activeContract.RuntimeState is MammonRuntimeState mammonState &&
                mammonState.CurrentDieValue == 6 &&
                !PreventsEnemyBust())
            {
                LastDemonContractEffectResult = new DemonContractEffectResult(
                    triggered: true,
                    bustedTarget: CombatantSide.Enemy,
                    paidSoulCost: 0);
                CompleteRound(RoundResolver.ResolveContractEffectBust(
                    RoundNumber,
                    playerIsTarget: false));
                return true;
            }

            if (enemyDepleted)
            {
                EndBattleWithoutRound();
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
                    if (Enemy.VisibleHandValue.IsBust && !PreventsEnemyBust())
                    {
                        CompleteRound(RoundResolver.ResolveNumericBust(
                            RoundNumber,
                            playerIsTarget: false));
                    }

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

        private bool TryResolveEnemyMammonReroll(
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

            DemonContractTurnChoiceResult result =
                _demonContractResolver.ResolveOwnerTurnChoice(
                    this,
                    activeContract,
                    selectedOption.OptionId);
            ClearEnemyDemonContractInteraction();

            if (result.OwnerBusted && !PreventsEnemyBust())
            {
                LastDemonContractEffectResult = new DemonContractEffectResult(
                    triggered: true,
                    bustedTarget: CombatantSide.Enemy,
                    paidSoulCost: 0);
                CompleteRound(RoundResolver.ResolveContractEffectBust(
                    RoundNumber,
                    playerIsTarget: false));
                return true;
            }

            State = CoreLoopState.EnemyTurn;
            RaiseStepped();
            return true;
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
                DemonContractDeck.CandidateCount - 1);
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
                discardedCards.Count != DemonContractDeck.CandidateCount - 1)
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

            activeContract.SetRuntimeState(
                _demonContractResolver.Activate(this, activeContract));
            bool playerDepleted = Player.Soul.IsDepleted;
            LastDemonContractResult = new DemonContractResult(
                pending.InteractionId,
                activeContract,
                BasePlayerDemonContractSoulCost,
                _playerDemonContractSoulAfterCost,
                Player.Soul.Current,
                endedBattle: playerDepleted);

            if (activeContract.RuntimeState is MammonRuntimeState mammonState &&
                mammonState.CurrentDieValue == 6 &&
                !PreventsPlayerBust())
            {
                LastDemonContractEffectResult = new DemonContractEffectResult(
                    triggered: true,
                    bustedTarget: CombatantSide.Player,
                    paidSoulCost: 0);
                CompleteRound(RoundResolver.ResolveContractEffectBust(
                    RoundNumber,
                    playerIsTarget: true));
                return true;
            }

            if (playerDepleted)
            {
                EndBattleWithoutRound();
                return true;
            }

            State = CoreLoopState.PlayerTurn;
            RaiseStepped();
            CompletePlayerActionAndRunEnemyTurn();
            return true;
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
            if (Player.VisibleHandValue.IsBust && !PreventsPlayerBust())
            {
                CompleteRound(RoundResolver.ResolveNumericBust(
                    RoundNumber,
                    playerIsTarget: true));
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

        private bool TryResolveMammonReroll(
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

            DemonContractTurnChoiceResult result =
                _demonContractResolver.ResolvePlayerTurnChoice(
                    this,
                    activeContract,
                    selectedOption.OptionId);
            ClearPlayerDemonContractInteraction();

            if (result.OwnerBusted && !PreventsPlayerBust())
            {
                LastDemonContractEffectResult = new DemonContractEffectResult(
                    triggered: true,
                    bustedTarget: CombatantSide.Player,
                    paidSoulCost: 0);
                CompleteRound(RoundResolver.ResolveContractEffectBust(
                    RoundNumber,
                    playerIsTarget: true));
                return true;
            }

            State = CoreLoopState.PlayerTurn;
            RaiseStepped();
            return true;
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
                    "확인한 카드를 공개 히트"),
                new DemonContractOption(
                    BelphegorDemonContractHandler.MoveTopCardToBottomOptionId,
                    contractCardId: null,
                    numericValue: null,
                    "확인한 카드를 덱 아래로 이동")
            };

            return new PendingDemonContractInteraction(
                interactionId,
                DemonContractInteractionKind.BelphegorTopCard,
                DemonContractKind.Belphegor,
                options,
                "확인한 덱 위 카드를 처리하십시오.",
                sourceContractCardId);
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
                    "현재 주사위 유지"),
                new DemonContractOption(
                    MammonDemonContractHandler.RerollDieOptionId,
                    contractCardId: null,
                    numericValue: null,
                    "주사위 다시 굴리기")
            };

            return new PendingDemonContractInteraction(
                interactionId,
                DemonContractInteractionKind.MammonReroll,
                DemonContractKind.Mammon,
                options,
                "현재 값을 유지하거나 주사위를 한 번 다시 굴리십시오.",
                activeContract.SourceCardId);
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
                    "주사위 값 적용 안 함"),
                new DemonContractOption(
                    MammonDemonContractHandler.ApplyDieOptionId,
                    contractCardId: null,
                    numericValue: null,
                    "주사위 값 적용")
            };

            return new PendingDemonContractInteraction(
                interactionId,
                DemonContractInteractionKind.MammonApplyDie,
                DemonContractKind.Mammon,
                options,
                "최종 승부에 현재 주사위 값을 적용할지 선택하십시오.",
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

        internal void ApplySoulDamage(CombatantSide ownerSide, int amount)
        {
            GetParticipant(ownerSide).Soul.ApplyDamage(amount);
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

            _activeAutomaticCardEffectContext =
                new AutomaticCardEffectContext(this, ownerSide, sourceCard);
            _automaticCardContinuation = continuation;
            AutomaticCardEffectStep step =
                _automaticCardEffectResolver.Begin(
                    _activeAutomaticCardEffectContext);
            bool isWaitingForChoice = ApplyAutomaticCardEffectStep(
                step,
                resumeContinuation: false);
            if (!isWaitingForChoice)
            {
                immediateResult = LastAutomaticCardResult;
            }

            return isWaitingForChoice;
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
                resumeContinuation: true);
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
                step.CompletionFlow ==
                    AutomaticCardCompletionFlow.EndBattle)
            {
                throw new InvalidOperationException(
                    "An automatic card cannot end battle before a pending choice resumes.");
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
                return true;
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

            var result = new AutomaticCardResult(
                sourceCard.Id,
                sourceCard.Definition.Effect,
                context.OwnerSide,
                disposition);
            AutomaticCardContinuation continuation =
                _automaticCardContinuation ??
                    throw new InvalidOperationException(
                        "Automatic card effect has no continuation.");

            LastAutomaticCardResult = result;
            _pendingAutomaticCardInteraction = null;
            _activeAutomaticCardEffectContext = null;
            _automaticCardContinuation = null;
            RaiseStepped();

            if (step.CompletionFlow ==
                AutomaticCardCompletionFlow.EndBattle)
            {
                EndBattleWithoutRound();
                return false;
            }

            if (resumeContinuation)
            {
                ResumeAfterAutomaticCard(continuation, result);
            }

            return false;
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
                        CompletePlayerActionAndRunEnemyTurn();
                    }
                    else
                    {
                        CompleteEnemyAction();
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
            CardEffectStep step = _cardEffectResolver.Begin(context);
            _activeCardEffectContext = context;
            _activeCardEffectActorSide = actorSide;
            RecordPublicAction(
                actorSide,
                PublicCombatActionType.UseCard,
                card.DefinitionKey);

            CardEffectApplicationResult applicationResult = ApplyCardEffectStep(step);
            if (actorSide == CombatantSide.Player &&
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
            if (actorSide == CombatantSide.Player &&
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
            if (roundResolution.HasValue &&
                IsBustPrevented(roundResolution.Value))
            {
                result = new CardEffectResult(
                    result.SourceCardId,
                    result.EffectKind,
                    result.Succeeded,
                    endedRound: false);
                roundResolution = null;
            }

            if (!sourceCard.TryCompleteUse())
            {
                throw new InvalidOperationException("Active card effect could not complete its source card.");
            }

            LastCardEffectResult = result;
            LastCardEffectActorSide = _activeCardEffectActorSide;
            CombatantSide actorSide = _activeCardEffectActorSide ??
                throw new InvalidOperationException("Card effect has no actor side.");
            _pendingCardEffect = null;
            _activeCardEffectContext = null;
            _activeCardEffectActorSide = null;
            RaiseStepped();

            if (roundResolution.HasValue)
            {
                CompleteRound(roundResolution.Value);
                return CardEffectApplicationResult.RoundEnded;
            }

            IReadOnlyList<ActiveDemonContract> ownerContracts =
                actorSide == CombatantSide.Player
                    ? _activePlayerDemonContracts
                    : _activeEnemyDemonContracts;
            if (_demonContractResolver.TryResolveOwnerAfterCardEffect(
                    this,
                    ownerContracts,
                    actorSide,
                    result,
                    out DemonContractAfterCardEffectStep contractStep))
            {
                RoundResolution? contractResolution = contractStep.RoundResolution;
                DemonContractEffectResult contractResult = contractStep.Result;
                if (contractResolution.HasValue &&
                    IsBustPrevented(contractResolution.Value))
                {
                    contractResolution = null;
                    contractResult = new DemonContractEffectResult(
                        triggered: true,
                        bustedTarget: null,
                        contractResult.PaidSoulCost);
                }

                LastDemonContractEffectResult = contractResult;
                RaiseStepped();

                if (contractResolution.HasValue)
                {
                    CompleteRound(contractResolution.Value);
                    return CardEffectApplicationResult.RoundEnded;
                }

                if (GetParticipant(actorSide).Soul.IsDepleted)
                {
                    EndBattleWithoutRound();
                    return CardEffectApplicationResult.RoundEnded;
                }
            }

            State = actorSide == CombatantSide.Player
                ? CoreLoopState.PlayerTurn
                : CoreLoopState.EnemyTurn;
            return CardEffectApplicationResult.Completed;
        }

        private bool IsBustPrevented(RoundResolution resolution)
        {
            switch (resolution.Outcome)
            {
                case RoundOutcome.PlayerBust:
                    return PreventsPlayerBust();
                case RoundOutcome.EnemyBust:
                    return PreventsEnemyBust();
                default:
                    return false;
            }
        }

        private void StartRound()
        {
            State = CoreLoopState.StartingRound;
            RoundNumber++;
            _activeCardEffectContext = null;
            _activeCardEffectActorSide = null;
            _pendingCardEffect = null;
            _activeAutomaticCardEffectContext = null;
            _automaticCardContinuation = null;
            _pendingAutomaticCardInteraction = null;
            _automaticCardBattleState.ClearRoundState();
            ClearPlayerDemonContractInteraction();
            ClearEnemyDemonContractInteraction();
            _playerFinalBonusForEnemyChoice = 0;
            _playerChangeSelection = null;
            _publicActionHistory.Clear();
            _enemyDecisionOrdinal = 0;

            Player.Draw(faceUp: true);
            Enemy.Draw(faceUp: true);
            Player.Draw(faceUp: false);
            Enemy.Draw(faceUp: false);

            BeginPlayerTurn();
            RaiseStepped();
        }

        internal BlackjackCard AddTemporaryFaceUpCard(
            CombatantSide ownerSide,
            CardDefinition definition)
        {
            if (!Enum.IsDefined(typeof(CombatantSide), ownerSide))
            {
                throw new ArgumentOutOfRangeException(nameof(ownerSide));
            }

            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            while (Player.Deck.ContainsKnownCardId(_nextTemporaryCardId) ||
                Enemy.Deck.ContainsKnownCardId(_nextTemporaryCardId))
            {
                if (_nextTemporaryCardId == 0)
                {
                    throw new InvalidOperationException(
                        "No physical card id remains for a temporary contract card.");
                }

                _nextTemporaryCardId--;
            }

            int cardId = _nextTemporaryCardId--;
            return GetParticipant(ownerSide).AddTemporaryFaceUpCard(
                cardId,
                definition);
        }

        private void CompletePlayerActionAndRunEnemyTurn()
        {
            if (State != CoreLoopState.PlayerTurn)
            {
                throw new InvalidOperationException(
                    "A player action can only complete from the player turn state.");
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

            RunEnemyTurn();
        }

        private void BeginPlayerTurn()
        {
            State = CoreLoopState.PlayerTurn;
            if (!HandleNormalTurnStarted(CombatantSide.Player))
            {
                return;
            }

            _demonContractResolver.NotifyPlayerTurnStarted(
                this,
                _activePlayerDemonContracts);

            if (_demonContractResolver.TryGetPlayerTurnChoiceContract(
                this,
                _activePlayerDemonContracts,
                out ActiveDemonContract choiceContract))
            {
                int interactionId = TakeNextDemonContractInteractionId();
                _pendingPlayerDemonContractInteraction =
                    CreateMammonRerollInteraction(interactionId, choiceContract);
                State = CoreLoopState.PlayerResolvingDemonContract;
                RaiseStepped();
            }
        }

        private void RunEnemyTurn()
        {
            State = CoreLoopState.EnemyTurn;

            if (!Enemy.IsStanding &&
                PendingEnemyCardEffect == null &&
                _pendingEnemyDemonContractInteraction == null)
            {
                if (!HandleNormalTurnStarted(CombatantSide.Enemy))
                {
                    return;
                }

                _demonContractResolver.NotifyOwnerTurnStarted(
                    this,
                    _activeEnemyDemonContracts,
                    CombatantSide.Enemy);

                if (_demonContractResolver.TryGetOwnerTurnChoiceContract(
                    this,
                    _activeEnemyDemonContracts,
                    CombatantSide.Enemy,
                    out ActiveDemonContract choiceContract))
                {
                    int interactionId = TakeNextDemonContractInteractionId();
                    _pendingEnemyDemonContractInteraction =
                        CreateMammonRerollInteraction(interactionId, choiceContract);
                    RaiseStepped();
                }
            }

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
                EnemyDecision decision = _enemyPolicy.Decide(observation);
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
                    executed = wasPending
                        ? decision.CardEffectOptionId.HasValue &&
                            TryResolveCardChoice(
                                CombatantSide.Enemy,
                                decision.CardEffectOptionId.Value)
                        : decision.CardId.HasValue &&
                            TryBeginCardUse(CombatantSide.Enemy, decision.CardId.Value);

                    if (executed &&
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
                        : TryBeginEnemyDemonContract();

                    if (executed &&
                        completedOwnerAction &&
                        State == CoreLoopState.EnemyTurn &&
                        _pendingEnemyDemonContractInteraction == null)
                    {
                        CompleteEnemyAction();
                    }

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
            if (Enemy.VisibleHandValue.IsBust && !PreventsEnemyBust())
            {
                CompleteRound(RoundResolver.ResolveNumericBust(
                    RoundNumber,
                    playerIsTarget: false));
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

        private void RecordPublicAction(
            CombatantSide actorSide,
            PublicCombatActionType actionType,
            string sourceCardDefinitionKey = null)
        {
            _publicActionHistory.Add(new PublicCombatAction(
                actorSide,
                actionType,
                sourceCardDefinitionKey));
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

        private bool PreventsPlayerBust()
        {
            return _demonContractResolver.PreventsPlayerBust(
                this,
                _activePlayerDemonContracts);
        }

        private bool PreventsEnemyBust()
        {
            return _demonContractResolver.PreventsOwnerBust(
                this,
                _activeEnemyDemonContracts,
                CombatantSide.Enemy);
        }

        private bool HandleNormalTurnStarted(CombatantSide actorSide)
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

            if (endedPlayerContracts.Count > 0 && Player.VisibleHandValue.IsBust)
            {
                CompleteRound(RoundResolver.ResolveNumericBust(
                    RoundNumber,
                    playerIsTarget: true));
                return false;
            }

            if (endedEnemyContracts.Count > 0 && Enemy.VisibleHandValue.IsBust)
            {
                CompleteRound(RoundResolver.ResolveNumericBust(
                    RoundNumber,
                    playerIsTarget: false));
                return false;
            }

            return true;
        }

        private void EndBattleWithoutRound()
        {
            CancelPendingEffectResolutions();
            _automaticCardBattleState.ClearRoundState();
            ClearPlayerDemonContractInteraction();
            ClearEnemyDemonContractInteraction();
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
            _activeAutomaticCardEffectContext = null;
            _automaticCardContinuation = null;
            _pendingAutomaticCardInteraction = null;
        }

        private void CleanupBattleContracts()
        {
            _demonContractResolver.NotifyBattleEnded(
                this,
                _activePlayerDemonContracts);
            _demonContractResolver.NotifyBattleEnded(
                this,
                _activeEnemyDemonContracts);
            _activePlayerDemonContracts.Clear();
            _activeEnemyDemonContracts.Clear();
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
            RoundResolution resolution = RoundResolver.Resolve(
                RoundNumber,
                Player.Hand.Cards,
                Enemy.Hand.Cards,
                playerBonus,
                enemyBonus);
            CompleteRound(resolution);
        }

        private void CompleteRound(RoundResolution resolution)
        {
            ClearPlayerDemonContractInteraction();
            ClearEnemyDemonContractInteraction();
            _demonContractResolver.NotifyRoundEnded(
                this,
                _activePlayerDemonContracts);
            _demonContractResolver.NotifyRoundEnded(
                this,
                _activeEnemyDemonContracts);
            State = CoreLoopState.ResolvingRound;
            _damageApplier.TryApply(resolution, Player.Soul, Enemy.Soul);
            _automaticCardBattleState.ResolvePoisonWinRewards(
                resolution,
                RoundNumber,
                Player,
                Enemy);
            _automaticCardBattleState.ClearRoundState();
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
