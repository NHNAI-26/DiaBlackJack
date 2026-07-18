using System.Collections.Generic;
using DiaBlackJack.CoreLoop.UI;
using NUnit.Framework;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class CoreLoopPresentationTests
    {
        [Test]
        public void CL_F04_EnemySoulZeroShowsVictoryAndRestart()
        {
            var session = new CoreLoopSession(CreatePlayerVictoryBattle);
            session.TryPlayerStand();

            CoreLoopViewModel model = CoreLoopPresenter.Create(session.Battle);

            Assert.That(model.Outcome, Is.EqualTo(BattleOutcome.PlayerVictory));
            Assert.That(model.CanHit, Is.False);
            Assert.That(model.CanStand, Is.False);
            Assert.That(model.CanRestart, Is.True);
            Assert.That(model.EnemySoul, Is.EqualTo("0 / 1"));
        }

        [Test]
        public void CL_F05_PlayerSoulZeroShowsDefeatAndRestart()
        {
            var session = new CoreLoopSession(CreatePlayerDefeatBattle);
            session.TryPlayerStand();

            CoreLoopViewModel model = CoreLoopPresenter.Create(session.Battle);

            Assert.That(model.Outcome, Is.EqualTo(BattleOutcome.PlayerDefeat));
            Assert.That(model.CanHit, Is.False);
            Assert.That(model.CanStand, Is.False);
            Assert.That(model.CanRestart, Is.True);
            Assert.That(model.PlayerSoul, Is.EqualTo("0 / 1"));
        }

        [Test]
        public void CL_F06_TenRestartsAlwaysCreateCleanInitialState()
        {
            int createdBattleCount = 0;
            var session = new CoreLoopSession(() =>
            {
                createdBattleCount++;
                return CreatePlayerVictoryBattle();
            });

            for (int i = 0; i < 10; i++)
            {
                Assert.That(session.TryPlayerStand(), Is.True, $"End battle {i}");
                Assert.That(session.Battle.State, Is.EqualTo(CoreLoopState.BattleEnded), $"Ended {i}");
                Assert.That(session.TryRestart(), Is.True, $"Restart {i}");
                AssertInitialState(session.Battle, i);
            }

            Assert.That(createdBattleCount, Is.EqualTo(11));
        }

        [Test]
        public void PresentationHidesEnemyPrivateCardWithoutHidingPlayerCard()
        {
            var session = new CoreLoopSession(CreatePlayerVictoryBattle);

            CoreLoopViewModel model = CoreLoopPresenter.Create(session.Battle);

            Assert.That(model.PlayerCards, Is.EqualTo("10  1"));
            Assert.That(model.EnemyCards, Is.EqualTo("10  ?"));
            Assert.That(model.PlayerTotal, Is.EqualTo(21));
            Assert.That(model.EnemyVisibleTotal, Is.EqualTo(10));
        }

        private static void AssertInitialState(CoreLoopBattle battle, int iteration)
        {
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn), $"State {iteration}");
            Assert.That(battle.RoundNumber, Is.EqualTo(1), $"Round {iteration}");
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(12), $"Player soul {iteration}");
            Assert.That(battle.Enemy.Soul.Current, Is.EqualTo(1), $"Enemy soul {iteration}");
            Assert.That(battle.Player.Hand.Count, Is.EqualTo(2), $"Player hand {iteration}");
            Assert.That(battle.Enemy.Hand.Count, Is.EqualTo(2), $"Enemy hand {iteration}");
            Assert.That(battle.LastResolution.HasValue, Is.False, $"Last result {iteration}");
        }

        private static CoreLoopBattle CreatePlayerVictoryBattle()
        {
            return CreateBattle(
                playerRanks: new[] { 10, 1 },
                enemyRanks: new[] { 10, 10 },
                playerMaximumSoul: 12,
                enemyMaximumSoul: 1);
        }

        private static CoreLoopBattle CreatePlayerDefeatBattle()
        {
            return CreateBattle(
                playerRanks: new[] { 10, 8 },
                enemyRanks: new[] { 10, 10 },
                playerMaximumSoul: 1,
                enemyMaximumSoul: 3);
        }

        private static CoreLoopBattle CreateBattle(
            IReadOnlyList<int> playerRanks,
            IReadOnlyList<int> enemyRanks,
            int playerMaximumSoul,
            int enemyMaximumSoul)
        {
            return new CoreLoopBattle(
                CreateDeck(playerRanks),
                CreateDeck(enemyRanks),
                playerMaximumSoul,
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
