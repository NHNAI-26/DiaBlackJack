using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class AutoPistolEffectTests
    {
        [TestCase(7)]
        [TestCase(8)]
        public void CU03_U01_DefaultBattleOffersOnlyNumbersOneThroughTen(int sourceRank)
        {
            CoreLoopBattle battle = CreateStartedBattle(
                playerRanks: new[] { 5, sourceRank },
                enemyRanks: new[] { 10, 7, 5 });
            BlackjackCard sourceCard = battle.Player.Hand.Cards[1];

            bool accepted = battle.TryBeginPlayerCardUse(sourceCard.Id);

            Assert.That(accepted, Is.True);
            Assert.That(sourceCard.IsFaceUp, Is.True);
            Assert.That(sourceCard.UseState, Is.EqualTo(CardUseState.Resolving));
            Assert.That(battle.PendingPlayerCardEffect.ChoiceKind,
                Is.EqualTo(CardEffectChoiceKind.DeclareNumber));
            Assert.That(
                battle.PendingPlayerCardEffect.Options.Select(option => option.Id),
                Is.EqualTo(Enumerable.Range(1, 10)));
            Assert.That(
                battle.PendingPlayerCardEffect.Options.Select(option => option.NumericValue),
                Is.EqualTo(Enumerable.Range(1, 10).Select(number => (int?)number)));
            Assert.That(
                battle.PendingPlayerCardEffect.Options.Select(option => option.Label),
                Is.EqualTo(Enumerable.Range(1, 10).Select(number => number.ToString())));

            PendingCardEffect pending = battle.PendingPlayerCardEffect;
            Assert.That(battle.TryResolvePlayerCardChoice(0), Is.False);
            Assert.That(battle.TryResolvePlayerCardChoice(11), Is.False);
            Assert.That(battle.PendingPlayerCardEffect, Is.SameAs(pending));
        }

        [Test]
        public void CU03_U02_UseIsRejectedWhenEnemyHiddenCardIsMissing()
        {
            CoreLoopBattle battle = CreateStartedBattle(
                playerRanks: new[] { 5, 7 },
                enemyRanks: new[] { 10, 7, 5 });
            BlackjackCard sourceCard = battle.Player.Hand.Cards[1];
            battle.Enemy.Hand.Cards[1].Reveal();

            bool accepted = battle.TryBeginPlayerCardUse(sourceCard.Id);

            Assert.That(accepted, Is.False);
            Assert.That(
                battle.EvaluatePlayerCardUse(sourceCard.Id).Reason,
                Is.EqualTo(CardUseUnavailableReason.EffectRequirementsNotMet));
            Assert.That(sourceCard.IsFaceUp, Is.False);
            Assert.That(sourceCard.UseState, Is.EqualTo(CardUseState.Available));
            Assert.That(battle.PendingPlayerCardEffect, Is.Null);
            Assert.That(battle.LastCardEffectResult, Is.Null);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
        }

        [Test]
        public void CU03_U03_IncorrectGuessConsumesCardAndRunsOneEnemyTurn()
        {
            CoreLoopBattle battle = CreateStartedBattle(
                playerRanks: new[] { 5, 7 },
                enemyRanks: new[] { 5, 7, 5 });
            BlackjackCard sourceCard = battle.Player.Hand.Cards[1];
            BlackjackCard hiddenEnemyCard = battle.Enemy.Hand.Cards[1];
            int enemySoulBefore = battle.Enemy.Soul.Current;

            Assert.That(battle.TryBeginPlayerCardUse(sourceCard.Id), Is.True);
            bool accepted = battle.TryResolvePlayerCardChoice(6);

            Assert.That(accepted, Is.True);
            Assert.That(sourceCard.UseState, Is.EqualTo(CardUseState.Used));
            Assert.That(hiddenEnemyCard.IsFaceUp, Is.False);
            Assert.That(battle.Enemy.Soul.Current, Is.EqualTo(enemySoulBefore));
            Assert.That(battle.Enemy.Hand.Count, Is.EqualTo(3));
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
            Assert.That(battle.PendingPlayerCardEffect, Is.Null);
            Assert.That(battle.LastCardEffectResult.Value.Succeeded, Is.False);
            Assert.That(battle.LastCardEffectResult.Value.EndedRound, Is.False);
            Assert.That(battle.TryBeginPlayerCardUse(sourceCard.Id), Is.False);
            Assert.That(
                battle.EvaluatePlayerCardUse(sourceCard.Id).Reason,
                Is.EqualTo(CardUseUnavailableReason.CardIsUnavailable));
        }

        [Test]
        public void CU03_U04_CorrectGuessDealsOneSoulAndStartsNextRound()
        {
            CoreLoopBattle battle = CreateStartedBattle(
                playerRanks: new[] { 5, 7, 2, 3 },
                enemyRanks: new[] { 5, 7, 2, 3 });
            BlackjackCard hiddenEnemyCard = battle.Enemy.Hand.Cards[1];

            Assert.That(battle.TryBeginPlayerCardUse(battle.Player.Hand.Cards[1].Id), Is.True);
            bool accepted = battle.TryResolvePlayerCardChoice(7);

            Assert.That(accepted, Is.True);
            Assert.That(hiddenEnemyCard.IsFaceUp, Is.False);
            Assert.That(battle.Enemy.Soul.Current, Is.EqualTo(2));
            Assert.That(battle.RoundNumber, Is.EqualTo(2));
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
            Assert.That(battle.Enemy.Hand.Cards.Select(card => card.Rank), Is.EqualTo(new[] { 2, 3 }));
            Assert.That(battle.LastResolution.Value.Outcome, Is.EqualTo(RoundOutcome.EnemyBust));
            Assert.That(battle.LastResolution.Value.Cause, Is.EqualTo(RoundEndCause.CardEffectBust));
            Assert.That(battle.LastResolution.Value.SourceCardKey, Is.EqualTo("auto-pistol-7"));
            Assert.That(battle.LastCardEffectResult.Value.Succeeded, Is.True);
            Assert.That(battle.LastCardEffectResult.Value.EndedRound, Is.True);
        }

        [Test]
        public void CU03_U05_LethalGuessEndsBattleWithoutEnemyTurn()
        {
            CoreLoopBattle battle = CreateStartedBattle(
                playerRanks: new[] { 5, 8 },
                enemyRanks: new[] { 5, 7, 9 },
                enemyMaximumSoul: 1);
            BlackjackCard sourceCard = battle.Player.Hand.Cards[1];
            BlackjackCard hiddenEnemyCard = battle.Enemy.Hand.Cards[1];
            int enemyDrawCountBefore = battle.Enemy.Deck.DrawCount;

            Assert.That(battle.TryBeginPlayerCardUse(sourceCard.Id), Is.True);
            bool accepted = battle.TryResolvePlayerCardChoice(7);

            Assert.That(accepted, Is.True);
            Assert.That(sourceCard.UseState, Is.EqualTo(CardUseState.Used));
            Assert.That(hiddenEnemyCard.IsFaceUp, Is.False);
            Assert.That(battle.Enemy.Soul.Current, Is.Zero);
            Assert.That(battle.Enemy.Deck.DrawCount, Is.EqualTo(enemyDrawCountBefore));
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.BattleEnded));
            Assert.That(battle.Outcome, Is.EqualTo(BattleOutcome.PlayerVictory));
            Assert.That(battle.LastResolution.Value.SourceCardKey, Is.EqualTo("auto-pistol-8"));
        }

        [Test]
        public void CU03_U06_CoreLoopSessionUsesProductionAutoPistolHandler()
        {
            var session = new CoreLoopSession(() => CreateBattle(
                playerRanks: new[] { 5, 7 },
                enemyRanks: new[] { 5, 7, 5 }));
            BlackjackCard sourceCard = session.Battle.Player.Hand.Cards[1];

            bool began = session.TryBeginPlayerCardUse(sourceCard.Id);
            bool resolved = session.TryResolvePlayerCardChoice(6);

            Assert.That(began, Is.True);
            Assert.That(resolved, Is.True);
            Assert.That(sourceCard.UseState, Is.EqualTo(CardUseState.Used));
            Assert.That(session.Battle.LastCardEffectResult.Value.EffectKind,
                Is.EqualTo(CardEffectKind.AutoPistol));
        }

        [Test]
        public void CU03_U07_PublicEffectResultCannotExposeHiddenRank()
        {
            string[] publicPropertyNames = typeof(CardEffectResult)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Select(property => property.Name)
                .OrderBy(name => name)
                .ToArray();

            Assert.That(
                publicPropertyNames,
                Is.EqualTo(new[] { "EffectKind", "EndedRound", "SourceCardId", "Succeeded" }));
        }

        private static CoreLoopBattle CreateStartedBattle(
            IReadOnlyList<int> playerRanks,
            IReadOnlyList<int> enemyRanks,
            int enemyMaximumSoul = 3)
        {
            CoreLoopBattle battle = CreateBattle(playerRanks, enemyRanks, enemyMaximumSoul);
            Assert.That(battle.Start(), Is.True);
            return battle;
        }

        private static CoreLoopBattle CreateBattle(
            IReadOnlyList<int> playerRanks,
            IReadOnlyList<int> enemyRanks,
            int enemyMaximumSoul = 3)
        {
            return new CoreLoopBattle(
                CreateDeck(playerRanks),
                CreateDeck(enemyRanks),
                playerMaximumSoul: 12,
                playerCurrentSoul: 12,
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
