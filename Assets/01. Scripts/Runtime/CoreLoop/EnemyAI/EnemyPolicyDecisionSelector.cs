using System;
using System.Collections.Generic;

namespace DiaBlackJack.CoreLoop
{
    internal static class EnemyPolicyDecisionSelector
    {
        public static EnemyDecision Select(
            EnemyObservation observation,
            Func<EnemyObservation, EnemyActionCandidate, EnemyActionScore> evaluate)
        {
            if (observation == null)
            {
                throw new ArgumentNullException(nameof(observation));
            }

            if (evaluate == null)
            {
                throw new ArgumentNullException(nameof(evaluate));
            }

            if (observation.ActionCandidates.Count == 0)
            {
                throw new InvalidOperationException(
                    "Specialized enemy policy requires at least one valid action candidate.");
            }

            var scores = new List<EnemyActionScore>(observation.ActionCandidates.Count);
            EnemyActionScore selected = null;
            foreach (EnemyActionCandidate candidate in observation.ActionCandidates)
            {
                EnemyActionScore score = evaluate(observation, candidate) ??
                    throw new InvalidOperationException(
                        "Enemy policy returned no candidate score.");
                if (!ReferenceEquals(score.Candidate, candidate))
                {
                    throw new InvalidOperationException(
                        "Enemy policy score does not match its candidate.");
                }

                scores.Add(score);
                if (selected == null || score.Score > selected.Score)
                {
                    selected = score;
                }
            }

            return EnemyDecision.FromCandidate(
                selected.Candidate,
                selected.ReasonCode,
                scores);
        }
    }
}
