using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DiaBlackJack.CoreLoop;
using UnityEngine;

namespace DiaBlackJack.Content
{
    [CreateAssetMenu(
        fileName = "CodexContentCatalog",
        menuName = "DiaBlackJack/Codex/Content Catalog")]
    public sealed class CodexContentCatalogSO : ScriptableObject
    {
        [Serializable]
        private sealed class EnemyPortraitEntry
        {
            [SerializeField] private string profileKey;
            [SerializeField] private Sprite portrait;

            public string ProfileKey => profileKey;

            public Sprite Portrait => portrait;
        }

        [Serializable]
        private sealed class DemonLoreEntry
        {
            [SerializeField] private string definitionKey;
            [TextArea(2, 4)]
            [SerializeField] private string loreDescription;

            public string DefinitionKey => definitionKey;

            public string LoreDescription => loreDescription;
        }

        [SerializeField] private List<EnemyPortraitEntry> enemyPortraits =
            new List<EnemyPortraitEntry>();
        [SerializeField] private List<DemonLoreEntry> demonLore =
            new List<DemonLoreEntry>();

        public int DemonLoreCount => demonLore == null ? 0 : demonLore.Count;

        public int EnemyPortraitCount =>
            enemyPortraits == null ? 0 : enemyPortraits.Count;

        public IReadOnlyDictionary<string, string> BuildDemonLoreCatalog(
            CardContentCatalog cardCatalog)
        {
            ValidateOrThrow(cardCatalog);
            Dictionary<string, string> result =
                new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (DemonLoreEntry entry in demonLore)
            {
                result.Add(
                    entry.DefinitionKey,
                    entry.LoreDescription.Trim());
            }

            return new ReadOnlyDictionary<string, string>(result);
        }

        public Sprite GetEnemyPortrait(string profileKey)
        {
            if (string.IsNullOrWhiteSpace(profileKey))
            {
                throw new ArgumentException(
                    "Enemy profile key cannot be empty.",
                    nameof(profileKey));
            }

            foreach (EnemyPortraitEntry entry in enemyPortraits)
            {
                if (entry != null &&
                    string.Equals(
                        entry.ProfileKey,
                        profileKey,
                        StringComparison.Ordinal))
                {
                    return entry.Portrait;
                }
            }

            throw new KeyNotFoundException(
                $"Codex portrait for enemy '{profileKey}' does not exist.");
        }

        public void ValidateOrThrow(CardContentCatalog cardCatalog)
        {
            if (cardCatalog == null)
            {
                throw new ArgumentNullException(nameof(cardCatalog));
            }

            ValidateEnemyPortraits();
            ValidateDemonLore(cardCatalog);
        }

        private void OnValidate()
        {
            if ((hideFlags & HideFlags.DontSave) != 0)
            {
                return;
            }

            try
            {
                ValidateBasicEntries();
            }
            catch (Exception exception)
            {
                Debug.LogError(exception.Message, this);
            }
        }

        private void ValidateBasicEntries()
        {
            if (enemyPortraits == null || demonLore == null)
            {
                throw new InvalidOperationException(
                    "Codex content lists cannot be null.");
            }

            foreach (EnemyPortraitEntry entry in enemyPortraits)
            {
                if (entry == null ||
                    string.IsNullOrWhiteSpace(entry.ProfileKey) ||
                    entry.Portrait == null)
                {
                    throw new InvalidOperationException(
                        $"Codex asset '{name}' contains invalid enemy portrait content.");
                }
            }

            foreach (DemonLoreEntry entry in demonLore)
            {
                if (entry == null ||
                    string.IsNullOrWhiteSpace(entry.DefinitionKey) ||
                    string.IsNullOrWhiteSpace(entry.LoreDescription))
                {
                    throw new InvalidOperationException(
                        $"Codex asset '{name}' contains invalid demon lore content.");
                }
            }
        }

        private void ValidateEnemyPortraits()
        {
            ValidateBasicEntries();
            HashSet<string> keys =
                new HashSet<string>(StringComparer.Ordinal);
            foreach (EnemyPortraitEntry entry in enemyPortraits)
            {
                EnemyCombatProfileCatalog.Default.GetByKey(entry.ProfileKey);
                if (!keys.Add(entry.ProfileKey))
                {
                    throw new InvalidOperationException(
                        $"Codex enemy profile '{entry.ProfileKey}' is duplicated.");
                }
            }

            foreach (EnemyCombatProfile profile in
                EnemyCombatProfileCatalog.Default.Profiles)
            {
                if (!keys.Contains(profile.Key))
                {
                    throw new InvalidOperationException(
                        $"Codex enemy profile '{profile.Key}' is missing.");
                }
            }

            if (keys.Count !=
                EnemyCombatProfileCatalog.Default.Profiles.Count)
            {
                throw new InvalidOperationException(
                    "Codex enemy portrait count does not match enemy catalog.");
            }
        }

        private void ValidateDemonLore(CardContentCatalog cardCatalog)
        {
            HashSet<string> keys =
                new HashSet<string>(StringComparer.Ordinal);
            foreach (DemonLoreEntry entry in demonLore)
            {
                cardCatalog.GetDemonByKey(entry.DefinitionKey);
                if (!keys.Add(entry.DefinitionKey))
                {
                    throw new InvalidOperationException(
                        $"Codex demon lore '{entry.DefinitionKey}' is duplicated.");
                }
            }

            foreach (DemonContractDefinition definition in
                cardCatalog.DemonDefinitions)
            {
                if (!keys.Contains(definition.Key))
                {
                    throw new InvalidOperationException(
                        $"Codex demon lore '{definition.Key}' is missing.");
                }
            }

            if (keys.Count != cardCatalog.DemonDefinitions.Count)
            {
                throw new InvalidOperationException(
                    "Codex demon lore count does not match demon catalog.");
            }
        }
    }
}
