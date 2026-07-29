using System;

namespace DiaBlackJack.CoreLoop
{
    public sealed class CultistEnemyPolicy : IEnemyBehaviorPolicy
    {
        public const int AggressiveHitCeiling = 18;
        public const int MammonRerollCeiling = 2;

        private const int PreferredContractScore = 950;
        private const int UsefulLeviathanScore = 960;
        private const int AvoidContractScore = -1000;
        private const int FatalContractScore = -1100;

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
            if (candidate.DemonContractSourceCardId.HasValue &&
                !candidate.DemonContractOptionId.HasValue)
            {
                switch (candidate.DemonContractKind)
                {
                    case DemonContractKind.Satan:
                        return Score(
                            candidate,
                            980,
                            "cultist-use-active-satan-contract");
                    case DemonContractKind.Mammon:
                        return EvaluateActiveMammonReroll(candidate);
                    default:
                        throw new InvalidOperationException(
                            "Cultist received an unsupported active contract action.");
                }
            }

            if (!candidate.DemonContractOptionId.HasValue)
            {
                return Score(candidate, 1000, "cultist-begin-demon-contract");
            }

            switch (candidate.DemonContractInteractionKind)
            {
                case DemonContractInteractionKind.ChooseContract:
                    return EvaluateContractChoice(observation, candidate);
                case DemonContractInteractionKind.LuciferChooseAdditionalContract:
                    return candidate.DemonContractKind.HasValue
                        ? EvaluateContractChoice(observation, candidate)
                        : Score(
                            candidate,
                            0,
                            "cultist-skip-unhelpful-lucifer-contract");
                case DemonContractInteractionKind.BelphegorTopCard:
                    return EvaluateBelphegorChoice(observation, candidate);
                case DemonContractInteractionKind.MammonApplyDie:
                    return EvaluateMammonFinalChoice(observation, candidate);
                case DemonContractInteractionKind.SatanDeclareFirstNumber:
                case DemonContractInteractionKind.SatanDeclareSecondNumber:
                    return EvaluateSatanNumber(observation, candidate);
                case DemonContractInteractionKind.BeelzebubChooseOwnerCard:
                    return EvaluateBeelzebubDiscard(
                        candidate,
                        preferHigherRank: true);
                case DemonContractInteractionKind.BeelzebubChooseOpponentCard:
                    return EvaluateBeelzebubDiscard(
                        candidate,
                        preferHigherRank: false);
                case DemonContractInteractionKind.AsmodeusForceOpponentHit:
                    bool forcesHit = candidate.DemonContractOptionId ==
                        AsmodeusDemonContractHandler.ForceHitOptionId;
                    return Score(
                        candidate,
                        forcesHit ? 1500 : 0,
                        forcesHit
                            ? "cultist-force-opponent-hit-with-asmodeus"
                            : "cultist-skip-asmodeus-forced-hit");
                case DemonContractInteractionKind.PaimonChooseDeck:
                    bool choosesOpponentDeck =
                        candidate.DemonContractOptionId ==
                            PaimonDemonContractHandler.OpponentDeckOptionId;
                    return Score(
                        candidate,
                        choosesOpponentDeck ? 1500 : 100,
                        choosesOpponentDeck
                            ? "cultist-inspect-opponent-deck-with-paimon"
                            : "cultist-avoid-own-deck-paimon-exile");
                case DemonContractInteractionKind.PaimonChooseExileCard:
                    return EvaluatePaimonExile(candidate);
                case DemonContractInteractionKind.BelialChooseOpponentCard:
                    return EvaluateBelialTransfer(candidate);
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
                    return Score(
                        candidate,
                        FatalContractScore,
                        "cultist-avoid-satan");
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
                case DemonContractKind.Beelzebub:
                    bool canSurviveBustCost = observation.EnemySoul.Current > 1;
                    return Score(
                        candidate,
                        canSurviveBustCost
                            ? PreferredContractScore
                            : FatalContractScore,
                        canSurviveBustCost
                            ? "cultist-select-survivable-beelzebub"
                            : "cultist-avoid-fatal-beelzebub");
                case DemonContractKind.Mephistopheles:
                    bool hasKnife = HasUnusedMilitaryKnife(observation);
                    return Score(
                        candidate,
                        hasKnife ? UsefulLeviathanScore : AvoidContractScore,
                        hasKnife
                            ? "cultist-select-mephistopheles-with-knife"
                            : "cultist-avoid-mephistopheles-without-knife");
                case DemonContractKind.Asmodeus:
                    return Score(
                        candidate,
                        PreferredContractScore,
                        "cultist-select-asmodeus");
                case DemonContractKind.Azazel:
                    return Score(
                        candidate,
                        PreferredContractScore,
                        "cultist-select-azazel");
                case DemonContractKind.Paimon:
                    return Score(
                        candidate,
                        PreferredContractScore,
                        "cultist-select-paimon");
                case DemonContractKind.Belial:
                    bool survivesNextRoundCost = observation.EnemySoul.Current >
                        BelialDemonContractHandler.RoundStartSoulCost;
                    return Score(
                        candidate,
                        survivesNextRoundCost
                            ? PreferredContractScore
                            : FatalContractScore,
                        survivesNextRoundCost
                            ? "cultist-select-survivable-belial"
                            : "cultist-avoid-fatal-belial");
                case DemonContractKind.Baphomet:
                    return Score(
                        candidate,
                        PreferredContractScore,
                        "cultist-select-baphomet");
                case DemonContractKind.Lucifer:
                    bool survivesLuciferCost = observation.EnemySoul.Current >
                        LuciferDemonContractHandler.IndividualSoulCost;
                    return Score(
                        candidate,
                        survivesLuciferCost ? 980 : FatalContractScore,
                        survivesLuciferCost
                            ? "cultist-select-survivable-lucifer"
                            : "cultist-avoid-fatal-lucifer");
                default:
                    throw new InvalidOperationException(
                        "Cultist received an unknown demon contract choice.");
            }
        }

        private static EnemyActionScore EvaluateSatanNumber(
            EnemyObservation observation,
            EnemyActionCandidate candidate)
        {
            int number = candidate.DemonContractOptionNumericValue ??
                throw new InvalidOperationException(
                    "Cultist Satan declaration requires a public number option.");
            int probability = 0;
            foreach (EnemyNumberInference inference in observation.NumberInferences)
            {
                if (inference.Number == number)
                {
                    probability = inference.ProbabilityPercent;
                    break;
                }
            }

            return Score(
                candidate,
                1200 + probability,
                "cultist-declare-likely-satan-number");
        }

        private static EnemyActionScore EvaluateBeelzebubDiscard(
            EnemyActionCandidate candidate,
            bool preferHigherRank)
        {
            int rank = candidate.DemonContractOptionNumericValue ??
                throw new InvalidOperationException(
                    "Cultist Beelzebub choice requires a public card rank.");
            int score = preferHigherRank ? 1200 + rank : 1200 - rank;
            return Score(
                candidate,
                score,
                preferHigherRank
                    ? "cultist-beelzebub-discard-highest-own-card"
                    : "cultist-beelzebub-discard-lowest-opponent-card");
        }

        private static EnemyActionScore EvaluatePaimonExile(
            EnemyActionCandidate candidate)
        {
            int? rank = candidate.DemonContractOptionNumericValue;
            if (!rank.HasValue)
            {
                return Score(
                    candidate,
                    500,
                    "cultist-skip-paimon-exile");
            }

            return Score(
                candidate,
                rank.Value > 0 ? 1200 + rank.Value : 0,
                rank.Value > 0
                    ? "cultist-exile-highest-opponent-card-with-paimon"
                    : "cultist-preserve-own-card-with-paimon");
        }

        private static EnemyActionScore EvaluateBelialTransfer(
            EnemyActionCandidate candidate)
        {
            int? rank = candidate.DemonContractOptionNumericValue;
            return Score(
                candidate,
                rank.HasValue ? 1200 + rank.Value : 0,
                rank.HasValue
                    ? "cultist-transfer-highest-opponent-card-with-belial"
                    : "cultist-skip-belial-transfer");
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

        private static EnemyActionScore EvaluateActiveMammonReroll(
            EnemyActionCandidate candidate)
        {
            int dieValue = candidate.DemonContractOptionNumericValue ??
                throw new InvalidOperationException(
                    "Cultist active Mammon action requires the current die value.");
            bool shouldReroll = dieValue <= MammonRerollCeiling;
            return Score(
                candidate,
                shouldReroll ? 1100 : 0,
                shouldReroll
                    ? "cultist-use-mammon-reroll-action"
                    : "cultist-skip-mammon-reroll-action");
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
                if (card.IsFaceUp && !card.IsHiddenCard)
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

        private static bool HasUnusedMilitaryKnife(EnemyObservation observation)
        {
            foreach (EnemyOwnedCardObservation card in observation.OwnCards)
            {
                if (card.UseState == CardUseState.Available &&
                    CardDefinitionCatalog.GetByKey(card.DefinitionKey).Effect ==
                        CardEffectKind.MilitaryKnife)
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
