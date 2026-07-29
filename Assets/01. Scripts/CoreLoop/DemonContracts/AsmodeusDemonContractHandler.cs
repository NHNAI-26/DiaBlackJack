namespace DiaBlackJack.CoreLoop
{
    public sealed class AsmodeusRuntimeState : DemonContractRuntimeState
    {
    }

    internal sealed class AsmodeusDemonContractHandler :
        IDemonContractHandler,
        IDemonContractCardUseRestrictionHandler,
        IDemonContractOwnerTurnStartChoiceHandler
    {
        public const int SkipForcedHitOptionId = 0;
        public const int ForceHitOptionId = 1;
        public const int MaximumRestrictedCardRank = 7;

        public DemonContractKind Kind => DemonContractKind.Asmodeus;

        public DemonContractRuntimeState Activate(DemonContractContext context)
        {
            return new AsmodeusRuntimeState();
        }

        public bool PreventsOwnerCardUse(
            DemonContractContext context,
            BlackjackCard card)
        {
            return card.Rank <= MaximumRestrictedCardRank;
        }

        public bool CanOfferOwnerTurnStartChoice(DemonContractContext context)
        {
            return !context.OpponentIsStanding && context.OpponentCanDraw;
        }
    }
}
