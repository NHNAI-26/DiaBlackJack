using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DiaBlackJack.CoreLoop
{
    public readonly struct EnemyInferenceDisplayEntry
    {
        public EnemyInferenceDisplayEntry(int number, int? probabilityPercent)
        {
            if (number < 1 || number > 10)
            {
                throw new ArgumentOutOfRangeException(nameof(number));
            }

            if (probabilityPercent.HasValue &&
                (probabilityPercent.Value < 0 || probabilityPercent.Value > 100))
            {
                throw new ArgumentOutOfRangeException(nameof(probabilityPercent));
            }

            Number = number;
            ProbabilityPercent = probabilityPercent;
        }

        public int Number { get; }

        public int? ProbabilityPercent { get; }
    }

    public sealed class EnemyCombatDisplaySnapshot
    {
        internal EnemyCombatDisplaySnapshot(
            string profileKey,
            string displayName,
            EnemyGrade? grade,
            string summary,
            EnemyInformationMode? informationMode,
            IEnumerable<EnemyInferenceDisplayEntry> inferenceEntries,
            EnemyInferenceConfidence? confidence,
            FinalBossPhase? bossPhase,
            BossInferenceDirection? bossInferenceDirection,
            BossTelegraphedAction? bossTelegraphedAction)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException(
                    "Enemy display name cannot be empty.",
                    nameof(displayName));
            }

            if (string.IsNullOrWhiteSpace(summary))
            {
                throw new ArgumentException(
                    "Enemy summary cannot be empty.",
                    nameof(summary));
            }

            if (inferenceEntries == null)
            {
                throw new ArgumentNullException(nameof(inferenceEntries));
            }

            ProfileKey = profileKey;
            DisplayName = displayName;
            Grade = grade;
            Summary = summary;
            InformationMode = informationMode;
            InferenceEntries = new ReadOnlyCollection<EnemyInferenceDisplayEntry>(
                new List<EnemyInferenceDisplayEntry>(inferenceEntries));
            Confidence = confidence;
            BossPhase = bossPhase;
            BossInferenceDirection = bossInferenceDirection;
            BossTelegraphedAction = bossTelegraphedAction;
        }

        public FinalBossPhase? BossPhase { get; }

        public BossInferenceDirection? BossInferenceDirection { get; }

        public BossTelegraphedAction? BossTelegraphedAction { get; }

        public EnemyInferenceConfidence? Confidence { get; }

        public string DisplayName { get; }

        public EnemyGrade? Grade { get; }

        public bool HasProfile => ProfileKey != null;

        public IReadOnlyList<EnemyInferenceDisplayEntry> InferenceEntries { get; }

        public EnemyInformationMode? InformationMode { get; }

        public string ProfileKey { get; }

        public string Summary { get; }
    }
}
