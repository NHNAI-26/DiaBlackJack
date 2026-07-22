using System;
using System.Collections.Generic;
using Border.Core;

namespace DiaBlackJack.CoreLoop
{
    public sealed class DemonContractDeck
    {
        public const int CandidateCount = 3;

        private readonly HashSet<int> _availableCardIds = new HashSet<int>();
        private readonly List<DemonContractCard> _discardPile = new List<DemonContractCard>();
        private readonly List<DemonContractCard> _drawPile = new List<DemonContractCard>();
        private readonly HashSet<int> _knownCardIds = new HashSet<int>();
        private readonly DeterministicRng _random = new DeterministicRng();

        public DemonContractDeck(IEnumerable<DemonContractCard> cards, int seed)
        {
            if (cards == null)
            {
                throw new ArgumentNullException(nameof(cards));
            }

            foreach (DemonContractCard card in cards)
            {
                if (card == null)
                {
                    throw new ArgumentException(
                        "Demon contract deck cannot contain null.",
                        nameof(cards));
                }

                if (!_knownCardIds.Add(card.Id))
                {
                    throw new ArgumentException(
                        $"Demon contract card id {card.Id} is duplicated.",
                        nameof(cards));
                }

                _availableCardIds.Add(card.Id);
                _drawPile.Add(card);
            }

            TotalCardCount = _drawPile.Count;
            _random.Reseed(seed);
            Shuffle(_drawPile);
        }

        public int AvailableCardCount => DrawCount + DiscardCount;

        public bool CanTakeCandidates => AvailableCardCount >= CandidateCount;

        public int CardsInPlayCount => TotalCardCount - _availableCardIds.Count;

        public int DiscardCount => _discardPile.Count;

        public int DrawCount => _drawPile.Count;

        public int TotalCardCount { get; }

        public void Discard(DemonContractCard card)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            if (!_knownCardIds.Contains(card.Id))
            {
                throw new InvalidOperationException(
                    $"Demon contract card id {card.Id} does not belong to this deck.");
            }

            if (!_availableCardIds.Add(card.Id))
            {
                throw new InvalidOperationException(
                    $"Demon contract card id {card.Id} is already available in this deck.");
            }

            _discardPile.Add(card);
        }

        public void Discard(IEnumerable<DemonContractCard> cards)
        {
            if (cards == null)
            {
                throw new ArgumentNullException(nameof(cards));
            }

            foreach (DemonContractCard card in cards)
            {
                Discard(card);
            }
        }

        public IReadOnlyList<DemonContractCard> TakeCandidates()
        {
            if (!CanTakeCandidates)
            {
                throw new InvalidOperationException(
                    $"At least {CandidateCount} available demon contract cards are required.");
            }

            var candidates = new List<DemonContractCard>(CandidateCount);
            for (int i = 0; i < CandidateCount; i++)
            {
                if (_drawPile.Count == 0)
                {
                    RecycleDiscardPile();
                }

                int lastIndex = _drawPile.Count - 1;
                DemonContractCard card = _drawPile[lastIndex];
                _drawPile.RemoveAt(lastIndex);
                _availableCardIds.Remove(card.Id);
                candidates.Add(card);
            }

            return candidates.AsReadOnly();
        }

        private void RecycleDiscardPile()
        {
            if (_discardPile.Count == 0)
            {
                throw new InvalidOperationException(
                    "No demon contract cards remain to draw or recycle.");
            }

            _drawPile.AddRange(_discardPile);
            _discardPile.Clear();
            Shuffle(_drawPile);
        }

        private void Shuffle(List<DemonContractCard> cards)
        {
            for (int i = cards.Count - 1; i > 0; i--)
            {
                int swapIndex = _random.Next(i + 1);
                DemonContractCard card = cards[i];
                cards[i] = cards[swapIndex];
                cards[swapIndex] = card;
            }
        }
    }
}
