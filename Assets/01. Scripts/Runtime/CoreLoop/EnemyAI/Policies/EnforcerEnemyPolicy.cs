using System;
using System.Collections.Generic;

namespace DiaBlackJack.CoreLoop
{
    public sealed class EnforcerEnemyPolicy : IEnemyBehaviorPolicy
    {
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
                        observation.OwnHandValue.Total <= 16 ? 600 : 100,
                        "enforcer-basic-hit");
                case EnemyActionType.Stand:
                    return Score(
                        candidate,
                        observation.OwnHandValue.Total >= 17 ? 700 : 200,
                        "enforcer-basic-stand");
                case EnemyActionType.Fold:
                    return Score(candidate, -500, "enforcer-avoid-fold");
                case EnemyActionType.UseCard:
                    return EvaluateCard(observation, candidate);
                default:
                    throw new ArgumentOutOfRangeException(nameof(candidate));
            }
        }

        private static EnemyActionScore EvaluateCard(
            EnemyObservation observation,
            EnemyActionCandidate candidate)
        {
            CardEffectKind effect = CardDefinitionCatalog
                .GetByKey(candidate.CardDefinitionKey)
                .Effect;
            if (effect == CardEffectKind.ThreatHammer)
            {
                return EvaluateThreatHammer(observation, candidate);
            }

            if (effect == CardEffectKind.MilitaryKnife)
            {
                int bustChance = EstimateMilitaryKnifeBustChance(observation);
                return Score(
                    candidate,
                    1400 + (bustChance * 5),
                    "enforcer-force-hit-and-evaluate-follow-up");
            }

            return Score(candidate, -600, "enforcer-ignore-non-disruption-card");
        }

        private static EnemyActionScore EvaluateThreatHammer(
            EnemyObservation observation,
            EnemyActionCandidate candidate)
        {
            if (observation.PendingCardEffectKind == CardEffectKind.ThreatHammer)
            {
                int costRank = candidate.CardEffectOptionCardRank ?? 10;
                return Score(
                    candidate,
                    3000 - (costRank * 10),
                    "enforcer-pay-lowest-hammer-cost");
            }

            if (!observation.PlayerIsStanding)
            {
                return Score(
                    candidate,
                    -400,
                    "enforcer-hold-hammer-until-player-stands");
            }

            int visiblePressure = CalculateBestTotal(observation.PlayerFaceUpCards);
            return Score(
                candidate,
                2100 + visiblePressure,
                "enforcer-break-player-stand-and-evaluate-follow-up");
        }

        private static int EstimateMilitaryKnifeBustChance(EnemyObservation observation)
        {
            if (observation.PlayerHiddenCardCount != 1 ||
                observation.NumberInferences.Count == 0)
            {
                return 0;
            }

            long bustWeight = 0;
            long totalWeight = 0;
            foreach (EnemyNumberInference hidden in observation.NumberInferences)
            {
                foreach (EnemyNumberInference forcedDraw in observation.NumberInferences)
                {
                    int weight = hidden.ProbabilityPercent *
                        forcedDraw.ProbabilityPercent;
                    totalWeight += weight;
                    if (CalculateProjectedTotal(
                        observation.PlayerFaceUpCards,
                        hidden.Number,
                        forcedDraw.Number) > 21)
                    {
                        bustWeight += weight;
                    }
                }
            }

            return totalWeight == 0
                ? 0
                : (int)((bustWeight * 100) / totalWeight);
        }

        private static int CalculateProjectedTotal(
            IReadOnlyList<PublicCardObservation> faceUpCards,
            int hiddenRank,
            int forcedDrawRank)
        {
            int total = hiddenRank + forcedDrawRank;
            int aceCount = 0;
            if (hiddenRank == 1)
            {
                aceCount++;
            }

            if (forcedDrawRank == 1)
            {
                aceCount++;
            }

            foreach (PublicCardObservation card in faceUpCards)
            {
                total += card.Rank;
                if (card.Rank == 1)
                {
                    aceCount++;
                }
            }

            while (aceCount > 0 && total + 10 <= 21)
            {
                total += 10;
                aceCount--;
            }

            return total;
        }

        private static int CalculateBestTotal(
            IReadOnlyList<PublicCardObservation> cards)
        {
            int total = 0;
            int aceCount = 0;
            foreach (PublicCardObservation card in cards)
            {
                total += card.Rank;
                if (card.Rank == 1)
                {
                    aceCount++;
                }
            }

            while (aceCount > 0 && total + 10 <= 21)
            {
                total += 10;
                aceCount--;
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
