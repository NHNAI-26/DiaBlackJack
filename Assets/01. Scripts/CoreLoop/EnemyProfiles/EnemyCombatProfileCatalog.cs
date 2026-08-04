using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DiaBlackJack.CoreLoop
{
    public sealed class EnemyCombatProfileCatalog
    {
        public const string CowardlyGamblerKey = "cowardly-gambler";
        public const string GunslingerKey = "gunslinger";
        public const string CultistKey = "cultist";
        public const string TricksterKey = "trickster";
        public const string EnforcerKey = "enforcer";
        public const string FinalBossKey = "final-boss";

        private static EnemyCombatProfileCatalog DefaultCatalog =
            new EnemyCombatProfileCatalog(CreateDefaultProfiles());

        private readonly Dictionary<string, EnemyCombatProfile> _profilesByKey;
        private readonly Dictionary<string, EnemyProfilePreview> _previewsByKey;

        public EnemyCombatProfileCatalog(IEnumerable<EnemyCombatProfile> profiles)
        {
            if (profiles == null)
            {
                throw new ArgumentNullException(nameof(profiles));
            }

            var profileList = new List<EnemyCombatProfile>();
            var previewList = new List<EnemyProfilePreview>();
            _profilesByKey = new Dictionary<string, EnemyCombatProfile>(StringComparer.Ordinal);
            _previewsByKey = new Dictionary<string, EnemyProfilePreview>(StringComparer.Ordinal);

            foreach (EnemyCombatProfile profile in profiles)
            {
                if (profile == null)
                {
                    throw new ArgumentException("Enemy profile catalog cannot contain null.", nameof(profiles));
                }

                if (!_profilesByKey.TryAdd(profile.Key, profile))
                {
                    throw new ArgumentException(
                        $"Enemy profile key '{profile.Key}' is duplicated.",
                        nameof(profiles));
                }

                EnemyProfilePreview preview = profile.CreatePreview();
                _previewsByKey.Add(profile.Key, preview);
                profileList.Add(profile);
                previewList.Add(preview);
            }

            if (profileList.Count == 0)
            {
                throw new ArgumentException(
                    "Enemy profile catalog must contain at least one profile.",
                    nameof(profiles));
            }

            Profiles = new ReadOnlyCollection<EnemyCombatProfile>(profileList);
            Previews = new ReadOnlyCollection<EnemyProfilePreview>(previewList);
        }

        public static EnemyCombatProfileCatalog Default => DefaultCatalog;

        public static void Install(EnemyCombatProfileCatalog catalog)
        {
            DefaultCatalog = catalog ??
                throw new ArgumentNullException(nameof(catalog));
        }

        public IReadOnlyList<EnemyCombatProfile> Profiles { get; }

        public IReadOnlyList<EnemyProfilePreview> Previews { get; }

        public EnemyCombatProfile GetByKey(string key)
        {
            ValidateKey(key);
            if (!_profilesByKey.TryGetValue(key, out EnemyCombatProfile profile))
            {
                throw new KeyNotFoundException($"Enemy profile '{key}' does not exist.");
            }

            return profile;
        }

        public EnemyProfilePreview GetPreviewByKey(string key)
        {
            ValidateKey(key);
            if (!_previewsByKey.TryGetValue(key, out EnemyProfilePreview preview))
            {
                throw new KeyNotFoundException($"Enemy profile preview '{key}' does not exist.");
            }

            return preview;
        }

        private static EnemyCombatProfile[] CreateDefaultProfiles()
        {
            return new[]
            {
                new EnemyCombatProfile(
                    GunslingerKey,
                    "총잡이",
                    EnemyGrade.Normal,
                    3,
                    EnemyBehaviorPolicyCatalog.Gunslinger,
                    new[]
                    {
                        "standard-ace-1", "standard-plain-2", "standard-plain-3",
                        "standard-plain-4", "crystal-orb-5", "threat-hammer-6",
                        "auto-pistol-7", "auto-pistol-7", "auto-pistol-7",
                        "auto-pistol-7", "auto-pistol-8", "auto-pistol-8",
                        "auto-pistol-8", "auto-pistol-8"
                    },
                    "백발백중을 자처하는 승부사입니다.\n" +
                    "덱에 다수의 리볼버가 있으며, 틈이 보이면 주저 없이 방아쇠를 당깁니다.",
                    EnemyInformationMode.Standard),
                new EnemyCombatProfile(
                    CultistKey,
                    "광신도",
                    EnemyGrade.Normal,
                    5,
                    EnemyBehaviorPolicyCatalog.Cultist,
                    new[]
                    {
                        "standard-ace-1", "standard-ace-1",
                        "standard-plain-2", "standard-plain-2",
                        "standard-plain-3", "standard-plain-3",
                        "standard-plain-4", "standard-plain-4",
                        "crystal-orb-5", "crystal-orb-5",
                        "threat-hammer-6", "threat-hammer-6",
                        "auto-pistol-7", "auto-pistol-7",
                        "auto-pistol-8", "auto-pistol-8",
                        "military-knife-9", "military-knife-9",
                        "military-knife-10", "military-knife-10"
                    },
                    "악마에게 영혼을 바치길 서슴지 않습니다.\n" +
                    "첫 기회가 오면 곧바로 악마와 계약합니다.",
                    EnemyInformationMode.Standard,
                    demonContractDefinitionKeys: new[]
                    {
                        DemonContractCatalog.BelphegorKey,
                        DemonContractCatalog.BeelzebubKey
                    },
                    demonContractCandidateCount: 2),
                new EnemyCombatProfile(
                    TricksterKey,
                    "사기꾼",
                    EnemyGrade.Normal,
                    5,
                    EnemyBehaviorPolicyCatalog.Trickster,
                    new[]
                    {
                        "standard-ace-1", "standard-ace-1", "standard-ace-1",
                        "standard-plain-2", "standard-plain-2", "standard-plain-2",
                        CardDefinitionCatalog.LieDetectorKey,
                        CardDefinitionCatalog.LieDetectorKey,
                        CardDefinitionCatalog.LieDetectorKey,
                        "standard-plain-4", "standard-plain-4", "standard-plain-4",
                        "crystal-orb-5", "crystal-orb-5", "crystal-orb-5",
                        "threat-hammer-6", "auto-pistol-7", "auto-pistol-7"
                    },
                    "그의 손을 유심히 보십시오.\n" +
                    "사기꾼의 체인지 비용은 항상 영혼 1개입니다.",
                    EnemyInformationMode.Standard,
                    changeCostMode: EnemyChangeCostMode.FixedOne),
                new EnemyCombatProfile(
                    CowardlyGamblerKey,
                    "겁쟁이 도박사",
                    EnemyGrade.Normal,
                    3,
                    EnemyBehaviorPolicyCatalog.CowardlyGambler,
                    new[]
                    {
                        "standard-ace-1",
                        "standard-plain-2", "standard-plain-2", "standard-plain-2",
                        "standard-plain-3", "standard-plain-3", "standard-plain-3",
                        "standard-plain-4", "standard-plain-4", "standard-plain-4",
                        "crystal-orb-5", "crystal-orb-5", "crystal-orb-5",
                        "threat-hammer-6", "auto-pistol-7", "auto-pistol-8",
                        "military-knife-9", "military-knife-10"
                    },
                    "강철다리조차 두들겨 보고 건넙니다.\n" +
                    "버스트를 크게 경계하며 빠르게 스탠드합니다.",
                    EnemyInformationMode.Standard),
                new EnemyCombatProfile(
                    EnforcerKey,
                    "집행자",
                    EnemyGrade.Elite,
                    5,
                    EnemyBehaviorPolicyCatalog.Enforcer,
                    new[]
                    {
                        CardDefinitionCatalog.PoisonKey,
                        CardDefinitionCatalog.PoisonKey,
                        CardDefinitionCatalog.PoisonKey,
                        "standard-ace-1", "standard-ace-1", "standard-ace-1",
                        "standard-plain-2", "standard-plain-2",
                        CardDefinitionCatalog.LieDetectorKey,
                        CardDefinitionCatalog.LieDetectorKey,
                        CardDefinitionCatalog.FlamethrowerKey,
                        CardDefinitionCatalog.FlamethrowerKey,
                        CardDefinitionCatalog.PocketWatchKey,
                        CardDefinitionCatalog.PocketWatchKey,
                        "threat-hammer-6", "auto-pistol-7", "auto-pistol-8",
                        "military-knife-9", "military-knife-10"
                    },
                    "시간이 지날수록 당신을 옥죄어 옵니다.\n" +
                    "라운드가 시작될 때마다 당신의 덱에 독극물을 섞어 넣습니다.",
                    EnemyInformationMode.Condensed,
                    injectsPoisonIntoPlayerDeckEachRound: true),
                new EnemyCombatProfile(
                    FinalBossKey,
                    "최종 보스",
                    EnemyGrade.Boss,
                    8,
                    EnemyBehaviorPolicyCatalog.FinalBoss,
                    new[]
                    {
                        "standard-ace-1", "standard-ace-1", "standard-ace-1",
                        "standard-ace-1", "standard-ace-1", "standard-ace-1",
                        "standard-ace-1", "standard-ace-1", "standard-ace-1",
                        "standard-ace-1",
                        "crystal-orb-5", "crystal-orb-5", "crystal-orb-5",
                        "crystal-orb-5", "crystal-orb-5",
                        "threat-hammer-6", "threat-hammer-6",
                        "auto-pistol-7", "auto-pistol-7", "auto-pistol-7",
                        "auto-pistol-7",
                        "military-knife-9", "military-knife-9",
                        "military-knife-9", "military-knife-9"
                    },
                    "악마의 힘을 직접 목도할 시간입니다.\n" +
                    "영혼이 줄어들수록 계약할 악마를 바꾸며 더욱 위험해집니다.",
                    EnemyInformationMode.PhaseDependent,
                    demonContractDefinitionKeys: new[]
                    {
                        DemonContractCatalog.BaphometKey,
                        DemonContractCatalog.MammonKey,
                        DemonContractCatalog.AsmodeusKey,
                        DemonContractCatalog.BeelzebubKey,
                        DemonContractCatalog.AzazelKey,
                        DemonContractCatalog.SatanKey
                    },
                    demonContractCandidateCount: 1,
                    fixedDemonContractPhases: new[]
                    {
                        new FixedDemonContractPhaseDefinition(
                            activationSoulThreshold: null,
                            DemonContractCatalog.BaphometKey,
                            DemonContractCatalog.MammonKey),
                        new FixedDemonContractPhaseDefinition(
                            activationSoulThreshold: 5,
                            DemonContractCatalog.AsmodeusKey,
                            DemonContractCatalog.BeelzebubKey),
                        new FixedDemonContractPhaseDefinition(
                            activationSoulThreshold: 2,
                            DemonContractCatalog.AzazelKey,
                            DemonContractCatalog.SatanKey)
                    })
            };
        }

        private static void ValidateKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Enemy profile key cannot be empty.", nameof(key));
            }
        }
    }
}
