using System;
using System.Collections.Generic;

namespace DiaBlackJack.CoreLoop
{
    public enum EnemyAction
    {
        Hit,
        Stand
    }

    public sealed class SimpleEnemyPolicy : IEnemyBehaviorPolicy
    {
        public EnemyAction Decide(HandValue ownHand)
        {
            return ownHand.Total <= 16 ? EnemyAction.Hit : EnemyAction.Stand;
        }

        public EnemyDecision Decide(EnemyObservation observation)
        {
            if (observation == null)
            {
                throw new ArgumentNullException(nameof(observation));
            }

            if (observation.ActionCandidates.Count > 0)
            {
                return DecideFromCandidates(observation);
            }

            EnemyAction legacyAction = Decide(observation.OwnHandValue);
            return legacyAction == EnemyAction.Hit
                ? new EnemyDecision(EnemyActionType.Hit, "simple-hit-at-sixteen-or-less")
                : new EnemyDecision(EnemyActionType.Stand, "simple-stand-at-seventeen-or-more");
        }

        private static EnemyDecision DecideFromCandidates(EnemyObservation observation)
        {
            var scores = new List<EnemyActionScore>(observation.ActionCandidates.Count);
            EnemyActionScore selectedScore = null;

            foreach (EnemyActionCandidate candidate in observation.ActionCandidates)
            {
                int score = Score(observation.OwnHandValue, candidate);
                var candidateScore = new EnemyActionScore(
                    candidate,
                    score,
                    $"simple-{candidate.ActionType.ToString().ToLowerInvariant()}");
                scores.Add(candidateScore);

                if (selectedScore == null || candidateScore.Score > selectedScore.Score)
                {
                    selectedScore = candidateScore;
                }
            }

            return EnemyDecision.FromCandidate(
                selectedScore.Candidate,
                selectedScore.ReasonCode,
                scores);
        }

        private static int Score(HandValue ownHand, EnemyActionCandidate candidate)
        {
            switch (candidate.ActionType)
            {
                case EnemyActionType.Hit:
                    return ownHand.Total <= 16 ? 100 : 0;
                case EnemyActionType.Stand:
                    return ownHand.Total >= 17 ? 100 : 0;
                case EnemyActionType.UseCard:
                    return -1000;
                default:
                    throw new ArgumentOutOfRangeException(nameof(candidate));
            }
        }
    }
}
