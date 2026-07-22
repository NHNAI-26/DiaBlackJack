using System;

namespace DiaBlackJack.CoreLoop
{
    public sealed class CultistEnemyPolicy : IEnemyBehaviorPolicy
    {
        public const int AggressiveHitCeiling = 18;
        public const int MammonRerollCeiling = 2;

        private const int PreferredContractScore = 950;
        private const int SafeSatanScore = 970;
        private const int UsefulLeviathanScore = 960;
        private const int AvoidContractScore = -1000;

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
                        observation.OwnHandValue.Total <= AggressiveHitCeiling
                            ? 700
                            : 100,
                        "cultist-accept-hit-risk");
                case EnemyActionType.Stand:
                    return Score(
                        candidate,
                        observation.OwnHandValue.Total > AggressiveHitCeiling
                            ? 700
                            : 350,
                        "cultist-delay-safe-stand");
                case EnemyActionType.UseCard:
                    return Score(candidate, 500, "cultist-use-implemented-aggression-card");
                case EnemyActionType.DemonContract:
                    return EvaluateDemonContract(observation, candidate);
                default:
                    throw new ArgumentOutOfRangeException(nameof(candidate));
            }
        }

        private static EnemyActionScore EvaluateDemonContract(
            EnemyObservation observation,
            EnemyActionCandidate candidate)
        {
            if (!candidate.DemonContractOptionId.HasValue)
            {
                return Score(candidate, 1000, "cultist-begin-demon-contract");
            }

            switch (candidate.DemonContractInteractionKind)
            {
                case DemonContractInteractionKind.ChooseContract:
                    return EvaluateContractChoice(observation, candidate);
                case DemonContractInteractionKind.BelphegorTopCard:
                    return EvaluateBelphegorChoice(observation, candidate);
                case DemonContractInteractionKind.MammonReroll:
                    return EvaluateMammonReroll(candidate);
                case DemonContractInteractionKind.MammonApplyDie:
                    return EvaluateMammonFinalChoice(observation, candidate);
                default:
                    throw new InvalidOperationException(
                        "Cultist contract option has no interaction kind.");
            }
        }

        private static EnemyActionScore EvaluateContractChoice(
            EnemyObservation observation,
            EnemyActionCandidate candidate)
        {
            switch (candidate.DemonContractKind)
            {
                case DemonContractKind.Satan:
                    bool guaranteedDeath = observation.EnemySoul.Current <=
                        SatanDemonContractHandler.ExpirationSoulCost;
                    return Score(
                        candidate,
                        guaranteedDeath ? AvoidContractScore : SafeSatanScore,
                        guaranteedDeath
                            ? "cultist-avoid-fatal-satan"
                            : "cultist-select-satan");
                case DemonContractKind.Belphegor:
                    return Score(
                        candidate,
                        PreferredContractScore,
                        "cultist-select-belphegor");
                case DemonContractKind.Mammon:
                    return Score(
                        candidate,
                        PreferredContractScore,
                        "cultist-select-mammon");
                case DemonContractKind.Leviathan:
                    bool hasRevolver = HasUnusedRevolver(observation);
                    return Score(
                        candidate,
                        hasRevolver ? UsefulLeviathanScore : AvoidContractScore,
                        hasRevolver
                            ? "cultist-select-leviathan-with-revolver"
                            : "cultist-avoid-leviathan-without-revolver");
                default:
                    throw new InvalidOperationException(
                        "Cultist received an unknown demon contract choice.");
            }
        }

        private static EnemyActionScore EvaluateBelphegorChoice(
            EnemyObservation observation,
            EnemyActionCandidate candidate)
        {
            int previewRank = candidate.DemonContractOptionNumericValue ??
                throw new InvalidOperationException(
                    "Cultist Belphegor choice requires the private preview rank.");
            bool shouldMove = GetOwnVisibleTotal(observation) + previewRank > 21;
            bool isMove = candidate.DemonContractOptionId ==
                BelphegorDemonContractHandler.MoveTopCardToBottomOptionId;
            return Score(
                candidate,
                shouldMove == isMove ? 1500 : 0,
                shouldMove == isMove
                    ? (isMove
                        ? "cultist-move-unsafe-belphegor-card"
                        : "cultist-keep-safe-belphegor-card")
                    : "cultist-reject-belphegor-option");
        }

        private static EnemyActionScore EvaluateMammonReroll(
            EnemyActionCandidate candidate)
        {
            int dieValue = candidate.DemonContractOptionNumericValue ??
                throw new InvalidOperationException(
                    "Cultist Mammon turn choice requires the current die value.");
            bool shouldReroll = dieValue <= MammonRerollCeiling;
            bool isReroll = candidate.DemonContractOptionId ==
                MammonDemonContractHandler.RerollDieOptionId;
            return Score(
                candidate,
                shouldReroll == isReroll ? 1500 : 0,
                shouldReroll == isReroll
                    ? (isReroll
                        ? "cultist-reroll-low-mammon-die"
                        : "cultist-keep-mammon-die")
                    : "cultist-reject-mammon-turn-option");
        }

        private static EnemyActionScore EvaluateMammonFinalChoice(
            EnemyObservation observation,
            EnemyActionCandidate candidate)
        {
            int dieValue = candidate.DemonContractOptionNumericValue ??
                throw new InvalidOperationException(
                    "Cultist Mammon final choice requires the current die value.");
            bool shouldApply = observation.OwnHandValue.Total + dieValue <= 21;
            bool isApply = candidate.DemonContractOptionId ==
                MammonDemonContractHandler.ApplyDieOptionId;
            return Score(
                candidate,
                shouldApply == isApply ? 1500 : 0,
                shouldApply == isApply
                    ? (isApply
                        ? "cultist-apply-safe-mammon-die"
                        : "cultist-decline-busting-mammon-die")
                    : "cultist-reject-mammon-final-option");
        }

        private static int GetOwnVisibleTotal(EnemyObservation observation)
        {
            int total = 0;
            foreach (EnemyOwnedCardObservation card in observation.OwnCards)
            {
                if (card.IsFaceUp)
                {
                    total += card.Rank;
                }
            }

            return total;
        }

        private static bool HasUnusedRevolver(EnemyObservation observation)
        {
            foreach (EnemyOwnedCardObservation card in observation.OwnCards)
            {
                if (card.UseState == CardUseState.Available &&
                    CardDefinitionCatalog.GetByKey(card.DefinitionKey).Effect ==
                        CardEffectKind.AutoPistol)
                {
                    return true;
                }
            }

            return false;
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
