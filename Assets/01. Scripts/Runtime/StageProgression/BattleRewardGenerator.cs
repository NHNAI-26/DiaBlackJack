using System;
using System.Collections.Generic;

namespace DiaBlackJack.StageProgression
{
    public sealed class BattleRewardGenerator
    {
        private readonly BattleRewardCatalog _catalog;
        private readonly Random _random;
        private int _nextOfferId;

        public BattleRewardGenerator(BattleRewardCatalog catalog, int seed)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _random = new Random(seed);
        }

        public BattleRewardOffer Generate(BattleRewardTier tier)
        {
            if (_nextOfferId == int.MaxValue)
            {
                throw new InvalidOperationException("Reward offer ids are exhausted.");
            }

            IReadOnlyList<string> pool = _catalog.GetDefinitionKeys(tier);
            var shuffledKeys = new List<string>(pool);
            var options = new BattleRewardOption[3];

            for (int optionId = 0; optionId < options.Length; optionId++)
            {
                int selectedIndex = _random.Next(optionId, shuffledKeys.Count);
                string selectedKey = shuffledKeys[selectedIndex];
                shuffledKeys[selectedIndex] = shuffledKeys[optionId];
                shuffledKeys[optionId] = selectedKey;
                options[optionId] = new BattleRewardOption(optionId, selectedKey);
            }

            var offer = new BattleRewardOffer(_nextOfferId, tier, options);
            _nextOfferId++;
            return offer;
        }
    }
}
