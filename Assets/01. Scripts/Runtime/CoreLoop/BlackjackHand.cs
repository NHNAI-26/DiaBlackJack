using System;
using System.Collections.Generic;

namespace DiaBlackJack.CoreLoop
{
    public sealed class BlackjackHand
    {
        private readonly List<BlackjackCard> _cards = new List<BlackjackCard>();

        public IReadOnlyList<BlackjackCard> Cards => _cards;

        public int Count => _cards.Count;

        public int HiddenCardCount
        {
            get
            {
                int count = 0;
                foreach (BlackjackCard card in _cards)
                {
                    if (!card.IsFaceUp)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public void Add(BlackjackCard card)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            card.PrepareForHand();
            _cards.Add(card);
        }

        public bool Contains(int cardId)
        {
            return TryGetCard(cardId, out _);
        }

        public IReadOnlyList<BlackjackCard> GetFaceUpCards()
        {
            var faceUpCards = new List<BlackjackCard>();
            foreach (BlackjackCard card in _cards)
            {
                if (card.IsFaceUp)
                {
                    faceUpCards.Add(card);
                }
            }

            return faceUpCards.AsReadOnly();
        }

        public bool TryGetCard(int cardId, out BlackjackCard card)
        {
            foreach (BlackjackCard candidate in _cards)
            {
                if (candidate.Id == cardId)
                {
                    card = candidate;
                    return true;
                }
            }

            card = null;
            return false;
        }

        public bool TryTakeCard(int cardId, out BlackjackCard card)
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                if (_cards[i].Id != cardId)
                {
                    continue;
                }

                card = _cards[i];
                _cards.RemoveAt(i);
                return true;
            }

            card = null;
            return false;
        }

        public BlackjackCard[] TakeAll()
        {
            BlackjackCard[] cards = _cards.ToArray();
            _cards.Clear();
            return cards;
        }

        public bool TryTakeSingleHiddenCard(out BlackjackCard hiddenCard)
        {
            hiddenCard = null;
            int hiddenCardIndex = -1;

            for (int i = 0; i < _cards.Count; i++)
            {
                if (_cards[i].IsFaceUp)
                {
                    continue;
                }

                if (hiddenCardIndex >= 0)
                {
                    return false;
                }

                hiddenCardIndex = i;
            }

            if (hiddenCardIndex < 0)
            {
                return false;
            }

            hiddenCard = _cards[hiddenCardIndex];
            _cards.RemoveAt(hiddenCardIndex);
            return true;
        }
    }
}
