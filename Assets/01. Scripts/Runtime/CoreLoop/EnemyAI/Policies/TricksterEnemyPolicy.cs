using System;

namespace DiaBlackJack.CoreLoop
{
    public sealed class TricksterEnemyPolicy : IEnemyBehaviorPolicy
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
                        observation.OwnHandValue.Total <= 15 ? 500 : 100,
                        "trickster-basic-hit");
                case EnemyActionType.Stand:
                    return Score(
                        candidate,
                        observation.OwnHandValue.Total >= 16 ? 600 : 200,
                        "trickster-basic-stand");
                case EnemyActionType.Fold:
                    return Score(candidate, -300, "trickster-avoid-fold");
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
            if (!IsCrystalOrb(candidate.CardDefinitionKey))
            {
                return Score(candidate, -500, "trickster-ignore-non-orb-card");
            }

            if (observation.PendingCardEffectKind != CardEffectKind.CrystalOrb)
            {
                return Score(candidate, 1500, "trickster-prioritize-crystal-orb");
            }

            if (!candidate.CardEffectOptionCardRank.HasValue)
            {
                return Score(
                    candidate,
                    1000 + observation.OwnHandValue.Total,
                    "trickster-keep-current-hand");
            }

            int resultingTotal = CalculateTotalWithAdditionalRank(
                observation,
                candidate.CardEffectOptionCardRank.Value);
            return resultingTotal <= 21
                ? Score(
                    candidate,
                    1000 + resultingTotal,
                    "trickster-take-highest-safe-orb-card")
                : Score(
                    candidate,
                    -1000 - resultingTotal,
                    "trickster-reject-busting-orb-card");
        }

        private static int CalculateTotalWithAdditionalRank(
            EnemyObservation observation,
            int additionalRank)
        {
            int total = additionalRank;
            int aceCount = additionalRank == 1 ? 1 : 0;
            foreach (EnemyOwnedCardObservation card in observation.OwnCards)
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

        private static bool IsCrystalOrb(string definitionKey)
        {
            return definitionKey != null &&
                CardDefinitionCatalog.GetByKey(definitionKey).Effect ==
                    CardEffectKind.CrystalOrb;
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
