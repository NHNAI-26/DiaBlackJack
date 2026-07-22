using System;

namespace DiaBlackJack.CoreLoop
{
    public sealed class DemonContractCard
    {
        public DemonContractCard(int id, DemonContractDefinition definition)
        {
            if (id < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(id),
                    "Demon contract card id cannot be negative.");
            }

            Id = id;
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        public DemonContractDefinition Definition { get; }

        public string DefinitionKey => Definition.Key;

        public int Id { get; }

        public DemonContractKind Kind => Definition.Kind;
    }
}
