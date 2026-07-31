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
            bool isCombatVisible = true)
        {
            if (battle == null || !isCombatVisible)
            {
                return new ContractPaperViewModel(0, false);
            }

            int reservedPlayerUse =
                battle.PendingPlayerDemonContractInteraction?.Kind ==
                    DemonContractInteractionKind.ChooseContract
                    ? 1
                    : 0;
            int committedPlayerUses = Math.Max(
                0,
                battle.UsedPlayerBaseDemonContractCount - reservedPlayerUse);
            int totalUses =
                CoreLoopBattle.BasePlayerDemonContractUseLimit +
                CoreLoopBattle.BaseEnemyDemonContractUseLimit;
            int committedUses =
                committedPlayerUses + battle.UsedEnemyBaseDemonContractCount;
            int visibleCount = Math.Max(0, totalUses - committedUses);

            return new ContractPaperViewModel(
                visibleCount,
                battle.PlayerDemonContractAvailability.CanBegin);
        }
    }
}
