using System;
using System.Collections.Generic;

namespace DiaBlackJack.CoreLoop
{
    public enum AutomaticCardChoiceKind
    {
        PoisonDecision,
        ResurrectionHerbDecision,
        ResurrectionHerbOpponentDecision,
        LieDetectorNumber,
        FlamethrowerOwnerDiscard,
        FlamethrowerOpponentDiscard,
        PocketWatchManualCard,
        PocketWatchSourceDisposition
    }

    public enum AutomaticCardSourceDisposition
    {
        Discard,
        RetainFaceUp
    }

    public sealed class AutomaticCardChoiceOption
    {
        public AutomaticCardChoiceOption(
            int optionId,
            string label,
            int? cardId = null,
            int? numericValue = null)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                throw new ArgumentException(
                    "Automatic card option label cannot be empty.",
                    nameof(label));
            }

            OptionId = optionId;
            Label = label;
            CardId = cardId;
            NumericValue = numericValue;
        }

        public int OptionId { get; }

        public string Label { get; }

        public int? CardId { get; }

        public int? NumericValue { get; }
    }

    public sealed class PendingAutomaticCardInteraction
    {
        private readonly IReadOnlyList<AutomaticCardChoiceOption> _options;

        internal PendingAutomaticCardInteraction(
            int interactionId,
            int sourceCardId,
            CardEffectKind effectKind,
            CombatantSide ownerSide,
            CombatantSide decisionSide,
            AutomaticCardChoiceKind choiceKind,
            string prompt,
            IReadOnlyList<AutomaticCardChoiceOption> options)
        {
            if (interactionId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(interactionId));
            }

            if (string.IsNullOrWhiteSpace(prompt))
            {
                throw new ArgumentException(
                    "Automatic card prompt cannot be empty.",
                    nameof(prompt));
            }

            if (options == null || options.Count == 0)
            {
                throw new ArgumentException(
                    "Automatic card interaction requires at least one option.",
                    nameof(options));
            }

            var copy = new List<AutomaticCardChoiceOption>(options.Count);
            var optionIds = new HashSet<int>();
            foreach (AutomaticCardChoiceOption option in options)
            {
                if (option == null)
                {
                    throw new ArgumentException(
                        "Automatic card options cannot contain null.",
                        nameof(options));
                }

                if (!optionIds.Add(option.OptionId))
                {
                    throw new ArgumentException(
                        $"Automatic card option id {option.OptionId} is duplicated.",
                        nameof(options));
                }

                copy.Add(option);
            }

            InteractionId = interactionId;
            SourceCardId = sourceCardId;
            EffectKind = effectKind;
            OwnerSide = ownerSide;
            DecisionSide = decisionSide;
            ChoiceKind = choiceKind;
            Prompt = prompt;
            _options = copy.AsReadOnly();
        }

        public int InteractionId { get; }

        public int SourceCardId { get; }

        public CardEffectKind EffectKind { get; }

        public CombatantSide OwnerSide { get; }

        public CombatantSide DecisionSide { get; }

        public AutomaticCardChoiceKind ChoiceKind { get; }

        public string Prompt { get; }

        public IReadOnlyList<AutomaticCardChoiceOption> Options => _options;

        internal bool TryGetOption(
            int optionId,
            out AutomaticCardChoiceOption selectedOption)
        {
            foreach (AutomaticCardChoiceOption option in _options)
            {
                if (option.OptionId == optionId)
                {
                    selectedOption = option;
                    return true;
                }
            }

            selectedOption = null;
            return false;
        }
    }

    public enum AutomaticCardDecisionOutcome
    {
        None,
        Declined,
        Accepted
    }

    public readonly struct AutomaticCardResult
    {
        public AutomaticCardResult(
            int sourceCardId,
            CardEffectKind effectKind,
            CombatantSide ownerSide,
            AutomaticCardSourceDisposition sourceDisposition,
            AutomaticCardDecisionOutcome playerDecision =
                AutomaticCardDecisionOutcome.None,
            AutomaticCardDecisionOutcome enemyDecision =
                AutomaticCardDecisionOutcome.None,
            int? playerTargetCardId = null,
            int? enemyTargetCardId = null)
        {
            SourceCardId = sourceCardId;
            EffectKind = effectKind;
            OwnerSide = ownerSide;
            SourceDisposition = sourceDisposition;
            PlayerDecision = playerDecision;
            EnemyDecision = enemyDecision;
            PlayerTargetCardId = playerTargetCardId;
            EnemyTargetCardId = enemyTargetCardId;
        }

        public int SourceCardId { get; }

        public CardEffectKind EffectKind { get; }

        public CombatantSide OwnerSide { get; }

        public AutomaticCardSourceDisposition SourceDisposition { get; }

        public AutomaticCardDecisionOutcome PlayerDecision { get; }

        public AutomaticCardDecisionOutcome EnemyDecision { get; }

        public int? PlayerTargetCardId { get; }

        public int? EnemyTargetCardId { get; }
    }
}
