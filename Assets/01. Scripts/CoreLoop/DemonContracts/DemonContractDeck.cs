using System;
using System.Collections.Generic;
using Border.Core;

namespace DiaBlackJack.CoreLoop
{
    public sealed class DemonContractDeck
    {
        public const int MaximumCandidateCount = 2;
        public const int LuciferCandidateCount = 5;

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

        public bool CanTakeCandidates => AvailableCardCount > 0;

        public int CardsInPlayCount => TotalCardCount - _availableCardIds.Count;

        public int DiscardCount => _discardPile.Count;

        public int DrawCount => _drawPile.Count;

        public int TotalCardCount { get; private set; }

        public static DemonContractDeck CreatePrototype(int seed)
        {
            DemonContractCatalog catalog = DemonContractCatalog.Default;
            IReadOnlyList<DemonContractDefinition> definitions = catalog.Definitions;
            var cards = new List<DemonContractCard>(definitions.Count);
            for (int i = 0; i < definitions.Count; i++)
            {
                cards.Add(new DemonContractCard(i, definitions[i]));
            }

            return new DemonContractDeck(cards, seed);
        }

        public bool TryAddAvailableCard(DemonContractCard card)
        {
            if (card == null ||
                TotalCardCount == int.MaxValue ||
                _knownCardIds.Contains(card.Id))
            {
                return false;
            }

            _knownCardIds.Add(card.Id);
            _availableCardIds.Add(card.Id);
            TotalCardCount++;

            int insertIndex = _random.Next(_drawPile.Count + 1);
            _drawPile.Insert(insertIndex, card);
            return true;
        }

        internal bool ContainsAvailableDefinitionKey(string definitionKey)
        {
            if (string.IsNullOrWhiteSpace(definitionKey))
            {
                return false;
            }

            return FindAvailableCard(_drawPile, definitionKey) != null ||
                FindAvailableCard(_discardPile, definitionKey) != null;
        }

        internal bool ContainsDiscardedDefinitionKey(string definitionKey)
        {
            return !string.IsNullOrWhiteSpace(definitionKey) &&
                FindAvailableCard(_discardPile, definitionKey) != null;
        }

        internal bool TryTakeFixedPhaseCards(
            string activeDefinitionKey,
            string discardedDefinitionKey,
            out DemonContractCard activeCard,
            out DemonContractCard discardedCard)
        {
            activeCard = FindAvailableCard(_drawPile, activeDefinitionKey) ??
                FindAvailableCard(_discardPile, activeDefinitionKey);
            discardedCard = FindAvailableCard(_drawPile, discardedDefinitionKey) ??
                FindAvailableCard(_discardPile, discardedDefinitionKey);
            if (activeCard == null ||
                discardedCard == null ||
                activeCard.Id == discardedCard.Id)
            {
                activeCard = null;
                discardedCard = null;
                return false;
            }

            RemoveAvailableCard(activeCard);
            RemoveAvailableCard(discardedCard);
            return true;
        }

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
            return TakeCandidates(MaximumCandidateCount);
        }

        internal IReadOnlyList<DemonContractCard> TakeLuciferCandidates()
        {
            return TakeCandidates(LuciferCandidateCount);
        }

        internal IReadOnlyList<DemonContractCard> TakeCandidates(
            int maximumCandidateCount)
        {
            if (maximumCandidateCount <= 0 ||
                maximumCandidateCount > LuciferCandidateCount)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumCandidateCount));
            }

            if (!CanTakeCandidates)
            {
                throw new InvalidOperationException(
                    "At least one available demon contract card is required.");
            }

            int candidateCount = Math.Min(
                maximumCandidateCount,
                AvailableCardCount);
            var candidates = new List<DemonContractCard>(candidateCount);
            for (int i = 0; i < candidateCount; i++)
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

        private static DemonContractCard FindAvailableCard(
            IReadOnlyList<DemonContractCard> cards,
            string definitionKey)
        {
            for (int index = 0; index < cards.Count; index++)
            {
                DemonContractCard card = cards[index];
                if (string.Equals(
                    card.DefinitionKey,
                    definitionKey,
                    StringComparison.Ordinal))
                {
                    return card;
                }
            }

            return null;
        }

        private void RemoveAvailableCard(DemonContractCard card)
        {
            bool removed = _drawPile.Remove(card) || _discardPile.Remove(card);
            if (!removed || !_availableCardIds.Remove(card.Id))
            {
                throw new InvalidOperationException(
                    $"Demon contract card id {card.Id} is not available.");
            }
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
