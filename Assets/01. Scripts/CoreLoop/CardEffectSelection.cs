using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DiaBlackJack.CoreLoop
{
    public enum CardEffectChoiceKind
    {
        None,
        TakePeekedCard,
        DiscardOpponentFaceUpCard,
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
        private readonly ReadOnlyCollection<BlackjackCard> _temporaryCards;

        public PendingCardEffect(
            int sourceCardId,
            CardEffectKind effectKind,
            string prompt,
            CardEffectChoiceKind choiceKind,
            IEnumerable<CardEffectChoiceOption> options)
            : this(
                sourceCardId,
                effectKind,
                prompt,
                choiceKind,
                options,
                Array.Empty<BlackjackCard>())
        {
        }

        internal PendingCardEffect(
            int sourceCardId,
            CardEffectKind effectKind,
            string prompt,
            CardEffectChoiceKind choiceKind,
            IEnumerable<CardEffectChoiceOption> options,
            IEnumerable<BlackjackCard> temporaryCards)
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

            if (temporaryCards == null)
            {
                throw new ArgumentNullException(nameof(temporaryCards));
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

            var copiedTemporaryCards = new List<BlackjackCard>();
            var temporaryCardIds = new HashSet<int>();
            foreach (BlackjackCard card in temporaryCards)
            {
                if (card == null)
                {
                    throw new ArgumentException(
                        "Temporary cards cannot contain null.",
                        nameof(temporaryCards));
                }

                if (!temporaryCardIds.Add(card.Id))
                {
                    throw new ArgumentException(
                        $"Temporary card id {card.Id} is duplicated.",
                        nameof(temporaryCards));
                }

                copiedTemporaryCards.Add(card);
            }

            _temporaryCards = copiedTemporaryCards.AsReadOnly();
        }

        public CardEffectChoiceKind ChoiceKind { get; }

        public CardEffectKind EffectKind { get; }

        public IReadOnlyList<CardEffectChoiceOption> Options => _options;

        public string Prompt { get; }

        public int SourceCardId { get; }

        internal IReadOnlyList<BlackjackCard> TemporaryCards => _temporaryCards;

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
