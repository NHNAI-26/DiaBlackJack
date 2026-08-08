using System;
using System.Collections.Generic;

namespace DiaBlackJack.CoreLoop
{
    internal interface IAutomaticCardEffectHandler
    {
        CardEffectKind EffectKind { get; }

        AutomaticCardEffectStep Begin(AutomaticCardEffectContext context);

        AutomaticCardEffectStep ResolveChoice(
            AutomaticCardEffectContext context,
            PendingAutomaticCardInteraction pendingInteraction,
            AutomaticCardChoiceOption selectedOption);
    }

    internal sealed class AutomaticCardEffectContext
    {
        private bool _hasPlayerFlamethrowerDecision;
        private bool _hasEnemyFlamethrowerDecision;
        private int? _playerFlamethrowerCardId;
        private int? _enemyFlamethrowerCardId;
        private bool _hasPlayerResurrectionHerbDecision;
        private bool _hasEnemyResurrectionHerbDecision;
        private bool _playerPaysResurrectionHerbSoul;
        private bool _enemyPaysResurrectionHerbSoul;
        private int? _reactivatedManualCardId;

        public AutomaticCardEffectContext(
            CoreLoopBattle battle,
            CombatantSide ownerSide,
            BlackjackCard sourceCard)
        {
            Battle = battle ?? throw new ArgumentNullException(nameof(battle));
            if (!Enum.IsDefined(typeof(CombatantSide), ownerSide))
            {
                throw new ArgumentOutOfRangeException(nameof(ownerSide));
            }

            OwnerSide = ownerSide;
            SourceCard = sourceCard ??
                throw new ArgumentNullException(nameof(sourceCard));
        }

        internal CoreLoopBattle Battle { get; }

        public CombatantSide OwnerSide { get; }

        public CombatantSide OpponentSide =>
            OwnerSide == CombatantSide.Player
                ? CombatantSide.Enemy
                : CombatantSide.Player;

        public BlackjackCard SourceCard { get; }

        public int OwnerCurrentSoul =>
            Battle.GetParticipant(OwnerSide).Soul.Current;

        public bool IsOwnerSoulDepleted =>
            Battle.GetParticipant(OwnerSide).Soul.IsDepleted;

        public bool CanOwnerStand =>
            Battle.CanOwnerStandForAutomaticCard(OwnerSide);

        internal bool DidResurrectionHerbRedeal { get; private set; }

        internal bool DidOwnerResurrectionHerbRedeal { get; private set; }

        public bool TryStandOwner()
        {
            return Battle.TryStandOwnerForAutomaticCard(OwnerSide);
        }

        public void ApplyOwnerSoulDamage(int amount)
        {
            Battle.ApplySoulDamage(
                OwnerSide,
                amount,
                SoulLossCause.AutomaticCardCost);
        }

        public void RegisterPoisonWinReward(int healAmount)
        {
            Battle.RegisterPoisonWinReward(
                SourceCard.Id,
                OwnerSide,
                healAmount);
        }

        public bool CanPayResurrectionHerbSoul(CombatantSide side)
        {
            return Battle.GetParticipant(side).Soul.Current > 0;
        }

        public bool PayResurrectionHerbSoulAndRedeal(CombatantSide side)
        {
            bool redealt = Battle.PayResurrectionHerbSoulAndRedeal(
                side,
                SourceCard);
            DidResurrectionHerbRedeal |= redealt;
            DidOwnerResurrectionHerbRedeal |=
                redealt && side == OwnerSide;
            return redealt;
        }

        public bool TryCompareSingleOpponentHiddenCard(
            int declaredNumber,
            out int subjectHiddenCardId,
            out bool isAtLeastDeclaredNumber)
        {
            if (declaredNumber < 1 || declaredNumber > 10)
            {
                throw new ArgumentOutOfRangeException(nameof(declaredNumber));
            }

            subjectHiddenCardId = default;
            isAtLeastDeclaredNumber = default;
            BlackjackHand opponentHand =
                Battle.GetParticipant(OpponentSide).Hand;
            if (!opponentHand.TryGetSingleHiddenCard(out BlackjackCard hiddenCard))
            {
                return false;
            }

            subjectHiddenCardId = hiddenCard.Id;
            isAtLeastDeclaredNumber = hiddenCard.Rank >= declaredNumber;
            return true;
        }

        public void RecordLieDetectorResult(
            int declaredNumber,
            int? subjectHiddenCardId,
            bool? isAtLeastDeclaredNumber)
        {
            Battle.RecordLieDetectorResult(
                SourceCard.Id,
                OwnerSide,
                declaredNumber,
                subjectHiddenCardId,
                isAtLeastDeclaredNumber);
        }

        public bool IsStanding(CombatantSide side)
        {
            return Battle.GetParticipant(side).IsStanding;
        }

        public IReadOnlyList<BlackjackCard> GetFaceUpDiscardCandidates(
            CombatantSide side)
        {
            BattleParticipant participant = Battle.GetParticipant(side);
            var candidates = new List<BlackjackCard>();
            foreach (BlackjackCard card in participant.Hand.Cards)
            {
                if (!card.IsFaceUp ||
                    participant.Hand.IsHiddenCard(card.Id) ||
                    (side == OwnerSide &&
                        ReferenceEquals(card, SourceCard)))
                {
                    continue;
                }

                candidates.Add(card);
            }

            return candidates.AsReadOnly();
        }

        public bool TryDiscardFaceUpCard(
            CombatantSide side,
            int cardId)
        {
            BattleParticipant participant = Battle.GetParticipant(side);
            if (!participant.Hand.TryGetCard(
                    cardId,
                    out BlackjackCard card) ||
                !card.IsFaceUp ||
                participant.Hand.IsHiddenCard(card.Id) ||
                (side == OwnerSide && ReferenceEquals(card, SourceCard)))
            {
                return false;
            }

            return participant.TryDiscardCard(cardId);
        }

        internal void CommitFlamethrowerDecision(
            CombatantSide side,
            int? cardId)
        {
            bool alreadyCommitted = side == CombatantSide.Player
                ? _hasPlayerFlamethrowerDecision
                : _hasEnemyFlamethrowerDecision;
            if (alreadyCommitted ||
                (cardId.HasValue && !IsFlamethrowerCandidate(side, cardId.Value)))
            {
                throw new InvalidOperationException(
                    "Flamethrower decision is duplicated or no longer valid.");
            }

            if (side == CombatantSide.Player)
            {
                _hasPlayerFlamethrowerDecision = true;
                _playerFlamethrowerCardId = cardId;
                return;
            }

            _hasEnemyFlamethrowerDecision = true;
            _enemyFlamethrowerCardId = cardId;
        }

        internal void ApplyCommittedFlamethrowerDecisions()
        {
            if (!_hasPlayerFlamethrowerDecision ||
                !_hasEnemyFlamethrowerDecision ||
                (_playerFlamethrowerCardId.HasValue &&
                    !IsFlamethrowerCandidate(
                        CombatantSide.Player,
                        _playerFlamethrowerCardId.Value)) ||
                (_enemyFlamethrowerCardId.HasValue &&
                    !IsFlamethrowerCandidate(
                        CombatantSide.Enemy,
                        _enemyFlamethrowerCardId.Value)))
            {
                throw new InvalidOperationException(
                    "Committed flamethrower decisions cannot be applied.");
            }

            if (_playerFlamethrowerCardId.HasValue &&
                !TryDiscardFaceUpCard(
                    CombatantSide.Player,
                    _playerFlamethrowerCardId.Value))
            {
                throw new InvalidOperationException(
                    "Committed player flamethrower card could not be discarded.");
            }

            if (_enemyFlamethrowerCardId.HasValue &&
                !TryDiscardFaceUpCard(
                    CombatantSide.Enemy,
                    _enemyFlamethrowerCardId.Value))
            {
                throw new InvalidOperationException(
                    "Committed enemy flamethrower card could not be discarded.");
            }
        }

        internal void CommitResurrectionHerbDecision(
            CombatantSide side,
            bool paysSoul)
        {
            bool alreadyCommitted = side == CombatantSide.Player
                ? _hasPlayerResurrectionHerbDecision
                : _hasEnemyResurrectionHerbDecision;
            if (alreadyCommitted ||
                (paysSoul && !CanPayResurrectionHerbSoul(side)))
            {
                throw new InvalidOperationException(
                    "Resurrection herb decision is duplicated or no longer valid.");
            }

            if (side == CombatantSide.Player)
            {
                _hasPlayerResurrectionHerbDecision = true;
                _playerPaysResurrectionHerbSoul = paysSoul;
                return;
            }

            _hasEnemyResurrectionHerbDecision = true;
            _enemyPaysResurrectionHerbSoul = paysSoul;
        }

        internal bool ApplyCommittedResurrectionHerbDecisions()
        {
            if (!_hasPlayerResurrectionHerbDecision ||
                !_hasEnemyResurrectionHerbDecision)
            {
                throw new InvalidOperationException(
                    "Both resurrection herb decisions must be committed first.");
            }

            bool playerSurvived = !_playerPaysResurrectionHerbSoul ||
                PayResurrectionHerbSoulAndRedeal(CombatantSide.Player);
            bool enemySurvived = !_enemyPaysResurrectionHerbSoul ||
                PayResurrectionHerbSoulAndRedeal(CombatantSide.Enemy);
            return !playerSurvived || !enemySurvived;
        }

        internal AutomaticCardResult CreateResult(
            AutomaticCardSourceDisposition sourceDisposition)
        {
            AutomaticCardDecisionOutcome playerDecision =
                AutomaticCardDecisionOutcome.None;
            AutomaticCardDecisionOutcome enemyDecision =
                AutomaticCardDecisionOutcome.None;
            int? playerTargetCardId = null;
            int? enemyTargetCardId = null;

            switch (SourceCard.Definition.Effect)
            {
                case CardEffectKind.Flamethrower:
                    if (!_hasPlayerFlamethrowerDecision ||
                        !_hasEnemyFlamethrowerDecision)
                    {
                        throw new InvalidOperationException(
                            "Flamethrower result requires both committed decisions.");
                    }

                    playerTargetCardId = _playerFlamethrowerCardId;
                    enemyTargetCardId = _enemyFlamethrowerCardId;
                    playerDecision = playerTargetCardId.HasValue
                        ? AutomaticCardDecisionOutcome.Accepted
                        : AutomaticCardDecisionOutcome.Declined;
                    enemyDecision = enemyTargetCardId.HasValue
                        ? AutomaticCardDecisionOutcome.Accepted
                        : AutomaticCardDecisionOutcome.Declined;
                    break;
                case CardEffectKind.ResurrectionHerb:
                    if (!_hasPlayerResurrectionHerbDecision ||
                        !_hasEnemyResurrectionHerbDecision)
                    {
                        throw new InvalidOperationException(
                            "Resurrection herb result requires both committed decisions.");
                    }

                    playerDecision = _playerPaysResurrectionHerbSoul
                        ? AutomaticCardDecisionOutcome.Accepted
                        : AutomaticCardDecisionOutcome.Declined;
                    enemyDecision = _enemyPaysResurrectionHerbSoul
                        ? AutomaticCardDecisionOutcome.Accepted
                        : AutomaticCardDecisionOutcome.Declined;
                    break;
            }

            return new AutomaticCardResult(
                SourceCard.Id,
                SourceCard.Definition.Effect,
                OwnerSide,
                sourceDisposition,
                playerDecision,
                enemyDecision,
                playerTargetCardId,
                enemyTargetCardId,
                _reactivatedManualCardId);
        }

        private bool IsFlamethrowerCandidate(
            CombatantSide side,
            int cardId)
        {
            IReadOnlyList<BlackjackCard> candidates =
                GetFaceUpDiscardCandidates(side);
            foreach (BlackjackCard candidate in candidates)
            {
                if (candidate.Id == cardId)
                {
                    return true;
                }
            }

            return false;
        }

        public IReadOnlyList<BlackjackCard>
            GetOwnerReactivatableManualCards()
        {
            BattleParticipant owner = Battle.GetParticipant(OwnerSide);
            var candidates = new List<BlackjackCard>();
            foreach (BlackjackCard card in owner.Hand.Cards)
            {
                if (!card.IsFaceUp ||
                    owner.Hand.IsHiddenCard(card.Id) ||
                    ReferenceEquals(card, SourceCard) ||
                    card.Definition.Activation != CardActivationKind.Manual ||
                    card.UseState != CardUseState.Used)
                {
                    continue;
                }

                candidates.Add(card);
            }

            return candidates.AsReadOnly();
        }

        public bool TryReactivateOwnerManualCard(int cardId)
        {
            BattleParticipant owner = Battle.GetParticipant(OwnerSide);
            if (!owner.Hand.TryGetCard(
                    cardId,
                    out BlackjackCard card) ||
                !card.IsFaceUp ||
                owner.Hand.IsHiddenCard(card.Id) ||
                ReferenceEquals(card, SourceCard))
            {
                return false;
            }

            if (!card.TryReactivate())
            {
                return false;
            }

            _reactivatedManualCardId = card.Id;
            return true;
        }
    }

    internal sealed class AutomaticCardChoiceRequest
    {
        public AutomaticCardChoiceRequest(
            CombatantSide decisionSide,
            AutomaticCardChoiceKind choiceKind,
            CombatPromptId promptId,
            IReadOnlyList<AutomaticCardChoiceOption> options)
        {
            if (!Enum.IsDefined(typeof(CombatantSide), decisionSide))
            {
                throw new ArgumentOutOfRangeException(nameof(decisionSide));
            }

            if (!Enum.IsDefined(typeof(AutomaticCardChoiceKind), choiceKind))
            {
                throw new ArgumentOutOfRangeException(nameof(choiceKind));
            }

            if (!Enum.IsDefined(typeof(CombatPromptId), promptId) ||
                promptId == CombatPromptId.None)
            {
                throw new ArgumentOutOfRangeException(nameof(promptId));
            }

            if (promptId != CombatPromptIdMap.ForAutomaticCard(choiceKind))
            {
                throw new ArgumentException(
                    "Automatic card prompt id does not match its choice kind.",
                    nameof(promptId));
            }

            if (options == null || options.Count == 0)
            {
                throw new ArgumentException(
                    "Automatic card choice requires at least one option.",
                    nameof(options));
            }

            DecisionSide = decisionSide;
            ChoiceKind = choiceKind;
            PromptId = promptId;
            Options = options;
        }

        public CombatantSide DecisionSide { get; }

        public AutomaticCardChoiceKind ChoiceKind { get; }

        public CombatPromptId PromptId { get; }

        public IReadOnlyList<AutomaticCardChoiceOption> Options { get; }
    }

    internal enum AutomaticCardCompletionFlow
    {
        ResumeContinuation,
        EndBattle,
        CancelContinuation
    }

    internal sealed class AutomaticCardEffectStep
    {
        private AutomaticCardEffectStep(
            AutomaticCardChoiceRequest choiceRequest,
            AutomaticCardSourceDisposition? sourceDisposition,
            AutomaticCardCompletionFlow completionFlow)
        {
            ChoiceRequest = choiceRequest;
            SourceDisposition = sourceDisposition;
            CompletionFlow = completionFlow;
        }

        public AutomaticCardChoiceRequest ChoiceRequest { get; }

        public AutomaticCardSourceDisposition? SourceDisposition { get; }

        public AutomaticCardCompletionFlow CompletionFlow { get; }

        public static AutomaticCardEffectStep AwaitChoice(
            CombatantSide decisionSide,
            AutomaticCardChoiceKind choiceKind,
            CombatPromptId promptId,
            IReadOnlyList<AutomaticCardChoiceOption> options)
        {
            return new AutomaticCardEffectStep(
                new AutomaticCardChoiceRequest(
                    decisionSide,
                    choiceKind,
                    promptId,
                    options),
                sourceDisposition: null,
                AutomaticCardCompletionFlow.ResumeContinuation);
        }

        public static AutomaticCardEffectStep Complete(
            AutomaticCardSourceDisposition sourceDisposition,
            AutomaticCardCompletionFlow completionFlow =
                AutomaticCardCompletionFlow.ResumeContinuation)
        {
            if (!Enum.IsDefined(
                typeof(AutomaticCardSourceDisposition),
                sourceDisposition))
            {
                throw new ArgumentOutOfRangeException(nameof(sourceDisposition));
            }

            if (!Enum.IsDefined(
                typeof(AutomaticCardCompletionFlow),
                completionFlow))
            {
                throw new ArgumentOutOfRangeException(nameof(completionFlow));
            }

            return new AutomaticCardEffectStep(
                choiceRequest: null,
                sourceDisposition,
                completionFlow);
        }
    }

    internal sealed class AutomaticCardEffectResolver
    {
        private readonly Dictionary<CardEffectKind, IAutomaticCardEffectHandler>
            _handlers =
                new Dictionary<CardEffectKind, IAutomaticCardEffectHandler>();

        public AutomaticCardEffectResolver(
            params IAutomaticCardEffectHandler[] handlers)
        {
            if (handlers == null)
            {
                throw new ArgumentNullException(nameof(handlers));
            }

            foreach (IAutomaticCardEffectHandler handler in handlers)
            {
                if (handler == null)
                {
                    throw new ArgumentException(
                        "Automatic card handlers cannot contain null.",
                        nameof(handlers));
                }

                if (handler.EffectKind == CardEffectKind.None ||
                    !Enum.IsDefined(typeof(CardEffectKind), handler.EffectKind))
                {
                    throw new ArgumentOutOfRangeException(nameof(handlers));
                }

                if (_handlers.ContainsKey(handler.EffectKind))
                {
                    throw new ArgumentException(
                        $"Automatic card handler for {handler.EffectKind} is duplicated.",
                        nameof(handlers));
                }

                _handlers.Add(handler.EffectKind, handler);
            }
        }

        public static AutomaticCardEffectResolver CreateDefault()
        {
            return new AutomaticCardEffectResolver(
                new PoisonEffectHandler(),
                new ResurrectionHerbEffectHandler(),
                new LieDetectorEffectHandler(),
                new FlamethrowerEffectHandler(),
                new PocketWatchEffectHandler());
        }

        public bool Supports(CardEffectKind effectKind)
        {
            return _handlers.ContainsKey(effectKind);
        }

        public AutomaticCardEffectStep Begin(
            AutomaticCardEffectContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return GetHandler(context.SourceCard.Definition.Effect).Begin(context);
        }

        public AutomaticCardEffectStep ResolveChoice(
            AutomaticCardEffectContext context,
            PendingAutomaticCardInteraction pendingInteraction,
            AutomaticCardChoiceOption selectedOption)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (pendingInteraction == null)
            {
                throw new ArgumentNullException(nameof(pendingInteraction));
            }

            if (selectedOption == null)
            {
                throw new ArgumentNullException(nameof(selectedOption));
            }

            if (pendingInteraction.SourceCardId != context.SourceCard.Id ||
                pendingInteraction.EffectKind !=
                    context.SourceCard.Definition.Effect ||
                pendingInteraction.OwnerSide != context.OwnerSide)
            {
                throw new InvalidOperationException(
                    "Pending automatic card interaction does not match its source.");
            }

            return GetHandler(pendingInteraction.EffectKind).ResolveChoice(
                context,
                pendingInteraction,
                selectedOption);
        }

        private IAutomaticCardEffectHandler GetHandler(
            CardEffectKind effectKind)
        {
            if (!_handlers.TryGetValue(
                effectKind,
                out IAutomaticCardEffectHandler handler))
            {
                throw new InvalidOperationException(
                    $"Automatic card handler for {effectKind} is not registered.");
            }

            return handler;
        }
    }

    internal enum CardEffectContinuationKind
    {
        CrystalOrbAfterActorCardAdded,
        MilitaryKnifeAfterOpponentDraw,
        SatanFlameAfterOpponentDraw
    }

    internal sealed class CardEffectContinuation
    {
        public CardEffectContinuation(
            CardEffectContinuationKind kind,
            int enteredCardId)
        {
            Kind = kind;
            EnteredCardId = enteredCardId;
        }

        public CardEffectContinuationKind Kind { get; }

        public int EnteredCardId { get; }
    }

    internal enum AutomaticCardContinuationKind
    {
        PlayerHit,
        EnemyHit,
        CardEffect,
        DemonContract
    }

    internal sealed class AutomaticCardContinuation
    {
        private AutomaticCardContinuation(
            AutomaticCardContinuationKind kind,
            CombatantSide actorSide,
            CardEffectContinuation cardEffectContinuation,
            DemonContractKind? demonContractKind = null,
            int? sourceContractCardId = null,
            int? enteredCardId = null)
        {
            Kind = kind;
            ActorSide = actorSide;
            CardEffectContinuation = cardEffectContinuation;
            DemonContractKind = demonContractKind;
            SourceContractCardId = sourceContractCardId;
            EnteredCardId = enteredCardId;
        }

        public AutomaticCardContinuationKind Kind { get; }

        public CombatantSide ActorSide { get; }

        public CardEffectContinuation CardEffectContinuation { get; }

        public DemonContractKind? DemonContractKind { get; }

        public int? EnteredCardId { get; }

        public int? SourceContractCardId { get; }

        public static AutomaticCardContinuation ForPlayerHit()
        {
            return new AutomaticCardContinuation(
                AutomaticCardContinuationKind.PlayerHit,
                CombatantSide.Player,
                cardEffectContinuation: null);
        }

        public static AutomaticCardContinuation ForEnemyHit()
        {
            return new AutomaticCardContinuation(
                AutomaticCardContinuationKind.EnemyHit,
                CombatantSide.Enemy,
                cardEffectContinuation: null);
        }

        public static AutomaticCardContinuation ForCardEffect(
            CombatantSide actorSide,
            CardEffectContinuation cardEffectContinuation)
        {
            return new AutomaticCardContinuation(
                AutomaticCardContinuationKind.CardEffect,
                actorSide,
                cardEffectContinuation ??
                    throw new ArgumentNullException(
                        nameof(cardEffectContinuation)));
        }

        public static AutomaticCardContinuation ForDemonContract(
            CombatantSide actorSide,
            DemonContractKind demonContractKind,
            int sourceContractCardId,
            int enteredCardId)
        {
            if (sourceContractCardId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceContractCardId));
            }

            if (enteredCardId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(enteredCardId));
            }

            if (!Enum.IsDefined(typeof(DemonContractKind), demonContractKind))
            {
                throw new ArgumentOutOfRangeException(nameof(demonContractKind));
            }

            return new AutomaticCardContinuation(
                AutomaticCardContinuationKind.DemonContract,
                actorSide,
                cardEffectContinuation: null,
                demonContractKind: demonContractKind,
                sourceContractCardId: sourceContractCardId,
                enteredCardId: enteredCardId);
        }
    }
}
