using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Border.Core;
using DiaBlackJack.CoreLoop;

namespace DiaBlackJack.StageProgression
{
    public sealed class StartingDemonGrantCard
    {
        internal StartingDemonGrantCard(DemonContractDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            DefinitionKey = definition.Key;
            DisplayName = definition.DisplayName;
            Summary = definition.Summary;
            CostSummary = definition.CostSummary;
        }

        public string CostSummary { get; }

        public string DefinitionKey { get; }

        public string DisplayName { get; }

        public string Summary { get; }
    }

    public sealed class StartingDemonGrant
    {
        private readonly ReadOnlyCollection<StartingDemonGrantCard> _cards;

        internal StartingDemonGrant(
            int grantId,
            IEnumerable<StartingDemonGrantCard> cards)
        {
            if (grantId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(grantId));
            }

            if (cards == null)
            {
                throw new ArgumentNullException(nameof(cards));
            }

            List<StartingDemonGrantCard> copiedCards =
                new List<StartingDemonGrantCard>();
            HashSet<string> definitionKeys =
                new HashSet<string>(StringComparer.Ordinal);
            foreach (StartingDemonGrantCard card in cards)
            {
                if (card == null ||
                    !definitionKeys.Add(card.DefinitionKey))
                {
                    throw new ArgumentException(
                        "Starting demon grant cards must be non-null and distinct.",
                        nameof(cards));
                }

                copiedCards.Add(card);
            }

            if (copiedCards.Count != 2)
            {
                throw new ArgumentException(
                    "Starting demon grant requires exactly two cards.",
                    nameof(cards));
            }

            GrantId = grantId;
            _cards = copiedCards.AsReadOnly();
        }

        public IReadOnlyList<StartingDemonGrantCard> Cards => _cards;

        public int GrantId { get; }
    }

    public sealed class StartingDemonGrantGenerator
    {
        private readonly ReadOnlyCollection<DemonContractDefinition>
            _candidateDefinitions;
        private readonly DeterministicRng _random = new DeterministicRng();
        private int _nextGrantId;

        public StartingDemonGrantGenerator(
            DemonContractCatalog catalog,
            int seed)
            : this(
                catalog,
                seed,
                DemonContractCatalog.PlayerDefaultDemonDeckKeys)
        {
        }

        public StartingDemonGrantGenerator(
            DemonContractCatalog catalog,
            int seed,
            IEnumerable<string> candidateDefinitionKeys)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            if (candidateDefinitionKeys == null)
            {
                throw new ArgumentNullException(nameof(candidateDefinitionKeys));
            }

            List<DemonContractDefinition> candidates =
                new List<DemonContractDefinition>();
            HashSet<string> knownKeys =
                new HashSet<string>(StringComparer.Ordinal);
            foreach (string definitionKey in candidateDefinitionKeys)
            {
                DemonContractDefinition definition =
                    catalog.GetByKey(definitionKey);
                if (!knownKeys.Add(definition.Key))
                {
                    throw new ArgumentException(
                        "Starting demon candidate keys must be distinct.",
                        nameof(candidateDefinitionKeys));
                }

                candidates.Add(definition);
            }

            if (candidates.Count < 2)
            {
                throw new ArgumentException(
                    "Starting demon grant requires at least two unlocked definitions.",
                    nameof(candidateDefinitionKeys));
            }

            _candidateDefinitions = candidates.AsReadOnly();
            _random.Reseed(seed);
        }

        public StartingDemonGrant Generate()
        {
            if (_nextGrantId == int.MaxValue)
            {
                throw new InvalidOperationException(
                    "Starting demon grant ids are exhausted.");
            }

            int firstIndex = _random.Next(_candidateDefinitions.Count);
            int secondIndex = _random.Next(_candidateDefinitions.Count - 1);
            if (secondIndex >= firstIndex)
            {
                secondIndex++;
            }

            int grantId = _nextGrantId++;
            return new StartingDemonGrant(
                grantId,
                new[]
                {
                    new StartingDemonGrantCard(
                        _candidateDefinitions[firstIndex]),
                    new StartingDemonGrantCard(
                        _candidateDefinitions[secondIndex])
                });
        }
    }
}
