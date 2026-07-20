using System;
using DiaBlackJack.StageProgression;

namespace DiaBlackJack.CoreLoop
{
    public sealed class EnemyProfilePreview
    {
        private EnemyProfilePreview(
            string profileKey,
            string displayName,
            EnemyGrade grade,
            int maximumSoul,
            string summary,
            BattleRewardTier expectedRewardTier)
        {
            ProfileKey = profileKey;
            DisplayName = displayName;
            Grade = grade;
            MaximumSoul = maximumSoul;
            Summary = summary;
            ExpectedRewardTier = expectedRewardTier;
        }

        public string DisplayName { get; }

        public BattleRewardTier ExpectedRewardTier { get; }

        public EnemyGrade Grade { get; }

        public int MaximumSoul { get; }

        public string ProfileKey { get; }

        public string Summary { get; }

        internal static EnemyProfilePreview FromProfile(EnemyCombatProfile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            BattleRewardTier rewardTier = profile.Grade == EnemyGrade.Normal
                ? BattleRewardTier.Normal
                : BattleRewardTier.HighGrade;

            return new EnemyProfilePreview(
                profile.Key,
                profile.DisplayName,
                profile.Grade,
                profile.MaximumSoul,
                profile.Summary,
                rewardTier);
        }
    }
}
