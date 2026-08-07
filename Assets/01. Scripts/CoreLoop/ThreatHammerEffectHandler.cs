using System;
using System.Collections.Generic;

namespace DiaBlackJack.CoreLoop
{
    internal sealed class ThreatHammerEffectHandler : ICardEffectHandler
    {
        public CardEffectKind EffectKind => CardEffectKind.ThreatHammer;

        public bool CanStart(CardEffectContext context)
        {
            return context.GetOpponentPublicCards().Count > 0 &&
                (!context.IsOpponentStanding || context.CanReplaceStandingOpponentHiddenCard());
        }

        public CardEffectStep Begin(CardEffectContext context)
        {
            IReadOnlyList<BlackjackCard> faceUpCards = context.GetOpponentPublicCards();
            if (faceUpCards.Count == 0)
            {
                throw new InvalidOperationException(
                    "Threat hammer requires an opponent face-up card to discard.");
            }

            var options = new List<CardEffectChoiceOption>(faceUpCards.Count);
            foreach (BlackjackCard card in faceUpCards)
            {
                options.Add(new CardEffectChoiceOption(
                    card.Id,
                    $"{card.Rank} {card.Definition.DisplayName}",
                    cardId: card.Id));
            }

            return CardEffectStep.AwaitChoice(new PendingCardEffect(
                context.SourceCard.Id,
                EffectKind,
                CombatPromptId.ManualThreatHammerChooseOpponentCard,
                CardEffectChoiceKind.DiscardOpponentFaceUpCard,
                options));
        }

        public CardEffectStep ResolveChoice(
            CardEffectContext context,
            PendingCardEffect pendingEffect,
            CardEffectChoiceOption selectedOption)
        {
            if (pendingEffect.ChoiceKind != CardEffectChoiceKind.DiscardOpponentFaceUpCard ||
                !selectedOption.CardId.HasValue ||
                !context.TryDiscardOpponentCard(selectedOption.CardId.Value))
            {
                throw new InvalidOperationException(
                    "Threat hammer received an invalid opponent discard choice.");
            }

            if (!context.IsOpponentStanding)
            {
                return Complete(context, endedRound: false, selectedOption.CardId);
            }

            if (!context.TryReplaceStandingOpponentHiddenCard(out _, out _))
            {
                throw new InvalidOperationException(
                    "Threat hammer could not replace the standing enemy hidden card.");
            }

            bool endedRound = context.OpponentVisibleHandValue.IsBust;
            return endedRound
                ? CardEffectStep.Complete(
                    CreateResult(context, endedRound: true, selectedOption.CardId),
                    context.CreateOpponentNumericBustResolution())
                : Complete(context, endedRound: false, selectedOption.CardId);
        }

        private CardEffectStep Complete(
            CardEffectContext context,
            bool endedRound,
            int? targetCardId)
        {
            return CardEffectStep.Complete(
                CreateResult(context, endedRound, targetCardId));
        }

        private CardEffectResult CreateResult(
            CardEffectContext context,
            bool endedRound,
            int? targetCardId)
        {
            return new CardEffectResult(
                context.SourceCard.Id,
                EffectKind,
                succeeded: true,
                endedRound,
                targetCardId);
        }
    }
}
