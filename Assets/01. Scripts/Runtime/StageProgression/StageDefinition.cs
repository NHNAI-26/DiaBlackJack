using System;
using DiaBlackJack.CoreLoop;

namespace DiaBlackJack.StageProgression
{
    public sealed class StageDefinition
    {
        public StageDefinition(
            string id,
            string displayName,
            StageKind kind,
            int enemyMaximumSoul,
            int playerDeckSeed,
            int enemyDeckSeed)
            : this(
                id,
                displayName,
                kind,
                enemyMaximumSoul,
                playerDeckSeed,
                enemyDeckSeed,
                null)
        {
        }

        private StageDefinition(
            string id,
            string displayName,
            StageKind kind,
            int enemyMaximumSoul,
            int playerDeckSeed,
            int enemyDeckSeed,
            string battleProfileKey)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Stage id cannot be empty.", nameof(id));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Stage display name cannot be empty.", nameof(displayName));
            }

            if (!Enum.IsDefined(typeof(StageKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind), "Stage kind is invalid.");
            }

            if (enemyMaximumSoul <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(enemyMaximumSoul),
                    "Enemy maximum soul must be positive.");
            }

            Id = id.Trim();
            DisplayName = displayName.Trim();
            Kind = kind;
            EnemyMaximumSoul = enemyMaximumSoul;
            PlayerDeckSeed = playerDeckSeed;
            EnemyDeckSeed = enemyDeckSeed;
            BattleProfileKey = battleProfileKey;
        }

        public string BattleProfileKey { get; }

        public string Id { get; }

        public string DisplayName { get; }

        public StageKind Kind { get; }

        public int EnemyMaximumSoul { get; }

        public int PlayerDeckSeed { get; }

        public int EnemyDeckSeed { get; }

        public static StageDefinition CreateForEnemyProfile(
            string id,
            string displayName,
            StageKind kind,
            string battleProfileKey,
            int playerDeckSeed,
            int enemyDeckSeed)
        {
            EnemyProfilePreview preview =
                EnemyCombatProfileCatalog.Default.GetPreviewByKey(battleProfileKey);
            bool isBossProfile = preview.Grade == EnemyGrade.Boss;
            bool isFinalBossStage = kind == StageKind.FinalBossCombat;
            if (isBossProfile != isFinalBossStage)
            {
                throw new ArgumentException(
                    "Boss profiles must be used only for final boss stages, and final boss stages require a boss profile.",
                    nameof(battleProfileKey));
            }

            return new StageDefinition(
                id,
                displayName,
                kind,
                preview.MaximumSoul,
                playerDeckSeed,
                enemyDeckSeed,
                preview.ProfileKey);
        }
    }
}
