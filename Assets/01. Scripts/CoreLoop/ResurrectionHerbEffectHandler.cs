using System;
using System.Collections.Generic;

namespace DiaBlackJack.CoreLoop
{
    internal sealed class ResurrectionHerbEffectHandler :
        IAutomaticCardEffectHandler
    {
        internal const int DeclineOptionId = 0;
        internal const int PaySoulAndRedealOptionId = 1;

        public CardEffectKind EffectKind => CardEffectKind.ResurrectionHerb;

        public AutomaticCardEffectStep Begin(
            AutomaticCardEffectContext context)
        {
            return AutomaticCardEffectStep.AwaitChoice(
                context.OwnerSide,
                AutomaticCardChoiceKind.ResurrectionHerbDecision,
                "Choose whether to pay 1 soul and redeal your hand.",
                CreateDecisionOptions(context, context.OwnerSide));
        }

        public AutomaticCardEffectStep ResolveChoice(
            AutomaticCardEffectContext context,
            PendingAutomaticCardInteraction pendingInteraction,
            AutomaticCardChoiceOption selectedOption)
        {
            if (pendingInteraction.ChoiceKind ==
                AutomaticCardChoiceKind.ResurrectionHerbDecision)
            {
                AutomaticCardEffectStep terminal = ResolvePayment(
                    context,
                    context.OwnerSide,
                    selectedOption);
                if (terminal != null)
                {
                    return terminal;
                }

                return AutomaticCardEffectStep.AwaitChoice(
                    context.OpponentSide,
                    AutomaticCardChoiceKind.ResurrectionHerbOpponentDecision,
                    "Choose whether to pay 1 soul and redeal your hand.",
                    CreateDecisionOptions(context, context.OpponentSide));
            }

            if (pendingInteraction.ChoiceKind !=
                AutomaticCardChoiceKind.ResurrectionHerbOpponentDecision)
            {
                throw new InvalidOperationException(
                    "Resurrection herb received an invalid choice kind.");
            }

            AutomaticCardEffectStep opponentTerminal = ResolvePayment(
                context,
                context.OpponentSide,
                selectedOption);
            if (opponentTerminal != null)
            {
                return opponentTerminal;
            }

            return AutomaticCardEffectStep.Complete(
                AutomaticCardSourceDisposition.Discard,
                context.DidResurrectionHerbRedeal
                    ? AutomaticCardCompletionFlow.CancelContinuation
                    : AutomaticCardCompletionFlow.ResumeContinuation);
        }

        private static IReadOnlyList<AutomaticCardChoiceOption>
            CreateDecisionOptions(
                AutomaticCardEffectContext context,
                CombatantSide decisionSide)
        {
            var options = new List<AutomaticCardChoiceOption>(2)
            {
                new AutomaticCardChoiceOption(DeclineOptionId, "Decline")
            };
            if (context.CanPayResurrectionHerbSoul(decisionSide))
            {
                options.Add(new AutomaticCardChoiceOption(
                    PaySoulAndRedealOptionId,
                    "Pay 1 soul and redeal your hand"));
            }

            return options.AsReadOnly();
        }

        private static AutomaticCardEffectStep ResolvePayment(
            AutomaticCardEffectContext context,
            CombatantSide decisionSide,
            AutomaticCardChoiceOption selectedOption)
        {
            if (selectedOption.OptionId == DeclineOptionId)
            {
                return null;
            }

            if (selectedOption.OptionId != PaySoulAndRedealOptionId ||
                !context.CanPayResurrectionHerbSoul(decisionSide))
            {
                throw new InvalidOperationException(
                    "Resurrection herb received an invalid payment option.");
            }

            if (context.PayResurrectionHerbSoulAndRedeal(decisionSide))
            {
                return null;
            }

            return AutomaticCardEffectStep.Complete(
                AutomaticCardSourceDisposition.Discard,
                AutomaticCardCompletionFlow.EndBattle);
        }
    }
}
