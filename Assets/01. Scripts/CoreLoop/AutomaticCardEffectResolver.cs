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

        public bool TryStandOwner()
        {
            return Battle.TryStandOwnerForAutomaticCard(OwnerSide);
        }

        public void ApplyOwnerSoulDamage(int amount)
        {
            Battle.ApplySoulDamage(OwnerSide, amount);
        }

        public void RegisterPoisonWinReward(int healAmount)
        {
            Battle.RegisterPoisonWinReward(
                SourceCard.Id,
                OwnerSide,
                healAmount);
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
            BlackjackCard hiddenCard = null;
            foreach (BlackjackCard card in
                Battle.GetParticipant(OpponentSide).Hand.Cards)
            {
                if (card.IsFaceUp)
                {
                    continue;
                }

                if (hiddenCard != null)
                {
                    return false;
                }

                hiddenCard = card;
            }

            if (hiddenCard == null)
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
                (side == OwnerSide && ReferenceEquals(card, SourceCard)))
            {
                return false;
            }

            return participant.TryDiscardCard(cardId);
        }

        public IReadOnlyList<BlackjackCard>
            GetOwnerReactivatableManualCards()
        {
            BattleParticipant owner = Battle.GetParticipant(OwnerSide);
            var candidates = new List<BlackjackCard>();
            foreach (BlackjackCard card in owner.Hand.Cards)
            {
                if (!card.IsFaceUp ||
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
                ReferenceEquals(card, SourceCard))
            {
                return false;
            }

            return card.TryReactivate();
        }
    }

    internal sealed class AutomaticCardChoiceRequest
    {
        public AutomaticCardChoiceRequest(
            CombatantSide decisionSide,
            AutomaticCardChoiceKind choiceKind,
            string prompt,
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

            if (string.IsNullOrWhiteSpace(prompt))
            {
                throw new ArgumentException(
                    "Automatic card prompt cannot be empty.",
                    nameof(prompt));
            }

            if (options == null || options.Count == 0)
            {
                throw new ArgumentException(
                    "Automatic card choice requires at least one option.",
                    nameof(options));
            }

            DecisionSide = decisionSide;
            ChoiceKind = choiceKind;
            Prompt = prompt;
            Options = options;
        }

        public CombatantSide DecisionSide { get; }

        public AutomaticCardChoiceKind ChoiceKind { get; }

        public string Prompt { get; }

        public IReadOnlyList<AutomaticCardChoiceOption> Options { get; }
    }

    internal enum AutomaticCardCompletionFlow
    {
        ResumeContinuation,
        EndBattle
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
            string prompt,
            IReadOnlyList<AutomaticCardChoiceOption> options)
        {
            return new AutomaticCardEffectStep(
                new AutomaticCardChoiceRequest(
                    decisionSide,
                    choiceKind,
                    prompt,
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
        CardEffect
    }

    internal sealed class AutomaticCardContinuation
    {
        private AutomaticCardContinuation(
            AutomaticCardContinuationKind kind,
            CombatantSide actorSide,
            CardEffectContinuation cardEffectContinuation)
        {
            Kind = kind;
            ActorSide = actorSide;
            CardEffectContinuation = cardEffectContinuation;
        }

        public AutomaticCardContinuationKind Kind { get; }

        public CombatantSide ActorSide { get; }

        public CardEffectContinuation CardEffectContinuation { get; }

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
    }
}
