using System;
using System.Collections.Generic;

namespace DiaBlackJack.CoreLoop
{
    internal sealed class AutoPistolEffectHandler : ICardEffectHandler
    {
        private static readonly IReadOnlyList<CardEffectChoiceOption> GuessOptions =
            CreateGuessOptions();

        public CardEffectKind EffectKind => CardEffectKind.AutoPistol;

        public bool CanStart(CardEffectContext context)
        {
            return context.TryGetSingleEnemyHiddenCard(out _);
        }

        public CardEffectStep Begin(CardEffectContext context)
        {
            if (!CanStart(context))
            {
                throw new InvalidOperationException(
                    "Auto pistol requires exactly one enemy hidden card.");
            }

            return CardEffectStep.AwaitChoice(new PendingCardEffect(
                context.SourceCard.Id,
                EffectKind,
                "상대 비공개 카드의 숫자를 선언하세요.",
                CardEffectChoiceKind.DeclareNumber,
                GuessOptions));
        }

        public CardEffectStep ResolveChoice(
            CardEffectContext context,
            PendingCardEffect pendingEffect,
            CardEffectChoiceOption selectedOption)
        {
            if (pendingEffect.ChoiceKind != CardEffectChoiceKind.DeclareNumber ||
                !selectedOption.NumericValue.HasValue ||
                selectedOption.NumericValue.Value < 1 ||
                selectedOption.NumericValue.Value > 10)
            {
                throw new InvalidOperationException("Auto pistol received an invalid number choice.");
            }

            if (!context.TryGetSingleEnemyHiddenCard(out BlackjackCard hiddenCard))
            {
                throw new InvalidOperationException(
                    "Auto pistol lost its single enemy hidden card while resolving.");
            }

            bool succeeded = selectedOption.NumericValue.Value == hiddenCard.Rank;
            var result = new CardEffectResult(
                context.SourceCard.Id,
                EffectKind,
                succeeded,
                endedRound: succeeded);

            return succeeded
                ? CardEffectStep.Complete(
                    result,
                    context.CreateEnemyCardEffectBustResolution())
                : CardEffectStep.Complete(result);
        }

        private static IReadOnlyList<CardEffectChoiceOption> CreateGuessOptions()
        {
            var options = new CardEffectChoiceOption[10];
            for (int number = 1; number <= options.Length; number++)
            {
                options[number - 1] = new CardEffectChoiceOption(
                    number,
                    number.ToString(),
                    numericValue: number);
            }

            return Array.AsReadOnly(options);
        }
    }
}
