using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DiaBlackJack.CoreLoop
{
    public enum EnemyInferenceConfidence
    {
        Low,
        Medium,
        High
    }

    public sealed class EnemyInferenceDisplayModel
    {
        public const int HighConfidenceMinimumPercent = 60;
        public const int MediumConfidenceMinimumPercent = 35;

        private EnemyInferenceDisplayModel(
            IEnumerable<int> topNumbers,
            EnemyInferenceConfidence confidence)
        {
            TopNumbers = new ReadOnlyCollection<int>(new List<int>(topNumbers));
            Confidence = confidence;
        }

        public EnemyInferenceConfidence Confidence { get; }

        public IReadOnlyList<int> TopNumbers { get; }

        public static EnemyInferenceDisplayModel CreateForElite(
            IEnumerable<EnemyNumberInference> inferences)
        {
            if (inferences == null)
            {
                throw new ArgumentNullException(nameof(inferences));
            }

            var ordered = new List<EnemyNumberInference>(inferences);
            ordered.Sort(CompareInference);

            var topNumbers = new List<int>(Math.Min(2, ordered.Count));
            for (int i = 0; i < ordered.Count && i < 2; i++)
            {
                topNumbers.Add(ordered[i].Number);
            }

            int topProbability = ordered.Count == 0
                ? 0
                : ordered[0].ProbabilityPercent;
            EnemyInferenceConfidence confidence = topProbability >=
                HighConfidenceMinimumPercent
                    ? EnemyInferenceConfidence.High
                    : topProbability >= MediumConfidenceMinimumPercent
                        ? EnemyInferenceConfidence.Medium
                        : EnemyInferenceConfidence.Low;

            return new EnemyInferenceDisplayModel(topNumbers, confidence);
        }

        private static int CompareInference(
            EnemyNumberInference left,
            EnemyNumberInference right)
        {
            int probabilityComparison = right.ProbabilityPercent.CompareTo(
                left.ProbabilityPercent);
            return probabilityComparison != 0
                ? probabilityComparison
                : left.Number.CompareTo(right.Number);
        }
    }
}
