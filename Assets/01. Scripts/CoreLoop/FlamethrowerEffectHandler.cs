using System;
using System.Collections.Generic;

namespace DiaBlackJack.CoreLoop
{
    internal sealed class FlamethrowerEffectHandler :
        IAutomaticCardEffectHandler
    {
        internal const int SkipOptionId = -1;

        public CardEffectKind EffectKind => CardEffectKind.Flamethrower;

        public AutomaticCardEffectStep Begin(
            AutomaticCardEffectContext context)
        {
            return BeginOwnerDiscardChoice(context);
        }

        public AutomaticCardEffectStep ResolveChoice(
            AutomaticCardEffectContext context,
            PendingAutomaticCardInteraction pendingInteraction,
            AutomaticCardChoiceOption selectedOption)
        {
            switch (pendingInteraction.ChoiceKind)
            {
                case AutomaticCardChoiceKind.FlamethrowerOwnerDiscard:
                    ResolveDiscardChoice(
                        context,
                        context.OwnerSide,
                        selectedOption);
                    return BeginOpponentDiscardChoice(context);

                case AutomaticCardChoiceKind.FlamethrowerOpponentDiscard:
                    ResolveDiscardChoice(
                        context,
                        context.OpponentSide,
                        selectedOption);
                    return AutomaticCardEffectStep.Complete(
                        AutomaticCardSourceDisposition.Discard);

                default:
                    throw new InvalidOperationException(
                        "Flamethrower received an invalid choice kind.");
            }
        }

        private static AutomaticCardEffectStep BeginOwnerDiscardChoice(
            AutomaticCardEffectContext context)
        {
            if (context.IsStanding(context.OwnerSide))
            {
                return BeginOpponentDiscardChoice(context);
            }

            IReadOnlyList<BlackjackCard> candidates =
                context.GetFaceUpDiscardCandidates(context.OwnerSide);
            if (candidates.Count == 0)
            {
                return BeginOpponentDiscardChoice(context);
            }

            return CreateDiscardChoice(
                context.OwnerSide,
                AutomaticCardChoiceKind.FlamethrowerOwnerDiscard,
                "Choose one of your face-up cards to discard, or skip.",
                candidates);
        }

        private static AutomaticCardEffectStep BeginOpponentDiscardChoice(
            AutomaticCardEffectContext context)
        {
            if (context.IsStanding(context.OpponentSide))
            {
                return AutomaticCardEffectStep.Complete(
                    AutomaticCardSourceDisposition.Discard);
            }

            IReadOnlyList<BlackjackCard> candidates =
                context.GetFaceUpDiscardCandidates(context.OpponentSide);
            if (candidates.Count == 0)
            {
                return AutomaticCardEffectStep.Complete(
                    AutomaticCardSourceDisposition.Discard);
            }

            return CreateDiscardChoice(
                context.OpponentSide,
                AutomaticCardChoiceKind.FlamethrowerOpponentDiscard,
                "Choose one of your face-up cards to discard, or skip.",
                candidates);
        }

        private static AutomaticCardEffectStep CreateDiscardChoice(
            CombatantSide decisionSide,
            AutomaticCardChoiceKind choiceKind,
            string prompt,
            IReadOnlyList<BlackjackCard> candidates)
        {
            var options = new List<AutomaticCardChoiceOption>(
                candidates.Count + 1)
            {
                new AutomaticCardChoiceOption(
                    SkipOptionId,
                    "Skip")
            };

            foreach (BlackjackCard card in candidates)
            {
                options.Add(new AutomaticCardChoiceOption(
                    card.Id,
                    $"{card.Definition.DisplayName} ({card.Rank})",
                    card.Id));
            }

            return AutomaticCardEffectStep.AwaitChoice(
                decisionSide,
                choiceKind,
                prompt,
                options);
        }

        private static void ResolveDiscardChoice(
            AutomaticCardEffectContext context,
            CombatantSide decisionSide,
            AutomaticCardChoiceOption selectedOption)
        {
            if (selectedOption.OptionId == SkipOptionId)
            {
                if (selectedOption.CardId.HasValue)
                {
                    throw new InvalidOperationException(
                        "Flamethrower skip option cannot identify a card.");
                }

                return;
            }

            if (!selectedOption.CardId.HasValue ||
                selectedOption.OptionId != selectedOption.CardId.Value ||
                !context.TryDiscardFaceUpCard(
                    decisionSide,
                    selectedOption.CardId.Value))
            {
                throw new InvalidOperationException(
                    "Flamethrower discard target is no longer valid.");
            }
        }
    }
}
