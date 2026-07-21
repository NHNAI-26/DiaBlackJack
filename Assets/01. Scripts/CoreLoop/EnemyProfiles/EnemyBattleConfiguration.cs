using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DiaBlackJack.StageProgression;

namespace DiaBlackJack.CoreLoop
{
    public sealed class EnemyBattleConfiguration
    {
        internal EnemyBattleConfiguration(
            EnemyCombatProfile profile,
            int enemyDeckSeed,
            IEnumerable<CardDefinition> enemyDeckDefinitions,
            IEnemyBehaviorPolicy behaviorPolicy)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (enemyDeckDefinitions == null)
            {
                throw new ArgumentNullException(nameof(enemyDeckDefinitions));
            }

            var definitions = new List<CardDefinition>();
            foreach (CardDefinition definition in enemyDeckDefinitions)
            {
                if (definition == null)
                {
                    throw new ArgumentException(
                        "Enemy battle configuration cannot contain a null card definition.",
                        nameof(enemyDeckDefinitions));
                }

                definitions.Add(definition);
            }

            if (definitions.Count == 0)
            {
                throw new ArgumentException(
                    "Enemy battle configuration requires at least one card definition.",
                    nameof(enemyDeckDefinitions));
            }

            ProfileKey = profile.Key;
            Grade = profile.Grade;
            EnemyMaximumSoul = profile.MaximumSoul;
            EnemyDeckSeed = enemyDeckSeed;
            EnemyDeckDefinitions = new ReadOnlyCollection<CardDefinition>(definitions);
            BehaviorPolicy = behaviorPolicy ?? throw new ArgumentNullException(nameof(behaviorPolicy));
            ExpectedRewardTier = profile.Grade == EnemyGrade.Normal
                ? BattleRewardTier.Normal
                : BattleRewardTier.HighGrade;
        }

        public IEnemyBehaviorPolicy BehaviorPolicy { get; }

        public IReadOnlyList<CardDefinition> EnemyDeckDefinitions { get; }

        public int EnemyDeckSeed { get; }

        public int EnemyMaximumSoul { get; }

        public BattleRewardTier ExpectedRewardTier { get; }

        public EnemyGrade Grade { get; }

        public string ProfileKey { get; }

        public BlackjackDeck CreateEnemyDeck()
        {
            var cards = new List<BlackjackCard>(EnemyDeckDefinitions.Count);
            for (int i = 0; i < EnemyDeckDefinitions.Count; i++)
            {
                cards.Add(new BlackjackCard(i, EnemyDeckDefinitions[i]));
            }

            return new BlackjackDeck(cards, EnemyDeckSeed);
        }
    }

    public static class EnemyBattleConfigurationFactory
    {
        public static EnemyBattleConfiguration Create(
            string profileKey,
            int enemyDeckSeed,
            EnemyCombatProfileCatalog catalog = null)
        {
            EnemyCombatProfileCatalog selectedCatalog = catalog ?? EnemyCombatProfileCatalog.Default;
            EnemyCombatProfile profile = selectedCatalog.GetByKey(profileKey);
            var definitions = new List<CardDefinition>(profile.DeckDefinitionKeys.Count);

            foreach (string definitionKey in profile.DeckDefinitionKeys)
            {
                definitions.Add(CardDefinitionCatalog.GetByKey(definitionKey));
            }

            IEnemyBehaviorPolicy behaviorPolicy =
                EnemyBehaviorPolicyCatalog.CreateByKey(profile.BehaviorPolicyKey);

            return new EnemyBattleConfiguration(
                profile,
                enemyDeckSeed,
                definitions,
                behaviorPolicy);
        }
    }
}
