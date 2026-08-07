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
            AutomaticCardEffectStep playerChoice =
                BeginDiscardChoice(context, CombatantSide.Player);
            if (playerChoice != null)
            {
                return playerChoice;
            }

            context.CommitFlamethrowerDecision(
                CombatantSide.Player,
                cardId: null);
            return BeginEnemyDiscardChoiceOrComplete(context);
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
                    "Flamethrower received an invalid choice kind.");
            }

            CommitDiscardChoice(
                context,
                pendingInteraction.DecisionSide,
                selectedOption);
            if (pendingInteraction.DecisionSide == CombatantSide.Player)
            {
                return BeginEnemyDiscardChoiceOrComplete(context);
            }

            context.ApplyCommittedFlamethrowerDecisions();
            return AutomaticCardEffectStep.Complete(
                AutomaticCardSourceDisposition.RetainFaceUp);
        }

        private static AutomaticCardEffectStep
            BeginEnemyDiscardChoiceOrComplete(
                AutomaticCardEffectContext context)
        {
            AutomaticCardEffectStep enemyChoice =
                BeginDiscardChoice(context, CombatantSide.Enemy);
            if (enemyChoice != null)
            {
                return enemyChoice;
            }

            context.CommitFlamethrowerDecision(
                CombatantSide.Enemy,
                cardId: null);
            context.ApplyCommittedFlamethrowerDecisions();
            return AutomaticCardEffectStep.Complete(
                AutomaticCardSourceDisposition.RetainFaceUp);
        }

        private static AutomaticCardEffectStep BeginDiscardChoice(
            AutomaticCardEffectContext context,
            CombatantSide decisionSide)
        {
            if (context.IsStanding(decisionSide))
            {
                return null;
            }

            IReadOnlyList<BlackjackCard> candidates =
                context.GetFaceUpDiscardCandidates(decisionSide);
            if (candidates.Count == 0)
            {
                return null;
            }

            return CreateDiscardChoice(
                decisionSide,
                GetChoiceKind(context, decisionSide),
                candidates);
        }

        private static AutomaticCardChoiceKind GetChoiceKind(
            AutomaticCardEffectContext context,
            CombatantSide decisionSide)
        {
            return decisionSide == context.OwnerSide
                ? AutomaticCardChoiceKind.FlamethrowerOwnerDiscard
                : AutomaticCardChoiceKind.FlamethrowerOpponentDiscard;
        }

        private static AutomaticCardEffectStep CreateDiscardChoice(
            CombatantSide decisionSide,
            AutomaticCardChoiceKind choiceKind,
            IReadOnlyList<BlackjackCard> candidates)
        {
            var options = new List<AutomaticCardChoiceOption>(
                candidates.Count + 1)
            {
                new AutomaticCardChoiceOption(
                    SkipOptionId,
                    "버리지 않기")
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
                CombatPromptId.AutomaticFlamethrowerChooseDiscard,
                options);
        }

        private static void CommitDiscardChoice(
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

                context.CommitFlamethrowerDecision(
                    decisionSide,
                    cardId: null);
                return;
            }

            if (!selectedOption.CardId.HasValue ||
                selectedOption.OptionId != selectedOption.CardId.Value)
            {
                throw new InvalidOperationException(
                    "Flamethrower discard target is no longer valid.");
            }

            context.CommitFlamethrowerDecision(
                decisionSide,
                selectedOption.CardId.Value);
        }
    }
}
