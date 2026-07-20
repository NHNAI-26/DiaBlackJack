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
        private static readonly IReadOnlyList<BlackjackCard> NoChangeCandidates =
            Array.AsReadOnly(Array.Empty<BlackjackCard>());

        private readonly IEnemyBehaviorPolicy _enemyPolicy;
        private readonly CardEffectResolver _cardEffectResolver;
        private readonly RoundDamageApplier _damageApplier = new RoundDamageApplier();
        private readonly List<PublicCombatAction> _publicActionHistory =
            new List<PublicCombatAction>();
        private CardEffectContext _activeCardEffectContext;
        private CombatantSide? _activeCardEffectActorSide;
        private PendingCardEffect _pendingCardEffect;
        private int _enemyDecisionOrdinal;
        private PlayerChangeSelection _playerChangeSelection;

        public CoreLoopBattle(
            BlackjackDeck playerDeck,
            BlackjackDeck enemyDeck,
            int playerMaximumSoul = 12,
            int enemyMaximumSoul = 3,
            IEnemyBehaviorPolicy enemyPolicy = null)
            : this(
                playerDeck,
                enemyDeck,
                playerMaximumSoul,
                playerMaximumSoul,
                enemyMaximumSoul,
                enemyPolicy)
        {
        }

        public CoreLoopBattle(
            BlackjackDeck playerDeck,
            BlackjackDeck enemyDeck,
            int playerMaximumSoul,
            int playerCurrentSoul,
            int enemyMaximumSoul,
            IEnemyBehaviorPolicy enemyPolicy = null)
            : this(
                playerDeck,
                enemyDeck,
                playerMaximumSoul,
                playerCurrentSoul,
                enemyMaximumSoul,
                enemyPolicy,
                CardEffectResolver.CreateDefault())
        {
        }

        internal CoreLoopBattle(
            BlackjackDeck playerDeck,
            BlackjackDeck enemyDeck,
            int playerMaximumSoul,
            int playerCurrentSoul,
            int enemyMaximumSoul,
            IEnemyBehaviorPolicy enemyPolicy,
            CardEffectResolver cardEffectResolver)
        {
            Player = new BattleParticipant(playerDeck, playerMaximumSoul, playerCurrentSoul);
            Enemy = new BattleParticipant(enemyDeck, enemyMaximumSoul);
            _enemyPolicy = enemyPolicy ?? new SimpleEnemyPolicy();
            _cardEffectResolver = cardEffectResolver ??
                throw new ArgumentNullException(nameof(cardEffectResolver));
            State = CoreLoopState.Initializing;
        }

        public BattleParticipant Player { get; }

        public BattleParticipant Enemy { get; }

        public CoreLoopState State { get; private set; }

        public int RoundNumber { get; private set; }

        public RoundResolution? LastResolution { get; private set; }

        public CardEffectResult? LastCardEffectResult { get; private set; }

        public CombatantSide? LastCardEffectActorSide { get; private set; }

        public EnemyDecision LastEnemyDecision { get; private set; }

        public bool CanPlayerAct => State == CoreLoopState.PlayerTurn && !Player.IsStanding;

        public bool CanPlayerFold => CanPlayerAct;

        public bool CanBeginPlayerChange =>
            CanPlayerAct &&
            !HasPlayerChangedThisRound &&
            _playerChangeSelection == null &&
            Player.Hand.HiddenCardCount == 1 &&
            Player.Deck.CanDraw(2);

        public bool CanSelectChangedCard =>
            State == CoreLoopState.PlayerChoosingChangeCard &&
            _playerChangeSelection != null;

        public bool HasPlayerChangedThisRound { get; private set; }

        public IReadOnlyList<BlackjackCard> PlayerChangeCandidates =>
            _playerChangeSelection?.Candidates ?? NoChangeCandidates;

        public PendingCardEffect PendingPlayerCardEffect =>
            _activeCardEffectActorSide == CombatantSide.Player
                ? _pendingCardEffect
                : null;

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

        public bool TryPlayerHit()
        {
            if (!CanAcceptPlayerAction())
            {
                return false;
            }

            RecordPublicAction(CombatantSide.Player, PublicCombatActionType.Hit);
            Player.Draw(faceUp: true);
            if (Player.HandValue.IsBust)
            {
                ResolveRound();
                return true;
            }

            RunEnemyTurn();
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
            RunEnemyTurn();
            return true;
        }

        public bool TryPlayerFold()
        {
            if (!CanAcceptPlayerAction())
            {
                return false;
            }

            RecordPublicAction(CombatantSide.Player, PublicCombatActionType.Fold);
            CompleteRound(RoundResolver.ResolvePlayerFold(RoundNumber));
            return true;
        }

        public bool TryBeginPlayerChange()
        {
            if (!CanBeginPlayerChange)
            {
                return false;
            }

            if (!Player.TryBeginChange(out PlayerChangeSelection selection))
            {
                return false;
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
            HasPlayerChangedThisRound = true;
            RecordPublicAction(CombatantSide.Player, PublicCombatActionType.Change);

            RunEnemyTurn();
            return true;
        }

        private bool CanAcceptPlayerAction()
        {
            return CanPlayerAct;
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
                RunEnemyTurn();
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
                RunEnemyTurn();
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

            if (step.RoundResolution.HasValue)
            {
                CompleteRound(step.RoundResolution.Value);
                return CardEffectApplicationResult.RoundEnded;
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
            HasPlayerChangedThisRound = false;

            Player.Draw(faceUp: true);
            Enemy.Draw(faceUp: true);
            Player.Draw(faceUp: false);
            Enemy.Draw(faceUp: false);

            State = CoreLoopState.PlayerTurn;
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
                        State = CoreLoopState.PlayerTurn;
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
                    if (Enemy.HandValue.IsBust)
                    {
                        ResolveRound();
                    }
                    else if (!Player.IsStanding)
                    {
                        State = CoreLoopState.PlayerTurn;
                    }

                    executed = true;
                    break;

                case EnemyActionType.Stand:
                    RecordPublicAction(CombatantSide.Enemy, PublicCombatActionType.Stand);
                    Enemy.Stand();
                    if (Player.IsStanding)
                    {
                        ResolveRound();
                    }
                    else
                    {
                        State = CoreLoopState.PlayerTurn;
                    }

                    executed = true;
                    break;

                case EnemyActionType.Fold:
                    RecordPublicAction(CombatantSide.Enemy, PublicCombatActionType.Fold);
                    CompleteRound(RoundResolver.ResolveEnemyFold(RoundNumber));
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
                        State = CoreLoopState.PlayerTurn;
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
            RoundResolution resolution = RoundResolver.Resolve(
                RoundNumber,
                Player.Hand.Cards,
                Enemy.Hand.Cards);
            CompleteRound(resolution);
        }

        private void CompleteRound(RoundResolution resolution)
        {
            State = CoreLoopState.ResolvingRound;
            _damageApplier.TryApply(resolution, Player.Soul, Enemy.Soul);
            LastResolution = resolution;

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
