using System;
using System.Collections.Generic;

namespace DiaBlackJack.CoreLoop
{
    internal sealed class ThreatHammerEffectHandler : ICardEffectHandler
    {
        public CardEffectKind EffectKind => CardEffectKind.ThreatHammer;

        public bool CanStart(CardEffectContext context)
        {
            bool hasDiscardCost = context.SourceCard.IsFaceUp ||
                context.GetPlayerFaceUpCards().Count > 0;
            return hasDiscardCost &&
                (!context.IsEnemyStanding || context.CanReplaceStandingEnemyHiddenCard());
        }

        public CardEffectStep Begin(CardEffectContext context)
        {
            IReadOnlyList<BlackjackCard> faceUpCards = context.GetPlayerFaceUpCards();
            if (faceUpCards.Count == 0)
            {
                throw new InvalidOperationException(
                    "Threat hammer requires a player face-up card to discard.");
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
                "버릴 공개 카드를 선택하세요.",
                CardEffectChoiceKind.DiscardOwnFaceUpCard,
                options));
        }

        public CardEffectStep ResolveChoice(
            CardEffectContext context,
            PendingCardEffect pendingEffect,
            CardEffectChoiceOption selectedOption)
        {
            if (pendingEffect.ChoiceKind != CardEffectChoiceKind.DiscardOwnFaceUpCard ||
                !selectedOption.CardId.HasValue ||
                !context.TryDiscardPlayerCard(selectedOption.CardId.Value))
            {
                throw new InvalidOperationException("Threat hammer received an invalid discard choice.");
            }

            if (!context.IsEnemyStanding)
            {
                return Complete(context, endedRound: false);
            }

            if (!context.TryReplaceStandingEnemyHiddenCard(out _, out _))
            {
                throw new InvalidOperationException(
                    "Threat hammer could not replace the standing enemy hidden card.");
            }

            bool endedRound = context.EnemyHandValue.IsBust;
            return endedRound
                ? CardEffectStep.Complete(
                    CreateResult(context, endedRound: true),
                    context.CreateCurrentNumericResolution())
                : Complete(context, endedRound: false);
        }

        private CardEffectStep Complete(CardEffectContext context, bool endedRound)
        {
            return CardEffectStep.Complete(CreateResult(context, endedRound));
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
