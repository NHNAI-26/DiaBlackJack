using System;

namespace DiaBlackJack.CoreLoop
{
    public sealed class CardDefinition
    {
        public CardDefinition(
            string key,
            string displayName,
            int rank,
            CardActivationKind activation,
            CardEffectKind effect)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Card definition key cannot be empty.", nameof(key));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Card display name cannot be empty.", nameof(displayName));
            }

            if (rank < 1 || rank > 10)
            {
                throw new ArgumentOutOfRangeException(nameof(rank), "Card rank must be between 1 and 10.");
            }

            if (!Enum.IsDefined(typeof(CardActivationKind), activation))
            {
                throw new ArgumentOutOfRangeException(nameof(activation));
            }

            if (!Enum.IsDefined(typeof(CardEffectKind), effect))
            {
                throw new ArgumentOutOfRangeException(nameof(effect));
            }

            Key = key;
            DisplayName = displayName;
            Rank = rank;
            Activation = activation;
            Effect = effect;
        }

        public CardActivationKind Activation { get; }

        public string DisplayName { get; }

        public CardEffectKind Effect { get; }

        public string Key { get; }

        public int Rank { get; }
    }
}
