using System;

namespace DiaBlackJack.CoreLoop
{
    internal interface IForcedDrawRetentionPolicy
    {
        bool ShouldKeep(HandValue enemyHandAfterDraw, BlackjackCard drawnCard);
    }

    internal sealed class SimpleForcedDrawRetentionPolicy : IForcedDrawRetentionPolicy
    {
        public bool ShouldKeep(HandValue enemyHandAfterDraw, BlackjackCard drawnCard)
        {
            return true;
        }
    }

    internal sealed class MilitaryKnifeEffectHandler : ICardEffectHandler
    {
        private readonly IForcedDrawRetentionPolicy _retentionPolicy;

        public MilitaryKnifeEffectHandler(IForcedDrawRetentionPolicy retentionPolicy = null)
        {
            _retentionPolicy = retentionPolicy ?? new SimpleForcedDrawRetentionPolicy();
        }

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
            if (context.OpponentHandValue.IsBust)
            {
                return CardEffectStep.Complete(
                    CreateResult(context, endedRound: true),
                    context.CreateCurrentNumericResolution());
            }

            if (!_retentionPolicy.ShouldKeep(context.OpponentHandValue, drawnCard) &&
                !context.TryDiscardOpponentCard(drawnCard.Id))
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
