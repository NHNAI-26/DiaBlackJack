using System;
using System.Collections.Generic;
using DiaBlackJack.CoreLoop;
using NUnit.Framework;

namespace DiaBlackJack.StageProgression.Tests
{
    public sealed class FormalRunGoldRewardTests
    {
        [Test]
        public void RF01B_U01_PrototypeCatalogContainsConfirmedProfileAmounts()
        {
            GoldRewardCatalog catalog = GoldRewardCatalog.CreatePrototype();

            Assert.That(catalog.GetAmount(EnemyCombatProfileCatalog.CowardlyGamblerKey), Is.EqualTo(100));
            Assert.That(catalog.GetAmount(EnemyCombatProfileCatalog.GunslingerKey), Is.EqualTo(120));
            Assert.That(catalog.GetAmount(EnemyCombatProfileCatalog.CultistKey), Is.EqualTo(200));
            Assert.That(catalog.GetAmount(EnemyCombatProfileCatalog.TricksterKey), Is.EqualTo(300));
            Assert.That(catalog.GetAmount(EnemyCombatProfileCatalog.EnforcerKey), Is.EqualTo(300));
            Assert.That(catalog.GetAmount(EnemyCombatProfileCatalog.FinalBossKey), Is.Zero);
        }

        [Test]
        public void RF01B_U02_EliteRewardsMatchBalanceAndBossAwardsNoGold()
        {
            GoldRewardCatalog catalog = GoldRewardCatalog.CreatePrototype();
            int largestNormalReward = Math.Max(
                catalog.GetAmount(EnemyCombatProfileCatalog.TricksterKey),
                Math.Max(
                    catalog.GetAmount(EnemyCombatProfileCatalog.CultistKey),
                    Math.Max(
                        catalog.GetAmount(EnemyCombatProfileCatalog.GunslingerKey),
                        catalog.GetAmount(EnemyCombatProfileCatalog.CowardlyGamblerKey))));
            int eliteReward = catalog.GetAmount(EnemyCombatProfileCatalog.EnforcerKey);
            int bossReward = catalog.GetAmount(EnemyCombatProfileCatalog.FinalBossKey);

            Assert.That(eliteReward, Is.EqualTo(largestNormalReward));
            Assert.That(bossReward, Is.Zero);
        }

        [Test]
        public void RF01B_U03_CatalogRejectsInvalidDefinitionsAndUnknownLookups()
        {
            Assert.Throws<ArgumentNullException>(() => new GoldRewardCatalog(null));
            Assert.Throws<ArgumentException>(() => new GoldRewardCatalog(
                Array.Empty<KeyValuePair<string, int>>()));
            Assert.Throws<ArgumentException>(() => new GoldRewardCatalog(new[]
            {
                new KeyValuePair<string, int>(" ", 1)
            }));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GoldRewardCatalog(new[]
            {
                new KeyValuePair<string, int>("profile", -1)
            }));
            Assert.Throws<ArgumentException>(() => new GoldRewardCatalog(new[]
            {
                new KeyValuePair<string, int>("profile", 1),
                new KeyValuePair<string, int>("profile", 2)
            }));

            GoldRewardCatalog catalog = GoldRewardCatalog.CreatePrototype();
            Assert.Throws<ArgumentException>(() => catalog.GetAmount(" "));
            Assert.Throws<KeyNotFoundException>(() => catalog.GetAmount("missing-profile"));
        }

        [Test]
        public void RF01B_I01_EachProfileVictoryAwardsConfiguredGoldExactlyOnce()
        {
            var expectedRewards = new[]
            {
                new KeyValuePair<string, int>(EnemyCombatProfileCatalog.CowardlyGamblerKey, 100),
                new KeyValuePair<string, int>(EnemyCombatProfileCatalog.GunslingerKey, 120),
                new KeyValuePair<string, int>(EnemyCombatProfileCatalog.CultistKey, 200),
                new KeyValuePair<string, int>(EnemyCombatProfileCatalog.TricksterKey, 300),
                new KeyValuePair<string, int>(EnemyCombatProfileCatalog.EnforcerKey, 300),
                new KeyValuePair<string, int>(EnemyCombatProfileCatalog.FinalBossKey, 0)
            };

            foreach (KeyValuePair<string, int> expected in expectedRewards)
            {
                StageKind stageKind = expected.Key == EnemyCombatProfileCatalog.FinalBossKey
                    ? StageKind.FinalBossCombat
                    : StageKind.NormalCombat;
                StageProgressionSession session = CreateSession(
                    expected.Key,
                    CreateImmediateVictoryBattle,
                    stageKind);

                Assert.That(session.TryStartRun(), Is.True, expected.Key);
                Assert.That(session.Progress.Player.CurrentGold, Is.Zero, expected.Key);
                Assert.That(session.TryPlayerStand(), Is.True, expected.Key);
                Assert.That(
                    session.Progress.State,
                    Is.EqualTo(StageProgressionState.RewardSelection),
                    expected.Key);
                Assert.That(
                    session.Progress.Player.CurrentGold,
                    Is.EqualTo(expected.Value),
                    expected.Key);

                Assert.That(session.TryPlayerStand(), Is.False, expected.Key);
                Assert.That(
                    session.Progress.Player.CurrentGold,
                    Is.EqualTo(expected.Value),
                    expected.Key);
            }
        }

        [Test]
        public void RF01B_I02_FinalBossVictoryAwardsNoGold()
        {
            StageProgressionSession session = CreateSession(
                EnemyCombatProfileCatalog.FinalBossKey,
                CreateImmediateVictoryBattle,
                StageKind.FinalBossCombat);

            Assert.That(session.TryStartRun(), Is.True);
            Assert.That(session.TryPlayerStand(), Is.True);

            Assert.That(session.Progress.State, Is.EqualTo(StageProgressionState.RewardSelection));
            Assert.That(session.Progress.Player.CurrentGold, Is.Zero);
            Assert.That(
                session.Progress.PendingReward.CompletionTarget,
                Is.EqualTo(BattleRewardCompletionTarget.RunVictory));
        }

        [Test]
        public void RF01B_I03_DefeatDoesNotAwardGold()
        {
            StageProgressionSession session = CreateSession(
                EnemyCombatProfileCatalog.CowardlyGamblerKey,
                CreateImmediateDefeatBattle);

            Assert.That(session.TryStartRun(), Is.True);
            Assert.That(session.TryPlayerStand(), Is.True);

            Assert.That(session.Progress.State, Is.EqualTo(StageProgressionState.RunDefeat));
            Assert.That(session.Progress.Player.CurrentGold, Is.Zero);
        }

        [Test]
        public void RF01B_I04_LegacyStageWithoutProfileRemainsGoldNeutral()
        {
            PlayerRunState player = CreatePlayer();
            StageDefinition legacyStage = new StageDefinition(
                "legacy",
                "Legacy",
                StageKind.FinalBossCombat,
                1,
                10,
                11);
            StageProgressionSession session = new StageProgressionSession(
                new RunProgress(new[] { legacyStage }, player),
                (stage, state) => CreateImmediateVictoryBattle(state, stage.EnemyMaximumSoul));

            Assert.That(session.TryStartRun(), Is.True);
            Assert.That(session.TryPlayerStand(), Is.True);

            Assert.That(session.Progress.Player.CurrentGold, Is.Zero);
        }

        private static StageProgressionSession CreateSession(
            string profileKey,
            Func<PlayerRunState, int, CoreLoopBattle> battleFactory,
            StageKind stageKind = StageKind.NormalCombat)
        {
            StageDefinition stage = StageDefinition.CreateForEnemyProfile(
                "profile-stage",
                "Profile Stage",
                stageKind,
                profileKey,
                10,
                11);
            IReadOnlyList<StageDefinition> stages = stageKind == StageKind.FinalBossCombat
                ? new[] { stage }
                : new[]
                {
                    stage,
                    StageDefinition.CreateForEnemyProfile(
                        "final-boss",
                        "Final Boss",
                        StageKind.FinalBossCombat,
                        EnemyCombatProfileCatalog.FinalBossKey,
                        20,
                        21)
                };
            return new StageProgressionSession(
                new RunProgress(stages, CreatePlayer()),
                (definition, player) => battleFactory(player, 1));
        }

        private static PlayerRunState CreatePlayer()
        {
            return new PlayerRunState(
                12,
                12,
                new[]
                {
                    new RunCardDefinition(0, 10),
                    new RunCardDefinition(1, 8),
                    new RunCardDefinition(2, 10),
                    new RunCardDefinition(3, 1)
                });
        }

        private static CoreLoopBattle CreateImmediateVictoryBattle(
            PlayerRunState player,
            int enemyMaximumSoul)
        {
            return CreateBattle(player, enemyMaximumSoul, new[] { 10, 1 }, new[] { 10, 10 });
        }

        private static CoreLoopBattle CreateImmediateDefeatBattle(
            PlayerRunState player,
            int enemyMaximumSoul)
        {
            return new CoreLoopBattle(
                CreateDeck(new[] { 10, 8 }),
                CreateDeck(new[] { 10, 10 }),
                1,
                1,
                enemyMaximumSoul);
        }

        private static CoreLoopBattle CreateBattle(
            PlayerRunState player,
            int enemyMaximumSoul,
            IReadOnlyList<int> playerRanks,
            IReadOnlyList<int> enemyRanks)
        {
            return new CoreLoopBattle(
                CreateDeck(playerRanks),
                CreateDeck(enemyRanks),
                player.MaximumSoul,
                player.CurrentSoul,
                enemyMaximumSoul);
        }

        private static BlackjackDeck CreateDeck(IReadOnlyList<int> ranks)
        {
            var cards = new List<BlackjackCard>(ranks.Count);
            for (int index = 0; index < ranks.Count; index++)
            {
                cards.Add(new BlackjackCard(index, ranks[index]));
            }

            return BlackjackDeck.CreateInDrawOrder(cards);
        }
    }
}
