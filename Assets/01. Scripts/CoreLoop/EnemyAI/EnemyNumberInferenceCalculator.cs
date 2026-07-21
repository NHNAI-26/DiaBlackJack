using System;
using System.Collections.Generic;

namespace DiaBlackJack.CoreLoop
{
    internal static class EnemyNumberInferenceCalculator
    {
        public static IReadOnlyList<EnemyNumberInference> Calculate(
            IReadOnlyList<int> knownRankCounts,
            IReadOnlyList<PublicCardObservation> playerFaceUpCards,
            IReadOnlyList<PublicCardObservation> playerDiscardedCards,
            int playerHiddenCardCount)
        {
            if (knownRankCounts == null)
            {
                throw new ArgumentNullException(nameof(knownRankCounts));
            }

            if (knownRankCounts.Count < 11)
            {
                throw new ArgumentException(
                    "Known rank counts must contain indexes 0 through 10.",
                    nameof(knownRankCounts));
            }

            if (playerFaceUpCards == null)
            {
                throw new ArgumentNullException(nameof(playerFaceUpCards));
            }

            if (playerDiscardedCards == null)
            {
                throw new ArgumentNullException(nameof(playerDiscardedCards));
            }

            if (playerHiddenCardCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(playerHiddenCardCount));
            }

            if (playerHiddenCardCount == 0)
            {
                return Array.Empty<EnemyNumberInference>();
            }

            var unknownRankCounts = new int[11];
            for (int rank = 1; rank <= 10; rank++)
            {
                if (knownRankCounts[rank] < 0)
                {
                    throw new ArgumentException(
                        "Known rank counts cannot be negative.",
                        nameof(knownRankCounts));
                }

                unknownRankCounts[rank] = knownRankCounts[rank];
            }

            RemovePublicCards(unknownRankCounts, playerFaceUpCards);
            RemovePublicCards(unknownRankCounts, playerDiscardedCards);

            int unknownCardCount = 0;
            for (int rank = 1; rank <= 10; rank++)
            {
                unknownCardCount += unknownRankCounts[rank];
            }

            if (unknownCardCount < playerHiddenCardCount)
            {
                throw new InvalidOperationException(
                    "Public card records leave fewer unknown cards than hidden cards.");
            }

            var probabilities = new List<RankProbability>();
            int allocatedPercent = 0;
            for (int rank = 1; rank <= 10; rank++)
            {
                int count = unknownRankCounts[rank];
                if (count == 0)
                {
                    continue;
                }

                int scaled = count * 100;
                int probabilityPercent = scaled / unknownCardCount;
                probabilities.Add(new RankProbability(
                    rank,
                    probabilityPercent,
                    scaled % unknownCardCount));
                allocatedPercent += probabilityPercent;
            }

            probabilities.Sort(CompareRemainder);
            int remainingPercent = 100 - allocatedPercent;
            for (int i = 0; i < remainingPercent; i++)
            {
                probabilities[i].ProbabilityPercent++;
            }

            probabilities.Sort(CompareProbability);
            var result = new List<EnemyNumberInference>(probabilities.Count);
            foreach (RankProbability probability in probabilities)
            {
                result.Add(new EnemyNumberInference(
                    probability.Rank,
                    probability.ProbabilityPercent));
            }

            return result.AsReadOnly();
        }

        private static void RemovePublicCards(
            int[] unknownRankCounts,
            IReadOnlyList<PublicCardObservation> publicCards)
        {
            foreach (PublicCardObservation card in publicCards)
            {
                if (card == null)
                {
                    throw new ArgumentException("Public cards cannot contain null.");
                }

                unknownRankCounts[card.Rank]--;
                if (unknownRankCounts[card.Rank] < 0)
                {
                    throw new InvalidOperationException(
                        "Public card records exceed the known deck composition.");
                }
            }
        }

        private static int CompareRemainder(RankProbability left, RankProbability right)
        {
            int remainderComparison = right.Remainder.CompareTo(left.Remainder);
            return remainderComparison != 0
                ? remainderComparison
                : left.Rank.CompareTo(right.Rank);
        }

        private static int CompareProbability(RankProbability left, RankProbability right)
        {
            int probabilityComparison =
                right.ProbabilityPercent.CompareTo(left.ProbabilityPercent);
            return probabilityComparison != 0
                ? probabilityComparison
                : left.Rank.CompareTo(right.Rank);
        }

        private sealed class RankProbability
        {
            public RankProbability(int rank, int probabilityPercent, int remainder)
            {
                Rank = rank;
                ProbabilityPercent = probabilityPercent;
                Remainder = remainder;
            }

            public int ProbabilityPercent { get; set; }

            public int Rank { get; }

            public int Remainder { get; }
        }
    }
}
