using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DiaBlackJack.CoreLoop
{
    public sealed class EnemyCombatProfileCatalog
    {
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
            const string policy = EnemyBehaviorPolicyCatalog.Simple;

            return new[]
            {
                new EnemyCombatProfile(
                    GunslingerKey,
                    "총잡이",
                    EnemyGrade.Normal,
                    3,
                    policy,
                    new[]
                    {
                        "standard-ace-1", "standard-plain-2", "standard-plain-3",
                        "standard-plain-4", "auto-pistol-7", "auto-pistol-8",
                        "standard-ace-1", "standard-plain-2", "standard-plain-3",
                        "standard-plain-4"
                    },
                    "공개 정보로 숫자를 추측하고 공격 기회를 노린다.",
                    EnemyInformationMode.Standard),
                new EnemyCombatProfile(
                    CultistKey,
                    "광신도",
                    EnemyGrade.Normal,
                    3,
                    policy,
                    new[]
                    {
                        "standard-ace-1", "standard-plain-2", "standard-plain-3",
                        "standard-plain-4", "standard-plain-2", "standard-plain-3",
                        "standard-plain-4", "standard-ace-1", "standard-plain-2",
                        "standard-plain-4"
                    },
                    "생존보다 공격 기대값을 우선하는 위험한 상대다.",
                    EnemyInformationMode.Standard),
                new EnemyCombatProfile(
                    TricksterKey,
                    "사기꾼",
                    EnemyGrade.Normal,
                    4,
                    policy,
                    new[]
                    {
                        "standard-ace-1", "standard-plain-2", "standard-plain-3",
                        "standard-plain-4", "crystal-orb-5", "crystal-orb-5",
                        "standard-ace-1", "standard-plain-2", "standard-plain-3",
                        "standard-plain-4"
                    },
                    "직접 피해보다 덱과 정보 우위를 먼저 만든다.",
                    EnemyInformationMode.Standard),
                new EnemyCombatProfile(
                    EnforcerKey,
                    "집행관",
                    EnemyGrade.Elite,
                    5,
                    policy,
                    new[]
                    {
                        "standard-ace-1", "standard-plain-2", "standard-plain-3",
                        "standard-plain-4", "threat-hammer-6", "threat-hammer-6",
                        "military-knife-9", "military-knife-10", "standard-plain-3",
                        "standard-plain-4"
                    },
                    "카드 효과로 안전 행동을 방해하고 압박한다.",
                    EnemyInformationMode.Condensed),
                new EnemyCombatProfile(
                    FinalBossKey,
                    "최종 보스",
                    EnemyGrade.Boss,
                    7,
                    policy,
                    new[]
                    {
                        "standard-ace-1", "standard-plain-2", "standard-plain-3",
                        "standard-plain-4", "crystal-orb-5", "threat-hammer-6",
                        "auto-pistol-7", "auto-pistol-8", "military-knife-9",
                        "military-knife-10"
                    },
                    "남은 영혼 구간에 따라 생존과 강행동의 비중이 변한다.",
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
