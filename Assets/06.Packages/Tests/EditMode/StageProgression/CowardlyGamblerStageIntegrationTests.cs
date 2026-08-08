using System.Collections.Generic;
using DiaBlackJack.CoreLoop;
using NUnit.Framework;

namespace DiaBlackJack.StageProgression.Tests
{
    public sealed class CowardlyGamblerStageIntegrationTests
    {
        [Test]
        public void EPR01_I01_BattleConfigurationUsesNormalRewardSoulAndDedicatedPolicy()
        {
            EnemyBattleConfiguration configuration = EnemyBattleConfigurationFactory.Create(
                EnemyCombatProfileCatalog.CowardlyGamblerKey,
                enemyDeckSeed: 71);

            Assert.That(configuration.Grade, Is.EqualTo(EnemyGrade.Normal));
            Assert.That(configuration.EnemyMaximumSoul, Is.EqualTo(4));
            Assert.That(configuration.EnemyDeckDefinitions.Count, Is.EqualTo(18));
            Assert.That(configuration.ExpectedRewardTier, Is.EqualTo(BattleRewardTier.Normal));
            Assert.That(configuration.BehaviorPolicy, Is.TypeOf<CowardlyGamblerEnemyPolicy>());
        }

        [Test]
        public void EPR01_I02_StageBattleHasAuthoredDeckAndNoEnemyDemonContracts()
        {
            StageDefinition stage = StageDefinition.CreateForEnemyProfile(
                "cowardly-gambler-stage",
                "겁쟁이 도박사",
                StageKind.NormalCombat,
                EnemyCombatProfileCatalog.CowardlyGamblerKey,
                playerDeckSeed: 10,
                enemyDeckSeed: 11);

            CoreLoopBattle battle = StageBattleFactory.Create(stage, CreatePlayer());

            Assert.That(battle.Enemy.Soul.Maximum, Is.EqualTo(4));
            Assert.That(battle.Enemy.Deck.TotalCardCount, Is.EqualTo(18));
            Assert.That(battle.EnemyDemonDeck.TotalCardCount, Is.Zero);
            Assert.That(battle.EnemyBehaviorPolicy, Is.TypeOf<CowardlyGamblerEnemyPolicy>());
        }

        [Test]
        public void EPR01_I03_OpponentSelectionCanOfferAllNonBossProfiles()
        {
            var generator = new OpponentSelectionGenerator(
                EnemyCombatProfileCatalog.Default,
                seed: 20260728,
                eliteOfferChancePercent: 0);
            var offeredKeys = new HashSet<string>();

            for (int stageIndex = 0; stageIndex < 100; stageIndex++)
            {
                OpponentSelectionOffer offer = generator.Generate(stageIndex);
                foreach (OpponentSelectionCandidate candidate in offer.Candidates)
                {
                    offeredKeys.Add(candidate.ProfileKey);
                }
            }

            Assert.That(offeredKeys, Does.Contain(EnemyCombatProfileCatalog.CowardlyGamblerKey));
            Assert.That(offeredKeys, Does.Contain(EnemyCombatProfileCatalog.GunslingerKey));
            Assert.That(offeredKeys, Does.Contain(EnemyCombatProfileCatalog.CultistKey));
            Assert.That(offeredKeys, Does.Contain(EnemyCombatProfileCatalog.TricksterKey));
            Assert.That(offeredKeys, Does.Contain(EnemyCombatProfileCatalog.EnforcerKey));
            Assert.That(offeredKeys.Count, Is.EqualTo(5));
        }

        private static PlayerRunState CreatePlayer()
        {
            var cards = new List<RunCardDefinition>(20);
            for (int index = 0; index < 20; index++)
            {
                cards.Add(new RunCardDefinition(index, index % 10 + 1));
            }

            return new PlayerRunState(12, 12, cards);
        }
    }
}
