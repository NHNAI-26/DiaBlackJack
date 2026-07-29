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

        private static readonly EnemyCombatProfileCatalog DefaultCatalog =
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
                    "공개 정보로 숫자를 추측하고 공격 기회를 노린다.",
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
                    "계약을 성사시킬 때까지 집요하게 악마의 힘을 좇는다.",
                    EnemyInformationMode.Standard,
                    demonContractDefinitionKeys: new[]
                    {
                        DemonContractCatalog.BelphegorKey,
                        DemonContractCatalog.BeelzebubKey,
                        DemonContractCatalog.BelialKey
                    },
                    demonContractCandidateCount: 3),
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
                    "직접 피해보다 덱과 정보 우위를 먼저 만든다.",
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
                    "낮은 숫자 중심의 덱으로 공개 합 15부터 빠르게 스탠드한다.",
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
                    "독극물과 강제 행동으로 장기전의 안전 지대를 무너뜨린다.",
                    EnemyInformationMode.Condensed,
                    demonContractDefinitionKeys: new[]
                    {
                        DemonContractCatalog.PaimonKey
                    },
                    demonContractCandidateCount: 1,
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
                    "영혼 구간마다 계약과 강행동의 양상을 바꾸는 최종 상대다.",
                    EnemyInformationMode.PhaseDependent)
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
