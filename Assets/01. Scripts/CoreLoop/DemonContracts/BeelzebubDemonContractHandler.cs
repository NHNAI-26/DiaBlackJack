namespace DiaBlackJack.CoreLoop
{
    public sealed class BeelzebubRuntimeState : DemonContractRuntimeState
    {
    }

    internal sealed class BeelzebubDemonContractHandler :
        IDemonContractHandler,
        IDemonContractStandRestrictionHandler,
        IDemonContractBustReplacementHandler
    {
        private const int BustSoulCost = 1;
        private const int MaximumRestrictedFaceUpCardCount = 3;

        public DemonContractKind Kind => DemonContractKind.Beelzebub;

        public DemonContractRuntimeState Activate(DemonContractContext context)
        {
            return new BeelzebubRuntimeState();
        }

        public bool PreventsOwnerStand(DemonContractContext context)
        {
            return context.OwnerFaceUpCardCount <=
                MaximumRestrictedFaceUpCardCount;
        }

        public bool TryReplaceOwnerBust(
            DemonContractContext context,
            out DemonContractEffectResult result)
        {
            result = null;
            if (!context.CanChooseBeelzebubDiscardCards)
            {
                return false;
            }

            context.ApplyOwnerSoulDamage(BustSoulCost);
            result = new DemonContractEffectResult(
                triggered: true,
                bustedTarget: null,
                paidSoulCost: BustSoulCost);
            return true;
        }
    }
}
