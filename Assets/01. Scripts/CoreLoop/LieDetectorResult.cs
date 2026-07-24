using System;

namespace DiaBlackJack.CoreLoop
{
    public readonly struct LieDetectorPublicResult
    {
        internal LieDetectorPublicResult(
            int sourceCardId,
            CombatantSide ownerSide,
            int declaredNumber,
            bool wasComparable)
        {
            if (sourceCardId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceCardId));
            }

            if (!Enum.IsDefined(typeof(CombatantSide), ownerSide))
            {
                throw new ArgumentOutOfRangeException(nameof(ownerSide));
            }

            if (declaredNumber < 1 || declaredNumber > 10)
            {
                throw new ArgumentOutOfRangeException(nameof(declaredNumber));
            }

            SourceCardId = sourceCardId;
            OwnerSide = ownerSide;
            DeclaredNumber = declaredNumber;
            WasComparable = wasComparable;
        }

        public int SourceCardId { get; }

        public CombatantSide OwnerSide { get; }

        public int DeclaredNumber { get; }

        public bool WasComparable { get; }
    }

    public readonly struct HiddenCardComparisonKnowledge
    {
        internal HiddenCardComparisonKnowledge(
            CombatantSide observerSide,
            CombatantSide subjectSide,
            int subjectHiddenCardId,
            int declaredNumber,
            bool isAtLeastDeclaredNumber,
            int roundNumber)
        {
            if (!Enum.IsDefined(typeof(CombatantSide), observerSide))
            {
                throw new ArgumentOutOfRangeException(nameof(observerSide));
            }

            if (!Enum.IsDefined(typeof(CombatantSide), subjectSide) ||
                subjectSide == observerSide)
            {
                throw new ArgumentOutOfRangeException(nameof(subjectSide));
            }

            if (subjectHiddenCardId < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(subjectHiddenCardId));
            }

            if (declaredNumber < 1 || declaredNumber > 10)
            {
                throw new ArgumentOutOfRangeException(nameof(declaredNumber));
            }

            if (roundNumber <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(roundNumber));
            }

            ObserverSide = observerSide;
            SubjectSide = subjectSide;
            SubjectHiddenCardId = subjectHiddenCardId;
            DeclaredNumber = declaredNumber;
            IsAtLeastDeclaredNumber = isAtLeastDeclaredNumber;
            RoundNumber = roundNumber;
        }

        public CombatantSide ObserverSide { get; }

        public CombatantSide SubjectSide { get; }

        internal int SubjectHiddenCardId { get; }

        public int DeclaredNumber { get; }

        public bool IsAtLeastDeclaredNumber { get; }

        public int RoundNumber { get; }
    }
}
