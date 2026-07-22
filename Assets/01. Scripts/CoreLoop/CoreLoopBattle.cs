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

        private static readonly IReadOnlyList<BlackjackCard> NoChangeCandidates =
            Array.AsReadOnly(Array.Empty<BlackjackCard>());

        private readonly IEnemyBehaviorPolicy _enemyPolicy;
        private readonly CardEffectResolver _cardEffectResolver;
        private readonly DemonContractResolver _demonContractResolver;
        private readonly RoundDamageApplier _damageApplier = new RoundDamageApplier();
        private readonly List<ActiveDemonContract> _activePlayerDemonContracts =
            new List<ActiveDemonContract>();
        private readonly List<PublicCombatAction> _publicActionHistory =
            new List<PublicCombatAction>();
        private CardEffectContext _activeCardEffectContext;
        private CombatantSide? _activeCardEffectActorSide;
        private PendingCardEffect _pendingCardEffect;
        private int _enemyDecisionOrdinal;
        private int _nextDemonContractInteractionId = 1;
        private PendingDemonContractInteraction _pendingPlayerDemonContractInteraction;
        private PlayerDemonContractPreview _playerDemonContractPreview;
        private IReadOnlyList<DemonContractCard> _playerDemonContractCandidates;
        private int _playerDemonContractSoulAfterCost;
        private PlayerChangeSelection _playerChangeSelection;

        public CoreLoopBattle(
            BlackjackDeck playerDeck,
            BlackjackDeck enemyDeck,
            int playerMaximumSoul = 12,
            int enemyMaximumSoul = 3,
            IEnemyBehaviorPolicy enemyPolicy = null,
            DemonContractDeck playerDemonDeck = null)
            : this(
                playerDeck,
                enemyDeck,
                playerMaximumSoul,
                playerMaximumSoul,
                enemyMaximumSoul,
                enemyPolicy,
                playerDemonDeck)
        {
        }

        public CoreLoopBattle(
            BlackjackDeck playerDeck,
            BlackjackDeck enemyDeck,
            int playerMaximumSoul,
            int playerCurrentSoul,
            int enemyMaximumSoul,
            IEnemyBehaviorPolicy enemyPolicy = null,
            DemonContractDeck playerDemonDeck = null)
            : this(
                playerDeck,
                enemyDeck,
                playerMaximumSoul,
                playerCurrentSoul,
                enemyMaximumSoul,
                enemyPolicy,
                CardEffectResolver.CreateDefault(),
                playerDemonDeck)
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
            DemonContractResolver demonContractResolver = null)
        {
            Player = new BattleParticipant(playerDeck, playerMaximumSoul, playerCurrentSoul);
            Enemy = new BattleParticipant(enemyDeck, enemyMaximumSoul);
            PlayerDemonDeck = playerDemonDeck ??
                new DemonContractDeck(Array.Empty<DemonContractCard>(), seed: 0);
            _enemyPolicy = enemyPolicy ?? new SimpleEnemyPolicy();
            _cardEffectResolver = cardEffectResolver ??
                throw new ArgumentNullException(nameof(cardEffectResolver));
            _demonContractResolver = demonContractResolver ??
                DemonContractResolver.CreateDefault();
            State = CoreLoopState.Initializing;
        }

        public BattleParticipant Player { get; }

        public DemonContractDeck PlayerDemonDeck { get; }

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

        public IReadOnlyList<ActiveDemonContract> ActivePlayerDemonContracts =>
            _activePlayerDemonContracts.AsReadOnly();

        public int UsedPlayerBaseDemonContractCount { get; private set; }

        public DemonContractResult LastDemonContractResult { get; private set; }

        public DemonContractEffectResult LastDemonContractEffectResult { get; private set; }

        public PendingDemonContractInteraction PendingPlayerDemonContractInteraction =>
            _pendingPlayerDemonContractInteraction;

        public PlayerDemonContractPreview PlayerDemonContractPreview =>
            _playerDemonContractPreview;

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
                mammonState.CurrentDieValue == 6)
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
                ClearPlayerDemonContractInteraction();
                _demonContractResolver.NotifyRoundEnded(
                    this,
                    _activePlayerDemonContracts);
                State = CoreLoopState.BattleEnded;
                RaiseStepped();
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
            if (Player.VisibleHandValue.IsBust)
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
                CreateBelphegorTopCardInteraction(interactionId);
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

            if (result.OwnerBusted)
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
            ResolveRoundWithPlayerBonus(playerBonus);
            return true;
        }

        public bool TryPlayerStand()
        {
            if (!CanAcceptPlayerAction())
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
            int interactionId)
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
                "확인한 덱 위 카드를 처리하십시오.");
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

        private void ClearPlayerDemonContractInteraction()
        {
            _pendingPlayerDemonContractInteraction = null;
            _playerDemonContractPreview = null;
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
                    return State == CoreLoopState.EnemyTurn && !Enemy.IsStanding;
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

            if (step.RoundResolution.HasValue)
            {
                CompleteRound(step.RoundResolution.Value);
                return CardEffectApplicationResult.RoundEnded;
            }

            if (actorSide == CombatantSide.Player &&
                _demonContractResolver.TryResolvePlayerAfterCardEffect(
                    this,
                    _activePlayerDemonContracts,
                    result,
                    out DemonContractAfterCardEffectStep contractStep))
            {
                LastDemonContractEffectResult = contractStep.Result;
                RaiseStepped();

                if (contractStep.RoundResolution.HasValue)
                {
                    CompleteRound(contractStep.RoundResolution.Value);
                    return CardEffectApplicationResult.RoundEnded;
                }

                if (Player.Soul.IsDepleted)
                {
                    ClearPlayerDemonContractInteraction();
                    _demonContractResolver.NotifyRoundEnded(
                        this,
                        _activePlayerDemonContracts);
                    State = CoreLoopState.BattleEnded;
                    RaiseStepped();
                    return CardEffectApplicationResult.RoundEnded;
                }
            }

            State = actorSide == CombatantSide.Player
                ? CoreLoopState.PlayerTurn
                : CoreLoopState.EnemyTurn;
            return CardEffectApplicationResult.Completed;
        }

        private void StartRound()
        {
            State = CoreLoopState.StartingRound;
            RoundNumber++;
            _activeCardEffectContext = null;
            _activeCardEffectActorSide = null;
            _pendingCardEffect = null;
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

        private void CompletePlayerActionAndRunEnemyTurn()
        {
            if (State != CoreLoopState.PlayerTurn)
            {
                throw new InvalidOperationException(
                    "A player action can only complete from the player turn state.");
            }

            if (!Player.IsStanding &&
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

            while (State == CoreLoopState.EnemyTurn)
            {
                if (Enemy.IsStanding && PendingEnemyCardEffect == null)
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
                    RecordPublicAction(CombatantSide.Enemy, PublicCombatActionType.Hit);
                    Enemy.Draw(faceUp: true);
                    RaiseStepped();
                    if (Enemy.VisibleHandValue.IsBust)
                    {
                        CompleteRound(RoundResolver.ResolveNumericBust(
                            RoundNumber,
                            playerIsTarget: false));
                    }
                    else if (!Player.IsStanding)
                    {
                        BeginPlayerTurn();
                    }

                    executed = true;
                    break;

                case EnemyActionType.Stand:
                    RecordPublicAction(CombatantSide.Enemy, PublicCombatActionType.Stand);
                    Enemy.Stand();
                    RaiseStepped();
                    if (Player.IsStanding)
                    {
                        ResolveRound();
                    }
                    else
                    {
                        BeginPlayerTurn();
                    }

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
                        State == CoreLoopState.EnemyTurn &&
                        !Player.IsStanding)
                    {
                        BeginPlayerTurn();
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

            ResolveRoundWithPlayerBonus(playerBonus: 0);
        }

        private void ResolveRoundWithPlayerBonus(int playerBonus)
        {
            RoundResolution resolution = RoundResolver.Resolve(
                RoundNumber,
                Player.Hand.Cards,
                Enemy.Hand.Cards,
                playerBonus);
            CompleteRound(resolution);
        }

        private void CompleteRound(RoundResolution resolution)
        {
            ClearPlayerDemonContractInteraction();
            _demonContractResolver.NotifyRoundEnded(
                this,
                _activePlayerDemonContracts);
            State = CoreLoopState.ResolvingRound;
            _damageApplier.TryApply(resolution, Player.Soul, Enemy.Soul);
            LastResolution = resolution;
            RaiseStepped();

            Player.ClearRound();
            Enemy.ClearRound();

            if (Player.Soul.IsDepleted || Enemy.Soul.IsDepleted)
            {
                State = CoreLoopState.BattleEnded;
                return;
            }

            StartRound();
        }
    }
}
