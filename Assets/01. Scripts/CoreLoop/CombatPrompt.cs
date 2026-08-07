using System;

namespace DiaBlackJack.CoreLoop
{
    public enum CombatPromptId
    {
        None = 0,
        ChangeCard = 1,
        ManualAutoPistolDeclareNumber = 2,
        ManualCrystalOrbChooseCard = 3,
        ManualThreatHammerChooseOpponentCard = 4,
        AutomaticLieDetectorDeclareNumber = 5,
        AutomaticPoisonDecision = 6,
        AutomaticFlamethrowerChooseDiscard = 7,
        AutomaticPocketWatchChooseManualCard = 8,
        AutomaticPocketWatchChooseDisposition = 9,
        AutomaticResurrectionHerbDecision = 10,
        DemonChooseContract = 11,
        DemonBelphegorTopCard = 12,
        DemonMammonReroll = 13,
        DemonMammonApplyDie = 14,
        DemonSatanDeclareFirstNumber = 15,
        DemonSatanDeclareSecondNumber = 16,
        DemonBeelzebubChooseOwnerCard = 17,
        DemonBeelzebubChooseOpponentCard = 18,
        DemonAsmodeusForceOpponentHit = 19,
        DemonSatanTurnStartChoice = 20,
        DemonPaimonChooseDeck = 21,
        DemonPaimonChooseExileCard = 22,
        DemonBelialChooseOpponentCard = 23,
        DemonLuciferChooseAdditionalContract = 24
    }

    public readonly struct CombatPromptRequest : IEquatable<CombatPromptRequest>
    {
        public CombatPromptRequest(
            CombatPromptId id,
            string sourceDisplayName = "",
            string contextText = "",
            int currentCount = 0,
            int requiredCount = 0)
        {
            if (!Enum.IsDefined(typeof(CombatPromptId), id) ||
                id == CombatPromptId.None)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            if (currentCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(currentCount));
            }

            if (requiredCount < 0 || currentCount > requiredCount && requiredCount > 0)
            {
                throw new ArgumentOutOfRangeException(nameof(requiredCount));
            }

            Id = id;
            SourceDisplayName = sourceDisplayName ?? string.Empty;
            ContextText = contextText ?? string.Empty;
            CurrentCount = currentCount;
            RequiredCount = requiredCount;
        }

        public CombatPromptId Id { get; }

        public string SourceDisplayName { get; }

        public string ContextText { get; }

        public int CurrentCount { get; }

        public int RequiredCount { get; }

        public CombatPromptRequest WithCounts(int currentCount, int requiredCount)
        {
            return new CombatPromptRequest(
                Id,
                SourceDisplayName,
                ContextText,
                currentCount,
                requiredCount);
        }

        public bool Equals(CombatPromptRequest other)
        {
            return Id == other.Id &&
                SourceDisplayName == other.SourceDisplayName &&
                ContextText == other.ContextText &&
                CurrentCount == other.CurrentCount &&
                RequiredCount == other.RequiredCount;
        }

        public override bool Equals(object obj)
        {
            return obj is CombatPromptRequest other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)Id;
                hashCode = (hashCode * 397) ^ SourceDisplayName.GetHashCode();
                hashCode = (hashCode * 397) ^ ContextText.GetHashCode();
                hashCode = (hashCode * 397) ^ CurrentCount;
                hashCode = (hashCode * 397) ^ RequiredCount;
                return hashCode;
            }
        }

        public static bool operator ==(
            CombatPromptRequest left,
            CombatPromptRequest right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            CombatPromptRequest left,
            CombatPromptRequest right)
        {
            return !left.Equals(right);
        }
    }

    public enum AutomaticCardResultPromptId
    {
        None = 0,
        Poison = 1,
        ResurrectionHerb = 2,
        LieDetector = 3,
        Flamethrower = 4,
        PocketWatch = 5
    }

    public enum AutomaticCardHiddenComparison
    {
        None = 0,
        AtLeastDeclared = 1,
        BelowDeclared = 2
    }

    public enum AutomaticCardResultOutcome
    {
        None = 0,
        WinHealReserved = 1,
        ReservationResolved = 2
    }

    public readonly struct AutomaticCardResultPromptRequest :
        IEquatable<AutomaticCardResultPromptRequest>
    {
        public AutomaticCardResultPromptRequest(
            AutomaticCardResultPromptId id,
            string sourceDisplayName,
            CombatantSide ownerSide,
            AutomaticCardSourceDisposition sourceDisposition,
            AutomaticCardDecisionOutcome playerDecision =
                AutomaticCardDecisionOutcome.None,
            AutomaticCardDecisionOutcome enemyDecision =
                AutomaticCardDecisionOutcome.None,
            int? declaredNumber = null,
            AutomaticCardHiddenComparison comparison =
                AutomaticCardHiddenComparison.None,
            AutomaticCardResultOutcome outcome =
                AutomaticCardResultOutcome.None)
        {
            if (!Enum.IsDefined(typeof(AutomaticCardResultPromptId), id) ||
                id == AutomaticCardResultPromptId.None)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            if (declaredNumber.HasValue &&
                (declaredNumber.Value < 1 || declaredNumber.Value > 10))
            {
                throw new ArgumentOutOfRangeException(nameof(declaredNumber));
            }

            Id = id;
            SourceDisplayName = sourceDisplayName ?? string.Empty;
            OwnerSide = ownerSide;
            SourceDisposition = sourceDisposition;
            PlayerDecision = playerDecision;
            EnemyDecision = enemyDecision;
            DeclaredNumber = declaredNumber;
            Comparison = comparison;
            Outcome = outcome;
        }

        public AutomaticCardResultPromptId Id { get; }

        public string SourceDisplayName { get; }

        public CombatantSide OwnerSide { get; }

        public AutomaticCardSourceDisposition SourceDisposition { get; }

        public AutomaticCardDecisionOutcome PlayerDecision { get; }

        public AutomaticCardDecisionOutcome EnemyDecision { get; }

        public int? DeclaredNumber { get; }

        public AutomaticCardHiddenComparison Comparison { get; }

        public AutomaticCardResultOutcome Outcome { get; }

        public bool Equals(AutomaticCardResultPromptRequest other)
        {
            return Id == other.Id &&
                SourceDisplayName == other.SourceDisplayName &&
                OwnerSide == other.OwnerSide &&
                SourceDisposition == other.SourceDisposition &&
                PlayerDecision == other.PlayerDecision &&
                EnemyDecision == other.EnemyDecision &&
                DeclaredNumber == other.DeclaredNumber &&
                Comparison == other.Comparison &&
                Outcome == other.Outcome;
        }

        public override bool Equals(object obj)
        {
            return obj is AutomaticCardResultPromptRequest other &&
                Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)Id;
                hashCode = (hashCode * 397) ^ SourceDisplayName.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)OwnerSide;
                hashCode = (hashCode * 397) ^ (int)SourceDisposition;
                hashCode = (hashCode * 397) ^ (int)PlayerDecision;
                hashCode = (hashCode * 397) ^ (int)EnemyDecision;
                hashCode = (hashCode * 397) ^ DeclaredNumber.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)Comparison;
                hashCode = (hashCode * 397) ^ (int)Outcome;
                return hashCode;
            }
        }

        public static bool operator ==(
            AutomaticCardResultPromptRequest left,
            AutomaticCardResultPromptRequest right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            AutomaticCardResultPromptRequest left,
            AutomaticCardResultPromptRequest right)
        {
            return !left.Equals(right);
        }
    }

    internal static class AutomaticCardResultPromptIdMap
    {
        public static AutomaticCardResultPromptId ForEffect(
            CardEffectKind effectKind)
        {
            switch (effectKind)
            {
                case CardEffectKind.Poison:
                    return AutomaticCardResultPromptId.Poison;
                case CardEffectKind.ResurrectionHerb:
                    return AutomaticCardResultPromptId.ResurrectionHerb;
                case CardEffectKind.LieDetector:
                    return AutomaticCardResultPromptId.LieDetector;
                case CardEffectKind.Flamethrower:
                    return AutomaticCardResultPromptId.Flamethrower;
                case CardEffectKind.PocketWatch:
                    return AutomaticCardResultPromptId.PocketWatch;
                default:
                    throw new ArgumentOutOfRangeException(nameof(effectKind));
            }
        }
    }

    internal static class CombatPromptIdMap
    {
        public static CombatPromptId ForManualCard(CardEffectKind effectKind)
        {
            switch (effectKind)
            {
                case CardEffectKind.AutoPistol:
                    return CombatPromptId.ManualAutoPistolDeclareNumber;
                case CardEffectKind.CrystalOrb:
                    return CombatPromptId.ManualCrystalOrbChooseCard;
                case CardEffectKind.ThreatHammer:
                    return CombatPromptId.ManualThreatHammerChooseOpponentCard;
                default:
                    throw new ArgumentOutOfRangeException(nameof(effectKind));
            }
        }

        public static CombatPromptId ForAutomaticCard(
            AutomaticCardChoiceKind choiceKind)
        {
            switch (choiceKind)
            {
                case AutomaticCardChoiceKind.LieDetectorNumber:
                    return CombatPromptId.AutomaticLieDetectorDeclareNumber;
                case AutomaticCardChoiceKind.PoisonDecision:
                    return CombatPromptId.AutomaticPoisonDecision;
                case AutomaticCardChoiceKind.FlamethrowerOwnerDiscard:
                case AutomaticCardChoiceKind.FlamethrowerOpponentDiscard:
                    return CombatPromptId.AutomaticFlamethrowerChooseDiscard;
                case AutomaticCardChoiceKind.PocketWatchManualCard:
                    return CombatPromptId.AutomaticPocketWatchChooseManualCard;
                case AutomaticCardChoiceKind.PocketWatchSourceDisposition:
                    return CombatPromptId.AutomaticPocketWatchChooseDisposition;
                case AutomaticCardChoiceKind.ResurrectionHerbDecision:
                case AutomaticCardChoiceKind.ResurrectionHerbOpponentDecision:
                    return CombatPromptId.AutomaticResurrectionHerbDecision;
                default:
                    throw new ArgumentOutOfRangeException(nameof(choiceKind));
            }
        }

        public static CombatPromptId ForDemonContract(
            DemonContractInteractionKind interactionKind)
        {
            switch (interactionKind)
            {
                case DemonContractInteractionKind.ChooseContract:
                    return CombatPromptId.DemonChooseContract;
                case DemonContractInteractionKind.BelphegorTopCard:
                    return CombatPromptId.DemonBelphegorTopCard;
                case DemonContractInteractionKind.MammonReroll:
                    return CombatPromptId.DemonMammonReroll;
                case DemonContractInteractionKind.MammonApplyDie:
                    return CombatPromptId.DemonMammonApplyDie;
                case DemonContractInteractionKind.SatanDeclareFirstNumber:
                    return CombatPromptId.DemonSatanDeclareFirstNumber;
                case DemonContractInteractionKind.SatanDeclareSecondNumber:
                    return CombatPromptId.DemonSatanDeclareSecondNumber;
                case DemonContractInteractionKind.BeelzebubChooseOwnerCard:
                    return CombatPromptId.DemonBeelzebubChooseOwnerCard;
                case DemonContractInteractionKind.BeelzebubChooseOpponentCard:
                    return CombatPromptId.DemonBeelzebubChooseOpponentCard;
                case DemonContractInteractionKind.AsmodeusForceOpponentHit:
                    return CombatPromptId.DemonAsmodeusForceOpponentHit;
                case DemonContractInteractionKind.SatanTurnStartChoice:
                    return CombatPromptId.DemonSatanTurnStartChoice;
                case DemonContractInteractionKind.PaimonChooseDeck:
                    return CombatPromptId.DemonPaimonChooseDeck;
                case DemonContractInteractionKind.PaimonChooseExileCard:
                    return CombatPromptId.DemonPaimonChooseExileCard;
                case DemonContractInteractionKind.BelialChooseOpponentCard:
                    return CombatPromptId.DemonBelialChooseOpponentCard;
                case DemonContractInteractionKind.LuciferChooseAdditionalContract:
                    return CombatPromptId.DemonLuciferChooseAdditionalContract;
                default:
                    throw new ArgumentOutOfRangeException(nameof(interactionKind));
            }
        }
    }
}
