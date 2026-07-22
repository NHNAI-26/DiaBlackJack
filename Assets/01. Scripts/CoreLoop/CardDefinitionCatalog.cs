using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DiaBlackJack.CoreLoop
{
    public static class CardDefinitionCatalog
    {
        public const string SatanPowerMightKey = "satan-power-might-8";
        public const string SatanPowerFlameKey = "satan-power-flame-10";

        private static readonly ReadOnlyCollection<CardDefinition> Definitions;
        private static readonly Dictionary<string, CardDefinition> DefinitionsByKey;
        private static readonly CardDefinition[] DefaultDefinitionsByRank;

        static CardDefinitionCatalog()
        {
            var definitions = new[]
            {
                new CardDefinition(
                    "standard-ace-1",
                    "에이스",
                    1,
                    CardActivationKind.Passive,
                    CardEffectKind.None),
                new CardDefinition(
                    "standard-plain-2",
                    "기본 카드",
                    2,
                    CardActivationKind.None,
                    CardEffectKind.None),
                new CardDefinition(
                    "standard-plain-3",
                    "기본 카드",
                    3,
                    CardActivationKind.None,
                    CardEffectKind.None),
                new CardDefinition(
                    "standard-plain-4",
                    "기본 카드",
                    4,
                    CardActivationKind.None,
                    CardEffectKind.None),
                new CardDefinition(
                    "crystal-orb-5",
                    "수정 구슬",
                    5,
                    CardActivationKind.Manual,
                    CardEffectKind.CrystalOrb),
                new CardDefinition(
                    "threat-hammer-6",
                    "위협용 해머",
                    6,
                    CardActivationKind.Manual,
                    CardEffectKind.ThreatHammer),
                new CardDefinition(
                    "auto-pistol-7",
                    "리볼버",
                    7,
                    CardActivationKind.Manual,
                    CardEffectKind.AutoPistol),
                new CardDefinition(
                    "auto-pistol-8",
                    "리볼버",
                    8,
                    CardActivationKind.Manual,
                    CardEffectKind.AutoPistol),
                new CardDefinition(
                    "military-knife-9",
                    "보위 나이프",
                    9,
                    CardActivationKind.Manual,
                    CardEffectKind.MilitaryKnife),
                new CardDefinition(
                    "military-knife-10",
                    "보위 나이프",
                    10,
                    CardActivationKind.Manual,
                    CardEffectKind.MilitaryKnife),
                new CardDefinition(
                    SatanPowerMightKey,
                    "사탄의 권능: 괴력",
                    8,
                    CardActivationKind.Manual,
                    CardEffectKind.SatanPower),
                new CardDefinition(
                    SatanPowerFlameKey,
                    "사탄의 권능: 화염",
                    10,
                    CardActivationKind.Manual,
                    CardEffectKind.SatanPower)
            };

            Definitions = Array.AsReadOnly(definitions);
            DefinitionsByKey = new Dictionary<string, CardDefinition>(
                definitions.Length,
                StringComparer.Ordinal);
            DefaultDefinitionsByRank = new CardDefinition[11];

            foreach (CardDefinition definition in definitions)
            {
                DefinitionsByKey.Add(definition.Key, definition);
                if (DefaultDefinitionsByRank[definition.Rank] == null)
                {
                    DefaultDefinitionsByRank[definition.Rank] = definition;
                }
            }
        }

        public static IReadOnlyList<CardDefinition> All => Definitions;

        public static CardDefinition GetByKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Card definition key cannot be empty.", nameof(key));
            }

            if (!DefinitionsByKey.TryGetValue(key, out CardDefinition definition))
            {
                throw new KeyNotFoundException($"Card definition '{key}' does not exist.");
            }

            return definition;
        }

        public static CardDefinition GetDefaultForRank(int rank)
        {
            if (rank < 1 || rank > 10)
            {
                throw new ArgumentOutOfRangeException(nameof(rank), "Card rank must be between 1 and 10.");
            }

            return DefaultDefinitionsByRank[rank];
        }
    }
}
