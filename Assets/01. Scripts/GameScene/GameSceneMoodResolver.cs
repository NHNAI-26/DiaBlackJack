using DiaBlackJack.CoreLoop;

namespace DiaBlackJack.GameScene
{
    internal static class GameSceneMoodResolver
    {
        internal const string ReadyStage = "readyStage";
        internal const string ShopStage = "shopStage";

        internal static string Resolve(
            GameFlowScreen screen,
            string enemyProfileKey)
        {
            switch (screen)
            {
                case GameFlowScreen.StartingDemonReveal:
                case GameFlowScreen.OpponentSelection:
                    return ReadyStage;
                case GameFlowScreen.Shop:
                    return ShopStage;
                case GameFlowScreen.FinalBossReveal:
                case GameFlowScreen.Combat:
                    return ResolveEnemyMood(enemyProfileKey);
                default:
                    return null;
            }
        }

        private static string ResolveEnemyMood(string enemyProfileKey)
        {
            switch (enemyProfileKey)
            {
                case EnemyCombatProfileCatalog.CowardlyGamblerKey:
                    return "cowardlyGambler";
                case EnemyCombatProfileCatalog.GunslingerKey:
                    return "gunslinger";
                case EnemyCombatProfileCatalog.CultistKey:
                    return "fanatic";
                case EnemyCombatProfileCatalog.TricksterKey:
                    return "fraud";
                case EnemyCombatProfileCatalog.EnforcerKey:
                    return "executor";
                case EnemyCombatProfileCatalog.FinalBossKey:
                    return "bossStage";
                default:
                    return null;
            }
        }
    }
}
