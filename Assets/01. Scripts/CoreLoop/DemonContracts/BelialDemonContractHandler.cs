namespace DiaBlackJack.CoreLoop
{
    public sealed class BelialRuntimeState : DemonContractRuntimeState
    {
    }

    internal sealed class BelialDemonContractHandler :
        IDemonContractHandler,
        IDemonContractOwnerTurnStartChoiceHandler,
        IDemonContractRoundStartHandler
    {
        public const int SkipTransferOptionId = 0;
        public const int FirstTransferOptionId = 1;
        public const int RoundStartSoulCost = 1;

        public DemonContractKind Kind => DemonContractKind.Belial;

        public DemonContractRuntimeState Activate(DemonContractContext context)
        {
            context.DiscardAllOwnerFaceUpCards();
            return new BelialRuntimeState();
        }

        public bool CanOfferOwnerTurnStartChoice(DemonContractContext context)
        {
            return context.OpponentFaceUpCards.Count > 0;
        }

        public void OnRoundStarted(DemonContractContext context)
        {
            context.ApplyOwnerSoulDamage(RoundStartSoulCost);
        }
    }
}
