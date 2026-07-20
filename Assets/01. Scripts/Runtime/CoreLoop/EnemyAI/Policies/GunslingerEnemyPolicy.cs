using System;

namespace DiaBlackJack.CoreLoop
{
    public sealed class GunslingerEnemyPolicy : IEnemyBehaviorPolicy
    {
        public const int MinimumAutoPistolConfidencePercent = 50;

        public EnemyDecision Decide(EnemyObservation observation)
        {
            return EnemyPolicyDecisionSelector.Select(observation, Evaluate);
        }

        private static EnemyActionScore Evaluate(
            EnemyObservation observation,
            EnemyActionCandidate candidate)
        {
            EnemyNumberInference? mostLikely = FindMostLikely(observation);
            switch (candidate.ActionType)
            {
                case EnemyActionType.Hit:
                    return Score(
                        candidate,
                        observation.OwnHandValue.Total <= 16 ? 500 : 50,
                        "gunslinger-basic-hit");
                case EnemyActionType.Stand:
                    return Score(
                        candidate,
                        observation.OwnHandValue.Total >= 17 ? 600 : 100,
                        "gunslinger-basic-stand");
                case EnemyActionType.Fold:
                    return Score(candidate, -300, "gunslinger-avoid-fold");
                case EnemyActionType.UseCard:
                    return EvaluateCard(observation, candidate, mostLikely);
                default:
                    throw new ArgumentOutOfRangeException(nameof(candidate));
            }
        }

        private static EnemyActionScore EvaluateCard(
            EnemyObservation observation,
            EnemyActionCandidate candidate,
            EnemyNumberInference? mostLikely)
        {
            if (!IsAutoPistol(candidate.CardDefinitionKey))
            {
                return Score(candidate, -500, "gunslinger-ignore-non-pistol-card");
            }

            if (observation.PendingCardEffectKind == CardEffectKind.AutoPistol)
            {
                int optionProbability = FindProbability(
                    observation,
                    candidate.CardEffectOptionNumericValue);
                bool isBestGuess = mostLikely.HasValue &&
                    candidate.CardEffectOptionNumericValue == mostLikely.Value.Number;
                return Score(
                    candidate,
                    isBestGuess ? 2000 + optionProbability : optionProbability,
                    isBestGuess
                        ? "gunslinger-declare-most-likely-number"
                        : "gunslinger-declare-lower-probability-number");
            }

            bool hasEnoughConfidence = mostLikely.HasValue &&
                mostLikely.Value.ProbabilityPercent >=
                    MinimumAutoPistolConfidencePercent;
            return Score(
                candidate,
                hasEnoughConfidence
                    ? 1500 + mostLikely.Value.ProbabilityPercent
                    : -200,
                hasEnoughConfidence
                    ? "gunslinger-use-pistol-at-high-confidence"
                    : "gunslinger-hold-pistol-at-low-confidence");
        }

        private static EnemyNumberInference? FindMostLikely(EnemyObservation observation)
        {
            EnemyNumberInference? selected = null;
            foreach (EnemyNumberInference inference in observation.NumberInferences)
            {
                if (!selected.HasValue ||
                    inference.ProbabilityPercent > selected.Value.ProbabilityPercent ||
                    (inference.ProbabilityPercent == selected.Value.ProbabilityPercent &&
                        inference.Number < selected.Value.Number))
                {
                    selected = inference;
                }
            }

            return selected;
        }

        private static int FindProbability(
            EnemyObservation observation,
            int? number)
        {
            if (!number.HasValue)
            {
                return 0;
            }

            foreach (EnemyNumberInference inference in observation.NumberInferences)
            {
                if (inference.Number == number.Value)
                {
                    return inference.ProbabilityPercent;
                }
            }

            return 0;
        }

        private static bool IsAutoPistol(string definitionKey)
        {
            return definitionKey != null &&
                CardDefinitionCatalog.GetByKey(definitionKey).Effect ==
                    CardEffectKind.AutoPistol;
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
