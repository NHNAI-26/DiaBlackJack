using System;

namespace DiaBlackJack.CoreLoop
{
    public enum RoundTransitionCause
    {
        ResurrectionHerb
    }

    public readonly struct RoundTransition
    {
        public RoundTransition(
            RoundTransitionCause cause,
            int previousRoundNumber,
            int newRoundNumber,
            int sourceCardId,
            CombatantSide ownerSide)
        {
            if (!Enum.IsDefined(typeof(RoundTransitionCause), cause))
            {
                throw new ArgumentOutOfRangeException(nameof(cause));
            }

            if (previousRoundNumber <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(previousRoundNumber));
            }

            if (newRoundNumber != previousRoundNumber + 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(newRoundNumber));
            }

            if (sourceCardId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceCardId));
            }

            if (!Enum.IsDefined(typeof(CombatantSide), ownerSide))
            {
                throw new ArgumentOutOfRangeException(nameof(ownerSide));
            }

            Cause = cause;
            PreviousRoundNumber = previousRoundNumber;
            NewRoundNumber = newRoundNumber;
            SourceCardId = sourceCardId;
            OwnerSide = ownerSide;
        }

        public RoundTransitionCause Cause { get; }

        public int PreviousRoundNumber { get; }

        public int NewRoundNumber { get; }

        public int SourceCardId { get; }

        public CombatantSide OwnerSide { get; }

        public bool HasWinner => false;

        public bool AppliesDamage => false;

        public bool CancelsContinuation => true;
    }
}
