using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DiaBlackJack.CoreLoop;

namespace DiaBlackJack.StageProgression
{
    public sealed class PlayerRunState
    {
        private readonly ReadOnlyCollection<RunCardDefinition> _initialDeck;
        private readonly ReadOnlyCollection<RunDemonDefinition> _initialDemonDeck;
        private readonly List<RunCardDefinition> _currentDeck;
        private readonly List<RunDemonDefinition> _currentDemonDeck;
        private readonly ReadOnlyCollection<RunCardDefinition> _deck;
        private readonly ReadOnlyCollection<RunDemonDefinition> _demonDeck;
        private readonly int _initialLastCardId;
        private readonly int _initialLastDemonCardId;
        private int _lastIssuedCardId;
        private int _lastIssuedDemonCardId;

        public PlayerRunState(
            int maximumSoul,
            int currentSoul,
            IEnumerable<RunCardDefinition> deck,
            IEnumerable<RunDemonDefinition> demonDeck = null)
        {
            if (maximumSoul <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumSoul), "Maximum soul must be positive.");
            }

            if (currentSoul < 0 || currentSoul > maximumSoul)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(currentSoul),
                    "Current soul must be between zero and maximum soul.");
            }

            if (deck == null)
            {
                throw new ArgumentNullException(nameof(deck));
            }

            var cards = new List<RunCardDefinition>();
            var knownCardIds = new HashSet<int>();
            foreach (RunCardDefinition card in deck)
            {
                if (card == null)
                {
                    throw new ArgumentException("Run deck cannot contain a null card.", nameof(deck));
                }

                if (!knownCardIds.Add(card.Id))
                {
                    throw new ArgumentException($"Card id {card.Id} is duplicated.", nameof(deck));
                }

                cards.Add(card);
            }

            if (cards.Count == 0)
            {
                throw new ArgumentException("Run deck must contain at least one card.", nameof(deck));
            }

            MaximumSoul = maximumSoul;
            CurrentSoul = currentSoul;
            _initialDeck = new List<RunCardDefinition>(cards).AsReadOnly();
            _currentDeck = new List<RunCardDefinition>(cards);
            _deck = _currentDeck.AsReadOnly();
            _initialLastCardId = FindMaximumCardId(cards);
            _lastIssuedCardId = _initialLastCardId;

            List<RunDemonDefinition> demonCards = ValidateAndCopyDemonDeck(
                demonDeck ?? CreatePrototypeDemonDeck());
            _initialDemonDeck = new List<RunDemonDefinition>(demonCards).AsReadOnly();
            _currentDemonDeck = new List<RunDemonDefinition>(demonCards);
            _demonDeck = _currentDemonDeck.AsReadOnly();
            _initialLastDemonCardId = FindMaximumDemonCardId(demonCards);
            _lastIssuedDemonCardId = _initialLastDemonCardId;
        }

        public int CurrentSoul { get; private set; }

        public int MaximumSoul { get; }

        public bool IsDepleted => CurrentSoul == 0;

        public IReadOnlyList<RunCardDefinition> Deck => _deck;

        public IReadOnlyList<RunDemonDefinition> DemonDeck => _demonDeck;

        public void SetCurrentSoul(int currentSoul)
        {
            if (currentSoul < 0 || currentSoul > MaximumSoul)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(currentSoul),
                    "Current soul must be between zero and maximum soul.");
            }

            CurrentSoul = currentSoul;
        }

        internal RunCardDefinition AddRewardCard(string definitionKey)
        {
            if (_lastIssuedCardId == int.MaxValue)
            {
                throw new InvalidOperationException("Run card ids are exhausted.");
            }

            int nextCardId = _lastIssuedCardId + 1;
            var rewardCard = new RunCardDefinition(nextCardId, definitionKey);
            _currentDeck.Add(rewardCard);
            _lastIssuedCardId = nextCardId;
            return rewardCard;
        }

        internal RunDemonDefinition AddDemonCard(string definitionKey)
        {
            if (_lastIssuedDemonCardId == int.MaxValue)
            {
                throw new InvalidOperationException("Run demon card ids are exhausted.");
            }

            int nextCardId = _lastIssuedDemonCardId + 1;
            var demonCard = new RunDemonDefinition(nextCardId, definitionKey);
            _currentDemonDeck.Add(demonCard);
            _lastIssuedDemonCardId = nextCardId;
            return demonCard;
        }

        internal void ResetForNewRun()
        {
            CurrentSoul = MaximumSoul;
            _currentDeck.Clear();
            foreach (RunCardDefinition card in _initialDeck)
            {
                _currentDeck.Add(card);
            }

            _lastIssuedCardId = _initialLastCardId;

            _currentDemonDeck.Clear();
            foreach (RunDemonDefinition card in _initialDemonDeck)
            {
                _currentDemonDeck.Add(card);
            }

            _lastIssuedDemonCardId = _initialLastDemonCardId;
        }

        private static int FindMaximumCardId(IReadOnlyList<RunCardDefinition> cards)
        {
            int maximumId = -1;
            for (int i = 0; i < cards.Count; i++)
            {
                maximumId = Math.Max(maximumId, cards[i].Id);
            }

            return maximumId;
        }

        private static IReadOnlyList<RunDemonDefinition> CreatePrototypeDemonDeck()
        {
            return new[]
            {
                new RunDemonDefinition(0, DemonContractCatalog.SatanKey),
                new RunDemonDefinition(1, DemonContractCatalog.BelphegorKey),
                new RunDemonDefinition(2, DemonContractCatalog.MammonKey),
                new RunDemonDefinition(3, DemonContractCatalog.LeviathanKey)
            };
        }

        private static List<RunDemonDefinition> ValidateAndCopyDemonDeck(
            IEnumerable<RunDemonDefinition> demonDeck)
        {
            var cards = new List<RunDemonDefinition>();
            var knownCardIds = new HashSet<int>();
            foreach (RunDemonDefinition card in demonDeck)
            {
                if (card == null)
                {
                    throw new ArgumentException(
                        "Run demon deck cannot contain a null card.",
                        nameof(demonDeck));
                }

                if (!knownCardIds.Add(card.Id))
                {
                    throw new ArgumentException(
                        $"Demon card id {card.Id} is duplicated.",
                        nameof(demonDeck));
                }

                cards.Add(card);
            }

            return cards;
        }

        private static int FindMaximumDemonCardId(IReadOnlyList<RunDemonDefinition> cards)
        {
            int maximumId = -1;
            for (int i = 0; i < cards.Count; i++)
            {
                maximumId = Math.Max(maximumId, cards[i].Id);
            }

            return maximumId;
        }
    }
}
