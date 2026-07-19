using System.Collections.Generic;
using NUnit.Framework;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class CoreLoopRulesTests
    {
        [Test]
        public void CL_U01_AceAndTenTotalTwentyOne()
        {
            Assert.That(Calculate(10, 1).Total, Is.EqualTo(21));
        }

        [Test]
        public void CL_U02_TwoAcesAndTenTotalTwelve()
        {
            Assert.That(Calculate(10, 1, 1).Total, Is.EqualTo(12));
        }

        [Test]
        public void CL_U03_TwoAcesAndNineTotalTwentyOne()
        {
            Assert.That(Calculate(1, 1, 9).Total, Is.EqualTo(21));
        }

        [Test]
        public void CL_U04_TotalAboveTwentyOneIsBust()
        {
            HandValue value = Calculate(10, 8, 4);

            Assert.That(value.Total, Is.EqualTo(22));
            Assert.That(value.IsBust, Is.True);
        }

        [Test]
        public void CL_U05_ExhaustedDrawPileRecyclesDiscardPile()
        {
            var deck = new BlackjackDeck(
                new[]
                {
                    new BlackjackCard(0, 1),
                    new BlackjackCard(1, 2)
                },
                seed: 7);

            BlackjackCard first = deck.Draw();
            BlackjackCard second = deck.Draw();
            first.Reveal();
            second.Reveal();
            deck.Discard(first);
            deck.Discard(second);

            BlackjackCard recycled = deck.Draw();

            Assert.That(recycled, Is.Not.Null);
            Assert.That(recycled.IsFaceUp, Is.False);
            Assert.That(deck.TotalCardCount, Is.EqualTo(2));
            Assert.That(deck.CardsInPlayCount, Is.EqualTo(1));
            Assert.That(deck.DrawCount + deck.DiscardCount + deck.CardsInPlayCount, Is.EqualTo(2));
        }

        [Test]
        public void StandardDeckContainsTwoOfEveryRank()
        {
            BlackjackDeck deck = BlackjackDeck.CreateStandard(seed: 17);
            var rankCounts = new int[11];

            for (int i = 0; i < 20; i++)
            {
                rankCounts[deck.Draw().Rank]++;
            }

            for (int rank = 1; rank <= 10; rank++)
            {
                Assert.That(rankCounts[rank], Is.EqualTo(2), $"Rank {rank}");
            }
        }

        [Test]
        public void SameSeedProducesSameDrawOrder()
        {
            BlackjackDeck first = BlackjackDeck.CreateStandard(seed: 1234);
            BlackjackDeck second = BlackjackDeck.CreateStandard(seed: 1234);

            for (int i = 0; i < 20; i++)
            {
                Assert.That(first.Draw().Id, Is.EqualTo(second.Draw().Id), $"Draw {i}");
            }
        }

        [Test]
        public void CL_U08_HigherPlayerTotalDamagesEnemyOnce()
        {
            RoundResolution resolution = Resolve(1, new[] { 10, 10 }, new[] { 10, 9 });

            Assert.That(resolution.Outcome, Is.EqualTo(RoundOutcome.PlayerWin));
            Assert.That(resolution.PlayerDamage, Is.Zero);
            Assert.That(resolution.EnemyDamage, Is.EqualTo(1));
        }

        [Test]
        public void CL_U09_PlayerTwentyOneDamagesEnemyTwice()
        {
            RoundResolution resolution = Resolve(2, new[] { 10, 1 }, new[] { 10, 10 });

            Assert.That(resolution.Outcome, Is.EqualTo(RoundOutcome.PlayerTwentyOneWin));
            Assert.That(resolution.EnemyDamage, Is.EqualTo(2));
        }

        [Test]
        public void CL_U10_TieIsPlayerWin()
        {
            RoundResolution resolution = Resolve(3, new[] { 10, 8 }, new[] { 9, 9 });

            Assert.That(resolution.Outcome, Is.EqualTo(RoundOutcome.PlayerWin));
            Assert.That(resolution.EnemyDamage, Is.EqualTo(1));
        }

        [Test]
        public void LowerPlayerTotalDealsOnePlayerDamage()
        {
            RoundResolution resolution = Resolve(31, new[] { 10, 8 }, new[] { 10, 9 });

            Assert.That(resolution.Outcome, Is.EqualTo(RoundOutcome.EnemyWin));
            Assert.That(resolution.PlayerDamage, Is.EqualTo(1));
            Assert.That(resolution.EnemyDamage, Is.Zero);
        }

        [Test]
        public void CL_U11_PlayerBustDealsTwoPlayerDamage()
        {
            RoundResolution resolution = Resolve(4, new[] { 10, 8, 4 }, new[] { 10, 9 });

            Assert.That(resolution.Outcome, Is.EqualTo(RoundOutcome.PlayerBust));
            Assert.That(resolution.PlayerDamage, Is.EqualTo(2));
            Assert.That(resolution.EnemyDamage, Is.Zero);
        }

        [Test]
        public void EnemyBustDealsOneEnemyDamage()
        {
            RoundResolution resolution = Resolve(5, new[] { 10, 9 }, new[] { 10, 8, 4 });

            Assert.That(resolution.Outcome, Is.EqualTo(RoundOutcome.EnemyBust));
            Assert.That(resolution.EnemyDamage, Is.EqualTo(1));
        }

        [Test]
        public void CL_U12_SameResolutionCannotApplyDamageTwice()
        {
            var playerSoul = new SoulPool(12);
            var enemySoul = new SoulPool(3);
            var applier = new RoundDamageApplier();
            RoundResolution resolution = Resolve(6, new[] { 10, 10 }, new[] { 10, 9 });

            bool firstApply = applier.TryApply(resolution, playerSoul, enemySoul);
            bool secondApply = applier.TryApply(resolution, playerSoul, enemySoul);

            Assert.That(firstApply, Is.True);
            Assert.That(secondApply, Is.False);
            Assert.That(playerSoul.Current, Is.EqualTo(12));
            Assert.That(enemySoul.Current, Is.EqualTo(2));
        }

        [Test]
        public void SoulDamageDoesNotDropBelowZero()
        {
            var soul = new SoulPool(1);

            soul.ApplyDamage(2);

            Assert.That(soul.Current, Is.Zero);
            Assert.That(soul.IsDepleted, Is.True);
        }

        private static HandValue Calculate(params int[] ranks)
        {
            return HandValueCalculator.Calculate(CreateCards(ranks));
        }

        private static RoundResolution Resolve(
            long id,
            IReadOnlyList<int> playerRanks,
            IReadOnlyList<int> enemyRanks)
        {
            return RoundResolver.Resolve(id, CreateCards(playerRanks), CreateCards(enemyRanks));
        }

        private static IReadOnlyList<BlackjackCard> CreateCards(IReadOnlyList<int> ranks)
        {
            var cards = new List<BlackjackCard>(ranks.Count);
            for (int i = 0; i < ranks.Count; i++)
            {
                cards.Add(new BlackjackCard(i, ranks[i]));
            }

            return cards;
        }
    }
}
