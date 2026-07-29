using System;
using System.Collections.Generic;

namespace DiaBlackJack.CoreLoop
{
    public sealed class PlayerChangeSelection
    {
        private static readonly IReadOnlyList<BlackjackCard> NoCards =
            Array.AsReadOnly(Array.Empty<BlackjackCard>());

        private readonly BlackjackCard[] _candidates;
        private readonly IReadOnlyList<BlackjackCard> _candidateView;
        private IReadOnlyList<BlackjackCard> _discardedCards = NoCards;

        public PlayerChangeSelection(
            BlackjackCard previousHiddenCard,
            BlackjackCard firstCandidate,
            BlackjackCard secondCandidate)
        {
            if (previousHiddenCard == null)
            {
                throw new ArgumentNullException(nameof(previousHiddenCard));
            }

            if (firstCandidate == null)
            {
                throw new ArgumentNullException(nameof(firstCandidate));
            }

            if (secondCandidate == null)
            {
                throw new ArgumentNullException(nameof(secondCandidate));
            }

            EnsureDistinctCardIds(previousHiddenCard, firstCandidate, secondCandidate);

            PreviousHiddenCardId = previousHiddenCard.Id;
            _candidates = new[] { firstCandidate, secondCandidate };
            _candidateView = Array.AsReadOnly(_candidates);
        }

        internal int PreviousHiddenCardId { get; }

        public IReadOnlyList<BlackjackCard> Candidates => _candidateView;

        public IReadOnlyList<BlackjackCard> DiscardedCards => _discardedCards;

        public bool IsCompleted { get; private set; }

        public BlackjackCard SelectedCard { get; private set; }

        public bool TrySelectCandidate(int candidateIndex)
        {
            if (IsCompleted || candidateIndex < 0 || candidateIndex >= _candidates.Length)
            {
                return false;
            }

            SelectedCard = _candidates[candidateIndex];
            SelectedCard.Conceal();

            int discardedCandidateIndex = candidateIndex == 0 ? 1 : 0;
            _discardedCards = Array.AsReadOnly(
                new[] { _candidates[discardedCandidateIndex] });
            IsCompleted = true;
            return true;
        }

        private static void EnsureDistinctCardIds(params BlackjackCard[] cards)
        {
            for (int i = 0; i < cards.Length; i++)
            {
                for (int j = i + 1; j < cards.Length; j++)
                {
                    if (cards[i].Id == cards[j].Id)
                    {
                        throw new ArgumentException("Change selection cards must have distinct ids.");
                    }
                }
            }
        }
    }

    internal static class EnemyChangeCandidateSelector
    {
        public static int Select(
            IReadOnlyList<BlackjackCard> remainingHand,
            IReadOnlyList<BlackjackCard> candidates)
        {
            if (remainingHand == null)
            {
                throw new ArgumentNullException(nameof(remainingHand));
            }

            if (candidates == null || candidates.Count != 2)
            {
                throw new ArgumentException(
                    "Enemy change requires exactly two candidates.",
                    nameof(candidates));
            }

            int firstTotal = CalculateTotal(remainingHand, candidates[0]);
            int secondTotal = CalculateTotal(remainingHand, candidates[1]);
            bool firstSafe = firstTotal <= 21;
            bool secondSafe = secondTotal <= 21;

            if (firstSafe != secondSafe)
            {
                return firstSafe ? 0 : 1;
            }

            if (firstSafe)
            {
                return secondTotal > firstTotal ? 1 : 0;
            }

            return secondTotal < firstTotal ? 1 : 0;
        }

        private static int CalculateTotal(
            IReadOnlyList<BlackjackCard> remainingHand,
            BlackjackCard candidate)
        {
            List<BlackjackCard> cards = new List<BlackjackCard>(remainingHand.Count + 1);
            foreach (BlackjackCard card in remainingHand)
            {
                cards.Add(card);
            }

            cards.Add(candidate ?? throw new ArgumentNullException(nameof(candidate)));
            return HandValueCalculator.Calculate(cards).Total;
        }
    }
}
