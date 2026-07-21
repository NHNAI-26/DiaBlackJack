using System;
using DiaBlackJack.CoreLoop;

namespace DiaBlackJack.StageProgression
{
    public sealed class RunCardDefinition
    {
        public RunCardDefinition(int id, int rank)
        {
            ValidateId(id);
            CardDefinition definition = CardDefinitionCatalog.GetDefaultForRank(rank);
            Id = id;
            DefinitionKey = definition.Key;
            Rank = definition.Rank;
        }

        public RunCardDefinition(int id, string definitionKey)
        {
            ValidateId(id);
            CardDefinition definition = CardDefinitionCatalog.GetByKey(definitionKey);
            Id = id;
            DefinitionKey = definition.Key;
            Rank = definition.Rank;
        }

        public string DefinitionKey { get; }

        public int Id { get; }

        public int Rank { get; }

        private static void ValidateId(int id)
        {
            if (id < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(id), "Card id cannot be negative.");
            }
        }
    }
}
