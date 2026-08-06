using System;
using System.Collections.Generic;

namespace DiaBlackJack.CoreLoop
{
    public sealed class FinalBossEnemyPolicy : IEnemyBehaviorPolicy
    {
        private BossTelegraphedAction _telegraphedAction;
        private int _telegraphPlayerActionCount = -1;
        private int _telegraphRoundNumber = -1;
        private readonly HashSet<int> _declaredNumbers = new HashSet<int>();
        private int _trackedRoundNumber = -1;
        private int _trackedPlayerChangeCount = -1;

        public BossCombatDisplayModel CurrentDisplay { get; private set; }

        public EnemyDecision Decide(EnemyObservation observation)
        {
            if (observation == null)
            {
                throw new ArgumentNullException(nameof(observation));
            }

            ResetDeclaredNumbersIfHiddenCardChanged(observation);
            EnemyDecision decision = DecideCore(observation);
            if (observation.PendingCardEffectKind == CardEffectKind.AutoPistol &&
                decision.ActionType == EnemyActionType.UseCard &&
                decision.CardEffectOptionId.HasValue)
            {
                _declaredNumbers.Add(decision.CardEffectOptionId.Value);
            }

            return decision;
        }

        private EnemyDecision DecideCore(EnemyObservation observation)
        {
            if (HasPendingDemonContractChoice(observation))
            {
                ClearTelegraph();
                CurrentDisplay = BossCombatDisplayModel.Create(
                    observation,
                    BossTelegraphedAction.None);
                return EnemyPolicyDecisionSelector.Select(
                    observation,
                    EvaluateDemonContractChoice);
            }

            FinalBossPhase phase = FinalBossPhaseResolver.Resolve(
                observation.EnemySoul);
            if (observation.PendingCardEffectKind.HasValue)
            {
                ClearTelegraph();
                CurrentDisplay = BossCombatDisplayModel.Create(
                    observation,
                    BossTelegraphedAction.None);
                return EnemyPolicyDecisionSelector.Select(
                    observation,
                    (state, candidate) =>
                        EvaluatePendingCardChoice(state, candidate, _declaredNumbers));
            }

            if (phase == FinalBossPhase.Survival)
            {
                ClearTelegraph();
                CurrentDisplay = BossCombatDisplayModel.Create(
                    observation,
                    BossTelegraphedAction.None);
                return EnemyPolicyDecisionSelector.Select(
                    observation,
                    (state, candidate) =>
                        EvaluateSurvival(state, candidate, _declaredNumbers));
            }

            if (phase == FinalBossPhase.Pressure)
            {
                ClearTelegraph();
                CurrentDisplay = BossCombatDisplayModel.Create(
                    observation,
                    BossTelegraphedAction.None);
                return EnemyPolicyDecisionSelector.Select(
                    observation,
                    (state, candidate) =>
                        EvaluatePressure(state, candidate, _declaredNumbers));
            }

            return DecideExecutionPhase(observation);
        }

        private void ResetDeclaredNumbersIfHiddenCardChanged(
            EnemyObservation observation)
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

        private static bool HasActiveSatanContract(EnemyObservation observation)
        {
            return observation.OwnerHasActiveSatanContract;
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

        private EnemyDecision DecideExecutionPhase(EnemyObservation observation)
        {
            int playerActionCount = CountPlayerActions(observation);
            if (_telegraphedAction != BossTelegraphedAction.None)
            {
                bool candidateStillExists = HasStrongCandidate(
                    observation,
                    _telegraphedAction);
                bool isSameWindow = observation.RoundNumber ==
                    _telegraphRoundNumber &&
                    playerActionCount == _telegraphPlayerActionCount;
                bool isLaterWindow = observation.RoundNumber ==
                    _telegraphRoundNumber &&
                    playerActionCount > _telegraphPlayerActionCount;

                if (candidateStillExists && isLaterWindow)
                {
                    BossTelegraphedAction actionToExecute = _telegraphedAction;
                    ClearTelegraph();
                    CurrentDisplay = BossCombatDisplayModel.Create(
                        observation,
                        BossTelegraphedAction.None);
                    return EnemyPolicyDecisionSelector.Select(
                        observation,
                        (state, candidate) => EvaluateExecution(
                            state,
                            candidate,
                            actionToExecute));
                }

                if (candidateStillExists && isSameWindow)
                {
                    CurrentDisplay = BossCombatDisplayModel.Create(
                        observation,
                        _telegraphedAction);
                    return EnemyPolicyDecisionSelector.Select(
                        observation,
                        EvaluateTelegraphTurn);
                }

                ClearTelegraph();
            }

            BossTelegraphedAction plannedAction = SelectStrongAction(observation);
            if (plannedAction == BossTelegraphedAction.None)
            {
                CurrentDisplay = BossCombatDisplayModel.Create(
                    observation,
                    BossTelegraphedAction.None);
                return EnemyPolicyDecisionSelector.Select(
                    observation,
                    (state, candidate) =>
                        EvaluatePressure(state, candidate, _declaredNumbers));
            }

            _telegraphedAction = plannedAction;
            _telegraphRoundNumber = observation.RoundNumber;
            _telegraphPlayerActionCount = playerActionCount;
            CurrentDisplay = BossCombatDisplayModel.Create(
                observation,
                plannedAction);
            return EnemyPolicyDecisionSelector.Select(
                observation,
                EvaluateTelegraphTurn);
        }

        private static EnemyActionScore EvaluateSurvival(
            EnemyObservation observation,
            EnemyActionCandidate candidate,
            HashSet<int> declaredNumbers)
        {
            if (candidate.ActionType != EnemyActionType.UseCard)
            {
                return EvaluateBasicAction(
                    observation,
                    candidate,
                    hitMaximum: 15,
                    "boss-survival");
            }

            CardEffectKind effect = GetEffect(candidate);
            switch (effect)
            {
                case CardEffectKind.CrystalOrb:
                    return Score(candidate, 1600, "boss-survival-improve-hand-with-orb");
                case CardEffectKind.ThreatHammer:
                    return Score(
                        candidate,
                        observation.PlayerIsStanding ? 900 : -300,
                        observation.PlayerIsStanding
                            ? "boss-survival-break-player-stand"
                            : "boss-survival-hold-hammer");
                case CardEffectKind.MilitaryKnife:
                {
                    int bustChance = EstimateMilitaryKnifeBustChance(observation);
                    bool hasPoisonSynergy = observation.InjectedPoisonCardCount > 0;
                    return bustChance == 0 && !hasPoisonSynergy
                        ? Score(candidate, -700, "boss-survival-hold-knife-no-bust-chance")
                        : Score(candidate, 600, "boss-survival-low-knife-priority");
                }
                case CardEffectKind.AutoPistol:
                {
                    EnemyActionScore gated = EvaluatePistolUseGate(
                        observation,
                        candidate,
                        declaredNumbers,
                        "boss-survival");
                    if (gated != null)
                    {
                        return gated;
                    }

                    int confidence = FindMostLikelyUntried(
                        observation,
                        declaredNumbers).Value.ProbabilityPercent;
                    return Score(
                        candidate,
                        confidence >= 60 ? 700 : -200,
                        confidence >= 60
                            ? "boss-survival-use-certain-pistol"
                            : "boss-survival-hold-pistol");
                }
                default:
                    return Score(candidate, -700, "boss-survival-ignore-card");
            }
        }

        private static EnemyActionScore EvaluatePressure(
            EnemyObservation observation,
            EnemyActionCandidate candidate,
            HashSet<int> declaredNumbers)
        {
            if (candidate.ActionType != EnemyActionType.UseCard)
            {
                return EvaluateBasicAction(
                    observation,
                    candidate,
                    hitMaximum: 16,
                    "boss-pressure");
            }

            CardEffectKind effect = GetEffect(candidate);
            switch (effect)
            {
                case CardEffectKind.ThreatHammer:
                    return Score(
                        candidate,
                        observation.PlayerIsStanding ? 2100 : -300,
                        observation.PlayerIsStanding
                            ? "boss-pressure-break-player-stand"
                            : "boss-pressure-hold-hammer");
                case CardEffectKind.MilitaryKnife:
                {
                    int bustChance = EstimateMilitaryKnifeBustChance(observation);
                    bool hasPoisonSynergy = observation.InjectedPoisonCardCount > 0;
                    return bustChance == 0 && !hasPoisonSynergy
                        ? Score(candidate, -700, "boss-pressure-hold-knife-no-bust-chance")
                        : Score(candidate, 1800, "boss-pressure-force-player-draw");
                }
                case CardEffectKind.AutoPistol:
                {
                    EnemyActionScore gated = EvaluatePistolUseGate(
                        observation,
                        candidate,
                        declaredNumbers,
                        "boss-pressure");
                    if (gated != null)
                    {
                        return gated;
                    }

                    int confidence = FindMostLikelyUntried(
                        observation,
                        declaredNumbers).Value.ProbabilityPercent;
                    return Score(
                        candidate,
                        confidence >= 35 ? 1700 + confidence : 400,
                        confidence >= 35
                            ? "boss-pressure-use-informed-pistol"
                            : "boss-pressure-low-confidence-pistol");
                }
                case CardEffectKind.CrystalOrb:
                    return Score(candidate, 1500, "boss-pressure-use-orb");
                default:
                    return Score(candidate, -700, "boss-pressure-ignore-card");
            }
        }

        private static EnemyActionScore EvaluateTelegraphTurn(
            EnemyObservation observation,
            EnemyActionCandidate candidate)
        {
            if (candidate.ActionType == EnemyActionType.UseCard)
            {
                return Score(
                    candidate,
                    IsStrongEffect(GetEffect(candidate)) ? -900 : -700,
                    "boss-execution-telegraph-before-strong-action");
            }

            return EvaluateBasicAction(
                observation,
                candidate,
                hitMaximum: 16,
                "boss-execution-telegraph");
        }

        private static EnemyActionScore EvaluateExecution(
            EnemyObservation observation,
            EnemyActionCandidate candidate,
            BossTelegraphedAction actionToExecute)
        {
            if (candidate.ActionType == EnemyActionType.UseCard)
            {
                BossTelegraphedAction candidateAction = ToTelegraphedAction(
                    GetEffect(candidate));
                return candidateAction == actionToExecute
                    ? Score(candidate, 4000, "boss-execute-telegraphed-strong-action")
                    : Score(candidate, -900, "boss-hold-non-telegraphed-card");
            }

            return EvaluateBasicAction(
                observation,
                candidate,
                hitMaximum: 16,
                "boss-execution");
        }

        private static EnemyActionScore EvaluatePistolUseGate(
            EnemyObservation observation,
            EnemyActionCandidate candidate,
            HashSet<int> declaredNumbers,
            string reasonPrefix)
        {
            if (HasActiveSatanContract(observation))
            {
                return Score(
                    candidate,
                    -900,
                    $"{reasonPrefix}-hold-pistol-during-satan-contract");
            }

            EnemyNumberInference? bestUntried = FindMostLikelyUntried(
                observation,
                declaredNumbers);
            if (!bestUntried.HasValue)
            {
                return Score(
                    candidate,
                    -600,
                    $"{reasonPrefix}-no-untried-numbers-remaining");
            }

            int opponentVisibleTotal = CalculateBestTotal(observation.PlayerFaceUpCards);
            bool alreadyWinning =
                observation.PlayerIsStanding &&
                observation.OwnHandValue.Total <= 21 &&
                opponentVisibleTotal + bestUntried.Value.Number >= 22;
            return alreadyWinning
                ? Score(
                    candidate,
                    -400,
                    $"{reasonPrefix}-hold-pistol-already-winning-at-showdown")
                : null;
        }

        private static EnemyActionScore EvaluatePendingCardChoice(
            EnemyObservation observation,
            EnemyActionCandidate candidate,
            HashSet<int> declaredNumbers)
        {
            switch (observation.PendingCardEffectKind.Value)
            {
                case CardEffectKind.CrystalOrb:
                    if (!candidate.CardEffectOptionCardRank.HasValue)
                    {
                        return Score(
                            candidate,
                            2000 + observation.OwnHandValue.Total,
                            "boss-orb-keep-current-hand");
                    }

                    int resultingTotal = CalculateTotalWithAdditionalRank(
                        observation,
                        candidate.CardEffectOptionCardRank.Value);
                    return resultingTotal <= 21
                        ? Score(
                            candidate,
                            2000 + resultingTotal,
                            "boss-orb-take-highest-safe-card")
                        : Score(
                            candidate,
                            -2000 - resultingTotal,
                            "boss-orb-reject-busting-card");
                case CardEffectKind.ThreatHammer:
                    int targetRank = candidate.CardEffectOptionCardRank ?? 0;
                    return Score(
                        candidate,
                        3000 + (targetRank * 10),
                        "boss-discard-highest-hammer-target");
                case CardEffectKind.AutoPistol:
                    int declaredNumber = candidate.CardEffectOptionNumericValue ?? 0;
                    if (declaredNumbers.Contains(declaredNumber))
                    {
                        return Score(
                            candidate,
                            -1000,
                            "boss-avoid-repeated-number");
                    }

                    int probability = FindInferenceProbability(
                        observation,
                        candidate.CardEffectOptionNumericValue);
                    return Score(
                        candidate,
                        3000 + probability,
                        "boss-declare-most-likely-number");
                default:
                    throw new InvalidOperationException(
                        "Final boss policy received an unsupported pending effect.");
            }
        }

        private static EnemyActionScore EvaluateDemonContractChoice(
            EnemyObservation observation,
            EnemyActionCandidate candidate)
        {
            if (candidate.ActionType != EnemyActionType.DemonContract)
            {
                throw new InvalidOperationException(
                    "Final boss received an unsupported demon contract choice.");
            }

            switch (candidate.DemonContractInteractionKind)
            {
                case DemonContractInteractionKind.AsmodeusForceOpponentHit:
                    bool forcesHit = candidate.DemonContractOptionId ==
                        AsmodeusDemonContractHandler.ForceHitOptionId;
                    bool opponentVisiblyAhead =
                        CalculateBestTotal(observation.PlayerFaceUpCards) >
                            CalculateOwnVisibleTotal(observation);
                    return Score(
                        candidate,
                        forcesHit == opponentVisiblyAhead ? 3000 : 0,
                        forcesHit
                            ? "boss-force-opponent-hit-with-asmodeus"
                            : "boss-skip-asmodeus-forced-hit");
                case DemonContractInteractionKind.SatanDeclareFirstNumber:
                case DemonContractInteractionKind.SatanDeclareSecondNumber:
                    int probability = FindInferenceProbability(
                        observation,
                        candidate.DemonContractOptionNumericValue);
                    return Score(
                        candidate,
                        3000 + probability,
                        "boss-declare-likely-satan-number");
                case DemonContractInteractionKind.SatanTurnStartChoice:
                    FinalBossPhase satanPhase = FinalBossPhaseResolver.Resolve(
                        observation.EnemySoul);
                    int satanHitMaximum =
                        satanPhase == FinalBossPhase.Survival ? 15 : 16;
                    bool prefersSatanAbility =
                        observation.OwnHandValue.Total > satanHitMaximum;
                    bool usesSatanAbility = candidate.DemonContractOptionId ==
                        SatanDemonContractHandler.UseAbilityOptionId;
                    return Score(
                        candidate,
                        usesSatanAbility == prefersSatanAbility ? 3000 : 0,
                        usesSatanAbility
                            ? "boss-use-satan-instead-of-unsafe-hit"
                            : "boss-skip-satan-continue-normal-action");
                default:
                    throw new InvalidOperationException(
                        "Final boss received an unsupported demon contract choice.");
            }
        }

        private static bool HasPendingDemonContractChoice(
            EnemyObservation observation)
        {
            foreach (EnemyActionCandidate candidate in observation.ActionCandidates)
            {
                if (candidate.ActionType == EnemyActionType.DemonContract &&
                    candidate.DemonContractOptionId.HasValue)
                {
                    return true;
                }
            }

            return false;
        }

        private static EnemyActionScore EvaluateBasicAction(
            EnemyObservation observation,
            EnemyActionCandidate candidate,
            int hitMaximum,
            string reasonPrefix)
        {
            switch (candidate.ActionType)
            {
                case EnemyActionType.Hit:
                    return Score(
                        candidate,
                        observation.OwnHandValue.Total <= hitMaximum ? 700 : 100,
                        $"{reasonPrefix}-hit");
                case EnemyActionType.Stand:
                    return Score(
                        candidate,
                        observation.OwnHandValue.Total > hitMaximum ? 800 : 200,
                        $"{reasonPrefix}-stand");
                case EnemyActionType.DemonContract:
                    return Score(
                        candidate,
                        -1000,
                        $"{reasonPrefix}-ignore-unsupported-contract");
                case EnemyActionType.Change:
                    return EnemyChangeRiskEvaluator.ShouldAcceptChange(observation)
                        ? Score(candidate, 2000, $"{reasonPrefix}-required-change")
                        : Score(candidate, -50, $"{reasonPrefix}-decline-risky-paid-change");
                default:
                    throw new ArgumentOutOfRangeException(nameof(candidate));
            }
        }

        private static BossTelegraphedAction SelectStrongAction(
            EnemyObservation observation)
        {
            bool hasPistol = HasStrongCandidate(
                observation,
                BossTelegraphedAction.NumberGuess);
            bool hasKnife = HasStrongCandidate(
                observation,
                BossTelegraphedAction.ForcedDraw);
            if (hasPistol && GetTopInferenceProbability(observation) >= 50)
            {
                return BossTelegraphedAction.NumberGuess;
            }

            if (hasKnife)
            {
                return BossTelegraphedAction.ForcedDraw;
            }

            return hasPistol
                ? BossTelegraphedAction.NumberGuess
                : BossTelegraphedAction.None;
        }

        private static bool HasStrongCandidate(
            EnemyObservation observation,
            BossTelegraphedAction action)
        {
            foreach (EnemyActionCandidate candidate in observation.ActionCandidates)
            {
                if (candidate.ActionType == EnemyActionType.UseCard &&
                    ToTelegraphedAction(GetEffect(candidate)) == action)
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountPlayerActions(EnemyObservation observation)
        {
            int count = 0;
            foreach (PublicCombatAction action in observation.PublicActionHistory)
            {
                if (action.ActorSide == CombatantSide.Player)
                {
                    count++;
                }
            }

            return count;
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

        private static int CalculateOwnVisibleTotal(EnemyObservation observation)
        {
            int total = 0;
            int aceCount = 0;
            foreach (EnemyOwnedCardObservation card in observation.OwnCards)
            {
                if (!card.IsFaceUp || card.IsHiddenCard)
                {
                    continue;
                }

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

        private static int GetTopInferenceProbability(EnemyObservation observation)
        {
            int topProbability = 0;
            foreach (EnemyNumberInference inference in observation.NumberInferences)
            {
                topProbability = Math.Max(
                    topProbability,
                    inference.ProbabilityPercent);
            }

            return topProbability;
        }

        private static int FindInferenceProbability(
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

        private static CardEffectKind GetEffect(EnemyActionCandidate candidate)
        {
            return CardDefinitionCatalog.GetByKey(
                candidate.CardDefinitionKey).Effect;
        }

        private static bool IsStrongEffect(CardEffectKind effect)
        {
            return effect == CardEffectKind.AutoPistol ||
                effect == CardEffectKind.MilitaryKnife;
        }

        private static BossTelegraphedAction ToTelegraphedAction(
            CardEffectKind effect)
        {
            switch (effect)
            {
                case CardEffectKind.AutoPistol:
                    return BossTelegraphedAction.NumberGuess;
                case CardEffectKind.MilitaryKnife:
                    return BossTelegraphedAction.ForcedDraw;
                default:
                    return BossTelegraphedAction.None;
            }
        }

        private void ClearTelegraph()
        {
            _telegraphedAction = BossTelegraphedAction.None;
            _telegraphRoundNumber = -1;
            _telegraphPlayerActionCount = -1;
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
