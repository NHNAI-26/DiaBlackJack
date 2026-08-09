using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DiaBlackJack.CoreLoop
{
    public static class CardDefinitionCatalog
    {
        public const string PoisonKey = "poison-1";
        public const string ResurrectionHerbKey = "resurrection-herb-2";
        public const string LieDetectorKey = "lie-detector-3";
        public const string FlamethrowerKey = "flamethrower-4";
        public const string PocketWatchKey = "pocket-watch-5";

        private static ReadOnlyCollection<CardDefinition> Definitions;
        private static Dictionary<string, CardDefinition> DefinitionsByKey;
        private static CardDefinition[] DefaultDefinitionsByRank;

        static CardDefinitionCatalog()
        {
            var definitions = new[]
            {
                new CardDefinition(
                    "standard-ace-1",
                    "에이스",
                    1,
                    CardActivationKind.Passive,
                    CardEffectKind.None,
                    "기본 카드",
                    isStandardDeckDefault: true),
                new CardDefinition(
                    "standard-plain-2",
                    "기본 카드",
                    2,
                    CardActivationKind.None,
                    CardEffectKind.None,
                    "기본 카드",
                    isStandardDeckDefault: true),
                new CardDefinition(
                    "standard-plain-3",
                    "기본 카드",
                    3,
                    CardActivationKind.None,
                    CardEffectKind.None,
                    "기본 카드",
                    isStandardDeckDefault: true),
                new CardDefinition(
                    "standard-plain-4",
                    "기본 카드",
                    4,
                    CardActivationKind.None,
                    CardEffectKind.None,
                    "기본 카드",
                    isStandardDeckDefault: true),
                new CardDefinition(
                    "crystal-orb-5",
                    "수정 구슬",
                    5,
                    CardActivationKind.Manual,
                    CardEffectKind.CrystalOrb,
                    "덱 맨 위 2장 훔쳐보고 1장 가져오기",
                    isStandardDeckDefault: true),
                new CardDefinition(
                    "threat-hammer-6",
                    "위협용 해머",
                    6,
                    CardActivationKind.Manual,
                    CardEffectKind.ThreatHammer,
                    "상대 공개 카드 1장을 버립니다. 상대가 스탠드했다면 스탠드를 취소하고 비공개 카드도 교체합니다.",
                    isStandardDeckDefault: true),
                new CardDefinition(
                    "auto-pistol-7",
                    "리볼버",
                    7,
                    CardActivationKind.Manual,
                    CardEffectKind.AutoPistol,
                    "숫자 하나를 선언합니다. 상대 비공개 카드와 일치하면 상대를 버스트시킵니다.",
                    isStandardDeckDefault: true),
                new CardDefinition(
                    "auto-pistol-8",
                    "리볼버",
                    8,
                    CardActivationKind.Manual,
                    CardEffectKind.AutoPistol,
                    "숫자 하나를 선언합니다. 상대 비공개 카드와 일치하면 상대를 버스트시킵니다.",
                    isStandardDeckDefault: true),
                new CardDefinition(
                    "military-knife-9",
                    "보위 나이프",
                    9,
                    CardActivationKind.Manual,
                    CardEffectKind.MilitaryKnife,
                    "적에게 공개카드 1장 강제로 뽑게 함",
                    isStandardDeckDefault: true),
                new CardDefinition(
                    "military-knife-10",
                    "보위 나이프",
                    10,
                    CardActivationKind.Manual,
                    CardEffectKind.MilitaryKnife,
                    "적에게 공개카드 1장 강제로 뽑게 함",
                    isStandardDeckDefault: true),
                new CardDefinition(
                    PoisonKey,
                    "독극물",
                    1,
                    CardActivationKind.Automatic,
                    CardEffectKind.Poison,
                    "즉시 스탠드하거나 영혼 3을 겁니다. 영혼이 3 미만이면 남은 영혼을 모두 겁니다. 영혼을 걸고 승리하면 영혼 5를 회복합니다."),
                new CardDefinition(
                    ResurrectionHerbKey,
                    "부활초",
                    2,
                    CardActivationKind.Automatic,
                    CardEffectKind.ResurrectionHerb,
                    "양측은 각자 선택하여 패를 모두 버리고 비공개 카드 1장과 공개 카드 1장을 다시 받을 수 있습니다."),
                new CardDefinition(
                    LieDetectorKey,
                    "거짓말 탐지기",
                    3,
                    CardActivationKind.Automatic,
                    CardEffectKind.LieDetector,
                    "숫자를 선언해 상대 비공개 카드의 이상·미만 확인"),
                new CardDefinition(
                    FlamethrowerKey,
                    "화염 방사기",
                    4,
                    CardActivationKind.Automatic,
                    CardEffectKind.Flamethrower,
                    "양측이 공개 카드 1장씩 선택해 버림"),
                new CardDefinition(
                    PocketWatchKey,
                    "회중시계",
                    5,
                    CardActivationKind.Automatic,
                    CardEffectKind.PocketWatch,
                    "사용 완료 수동 카드 1장을 재활성화")
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

        /// <summary>
        /// Installs Unity-authored content after it has been converted to the pure catalog. The
        /// static facade exists temporarily for legacy rule callers; it never references Unity.
        /// </summary>
        public static void Install(CardContentCatalog catalog)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            Definitions = Array.AsReadOnly(catalog.NormalDefinitions is CardDefinition[] array
                ? array
                : new List<CardDefinition>(catalog.NormalDefinitions).ToArray());
            DefinitionsByKey = new Dictionary<string, CardDefinition>(StringComparer.Ordinal);
            DefaultDefinitionsByRank = new CardDefinition[11];

            foreach (CardDefinition definition in Definitions)
            {
                DefinitionsByKey.Add(definition.Key, definition);
                if (definition.IsStandardDeckDefault)
                {
                    DefaultDefinitionsByRank[definition.Rank] = definition;
                }
            }
        }

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

            return DefaultDefinitionsByRank[rank] ?? throw new InvalidOperationException(
                $"Card rank {rank} has no standard-deck definition.");
        }
    }
}
