using System;
using System.Collections.Generic;

namespace DiaBlackJack.CoreLoop
{
    public sealed class BlackjackHand
    {
        private readonly List<BlackjackCard> _cards = new List<BlackjackCard>();

        public IReadOnlyList<BlackjackCard> Cards => _cards;

        public int Count => _cards.Count;

        public void Add(BlackjackCard card)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            _cards.Add(card);
        }

        public BlackjackCard[] TakeAll()
        {
            BlackjackCard[] cards = _cards.ToArray();
            _cards.Clear();
            return cards;
        }
    }
}
