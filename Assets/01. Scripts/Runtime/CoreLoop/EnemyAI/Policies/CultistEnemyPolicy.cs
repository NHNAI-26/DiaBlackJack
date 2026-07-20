using System;

namespace DiaBlackJack.CoreLoop
{
    public sealed class CultistEnemyPolicy : IEnemyBehaviorPolicy
    {
        public const int AggressiveHitCeiling = 18;

        public EnemyDecision Decide(EnemyObservation observation)
        {
            return EnemyPolicyDecisionSelector.Select(observation, Evaluate);
        }

        private static EnemyActionScore Evaluate(
            EnemyObservation observation,
            EnemyActionCandidate candidate)
        {
            switch (candidate.ActionType)
            {
                case EnemyActionType.Hit:
                    return Score(
                        candidate,
                        observation.OwnHandValue.Total <= AggressiveHitCeiling
                            ? 700
                            : 100,
                        "cultist-accept-hit-risk");
                case EnemyActionType.Stand:
                    return Score(
                        candidate,
                        observation.OwnHandValue.Total > AggressiveHitCeiling
                            ? 700
                            : 350,
                        "cultist-delay-safe-stand");
                case EnemyActionType.UseCard:
                    return Score(candidate, 500, "cultist-use-implemented-aggression-card");
                default:
                    throw new ArgumentOutOfRangeException(nameof(candidate));
            }
        }

        private static EnemyActionScore Score(
            EnemyActionCandidate candidate,
            int score,
            string reason)
        {
            return new EnemyActionScore(candidate, score, reason);
        }
    }
}
