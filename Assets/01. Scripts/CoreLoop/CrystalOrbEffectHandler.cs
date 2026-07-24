using System;
using System.Collections.Generic;

namespace DiaBlackJack.CoreLoop
{
    internal sealed class CrystalOrbEffectHandler :
        ICardEffectHandler,
        ICardEffectContinuationHandler
    {
        private const int TakeNoneOptionId = 0;

        public CardEffectKind EffectKind => CardEffectKind.CrystalOrb;

        public bool CanStart(CardEffectContext context)
        {
            return context.CanDrawActorCards(2);
        }

        public CardEffectStep Begin(CardEffectContext context)
        {
            if (!CanStart(context))
            {
                throw new InvalidOperationException(
                    "Crystal orb requires two available player deck cards.");
            }

            IReadOnlyList<BlackjackCard> peekedCards = context.TakeActorTopCards(2);
            var options = new[]
            {
                new CardEffectChoiceOption(TakeNoneOptionId, "가져오지 않음"),
                CreateTakeOption(1, peekedCards[0]),
                CreateTakeOption(2, peekedCards[1])
            };

            return CardEffectStep.AwaitChoice(new PendingCardEffect(
                context.SourceCard.Id,
                EffectKind,
                "가져올 카드를 선택하세요.",
                CardEffectChoiceKind.TakePeekedCard,
                options,
                peekedCards));
        }

        public CardEffectStep ResolveChoice(
            CardEffectContext context,
            PendingCardEffect pendingEffect,
            CardEffectChoiceOption selectedOption)
        {
            if (pendingEffect.ChoiceKind != CardEffectChoiceKind.TakePeekedCard ||
                pendingEffect.TemporaryCards.Count != 2)
            {
                throw new InvalidOperationException("Crystal orb has invalid temporary cards.");
            }

            BlackjackCard selectedCard = FindSelectedCard(pendingEffect, selectedOption);
            var returningCards = new List<BlackjackCard>(2);
            foreach (BlackjackCard card in pendingEffect.TemporaryCards)
            {
                if (card != selectedCard)
                {
                    card.Conceal();
                    returningCards.Add(card);
                }
            }

            context.ReturnActorCardsToTop(returningCards);
            if (selectedCard != null)
            {
                var continuation = new CardEffectContinuation(
                    CardEffectContinuationKind.CrystalOrbAfterActorCardAdded,
                    selectedCard.Id);
                if (context.AddActorCardFaceUp(selectedCard, continuation))
                {
                    return CardEffectStep.Suspend(continuation);
                }
            }

            return CompleteAfterSelectedCard(context);
        }

        public CardEffectStep ResumeAfterAutomaticCard(
            CardEffectContext context,
            CardEffectContinuation continuation,
            AutomaticCardResult automaticCardResult)
        {
            if (continuation.Kind !=
                    CardEffectContinuationKind.CrystalOrbAfterActorCardAdded ||
                continuation.EnteredCardId !=
                    automaticCardResult.SourceCardId)
            {
                throw new InvalidOperationException(
                    "Crystal orb received an invalid automatic card continuation.");
            }

            return CompleteAfterSelectedCard(context);
        }

        private CardEffectStep CompleteAfterSelectedCard(
            CardEffectContext context)
        {
            bool endedRound = context.ActorVisibleHandValue.IsBust;
            var result = new CardEffectResult(
                context.SourceCard.Id,
                EffectKind,
                succeeded: true,
                endedRound);
            return endedRound
                ? CardEffectStep.Complete(result, context.CreateActorNumericBustResolution())
                : CardEffectStep.Complete(result);
        }

        private static CardEffectChoiceOption CreateTakeOption(int optionId, BlackjackCard card)
        {
            return new CardEffectChoiceOption(
                optionId,
                $"{card.Rank} {card.Definition.DisplayName}",
                cardId: card.Id);
        }

        private static BlackjackCard FindSelectedCard(
            PendingCardEffect pendingEffect,
            CardEffectChoiceOption selectedOption)
        {
            if (!selectedOption.CardId.HasValue)
            {
                if (selectedOption.Id != TakeNoneOptionId)
                {
                    throw new InvalidOperationException("Crystal orb received an invalid no-card option.");
                }

                return null;
            }

            foreach (BlackjackCard card in pendingEffect.TemporaryCards)
            {
                if (card.Id == selectedOption.CardId.Value)
                {
                    return card;
                }
            }

            throw new InvalidOperationException("Crystal orb selected a card it does not own.");
        }
    }
}
