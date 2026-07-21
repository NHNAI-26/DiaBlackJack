using System;

namespace DiaBlackJack.CoreLoop
{
    public enum FinalBossPhase
    {
        Survival,
        Pressure,
        Execution
    }

    public enum BossInferenceDirection
    {
        Unknown,
        LowNumbers,
        Balanced,
        HighNumbers
    }

    public enum BossTelegraphedAction
    {
        None,
        NumberGuess,
        ForcedDraw
    }

    public static class FinalBossPhaseResolver
    {
        public const int ExpectedMaximumSoul = 7;

        public static FinalBossPhase Resolve(SoulObservation enemySoul)
        {
            if (enemySoul.Maximum != ExpectedMaximumSoul)
            {
                throw new InvalidOperationException(
                    "Final boss policy requires maximum soul 7.");
            }

            if (enemySoul.Current >= 5)
            {
                return FinalBossPhase.Survival;
            }

            return enemySoul.Current >= 3
                ? FinalBossPhase.Pressure
                : FinalBossPhase.Execution;
        }
    }

    public sealed class BossCombatDisplayModel
    {
        private BossCombatDisplayModel(
            FinalBossPhase phase,
            BossInferenceDirection inferenceDirection,
            EnemyInferenceConfidence confidence,
            BossTelegraphedAction telegraphedAction)
        {
            Phase = phase;
            InferenceDirection = inferenceDirection;
            Confidence = confidence;
            TelegraphedAction = telegraphedAction;
        }

        public EnemyInferenceConfidence Confidence { get; }

        public BossInferenceDirection InferenceDirection { get; }

        public FinalBossPhase Phase { get; }

        public BossTelegraphedAction TelegraphedAction { get; }

        internal static BossCombatDisplayModel Create(
            EnemyObservation observation,
            BossTelegraphedAction telegraphedAction)
        {
            if (observation == null)
            {
                throw new ArgumentNullException(nameof(observation));
            }

            if (!Enum.IsDefined(typeof(BossTelegraphedAction), telegraphedAction))
            {
                throw new ArgumentOutOfRangeException(nameof(telegraphedAction));
            }

            EnemyInferenceDisplayModel inference =
                EnemyInferenceDisplayModel.CreateForElite(
                    observation.NumberInferences);
            return new BossCombatDisplayModel(
                FinalBossPhaseResolver.Resolve(observation.EnemySoul),
                ResolveDirection(observation),
                inference.Confidence,
                telegraphedAction);
        }

        private static BossInferenceDirection ResolveDirection(
            EnemyObservation observation)
        {
            if (observation.NumberInferences.Count == 0)
            {
                return BossInferenceDirection.Unknown;
            }

            EnemyNumberInference mostLikely = observation.NumberInferences[0];
            foreach (EnemyNumberInference inference in observation.NumberInferences)
            {
                if (inference.ProbabilityPercent > mostLikely.ProbabilityPercent ||
                    (inference.ProbabilityPercent == mostLikely.ProbabilityPercent &&
                        inference.Number < mostLikely.Number))
                {
                    mostLikely = inference;
                }
            }

            if (mostLikely.Number <= 4)
            {
                return BossInferenceDirection.LowNumbers;
            }

            return mostLikely.Number >= 7
                ? BossInferenceDirection.HighNumbers
                : BossInferenceDirection.Balanced;
        }
    }
}
