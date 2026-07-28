namespace DiaBlackJack.CoreLoop
{
    public sealed class LuciferRuntimeState : DemonContractRuntimeState
    {
    }

    internal sealed class LuciferDemonContractHandler : IDemonContractHandler
    {
        internal const int IndividualSoulCost = 1;
        internal const int SkipAdditionalContractOptionId =
            DemonContractDeck.LuciferCandidateCount;

        public DemonContractKind Kind => DemonContractKind.Lucifer;

        public DemonContractRuntimeState Activate(DemonContractContext context)
        {
            context.ApplyOwnerSoulDamage(IndividualSoulCost);
            return new LuciferRuntimeState();
        }
    }
}
