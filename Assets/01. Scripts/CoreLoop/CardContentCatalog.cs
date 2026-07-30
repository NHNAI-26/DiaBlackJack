using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DiaBlackJack.CoreLoop
{
    /// <summary>
    /// Immutable rule-facing card content. Unity authoring assets are converted to this type before
    /// battle, AI, progression, or save code receives the definitions.
    /// </summary>
    public sealed class CardContentCatalog
    {
        private readonly Dictionary<string, CardDefinition> _normalByKey;
        private readonly Dictionary<string, DemonContractDefinition> _demonByKey;
        private readonly CardDefinition[] _standardDeckDefaultsByRank;

        public CardContentCatalog(
            IEnumerable<CardDefinition> normalDefinitions,
            IEnumerable<DemonContractDefinition> demonDefinitions)
        {
            if (normalDefinitions == null)
            {
                throw new ArgumentNullException(nameof(normalDefinitions));
            }

            if (demonDefinitions == null)
            {
                throw new ArgumentNullException(nameof(demonDefinitions));
            }

            _normalByKey = new Dictionary<string, CardDefinition>(StringComparer.Ordinal);
            _demonByKey = new Dictionary<string, DemonContractDefinition>(StringComparer.Ordinal);
            _standardDeckDefaultsByRank = new CardDefinition[11];

            var copiedNormal = new List<CardDefinition>();
            foreach (CardDefinition definition in normalDefinitions)
            {
                if (definition == null || !_normalByKey.TryAdd(definition.Key, definition))
                {
                    throw new ArgumentException("Normal card keys must be unique.", nameof(normalDefinitions));
                }

                if (definition.IsStandardDeckDefault)
                {
                    if (_standardDeckDefaultsByRank[definition.Rank] != null)
                    {
                        throw new ArgumentException(
                            $"Standard deck rank {definition.Rank} has multiple defaults.",
                            nameof(normalDefinitions));
                    }

                    _standardDeckDefaultsByRank[definition.Rank] = definition;
                }

                copiedNormal.Add(definition);
            }

            if (copiedNormal.Count == 0)
            {
                throw new ArgumentException("At least one normal card definition is required.", nameof(normalDefinitions));
            }

            for (int rank = 1; rank <= 10; rank++)
            {
                if (_standardDeckDefaultsByRank[rank] == null)
                {
                    throw new ArgumentException(
                        $"Standard deck rank {rank} has no default definition.",
                        nameof(normalDefinitions));
                }
            }

            var copiedDemon = new List<DemonContractDefinition>();
            foreach (DemonContractDefinition definition in demonDefinitions)
            {
                if (definition == null || !_demonByKey.TryAdd(definition.Key, definition))
                {
                    throw new ArgumentException("Demon card keys must be unique.", nameof(demonDefinitions));
                }

                copiedDemon.Add(definition);
            }

            if (copiedDemon.Count == 0)
            {
                throw new ArgumentException("At least one demon card definition is required.", nameof(demonDefinitions));
            }

            NormalDefinitions = new ReadOnlyCollection<CardDefinition>(copiedNormal);
            DemonDefinitions = new ReadOnlyCollection<DemonContractDefinition>(copiedDemon);
        }

        public IReadOnlyList<DemonContractDefinition> DemonDefinitions { get; }

        public IReadOnlyList<CardDefinition> NormalDefinitions { get; }

        public DemonContractDefinition GetDemonByKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key) ||
                !_demonByKey.TryGetValue(key, out DemonContractDefinition definition))
            {
                throw new KeyNotFoundException($"Demon card definition '{key}' does not exist.");
            }

            return definition;
        }

        public CardDefinition GetNormalByKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key) ||
                !_normalByKey.TryGetValue(key, out CardDefinition definition))
            {
                throw new KeyNotFoundException($"Normal card definition '{key}' does not exist.");
            }

            return definition;
        }

        public CardDefinition GetStandardDeckDefault(int rank)
        {
            if (rank < 1 || rank > 10)
            {
                throw new ArgumentOutOfRangeException(nameof(rank));
            }

            return _standardDeckDefaultsByRank[rank];
        }
    }
}
