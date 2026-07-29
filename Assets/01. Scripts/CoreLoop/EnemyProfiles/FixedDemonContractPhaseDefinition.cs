using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DiaBlackJack.CoreLoop
{
    public sealed class FixedDemonContractPhaseDefinition
    {
        public FixedDemonContractPhaseDefinition(
            int? activationSoulThreshold,
            string activeDefinitionKey,
            string discardedDefinitionKey)
        {
            if (activationSoulThreshold.HasValue &&
                activationSoulThreshold.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(activationSoulThreshold));
            }

            if (string.IsNullOrWhiteSpace(activeDefinitionKey))
            {
                throw new ArgumentException(
                    "Active demon contract key cannot be empty.",
                    nameof(activeDefinitionKey));
            }

            if (string.IsNullOrWhiteSpace(discardedDefinitionKey))
            {
                throw new ArgumentException(
                    "Discarded demon contract key cannot be empty.",
                    nameof(discardedDefinitionKey));
            }

            if (string.Equals(
                activeDefinitionKey,
                discardedDefinitionKey,
                StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "A fixed phase must activate and discard different demon cards.");
            }

            DemonContractCatalog.Default.GetByKey(activeDefinitionKey);
            DemonContractCatalog.Default.GetByKey(discardedDefinitionKey);

            ActivationSoulThreshold = activationSoulThreshold;
            ActiveDefinitionKey = activeDefinitionKey;
            DiscardedDefinitionKey = discardedDefinitionKey;
        }

        public string ActiveDefinitionKey { get; }

        public int? ActivationSoulThreshold { get; }

        public string DiscardedDefinitionKey { get; }

        internal static IReadOnlyList<FixedDemonContractPhaseDefinition>
            ValidateAndCopy(
                IEnumerable<FixedDemonContractPhaseDefinition> phases,
                int enemyMaximumSoul)
        {
            List<FixedDemonContractPhaseDefinition> copied =
                new List<FixedDemonContractPhaseDefinition>();
            if (phases == null)
            {
                return new ReadOnlyCollection<FixedDemonContractPhaseDefinition>(
                    copied);
            }

            HashSet<string> knownDefinitionKeys =
                new HashSet<string>(StringComparer.Ordinal);
            int? previousThreshold = null;
            foreach (FixedDemonContractPhaseDefinition phase in phases)
            {
                if (phase == null)
                {
                    throw new ArgumentException(
                        "Fixed demon contract phases cannot contain null.",
                        nameof(phases));
                }

                if (copied.Count == 0)
                {
                    if (phase.ActivationSoulThreshold.HasValue)
                    {
                        throw new ArgumentException(
                            "The first fixed demon contract phase must activate at battle start.",
                            nameof(phases));
                    }
                }
                else
                {
                    if (!phase.ActivationSoulThreshold.HasValue ||
                        phase.ActivationSoulThreshold.Value >= enemyMaximumSoul ||
                        (previousThreshold.HasValue &&
                            phase.ActivationSoulThreshold.Value >=
                                previousThreshold.Value))
                    {
                        throw new ArgumentException(
                            "Fixed demon contract thresholds must be positive and strictly descending below maximum soul.",
                            nameof(phases));
                    }

                    previousThreshold = phase.ActivationSoulThreshold;
                }

                if (!knownDefinitionKeys.Add(phase.ActiveDefinitionKey) ||
                    !knownDefinitionKeys.Add(phase.DiscardedDefinitionKey))
                {
                    throw new ArgumentException(
                        "Fixed demon contract phases require unique demon cards.",
                        nameof(phases));
                }

                copied.Add(phase);
            }

            return new ReadOnlyCollection<FixedDemonContractPhaseDefinition>(copied);
        }
    }
}
