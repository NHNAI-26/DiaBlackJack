using System;

namespace DiaBlackJack.CoreLoop
{
    public sealed class BlackjackCard
    {
        public BlackjackCard(int id, int rank, bool isFaceUp = false)
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
            IsFaceUp = isFaceUp;
        }

        public int Id { get; }

        public int Rank { get; }

        public bool IsFaceUp { get; private set; }

        public void Reveal()
        {
            IsFaceUp = true;
        }

        public void Conceal()
        {
            IsFaceUp = false;
        }
    }
}
