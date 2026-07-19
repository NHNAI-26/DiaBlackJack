using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class CombatActionChangeTests
    {
        [Test]
        public void BA03_BeginChangeMovesOnlyPlayerCardsIntoPendingSelection()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 10, 2, 4, 9 },
                enemyRanks: new[] { 10, 7 });
            battle.Start();

            HandValue enemyVisibleValue = battle.Enemy.VisibleHandValue;
            bool accepted = battle.TryBeginPlayerChange();

            Assert.That(accepted, Is.True);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerChoosingChangeCard));
            Assert.That(battle.CanBeginPlayerChange, Is.False);
            Assert.That(battle.CanSelectChangedCard, Is.True);
            Assert.That(battle.HasPlayerChangedThisRound, Is.False);
            Assert.That(battle.Player.Hand.Count, Is.EqualTo(1));
            Assert.That(battle.Player.Hand.HiddenCardCount, Is.Zero);
            Assert.That(
                battle.PlayerChangeCandidates.Select(card => card.Rank),
                Is.EqualTo(new[] { 4, 9 }));
            Assert.That(
                battle.PlayerChangeCandidates.All(card => card.IsFaceUp),
                Is.True);
            Assert.That(battle.Player.Deck.CardsInPlayCount, Is.EqualTo(4));
            Assert.That(battle.Enemy.Hand.Count, Is.EqualTo(2));
            Assert.That(battle.Enemy.VisibleHandValue.Total, Is.EqualTo(enemyVisibleValue.Total));
        }

        [Test]
        public void BA03_InvalidCandidateLeavesPendingCardsAndStateUnchanged()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 10, 2, 4, 9, 6, 7 },
                enemyRanks: new[] { 10, 7 });
            battle.Start();
            battle.TryBeginPlayerChange();
            BlackjackCard[] candidates = battle.PlayerChangeCandidates.ToArray();
            int availableCardCount = battle.Player.Deck.AvailableCardCount;

            bool negativeAccepted = battle.TrySelectChangedCard(-1);
            bool overflowAccepted = battle.TrySelectChangedCard(2);

            Assert.That(negativeAccepted, Is.False);
            Assert.That(overflowAccepted, Is.False);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerChoosingChangeCard));
            Assert.That(battle.PlayerChangeCandidates, Is.EqualTo(candidates));
            Assert.That(battle.Player.Hand.Count, Is.EqualTo(1));
            Assert.That(battle.Player.Deck.AvailableCardCount, Is.EqualTo(availableCardCount));
            Assert.That(battle.Player.Deck.DiscardCount, Is.Zero);
            Assert.That(battle.HasPlayerChangedThisRound, Is.False);
        }

        [Test]
        public void BA03_ValidSelectionKeepsChosenCardHiddenAndDiscardsOtherTwo()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 10, 2, 4, 9, 6, 7 },
                enemyRanks: new[] { 10, 7 });
            battle.Start();
            BlackjackCard previousHiddenCard = battle.Player.Hand.Cards.Single(card => !card.IsFaceUp);
            battle.TryBeginPlayerChange();
            BlackjackCard unselectedCandidate = battle.PlayerChangeCandidates[0];
            BlackjackCard selectedCandidate = battle.PlayerChangeCandidates[1];

            bool accepted = battle.TrySelectChangedCard(1);

            Assert.That(accepted, Is.True);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
            Assert.That(battle.HasPlayerChangedThisRound, Is.True);
            Assert.That(battle.CanSelectChangedCard, Is.False);
            Assert.That(battle.PlayerChangeCandidates, Is.Empty);
            Assert.That(battle.Player.Hand.Count, Is.EqualTo(2));
            Assert.That(battle.Player.Hand.Cards.Contains(selectedCandidate), Is.True);
            Assert.That(selectedCandidate.IsFaceUp, Is.False);
            Assert.That(battle.Player.Hand.Cards.Contains(previousHiddenCard), Is.False);
            Assert.That(battle.Player.Hand.Cards.Contains(unselectedCandidate), Is.False);
            Assert.That(battle.Player.Deck.DiscardCount, Is.EqualTo(2));
            Assert.That(battle.Player.Deck.AvailableCardCount + battle.Player.Hand.Count,
                Is.EqualTo(battle.Player.Deck.TotalCardCount));
        }

        [Test]
        public void BA03_ChoosingChangeRejectsOtherActionsAndSelectionConsumesEnemyTurn()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 10, 2, 4, 9 },
                enemyRanks: new[] { 5, 5, 7 });
            battle.Start();
            battle.TryBeginPlayerChange();

            Assert.That(battle.TryPlayerHit(), Is.False);
            Assert.That(battle.TryPlayerStand(), Is.False);
            Assert.That(battle.TryPlayerFold(), Is.False);
            Assert.That(battle.TryBeginPlayerChange(), Is.False);
            Assert.That(battle.Enemy.Hand.Count, Is.EqualTo(2));

            bool selected = battle.TrySelectChangedCard(0);

            Assert.That(selected, Is.True);
            Assert.That(battle.Enemy.Hand.Count, Is.EqualTo(3));
            Assert.That(battle.Enemy.HandValue.Total, Is.EqualTo(17));
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
        }

        [Test]
        public void BA03_ChangeCannotBeReusedUntilNextRound()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 10, 8, 5, 6, 9, 7, 4, 3 },
                enemyRanks: new[] { 10, 7, 9, 7 });
            battle.Start();
            battle.TryBeginPlayerChange();
            battle.TrySelectChangedCard(0);

            Assert.That(battle.HasPlayerChangedThisRound, Is.True);
            Assert.That(battle.TryBeginPlayerChange(), Is.False);

            battle.TryPlayerStand();

            Assert.That(battle.RoundNumber, Is.EqualTo(2));
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
            Assert.That(battle.HasPlayerChangedThisRound, Is.False);
            Assert.That(battle.TryBeginPlayerChange(), Is.True);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerChoosingChangeCard));
        }

        [Test]
        public void BA03_InsufficientCandidatesRejectWithoutMutation()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 10, 2, 4 },
                enemyRanks: new[] { 10, 7 });
            battle.Start();
            BlackjackCard[] hand = battle.Player.Hand.Cards.ToArray();
            int drawCount = battle.Player.Deck.DrawCount;
            int cardsInPlayCount = battle.Player.Deck.CardsInPlayCount;

            bool accepted = battle.TryBeginPlayerChange();

            Assert.That(accepted, Is.False);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
            Assert.That(battle.Player.Hand.Cards, Is.EqualTo(hand));
            Assert.That(battle.Player.Deck.DrawCount, Is.EqualTo(drawCount));
            Assert.That(battle.Player.Deck.CardsInPlayCount, Is.EqualTo(cardsInPlayCount));
            Assert.That(battle.PlayerChangeCandidates, Is.Empty);
        }

        [Test]
        public void BA03_ChangeRequiresExactlyOneHiddenCard()
        {
            CoreLoopBattle noHiddenBattle = CreateBattle(
                playerRanks: new[] { 10, 2, 4, 9 },
                enemyRanks: new[] { 10, 7 });
            noHiddenBattle.Start();
            noHiddenBattle.Player.Hand.Cards[1].Reveal();

            CoreLoopBattle twoHiddenBattle = CreateBattle(
                playerRanks: new[] { 10, 2, 4, 9 },
                enemyRanks: new[] { 10, 7 });
            twoHiddenBattle.Start();
            twoHiddenBattle.Player.Hand.Cards[0].Conceal();

            Assert.That(noHiddenBattle.TryBeginPlayerChange(), Is.False);
            Assert.That(twoHiddenBattle.TryBeginPlayerChange(), Is.False);
            Assert.That(noHiddenBattle.Player.Hand.Count, Is.EqualTo(2));
            Assert.That(twoHiddenBattle.Player.Hand.Count, Is.EqualTo(2));
            Assert.That(noHiddenBattle.Player.Deck.DrawCount, Is.EqualTo(2));
            Assert.That(twoHiddenBattle.Player.Deck.DrawCount, Is.EqualTo(2));
        }

        [Test]
        public void BA03_CoreLoopSessionForwardsChangeFlow()
        {
            var session = new CoreLoopSession(() => CreateBattle(
                playerRanks: new[] { 10, 2, 4, 9 },
                enemyRanks: new[] { 10, 7 }));

            bool began = session.TryBeginPlayerChange();
            bool selected = session.TrySelectChangedCard(0);

            Assert.That(began, Is.True);
            Assert.That(selected, Is.True);
            Assert.That(session.Battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
            Assert.That(session.Battle.HasPlayerChangedThisRound, Is.True);
            Assert.That(session.Battle.Player.Hand.HiddenCardCount, Is.EqualTo(1));
        }

        private static CoreLoopBattle CreateBattle(
            IReadOnlyList<int> playerRanks,
            IReadOnlyList<int> enemyRanks)
        {
            return new CoreLoopBattle(
                CreateDeck(playerRanks),
                CreateDeck(enemyRanks));
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
