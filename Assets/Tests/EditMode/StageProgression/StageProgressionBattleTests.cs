using System.Collections.Generic;
using System.Linq;
using DiaBlackJack.CoreLoop;
using NUnit.Framework;

namespace DiaBlackJack.StageProgression.Tests
{
    public sealed class StageProgressionBattleTests
    {
        [Test]
        public void BattleActionsAreRejectedBeforeRunStarts()
        {
            var session = new StageProgressionSession(CreateProgress());

            Assert.That(session.TryPlayerHit(), Is.False);
            Assert.That(session.TryPlayerStand(), Is.False);
            Assert.That(session.TryAdvanceToNextStage(), Is.False);
            Assert.That(session.TryRestartRun(), Is.False);
            Assert.That(session.Battle, Is.Null);
            Assert.That(session.Progress.State, Is.EqualTo(StageProgressionState.NotStarted));
        }

        [Test]
        public void SP_U05_NextBattleKeepsPlayerSoulAndResetsEnemy()
        {
            int createdBattleCount = 0;
            var session = new StageProgressionSession(
                CreateProgress(),
                (stage, player) =>
                {
                    createdBattleCount++;
                    return stage.Id == "normal-1"
                        ? CreateTwoRoundVictoryBattle(player, stage.EnemyMaximumSoul)
                        : CreateImmediateVictoryBattle(player, stage.EnemyMaximumSoul);
                });

            Assert.That(session.TryStartRun(), Is.True);
            CoreLoopBattle firstBattle = session.Battle;
            Assert.That(session.TryPlayerStand(), Is.True);
            Assert.That(firstBattle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
            Assert.That(firstBattle.Player.Soul.Current, Is.EqualTo(11));
            Assert.That(session.Progress.Player.CurrentSoul, Is.EqualTo(12));

            Assert.That(session.TryPlayerStand(), Is.True);
            Assert.That(session.Progress.State, Is.EqualTo(StageProgressionState.StageCleared));
            Assert.That(session.Progress.Player.CurrentSoul, Is.EqualTo(11));

            Assert.That(session.TryAdvanceToNextStage(), Is.True);
            Assert.That(session.Progress.CurrentStageIndex, Is.EqualTo(1));
            Assert.That(session.Battle, Is.Not.SameAs(firstBattle));
            Assert.That(session.Battle.Player.Soul.Current, Is.EqualTo(11));
            Assert.That(session.Battle.Player.Soul.Maximum, Is.EqualTo(12));
            Assert.That(session.Battle.Enemy.Soul.Current, Is.EqualTo(2));
            Assert.That(session.Battle.RoundNumber, Is.EqualTo(1));
            Assert.That(createdBattleCount, Is.EqualTo(2));
        }

        [Test]
        public void SP_U06_PlayerDefeatEndsRunWithoutCreatingNextBattle()
        {
            int createdBattleCount = 0;
            var session = new StageProgressionSession(
                CreateProgress(playerMaximumSoul: 1),
                (stage, player) =>
                {
                    createdBattleCount++;
                    return CreateImmediateDefeatBattle(player, stage.EnemyMaximumSoul);
                });

            session.TryStartRun();
            CoreLoopBattle defeatedBattle = session.Battle;

            Assert.That(session.TryPlayerStand(), Is.True);
            Assert.That(session.Progress.State, Is.EqualTo(StageProgressionState.RunDefeat));
            Assert.That(session.Progress.CurrentStageIndex, Is.Zero);
            Assert.That(session.Progress.Player.CurrentSoul, Is.Zero);
            Assert.That(session.Battle, Is.SameAs(defeatedBattle));
            Assert.That(session.TryAdvanceToNextStage(), Is.False);
            Assert.That(session.TryPlayerStand(), Is.False);
            Assert.That(createdBattleCount, Is.EqualTo(1));
        }

        [Test]
        public void SP_U07_FinalBossVictoryEndsRunWithoutCreatingNextBattle()
        {
            int createdBattleCount = 0;
            var progress = new RunProgress(
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
            var session = new StageProgressionSession(
                progress,
                (stage, player) =>
                {
                    createdBattleCount++;
                    return CreateImmediateVictoryBattle(player, stage.EnemyMaximumSoul);
                });

            session.TryStartRun();
            Assert.That(session.TryPlayerStand(), Is.True);

            Assert.That(session.Progress.State, Is.EqualTo(StageProgressionState.RunVictory));
            Assert.That(session.Progress.CurrentStageIndex, Is.Zero);
            Assert.That(session.TryAdvanceToNextStage(), Is.False);
            Assert.That(createdBattleCount, Is.EqualTo(1));
        }

        [Test]
        public void SP_U12_BattleFactoryCopiesRunDeckAndCurrentSoul()
        {
            var player = new PlayerRunState(
                12,
                8,
                new[]
                {
                    new RunCardDefinition(7, 3),
                    new RunCardDefinition(9, 10)
                });
            var stage = new StageDefinition(
                "normal",
                "Normal",
                StageKind.NormalCombat,
                4,
                10,
                11);

            CoreLoopBattle battle = StageBattleFactory.Create(stage, player);
            Assert.That(battle.Start(), Is.True);

            Assert.That(battle.Player.Soul.Maximum, Is.EqualTo(12));
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(8));
            Assert.That(battle.Player.Deck.TotalCardCount, Is.EqualTo(2));
            Assert.That(battle.Player.Hand.Cards.Select(card => card.Id), Is.EquivalentTo(new[] { 7, 9 }));
            Assert.That(battle.Player.Hand.Cards.Select(card => card.Rank), Is.EquivalentTo(new[] { 3, 10 }));
            Assert.That(battle.Enemy.Soul.Current, Is.EqualTo(4));
            Assert.That(battle.Enemy.Deck.TotalCardCount, Is.EqualTo(20));
        }

        private static RunProgress CreateProgress(int playerMaximumSoul = 12)
        {
            return new RunProgress(
                new[]
                {
                    new StageDefinition("normal-1", "Normal 1", StageKind.NormalCombat, 1, 10, 11),
                    new StageDefinition("normal-2", "Normal 2", StageKind.NormalCombat, 2, 20, 21),
                    new StageDefinition("final-boss", "Final Boss", StageKind.FinalBossCombat, 3, 30, 31)
                },
                CreatePlayer(playerMaximumSoul));
        }

        private static PlayerRunState CreatePlayer(int maximumSoul = 12)
        {
            return new PlayerRunState(
                maximumSoul,
                maximumSoul,
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

        private static CoreLoopBattle CreateImmediateDefeatBattle(
            PlayerRunState player,
            int enemyMaximumSoul)
        {
            return CreateBattle(
                player,
                enemyMaximumSoul,
                new[] { 10, 8 },
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
