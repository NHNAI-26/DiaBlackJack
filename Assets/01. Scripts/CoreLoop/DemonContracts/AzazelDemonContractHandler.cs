namespace DiaBlackJack.CoreLoop
{
    public sealed class AzazelRuntimeState : DemonContractRuntimeState
    {
    }

    internal sealed class AzazelDemonContractHandler :
        IDemonContractHandler,
        IDemonContractOwnerHitHandler,
        IDemonContractFaceUpCardAddedBustHandler
    {
        public DemonContractKind Kind => DemonContractKind.Azazel;

        public DemonContractRuntimeState Activate(DemonContractContext context)
        {
            context.ReactivateOwnerUsedFaceUpManualCards();
            return new AzazelRuntimeState();
        }

        public void OnOwnerHit(DemonContractContext context)
        {
            context.ReactivateOwnerUsedFaceUpManualCards();
        }

        public bool BustsOwnerAfterFaceUpCardAdded(
            DemonContractContext context,
            BlackjackCard addedCard)
        {
            return context.OwnerHasAnotherFaceUpCardWithRank(addedCard);
        }
    }
}
