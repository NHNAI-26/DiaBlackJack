using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Border.Core;
using DiaBlackJack.CoreLoop;

namespace DiaBlackJack.StageProgression
{
    public sealed class StartingDemonSelectionOption
    {
        internal StartingDemonSelectionOption(
            int optionId,
            DemonContractDefinition definition)
        {
            if (optionId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(optionId));
            }

            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            OptionId = optionId;
            DefinitionKey = definition.Key;
            DisplayName = definition.DisplayName;
            Summary = definition.Summary;
            CostSummary = definition.CostSummary;
        }

        public string CostSummary { get; }

        public string DefinitionKey { get; }

        public string DisplayName { get; }

        public int OptionId { get; }

        public string Summary { get; }
    }

    public sealed class StartingDemonSelectionOffer
    {
        private readonly ReadOnlyCollection<StartingDemonSelectionOption> _options;

        internal StartingDemonSelectionOffer(
            int offerId,
            IEnumerable<StartingDemonSelectionOption> options)
        {
            if (offerId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offerId));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            List<StartingDemonSelectionOption> copiedOptions =
                new List<StartingDemonSelectionOption>();
            HashSet<int> optionIds = new HashSet<int>();
            HashSet<string> definitionKeys =
                new HashSet<string>(StringComparer.Ordinal);
            foreach (StartingDemonSelectionOption option in options)
            {
                if (option == null ||
                    !optionIds.Add(option.OptionId) ||
                    !definitionKeys.Add(option.DefinitionKey))
                {
                    throw new ArgumentException(
                        "Starting demon options must be non-null and distinct.",
                        nameof(options));
                }

                copiedOptions.Add(option);
            }

            if (copiedOptions.Count != 2)
            {
                throw new ArgumentException(
                    "Starting demon selection requires exactly two options.",
                    nameof(options));
            }

            OfferId = offerId;
            _options = copiedOptions.AsReadOnly();
        }

        public int OfferId { get; }

        public IReadOnlyList<StartingDemonSelectionOption> Options => _options;

        internal bool TryGetOption(
            int optionId,
            out StartingDemonSelectionOption option)
        {
            for (int i = 0; i < _options.Count; i++)
            {
                if (_options[i].OptionId == optionId)
                {
                    option = _options[i];
                    return true;
                }
            }

            option = null;
            return false;
        }
    }

    public sealed class StartingDemonSelectionGenerator
    {
        private readonly DemonContractCatalog _catalog;
        private readonly DeterministicRng _random = new DeterministicRng();
        private int _nextOfferId;

        public StartingDemonSelectionGenerator(
            DemonContractCatalog catalog,
            int seed)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            if (_catalog.Definitions.Count < 2)
            {
                throw new ArgumentException(
                    "Starting demon selection requires at least two unlocked definitions.",
                    nameof(catalog));
            }

            _random.Reseed(seed);
        }

        public StartingDemonSelectionOffer Generate()
        {
            if (_nextOfferId == int.MaxValue)
            {
                throw new InvalidOperationException(
                    "Starting demon selection offer ids are exhausted.");
            }

            int firstIndex = _random.Next(_catalog.Definitions.Count);
            int secondIndex = _random.Next(_catalog.Definitions.Count - 1);
            if (secondIndex >= firstIndex)
            {
                secondIndex++;
            }

            int offerId = _nextOfferId++;
            return new StartingDemonSelectionOffer(
                offerId,
                new[]
                {
                    new StartingDemonSelectionOption(
                        0,
                        _catalog.Definitions[firstIndex]),
                    new StartingDemonSelectionOption(
                        1,
                        _catalog.Definitions[secondIndex])
                });
        }
    }
}
