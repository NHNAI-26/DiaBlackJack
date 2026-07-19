using System;
using System.Collections.Generic;

namespace DiaBlackJack.CoreLoop
{
    public sealed class BattleParticipant
    {
        public BattleParticipant(BlackjackDeck deck, int maximumSoul)
            : this(deck, maximumSoul, maximumSoul)
        {
        }

        public BattleParticipant(BlackjackDeck deck, int maximumSoul, int currentSoul)
        {
            Deck = deck ?? throw new ArgumentNullException(nameof(deck));
            Hand = new BlackjackHand();
            Soul = new SoulPool(maximumSoul, currentSoul);
        }

        public BlackjackDeck Deck { get; }

        public BlackjackHand Hand { get; }

        public SoulPool Soul { get; }

        public bool IsStanding { get; private set; }

        public HandValue HandValue => HandValueCalculator.Calculate(Hand.Cards);

        public HandValue VisibleHandValue => HandValueCalculator.Calculate(GetVisibleCards());

        internal BlackjackCard Draw(bool faceUp)
        {
            BlackjackCard card = Deck.Draw();
            card.Conceal();
            if (faceUp)
            {
                card.Reveal();
            }

            Hand.Add(card);
            return card;
        }

        internal void Stand()
        {
            IsStanding = true;
        }

        internal bool TryBeginChange(out PlayerChangeSelection selection)
        {
            selection = null;
            if (Hand.HiddenCardCount != 1 || !Deck.CanDraw(2))
            {
                return false;
            }

            if (!Hand.TryTakeSingleHiddenCard(out BlackjackCard previousHiddenCard))
            {
                return false;
            }

            BlackjackCard firstCandidate = Deck.Draw();
            BlackjackCard secondCandidate = Deck.Draw();
            firstCandidate.Reveal();
            secondCandidate.Reveal();

            selection = new PlayerChangeSelection(
                previousHiddenCard,
                firstCandidate,
                secondCandidate);
            return true;
        }

        internal void CompleteChange(PlayerChangeSelection selection)
        {
            if (selection == null)
            {
                throw new ArgumentNullException(nameof(selection));
            }

            if (!selection.IsCompleted)
            {
                throw new InvalidOperationException("Change selection must be completed first.");
            }

            Hand.Add(selection.SelectedCard);
            Deck.Discard(selection.DiscardedCards);
        }

        internal void ClearRound()
        {
            Deck.Discard(Hand.TakeAll());
            IsStanding = false;
        }

        private IEnumerable<BlackjackCard> GetVisibleCards()
        {
            foreach (BlackjackCard card in Hand.Cards)
            {
                if (card.IsFaceUp)
                {
                    yield return card;
                }
            }
        }
    }
}
