using System;
using System.Collections.Generic;

namespace DiaBlackJack.CoreLoop
{
    public sealed class BlackjackHand
    {
        private readonly List<BlackjackCard> _cards = new List<BlackjackCard>();
        private readonly HashSet<int> _hiddenCardIds = new HashSet<int>();

        public IReadOnlyList<BlackjackCard> Cards => _cards;

        public int Count => _cards.Count;

        public int HiddenCardCount => _hiddenCardIds.Count;

        public void Add(BlackjackCard card)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            Add(card, isHiddenRole: !card.IsFaceUp);
        }

        internal void AddPublicRole(BlackjackCard card)
        {
            Add(card, isHiddenRole: false);
        }

        private void Add(BlackjackCard card, bool isHiddenRole)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            card.PrepareForHand();
            _cards.Add(card);
            if (isHiddenRole)
            {
                _hiddenCardIds.Add(card.Id);
            }
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

        public IReadOnlyList<BlackjackCard> GetPublicCards()
        {
            var publicCards = new List<BlackjackCard>();
            foreach (BlackjackCard card in _cards)
            {
                if (card.IsFaceUp && !IsHiddenCard(card.Id))
                {
                    publicCards.Add(card);
                }
            }

            return publicCards.AsReadOnly();
        }

        public bool IsHiddenCard(int cardId)
        {
            return _hiddenCardIds.Contains(cardId);
        }

        public bool TryGetSingleHiddenCard(out BlackjackCard hiddenCard)
        {
            hiddenCard = null;
            if (_hiddenCardIds.Count != 1)
            {
                return false;
            }

            int hiddenCardId = default;
            foreach (int candidateId in _hiddenCardIds)
            {
                hiddenCardId = candidateId;
            }

            if (!TryGetCard(hiddenCardId, out hiddenCard))
            {
                throw new InvalidOperationException(
                    "The hidden-role card is missing from its hand.");
            }

            return true;
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
                _hiddenCardIds.Remove(card.Id);

                return true;
            }

            card = null;
            return false;
        }

        public BlackjackCard[] TakeAll()
        {
            BlackjackCard[] cards = _cards.ToArray();
            _cards.Clear();
            _hiddenCardIds.Clear();
            return cards;
        }

        public bool TryTakeSingleHiddenCard(out BlackjackCard hiddenCard)
        {
            hiddenCard = null;
            if (!TryGetSingleHiddenCard(out BlackjackCard currentHiddenCard))
            {
                return false;
            }

            return TryTakeCard(currentHiddenCard.Id, out hiddenCard);
        }
    }
}
