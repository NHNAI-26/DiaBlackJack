namespace DiaBlackJack.CoreLoop
{
    public sealed class BelphegorRuntimeState : DemonContractRuntimeState
    {
        public bool AutoStandPending { get; private set; }

        internal void ReserveAutoStand()
        {
            AutoStandPending = true;
        }

        internal bool TryConsumeAutoStand()
        {
            if (!AutoStandPending)
            {
                return false;
            }

            AutoStandPending = false;
            return true;
        }

        internal void ResetRound()
        {
            AutoStandPending = false;
        }
    }

    internal sealed class BelphegorDemonContractHandler :
        IDemonContractHandler,
        IDemonContractPlayerHitPreviewHandler,
        IDemonContractOwnerTurnHandler
    {
        internal const int KeepTopCardOptionId = 0;
        internal const int MoveTopCardToBottomOptionId = 1;

        public DemonContractKind Kind => DemonContractKind.Belphegor;

        public DemonContractRuntimeState Activate(DemonContractContext context)
        {
            return new BelphegorRuntimeState();
        }

        public bool RequiresOwnerHitPreview(DemonContractContext context)
        {
            return !context.OwnerIsStanding;
        }

        public void OnOwnerTurnStarted(DemonContractContext context)
        {
            if (context.OpponentIsStanding)
            {
                GetState(context).ReserveAutoStand();
            }
        }

        public bool TryConsumeAutoStandAfterOwnerAction(DemonContractContext context)
        {
            return GetState(context).TryConsumeAutoStand();
        }

        public void OnRoundEnded(DemonContractContext context)
        {
            GetState(context).ResetRound();
        }

        private static BelphegorRuntimeState GetState(DemonContractContext context)
        {
            if (!(context.ActiveContract.RuntimeState is BelphegorRuntimeState runtimeState))
            {
                throw new System.InvalidOperationException(
                    "Belphegor contract has an invalid runtime state.");
            }

            return runtimeState;
        }
    }
}
