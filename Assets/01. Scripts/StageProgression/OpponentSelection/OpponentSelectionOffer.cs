using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DiaBlackJack.CoreLoop;

namespace DiaBlackJack.StageProgression
{
    public sealed class OpponentSelectionOffer
    {
        private readonly ReadOnlyCollection<OpponentSelectionCandidate> _candidates;

        public OpponentSelectionOffer(
            int offerId,
            int stageIndex,
            IEnumerable<OpponentSelectionCandidate> candidates)
        {
            if (offerId < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(offerId),
                    "Opponent offer id cannot be negative.");
            }

            if (stageIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(stageIndex),
                    "Opponent offer stage index cannot be negative.");
            }

            if (candidates == null)
            {
                throw new ArgumentNullException(nameof(candidates));
            }

            var copiedCandidates = new List<OpponentSelectionCandidate>();
            var knownProfileKeys = new HashSet<string>(StringComparer.Ordinal);
            int eliteCount = 0;
            foreach (OpponentSelectionCandidate candidate in candidates)
            {
                if (candidate == null)
                {
                    throw new ArgumentException(
                        "An opponent offer cannot contain a null candidate.",
                        nameof(candidates));
                }

                if (!knownProfileKeys.Add(candidate.ProfileKey))
                {
                    throw new ArgumentException(
                        $"Opponent profile '{candidate.ProfileKey}' is duplicated.",
                        nameof(candidates));
                }

                switch (candidate.Preview.Grade)
                {
                    case EnemyGrade.Normal:
                        break;
                    case EnemyGrade.Elite:
                        eliteCount++;
                        break;
                    case EnemyGrade.Boss:
                        throw new ArgumentException(
                            "Boss profiles cannot appear in an opponent offer.",
                            nameof(candidates));
                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(candidates),
                            "Opponent candidate grade is invalid.");
                }

                copiedCandidates.Add(candidate);
            }

            if (copiedCandidates.Count != 2)
            {
                throw new ArgumentException(
                    "An opponent offer must contain exactly two candidates.",
                    nameof(candidates));
            }

            if (eliteCount > 1)
            {
                throw new ArgumentException(
                    "An opponent offer cannot contain more than one elite.",
                    nameof(candidates));
            }

            OfferId = offerId;
            StageIndex = stageIndex;
            _candidates = copiedCandidates.AsReadOnly();
        }

        public IReadOnlyList<OpponentSelectionCandidate> Candidates => _candidates;

        public int OfferId { get; }

        public int StageIndex { get; }
    }
}
