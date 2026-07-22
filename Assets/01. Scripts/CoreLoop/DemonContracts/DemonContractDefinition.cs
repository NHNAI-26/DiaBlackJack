using System;

namespace DiaBlackJack.CoreLoop
{
    public sealed class DemonContractDefinition
    {
        public DemonContractDefinition(
            string key,
            string displayName,
            DemonContractKind kind,
            int baseSoulCost,
            string summary,
            string costSummary)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException(
                    "Demon contract definition key cannot be empty.",
                    nameof(key));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException(
                    "Demon contract display name cannot be empty.",
                    nameof(displayName));
            }

            if (!Enum.IsDefined(typeof(DemonContractKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            if (baseSoulCost < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(baseSoulCost),
                    "Demon contract base soul cost cannot be negative.");
            }

            if (string.IsNullOrWhiteSpace(costSummary))
            {
                throw new ArgumentException(
                    "Demon contract cost summary cannot be empty.",
                    nameof(costSummary));
            }

            if (string.IsNullOrWhiteSpace(summary))
            {
                throw new ArgumentException(
                    "Demon contract summary cannot be empty.",
                    nameof(summary));
            }

            Key = key.Trim();
            DisplayName = displayName.Trim();
            Kind = kind;
            BaseSoulCost = baseSoulCost;
            Summary = summary.Trim();
            CostSummary = costSummary.Trim();
        }

        public int BaseSoulCost { get; }

        public string CostSummary { get; }

        public string DisplayName { get; }

        public string Key { get; }

        public DemonContractKind Kind { get; }

        public string Summary { get; }
    }
}
