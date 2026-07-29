using System;
using System.Collections.Generic;

namespace DiaBlackJack.CoreLoop
{
    public sealed class EnforcerEnemyPolicy :
        IEnemyBehaviorPolicy,
        IEnemyForcedActionPolicy
    {
        public EnemyDecision Decide(EnemyObservation observation)
        {
            return EnemyPolicyDecisionSelector.Select(observation, Evaluate);
        }

        public bool TryDecideForcedAction(
            EnemyObservation observation,
            out EnemyDecision decision)
        {
            decision = null;
            foreach (EnemyActionCandidate candidate in observation.ActionCandidates)
            {
                if (candidate.ActionType != EnemyActionType.DemonContract)
                {
                    continue;
                }

                bool startsContract = !candidate.DemonContractOptionId.HasValue &&
                    !candidate.DemonContractSourceCardId.HasValue;
                bool selectsPaimon = candidate.DemonContractInteractionKind ==
                        DemonContractInteractionKind.ChooseContract &&
                    candidate.DemonContractKind == DemonContractKind.Paimon;
                if (!startsContract && !selectsPaimon)
                {
                    continue;
                }

                decision = EnemyDecision.FromCandidate(
                    candidate,
                    startsContract
                        ? "enforcer-begin-paimon-contract"
                        : "enforcer-select-paimon-contract");
                return true;
            }

            return false;
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
                case EnemyActionType.UseCard:
                    return EvaluateCard(observation, candidate);
                case EnemyActionType.DemonContract:
                    return EvaluateDemonContract(candidate);
                case EnemyActionType.Change:
                    return Score(candidate, 2000, "enforcer-required-change");
                default:
                    throw new ArgumentOutOfRangeException(nameof(candidate));
            }
        }

        private static EnemyActionScore EvaluateDemonContract(
            EnemyActionCandidate candidate)
        {
            if (candidate.DemonContractInteractionKind ==
                DemonContractInteractionKind.PaimonChooseDeck)
            {
                bool choosesOpponentDeck = candidate.DemonContractOptionId ==
                    PaimonDemonContractHandler.OpponentDeckOptionId;
                return Score(
                    candidate,
                    choosesOpponentDeck ? 1500 : 100,
                    choosesOpponentDeck
                        ? "enforcer-inspect-opponent-deck-with-paimon"
                        : "enforcer-avoid-own-deck-paimon-exile");
            }

            if (candidate.DemonContractInteractionKind ==
                DemonContractInteractionKind.PaimonChooseExileCard)
            {
                int? rank = candidate.DemonContractOptionNumericValue;
                return Score(
                    candidate,
                    rank.HasValue && rank.Value > 0 ? 1200 + rank.Value : 0,
                    rank.HasValue && rank.Value > 0
                        ? "enforcer-exile-highest-opponent-card-with-paimon"
                        : "enforcer-skip-own-card-paimon-exile");
            }

            return Score(candidate, -1000, "enforcer-ignore-unsupported-contract");
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
                int targetRank = candidate.CardEffectOptionCardRank ?? 0;
                return Score(
                    candidate,
                    3000 + (targetRank * 10),
                    "enforcer-discard-highest-hammer-target");
            }

            int visiblePressure = CalculateBestTotal(observation.PlayerFaceUpCards);
            return Score(
                candidate,
                (observation.PlayerIsStanding ? 2100 : 1300) + visiblePressure,
                observation.PlayerIsStanding
                    ? "enforcer-remove-public-card-and-break-stand"
                    : "enforcer-remove-high-pressure-public-card");
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
