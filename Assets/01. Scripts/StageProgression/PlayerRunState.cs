using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DiaBlackJack.CoreLoop;

namespace DiaBlackJack.StageProgression
{
    public sealed class PlayerRunState
    {
        private readonly ReadOnlyCollection<RunCardDefinition> _initialDeck;
        private readonly List<RunDemonDefinition> _initialDemonCards;
        private readonly ReadOnlyCollection<RunDemonDefinition> _initialDemonDeck;
        private readonly List<RunCardDefinition> _currentDeck;
        private readonly List<RunDemonDefinition> _currentDemonDeck;
        private readonly ReadOnlyCollection<RunCardDefinition> _deck;
        private readonly ReadOnlyCollection<RunDemonDefinition> _demonDeck;
        private readonly int _initialGold;
        private readonly int _initialLastCardId;
        private int _initialLastDemonCardId;
        private int _lastIssuedCardId;
        private int _lastIssuedDemonCardId;

        public PlayerRunState(
            int maximumSoul,
            int currentSoul,
            IEnumerable<RunCardDefinition> deck,
            IEnumerable<RunDemonDefinition> demonDeck = null)
            : this(
                maximumSoul,
                currentSoul,
                deck,
                demonDeck,
                0,
                0,
                null,
                null,
                demonDeck == null,
                false)
        {
        }

        public PlayerRunState(
            int maximumSoul,
            int currentSoul,
            IEnumerable<RunCardDefinition> deck,
            int initialGold,
            IEnumerable<RunDemonDefinition> demonDeck = null)
            : this(
                maximumSoul,
                currentSoul,
                deck,
                demonDeck,
                initialGold,
                initialGold,
                null,
                null,
                demonDeck == null,
                false)
        {
        }

        private PlayerRunState(
            int maximumSoul,
            int currentSoul,
            IEnumerable<RunCardDefinition> deck,
            IEnumerable<RunDemonDefinition> demonDeck,
            int initialGold,
            int currentGold,
            int? lastIssuedCardId,
            int? lastIssuedDemonCardId,
            bool startingDemonGrantCompleted,
            bool hasMadeDemonContract)
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

            if (initialGold < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(initialGold),
                    "Initial gold cannot be negative.");
            }

            if (currentGold < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(currentGold),
                    "Current gold cannot be negative.");
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
            _initialGold = initialGold;
            CurrentGold = currentGold;
            _initialDeck = new List<RunCardDefinition>(cards).AsReadOnly();
            _currentDeck = new List<RunCardDefinition>(cards);
            _deck = _currentDeck.AsReadOnly();
            int maximumCardId = FindMaximumCardId(cards);
            _initialLastCardId = ValidateLastIssuedId(
                lastIssuedCardId,
                maximumCardId,
                nameof(lastIssuedCardId));
            _lastIssuedCardId = _initialLastCardId;

            List<RunDemonDefinition> demonCards = ValidateAndCopyDemonDeck(
                demonDeck ?? CreatePrototypeDemonDeck());
            StartingDemonGrantCompleted = ValidateStartingDemonGrantCompleted(
                startingDemonGrantCompleted,
                demonCards);
            _initialDemonCards = new List<RunDemonDefinition>(demonCards);
            _initialDemonDeck = _initialDemonCards.AsReadOnly();
            _currentDemonDeck = new List<RunDemonDefinition>(demonCards);
            _demonDeck = _currentDemonDeck.AsReadOnly();
            int maximumDemonCardId = FindMaximumDemonCardId(demonCards);
            _initialLastDemonCardId = ValidateLastIssuedId(
                lastIssuedDemonCardId,
                maximumDemonCardId,
                nameof(lastIssuedDemonCardId));
            _lastIssuedDemonCardId = _initialLastDemonCardId;
            HasMadeDemonContract = hasMadeDemonContract;
        }

        public int CurrentSoul { get; private set; }

        public int CurrentGold { get; private set; }

        public int MaximumSoul { get; }

        public bool IsDepleted => CurrentSoul == 0;

        public IReadOnlyList<RunCardDefinition> Deck => _deck;

        public IReadOnlyList<RunDemonDefinition> DemonDeck => _demonDeck;

        public bool StartingDemonGrantCompleted { get; private set; }

        public bool HasMadeDemonContract { get; private set; }

        internal bool CanReceiveStartingDemonGrant =>
            !StartingDemonGrantCompleted && _currentDemonDeck.Count == 0;

        internal int LastIssuedCardId => _lastIssuedCardId;

        internal int LastIssuedDemonCardId => _lastIssuedDemonCardId;

        internal bool CanAddCard => _lastIssuedCardId < int.MaxValue;

        internal bool CanAddDemonCard => _lastIssuedDemonCardId < int.MaxValue;

        internal static PlayerRunState Restore(
            int maximumSoul,
            int currentSoul,
            int currentGold,
            IEnumerable<RunCardDefinition> deck,
            IEnumerable<RunDemonDefinition> demonDeck,
            int lastIssuedCardId,
            int lastIssuedDemonCardId,
            bool startingDemonGrantCompleted = false,
            bool hasMadeDemonContract = false)
        {
            return new PlayerRunState(
                maximumSoul,
                currentSoul,
                deck,
                demonDeck,
                0,
                currentGold,
                lastIssuedCardId,
                lastIssuedDemonCardId,
                startingDemonGrantCompleted,
                hasMadeDemonContract);
        }

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

        internal void AddGold(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    "Gold amount cannot be negative.");
            }

            if (CurrentGold > int.MaxValue - amount)
            {
                throw new OverflowException("Run gold cannot exceed Int32.MaxValue.");
            }

            CurrentGold += amount;
        }

        internal bool TrySpendGold(int amount)
        {
            if (amount < 0 || amount > CurrentGold)
            {
                return false;
            }

            CurrentGold -= amount;
            return true;
        }

        internal bool CanRemoveCard(int cardId)
        {
            if (_currentDeck.Count <= 1)
            {
                return false;
            }

            for (int index = 0; index < _currentDeck.Count; index++)
            {
                if (_currentDeck[index].Id == cardId)
                {
                    return true;
                }
            }

            return false;
        }

        internal bool TryRemoveCard(int cardId)
        {
            if (!CanRemoveCard(cardId))
            {
                return false;
            }

            for (int index = 0; index < _currentDeck.Count; index++)
            {
                if (_currentDeck[index].Id == cardId)
                {
                    _currentDeck.RemoveAt(index);
                    return true;
                }
            }

            throw new InvalidOperationException("Validated run card removal target disappeared.");
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

        internal bool TryGrantStartingDemons(
            IReadOnlyList<string> definitionKeys)
        {
            if (!CanReceiveStartingDemonGrant ||
                definitionKeys == null ||
                definitionKeys.Count != 2 ||
                _lastIssuedDemonCardId > int.MaxValue - 2)
            {
                return false;
            }

            string firstKey = definitionKeys[0];
            string secondKey = definitionKeys[1];
            if (!ContainsDemonDefinition(firstKey) ||
                !ContainsDemonDefinition(secondKey) ||
                string.Equals(firstKey, secondKey, StringComparison.Ordinal))
            {
                return false;
            }

            int firstId = _lastIssuedDemonCardId + 1;
            int secondId = firstId + 1;
            var first = new RunDemonDefinition(firstId, firstKey);
            var second = new RunDemonDefinition(secondId, secondKey);
            _initialDemonCards.Add(first);
            _initialDemonCards.Add(second);
            _currentDemonDeck.Add(first);
            _currentDemonDeck.Add(second);
            _initialLastDemonCardId = secondId;
            _lastIssuedDemonCardId = secondId;
            StartingDemonGrantCompleted = true;
            return true;
        }

        internal void ResetForNewRun()
        {
            CurrentSoul = MaximumSoul;
            CurrentGold = _initialGold;
            HasMadeDemonContract = false;
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

        internal void MarkDemonContractMade()
        {
            HasMadeDemonContract = true;
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
            IReadOnlyList<string> keys =
                DemonContractCatalog.PlayerDefaultDemonDeckKeys;
            var cards = new List<RunDemonDefinition>(keys.Count);
            for (int i = 0; i < keys.Count; i++)
            {
                cards.Add(new RunDemonDefinition(i, keys[i]));
            }

            return cards.AsReadOnly();
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

        private static int ValidateLastIssuedId(
            int? requestedId,
            int currentMaximumId,
            string parameterName)
        {
            int resolvedId = requestedId ?? currentMaximumId;
            if (resolvedId < currentMaximumId || resolvedId < -1)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    "Last issued id cannot be lower than the current maximum id.");
            }

            return resolvedId;
        }

        private static bool ValidateStartingDemonGrantCompleted(
            bool completed,
            IReadOnlyList<RunDemonDefinition> demonCards)
        {
            if (completed && demonCards.Count < 2)
            {
                throw new ArgumentException(
                    "A completed starting demon grant requires at least two demon cards.",
                    nameof(completed));
            }

            return completed || demonCards.Count >= 2;
        }

        private static bool ContainsDemonDefinition(string definitionKey)
        {
            if (string.IsNullOrWhiteSpace(definitionKey))
            {
                return false;
            }

            IReadOnlyList<DemonContractDefinition> definitions =
                DemonContractCatalog.Default.Definitions;
            for (int i = 0; i < definitions.Count; i++)
            {
                if (string.Equals(
                    definitions[i].Key,
                    definitionKey,
                    StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
