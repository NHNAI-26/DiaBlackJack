using System;

namespace DiaBlackJack.CoreLoop
{
    public enum SatanContractFace
    {
        Upper,
        Lower
    }

    public sealed class SatanRuntimeState : DemonContractRuntimeState
    {
        internal SatanRuntimeState(int remainingDoomCount)
        {
            if (remainingDoomCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(remainingDoomCount));
            }

            RemainingDoomCount = remainingDoomCount;
            CurrentFace = SatanContractFace.Upper;
        }

        public SatanContractFace CurrentFace { get; private set; }

        public bool DoomPenaltyActive => RemainingDoomCount <= 0;

        public int RemainingDoomCount { get; private set; }

        internal bool BeginOwnerTurn()
        {
            if (RemainingDoomCount > 0)
            {
                RemainingDoomCount--;
            }

            return DoomPenaltyActive;
        }

        internal void CompleteCurrentFaceAbility()
        {
            CurrentFace = CurrentFace == SatanContractFace.Upper
                ? SatanContractFace.Lower
                : SatanContractFace.Upper;
        }
    }

    internal sealed class SatanDemonContractHandler :
        IDemonContractHandler,
        IDemonContractNormalTurnHandler,
        IDemonContractStandRestrictionHandler,
        IDemonContractBustPreventionHandler,
        IDemonContractOwnerTurnStartChoiceHandler
    {
        public const int InitialDoomCount = 4;
        public const int DoomSoulCost = 2;
        public const int SkipAbilityOptionId = 0;
        public const int UseAbilityOptionId = 1;

        public DemonContractKind Kind => DemonContractKind.Satan;

        public DemonContractRuntimeState Activate(DemonContractContext context)
        {
            return new SatanRuntimeState(InitialDoomCount);
        }

        public bool OnNormalTurnStarted(
            DemonContractContext context,
            CombatantSide actorSide)
        {
            SatanRuntimeState state = GetState(context);
            if (actorSide != context.ActiveContract.OwnerSide ||
                !state.BeginOwnerTurn())
            {
                return false;
            }

            context.ApplyOwnerSoulDamage(DoomSoulCost);
            return false;
        }

        public bool CanOfferOwnerTurnStartChoice(DemonContractContext context)
        {
            SatanRuntimeState state = GetState(context);
            return state.CurrentFace == SatanContractFace.Upper
                ? context.OpponentHasSingleHiddenCard
                : context.OpponentCanDraw;
        }

        public bool PreventsOwnerStand(DemonContractContext context)
        {
            return true;
        }

        public bool PreventsOwnerBust(DemonContractContext context)
        {
            return true;
        }

        private static SatanRuntimeState GetState(DemonContractContext context)
        {
            if (!(context.ActiveContract.RuntimeState is SatanRuntimeState state))
            {
                throw new InvalidOperationException(
                    "Satan contract has no Satan runtime state.");
            }

            return state;
        }
    }
}
