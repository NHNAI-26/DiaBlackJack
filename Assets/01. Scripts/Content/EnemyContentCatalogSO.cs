using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DiaBlackJack.CoreLoop;
using DiaBlackJack.StageProgression;
using UnityEngine;

namespace DiaBlackJack.Content
{
    [CreateAssetMenu(
        fileName = "EnemyContentCatalog",
        menuName = "DiaBlackJack/Enemies/Enemy Content Catalog")]
    public sealed class EnemyContentCatalogSO : ScriptableObject
    {
        [SerializeField] private List<EnemyCombatProfileDefinitionSO> enemies =
            new List<EnemyCombatProfileDefinitionSO>();

        public IReadOnlyList<EnemyCombatProfileDefinitionSO> Enemies =>
            new ReadOnlyCollection<EnemyCombatProfileDefinitionSO>(enemies);

        public int EnemyCount => enemies == null ? 0 : enemies.Count;

        public EnemyCombatProfileCatalog BuildRuntimeCatalog()
        {
            ValidateList();
            var profiles = new List<EnemyCombatProfile>(enemies.Count);
            foreach (EnemyCombatProfileDefinitionSO enemy in enemies)
            {
                profiles.Add(enemy.CreateRuntimeProfile());
            }

            return new EnemyCombatProfileCatalog(profiles);
        }

        public GoldRewardCatalog BuildGoldRewardCatalog()
        {
            ValidateList();
            var rewards = new List<KeyValuePair<string, int>>(enemies.Count);
            foreach (EnemyCombatProfileDefinitionSO enemy in enemies)
            {
                rewards.Add(enemy.CreateGoldReward());
            }

            return new GoldRewardCatalog(rewards);
        }

        public EnemyCombatProfileDefinitionSO GetByKey(string profileKey)
        {
            if (string.IsNullOrWhiteSpace(profileKey))
            {
                throw new ArgumentException(
                    "Enemy profile key cannot be empty.",
                    nameof(profileKey));
            }

            ValidateList();
            foreach (EnemyCombatProfileDefinitionSO enemy in enemies)
            {
                if (string.Equals(
                    enemy.Key,
                    profileKey,
                    StringComparison.Ordinal))
                {
                    return enemy;
                }
            }

            throw new KeyNotFoundException(
                $"Enemy content '{profileKey}' does not exist.");
        }

        public Sprite GetPortrait(string profileKey)
        {
            return GetByKey(profileKey).Portrait;
        }

        public SpeechProfileSO GetSpeechProfile(string profileKey)
        {
            return GetByKey(profileKey).SpeechProfile;
        }

        public void ValidateOrThrow()
        {
            BuildRuntimeCatalog();
            BuildGoldRewardCatalog();
        }

        private void OnValidate()
        {
            if ((hideFlags & HideFlags.DontSave) != 0)
            {
                return;
            }

            try
            {
                ValidateOrThrow();
            }
            catch (Exception exception)
            {
                Debug.LogError(exception.Message, this);
            }
        }

        private void ValidateList()
        {
            if (enemies == null || enemies.Count == 0)
            {
                throw new InvalidOperationException(
                    "Enemy content catalog must contain at least one enemy.");
            }

            foreach (EnemyCombatProfileDefinitionSO enemy in enemies)
            {
                if (enemy == null)
                {
                    throw new InvalidOperationException(
                        "Enemy content catalog contains a null enemy.");
                }
            }
        }
    }
}
