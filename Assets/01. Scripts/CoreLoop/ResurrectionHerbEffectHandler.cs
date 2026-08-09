using System;
using System.Collections.Generic;

namespace DiaBlackJack.CoreLoop
{
    internal sealed class ResurrectionHerbEffectHandler :
        IAutomaticCardEffectHandler
    {
        internal const int DeclineOptionId = 0;
        internal const int RedealOptionId = 1;

        public CardEffectKind EffectKind => CardEffectKind.ResurrectionHerb;

        public AutomaticCardEffectStep Begin(
            AutomaticCardEffectContext context)
        {
            return AutomaticCardEffectStep.AwaitChoice(
                CombatantSide.Player,
                GetChoiceKind(context, CombatantSide.Player),
                CombatPromptId.AutomaticResurrectionHerbDecision,
                CreateDecisionOptions());
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
                    CombatPromptId.AutomaticResurrectionHerbDecision,
                    CreateDecisionOptions());
            }

            context.ApplyCommittedResurrectionHerbDecisions();
            return AutomaticCardEffectStep.Complete(
                GetSourceDisposition(context),
                context.DidResurrectionHerbRedeal
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
            CreateDecisionOptions()
        {
            return new List<AutomaticCardChoiceOption>(2)
            {
                new AutomaticCardChoiceOption(
                    DeclineOptionId,
                    "부활하지 않기"),
                new AutomaticCardChoiceOption(
                    RedealOptionId,
                    "부활하기")
            }.AsReadOnly();
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
                    redeals: false);
                return;
            }

            if (selectedOption.OptionId != RedealOptionId)
            {
                throw new InvalidOperationException(
                    "Resurrection herb received an invalid redeal option.");
            }

            context.CommitResurrectionHerbDecision(
                decisionSide,
                redeals: true);
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
