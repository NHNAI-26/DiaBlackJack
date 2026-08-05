using System;
using System.Collections.Generic;

namespace DiaBlackJack.CoreLoop
{
    public sealed class GunslingerEnemyPolicy : IEnemyBehaviorPolicy
    {
        public const int MinimumAutoPistolConfidencePercent = 50;
        public const int LowConfidenceAutoPistolUsePercent = 25;
        public const int StandThreshold = 17;

        private readonly HashSet<int> _declaredNumbers = new HashSet<int>();
        private int _trackedRoundNumber = -1;
        private int _trackedPlayerChangeCount = -1;

        public EnemyDecision Decide(EnemyObservation observation)
        {
            ResetDeclaredNumbersIfHiddenCardChanged(observation);

            EnemyDecision decision = EnemyPolicyDecisionSelector.Select(
                observation,
                (state, candidate) => Evaluate(state, candidate, _declaredNumbers));

            if (observation.PendingCardEffectKind == CardEffectKind.AutoPistol &&
                decision.ActionType == EnemyActionType.UseCard &&
                decision.CardEffectOptionId.HasValue)
            {
                _declaredNumbers.Add(decision.CardEffectOptionId.Value);
            }

            return decision;
        }

        private void ResetDeclaredNumbersIfHiddenCardChanged(EnemyObservation observation)
        {
            int playerChangeCount = CountPlayerChanges(observation);
            if (observation.RoundNumber != _trackedRoundNumber ||
                playerChangeCount != _trackedPlayerChangeCount)
            {
                _declaredNumbers.Clear();
            }

            _trackedRoundNumber = observation.RoundNumber;
            _trackedPlayerChangeCount = playerChangeCount;
        }

        private static int CountPlayerChanges(EnemyObservation observation)
        {
            int count = 0;
            foreach (PublicCombatAction action in observation.PublicActionHistory)
            {
                if (action.ActorSide == CombatantSide.Player &&
                    action.ActionType == PublicCombatActionType.Change)
                {
                    count++;
                }
            }

            return count;
        }

        private static EnemyActionScore Evaluate(
            EnemyObservation observation,
            EnemyActionCandidate candidate,
            HashSet<int> declaredNumbers)
        {
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
                case EnemyActionType.UseCard:
                    return EvaluateCard(observation, candidate, declaredNumbers);
                case EnemyActionType.Change:
                    return EnemyChangeRiskEvaluator.ShouldAcceptChange(observation)
                        ? Score(candidate, 2000, "gunslinger-required-change")
                        : Score(candidate, -50, "gunslinger-decline-risky-paid-change");
                default:
                    throw new ArgumentOutOfRangeException(nameof(candidate));
            }
        }

        private static EnemyActionScore EvaluateCard(
            EnemyObservation observation,
            EnemyActionCandidate candidate,
            HashSet<int> declaredNumbers)
        {
            if (!IsAutoPistol(candidate.CardDefinitionKey))
            {
                return Score(candidate, -500, "gunslinger-ignore-non-pistol-card");
            }

            if (observation.PendingCardEffectKind == CardEffectKind.AutoPistol)
            {
                int number = candidate.CardEffectOptionNumericValue ?? 0;
                if (declaredNumbers.Contains(number))
                {
                    return Score(candidate, -1000, "gunslinger-avoid-repeated-number");
                }

                int optionProbability = FindProbability(observation, number);
                EnemyNumberInference? bestUntried = FindMostLikelyUntried(
                    observation,
                    declaredNumbers);
                bool isBestGuess = bestUntried.HasValue &&
                    number == bestUntried.Value.Number;
                return Score(
                    candidate,
                    isBestGuess ? 2000 + optionProbability : optionProbability,
                    isBestGuess
                        ? "gunslinger-declare-most-likely-number"
                        : "gunslinger-declare-lower-probability-number");
            }

            if (HasActiveSatanContract(observation))
            {
                return Score(
                    candidate,
                    -900,
                    "gunslinger-hold-pistol-during-satan-contract");
            }

            EnemyNumberInference? mostLikely = FindMostLikelyUntried(
                observation,
                declaredNumbers);
            if (!mostLikely.HasValue)
            {
                return Score(candidate, -600, "gunslinger-no-untried-numbers-remaining");
            }

            int opponentVisibleTotal = CalculateBestTotal(observation.PlayerFaceUpCards);
            bool alreadyWinningWithoutForcingBust =
                observation.PlayerIsStanding &&
                observation.OwnHandValue.Total <= 21 &&
                opponentVisibleTotal + mostLikely.Value.Number >= 22;
            if (alreadyWinningWithoutForcingBust)
            {
                return Score(
                    candidate,
                    -400,
                    "gunslinger-hold-pistol-already-winning-at-showdown");
            }

            bool hasEnoughConfidence = mostLikely.Value.ProbabilityPercent >=
                MinimumAutoPistolConfidencePercent;
            bool firesBeforeStand = observation.OwnHandValue.Total >=
                StandThreshold;
            bool takesLowConfidenceShot =
                (uint)observation.DecisionSeed % 100u <
                    LowConfidenceAutoPistolUsePercent;
            bool usesPistol = hasEnoughConfidence ||
                firesBeforeStand ||
                takesLowConfidenceShot;
            int probability = mostLikely.Value.ProbabilityPercent;
            string reason = hasEnoughConfidence
                ? "gunslinger-use-pistol-at-high-confidence"
                : firesBeforeStand
                    ? "gunslinger-fire-pistol-before-stand"
                    : takesLowConfidenceShot
                        ? "gunslinger-risk-low-confidence-shot"
                        : "gunslinger-hold-pistol-at-low-confidence";
            return Score(
                candidate,
                usesPistol ? 1500 + probability : -200,
                reason);
        }

        private static EnemyNumberInference? FindMostLikelyUntried(
            EnemyObservation observation,
            HashSet<int> declaredNumbers)
        {
            EnemyNumberInference? selected = null;
            foreach (EnemyNumberInference inference in observation.NumberInferences)
            {
                if (declaredNumbers.Contains(inference.Number))
                {
                    continue;
                }

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

        private static bool HasActiveSatanContract(EnemyObservation observation)
        {
            foreach (EnemyActionCandidate candidate in observation.ActionCandidates)
            {
                if (candidate.ActionType == EnemyActionType.DemonContract &&
                    candidate.DemonContractKind == DemonContractKind.Satan)
                {
                    return true;
                }
            }

            return false;
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
