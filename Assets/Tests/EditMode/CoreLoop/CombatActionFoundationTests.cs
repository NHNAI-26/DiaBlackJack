using System;
using System.Linq;
using NUnit.Framework;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class CombatActionFoundationTests
    {
        [Test]
        public void BA01_HandTakesItsOnlyHiddenCardWithoutChangingTheOthers()
        {
            var hand = new BlackjackHand();
            var visibleCard = new BlackjackCard(0, 4, isFaceUp: true);
            var hiddenCard = new BlackjackCard(1, 9);
            hand.Add(visibleCard);
            hand.Add(hiddenCard);

            bool taken = hand.TryTakeSingleHiddenCard(out BlackjackCard result);

            Assert.That(taken, Is.True);
            Assert.That(result, Is.SameAs(hiddenCard));
            Assert.That(hand.Count, Is.EqualTo(1));
            Assert.That(hand.Cards[0], Is.SameAs(visibleCard));
            Assert.That(hand.HiddenCardCount, Is.Zero);
        }

        [Test]
        public void BA01_HandWithNoHiddenCardRejectsWithoutMutation()
        {
            var hand = new BlackjackHand();
            var visibleCard = new BlackjackCard(0, 4, isFaceUp: true);
            hand.Add(visibleCard);

            bool taken = hand.TryTakeSingleHiddenCard(out BlackjackCard result);

            Assert.That(taken, Is.False);
            Assert.That(result, Is.Null);
            Assert.That(hand.Count, Is.EqualTo(1));
            Assert.That(hand.Cards[0], Is.SameAs(visibleCard));
        }

        [Test]
        public void BA01_HandWithMultipleHiddenCardsRejectsWithoutMutation()
        {
            var hand = new BlackjackHand();
            var first = new BlackjackCard(0, 4);
            var second = new BlackjackCard(1, 9);
            hand.Add(first);
            hand.Add(second);

            bool taken = hand.TryTakeSingleHiddenCard(out BlackjackCard result);

            Assert.That(taken, Is.False);
            Assert.That(result, Is.Null);
            Assert.That(hand.Count, Is.EqualTo(2));
            Assert.That(hand.Cards, Is.EqualTo(new[] { first, second }));
            Assert.That(hand.HiddenCardCount, Is.EqualTo(2));
        }

        [Test]
        public void BA01_DeckChecksCandidateCapacityAcrossDrawAndDiscardWithoutMutation()
        {
            BlackjackDeck deck = BlackjackDeck.CreateInDrawOrder(
                new[]
                {
                    new BlackjackCard(0, 2),
                    new BlackjackCard(1, 3),
                    new BlackjackCard(2, 4)
                });
            BlackjackCard first = deck.Draw();
            deck.Draw();
            deck.Discard(first);

            int drawCount = deck.DrawCount;
            int discardCount = deck.DiscardCount;
            int cardsInPlayCount = deck.CardsInPlayCount;

            Assert.That(deck.AvailableCardCount, Is.EqualTo(2));
            Assert.That(deck.CanDraw(2), Is.True);
            Assert.That(deck.CanDraw(3), Is.False);
            Assert.That(deck.DrawCount, Is.EqualTo(drawCount));
            Assert.That(deck.DiscardCount, Is.EqualTo(discardCount));
            Assert.That(deck.CardsInPlayCount, Is.EqualTo(cardsInPlayCount));
        }

        [Test]
        public void BA01_DeckRejectsNegativeCapacityRequests()
        {
            BlackjackDeck deck = BlackjackDeck.CreateStandard(seed: 1);

            Assert.Throws<ArgumentOutOfRangeException>(() => deck.CanDraw(-1));
            Assert.That(deck.CanDraw(0), Is.True);
        }

        [Test]
        public void BA01_ChangeSelectionRejectsDuplicateCardIdentity()
        {
            var previous = new BlackjackCard(0, 2);
            var firstCandidate = new BlackjackCard(1, 3);
            var duplicatedCandidate = new BlackjackCard(1, 8);

            Assert.Throws<ArgumentException>(() =>
                new PlayerChangeSelection(previous, firstCandidate, duplicatedCandidate));
        }

        [Test]
        public void BA01_InvalidCandidateIndexLeavesSelectionPending()
        {
            PlayerChangeSelection selection = CreateSelection();

            bool selected = selection.TrySelectCandidate(2);

            Assert.That(selected, Is.False);
            Assert.That(selection.IsCompleted, Is.False);
            Assert.That(selection.SelectedCard, Is.Null);
            Assert.That(selection.DiscardedCards, Is.Empty);
            Assert.That(selection.Candidates.Count, Is.EqualTo(2));
        }

        [Test]
        public void BA01_SelectionCompletesOnceAndPartitionsAllCards()
        {
            var previous = new BlackjackCard(0, 2);
            var firstCandidate = new BlackjackCard(1, 3);
            var secondCandidate = new BlackjackCard(2, 8);
            var selection = new PlayerChangeSelection(previous, firstCandidate, secondCandidate);

            bool selected = selection.TrySelectCandidate(1);
            bool selectedAgain = selection.TrySelectCandidate(0);

            Assert.That(selected, Is.True);
            Assert.That(selectedAgain, Is.False);
            Assert.That(selection.IsCompleted, Is.True);
            Assert.That(selection.SelectedCard, Is.SameAs(secondCandidate));
            Assert.That(selection.SelectedCard.IsFaceUp, Is.False);
            Assert.That(selection.DiscardedCards, Is.EquivalentTo(new[] { previous, firstCandidate }));
            Assert.That(
                selection.DiscardedCards.Append(selection.SelectedCard).Select(card => card.Id),
                Is.EquivalentTo(new[] { 0, 1, 2 }));
        }

        private static PlayerChangeSelection CreateSelection()
        {
            return new PlayerChangeSelection(
                new BlackjackCard(0, 2),
                new BlackjackCard(1, 3),
                new BlackjackCard(2, 8));
        }
    }
}
