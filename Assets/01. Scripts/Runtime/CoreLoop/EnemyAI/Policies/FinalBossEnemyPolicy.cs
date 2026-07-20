using System;

namespace DiaBlackJack.CoreLoop
{
    public sealed class FinalBossEnemyPolicy : IEnemyBehaviorPolicy
    {
        private BossTelegraphedAction _telegraphedAction;
        private int _telegraphPlayerActionCount = -1;
        private int _telegraphRoundNumber = -1;

        public BossCombatDisplayModel CurrentDisplay { get; private set; }

        public EnemyDecision Decide(EnemyObservation observation)
        {
            if (observation == null)
            {
                throw new ArgumentNullException(nameof(observation));
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
                    EvaluatePendingCardChoice);
            }

            if (phase == FinalBossPhase.Survival)
            {
                ClearTelegraph();
                CurrentDisplay = BossCombatDisplayModel.Create(
                    observation,
                    BossTelegraphedAction.None);
                return EnemyPolicyDecisionSelector.Select(
                    observation,
                    EvaluateSurvival);
            }

            if (phase == FinalBossPhase.Pressure)
            {
                ClearTelegraph();
                CurrentDisplay = BossCombatDisplayModel.Create(
                    observation,
                    BossTelegraphedAction.None);
                return EnemyPolicyDecisionSelector.Select(
                    observation,
                    EvaluatePressure);
            }

            return DecideExecutionPhase(observation);
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
                    EvaluatePressure);
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
            EnemyActionCandidate candidate)
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
                    return Score(candidate, 600, "boss-survival-low-knife-priority");
                case CardEffectKind.AutoPistol:
                    int confidence = GetTopInferenceProbability(observation);
                    return Score(
                        candidate,
                        confidence >= 60 ? 700 : -200,
                        confidence >= 60
                            ? "boss-survival-use-certain-pistol"
                            : "boss-survival-hold-pistol");
                default:
                    return Score(candidate, -700, "boss-survival-ignore-card");
            }
        }

        private static EnemyActionScore EvaluatePressure(
            EnemyObservation observation,
            EnemyActionCandidate candidate)
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
                    return Score(candidate, 1800, "boss-pressure-force-player-draw");
                case CardEffectKind.AutoPistol:
                    int confidence = GetTopInferenceProbability(observation);
                    return Score(
                        candidate,
                        confidence >= 35 ? 1700 + confidence : 400,
                        confidence >= 35
                            ? "boss-pressure-use-informed-pistol"
                            : "boss-pressure-low-confidence-pistol");
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

        private static EnemyActionScore EvaluatePendingCardChoice(
            EnemyObservation observation,
            EnemyActionCandidate candidate)
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
                    int costRank = candidate.CardEffectOptionCardRank ?? 10;
                    return Score(
                        candidate,
                        3000 - (costRank * 10),
                        "boss-pay-lowest-hammer-cost");
                case CardEffectKind.AutoPistol:
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
                case EnemyActionType.Fold:
                    return Score(candidate, -600, $"{reasonPrefix}-avoid-fold");
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
