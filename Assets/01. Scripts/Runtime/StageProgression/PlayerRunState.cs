using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DiaBlackJack.StageProgression
{
    public sealed class PlayerRunState
    {
        private readonly ReadOnlyCollection<RunCardDefinition> _deck;

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
            _deck = cards.AsReadOnly();
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

        internal void ResetForNewRun()
        {
            CurrentSoul = MaximumSoul;
        }
    }
}
