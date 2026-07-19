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

    internal sealed class CardEffectContext
    {
        private readonly CoreLoopBattle _battle;

        public CardEffectContext(CoreLoopBattle battle, BlackjackCard sourceCard)
        {
            _battle = battle ?? throw new ArgumentNullException(nameof(battle));
            SourceCard = sourceCard ?? throw new ArgumentNullException(nameof(sourceCard));
        }

        public BlackjackCard SourceCard { get; }

        public HandValue EnemyHandValue => _battle.Enemy.HandValue;

        public bool IsEnemyStanding => _battle.Enemy.IsStanding;

        public IReadOnlyList<BlackjackCard> GetPlayerFaceUpCards()
        {
            return _battle.Player.Hand.GetFaceUpCards();
        }

        public bool TryGetSingleEnemyHiddenCard(out BlackjackCard hiddenCard)
        {
            hiddenCard = null;
            foreach (BlackjackCard card in _battle.Enemy.Hand.Cards)
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

        public bool CanDrawPlayerCards(int count)
        {
            return _battle.Player.Deck.CanDraw(count);
        }

        public IReadOnlyList<BlackjackCard> TakePlayerTopCards(int count)
        {
            return _battle.Player.Deck.TakeTop(count);
        }

        public void ReturnPlayerCardsToTop(IReadOnlyList<BlackjackCard> cardsInNextDrawOrder)
        {
            _battle.Player.Deck.ReturnToTop(cardsInNextDrawOrder);
        }

        public void AddPlayerCardFaceUp(BlackjackCard card)
        {
            _battle.Player.AddFaceUpCard(card);
        }

        public bool TryDiscardPlayerCard(int cardId)
        {
            return _battle.Player.TryDiscardCard(cardId);
        }

        public BlackjackCard ForceEnemyDrawFaceUp()
        {
            return _battle.Enemy.Draw(faceUp: true);
        }

        public RoundResolution CreateEnemyCardEffectBustResolution()
        {
            return RoundResolver.ResolveCardEffectBust(
                _battle.RoundNumber,
                playerIsTarget: false,
                sourceCardKey: SourceCard.DefinitionKey);
        }

        public RoundResolution CreatePlayerCardEffectBustResolution()
        {
            return RoundResolver.ResolveCardEffectBust(
                _battle.RoundNumber,
                playerIsTarget: true,
                sourceCardKey: SourceCard.DefinitionKey);
        }
    }

    internal sealed class CardEffectStep
    {
        private CardEffectStep(
            PendingCardEffect pendingEffect,
            CardEffectResult? result,
            RoundResolution? roundResolution)
        {
            PendingEffect = pendingEffect;
            Result = result;
            RoundResolution = roundResolution;
        }

        public PendingCardEffect PendingEffect { get; }

        public CardEffectResult? Result { get; }

        public RoundResolution? RoundResolution { get; }

        public static CardEffectStep AwaitChoice(PendingCardEffect pendingEffect)
        {
            return new CardEffectStep(
                pendingEffect ?? throw new ArgumentNullException(nameof(pendingEffect)),
                result: null,
                roundResolution: null);
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
                roundResolution);
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
            return new CardEffectResolver(new AutoPistolEffectHandler());
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
            if (battle.State == CoreLoopState.PlayerResolvingCardEffect)
            {
                return Unavailable(cardId, CardUseUnavailableReason.EffectInProgress);
            }

            if (!battle.CanPlayerAct)
            {
                return Unavailable(cardId, CardUseUnavailableReason.NotPlayerTurn);
            }

            if (!battle.Player.Hand.TryGetCard(cardId, out BlackjackCard card))
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

            var context = new CardEffectContext(battle, card);
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
