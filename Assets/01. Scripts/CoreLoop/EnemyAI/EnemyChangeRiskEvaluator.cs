namespace DiaBlackJack.CoreLoop
{
    internal static class EnemyChangeRiskEvaluator
    {
        public const int CriticalEnemySoulThreshold = 2;
        public const int LikelyFinishingPlayerSoulThreshold = 2;

        public static bool ShouldAcceptChange(EnemyObservation observation)
        {
            if (observation.EnemyChangeSoulCost <= 0)
            {
                return true;
            }

            if (observation.EnemySoul.Current > CriticalEnemySoulThreshold)
            {
                return true;
            }

            return observation.PlayerSoul.Current <=
                LikelyFinishingPlayerSoulThreshold;
        }
    }
}
