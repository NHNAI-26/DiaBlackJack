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
}
