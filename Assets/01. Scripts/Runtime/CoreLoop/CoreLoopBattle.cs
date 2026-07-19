using System;
using System.Collections.Generic;

namespace DiaBlackJack.CoreLoop
{
    public sealed class CoreLoopBattle
    {
        private static readonly IReadOnlyList<BlackjackCard> NoChangeCandidates =
            Array.AsReadOnly(Array.Empty<BlackjackCard>());

        private readonly SimpleEnemyPolicy _enemyPolicy;
        private readonly CardEffectResolver _cardEffectResolver;
        private readonly RoundDamageApplier _damageApplier = new RoundDamageApplier();
        private CardEffectContext _activeCardEffectContext;
        private PendingCardEffect _pendingPlayerCardEffect;
        private PlayerChangeSelection _playerChangeSelection;

        public CoreLoopBattle(
            BlackjackDeck playerDeck,
            BlackjackDeck enemyDeck,
            int playerMaximumSoul = 12,
            int enemyMaximumSoul = 3,
            SimpleEnemyPolicy enemyPolicy = null)
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
            SimpleEnemyPolicy enemyPolicy = null)
            : this(
                playerDeck,
                enemyDeck,
                playerMaximumSoul,
                playerCurrentSoul,
                enemyMaximumSoul,
                enemyPolicy,
                new CardEffectResolver())
        {
        }

        internal CoreLoopBattle(
            BlackjackDeck playerDeck,
            BlackjackDeck enemyDeck,
            int playerMaximumSoul,
            int playerCurrentSoul,
            int enemyMaximumSoul,
            SimpleEnemyPolicy enemyPolicy,
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

        public PendingCardEffect PendingPlayerCardEffect => _pendingPlayerCardEffect;

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
            if (!CanUsePlayerCard(cardId) ||
                !Player.Hand.TryGetCard(cardId, out BlackjackCard card))
            {
                return false;
            }

            var context = new CardEffectContext(this, card);
            if (!card.TryBeginUse())
            {
                throw new InvalidOperationException("Validated card could not begin use.");
            }

            card.Reveal();
            _activeCardEffectContext = context;
            ApplyCardEffectStep(_cardEffectResolver.Begin(context));
            return true;
        }

        public bool TryResolvePlayerCardChoice(int optionId)
        {
            if (State != CoreLoopState.PlayerResolvingCardEffect ||
                _pendingPlayerCardEffect == null ||
                _activeCardEffectContext == null ||
                !_pendingPlayerCardEffect.TryGetOption(
                    optionId,
                    out CardEffectChoiceOption selectedOption))
            {
                return false;
            }

            CardEffectStep step = _cardEffectResolver.ResolveChoice(
                _activeCardEffectContext,
                _pendingPlayerCardEffect,
                selectedOption);
            ApplyCardEffectStep(step);
            return true;
        }

        public bool TryPlayerHit()
        {
            if (!CanAcceptPlayerAction())
            {
                return false;
            }

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

        private void ApplyCardEffectStep(CardEffectStep step)
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

                _pendingPlayerCardEffect = step.PendingEffect;
                State = CoreLoopState.PlayerResolvingCardEffect;
                return;
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
            _pendingPlayerCardEffect = null;
            _activeCardEffectContext = null;

            if (step.RoundResolution.HasValue)
            {
                CompleteRound(step.RoundResolution.Value);
                return;
            }

            RunEnemyTurn();
        }

        private void StartRound()
        {
            State = CoreLoopState.StartingRound;
            RoundNumber++;
            _activeCardEffectContext = null;
            _pendingPlayerCardEffect = null;
            _playerChangeSelection = null;
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

            while (true)
            {
                if (Enemy.IsStanding)
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

                EnemyAction action = _enemyPolicy.Decide(Enemy.HandValue);
                if (action == EnemyAction.Stand)
                {
                    Enemy.Stand();
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

                Enemy.Draw(faceUp: true);
                if (Enemy.HandValue.IsBust)
                {
                    ResolveRound();
                    return;
                }

                if (!Player.IsStanding)
                {
                    State = CoreLoopState.PlayerTurn;
                    return;
                }
            }
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
