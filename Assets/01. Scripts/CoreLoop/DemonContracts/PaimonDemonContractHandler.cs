namespace DiaBlackJack.CoreLoop
{
    public sealed class PaimonRuntimeState : DemonContractRuntimeState
    {
    }

    internal sealed class PaimonDemonContractHandler :
        IDemonContractHandler,
        IDemonContractOpponentBustChoiceHandler,
        IDemonContractRoundEndBustHandler
    {
        public const int OwnerDeckOptionId = 0;
        public const int OpponentDeckOptionId = 1;
        public const int SkipExileOptionId = 0;
        public const int FirstCardOptionId = 1;
        public const int SecondCardOptionId = 2;
        public const int PeekCardCount = 2;
        public const int MaximumSafeRoundEndTotal = 18;

        public DemonContractKind Kind => DemonContractKind.Paimon;

        public DemonContractRuntimeState Activate(DemonContractContext context)
        {
            return new PaimonRuntimeState();
        }

        public bool CanChooseExileAfterOpponentBust(DemonContractContext context)
        {
            return context.OwnerDeckCanDraw(PeekCardCount) ||
                context.OpponentDeckCanDraw(PeekCardCount);
        }

        public bool BustsOwnerAtRoundEnd(DemonContractContext context)
        {
            return context.OwnerHandValue.Total <= MaximumSafeRoundEndTotal;
        }
    }
}
