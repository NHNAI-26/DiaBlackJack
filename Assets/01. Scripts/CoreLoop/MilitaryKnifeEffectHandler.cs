using System;

namespace DiaBlackJack.CoreLoop
{
    internal sealed class MilitaryKnifeEffectHandler :
        ICardEffectHandler,
        ICardEffectContinuationHandler
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

            BlackjackCard drawnCard = context.ForceOpponentDrawFaceUp(
                CardEffectContinuationKind.MilitaryKnifeAfterOpponentDraw,
                out bool isWaitingForAutomaticChoice,
                out AutomaticCardResult? immediateAutomaticResult);
            var continuation = new CardEffectContinuation(
                CardEffectContinuationKind.MilitaryKnifeAfterOpponentDraw,
                drawnCard.Id);
            if (isWaitingForAutomaticChoice)
            {
                return CardEffectStep.Suspend(continuation);
            }

            return CompleteAfterForcedDraw(
                context,
                drawnCard.Id,
                immediateAutomaticResult?.SourceDisposition ??
                    AutomaticCardSourceDisposition.RetainFaceUp);
        }

        public CardEffectStep ResumeAfterAutomaticCard(
            CardEffectContext context,
            CardEffectContinuation continuation,
            AutomaticCardResult automaticCardResult)
        {
            if (continuation.Kind !=
                    CardEffectContinuationKind.MilitaryKnifeAfterOpponentDraw ||
                continuation.EnteredCardId !=
                    automaticCardResult.SourceCardId)
            {
                throw new InvalidOperationException(
                    "Military knife received an invalid automatic card continuation.");
            }

            return CompleteAfterForcedDraw(
                context,
                continuation.EnteredCardId,
                automaticCardResult.SourceDisposition);
        }

        private CardEffectStep CompleteAfterForcedDraw(
            CardEffectContext context,
            int drawnCardId,
            AutomaticCardSourceDisposition sourceDisposition)
        {
            if (context.OpponentVisibleHandValue.IsBust)
            {
                return CardEffectStep.Complete(
                    CreateResult(context, endedRound: true),
                    context.CreateOpponentNumericBustResolution());
            }

            if (sourceDisposition == AutomaticCardSourceDisposition.RetainFaceUp &&
                !context.TryDiscardOpponentCard(drawnCardId))
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
