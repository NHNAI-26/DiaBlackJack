using System;
using System.Collections.Generic;
using Border.Core;

namespace DiaBlackJack.CoreLoop
{
    public sealed class BlackjackDeck
    {
        private readonly List<BlackjackCard> _drawPile;
        private readonly List<BlackjackCard> _discardPile = new List<BlackjackCard>();
        private readonly HashSet<int> _knownCardIds = new HashSet<int>();
        private readonly HashSet<int> _availableCardIds = new HashSet<int>();
        private readonly DeterministicRng _random = new DeterministicRng();

        public BlackjackDeck(IEnumerable<BlackjackCard> cards, int seed)
            : this(cards, seed, shuffleOnCreate: true)
        {
        }

        private BlackjackDeck(IEnumerable<BlackjackCard> cards, int seed, bool shuffleOnCreate)
        {
            if (cards == null)
            {
                throw new ArgumentNullException(nameof(cards));
            }

            _drawPile = new List<BlackjackCard>();
            foreach (BlackjackCard card in cards)
            {
                if (card == null)
                {
                    throw new ArgumentException("Deck cannot contain a null card.", nameof(cards));
                }

                if (!_knownCardIds.Add(card.Id))
                {
                    throw new ArgumentException($"Card id {card.Id} is duplicated.", nameof(cards));
                }

                _availableCardIds.Add(card.Id);
                _drawPile.Add(card);
            }

            if (_drawPile.Count == 0)
            {
                throw new ArgumentException("Deck must contain at least one card.", nameof(cards));
            }

            TotalCardCount = _drawPile.Count;
            _random.Reseed(seed);
            if (shuffleOnCreate)
            {
                Shuffle(_drawPile);
            }
        }

        public int DrawCount => _drawPile.Count;

        public int DiscardCount => _discardPile.Count;

        public int AvailableCardCount => _drawPile.Count + _discardPile.Count;

        public int CardsInPlayCount => TotalCardCount - _availableCardIds.Count;

        public int TotalCardCount { get; }

        public static BlackjackDeck CreateStandard(int seed)
        {
            var cards = new List<BlackjackCard>(20);
            int id = 0;
            for (int rank = 1; rank <= 10; rank++)
            {
                cards.Add(new BlackjackCard(id++, rank));
                cards.Add(new BlackjackCard(id++, rank));
            }

            return new BlackjackDeck(cards, seed);
        }

        public static BlackjackDeck CreateInDrawOrder(IEnumerable<BlackjackCard> cards)
        {
            if (cards == null)
            {
                throw new ArgumentNullException(nameof(cards));
            }

            var drawPile = new List<BlackjackCard>(cards);
            drawPile.Reverse();
            return new BlackjackDeck(drawPile, seed: 0, shuffleOnCreate: false);
        }

        public BlackjackCard Draw()
        {
            if (_drawPile.Count == 0)
            {
                RecycleDiscardPile();
            }

            int lastIndex = _drawPile.Count - 1;
            BlackjackCard card = _drawPile[lastIndex];
            _drawPile.RemoveAt(lastIndex);
            _availableCardIds.Remove(card.Id);
            return card;
        }

        public bool CanDraw(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Draw count cannot be negative.");
            }

            return AvailableCardCount >= count;
        }

        public void Discard(BlackjackCard card)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            if (!_knownCardIds.Contains(card.Id))
            {
                throw new InvalidOperationException($"Card id {card.Id} does not belong to this deck.");
            }

            if (!_availableCardIds.Add(card.Id))
            {
                throw new InvalidOperationException($"Card id {card.Id} is already in this deck.");
            }

            _discardPile.Add(card);
        }

        public void Discard(IEnumerable<BlackjackCard> cards)
        {
            if (cards == null)
            {
                throw new ArgumentNullException(nameof(cards));
            }

            foreach (BlackjackCard card in cards)
            {
                Discard(card);
            }
        }

        private void RecycleDiscardPile()
        {
            if (_discardPile.Count == 0)
            {
                throw new InvalidOperationException("No cards remain to draw or recycle.");
            }

            foreach (BlackjackCard card in _discardPile)
            {
                card.Conceal();
            }

            _drawPile.AddRange(_discardPile);
            _discardPile.Clear();
            Shuffle(_drawPile);
        }

        private void Shuffle(List<BlackjackCard> cards)
        {
            for (int i = cards.Count - 1; i > 0; i--)
            {
                int swapIndex = _random.Next(i + 1);
                BlackjackCard card = cards[i];
                cards[i] = cards[swapIndex];
                cards[swapIndex] = card;
            }
        }
    }
}
