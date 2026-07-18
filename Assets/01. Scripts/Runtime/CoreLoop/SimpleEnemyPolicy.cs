namespace DiaBlackJack.CoreLoop
{
    public enum EnemyAction
    {
        Hit,
        Stand
    }

    public sealed class SimpleEnemyPolicy
    {
        public EnemyAction Decide(HandValue ownHand)
        {
            return ownHand.Total <= 16 ? EnemyAction.Hit : EnemyAction.Stand;
        }
    }
}
