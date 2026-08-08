using System.Collections.Generic;
using NUnit.Framework;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class CoreLoopFlowTests
    {
        [Test]
        public void CL_U06_EnemyHitsAtSixteen()
        {
            var policy = new SimpleEnemyPolicy();

            EnemyAction action = policy.Decide(new HandValue(16));

            Assert.That(action, Is.EqualTo(EnemyAction.Hit));
        }

        [Test]
        public void CL_U07_EnemyStandsAtSeventeen()
        {
            var policy = new SimpleEnemyPolicy();

            EnemyAction action = policy.Decide(new HandValue(17));

            Assert.That(action, Is.EqualTo(EnemyAction.Stand));
        }

        [Test]
        public void CL_F01_StartDealsTwoCardsAndWaitsForPlayer()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 10, 8 },
                enemyRanks: new[] { 9, 7 });

            bool started = battle.Start();

            Assert.That(started, Is.True);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
            Assert.That(battle.RoundNumber, Is.EqualTo(1));
            Assert.That(battle.TurnNumber, Is.EqualTo(1));
            Assert.That(battle.Player.Hand.Count, Is.EqualTo(2));
            Assert.That(battle.Enemy.Hand.Count, Is.EqualTo(2));
            Assert.That(battle.Player.Hand.Cards[0].IsFaceUp, Is.True);
            Assert.That(battle.Player.Hand.Cards[1].IsFaceUp, Is.False);
            Assert.That(battle.Enemy.Hand.Cards[0].IsFaceUp, Is.True);
            Assert.That(battle.Enemy.Hand.Cards[1].IsFaceUp, Is.False);
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(12));
            Assert.That(battle.Enemy.Soul.Current, Is.EqualTo(3));
        }

        [Test]
        public void CL_F02_PlayerIntermediateBustUsesVisibleTotal()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 10, 8, 10, 2 },
                enemyRanks: new[] { 10, 9 });
            battle.Start();

            bool firstHitAccepted = battle.TryPlayerHit();

            Assert.That(firstHitAccepted, Is.True);
            Assert.That(battle.Player.HandValue.Total, Is.EqualTo(28));
            Assert.That(battle.Player.VisibleHandValue.Total, Is.EqualTo(20));
            Assert.That(battle.LastResolution.HasValue, Is.False);

            bool secondHitAccepted = battle.TryPlayerHit();

            Assert.That(secondHitAccepted, Is.True);
            Assert.That(battle.LastResolution.HasValue, Is.True);
            Assert.That(battle.LastResolution.Value.Outcome, Is.EqualTo(RoundOutcome.PlayerBust));
            Assert.That(battle.LastResolution.Value.Cause, Is.EqualTo(RoundEndCause.NumericBust));
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(10));
            Assert.That(battle.Enemy.Soul.Current, Is.EqualTo(3));
            Assert.That(battle.RoundNumber, Is.EqualTo(2));
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
        }

        [Test]
        public void CL_F02_FinalShowdownHiddenCardOver21OnOneSideIsAnOrdinaryLossNotABust()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 10, 10, 2 },
                enemyRanks: new[] { 10, 7 });
            battle.Start();

            Assert.That(battle.TryPlayerHit(), Is.True);
            Assert.That(battle.Player.HandValue.Total, Is.EqualTo(22));
            Assert.That(battle.Player.VisibleHandValue.Total, Is.EqualTo(12));
            Assert.That(battle.LastResolution.HasValue, Is.False);

            Assert.That(battle.TryPlayerStand(), Is.True);

            Assert.That(battle.LastResolution.HasValue, Is.True);
            // rule.md 7.4/8.2: a total that only exceeds 21 once the hidden card is
            // folded in at final showdown is never a bust — it's an ordinary
            // total-comparison loss for that side alone (mutual loss only happens
            // when BOTH sides end up over 21), so the enemy just wins normally here.
            Assert.That(battle.LastResolution.Value.Outcome, Is.EqualTo(RoundOutcome.EnemyWin));
            Assert.That(battle.LastResolution.Value.Cause, Is.EqualTo(RoundEndCause.TotalComparison));
        }

        [Test]
        public void CL_F03_PlayerStandLetsEnemyActUntilRoundResolves()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 10, 9 },
                enemyRanks: new[] { 10, 6, 2 });
            battle.Start();

            bool accepted = battle.TryPlayerStand();

            Assert.That(accepted, Is.True);
            Assert.That(battle.LastResolution.HasValue, Is.True);
            Assert.That(battle.LastResolution.Value.Outcome, Is.EqualTo(RoundOutcome.PlayerWin));
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(12));
            Assert.That(battle.Enemy.Soul.Current, Is.EqualTo(2));
            Assert.That(battle.RoundNumber, Is.EqualTo(2));
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
        }

        [Test]
        public void SafeHitsAlternateBackToPlayerTurn()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 5, 5, 2 },
                enemyRanks: new[] { 8, 8, 1 });
            battle.Start();

            var actorTurns = new List<(CombatantSide Actor, int Turn)>();
            int observedActionCount = 0;
            battle.Stepped += () =>
            {
                if (battle.PublicActionHistory.Count <= observedActionCount)
                {
                    return;
                }

                observedActionCount = battle.PublicActionHistory.Count;
                PublicCombatAction action = battle.LastPublicAction;
                actorTurns.Add((action.ActorSide, battle.TurnNumber));
            };

            battle.TryPlayerHit();

            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
            Assert.That(battle.Player.HandValue.Total, Is.EqualTo(12));
            Assert.That(battle.Enemy.HandValue.Total, Is.EqualTo(17));
            Assert.That(battle.Enemy.IsStanding, Is.False);
            Assert.That(actorTurns[0], Is.EqualTo((CombatantSide.Player, 1)));
            Assert.That(actorTurns[1], Is.EqualTo((CombatantSide.Enemy, 1)));
            Assert.That(battle.TurnNumber, Is.EqualTo(2));
        }

        [Test]
        public void TURN01_U01_StandingPlayerLetsEachConsecutiveEnemyActionAdvanceTurn()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 10, 9 },
                enemyRanks: new[] { 2, 2, 2, 2, 2, 2, 2, 2, 2, 2 });
            battle.Start();
            var enemyActionTurns = new List<int>();
            int observedActionCount = 0;
            battle.Stepped += () =>
            {
                if (battle.RoundNumber != 1 ||
                    battle.PublicActionHistory.Count <= observedActionCount)
                {
                    return;
                }

                observedActionCount = battle.PublicActionHistory.Count;
                if (battle.LastPublicAction.ActorSide == CombatantSide.Enemy)
                {
                    enemyActionTurns.Add(battle.TurnNumber);
                }
            };

            Assert.That(battle.TryPlayerStand(), Is.True);

            Assert.That(enemyActionTurns.Count, Is.GreaterThan(2));
            Assert.That(enemyActionTurns[0], Is.EqualTo(1));
            Assert.That(enemyActionTurns[1], Is.EqualTo(2));
            Assert.That(enemyActionTurns[2], Is.EqualTo(3));
        }

        [Test]
        public void PlayerActionsAreRejectedOutsidePlayerTurn()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 10, 9 },
                enemyRanks: new[] { 10, 8 });

            Assert.That(battle.TryPlayerHit(), Is.False);
            Assert.That(battle.TryPlayerStand(), Is.False);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.Initializing));
        }

        [Test]
        public void DepletedSoulEndsBattleWithoutStartingAnotherRound()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 10, 1 },
                enemyRanks: new[] { 10, 10 },
                enemyMaximumSoul: 1);
            battle.Start();

            battle.TryPlayerStand();

            Assert.That(battle.LastResolution.HasValue, Is.True);
            Assert.That(battle.LastResolution.Value.Outcome, Is.EqualTo(RoundOutcome.PlayerTwentyOneWin));
            Assert.That(battle.Enemy.Soul.Current, Is.Zero);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.BattleEnded));
            Assert.That(battle.RoundNumber, Is.EqualTo(1));
            Assert.That(battle.Player.Hand.Count, Is.Zero);
            Assert.That(battle.Enemy.Hand.Count, Is.Zero);
            Assert.That(battle.TryPlayerHit(), Is.False);
        }

        private static CoreLoopBattle CreateBattle(
            IReadOnlyList<int> playerRanks,
            IReadOnlyList<int> enemyRanks,
            int enemyMaximumSoul = 3)
        {
            return new CoreLoopBattle(
                CreateDeck(playerRanks),
                CreateDeck(enemyRanks),
                enemyMaximumSoul: enemyMaximumSoul);
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
