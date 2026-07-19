using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace DiaBlackJack.StageProgression.Tests
{
    public sealed class StageProgressionStateTests
    {
        [Test]
        public void SP_U01_StartRunCreatesFirstBattleState()
        {
            RunProgress progress = CreateProgress();

            bool started = progress.StartRun();

            Assert.That(started, Is.True);
            Assert.That(progress.State, Is.EqualTo(StageProgressionState.InBattle));
            Assert.That(progress.CurrentStageIndex, Is.Zero);
            Assert.That(progress.CurrentStage.Id, Is.EqualTo("normal-1"));
            Assert.That(progress.Player.CurrentSoul, Is.EqualTo(12));
        }

        [Test]
        public void SP_U02_ProgressActionsAreRejectedBeforeRunStarts()
        {
            RunProgress progress = CreateProgress();

            Assert.That(
                progress.TryBeginBattleReward(
                    CreateOffer(BattleRewardTier.Normal),
                    BattleRewardCompletionTarget.StageCleared),
                Is.False);
            Assert.That(progress.TryAdvanceToNextStage(), Is.False);
            Assert.That(progress.TryDefeatRun(), Is.False);
            Assert.That(progress.TryRestartRun(), Is.False);
            Assert.That(progress.State, Is.EqualTo(StageProgressionState.NotStarted));
            Assert.That(progress.CurrentStageIndex, Is.Zero);
        }

        [Test]
        public void SP_U03_CompletingNormalStageWaitsWithoutAdvancingIndex()
        {
            RunProgress progress = CreateStartedProgress();

            bool completed = CompleteCurrentStageWithSkippedReward(progress);

            Assert.That(completed, Is.True);
            Assert.That(progress.State, Is.EqualTo(StageProgressionState.StageCleared));
            Assert.That(progress.CurrentStageIndex, Is.Zero);
        }

        [Test]
        public void SP_U04_AdvancingMovesExactlyOneStage()
        {
            RunProgress progress = CreateStartedProgress();
            CompleteCurrentStageWithSkippedReward(progress);

            bool advanced = progress.TryAdvanceToNextStage();

            Assert.That(advanced, Is.True);
            Assert.That(progress.State, Is.EqualTo(StageProgressionState.InBattle));
            Assert.That(progress.CurrentStageIndex, Is.EqualTo(1));
            Assert.That(progress.CurrentStage.Id, Is.EqualTo("normal-2"));
        }

        [Test]
        public void SP_U08_CompletedStageCannotBeProcessedTwice()
        {
            RunProgress progress = CreateStartedProgress();

            bool first = CompleteCurrentStageWithSkippedReward(progress);
            bool second = progress.TryBeginBattleReward(
                CreateOffer(BattleRewardTier.Normal),
                BattleRewardCompletionTarget.StageCleared);

            Assert.That(first, Is.True);
            Assert.That(second, Is.False);
            Assert.That(progress.State, Is.EqualTo(StageProgressionState.StageCleared));
            Assert.That(progress.CurrentStageIndex, Is.Zero);
        }

        [Test]
        public void SP_U09_InvalidTransitionsDoNotChangeProgress()
        {
            RunProgress progress = CreateStartedProgress();

            Assert.That(progress.StartRun(), Is.False);
            Assert.That(progress.TryAdvanceToNextStage(), Is.False);
            Assert.That(progress.TryRestartRun(), Is.False);
            Assert.That(progress.State, Is.EqualTo(StageProgressionState.InBattle));
            Assert.That(progress.CurrentStageIndex, Is.Zero);
        }

        [Test]
        public void SP_U10_RestartResetsStageSoulAndResult()
        {
            RunProgress progress = CreateStartedProgress();
            progress.Player.SetCurrentSoul(0);
            progress.TryDefeatRun();

            bool restarted = progress.TryRestartRun();

            Assert.That(restarted, Is.True);
            Assert.That(progress.State, Is.EqualTo(StageProgressionState.InBattle));
            Assert.That(progress.CurrentStageIndex, Is.Zero);
            Assert.That(progress.Player.CurrentSoul, Is.EqualTo(12));
        }

        [Test]
        public void SP_U11_InvalidRoutesAreRejected()
        {
            PlayerRunState player = CreatePlayer();

            Assert.Throws<ArgumentException>(() => new RunProgress(Array.Empty<StageDefinition>(), player));
            Assert.Throws<ArgumentException>(() => new RunProgress(
                new[] { new StageDefinition("normal", "Normal", StageKind.NormalCombat, 3, 1, 2) },
                player));
            Assert.Throws<ArgumentException>(() => new RunProgress(
                new[]
                {
                    new StageDefinition("boss", "Boss", StageKind.FinalBossCombat, 7, 1, 2),
                    new StageDefinition("normal", "Normal", StageKind.NormalCombat, 3, 3, 4)
                },
                player));
            Assert.Throws<ArgumentException>(() => new RunProgress(
                new[]
                {
                    new StageDefinition("same", "Normal", StageKind.NormalCombat, 3, 1, 2),
                    new StageDefinition("same", "Boss", StageKind.FinalBossCombat, 7, 3, 4)
                },
                player));
        }

        [Test]
        public void FinalStageCompletionEndsRunWithVictory()
        {
            RunProgress progress = CreateStartedProgress();
            CompleteAndAdvance(progress);
            CompleteAndAdvance(progress);

            bool completed = CompleteCurrentStageWithSkippedReward(progress);

            Assert.That(completed, Is.True);
            Assert.That(progress.CurrentStage.Kind, Is.EqualTo(StageKind.FinalBossCombat));
            Assert.That(progress.State, Is.EqualTo(StageProgressionState.RunVictory));
            Assert.That(progress.TryAdvanceToNextStage(), Is.False);
        }

        [Test]
        public void RunDefeatRequiresDepletedPlayerSoul()
        {
            RunProgress progress = CreateStartedProgress();

            Assert.That(progress.TryDefeatRun(), Is.False);
            progress.Player.SetCurrentSoul(0);
            Assert.That(progress.TryDefeatRun(), Is.True);
            Assert.That(progress.State, Is.EqualTo(StageProgressionState.RunDefeat));
        }

        [Test]
        public void PlayerRunStateCopiesDeckComposition()
        {
            var source = new List<RunCardDefinition>
            {
                new RunCardDefinition(0, 1),
                new RunCardDefinition(1, 10)
            };
            var player = new PlayerRunState(12, 8, source);

            source.Clear();

            Assert.That(player.Deck.Count, Is.EqualTo(2));
            Assert.That(player.Deck[0].Id, Is.Zero);
            Assert.That(player.Deck[0].DefinitionKey, Is.EqualTo("standard-ace-1"));
            Assert.That(player.Deck[1].Rank, Is.EqualTo(10));
            Assert.That(player.Deck[1].DefinitionKey, Is.EqualTo("military-knife-10"));
            Assert.That(player.CurrentSoul, Is.EqualTo(8));
        }

        [Test]
        public void CU_U08_RunCardDefinitionSupportsLegacyRankAndStableKey()
        {
            var legacyCard = new RunCardDefinition(3, 5);
            var keyedCard = new RunCardDefinition(4, "auto-pistol-8");

            Assert.That(legacyCard.DefinitionKey, Is.EqualTo("crystal-orb-5"));
            Assert.That(legacyCard.Rank, Is.EqualTo(5));
            Assert.That(keyedCard.DefinitionKey, Is.EqualTo("auto-pistol-8"));
            Assert.That(keyedCard.Rank, Is.EqualTo(8));
        }

        [Test]
        public void CU_U09_RunCardDefinitionRejectsUnknownKey()
        {
            Assert.Throws<KeyNotFoundException>(() =>
                new RunCardDefinition(0, "missing-card"));
        }

        [Test]
        public void PlayerRunStateRejectsInvalidSoulAndDuplicateCards()
        {
            var validDeck = new[] { new RunCardDefinition(0, 1) };
            var duplicateDeck = new[]
            {
                new RunCardDefinition(0, 1),
                new RunCardDefinition(0, 2)
            };

            Assert.Throws<ArgumentOutOfRangeException>(() => new PlayerRunState(0, 0, validDeck));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PlayerRunState(12, 13, validDeck));
            Assert.Throws<ArgumentException>(() => new PlayerRunState(12, 12, duplicateDeck));
        }

        [Test]
        public void StageDefinitionRejectsInvalidIdentityAndSoul()
        {
            Assert.Throws<ArgumentException>(() =>
                new StageDefinition(" ", "Normal", StageKind.NormalCombat, 3, 1, 2));
            Assert.Throws<ArgumentException>(() =>
                new StageDefinition("normal", " ", StageKind.NormalCombat, 3, 1, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new StageDefinition("normal", "Normal", StageKind.NormalCombat, 0, 1, 2));
        }

        private static RunProgress CreateStartedProgress()
        {
            RunProgress progress = CreateProgress();
            progress.StartRun();
            return progress;
        }

        private static RunProgress CreateProgress()
        {
            return new RunProgress(CreateStages(), CreatePlayer());
        }

        private static PlayerRunState CreatePlayer()
        {
            return new PlayerRunState(
                12,
                12,
                new[]
                {
                    new RunCardDefinition(0, 1),
                    new RunCardDefinition(1, 10)
                });
        }

        private static IReadOnlyList<StageDefinition> CreateStages()
        {
            return new[]
            {
                new StageDefinition("normal-1", "Normal 1", StageKind.NormalCombat, 3, 10, 11),
                new StageDefinition("normal-2", "Normal 2", StageKind.NormalCombat, 4, 20, 21),
                new StageDefinition("final-boss", "Final Boss", StageKind.FinalBossCombat, 7, 30, 31)
            };
        }

        private static void CompleteAndAdvance(RunProgress progress)
        {
            Assert.That(CompleteCurrentStageWithSkippedReward(progress), Is.True);
            Assert.That(progress.TryAdvanceToNextStage(), Is.True);
        }

        private static bool CompleteCurrentStageWithSkippedReward(RunProgress progress)
        {
            bool isFinalBoss = progress.CurrentStage.Kind == StageKind.FinalBossCombat;
            BattleRewardTier tier = isFinalBoss
                ? BattleRewardTier.HighGrade
                : BattleRewardTier.Normal;
            BattleRewardCompletionTarget target = isFinalBoss
                ? BattleRewardCompletionTarget.RunVictory
                : BattleRewardCompletionTarget.StageCleared;

            if (!progress.TryBeginBattleReward(CreateOffer(tier), target))
            {
                return false;
            }

            return progress.TrySkipBattleReward();
        }

        private static BattleRewardOffer CreateOffer(BattleRewardTier tier)
        {
            return new BattleRewardGenerator(BattleRewardCatalog.CreateDefault(), 2001)
                .Generate(tier);
        }
    }
}
