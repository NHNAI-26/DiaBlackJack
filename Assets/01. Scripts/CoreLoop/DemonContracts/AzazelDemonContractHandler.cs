namespace DiaBlackJack.CoreLoop
{
    public sealed class AzazelRuntimeState : DemonContractRuntimeState
    {
    }

    internal sealed class AzazelDemonContractHandler :
        IDemonContractHandler,
        IDemonContractFaceUpCardAddedBustHandler
    {
        public DemonContractKind Kind => DemonContractKind.Azazel;

        public DemonContractRuntimeState Activate(DemonContractContext context)
        {
            return new AzazelRuntimeState();
        }

        public bool BustsOwnerAfterFaceUpCardAdded(
            DemonContractContext context,
            BlackjackCard addedCard)
        {
            return context.OwnerHasAnotherFaceUpCardWithRank(addedCard);
        }
    }
}
