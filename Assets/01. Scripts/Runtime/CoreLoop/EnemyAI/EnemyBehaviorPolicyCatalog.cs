using System;
using System.Collections.Generic;

namespace DiaBlackJack.CoreLoop
{
    public static class EnemyBehaviorPolicyCatalog
    {
        public const string Simple = "simple-16-stand-17";

        public static IEnemyBehaviorPolicy CreateByKey(string key)
        {
            ValidateKey(key);
            if (StringComparer.Ordinal.Equals(key, Simple))
            {
                return new SimpleEnemyPolicy();
            }

            throw new KeyNotFoundException($"Enemy behavior policy '{key}' does not exist.");
        }

        public static bool Contains(string key)
        {
            return key != null && StringComparer.Ordinal.Equals(key, Simple);
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
