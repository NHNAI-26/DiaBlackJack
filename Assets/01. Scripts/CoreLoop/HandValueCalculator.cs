using System;
using System.Collections.Generic;

namespace DiaBlackJack.CoreLoop
{
    public readonly struct HandValue
    {
        public HandValue(int total)
        {
            Total = total;
        }

        public int Total { get; }

        public bool IsBust => Total > 21;

        public bool IsTwentyOne => Total == 21;
    }

    public static class HandValueCalculator
    {
        public static HandValue Calculate(IEnumerable<BlackjackCard> cards)
        {
            return CalculateWithBonus(cards, bonus: 0);
        }

        internal static HandValue CalculateWithBonus(
            IEnumerable<BlackjackCard> cards,
            int bonus)
        {
            if (cards == null)
            {
                throw new ArgumentNullException(nameof(cards));
            }

            if (bonus < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bonus));
            }

            int total = bonus;
            int aceCount = 0;
            foreach (BlackjackCard card in cards)
            {
                if (card == null)
                {
                    throw new ArgumentException("Hand cannot contain a null card.", nameof(cards));
                }

                total += card.Rank;
                if (card.Rank == 1)
                {
                    aceCount++;
                }
            }

            while (aceCount > 0 && total + 10 <= 21)
            {
                total += 10;
                aceCount--;
            }

            return new HandValue(total);
        }
    }
}
