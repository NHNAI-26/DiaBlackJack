using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DiaBlackJack.StageProgression
{
    public sealed class BattleRewardOffer
    {
        private readonly ReadOnlyCollection<BattleRewardOption> _options;

        public BattleRewardOffer(
            int offerId,
            BattleRewardTier tier,
            IEnumerable<BattleRewardOption> options)
        {
            if (offerId < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(offerId),
                    "Reward offer id cannot be negative.");
            }

            if (tier != BattleRewardTier.Normal && tier != BattleRewardTier.HighGrade)
            {
                throw new ArgumentOutOfRangeException(nameof(tier), tier, "Unknown reward tier.");
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var copiedOptions = new List<BattleRewardOption>();
            var knownOptionIds = new HashSet<int>();
            var knownDefinitionKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (BattleRewardOption option in options)
            {
                if (option == null)
                {
                    throw new ArgumentException(
                        "A reward offer cannot contain a null option.",
                        nameof(options));
                }

                if (!knownOptionIds.Add(option.OptionId))
                {
                    throw new ArgumentException(
                        $"Reward option id {option.OptionId} is duplicated.",
                        nameof(options));
                }

                if (!knownDefinitionKeys.Add(option.DefinitionKey))
                {
                    throw new ArgumentException(
                        $"Reward definition '{option.DefinitionKey}' is duplicated.",
                        nameof(options));
                }

                copiedOptions.Add(option);
            }

            if (copiedOptions.Count != 3 ||
                !knownOptionIds.Contains(0) ||
                !knownOptionIds.Contains(1) ||
                !knownOptionIds.Contains(2))
            {
                throw new ArgumentException(
                    "A reward offer must contain option ids zero, one and two.",
                    nameof(options));
            }

            OfferId = offerId;
            Tier = tier;
            _options = copiedOptions.AsReadOnly();
        }

        public int OfferId { get; }

        public BattleRewardTier Tier { get; }

        public IReadOnlyList<BattleRewardOption> Options => _options;
    }
}
