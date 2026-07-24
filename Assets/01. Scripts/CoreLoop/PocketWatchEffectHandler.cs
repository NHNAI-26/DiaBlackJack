using System;
using System.Collections.Generic;

namespace DiaBlackJack.CoreLoop
{
    internal sealed class PocketWatchEffectHandler :
        IAutomaticCardEffectHandler
    {
        internal const int SkipManualCardOptionId = -1;
        internal const int DiscardSourceOptionId = 0;
        internal const int RetainSourceOptionId = 1;

        private static readonly IReadOnlyList<AutomaticCardChoiceOption>
            SourceDispositionOptions = CreateSourceDispositionOptions();

        public CardEffectKind EffectKind => CardEffectKind.PocketWatch;

        public AutomaticCardEffectStep Begin(
            AutomaticCardEffectContext context)
        {
            IReadOnlyList<BlackjackCard> candidates =
                context.GetOwnerReactivatableManualCards();
            if (candidates.Count == 0)
            {
                return BeginSourceDispositionChoice(context);
            }

            var options = new List<AutomaticCardChoiceOption>(
                candidates.Count + 1)
            {
                new AutomaticCardChoiceOption(
                    SkipManualCardOptionId,
                    "Skip reactivation")
            };

            foreach (BlackjackCard card in candidates)
            {
                options.Add(new AutomaticCardChoiceOption(
                    card.Id,
                    $"{card.Definition.DisplayName} ({card.Rank})",
                    card.Id));
            }

            return AutomaticCardEffectStep.AwaitChoice(
                context.OwnerSide,
                AutomaticCardChoiceKind.PocketWatchManualCard,
                "Choose one used manual card to reactivate, or skip.",
                options);
        }

        public AutomaticCardEffectStep ResolveChoice(
            AutomaticCardEffectContext context,
            PendingAutomaticCardInteraction pendingInteraction,
            AutomaticCardChoiceOption selectedOption)
        {
            switch (pendingInteraction.ChoiceKind)
            {
                case AutomaticCardChoiceKind.PocketWatchManualCard:
                    ResolveManualCardChoice(context, selectedOption);
                    return BeginSourceDispositionChoice(context);

                case AutomaticCardChoiceKind.PocketWatchSourceDisposition:
                    return ResolveSourceDisposition(selectedOption);

                default:
                    throw new InvalidOperationException(
                        "Pocket watch received an invalid choice kind.");
            }
        }

        private static void ResolveManualCardChoice(
            AutomaticCardEffectContext context,
            AutomaticCardChoiceOption selectedOption)
        {
            if (selectedOption.OptionId == SkipManualCardOptionId)
            {
                if (selectedOption.CardId.HasValue)
                {
                    throw new InvalidOperationException(
                        "Pocket watch skip option cannot identify a card.");
                }

                return;
            }

            if (!selectedOption.CardId.HasValue ||
                selectedOption.OptionId != selectedOption.CardId.Value ||
                !context.TryReactivateOwnerManualCard(
                    selectedOption.CardId.Value))
            {
                throw new InvalidOperationException(
                    "Pocket watch reactivation target is no longer valid.");
            }
        }

        private static AutomaticCardEffectStep BeginSourceDispositionChoice(
            AutomaticCardEffectContext context)
        {
            return AutomaticCardEffectStep.AwaitChoice(
                context.OwnerSide,
                AutomaticCardChoiceKind.PocketWatchSourceDisposition,
                "Choose whether to retain or discard the pocket watch.",
                SourceDispositionOptions);
        }

        private static AutomaticCardEffectStep ResolveSourceDisposition(
            AutomaticCardChoiceOption selectedOption)
        {
            switch (selectedOption.OptionId)
            {
                case DiscardSourceOptionId:
                    return AutomaticCardEffectStep.Complete(
                        AutomaticCardSourceDisposition.Discard);
                case RetainSourceOptionId:
                    return AutomaticCardEffectStep.Complete(
                        AutomaticCardSourceDisposition.RetainFaceUp);
                default:
                    throw new InvalidOperationException(
                        "Pocket watch received an unknown source disposition.");
            }
        }

        private static IReadOnlyList<AutomaticCardChoiceOption>
            CreateSourceDispositionOptions()
        {
            return new[]
            {
                new AutomaticCardChoiceOption(
                    DiscardSourceOptionId,
                    "Discard pocket watch"),
                new AutomaticCardChoiceOption(
                    RetainSourceOptionId,
                    "Retain pocket watch face-up")
            };
        }
    }
}
