using System;
using System.Collections.Generic;

namespace DiaBlackJack.CoreLoop
{
    internal sealed class LieDetectorEffectHandler :
        IAutomaticCardEffectHandler
    {
        private static readonly IReadOnlyList<AutomaticCardChoiceOption>
            DeclarationOptions = CreateDeclarationOptions();

        public CardEffectKind EffectKind => CardEffectKind.LieDetector;

        public AutomaticCardEffectStep Begin(
            AutomaticCardEffectContext context)
        {
            return AutomaticCardEffectStep.AwaitChoice(
                context.OwnerSide,
                AutomaticCardChoiceKind.LieDetectorNumber,
                "상대 비공개 카드와 비교할 숫자를 선언하세요.",
                DeclarationOptions);
        }

        public AutomaticCardEffectStep ResolveChoice(
            AutomaticCardEffectContext context,
            PendingAutomaticCardInteraction pendingInteraction,
            AutomaticCardChoiceOption selectedOption)
        {
            if (pendingInteraction.ChoiceKind !=
                    AutomaticCardChoiceKind.LieDetectorNumber ||
                !selectedOption.NumericValue.HasValue ||
                selectedOption.NumericValue.Value < 1 ||
                selectedOption.NumericValue.Value > 10)
            {
                throw new InvalidOperationException(
                    "Lie detector received an invalid declaration.");
            }

            int declaredNumber = selectedOption.NumericValue.Value;
            bool wasComparable =
                context.TryCompareSingleOpponentHiddenCard(
                    declaredNumber,
                    out int subjectHiddenCardId,
                    out bool isAtLeastDeclaredNumber);
            context.RecordLieDetectorResult(
                declaredNumber,
                wasComparable ? subjectHiddenCardId : (int?)null,
                wasComparable ? isAtLeastDeclaredNumber : (bool?)null);

            return AutomaticCardEffectStep.Complete(
                AutomaticCardSourceDisposition.Discard);
        }

        private static IReadOnlyList<AutomaticCardChoiceOption>
            CreateDeclarationOptions()
        {
            var options = new List<AutomaticCardChoiceOption>(10);
            for (int declaredNumber = 1;
                declaredNumber <= 10;
                declaredNumber++)
            {
                options.Add(new AutomaticCardChoiceOption(
                    declaredNumber,
                    $"{declaredNumber} 선언",
                    numericValue: declaredNumber));
            }

            return options.AsReadOnly();
        }
    }
}
