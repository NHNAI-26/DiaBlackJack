using System;
using System.Collections.Generic;

namespace DiaBlackJack.CoreLoop
{
    public static class EnemyBehaviorPolicyCatalog
    {
        public const string Simple = "simple-16-stand-17";
        public const string Gunslinger = "gunslinger-public-inference";
        public const string Cultist = "cultist-aggressive-risk";
        public const string Trickster = "trickster-information-control";
        public const string Enforcer = "enforcer-disruption-pressure";
        public const string FinalBoss = "final-boss-three-phase";

        public static IEnemyBehaviorPolicy CreateByKey(string key)
        {
            ValidateKey(key);
            if (StringComparer.Ordinal.Equals(key, Simple))
            {
                return new SimpleEnemyPolicy();
            }

            if (StringComparer.Ordinal.Equals(key, Gunslinger))
            {
                return new GunslingerEnemyPolicy();
            }

            if (StringComparer.Ordinal.Equals(key, Cultist))
            {
                return new CultistEnemyPolicy();
            }

            if (StringComparer.Ordinal.Equals(key, Trickster))
            {
                return new TricksterEnemyPolicy();
            }

            if (StringComparer.Ordinal.Equals(key, Enforcer))
            {
                return new EnforcerEnemyPolicy();
            }

            if (StringComparer.Ordinal.Equals(key, FinalBoss))
            {
                return new FinalBossEnemyPolicy();
            }

            throw new KeyNotFoundException($"Enemy behavior policy '{key}' does not exist.");
        }

        public static bool Contains(string key)
        {
            return key != null &&
                (StringComparer.Ordinal.Equals(key, Simple) ||
                    StringComparer.Ordinal.Equals(key, Gunslinger) ||
                    StringComparer.Ordinal.Equals(key, Cultist) ||
                    StringComparer.Ordinal.Equals(key, Trickster) ||
                    StringComparer.Ordinal.Equals(key, Enforcer) ||
                    StringComparer.Ordinal.Equals(key, FinalBoss));
        }

        private static void ValidateKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Enemy behavior policy key cannot be empty.", nameof(key));
            }
        }
    }
}
