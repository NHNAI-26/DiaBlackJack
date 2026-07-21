using System;

namespace DiaBlackJack.CoreLoop
{
    internal sealed class MilitaryKnifeEffectHandler : ICardEffectHandler
    {
        public CardEffectKind EffectKind => CardEffectKind.MilitaryKnife;

        public bool CanStart(CardEffectContext context)
        {
            return context.OpponentVisibleHandValue.Total <= 16 &&
                context.CanDrawOpponentCards(1);
        }

        public CardEffectStep Begin(CardEffectContext context)
        {
            if (!CanStart(context))
            {
                throw new InvalidOperationException(
                    "Military knife requires enemy visible total at most 16 and one deck card.");
            }

            BlackjackCard drawnCard = context.ForceOpponentDrawFaceUp();
            if (context.OpponentVisibleHandValue.IsBust)
            {
                return CardEffectStep.Complete(
                    CreateResult(context, endedRound: true),
                    context.CreateOpponentNumericBustResolution());
            }

            if (!context.TryDiscardOpponentCard(drawnCard.Id))
            {
                throw new InvalidOperationException(
                    "Military knife could not discard the forced draw card.");
            }

            return CardEffectStep.Complete(CreateResult(context, endedRound: false));
        }

        public CardEffectStep ResolveChoice(
            CardEffectContext context,
            PendingCardEffect pendingEffect,
            CardEffectChoiceOption selectedOption)
        {
            throw new InvalidOperationException("Military knife does not require a choice.");
        }

        private CardEffectResult CreateResult(CardEffectContext context, bool endedRound)
        {
            return new CardEffectResult(
                context.SourceCard.Id,
                EffectKind,
                succeeded: true,
                endedRound);
        }
    }
}
