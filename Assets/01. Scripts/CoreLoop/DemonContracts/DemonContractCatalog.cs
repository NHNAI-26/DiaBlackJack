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

        private static readonly ReadOnlyCollection<string>
            PlayerDefaultDemonDeckDefinitionKeys =
                new List<string>
                {
                    SatanKey,
                    BelphegorKey,
                    BeelzebubKey,
                    MammonKey
                }.AsReadOnly();

        private static readonly ReadOnlyCollection<string>
            PrototypeEnabledDemonDefinitionKeys =
                new List<string>
                {
                    SatanKey,
                    BelphegorKey,
                    BeelzebubKey,
                    AsmodeusKey,
                    MammonKey,
                    AzazelKey
                }.AsReadOnly();

        private static DemonContractCatalog DefaultCatalog =
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

        /// <summary>
        /// Replaces prototype data with content converted from Unity authoring assets. The facade
        /// remains pure C# so CoreLoop never acquires a UnityEngine dependency.
        /// </summary>
        public static void Install(CardContentCatalog catalog)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            DefaultCatalog = new DemonContractCatalog(catalog.DemonDefinitions);
        }

        public static IReadOnlyList<string> PlayerDefaultDemonDeckKeys =>
            PlayerDefaultDemonDeckDefinitionKeys;

        public static IReadOnlyList<string> PrototypeEnabledDemonKeys =>
            PrototypeEnabledDemonDefinitionKeys;

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
                    "스탠드할 수 없으며 어떤 이유로도 버스트하지 않습니다.\n" +
                    "자신의 차례에 일반 행동 대신, 사탄의 능력을 사용하고 차례를 끝낼 수 있습니다.\n" +
                    "사탄의 능력은 사탄이 향하는 방향에 따라 달라지며, 능력을 쓸 때마다 방향을 바꿉니다.\n\n" +
                    "정방향: 숫자 2개를 선언합니다. 둘 중 하나라도 상대 비공개 카드와 일치하면 상대를 버스트시킵니다.\n" +
                    "역방향: 상대에게 히트를 강제합니다. 버스트하지 않으면 뽑은 카드를 버립니다.",
                    "계약 직후 종말 카운트를 4 얻습니다. 자신의 차례를 시작할 때마다 종말 카운트가 1 감소합니다.\n" +
                    "자신의 차례를 시작할 때마다 종말 카운트가 0 이하라면, 영혼 2를 잃습니다."),
                new DemonContractDefinition(
                    BelphegorKey,
                    "벨페고르",
                    DemonContractKind.Belphegor,
                    baseSoulCost,
                    "히트 전에 덱 위 카드를 확인합니다.\n" +
                    "그 카드를 그대로 뽑거나 덱 아래로 보내고 차례를 끝냅니다.",
                    "상대가 스탠드하면 다음 자신의 차례 종료 시 강제로 스탠드합니다."),
                new DemonContractDefinition(
                    MammonKey,
                    "마몬",
                    DemonContractKind.Mammon,
                    baseSoulCost,
                    "계약 직후 주사위를 굴립니다.\n" +
                    "자신의 차례에 일반 행동 대신, 주사위를 다시 굴리고 차례를 끝낼 수 있습니다.\n" +
                    "승부 전에 주사위 값을 카드 합에 더할 수 있습니다.\n" +
                    "새 라운드 시작 시, 주사위를 다시 굴립니다.",
                    "주사위가 (해골 그림)이 나오면 버스트합니다."),
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
                    "버스트할 때 영혼 1을 지불하고, 양측 공개 카드 1장씩을 골라 버린 뒤 생존합니다.",
                    "자신의 공개 카드가 3장 이하라면 스탠드할 수 없습니다."),
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
                    "차례 시작 시 스탠드하지 않은 상대에게 히트를 강제할 수 있습니다.",
                    "숫자 7 이하 카드의 효과를 사용할 수 없습니다."),
                new DemonContractDefinition(
                    AzazelKey,
                    "아자젤",
                    DemonContractKind.Azazel,
                    baseSoulCost,
                    "계약 직후, 자신의 공개 카드 효과를 왼쪽부터 오른쪽 순서로 각각 한 번씩 발동합니다.\n" +
                    "히트할 때마다 위 효과를 반복합니다.",
                    "같은 숫자의 공개 카드가 추가되면 버스트합니다."),
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
