using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DiaBlackJack.CoreLoop
{
    public enum CardEffectChoiceKind
    {
        None,
        TakePeekedCard,
        DiscardOwnFaceUpCard,
        DeclareNumber
    }

    public sealed class CardEffectChoiceOption
    {
        public CardEffectChoiceOption(
            int id,
            string label,
            int? cardId = null,
            int? numericValue = null)
        {
            if (id < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            if (string.IsNullOrWhiteSpace(label))
            {
                throw new ArgumentException("Card effect option label cannot be empty.", nameof(label));
            }

            if (cardId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cardId));
            }

            Id = id;
            Label = label;
            CardId = cardId;
            NumericValue = numericValue;
        }

        public int? CardId { get; }

        public int Id { get; }

        public string Label { get; }

        public int? NumericValue { get; }
    }

    public sealed class PendingCardEffect
    {
        private readonly ReadOnlyCollection<CardEffectChoiceOption> _options;

        public PendingCardEffect(
            int sourceCardId,
            CardEffectKind effectKind,
            string prompt,
            CardEffectChoiceKind choiceKind,
            IEnumerable<CardEffectChoiceOption> options)
        {
            if (sourceCardId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceCardId));
            }

            if (!Enum.IsDefined(typeof(CardEffectKind), effectKind) ||
                effectKind == CardEffectKind.None)
            {
                throw new ArgumentOutOfRangeException(nameof(effectKind));
            }

            if (string.IsNullOrWhiteSpace(prompt))
            {
                throw new ArgumentException("Card effect prompt cannot be empty.", nameof(prompt));
            }

            if (!Enum.IsDefined(typeof(CardEffectChoiceKind), choiceKind) ||
                choiceKind == CardEffectChoiceKind.None)
            {
                throw new ArgumentOutOfRangeException(nameof(choiceKind));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var copiedOptions = new List<CardEffectChoiceOption>();
            var knownOptionIds = new HashSet<int>();
            foreach (CardEffectChoiceOption option in options)
            {
                if (option == null)
                {
                    throw new ArgumentException("Card effect options cannot contain null.", nameof(options));
                }

                if (!knownOptionIds.Add(option.Id))
                {
                    throw new ArgumentException(
                        $"Card effect option id {option.Id} is duplicated.",
                        nameof(options));
                }

                copiedOptions.Add(option);
            }

            if (copiedOptions.Count == 0)
            {
                throw new ArgumentException("Pending card effect requires at least one option.", nameof(options));
            }

            SourceCardId = sourceCardId;
            EffectKind = effectKind;
            Prompt = prompt;
            ChoiceKind = choiceKind;
            _options = copiedOptions.AsReadOnly();
        }

        public CardEffectChoiceKind ChoiceKind { get; }

        public CardEffectKind EffectKind { get; }

        public IReadOnlyList<CardEffectChoiceOption> Options => _options;

        public string Prompt { get; }

        public int SourceCardId { get; }

        internal bool TryGetOption(int optionId, out CardEffectChoiceOption option)
        {
            foreach (CardEffectChoiceOption candidate in _options)
            {
                if (candidate.Id == optionId)
                {
                    option = candidate;
                    return true;
                }
            }

            option = null;
            return false;
        }
    }
}
