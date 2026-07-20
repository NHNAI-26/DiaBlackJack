using System;
using System.Collections.Generic;
using DiaBlackJack.CoreLoop;
using NUnit.Framework;

namespace DiaBlackJack.StageProgression.Tests
{
    public sealed class EnemyProfileStageIntegrationTests
    {
        [TestCase(EnemyCombatProfileCatalog.GunslingerKey, StageKind.NormalCombat)]
        [TestCase(EnemyCombatProfileCatalog.CultistKey, StageKind.NormalCombat)]
        [TestCase(EnemyCombatProfileCatalog.TricksterKey, StageKind.NormalCombat)]
        [TestCase(EnemyCombatProfileCatalog.EnforcerKey, StageKind.NormalCombat)]
        [TestCase(EnemyCombatProfileCatalog.FinalBossKey, StageKind.FinalBossCombat)]
        public void EP06_U01_ProfileSelectionCreatesStageFromPreview(
            string profileKey,
            StageKind kind)
        {
            EnemyProfilePreview preview =
                EnemyCombatProfileCatalog.Default.GetPreviewByKey(profileKey);

            StageDefinition stage = StageDefinition.CreateForEnemyProfile(
                $"stage-{profileKey}",
                preview.DisplayName,
                kind,
                profileKey,
                10,
                11);

            Assert.That(stage.BattleProfileKey, Is.EqualTo(preview.ProfileKey));
            Assert.That(stage.EnemyMaximumSoul, Is.EqualTo(preview.MaximumSoul));
            Assert.That(stage.Kind, Is.EqualTo(kind));
        }

        [Test]
        public void EP06_U02_ProfileSelectionRejectsUnknownAndGradeMismatch()
        {
            Assert.Throws<ArgumentException>(() => StageDefinition.CreateForEnemyProfile(
                "empty",
                "Empty",
                StageKind.NormalCombat,
                " ",
                10,
                11));
            Assert.Throws<KeyNotFoundException>(() => StageDefinition.CreateForEnemyProfile(
                "missing",
                "Missing",
                StageKind.NormalCombat,
                "missing-profile",
                10,
                11));
            Assert.Throws<ArgumentException>(() => StageDefinition.CreateForEnemyProfile(
                "boss-as-normal",
                "Boss As Normal",
                StageKind.NormalCombat,
                EnemyCombatProfileCatalog.FinalBossKey,
                10,
                11));
            Assert.Throws<ArgumentException>(() => StageDefinition.CreateForEnemyProfile(
                "normal-as-boss",
                "Normal As Boss",
                StageKind.FinalBossCombat,
                EnemyCombatProfileCatalog.GunslingerKey,
                10,
                11));
            Assert.Throws<ArgumentException>(() => StageDefinition.CreateForEnemyProfile(
                "elite-as-boss",
                "Elite As Boss",
                StageKind.FinalBossCombat,
                EnemyCombatProfileCatalog.EnforcerKey,
                10,
                11));
        }

        [TestCase(EnemyCombatProfileCatalog.GunslingerKey, "GunslingerEnemyPolicy")]
        [TestCase(EnemyCombatProfileCatalog.CultistKey, "CultistEnemyPolicy")]
        [TestCase(EnemyCombatProfileCatalog.TricksterKey, "TricksterEnemyPolicy")]
        [TestCase(EnemyCombatProfileCatalog.EnforcerKey, "EnforcerEnemyPolicy")]
        [TestCase(EnemyCombatProfileCatalog.FinalBossKey, "FinalBossEnemyPolicy")]
        public void EP06_U03_StageBattleFactoryUsesSelectedDeckSoulAndPolicy(
            string profileKey,
            string expectedPolicyName)
        {
            EnemyProfilePreview preview =
                EnemyCombatProfileCatalog.Default.GetPreviewByKey(profileKey);
            StageDefinition stage = CreateProfileStage(profileKey, 20, 21);
            PlayerRunState player = CreatePlayer(currentSoul: 8);

            CoreLoopBattle battle = StageBattleFactory.Create(stage, player);

            Assert.That(battle.State, Is.EqualTo(CoreLoopState.Initializing));
            Assert.That(battle.Player.Soul.Maximum, Is.EqualTo(12));
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(8));
            Assert.That(battle.Player.Deck.TotalCardCount, Is.EqualTo(player.Deck.Count));
            Assert.That(battle.Enemy.Soul.Maximum, Is.EqualTo(preview.MaximumSoul));
            Assert.That(battle.Enemy.Soul.Current, Is.EqualTo(preview.MaximumSoul));
            Assert.That(battle.Enemy.Deck.TotalCardCount, Is.EqualTo(10));
            Assert.That(battle.EnemyBehaviorPolicy.GetType().Name, Is.EqualTo(expectedPolicyName));
        }

        [Test]
        public void EP06_U04_RepeatedFactoryCreatesFreshCombatStateForAllProfiles()
        {
            PlayerRunState player = CreatePlayer();

            foreach (EnemyCombatProfile profile in EnemyCombatProfileCatalog.Default.Profiles)
            {
                IEnemyBehaviorPolicy previousPolicy = null;
                for (int iteration = 0; iteration < 10; iteration++)
                {
                    StageDefinition stage = CreateProfileStage(
                        profile.Key,
                        iteration,
                        iteration + 100);
                    CoreLoopBattle battle = StageBattleFactory.Create(stage, player);

                    Assert.That(battle.State, Is.EqualTo(CoreLoopState.Initializing));
                    Assert.That(battle.RoundNumber, Is.Zero);
                    Assert.That(battle.Player.Hand.Cards, Is.Empty);
                    Assert.That(battle.Enemy.Hand.Cards, Is.Empty);
                    Assert.That(battle.Enemy.Soul.Current, Is.EqualTo(profile.MaximumSoul));
                    Assert.That(battle.Enemy.Deck.TotalCardCount, Is.EqualTo(10));
                    Assert.That(battle.EnemyBehaviorPolicy, Is.Not.SameAs(previousPolicy));

                    previousPolicy = battle.EnemyBehaviorPolicy;
                }
            }
        }

        [Test]
        public void EP06_U05_LegacyStageKeepsStandardDeckAndSimplePolicy()
        {
            var stage = new StageDefinition(
                "legacy",
                "Legacy",
                StageKind.NormalCombat,
                4,
                10,
                11);

            CoreLoopBattle battle = StageBattleFactory.Create(stage, CreatePlayer());

            Assert.That(stage.BattleProfileKey, Is.Null);
            Assert.That(battle.Enemy.Soul.Maximum, Is.EqualTo(4));
            Assert.That(battle.Enemy.Deck.TotalCardCount, Is.EqualTo(20));
            Assert.That(battle.EnemyBehaviorPolicy, Is.TypeOf<SimpleEnemyPolicy>());
        }

        [Test]
        public void EP06_I01_EliteProfileRewardUsesHighGradeAndClearsStage()
        {
            StageDefinition eliteStage = CreateProfileStage(
                EnemyCombatProfileCatalog.EnforcerKey,
                10,
                11);
            StageDefinition bossStage = CreateProfileStage(
                EnemyCombatProfileCatalog.FinalBossKey,
                12,
                13);
            var session = new StageProgressionSession(
                new RunProgress(new[] { eliteStage, bossStage }, CreatePlayer()),
                CreateDeterministicVictoryBattle);

            Assert.That(session.TryStartRun(), Is.True);
            WinCurrentBattle(session);

            Assert.That(session.Progress.PendingReward.Offer.Tier, Is.EqualTo(BattleRewardTier.HighGrade));
            Assert.That(
                session.Progress.PendingReward.CompletionTarget,
                Is.EqualTo(BattleRewardCompletionTarget.StageCleared));
            Assert.That(session.TrySkipBattleReward(), Is.True);
            Assert.That(session.Progress.State, Is.EqualTo(StageProgressionState.StageCleared));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EP06_I02_FinalBossProfileRewardEndsAndRestartsRun(bool selectReward)
        {
            StageDefinition bossStage = CreateProfileStage(
                EnemyCombatProfileCatalog.FinalBossKey,
                30,
                31);
            var session = new StageProgressionSession(
                new RunProgress(new[] { bossStage }, CreatePlayer()),
                CreateDeterministicVictoryBattle);

            Assert.That(session.TryStartRun(), Is.True);
            WinCurrentBattle(session);
            Assert.That(session.Progress.PendingReward.Offer.Tier, Is.EqualTo(BattleRewardTier.HighGrade));
            Assert.That(
                session.Progress.PendingReward.CompletionTarget,
                Is.EqualTo(BattleRewardCompletionTarget.RunVictory));

            bool resolved = selectReward
                ? session.TrySelectBattleReward(
                    session.Progress.PendingReward.Offer.Options[0].OptionId)
                : session.TrySkipBattleReward();
            Assert.That(resolved, Is.True);
            Assert.That(session.Progress.State, Is.EqualTo(StageProgressionState.RunVictory));
            Assert.That(session.TryRestartRun(), Is.True);
            Assert.That(session.Progress.State, Is.EqualTo(StageProgressionState.InBattle));
            Assert.That(session.Progress.CurrentStage.BattleProfileKey, Is.EqualTo(EnemyCombatProfileCatalog.FinalBossKey));
            Assert.That(session.Battle.Enemy.Soul.Current, Is.EqualTo(7));
            Assert.That(session.Progress.PendingReward, Is.Null);
        }

        private static StageDefinition CreateProfileStage(
            string profileKey,
            int playerDeckSeed,
            int enemyDeckSeed)
        {
            EnemyProfilePreview preview =
                EnemyCombatProfileCatalog.Default.GetPreviewByKey(profileKey);
            StageKind kind = preview.Grade == EnemyGrade.Boss
                ? StageKind.FinalBossCombat
                : StageKind.NormalCombat;
            return StageDefinition.CreateForEnemyProfile(
                $"stage-{profileKey}",
                preview.DisplayName,
                kind,
                profileKey,
                playerDeckSeed,
                enemyDeckSeed);
        }

        private static PlayerRunState CreatePlayer(int currentSoul = 12)
        {
            var cards = new List<RunCardDefinition>(20);
            for (int index = 0; index < 20; index++)
            {
                cards.Add(new RunCardDefinition(index, index % 10 + 1));
            }

            return new PlayerRunState(12, currentSoul, cards);
        }

        private static CoreLoopBattle CreateDeterministicVictoryBattle(
            StageDefinition stage,
            PlayerRunState player)
        {
            return new CoreLoopBattle(
                CreateRepeatedDeck(20, 10, 1),
                CreateRepeatedDeck(20, 10, 10),
                player.MaximumSoul,
                player.CurrentSoul,
                stage.EnemyMaximumSoul,
                new SimpleEnemyPolicy());
        }

        private static BlackjackDeck CreateRepeatedDeck(int cardCount, int firstRank, int secondRank)
        {
            var cards = new List<BlackjackCard>(cardCount);
            for (int index = 0; index < cardCount; index++)
            {
                int rank = index % 2 == 0 ? firstRank : secondRank;
                cards.Add(new BlackjackCard(index, rank));
            }

            return BlackjackDeck.CreateInDrawOrder(cards);
        }

        private static void WinCurrentBattle(StageProgressionSession session)
        {
            for (int action = 0;
                action < 12 && session.Progress.State == StageProgressionState.InBattle;
                action++)
            {
                Assert.That(session.TryPlayerStand(), Is.True, $"Stand action {action}");
            }

            Assert.That(session.Progress.State, Is.EqualTo(StageProgressionState.RewardSelection));
        }
    }
}
