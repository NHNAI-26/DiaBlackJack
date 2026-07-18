using System;

namespace DiaBlackJack.CoreLoop
{
    public sealed class BattleParticipant
    {
        public BattleParticipant(BlackjackDeck deck, int maximumSoul)
        {
            Deck = deck ?? throw new ArgumentNullException(nameof(deck));
            Hand = new BlackjackHand();
            Soul = new SoulPool(maximumSoul);
        }

        public BlackjackDeck Deck { get; }

        public BlackjackHand Hand { get; }

        public SoulPool Soul { get; }

        public bool IsStanding { get; private set; }

        public HandValue HandValue => HandValueCalculator.Calculate(Hand.Cards);

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

        internal void ClearRound()
        {
            Deck.Discard(Hand.TakeAll());
            IsStanding = false;
        }
    }
}
