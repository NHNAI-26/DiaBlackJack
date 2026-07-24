using System;
using System.Collections.Generic;

namespace DiaBlackJack.CoreLoop
{
    internal sealed class ResurrectionHerbEffectHandler :
        IAutomaticCardEffectHandler
    {
        internal const int DeclineOptionId = 0;
        internal const int RestartRoundOptionId = 1;

        public CardEffectKind EffectKind => CardEffectKind.ResurrectionHerb;

        public AutomaticCardEffectStep Begin(
            AutomaticCardEffectContext context)
        {
            var options = new List<AutomaticCardChoiceOption>(2)
            {
                new AutomaticCardChoiceOption(
                    DeclineOptionId,
                    "Discard resurrection herb")
            };

            if (context.CanRestartRound)
            {
                options.Add(new AutomaticCardChoiceOption(
                    RestartRoundOptionId,
                    "Both participants lose 1 soul and restart the round"));
            }

            return AutomaticCardEffectStep.AwaitChoice(
                context.OwnerSide,
                AutomaticCardChoiceKind.ResurrectionHerbDecision,
                "Choose whether to restart the current round.",
                options);
        }

        public AutomaticCardEffectStep ResolveChoice(
            AutomaticCardEffectContext context,
            PendingAutomaticCardInteraction pendingInteraction,
            AutomaticCardChoiceOption selectedOption)
        {
            if (pendingInteraction.ChoiceKind !=
                AutomaticCardChoiceKind.ResurrectionHerbDecision)
            {
                throw new InvalidOperationException(
                    "Resurrection herb received an invalid choice kind.");
            }

            switch (selectedOption.OptionId)
            {
                case DeclineOptionId:
                    return AutomaticCardEffectStep.Complete(
                        AutomaticCardSourceDisposition.Discard);

                case RestartRoundOptionId:
                    if (!context.CanRestartRound)
                    {
                        throw new InvalidOperationException(
                            "Both participants must have at least 2 soul to restart the round.");
                    }

                    return AutomaticCardEffectStep.Complete(
                        AutomaticCardSourceDisposition.Discard,
                        AutomaticCardCompletionFlow.RestartRound);

                default:
                    throw new InvalidOperationException(
                        "Resurrection herb received an unknown option.");
            }
        }
    }
}
