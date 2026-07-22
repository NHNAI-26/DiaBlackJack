using System;
using DiaBlackJack.CoreLoop;

namespace DiaBlackJack.StageProgression
{
    public sealed class RunDemonDefinition
    {
        public RunDemonDefinition(int id, string definitionKey)
        {
            if (id < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(id),
                    "Run demon card id cannot be negative.");
            }

            DemonContractDefinition definition =
                DemonContractCatalog.Default.GetByKey(definitionKey);
            Id = id;
            DefinitionKey = definition.Key;
        }

        public string DefinitionKey { get; }

        public int Id { get; }
    }
}
