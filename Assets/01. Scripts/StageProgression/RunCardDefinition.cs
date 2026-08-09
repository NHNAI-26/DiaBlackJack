using System;
using DiaBlackJack.CoreLoop;

namespace DiaBlackJack.StageProgression
{
    public sealed class RunCardDefinition
    {
        public RunCardDefinition(
            int id,
            int rank,
            CardSuit suit = CardSuit.Spade)
        {
            ValidateId(id);
            CardSuitUtility.Validate(suit, nameof(suit));
            CardDefinition definition = CardDefinitionCatalog.GetDefaultForRank(rank);
            Id = id;
            DefinitionKey = definition.Key;
            Rank = definition.Rank;
            Suit = NormalizeSuit(definition, suit);
        }

        public RunCardDefinition(
            int id,
            string definitionKey,
            CardSuit suit = CardSuit.Spade)
        {
            ValidateId(id);
            CardSuitUtility.Validate(suit, nameof(suit));
            CardDefinition definition = CardDefinitionCatalog.GetByKey(definitionKey);
            Id = id;
            DefinitionKey = definition.Key;
            Rank = definition.Rank;
            Suit = NormalizeSuit(definition, suit);
        }

        public string DefinitionKey { get; }

        public int Id { get; }

        public int Rank { get; }

        public CardSuit Suit { get; }

        private static CardSuit NormalizeSuit(
            CardDefinition definition,
            CardSuit suit)
        {
            return definition.Activation == CardActivationKind.Automatic
                ? CardSuit.Spade
                : suit;
        }

        private static void ValidateId(int id)
        {
            if (id < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(id), "Card id cannot be negative.");
            }
        }
    }
}
