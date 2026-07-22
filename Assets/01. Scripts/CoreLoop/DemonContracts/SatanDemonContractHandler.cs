using System;

namespace DiaBlackJack.CoreLoop
{
    public sealed class SatanRuntimeState : DemonContractRuntimeState
    {
        internal SatanRuntimeState(int remainingNormalTurns, int powerCardId)
        {
            if (remainingNormalTurns <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(remainingNormalTurns));
            }

            if (powerCardId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(powerCardId));
            }

            RemainingNormalTurns = remainingNormalTurns;
            PowerCardId = powerCardId;
            IsActive = true;
        }

        public bool IsActive { get; private set; }

        public int PowerCardId { get; }

        public int RemainingNormalTurns { get; private set; }

        internal bool AdvanceNormalTurn()
        {
            if (!IsActive)
            {
                return false;
            }

            RemainingNormalTurns--;
            return RemainingNormalTurns == 0;
        }

        internal void End()
        {
            IsActive = false;
            RemainingNormalTurns = 0;
        }
    }

    internal sealed class SatanDemonContractHandler :
        IDemonContractHandler,
        IDemonContractNormalTurnHandler,
        IDemonContractStandRestrictionHandler,
        IDemonContractBustPreventionHandler,
        IDemonContractBattleEndHandler
    {
        public const int InitialNormalTurnCount = 6;
        public const int ExpirationSoulCost = 2;

        public DemonContractKind Kind => DemonContractKind.Satan;

        public DemonContractRuntimeState Activate(DemonContractContext context)
        {
            BlackjackCard power = context.AddOwnerTemporaryFaceUpCard(
                CardDefinitionCatalog.GetByKey(
                    CardDefinitionCatalog.SatanPowerFlameKey));
            return new SatanRuntimeState(InitialNormalTurnCount, power.Id);
        }

        public bool OnNormalTurnStarted(
            DemonContractContext context,
            CombatantSide actorSide)
        {
            SatanRuntimeState state = GetState(context);
            if (!state.AdvanceNormalTurn())
            {
                return false;
            }

            context.ApplyOwnerSoulDamage(ExpirationSoulCost);
            context.TryRemoveOwnerTemporaryCard(state.PowerCardId);
            state.End();
            return true;
        }

        public bool PreventsOwnerStand(DemonContractContext context)
        {
            return GetState(context).IsActive;
        }

        public bool PreventsOwnerBust(DemonContractContext context)
        {
            return GetState(context).IsActive;
        }

        public void OnBattleEnded(DemonContractContext context)
        {
            SatanRuntimeState state = GetState(context);
            context.TryRemoveOwnerTemporaryCard(state.PowerCardId);
            state.End();
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
