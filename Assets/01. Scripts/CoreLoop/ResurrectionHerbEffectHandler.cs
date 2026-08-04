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
                CombatantSide.Player,
                GetChoiceKind(context, CombatantSide.Player),
                "Choose whether to pay 1 soul and redeal your hand.",
                CreateDecisionOptions(context, CombatantSide.Player));
        }

        public AutomaticCardEffectStep ResolveChoice(
            AutomaticCardEffectContext context,
            PendingAutomaticCardInteraction pendingInteraction,
            AutomaticCardChoiceOption selectedOption)
        {
            if (pendingInteraction.ChoiceKind !=
                GetChoiceKind(context, pendingInteraction.DecisionSide))
            {
                throw new InvalidOperationException(
                    "Resurrection herb received an invalid choice kind.");
            }

            CommitDecision(
                context,
                pendingInteraction.DecisionSide,
                selectedOption);
            if (pendingInteraction.DecisionSide == CombatantSide.Player)
            {
                return AutomaticCardEffectStep.AwaitChoice(
                    CombatantSide.Enemy,
                    GetChoiceKind(context, CombatantSide.Enemy),
                    "Choose whether to pay 1 soul and redeal your hand.",
                    CreateDecisionOptions(context, CombatantSide.Enemy));
            }

            bool battleEndedBySoulLoss =
                context.ApplyCommittedResurrectionHerbDecisions();
            return AutomaticCardEffectStep.Complete(
                GetSourceDisposition(context),
                battleEndedBySoulLoss
                    ? AutomaticCardCompletionFlow.EndBattle
                    : context.DidResurrectionHerbRedeal
                    ? AutomaticCardCompletionFlow.CancelContinuation
                    : AutomaticCardCompletionFlow.ResumeContinuation);
        }

        private static AutomaticCardChoiceKind GetChoiceKind(
            AutomaticCardEffectContext context,
            CombatantSide decisionSide)
        {
            return decisionSide == context.OwnerSide
                ? AutomaticCardChoiceKind.ResurrectionHerbDecision
                : AutomaticCardChoiceKind.ResurrectionHerbOpponentDecision;
        }

        private static IReadOnlyList<AutomaticCardChoiceOption>
            CreateDecisionOptions(
                AutomaticCardEffectContext context,
                CombatantSide decisionSide)
        {
            var options = new List<AutomaticCardChoiceOption>(2)
            {
                new AutomaticCardChoiceOption(
                    DeclineOptionId,
                    "부활하지 않기")
            };
            if (context.CanPayResurrectionHerbSoul(decisionSide))
            {
                options.Add(new AutomaticCardChoiceOption(
                    PaySoulAndRedealOptionId,
                    "부활하기"));
            }

            return options.AsReadOnly();
        }

        private static void CommitDecision(
            AutomaticCardEffectContext context,
            CombatantSide decisionSide,
            AutomaticCardChoiceOption selectedOption)
        {
            if (selectedOption.OptionId == DeclineOptionId)
            {
                context.CommitResurrectionHerbDecision(
                    decisionSide,
                    paysSoul: false);
                return;
            }

            if (selectedOption.OptionId != PaySoulAndRedealOptionId ||
                !context.CanPayResurrectionHerbSoul(decisionSide))
            {
                throw new InvalidOperationException(
                    "Resurrection herb received an invalid payment option.");
            }

            context.CommitResurrectionHerbDecision(
                decisionSide,
                paysSoul: true);
        }

        private static AutomaticCardSourceDisposition GetSourceDisposition(
            AutomaticCardEffectContext context)
        {
            return context.DidOwnerResurrectionHerbRedeal
                ? AutomaticCardSourceDisposition.Discard
                : AutomaticCardSourceDisposition.RetainFaceUp;
        }
    }
}
