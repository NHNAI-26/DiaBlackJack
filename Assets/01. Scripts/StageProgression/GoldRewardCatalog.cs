using System;
using System.Collections.Generic;
using DiaBlackJack.CoreLoop;

namespace DiaBlackJack.StageProgression
{
    public sealed class GoldRewardCatalog
    {
        private static GoldRewardCatalog PrototypeCatalog =
            CreateFallbackPrototype();

        private readonly Dictionary<string, int> _amountsByProfileKey;

        public GoldRewardCatalog(IEnumerable<KeyValuePair<string, int>> rewards)
        {
            if (rewards == null)
            {
                throw new ArgumentNullException(nameof(rewards));
            }

            _amountsByProfileKey = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, int> reward in rewards)
            {
                if (string.IsNullOrWhiteSpace(reward.Key))
                {
                    throw new ArgumentException(
                        "Gold reward profile key cannot be empty.",
                        nameof(rewards));
                }

                if (reward.Value < 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(rewards),
                        "Gold reward amount cannot be negative.");
                }

                if (!_amountsByProfileKey.TryAdd(reward.Key, reward.Value))
                {
                    throw new ArgumentException(
                        $"Gold reward profile key '{reward.Key}' is duplicated.",
                        nameof(rewards));
                }
            }

            if (_amountsByProfileKey.Count == 0)
            {
                throw new ArgumentException(
                    "Gold reward catalog must contain at least one reward.",
                    nameof(rewards));
            }
        }

        public static GoldRewardCatalog CreatePrototype()
        {
            return PrototypeCatalog;
        }

        public static void Install(GoldRewardCatalog catalog)
        {
            PrototypeCatalog = catalog ??
                throw new ArgumentNullException(nameof(catalog));
        }

        private static GoldRewardCatalog CreateFallbackPrototype()
        {
            return new GoldRewardCatalog(new[]
            {
                CreateReward(EnemyCombatProfileCatalog.CowardlyGamblerKey, 100),
                CreateReward(EnemyCombatProfileCatalog.GunslingerKey, 120),
                CreateReward(EnemyCombatProfileCatalog.CultistKey, 200),
                CreateReward(EnemyCombatProfileCatalog.TricksterKey, 300),
                CreateReward(EnemyCombatProfileCatalog.EnforcerKey, 300),
                CreateReward(EnemyCombatProfileCatalog.FinalBossKey, 0)
            });
        }

        public int GetAmount(string profileKey)
        {
            if (string.IsNullOrWhiteSpace(profileKey))
            {
                throw new ArgumentException(
                    "Gold reward profile key cannot be empty.",
                    nameof(profileKey));
            }

            if (!_amountsByProfileKey.TryGetValue(profileKey, out int amount))
            {
                throw new KeyNotFoundException(
                    $"Gold reward for profile '{profileKey}' does not exist.");
            }

            return amount;
        }

        private static KeyValuePair<string, int> CreateReward(
            string profileKey,
            int amount)
        {
            return new KeyValuePair<string, int>(profileKey, amount);
        }
    }
}
