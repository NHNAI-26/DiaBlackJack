using System;

namespace DiaBlackJack.StageProgression
{
    public sealed class RunCardDefinition
    {
        public RunCardDefinition(int id, int rank)
        {
            if (id < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(id), "Card id cannot be negative.");
            }

            if (rank < 1 || rank > 10)
            {
                throw new ArgumentOutOfRangeException(nameof(rank), "Card rank must be between 1 and 10.");
            }

            Id = id;
            Rank = rank;
        }

        public int Id { get; }

        public int Rank { get; }
    }
}
