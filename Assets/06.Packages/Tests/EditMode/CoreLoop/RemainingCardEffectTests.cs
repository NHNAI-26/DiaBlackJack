using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class RemainingCardEffectTests
    {
        [Test]
        public void CU04_U01_CrystalOrbRejectsUseWithoutTwoAvailableCards()
        {
            CoreLoopBattle battle = CreateStartedBattle(
                playerRanks: new[] { 2, 5, 3 },
                enemyRanks: new[] { 10, 7, 5 });
            BlackjackCard sourceCard = battle.Player.Hand.Cards[1];

            bool accepted = battle.TryBeginPlayerCardUse(sourceCard.Id);

            Assert.That(accepted, Is.False);
            Assert.That(
                battle.EvaluatePlayerCardUse(sourceCard.Id).Reason,
                Is.EqualTo(CardUseUnavailableReason.EffectRequirementsNotMet));
            Assert.That(sourceCard.IsFaceUp, Is.False);
            Assert.That(sourceCard.UseState, Is.EqualTo(CardUseState.Available));
            Assert.That(battle.PendingPlayerCardEffect, Is.Null);
            Assert.That(battle.Player.Deck.DrawCount, Is.EqualTo(1));
        }

        [Test]
        public void CU04_U02_CrystalOrbDetachesTwoCardsAndOffersOnlyItsThreeChoices()
        {
            CoreLoopBattle battle = CreateStartedBattle(
                playerRanks: new[] { 2, 5, 7, 8, 9 },
                enemyRanks: new[] { 10, 7, 5 });
            BlackjackCard sourceCard = battle.Player.Hand.Cards[1];

            bool accepted = battle.TryBeginPlayerCardUse(sourceCard.Id);

            Assert.That(accepted, Is.True);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerResolvingCardEffect));
            Assert.That(sourceCard.UseState, Is.EqualTo(CardUseState.Resolving));
            Assert.That(battle.Player.Deck.DrawCount, Is.EqualTo(1));
            Assert.That(
                battle.PendingPlayerCardEffect.Options.Select(option => option.Id),
                Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(
                battle.PendingPlayerCardEffect.Options.Select(option => option.CardId),
                Is.EqualTo(new int?[] { null, 2, 3 }));
            Assert.That(
                battle.PendingPlayerCardEffect.TemporaryCards.Select(card => card.Rank),
                Is.EqualTo(new[] { 7, 8 }));

            PendingCardEffect pending = battle.PendingPlayerCardEffect;
            Assert.That(battle.TryResolvePlayerCardChoice(3), Is.False);
            Assert.That(battle.PendingPlayerCardEffect, Is.SameAs(pending));
            Assert.That(battle.Player.Deck.DrawCount, Is.EqualTo(1));
        }

        [Test]
        public void CU04_U03_CrystalOrbTakingNoneRestoresExactNextDrawOrder()
        {
            CoreLoopBattle battle = CreateStartedBattle(
                playerRanks: new[] { 2, 5, 7, 8, 9 },
                enemyRanks: new[] { 10, 7, 5 });

            Assert.That(battle.TryBeginPlayerCardUse(battle.Player.Hand.Cards[1].Id), Is.True);
            Assert.That(battle.TryResolvePlayerCardChoice(0), Is.True);

            Assert.That(battle.Player.Hand.Count, Is.EqualTo(2));
            Assert.That(battle.Player.Deck.Draw().Rank, Is.EqualTo(7));
            Assert.That(battle.Player.Deck.Draw().Rank, Is.EqualTo(8));
            Assert.That(battle.LastCardEffectResult.Value.Succeeded, Is.True);
        }

        [TestCase(1, 7, 8)]
        [TestCase(2, 8, 7)]
        public void CU04_U04_CrystalOrbTakesSelectedCardAndReturnsTheOther(
            int optionId,
            int selectedRank,
            int nextDrawRank)
        {
            CoreLoopBattle battle = CreateStartedBattle(
                playerRanks: new[] { 2, 5, 7, 8, 9 },
                enemyRanks: new[] { 10, 7, 5 });

            Assert.That(battle.TryBeginPlayerCardUse(battle.Player.Hand.Cards[1].Id), Is.True);
            Assert.That(battle.TryResolvePlayerCardChoice(optionId), Is.True);

            BlackjackCard selectedCard = battle.Player.Hand.Cards.Single(
                card => card.Rank == selectedRank);
            Assert.That(selectedCard.IsFaceUp, Is.True);
            Assert.That(battle.Player.Deck.Draw().Rank, Is.EqualTo(nextDrawRank));
            Assert.That(battle.LastCardEffectResult.Value.EffectKind,
                Is.EqualTo(CardEffectKind.CrystalOrb));
        }

        [Test]
        public void CU04_U05_CrystalOrbNumericBustResolvesImmediatelyWithoutEnemyTurn()
        {
            CoreLoopBattle battle = CreateStartedBattle(
                playerRanks: new[] { 10, 5, 10, 2 },
                enemyRanks: new[] { 2, 3, 4, 5 },
                playerCurrentSoul: 2);
            BlackjackCard sourceCard = battle.Player.Hand.Cards[1];
            int enemyDrawCountBefore = battle.Enemy.Deck.DrawCount;

            Assert.That(battle.TryBeginPlayerCardUse(sourceCard.Id), Is.True);
            Assert.That(battle.TryResolvePlayerCardChoice(1), Is.True);

            Assert.That(sourceCard.UseState, Is.EqualTo(CardUseState.Used));
            Assert.That(battle.Player.Soul.Current, Is.Zero);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.BattleEnded));
            Assert.That(battle.Outcome, Is.EqualTo(BattleOutcome.PlayerDefeat));
            Assert.That(battle.Enemy.Deck.DrawCount, Is.EqualTo(enemyDrawCountBefore));
            Assert.That(battle.LastResolution.Value.Outcome, Is.EqualTo(RoundOutcome.PlayerBust));
            Assert.That(battle.LastResolution.Value.Cause, Is.EqualTo(RoundEndCause.NumericBust));
            Assert.That(battle.LastCardEffectResult.Value.EndedRound, Is.True);
        }

        [Test]
        public void CU04_U06_ThreatHammerOffersOnlyOpponentFaceUpCards()
        {
            CoreLoopBattle battle = CreateStartedBattle(
                playerRanks: new[] { 2, 6, 3 },
                enemyRanks: new[] { 10, 7, 5, 4 });
            BlackjackCard sourceCard = battle.Player.Hand.Cards[1];
            battle.Enemy.Draw(faceUp: true);

            bool accepted = battle.TryBeginPlayerCardUse(sourceCard.Id);

            Assert.That(accepted, Is.True);
            Assert.That(sourceCard.IsFaceUp, Is.True);
            Assert.That(battle.Player.Hand.Cards.Contains(sourceCard), Is.True);
            Assert.That(
                battle.PendingPlayerCardEffect.Options.Select(option => option.CardId),
                Is.EqualTo(new int?[] { 0, 2 }));
            Assert.That(
                battle.PendingPlayerCardEffect.ChoiceKind,
                Is.EqualTo(CardEffectChoiceKind.DiscardOpponentFaceUpCard));
            Assert.That(
                battle.PendingPlayerCardEffect.Prompt,
                Is.EqualTo("버릴 상대 공개 카드를 선택하세요."));
        }

        [TestCase(0, 10)]
        [TestCase(2, 5)]
        public void CU04_U07_ThreatHammerDiscardsChosenOpponentCardWithoutOwnCost(
            int optionId,
            int discardedRank)
        {
            CoreLoopBattle battle = CreateStartedBattle(
                playerRanks: new[] { 2, 6, 3 },
                enemyRanks: new[] { 10, 7, 5, 4 });
            BlackjackCard sourceCard = battle.Player.Hand.Cards[1];
            battle.Enemy.Draw(faceUp: true);

            Assert.That(battle.TryBeginPlayerCardUse(sourceCard.Id), Is.True);
            Assert.That(battle.TryResolvePlayerCardChoice(optionId), Is.True);

            Assert.That(sourceCard.UseState, Is.EqualTo(CardUseState.Used));
            Assert.That(battle.Player.Hand.Cards.Contains(sourceCard), Is.True);
            Assert.That(battle.Player.Deck.DiscardCount, Is.Zero);
            Assert.That(
                battle.Enemy.Deck.GetDiscardedCards().Select(card => card.Rank),
                Does.Contain(discardedRank));
            Assert.That(battle.Enemy.Hand.Contains(optionId), Is.False);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
        }

        [Test]
        public void CU04_U08_ThreatHammerRejectsWithoutOpponentFaceUpCard()
        {
            CoreLoopBattle battle = CreateStartedBattle(
                playerRanks: new[] { 2, 6, 3 },
                enemyRanks: new[] { 5, 7, 4 });
            BlackjackCard sourceCard = battle.Player.Hand.Cards[1];
            BlackjackCard enemyFaceUpCard = battle.Enemy.Hand.Cards[0];
            Assert.That(battle.Enemy.TryDiscardCard(enemyFaceUpCard.Id), Is.True);

            bool accepted = battle.TryBeginPlayerCardUse(sourceCard.Id);

            Assert.That(accepted, Is.False);
            Assert.That(sourceCard.IsFaceUp, Is.False);
            Assert.That(sourceCard.UseState, Is.EqualTo(CardUseState.Available));
            Assert.That(battle.PendingPlayerCardEffect, Is.Null);
            Assert.That(
                battle.EvaluatePlayerCardUse(sourceCard.Id).Reason,
                Is.EqualTo(CardUseUnavailableReason.EffectRequirementsNotMet));
        }

        [Test]
        public void CU04_U09_ThreatHammerStandingReplacementRejectsAtomicallyWithoutDeckCard()
        {
            CoreLoopBattle battle = CreateStartedBattle(
                playerRanks: new[] { 2, 6, 3 },
                enemyRanks: new[] { 5, 7 });
            BlackjackCard sourceCard = battle.Player.Hand.Cards[1];
            BlackjackCard hiddenEnemyCard = battle.Enemy.Hand.Cards[1];
            battle.Enemy.Stand();

            bool accepted = battle.TryBeginPlayerCardUse(sourceCard.Id);

            Assert.That(accepted, Is.False);
            Assert.That(sourceCard.IsFaceUp, Is.False);
            Assert.That(sourceCard.UseState, Is.EqualTo(CardUseState.Available));
            Assert.That(hiddenEnemyCard, Is.SameAs(battle.Enemy.Hand.Cards[1]));
            Assert.That(hiddenEnemyCard.IsFaceUp, Is.False);
            Assert.That(battle.Enemy.IsStanding, Is.True);
            Assert.That(battle.Enemy.Deck.DiscardCount, Is.Zero);
        }

        [Test]
        public void CU04_U10_ThreatHammerDiscardsPublicAndReplacesHiddenWhenStanding()
        {
            CoreLoopBattle battle = CreateStartedBattle(
                playerRanks: new[] { 2, 6 },
                enemyRanks: new[] { 5, 7, 6, 5 });
            BlackjackCard sourceCard = battle.Player.Hand.Cards[1];
            BlackjackCard previousHiddenCard = battle.Enemy.Hand.Cards[1];
            battle.Enemy.Stand();

            Assert.That(battle.TryBeginPlayerCardUse(sourceCard.Id), Is.True);
            Assert.That(battle.TryResolvePlayerCardChoice(0), Is.True);

            Assert.That(sourceCard.UseState, Is.EqualTo(CardUseState.Used));
            Assert.That(battle.Player.Hand.Cards.Contains(sourceCard), Is.True);
            Assert.That(battle.Player.Deck.DiscardCount, Is.Zero);
            Assert.That(previousHiddenCard.IsFaceUp, Is.False);
            Assert.That(battle.Enemy.Deck.DiscardCount, Is.EqualTo(2));
            Assert.That(
                battle.Enemy.Deck.GetDiscardedCards().Select(card => card.Rank),
                Is.EquivalentTo(new[] { 5, 7 }));
            Assert.That(battle.Enemy.Hand.Cards.Select(card => card.Rank),
                Is.EqualTo(new[] { 6, 5 }));
            Assert.That(battle.Enemy.Hand.HiddenCardCount, Is.EqualTo(1));
            Assert.That(battle.Enemy.Hand.Cards.Single(card => !card.IsFaceUp).Rank,
                Is.EqualTo(6));
            Assert.That(battle.Enemy.IsStanding, Is.False);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
        }

        [Test]
        public void CU04_U11_ThreatHammerReplacementBustResolvesAsNumericBust()
        {
            CoreLoopBattle battle = CreateStartedBattle(
                playerRanks: new[] { 2, 6 },
                enemyRanks: new[] { 10, 2, 8, 7, 9 },
                enemyMaximumSoul: 1);
            BlackjackCard sourceCard = battle.Player.Hand.Cards[1];
            BlackjackCard previousHiddenCard = battle.Enemy.Hand.Cards[1];
            battle.Enemy.Draw(faceUp: true);
            battle.Enemy.Draw(faceUp: true);
            battle.Enemy.Stand();

            Assert.That(battle.TryBeginPlayerCardUse(sourceCard.Id), Is.True);
            Assert.That(battle.TryResolvePlayerCardChoice(3), Is.True);

            Assert.That(previousHiddenCard.IsFaceUp, Is.False);
            Assert.That(battle.Enemy.Soul.Current, Is.Zero);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.BattleEnded));
            Assert.That(battle.Outcome, Is.EqualTo(BattleOutcome.PlayerVictory));
            Assert.That(battle.LastResolution.Value.Outcome, Is.EqualTo(RoundOutcome.EnemyBust));
            Assert.That(battle.LastResolution.Value.Cause, Is.EqualTo(RoundEndCause.NumericBust));
            Assert.That(battle.LastCardEffectResult.Value.EndedRound, Is.True);
        }

        [Test]
        public void CU04_U12_MilitaryKnifeRejectsVisibleTotalOverSixteenOrEmptyDeck()
        {
            CoreLoopBattle highTotalBattle = CreateStartedBattle(
                playerRanks: new[] { 2, 9 },
                enemyRanks: new[] { 10, 7, 8, 2 });
            highTotalBattle.Enemy.Draw(faceUp: true);
            BlackjackCard highTotalSource = highTotalBattle.Player.Hand.Cards[1];

            CoreLoopBattle emptyDeckBattle = CreateStartedBattle(
                playerRanks: new[] { 2, 10 },
                enemyRanks: new[] { 5, 7 });
            BlackjackCard emptyDeckSource = emptyDeckBattle.Player.Hand.Cards[1];

            Assert.That(highTotalBattle.TryBeginPlayerCardUse(highTotalSource.Id), Is.False);
            Assert.That(highTotalSource.IsFaceUp, Is.False);
            Assert.That(highTotalSource.UseState, Is.EqualTo(CardUseState.Available));
            Assert.That(emptyDeckBattle.TryBeginPlayerCardUse(emptyDeckSource.Id), Is.False);
            Assert.That(emptyDeckSource.IsFaceUp, Is.False);
            Assert.That(emptyDeckSource.UseState, Is.EqualTo(CardUseState.Available));
        }

        [TestCase(9)]
        [TestCase(10)]
        public void CU04_U13_MilitaryKnifeDefaultPolicyKeepsForcedCard(int sourceRank)
        {
            CoreLoopBattle battle = CreateStartedBattle(
                playerRanks: new[] { 2, sourceRank },
                enemyRanks: new[] { 5, 7, 2, 3 });
            BlackjackCard sourceCard = battle.Player.Hand.Cards[1];

            bool accepted = battle.TryBeginPlayerCardUse(sourceCard.Id);

            Assert.That(accepted, Is.True);
            Assert.That(sourceCard.UseState, Is.EqualTo(CardUseState.Used));
            Assert.That(battle.PendingPlayerCardEffect, Is.Null);
            Assert.That(battle.Enemy.Hand.Cards.Select(card => card.Rank),
                Is.EqualTo(new[] { 5, 7, 2, 3 }));
            Assert.That(battle.Enemy.Deck.DiscardCount, Is.Zero);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
            Assert.That(battle.LastCardEffectResult.Value.EffectKind,
                Is.EqualTo(CardEffectKind.MilitaryKnife));
        }

        [Test]
        public void CU04_U14_MilitaryKnifeForcedDrawBustResolvesBeforeEnemyTurn()
        {
            CoreLoopBattle battle = CreateStartedBattle(
                playerRanks: new[] { 2, 9 },
                enemyRanks: new[] { 10, 10, 2, 9 },
                enemyMaximumSoul: 1);
            BlackjackCard sourceCard = battle.Player.Hand.Cards[1];
            int enemyDrawCountBefore = battle.Enemy.Deck.DrawCount;

            bool accepted = battle.TryBeginPlayerCardUse(sourceCard.Id);

            Assert.That(accepted, Is.True);
            Assert.That(sourceCard.UseState, Is.EqualTo(CardUseState.Used));
            Assert.That(battle.Enemy.Deck.DrawCount, Is.EqualTo(enemyDrawCountBefore - 1));
            Assert.That(battle.Enemy.Soul.Current, Is.Zero);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.BattleEnded));
            Assert.That(battle.LastResolution.Value.Outcome, Is.EqualTo(RoundOutcome.EnemyBust));
            Assert.That(battle.LastResolution.Value.Cause, Is.EqualTo(RoundEndCause.NumericBust));
            Assert.That(battle.LastCardEffectResult.Value.EndedRound, Is.True);
        }

        [Test]
        public void CU04_U15_MilitaryKnifeRetentionPolicyCanDiscardForcedCard()
        {
            var resolver = new CardEffectResolver(
                new MilitaryKnifeEffectHandler(new DiscardForcedDrawPolicy()));
            CoreLoopBattle battle = CreateStartedBattle(
                playerRanks: new[] { 2, 9 },
                enemyRanks: new[] { 5, 7, 2, 3 },
                cardEffectResolver: resolver);

            bool accepted = battle.TryBeginPlayerCardUse(battle.Player.Hand.Cards[1].Id);

            Assert.That(accepted, Is.True);
            Assert.That(battle.Enemy.Hand.Cards.Select(card => card.Rank),
                Is.EqualTo(new[] { 5, 7, 3 }));
            Assert.That(battle.Enemy.Deck.DiscardCount, Is.EqualTo(1));
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
        }

        [Test]
        public void CU04_U16_CoreLoopSessionUsesProductionCrystalOrbHandler()
        {
            var session = new CoreLoopSession(() => CreateBattle(
                playerRanks: new[] { 2, 5, 7, 8 },
                enemyRanks: new[] { 10, 7, 5 }));
            BlackjackCard sourceCard = session.Battle.Player.Hand.Cards[1];

            bool began = session.TryBeginPlayerCardUse(sourceCard.Id);
            bool resolved = session.TryResolvePlayerCardChoice(0);

            Assert.That(began, Is.True);
            Assert.That(resolved, Is.True);
            Assert.That(sourceCard.UseState, Is.EqualTo(CardUseState.Used));
            Assert.That(session.Battle.LastCardEffectResult.Value.EffectKind,
                Is.EqualTo(CardEffectKind.CrystalOrb));
        }

        private static CoreLoopBattle CreateStartedBattle(
            IReadOnlyList<int> playerRanks,
            IReadOnlyList<int> enemyRanks,
            int playerCurrentSoul = 12,
            int enemyMaximumSoul = 3,
            CardEffectResolver cardEffectResolver = null)
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks,
                enemyRanks,
                playerCurrentSoul,
                enemyMaximumSoul,
                cardEffectResolver);
            Assert.That(battle.Start(), Is.True);
            return battle;
        }

        private static CoreLoopBattle CreateBattle(
            IReadOnlyList<int> playerRanks,
            IReadOnlyList<int> enemyRanks,
            int playerCurrentSoul = 12,
            int enemyMaximumSoul = 3,
            CardEffectResolver cardEffectResolver = null)
        {
            if (cardEffectResolver == null)
            {
                return new CoreLoopBattle(
                    CreateDeck(playerRanks),
                    CreateDeck(enemyRanks),
                    playerMaximumSoul: 12,
                    playerCurrentSoul,
                    enemyMaximumSoul);
            }

            return new CoreLoopBattle(
                CreateDeck(playerRanks),
                CreateDeck(enemyRanks),
                playerMaximumSoul: 12,
                playerCurrentSoul,
                enemyMaximumSoul,
                enemyPolicy: null,
                cardEffectResolver);
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

        private sealed class DiscardForcedDrawPolicy : IForcedDrawRetentionPolicy
        {
            public bool ShouldKeep(HandValue enemyHandAfterDraw, BlackjackCard drawnCard)
            {
                return false;
            }
        }
    }
}
