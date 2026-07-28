using System;
using System.Collections.Generic;

namespace DiaBlackJack.CoreLoop
{
    public sealed class BaphometRuntimeState : DemonContractRuntimeState
    {
    }

    internal static class BaphometPentagramCatalog
    {
        public const string KeyPrefix = "baphomet-pentagram-";

        private static readonly CardDefinition[] DefinitionsByRank =
        {
            null,
            Create(rank: 1),
            Create(rank: 2),
            Create(rank: 3),
            Create(rank: 4),
            Create(rank: 5)
        };

        public static IReadOnlyList<int> OpponentRanks { get; } =
            Array.AsReadOnly(new[] { 1, 2, 3, 4, 5 });

        public static IReadOnlyList<int> OwnerRanks { get; } =
            Array.AsReadOnly(new[] { 1, 2, 3 });

        public static CardDefinition GetByRank(int rank)
        {
            if (rank < 1 || rank > 5)
            {
                throw new ArgumentOutOfRangeException(nameof(rank));
            }

            return DefinitionsByRank[rank];
        }

        private static CardDefinition Create(int rank)
        {
            return new CardDefinition(
                $"{KeyPrefix}{rank}",
                "오망성",
                rank,
                CardActivationKind.None,
                CardEffectKind.None);
        }
    }

    internal sealed class BaphometDemonContractHandler :
        IDemonContractHandler
    {
        public DemonContractKind Kind => DemonContractKind.Baphomet;

        public DemonContractRuntimeState Activate(DemonContractContext context)
        {
            context.CreateBaphometPentagrams();
            return new BaphometRuntimeState();
        }
    }
}
