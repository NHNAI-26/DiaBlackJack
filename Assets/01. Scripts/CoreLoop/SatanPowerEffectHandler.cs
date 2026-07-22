using System;
using System.Collections.Generic;

namespace DiaBlackJack.CoreLoop
{
    internal sealed class SatanPowerEffectHandler : ICardEffectHandler
    {
        private static readonly IReadOnlyList<CardEffectChoiceOption> NumberOptions =
            CreateNumberOptions(excludedNumber: null);

        public CardEffectKind EffectKind => CardEffectKind.SatanPower;

        public bool CanStart(CardEffectContext context)
        {
            switch (context.SourceCard.DefinitionKey)
            {
                case CardDefinitionCatalog.SatanPowerFlameKey:
                    return context.OpponentVisibleHandValue.Total <= 17 &&
                        context.CanDrawOpponentCards(1);
                case CardDefinitionCatalog.SatanPowerMightKey:
                    return context.TryGetSingleOpponentHiddenCard(out _);
                default:
                    return false;
            }
        }

        public CardEffectStep Begin(CardEffectContext context)
        {
            if (!CanStart(context))
            {
                throw new InvalidOperationException(
                    "Satan power cannot start in the current combat state.");
            }

            if (context.SourceCard.DefinitionKey ==
                CardDefinitionCatalog.SatanPowerFlameKey)
            {
                return ResolveFlame(context);
            }

            return CardEffectStep.AwaitChoice(new PendingCardEffect(
                context.SourceCard.Id,
                EffectKind,
                "첫 번째 숫자를 선언하세요.",
                CardEffectChoiceKind.DeclareFirstOfTwoNumbers,
                NumberOptions));
        }

        public CardEffectStep ResolveChoice(
            CardEffectContext context,
            PendingCardEffect pendingEffect,
            CardEffectChoiceOption selectedOption)
        {
            if (!selectedOption.NumericValue.HasValue ||
                selectedOption.NumericValue.Value < 1 ||
                selectedOption.NumericValue.Value > 10)
            {
                throw new InvalidOperationException(
                    "Satan power requires a number between 1 and 10.");
            }

            int selectedNumber = selectedOption.NumericValue.Value;
            if (pendingEffect.ChoiceKind ==
                CardEffectChoiceKind.DeclareFirstOfTwoNumbers)
            {
                return CardEffectStep.AwaitChoice(new PendingCardEffect(
                    context.SourceCard.Id,
                    EffectKind,
                    "두 번째 숫자를 선언하세요.",
                    CardEffectChoiceKind.DeclareSecondOfTwoNumbers,
                    CreateNumberOptions(selectedNumber),
                    contextNumericValue: selectedNumber));
            }

            if (pendingEffect.ChoiceKind !=
                    CardEffectChoiceKind.DeclareSecondOfTwoNumbers ||
                !pendingEffect.ContextNumericValue.HasValue ||
                pendingEffect.ContextNumericValue.Value == selectedNumber)
            {
                throw new InvalidOperationException(
                    "Satan power received an invalid second number.");
            }

            if (!context.TryGetSingleOpponentHiddenCard(out BlackjackCard hiddenCard))
            {
                throw new InvalidOperationException(
                    "Satan power lost the opponent hidden card while resolving.");
            }

            bool succeeded = hiddenCard.Rank == pendingEffect.ContextNumericValue.Value ||
                hiddenCard.Rank == selectedNumber;
            context.TransformSourceCard(CardDefinitionCatalog.GetByKey(
                CardDefinitionCatalog.SatanPowerFlameKey));
            var result = new CardEffectResult(
                context.SourceCard.Id,
                EffectKind,
                succeeded,
                endedRound: succeeded);
            return succeeded
                ? CardEffectStep.Complete(
                    result,
                    context.CreateOpponentCardEffectBustResolution())
                : CardEffectStep.Complete(result);
        }

        private static CardEffectStep ResolveFlame(CardEffectContext context)
        {
            BlackjackCard drawnCard = context.ForceOpponentDrawFaceUp();
            bool busted = context.OpponentVisibleHandValue.IsBust;
            if (!busted && !context.TryDiscardOpponentCard(drawnCard.Id))
            {
                throw new InvalidOperationException(
                    "Satan flame could not discard the safe forced draw.");
            }

            context.TransformSourceCard(CardDefinitionCatalog.GetByKey(
                CardDefinitionCatalog.SatanPowerMightKey));
            var result = new CardEffectResult(
                context.SourceCard.Id,
                CardEffectKind.SatanPower,
                succeeded: true,
                endedRound: busted);
            return busted
                ? CardEffectStep.Complete(
                    result,
                    context.CreateOpponentNumericBustResolution())
                : CardEffectStep.Complete(result);
        }

        private static IReadOnlyList<CardEffectChoiceOption> CreateNumberOptions(
            int? excludedNumber)
        {
            var options = new List<CardEffectChoiceOption>(10);
            for (int number = 1; number <= 10; number++)
            {
                if (number == excludedNumber)
                {
                    continue;
                }

                options.Add(new CardEffectChoiceOption(
                    number,
                    number.ToString(),
                    numericValue: number));
            }

            return options.AsReadOnly();
        }
    }
}
