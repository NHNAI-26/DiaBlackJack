using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DiaBlackJack.CoreLoop;

namespace DiaBlackJack.StageProgression
{
    public sealed class BattleRewardCatalog
    {
        private static readonly string[] DefaultNormalDefinitionKeys =
        {
            "standard-ace-1",
            "standard-plain-2",
            "standard-plain-3",
            "standard-plain-4",
            "crystal-orb-5",
            "threat-hammer-6",
            "auto-pistol-7",
            "auto-pistol-8",
            "military-knife-9",
            "military-knife-10"
        };

        private static readonly string[] DefaultHighGradeDefinitionKeys =
        {
            "crystal-orb-5",
            "threat-hammer-6",
            "auto-pistol-7",
            "auto-pistol-8",
            "military-knife-9",
            "military-knife-10"
        };

        private readonly ReadOnlyCollection<string> _normalDefinitionKeys;
        private readonly ReadOnlyCollection<string> _highGradeDefinitionKeys;

        public BattleRewardCatalog(
            IEnumerable<string> normalDefinitionKeys,
            IEnumerable<string> highGradeDefinitionKeys)
        {
            _normalDefinitionKeys = ValidateAndCopyPool(
                normalDefinitionKeys,
                nameof(normalDefinitionKeys));
            _highGradeDefinitionKeys = ValidateAndCopyPool(
                highGradeDefinitionKeys,
                nameof(highGradeDefinitionKeys));
        }

        public static BattleRewardCatalog CreateDefault()
        {
            return new BattleRewardCatalog(
                DefaultNormalDefinitionKeys,
                DefaultHighGradeDefinitionKeys);
        }

        public IReadOnlyList<string> GetDefinitionKeys(BattleRewardTier tier)
        {
            switch (tier)
            {
                case BattleRewardTier.Normal:
                    return _normalDefinitionKeys;
                case BattleRewardTier.HighGrade:
                    return _highGradeDefinitionKeys;
                default:
                    throw new ArgumentOutOfRangeException(nameof(tier), tier, "Unknown reward tier.");
            }
        }

        public bool Contains(BattleRewardTier tier, string definitionKey)
        {
            IReadOnlyList<string> pool = GetDefinitionKeys(tier);
            for (int i = 0; i < pool.Count; i++)
            {
                if (string.Equals(pool[i], definitionKey, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static ReadOnlyCollection<string> ValidateAndCopyPool(
            IEnumerable<string> definitionKeys,
            string parameterName)
        {
            if (definitionKeys == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            var copiedKeys = new List<string>();
            var knownKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (string definitionKey in definitionKeys)
            {
                if (string.IsNullOrWhiteSpace(definitionKey))
                {
                    throw new ArgumentException(
                        "A reward pool cannot contain an empty definition key.",
                        parameterName);
                }

                if (!knownKeys.Add(definitionKey))
                {
                    throw new ArgumentException(
                        $"Reward definition '{definitionKey}' is duplicated.",
                        parameterName);
                }

                CardDefinition definition = CardDefinitionCatalog.GetByKey(definitionKey);
                copiedKeys.Add(definition.Key);
            }

            if (copiedKeys.Count < 3)
            {
                throw new ArgumentException(
                    "A reward pool must contain at least three definitions.",
                    parameterName);
            }

            return copiedKeys.AsReadOnly();
        }
    }
}
