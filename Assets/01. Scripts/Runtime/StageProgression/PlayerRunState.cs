using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DiaBlackJack.StageProgression
{
    public sealed class PlayerRunState
    {
        private readonly ReadOnlyCollection<RunCardDefinition> _initialDeck;
        private readonly List<RunCardDefinition> _currentDeck;
        private readonly ReadOnlyCollection<RunCardDefinition> _deck;
        private readonly int _initialLastCardId;
        private int _lastIssuedCardId;

        public PlayerRunState(
            int maximumSoul,
            int currentSoul,
            IEnumerable<RunCardDefinition> deck)
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
        }

        public int CurrentSoul { get; private set; }

        public int MaximumSoul { get; }

        public bool IsDepleted => CurrentSoul == 0;

        public IReadOnlyList<RunCardDefinition> Deck => _deck;

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

        internal void ResetForNewRun()
        {
            CurrentSoul = MaximumSoul;
            _currentDeck.Clear();
            foreach (RunCardDefinition card in _initialDeck)
            {
                _currentDeck.Add(card);
            }

            _lastIssuedCardId = _initialLastCardId;
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
    }
}
