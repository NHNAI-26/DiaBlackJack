using DiaBlackJack.StageProgression.UI;
using NUnit.Framework;

namespace DiaBlackJack.StageProgression.Tests
{
    public sealed class StageProgressionPresentationTests
    {
        [Test]
        public void NotStartedShowsOnlyStartRun()
        {
            RunProgress progress = CreateProgress();

            StageProgressionViewModel model = StageProgressionPresenter.Create(progress);

            Assert.That(model.StageProgress, Is.EqualTo("STAGE 1 / 3"));
            Assert.That(model.StageName, Is.EqualTo("Normal 1"));
            Assert.That(model.StageKind, Is.EqualTo("NORMAL COMBAT"));
            Assert.That(model.PlayerSoul, Is.EqualTo("12 / 12"));
            Assert.That(model.Message, Is.EqualTo("READY TO START RUN"));
            Assert.That(model.CanStartRun, Is.True);
            Assert.That(model.CanAdvanceStage, Is.False);
            Assert.That(model.CanRestartRun, Is.False);
        }

        [Test]
        public void InBattleHidesProgressionInputs()
        {
            RunProgress progress = CreateProgress();
            progress.StartRun();

            StageProgressionViewModel model = StageProgressionPresenter.Create(progress);

            Assert.That(model.State, Is.EqualTo(StageProgressionState.InBattle));
            Assert.That(model.Message, Is.EqualTo("BATTLE IN PROGRESS"));
            Assert.That(model.CanStartRun, Is.False);
            Assert.That(model.CanAdvanceStage, Is.False);
            Assert.That(model.CanRestartRun, Is.False);
        }

        [Test]
        public void ClearedStageShowsOnlyNextStage()
        {
            RunProgress progress = CreateProgress();
            progress.StartRun();
            progress.Player.SetCurrentSoul(9);
            CompleteCurrentStageWithSkippedReward(progress);

            StageProgressionViewModel model = StageProgressionPresenter.Create(progress);

            Assert.That(model.Message, Is.EqualTo("STAGE CLEARED"));
            Assert.That(model.PlayerSoul, Is.EqualTo("9 / 12"));
            Assert.That(model.CanStartRun, Is.False);
            Assert.That(model.CanAdvanceStage, Is.True);
            Assert.That(model.CanRestartRun, Is.False);
        }

        [Test]
        public void FinalVictoryShowsBossAndOnlyRestartRun()
        {
            RunProgress progress = CreateProgress();
            progress.StartRun();
            CompleteAndAdvance(progress);
            CompleteAndAdvance(progress);
            CompleteCurrentStageWithSkippedReward(progress);

            StageProgressionViewModel model = StageProgressionPresenter.Create(progress);

            Assert.That(model.StageProgress, Is.EqualTo("STAGE 3 / 3"));
            Assert.That(model.StageKind, Is.EqualTo("FINAL BOSS"));
            Assert.That(model.Message, Is.EqualTo("RUN VICTORY"));
            Assert.That(model.CanStartRun, Is.False);
            Assert.That(model.CanAdvanceStage, Is.False);
            Assert.That(model.CanRestartRun, Is.True);
        }

        [Test]
        public void RunDefeatShowsOnlyRestartRun()
        {
            RunProgress progress = CreateProgress();
            progress.StartRun();
            progress.Player.SetCurrentSoul(0);
            progress.TryDefeatRun();

            StageProgressionViewModel model = StageProgressionPresenter.Create(progress);

            Assert.That(model.Message, Is.EqualTo("RUN DEFEAT"));
            Assert.That(model.CanStartRun, Is.False);
            Assert.That(model.CanAdvanceStage, Is.False);
            Assert.That(model.CanRestartRun, Is.True);
        }

        private static RunProgress CreateProgress()
        {
            return new RunProgress(
                new[]
                {
                    new StageDefinition("normal-1", "Normal 1", StageKind.NormalCombat, 3, 10, 11),
                    new StageDefinition("normal-2", "Normal 2", StageKind.NormalCombat, 4, 20, 21),
                    new StageDefinition("final-boss", "Final Boss", StageKind.FinalBossCombat, 7, 30, 31)
                },
                new PlayerRunState(
                    12,
                    12,
                    new[]
                    {
                        new RunCardDefinition(0, 1),
                        new RunCardDefinition(1, 10)
                    }));
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

            BattleRewardOffer offer = new BattleRewardGenerator(
                    BattleRewardCatalog.CreateDefault(),
                    2002)
                .Generate(tier);
            return progress.TryBeginBattleReward(offer, target) &&
                progress.TrySkipBattleReward();
        }
    }
}
