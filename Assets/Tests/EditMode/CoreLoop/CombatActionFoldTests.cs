using System.Collections.Generic;
using DiaBlackJack.CoreLoop.UI;
using NUnit.Framework;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class CombatActionFoldTests
    {
        [Test]
        public void BA02_FoldResolutionDamagesPlayerExactlyOnce()
        {
            RoundResolution resolution = RoundResolver.ResolvePlayerFold(resolutionId: 7);
            var playerSoul = new SoulPool(12);
            var enemySoul = new SoulPool(3);
            var applier = new RoundDamageApplier();

            bool firstApply = applier.TryApply(resolution, playerSoul, enemySoul);
            bool secondApply = applier.TryApply(resolution, playerSoul, enemySoul);

            Assert.That(resolution.Outcome, Is.EqualTo(RoundOutcome.PlayerFold));
            Assert.That(resolution.PlayerDamage, Is.EqualTo(1));
            Assert.That(resolution.EnemyDamage, Is.Zero);
            Assert.That(firstApply, Is.True);
            Assert.That(secondApply, Is.False);
            Assert.That(playerSoul.Current, Is.EqualTo(11));
            Assert.That(enemySoul.Current, Is.EqualTo(3));
        }

        [Test]
        public void BA02_PlayerFoldSkipsEnemyTurnAndStartsNextRound()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 10, 1, 4, 5 },
                enemyRanks: new[] { 5, 5, 10, 9 });
            battle.Start();

            bool accepted = battle.TryPlayerFold();

            Assert.That(accepted, Is.True);
            Assert.That(battle.LastResolution.HasValue, Is.True);
            Assert.That(battle.LastResolution.Value.Outcome, Is.EqualTo(RoundOutcome.PlayerFold));
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(11));
            Assert.That(battle.Enemy.Soul.Current, Is.EqualTo(3));
            Assert.That(battle.RoundNumber, Is.EqualTo(2));
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
            Assert.That(battle.Player.Hand.Count, Is.EqualTo(2));
            Assert.That(battle.Enemy.Hand.Count, Is.EqualTo(2));
            Assert.That(battle.Player.Deck.DiscardCount, Is.EqualTo(2));
            Assert.That(battle.Enemy.Deck.DiscardCount, Is.EqualTo(2));
        }

        [Test]
        public void BA02_PlayerAtOneSoulFoldsIntoBattleDefeatWithoutNewRound()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 10, 1 },
                enemyRanks: new[] { 5, 5 },
                playerCurrentSoul: 1);
            battle.Start();

            bool accepted = battle.TryPlayerFold();
            bool acceptedAgain = battle.TryPlayerFold();

            Assert.That(accepted, Is.True);
            Assert.That(acceptedAgain, Is.False);
            Assert.That(battle.LastResolution.Value.Outcome, Is.EqualTo(RoundOutcome.PlayerFold));
            Assert.That(battle.Player.Soul.Current, Is.Zero);
            Assert.That(battle.Enemy.Soul.Current, Is.EqualTo(3));
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.BattleEnded));
            Assert.That(battle.Outcome, Is.EqualTo(BattleOutcome.PlayerDefeat));
            Assert.That(battle.RoundNumber, Is.EqualTo(1));
            Assert.That(battle.Player.Hand.Count, Is.Zero);
            Assert.That(battle.Enemy.Hand.Count, Is.Zero);
            Assert.That(battle.Player.Deck.DiscardCount, Is.EqualTo(2));
            Assert.That(battle.Enemy.Deck.DiscardCount, Is.EqualTo(2));
        }

        [Test]
        public void BA02_FoldBeforeBattleStartIsRejectedWithoutMutation()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 10, 1 },
                enemyRanks: new[] { 5, 5 });

            bool accepted = battle.TryPlayerFold();

            Assert.That(accepted, Is.False);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.Initializing));
            Assert.That(battle.LastResolution.HasValue, Is.False);
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(12));
            Assert.That(battle.Enemy.Soul.Current, Is.EqualTo(3));
            Assert.That(battle.Player.Hand.Count, Is.Zero);
            Assert.That(battle.Enemy.Hand.Count, Is.Zero);
            Assert.That(battle.Player.Deck.DrawCount, Is.EqualTo(2));
            Assert.That(battle.Enemy.Deck.DrawCount, Is.EqualTo(2));
        }

        [Test]
        public void BA02_CoreLoopSessionForwardsPlayerFold()
        {
            var session = new CoreLoopSession(() => CreateBattle(
                playerRanks: new[] { 10, 1 },
                enemyRanks: new[] { 5, 5 },
                playerCurrentSoul: 1));

            bool accepted = session.TryPlayerFold();

            Assert.That(accepted, Is.True);
            Assert.That(session.Battle.State, Is.EqualTo(CoreLoopState.BattleEnded));
            Assert.That(session.Battle.Outcome, Is.EqualTo(BattleOutcome.PlayerDefeat));
            Assert.That(session.Battle.Player.Soul.Current, Is.Zero);
        }

        [Test]
        public void BA02_FoldResultRemainsPresentableBeforeUiInputIsAdded()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 10, 1, 4, 5 },
                enemyRanks: new[] { 5, 5, 10, 9 });
            battle.Start();
            battle.TryPlayerFold();

            CoreLoopViewModel model = CoreLoopPresenter.Create(battle);

            Assert.That(model.LastRound, Does.Contain("fold"));
            Assert.That(model.LastRound, Does.Contain("-1"));
        }

        private static CoreLoopBattle CreateBattle(
            IReadOnlyList<int> playerRanks,
            IReadOnlyList<int> enemyRanks,
            int playerCurrentSoul = 12)
        {
            return new CoreLoopBattle(
                CreateDeck(playerRanks),
                CreateDeck(enemyRanks),
                playerMaximumSoul: 12,
                playerCurrentSoul: playerCurrentSoul,
                enemyMaximumSoul: 3);
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
