using System.Collections.Generic;
using System.Linq;
using DiaBlackJack.CoreLoop;
using NUnit.Framework;

namespace DiaBlackJack.StageProgression.Tests
{
    public sealed class BattleRewardSessionTests
    {
        [Test]
        public void RW03_I01_NormalVictorySynchronizesSoulAndCreatesOneReward()
        {
            int createdBattleCount = 0;
            var session = new StageProgressionSession(
                CreateProgress(),
                (stage, player) =>
                {
                    createdBattleCount++;
                    return CreateTwoRoundVictoryBattle(player, stage.EnemyMaximumSoul);
                },
                CreateRewardGenerator(3001));

            Assert.That(session.TryStartRun(), Is.True);
            Assert.That(session.TryPlayerStand(), Is.True);
            Assert.That(session.Progress.State, Is.EqualTo(StageProgressionState.InBattle));
            Assert.That(session.Progress.Player.CurrentSoul, Is.EqualTo(12));

            Assert.That(session.TryPlayerStand(), Is.True);

            Assert.That(session.Progress.State, Is.EqualTo(StageProgressionState.RewardSelection));
            Assert.That(session.Progress.Player.CurrentSoul, Is.EqualTo(11));
            Assert.That(session.Progress.PendingReward, Is.Not.Null);
            Assert.That(
                session.Progress.PendingReward.Offer.Tier,
                Is.EqualTo(BattleRewardTier.Normal));
            Assert.That(session.Progress.PendingReward.Offer.OfferId, Is.Zero);
            Assert.That(
                session.Progress.PendingReward.CompletionTarget,
                Is.EqualTo(BattleRewardCompletionTarget.StageCleared));
            PendingBattleReward pending = session.Progress.PendingReward;

            Assert.That(session.TryPlayerStand(), Is.False);
            Assert.That(session.Progress.PendingReward, Is.SameAs(pending));
            Assert.That(createdBattleCount, Is.EqualTo(1));
        }

        [Test]
        public void RW03_I02_FinalBossUsesHighGradeRewardBeforeRunVictory()
        {
            var session = new StageProgressionSession(
                CreateFinalBossProgress(),
                (stage, player) =>
                    CreateImmediateVictoryBattle(player, stage.EnemyMaximumSoul),
                CreateRewardGenerator(3002));

            Assert.That(session.TryStartRun(), Is.True);
            Assert.That(session.TryPlayerStand(), Is.True);

            Assert.That(session.Progress.State, Is.EqualTo(StageProgressionState.RewardSelection));
            Assert.That(
                session.Progress.PendingReward.Offer.Tier,
                Is.EqualTo(BattleRewardTier.HighGrade));
            Assert.That(
                session.Progress.PendingReward.CompletionTarget,
                Is.EqualTo(BattleRewardCompletionTarget.RunVictory));
            Assert.That(session.TryAdvanceToNextStage(), Is.False);

            Assert.That(session.TrySkipBattleReward(), Is.True);
            Assert.That(session.Progress.State, Is.EqualTo(StageProgressionState.RunVictory));
        }

        [Test]
        public void RW03_I03_ExplicitEliteBoundaryUsesHighGradeReward()
        {
            var session = new StageProgressionSession(
                CreateProgress(),
                (stage, player) =>
                    CreateImmediateVictoryBattle(player, stage.EnemyMaximumSoul),
                CreateRewardGenerator(3003),
                stage => BattleRewardTier.HighGrade);

            Assert.That(session.TryStartRun(), Is.True);
            Assert.That(session.TryPlayerStand(), Is.True);

            Assert.That(session.Progress.CurrentStage.Kind, Is.EqualTo(StageKind.NormalCombat));
            Assert.That(session.Progress.State, Is.EqualTo(StageProgressionState.RewardSelection));
            Assert.That(
                session.Progress.PendingReward.Offer.Tier,
                Is.EqualTo(BattleRewardTier.HighGrade));
            Assert.That(
                session.Progress.PendingReward.CompletionTarget,
                Is.EqualTo(BattleRewardCompletionTarget.StageCleared));
        }

        [Test]
        public void RW03_I04_SelectedRewardIsPassedToNextBattleDeck()
        {
            var capturedDecks = new List<IReadOnlyList<RunCardDefinition>>();
            var session = new StageProgressionSession(
                CreateProgress(),
                (stage, player) =>
                {
                    capturedDecks.Add(player.Deck.ToArray());
                    return CreateImmediateVictoryBattle(player, stage.EnemyMaximumSoul);
                },
                CreateRewardGenerator(3004));

            Assert.That(session.TryStartRun(), Is.True);
            Assert.That(session.TryPlayerStand(), Is.True);
            BattleRewardOption selectedOption = session.Progress.PendingReward.Offer.Options[1];

            Assert.That(
                session.TrySelectBattleReward(selectedOption.OptionId),
                Is.True);
            int addedCardId = session.Progress.LastRewardResolution.AddedCardId.Value;
            Assert.That(session.TryAdvanceToNextStage(), Is.True);

            Assert.That(capturedDecks.Count, Is.EqualTo(2));
            Assert.That(capturedDecks[0].Count, Is.EqualTo(4));
            Assert.That(capturedDecks[1].Count, Is.EqualTo(5));
            Assert.That(
                capturedDecks[1].Single(card => card.Id == addedCardId).DefinitionKey,
                Is.EqualTo(selectedOption.DefinitionKey));
        }

        [Test]
        public void RW03_I05_SkippedRewardKeepsNextBattleDeckCount()
        {
            var capturedDeckCounts = new List<int>();
            var session = new StageProgressionSession(
                CreateProgress(),
                (stage, player) =>
                {
                    capturedDeckCounts.Add(player.Deck.Count);
                    return CreateImmediateVictoryBattle(player, stage.EnemyMaximumSoul);
                },
                CreateRewardGenerator(3005));

            Assert.That(session.TryStartRun(), Is.True);
            Assert.That(session.TryPlayerStand(), Is.True);
            Assert.That(session.TrySkipBattleReward(), Is.True);
            Assert.That(session.TryAdvanceToNextStage(), Is.True);

            Assert.That(capturedDeckCounts, Is.EqualTo(new[] { 4, 4 }));
        }

        [Test]
        public void RW03_I06_RestartRemovesRewardFromNewBattleDeck()
        {
            var capturedDecks = new List<IReadOnlyList<RunCardDefinition>>();
            var session = new StageProgressionSession(
                CreateFinalBossProgress(),
                (stage, player) =>
                {
                    capturedDecks.Add(player.Deck.ToArray());
                    return CreateImmediateVictoryBattle(player, stage.EnemyMaximumSoul);
                },
                CreateRewardGenerator(3006));

            Assert.That(session.TryStartRun(), Is.True);
            CoreLoopBattle completedBattle = session.Battle;
            Assert.That(session.TryPlayerStand(), Is.True);
            BattleRewardOption selectedOption = session.Progress.PendingReward.Offer.Options[0];
            Assert.That(
                session.TrySelectBattleReward(selectedOption.OptionId),
                Is.True);
            int addedCardId = session.Progress.LastRewardResolution.AddedCardId.Value;
            Assert.That(session.Progress.State, Is.EqualTo(StageProgressionState.RunVictory));
            Assert.That(session.Progress.Player.Deck.Count, Is.EqualTo(5));

            Assert.That(session.TryRestartRun(), Is.True);

            Assert.That(session.Progress.State, Is.EqualTo(StageProgressionState.InBattle));
            Assert.That(session.Battle, Is.Not.SameAs(completedBattle));
            Assert.That(capturedDecks.Count, Is.EqualTo(2));
            Assert.That(capturedDecks[1].Count, Is.EqualTo(4));
            Assert.That(capturedDecks[1].Any(card => card.Id == addedCardId), Is.False);
            Assert.That(session.Progress.LastRewardResolution, Is.Null);
        }

        private static BattleRewardGenerator CreateRewardGenerator(int seed)
        {
            return new BattleRewardGenerator(BattleRewardCatalog.CreateDefault(), seed);
        }

        private static RunProgress CreateProgress()
        {
            return new RunProgress(
                new[]
                {
                    new StageDefinition(
                        "normal-1",
                        "Normal 1",
                        StageKind.NormalCombat,
                        1,
                        10,
                        11),
                    new StageDefinition(
                        "normal-2",
                        "Normal 2",
                        StageKind.NormalCombat,
                        2,
                        20,
                        21),
                    new StageDefinition(
                        "final-boss",
                        "Final Boss",
                        StageKind.FinalBossCombat,
                        3,
                        30,
                        31)
                },
                CreatePlayer());
        }

        private static RunProgress CreateFinalBossProgress()
        {
            return new RunProgress(
                new[]
                {
                    new StageDefinition(
                        "final-boss",
                        "Final Boss",
                        StageKind.FinalBossCombat,
                        1,
                        30,
                        31)
                },
                CreatePlayer());
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

        private static CoreLoopBattle CreateTwoRoundVictoryBattle(
            PlayerRunState player,
            int enemyMaximumSoul)
        {
            return CreateBattle(
                player,
                enemyMaximumSoul,
                new[] { 10, 8, 10, 1 },
                new[] { 10, 10, 10, 10 });
        }

        private static CoreLoopBattle CreateImmediateVictoryBattle(
            PlayerRunState player,
            int enemyMaximumSoul)
        {
            return CreateBattle(
                player,
                enemyMaximumSoul,
                new[] { 10, 1 },
                new[] { 10, 10 });
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
            for (int i = 0; i < ranks.Count; i++)
            {
                cards.Add(new BlackjackCard(i, ranks[i]));
            }

            return BlackjackDeck.CreateInDrawOrder(cards);
        }
    }
}
