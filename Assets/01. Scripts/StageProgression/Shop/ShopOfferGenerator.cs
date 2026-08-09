using System;
using System.Collections.Generic;
using Border.Core;
using DiaBlackJack.CoreLoop;

namespace DiaBlackJack.StageProgression
{
    public sealed class ShopOfferGenerator
    {
        public const int DefaultLighterPrice = 50;
        public const int DefaultWhiskeyPrice = 50;
        public const int DefaultUtilityPriceIncrease = 20;
        public const int DefaultWhiskeyRecovery = 2;

        private readonly CardContentCatalog _cardContentCatalog;
        private readonly DeterministicRng _random = new DeterministicRng();
        private readonly IReadOnlyList<string> _normalDefinitionKeys;
        private readonly IReadOnlyList<string> _demonDefinitionKeys;
        private readonly int _lighterBasePrice;
        private readonly int _whiskeyBasePrice;
        private readonly int _utilityPriceIncrease;
        private readonly int _whiskeyRecovery;
        private readonly int _seed;
        private int _nextOfferId;

        public ShopOfferGenerator(
            int seed,
            int lighterBasePrice = DefaultLighterPrice,
            int whiskeyBasePrice = DefaultWhiskeyPrice,
            int utilityPriceIncrease = DefaultUtilityPriceIncrease,
            int whiskeyRecovery = DefaultWhiskeyRecovery)
            : this(
                CreateLegacyCatalog(),
                seed,
                lighterBasePrice,
                whiskeyBasePrice,
                utilityPriceIncrease,
                whiskeyRecovery)
        {
        }

        public ShopOfferGenerator(
            CardContentCatalog cardContentCatalog,
            int seed,
            int lighterBasePrice = DefaultLighterPrice,
            int whiskeyBasePrice = DefaultWhiskeyPrice,
            int utilityPriceIncrease = DefaultUtilityPriceIncrease,
            int whiskeyRecovery = DefaultWhiskeyRecovery)
        {
            _cardContentCatalog = cardContentCatalog ??
                throw new ArgumentNullException(nameof(cardContentCatalog));
            ValidateNonNegative(lighterBasePrice, nameof(lighterBasePrice));
            ValidateNonNegative(whiskeyBasePrice, nameof(whiskeyBasePrice));
            ValidateNonNegative(utilityPriceIncrease, nameof(utilityPriceIncrease));
            if (whiskeyRecovery <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(whiskeyRecovery));
            }

            _normalDefinitionKeys = CreateNormalPool(_cardContentCatalog);
            _demonDefinitionKeys = CreateDemonPool(_cardContentCatalog);
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
                _cardContentCatalog,
                _seed,
                _lighterBasePrice,
                _whiskeyBasePrice,
                _utilityPriceIncrease,
                _whiskeyRecovery);
        }

        public ShopOffer Generate(
            int visitIndex,
            int utilityPriceLevel,
            bool followsEliteVictory,
            IEnumerable<string> ownedDemonDefinitionKeys = null)
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
                0);
            var options = new List<ShopCardOption>(5);
            AddNormalOptions(options, followsEliteVictory);
            AddDemonOptions(options, ownedDemonDefinitionKeys);

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
                    _cardContentCatalog.GetNormalByKey(selectedKey).BasePurchasePrice));
            }
        }

        private void AddDemonOptions(
            ICollection<ShopCardOption> options,
            IEnumerable<string> ownedDemonDefinitionKeys)
        {
            var remainingKeys = new List<string>(_demonDefinitionKeys);
            if (ownedDemonDefinitionKeys != null)
            {
                var ownedKeys = new HashSet<string>(
                    ownedDemonDefinitionKeys,
                    StringComparer.Ordinal);
                remainingKeys.RemoveAll(ownedKeys.Contains);
            }

            if (remainingKeys.Count < 2)
            {
                throw new InvalidOperationException(
                    "A shop requires two unowned demon card definitions.");
            }

            for (int optionId = 3; optionId < 5; optionId++)
            {
                int selectedIndex = SelectDemonIndex(remainingKeys);
                string selectedKey = remainingKeys[selectedIndex];
                remainingKeys.RemoveAt(selectedIndex);
                options.Add(new ShopCardOption(
                    optionId,
                    ShopCardDeckKind.Demon,
                    selectedKey,
                    _cardContentCatalog.GetDemonByKey(selectedKey).BasePurchasePrice));
            }
        }

        private int SelectNormalIndex(
            IReadOnlyList<string> remainingKeys,
            bool followsEliteVictory)
        {
            int totalWeight = 0;
            for (int index = 0; index < remainingKeys.Count; index++)
            {
                CardDefinition definition = _cardContentCatalog.GetNormalByKey(remainingKeys[index]);
                totalWeight += GetNormalShopWeight(definition, followsEliteVictory);
            }

            int selectedWeight = _random.Next(totalWeight);
            for (int index = 0; index < remainingKeys.Count; index++)
            {
                CardDefinition definition = _cardContentCatalog.GetNormalByKey(remainingKeys[index]);
                selectedWeight -= GetNormalShopWeight(definition, followsEliteVictory);
                if (selectedWeight < 0)
                {
                    return index;
                }
            }

            throw new InvalidOperationException("Weighted shop selection failed.");
        }

        private static int GetNormalShopWeight(
            CardDefinition definition,
            bool followsEliteVictory)
        {
            return definition.ShopWeight *
                (followsEliteVictory && IsHighGrade(definition) ? 2 : 1);
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

        private int SelectDemonIndex(IReadOnlyList<string> remainingKeys)
        {
            int totalWeight = 0;
            for (int index = 0; index < remainingKeys.Count; index++)
            {
                totalWeight += _cardContentCatalog
                    .GetDemonByKey(remainingKeys[index])
                    .ShopWeight;
            }

            int selectedWeight = _random.Next(totalWeight);
            for (int index = 0; index < remainingKeys.Count; index++)
            {
                selectedWeight -= _cardContentCatalog
                    .GetDemonByKey(remainingKeys[index])
                    .ShopWeight;
                if (selectedWeight < 0)
                {
                    return index;
                }
            }

            throw new InvalidOperationException("Weighted demon shop selection failed.");
        }

        private static bool IsHighGrade(CardDefinition definition)
        {
            return definition.Rank >= 5;
        }

        private static CardContentCatalog CreateLegacyCatalog()
        {
            return new CardContentCatalog(
                CardDefinitionCatalog.All,
                DemonContractCatalog.Default.Definitions);
        }

        private static IReadOnlyList<string> CreateNormalPool(
            CardContentCatalog cardContentCatalog)
        {
            var keys = new List<string>(cardContentCatalog.NormalDefinitions.Count);
            foreach (CardDefinition definition in cardContentCatalog.NormalDefinitions)
            {
                keys.Add(definition.Key);
            }

            return keys.AsReadOnly();
        }

        private static IReadOnlyList<string> CreateDemonPool(
            CardContentCatalog cardContentCatalog)
        {
            IReadOnlyList<string> prototypeKeys =
                DemonContractCatalog.PrototypeEnabledDemonKeys;
            var keys = new List<string>(prototypeKeys.Count);
            foreach (string definitionKey in prototypeKeys)
            {
                cardContentCatalog.GetDemonByKey(definitionKey);
                keys.Add(definitionKey);
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
