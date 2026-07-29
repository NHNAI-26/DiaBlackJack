using System;
using System.Collections.Generic;
using Border.Core;
using DiaBlackJack.CoreLoop;

namespace DiaBlackJack.StageProgression
{
    public sealed class ShopOfferGenerator
    {
        public const int DefaultNormalCardPrice = 3;
        public const int DefaultDemonCardPrice = 3;
        public const int DefaultLighterPrice = 2;
        public const int DefaultWhiskeyPrice = 2;
        public const int DefaultUtilityPriceIncrease = 1;
        public const int DefaultWhiskeyRecovery = 2;

        private readonly DeterministicRng _random = new DeterministicRng();
        private readonly IReadOnlyList<string> _normalDefinitionKeys;
        private readonly IReadOnlyList<string> _demonDefinitionKeys;
        private readonly int _normalCardPrice;
        private readonly int _demonCardPrice;
        private readonly int _lighterBasePrice;
        private readonly int _whiskeyBasePrice;
        private readonly int _utilityPriceIncrease;
        private readonly int _whiskeyRecovery;
        private readonly int _seed;
        private int _nextOfferId;

        public ShopOfferGenerator(
            int seed,
            int normalCardPrice = DefaultNormalCardPrice,
            int demonCardPrice = DefaultDemonCardPrice,
            int lighterBasePrice = DefaultLighterPrice,
            int whiskeyBasePrice = DefaultWhiskeyPrice,
            int utilityPriceIncrease = DefaultUtilityPriceIncrease,
            int whiskeyRecovery = DefaultWhiskeyRecovery)
        {
            ValidateNonNegative(normalCardPrice, nameof(normalCardPrice));
            ValidateNonNegative(demonCardPrice, nameof(demonCardPrice));
            ValidateNonNegative(lighterBasePrice, nameof(lighterBasePrice));
            ValidateNonNegative(whiskeyBasePrice, nameof(whiskeyBasePrice));
            ValidateNonNegative(utilityPriceIncrease, nameof(utilityPriceIncrease));
            if (whiskeyRecovery <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(whiskeyRecovery));
            }

            _normalDefinitionKeys = CreateNormalPool();
            _demonDefinitionKeys = CreateDemonPool();
            _normalCardPrice = normalCardPrice;
            _demonCardPrice = demonCardPrice;
            _lighterBasePrice = lighterBasePrice;
            _whiskeyBasePrice = whiskeyBasePrice;
            _utilityPriceIncrease = utilityPriceIncrease;
            _whiskeyRecovery = whiskeyRecovery;
            _seed = seed;
            _random.Reseed(seed);
        }

        internal int NextOfferOrdinal => _nextOfferId;

        internal ShopOfferGenerator CreateFresh()
        {
            return new ShopOfferGenerator(
                _seed,
                _normalCardPrice,
                _demonCardPrice,
                _lighterBasePrice,
                _whiskeyBasePrice,
                _utilityPriceIncrease,
                _whiskeyRecovery);
        }

        public ShopOffer Generate(
            int visitIndex,
            int utilityPriceLevel,
            bool followsEliteVictory)
        {
            if (visitIndex < 0 || visitIndex > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(visitIndex));
            }

            if (utilityPriceLevel < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(utilityPriceLevel));
            }

            if (_nextOfferId == int.MaxValue)
            {
                throw new InvalidOperationException("Shop offer ids are exhausted.");
            }

            int lighterPrice = CalculateUtilityPrice(
                _lighterBasePrice,
                utilityPriceLevel);
            int whiskeyPrice = CalculateUtilityPrice(
                _whiskeyBasePrice,
                utilityPriceLevel);
            var options = new List<ShopCardOption>(5);
            AddNormalOptions(options, followsEliteVictory);
            AddDemonOptions(options);

            var offer = new ShopOffer(
                _nextOfferId,
                visitIndex,
                utilityPriceLevel,
                lighterPrice,
                whiskeyPrice,
                _whiskeyRecovery,
                options);
            _nextOfferId++;
            return offer;
        }

        private void AddNormalOptions(
            ICollection<ShopCardOption> options,
            bool followsEliteVictory)
        {
            var remainingKeys = new List<string>(_normalDefinitionKeys);
            for (int optionId = 0; optionId < 3; optionId++)
            {
                int selectedIndex = SelectNormalIndex(
                    remainingKeys,
                    followsEliteVictory);
                string selectedKey = remainingKeys[selectedIndex];
                remainingKeys.RemoveAt(selectedIndex);
                options.Add(new ShopCardOption(
                    optionId,
                    ShopCardDeckKind.Normal,
                    selectedKey,
                    _normalCardPrice));
            }
        }

        private void AddDemonOptions(ICollection<ShopCardOption> options)
        {
            var remainingKeys = new List<string>(_demonDefinitionKeys);
            for (int optionId = 3; optionId < 5; optionId++)
            {
                int selectedIndex = _random.Next(remainingKeys.Count);
                string selectedKey = remainingKeys[selectedIndex];
                remainingKeys.RemoveAt(selectedIndex);
                options.Add(new ShopCardOption(
                    optionId,
                    ShopCardDeckKind.Demon,
                    selectedKey,
                    _demonCardPrice));
            }
        }

        private int SelectNormalIndex(
            IReadOnlyList<string> remainingKeys,
            bool followsEliteVictory)
        {
            if (!followsEliteVictory)
            {
                return _random.Next(remainingKeys.Count);
            }

            int totalWeight = 0;
            for (int index = 0; index < remainingKeys.Count; index++)
            {
                totalWeight += IsHighGrade(remainingKeys[index]) ? 2 : 1;
            }

            int selectedWeight = _random.Next(totalWeight);
            for (int index = 0; index < remainingKeys.Count; index++)
            {
                selectedWeight -= IsHighGrade(remainingKeys[index]) ? 2 : 1;
                if (selectedWeight < 0)
                {
                    return index;
                }
            }

            throw new InvalidOperationException("Weighted shop selection failed.");
        }

        private int CalculateUtilityPrice(int basePrice, int utilityPriceLevel)
        {
            if (_utilityPriceIncrease != 0 &&
                utilityPriceLevel > (int.MaxValue - basePrice) / _utilityPriceIncrease)
            {
                throw new OverflowException("Shop utility price exceeds Int32.MaxValue.");
            }

            return basePrice + utilityPriceLevel * _utilityPriceIncrease;
        }

        private static bool IsHighGrade(string definitionKey)
        {
            return CardDefinitionCatalog.GetByKey(definitionKey).Rank >= 5;
        }

        private static IReadOnlyList<string> CreateNormalPool()
        {
            var keys = new List<string>(CardDefinitionCatalog.All.Count);
            foreach (CardDefinition definition in CardDefinitionCatalog.All)
            {
                keys.Add(definition.Key);
            }

            return keys.AsReadOnly();
        }

        private static IReadOnlyList<string> CreateDemonPool()
        {
            var keys = new List<string>(DemonContractCatalog.Default.Definitions.Count);
            foreach (DemonContractDefinition definition in
                     DemonContractCatalog.Default.Definitions)
            {
                keys.Add(definition.Key);
            }

            return keys.AsReadOnly();
        }

        private static void ValidateNonNegative(int value, string parameterName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }
    }
}
