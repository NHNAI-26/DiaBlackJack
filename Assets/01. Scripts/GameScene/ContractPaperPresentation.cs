using System;
using DiaBlackJack.CoreLoop;

namespace DiaBlackJack.GameScene
{
    public sealed class ContractPaperViewModel
    {
        public ContractPaperViewModel(int visibleCount, bool canPlayerBegin)
        {
            if (visibleCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(visibleCount));
            }

            VisibleCount = visibleCount;
            CanPlayerBegin = canPlayerBegin;
        }

        public int VisibleCount { get; }

        public bool CanPlayerBegin { get; }
    }

    public static class ContractPaperPresenter
    {
        public static ContractPaperViewModel Create(
            CoreLoopBattle battle,
            bool isCombatVisible = true,
            bool forceDisabled = false)
        {
            if (battle == null || !isCombatVisible)
            {
                return new ContractPaperViewModel(0, false);
            }

            int totalUses =
                CoreLoopBattle.BasePlayerDemonContractUseLimit +
                CoreLoopBattle.BaseEnemyDemonContractUseLimit;
            int usedCount =
                battle.UsedPlayerBaseDemonContractCount +
                battle.UsedEnemyBaseDemonContractCount;
            int visibleCount = Math.Max(0, totalUses - usedCount);

            return new ContractPaperViewModel(
                visibleCount,
                !forceDisabled && battle.PlayerDemonContractAvailability.CanBegin);
        }
    }
}
