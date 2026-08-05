using System;

namespace DiaBlackJack.CoreLoop
{
    public sealed class CowardlyGamblerEnemyPolicy : IEnemyBehaviorPolicy
    {
        public const int StandThreshold = 15;

        public EnemyDecision Decide(EnemyObservation observation)
        {
            return EnemyPolicyDecisionSelector.Select(observation, Evaluate);
        }

        private static EnemyActionScore Evaluate(
            EnemyObservation observation,
            EnemyActionCandidate candidate)
        {
            int faceUpTotal = GetOwnFaceUpTotal(observation);
            switch (candidate.ActionType)
            {
                case EnemyActionType.Hit:
                    return Score(
                        candidate,
                        faceUpTotal < StandThreshold ? 600 : 50,
                        "cowardly-gambler-basic-hit");
                case EnemyActionType.Stand:
                    return Score(
                        candidate,
                        faceUpTotal >= StandThreshold ? 700 : 100,
                        "cowardly-gambler-early-stand");
                case EnemyActionType.UseCard:
                    return EvaluateCard(observation, candidate);
                case EnemyActionType.DemonContract:
                    return Score(candidate, -1000, "cowardly-gambler-has-no-contract");
                case EnemyActionType.Change:
                    return EnemyChangeRiskEvaluator.ShouldAcceptChange(observation)
                        ? Score(candidate, 2000, "cowardly-gambler-required-change")
                        : Score(candidate, -50, "cowardly-gambler-decline-risky-paid-change");
                default:
                    throw new ArgumentOutOfRangeException(nameof(candidate));
            }
        }

        private static EnemyActionScore EvaluateCard(
            EnemyObservation observation,
            EnemyActionCandidate candidate)
        {
            if (observation.PendingCardEffectKind.HasValue)
            {
                int choiceValue = candidate.CardEffectOptionCardRank ??
                    candidate.CardEffectOptionNumericValue ?? 0;
                return Score(
                    candidate,
                    1200 + choiceValue,
                    "cowardly-gambler-complete-manual-card");
            }

            bool usesCard = (uint)observation.DecisionSeed % 100u < 15u;
            return Score(
                candidate,
                usesCard ? 900 : -500,
                usesCard
                    ? "cowardly-gambler-low-chance-manual-card"
                    : "cowardly-gambler-keep-manual-card");
        }

        private static int GetOwnFaceUpTotal(EnemyObservation observation)
        {
            int total = 0;
            foreach (EnemyOwnedCardObservation card in observation.OwnCards)
            {
                if (card.IsFaceUp && !card.IsHiddenCard)
                {
                    total += card.Rank;
                }
            }

            return total;
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
