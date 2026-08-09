using System;
using System.Collections.Generic;
using DiaBlackJack.CoreLoop;

namespace DiaBlackJack.GameScene
{
    public sealed class ContractPaperViewModel
    {
        public ContractPaperViewModel(
            int visibleCount,
            bool canPlayerBegin,
            string availableDemonNames = "없음")
        {
            if (visibleCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(visibleCount));
            }

            VisibleCount = visibleCount;
            CanPlayerBegin = canPlayerBegin;
            AvailableDemonNames = string.IsNullOrWhiteSpace(availableDemonNames)
                ? "없음"
                : availableDemonNames;
        }

        public int VisibleCount { get; }

        public bool CanPlayerBegin { get; }

        public string AvailableDemonNames { get; }
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
                !forceDisabled && battle.PlayerDemonContractAvailability.CanBegin,
                CreateAvailableDemonNames(battle.PlayerDemonDeck));
        }

        private static string CreateAvailableDemonNames(DemonContractDeck deck)
        {
            if (deck == null)
            {
                return "없음";
            }

            IReadOnlyList<DemonContractCard> availableCards =
                deck.GetAvailableCardsSnapshot();
            var availableKeys = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < availableCards.Count; index++)
            {
                availableKeys.Add(availableCards[index].DefinitionKey);
            }

            IReadOnlyList<DemonContractDefinition> definitions =
                DemonContractCatalog.Default.Definitions;
            var names = new List<string>(availableKeys.Count);
            for (int index = 0; index < definitions.Count; index++)
            {
                DemonContractDefinition definition = definitions[index];
                if (availableKeys.Contains(definition.Key))
                {
                    names.Add(definition.DisplayName);
                }
            }

            return names.Count == 0 ? "없음" : string.Join(", ", names);
        }
    }
}
