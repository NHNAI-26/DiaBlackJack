using System;
using System.Collections.Generic;

namespace DiaBlackJack.CoreLoop
{
    internal interface ICardEffectHandler
    {
        CardEffectKind EffectKind { get; }

        bool CanStart(CardEffectContext context);

        CardEffectStep Begin(CardEffectContext context);

        CardEffectStep ResolveChoice(
            CardEffectContext context,
            PendingCardEffect pendingEffect,
            CardEffectChoiceOption selectedOption);
    }

    internal interface ICardEffectContinuationHandler
    {
        CardEffectStep ResumeAfterAutomaticCard(
            CardEffectContext context,
            CardEffectContinuation continuation,
            AutomaticCardResult automaticCardResult);
    }

    internal sealed class CardEffectContext
    {
        private readonly CoreLoopBattle _battle;

        public CardEffectContext(CoreLoopBattle battle, BlackjackCard sourceCard)
            : this(battle, CombatantSide.Player, sourceCard)
        {
        }

        public CardEffectContext(
            CoreLoopBattle battle,
            CombatantSide actorSide,
            BlackjackCard sourceCard)
        {
            _battle = battle ?? throw new ArgumentNullException(nameof(battle));
            if (!Enum.IsDefined(typeof(CombatantSide), actorSide))
            {
                throw new ArgumentOutOfRangeException(nameof(actorSide));
            }

            ActorSide = actorSide;
            SourceCard = sourceCard ?? throw new ArgumentNullException(nameof(sourceCard));
        }

        public CombatantSide ActorSide { get; }

        public BlackjackCard SourceCard { get; }

        public HandValue ActorVisibleHandValue => Actor.VisibleHandValue;

        public HandValue OpponentVisibleHandValue => Opponent.VisibleHandValue;

        public bool IsOpponentStanding => Opponent.IsStanding;

        private BattleParticipant Actor => _battle.GetParticipant(ActorSide);

        private BattleParticipant Opponent => _battle.GetOpponent(ActorSide);

        public IReadOnlyList<BlackjackCard> GetActorFaceUpCards()
        {
            return Actor.Hand.GetFaceUpCards();
        }

        public IReadOnlyList<BlackjackCard> GetOpponentFaceUpCards()
        {
            return Opponent.Hand.GetFaceUpCards();
        }

        public bool TryGetSingleOpponentHiddenCard(out BlackjackCard hiddenCard)
        {
            hiddenCard = null;
            foreach (BlackjackCard card in Opponent.Hand.Cards)
            {
                if (card.IsFaceUp)
                {
                    continue;
                }

                if (hiddenCard != null)
                {
                    hiddenCard = null;
                    return false;
                }

                hiddenCard = card;
            }

            return hiddenCard != null;
        }

        public bool CanDrawActorCards(int count)
        {
            return Actor.Deck.CanDraw(count);
        }

        public bool CanDrawOpponentCards(int count)
        {
            return Opponent.Deck.CanDraw(count);
        }

        public IReadOnlyList<BlackjackCard> TakeActorTopCards(int count)
        {
            return Actor.Deck.TakeTop(count);
        }

        public void ReturnActorCardsToTop(IReadOnlyList<BlackjackCard> cardsInNextDrawOrder)
        {
            Actor.Deck.ReturnToTop(cardsInNextDrawOrder);
        }

        public bool AddActorCardFaceUp(
            BlackjackCard card,
            CardEffectContinuation continuation)
        {
            Actor.AddFaceUpCard(card);
            return _battle.TryBeginAutomaticCardEffect(
                ActorSide,
                card,
                AutomaticCardContinuation.ForCardEffect(
                    ActorSide,
                    continuation));
        }

        public bool TryDiscardActorCard(int cardId)
        {
            return Actor.TryDiscardCard(cardId);
        }

        public bool TryDiscardOpponentCard(int cardId)
        {
            return Opponent.TryDiscardCard(cardId);
        }

        public bool CanReplaceStandingOpponentHiddenCard()
        {
            return Opponent.CanReplaceStandingHiddenCard;
        }

        public bool TryReplaceStandingOpponentHiddenCard(
            out BlackjackCard previousHiddenCard,
            out BlackjackCard replacementCard)
        {
            return Opponent.TryReplaceStandingHiddenCard(
                out previousHiddenCard,
                out replacementCard);
        }

        public BlackjackCard ForceOpponentDrawFaceUp(
            CardEffectContinuationKind continuationKind,
            out bool isWaitingForAutomaticChoice,
            out AutomaticCardResult? immediateAutomaticResult)
        {
            BlackjackCard drawnCard = Opponent.Draw(faceUp: true);
            var continuation = new CardEffectContinuation(
                continuationKind,
                drawnCard.Id);
            isWaitingForAutomaticChoice =
                _battle.TryBeginAutomaticCardEffect(
                    ActorSide == CombatantSide.Player
                        ? CombatantSide.Enemy
                        : CombatantSide.Player,
                    drawnCard,
                    AutomaticCardContinuation.ForCardEffect(
                        ActorSide,
                        continuation),
                    out immediateAutomaticResult);
            return drawnCard;
        }

        public RoundResolution CreateOpponentCardEffectBustResolution()
        {
            return RoundResolver.ResolveCardEffectBust(
                _battle.RoundNumber,
                playerIsTarget: ActorSide == CombatantSide.Enemy,
                sourceCardKey: SourceCard.DefinitionKey);
        }

        public RoundResolution CreateActorCardEffectBustResolution()
        {
            return RoundResolver.ResolveCardEffectBust(
                _battle.RoundNumber,
                playerIsTarget: ActorSide == CombatantSide.Player,
                sourceCardKey: SourceCard.DefinitionKey);
        }

        public RoundResolution CreateEnemyCardEffectBustResolution()
        {
            return CreateOpponentCardEffectBustResolution();
        }

        public void TransformSourceCard(CardDefinition definition)
        {
            Actor.Deck.TransformCardDefinition(SourceCard, definition);
        }

        public RoundResolution CreateActorNumericBustResolution()
        {
            return RoundResolver.ResolveNumericBust(
                _battle.RoundNumber,
                playerIsTarget: ActorSide == CombatantSide.Player);
        }

        public RoundResolution CreateOpponentNumericBustResolution()
        {
            return RoundResolver.ResolveNumericBust(
                _battle.RoundNumber,
                playerIsTarget: ActorSide == CombatantSide.Enemy);
        }
    }

    internal sealed class CardEffectStep
    {
        private CardEffectStep(
            PendingCardEffect pendingEffect,
            CardEffectResult? result,
            RoundResolution? roundResolution,
            CardEffectContinuation continuation)
        {
            PendingEffect = pendingEffect;
            Result = result;
            RoundResolution = roundResolution;
            Continuation = continuation;
        }

        public PendingCardEffect PendingEffect { get; }

        public CardEffectResult? Result { get; }

        public RoundResolution? RoundResolution { get; }

        public CardEffectContinuation Continuation { get; }

        public static CardEffectStep AwaitChoice(PendingCardEffect pendingEffect)
        {
            return new CardEffectStep(
                pendingEffect ?? throw new ArgumentNullException(nameof(pendingEffect)),
                result: null,
                roundResolution: null,
                continuation: null);
        }

        public static CardEffectStep Complete(
            CardEffectResult result,
            RoundResolution? roundResolution = null)
        {
            if (result.EndedRound != roundResolution.HasValue)
            {
                throw new ArgumentException(
                    "Card effect result and round resolution must agree on round completion.",
                    nameof(roundResolution));
            }

            return new CardEffectStep(
                pendingEffect: null,
                result,
                roundResolution,
                continuation: null);
        }

        public static CardEffectStep Suspend(
            CardEffectContinuation continuation)
        {
            return new CardEffectStep(
                pendingEffect: null,
                result: null,
                roundResolution: null,
                continuation: continuation ?? throw new ArgumentNullException(
                    nameof(continuation)));
        }
    }

    internal sealed class CardEffectResolver
    {
        private readonly Dictionary<CardEffectKind, ICardEffectHandler> _handlers =
            new Dictionary<CardEffectKind, ICardEffectHandler>();

        public CardEffectResolver(params ICardEffectHandler[] handlers)
        {
            if (handlers == null)
            {
                throw new ArgumentNullException(nameof(handlers));
            }

            foreach (ICardEffectHandler handler in handlers)
            {
                if (handler == null)
                {
                    throw new ArgumentException("Card effect handlers cannot contain null.", nameof(handlers));
                }

                if (handler.EffectKind == CardEffectKind.None ||
                    !Enum.IsDefined(typeof(CardEffectKind), handler.EffectKind))
                {
                    throw new ArgumentOutOfRangeException(nameof(handlers));
                }

                if (_handlers.ContainsKey(handler.EffectKind))
                {
                    throw new ArgumentException(
                        $"Card effect handler for {handler.EffectKind} is duplicated.",
                        nameof(handlers));
                }

                _handlers.Add(handler.EffectKind, handler);
            }
        }

        public static CardEffectResolver CreateDefault()
        {
            return new CardEffectResolver(
                new CrystalOrbEffectHandler(),
                new ThreatHammerEffectHandler(),
                new AutoPistolEffectHandler(),
                new MilitaryKnifeEffectHandler(),
                new SatanPowerEffectHandler());
        }

        public bool Supports(CardEffectKind effectKind)
        {
            return _handlers.ContainsKey(effectKind);
        }

        public bool CanStart(CardEffectContext context)
        {
            return TryGetHandler(context.SourceCard.Definition.Effect, out ICardEffectHandler handler) &&
                handler.CanStart(context);
        }

        public CardEffectStep Begin(CardEffectContext context)
        {
            return GetHandler(context.SourceCard.Definition.Effect).Begin(context);
        }

        public CardEffectStep ResolveChoice(
            CardEffectContext context,
            PendingCardEffect pendingEffect,
            CardEffectChoiceOption selectedOption)
        {
            if (pendingEffect == null)
            {
                throw new ArgumentNullException(nameof(pendingEffect));
            }

            if (selectedOption == null)
            {
                throw new ArgumentNullException(nameof(selectedOption));
            }

            if (pendingEffect.EffectKind != context.SourceCard.Definition.Effect ||
                pendingEffect.SourceCardId != context.SourceCard.Id)
            {
                throw new InvalidOperationException("Pending card effect does not match its source card.");
            }

            return GetHandler(pendingEffect.EffectKind).ResolveChoice(
                context,
                pendingEffect,
                selectedOption);
        }

        public CardEffectStep ResumeAfterAutomaticCard(
            CardEffectContext context,
            CardEffectContinuation continuation,
            AutomaticCardResult automaticCardResult)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (continuation == null)
            {
                throw new ArgumentNullException(nameof(continuation));
            }

            ICardEffectHandler handler =
                GetHandler(context.SourceCard.Definition.Effect);
            if (!(handler is ICardEffectContinuationHandler continuationHandler))
            {
                throw new InvalidOperationException(
                    $"Card effect handler for {handler.EffectKind} cannot resume after an automatic card.");
            }

            return continuationHandler.ResumeAfterAutomaticCard(
                context,
                continuation,
                automaticCardResult);
        }

        private ICardEffectHandler GetHandler(CardEffectKind effectKind)
        {
            if (!TryGetHandler(effectKind, out ICardEffectHandler handler))
            {
                throw new InvalidOperationException(
                    $"Card effect handler for {effectKind} is not registered.");
            }

            return handler;
        }

        private bool TryGetHandler(CardEffectKind effectKind, out ICardEffectHandler handler)
        {
            return _handlers.TryGetValue(effectKind, out handler);
        }
    }

    internal static class CardUseValidator
    {
        public static CardUseAvailability Evaluate(
            CoreLoopBattle battle,
            CardEffectResolver resolver,
            int cardId)
        {
            return EvaluateForActor(
                battle,
                resolver,
                CombatantSide.Player,
                cardId);
        }

        public static CardUseAvailability EvaluateForActor(
            CoreLoopBattle battle,
            CardEffectResolver resolver,
            CombatantSide actorSide,
            int cardId)
        {
            if (battle == null)
            {
                throw new ArgumentNullException(nameof(battle));
            }

            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            if (!Enum.IsDefined(typeof(CombatantSide), actorSide))
            {
                throw new ArgumentOutOfRangeException(nameof(actorSide));
            }

            if (battle.HasActiveCardEffect)
            {
                return Unavailable(cardId, CardUseUnavailableReason.EffectInProgress);
            }

            if (!battle.CanActorUseCard(actorSide))
            {
                return Unavailable(cardId, CardUseUnavailableReason.NotPlayerTurn);
            }

            BattleParticipant actor = battle.GetParticipant(actorSide);
            if (!actor.Hand.TryGetCard(cardId, out BlackjackCard card))
            {
                return Unavailable(cardId, CardUseUnavailableReason.CardNotInHand);
            }

            if (card.Definition.Activation != CardActivationKind.Manual)
            {
                return Unavailable(cardId, CardUseUnavailableReason.CardIsNotManual);
            }

            if (!card.CanUse)
            {
                return Unavailable(cardId, CardUseUnavailableReason.CardIsUnavailable);
            }

            if (!resolver.Supports(card.Definition.Effect))
            {
                return Unavailable(cardId, CardUseUnavailableReason.EffectNotImplemented);
            }

            var context = new CardEffectContext(battle, actorSide, card);
            if (!resolver.CanStart(context))
            {
                return Unavailable(cardId, CardUseUnavailableReason.EffectRequirementsNotMet);
            }

            return new CardUseAvailability(cardId, canUse: true, CardUseUnavailableReason.None);
        }

        private static CardUseAvailability Unavailable(
            int cardId,
            CardUseUnavailableReason reason)
        {
            return new CardUseAvailability(cardId, canUse: false, reason);
        }
    }
}
