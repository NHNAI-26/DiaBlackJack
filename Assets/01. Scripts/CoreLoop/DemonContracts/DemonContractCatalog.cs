using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DiaBlackJack.CoreLoop
{
    public sealed class DemonContractCatalog
    {
        public const string SatanKey = "satan";
        public const string BelphegorKey = "belphegor";
        public const string MammonKey = "mammon";
        public const string LeviathanKey = "leviathan";

        private static readonly DemonContractCatalog DefaultCatalog =
            new DemonContractCatalog(CreateDefaultDefinitions());

        private readonly Dictionary<string, DemonContractDefinition> _definitionsByKey;

        public DemonContractCatalog(IEnumerable<DemonContractDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            var copiedDefinitions = new List<DemonContractDefinition>();
            _definitionsByKey = new Dictionary<string, DemonContractDefinition>(
                StringComparer.Ordinal);

            foreach (DemonContractDefinition definition in definitions)
            {
                if (definition == null)
                {
                    throw new ArgumentException(
                        "Demon contract catalog cannot contain null.",
                        nameof(definitions));
                }

                if (!_definitionsByKey.TryAdd(definition.Key, definition))
                {
                    throw new ArgumentException(
                        $"Demon contract definition key '{definition.Key}' is duplicated.",
                        nameof(definitions));
                }

                copiedDefinitions.Add(definition);
            }

            if (copiedDefinitions.Count == 0)
            {
                throw new ArgumentException(
                    "Demon contract catalog must contain at least one definition.",
                    nameof(definitions));
            }

            Definitions = new ReadOnlyCollection<DemonContractDefinition>(copiedDefinitions);
        }

        public static DemonContractCatalog Default => DefaultCatalog;

        public IReadOnlyList<DemonContractDefinition> Definitions { get; }

        public DemonContractDefinition GetByKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException(
                    "Demon contract definition key cannot be empty.",
                    nameof(key));
            }

            if (!_definitionsByKey.TryGetValue(key, out DemonContractDefinition definition))
            {
                throw new KeyNotFoundException(
                    $"Demon contract definition '{key}' does not exist.");
            }

            return definition;
        }

        private static DemonContractDefinition[] CreateDefaultDefinitions()
        {
            const int baseSoulCost = 1;
            return new[]
            {
                new DemonContractDefinition(
                    SatanKey,
                    "사탄",
                    DemonContractKind.Satan,
                    baseSoulCost,
                    "스탠드와 버스트를 거부하고 종말 카운터를 진행한다.",
                    "계약 시 영혼 1, 개별 대가 영혼 2"),
                new DemonContractDefinition(
                    BelphegorKey,
                    "벨페고르",
                    DemonContractKind.Belphegor,
                    baseSoulCost,
                    "히트 전에 다음 카드를 보고 진행 여부를 결정한다.",
                    "계약 시 영혼 1, 상대 스탠드 이후 강제 스탠드"),
                new DemonContractDefinition(
                    MammonKey,
                    "마몬",
                    DemonContractKind.Mammon,
                    baseSoulCost,
                    "주사위를 굴려 라운드 합계에 선택적으로 더한다.",
                    "계약 시 영혼 1, 선택한 주사위가 6이면 버스트"),
                new DemonContractDefinition(
                    LeviathanKey,
                    "레비아탄",
                    DemonContractKind.Leviathan,
                    baseSoulCost,
                    "리볼버가 높은 상대 합계를 직접 버스트시킬 수 있다.",
                    "계약 시 영혼 1, 효과 실패 시 영혼 1"),
            };
        }
    }
}
