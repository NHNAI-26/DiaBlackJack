using NUnit.Framework;

namespace DiaBlackJack.StageProgression.Tests
{
    public sealed class BattleRewardStateTests
    {
        [Test]
        public void RW02_U01_NormalVictoryBeginsRewardWithoutClearingStage()
        {
            RunProgress progress = CreateStartedProgress();
            BattleRewardOffer offer = CreateOffer(BattleRewardTier.Normal, 1001);

            bool begun = progress.TryBeginBattleReward(
                offer,
                BattleRewardCompletionTarget.StageCleared);

            Assert.That(begun, Is.True);
            Assert.That(progress.State, Is.EqualTo(StageProgressionState.RewardSelection));
            Assert.That(progress.CurrentStageIndex, Is.Zero);
            Assert.That(progress.PendingReward, Is.Not.Null);
            Assert.That(progress.PendingReward.Offer, Is.SameAs(offer));
            Assert.That(
                progress.PendingReward.CompletionTarget,
                Is.EqualTo(BattleRewardCompletionTarget.StageCleared));
            Assert.That(progress.LastRewardResolution, Is.Null);
            Assert.That(progress.Player.Deck.Count, Is.EqualTo(2));
        }

        [Test]
        public void RW02_U02_FinalBossVictoryBeginsRewardWithoutWinningRun()
        {
            RunProgress progress = CreateStartedProgress();
            CompleteNormalRewardAndAdvance(progress);
            BattleRewardOffer offer = CreateOffer(BattleRewardTier.HighGrade, 1002);

            Assert.That(
                progress.TryBeginBattleReward(
                    CreateOffer(BattleRewardTier.Normal, 1009),
                    BattleRewardCompletionTarget.RunVictory),
                Is.False);
            bool begun = progress.TryBeginBattleReward(
                offer,
                BattleRewardCompletionTarget.RunVictory);

            Assert.That(begun, Is.True);
            Assert.That(progress.CurrentStage.Kind, Is.EqualTo(StageKind.FinalBossCombat));
            Assert.That(progress.State, Is.EqualTo(StageProgressionState.RewardSelection));
            Assert.That(progress.PendingReward.Offer, Is.SameAs(offer));
            Assert.That(
                progress.PendingReward.CompletionTarget,
                Is.EqualTo(BattleRewardCompletionTarget.RunVictory));
        }

        [Test]
        public void RW02_U03_SelectNormalRewardAddsOneCardAndClearsStage()
        {
            RunProgress progress = CreateStartedProgress();
            BattleRewardOffer offer = CreateOffer(BattleRewardTier.Normal, 1003);
            BattleRewardOption selectedOption = offer.Options[1];
            progress.TryBeginBattleReward(
                offer,
                BattleRewardCompletionTarget.StageCleared);

            bool selected = progress.TrySelectBattleReward(selectedOption.OptionId);

            Assert.That(selected, Is.True);
            Assert.That(progress.State, Is.EqualTo(StageProgressionState.StageCleared));
            Assert.That(progress.PendingReward, Is.Null);
            Assert.That(progress.Player.Deck.Count, Is.EqualTo(3));
            Assert.That(
                progress.Player.Deck[2].DefinitionKey,
                Is.EqualTo(selectedOption.DefinitionKey));
            Assert.That(progress.LastRewardResolution, Is.Not.Null);
            Assert.That(progress.LastRewardResolution.WasSkipped, Is.False);
            Assert.That(progress.LastRewardResolution.OfferId, Is.EqualTo(offer.OfferId));
            Assert.That(
                progress.LastRewardResolution.SelectedOptionId,
                Is.EqualTo(selectedOption.OptionId));
            Assert.That(
                progress.LastRewardResolution.SelectedDefinitionKey,
                Is.EqualTo(selectedOption.DefinitionKey));
            Assert.That(
                progress.LastRewardResolution.AddedCardId,
                Is.EqualTo(progress.Player.Deck[2].Id));
            Assert.That(
                progress.LastRewardResolution.CompletionTarget,
                Is.EqualTo(BattleRewardCompletionTarget.StageCleared));
        }

        [Test]
        public void RW02_U04_SkipBossRewardKeepsDeckAndWinsRun()
        {
            RunProgress progress = CreateStartedProgress();
            CompleteNormalRewardAndAdvance(progress);
            BattleRewardOffer offer = CreateOffer(BattleRewardTier.HighGrade, 1004);
            progress.TryBeginBattleReward(
                offer,
                BattleRewardCompletionTarget.RunVictory);
            int deckCount = progress.Player.Deck.Count;

            bool skipped = progress.TrySkipBattleReward();

            Assert.That(skipped, Is.True);
            Assert.That(progress.State, Is.EqualTo(StageProgressionState.RunVictory));
            Assert.That(progress.PendingReward, Is.Null);
            Assert.That(progress.Player.Deck.Count, Is.EqualTo(deckCount));
            Assert.That(progress.LastRewardResolution.WasSkipped, Is.True);
            Assert.That(progress.LastRewardResolution.OfferId, Is.EqualTo(offer.OfferId));
            Assert.That(progress.LastRewardResolution.SelectedOptionId, Is.Null);
            Assert.That(progress.LastRewardResolution.SelectedDefinitionKey, Is.Null);
            Assert.That(progress.LastRewardResolution.AddedCardId, Is.Null);
            Assert.That(
                progress.LastRewardResolution.CompletionTarget,
                Is.EqualTo(BattleRewardCompletionTarget.RunVictory));

            Assert.That(progress.TryRestartRun(), Is.True);
            Assert.That(progress.State, Is.EqualTo(StageProgressionState.InBattle));
            Assert.That(progress.CurrentStageIndex, Is.Zero);
            Assert.That(progress.PendingReward, Is.Null);
            Assert.That(progress.LastRewardResolution, Is.Null);
            Assert.That(progress.Player.Deck.Count, Is.EqualTo(2));
        }

        [Test]
        public void RW02_U05_DefeatDoesNotCreateReward()
        {
            RunProgress progress = CreateStartedProgress();
            progress.Player.SetCurrentSoul(0);

            bool begun = progress.TryBeginBattleReward(
                CreateOffer(BattleRewardTier.Normal, 1005),
                BattleRewardCompletionTarget.StageCleared);
            bool defeated = progress.TryDefeatRun();

            Assert.That(begun, Is.False);
            Assert.That(defeated, Is.True);
            Assert.That(progress.State, Is.EqualTo(StageProgressionState.RunDefeat));
            Assert.That(progress.PendingReward, Is.Null);
            Assert.That(progress.LastRewardResolution, Is.Null);
            Assert.That(progress.Player.Deck.Count, Is.EqualTo(2));
        }

        [Test]
        public void RW02_U06_InvalidAndRepeatedInputsAreAtomic()
        {
            RunProgress progress = CreateStartedProgress();
            BattleRewardOffer offer = CreateOffer(BattleRewardTier.Normal, 1006);
            int initialDeckCount = progress.Player.Deck.Count;

            Assert.That(
                progress.TryBeginBattleReward(
                    offer,
                    BattleRewardCompletionTarget.RunVictory),
                Is.False);
            Assert.That(
                progress.TryBeginBattleReward(
                    offer,
                    (BattleRewardCompletionTarget)999),
                Is.False);
            Assert.That(progress.State, Is.EqualTo(StageProgressionState.InBattle));
            Assert.That(progress.PendingReward, Is.Null);
            Assert.That(progress.Player.Deck.Count, Is.EqualTo(initialDeckCount));

            Assert.That(
                progress.TryBeginBattleReward(
                    offer,
                    BattleRewardCompletionTarget.StageCleared),
                Is.True);
            PendingBattleReward pending = progress.PendingReward;
            Assert.That(progress.TrySelectBattleReward(99), Is.False);
            Assert.That(progress.State, Is.EqualTo(StageProgressionState.RewardSelection));
            Assert.That(progress.PendingReward, Is.SameAs(pending));
            Assert.That(progress.LastRewardResolution, Is.Null);
            Assert.That(progress.Player.Deck.Count, Is.EqualTo(initialDeckCount));

            Assert.That(progress.TrySelectBattleReward(0), Is.True);
            int resolvedDeckCount = progress.Player.Deck.Count;
            BattleRewardResolution resolution = progress.LastRewardResolution;
            Assert.That(progress.TrySelectBattleReward(0), Is.False);
            Assert.That(progress.TrySkipBattleReward(), Is.False);
            Assert.That(progress.Player.Deck.Count, Is.EqualTo(resolvedDeckCount));
            Assert.That(progress.LastRewardResolution, Is.SameAs(resolution));
        }

        [Test]
        public void RW02_U07_RewardSelectionLocksOtherProgressActions()
        {
            RunProgress progress = CreateStartedProgress();
            BattleRewardOffer offer = CreateOffer(BattleRewardTier.Normal, 1007);
            progress.TryBeginBattleReward(
                offer,
                BattleRewardCompletionTarget.StageCleared);

            Assert.That(
                progress.TryBeginBattleReward(
                    CreateOffer(BattleRewardTier.Normal, 1008),
                    BattleRewardCompletionTarget.StageCleared),
                Is.False);
            Assert.That(progress.TryAdvanceToNextStage(), Is.False);
            Assert.That(progress.TryDefeatRun(), Is.False);
            Assert.That(progress.TryRestartRun(), Is.False);
            Assert.That(progress.TryCompleteCurrentStageWithoutReward(), Is.False);
            Assert.That(progress.State, Is.EqualTo(StageProgressionState.RewardSelection));
            Assert.That(progress.PendingReward.Offer, Is.SameAs(offer));
        }

        private static RunProgress CreateStartedProgress()
        {
            var progress = new RunProgress(
                new[]
                {
                    new StageDefinition(
                        "normal-1",
                        "Normal 1",
                        StageKind.NormalCombat,
                        3,
                        10,
                        11),
                    new StageDefinition(
                        "final-boss",
                        "Final Boss",
                        StageKind.FinalBossCombat,
                        7,
                        20,
                        21)
                },
                new PlayerRunState(
                    12,
                    12,
                    new[]
                    {
                        new RunCardDefinition(0, 1),
                        new RunCardDefinition(1, 10)
                    }));
            Assert.That(progress.StartRun(), Is.True);
            return progress;
        }

        private static BattleRewardOffer CreateOffer(BattleRewardTier tier, int seed)
        {
            return new BattleRewardGenerator(BattleRewardCatalog.CreateDefault(), seed)
                .Generate(tier);
        }

        private static void CompleteNormalRewardAndAdvance(RunProgress progress)
        {
            Assert.That(
                progress.TryBeginBattleReward(
                    CreateOffer(BattleRewardTier.Normal, 2000 + progress.CurrentStageIndex),
                    BattleRewardCompletionTarget.StageCleared),
                Is.True);
            Assert.That(progress.TrySkipBattleReward(), Is.True);
            Assert.That(progress.TryAdvanceToNextStage(), Is.True);
        }
    }
}
