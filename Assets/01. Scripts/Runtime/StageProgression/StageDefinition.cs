using System;

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
        }

        public string Id { get; }

        public string DisplayName { get; }

        public StageKind Kind { get; }

        public int EnemyMaximumSoul { get; }

        public int PlayerDeckSeed { get; }

        public int EnemyDeckSeed { get; }
    }
}
