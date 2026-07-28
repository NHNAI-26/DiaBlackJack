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
        public const string BeelzebubKey = "beelzebub";
        public const string MephistophelesKey = "mephistopheles";
        public const string AsmodeusKey = "asmodeus";
        public const string AzazelKey = "azazel";
        public const string PaimonKey = "paimon";
        public const string BelialKey = "belial";
        public const string BaphometKey = "baphomet";
        public const string LuciferKey = "lucifer";

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
                    "리볼버 첫 예측 실패 시 한 번 더 예측한다.",
                    "계약 시 영혼 1, 두 예측 모두 실패 시 영혼 1"),
                new DemonContractDefinition(
                    BeelzebubKey,
                    "바알제붑",
                    DemonContractKind.Beelzebub,
                    baseSoulCost,
                    "버스트를 영혼과 소유자가 고른 양측 공개 카드로 대신한다.",
                    "계약 시 영혼 1, 공개 카드 3장 이하일 때 스탠드 불가"),
                new DemonContractDefinition(
                    MephistophelesKey,
                    "메피스토펠레스",
                    DemonContractKind.Mephistopheles,
                    baseSoulCost,
                    "보위 나이프의 공개 합 버스트 판정을 강화한다.",
                    "계약 시 영혼 1, 비버스트 시 자신의 비공개 카드 공개"),
                new DemonContractDefinition(
                    AsmodeusKey,
                    "아스모데우스",
                    DemonContractKind.Asmodeus,
                    baseSoulCost,
                    "차례 시작에 스탠드하지 않은 상대를 히트시킬 수 있다.",
                    "계약 시 영혼 1, 숫자 6 이하 카드 사용 불가"),
                new DemonContractDefinition(
                    AzazelKey,
                    "아자젤",
                    DemonContractKind.Azazel,
                    baseSoulCost,
                    "사용 완료 공개 카드를 계약 시와 히트 뒤 재사용 가능하게 한다.",
                    "계약 시 영혼 1, 같은 숫자 공개 카드 유입 시 버스트"),
                new DemonContractDefinition(
                    PaimonKey,
                    "파이몬",
                    DemonContractKind.Paimon,
                    baseSoulCost,
                    "상대 버스트 뒤 선택한 덱 위 카드 한 장을 전투 동안 추방한다.",
                    "계약 시 영혼 1, 라운드 승리 합계 18 이하면 버스트"),
                new DemonContractDefinition(
                    BelialKey,
                    "벨리알",
                    DemonContractKind.Belial,
                    baseSoulCost,
                    "차례 시작에 상대 공개 카드를 가져와 즉시 사용한다.",
                    "계약 시 영혼 1, 라운드 시작 영혼 1"),
                new DemonContractDefinition(
                    BaphometKey,
                    "바포메트",
                    DemonContractKind.Baphomet,
                    baseSoulCost,
                    "양쪽 덱에 전투 한정 오망성을 섞고 소진된 쪽을 버스트시킨다.",
                    "계약 시 영혼 1, 자신의 오망성 3장 소진 시 자신 버스트"),
                new DemonContractDefinition(
                    LuciferKey,
                    "루시퍼",
                    DemonContractKind.Lucifer,
                    baseSoulCost,
                    "현재 악마 덱의 후보 중 하나와 추가로 계약할 수 있다.",
                    "계약 시 영혼 1, 개별 대가 영혼 1과 선택한 악마의 대가"),
            };
        }
    }
}
