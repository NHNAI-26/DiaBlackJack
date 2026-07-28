using System;

namespace DiaBlackJack.CoreLoop
{
    public sealed class CowardlyGamblerEnemyPolicy : IEnemyBehaviorPolicy
    {
        public const int StandThreshold = 14;

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
                    return Score(candidate, -500, "cowardly-gambler-keep-manual-card");
                case EnemyActionType.DemonContract:
                    return Score(candidate, -1000, "cowardly-gambler-has-no-contract");
                default:
                    throw new ArgumentOutOfRangeException(nameof(candidate));
            }
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
